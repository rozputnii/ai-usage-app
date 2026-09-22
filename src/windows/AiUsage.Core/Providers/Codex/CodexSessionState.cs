using AiUsage.Core.Usage;

namespace AiUsage.Core.Providers.Codex;

public enum CodexSessionStatus
{
    NotConnected,
    Working,
    QuotaAvailable,
    QuotaUnavailable,
    ReauthenticationRequired,
    RecoveryRequired
}

/// <summary>
/// What a consumer may render. `Quota` is present only when a reading exists; `RetrievedAt` says
/// when it was actually taken and `FromCache` marks it as last-known rather than current. No status
/// implies a numeric value, so an unavailable quota can never be displayed as zero.
/// </summary>
public sealed record CodexSessionState(
    CodexSessionStatus Status,
    QuotaSnapshot? Quota = null,
    CodexFailureKind? Failure = null,
    DateTimeOffset? RetrievedAt = null,
    bool FromCache = false)
{
    public static readonly CodexSessionState NotConnected = new(CodexSessionStatus.NotConnected);
    public static readonly CodexSessionState Working = new(CodexSessionStatus.Working);
}
