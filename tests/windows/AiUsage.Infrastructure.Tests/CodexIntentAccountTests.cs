using System.Net;
using AiUsage.Core.Accounts;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

/// <summary>A refused Codex renewal seen through the account service, across a relaunch.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexIntentAccountTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.CodexIntentAccount.Tests", Guid.NewGuid().ToString("N"));
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ARefusedRenewalStillAsksForSignInAfterARelaunch()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(
            CodexTestServer.Json("""{"error":"invalid_grant"}""", HttpStatusCode.BadRequest)));
        using var http = new HttpClient(server);
        var collection = new ServiceCollection();
        collection.AddClaudeIntegration(); collection.AddCodexIntegration(); collection.AddCopilotIntegration(); collection.AddAntigravityIntegration();
        // The Codex clients talk only to the synthetic server.
        collection.AddSingleton(new CodexAuthClient(http));
        collection.AddSingleton(new CodexQuotaClient(http));
        using var services = collection.BuildServiceProvider();
        await MultiAccountSessionTests.SeedAsync("codex", Path.Combine(root, "providers"), "synthetic-workspace");
        var registry = new AccountRegistry(root);
        var factory = new ProviderSessionFactory(services, root);
        await new AccountMigration(registry, factory).RunAsync(Token);

        Guid id;
        using (var first = new AccountService(registry, factory))
        {
            await first.InitializeAsync(Token);
            id = Assert.Single(first.Current).AccountId;
            await first.RefreshAsync(id, Token);
            Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, Assert.Single(first.Current).Session.Status);
            await first.StopAsync();
        }

        using var relaunched = new AccountService(new AccountRegistry(root), new ProviderSessionFactory(services, root));
        await relaunched.InitializeAsync(Token);
        var loaded = Assert.Single(relaunched.Current).Session;
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, loaded.Status);
        Assert.Equal(ProviderFailureKind.AuthenticationRequired, loaded.Failure);
        await relaunched.RefreshAsync(id, Token);
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, Assert.Single(relaunched.Current).Session.Status);
        Assert.Equal(1, server.Calls);
        await relaunched.StopAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
