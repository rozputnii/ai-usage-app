using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Antigravity;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ProtectedQuotaMigrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.ProtectedV2.Tests", Guid.NewGuid().ToString("N"));
    private static readonly Guid Revision = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid Parent = Guid.Parse("11111111-2222-3333-4444-666666666666");
    private const string Quota = """{"FetchedAt":"2026-10-03T12:00:00Z","PlanType":"Opaque / Ω","Groups":[{"Id":"opaque","Windows":[{"Id":"weekly","UsedPercent":0,"RemainingPercent":100,"Duration":"7.00:00:00","ResetsAt":"2026-10-10T00:00:00Z"}]}]}""";

    [Theory]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Antigravity")]
    public async Task FreshNativeFactsRoundTripInProtectedV2State(string provider)
    {
        var token = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        QuotaSnapshot expected;
        if (provider == "Claude")
        {
            Assert.True(ClaudeQuotaParser.TryParse(Encoding.UTF8.GetBytes("""
                {"spend":{"used":{"amount_minor":7,"exponent":2,"currency":" uSd "},"limit":{"amount_minor":100,"exponent":2,"currency":" uSd "}}}
                """), now, out var reading));
            expected = reading.Quota;
            await using var lease = await new ClaudeStateStore(root).AcquireAsync(token);
            await lease.SaveAsync(new() { Identity = new("account", "organization"), RefreshToken = "synthetic", CachedQuota = reading }, null, token);
        }
        else if (provider == "Copilot")
        {
            expected = CopilotQuotaParser.Parse(Encoding.UTF8.GetBytes("""{"quota_snapshots":{"chat":{"entitlement":null,"remaining":0}}}"""), now);
            await using var lease = await new CopilotStateStore(root).AcquireAsync(token);
            await lease.SaveAsync(new() { AccountId = "123", AccessToken = "synthetic", CachedQuota = expected }, null, token);
        }
        else
        {
            expected = AntigravityQuotaParser.Parse(Encoding.UTF8.GetBytes("""
                {"buckets":[{"bucketId":" Opaque / Ω ","window":"weekly","remainingFraction":1,"remainingAmount":"4.5","resetTime":"2026-10-10T00:00:00"}]}
                """), now);
            await using var lease = await new AntigravityStateStore(root).AcquireAsync(token);
            await lease.SaveAsync(new() { AccountId = "account", RefreshToken = "synthetic", ProjectId = "project", CachedQuota = expected }, null, token);
        }
        var actual = await Load(provider);
        Assert.Equal(2, actual.Version);
        Assert.Equal(Assert.Single(expected.Limits!.Limits), Assert.Single(actual.Quota!.Limits!.Limits));
    }

    [Theory]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Antigravity")]
    public async Task MigrationPreservesCompleteGenerationAndUsableOfflineQuota(string provider)
    {
        var original = await Seed(provider, Quota);
        var result = await Load(provider);
        Assert.Equal(2, result.Version);
        Assert.Equal(Revision, result.Revision);
        Assert.Equal(Parent, result.Parent);
        Assert.Equal("Opaque / Ω", result.Quota!.PlanType);
        Assert.Null(Assert.Single(result.Quota.Limits!.Limits).PeriodStart);
        Assert.Equal(original, await File.ReadAllBytesAsync(PathFor(provider) + ".v1.bak", TestContext.Current.CancellationToken));
        var reopened = await Load(provider);
        Assert.Equal(result.Revision, reopened.Revision);
        Assert.Equal(result.Quota.Limits.Limits.Single().Key, reopened.Quota!.Limits!.Limits.Single().Key);
        using var before = Unprotect(provider, original);
        using var after = Unprotect(provider, await File.ReadAllBytesAsync(PathFor(provider), TestContext.Current.CancellationToken));
        foreach (var property in before.RootElement.EnumerateObject().Where(p => p.Name is not ("Version" or "CachedQuota")))
            Assert.True(JsonElement.DeepEquals(property.Value, after.RootElement.GetProperty(property.Name)), property.Name);
    }

    [Theory]
    [InlineData("Claude", "{\"Groups\":null}")]
    [InlineData("Copilot", "{\"Groups\":[null]}")]
    [InlineData("Antigravity", "{\"unknown_payload\":\"do-not-retain\"}")]
    [InlineData("Claude", "42")]
    [InlineData("Copilot", "[]")]
    [InlineData("Copilot", "{\"Groups\":[{\"Id\":\"chat\",\"Windows\":[{\"Id\":\"monthly\"},{\"Id\":\"monthly\"}]}]}")]
    public async Task InvalidV1CacheDropsAloneAndRetainsGrant(string provider, string quota)
    {
        var original = await Seed(provider, quota);
        var state = await Load(provider);
        Assert.Equal(2, state.Version);
        Assert.Equal(Revision, state.Revision);
        Assert.Null(state.Quota);
        Assert.Equal(original, await File.ReadAllBytesAsync(PathFor(provider) + ".v1.bak", TestContext.Current.CancellationToken));
        using var after = Unprotect(provider, await File.ReadAllBytesAsync(PathFor(provider), TestContext.Current.CancellationToken));
        Assert.Equal("synthetic-token", after.RootElement.GetProperty(provider == "Copilot" ? "AccessToken" : "RefreshToken").GetString());
    }

    [Theory]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Antigravity")]
    public async Task InterruptedMigrationRetriesWithoutChangingLineageAndSignOutRemovesArtifacts(string provider)
    {
        var original = await Seed(provider, Quota);
        var failure = await Assert.ThrowsAsync<ProviderException>(() => Load(provider, () => throw new IOException("synthetic interruption")));
        Assert.Equal(ProviderFailureKind.StorageUnavailable, failure.Kind);
        Assert.Equal(original, await File.ReadAllBytesAsync(PathFor(provider), TestContext.Current.CancellationToken));
        await File.WriteAllTextAsync(PathFor(provider) + ".v2.new", "torn-stage", TestContext.Current.CancellationToken);
        var recovered = await Load(provider);
        Assert.Equal(Revision, recovered.Revision);
        Assert.Equal(2, recovered.Version);
        await Delete(provider);
        Assert.Single(Directory.GetFiles(root)); // only the lease file
        Assert.EndsWith(".lock", Directory.GetFiles(root).Single());
    }

    [Theory]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Antigravity")]
    public async Task UnsupportedVersionAndMismatchedCheckpointFailClosed(string provider)
    {
        var original = await Seed(provider, Quota, version: 3);
        Assert.Equal(ProviderFailureKind.RecoveryRequired, (await Assert.ThrowsAsync<ProviderException>(() => Load(provider))).Kind);
        Assert.Equal(original, await File.ReadAllBytesAsync(PathFor(provider), TestContext.Current.CancellationToken));
        await Seed(provider, Quota);
        await File.WriteAllBytesAsync(PathFor(provider) + ".v1.bak", original, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.RecoveryRequired, (await Assert.ThrowsAsync<ProviderException>(() => Load(provider))).Kind);
        Assert.Equal(original, await File.ReadAllBytesAsync(PathFor(provider) + ".v1.bak", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Antigravity")]
    public async Task PendingGrantSuccessorIsPromotedBeforeCheckpoint(string provider)
    {
        await Seed(provider, Quota);
        var successor = Record(provider, Quota, 1).Replace(Revision.ToString(), "99999999-2222-3333-4444-555555555555", StringComparison.Ordinal)
            .Replace(Parent.ToString(), Revision.ToString(), StringComparison.Ordinal).Replace("synthetic-token", "synthetic-successor", StringComparison.Ordinal);
        var bytes = Protect(provider, successor);
        await File.WriteAllBytesAsync(PathFor(provider) + ".pending", bytes, TestContext.Current.CancellationToken);
        var state = await Load(provider);
        Assert.Equal(Guid.Parse("99999999-2222-3333-4444-555555555555"), state.Revision);
        Assert.Equal(Revision, state.Parent);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(PathFor(provider) + ".v1.bak", TestContext.Current.CancellationToken));
        using var after = Unprotect(provider, await File.ReadAllBytesAsync(PathFor(provider), TestContext.Current.CancellationToken));
        Assert.Equal("synthetic-successor", after.RootElement.GetProperty(provider == "Copilot" ? "AccessToken" : "RefreshToken").GetString());
    }

    [Theory]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Antigravity")]
    public async Task OversizedNormalizedCacheDropsAloneDuringMigration(string provider)
    {
        var opaque = new string('a', 600_000);
        var quota = "{\"Groups\":[{\"Id\":\"" + opaque + "\",\"Name\":\"" + opaque +
            "\",\"Windows\":[{\"Id\":\"monthly\",\"UsedPercent\":25}]}]}";
        var original = await Seed(provider, quota);
        Assert.True(original.Length < 2 * 1024 * 1024);
        var staged = false;
        var interrupted = await Assert.ThrowsAsync<ProviderException>(() => Load(provider, () =>
        {
            staged = true;
            throw new IOException("synthetic interruption after cache-only recovery");
        }));
        Assert.True(staged);
        Assert.Equal(ProviderFailureKind.StorageUnavailable, interrupted.Kind);
        Assert.Equal(original, await File.ReadAllBytesAsync(PathFor(provider), TestContext.Current.CancellationToken));
        var result = await Load(provider);
        Assert.Equal(2, result.Version);
        Assert.Equal(Revision, result.Revision);
        Assert.Equal(Parent, result.Parent);
        Assert.Null(result.Quota);
        Assert.Equal(original, await File.ReadAllBytesAsync(PathFor(provider) + ".v1.bak", TestContext.Current.CancellationToken));
        using var before = Unprotect(provider, original);
        using var after = Unprotect(provider, await File.ReadAllBytesAsync(PathFor(provider), TestContext.Current.CancellationToken));
        foreach (var property in before.RootElement.EnumerateObject().Where(p => p.Name is not ("Version" or "CachedQuota")))
            Assert.True(JsonElement.DeepEquals(property.Value, after.RootElement.GetProperty(property.Name)), property.Name);
        Assert.Equal(Revision, (await Load(provider)).Revision);
    }

    private async Task<byte[]> Seed(string provider, string quota, int version = 1)
    {
        Directory.CreateDirectory(root);
        var bytes = Protect(provider, Record(provider, quota, version));
        await File.WriteAllBytesAsync(PathFor(provider), bytes, TestContext.Current.CancellationToken);
        return bytes;
    }
    private static string Record(string provider, string quota, int version)
    {
        var identity = provider switch
        {
            "Claude" => "\"Identity\":{\"AccountId\":\"account\",\"OrganizationId\":\"organization\"},\"RefreshToken\":\"synthetic-token\"",
            "Copilot" => "\"AccountId\":\"123\",\"AccessToken\":\"synthetic-token\",\"ExpiresAt\":null",
            _ => "\"AccountId\":\"account\",\"RefreshToken\":\"synthetic-token\",\"ProjectId\":\"project\",\"Tier\":\"Opaque-Tier\""
        };
        var cache = provider == "Claude" ? "{\"Quota\":" + quota + ",\"ExtraUsage\":null}" : quota;
        return "{\"Version\":" + version.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"Revision\":\"" + Revision +
            "\",\"ParentRevision\":\"" + Parent + "\",\"NeedsReauthentication\":false," + identity + ",\"CachedQuota\":" + cache + "}";
    }
    private static byte[] Protect(string provider, string text) => ProtectedData.Protect(Encoding.UTF8.GetBytes(text), Encoding.UTF8.GetBytes($"AiUsage.{provider}.State.v1"), DataProtectionScope.CurrentUser);
    private static JsonDocument Unprotect(string provider, byte[] bytes)
    {
        var plain = ProtectedData.Unprotect(bytes, Encoding.UTF8.GetBytes($"AiUsage.{provider}.State.v1"), DataProtectionScope.CurrentUser);
        try { return JsonDocument.Parse(plain.ToArray()); } finally { CryptographicOperations.ZeroMemory(plain); }
    }
    private string PathFor(string provider) => Path.Combine(root, provider.ToLowerInvariant() + ".state");
    private async Task<(int Version, Guid Revision, Guid? Parent, QuotaSnapshot? Quota)> Load(string provider, Action? stage = null)
    {
        var token = TestContext.Current.CancellationToken;
        if (provider == "Claude") { await using var lease = await new ClaudeStateStore(root, stage).AcquireAsync(token); var s = (await lease.LoadAsync(token))!; return (s.Version, s.Revision, s.ParentRevision, s.CachedQuota?.Quota); }
        if (provider == "Copilot") { await using var lease = await new CopilotStateStore(root, stage).AcquireAsync(token); var s = (await lease.LoadAsync(token))!; return (s.Version, s.Revision, s.ParentRevision, s.CachedQuota); }
        await using var ag = await new AntigravityStateStore(root, stage).AcquireAsync(token); var state = (await ag.LoadAsync(token))!; return (state.Version, state.Revision, state.ParentRevision, state.CachedQuota);
    }
    private async Task Delete(string provider)
    {
        var token = TestContext.Current.CancellationToken;
        if (provider == "Claude") { await using var lease = await new ClaudeStateStore(root).AcquireAsync(token); await lease.DeleteAsync(token); }
        else if (provider == "Copilot") { await using var lease = await new CopilotStateStore(root).AcquireAsync(token); await lease.DeleteAsync(token); }
        else { await using var lease = await new AntigravityStateStore(root).AcquireAsync(token); await lease.DeleteAsync(token); }
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
}
