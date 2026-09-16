using System.Runtime.CompilerServices;
using AiUsage.Features.CliImport;
using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.Settings;
using AiUsage.Features.SystemStatusPage;

namespace AiUsage.Adapters.Live;

/// <summary>Future AIUs have no product side effects and never return simulated success.</summary>
internal sealed class UnavailableServices : IHistorySource, ICliImportService, IDiagnosticsService,
    IDataManagementService, IRecoveryService, IUpdateService, INotificationPreview
{
    private static Task<UiCommandResult> Unsupported() => Task.FromResult(UiCommandResult.Unsupported);
    public Task<HistoryResult> QueryHistoryAsync(HistoryQuery query, CancellationToken cancellationToken) => Task.FromResult(new HistoryResult(HistoryOutcome.Empty, [], false, false));
    public async IAsyncEnumerable<CliCandidate> DiscoverAsync([EnumeratorCancellation] CancellationToken cancellationToken) { await Task.CompletedTask; yield break; }
    public async IAsyncEnumerable<CliImportOutcome> ImportAsync(IReadOnlyList<string> candidateIds, bool reimport, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var id in candidateIds) yield return new(id, CliImportOutcomeKind.Unsupported);
        await Task.CompletedTask;
    }
    public async IAsyncEnumerable<HealthCheckStep> RunHealthCheckAsync([EnumeratorCancellation] CancellationToken cancellationToken) { await Task.CompletedTask; yield break; }
    public Task<string> PreviewDiagnosticsAsync(CancellationToken cancellationToken) => Task.FromResult(string.Empty);
    public Task<string> PreviewLogsAsync(CancellationToken cancellationToken) => Task.FromResult(string.Empty);
    public Task<ExportPreview> PreviewExportAsync(IProgress<double> progress, CancellationToken cancellationToken) => Task.FromResult(new ExportPreview("", 0, 0, false, false));
    public Task<IReadOnlyList<ReplaceCandidate>> ListReplaceCandidatesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ReplaceCandidate>>([]);
    public Task<ReplaceValidation> ValidateReplaceImportAsync(string candidateId, CancellationToken cancellationToken) => Task.FromResult(new ReplaceValidation(false, "Dialog_OperationFailed", null, null, null, null));
    public Task<UiCommandResult> ApplyReplaceImportAsync(string candidateId, CancellationToken cancellationToken) => Unsupported();
    public Task<UiCommandResult> FactoryResetAsync(CancellationToken cancellationToken) => Unsupported();
    public Task<UiCommandResult> DeleteAccountDataAsync(string accountId, CancellationToken cancellationToken) => Unsupported();
    public Task<string> PreviewDataFolderAsync(CancellationToken cancellationToken) => Task.FromResult(string.Empty);
    public RecoveryDetails Details { get; } = new(RecoveryState.None, "", "", "");
    public Task<UiCommandResult> RetryAsync(IProgress<double> progress, CancellationToken cancellationToken) => Unsupported();
    public Task<IReadOnlyList<RecoveryCheckpoint>> ListCheckpointsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RecoveryCheckpoint>>([]);
    public Task<UiCommandResult> RestoreCheckpointAsync(string checkpointId, IProgress<double> progress, CancellationToken cancellationToken) => Unsupported();
    public Task<UiCommandResult> CheckAsync(CancellationToken cancellationToken) => Unsupported();
    public Task<UiCommandResult> DownloadAsync(CancellationToken cancellationToken) => Unsupported();
    public Task<UiCommandResult> SetChannelAsync(UpdateChannel channel, CancellationToken cancellationToken) => Unsupported();
    public Task<UiCommandResult> RestartAndUpdateAsync(CancellationToken cancellationToken) => Unsupported();
    public Task<UiCommandResult> OverrideCompatibilityBlockAsync(CancellationToken cancellationToken) => Unsupported();
    public Task<NotificationPreviewResult> PreviewAsync(NotificationTarget? target, CancellationToken cancellationToken) => Task.FromResult(new NotificationPreviewResult(NotificationDelivery.Unknown, target));
}
