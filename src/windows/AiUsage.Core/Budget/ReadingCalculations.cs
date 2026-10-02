using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public sealed record ReadingSeriesKey(string AccountTarget, LimitKey Limit);
public sealed record ReadingRun(ReadingSeriesKey Series, Quantity Value, DateTimeOffset FirstSeen,
    DateTimeOffset LastConfirmed, string PeriodInstance, string? PlanType, SnapshotSource Source)
{
    public DateTimeOffset? ResetAt { get; init; }
    public DateTimeOffset? PeriodStartedAt { get; init; }
    public DateTimeOffset? RestartAfter { get; init; }
    public ResetPrecision? ResetPrecision { get; init; }
    public decimal? UsedPercent { get; init; }
    public string? SourceVersion { get; init; }
    public bool IsBalance { get; init; }
}
public enum PeriodChangeKind { Continuing, Rollover, EarlyReplenishment, Correction }
public sealed record PeriodChange(PeriodChangeKind Kind, DateTimeOffset? StartedAt = null, DateTimeOffset? After = null);
public enum DayStartOrigin { Unknown, RestartedToday, Exact, Carried, Since }
public sealed record DayStartValue(Quantity? Value, DayStartOrigin Origin, DateTimeOffset? Since = null);
public sealed record TrackedConsumption(Quantity? Used, DateTimeOffset? TrackedSince, bool Incomplete, IReadOnlyList<ReadingRun> Runs);

public static class ReadingCalculations
{
    public static PeriodChange Transition(ReadingRun previous, ReadingRun current, decimal roundingUnit)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roundingUnit);
        if (previous.Series != current.Series || current.FirstSeen <= previous.LastConfirmed)
            return new(PeriodChangeKind.Continuing);
        bool decreased = QuantityMath.TryAlign(previous.Value, current.Value, out var a, out var b, out _) && a - b > roundingUnit;
        if (previous.ResetAt is { } oldReset && current.ResetAt is { } reset)
        {
            if (current.FirstSeen >= oldReset && reset > oldReset.AddSeconds(60))
                return new(PeriodChangeKind.Rollover, oldReset);
            if (decreased && (reset - oldReset).Duration() > TimeSpan.FromSeconds(60))
                return new(PeriodChangeKind.EarlyReplenishment, After: previous.LastConfirmed);
            if (decreased) return new(PeriodChangeKind.Correction);
        }
        return new(PeriodChangeKind.Continuing);
    }

    public static DayStartValue DayStart(IEnumerable<ReadingRun> readings, ReadingSeriesKey key, string instance,
        DateTimeOffset now, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(readings);
        var midnight = WorkCalendar.Midnight(WorkCalendar.Date(now, zone), zone);
        var runs = Ordered(readings, key, now).Where(x => x.PeriodInstance == instance).ToArray();
        var first = runs.FirstOrDefault(x => x.LastConfirmed >= midnight);
        if (first is null) return new(null, DayStartOrigin.Unknown);
        bool restarted = runs.Any(x => x.PeriodStartedAt >= midnight && x.PeriodStartedAt <= now ||
            x.RestartAfter >= midnight && x.RestartAfter <= now && x.RestartAfter < x.FirstSeen && x.FirstSeen <= now);
        if (restarted) return new(WithAmount(first.Value, 0), DayStartOrigin.RestartedToday);
        var covered = runs.FirstOrDefault(x => x.FirstSeen <= midnight && x.LastConfirmed >= midnight);
        if (covered is not null) return new(covered.Value, DayStartOrigin.Exact);
        var previous = runs.LastOrDefault(x => x.LastConfirmed < midnight);
        if (previous is not null && midnight - previous.LastConfirmed <= TimeSpan.FromMinutes(15))
            return new(previous.Value, DayStartOrigin.Carried);
        if (previous is not null && QuantityMath.TryAlign(previous.Value, first.Value, out var p, out var f, out _) && p == f)
            return new(first.Value, DayStartOrigin.Exact);
        return new(first.Value, DayStartOrigin.Since, first.FirstSeen);
    }

    public static TrackedConsumption Track(IEnumerable<ReadingRun> readings, ReadingSeriesKey key, PeriodBounds period,
        DateTimeOffset now, bool balance, decimal roundingUnit)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roundingUnit);
        var all = Ordered(readings, key, now).Where(x => x.FirstSeen < period.End).ToArray();
        var current = all.Where(x => x.LastConfirmed >= period.Start).ToArray();
        if (current.Length == 0) return new(null, null, false, []);
        var previous = all.LastOrDefault(x => x.FirstSeen < period.Start);
        var relevant = previous is null ? current : new[] { previous }.Concat(current).Distinct().ToArray();
        Quantity? scale = relevant[0].Value;
        foreach (var run in relevant)
        {
            if (!QuantityMath.TryAlign(scale, run.Value, out _, out _, out scale)) return new(null, null, true, []);
        }
        try
        {
            decimal used = 0;
            bool incomplete = false;
            List<ReadingRun> totals = [];
            string instance = "tracked:" + period.Start.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ReadingRun Cumulative(ReadingRun run, DateTimeOffset first, DateTimeOffset last) => run with
            {
                Value = WithAmount(scale!, used)!, FirstSeen = first, LastConfirmed = last,
                PeriodInstance = instance, PeriodStartedAt = null, RestartAfter = null, ResetAt = null
            };
            if (previous is not null)
                totals.Add(Cumulative(previous, period.Start, previous.LastConfirmed > period.Start ? previous.LastConfirmed : period.Start));
            foreach (var run in current)
            {
                if (ReferenceEquals(run, previous)) continue;
                QuantityMath.TryAlign(scale, run.Value, out _, out var value, out _);
                if (previous is null) used = balance ? 0 : Math.Max(0, value);
                else
                {
                    QuantityMath.TryAlign(scale, previous.Value, out _, out var before, out _);
                    decimal delta = balance ? before - value : value - before;
                    if (delta > 0) used = checked(used + delta);
                    if (delta < -roundingUnit && run.FirstSeen - previous.LastConfirmed > TimeSpan.FromMinutes(15)) incomplete = true;
                }
                if (WithAmount(scale!, used) is null) return new(null, null, true, []);
                totals.Add(Cumulative(run, run.FirstSeen, run.LastConfirmed < now ? run.LastConfirmed : now));
                previous = run;
            }
            return new(WithAmount(scale!, used), current[0].FirstSeen < period.Start ? period.Start : current[0].FirstSeen, incomplete, totals);
        }
        catch (OverflowException) { return new(null, null, true, []); }
    }

    internal static ReadingRun[] Ordered(IEnumerable<ReadingRun> readings, ReadingSeriesKey key, DateTimeOffset now) =>
        readings.Where(x => x.Series == key && x.FirstSeen <= now && x.LastConfirmed >= x.FirstSeen)
            // A run does not retain intermediate confirmation times. During replay only its
            // first observation is proven when its final confirmation is still in the future.
            .Select(x => x.LastConfirmed > now ? x with { LastConfirmed = x.FirstSeen } : x)
            .OrderBy(x => x.FirstSeen).ToArray();

    internal static Quantity? WithAmount(Quantity scale, decimal value)
    {
        try
        {
            return scale switch
            {
                CountQuantity count => count with { Value = value },
                MoneyQuantity money when decimal.Truncate(value) == value => money with { MinorUnits = checked((long)value) },
                _ => null
            };
        }
        catch (OverflowException) { return null; }
    }
}
