using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public sealed record SessionPair(ReadingSeriesKey FiveHour, ReadingSeriesKey Weekly, string ShortPool, string WeeklyPool,
    TimeSpan ShortDuration, TimeSpan WeeklyDuration);
public sealed record SessionSample(string Instance, DateTimeOffset At, decimal Cost, decimal WeeklyMovement);
public sealed record SessionEstimate(bool Ready, decimal? Cost, decimal? MedianAbsoluteDeviation,
    decimal WeeklyMovement, IReadOnlyList<SessionSample> Samples);
public sealed record SessionCount(decimal WholeSessions, bool LessThanOne);
public sealed record SessionFigures(SessionCount? Weekly, SessionCount? Today);

public static class SessionEstimator
{
    public static SessionEstimate Estimate(IEnumerable<ReadingRun> readings, SessionPair pair, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(pair);
        if (pair.ShortDuration != TimeSpan.FromHours(5) || pair.WeeklyDuration != TimeSpan.FromDays(7) ||
            string.IsNullOrEmpty(pair.ShortPool) || pair.ShortPool != pair.WeeklyPool ||
            pair.FiveHour.AccountTarget != pair.Weekly.AccountTarget || pair.FiveHour.Limit.Provider != pair.Weekly.Limit.Provider || pair.FiveHour == pair.Weekly)
            return new(false, null, null, 0, []);
        var all = readings.ToArray();
        var shortRuns = ReadingCalculations.Ordered(all, pair.FiveHour, now);
        var weeklyRuns = ReadingCalculations.Ordered(all, pair.Weekly, now);
        var planCutoff = new[] { LastPlanChange(shortRuns), LastPlanChange(weeklyRuns) }.Max();
        List<SessionSample> samples = [];
        foreach (var instance in shortRuns.GroupBy(x => x.PeriodInstance))
        {
            var first = instance.First();
            var last = instance.LastOrDefault(x => Percent(x) is < 100);
            if (last is null || first.FirstSeen < planCutoff) continue;
            var end = last.LastConfirmed < now ? last.LastConfirmed : now;
            if (end < now.AddDays(-28) || end <= first.FirstSeen) continue;
            var wf = Cover(weeklyRuns, first.FirstSeen);
            var wl = Cover(weeklyRuns, end);
            if (wf is null || wl is null || wf.PeriodInstance != wl.PeriodInstance ||
                first.Source != last.Source || first.Source != wf.Source || first.Source != wl.Source ||
                first.PlanType != last.PlanType || first.PlanType != wf.PlanType || first.PlanType != wl.PlanType) continue;
            var ds = Percent(last) - Percent(first);
            var dw = Percent(wl) - Percent(wf);
            if (ds is not >= 10 || dw is not >= 0) continue;
            samples.Add(new(first.PeriodInstance, end, 100 * dw.Value / ds.Value, dw.Value));
        }
        var accepted = samples.OrderByDescending(x => x.At).ThenBy(x => x.Instance, StringComparer.Ordinal).Take(10).ToArray();
        if (accepted.Length == 0) return new(false, null, null, 0, accepted);
        var median = Median(accepted.Select(x => x.Cost));
        var mad = Median(accepted.Select(x => Math.Abs(x.Cost - median)));
        var movement = accepted.Sum(x => x.WeeklyMovement);
        bool ready = accepted.Length >= 3 && median > 0 && mad <= median * .25m && movement >= 5;
        return new(ready, ready ? median : null, mad, movement, accepted);
    }

    public static SessionFigures Figures(SessionEstimate estimate, decimal weeklyUsed, decimal? todayShare)
    {
        ArgumentNullException.ThrowIfNull(estimate);
        if (!estimate.Ready || estimate.Cost is not > 0 || weeklyUsed is < 0 or > 100) return new(null, null);
        SessionCount Count(decimal amount)
        {
            var sessions = Math.Max(0, amount) / estimate.Cost.Value;
            return new(decimal.Floor(sessions), sessions is > 0 and < 1);
        }
        return new(Count(100 - weeklyUsed), todayShare is { } share ? Count(share) : null);
    }

    internal static decimal? Percent(ReadingRun run) => run.Value is CountQuantity { Unit: "percent", Value: >= 0 and <= 100 } q ? q.Value : null;
    // Ordered supplies chronological, nonoverlapping runs. Locate the last possible
    // start, then check actual coverage: a gap must never become a confirmation.
    internal static ReadingRun? Cover(ReadOnlySpan<ReadingRun> runs, DateTimeOffset at)
    {
        int low = 0, high = runs.Length;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (runs[middle].FirstSeen <= at) low = middle + 1;
            else high = middle;
        }
        return low > 0 && runs[low - 1].LastConfirmed >= at ? runs[low - 1] : null;
    }

    private static DateTimeOffset LastPlanChange(ReadingRun[] runs)
    {
        var cutoff = DateTimeOffset.MinValue;
        for (int i = 1; i < runs.Length; i++)
            if (runs[i].PlanType != runs[i - 1].PlanType) cutoff = runs[i].FirstSeen;
        return cutoff;
    }

    private static decimal Median(IEnumerable<decimal> values)
    {
        var ordered = values.Order().ToArray();
        int middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2 : ordered[middle];
    }
}
