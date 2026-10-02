using AiUsage.Core.Budget;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Persistence;

// Private disk DTOs keep the Core model independent of a serializer and freeze the v1 schema.
internal sealed record StoredQuantity(string Kind, decimal? Count, string? Unit, long? MinorUnits, int? Exponent, string? Currency)
{
    internal static StoredQuantity From(Quantity value) => value switch
    {
        CountQuantity c => new("count", c.Value, c.Unit, null, null, null),
        MoneyQuantity m => new("money", null, null, m.MinorUnits, m.Exponent, m.Currency),
        _ => throw new ArgumentException("Unsupported quantity.", nameof(value))
    };
    internal Quantity ToQuantity() => this switch
    {
        { Kind: "count", Count: { } c, Unit: { Length: > 0 and <= 128 } u, MinorUnits: null, Exponent: null, Currency: null } => new CountQuantity(c, u),
        { Kind: "money", Count: null, Unit: null, MinorUnits: { } m, Exponent: null or >= 0 and <= 28, Currency: null or { Length: <= 128 } } => new MoneyQuantity(m, Exponent, Currency),
        _ => throw new InvalidDataException("Invalid stored quantity.")
    };
}

internal sealed record StoredRun(StoredQuantity Value, DateTimeOffset FirstSeen, DateTimeOffset LastConfirmed,
    string PeriodInstance, string? PlanType, SnapshotSource Source, DateTimeOffset? ResetAt,
    DateTimeOffset? PeriodStartedAt, DateTimeOffset? RestartAfter, ResetPrecision? ResetPrecision,
    decimal? UsedPercent, string? SourceVersion, bool IsBalance)
{
    internal static StoredRun From(ReadingRun r) => new(StoredQuantity.From(r.Value), r.FirstSeen, r.LastConfirmed,
        r.PeriodInstance, r.PlanType, r.Source, r.ResetAt, r.PeriodStartedAt, r.RestartAfter, r.ResetPrecision,
        r.UsedPercent, r.SourceVersion, r.IsBalance);
    internal ReadingRun ToRun(ReadingSeriesKey key) => new(key, Value.ToQuantity(), FirstSeen, LastConfirmed, PeriodInstance, PlanType, Source)
    {
        ResetAt = ResetAt, PeriodStartedAt = PeriodStartedAt, RestartAfter = RestartAfter,
        ResetPrecision = ResetPrecision, UsedPercent = UsedPercent, SourceVersion = SourceVersion, IsBalance = IsBalance
    };
}
internal sealed record SeriesDocument(int Version, ReadingSeriesKey Series, IReadOnlyList<StoredRun> Runs);
internal sealed record CapDocument(ReadingSeriesKey Series, StoredQuantity Amount, DateTimeOffset SetAt);
internal sealed record ConfigurationDocument(int Version, IReadOnlyList<DayOfWeek> WorkDays, IReadOnlyList<CapDocument> Caps);
