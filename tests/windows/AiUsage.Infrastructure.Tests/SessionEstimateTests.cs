using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using Xunit;
using static AiUsage.Infrastructure.Tests.BudgetEngineTests;
using static AiUsage.Infrastructure.Tests.ReadingBudgetTests;

namespace AiUsage.Infrastructure.Tests;

public sealed class SessionEstimateTests
{
    private static readonly ReadingSeriesKey ShortKey = Key with { Limit = Key.Limit with { NativeDiscriminator = "five-hour" } };
    private static readonly SessionPair Pair = new(ShortKey, Key, "shared", "shared", TimeSpan.FromHours(5), TimeSpan.FromDays(7));
    private static readonly DateTimeOffset Now = Local("2026-10-06 14:00");

    internal static ReadingRun[] Samples(params (decimal S, decimal W)[] deltas)
    {
        List<ReadingRun> runs = [];
        for (int i = 0; i < deltas.Length; i++)
        {
            var at = Now.AddDays(-deltas.Length + i);
            runs.Add(Run(10, "2026-10-01") with { Series = ShortKey, FirstSeen = at, LastConfirmed = at, PeriodInstance = "s" + i });
            runs.Add(runs[^1] with { Value = new CountQuantity(10 + deltas[i].S, "percent"), FirstSeen = at.AddHours(4), LastConfirmed = at.AddHours(4) });
            runs.Add(Run(20, "2026-10-01") with { FirstSeen = at, LastConfirmed = at });
            runs.Add(runs[^1] with { Value = new CountQuantity(20 + deltas[i].W, "percent"), FirstSeen = at.AddHours(4), LastConfirmed = at.AddHours(4) });
        }
        return runs.ToArray();
    }

    [Fact]
    public void S01S02S03S04ReadinessUsesMedianDispersionAndMovement()
    {
        Assert.False(SessionEstimator.Estimate(Samples((20, 0), (30, 0), (40, 0)), Pair, Now).Ready);
        var ready = SessionEstimator.Estimate(Samples((50, 6), (40, 4), (70, 9.8m)), Pair, Now);
        Assert.True(ready.Ready);
        Assert.Equal(12, ready.Cost);
        Assert.Equal(2, ready.MedianAbsoluteDeviation);
        Assert.Equal(19.8m, ready.WeeklyMovement);
        Assert.False(SessionEstimator.Estimate(Samples((10, 1), (10, 1), (10, 1)), Pair, Now).Ready);
        Assert.False(SessionEstimator.Estimate(Samples((50, 3), (50, 6), (50, 10)), Pair, Now).Ready);
    }

    [Fact]
    public void S05S06GapIsAcceptedButWeeklyRolloverIsNot()
    {
        var gap = Samples((50, 6));
        Assert.Equal(12, Assert.Single(SessionEstimator.Estimate(gap, Pair, Now).Samples).Cost);
        gap[^1] = gap[^1] with { PeriodInstance = "new-week" };
        Assert.Empty(SessionEstimator.Estimate(gap, Pair, Now).Samples);
    }

    [Fact]
    public void S07PlanChangeDiscardsOldSamplesEvenAfterChangingBack()
    {
        var runs = Samples((50, 6), (40, 4), (70, 9.8m)).ToList();
        runs.Add(runs[0] with { FirstSeen = Now.AddMinutes(-20), LastConfirmed = Now.AddMinutes(-20), PlanType = "Max", PeriodInstance = "s-new" });
        runs.Add(runs[^1] with { FirstSeen = Now.AddMinutes(-10), LastConfirmed = Now.AddMinutes(-10), PlanType = "Pro" });
        Assert.False(SessionEstimator.Estimate(runs, Pair, Now).Ready);
        Assert.Empty(SessionEstimator.Estimate(runs, Pair, Now).Samples);
    }

    [Fact]
    public void S08S09SessionDisplayUsesPartialShareAndHidesUnknown()
    {
        var estimate = SessionEstimator.Estimate(Samples((50, 6), (40, 4), (70, 9.8m)), Pair, Now);
        var full = SessionEstimator.Figures(estimate, 47, 62m / 2.625m);
        Assert.Equal(4, full.Weekly!.WholeSessions);
        Assert.Equal(1, full.Today!.WholeSessions);
        var partial = SessionEstimator.Figures(estimate, 4, 7.5m);
        Assert.Equal(8, partial.Weekly!.WholeSessions);
        Assert.True(partial.Today!.LessThanOne);
        Assert.Null(SessionEstimator.Figures(estimate, 4, null).Today);
        Assert.Equal(0, SessionEstimator.Figures(estimate, 100, 1).Weekly!.WholeSessions);
        Assert.False(SessionEstimator.Figures(estimate, 100, 1).Weekly!.LessThanOne);
        Assert.Null(SessionEstimator.Figures(SessionEstimator.Estimate([], Pair, Now), 47, 10).Weekly);
    }

    [Fact]
    public void SamplesRejectMismatchedPoolsSourcesMissingValuesAndSmallOrNegativeChanges()
    {
        var runs = Samples((50, 6));
        Assert.Empty(SessionEstimator.Estimate(runs, Pair with { WeeklyPool = "scoped" }, Now).Samples);
        Assert.Empty(SessionEstimator.Estimate(runs, Pair with { ShortDuration = TimeSpan.FromHours(4) }, Now).Samples);
        Assert.Empty(SessionEstimator.Estimate(runs.Where(r => r != runs[^1]), Pair, Now).Samples);
        Assert.Empty(SessionEstimator.Estimate(runs.Select(r => r == runs[1] ? r with { Source = SnapshotSource.LocalCli } : r), Pair, Now).Samples);
        Assert.Empty(SessionEstimator.Estimate(Samples((9, 6)), Pair, Now).Samples);
        Assert.Empty(SessionEstimator.Estimate(Samples((50, -1)), Pair, Now).Samples);
        Assert.Empty(SessionEstimator.Estimate(Samples((-1, 6)), Pair, Now).Samples);
    }

    [Fact]
    public void WeeklyCoverageKeepsInclusiveEndpointsAndDoesNotBridgeGaps()
    {
        var runs = Samples((50, 6));
        var at = runs[0].FirstSeen;
        var end = runs[1].LastConfirmed;
        runs[2] = runs[2] with { FirstSeen = at.AddMinutes(-5), LastConfirmed = at };
        runs[3] = runs[3] with { FirstSeen = end, LastConfirmed = end.AddMinutes(5) };
        Assert.Equal(12, Assert.Single(SessionEstimator.Estimate(runs.Reverse(), Pair, Now).Samples).Cost);
        runs[2] = runs[2] with { LastConfirmed = at.AddTicks(-1) };
        Assert.Empty(SessionEstimator.Estimate(runs, Pair, Now).Samples);
        runs[2] = runs[2] with { LastConfirmed = at };
        runs[3] = runs[3] with { FirstSeen = end.AddTicks(1) };
        Assert.Empty(SessionEstimator.Estimate(runs, Pair, Now).Samples);
    }

    [Fact]
    public void UsesLastBelowExhaustionAndLatestTenWithin28Days()
    {
        var runs = Samples((50, 6)).ToList();
        runs.Add(runs[1] with { Value = new CountQuantity(100, "percent"), FirstSeen = runs[1].FirstSeen.AddMinutes(30), LastConfirmed = runs[1].LastConfirmed.AddMinutes(30) });
        runs.Add(runs[3] with { Value = new CountQuantity(90, "percent"), FirstSeen = runs[3].FirstSeen.AddMinutes(30), LastConfirmed = runs[3].LastConfirmed.AddMinutes(30) });
        Assert.Equal(12, Assert.Single(SessionEstimator.Estimate(runs, Pair, Now).Samples).Cost);
        var many = Samples(Enumerable.Repeat((50m, 6m), 12).ToArray());
        Assert.Equal(10, SessionEstimator.Estimate(many, Pair, Now).Samples.Count);
        Assert.Empty(SessionEstimator.Estimate(many, Pair, Now.AddDays(29)).Samples);
        Assert.False(SessionEstimator.Estimate(many.Take(8), Pair, Now).Ready);
    }

    [Fact]
    public void ClaudeExtraUsageRequiresCurrentFullWindowAndComparableSpendAtFill()
    {
        var spendKey = Key with { Limit = new("claude", "extra", "extra") };
        var below = Run(90, "2026-10-06 09:00");
        var full = Run(100, "2026-10-06 10:00", "2026-10-06 14:00");
        var start = Run(0, "2026-10-06 10:00") with { Series = spendKey, Value = new MoneyQuantity(1000, 2, "USD") };
        var end = start with { FirstSeen = Now, LastConfirmed = Now, Value = new MoneyQuantity(1275, 2, "USD") };
        var result = ExtraUsageEvidence.Calculate([below, full], Key, TimeSpan.FromDays(7), [start, end], spendKey, Now);
        Assert.Null(ExtraUsageEvidence.Calculate([below, full with { LastConfirmed = Now.AddMinutes(-16) }], Key,
            TimeSpan.FromDays(7), [start, end], spendKey, Now).OnExtraUsage);
        Assert.Null(ExtraUsageEvidence.Calculate([below, full], Key, TimeSpan.FromDays(7),
            [start, end with { FirstSeen = Now.AddMinutes(-16), LastConfirmed = Now.AddMinutes(-16) }], spendKey, Now).OnExtraUsage);
        Assert.True(result.OnExtraUsage);
        Assert.Equal(new MoneyQuantity(275, 2, "USD"), result.SpendSinceFull);
        Assert.Null(ExtraUsageEvidence.Calculate([full], Key, TimeSpan.FromDays(7), [start, end], spendKey, Now).OnExtraUsage);
        Assert.Null(ExtraUsageEvidence.Calculate([below, full], Key, TimeSpan.FromDays(7), [start, end with { PeriodInstance = "new-spend" }], spendKey, Now).OnExtraUsage);
        var reset = start with { Value = new MoneyQuantity(0, 2, "USD"), FirstSeen = Local("2026-10-06 11:00"), LastConfirmed = Local("2026-10-06 11:00") };
        Assert.Null(ExtraUsageEvidence.Calculate([below, full], Key, TimeSpan.FromDays(7), [start, reset, end], spendKey, Now).OnExtraUsage);
        Assert.Null(ExtraUsageEvidence.Calculate([full], Key, TimeSpan.FromHours(5), [end], spendKey, Now).OnExtraUsage);
        Assert.Null(ExtraUsageEvidence.Calculate([full], Key, TimeSpan.FromDays(7), [start, end with { Value = new MoneyQuantity(1275, 2, "EUR") }], spendKey, Now).OnExtraUsage);
        Assert.False(ExtraUsageEvidence.Calculate([below], Key, TimeSpan.FromDays(7), [start, end], spendKey, Now).OnExtraUsage);
        Assert.False(ExtraUsageEvidence.Calculate([full, full with { Value = new CountQuantity(3, "percent"), FirstSeen = Now, PeriodInstance = "new" }], Key, TimeSpan.FromDays(7), [start, end], spendKey, Now).OnExtraUsage);
    }
}
