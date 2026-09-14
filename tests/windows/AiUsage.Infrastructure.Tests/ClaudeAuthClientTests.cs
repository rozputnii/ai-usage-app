using AiUsage.Core.Providers.Claude;
using AiUsage.Infrastructure.Providers.Claude;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class ClaudeAuthClientTests
{
    internal static string Tokens(string account = "synthetic-account", string organization = "synthetic-organization") =>
        JsonSerializer.Serialize(new { access_token = "synthetic-access", refresh_token = "synthetic-refresh", expires_in = 3600,
            account = new { uuid = account }, organization = new { uuid = organization } });

    [Fact]
    public async Task BrowserFlowBindsPkceStateAndIdentityWithoutImpersonatingClaudeCode()
    {
        string? body = null;
        using var server = new CodexTestServer(async (request, token) =>
        {
            Assert.Equal("https://api.anthropic.com/v1/oauth/token", request.RequestUri!.AbsoluteUri);
            Assert.Equal("AiUsage/0.1", request.Headers.UserAgent.ToString());
            Assert.Null(request.Headers.Authorization);
            body = await request.Content!.ReadAsStringAsync(token);
            return CodexTestServer.Json(Tokens());
        });
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http, new CodexTestServer.Clock());
        using var attempt = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query);
        Assert.Equal("claude.ai", attempt.AuthorizationUrl.Host);
        Assert.Equal("9d1c250a-e61b-44d9-88ed-5944d1962f5e", query["client_id"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Contains("user:profile", query["scope"]);
        var login = auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken);
        using var callback = new HttpClient();
        var page = await callback.GetStringAsync(query["redirect_uri"] + "?code=synthetic-code&state=" + query["state"], TestContext.Current.CancellationToken);
        var credentials = await login;
        Assert.Equal(new ClaudeIdentity("synthetic-account", "synthetic-organization"), credentials.Identity);
        Assert.Equal(CodexTestServer.Clock.Now.AddMinutes(55), credentials.RefreshAt);
        using var sent = JsonDocument.Parse(body!);
        Assert.Equal(query["state"], sent.RootElement.GetProperty("state").GetString());
        Assert.Equal(query["redirect_uri"], sent.RootElement.GetProperty("redirect_uri").GetString());
        var verifier = sent.RootElement.GetProperty("code_verifier").GetString()!;
        Assert.Equal(query["code_challenge"], Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        Assert.DoesNotContain("synthetic", page);
        Assert.DoesNotContain("synthetic", credentials.ToString());
        Assert.Equal("{}", JsonSerializer.Serialize(credentials));
        await Assert.ThrowsAsync<ClaudeException>(() => auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken));
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task ForeignCallbackIsIgnoredAndRefusalIsSanitized()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("No exchange expected."));
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http);
        using var attempt = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken);
        using var callback = new HttpClient();
        using var rejected = await callback.GetAsync(query["redirect_uri"] + "?code=synthetic-code&state=foreign", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.False(login.IsCompleted);
        await callback.GetStringAsync(query["redirect_uri"] + "?error=synthetic-secret&state=" + query["state"], TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<ClaudeException>(() => login);
        Assert.Equal(ClaudeFailureKind.AccessDenied, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Equal(0, server.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitManualInputWorksAndRejectsSuppliedForeignState(bool rawCode)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(Tokens())));
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http);
        using var attempt = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken);
        Assert.False(attempt.TrySubmitCode("synthetic-code#foreign"));
        Assert.False(attempt.TrySubmitCode("https://unrelated.invalid/?code=synthetic-code&state=" + query["state"]));
        Assert.True(attempt.TrySubmitCode(rawCode ? "synthetic-code" : query["redirect_uri"] + "?code=synthetic-code&state=" + query["state"]));
        Assert.Equal("synthetic-account", (await login).Identity.AccountId);
        Assert.False(attempt.TrySubmitCode("synthetic-code"));
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task CancellationClosesAnIncompleteCallbackAndNeverExchanges()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException());
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http);
        using var attempt = auth.BeginBrowserLogin();
        using var cancel = new CancellationTokenSource();
        var login = auth.CompleteBrowserLoginAsync(attempt, cancel.Token);
        var redirect = new Uri(HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query)["redirect_uri"]!);
        using var socket = new System.Net.Sockets.TcpClient();
        await socket.ConnectAsync(IPAddress.Loopback, redirect.Port, TestContext.Current.CancellationToken);
        await socket.GetStream().WriteAsync("GET /callback HTTP/1.1\r\n"u8.ToArray(), TestContext.Current.CancellationToken);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => login);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task StalledBrowserHeadersDoNotBlockTheNextValidCallback()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(Tokens())));
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http);
        using var attempt = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancel.CancelAfter(TimeSpan.FromSeconds(9));
        var login = auth.CompleteBrowserLoginAsync(attempt, cancel.Token);
        var redirect = new Uri(query["redirect_uri"]!);
        using var stalled = new System.Net.Sockets.TcpClient();
        await stalled.ConnectAsync(IPAddress.Loopback, redirect.Port, cancel.Token);
        await stalled.GetStream().WriteAsync("GET /callback HTTP/1.1\r\n"u8.ToArray(), cancel.Token);
        using var callback = new HttpClient();
        await callback.GetStringAsync(redirect + "?code=synthetic-code&state=" + query["state"], cancel.Token);
        Assert.Equal("synthetic-account", (await login).Identity.AccountId);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task MissingIdentityUsesOnlyTheEvidencedBootstrapAndRejectsConflicts()
    {
        using var server = new CodexTestServer((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
                return Task.FromResult(CodexTestServer.Json("{\"access_token\":\"synthetic-access\",\"refresh_token\":\"synthetic-refresh\",\"expires_in\":3600,\"account\":{\"uuid\":\"synthetic-account\"}}"));
            Assert.Equal("/api/claude_cli/bootstrap", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer synthetic-access", request.Headers.Authorization!.ToString());
            return Task.FromResult(CodexTestServer.Json("{\"oauth_account\":{\"account_uuid\":\"other-account\",\"organization_uuid\":\"synthetic-organization\"}}"));
        });
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http);
        using var attempt = auth.BeginBrowserLogin();
        Assert.True(attempt.TrySubmitCode("synthetic-code"));
        var error = await Assert.ThrowsAsync<ClaudeException>(() => auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken));
        Assert.Equal(ClaudeFailureKind.AccountMismatch, error.Kind);
        Assert.Equal(2, server.Calls);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"access_token\":\"synthetic-access\",\"refresh_token\":\"synthetic-refresh\",\"expires_in\":-1}")]
    [InlineData("{\"access_token\":\"synthetic-access\\r\\nsecret\",\"refresh_token\":\"synthetic-refresh\",\"expires_in\":3600}")]
    [InlineData("{\"access_token\":\"synthetic-access\",\"refresh_token\":\"synthetic-refresh\",\"expires_in\":3600,\"scope\":\"user:inference\"}")]
    public async Task InvalidTokenResponsesNeverPromoteCredentialsOrEchoValues(string payload)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(payload)));
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http);
        using var attempt = auth.BeginBrowserLogin();
        Assert.True(attempt.TrySubmitCode("synthetic-code"));
        var error = await Assert.ThrowsAsync<ClaudeException>(() => auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken));
        Assert.Equal(ClaudeFailureKind.InvalidResponse, error.Kind);
        Assert.DoesNotContain("synthetic", error.ToString());
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task RefreshRetainsOmittedBindingAndRefreshTokenButRejectsAnOrganizationSwitch()
    {
        var responses = new Queue<string>(["{\"access_token\":\"synthetic-next\",\"expires_in\":3600}", Tokens(organization: "other-organization")]);
        using var server = new CodexTestServer((request, _) =>
        {
            Assert.Equal("oauth-2025-04-20", Assert.Single(request.Headers.GetValues("anthropic-beta")));
            return Task.FromResult(CodexTestServer.Json(responses.Dequeue()));
        });
        using var http = new HttpClient(server);
        var auth = new ClaudeAuthClient(http);
        var old = new ClaudeCredentials("synthetic-access", "synthetic-refresh", new("synthetic-account", "synthetic-organization"), DateTimeOffset.MinValue);
        var next = await auth.RefreshAsync(old, TestContext.Current.CancellationToken);
        Assert.Equal(old.Identity, next.Identity);
        Assert.Equal(old.RefreshToken, next.RefreshToken);
        var error = await Assert.ThrowsAsync<ClaudeException>(() => auth.RefreshAsync(next, TestContext.Current.CancellationToken));
        Assert.Equal(ClaudeFailureKind.AccountMismatch, error.Kind);
        Assert.Equal(2, server.Calls);
    }

    [Fact]
    public async Task AmbiguousRefreshIsNeverReplayedAndErrorsDoNotContainProviderBodies()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json("{\"error\":\"invalid_grant\",\"message\":\"synthetic-refresh\"}", HttpStatusCode.BadRequest)));
        using var http = new HttpClient(server);
        var old = new ClaudeCredentials("synthetic-access", "synthetic-refresh", new("synthetic-account", "synthetic-organization"), DateTimeOffset.MinValue);
        var error = await Assert.ThrowsAsync<ClaudeException>(() => new ClaudeAuthClient(http).RefreshAsync(old, TestContext.Current.CancellationToken));
        Assert.Equal(ClaudeFailureKind.AuthenticationRequired, error.Kind);
        Assert.DoesNotContain("synthetic", error.ToString());
        Assert.Equal(1, server.Calls);
    }
}
