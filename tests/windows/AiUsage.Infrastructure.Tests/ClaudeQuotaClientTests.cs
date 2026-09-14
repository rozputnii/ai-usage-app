using AiUsage.Core.Providers.Claude;
using AiUsage.Infrastructure.Providers.Claude;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ClaudeQuotaClientTests
{
    [Fact]
    public async Task UsageIsAReadOnlyRequestAndRetryAfterDefersLaterCallsWithoutSleeping()
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.anthropic.com/api/oauth/usage", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer synthetic-access", request.Headers.Authorization!.ToString());
            Assert.Equal("AiUsage/0.1", request.Headers.UserAgent.ToString());
            Assert.Equal("oauth-2025-04-20", Assert.Single(request.Headers.GetValues("anthropic-beta")));
            var response = CodexTestServer.Json("{\"message\":\"synthetic-secret\"}", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return Task.FromResult(response);
        });
        using var http = new HttpClient(server);
        var client = new ClaudeQuotaClient(http, clock);
        var credentials = new ClaudeCredentials("synthetic-access", "synthetic-refresh", new("synthetic-account", "synthetic-org"), clock.GetUtcNow().AddHours(1));
        var first = await Assert.ThrowsAsync<ClaudeException>(() => client.GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(ClaudeFailureKind.RateLimited, first.Kind);
        Assert.DoesNotContain("synthetic-secret", first.ToString());
        await Assert.ThrowsAsync<ClaudeException>(() => client.GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(1, server.Calls);
        clock.Current = clock.Current.AddSeconds(31);
        await Assert.ThrowsAsync<ClaudeException>(() => client.GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(2, server.Calls);
    }

    [Theory]
    [InlineData("{\"unexpected\":\"synthetic-secret\"}")]
    [InlineData("not-json-synthetic-secret")]
    [InlineData("{\"limits\":[42]}")]
    public async Task MalformedPayloadsCannotBecomeHealthyZeroQuota(string payload)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(payload)));
        using var http = new HttpClient(server);
        var credentials = new ClaudeCredentials("synthetic-access", "synthetic-refresh", new("synthetic-account", "synthetic-org"), DateTimeOffset.MaxValue);
        var error = await Assert.ThrowsAsync<ClaudeException>(() => new ClaudeQuotaClient(http).GetQuotaAsync(credentials, TestContext.Current.CancellationToken));
        Assert.Equal(ClaudeFailureKind.InvalidResponse, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Equal(1, server.Calls);
    }
}
