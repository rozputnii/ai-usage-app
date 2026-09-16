namespace AiUsage.Core.Providers.Copilot;

public enum CopilotFailureKind
{
    AuthenticationRequired,
    AccessDenied,
    LoginDenied,
    LoginAttemptExpired,
    RateLimited,
    NetworkFailure,
    Timeout,
    InvalidResponse,
    AccountMismatch,
    ReportUnavailable,
    ProviderUnavailable,
    RequestRejected,
    StorageUnavailable,
    RecoveryRequired,
    GrantNotRemoved
}
