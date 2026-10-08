using AiUsage.Core.Usage;

namespace AiUsage.Core.Budget;

public sealed record ExtraUsageResult(bool? OnExtraUsage, MoneyQuantity? SpendSinceFull, DateTimeOffset? FullSince);

public static class ExtraUsageEvidence
{
    public static ExtraUsageResult Calculate(IEnumerable<ReadingRun> windowReadings, ReadingSeriesKey window,
        TimeSpan duration, IEnumerable<ReadingRun> spendReadings, ReadingSeriesKey spend, DateTimeOffset now,
        TimeSpan? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(windowReadings);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(spendReadings);
        ArgumentNullException.ThrowIfNull(spend);
        if (window.Limit.Provider != "claude" || spend.Limit.Provider != "claude" || window.AccountTarget != spend.AccountTarget ||
            duration != TimeSpan.FromHours(5) && duration != TimeSpan.FromDays(7)) return new(false, null, null);
        var windows = ReadingCalculations.Ordered(windowReadings, window, now);
        var current = windows.LastOrDefault();
        if (current is null) return new(null, null, null);
        if (SessionEstimator.Percent(current) != 100 || current.ResetAt <= now) return new(false, null, null);
        var maxAge = tolerance ?? ReadingContinuity.Floor;
        if (now - current.LastConfirmed > maxAge) return new(null, null, null);
        var active = windows.Where(x => x.PeriodInstance == current.PeriodInstance).ToArray();
        int afterNotFull = Array.FindLastIndex(active, x => SessionEstimator.Percent(x) != 100) + 1;
        if (afterNotFull == 0 || SessionEstimator.Percent(active[afterNotFull - 1]) is null)
            return new(null, null, null);
        var filled = active[afterNotFull].FirstSeen;
        var spends = ReadingCalculations.Ordered(spendReadings, spend, now);
        var baseline = SessionEstimator.Cover(spends, filled);
        var latest = spends.LastOrDefault();
        if (baseline?.Value is not MoneyQuantity || latest?.Value is not MoneyQuantity || latest.LastConfirmed < filled ||
            now - latest.LastConfirmed > maxAge)
            return new(null, null, filled);
        var since = spends.Where(x => x.LastConfirmed >= filled).ToArray();
        for (int i = 0; i < since.Length; i++)
        {
            if (since[i].PeriodInstance != baseline.PeriodInstance) return new(null, null, filled);
            if (i > 0 && (!QuantityMath.TryAlign(since[i - 1].Value, since[i].Value, out var before, out var after, out _) || after < before))
                return new(null, null, filled);
        }
        if (QuantityMath.Subtract(latest.Value, baseline.Value) is not MoneyQuantity difference || difference.MinorUnits < 0)
            return new(null, null, filled);
        return new(difference.MinorUnits > 0, difference, filled);
    }
}
