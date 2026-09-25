namespace AiUsage.Core.Diagnostics;

public enum DiagnosticEvent { StartupFailure, ShutdownFailure, DisposalFailure, OperationFailure, UnhandledFailure, TrayFailure }
public enum DiagnosticCategory { Unexpected, InvalidOperation, InvalidData, Io, AccessDenied, Platform }

/// <summary>Local best-effort diagnostics. Implementations must not throw. No arbitrary data crosses this port.</summary>
public interface IDiagnosticSink
{
    void Record(DiagnosticEvent eventCode, DiagnosticCategory category);
}
