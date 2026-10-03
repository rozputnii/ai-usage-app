using System.Text;
using System.Text.Json;
using AiUsage.Core.Usage;
using AiUsage.Core.Budget;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class QuotaV2StorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.V2.Tests", Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task NativeFactsRoundTripWithoutRawPayload()
    {
        var quota = CodexQuotaParser.Parse(Encoding.UTF8.GetBytes("""
            {"plan_type":" Plan / Ω ","credits":{"balance":"-1.25","unlimited":true},
             "spend_control":{"individual_limit":{"used":"001.00","remaining":"?","used_percent":0}},"secret_extra":"must-not-persist"}
            """), Now);
        var cache = new CodexQuotaCache(root);
        await cache.WriteAsync(new(quota, Now), TestContext.Current.CancellationToken);
        var read = Assert.IsType<CachedQuota>(await cache.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(" Plan / Ω ", read.Quota.PlanType);
        var facts = Assert.IsType<LimitSnapshot>(read.Quota.Limits).Limits;
        Assert.Equal(new CountQuantity(-1.25m, "credits"), Assert.Single(facts, f => f.Key.Family == "CX-B").Remaining);
        Assert.Equal(LimitValueState.Unlimited, Assert.Single(facts, f => f.Key.Family == "CX-B").Limit.State);
        Assert.Equal("001.00", Assert.Single(facts, f => f.Key.Family == "CX-I").SecondaryAmounts!.Used);
        var text = await File.ReadAllTextAsync(Path.Combine(root, "codex.quota.json"), TestContext.Current.CancellationToken);
        Assert.DoesNotContain("must-not-persist", text);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal(2, doc.RootElement.GetProperty("v").GetInt32());
    }

    [Fact]
    public async Task CodexV1PreservesReadingAndWritesCheckpoint()
    {
        Directory.CreateDirectory(root);
        const string json = """{"v":1,"retrievedAt":"2026-10-03T12:00:00Z","quota":{"FetchedAt":"2026-10-03T12:00:00Z","PlanType":"legacy","Groups":[{"Id":"codex","Windows":[{"Id":"primary","UsedPercent":20,"RemainingPercent":80,"Duration":"05:00:00","ResetsAt":"2026-10-03T17:00:00Z"}]}]}}""";
        var path = Path.Combine(root, "codex.quota.json");
        await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);
        var read = Assert.IsType<CachedQuota>(await new CodexQuotaCache(root).ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(Now, read.RetrievedAt);
        var fact = Assert.Single(Assert.IsType<LimitSnapshot>(read.Quota.Limits).Limits);
        Assert.Equal(20, fact.UsedPercent);
        Assert.Null(fact.PeriodStart);
        Assert.Equal(ResetPrecision.Unknown, fact.Reset!.Precision);
        Assert.Equal(json, await File.ReadAllTextAsync(path + ".v1.bak", TestContext.Current.CancellationToken));
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(2, doc.RootElement.GetProperty("v").GetInt32());
    }

    [Fact]
    public async Task PersistedHistoryContinuesOnlyTheExplicitCompatibleAlias()
    {
        var quota = CodexQuotaParser.Parse(Encoding.UTF8.GetBytes("""
            {"rate_limit":{"primary_window":{"used_percent":25}},"spend_control":{"individual_limit":{"used_percent":40}}}
            """), Now.AddMinutes(5));
        var native = Assert.Single(quota.Limits!.Limits, f => f.Key.Family == "CX-P");
        var key = new ReadingSeriesKey("account", native.LegacyKey!);
        var unrelated = new ReadingSeriesKey("account", new("codex", "legacy-window-v1", "unrelated"));
        using (var old = new LocalBudgetStore(root))
            await old.AppendAsync([new(key, new CountQuantity(20, "percent"), Now), new(unrelated, new CountQuantity(90, "percent"), Now)], TestContext.Current.CancellationToken);
        using (var live = new LocalBudgetStore(root))
            await new QuotaObservationRecorder(live).RecordAsync("account", "codex", new(ProviderSessionStatus.QuotaAvailable, quota), TestContext.Current.CancellationToken);
        using var reopened = new LocalBudgetStore(root);
        Assert.Equal(key, await CompatibleReadingSeries.ResolveAsync(reopened, "account", native, TestContext.Current.CancellationToken));
        Assert.Equal(new decimal[] { 20, 25 }, (await reopened.ReadAsync(key, TestContext.Current.CancellationToken)).Value.Select(r => ((CountQuantity)r.Value).Value));
        Assert.Single((await reopened.ReadAsync(unrelated, TestContext.Current.CancellationToken)).Value);
        var individual = Assert.Single(quota.Limits.Limits, f => f.Key.Family == "CX-I");
        Assert.Single((await reopened.ReadAsync(new("account", individual.Key), TestContext.Current.CancellationToken)).Value);
        Assert.Empty((await reopened.ReadAsync(new("account", native.Key), TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task CodexMigrationCutoverFailurePreservesCheckpointAndRetries()
    {
        Directory.CreateDirectory(root);
        const string original = """{"v":1,"retrievedAt":"2026-10-03T12:00:00Z","quota":{"Groups":[]}}""";
        var path = Path.Combine(root, "codex.quota.json");
        await File.WriteAllTextAsync(path, original, TestContext.Current.CancellationToken);
        var cache = new CodexQuotaCache(root);
        // Sharing permits reads but prevents the final replace, after checkpoint/staging.
        await using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.Null(await cache.ReadAsync(TestContext.Current.CancellationToken));
            Assert.Equal(original, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.Equal(original, await File.ReadAllTextAsync(path + ".v1.bak", TestContext.Current.CancellationToken));
        }
        await File.WriteAllTextAsync(path + ".new", "torn stage", TestContext.Current.CancellationToken);
        Assert.NotNull(await cache.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(original, await File.ReadAllTextAsync(path + ".v1.bak", TestContext.Current.CancellationToken));
        await cache.DeleteAsync(TestContext.Current.CancellationToken);
        Assert.EndsWith(".lock", Assert.Single(Directory.GetFiles(root)));
    }

    [Fact]
    public async Task CodexFutureVersionIsNotMigratedOrDestroyed()
    {
        Directory.CreateDirectory(root);
        const string future = """{"v":3,"retrievedAt":"2026-10-03T12:00:00Z","quota":{"Groups":[]}}""";
        var path = Path.Combine(root, "codex.quota.json");
        await File.WriteAllTextAsync(path, future, TestContext.Current.CancellationToken);
        Assert.Null(await new CodexQuotaCache(root).ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(future, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(path + ".v1.bak"));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
}
