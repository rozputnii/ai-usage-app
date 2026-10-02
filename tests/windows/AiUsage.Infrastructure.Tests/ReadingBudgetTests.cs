using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using Xunit;
using static AiUsage.Infrastructure.Tests.BudgetEngineTests;

namespace AiUsage.Infrastructure.Tests;

public sealed class ReadingBudgetTests
{
    internal static readonly ReadingSeriesKey Key = new("local-target", new("claude", "shared", "weekly"));
    internal static ReadingRun Run(decimal value, string first, string? last = null, string instance = "p1") =>
        new(Key, new CountQuantity(value, "percent"), Local(first), Local(last ?? first), instance, "Pro", SnapshotSource.ProviderApi);

    [Theory]
    [InlineData("P01", "2026-10-05 23:40", "2026-10-06 00:05", 40, "2026-10-06 00:20", 40, DayStartOrigin.Exact)]
    [InlineData("P02", "2026-10-05 23:40", "2026-10-05 23:40", 40, "2026-10-06 00:20", 40, DayStartOrigin.Exact)]
    [InlineData("P03", "2026-10-05 23:40", "2026-10-05 23:40", 43, "2026-10-06 00:20", 43, DayStartOrigin.Since)]
    [InlineData("P04", "2026-10-05 23:55", "2026-10-05 23:55", 41, "2026-10-06 00:05", 40, DayStartOrigin.Carried)]
    public void DayStartExamplesReplayIdentically(string id, string first, string last, int next, string at, int expected, DayStartOrigin origin)
    {
        _ = id;
        ReadingRun[] runs = [Run(40, first, last), Run(next, at), Run(49, "2026-10-06 12:00")];
        var result = ReadingCalculations.DayStart(runs, Key, "p1", Local("2026-10-06 14:00"), Zone);
        Assert.Equal(new CountQuantity(expected, "percent"), result.Value);
        Assert.Equal(origin, result.Origin);
        Assert.Equal(result, ReadingCalculations.DayStart(runs.Reverse().ToArray(), Key, "p1", Local("2026-10-06 14:00"), Zone));
        if (origin == DayStartOrigin.Since) Assert.Equal(Local(at), result.Since);
    }

    [Fact]
    public void P05P08P10JitterMovingBoundAndCorrectionKeepInstance()
    {
        var old = Run(40, "2026-10-06 10:00") with { ResetAt = Local("2026-10-08 15:00") };
        var jitter = Run(41, "2026-10-06 10:05") with { ResetAt = old.ResetAt!.Value.AddSeconds(1) };
        Assert.Equal(PeriodChangeKind.Continuing, ReadingCalculations.Transition(old, jitter, 1).Kind);
        var moving = jitter with { Value = new CountQuantity(40, "percent"), ResetAt = old.ResetAt.Value.AddMinutes(5) };
        Assert.Equal(PeriodChangeKind.Continuing, ReadingCalculations.Transition(old, moving, 1).Kind);
        var corrected = jitter with { Value = new CountQuantity(35, "percent"), ResetAt = old.ResetAt };
        Assert.Equal(PeriodChangeKind.Correction, ReadingCalculations.Transition(old, corrected, 1).Kind);
        ReadingRun[] runs = [Run(58, "2026-10-05 23:55"), Run(60, "2026-10-06 10:00"), Run(55, "2026-10-06 11:00")];
        Assert.Equal(new CountQuantity(58, "percent"), ReadingCalculations.DayStart(runs, Key, "p1", Local("2026-10-06 12:00"), Zone).Value);
    }

    [Fact]
    public void P06P07P09RolloverAndEarlyReplenishmentRespectLocalMidnight()
    {
        var old = Run(95, "2026-10-08 14:55") with { ResetAt = Local("2026-10-08 15:00") };
        var next = Run(4, "2026-10-08 18:00", instance: "p2") with { ResetAt = Local("2026-10-15 15:00") };
        var transition = ReadingCalculations.Transition(old, next, 1);
        Assert.Equal(PeriodChangeKind.Rollover, transition.Kind);
        Assert.Equal(old.ResetAt, transition.StartedAt);
        next = next with { PeriodStartedAt = transition.StartedAt };
        Assert.Equal(new CountQuantity(0, "percent"), ReadingCalculations.DayStart([old, next], Key, "p2", next.LastConfirmed, Zone).Value);
        var beforeMidnight = Run(1, "2026-10-09 00:05", instance: "p2") with { PeriodStartedAt = Local("2026-10-08 23:00") };
        Assert.Equal(new CountQuantity(1, "percent"), ReadingCalculations.DayStart([beforeMidnight], Key, "p2", beforeMidnight.LastConfirmed, Zone).Value);
        var earlyOld = Run(60, "2026-10-06 10:55") with { ResetAt = Local("2026-10-08 15:00") };
        var early = Run(0, "2026-10-06 11:00", instance: "p2") with { ResetAt = Local("2026-10-11 15:00") };
        var change = ReadingCalculations.Transition(earlyOld, early, 1);
        Assert.Equal(PeriodChangeKind.EarlyReplenishment, change.Kind);
        Assert.Null(change.StartedAt);
        Assert.Equal(new CountQuantity(0, "percent"), ReadingCalculations.DayStart([early with { RestartAfter = change.After }], Key, "p2", early.LastConfirmed, Zone).Value);
    }

    [Fact]
    public void P11PlanChangePreservesDayStartAndOtherSeriesCannotSupplyBaseline()
    {
        var morning = Run(40, "2026-10-06 00:05");
        var noon = Run(40, "2026-10-06 12:00") with { PlanType = "Max" };
        Assert.Equal(new CountQuantity(40, "percent"), ReadingCalculations.DayStart([morning, noon], Key, "p1", noon.LastConfirmed, Zone).Value);
        var other = Run(1, "2026-10-05 23:55") with { Series = Key with { AccountTarget = "different" } };
        Assert.Equal(DayStartOrigin.Since, ReadingCalculations.DayStart([other, morning], Key, "p1", noon.LastConfirmed, Zone).Origin);
        Assert.Null(ReadingCalculations.DayStart([other, morning], Key, "p1", Local("2026-10-06 00:01"), Zone).Value);
    }

    [Fact]
    public void TrackingCountsIncreasesAndBalanceDecreasesWithoutInventingResets()
    {
        var period = Period("2026-10-01", "2026-11-01");
        var runs = new[] { Run(21800, "2026-09-30 23:55"), Run(22100, "2026-10-01 08:00") };
        var tracked = ReadingCalculations.Track(runs, Key, period, Local("2026-10-01 09:00"), false, 1);
        Assert.Equal(new CountQuantity(300, "percent"), tracked.Used);
        Assert.Equal(new CountQuantity(0, "percent"), ReadingCalculations.DayStart(tracked.Runs, Key, tracked.Runs[0].PeriodInstance, Local("2026-10-01 09:00"), Zone).Value);
        var reset = new[] { Run(21900, "2026-09-30 23:55"), Run(28900, "2026-10-15 00:00"), Run(29000, "2026-10-15 10:00"), Run(40, "2026-10-15 10:05"), Run(400, "2026-10-15 17:00") };
        var result = ReadingCalculations.Track(reset, Key, period, Local("2026-10-15 18:00"), false, 1);
        Assert.Equal(new CountQuantity(7460, "percent"), result.Used);
        Assert.False(result.Incomplete);
        Assert.Equal(new CountQuantity(7000, "percent"), ReadingCalculations.DayStart(result.Runs, Key, result.Runs[0].PeriodInstance, Local("2026-10-15 18:00"), Zone).Value);
        var gap = reset.Select(r => r.Value == new CountQuantity(40, "percent") ? r with { FirstSeen = Local("2026-10-15 11:00"), LastConfirmed = Local("2026-10-15 11:00") } : r).ToArray();
        Assert.True(ReadingCalculations.Track(gap, Key, period, Local("2026-10-15 18:00"), false, 1).Incomplete);
        var balance = new[] { Run(100, "2026-10-02"), Run(80, "2026-10-03"), Run(130, "2026-10-04"), Run(-5, "2026-10-05") };
        Assert.Equal(new CountQuantity(155, "percent"), ReadingCalculations.Track(balance, Key, period, Local("2026-10-06"), true, 1).Used);
        Assert.Equal(new CountQuantity(100, "percent"), ReadingCalculations.Track([balance[0]], Key, period, Local("2026-10-06"), false, 1).Used);
    }
}
