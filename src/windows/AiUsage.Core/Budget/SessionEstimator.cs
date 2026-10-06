using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public sealed record SessionPair(ReadingSeriesKey FiveHour, ReadingSeriesKey Weekly, string ShortPool, string WeeklyPool,
    TimeSpan ShortDuration, TimeSpan WeeklyDuration);
public enum SessionEstimateLevel { None, Rough, Settled }
/// <summary>Weekly percent per full five-hour window: guaranteed bounds (High null when unbounded),
/// the point estimate √(Low·High) once ready, and the number of pooled five-hour parts.</summary>
public sealed record SessionEstimate(SessionEstimateLevel Level, decimal? Cost, decimal Low, decimal? High, int Windows)
{
    public bool Ready => Level != SessionEstimateLevel.None;
    public static SessionEstimate Empty { get; } = new(SessionEstimateLevel.None, null, 0, null, 0);
}
public sealed record SessionCount(decimal WholeSessions, bool LessThanOne, decimal? UpTo = null);
public sealed record SessionFigures(SessionCount? Weekly, SessionCount? Today);

// AIU-046 design.md: every pair of integer readings inside one part bounds the cost; parts are pooled newest first.
public static class SessionEstimator
{
    public static SessionEstimate Estimate(IEnumerable<ReadingRun> readings, SessionPair pair, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(pair);
        if (pair.ShortDuration != TimeSpan.FromHours(5) || pair.WeeklyDuration != TimeSpan.FromDays(7) ||
            string.IsNullOrEmpty(pair.ShortPool) || pair.ShortPool != pair.WeeklyPool ||
            pair.FiveHour.AccountTarget != pair.Weekly.AccountTarget || pair.FiveHour.Limit.Provider != pair.Weekly.Limit.Provider || pair.FiveHour == pair.Weekly)
            return SessionEstimate.Empty;
        var parts = Parts(readings.ToArray(), pair, now);
        double high = double.PositiveInfinity, low = 0;
        for (int pass = 0; pass < 4 && parts.Count > 0; pass++)
        {
            double pooledLow = 0, pooledHigh = double.PositiveInfinity;
            for (int p = 0; p < parts.Count; p++)
            {
                var (l, h) = Bounds(parts[p], high);
                if (Math.Max(pooledLow, l) > Math.Min(pooledHigh, h))
                {
                    // The newest part conflicts with itself, or an older one with the pool: drop it and everything older.
                    if (p == 0) return SessionEstimate.Empty;
                    parts.RemoveRange(p, parts.Count - p);
                    break;
                }
                pooledLow = Math.Max(pooledLow, l);
                pooledHigh = Math.Min(pooledHigh, h);
            }
            bool stable = pooledHigh == high;
            (low, high) = (pooledLow, pooledHigh);
            if (stable) break;
        }
        var level = low <= 0 || double.IsPositiveInfinity(high) ? SessionEstimateLevel.None
            : high <= 1.25 * low ? SessionEstimateLevel.Settled
            : high <= 2 * low ? SessionEstimateLevel.Rough : SessionEstimateLevel.None;
        return new(level, level == SessionEstimateLevel.None ? null : (decimal)Math.Sqrt(low * high), (decimal)low,
            double.IsPositiveInfinity(high) ? null : (decimal)high, parts.Count);
    }

    public static SessionFigures Figures(SessionEstimate estimate, decimal weeklyUsed, decimal? todayShare)
    {
        ArgumentNullException.ThrowIfNull(estimate);
        if (!estimate.Ready || estimate.Cost is not > 0 || weeklyUsed is < 0 or > 100) return new(null, null);
        static SessionCount Count(decimal amount, decimal cost)
        {
            var sessions = Math.Max(0, amount) / cost;
            return new(decimal.Floor(sessions), sessions is > 0 and < 1);
        }
        var left = 100 - weeklyUsed;
        var weekly = Count(left, estimate.Cost.Value);
        if (estimate is { Level: SessionEstimateLevel.Rough, High: > 0 and var high, Low: > 0 })
        {
            weekly = Count(left, high);
            var upTo = decimal.Floor(left / estimate.Low);
            if (upTo > weekly.WholeSessions) weekly = weekly with { UpTo = upTo };
        }
        return new(weekly, todayShare is { } share ? Count(share, estimate.Cost.Value) : null);
    }

    // Readings at every run endpoint of either series where both are covered, grouped into parts by five-hour
    // instance, weekly instance, source and plan; newest part first.
    private static List<(double S, double W)[]> Parts(ReadingRun[] all, SessionPair pair, DateTimeOffset now)
    {
        var shortRuns = ReadingCalculations.Ordered(all, pair.FiveHour, now);
        var weeklyRuns = ReadingCalculations.Ordered(all, pair.Weekly, now);
        var planCutoff = new[] { LastPlanChange(shortRuns), LastPlanChange(weeklyRuns) }.Max();
        var times = shortRuns.Concat(weeklyRuns).SelectMany(x => new[] { x.FirstSeen, x.LastConfirmed })
            .Where(t => t >= planCutoff).Distinct().Order();
        List<(ReadingRun S, ReadingRun W, DateTimeOffset At)> found = [];
        foreach (var at in times)
        {
            var s = Cover(shortRuns, at);
            var w = Cover(weeklyRuns, at);
            if (s is null || w is null || Percent(s) is not < 100 || Percent(w) is not < 100 ||
                s.Source != w.Source || s.PlanType != w.PlanType) continue;
            found.Add((s, w, at));
        }
        return found.GroupBy(x => (x.S.PeriodInstance, x.W.PeriodInstance, x.S.Source, x.S.PlanType))
            .Where(g => g.Last().At >= now.AddDays(-28))
            .OrderByDescending(g => g.Last().At)
            .Select(g =>
            {
                List<(double S, double W)> values = [];
                foreach (var x in g)
                {
                    (double S, double W) value = ((double)Percent(x.S)!.Value, (double)Percent(x.W)!.Value);
                    // A repeated reading can only loosen bounds; the first of a run is the only possible tick.
                    if (values.Count == 0 || values[^1] != value) values.Add(value);
                }
                return values.ToArray();
            }).ToList();
    }

    // Bounds of one part from all pairs i < k with S >= 2, using tick allowances from the upper bound high.
    private static (double Low, double High) Bounds((double S, double W)[] part, double high)
    {
        var allowance = new double[part.Length];
        for (int j = 0; j < part.Length; j++)
            allowance[j] = j > 0 && part[j].W > part[j - 1].W && !double.IsPositiveInfinity(high)
                ? Math.Min(1, high * (part[j].S - part[j - 1].S + 1) / 100) : 1;
        double low = 0, upper = double.PositiveInfinity;
        for (int i = 0; i < part.Length; i++)
        for (int k = i + 1; k < part.Length; k++)
        {
            double s = part[k].S - part[i].S, w = part[k].W - part[i].W;
            if (s < 2) continue;
            low = Math.Max(low, 100 * Math.Max(0, w - allowance[i]) / (s + 1));
            upper = Math.Min(upper, 100 * (w + allowance[k]) / (s - 1));
        }
        return (low, upper);
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
}
