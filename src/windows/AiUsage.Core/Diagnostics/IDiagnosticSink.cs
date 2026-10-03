namespace AiUsage.Core.Diagnostics;

public enum DiagnosticEvent
{
    StartupFailure, ShutdownFailure, DisposalFailure, OperationFailure, UnhandledFailure, TrayFailure, BudgetStoreRecovered,
    SessionStarted, SessionExited, PreviousExitUnknown, OperationStarted, OperationCompleted, OperationCancelled,
    HttpCompleted, CapturePersisted, CaptureLost, LoggerHealth, DispatchCompleted, DispatchRejected, DispatcherStalled, DispatcherRecovered,
    BindingFailure, AnimationFailure, BackgroundFailure, NavigationCompleted, WindowHidden, WindowShown, RecoveryCompleted
}
public enum DiagnosticCategory { Unexpected, InvalidOperation, InvalidData, Io, AccessDenied, Platform }
public enum DiagnosticSeverity { Debug, Information, Warning, Error, Critical }
public enum DiagnosticOperation { Startup, Shutdown, Connect, Resume, Refresh, AutomaticRefresh, Disconnect, History, Recovery, Persistence, Command }
public enum DiagnosticOutcome { Completed, Failed, Cancelled, ExitCancelled, Reauthentication, StaleFallback, RecoveryRequired }

public interface IDiagnosticOperation : IDisposable
{
    void SetOutcome(DiagnosticOutcome outcome);
}

/// <summary>Local best-effort diagnostics. Implementations must not throw. No arbitrary data crosses this port.</summary>
public interface IDiagnosticSink
{
    void Record(DiagnosticEvent eventCode, DiagnosticCategory category);

    // The owning implementation must project exception details before generic logging or persistence.
    void Failure(DiagnosticEvent eventCode, Exception exception) => Record(eventCode, exception switch
    {
        InvalidOperationException => DiagnosticCategory.InvalidOperation,
        System.Text.Json.JsonException or InvalidDataException => DiagnosticCategory.InvalidData,
        UnauthorizedAccessException => DiagnosticCategory.AccessDenied,
        IOException => DiagnosticCategory.Io,
        System.Runtime.InteropServices.ExternalException => DiagnosticCategory.Platform,
        _ => DiagnosticCategory.Unexpected
    });
    IDiagnosticOperation? Begin(DiagnosticOperation operation, Guid? accountReference = null) => null;
    void Signal(DiagnosticEvent eventCode, DiagnosticSeverity severity = DiagnosticSeverity.Information) => Record(eventCode, DiagnosticCategory.Unexpected);
}
