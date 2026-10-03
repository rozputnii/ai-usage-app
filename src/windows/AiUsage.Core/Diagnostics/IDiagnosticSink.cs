namespace AiUsage.Core.Diagnostics;

public enum DiagnosticEvent
{
    StartupFailure, ShutdownFailure, DisposalFailure, OperationFailure, UnhandledFailure, TrayFailure, BudgetStoreRecovered,
    SessionStarted, SessionExited, PreviousExitUnknown, OperationStarted, OperationCompleted, OperationCancelled,
    HttpCompleted, CaptureLost, LoggerHealth, DispatchCompleted, DispatchRejected, DispatcherStalled, DispatcherRecovered,
    BindingFailure, AnimationFailure, BackgroundFailure, NavigationCompleted, WindowHidden, WindowShown, RecoveryCompleted
}
public enum DiagnosticCategory { Unexpected, InvalidOperation, InvalidData, Io, AccessDenied, Platform }
public enum DiagnosticSeverity { Debug, Information, Warning, Error, Critical }

/// <summary>Local best-effort diagnostics. Implementations must not throw. No arbitrary data crosses this port.</summary>
public interface IDiagnosticSink
{
    void Record(DiagnosticEvent eventCode, DiagnosticCategory category);

    // The owning implementation must project exception details before generic logging or persistence.
    void Failure(DiagnosticEvent eventCode, Exception exception) => Record(eventCode, DiagnosticCategory.Unexpected);
}
