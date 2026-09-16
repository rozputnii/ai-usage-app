using AiUsage.Features.Presentation;

namespace AiUsage.Features.Recovery;

public sealed record RecoveryDetails(RecoveryState State, string SchemaText, string CheckpointId, string DataFolderPreview);

public sealed record RecoveryCheckpoint(string Id, DateTimeOffset CreatedAt, string KindKey, int SchemaVersion, int Accounts, long HistoryRows);

/// <summary>
/// Adapter boundary for the blocking recovery surface (D-110, D-146–D-148). Nothing is wiped or restored without an
/// explicit confirmed call; failures leave current data unchanged.
/// </summary>
public interface IRecoveryService
{
    RecoveryDetails Details { get; }

    Task<UiCommandResult> RetryAsync(IProgress<double> progress, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecoveryCheckpoint>> ListCheckpointsAsync(CancellationToken cancellationToken);

    Task<UiCommandResult> RestoreCheckpointAsync(string checkpointId, IProgress<double> progress, CancellationToken cancellationToken);

    Task<string> PreviewDiagnosticsAsync(CancellationToken cancellationToken);
}
