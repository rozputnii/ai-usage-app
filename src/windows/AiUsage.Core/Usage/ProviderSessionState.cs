using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Providers.Copilot;

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
    RequestRejected, StorageUnavailable, RecoveryRequired, LoginDenied, ReportUnavailable
}

public sealed record ProviderSessionState(
    ProviderSessionStatus Status,
    QuotaSnapshot? Quota = null,
    ProviderFailureKind? Failure = null,
    DateTimeOffset? RetrievedAt = null,
    bool FromCache = false,
    ClaudeExtraUsage? ExtraUsage = null,
    CopilotUsageReading? CopilotUsage = null)
{
    public static ProviderSessionState NotConnected { get; } = new(ProviderSessionStatus.NotConnected);
    public static ProviderSessionState Working { get; } = new(ProviderSessionStatus.Working);
}
