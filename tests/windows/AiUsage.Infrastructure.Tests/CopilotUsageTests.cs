using AiUsage.Core.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Copilot;
using System.Net;
using System.Text;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class CopilotUsageTests
{
    internal const string Report = """
        {"timePeriod":{"year":2030,"month":1},"user":"synthetic-user","usageItems":[
          {"product":"copilot","sku":"copilot_ai_credit","model":"synthetic-model-a","unitType":"ai-credits","pricePerUnit":0.01,"grossQuantity":120.5,"grossAmount":1.205,"discountQuantity":100.25,"discountAmount":1.0025,"netQuantity":20.25,"netAmount":0.2025},
          {"product":"copilot","sku":"copilot_ai_credit","model":"","unitType":"ai-credits","pricePerUnit":0.01,"grossQuantity":0,"grossAmount":0,"discountQuantity":0,"discountAmount":0,"netQuantity":0,"netAmount":0}
        ]}
        """;

    [Fact]
    public void DocumentedReportKeepsProviderUnitsAndOpaqueNames()
    {
        Assert.True(CopilotUsageParser.TryParse(Encoding.UTF8.GetBytes(Report), CopilotUsageReportKind.AiCredits, CodexTestServer.Clock.Now, out var report, out var user));
        Assert.Equal("synthetic-user", user);
        Assert.Equal((2030, 1, (int?)null), (report!.Year, report.Month!.Value, report.Day));
        Assert.Equal(2, report.Items.Count);
        var first = report.Items[0];
        Assert.Equal(("copilot_ai_credit", "synthetic-model-a", "ai-credits"), (first.Sku, first.Model, first.UnitType));
        Assert.Equal((120.5m, 100.25m, 20.25m, 0.2025m), (first.GrossQuantity, first.DiscountQuantity, first.NetQuantity, first.NetAmount));
        Assert.Null(report.Items[1].Model);
        Assert.Equal(CodexTestServer.Clock.Now, report.ObservedAt);
    }

    [Theory]
    [InlineData("{\"timePeriod\":{\"year\":2030},\"user\":\"u\",\"usageItems\":[]}", true)]
    [InlineData("{\"timePeriod\":{\"year\":2030},\"usageItems\":[]}", false)]
    [InlineData("{\"timePeriod\":{\"year\":2030,\"month\":13},\"user\":\"u\",\"usageItems\":[]}", false)]
    [InlineData("{\"timePeriod\":{\"year\":2030},\"user\":\"u\"}", false)]
    [InlineData("{\"timePeriod\":{\"year\":2030},\"user\":\"u\",\"usageItems\":[{\"product\":\"p\",\"sku\":\"s\",\"unitType\":\"u\",\"pricePerUnit\":1,\"grossQuantity\":\"12\",\"grossAmount\":0,\"discountQuantity\":0,\"discountAmount\":0,\"netQuantity\":0,\"netAmount\":0}]}", false)]
    [InlineData("{\"timePeriod\":{\"year\":2030},\"user\":\"u\",\"usageItems\":[{\"product\":\"p\",\"sku\":\"s\",\"unitType\":\"u\",\"pricePerUnit\":1,\"grossQuantity\":-1,\"grossAmount\":0,\"discountQuantity\":0,\"discountAmount\":0,\"netQuantity\":0,\"netAmount\":0}]}", false)]
    [InlineData("{\"timePeriod\":{\"year\":2030},\"user\":\"u\",\"usageItems\":[{\"sku\":\"s\",\"unitType\":\"u\",\"pricePerUnit\":1,\"grossQuantity\":1,\"grossAmount\":0,\"discountQuantity\":0,\"discountAmount\":0,\"netQuantity\":0,\"netAmount\":0}]}", false)]
    [InlineData("not json", false)]
    public void MissingOrInvalidValuesAreASchemaFailureNotZero(string json, bool expected) =>
        Assert.Equal(expected, CopilotUsageParser.TryParse(Encoding.UTF8.GetBytes(json), CopilotUsageReportKind.AiCredits, CodexTestServer.Clock.Now, out _, out _));

    [Fact]
    public async Task UsageRequestsAreDocumentedReadOnlyReportsForTheBoundLogin()
    {
        var urls = new List<string>();
        using var server = new CodexTestServer((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("Bearer synthetic-token", request.Headers.Authorization!.ToString());
            Assert.Equal("2022-11-28", Assert.Single(request.Headers.GetValues("X-GitHub-Api-Version")));
            Assert.Contains("application/vnd.github+json", request.Headers.Accept.ToString());
            urls.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(CodexTestServer.Json(Report));
        });
        using var http = new HttpClient(server);
        var client = new CopilotUsageClient(http);
        var credentials = new CopilotCredentials("synthetic-token", new(42, "synthetic-user"), "read:user");
        await client.GetUsageAsync(credentials, CopilotUsageReportKind.AiCredits, TestContext.Current.CancellationToken);
        var legacy = await client.GetUsageAsync(credentials, CopilotUsageReportKind.PremiumRequests, TestContext.Current.CancellationToken);
        Assert.Equal(CopilotUsageReportKind.PremiumRequests, legacy.Kind);
        Assert.Equal([
            "https://api.github.com/users/synthetic-user/settings/billing/ai_credit/usage",
            "https://api.github.com/users/synthetic-user/settings/billing/premium_request/usage"], urls);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, CopilotFailureKind.ReportUnavailable)]
    [InlineData(HttpStatusCode.Forbidden, CopilotFailureKind.AccessDenied)]
    [InlineData(HttpStatusCode.Unauthorized, CopilotFailureKind.AuthenticationRequired)]
    [InlineData(HttpStatusCode.OK, CopilotFailureKind.AccountMismatch)]
    public async Task RejectedOrForeignReportsNeverBecomeUsage(HttpStatusCode status, CopilotFailureKind expected)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(status == HttpStatusCode.OK
            ? CodexTestServer.Json(Report.Replace("synthetic-user", "someone-else", StringComparison.Ordinal))
            : CodexTestServer.Json("{\"message\":\"synthetic-secret\"}", status)));
        using var http = new HttpClient(server);
        var credentials = new CopilotCredentials("synthetic-token", new(42, "synthetic-user"), null);
        var error = await Assert.ThrowsAsync<CopilotException>(() =>
            new CopilotUsageClient(http).GetUsageAsync(credentials, CopilotUsageReportKind.AiCredits, TestContext.Current.CancellationToken));
        Assert.Equal(expected, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
    }
}
