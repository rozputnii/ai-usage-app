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
        request.Headers.Add("x-request-id", "identity-header-canary");
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

    [Theory]
    [InlineData("https://auth.openai.com/oauth/token", "codex", "auth")]
    [InlineData("https://auth.openai.com/api/accounts/deviceauth/usercode", "codex", "auth")]
    [InlineData("https://auth.openai.com/api/accounts/deviceauth/token", "codex", "auth")]
    [InlineData("https://chatgpt.com/backend-api/wham/usage", "codex", "quota")]
    [InlineData("https://chatgpt.com/backend-api/wham/usage/daily-token-usage-breakdown", "codex", "history")]
    [InlineData("https://chatgpt.com/backend-api/wham/usage/credit-usage-events", "codex", "history")]
    [InlineData("https://chatgpt.com/backend-api/wham/analytics/daily-workspace-usage-counts", "codex", "history")]
    [InlineData("https://chatgpt.com/backend-api/wham/analytics/daily-plugin-usage-metrics", "codex", "history")]
    [InlineData("https://chatgpt.com/backend-api/wham/analytics/daily-skill-usage-metrics", "codex", "history")]
    [InlineData("https://chatgpt.com/backend-api/wham/usage/daily-workspace-user-token-usage-breakdown", "codex", "history")]
    [InlineData("https://chatgpt.com/backend-api/wham/usage/daily-workspace-user-credit-usage", "codex", "history")]
    [InlineData("https://api.anthropic.com/v1/oauth/token", "claude", "auth")]
    [InlineData("https://api.anthropic.com/api/oauth/usage", "claude", "quota")]
    [InlineData("https://api.anthropic.com/api/claude_cli/bootstrap?entrypoint=private", "claude", "identity")]
    [InlineData("https://github.com/login/device/code", "copilot", "auth")]
    [InlineData("https://github.com/login/oauth/access_token", "copilot", "auth")]
    [InlineData("https://api.github.com/user", "copilot", "identity")]
    [InlineData("https://api.github.com/copilot_internal/user", "copilot", "quota")]
    [InlineData("https://api.github.com/users/private/settings/billing/ai_credit/usage", "copilot", "history")]
    [InlineData("https://api.github.com/users/private/settings/billing/premium_request/usage", "copilot", "history")]
    [InlineData("https://oauth2.googleapis.com/token", "antigravity", "auth")]
    [InlineData("https://www.googleapis.com/oauth2/v1/userinfo", "antigravity", "identity")]
    [InlineData("https://daily-cloudcode-pa.googleapis.com/v1internal:loadCodeAssist", "antigravity", "provisioning")]
    [InlineData("https://daily-cloudcode-pa.googleapis.com/v1internal:onboardUser", "antigravity", "provisioning")]
    [InlineData("https://daily-cloudcode-pa.googleapis.com/v1internal/operations/private", "antigravity", "provisioning")]
    [InlineData("https://daily-cloudcode-pa.googleapis.com/v1internal:retrieveUserQuotaSummary", "antigravity", "quota")]
    [InlineData("https://unknown.example/private?secret=private", "unknown", "unknown")]
    public void ExistingRoutesHaveExplicitPoliciesWithoutPrivateSegments(string url, string provider, string kind)
    {
        var policy = EndpointPolicy.Classify(new Uri(url));
        Assert.Equal(provider, policy.Provider);
        Assert.Equal(kind, policy.Kind);
        Assert.DoesNotContain("private", policy.Route);
    }

    [Fact]
    public async Task DisabledCaptureAndMalformedErrorAreNeverCalledComplete()
    {
        using var log = new FileDiagnostics(root, new DiagnosticOptions { CaptureBodies = false });
        using var client = new HttpClient(new Handler(HttpStatusCode.BadRequest, "malformed-canary"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/copilot_internal/user");
        using var response = await ProviderHttp.SendAsync(client, request, TimeProvider.System, TestContext.Current.CancellationToken, new() { Diagnostics = log });
        await log.FlushAsync();
        var artifact = File.ReadAllText(Assert.Single(Directory.GetFiles(log.DirectoryPath, "response-*.json")));
        using var parsed = JsonDocument.Parse(artifact);
        Assert.Equal("malformed", parsed.RootElement.GetProperty("completeness").GetString());
        Assert.DoesNotContain("canary", artifact);
    }

    [Theory]
    [InlineData("https://api.anthropic.com/api/oauth/usage", """{"limits":[{"kind":"session","percent":37.5,"is_active":true}],"spend":{"used":{"amount_minor":12345,"currency":"EUR","exponent":2}}}""", "12345")]
    [InlineData("https://api.github.com/copilot_internal/user", """{"quota_snapshots":{"premium_interactions":{"entitlement":300,"remaining":295,"overage_permitted":false}}}""", "295")]
    [InlineData("https://daily-cloudcode-pa.googleapis.com/v1internal:retrieveUserQuotaSummary", """{"buckets":[{"remainingAmount":"123.456","remainingFraction":0.5,"resetTime":"2026-10-03T12:00:00Z"}]}""", "123.456")]
    public void NativeQuotaFieldsRetainApprovedUnitsAndTypes(string url, string json, string nativeValue)
    {
        using var document = JsonDocument.Parse(json);
        var sanitized = ResponseSanitizer.Sanitize(document.RootElement, EndpointPolicy.Classify(new Uri(url)));
        Assert.Empty(sanitized.Redactions);
        Assert.Contains(nativeValue, sanitized.Body.GetRawText());
    }

    [Fact]
    public void KnownNumericKeysNestedInsideIdentityRemainWithheld()
    {
        using var document = JsonDocument.Parse("""{"account":{"used_percent":123456789},"future":{"used_percent":987654321}}""");
        var sanitized = ResponseSanitizer.Sanitize(document.RootElement, EndpointPolicy.Classify(new Uri("https://chatgpt.com/backend-api/wham/usage")));
        Assert.DoesNotContain("123456789", sanitized.Body.GetRawText());
        Assert.DoesNotContain("987654321", sanitized.Body.GetRawText());
    }

    [Fact]
    public async Task DisabledValidResponseKeepsMetadataWithoutBody()
    {
        using var log = new FileDiagnostics(root, new DiagnosticOptions { CaptureBodies = false });
        using var client = new HttpClient(new Handler(HttpStatusCode.OK, """{"expires_in":3600,"access_token":"canary"}"""));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.openai.com/oauth/token");
        using var response = await ProviderHttp.SendAsync(client, request, TimeProvider.System, TestContext.Current.CancellationToken, new() { Diagnostics = log });
        await log.FlushAsync();
        using var captured = JsonDocument.Parse(File.ReadAllText(Assert.Single(Directory.GetFiles(log.DirectoryPath, "response-*.json"))));
        Assert.Equal("disabled", captured.RootElement.GetProperty("completeness").GetString());
        Assert.Equal(JsonValueKind.Null, captured.RootElement.GetProperty("body").ValueKind);
        Assert.Equal(200, captured.RootElement.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("network-failure")]
    [InlineData("timeout")]
    [InlineData("cancelled")]
    [InlineData("oversized")]
    public async Task FailedAttemptsStillHaveExactlyOneTerminalRecordAndMetadata(string state)
    {
        using var log = new FileDiagnostics(root);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var client = new HttpClient(new FailedHandler(state, cancellation));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://chatgpt.com/backend-api/wham/usage?private=canary");
        var error = await Record.ExceptionAsync(async () =>
        {
            using var response = await ProviderHttp.SendAsync(client, request, TimeProvider.System, cancellation.Token, new() { Diagnostics = log });
        });
        Assert.NotNull(error);
        if (state == "cancelled") Assert.IsAssignableFrom<OperationCanceledException>(error);
        else Assert.Equal(state switch
        {
            "timeout" => TransportFailure.Timeout,
            "oversized" => TransportFailure.InvalidResponse,
            _ => TransportFailure.NetworkFailure
        }, Assert.IsType<ProviderHttpException>(error).Kind);
        Assert.True(await log.FlushAsync());
        using var artifact = JsonDocument.Parse(File.ReadAllText(Assert.Single(Directory.GetFiles(log.DirectoryPath, "response-*.json"))));
        Assert.Equal(state, artifact.RootElement.GetProperty("completeness").GetString());
        Assert.Equal(JsonValueKind.Null, artifact.RootElement.GetProperty("body").ValueKind);
        Assert.DoesNotContain("canary", artifact.RootElement.GetRawText());
        var events = Directory.GetFiles(log.DirectoryPath, "application-*.jsonl").SelectMany(p => FileDiagnosticsTests.ReadShared(p).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(s => JsonDocument.Parse(s).RootElement.Clone());
        Assert.Single(events, e => e.GetProperty("eventId").GetString() == "HttpCompleted");
    }

    private sealed class FailedHandler(string state, CancellationTokenSource cancellation) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (state == "cancelled") cancellation.Cancel();
            if (state is "cancelled" or "timeout") throw new OperationCanceledException("canary", cancellationToken);
            if (state == "network-failure") throw new HttpRequestException("canary");
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("canary") };
            response.Content.Headers.ContentLength = 2 * 1024 * 1024;
            return Task.FromResult(response);
        }
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
