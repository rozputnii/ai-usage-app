using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using System.Net;
using System.Web;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AntigravitySessionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "AiUsage.Antigravity.Session.Tests", Guid.NewGuid().ToString("N"));
    private readonly CodexTestServer.Clock clock = new();
    private HttpStatusCode summaryStatus = HttpStatusCode.OK;
    private string identity = "104729";
    private string workspace = AntigravityProtocolTests.Workspace;
    private string refreshToken = "synthetic-refresh";

    private HttpResponseMessage Respond(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch
    {
        "/token" => CodexTestServer.Json(AntigravityProtocolTests.Tokens(refreshToken)),
        "/oauth2/v1/userinfo" => CodexTestServer.Json("{\"id\":\"" + identity + "\"}"),
        "/v1internal:loadCodeAssist" => CodexTestServer.Json(workspace),
        "/v1internal:retrieveUserQuotaSummary" => CodexTestServer.Json(AntigravityProtocolTests.Summary, summaryStatus),
        _ => throw new InvalidOperationException("Unexpected endpoint.")
    };

    private AntigravitySession Session(HttpClient http) =>
        new(AntigravityProtocolTests.Auth(http, clock), new(http, clock), new(directory), clock);

    /// <summary>Answers the loopback callback without blocking the caller that is about to accept it.</summary>
    private static void Redirect(Uri authorizationUrl)
    {
        var query = HttpUtility.ParseQueryString(authorizationUrl.Query);
        var target = query["redirect_uri"] + "?code=synthetic-code&state=" + query["state"];
        _ = Task.Run(async () =>
        {
            using var callback = new HttpClient();
            await callback.GetStringAsync(target);
        });
    }

    [Fact]
    public async Task ConnectResumeDisconnectAndReconnectKeepProviderOwnedState()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using (var initial = Session(http))
        {
            Assert.Equal(ProviderSessionStatus.NotConnected, (await initial.ReadCachedStateAsync(TestContext.Current.CancellationToken)).Status);
            Assert.Equal(0, server.Calls);
            var connected = await initial.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
            Assert.Equal(ProviderSessionStatus.QuotaAvailable, connected.Status);
            Assert.Equal("free-tier", connected.Quota!.PlanType);
            Assert.Equal(4, server.Calls); // exchange, identity, one discovery, one quota read
        }
        using var resumed = Session(http);
        var cached = await resumed.ReadCachedStateAsync(TestContext.Current.CancellationToken);
        Assert.True(cached.FromCache);
        Assert.Equal(4, server.Calls);
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await resumed.ResumeAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(7, server.Calls); // renewal, identity revalidation, quota; discovery is not repeated
        Assert.Equal(ProviderSessionStatus.NotConnected, (await resumed.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
        Assert.False(resumed.HasStoredGrant);
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await resumed.ConnectAsync(Redirect, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task ARotatedRefreshTokenIsDurableBeforeTheQuotaRequest()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using (var initial = Session(http))
            await initial.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        refreshToken = "synthetic-rotated";
        summaryStatus = HttpStatusCode.InternalServerError;
        using var resumed = Session(http);
        Assert.Equal(ProviderFailureKind.ProviderUnavailable, (await resumed.ResumeAsync(TestContext.Current.CancellationToken)).Failure);
        await using var lease = await new AntigravityStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        var stored = await lease.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-rotated", stored!.RefreshToken);
        Assert.False(stored.NeedsReauthentication);
    }

    [Fact]
    public async Task ReconnectCannotReplaceAccountIdentityEvenWhenQuotaWouldSucceed()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        identity = "999";
        var calls = server.Calls;
        var result = await session.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.AccountMismatch, result.Failure);
        Assert.True(result.FromCache);
        Assert.Equal(calls + 2, server.Calls); // Rejected before discovery, quota or a durable write.
        await using var lease = await new AntigravityStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        Assert.Equal("104729", (await lease.LoadAsync(TestContext.Current.CancellationToken))!.AccountId);
    }

    [Fact]
    public async Task FailedReplacementPreservesThePreviousGrantAndCache()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        summaryStatus = HttpStatusCode.Forbidden;
        var failed = await session.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.AccessDenied, failed.Failure);
        Assert.True(failed.FromCache);
        await using var lease = await new AntigravityStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        var stored = await lease.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-project", stored!.ProjectId);
        Assert.NotNull(stored.CachedQuota);
    }

    [Fact]
    public async Task UnauthorizedQuotaRequiresReconnectWithoutAnotherProviderRequest()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        summaryStatus = HttpStatusCode.Unauthorized;
        var reauth = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, reauth.Status);
        Assert.True(reauth.FromCache);
        var calls = server.Calls;
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, (await session.RefreshAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(calls, server.Calls);
    }

    [Fact]
    public async Task AnAccountWithoutAWorkspaceIsNeverStoredAsAConnection()
    {
        workspace = "{}";
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        var result = await session.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.ProjectUnavailable, result.Failure);
        Assert.Equal(ProviderSessionStatus.NotConnected, result.Status);
        Assert.False(session.HasStoredGrant);
        Assert.False(File.Exists(Path.Combine(directory, "antigravity.state")));
    }

    [Fact]
    public async Task FailedDeleteReportsRecoveryAndKeepsTheGrantUntilExplicitRetry()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectAsync(Redirect, TestContext.Current.CancellationToken);
        var path = Path.Combine(directory, "antigravity.state");
        using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var result = await session.DisconnectAsync(TestContext.Current.CancellationToken);
            Assert.Equal(ProviderSessionStatus.RecoveryRequired, result.Status);
            Assert.Equal(ProviderFailureKind.GrantNotRemoved, result.Failure);
            Assert.True(session.HasStoredGrant);
            Assert.True(File.Exists(path));
        }
        Assert.Equal(ProviderSessionStatus.NotConnected, (await session.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task CancelledAuthorizationDoesNotWriteAConnection()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        using var cancel = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ConnectAsync(_ => cancel.Cancel(), cancel.Token));
        Assert.False(session.HasStoredGrant);
        Assert.Equal(ProviderSessionStatus.NotConnected, session.State.Status);
        Assert.False(File.Exists(Path.Combine(directory, "antigravity.state")));
        Assert.Equal(0, server.Calls);
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
}
