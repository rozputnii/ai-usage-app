namespace AiUsage.Core.Usage;

public abstract record Quantity;
public sealed record CountQuantity(decimal Value, string Unit) : Quantity;
public sealed record MoneyQuantity(long MinorUnits, int? Exponent, string? Currency) : Quantity;

public enum LimitKind { PercentWindow, CountablePool, MonetaryPool }
public enum LimitValueState { Unknown, ExplicitNull, Unlimited, NotApplicable, Finite }
public enum ValueOrigin { Provider, Derived, Assumed }
public enum ResetMeaning { Unknown, Replenish, Expire }
public enum ResetPrecision { Instant, Date }
public enum ResetZone { Explicit, AssumedUtc }
public enum SnapshotSource { ProviderApi, LocalCli }

public sealed record LimitValue
{
    private LimitValue(LimitValueState state, Quantity? value = null) { State = state; Value = value; }
    public LimitValueState State { get; }
    public Quantity? Value { get; }
    public static LimitValue Unknown { get; } = new(LimitValueState.Unknown);
    public static LimitValue ExplicitNull { get; } = new(LimitValueState.ExplicitNull);
    public static LimitValue Unlimited { get; } = new(LimitValueState.Unlimited);
    public static LimitValue NotApplicable { get; } = new(LimitValueState.NotApplicable);
    public static LimitValue Finite(Quantity value) => new(LimitValueState.Finite, value);
}

public sealed record LimitKey(string Provider, string Family, string NativeDiscriminator);
public sealed record ResetFact(DateTimeOffset At, ValueOrigin Origin, ResetMeaning Meaning,
    ResetPrecision Precision = ResetPrecision.Instant, ResetZone Zone = ResetZone.Explicit);
public sealed record PersonalCap(Quantity Amount, DateTimeOffset SetAt);
public sealed record LimitFacts(LimitKey Key, LimitKind Kind, string Unit, LimitValue Limit)
{
    public Quantity? Used { get; init; }
    public Quantity? Remaining { get; init; }
    public ValueOrigin RemainingOrigin { get; init; } = ValueOrigin.Provider;
    public decimal? UsedPercent { get; init; }
    public decimal? RemainingPercent { get; init; }
    public Quantity? SecondaryAmount { get; init; }
    // An explicit unlimited flag wins; any accompanying entitlement remains a source fact.
    public Quantity? ReportedLimit { get; init; }
    public QuotaSourceDetails? SourceDetails { get; init; }
    public TimeSpan? Duration { get; init; }
    public DateTimeOffset? PeriodStart { get; init; }
    public ValueOrigin? PeriodStartOrigin { get; init; }
    public ResetFact? Reset { get; init; }
    public bool IsMonthly { get; init; }
    public bool AllowsCalendarFallback { get; init; }
    public bool? Allowed { get; init; }
    public bool? LimitReached { get; init; }
    public bool? OveragePermitted { get; init; }
    public bool? HasCredits { get; init; }
    public bool? Enabled { get; init; }
    public string? Provenance { get; init; }
}

public sealed record LimitSnapshot(DateTimeOffset FetchedAt, string? PlanType,
    SnapshotSource Source, string? SourceVersion, IReadOnlyList<LimitFacts> Limits);
