using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class MultiAccountSessionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.MultiAccount.Tests", Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("claude")]
    [InlineData("codex")]
    [InlineData("copilot")]
    [InlineData("antigravity")]
    public async Task TwoStoredAccountsRestoreAndDisconnectIndependently(string provider)
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        using var services = Services();
        var factory = new ProviderSessionFactory(services, root);
        await SeedAsync(provider, factory.DirectoryFor(firstId), "123");
        await SeedAsync(provider, factory.DirectoryFor(secondId), "456");
        var first = factory.Create(provider, firstId);
        var second = factory.Create(provider, secondId);
        try
        {
            await first.ReadCachedStateAsync(TestContext.Current.CancellationToken);
            await second.ReadCachedStateAsync(TestContext.Current.CancellationToken);
            Assert.True(first.HasStoredGrant);
            Assert.True(second.HasStoredGrant);
            Assert.Equal("123", ProviderSessionFactory.IdentityOf(first)!.Subject);
            Assert.Equal("456", ProviderSessionFactory.IdentityOf(second)!.Subject);
            Assert.NotEqual(ProviderSessionFactory.IdentityOf(first), ProviderSessionFactory.IdentityOf(second));
            Assert.Equal(ProviderSessionStatus.NotConnected,
                (await first.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
            await second.ReadCachedStateAsync(TestContext.Current.CancellationToken);
            Assert.True(second.HasStoredGrant);
            Assert.Equal("456", ProviderSessionFactory.IdentityOf(second)!.Subject);
            Assert.Null(ProviderSessionFactory.IdentityOf(first));
        }
        finally
        {
            ((IDisposable)first).Dispose();
            ((IDisposable)second).Dispose();
        }
        var reopened = factory.Create(provider, secondId);
        try
        {
            await reopened.ReadCachedStateAsync(TestContext.Current.CancellationToken);
            Assert.True(reopened.HasStoredGrant);
            Assert.Equal("456", ProviderSessionFactory.IdentityOf(reopened)!.Subject);
        }
        finally { ((IDisposable)reopened).Dispose(); }
    }

    [Fact]
    public void EmptyStorageReferenceAndUnknownProviderAreRejected()
    {
        using var services = Services();
        var factory = new ProviderSessionFactory(services, root);
        Assert.Throws<ArgumentException>(() => factory.Create("claude", Guid.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create("../claude", Guid.NewGuid()));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void IdentityIncludesContextAndDoesNotPrintPrivateValues()
    {
        var first = new ProviderIdentity("private-subject", "private-context");
        Assert.NotEqual(first, new ProviderIdentity("private-subject", "another-context"));
        Assert.DoesNotContain("private", first.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CodexCacheFromAnotherAccountIsRejectedEvenWithAMatchingGrant()
    {
        using var services = Services();
        var factory = new ProviderSessionFactory(services, root);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var firstDirectory = factory.DirectoryFor(firstId);
        var secondDirectory = factory.DirectoryFor(secondId);
        await SeedAsync("codex", firstDirectory, "first");
        await SeedAsync("codex", secondDirectory, "second");
        var quota = CodexQuotaParser.Parse("{\"plan_type\":\"synthetic-second\"}"u8.ToArray(), DateTimeOffset.UtcNow);
        new CodexQuotaCache(secondDirectory, null, binding: secondId).Write(new(quota, quota.FetchedAt));
        File.Copy(Path.Combine(secondDirectory, "codex.quota.json"), Path.Combine(firstDirectory, "codex.quota.json"));
        var session = factory.Create("codex", firstId);
        try
        {
            var cached = await session.ReadCachedStateAsync(TestContext.Current.CancellationToken);
            Assert.True(session.HasStoredGrant);
            Assert.Equal("first", ProviderSessionFactory.IdentityOf(session)!.Subject);
            Assert.Null(cached.Quota);
        }
        finally { ((IDisposable)session).Dispose(); }
        var matching = factory.Create("codex", secondId);
        try { Assert.Equal("synthetic-second", (await matching.ReadCachedStateAsync(TestContext.Current.CancellationToken)).Quota!.PlanType); }
        finally { ((IDisposable)matching).Dispose(); }
    }

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddCodexIntegration();
        services.AddClaudeIntegration();
        services.AddCopilotIntegration();
        services.AddAntigravityIntegration();
        return services.BuildServiceProvider();
    }

    internal static async Task SeedAsync(string provider, string directory, string identity)
    {
        var token = TestContext.Current.CancellationToken;
        switch (provider)
        {
            case "claude":
                await using (var lease = await new ClaudeStateStore(directory).AcquireAsync(token))
                    await lease.SaveAsync(new() { Identity = new(identity, "synthetic-org"), RefreshToken = "synthetic-refresh" }, null, token);
                break;
            case "codex":
                new CodexGrantStore(directory).Write(new(identity, "synthetic-refresh"));
                break;
            case "copilot":
                await using (var lease = await new CopilotStateStore(directory).AcquireAsync(token))
                    await lease.SaveAsync(new() { AccountId = identity, AccessToken = "synthetic-access" }, null, token);
                break;
            case "antigravity":
                await using (var lease = await new AntigravityStateStore(directory).AcquireAsync(token))
                    await lease.SaveAsync(new() { AccountId = identity, ProjectId = "synthetic-project", RefreshToken = "synthetic-refresh" }, null, token);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(provider));
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
