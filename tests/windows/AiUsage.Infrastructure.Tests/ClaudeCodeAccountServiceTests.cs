using AiUsage.Core.Accounts;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Providers.Claude;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static AiUsage.Infrastructure.Tests.ClaudeCodeSessionTests;

namespace AiUsage.Infrastructure.Tests;

/// <summary>Claude Code accounts next to browser Claude accounts in the shared account service.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ClaudeCodeAccountServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.ClaudeCode.Accounts.Tests", Guid.NewGuid().ToString("N"));
    private readonly CodexTestServer server;
    private readonly HttpClient http;
    private readonly ServiceProvider services;
    private readonly FakeClaudeCode claude = new();
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public ClaudeCodeAccountServiceTests()
    {
        server = new((request, _) => Task.FromResult(CodexTestServer.Json(request.Method == HttpMethod.Post
            ? ClaudeAuthClientTests.Tokens("browser") : "{\"seven_day\":{\"utilization\":25,\"resets_at\":\"2030-01-07T04:00:00Z\"}}")));
        http = new(server);
        var collection = new ServiceCollection();
        collection.AddSingleton<TimeProvider>(new CodexTestServer.Clock());
        collection.AddSingleton(new ClaudeAuthClient(http, new CodexTestServer.Clock()));
        collection.AddSingleton(new ClaudeQuotaClient(http, new CodexTestServer.Clock()));
        collection.AddSingleton<IClaudeCode>(claude);
        services = collection.BuildServiceProvider();
    }

    private AccountService Create() => new(new AccountRegistry(root), new ProviderSessionFactory(services, root));

    private Task<AccountResult> ConnectClaudeCode(AccountService service, Guid? target = null, string email = "owner@example.test")
    {
        claude.Enqueue(Answer(Auth(email: email)), Answer(ClaudeCodeReportTests.Stream()));
        return service.ConnectAsync("claude-code", target, Guid.NewGuid(), _ => Assert.Fail("No browser is opened."), Token);
    }

    private static Task<AccountResult> ConnectBrowser(AccountService service)
    {
        var attempt = Guid.NewGuid();
        return service.ConnectAsync("claude", null, attempt, _ => Assert.True(service.TrySubmitCode(attempt, "synthetic-code")), Token);
    }

    [Fact]
    public async Task AClaudeCodeAccountCoexistsWithABrowserAccountAndDisconnectsOnlyItsOwnState()
    {
        Guid claudeCodeId;
        using (var service = Create())
        {
            await service.InitializeAsync(Token);
            var browser = await ConnectBrowser(service);
            var local = await ConnectClaudeCode(service);
            Assert.Equal(AccountOutcome.Done, browser.Outcome);
            Assert.Equal(AccountOutcome.Done, local.Outcome);
            claudeCodeId = local.AccountId!.Value;
            Assert.Equal(new[] { "claude", "claude-code" }, service.Current.Select(a => a.Provider));
            Assert.Equal("Max", service.Current[1].Session.Quota!.PlanType);
            Assert.Equal(AccountOutcome.Duplicate, (await ConnectClaudeCode(service)).Outcome);

            claude.Enqueue(Answer(Auth()), Answer(ClaudeCodeReportTests.Stream()));
            Assert.Equal(AccountOutcome.Done, (await service.RefreshAsync(claudeCodeId, Token)).Outcome);
            await service.StopAsync();
        }
        var records = await new AccountRegistry(root).ReadAsync(Token);
        var storage = records.Accounts.Single(a => a.Provider == "claude-code").StorageId;
        var folder = new ProviderSessionFactory(services, root).DirectoryFor(storage);
        Assert.True(File.Exists(Path.Combine(folder, "claude-code.state")));
        Assert.Empty(records.Pending);

        using var restarted = Create();
        await restarted.InitializeAsync(Token);
        Assert.True(restarted.Current.Single(a => a.AccountId == claudeCodeId).Session.FromCache);
        Assert.Equal(AccountOutcome.Done, (await restarted.DisconnectAsync(claudeCodeId, Token)).Outcome);
        Assert.False(File.Exists(Path.Combine(folder, "claude-code.state")));
        Assert.True(restarted.Current.Single(a => a.Provider == "claude").Connected);
        await restarted.StopAsync();
    }

    [Fact]
    public async Task ReconnectUsesClaudeCodeAgainAndRefusesAnotherSignIn()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var id = (await ConnectClaudeCode(service)).AccountId!.Value;
        Assert.Equal(AccountOutcome.Done, (await service.DisconnectAsync(id, Token)).Outcome);
        var mismatch = await ConnectClaudeCode(service, id, "other@example.test");
        Assert.Equal(ProviderFailureKind.AccountMismatch, mismatch.Failure);
        var reconnected = await ConnectClaudeCode(service, id);
        Assert.Equal(AccountOutcome.Done, reconnected.Outcome);
        Assert.Equal(id, reconnected.AccountId);
        Assert.True(service.Current.Single().Connected);
        await service.StopAsync();
    }

    [Fact]
    public async Task ConnectWithoutClaudeCodeLeavesNoAccountOrCandidate()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        claude.Enqueue(Fail(ProviderFailureKind.ProviderUnavailable));
        var result = await service.ConnectAsync("claude-code", null, Guid.NewGuid(), _ => { }, Token);
        Assert.Equal(AccountOutcome.Failed, result.Outcome);
        Assert.Equal(ProviderFailureKind.ProviderUnavailable, result.Failure);
        Assert.Empty(service.Current);
        Assert.Empty((await new AccountRegistry(root).ReadAsync(Token)).Pending);
        await service.StopAsync();
    }

    public void Dispose()
    {
        services.Dispose();
        http.Dispose();
        server.Dispose();
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
