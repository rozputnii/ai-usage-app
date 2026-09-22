using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Copilot;
using System.Net;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CopilotSessionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "AiUsage.Copilot.Session.Tests", Guid.NewGuid().ToString("N"));
    private readonly CodexTestServer.Clock clock = new();
    private HttpStatusCode usageStatus = HttpStatusCode.OK;
    private string identity = "123";
    private HttpResponseMessage Respond(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch
    {
        "/login/device/code" => CodexTestServer.Json(CopilotProtocolTests.Device),
        "/login/oauth/access_token" => CodexTestServer.Json(CopilotProtocolTests.Token),
        "/user" => CodexTestServer.Json("{\"id\":" + identity + "}"),
        "/copilot_internal/user" => CodexTestServer.Json(CopilotProtocolTests.Usage, usageStatus),
        _ => throw new InvalidOperationException("Unexpected endpoint.")
    };
    private CopilotSession Session(HttpClient http) => new(new(http, clock, (_, token) => { token.ThrowIfCancellationRequested(); return Task.CompletedTask; }), new(http, clock), new(directory), clock, new(http, clock));

    [Fact]
    public async Task ConnectResumeDisconnectAndReconnectKeepProviderOwnedState()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using (var initial = Session(http))
        {
            Assert.Equal(ProviderSessionStatus.NotConnected, (await initial.ReadCachedStateAsync(TestContext.Current.CancellationToken)).Status);
            Assert.Equal(0, server.Calls);
            Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await initial.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken)).Status);
        }
        using var resumed = Session(http);
        Assert.True((await resumed.ReadCachedStateAsync(TestContext.Current.CancellationToken)).FromCache);
        Assert.Equal(4, server.Calls);
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await resumed.ResumeAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(6, server.Calls); // No synthetic OAuth refresh or inference-token exchange.
        Assert.Equal(ProviderSessionStatus.NotConnected, (await resumed.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
        Assert.False(resumed.HasStoredGrant);
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await resumed.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task FailedReplacementPreservesPreviousGrantAndCache()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken);
        usageStatus = HttpStatusCode.Forbidden;
        var failed = await session.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.AccessDenied, failed.Failure);
        Assert.True(failed.FromCache);
        await using var lease = await new CopilotStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        Assert.Equal("123", (await lease.LoadAsync(TestContext.Current.CancellationToken))!.AccountId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnauthorizedOrMismatchedIdentityRequiresReconnectWithoutRetry(bool mismatch)
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken);
        if (mismatch) identity = "456"; else usageStatus = HttpStatusCode.Unauthorized;
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, (await session.RefreshAsync(TestContext.Current.CancellationToken)).Status);
        var calls = server.Calls;
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, (await session.RefreshAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(calls, server.Calls);
    }

    [Fact]
    public async Task ReconnectCannotReplaceAccountIdentityEvenWhenQuotaWouldSucceed()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken);
        identity = "456";
        var calls = server.Calls;
        var result = await session.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderFailureKind.AccountMismatch, result.Failure);
        Assert.True(result.FromCache);
        Assert.Equal(calls + 3, server.Calls); // Reject before a new quota request or durable write.
        await using var lease = await new CopilotStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken);
        Assert.Equal("123", (await lease.LoadAsync(TestContext.Current.CancellationToken))!.AccountId);
    }

    [Fact]
    public async Task ExpiredGrantRequiresReconnectWithoutNetworkOrSyntheticRefresh()
    {
        await using (var lease = await new CopilotStateStore(directory).AcquireAsync(TestContext.Current.CancellationToken))
            await lease.SaveAsync(CopilotStateStoreTests.State() with { ExpiresAt = clock.GetUtcNow() }, null, TestContext.Current.CancellationToken);
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Expired grant must not be sent."));
        using var http = new HttpClient(server);
        using var session = Session(http);
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, (await session.ResumeAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(0, server.Calls);
        Assert.True(session.HasStoredGrant);
    }

    [Fact]
    public async Task FailedDeleteReportsRecoveryAndKeepsGrantUntilExplicitRetry()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(Respond(request)));
        using var http = new HttpClient(server);
        using var session = Session(http);
        await session.ConnectWithChallengeAsync(_ => { }, TestContext.Current.CancellationToken);
        var path = Path.Combine(directory, "copilot.state");
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
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.ConnectWithChallengeAsync(_ => cancel.Cancel(), cancel.Token));
        Assert.False(session.HasStoredGrant);
        Assert.Equal(ProviderSessionStatus.NotConnected, session.State.Status);
        Assert.False(File.Exists(Path.Combine(directory, "copilot.state")));
        Assert.Equal(1, server.Calls);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
}
