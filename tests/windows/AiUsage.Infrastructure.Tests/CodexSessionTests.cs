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

    private CodexSession Session(CodexTestServer server, out CodexGrantStore store)
    {
        var http = new HttpClient(server);
        store = new CodexGrantStore(root);
        return new CodexSession(new CodexAuthClient(http), new CodexQuotaClient(http), store);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
