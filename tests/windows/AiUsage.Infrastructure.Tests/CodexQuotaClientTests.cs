using AiUsage.Core.Providers.Codex;
using System.Net;
using System.Net.Http.Headers;
using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class CodexQuotaClientTests
{
    [Fact]
    public async Task FetchesSubscriptionQuotaForSelectedWorkspaceWithoutInference()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(
            request.Method == HttpMethod.Get && request.RequestUri!.AbsoluteUri == "https://chatgpt.com/backend-api/wham/usage" &&
            request.Headers.Authorization?.Parameter == "synthetic-access" &&
            request.Headers.GetValues("ChatGPT-Account-Id").Single() == "synthetic-workspace" && !request.Headers.Contains("x-openai-codex-luna-reserve")
                ? CodexTestServer.Json("""{"account_id":"synthetic-workspace","rate_limit":{"primary_window":{"used_percent":40}}}""")
                : CodexTestServer.Json("{}", HttpStatusCode.Unauthorized)));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var result = await new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken);
        Assert.Equal(60, Assert.Single(Assert.Single(result.Groups).Windows).RemainingPercent);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task UnauthorizedGrantIsNotReplayed()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("{\"error\":\"synthetic-secret\"}", HttpStatusCode.Unauthorized)));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var quota = new CodexQuotaClient(http, new CodexTestServer.Clock());
        var first = await Assert.ThrowsAsync<CodexException>(() => quota.GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        var second = await Assert.ThrowsAsync<CodexException>(() => quota.GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AuthenticationRequired, first.Kind);
        Assert.Equal(CodexFailureKind.AuthenticationRequired, second.Kind);
        Assert.True(credentials.RequiresReauthentication);
        Assert.Equal(1, server.Calls);
        Assert.DoesNotContain("synthetic-secret", first.ToString());
    }

    [Fact]
    public async Task WrongWorkspaceCannotProduceAnotherAccountsQuota()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("""{"account_id":"other-workspace","plan_type":"pro"}""")));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AccountMismatch, error.Kind);
    }

    [Fact]
    public async Task MalformedWorkspaceAttributionFailsClosed()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("""{"account_id":42,"plan_type":"pro"}""")));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.InvalidResponse, error.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MalformedOrOversizedSuccessCannotBecomeQuota(bool oversized)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(oversized ? new string('x', 1024 * 1024 + 1) : "synthetic-secret")));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.InvalidResponse, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
    }

    [Fact]
    public async Task ThrottlingPreservesRetryAfterWithoutAutomaticRetryOrLogout()
    {
        using var server = new CodexTestServer((_, _) =>
        {
            var response = CodexTestServer.Json("{}", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(CodexTestServer.Clock.Now.AddSeconds(37));
            return Task.FromResult(response);
        });
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.RateLimited, error.Kind);
        Assert.Equal(TimeSpan.FromSeconds(37), error.RetryAfter);
        Assert.False(credentials.RequiresReauthentication);
        Assert.Equal(1, server.Calls);
    }

    [Theory]
    [InlineData(403, CodexFailureKind.AccessDenied)]
    [InlineData(503, CodexFailureKind.ProviderUnavailable)]
    [InlineData(302, CodexFailureKind.RequestRejected)]
    public async Task NonSuccessIsNotParsedAsQuota(int status, CodexFailureKind expected)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("synthetic-secret", (HttpStatusCode)status)));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(expected, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
    }

    [Fact]
    public async Task ExpiredCredentialsFailBeforeNetworkAccess()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Network must not be used."));
        using var http = new HttpClient(server);
        using var credentials = new CodexCredentials("synthetic-access", "synthetic-refresh", "synthetic-workspace", CodexTestServer.Clock.Now);
        var error = await Assert.ThrowsAsync<CodexException>(() => new CodexQuotaClient(http, new CodexTestServer.Clock()).GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.AuthenticationRequired, error.Kind);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task NetworkFailureIsRedactedAndCancellationRemainsCancellation()
    {
        using var server = new CodexTestServer((_, _) => throw new HttpRequestException("synthetic-secret"));
        using var http = new HttpClient(server);
        using var credentials = CodexTestServer.Credentials();
        var quota = new CodexQuotaClient(http, new CodexTestServer.Clock());
        var error = await Assert.ThrowsAsync<CodexException>(() => quota.GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.NetworkFailure, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => quota.GetQuotaAsync(credentials, cancelled.Token));
        Assert.Equal(1, server.Calls);
    }
}
