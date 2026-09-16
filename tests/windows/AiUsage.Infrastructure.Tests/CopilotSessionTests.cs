using AiUsage.Core.Providers.Copilot;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Copilot;
using System.Net;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CopilotSessionTests : IDisposable
{
    private const string DeviceJson = "{\"device_code\":\"synthetic-device\",\"user_code\":\"ABCD-1234\",\"verification_uri\":\"https://github.com/login/device\",\"expires_in\":900,\"interval\":5}";
    private readonly string directory = Path.Combine(Path.GetTempPath(), "AiUsage.Copilot.Session.Tests", Guid.NewGuid().ToString("N"));
    private readonly ManualClock clock = new(CodexTestServer.Clock.Now);

    [Fact]
    public async Task ConnectCacheResumeAndDisconnectUseOneAccountBoundProtectedRecord()
    {
        using var server = Provider(user: 42);
        using var http = new HttpClient(server);
        var store = new CopilotStateStore(directory);
        using (var session = Session(http, store))
        {
            Assert.Equal(ProviderSessionStatus.NotConnected, (await session.ReadCachedStateAsync(TestContext.Current.CancellationToken)).Status);
            Assert.Equal(0, server.Calls);
            string? shownCode = null;
            var connected = await Drive(session.ConnectAsync(url =>
            {
                Assert.Equal("https://github.com/login/device", url.AbsoluteUri);
                shownCode = session.PendingUserCode;
            }, TestContext.Current.CancellationToken));
            Assert.Equal("ABCD-1234", shownCode);
            Assert.Null(session.PendingUserCode);
            Assert.True(session.HasStoredGrant);
            Assert.Equal(ProviderSessionStatus.QuotaAvailable, connected.Status);
            Assert.Equal(120.5m, connected.CopilotUsage!.AiCredits!.Items[0].GrossQuantity);
            Assert.NotNull(connected.CopilotUsage.PremiumRequests);
        }
        var bytes = await File.ReadAllBytesAsync(Path.Combine(directory, "copilot.state"), TestContext.Current.CancellationToken);
        Assert.DoesNotContain("synthetic-token", System.Text.Encoding.UTF8.GetString(bytes));
        using var resumed = Session(http, store);
        var cache = await resumed.ReadCachedStateAsync(TestContext.Current.CancellationToken);
        Assert.True(cache.FromCache);
        Assert.NotNull(cache.CopilotUsage);
        var calls = server.Calls;
        Assert.Equal(ProviderSessionStatus.QuotaAvailable, (await resumed.ResumeAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(calls + 3, server.Calls); // identity check plus the two documented reports
        Assert.Equal(ProviderSessionStatus.NotConnected, (await resumed.DisconnectAsync(TestContext.Current.CancellationToken)).Status);
        Assert.False(resumed.HasStoredGrant);
        Assert.False(File.Exists(Path.Combine(directory, "copilot.state")));
    }

    [Fact]
    public async Task OneMissingReportDoesNotHideTheOtherAndBothMissingKeepsTheGrant()
    {
        var premium = HttpStatusCode.NotFound;
        var credits = HttpStatusCode.OK;
        using var server = Provider(user: 42, aiCredits: () => credits, premium: () => premium);
        using var http = new HttpClient(server);
        using var session = Session(http, new CopilotStateStore(directory));
        var connected = await Drive(session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken));
        Assert.NotNull(connected.CopilotUsage!.AiCredits);
        Assert.Null(connected.CopilotUsage.PremiumRequests);
        credits = HttpStatusCode.Forbidden;
        var failed = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderSessionStatus.QuotaUnavailable, failed.Status);
        Assert.Equal(ProviderFailureKind.AccessDenied, failed.Failure);
        Assert.True(failed.FromCache);
        Assert.True(session.HasStoredGrant);
    }

    [Fact]
    public async Task RevokedTokenRequiresReauthenticationWithoutFurtherRequests()
    {
        var authorized = true;
        using var server = Provider(user: 42, aiCredits: () => authorized ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
        using var http = new HttpClient(server);
        using var session = Session(http, new CopilotStateStore(directory));
        await Drive(session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken));
        authorized = false;
        var state = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, state.Status);
        Assert.True(state.FromCache);
        var calls = server.Calls;
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, (await session.RefreshAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(calls, server.Calls);
    }

    [Fact]
    public async Task FailedReconnectKeepsThePreviousAccountAndItsCache()
    {
        var user = 42L;
        var credits = HttpStatusCode.OK;
        using var server = Provider(() => user, aiCredits: () => credits, premium: () => credits);
        using var http = new HttpClient(server);
        var store = new CopilotStateStore(directory);
        using var session = Session(http, store);
        await Drive(session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken));
        user = 43;
        credits = HttpStatusCode.NotFound;
        var failed = await Drive(session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.ReportUnavailable, failed.Failure);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        var saved = await lease.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(42, saved!.Identity.AccountId);
        Assert.NotNull(saved.CachedUsage);
    }

    [Fact]
    public async Task SameAccountReconnectRecoversReauthenticationWithoutAnyReport()
    {
        var credits = HttpStatusCode.OK;
        using var server = Provider(() => 42, aiCredits: () => credits, premium: () => credits == HttpStatusCode.OK ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        using var http = new HttpClient(server);
        var store = new CopilotStateStore(directory);
        using var session = Session(http, store);
        await Drive(session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken));
        credits = HttpStatusCode.Unauthorized;
        Assert.Equal(ProviderSessionStatus.ReauthenticationRequired, (await session.RefreshAsync(TestContext.Current.CancellationToken)).Status);
        credits = HttpStatusCode.NotFound;
        var reconnected = await Drive(session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken));
        Assert.Equal(ProviderSessionStatus.QuotaUnavailable, reconnected.Status);
        Assert.Equal(ProviderFailureKind.ReportUnavailable, reconnected.Failure);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        var saved = await lease.LoadAsync(TestContext.Current.CancellationToken);
        Assert.False(saved!.NeedsReauthentication);
        Assert.NotNull(saved.CachedUsage);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, ProviderFailureKind.ProviderUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests, ProviderFailureKind.RateLimited)]
    public async Task TransientReportFailureNeverOverwritesACachedReport(HttpStatusCode failure, ProviderFailureKind expected)
    {
        var credits = HttpStatusCode.OK;
        using var server = Provider(() => 42, aiCredits: () => credits);
        using var http = new HttpClient(server);
        var store = new CopilotStateStore(directory);
        using var session = Session(http, store);
        await Drive(session.ConnectAsync(_ => { }, TestContext.Current.CancellationToken));
        credits = failure;
        var state = await session.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expected, state.Failure);
        Assert.True(state.FromCache);
        await using var lease = await store.AcquireAsync(TestContext.Current.CancellationToken);
        Assert.NotNull((await lease.LoadAsync(TestContext.Current.CancellationToken))!.CachedUsage!.AiCredits);
    }

    [Fact]
    public async Task CancellationAfterIssuanceStillBindsTheIssuedToken()
    {
        using var cancel = new CancellationTokenSource();
        using var server = new CodexTestServer((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/login/device/code") return Task.FromResult(CodexTestServer.Json(DeviceJson));
            if (path == "/login/oauth/access_token")
            {
                cancel.Cancel();
                return Task.FromResult(CodexTestServer.Json("{\"access_token\":\"synthetic-token\",\"token_type\":\"bearer\"}"));
            }
            return Task.FromResult(CodexTestServer.Json("{\"id\":42,\"login\":\"synthetic-user\"}"));
        });
        using var http = new HttpClient(server);
        var auth = new CopilotAuthClient(http, clock);
        var attempt = await auth.BeginDeviceLoginAsync(TestContext.Current.CancellationToken);
        var completion = auth.CompleteDeviceLoginAsync(attempt, cancel.Token);
        for (var i = 0; i < 100 && !completion.IsCompleted; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(5));
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
        Assert.Equal(42, (await completion).Identity.AccountId);
    }

    [Fact]
    public async Task InterruptedStageIsDiscardedAndCorruptRecordsArePreservedForRecovery()
    {
        var store = new CopilotStateStore(directory, afterStage: () => throw new IOException("synthetic interruption"));
        await using (var lease = await store.AcquireAsync(TestContext.Current.CancellationToken))
            await Assert.ThrowsAsync<CopilotException>(() => lease.SaveAsync(Record(), null, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(Path.Combine(directory, "copilot.state.pending")));
        var clean = new CopilotStateStore(directory);
        await using (var lease = await clean.AcquireAsync(TestContext.Current.CancellationToken))
        {
            Assert.Null(await lease.LoadAsync(TestContext.Current.CancellationToken));
            Assert.False(File.Exists(Path.Combine(directory, "copilot.state.pending")));
        }
        await File.WriteAllBytesAsync(Path.Combine(directory, "copilot.state"), [1, 2, 3], TestContext.Current.CancellationToken);
        using var http = new HttpClient(Provider(user: 42));
        using var session = Session(http, clean);
        var state = await session.ReadCachedStateAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProviderSessionStatus.RecoveryRequired, state.Status);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Path.Combine(directory, "copilot.state"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OverlappingWorkIsRejectedAndCanceledConnectionStoresNothing()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(CodexTestServer.Json(
            request.RequestUri!.AbsolutePath == "/login/device/code" ? DeviceJson : "{\"error\":\"authorization_pending\"}")));
        using var http = new HttpClient(server);
        using var session = Session(http, new CopilotStateStore(directory));
        using var cancel = new CancellationTokenSource();
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connect = session.ConnectAsync(_ => opened.SetResult(), cancel.Token);
        await opened.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.RefreshAsync(TestContext.Current.CancellationToken));
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connect);
        Assert.False(session.HasStoredGrant);
        Assert.Null(session.PendingUserCode);
        Assert.False(File.Exists(Path.Combine(directory, "copilot.state")));
    }

    private CopilotSession Session(HttpClient http, CopilotStateStore store) =>
        new(new CopilotAuthClient(http, clock), new CopilotUsageClient(http, clock), store);

    private static CopilotStoredState Record() => new() { Identity = new(42, "synthetic-user"), AccessToken = "synthetic-token" };

    private CodexTestServer Provider(long user, Func<HttpStatusCode>? aiCredits = null, Func<HttpStatusCode>? premium = null) =>
        Provider(() => user, aiCredits, premium);

    private static CodexTestServer Provider(Func<long> user, Func<HttpStatusCode>? aiCredits = null, Func<HttpStatusCode>? premium = null) =>
        new((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/login/device/code") return Task.FromResult(CodexTestServer.Json(DeviceJson));
            if (path == "/login/oauth/access_token")
                return Task.FromResult(CodexTestServer.Json("{\"access_token\":\"synthetic-token\",\"token_type\":\"bearer\",\"scope\":\"read:user\"}"));
            if (path == "/user")
                return Task.FromResult(CodexTestServer.Json($"{{\"id\":{user()},\"login\":\"synthetic-user\"}}"));
            var status = (path.Contains("/ai_credit/", StringComparison.Ordinal) ? aiCredits : premium)?.Invoke() ?? HttpStatusCode.OK;
            return Task.FromResult(status == HttpStatusCode.OK
                ? CodexTestServer.Json(CopilotUsageTests.Report)
                : CodexTestServer.Json("{\"message\":\"synthetic-secret\"}", status));
        });

    private async Task<ProviderSessionState> Drive(Task<ProviderSessionState> operation)
    {
        for (var i = 0; i < 2000 && !operation.IsCompleted; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(5));
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
        return await operation;
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (DirectoryNotFoundException) { }
    }
}
