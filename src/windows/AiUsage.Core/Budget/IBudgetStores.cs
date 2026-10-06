using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public sealed record ReadingObservation(ReadingSeriesKey Series, Quantity Value, DateTimeOffset FetchedAt, decimal RoundingUnit = 1)
{
    public string? PlanType { get; init; }
    public SnapshotSource Source { get; init; }
    public string? SourceVersion { get; init; }
    public decimal? UsedPercent { get; init; }
    public DateTimeOffset? ResetAt { get; init; }
    public ResetPrecision? ResetPrecision { get; init; }
    public DateTimeOffset? PeriodStartedAt { get; init; }
    public bool IsBalance { get; init; }
}

public sealed record StoreRead<T>(T Value, bool Recovered = false);
public sealed record StoreWrite(bool Recovered = false);
public interface IReadingSeriesStore
{
    Task<StoreWrite> AppendAsync(IReadOnlyList<ReadingObservation> observations, CancellationToken token);
    Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series, CancellationToken token);
}

public sealed record StoredPersonalCap(ReadingSeriesKey Series, PersonalCap Cap);
public sealed record BudgetConfiguration(IReadOnlyList<DayOfWeek> WorkDays, IReadOnlyList<StoredPersonalCap> Caps)
{
    public static BudgetConfiguration Default => new(
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday], []);
}
public interface IBudgetConfigurationStore
{
    Task<StoreRead<BudgetConfiguration>> LoadConfigurationAsync(CancellationToken token);
    Task<StoreWrite> SaveConfigurationAsync(BudgetConfiguration configuration, CancellationToken token);
}

/// <summary>Only these stores, not a claim of complete product factory reset. Call after writers drain.</summary>
public interface IBudgetDataCleanup
{
    Task DeleteAccountAsync(string accountTarget, CancellationToken token);
    Task DeleteAllAsync(CancellationToken token);
}

public interface IQuotaObservationRecorder
{
    Task RecordAsync(string accountTarget, string provider, ProviderSessionState state, CancellationToken token);
}
