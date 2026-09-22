namespace AiUsage.Core.Persistence;

public enum MaintenanceCondition { Ready, Interrupted, NewerSchema, RestoreFailed }
public sealed record MaintenanceCheckpoint(string Id, DateTimeOffset CreatedAt, int LayoutVersion);
public sealed record MaintenanceReport(MaintenanceCondition Condition, int? LayoutVersion, MaintenanceCheckpoint? Checkpoint);

/// <summary>Credential-free gate before any durable product workflow starts.</summary>
public interface IStateMaintenance
{
    MaintenanceReport Current { get; }
    Task<MaintenanceReport> InitializeAsync(CancellationToken cancellationToken);
    Task<MaintenanceReport> RetryAsync(CancellationToken cancellationToken);
    Task<MaintenanceReport> RestoreAsync(string checkpointId, CancellationToken cancellationToken);
}
