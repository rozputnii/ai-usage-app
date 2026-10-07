using System.Net;
using AiUsage.Core.Accounts;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Providers.Claude;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AccountServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "AiUsage.AccountService.Tests", Guid.NewGuid().ToString("N"));
    private readonly CodexTestServer server;
    private readonly HttpClient http;
    private readonly ServiceProvider services;
    private string nextIdentity = "first";
    private bool failQuota;
    private TaskCompletionSource? quotaStarted;
    private const string Usage = "{\"seven_day\":{\"utilization\":25,\"resets_at\":\"2030-01-07T04:00:00Z\"}}";
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public AccountServiceTests()
    {
        server = new(async (request, token) =>
        {
            if (request.Method == HttpMethod.Post) return CodexTestServer.Json(ClaudeAuthClientTests.Tokens(nextIdentity));
            if (quotaStarted is { } started)
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            return failQuota ? CodexTestServer.Json("{}", HttpStatusCode.ServiceUnavailable) : CodexTestServer.Json(Usage);
        });
        http = new(server);
        var collection = new ServiceCollection();
        collection.AddSingleton<TimeProvider>(new CodexTestServer.Clock());
        collection.AddSingleton(new ClaudeAuthClient(http, new CodexTestServer.Clock()));
        collection.AddSingleton(new ClaudeQuotaClient(http, new CodexTestServer.Clock()));
        services = collection.BuildServiceProvider();
    }

    private AccountService Create() => new(new AccountRegistry(root), new ProviderSessionFactory(services, root));

    private Task<AccountResult> Connect(AccountService service, string identity, Guid? target = null)
    {
        nextIdentity = identity;
        var attempt = Guid.NewGuid();
        return service.ConnectAsync("claude", target, attempt,
            _ => Assert.True(service.TrySubmitCode(attempt, "synthetic-code")), Token);
    }

    [Fact]
    public async Task BrowserLaunchFailureCleansCandidateAndAllowsAnotherAttempt()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var failed = await service.ConnectAsync("claude", null, Guid.NewGuid(), _ => throw new InvalidOperationException("Synthetic browser unavailable"), Token);
        Assert.Equal(AccountOutcome.Failed, failed.Outcome);
        Assert.Empty((await new AccountRegistry(root).ReadAsync(Token)).Pending);
        Assert.Equal(AccountOutcome.Done, (await Connect(service, "first")).Outcome);
        await service.StopAsync();
    }

    [Fact]
    public async Task TwoAccountsRejectDuplicateAndWrongReconnectWithoutChangingEitherReference()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var first = await Connect(service, "first");
        var second = await Connect(service, "second");
        Assert.Equal(AccountOutcome.Done, first.Outcome);
        Assert.Equal(AccountOutcome.Done, second.Outcome);
        Assert.NotEqual(first.AccountId, second.AccountId);
        Assert.Equal(2, service.Current.Count);
        var duplicate = await Connect(service, "first");
        Assert.Equal(AccountOutcome.Duplicate, duplicate.Outcome);
        Assert.Equal(first.AccountId, duplicate.AccountId);
        var mismatch = await Connect(service, "third", first.AccountId);
        Assert.Equal(ProviderFailureKind.AccountMismatch, mismatch.Failure);
        Assert.Equal(AccountOutcome.Failed, mismatch.Outcome);
        Assert.Equal(new[] { first.AccountId, second.AccountId }, service.Current.Select(a => (Guid?)a.AccountId));
        var records = await new AccountRegistry(root).ReadAsync(Token);
        Assert.Equal(new[] { "first", "second" }, records.Accounts.Select(a => a.Identity.Subject));
        Assert.Empty(records.Pending);
        await service.StopAsync();
    }

    [Fact]
    public async Task SelectedSignOutAndReconnectPreserveReferenceAcrossRestart()
    {
        Guid firstId;
        Guid secondId;
        using (var initial = Create())
        {
            await initial.InitializeAsync(Token);
            firstId = (await Connect(initial, "first")).AccountId!.Value;
            secondId = (await Connect(initial, "second")).AccountId!.Value;
            Assert.Equal(AccountOutcome.Done, (await initial.DisconnectAsync(firstId, Token)).Outcome);
            Assert.False(initial.Current.Single(a => a.AccountId == firstId).Connected);
            Assert.True(initial.Current.Single(a => a.AccountId == secondId).Connected);
            await initial.StopAsync();
        }
        var calls = server.Calls;
        using var restarted = Create();
        await restarted.InitializeAsync(Token);
        Assert.Equal(calls, server.Calls);
        Assert.Equal(new[] { firstId, secondId }, restarted.Current.Select(a => a.AccountId));
        Assert.False(restarted.Current[0].Connected);
        Assert.True(restarted.Current[1].Session.FromCache);
        Assert.Equal(firstId, (await Connect(restarted, "first")).AccountId);
        Assert.Equal(2, restarted.Current.Count);
        await restarted.StopAsync();
    }

    [Fact]
    public async Task RefreshFailureDoesNotChangeAnotherAccount()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var first = (await Connect(service, "first")).AccountId!.Value;
        var second = (await Connect(service, "second")).AccountId!.Value;
        var before = service.Current.Single(a => a.AccountId == second);
        failQuota = true;
        var result = await service.RefreshAsync(first, Token);
        Assert.Equal(AccountOutcome.Failed, result.Outcome);
        Assert.True(service.Current.Single(a => a.AccountId == first).Session.FromCache);
        Assert.Equal(before, service.Current.Single(a => a.AccountId == second));
        await service.StopAsync();
    }

    [Fact]
    public async Task CancellingOnlyTheActiveAttemptPreventsLateAccountAdmission()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempt = Guid.NewGuid();
        var pending = service.ConnectAsync("claude", null, attempt, _ => started.SetResult(), Token);
        await started.Task.WaitAsync(Token);
        Assert.False(service.TrySubmitCode(Guid.NewGuid(), "synthetic-code"));
        await service.CancelConnectAsync(attempt);
        Assert.Equal(AccountOutcome.Cancelled, (await pending).Outcome);
        Assert.False(service.TrySubmitCode(attempt, "synthetic-code"));
        Assert.Empty(service.Current);
        Assert.Empty((await new AccountRegistry(root).ReadAsync(Token)).Pending);
        await service.StopAsync();
    }

    [Fact]
    public async Task StartupRemovesUnadmittedCandidateWithoutPublishingIt()
    {
        var storage = Guid.NewGuid();
        var factory = new ProviderSessionFactory(services, root);
        await MultiAccountSessionTests.SeedAsync("claude", factory.DirectoryFor(storage), "orphan");
        await new AccountRegistry(root).UpdateAsync(s => s with { Pending = [new("claude", storage)] }, Token);
        using var service = Create();
        await service.InitializeAsync(Token);
        Assert.Empty(service.Current);
        Assert.False(File.Exists(Path.Combine(factory.DirectoryFor(storage), "claude.state")));
        Assert.Empty((await new AccountRegistry(root).ReadAsync(Token)).Pending);
        Assert.Equal(0, server.Calls);
        await service.StopAsync();
    }

    [Fact]
    public async Task FailedInitializationStillDrainsAndAllowsConfirmedReset()
    {
        var storage = Guid.NewGuid();
        var folder = new ProviderSessionFactory(services, root).DirectoryFor(storage);
        await MultiAccountSessionTests.SeedAsync("claude", folder, "orphan");
        await new AccountRegistry(root).UpdateAsync(s => s with { LegacyMigrationComplete = true, Pending = [new("claude", storage)] }, Token);
        using var service = Create();
        using (var obstruction = File.Open(Path.Combine(folder, "claude.state"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            await Assert.ThrowsAnyAsync<Exception>(() => service.InitializeAsync(Token));
        await service.StopAsync();
        await service.StopAsync();
        await new AiUsage.Infrastructure.Persistence.OwnedDataDeletion(root).RunAsync(true, Token);
        Assert.False(File.Exists(Path.Combine(folder, "claude.state")));
    }

    [Fact]
    public async Task ReinstalledStateReattachesTheSameIdentityToItsPreviousAccountId()
    {
        var history = Path.Combine(root, "history");
        var sink = new HistoryPersistenceTests.RecordingSink();
        AccountService Installed(string state) => new(new AccountRegistry(state), new ProviderSessionFactory(services, state), sink,
            new AccountIdentityMap(history));
        Guid original;
        using (var first = Installed(Path.Combine(root, "install-1")))
        {
            await first.InitializeAsync(Token);
            original = (await Connect(first, "first")).AccountId!.Value;
            Assert.DoesNotContain(AiUsage.Core.Diagnostics.DiagnosticEvent.HistoryReattached, sink.Events);
            await first.StopAsync();
        }
        using var reinstalled = Installed(Path.Combine(root, "install-2"));
        await reinstalled.InitializeAsync(Token);
        Assert.Empty(reinstalled.Current);
        Assert.Equal(original, (await Connect(reinstalled, "first")).AccountId);
        Assert.Single(sink.Events, e => e == AiUsage.Core.Diagnostics.DiagnosticEvent.HistoryReattached);
        var other = await Connect(reinstalled, "second");
        Assert.Equal(AccountOutcome.Done, other.Outcome);
        Assert.NotEqual(original, other.AccountId);
        await reinstalled.StopAsync();
    }

    [Fact]
    public async Task UnavailableIdentityMapDoesNotFailSignIn()
    {
        Directory.CreateDirectory(root);
        var blocked = Path.Combine(root, "history-is-a-file");
        File.WriteAllText(blocked, "synthetic");
        using var service = new AccountService(new AccountRegistry(root), new ProviderSessionFactory(services, root),
            identities: new AccountIdentityMap(blocked));
        await service.InitializeAsync(Token);
        Assert.Equal(AccountOutcome.Done, (await Connect(service, "first")).Outcome);
        await service.StopAsync();
    }

    [Fact]
    public async Task FailedReconnectKeepsThePreviousGrantAndReading()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var account = (await Connect(service, "first")).AccountId!.Value;
        var before = Assert.Single((await new AccountRegistry(root).ReadAsync(Token)).Accounts);
        var reading = Assert.Single(service.Current).Session;
        failQuota = true;
        var result = await Connect(service, "first", account);
        Assert.Equal(AccountOutcome.Failed, result.Outcome);
        Assert.Equal(before, Assert.Single((await new AccountRegistry(root).ReadAsync(Token)).Accounts));
        Assert.Equal(reading, Assert.Single(service.Current).Session);
        await service.StopAsync();
    }

    [Fact]
    public async Task LastRefreshNotificationReportsIdle()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var account = (await Connect(service, "first")).AccountId!.Value;
        bool? lastBusy = null;
        service.Changed += (_, _) => lastBusy = Assert.Single(service.Current).Busy;
        await service.RefreshAsync(account, Token);
        Assert.False(lastBusy);
        await service.StopAsync();
    }

    [Fact]
    public async Task SignOutDrainsRefreshAndCannotBeUndoneByItsLateCompletion()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var account = (await Connect(service, "first")).AccountId!.Value;
        quotaStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var refresh = service.RefreshAsync(account, Token);
        await quotaStarted.Task.WaitAsync(Token);
        var signOut = await service.DisconnectAsync(account, Token);
        Assert.Equal(AccountOutcome.Done, signOut.Outcome);
        Assert.Equal(AccountOutcome.Cancelled, (await refresh).Outcome);
        Assert.False(Assert.Single(service.Current).Connected);
        Assert.Equal(ProviderSessionStatus.NotConnected, Assert.Single(service.Current).Session.Status);
        await service.StopAsync();
    }

    [Fact]
    public async Task SwappedProtectedIdentityIsRejectedBeforeRenewalOrQuotaTraffic()
    {
        using var service = Create();
        await service.InitializeAsync(Token);
        var account = (await Connect(service, "first")).AccountId!.Value;
        var record = Assert.Single((await new AccountRegistry(root).ReadAsync(Token)).Accounts);
        var factory = new ProviderSessionFactory(services, root);
        await using (var lease = await new ClaudeStateStore(factory.DirectoryFor(record.StorageId)).AcquireAsync(Token))
        {
            var stored = (await lease.LoadAsync(Token))!;
            await lease.SaveAsync(stored with { Identity = new("other", "synthetic-organization") }, stored.Revision, Token);
        }
        var calls = server.Calls;
        var result = await service.RefreshAsync(account, Token);
        Assert.Equal(ProviderFailureKind.AccountMismatch, result.Failure);
        Assert.Equal(calls, server.Calls);
        Assert.Null(Assert.Single(service.Current).Session.Quota);
        await service.StopAsync();
    }

    public void Dispose()
    {
        services.Dispose();
        http.Dispose();
        server.Dispose();
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
