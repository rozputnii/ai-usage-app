using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class LocalBudgetStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-budget-" + Guid.NewGuid().ToString("N"));
    private static readonly ReadingSeriesKey Key = new("test-account", new("test", "weekly", "opaque/../key"));
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private readonly List<LocalBudgetStore> stores = [];
    private LocalBudgetStore Store() { var store = new LocalBudgetStore(root); stores.Add(store); return store; }
    private static ReadingObservation At(decimal value, int minutes) => new(Key, new CountQuantity(value, "percent"), Start.AddMinutes(minutes))
    { ResetAt = Start.AddDays(6), PeriodStartedAt = Start.AddDays(-1), ResetPrecision = ResetPrecision.Instant, PlanType = "opaque plan" };

    [Fact]
    public async Task RestartPreservesCoalescedRunsGapsAndResetPrecision()
    {
        await Store().AppendAsync([At(40, -5), At(40, 5), At(41, 10), At(41, 30)], Token);
        var read = await Store().ReadAsync(Key, Token);
        Assert.False(read.Recovered);
        Assert.Equal(3, read.Value.Count);
        Assert.Equal(Start.AddMinutes(5), read.Value[0].LastConfirmed);
        Assert.Equal(Start.AddMinutes(10), read.Value[1].LastConfirmed);
        Assert.Equal(ResetPrecision.Instant, read.Value[2].ResetPrecision);
        var baseline = ReadingCalculations.DayStart(read.Value, Key, read.Value[2].PeriodInstance, Start.AddHours(1), TimeZoneInfo.Utc);
        Assert.Equal(new CountQuantity(40, "percent"), baseline.Value);
        Assert.Equal(DayStartOrigin.Exact, baseline.Origin);
    }

    [Fact]
    public async Task RolloverCorrectionPlanAndSourceChangesSurviveRestart()
    {
        await Store().AppendAsync([At(90, 0) with { ResetAt = Start.AddMinutes(10) },
            At(4, 15), At(2, 20), At(2, 25) with { PlanType = "new plan" },
            At(2, 30) with { PlanType = "new plan", Source = SnapshotSource.LocalCli }], Token);
        var runs = (await Store().ReadAsync(Key, Token)).Value;
        Assert.Equal(5, runs.Count);
        Assert.NotEqual(runs[0].PeriodInstance, runs[1].PeriodInstance);
        Assert.Equal(Start.AddMinutes(10), runs[1].PeriodStartedAt);
        Assert.Equal(runs[1].PeriodInstance, runs[2].PeriodInstance);
        Assert.Equal(runs[2].PeriodInstance, runs[3].PeriodInstance);
        Assert.Equal("new plan", runs[3].PlanType);
        Assert.Equal(SnapshotSource.LocalCli, runs[4].Source);
    }

    [Fact]
    public async Task AnEndedWindowReportedWithoutAResetStartsTheNextPeriod()
    {
        // Claude reports an ended five-hour window as 0 % with no reset until the next window starts on first use.
        var end = Start.AddMinutes(10);
        await Store().AppendAsync([At(87, 0) with { ResetAt = end }, At(0, 15) with { ResetAt = null, ResetPrecision = null },
            At(0, 20) with { ResetAt = Start.AddHours(5) }, At(4, 25) with { ResetAt = Start.AddHours(5) }], Token);
        var runs = (await Store().ReadAsync(Key, Token)).Value;
        Assert.Equal(4, runs.Count);
        Assert.NotEqual(runs[0].PeriodInstance, runs[1].PeriodInstance);
        Assert.Equal(end, runs[1].PeriodStartedAt);
        Assert.Equal(runs[1].PeriodInstance, runs[2].PeriodInstance);
        Assert.Equal(runs[2].PeriodInstance, runs[3].PeriodInstance);

        // Before the old reset, a reading without a reset still belongs to the current period.
        Assert.Equal(PeriodChangeKind.Continuing, ReadingCalculations.Transition(runs[0],
            runs[1] with { FirstSeen = end.AddMinutes(-1), LastConfirmed = end.AddMinutes(-1) }, 1).Kind);
    }

    [Fact]
    public async Task ReplayAndOutOfOrderReadingsCannotRewriteHistory()
    {
        await Store().AppendAsync([At(4, 10), At(5, 20)], Token);
        await Store().AppendAsync([At(99, 15), At(99, 20)], Token);
        Assert.Equal(new decimal[] { 4, 5 }, (await Store().ReadAsync(Key, Token)).Value.Select(r => ((CountQuantity)r.Value).Value));
    }

    [Fact]
    public async Task MoneyAndSignedBalancesRetainOpaqueUnitsWithoutConversion()
    {
        await Store().AppendAsync([At(0, 0) with { Value = new MoneyQuantity(1234, 2, "uSd"), UsedPercent = 2.5m },
            At(0, 5) with { Value = new MoneyQuantity(1234, 2, "EUR") }], Token);
        var runs = (await Store().ReadAsync(Key, Token)).Value;
        Assert.Equal(new MoneyQuantity(1234, 2, "uSd"), runs[0].Value);
        Assert.Equal(2.5m, runs[0].UsedPercent);
        Assert.NotEqual(runs[0].PeriodInstance, runs[1].PeriodInstance);
        var balance = Key with { Limit = Key.Limit with { Family = "balance" } };
        await Store().AppendAsync([new(balance, new CountQuantity(-8.75m, "credits"), Start) { IsBalance = true }], Token);
        Assert.Equal(new CountQuantity(-8.75m, "credits"), Assert.Single((await Store().ReadAsync(balance, Token)).Value).Value);
    }

    [Fact]
    public async Task ConfigurationDefaultsAndUnmatchedCapsSurviveRestart()
    {
        var defaults = (await Store().LoadConfigurationAsync(Token)).Value;
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, defaults.WorkDays);
        BudgetConfiguration configuration = new([DayOfWeek.Sunday], [new(Key, new(new MoneyQuantity(9900, 2, "EUR"), Start))]);
        await Store().SaveConfigurationAsync(configuration, Token);
        var read = (await Store().LoadConfigurationAsync(Token)).Value;
        Assert.Equal(configuration.WorkDays, read.WorkDays);
        Assert.Equal(configuration.Caps, read.Caps);
        Assert.Empty((await Store().ReadAsync(Key, Token)).Value);
    }

    [Theory]
    [InlineData("broken")]
    [InlineData("{\"Version\":99,\"Runs\":[]}")]
    public async Task InvalidSeriesIsPreservedAndNewObservationsStartFresh(string invalid)
    {
        await Store().AppendAsync([At(40, 0)], Token);
        var path = Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "series-*.json"));
        await File.WriteAllTextAsync(path, invalid, Token);
        var recovered = await Store().ReadAsync(Key, Token);
        Assert.True(recovered.Recovered);
        Assert.Empty(recovered.Value);
        Assert.Equal(invalid, await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(Path.Combine(root, "budget"), "*.quarantine-*")), Token));
        await Store().AppendAsync([At(45, 60)], Token);
        var run = Assert.Single((await Store().ReadAsync(Key, Token)).Value);
        Assert.Equal(new CountQuantity(45, "percent"), ReadingCalculations.DayStart([run], Key, run.PeriodInstance, Start.AddHours(2), TimeZoneInfo.Utc).Value);
    }

    [Fact]
    public async Task CancelledWriteLeavesCommittedBytesAndNoStage()
    {
        await Store().AppendAsync([At(40, 0)], Token);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Store().AppendAsync([At(50, 5)], cancelled.Token));
        Assert.Equal(new CountQuantity(40, "percent"), Assert.Single((await Store().ReadAsync(Key, Token)).Value).Value);
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "budget"), "*.stage-*"));
    }

    [Fact]
    public async Task RetentionKeepsThirtyFiveDaysAndOneBoundaryObservation()
    {
        await Store().AppendAsync(Enumerable.Range(0, 40).Select(i => At(i, i * 1440)).ToArray(), Token);
        var runs = (await Store().ReadAsync(Key, Token)).Value;
        Assert.Equal(37, runs.Count);
        Assert.Equal(Start.AddDays(3), runs[0].FirstSeen);
        Assert.Equal(Start.AddDays(39), runs[^1].FirstSeen);
    }

    [Fact]
    public async Task AccountCleanupPreservesOtherTargetsAndWholeCleanupRejectsUnknownFiles()
    {
        var other = Key with { AccountTarget = "other" };
        await Store().AppendAsync([At(40, 0), At(30, 0) with { Series = other }], Token);
        await Store().SaveConfigurationAsync(new([DayOfWeek.Monday], [new(Key, new(new CountQuantity(10, "requests"), Start)), new(other, new(new CountQuantity(20, "requests"), Start))]), Token);
        await Store().DeleteAccountAsync(Key.AccountTarget, Token);
        Assert.Empty((await Store().ReadAsync(Key, Token)).Value);
        Assert.Single((await Store().ReadAsync(other, Token)).Value);
        Assert.Equal(other, Assert.Single((await Store().LoadConfigurationAsync(Token)).Value.Caps).Series);
        var unknown = Path.Combine(root, "budget", "unknown.txt");
        await File.WriteAllTextAsync(unknown, "preserve", Token);
        await Assert.ThrowsAsync<IOException>(() => Store().DeleteAllAsync(Token));
        Assert.Single((await Store().ReadAsync(other, Token)).Value);
        File.Delete(unknown);
        await Store().DeleteAllAsync(Token);
        Assert.Empty((await Store().ReadAsync(other, Token)).Value);
        Assert.Empty((await Store().LoadConfigurationAsync(Token)).Value.Caps);
    }

    [Fact]
    public async Task LaterMovingResetCannotInventAnObservedPeriodStart()
    {
        await Store().AppendAsync([At(40, -5) with { PeriodStartedAt = null },
            At(41, 5) with { PeriodStartedAt = Start }], Token);
        var runs = (await Store().ReadAsync(Key, Token)).Value;
        Assert.Null(runs[^1].PeriodStartedAt);
        Assert.Equal(new CountQuantity(40, "percent"), ReadingCalculations.DayStart(runs, Key, runs[^1].PeriodInstance, Start.AddHours(1), TimeZoneInfo.Utc).Value);
    }

    [Fact]
    public async Task ExcessSeriesCannotGrowTheOwnedStoreIndefinitely()
    {
        var observations = Enumerable.Range(0, 256).Select(i => At(1, 0) with { Series = Key with { Limit = Key.Limit with { NativeDiscriminator = "series-" + i } } }).ToArray();
        await Store().AppendAsync(observations, Token);
        await Assert.ThrowsAsync<IOException>(() => Store().AppendAsync([At(2, 0)], Token));
        Assert.Equal(256, Directory.GetFiles(Path.Combine(root, "budget"), "series-*.json").Length);
    }

    public void Dispose()
    {
        foreach (var store in stores) store.Dispose();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
