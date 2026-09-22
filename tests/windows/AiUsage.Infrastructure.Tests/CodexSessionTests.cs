using AiUsage.Core.Providers.Codex;
using System.Net;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers;
using AiUsage.Core.Usage;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexSessionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiusage-session-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AFirstRunWithoutAStoredGrantStaysNotConnected()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("No provider request expected."));
        using var session = Session(server, out _);
        var state = await session.ResumeAsync(TestContext.Current.CancellationToken);
        Assert.Equal(CodexSessionStatus.NotConnected, state.Status);
        Assert.Null(state.Quota);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task ResumeShowsQuotaAndPersistsTheRotatedGrant()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method == HttpMethod.Get
            ? CodexTestServer.Json("""{"plan_type":"synthetic","rate_limit":{"primary_window":{"used_percent":40}}}""")
            : CodexTestServer.Json(CodexTestServer.Tokens())));
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));

        var state = await session.ResumeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodexSessionStatus.QuotaAvailable, state.Status);
        Assert.Equal(60, state.Quota!.Groups.Single().Windows.Single().RemainingPercent);
        Assert.Equal("synthetic-rotated", store.Read()!.RefreshToken);
    }

    [Fact]
    public async Task AnUnavailableQuotaKeepsTheConnectedGrant()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method == HttpMethod.Get
            ? CodexTestServer.Json("{}", HttpStatusCode.ServiceUnavailable)
            : CodexTestServer.Json(CodexTestServer.Tokens())));
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));

        var state = await session.ResumeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodexSessionStatus.QuotaUnavailable, state.Status);
        Assert.Equal(CodexFailureKind.ProviderUnavailable, state.Failure);
        Assert.Null(state.Quota);
        Assert.True(session.HasStoredGrant);
    }

    [Fact]
    public async Task AnInvalidatedGrantAsksForANewSignInAndKeepsTheRecordForRetry()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(
            CodexTestServer.Json("""{"error":"invalid_grant"}""", HttpStatusCode.BadRequest)));
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stale"));

        var state = await session.ResumeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodexSessionStatus.ReauthenticationRequired, state.Status);
        Assert.Null(state.Quota);
        Assert.NotNull(store.Read());
    }

    [Fact]
    public async Task DisconnectRemovesTheGrantAndReturnsToNotConnected()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method == HttpMethod.Get
            ? CodexTestServer.Json("""{"plan_type":"synthetic"}""")
            : CodexTestServer.Json(CodexTestServer.Tokens())));
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));
        await session.ResumeAsync(TestContext.Current.CancellationToken);

        var state = await session.DisconnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodexSessionStatus.NotConnected, state.Status);
        Assert.False(session.HasStoredGrant);
    }

    [Fact]
    public async Task RefreshReusesTheLiveAccessTokenWithoutRenewingTheGrant()
    {
        var refreshes = 0;
        using var server = new CodexTestServer((request, _) =>
        {
            if (request.Method != HttpMethod.Get)
                refreshes++;
            return Task.FromResult(request.Method == HttpMethod.Get
                ? CodexTestServer.Json("""{"plan_type":"synthetic"}""")
                : CodexTestServer.Json(CodexTestServer.Tokens()));
        });
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));
        await session.ResumeAsync(TestContext.Current.CancellationToken);

        var state = await session.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodexSessionStatus.QuotaAvailable, state.Status);
        Assert.Equal(1, refreshes);
    }

    [Fact]
    public async Task AnUnavailableProviderShowsTheLastReadingLabelledStale()
    {
        var failQuota = false;
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method != HttpMethod.Get
            ? CodexTestServer.Json(CodexTestServer.Tokens())
            : failQuota
                ? CodexTestServer.Json("{}", HttpStatusCode.ServiceUnavailable)
                : CodexTestServer.Json("""{"plan_type":"synthetic","rate_limit":{"primary_window":{"used_percent":30}}}""")));
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));
        var fresh = await session.ResumeAsync(TestContext.Current.CancellationToken);
        Assert.False(fresh.FromCache);

        failQuota = true;
        var stale = await session.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodexSessionStatus.QuotaUnavailable, stale.Status);
        Assert.True(stale.FromCache);
        Assert.Equal(70, stale.Quota!.Groups.Single().Windows.Single().RemainingPercent);
        Assert.Equal(fresh.RetrievedAt, stale.RetrievedAt);
    }

    [Fact]
    public async Task ARelaunchShowsCachedValuesBeforeAnyProviderRequest()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method != HttpMethod.Get
            ? CodexTestServer.Json(CodexTestServer.Tokens())
            : CodexTestServer.Json("""{"plan_type":"synthetic","rate_limit":{"primary_window":{"used_percent":10}}}""")));
        using var first = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));
        await first.ResumeAsync(TestContext.Current.CancellationToken);
        var callsAfterFirstRun = server.Calls;

        using var relaunched = Session(server, out _);
        var cached = relaunched.ReadCachedState();

        Assert.True(cached.FromCache);
        Assert.Equal(90, cached.Quota!.Groups.Single().Windows.Single().RemainingPercent);
        Assert.Equal(callsAfterFirstRun, server.Calls);
    }

    [Fact]
    public async Task DisconnectAlsoRemovesTheCachedReading()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method != HttpMethod.Get
            ? CodexTestServer.Json(CodexTestServer.Tokens())
            : CodexTestServer.Json("""{"plan_type":"synthetic","rate_limit":{"primary_window":{"used_percent":10}}}""")));
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));
        await session.ResumeAsync(TestContext.Current.CancellationToken);

        await session.DisconnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CodexSessionStatus.NotConnected, session.ReadCachedState().Status);
        Assert.Null(session.ReadCachedState().Quota);
    }

    [Fact]
    public async Task TheCachedRecordHoldsNoCredentialMaterial()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method != HttpMethod.Get
            ? CodexTestServer.Json(CodexTestServer.Tokens())
            : CodexTestServer.Json("""{"plan_type":"synthetic","rate_limit":{"primary_window":{"used_percent":10}}}""")));
        using var session = Session(server, out var store);
        store.Write(new CodexStoredGrant("synthetic-workspace", "synthetic-stored"));
        await session.ResumeAsync(TestContext.Current.CancellationToken);

        var cacheFile = Path.Combine(root, "codex.quota.json");
        var text = await File.ReadAllTextAsync(cacheFile, TestContext.Current.CancellationToken);
        Assert.Contains("synthetic", text);
        Assert.DoesNotContain("synthetic-stored", text);
        Assert.DoesNotContain("synthetic-rotated", text);
        Assert.DoesNotContain("synthetic-workspace", text);
        Assert.DoesNotContain("synthetic-access", text);
    }

    [Fact]
    public async Task LeaseCoversTheEntireRenewalAndRejectsAnotherSessionBeforeProviderTraffic()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new CodexTestServer(async (request, token) =>
        {
            if (request.Method != HttpMethod.Get)
            {
                entered.SetResult();
                await release.Task.WaitAsync(token);
            }
            return request.Method == HttpMethod.Get ? CodexTestServer.Json("""{"plan_type":"synthetic"}""") : CodexTestServer.Json(CodexTestServer.Tokens());
        });
        using var first = Session(server, out var store);
        store.Write(new("synthetic-workspace", "synthetic-stored"));
        var work = first.ResumeAsync(TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            using var second = Session(server, out _);
            var blocked = await second.ResumeAsync(TestContext.Current.CancellationToken);
            Assert.Equal(CodexSessionStatus.RecoveryRequired, blocked.Status);
            Assert.Equal(CodexFailureKind.StorageUnavailable, blocked.Failure);
            Assert.Equal(1, server.Calls);
            Assert.Equal(ProviderFailureKind.StorageUnavailable, (await Assert.ThrowsAsync<ProviderException>(() => store.DeleteAsync(TestContext.Current.CancellationToken))).Kind);
        }
        finally { release.TrySetResult(); await work; }
        Assert.Equal("synthetic-rotated", store.Read()!.RefreshToken);
    }

    [Fact]
    public async Task PendingRotationFailureNeverReusesInMemoryCredentialsBeforeRecovery()
    {
        var interrupt = false;
        var faulty = new CodexGrantStore(root, () => { if (interrupt) throw new IOException("Synthetic interruption."); });
        faulty.Write(new("synthetic-workspace", "synthetic-stored"));
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.Method == HttpMethod.Get
            ? CodexTestServer.Json("""{"plan_type":"synthetic"}""") : CodexTestServer.Json(CodexTestServer.Tokens())));
        using var http = new HttpClient(server);
        using var session = new CodexSession(new(http), new(http), faulty, new(root));
        interrupt = true;
        var failed = await session.ResumeAsync(TestContext.Current.CancellationToken);
        Assert.Equal(CodexSessionStatus.RecoveryRequired, failed.Status);
        Assert.Equal(CodexFailureKind.StorageUnavailable, failed.Failure);
        Assert.Equal(1, server.Calls); // No quota request after failed durable cutover.
        interrupt = false;
        var recovered = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(CodexSessionStatus.QuotaAvailable, recovered.Status);
        Assert.Equal(3, server.Calls); // Renewal from the recovered grant, then quota.
    }

    [Fact]
    public async Task CorruptStorageMapsToRecoveryAndDisconnectCanRemoveItWithoutProviderTraffic()
    {
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "codex.grant"), "synthetic-corrupt", TestContext.Current.CancellationToken);
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("No provider request expected."));
        using var session = Session(server, out _);
        var cached = session.ReadCachedState();
        Assert.Equal(CodexSessionStatus.RecoveryRequired, cached.Status);
        Assert.Equal(CodexFailureKind.RecoveryRequired, cached.Failure);
        Assert.True(session.HasStoredGrant);
        Assert.Equal(CodexSessionStatus.NotConnected, (await session.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
        Assert.False(session.HasStoredGrant);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task CancellationDuringQuotaDoesNotDiscardTheReturnedRotatingGrant()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var server = new CodexTestServer((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }
            return Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens()));
        });
        using var session = Session(server, out var store);
        store.Write(new("synthetic-workspace", "synthetic-stored"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ResumeAsync(cancellation.Token));
        Assert.Equal("synthetic-rotated", store.Read()!.RefreshToken);
    }

    private CodexSession Session(CodexTestServer server, out CodexGrantStore store)
    {
        var http = new HttpClient(server);
        store = new CodexGrantStore(root);
        return new CodexSession(new CodexAuthClient(http), new CodexQuotaClient(http), store, new CodexQuotaCache(root));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
