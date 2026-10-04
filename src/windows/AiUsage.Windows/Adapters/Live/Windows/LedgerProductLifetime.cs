using System.Diagnostics;
using AiUsage.Adapters.Live;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Persistence;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Infrastructure.Diagnostics;
using AiUsage.Infrastructure.Persistence;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;

namespace AiUsage.Composition;

/// <summary>Desktop maintenance, refresh timer, recovery actions and writer drain for the live Ledger.</summary>
internal sealed class LedgerProductLifetime : IDisposable
{
    private readonly string root;
    private readonly LiveLedgerSource source;
    private readonly StateMaintenance maintenance;
    private readonly ApplicationDiagnostics diagnostics;
    private readonly DispatcherQueueTimer timer;
    private readonly string restartArguments;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource shutdown = new();
    private bool started;
    private bool deletionStarted;
    private Task? stopping;

    public LedgerProductLifetime(string root, LiveLedgerSource source, StateMaintenance maintenance,
        ApplicationDiagnostics diagnostics, DispatcherQueue queue, string restartArguments = "")
    {
        this.root = root; this.source = source; this.maintenance = maintenance; this.diagnostics = diagnostics;
        this.restartArguments = restartArguments;
        timer = queue.CreateTimer(); timer.Interval = TimeSpan.FromMinutes(1); timer.Tick += Tick;
        source.DeleteData = DeleteAsync;
        source.SupportAction = SupportAsync;
        source.DiagnosticsPreview = PreviewAsync;
    }

    public async Task InitializeAsync()
    {
        if (new OwnedDataDeletion(root).Pending) { await DeleteAsync(CancellationToken.None); return; }
        await RecoverAsync(false, false, shutdown.Token);
    }

    private async Task<CommandOutcome> RecoverAsync(bool retry, bool restore, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (shutdown.IsCancellationRequested) return CommandOutcome.Unavailable;
            if (started) return CommandOutcome.Unavailable;
            var report = restore && maintenance.Current.Checkpoint is { } checkpoint
                ? await maintenance.RestoreAsync(checkpoint.Id, token)
                : retry ? await maintenance.RetryAsync(token) : await maintenance.InitializeAsync(token);
            if (report.Condition != MaintenanceCondition.Ready)
            {
                await source.SetRecoveryAsync(new(report.Condition switch
                {
                    MaintenanceCondition.NewerSchema => "These local data were written by a newer app. Open them with that version.",
                    MaintenanceCondition.DeletionPending => "Stored-data deletion is incomplete. Retry to finish it.",
                    MaintenanceCondition.InUse => "AI Usage is already using these local data in another process. Open its window from the tray, or exit it and retry here. Your data is unchanged.",
                    _ => "Local data needs recovery before accounts can be opened. Existing data is preserved."
                }, report.Condition != MaintenanceCondition.NewerSchema, report.Checkpoint is not null && report.Condition != MaintenanceCondition.NewerSchema));
                return CommandOutcome.Unavailable;
            }
            var legacy = await new PresentationPreferenceFile(Path.Combine(root, "preferences")).ReadAsync(token);
            await source.InitializeAsync(legacy, token);
            started = true;
            await source.SetRecoveryAsync(null);
            timer.Start();
            await source.TickAsync(token);
            return CommandOutcome.Done;
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { return CommandOutcome.Unavailable; }
        catch (Exception error)
        {
            diagnostics.Failure(DiagnosticEvent.StartupFailure, error);
            await source.SetRecoveryAsync(new("Account data could not be opened. Close and reopen the app after checking local data.", false, false));
            return CommandOutcome.Unavailable;
        }
        finally { gate.Release(); }
    }

    private async void Tick(DispatcherQueueTimer sender, object args)
    {
        try { await source.TickAsync(shutdown.Token); }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
        catch (Exception error) { diagnostics.Failure(DiagnosticEvent.BackgroundFailure, error); }
    }

    private async Task<CommandOutcome> SupportAsync(LedgerSupportAction action, CancellationToken token)
    {
        try
        {
            switch (action)
            {
                case LedgerSupportAction.RetryRecovery:
                    return deletionStarted || new OwnedDataDeletion(root).Pending ? await DeleteAsync(token) : await RecoverAsync(true, false, token);
                case LedgerSupportAction.RestorePreferences:
                    return maintenance.Current.Checkpoint is null ? CommandOutcome.Unavailable : await RecoverAsync(true, true, token);
                case LedgerSupportAction.OpenDataFolder:
                    DiagnosticFiles.ValidateDirectory(root);
                    Process.Start(new ProcessStartInfo(root) { UseShellExecute = true });
                    return CommandOutcome.Done;
                case LedgerSupportAction.OpenLogs:
                    if (diagnostics.Files is not { } files) return CommandOutcome.Unavailable;
                    await files.FlushAsync(); DiagnosticFiles.ValidateDirectory(files.DirectoryPath);
                    Process.Start(new ProcessStartInfo(files.DirectoryPath) { UseShellExecute = true });
                    return CommandOutcome.Done;
                case LedgerSupportAction.ExportRecovery:
                    if (deletionStarted) return CommandOutcome.Unavailable;
                    await maintenance.ExportDiagnosticsAsync(token);
                    return CommandOutcome.Done;
                default: return CommandOutcome.Unavailable;
            }
        }
        catch (Exception error)
        {
            diagnostics.Failure(DiagnosticEvent.OperationFailure, error);
            return CommandOutcome.Unavailable;
        }
    }

    private async Task<string> PreviewAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var summary = $"Recovery: {maintenance.Current.Condition}\nLayout: {maintenance.Current.LayoutVersion}\nLegacy preferences checkpoint: {maintenance.Current.Checkpoint is not null}\n";
        return summary + (diagnostics.Files is { } files ? await files.PreviewAsync() : "Logs unavailable");
    }

    private async Task<CommandOutcome> DeleteAsync(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            deletionStarted = true;
            timer.Stop();
            await source.StopAsync();
            maintenance.Dispose(); // Releases the product lease; the deletion coordinator reacquires it exclusively.
            await diagnostics.StopForDeletionAsync();
            await new OwnedDataDeletion(root).RunAsync(confirmed: true, token);
            // The native restart API handles both packaged and unpackaged activation. Success terminates this process.
            await Task.Run(() => AppInstance.Restart(restartArguments), CancellationToken.None);
            await source.SetRecoveryAsync(new("Stored data was deleted. Close and reopen AI Usage to start again.", false, false));
            return CommandOutcome.Done;
        }
        catch (Exception)
        {
            // Logging is intentionally stopped; never create new stored data while a reset is pending.
            await source.SetRecoveryAsync(new("Stored-data deletion is incomplete. Unknown or unavailable files were preserved. Retry after checking the data folder.", true, false));
            return CommandOutcome.Unavailable;
        }
        finally { gate.Release(); }
    }

    public Task StopAsync() => stopping ??= StopCoreAsync();
    private async Task StopCoreAsync()
    {
        timer.Stop();
        await shutdown.CancelAsync();
        await source.StopAsync();
        await gate.WaitAsync(); gate.Release();
    }
    public void Dispose()
    {
        timer.Stop(); timer.Tick -= Tick;
        gate.Dispose(); shutdown.Dispose();
    }
}
