using System.Net;
using System.Web;
using AiUsage.Infrastructure.Providers.Codex;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class CodexBrowserLoginTests
{
    [Fact]
    public async Task ApprovedRedirectExchangesTheCodeBoundToThisAttempt()
    {
        string? sentBody = null;
        using var server = new CodexTestServer(async (request, cancellation) =>
        {
            sentBody = await request.Content!.ReadAsStringAsync(cancellation);
            return CodexTestServer.Json(CodexTestServer.Tokens());
        });
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal("app_EMoamEEZ73f0CkXaXp7hrann", query["client_id"]);
        var redirectUri = query["redirect_uri"]!;
        var login = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);
        var page = await CallbackAsync(redirectUri, "code=synthetic-authorization-code&state=" + Uri.EscapeDataString(query["state"]!));
        using var credentials = await login;

        Assert.Equal("synthetic-workspace", credentials.AccountId);
        Assert.Contains("grant_type=authorization_code", sentBody);
        Assert.Contains("code=synthetic-authorization-code", sentBody);
        Assert.Contains("code_verifier=", sentBody);
        Assert.DoesNotContain("synthetic-authorization-code", page);
        Assert.DoesNotContain(CodexTestServer.AccessToken(), page);

        await Assert.ThrowsAsync<CodexException>(() => auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken));
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task ForeignStateNeverReachesTheTokenEndpoint()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens())));
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        var redirectUri = query["redirect_uri"]!;
        var login = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);

        Assert.Contains("state did not match", await CallbackAsync(redirectUri, "code=attacker-code&state=attacker-state"));
        Assert.Equal(0, server.Calls);

        await CallbackAsync(redirectUri, "code=synthetic-code&state=" + Uri.EscapeDataString(query["state"]!));
        using var credentials = await login;
        Assert.False(credentials.RequiresReauthentication);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task RefusedConsentIsNotAnUnknownFailure()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Token endpoint must not be called."));
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);
        await CallbackAsync(query["redirect_uri"]!, "error=access_denied&state=" + Uri.EscapeDataString(query["state"]!));
        var error = await Assert.ThrowsAsync<CodexException>(() => login);
        Assert.Equal(CodexFailureKind.AccessDenied, error.Kind);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task CancellationStopsWaitingWithoutTokenRequest()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Token endpoint must not be called."));
        using var http = new HttpClient(server);
        using var cancel = new CancellationTokenSource();
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var login = auth.CompleteBrowserLoginAsync(authorization, cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => login);
        Assert.Equal(0, server.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CredentialsAreNotIssuedAfterTheAttemptDeadline(bool lateDuringExchange)
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((_, _) =>
        {
            clock.Current = CodexTestServer.Clock.Now.AddMinutes(16);
            return Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens()));
        });
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, clock);
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);
        if (!lateDuringExchange)
            clock.Current = CodexTestServer.Clock.Now.AddMinutes(16);
        await CallbackAsync(query["redirect_uri"]!, "code=synthetic-code&state=" + Uri.EscapeDataString(query["state"]!));
        var error = await Assert.ThrowsAsync<CodexException>(() => login);
        Assert.Equal(CodexFailureKind.LoginAttemptExpired, error.Kind);
        Assert.Equal(lateDuringExchange ? 1 : 0, server.Calls);
    }

    [Fact]
    public async Task RefusalReportsTheProviderCodeWithoutItsDescription()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Token endpoint must not be called."));
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);
        await CallbackAsync(query["redirect_uri"]!, "error=access_denied&error_description=" + Uri.EscapeDataString("synthetic-secret detail") + "&state=" + Uri.EscapeDataString(query["state"]!));
        var error = await Assert.ThrowsAsync<CodexException>(() => login);
        Assert.Equal("access_denied", error.ProviderErrorCode);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
    }
    [Fact]
    public async Task OpaqueRefusalValueIsReducedToItsShape()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Token endpoint must not be called."));
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);
        await CallbackAsync(query["redirect_uri"]!, "error=sk-synthetic-opaque-identifier-1234&state=" + Uri.EscapeDataString(query["state"]!));
        var error = await Assert.ThrowsAsync<CodexException>(() => login);
        Assert.Null(error.ProviderErrorCode);
        Assert.Contains("characters", error.UnrecognizedProviderError);
        Assert.DoesNotContain("synthetic-opaque-identifier", error.UnrecognizedProviderError);
        Assert.DoesNotContain("synthetic-opaque-identifier", error.ToString());
    }

    [Fact]
    public async Task AnAttemptThatIsNeverApprovedExpiresAsALoginAttempt()
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("Token endpoint must not be called."));
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, clock);
        using var authorization = auth.BeginBrowserLogin();
        clock.Current = authorization.ExpiresAt - TimeSpan.FromMilliseconds(250);
        var error = await Assert.ThrowsAsync<CodexException>(() => auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken));
        Assert.Equal(CodexFailureKind.LoginAttemptExpired, error.Kind);
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task BothLoopbackFamiliesReachTheWaitingAttempt()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens())));
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        var port = new Uri(query["redirect_uri"]!).Port;
        var login = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);
        Assert.Contains("state did not match", await CallbackAsync($"http://[::1]:{port}/auth/callback", "code=x&state=other"));
        await CallbackAsync($"http://127.0.0.1:{port}/auth/callback", "code=synthetic-code&state=" + Uri.EscapeDataString(query["state"]!));
        using var credentials = await login;
        Assert.Equal("synthetic-workspace", credentials.AccountId);
    }

    [Fact]
    public async Task ACancelledWaitDoesNotBreakARetryOnTheSameAttempt()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(CodexTestServer.Tokens())));
        using var http = new HttpClient(server);
        var auth = new CodexAuthClient(http, new CodexTestServer.Clock());
        using var authorization = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        var port = new Uri(query["redirect_uri"]!).Port;
        using var cancel = new CancellationTokenSource();
        var abandoned = auth.CompleteBrowserLoginAsync(authorization, cancel.Token);
        // A handled callback proves the attempt is already waiting on both families.
        Assert.Contains("state did not match", await CallbackAsync($"http://127.0.0.1:{port}/auth/callback", "code=x&state=other"));
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned);
        var retry = auth.CompleteBrowserLoginAsync(authorization, TestContext.Current.CancellationToken);
        await CallbackAsync(query["redirect_uri"]!, "code=synthetic-code&state=" + Uri.EscapeDataString(query["state"]!));
        using var credentials = await retry;
        Assert.Equal("synthetic-workspace", credentials.AccountId);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public void AuthorizationRequestCarriesThePinnedConsentContract()
    {
        using var http = new HttpClient(new CodexTestServer((_, _) => throw new InvalidOperationException("No request expected.")));
        using var authorization = new CodexAuthClient(http, new CodexTestServer.Clock()).BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(authorization.AuthorizationUrl.Query);
        Assert.Equal("https://auth.openai.com/oauth/authorize", authorization.AuthorizationUrl.GetLeftPart(UriPartial.Path));
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("openid profile email offline_access api.connectors.read api.connectors.invoke", query["scope"]);
        Assert.Equal("ai_usage", query["originator"]);
        Assert.StartsWith("http://localhost:", query["redirect_uri"]);
        Assert.EndsWith("/auth/callback", query["redirect_uri"]);
        Assert.True(query["state"]!.Length >= 40);
        Assert.DoesNotContain("client_secret", authorization.AuthorizationUrl.Query);
    }


    private static async Task<string> CallbackAsync(string redirectUri, string query)
    {
        using var browser = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var response = await browser.GetAsync(redirectUri + "?" + query, TestContext.Current.CancellationToken);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }
}
