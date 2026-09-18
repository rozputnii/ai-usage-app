using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Copilot;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class CopilotProtocolTests
{
    internal const string Device = """{"device_code":"synthetic-device","user_code":"ABCD-1234","verification_uri":"https://github.com/login/device","interval":5,"expires_in":900}""";
    internal const string Token = """{"access_token":"synthetic-access","token_type":"bearer","scope":"read:user"}""";
    internal const string Usage = """{"copilot_plan":"individual","quota_reset_date":"2030-02-01","quota_snapshots":{"premium_interactions":{"entitlement":300,"remaining":225,"percent_remaining":75,"unlimited":false},"chat":{"unlimited":true}}}""";

    [Fact]
    public async Task DeviceFlowUsesOmpRegistrationAndBackoffWithoutModelSideEffects()
    {
        var clock = new CodexTestServer.Clock();
        var waits = new List<TimeSpan>();
        var polls = 0;
        using var server = new CodexTestServer(async (request, token) =>
        {
            Assert.Equal("AiUsage/0.1", request.Headers.UserAgent.ToString());
            switch (request.RequestUri!.AbsolutePath)
            {
                case "/login/device/code":
                    Assert.Equal("client_id=Ov23li8tweQw6odWQebz&scope=read%3Auser", await request.Content!.ReadAsStringAsync(token));
                    return CodexTestServer.Json(Device);
                case "/login/oauth/access_token":
                    Assert.Contains("device_code=synthetic-device", await request.Content!.ReadAsStringAsync(token));
                    return CodexTestServer.Json(++polls switch { 1 => """{"error":"authorization_pending"}""", 2 => """{"error":"slow_down","interval":10}""", _ => Token });
                case "/user":
                    Assert.Equal("synthetic-access", request.Headers.Authorization!.Parameter);
                    return CodexTestServer.Json("""{"id":123,"login":"synthetic-name"}""");
                default: throw new InvalidOperationException("Unexpected side effect.");
            }
        });
        using var http = new HttpClient(server);
        var auth = new CopilotAuthClient(http, clock, (duration, token) =>
        {
            token.ThrowIfCancellationRequested(); waits.Add(duration); clock.Current += duration; return Task.CompletedTask;
        });
        var credentials = await auth.LoginAsync(challenge =>
        {
            Assert.Equal("ABCD-1234", challenge.UserCode);
            Assert.Equal("https://github.com/login/device", challenge.VerificationUri.AbsoluteUri);
            Assert.DoesNotContain("ABCD", challenge.ToString());
        }, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(14) }, waits);
        Assert.Equal("123", credentials.AccountId);
        Assert.Null(credentials.ExpiresAt);
        Assert.Equal("{}", JsonSerializer.Serialize(credentials));
        Assert.DoesNotContain("synthetic", credentials.ToString());
        Assert.Equal(5, server.Calls);
    }

    [Theory]
    [InlineData("access_denied", ProviderFailureKind.AccessDenied)]
    [InlineData("expired_token", ProviderFailureKind.DeviceCodeExpired)]
    [InlineData("device_flow_disabled", ProviderFailureKind.DeviceLoginUnavailable)]
    [InlineData("unknown", ProviderFailureKind.InvalidResponse)]
    public async Task DeviceFailuresAreClassifiedAndSanitized(string error, ProviderFailureKind expected)
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(CodexTestServer.Json(request.RequestUri!.AbsolutePath.EndsWith("/code", StringComparison.Ordinal)
            ? Device : JsonSerializer.Serialize(new { error, error_description = "synthetic-secret" }))));
        using var http = new HttpClient(server);
        var auth = new CopilotAuthClient(http, new CodexTestServer.Clock(), (_, _) => Task.CompletedTask);
        var failure = await Assert.ThrowsAsync<CopilotException>(() => auth.LoginAsync(_ => { }, TestContext.Current.CancellationToken));
        Assert.Equal(expected, failure.Kind);
        Assert.DoesNotContain("synthetic-secret", failure.ToString());
        Assert.Equal(2, server.Calls);
    }

    [Fact]
    public async Task CancellationAndExpiryDoNotPollOrAdoptTokensAfterDeadline()
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(Device)));
        using var http = new HttpClient(server);
        var auth = new CopilotAuthClient(http, clock, (_, token) => { token.ThrowIfCancellationRequested(); clock.Current += TimeSpan.FromHours(1); return Task.CompletedTask; });
        Assert.Equal(ProviderFailureKind.DeviceCodeExpired, (await Assert.ThrowsAsync<CopilotException>(() => auth.LoginAsync(_ => { }, TestContext.Current.CancellationToken))).Kind);
        Assert.Equal(1, server.Calls);
        using var cancel = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => auth.LoginAsync(_ => cancel.Cancel(), cancel.Token));
        Assert.Equal(2, server.Calls);
    }

    [Fact]
    public async Task ForeignVerificationUriIsRejectedBeforeBrowserLaunch()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(Device.Replace("github.com/login/device", "example.com", StringComparison.Ordinal))));
        using var http = new HttpClient(server);
        var auth = new CopilotAuthClient(http);
        var opened = false;
        Assert.Equal(ProviderFailureKind.InvalidResponse, (await Assert.ThrowsAsync<CopilotException>(() => auth.LoginAsync(_ => opened = true, TestContext.Current.CancellationToken))).Kind);
        Assert.False(opened);
    }

    [Theory]
    [InlineData("{\"access_token\":\"synthetic-access\"}", true)]
    [InlineData("{\"access_token\":\"synthetic-access\",\"token_type\":\"mac\"}", false)]
    public async Task OmpOptionalTokenTypeDoesNotPermitAnExplicitIncompatibleType(string grant, bool valid)
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(CodexTestServer.Json(request.RequestUri!.AbsolutePath switch
        {
            "/login/device/code" => Device,
            "/login/oauth/access_token" => grant,
            "/user" => "{\"id\":123}",
            _ => throw new InvalidOperationException()
        })));
        using var http = new HttpClient(server);
        var auth = new CopilotAuthClient(http, new CodexTestServer.Clock(), (_, _) => Task.CompletedTask);
        if (valid) Assert.Equal("123", (await auth.LoginAsync(_ => { }, TestContext.Current.CancellationToken)).AccountId);
        else Assert.Equal(ProviderFailureKind.InvalidResponse, (await Assert.ThrowsAsync<CopilotException>(() => auth.LoginAsync(_ => { }, TestContext.Current.CancellationToken))).Kind);
        Assert.Equal(valid ? 3 : 2, server.Calls);
    }

    [Fact]
    public void QuotaRetainsNativeRequestsExplicitUnlimitedAndUnknownFutureGroups()
    {
        var parsed = CopilotQuotaParser.Parse(Encoding.UTF8.GetBytes(Usage), CodexTestServer.Clock.Now);
        var premium = parsed.Groups[0].Windows[0];
        Assert.Equal(new QuotaAmount(225, 75, 300, "requests"), premium.Amount);
        Assert.Equal(75, premium.RemainingPercent);
        Assert.Equal(new DateTimeOffset(2030, 2, 1, 0, 0, 0, TimeSpan.Zero), premium.ResetsAt);
        Assert.True(parsed.Groups[1].Windows[0].Unlimited);
        Assert.Null(parsed.Groups[1].Windows[0].RemainingPercent);
        var unknown = CopilotQuotaParser.Parse("""{"quota_snapshots":{"future/opaque":{"entitlement":10,"remaining":0},"premium_interactions":{}}}"""u8.ToArray(), CodexTestServer.Clock.Now);
        Assert.Equal("unknown", unknown.Groups[1].Windows[0].Amount!.Unit);
        Assert.Null(unknown.Groups[1].Windows[0].RemainingPercent);
        Assert.Null(unknown.Groups[0].Windows[0].Amount!.Remaining);
        Assert.Null(unknown.Groups[0].Windows[0].Unlimited);
    }

    [Fact]
    public void PremiumOrderAndOpaqueDetailsDoNotInventProviderRestrictionsOrCredits()
    {
        var parsed = CopilotQuotaParser.Parse("""{"quota_snapshots":{"chat":{},"completions":{},"premium_interactions":{"entitlement":0,"remaining":0,"percent_remaining":0,"quota_id":"opaque/Quota:ID","quota_remaining":12.5,"overage_count":2,"overage_permitted":true}}}"""u8.ToArray(), CodexTestServer.Clock.Now);
        Assert.Equal(new[] { "premium_interactions", "chat", "completions" }, parsed.Groups.Select(g => g.Id));
        var group = parsed.Groups[0];
        Assert.Null(group.Allowed);
        Assert.Null(group.LimitReached);
        Assert.Null(parsed.Credits);
        var window = group.Windows[0];
        Assert.Equal(new QuotaAmount(0, 0, 0, "requests"), window.Amount);
        Assert.Equal(new QuotaSourceDetails("opaque/Quota:ID", 12.5m, 2, true), window.SourceDetails);
        Assert.Null(parsed.Groups[1].Windows[0].SourceDetails!.OveragePermitted);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"quota_snapshots\":[]}")]
    [InlineData("{\"quota_snapshots\":{\"chat\":{\"remaining\":\"secret\"}}}")]
    [InlineData("{\"quota_snapshots\":{\"chat\":{},\"chat\":{}}}")]
    public void InvalidQuotasFailWithoutPayloadDisclosure(string json)
    {
        var error = Assert.Throws<CopilotException>(() => CopilotQuotaParser.Parse(Encoding.UTF8.GetBytes(json), CodexTestServer.Clock.Now));
        Assert.Equal(ProviderFailureKind.InvalidResponse, error.Kind);
        Assert.DoesNotContain("secret", error.ToString());
    }

    [Fact]
    public async Task QuotaRetryAfterPreventsRepeatedCallsWithoutBillingFallback()
    {
        using var server = new CodexTestServer((request, _) =>
        {
            Assert.Equal("https://api.github.com/copilot_internal/user", request.RequestUri!.AbsoluteUri);
            Assert.Equal(HttpMethod.Get, request.Method);
            var response = CodexTestServer.Json("{}", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromSeconds(60)); return Task.FromResult(response);
        });
        using var http = new HttpClient(server);
        var quota = new CopilotQuotaClient(http, new CodexTestServer.Clock());
        for (var attempt = 0; attempt < 2; attempt++)
            Assert.Equal(ProviderFailureKind.RateLimited, (await Assert.ThrowsAsync<CopilotException>(() => quota.GetQuotaAsync(new("synthetic-access", "123"), TestContext.Current.CancellationToken))).Kind);
        Assert.Equal(1, server.Calls);
    }
}
