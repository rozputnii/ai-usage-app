using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AccountMigrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.AccountMigration.Tests", Guid.NewGuid().ToString("N"));
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddClaudeIntegration(); services.AddCodexIntegration(); services.AddCopilotIntegration(); services.AddAntigravityIntegration();
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("claude", "claude.state")]
    [InlineData("codex", "codex.grant")]
    [InlineData("copilot", "copilot.state")]
    [InlineData("antigravity", "antigravity.state")]
    public async Task LegacyGrantRemainsInPlaceAndGetsOneStableAppReference(string provider, string file)
    {
        using var services = Services();
        var legacy = Path.Combine(root, "providers");
        await MultiAccountSessionTests.SeedAsync(provider, legacy, "123");
        var bytes = await File.ReadAllBytesAsync(Path.Combine(legacy, file), Token);
        var budget = Path.Combine(root, "budget");
        Directory.CreateDirectory(budget);
        await File.WriteAllTextAsync(Path.Combine(budget, "unassigned-history.json"), "synthetic retained bytes", Token);
        var registry = new AccountRegistry(root);
        var factory = new ProviderSessionFactory(services, root);
        var migration = new AccountMigration(registry, factory);
        await migration.RunAsync(Token);
        var first = Assert.Single((await registry.ReadAsync(Token)).Accounts);
        Assert.True(first.LegacyStorage);
        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.Equal(provider, first.Provider);
        Assert.Equal("123", first.Identity.Subject);
        await migration.RunAsync(Token);
        Assert.Equal(first, Assert.Single((await registry.ReadAsync(Token)).Accounts));
        Assert.True((await registry.ReadAsync(Token)).LegacyMigrationComplete);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(legacy, file), Token));
        Assert.Equal("synthetic retained bytes", await File.ReadAllTextAsync(Path.Combine(budget, "unassigned-history.json"), Token));
        using var accounts = new AccountService(registry, factory);
        await accounts.InitializeAsync(Token);
        Assert.True(Assert.Single(accounts.Current).Connected);
        await accounts.DisconnectAsync(first.Id, Token);
        Assert.False(File.Exists(Path.Combine(legacy, file)));
        Assert.True(File.Exists(Path.Combine(budget, "unassigned-history.json")));
        await accounts.StopAsync();
    }

    [Theory]
    [InlineData("account-registered")]
    [InlineData("migration-complete")]
    public async Task InterruptedReferencePublicationResumesWithoutCreatingAnotherAccount(string phase)
    {
        using var services = Services();
        await MultiAccountSessionTests.SeedAsync("claude", Path.Combine(root, "providers"), "123");
        var registry = new AccountRegistry(root);
        var factory = new ProviderSessionFactory(services, root);
        var interrupted = new AccountMigration(registry, factory, p => { if (p == phase) throw new IOException("synthetic interruption"); });
        await Assert.ThrowsAsync<IOException>(() => interrupted.RunAsync(Token));
        var first = Assert.Single((await registry.ReadAsync(Token)).Accounts);
        await new AccountMigration(registry, factory).RunAsync(Token);
        Assert.Equal(first, Assert.Single((await registry.ReadAsync(Token)).Accounts));
    }

    [Fact]
    public async Task MaintenancePublishesForwardOnlyLayoutBeforeAccountMigration()
    {
        using var services = Services();
        var registry = new AccountRegistry(root);
        var factory = new ProviderSessionFactory(services, root);
        var migration = new AccountMigration(registry, factory);
        using (var maintenance = new StateMaintenance(root, accountMigration: async token =>
        {
            Assert.Contains("\"Layout\":2", await File.ReadAllTextAsync(Path.Combine(root, "layout.v1.json"), token), StringComparison.Ordinal);
            await migration.RunAsync(token);
        }))
            Assert.Equal(AiUsage.Core.Persistence.MaintenanceCondition.Ready, (await maintenance.InitializeAsync(Token)).Condition);
        using var oldWriter = new StateMaintenance(root);
        Assert.Equal(AiUsage.Core.Persistence.MaintenanceCondition.NewerSchema, (await oldWriter.InitializeAsync(Token)).Condition);
    }

    [Theory]
    [InlineData("checkpoint")]
    [InlineData("journal")]
    [InlineData("target")]
    [InlineData("commit")]
    [InlineData("cleanup")]
    public async Task InterruptedLegacyMaintenanceUpgradesToLayoutTwoWithTheSameGrant(string phase)
    {
        using var services = Services();
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "appearance.v1.json"), "{\"Version\":1,\"AlwaysOnTop\":true}", Token);
        await MultiAccountSessionTests.SeedAsync("claude", Path.Combine(root, "providers"), "synthetic");
        var grant = Path.Combine(root, "providers", "claude.state");
        var before = await File.ReadAllBytesAsync(grant, Token);
        var registry = new AccountRegistry(root);
        var migration = new AccountMigration(registry, new ProviderSessionFactory(services, root));
        using (var first = new StateMaintenance(root, p => { if (p == phase) throw new IOException(); }))
            Assert.Equal(AiUsage.Core.Persistence.MaintenanceCondition.Interrupted, (await first.InitializeAsync(Token)).Condition);
        using var second = new StateMaintenance(root, accountMigration: migration.RunAsync);
        await second.InitializeAsync(Token);
        Assert.Equal(AiUsage.Core.Persistence.MaintenanceCondition.Ready, (await second.RetryAsync(Token)).Condition);
        Assert.Single((await registry.ReadAsync(Token)).Accounts);
        Assert.Equal(before, await File.ReadAllBytesAsync(grant, Token));
        Assert.Equal(2, second.Current.LayoutVersion);
    }

    [Fact]
    public async Task CorruptLegacyGrantBlocksMigrationWithoutDeletingIt()
    {
        using var services = Services();
        var legacy = Path.Combine(root, "providers");
        Directory.CreateDirectory(legacy);
        var path = Path.Combine(legacy, "claude.state");
        await File.WriteAllBytesAsync(path, [1, 2, 3], Token);
        var registry = new AccountRegistry(root);
        var migration = new AccountMigration(registry, new ProviderSessionFactory(services, root));
        await Assert.ThrowsAsync<ProviderException>(() => migration.RunAsync(Token));
        Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(path, Token));
        Assert.False((await registry.ReadAsync(Token)).LegacyMigrationComplete);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
