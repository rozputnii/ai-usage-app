using AiUsage.Core.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Copilot;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class CopilotAuthClientTests
{
    private const string DeviceJson = "{\"device_code\":\"synthetic-device\",\"user_code\":\"ABCD-1234\",\"verification_uri\":\"https://github.com/login/device\",\"expires_in\":900,\"interval\":5}";
    private const string UserJson = "{\"id\":42,\"login\":\"synthetic-user\",\"email\":\"synthetic-secret@example.invalid\"}";

    [Fact]
    public async Task DeviceFlowUsesTheRecordedClientAndBindsTheNumericAccount()
    {
        var requests = new List<(string Url, string Body, string? Authorization)>();
        using var server = new CodexTestServer(async (request, token) =>
        {
            requests.Add((request.RequestUri!.AbsoluteUri, request.Content is null ? "" : await request.Content.ReadAsStringAsync(token), request.Headers.Authorization?.ToString()));
            return requests.Count switch
            {
                1 => CodexTestServer.Json(DeviceJson),
                2 => CodexTestServer.Json("{\"error\":\"authorization_pending\"}"),
                3 => CodexTestServer.Json("{\"access_token\":\"synthetic-token\",\"token_type\":\"bearer\",\"scope\":\"read:user\"}"),
                _ => CodexTestServer.Json(UserJson)
            };
        });
        var clock = new ManualClock(CodexTestServer.Clock.Now);
        using var http = new HttpClient(server);
        var client = new CopilotAuthClient(http, clock);
        var authorization = await client.BeginDeviceLoginAsync(TestContext.Current.CancellationToken);
        Assert.Equal("ABCD-1234", authorization.UserCode);
        Assert.DoesNotContain("synthetic-device", authorization.ToString());
        var completion = client.CompleteDeviceLoginAsync(authorization, TestContext.Current.CancellationToken);
        while (!completion.IsCompleted)
        {
            clock.Advance(TimeSpan.FromSeconds(5));
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
        var credentials = await completion;
        Assert.Equal(new CopilotIdentity(42, "synthetic-user"), credentials.Identity);
        Assert.Equal("read:user", credentials.GrantedScope);
        Assert.DoesNotContain("synthetic-token", credentials.ToString());
        Assert.Equal("https://github.com/login/device/code", requests[0].Url);
        Assert.Equal("client_id=Ov23li8tweQw6odWQebz&scope=read%3Auser", requests[0].Body);
        Assert.Contains("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Adevice_code", requests[1].Body);
        Assert.Equal("https://api.github.com/user", requests[3].Url);
        Assert.Equal("Bearer synthetic-token", requests[3].Authorization);
        Assert.Equal(4, server.Calls);
    }

    [Theory]
    [InlineData("access_denied", CopilotFailureKind.LoginDenied)]
    [InlineData("expired_token", CopilotFailureKind.LoginAttemptExpired)]
    [InlineData("device_flow_disabled", CopilotFailureKind.RequestRejected)]
    [InlineData("synthetic-secret-unknown", CopilotFailureKind.RequestRejected)]
    public async Task TerminalDeviceErrorsEndTheAttemptWithoutReplay(string error, CopilotFailureKind expected)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json($"{{\"error\":\"{error}\",\"error_description\":\"synthetic-secret\"}}")));
        var (client, authorization, clock) = await BeginAsync(server);
        var failure = await CompleteAsync(client, authorization, clock);
        Assert.Equal(expected, failure.Kind);
        Assert.DoesNotContain("synthetic-secret", failure.ToString());
        var again = await Assert.ThrowsAsync<CopilotException>(() => client.CompleteDeviceLoginAsync(authorization, TestContext.Current.CancellationToken));
        Assert.Equal(CopilotFailureKind.AuthenticationRequired, again.Kind);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task SlowDownIncreasesTheIntervalAndExpiryStopsPolling()
    {
        var polls = new List<DateTimeOffset>();
        ManualClock? time = null;
        using var server = new CodexTestServer((_, _) =>
        {
            polls.Add(time!.GetUtcNow());
            return Task.FromResult(CodexTestServer.Json("{\"error\":\"slow_down\",\"interval\":10}"));
        });
        using var http = new HttpClient(new DeviceStartHandler(server, "{\"device_code\":\"d\",\"user_code\":\"U\",\"verification_uri\":\"https://github.com/login/device\",\"expires_in\":30,\"interval\":5}"));
        time = new ManualClock(CodexTestServer.Clock.Now);
        var client = new CopilotAuthClient(http, time);
        var authorization = await client.BeginDeviceLoginAsync(TestContext.Current.CancellationToken);
        var failure = await CompleteAsync(client, authorization, time);
        Assert.Equal(CopilotFailureKind.LoginAttemptExpired, failure.Kind);
        Assert.Equal(2, polls.Count);
        Assert.True(polls[1] - polls[0] >= TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("{\"device_code\":\"d\",\"user_code\":\"U\",\"verification_uri\":\"https://evil.invalid/login/device\",\"expires_in\":900,\"interval\":5}")]
    [InlineData("{\"device_code\":\"d\",\"user_code\":\"U\",\"verification_uri\":\"http://github.com/login/device\",\"expires_in\":900,\"interval\":5}")]
    [InlineData("{\"device_code\":\"d\",\"user_code\":\"U\",\"verification_uri\":\"https://github.com/login/device\",\"interval\":5}")]
    [InlineData("{\"error\":\"unauthorized_client\"}")]
    [InlineData("[]")]
    public async Task UntrustedOrIncompleteDeviceStartIsRejected(string json)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(json)));
        using var http = new HttpClient(server);
        var failure = await Assert.ThrowsAsync<CopilotException>(() => new CopilotAuthClient(http).BeginDeviceLoginAsync(TestContext.Current.CancellationToken));
        Assert.Contains(failure.Kind, new[] { CopilotFailureKind.InvalidResponse, CopilotFailureKind.RequestRejected });
    }

    [Theory]
    [InlineData("{\"login\":\"synthetic-user\"}")]
    [InlineData("{\"id\":0,\"login\":\"synthetic-user\"}")]
    [InlineData("{\"id\":42,\"login\":\"../synthetic\"}")]
    public async Task IssuedTokenWithoutStableIdentityIsNotReturned(string user)
    {
        var calls = 0;
        using var server = new CodexTestServer((_, _) => Task.FromResult(++calls == 1
            ? CodexTestServer.Json("{\"access_token\":\"synthetic-token\",\"token_type\":\"bearer\"}")
            : CodexTestServer.Json(user)));
        var (client, authorization, clock) = await BeginAsync(server);
        var failure = await CompleteAsync(client, authorization, clock);
        Assert.Equal(CopilotFailureKind.InvalidResponse, failure.Kind);
        var again = await Assert.ThrowsAsync<CopilotException>(() => client.CompleteDeviceLoginAsync(authorization, TestContext.Current.CancellationToken));
        Assert.Equal(CopilotFailureKind.AuthenticationRequired, again.Kind);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, false, CopilotFailureKind.AuthenticationRequired)]
    [InlineData(HttpStatusCode.Forbidden, false, CopilotFailureKind.AccessDenied)]
    [InlineData(HttpStatusCode.Forbidden, true, CopilotFailureKind.RateLimited)]
    [InlineData(HttpStatusCode.TooManyRequests, true, CopilotFailureKind.RateLimited)]
    [InlineData(HttpStatusCode.BadGateway, false, CopilotFailureKind.ProviderUnavailable)]
    public async Task VerifyClassifiesProviderRejectionAndRejectsAnotherAccount(HttpStatusCode status, bool retryAfter, CopilotFailureKind expected)
    {
        var calls = 0;
        using var server = new CodexTestServer((_, _) =>
        {
            calls++;
            if (calls <= 2)
                return Task.FromResult(calls == 1 ? CodexTestServer.Json("{\"access_token\":\"synthetic-token\",\"token_type\":\"bearer\"}") : CodexTestServer.Json(UserJson));
            if (calls == 3)
                return Task.FromResult(CodexTestServer.Json("{\"id\":43,\"login\":\"synthetic-user\"}"));
            var response = CodexTestServer.Json("{\"message\":\"synthetic-secret\"}", status);
            if (retryAfter) response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
            return Task.FromResult(response);
        });
        var (client, authorization, clock) = await BeginAsync(server);
        var completion = client.CompleteDeviceLoginAsync(authorization, TestContext.Current.CancellationToken);
        while (!completion.IsCompleted) { clock.Advance(TimeSpan.FromSeconds(5)); await Task.Delay(1, TestContext.Current.CancellationToken); }
        var credentials = await completion;
        var mismatch = await Assert.ThrowsAsync<CopilotException>(() => client.VerifyAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CopilotFailureKind.AccountMismatch, mismatch.Kind);
        var failure = await Assert.ThrowsAsync<CopilotException>(() => client.VerifyAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(expected, failure.Kind);
        Assert.DoesNotContain("synthetic-secret", failure.ToString());
    }

    [Fact]
    public async Task CancellationStopsPollingWithoutARequest()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("{\"error\":\"authorization_pending\"}")));
        var (client, authorization, _) = await BeginAsync(server);
        using var cancellation = new CancellationTokenSource();
        var completion = client.CompleteDeviceLoginAsync(authorization, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
        Assert.Equal(0, server.Calls);
    }

    private static async Task<(CopilotAuthClient Client, CopilotDeviceAuthorization Authorization, ManualClock Clock)> BeginAsync(CodexTestServer server)
    {
        var clock = new ManualClock(CodexTestServer.Clock.Now);
        var http = new HttpClient(new DeviceStartHandler(server, DeviceJson));
        var client = new CopilotAuthClient(http, clock);
        return (client, await client.BeginDeviceLoginAsync(TestContext.Current.CancellationToken), clock);
    }

    private static async Task<CopilotException> CompleteAsync(CopilotAuthClient client, CopilotDeviceAuthorization authorization, ManualClock clock)
    {
        var completion = client.CompleteDeviceLoginAsync(authorization, TestContext.Current.CancellationToken);
        for (var i = 0; i < 1000 && !completion.IsCompleted; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(5));
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }
        return await Assert.ThrowsAsync<CopilotException>(() => completion);
    }

    /// <summary>Answers the device-code request so the inner server sees only polling and API calls.</summary>
    private sealed class DeviceStartHandler(HttpMessageHandler inner, string deviceJson) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            request.RequestUri!.AbsoluteUri == "https://github.com/login/device/code"
                ? Task.FromResult(CodexTestServer.Json(deviceJson))
                : base.SendAsync(request, cancellationToken);
    }
}
