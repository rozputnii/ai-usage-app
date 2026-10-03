using AiUsage.Core.Diagnostics;

namespace AiUsage.Infrastructure.Diagnostics;

internal sealed record DiagnosticEventRecord(
    int SchemaVersion, DateTimeOffset Timestamp, string Severity, string EventId,
    string Component, int ProcessId, string SessionId, long Sequence, Guid? OperationId,
    Guid? ParentOperationId, string? Operation, string? Outcome, double? DurationMs,
    SafeException? Exception = null, bool? Terminating = null, int? ThreadId = null,
    long? LostRecords = null, object? Context = null, Guid? AccountReference = null);

internal sealed record DiagnosticContext(Guid Id, Guid? Parent, string Operation, Guid? AccountReference);

public sealed class DiagnosticScope : IDiagnosticOperation
{
    private readonly FileDiagnostics owner;
    private readonly DiagnosticContext? previous;
    private readonly DiagnosticContext context;
    private readonly long started = System.Diagnostics.Stopwatch.GetTimestamp();
    private string outcome = "completed";
    private int disposed;

    internal DiagnosticScope(FileDiagnostics owner, DiagnosticOperation operation, Guid? accountReference)
    {
        this.owner = owner;
        previous = FileDiagnostics.Operation.Value;
        context = new(Guid.NewGuid(), previous?.Id, operation.ToString(), accountReference ?? previous?.AccountReference);
        FileDiagnostics.Operation.Value = context;
        owner.Event(DiagnosticEvent.OperationStarted, DiagnosticSeverity.Information);
    }

    public void Failed(Exception error)
    {
        outcome = "failed";
        owner.Failure(DiagnosticEvent.OperationFailure, error);
    }

    public void Cancelled() => outcome = "cancelled";
    public void SetOutcome(DiagnosticOutcome outcome) => this.outcome = outcome.ToString();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        owner.Event(outcome is "cancelled" or "Cancelled" or "ExitCancelled" ? DiagnosticEvent.OperationCancelled : DiagnosticEvent.OperationCompleted,
            outcome is "failed" or "Failed" or "RecoveryRequired" ? DiagnosticSeverity.Error : DiagnosticSeverity.Information,
            outcome: outcome, duration: System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        FileDiagnostics.Operation.Value = previous;
    }
}
