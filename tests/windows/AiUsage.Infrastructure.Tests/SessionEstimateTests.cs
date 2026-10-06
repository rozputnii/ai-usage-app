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

    // One single-point run per reading and series, five minutes apart.
    internal static ReadingRun[] Part(string shortInstance, DateTimeOffset start, params (decimal S, decimal W)[] readings) =>
        readings.SelectMany((r, i) => new ReadingRun[]
        {
            new(ShortKey, new CountQuantity(r.S, "percent"), start.AddMinutes(5 * i), start.AddMinutes(5 * i), shortInstance, null, SnapshotSource.ProviderApi),
            new(Key, new CountQuantity(r.W, "percent"), start.AddMinutes(5 * i), start.AddMinutes(5 * i), "week", null, SnapshotSource.ProviderApi)
        }).ToArray();

    // Readings of one window with true cost C: the weekly value moves C / 100 per five-hour point.
    // Each true value is reported as a multiple of unit, floored or rounded half away from zero.
    private static (decimal S, decimal W)[] Window(double cost, double weekly, IEnumerable<double> fiveHour, bool round = false, double unit = 1) =>
        fiveHour.Select(s => (Report(s, round, unit), Report(weekly + cost * s / 100, round, unit))).ToArray();

    private static decimal Report(double value, bool round, double unit) =>
        (decimal)((round ? Math.Round(value / unit, MidpointRounding.AwayFromZero) : Math.Floor(value / unit)) * unit);

    private static IEnumerable<double> Steps(double step, double end)
    {
        for (double s = 0; s <= end; s += step) yield return s;
    }

    [Fact]
    public void NoEstimateWithFewerThanTwoWeeklyTicks()
    {
        var readings = Enumerable.Range(0, 18).Select(s => ((decimal)s, s <= 9 ? 2m : 3m)).ToArray();
        var estimate = SessionEstimator.Estimate(Part("s", Now.AddHours(-2), readings), Pair, Now);
        Assert.Equal(SessionEstimateLevel.None, estimate.Level);
        Assert.Equal(0, estimate.Low);
        Assert.False(estimate.Ready);
        Assert.Null(estimate.Cost);
    }

    [Fact]
    public void TwoTickPartMatchesHandComputedBounds()
    {
        // r0..r7 = (10,2) (12,2) (14,3) (16,3) (18,3) (20,3) (22,3) (24,4); ticks at r2 and r7.
        // Pass 1, all allowances u = 1:
        //   L: only W = 2 pairs lift above 0: r1 -> r7, S = 12: 100 * (2 - 1) / 13 = 7.6923 (r0 -> r7 gives 100 / 15).
        //   H: r2 -> r6, W = 0, S = 8: 100 * (0 + 1) / 7 = 14.2857
        //      (r0 -> r7: 300 / 13 = 23.08; r1 -> r6 and r2 -> r7: 200 / 9 = 22.2; r0 -> r1: 100).
        //   H <= 2 L (14.29 <= 15.38) but not <= 1.25 L: rough.
        // Pass 2 with H = 100 / 7: u2 = u7 = 100/7 * (2 + 1) / 100 = 3/7 = 0.42857.
        //   L: r2 -> r7, W = 1: 100 * (1 - 3/7) / 11 = 5.19 < 7.69, unchanged.
        //   H: r2 -> r7: 100 * (1 + 3/7) / 9 = 15.87; r0 -> r7: 100 * (2 + 3/7) / 13 = 18.68;
        //      r0 -> r2: 100 * (1 + 3/7) / 3 = 47.6; all above 14.2857, unchanged, so the passes stop.
        // C = sqrt(100/13 * 100/7) = sqrt(10000 / 91) = 10.483.
        var estimate = SessionEstimator.Estimate(Part("s", Now.AddHours(-1),
            (10, 2), (12, 2), (14, 3), (16, 3), (18, 3), (20, 3), (22, 3), (24, 4)), Pair, Now);
        Assert.Equal(100.0 / 13, (double)estimate.Low, 3);
        Assert.Equal(100.0 / 7, (double)estimate.High!.Value, 3);
        Assert.Equal(SessionEstimateLevel.Rough, estimate.Level);
        Assert.Equal(Math.Sqrt(10000.0 / 91), (double)estimate.Cost!.Value, 3);
        Assert.Equal(1, estimate.Windows);
    }

    [Fact]
    public void BoundsContainTrueCostUnderBothRoundingModes()
    {
        var random = new Random(46);
        for (int run = 0; run < 2000; run++)
        {
            double cost = 1 + 39 * random.NextDouble();
            bool round = random.Next(2) == 1;
            int parts = random.Next(1, 12);
            List<ReadingRun> runs = [];
            for (int p = 0; p < parts; p++)
            {
                double weekly = 50 * random.NextDouble(), mean = .3 + 5.7 * random.NextDouble(), s = 0;
                List<double> polls = [];
                // A dropped poll is a gap: the next kept reading jumps by several steps.
                for (int poll = 0; poll < 60 && s < 100; poll++, s += 2 * mean * random.NextDouble())
                    if (random.NextDouble() >= .2) polls.Add(s);
                runs.AddRange(Part("s" + p, Now.AddHours(-6 * (parts - p)), Window(cost, weekly, polls, round)));
            }
            var estimate = SessionEstimator.Estimate(runs, Pair, Now);
            Assert.True((double)estimate.Low <= cost, $"run {run}: L {estimate.Low} > C {cost}");
            if (estimate.High is { } high) Assert.True(cost <= (double)high, $"run {run}: H {high} < C {cost}");
        }
    }

    [Fact]
    public void SingleWindowReachesRoughByTwentyFiveWhenCostIsTen()
    {
        var estimate = SessionEstimator.Estimate(Part("s", Now.AddHours(-2), Window(10, 2.3, Steps(2, 25))), Pair, Now);
        Assert.NotEqual(SessionEstimateLevel.None, estimate.Level);
        Assert.InRange(10m, estimate.Low, estimate.High!.Value);
    }

    [Fact]
    public void ConsistentPartsNeverWiden()
    {
        var random = new Random(2);
        List<ReadingRun> runs = [];
        SessionEstimate? previous = null;
        for (int p = 0; p < 10; p++)
        {
            // 20-60 points of five-hour use in 20-59 polls, so each window fits its five hours.
            double use = 20 + 40 * random.NextDouble(), step = use / (20 + 39 * random.NextDouble());
            runs.AddRange(Part("s" + p, Now.AddHours(-6 * (10 - p)), Window(10, 50 * random.NextDouble(), Steps(step, use))));
            var estimate = SessionEstimator.Estimate(runs, Pair, Now);
            Assert.InRange(10m, estimate.Low, estimate.High ?? decimal.MaxValue);
            if (previous is not null)
            {
                Assert.True(estimate.Low >= previous.Low, $"part {p}: L {estimate.Low} < {previous.Low}");
                Assert.True((estimate.High ?? decimal.MaxValue) <= (previous.High ?? decimal.MaxValue), $"part {p}: H {estimate.High} > {previous.High}");
            }
            previous = estimate;
        }
        Assert.Equal(SessionEstimateLevel.Settled, previous!.Level);
        Assert.Equal(10, previous.Windows);
    }

    [Fact]
    public void ConflictingOlderPartAndOlderAreExcluded()
    {
        var newest = Part("new", Now.AddHours(-4), Window(10, 20.4, Steps(1.5, 50)));
        var conflicting = Part("old", Now.AddHours(-10), (0, 10), (20, 16));
        var oldest = Part("oldest", Now.AddHours(-16), Window(10, 5.7, Steps(1.5, 50)));
        var alone = SessionEstimator.Estimate(newest, Pair, Now);
        var estimate = SessionEstimator.Estimate([.. oldest, .. conflicting, .. newest], Pair, Now);
        Assert.NotEqual(SessionEstimateLevel.None, estimate.Level);
        Assert.Equal(1, estimate.Windows);
        Assert.Equal(alone, estimate);
        // The newest part's own pairs conflict: no estimate.
        Assert.Equal(SessionEstimate.Empty, SessionEstimator.Estimate([.. newest, .. Part("new", Now.AddHours(-1), (52, 25), (72, 31))], Pair, Now));
    }

    [Fact]
    public void PlanChangeWeeklyInstanceSourceAgeAndExhaustionExclusions()
    {
        var window = Part("s", Now.AddHours(-3), Window(10, 2.3, Steps(2, 30)));
        var estimate = SessionEstimator.Estimate(window, Pair, Now);
        Assert.NotEqual(SessionEstimateLevel.None, estimate.Level);
        Assert.Equal(1, estimate.Windows);

        var earlierPlan = Part("old", Now.AddHours(-9), Window(10, 1.1, Steps(2, 40))).Select(r => r with { PlanType = "Pro" });
        Assert.Equal(estimate, SessionEstimator.Estimate([.. earlierPlan, .. window], Pair, Now));

        var reset = window.Select((r, i) => r.Series == Key && i >= 16 ? r with { PeriodInstance = "next-week" } : r).ToArray();
        Assert.Equal(2, SessionEstimator.Estimate(reset, Pair, Now).Windows);
        var otherSource = window.Select((r, i) => i >= 16 ? r with { Source = SnapshotSource.LocalCli } : r).ToArray();
        Assert.Equal(2, SessionEstimator.Estimate(otherSource, Pair, Now).Windows);
        var unpaired = window.Select(r => r.Series == Key ? r with { Source = SnapshotSource.LocalCli } : r);
        Assert.Equal(SessionEstimate.Empty, SessionEstimator.Estimate(unpaired, Pair, Now));

        var old = SessionEstimator.Estimate(window, Pair, Now.AddDays(29));
        Assert.Equal(SessionEstimateLevel.None, old.Level);
        Assert.Equal(0, old.Windows);

        var full = Part("s", Now.AddHours(-1), (100, 50), (60, 100));
        Assert.Equal(estimate, SessionEstimator.Estimate([.. window, .. full], Pair, Now));
        Assert.Equal(SessionEstimate.Empty, SessionEstimator.Estimate(window, Pair with { WeeklyPool = "scoped" }, Now));
        Assert.Equal(SessionEstimate.Empty, SessionEstimator.Estimate(window, Pair with { ShortDuration = TimeSpan.FromHours(4) }, Now));
    }

    [Fact]
    public void ReadingsComeFromBothSeriesEndpoints()
    {
        // Weekly runs span several five-hour runs and the gap between two instances; five-hour runs span several weekly runs.
        ReadingRun[] points = [.. Part("a", Now.AddHours(-40), Window(10, 2.3, Steps(.25, 40))),
            .. Part("b", Now.AddHours(-20), Window(10, 6.2, Steps(.25, 40)))];
        var expected = SessionEstimator.Estimate(points, Pair, Now);
        Assert.NotEqual(SessionEstimateLevel.None, expected.Level);
        foreach (var merged in new[] { new[] { Key }, [ShortKey], [Key, ShortKey] })
            Assert.Equal(expected, SessionEstimator.Estimate(Merge(points, merged), Pair, Now));
    }

    // Joins consecutive equal values of the given series into one run, as the store does.
    private static ReadingRun[] Merge(ReadingRun[] runs, ReadingSeriesKey[] series)
    {
        List<ReadingRun> merged = [];
        foreach (var run in runs.OrderBy(r => r.FirstSeen))
        {
            int last = merged.FindLastIndex(r => r.Series == run.Series);
            if (series.Contains(run.Series) && last >= 0 && merged[last].Value == run.Value && merged[last].PeriodInstance == run.PeriodInstance)
                merged[last] = merged[last] with { LastConfirmed = run.LastConfirmed };
            else merged.Add(run);
        }
        return merged.ToArray();
    }

    [Fact]
    public void DecimalReadingsKeepBoundsValid()
    {
        var estimate = SessionEstimator.Estimate([
            .. Part("a", Now.AddHours(-16), Window(10, 3.1, Steps(1.3, 45), unit: .25)),
            .. Part("b", Now.AddHours(-10), Window(10, 12.6, Steps(.7, 30), round: true, unit: .25)),
            .. Part("c", Now.AddHours(-4), Window(10, 17.9, Steps(2.1, 60), unit: .25))], Pair, Now);
        Assert.NotEqual(SessionEstimateLevel.None, estimate.Level);
        Assert.InRange(10m, estimate.Low, estimate.High!.Value);
    }

    [Fact]
    public void FiguresUseRangeWhenRoughAndPointWhenSettled()
    {
        var rough = new SessionEstimate(SessionEstimateLevel.Rough, 9.54m, 7, 13, 2);
        Assert.Equal(new SessionCount(3, false, 7), SessionEstimator.Figures(rough, 50, null).Weekly);
        var settled = new SessionEstimate(SessionEstimateLevel.Settled, 12, 11.5m, 12.5m, 6);
        var figures = SessionEstimator.Figures(settled, 47, 6);
        Assert.Equal(new SessionCount(4, false), figures.Weekly);
        Assert.True(figures.Today!.LessThanOne);
        Assert.Null(SessionEstimator.Figures(settled, 47, null).Today);
        Assert.Equal(new SessionCount(0, false), SessionEstimator.Figures(rough, 100, 1).Weekly);
        Assert.Equal(new SessionCount(0, false), SessionEstimator.Figures(settled, 100, 1).Weekly);
        Assert.Equal(new SessionFigures(null, null), SessionEstimator.Figures(SessionEstimate.Empty, 47, 10));
        Assert.Equal(new SessionFigures(null, null), SessionEstimator.Figures(SessionEstimator.Estimate([], Pair, Now), 47, 10));
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
