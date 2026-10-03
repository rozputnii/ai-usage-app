using System.Net;
using System.Text.Json;
using AiUsage.Infrastructure.Diagnostics;
using AiUsage.Infrastructure.Providers;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ProviderCaptureTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-capture-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task CapturePreservesApprovedNumbersBeforeProjectionAndWithholdsUnknownValues(HttpStatusCode status)
    {
        const string body = """{"rate_limit":{"primary_window":{"used_percent":12.123456789012345678901234567890,"reset_at":null}},"access_token":"token-canary","future":{"nested":["value-canary",null,123],"identity-canary@example.com":true},"123456789":"identity-canary"}""";
        using var log = new FileDiagnostics(root);
        using var handler = new Handler(status, body);
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://chatgpt.com/backend-api/wham/usage?private=query-canary");
        request.Headers.Add("Authorization", "Bearer header-canary");
        using var response = await ProviderHttp.SendAsync(client, request, TimeProvider.System, TestContext.Current.CancellationToken, new() { Diagnostics = log });
        Assert.Equal(status, response.StatusCode);
        Assert.Contains("value-canary", response.Body!.RootElement.GetRawText());
        await log.FlushAsync();
        Assert.Equal(1, handler.Count);
        var artifact = File.ReadAllText(Assert.Single(Directory.GetFiles(log.DirectoryPath, "response-*.json")));
        Assert.Contains("12.123456789012345678901234567890", artifact);
        Assert.DoesNotContain("canary", artifact);
        Assert.DoesNotContain("123456789\"", artifact);
        using var parsed = JsonDocument.Parse(artifact);
        Assert.Equal("withheld-values", parsed.RootElement.GetProperty("completeness").GetString());
        Assert.Equal(JsonValueKind.Null, parsed.RootElement.GetProperty("body").GetProperty("rate_limit").GetProperty("primary_window").GetProperty("reset_at").ValueKind);
        Assert.NotEmpty(parsed.RootElement.GetProperty("redactions").EnumerateArray());
    }

    [Theory]
    [InlineData("<html>malformed-canary</html>", "malformed")]
    [InlineData("", "empty")]
    public async Task MalformedSuccessStillFailsAndCapturesOnlyMetadata(string body, string completeness)
    {
        using var log = new FileDiagnostics(root);
        using var client = new HttpClient(new Handler(HttpStatusCode.OK, body));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/api/oauth/usage");
        await Assert.ThrowsAsync<ProviderHttpException>(() => ProviderHttp.SendAsync(client, request, TimeProvider.System, TestContext.Current.CancellationToken, new() { Diagnostics = log }));
        await log.FlushAsync();
        var artifact = File.ReadAllText(Assert.Single(Directory.GetFiles(log.DirectoryPath, "response-*.json")));
        Assert.DoesNotContain("canary", artifact);
        using var parsed = JsonDocument.Parse(artifact);
        Assert.Equal(completeness, parsed.RootElement.GetProperty("completeness").GetString());
    }

    [Fact]
    public void AuthPolicyAndDuplicatePropertiesRemainExplicit()
    {
        using var body = JsonDocument.Parse("""{"expires_in":3600,"token_type":"Bearer","access_token":"canary","expires_in":3601,"account":{"email":"canary"}}""");
        var capture = ResponseSanitizer.Sanitize(body.RootElement, EndpointPolicy.Classify(new Uri("https://auth.openai.com/oauth/token")));
        Assert.DoesNotContain("canary", capture.Body.GetRawText());
        Assert.Equal(2, capture.Body.EnumerateObject().Count(p => p.Name == "expires_in"));
        Assert.True(capture.DuplicateProperties);
        Assert.Equal("Bearer", capture.Body.GetProperty("token_type").GetString());
    }

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Count { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
        }
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
