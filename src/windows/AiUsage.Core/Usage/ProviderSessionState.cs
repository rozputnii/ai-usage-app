using AiUsage.Core.Providers.Claude;

namespace AiUsage.Core.Usage;

public enum ProviderSessionStatus
{
    NotConnected, Working, QuotaAvailable, QuotaUnavailable, ReauthenticationRequired, RecoveryRequired
}

public enum ProviderFailureKind
{
    AuthenticationRequired, AccessDenied, DeviceLoginUnavailable, DeviceCodeExpired,
    BrowserCallbackUnavailable, LoginAttemptExpired, GrantNotRemoved, RateLimited,
    NetworkFailure, Timeout, InvalidResponse, AccountMismatch, ProviderUnavailable,
    RequestRejected, StorageUnavailable, RecoveryRequired,
    // The account has no provider-side workspace this monitor may read, and provisioning one is a
    // provider write that a quota monitor never performs on the account holder's behalf.
    ProjectUnavailable,
    // No client registration is configured on this device for a provider that requires the operator
    // to supply one. The repository never vendors another application's OAuth registration.
    RegistrationUnavailable
}

public sealed record ProviderSessionState(
    ProviderSessionStatus Status,
    QuotaSnapshot? Quota = null,
    ProviderFailureKind? Failure = null,
    DateTimeOffset? RetrievedAt = null,
    bool FromCache = false,
    ClaudeExtraUsage? ExtraUsage = null)
{
    public static ProviderSessionState NotConnected { get; } = new(ProviderSessionStatus.NotConnected);
    public static ProviderSessionState Working { get; } = new(ProviderSessionStatus.Working);
}
