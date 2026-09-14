using AiUsage.Core.Providers.Codex;
using System.Net;
using AiUsage.Infrastructure.Providers.Codex;
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
