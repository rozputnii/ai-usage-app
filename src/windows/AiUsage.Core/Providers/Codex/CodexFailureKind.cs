namespace AiUsage.Core.Providers.Codex;

public enum CodexFailureKind
{
    AuthenticationRequired,
    AccessDenied,
    DeviceLoginUnavailable,
    DeviceCodeExpired,
    BrowserCallbackUnavailable,
    LoginAttemptExpired,
    GrantNotRemoved,
    RateLimited,
    NetworkFailure,
    Timeout,
    InvalidResponse,
    AccountMismatch,
    ProviderUnavailable,
    RequestRejected,
    StorageUnavailable,
    RecoveryRequired
}
