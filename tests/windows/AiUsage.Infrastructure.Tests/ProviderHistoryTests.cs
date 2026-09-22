using System.Net;
using System.Text.Json;
using AiUsage.Core.History;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ProviderHistoryTests
{
    private static readonly HistoryRange Range = new(new(2029, 12, 1), new(2029, 12, 31));

    [Fact]
    public void CodexPreservesNativeUnitsSignedEventsAndAbsentValues()
    {
        using var daily = JsonDocument.Parse("""{"units":"credits","data":[{"date":"2029-12-15","product_surface_usage_values":{"future/surface":12.5},"models":[{"model":"opaque MODEL","credits":2,"text_output_tokens":34}]}]}""");
        var rows = CodexHistoryParser.Parse("usage", daily.RootElement, Range);
        Assert.Contains(rows, r => r.Value == 12.5m && r.Unit == "credits" && r.Dimensions["surface"] == "future/surface");
        Assert.Contains(rows, r => r.Metric == "text_output_tokens" && r.Value == 34 && r.Unit == "tokens" && r.Dimensions["model"] == "opaque MODEL");
        Assert.DoesNotContain(rows, r => r.Metric == "cost_usd");
        using var events = JsonDocument.Parse("""{"data":[{"date":"2029-12-16","product_surface":"cli","credit_amount":-0.004}]}""");
        Assert.Equal(-0.004m, Assert.Single(CodexHistoryParser.Parse("credits", events.RootElement, Range)).Value);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"data\":null}")]
    [InlineData("{\"data\":[{\"date\":\"bad\",\"product_surface_usage_values\":{\"cli\":1}}]}")]
    [InlineData("{\"data\":[{\"date\":\"2029-12-02\",\"product_surface_usage_values\":{\"cli\":\"bad\"}}]}")]
    public void MalformedDataIsNotAnEmptyHistory(string json)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.ThrowsAny<Exception>(() => CodexHistoryParser.Parse("usage", doc.RootElement, Range));
    }

    [Fact]
    public async Task CodexUsesExistingBearerAndDatesWithoutCookiesAndRetainsPartialSuccess()
    {
        using var credentials = CodexTestServer.Credentials();
        using var server = new CodexTestServer((request, _) =>
        {
            Assert.Equal("chatgpt.com", request.RequestUri!.Host);
            Assert.Equal("synthetic-access", request.Headers.Authorization!.Parameter);
            Assert.Equal("synthetic-workspace", Assert.Single(request.Headers.GetValues("ChatGPT-Account-Id")));
            Assert.False(request.Headers.Contains("Cookie"));
            Assert.Equal(HttpMethod.Get, request.Method);
            if (request.RequestUri.AbsolutePath.EndsWith("daily-token-usage-breakdown", StringComparison.Ordinal))
            {
                Assert.Contains("start_date=2029-12-01", request.RequestUri.Query);
                Assert.Contains("end_date=2029-12-31", request.RequestUri.Query);
                return Task.FromResult(CodexTestServer.Json("""{"units":"tokens","data":[{"date":"2029-12-02","product_surface_usage_values":{"cli":100}}]}"""));
            }
            return Task.FromResult(CodexTestServer.Json("{}", HttpStatusCode.Forbidden));
        });
        using var http = new HttpClient(server);
        var result = await new CodexHistoryClient(http, new CodexTestServer.Clock()).FetchAsync(credentials, Range, TestContext.Current.CancellationToken);
        Assert.Contains(result.Reports, r => r.Status == HistoryStatus.Available && r.Values.Count == 1);
        Assert.Contains(result.Reports, r => r.Status == HistoryStatus.AccessDenied);
        Assert.False(credentials.RequiresReauthentication);
    }

    [Fact]
    public void CopilotKeepsPeriodAggregatesAndMissingMoneyDistinct()
    {
        using var json = JsonDocument.Parse("""{"timePeriod":{"year":2029,"month":12},"user":"synthetic","usageItems":[{"model":"model/X","unitType":"requests","grossQuantity":3,"netAmount":null}]}""");
        var rows = CopilotHistoryParser.Parse(json.RootElement, Range);
        Assert.Contains(rows, r => r.Value == 3 && r.Unit == "requests" && r.Dimensions["model"] == "model/X");
        Assert.Contains(rows, r => r.Metric == "netAmount" && r.Value is null);
        Assert.All(rows, r => { Assert.Equal(Range.From, r.From); Assert.Equal(Range.To, r.To); });
    }

    [Fact]
    public async Task RateLimitStopsFanoutAndPreventsImmediateRetry()
    {
        using var credentials = CodexTestServer.Credentials();
        using var server = new CodexTestServer((_, _) =>
        {
            var response = CodexTestServer.Json("{}", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromMinutes(2));
            return Task.FromResult(response);
        });
        using var http = new HttpClient(server);
        var client = new CodexHistoryClient(http, new CodexTestServer.Clock());
        var first = await client.FetchAsync(credentials, Range, TestContext.Current.CancellationToken);
        var second = await client.FetchAsync(credentials, Range, TestContext.Current.CancellationToken);
        Assert.Equal(1, server.Calls);
        Assert.Equal(CodexTestServer.Clock.Now.AddMinutes(2), Assert.Single(first.Reports).RetryAt);
        Assert.Equal(HistoryStatus.RateLimited, Assert.Single(second.Reports).Status);
    }

    [Fact]
    public async Task CopilotValidatesNumericIdentityBeforeBillingAndUsesCalendarPeriods()
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((request, _) =>
        {
            Assert.Equal("api.github.com", request.RequestUri!.Host);
            Assert.Equal("synthetic-access", request.Headers.Authorization!.Parameter);
            if (request.RequestUri.AbsolutePath == "/user") return Task.FromResult(CodexTestServer.Json("{\"id\":123,\"login\":\"synthetic\"}"));
            Assert.StartsWith("/users/synthetic/settings/billing/", request.RequestUri.AbsolutePath);
            Assert.Equal("?year=2029&month=12", request.RequestUri.Query);
            return Task.FromResult(CodexTestServer.Json("{\"timePeriod\":{\"year\":2029,\"month\":12},\"user\":\"synthetic\",\"usageItems\":[]}"));
        });
        using var http = new HttpClient(server);
        var result = await new CopilotHistoryClient(http, clock).FetchAsync(new("synthetic-access", "123"), Range, TestContext.Current.CancellationToken);
        Assert.Equal(3, server.Calls);
        Assert.Equal(2, result.Reports.Count);
        Assert.All(result.Reports, r => Assert.Equal(HistoryStatus.Empty, r.Status));
    }

    [Fact]
    public async Task CopilotAccountMismatchDoesNotRequestAnyBillingData()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("{\"id\":999,\"login\":\"other\"}")));
        using var http = new HttpClient(server);
        var result = await new CopilotHistoryClient(http).FetchAsync(new("synthetic", "123"), Range, TestContext.Current.CancellationToken);
        Assert.Equal(1, server.Calls);
        Assert.Equal(HistoryStatus.Failed, Assert.Single(result.Reports).Status);
    }

    [Fact]
    public void CopilotPartialMonthsUseOnlyRequestedDays()
    {
        var range = new HistoryRange(new(2028, 1, 31), new(2028, 3, 2));
        Assert.Equal([new(new(2028, 1, 31), new(2028, 1, 31)), new(new(2028, 2, 1), new(2028, 2, 29)),
            new(new(2028, 3, 1), new(2028, 3, 1)), new(new(2028, 3, 2), new(2028, 3, 2))], CopilotHistoryClient.Periods(range));
    }

    [Fact]
    public void SuccessfulUnknownSchemaMustNotBeReportedAsEmpty()
    {
        using var json = JsonDocument.Parse("{\"data\":[{\"date\":\"2029-12-02\",\"unexpected\":45}]}");
        Assert.ThrowsAny<Exception>(() => CodexHistoryParser.Parse("usage", json.RootElement, Range));
    }
}
