using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexCacheBoundaryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage-CacheTests-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"Id\":\"opaque\",\"Windows\":null}]")]
    [InlineData("[{\"Id\":\"opaque\",\"Windows\":[null]}]")]
    [InlineData("[{\"Id\":\"opaque\",\"Windows\":[{\"Id\":\"window\",\"UsedPercent\":101}]}]")]
    public async Task StructurallyInvalidCacheIsIgnoredWithoutDeletingIt(string groups)
    {
        Directory.CreateDirectory(root);
        var json = "{\"v\":1,\"retrievedAt\":\"2030-01-01T00:00:00Z\",\"quota\":{\"Groups\":" + groups + "}}";
        var path = Path.Combine(root, "codex.quota.json");
        await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);
        Assert.Null(await new CodexQuotaCache(root).ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(json, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnknownFieldsAndOpaqueValuesInCompatibleCacheRemainUsable()
    {
        Directory.CreateDirectory(root);
        const string json = """{"v":1,"future":"opaque","retrievedAt":"2030-01-01T00:00:00Z","quota":{"PlanType":" 測試/Plan ","Groups":[{"Id":" native/id ","Windows":[{"Id":" opaque/window ","UsedPercent":null,"Amount":{"Remaining":-1,"Unit":" Tokens "}}]}]}}""";
        await File.WriteAllTextAsync(Path.Combine(root, "codex.quota.json"), json, TestContext.Current.CancellationToken);
        var cached = Assert.IsType<CachedQuota>(await new CodexQuotaCache(root).ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(" 測試/Plan ", cached.Quota.PlanType);
        var group = Assert.Single(cached.Quota.Groups);
        Assert.Equal(" native/id ", group.Id);
        Assert.Equal(" Tokens ", Assert.Single(group.Windows).Amount!.Unit);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
