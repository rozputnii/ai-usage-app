using System.Globalization;
using System.Net;
using System.Text.Json;
using AiUsage.Core.History;
using AiUsage.Infrastructure.Providers;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class HistoryBoundaryTests
{
    private static readonly HistoryRange Range = new(new(2029, 12, 1), new(2029, 12, 31));

    [Fact]
    public async Task CodexRequestDatesUseGregorianCalendarRegardlessOfUserCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            using var credentials = CodexTestServer.Credentials();
            using var server = new CodexTestServer((request, _) =>
            {
                if (request.RequestUri!.Query.Length > 0)
                {
                    Assert.Contains("start_date=2029-12-01", request.RequestUri.Query);
                    Assert.Contains("end_date=2029-12-31", request.RequestUri.Query);
                }
                return Task.FromResult(CodexTestServer.Json("{\"data\":[]}"));
            });
            using var http = new HttpClient(server);
            await new CodexHistoryClient(http, new CodexTestServer.Clock()).FetchAsync(credentials, Range, TestContext.Current.CancellationToken);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExcessiveRetryAfterRemainsRateLimitedInsteadOfOverflowing(bool copilot)
    {
        using var credentials = new CodexCredentials("synthetic", "synthetic", "synthetic", DateTimeOffset.MaxValue);
        using var server = new CodexTestServer((_, _) =>
        {
            var response = CodexTestServer.Json("{}", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromDays(2));
            return Task.FromResult(response);
        });
        using var http = new HttpClient(server);
        var clock = new CodexTestServer.Clock { Current = DateTimeOffset.MaxValue.AddDays(-1) };
        var result = copilot
            ? await new CopilotHistoryClient(http, clock).FetchAsync(new("synthetic", "123"), Range, TestContext.Current.CancellationToken)
            : await new CodexHistoryClient(http, clock).FetchAsync(credentials, Range, TestContext.Current.CancellationToken);
        var report = Assert.Single(result.Reports);
        Assert.Equal(HistoryStatus.RateLimited, report.Status);
        Assert.Equal(DateTimeOffset.MaxValue, report.RetryAt);
        Assert.Equal(1, server.Calls);
    }

    [Theory]
    [InlineData("activity", "\"totals\":42")]
    [InlineData("activity", "\"totals\":[]")]
    [InlineData("plugins", "\"plugin_usage_overviews\":42")]
    [InlineData("usage", "\"product_surface_usage_values\":42")]
    public void WrongRequiredReportShapeIsInvalidRatherThanEmpty(string report, string field)
    {
        using var doc = JsonDocument.Parse("{\"data\":[{\"date\":\"2029-12-02\"," + field + "}]}");
        Assert.Throws<ProviderException>(() => CodexHistoryParser.Parse(report, doc.RootElement, Range));
    }
}
