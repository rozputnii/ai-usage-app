using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class PersistedBudgetCalculationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-replay-" + Guid.NewGuid().ToString("N"));
    private static readonly ReadingSeriesKey Key = new("local-target", new("test", "weekly", "shared"));
    private static readonly DateTimeOffset Day = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static ReadingObservation At(decimal value, int minutes) => new(Key, new CountQuantity(value, "percent"), Day.AddMinutes(minutes))
    { ResetAt = Day.AddDays(2).AddHours(15), PeriodStartedAt = Day.AddDays(-5), PlanType = "Pro" };
    private async Task<IReadOnlyList<ReadingRun>> Persist(params ReadingObservation[] observations)
    {
        using (var writer = new LocalBudgetStore(root)) await writer.AppendAsync(observations, Token);
        using var restarted = new LocalBudgetStore(root);
        return (await restarted.ReadAsync(Key, Token)).Value;
    }
    private static DayStartValue Baseline(IReadOnlyList<ReadingRun> runs) =>
        ReadingCalculations.DayStart(runs, Key, runs[^1].PeriodInstance, runs[^1].LastConfirmed, TimeZoneInfo.Utc);

    [Theory]
    [InlineData(1, 40, DayStartOrigin.Exact)]
    [InlineData(2, 40, DayStartOrigin.Exact)]
    [InlineData(3, 43, DayStartOrigin.Since)]
    [InlineData(4, 40, DayStartOrigin.Carried)]
    public async Task P01ToP04MidnightBaselinesSurviveActualStoreRestart(int scenario, decimal expected, DayStartOrigin origin)
    {
        ReadingObservation[] observations = scenario switch
        {
            1 => [At(40, -20), At(40, -10), At(40, 0), At(40, 5)],
            2 => [At(40, -20), At(40, 20)],
            3 => [At(40, -20), At(43, 20)],
            _ => [At(40, -5), At(41, 5)]
        };
        var runs = await Persist(observations);
        var baseline = Baseline(runs);
        Assert.Equal(new CountQuantity(expected, "percent"), baseline.Value);
        Assert.Equal(origin, baseline.Origin);
        if (scenario == 3) Assert.Equal(Day.AddMinutes(20), baseline.Since);
        Assert.Equal(scenario == 1 ? 1 : 2, runs.Count);
    }

    [Fact]
    public async Task P05JitterAndP08MovingBoundKeepTheInstanceAndOriginalStart()
    {
        var runs = await Persist(At(40, -5), At(41, 5) with { ResetAt = At(40, -5).ResetAt!.Value.AddSeconds(1) });
        Assert.Equal(runs[0].PeriodInstance, runs[1].PeriodInstance);
        Assert.Equal(new CountQuantity(40, "percent"), Baseline(runs).Value);
        using var clean = new LocalBudgetStore(root);
        await clean.DeleteAllAsync(Token);
        runs = await Persist(At(0, 600) with { ResetAt = Day.AddMinutes(600).AddDays(7), PeriodStartedAt = Day.AddMinutes(600) },
            At(0, 605) with { ResetAt = Day.AddMinutes(605).AddDays(7), PeriodStartedAt = Day.AddMinutes(605) });
        var run = Assert.Single(runs);
        Assert.Equal(Day.AddMinutes(600), run.PeriodStartedAt);
        Assert.Equal(Day.AddMinutes(605).AddDays(7), run.ResetAt);
    }

    [Theory]
    [InlineData(6, 895, 900, 1080, 4, 0, DayStartOrigin.RestartedToday)]
    [InlineData(7, -65, -60, 5, 1, 1, DayStartOrigin.Since)]
    public async Task P06P07RolloverHasTheObservedStartAndCorrectDayBaseline(int scenario, int before, int reset, int after, int used, int baseline, DayStartOrigin origin)
    {
        _ = scenario;
        var runs = await Persist(At(95, before) with { ResetAt = Day.AddMinutes(reset) },
            At(used, after) with { ResetAt = Day.AddMinutes(reset).AddDays(7) });
        Assert.NotEqual(runs[0].PeriodInstance, runs[1].PeriodInstance);
        Assert.Equal(Day.AddMinutes(reset), runs[1].PeriodStartedAt);
        Assert.Equal(new CountQuantity(baseline, "percent"), Baseline(runs).Value);
        Assert.Equal(origin, Baseline(runs).Origin);
    }

    [Fact]
    public async Task P09EarlyReplenishmentUsesObservedBoundaryWithoutInventingTime()
    {
        var runs = await Persist(At(60, 655), At(0, 660) with { ResetAt = Day.AddDays(5).AddHours(15) });
        Assert.NotEqual(runs[0].PeriodInstance, runs[1].PeriodInstance);
        Assert.Null(runs[1].PeriodStartedAt);
        Assert.Equal(Day.AddMinutes(655), runs[1].RestartAfter);
        Assert.Equal(new CountQuantity(0, "percent"), Baseline(runs).Value);
        Assert.Equal(DayStartOrigin.RestartedToday, Baseline(runs).Origin);
    }

    [Fact]
    public async Task P10CorrectionAndP11PlanChangeKeepTheDayBaseline()
    {
        var runs = await Persist(At(58, -5), At(60, 600), At(55, 660), At(55, 720) with { PlanType = "Max" });
        Assert.Equal(4, runs.Count);
        Assert.Single(runs.Select(r => r.PeriodInstance).Distinct());
        Assert.Equal(new CountQuantity(58, "percent"), Baseline(runs).Value);
        Assert.Equal("Max", runs[^1].PlanType);
    }

    [Fact]
    public async Task PersistedPairsEstimateSessionCostAndPlanChangeInvalidatesSamples()
    {
        var shortKey = Key with { Limit = Key.Limit with { Family = "five-hour" } };
        List<ReadingObservation> observations = [];
        for (int i = 0; i < 3; i++)
        {
            var first = At(10, i * 1440) with { Series = shortKey, ResetAt = Day.AddDays(i).AddHours(5), PeriodStartedAt = Day.AddDays(i) };
            observations.AddRange([first, first with { Value = new CountQuantity(60, "percent"), FetchedAt = first.FetchedAt.AddHours(4) },
                At(20 + 6 * i, i * 1440) with { ResetAt = Day.AddDays(7) },
                At(26 + 6 * i, i * 1440 + 240) with { ResetAt = Day.AddDays(7) }]);
        }
        using (var writer = new LocalBudgetStore(root)) await writer.AppendAsync(observations, Token);
        using var reader = new LocalBudgetStore(root);
        var runs = (await reader.ReadAsync(Key, Token)).Value.Concat((await reader.ReadAsync(shortKey, Token)).Value).ToArray();
        var pair = new SessionPair(shortKey, Key, "shared", "shared", TimeSpan.FromHours(5), TimeSpan.FromDays(7));
        var estimate = SessionEstimator.Estimate(runs, pair, Day.AddDays(2).AddHours(5));
        Assert.True(estimate.Ready);
        Assert.Equal(12, estimate.Cost);
        Assert.Equal(3, estimate.Samples.Count);
        var figures = SessionEstimator.Figures(estimate, 47, 18.3m);
        Assert.Equal(4, figures.Weekly!.WholeSessions);
        Assert.Equal(1, figures.Today!.WholeSessions);
        await reader.AppendAsync([At(38, 2 * 1440 + 300) with { PlanType = "Max" }], Token);
        runs = (await reader.ReadAsync(Key, Token)).Value.Concat((await reader.ReadAsync(shortKey, Token)).Value).ToArray();
        Assert.False(SessionEstimator.Estimate(runs, pair, Day.AddDays(2).AddHours(6)).Ready);
    }

    [Fact]
    public async Task SignedBalanceTrackingSurvivesTopUpsAndRestart()
    {
        ReadingObservation Balance(decimal value, int day) => At(0, day * 1440) with
        { Value = new CountQuantity(value, "credits"), IsBalance = true, ResetAt = null, PeriodStartedAt = null };
        var runs = await Persist(Balance(100, 0), Balance(80, 1), Balance(130, 2), Balance(-5, 3));
        var period = new PeriodBounds(Day, Day.AddMonths(1), ValueOrigin.Assumed, ValueOrigin.Assumed);
        Assert.Equal(new CountQuantity(155, "credits"), ReadingCalculations.Track(runs, Key, period, Day.AddDays(4), true, 1).Used);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
