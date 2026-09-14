using System.Net;
using System.Text.Json;
using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class CodexAuthClientTests
{
    [Fact]
    public async Task DeviceFlowWaitsForConsentThenUsesOneTimeCodeAndQuota()
    {
        var polls = 0;
        using var server = new CodexTestServer(async (request, cancellation) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellation);
            if (path.EndsWith("/usercode", StringComparison.Ordinal))
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Contains("app_EMoamEEZ73f0CkXaXp7hrann", body);
                return CodexTestServer.Json("""{"device_auth_id":"synthetic-device-secret","usercode":"SYNTHETIC-CODE","interval":"1"}""");
            }
            if (path.EndsWith("/deviceauth/token", StringComparison.Ordinal))
            {
                Assert.Contains("synthetic-device-secret", body);
                return ++polls == 1 ? CodexTestServer.Json("{}", HttpStatusCode.Forbidden)
                    : CodexTestServer.Json("""{"authorization_code":"synthetic-one-time-code","code_verifier":"synthetic-verifier"}""");
            }
            if (path.EndsWith("/oauth/token", StringComparison.Ordinal))
            {
                Assert.Equal("application/x-www-form-urlencoded", request.Content!.Headers.ContentType!.MediaType);
                Assert.Contains("grant_type=authorization_code", body);
                Assert.Contains("code_verifier=synthetic-verifier", body);
                return CodexTestServer.Json(CodexTestServer.Tokens());
            }
            return request.Method == HttpMethod.Get && request.Headers.Authorization?.Parameter == CodexTestServer.AccessToken()
                ? CodexTestServer.Json("""{"plan_type":"synthetic","rate_limit":{"primary_window":{"used_percent":15}}}""")
                : CodexTestServer.Json("{}", HttpStatusCode.Unauthorized);
        });
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        var device = await auth.BeginDeviceLoginAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new Uri("https://auth.openai.com/codex/device"), device.VerificationUri);
        using var credentials = await auth.CompleteDeviceLoginAsync(device, TestContext.Current.CancellationToken);
        var quota = await new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken);
        Assert.Equal(85, Assert.Single(Assert.Single(quota.Groups).Windows).RemainingPercent);
        var calls = server.Calls;
        await Assert.ThrowsAsync<CodexException>(() => auth.CompleteDeviceLoginAsync(device, TestContext.Current.CancellationToken));
        Assert.Equal(calls, server.Calls);
        Assert.DoesNotContain("synthetic-device-secret", device.ToString());
        Assert.DoesNotContain("SYNTHETIC-CODE", JsonSerializer.Serialize(device));
        Assert.Equal("{}", JsonSerializer.Serialize(credentials));
    }

    [Fact]
    public async Task ExpiredDeviceCodeDoesNotPoll()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Must not poll."));
        using var http = new HttpClient(server);
        var device = new CodexDeviceAuthorization("synthetic", "synthetic", TimeSpan.FromSeconds(1), CodexTestServer.Clock.Now);
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexAuthClient(http, new CodexTestServer.Clock()).CompleteDeviceLoginAsync(device, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.DeviceCodeExpired, error.Kind);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task DeviceDeadlineCancelsAnInFlightPoll()
    {
        using var server = new CodexTestServer(async (_, cancellation) =>
        {
            await Task.Delay(Timeout.Infinite, cancellation);
            return CodexTestServer.Json("{}");
        });
        using var http = new HttpClient(server);
        var device = new CodexDeviceAuthorization("synthetic", "synthetic", TimeSpan.Zero, DateTimeOffset.UtcNow.AddSeconds(1));
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexAuthClient(http).CompleteDeviceLoginAsync(device, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.DeviceCodeExpired, error.Kind);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task LateConsentResponseCannotStartTokenExchange()
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((_, _) =>
        {
            clock.Current = CodexTestServer.Clock.Now.AddMinutes(16);
            return Task.FromResult(CodexTestServer.Json("""{"authorization_code":"synthetic","code_verifier":"synthetic"}"""));
        });
        using var http = new HttpClient(server);
        var device = new CodexDeviceAuthorization("synthetic", "synthetic", TimeSpan.Zero, CodexTestServer.Clock.Now.AddMinutes(15));
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexAuthClient(http, clock).CompleteDeviceLoginAsync(device, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.DeviceCodeExpired, error.Kind);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task DeviceUnavailableIsNotTreatedAsPending()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("{}", HttpStatusCode.NotFound)));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexAuthClient(http).BeginDeviceLoginAsync(TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.DeviceLoginUnavailable, error.Kind);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task ConcurrentRefreshesConsumeOneGrantAndUseItsReplacement()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshes = 0;
        using var server = new CodexTestServer(async (request, cancellation) =>
        {
            if (request.Method == HttpMethod.Get)
                return request.Headers.Authorization?.Parameter == CodexTestServer.AccessToken()
                    ? CodexTestServer.Json("""{"plan_type":"synthetic"}""") : CodexTestServer.Json("{}", HttpStatusCode.Unauthorized);
            var body = await request.Content!.ReadAsStringAsync(cancellation);
            if (++refreshes == 1)
            {
                Assert.Contains("synthetic-refresh", body);
                entered.SetResult();
                await release.Task.WaitAsync(cancellation);
            }
            else
                Assert.Contains("synthetic-rotated", body);
            return CodexTestServer.Json(CodexTestServer.Tokens());
        });
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        var first = auth.RefreshAsync(credentials, TestContext.Current.CancellationToken);
        await entered.Task;
        var second = auth.RefreshAsync(credentials, TestContext.Current.CancellationToken);
        release.SetResult();
        await Task.WhenAll(first, second);
        Assert.Equal(1, refreshes);
        var quota = await new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken);
        Assert.Equal("synthetic", quota.PlanType);
        await auth.RefreshAsync(credentials, TestContext.Current.CancellationToken);
        Assert.Equal(2, refreshes);
    }

    [Fact]
    public async Task OmittedRefreshReplacementRetainsExistingGrant()
    {
        using var server = new CodexTestServer(async (request, cancellation) =>
        {
            Assert.Contains("synthetic-refresh", await request.Content!.ReadAsStringAsync(cancellation));
            return CodexTestServer.Json(CodexTestServer.Tokens(refresh: null));
        });
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        await auth.RefreshAsync(credentials, TestContext.Current.CancellationToken);
        await auth.RefreshAsync(credentials, TestContext.Current.CancellationToken);
        Assert.Equal(2, server.Calls);
        Assert.False(credentials.RequiresReauthentication);
    }

    [Theory]
    [InlineData("{\"error\":\"invalid_grant\",\"error_description\":\"synthetic-secret\"}")]
    [InlineData("{\"error\":{\"code\":\"refresh_token_reused\",\"message\":\"synthetic-secret\"}}")]
    public async Task InvalidGrantRequiresNewLoginWithoutRetry(string body)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(body, HttpStatusCode.BadRequest)));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        var error = await Assert.ThrowsAsync<CodexException>(() => auth.RefreshAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AuthenticationRequired, error.Kind);
        await Assert.ThrowsAsync<CodexException>(() => auth.RefreshAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(1, server.Calls);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
    }

    [Fact]
    public async Task AmbiguousRefreshFailureCannotReplayOldGrant()
    {
        using var server = new CodexTestServer((_, _) => throw new HttpRequestException("synthetic-secret"));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        var error = await Assert.ThrowsAsync<CodexException>(() => auth.RefreshAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.NetworkFailure, error.Kind);
        var retry = await Assert.ThrowsAsync<CodexException>(() => auth.RefreshAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AuthenticationRequired, retry.Kind);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task CancellationAfterSendingRefreshRequiresReauthentication()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new CodexTestServer(async (_, cancellation) =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, cancellation);
            return CodexTestServer.Json("{}");
        });
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        using var cancel = new CancellationTokenSource();
        var refresh = new CodexAuthClient(http, new CodexTestServer.Clock()).RefreshAsync(credentials, cancel.Token);
        await entered.Task;
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
        Assert.True(credentials.RequiresReauthentication);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task CancellationBeforeSendingDoesNotInvalidateGoodCredentials()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Must not send."));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CodexAuthClient(http).RefreshAsync(credentials, cancel.Token));
        Assert.False(credentials.RequiresReauthentication);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task RefreshCannotSilentlyChangeWorkspace()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens("other-workspace"))));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexAuthClient(http, new CodexTestServer.Clock()).RefreshAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AccountMismatch, error.Kind);
        Assert.True(credentials.RequiresReauthentication);
        Assert.Equal("synthetic-workspace", credentials.AccountId);
    }

    [Theory]
    [InlineData(true, "synthetic-region")]
    [InlineData(false, null)]
    public async Task ProviderIssuedAccountContextIsNeverRefusedLocally(bool fedRamp, string? residency)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens(residency: residency, fedRamp: fedRamp))));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        await new CodexAuthClient(http, new CodexTestServer.Clock()).RefreshAsync(credentials, TestContext.Current.CancellationToken);
        Assert.False(credentials.RequiresReauthentication);
    }

    [Fact]
    public async Task QuotaRequestsCarryOnlyTheInspectedProviderHeaders()
    {
        var headers = new List<string>();
        using var server = new CodexTestServer((request, _) =>
        {
            headers.AddRange(request.Headers.Select(header => header.Key));
            return Task.FromResult(CodexTestServer.Json("""{"plan_type":"synthetic"}"""));
        });
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        await new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken);
        Assert.Equal(["Accept", "Authorization", "ChatGPT-Account-Id", "User-Agent"], headers.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase));
    }
}
