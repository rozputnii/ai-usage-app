using AiUsage.Core.Budget;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class QuotaObservationRecorderTests
{
    [Fact]
    public async Task NativeFactsRecordNewControlsAndPreserveAnExistingCompatibleSeries()
    {
        var store = new CaptureStore();
        var recorder = new QuotaObservationRecorder(store);
        var at = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        var legacy = new LimitKey("codex", "legacy-window-v1", "[\"codex\",\"primary\"]");
        store.Existing = new("account", legacy);
        var facts = new LimitSnapshot(at, "plan", SnapshotSource.ProviderApi, "quota-v2",
        [new(new("codex", "CX-P", "native"), LimitKind.PercentWindow, "percent", LimitValue.NotApplicable)
            { Used = new CountQuantity(25, "percent"), UsedPercent = 25, LegacyKey = legacy },
         new(new("codex", "CX-I", "individual_limit"), LimitKind.PercentWindow, "percent", LimitValue.NotApplicable)
            { Used = new CountQuantity(30, "percent"), SecondaryAmounts = new("123", "456", "333") }]);
        var quota = new QuotaSnapshot(at, "plan", [], null, null, null, null) { Limits = facts };
        await recorder.RecordAsync("account", "codex", new(ProviderSessionStatus.QuotaAvailable, quota), TestContext.Current.CancellationToken);
        Assert.Equal(2, store.Readings.Count);
        Assert.Contains(store.Readings, r => r.Series.Limit == legacy && r.Value == new CountQuantity(25, "percent"));
        Assert.Contains(store.Readings, r => r.Series.Limit.Family == "CX-I" && r.Value == new CountQuantity(30, "percent"));
    }
    [Fact]
    public async Task ExistingSnapshotsRecordOnlyKnownNormalizedValuesWithoutSecondaryUnknownAmounts()
    {
        var store = new CaptureStore();
        var recorder = new QuotaObservationRecorder(store);
        var at = new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
        QuotaWindow[] windows = [new("five", 23.5, 76.5, TimeSpan.FromHours(5), at.AddHours(1)),
            new("absent", null, null, null, null),
            new("count", 60, 40, null, null) { Amount = new(40, 60, 100, "requests") },
            new("opaque", null, null, null, null) { Amount = new(100, 50, 150, "unknown") }];
        var quota = new QuotaSnapshot(at, "opaque plan", [new("opaque/group", null, null, null, null, null, windows)], new(true, false, -5), null, null, null);
        var state = new ProviderSessionState(ProviderSessionStatus.QuotaAvailable, quota, ExtraUsage:
            new(true, ClaudeExtraUsageSource.Current, new(345, 2, "USD"), null, false));
        await recorder.RecordAsync("account", "claude", state, TestContext.Current.CancellationToken);
        Assert.Equal(3, store.Readings.Count);
        Assert.Contains(store.Readings, r => r.Value == new CountQuantity(23.5m, "percent") && r.PeriodStartedAt == at.AddHours(-4));
        Assert.Contains(store.Readings, r => r.Value == new CountQuantity(60, "requests") && r.UsedPercent == 60);
        Assert.Contains(store.Readings, r => r.Value == new MoneyQuantity(345, 2, "USD"));
        await recorder.RecordAsync("account", "codex", state with { ExtraUsage = null }, TestContext.Current.CancellationToken);
        Assert.Contains(store.Readings, r => r.Value == new CountQuantity(-5, "credits") && r.IsBalance);
        int count = store.Readings.Count;
        await recorder.RecordAsync("account", "codex", state with { FromCache = true }, TestContext.Current.CancellationToken);
        await recorder.RecordAsync("account", "codex", state with { Failure = ProviderFailureKind.NetworkFailure }, TestContext.Current.CancellationToken);
        Assert.Equal(count, store.Readings.Count);
    }
    private sealed class CaptureStore : IReadingSeriesStore
    {
        public List<ReadingObservation> Readings { get; } = [];
        public ReadingSeriesKey? Existing { get; set; }
        public Task<StoreWrite> AppendAsync(IReadOnlyList<ReadingObservation> observations, CancellationToken token) { Readings.AddRange(observations); return Task.FromResult(new StoreWrite()); }
        public Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series, CancellationToken token) =>
            Task.FromResult(new StoreRead<IReadOnlyList<ReadingRun>>(series == Existing ?
                [new(series, new CountQuantity(20, "percent"), DateTimeOffset.MinValue, DateTimeOffset.MinValue, "legacy", null, SnapshotSource.ProviderApi)] : []));
    }
}
