namespace AiUsage.Core.Providers.Claude;

public enum ClaudeFailureKind
{
    AuthenticationRequired,
    AccessDenied,
    BrowserCallbackUnavailable,
    LoginAttemptExpired,
    RateLimited,
    NetworkFailure,
    Timeout,
    InvalidResponse,
    AccountMismatch,
    ProviderUnavailable,
    RequestRejected,
    StorageUnavailable,
    RecoveryRequired,
    GrantNotRemoved
}
