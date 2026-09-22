using AiUsage.Core.Persistence;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;

namespace AiUsage.Adapters.Live;

internal sealed class LiveRecoveryService : IRecoveryService, IDisposable
{
    private readonly IStateMaintenance maintenance;
    private readonly LiveUsageSource source;
    private readonly string folder;
    private readonly Func<Task> openFolder;
    private readonly Func<CancellationToken, Task>? export;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool stopped;
    private bool running;
    private Func<Task>? resume;

    public LiveRecoveryService(IStateMaintenance maintenance, LiveUsageSource source, string folder, Func<Task> openFolder,
        Func<CancellationToken, Task>? export = null)
    {
        this.maintenance = maintenance;
        this.source = source;
        this.folder = folder;
        this.openFolder = openFolder;
        this.export = export;
        source.SetRecovery(RecoveryState.Interrupted);
    }

    public RecoveryDetails Details => new(Map(maintenance.Current.Condition),
        maintenance.Current.LayoutVersion?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown",
        maintenance.Current.Checkpoint?.Id ?? string.Empty, folder);

    internal async Task InitializeAsync(Func<Task> startProduct)
    {
        resume = startProduct;
        await RunAsync(maintenance.InitializeAsync, CancellationToken.None);
    }

    public async Task<UiCommandResult> RetryAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (maintenance.Current.Condition == MaintenanceCondition.NewerSchema) return UiCommandResult.Unsupported;
        return await RunAsync(maintenance.RetryAsync, cancellationToken);
    }

    public Task<IReadOnlyList<RecoveryCheckpoint>> ListCheckpointsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RecoveryCheckpoint> checkpoints = !running && maintenance.Current.Condition != MaintenanceCondition.NewerSchema &&
            maintenance.Current.Checkpoint is { } checkpoint
            ? [new(checkpoint.Id, checkpoint.CreatedAt, "Recovery_CheckpointBeforeMigration", checkpoint.LayoutVersion, 0, 0)] : [];
        return Task.FromResult(checkpoints);
    }

    public Task<UiCommandResult> RestoreCheckpointAsync(string checkpointId, IProgress<double> progress, CancellationToken cancellationToken) =>
        maintenance.Current.Condition == MaintenanceCondition.NewerSchema ? Task.FromResult(UiCommandResult.Unsupported) :
        RunAsync(token => maintenance.RestoreAsync(checkpointId, token), cancellationToken);

    private async Task<UiCommandResult> RunAsync(Func<CancellationToken, Task<MaintenanceReport>> operation, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (stopped) return UiCommandResult.Cancelled;
            if (running) return UiCommandResult.Unsupported;
            var report = await operation(token);
            if (stopped) return UiCommandResult.Cancelled;
            source.SetRecovery(Map(report.Condition));
            if (report.Condition != MaintenanceCondition.Ready) return UiCommandResult.Failed();
            running = true;
            if (resume is not null) await resume();
            return UiCommandResult.Succeeded;
        }
        catch (OperationCanceledException) { return UiCommandResult.Cancelled; }
        finally { gate.Release(); }
    }

    internal async Task StopAsync()
    {
        stopped = true;
        await gate.WaitAsync();
        gate.Release();
    }

    public Task<string> PreviewDiagnosticsAsync(CancellationToken cancellationToken) => Task.FromResult(
        $"Recovery: {maintenance.Current.Condition}\nLayout: {Details.SchemaText}\nCheckpoint available: {maintenance.Current.Checkpoint is not null}");
    public async Task<UiCommandResult> OpenDataFolderAsync(CancellationToken cancellationToken)
    {
        try { await openFolder(); return UiCommandResult.Succeeded; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { return UiCommandResult.Failed(); }
    }
    public async Task<UiCommandResult> ExportDiagnosticsAsync(CancellationToken cancellationToken)
    {
        if (export is null) return UiCommandResult.Unsupported;
        try { await export(cancellationToken); await openFolder(); return UiCommandResult.Succeeded; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { return UiCommandResult.Failed(); }
    }

    private static RecoveryState Map(MaintenanceCondition condition) => condition switch
    {
        MaintenanceCondition.Ready => RecoveryState.None,
        MaintenanceCondition.NewerSchema => RecoveryState.NewerSchema,
        MaintenanceCondition.RestoreFailed => RecoveryState.RestoreFailed,
        _ => RecoveryState.Interrupted
    };

    public void Dispose() => gate.Dispose();
}
