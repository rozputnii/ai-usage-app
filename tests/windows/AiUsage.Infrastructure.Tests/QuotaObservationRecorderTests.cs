using AiUsage.Core.Budget;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class QuotaObservationRecorderTests
{
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
        public Task<StoreWrite> AppendAsync(IReadOnlyList<ReadingObservation> observations, CancellationToken token) { Readings.AddRange(observations); return Task.FromResult(new StoreWrite()); }
        public Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series, CancellationToken token) => throw new NotSupportedException();
    }
}
