namespace AiUsage.Core.Usage;

public sealed record QuotaSnapshot(
    DateTimeOffset FetchedAt,
    string? PlanType,
    IReadOnlyList<QuotaGroup> Groups,
    CreditBalance? Credits,
    int? AvailableResetCredits,
    bool? SpendControlReached,
    string? LimitReachedType);

public sealed record QuotaGroup(
    string Id,
    string? Name,
    string? MeteredFeature,
    string? NormalModelSlug,
    bool? Allowed,
    bool? LimitReached,
    IReadOnlyList<QuotaWindow> Windows);

public sealed record QuotaWindow(
    string Id,
    double? UsedPercent,
    double? RemainingPercent,
    TimeSpan? Duration,
    DateTimeOffset? ResetsAt);

public sealed record CreditBalance(bool? HasCredits, bool? Unlimited, decimal? Balance);
