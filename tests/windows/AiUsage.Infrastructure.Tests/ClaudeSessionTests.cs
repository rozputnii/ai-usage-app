using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Claude;
using System.Net;
using System.Text.Json;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ClaudeSessionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "AiUsage.Claude.Session.Tests", Guid.NewGuid().ToString("N"));
    private const string Usage = "{\"five_hour\":{\"utilization\":25,\"resets_at\":\"2030-01-01T04:00:00Z\"}}";

    [Fact]
    public void RepeatedDisposalOfAnIdleSessionIsHarmless()
    {
        var session = new ClaudeSession(null!, null!, null!, TimeProvider.System);
        session.Dispose();
        session.Dispose();
    }

    [Fact]
    public async Task ConnectCacheResumeAndDisconnectUseOneAccountBoundProtectedRecord()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(CodexTestServer.Json(request.Method == HttpMethod.Post ? ClaudeAuthClientTests.Tokens() : Usage)));
        using var http = new HttpClient(server);
        var store = new ClaudeStateStore(directory);
        using (var session = Session(http, store))
        {
            Assert.Equal(ProviderSessionStatus.NotConnected, (await session.ReadCachedStateAsync(TestContext.Current.CancellationToken)).Status);
            Assert.Equal(0, server.Calls);
            var connected = await session.ConnectAsync(_ => Assert.True(session.TrySubmitCode("synthetic-code")), TestContext.Current.CancellationToken);
            Assert.True(session.HasStoredGrant);
            Assert.Equal(ProviderSessionStatus.QuotaAvailable, connected.Status);
            Assert.Equal(75, Assert.Single(Assert.Single(connected.Quota!.Groups).Windows).RemainingPercent);
        }
        using var resumed = Session(http, store);
        var cache = await resumed.ReadCachedStateAsync(TestContext.Current.CancellationToken);
        Assert.True(cache.FromCache);
        Assert.Equal(2, server.Calls);
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await resumed.ResumeAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(4, server.Calls);
        Assert.Equal(ProviderSessionStatus.NotConnected, (await resumed.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
        Assert.False(resumed.HasStoredGrant);
        Assert.False(File.Exists(Path.Combine(directory, "claude.state")));
    }

    [Fact]
    public async Task ReturnedRotationIsPersistedEvenWhenQuotaIsCanceled()
    {
        var store = new ClaudeStateStore(directory);
        await SeedAsync(store);
        using var cancel = new CancellationTokenSource();
        using var server = new CodexTestServer((request, token) =>
        {
            if (request.Method == HttpMethod.Post)
                return Task.FromResult(CodexTestServer.Json(ClaudeAuthClientTests.Tokens().Replace("synthetic-refresh", "synthetic-rotated", StringComparison.Ordinal)));
            cancel.Cancel();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException();
        });
        using var http = new HttpClient(server);
        using var session = Session(http, store);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ResumeAsync(cancel.Token));
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        var saved = await lease.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-rotated", saved!.RefreshToken);
        Assert.False(saved.NeedsReauthentication);
        Assert.True(session.HasStoredGrant);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AmbiguousOrCrossOrganizationRefreshCannotReplayThePreviousGrant(bool mismatch)
    {
        var store = new ClaudeStateStore(directory);
        await SeedAsync(store);
        using var server = new CodexTestServer((_, _) => mismatch
            ? Task.FromResult(CodexTestServer.Json(ClaudeAuthClientTests.Tokens(organization: "other-organization")))
            : throw new HttpRequestException("synthetic-secret network detail"));
        using var http = new HttpClient(server);
        using (var session = Session(http, store))
        {
            var state = await session.ResumeAsync(TestContext.Current.CancellationToken);
            Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, state.Status);
            Assert.Equal(mismatch ? ProviderFailureKind.AccountMismatch : ProviderFailureKind.NetworkFailure, state.Failure);
        }
        using var restarted = Session(http, store);
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, (await restarted.ResumeAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(1, server.Calls);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        var saved = await lease.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-organization", saved!.Identity.OrganizationId);
        Assert.True(saved.NeedsReauthentication);
    }

    [Fact]
    public async Task FailedReconnectAndQuotaFailureKeepTheLastMatchingCache()
    {
        var store = new ClaudeStateStore(directory);
        var phase = 0;
        using var server = new CodexTestServer((request, _) => Task.FromResult(phase switch
        {
            1 => CodexTestServer.Json("{\"error\":\"synthetic-secret\"}", HttpStatusCode.ServiceUnavailable),
            2 => CodexTestServer.Json("{\"access_token\":\"synthetic-invalid\"}"),
            _ => CodexTestServer.Json(request.Method == HttpMethod.Post ? ClaudeAuthClientTests.Tokens() : Usage)
        }));
        using var http = new HttpClient(server);
        using var session = Session(http, store);
        await session.ConnectAsync(_ => Assert.True(session.TrySubmitCode("synthetic-code")), TestContext.Current.CancellationToken);
        phase = 1;
        var failedQuota = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderSessionStatus.QuotaUnavailable, failedQuota.Status);
        Assert.True(failedQuota.FromCache);
        Assert.Equal(25, failedQuota.Quota!.Groups[0].Windows[0].UsedPercent);
        phase = 2;
        var failedReconnect = await session.ConnectAsync(_ => Assert.True(session.TrySubmitCode("synthetic-code")), TestContext.Current.CancellationToken);
        Assert.True(failedReconnect.FromCache);
        Assert.Equal(ProviderFailureKind.InvalidResponse, failedReconnect.Failure);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        var saved = await lease.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-account", saved!.Identity.AccountId);
        Assert.False(saved.NeedsReauthentication);
        Assert.Equal(25, saved.CachedQuota!.Quota.Groups[0].Windows[0].UsedPercent);
    }

    [Theory]
    [InlineData(false, "unauthorized")]
    [InlineData(false, "network")]
    [InlineData(false, "canceled")]
    [InlineData(true, "unauthorized")]
    [InlineData(true, "network")]
    [InlineData(true, "canceled")]
    public async Task FailedReconnectPreservesThePreviousGenerationAndUsableConnection(bool differentAccount, string failure)
    {
        var store = new ClaudeStateStore(directory);
        var reconnecting = false;
        using var cancel = new CancellationTokenSource();
        using var server = new CodexTestServer((request, token) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var tokens = ClaudeAuthClientTests.Tokens(account: reconnecting && differentAccount ? "other-account" : "synthetic-account");
                if (reconnecting) tokens = tokens.Replace("synthetic-access", "synthetic-new-access", StringComparison.Ordinal)
                    .Replace("synthetic-refresh", "synthetic-new-refresh", StringComparison.Ordinal);
                return Task.FromResult(CodexTestServer.Json(tokens));
            }
            if (!reconnecting)
            {
                Assert.Equal("Bearer synthetic-access", request.Headers.Authorization!.ToString());
                return Task.FromResult(CodexTestServer.Json(Usage));
            }
            if (failure == "network") throw new HttpRequestException("Synthetic connection failure.");
            if (failure == "canceled")
            {
                cancel.Cancel();
                token.ThrowIfCancellationRequested();
            }
            return Task.FromResult(CodexTestServer.Json("{}", HttpStatusCode.Unauthorized));
        });
        using var http = new HttpClient(server);
        using var session = Session(http, store);
        await session.ConnectAsync(_ => Assert.True(session.TrySubmitCode("synthetic-code")), TestContext.Current.CancellationToken);
        Guid previousRevision;
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
            previousRevision = (await lease.LoadAsync(TestContext.Current.CancellationToken))!.Revision;
        reconnecting = true;
        var reconnect = session.ConnectAsync(_ => Assert.True(session.TrySubmitCode("synthetic-code")), cancel.Token);
        if (failure == "canceled")
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reconnect);
        else
            Assert.True((await reconnect).FromCache);
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
        {
            var saved = (await lease.LoadAsync(TestContext.Current.CancellationToken))!;
            Assert.Equal(previousRevision, saved.Revision);
            Assert.Equal("synthetic-account", saved.Identity.AccountId);
            Assert.Equal("synthetic-refresh", saved.RefreshToken);
            Assert.False(saved.NeedsReauthentication);
            Assert.Equal(25, saved.CachedQuota!.Quota.Groups[0].Windows[0].UsedPercent);
        }
        reconnecting = false;
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await session.RefreshAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task OverlapDoesNotQueueAnotherRefreshAndDisposalRequiresDrain()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancel = new CancellationTokenSource();
        using var server = new CodexTestServer(async (request, token) =>
        {
            if (request.Method == HttpMethod.Post) return CodexTestServer.Json(ClaudeAuthClientTests.Tokens());
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return CodexTestServer.Json(Usage);
        });
        using var http = new HttpClient(server);
        using var session = Session(http, new ClaudeStateStore(directory));
        var connect = session.ConnectAsync(_ => Assert.True(session.TrySubmitCode("synthetic-code")), cancel.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.RefreshAsync(TestContext.Current.CancellationToken));
        Assert.Throws<InvalidOperationException>(session.Dispose);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connect);
        Assert.Equal(2, server.Calls);
    }

    [Fact]
    public async Task FailedDeletionKeepsARecoverableConnectedState()
    {
        var store = new ClaudeStateStore(directory);
        await SeedAsync(store);
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException());
        using var http = new HttpClient(server);
        using var session = Session(http, store);
        using var blocked = new FileStream(Path.Combine(directory, "claude.state"), FileMode.Open, FileAccess.Read, FileShare.Read);
        var state = await session.DisconnectAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.GrantNotRemoved, state.Failure);
        Assert.True(session.HasStoredGrant);
        Assert.True(File.Exists(Path.Combine(directory, "claude.state")));
        Assert.Equal(0, server.Calls);
    }

    private static ClaudeSession Session(HttpClient http, ClaudeStateStore store) => new(
        new ClaudeAuthClient(http, new CodexTestServer.Clock()), new ClaudeQuotaClient(http, new CodexTestServer.Clock()), store, new CodexTestServer.Clock());
    private static async Task SeedAsync(ClaudeStateStore store)
    {
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        await lease.SaveAsync(ClaudeStateStoreTests.State(), null, TestContext.Current.CancellationToken);
    }
    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
