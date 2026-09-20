using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class AntigravityProtocolTests
{
    internal const string Identity = """{"id":"104729","email":"synthetic@example.invalid"}""";
    internal const string Workspace = """{"cloudaicompanionProject":"synthetic-project","currentTier":{"id":"free-tier"}}""";
    internal const string Summary = """
        {"groups":[{"displayName":"Gemini Models","buckets":[
            {"bucketId":"gemini-5h","displayName":"Five Hour Limit Remaining","window":"5h","remainingFraction":0.59,"resetTime":"2030-01-01T05:00:00Z"},
            {"bucketId":"gemini-weekly","displayName":"Weekly Limit Remaining","window":"weekly","remainingFraction":0.88,"resetTime":"2030-01-08T00:00:00Z"}]}]}
        """;
    internal static string Tokens(string refresh = "synthetic-refresh", string? scope = AntigravityHttpScopes) =>
        JsonSerializer.Serialize(new { access_token = "synthetic-access", refresh_token = refresh, expires_in = 3600, token_type = "Bearer", scope });
    private const string AntigravityHttpScopes = "https://www.googleapis.com/auth/cloud-platform https://www.googleapis.com/auth/userinfo.email";

    private static HttpResponseMessage Route(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch
    {
        "/token" => CodexTestServer.Json(Tokens()),
        "/oauth2/v1/userinfo" => CodexTestServer.Json(Identity),
        "/v1internal:loadCodeAssist" => CodexTestServer.Json(Workspace),
        "/v1internal:retrieveUserQuotaSummary" => CodexTestServer.Json(Summary),
        _ => throw new InvalidOperationException("Unexpected endpoint.")
    };

    [Fact]
    public async Task BrowserFlowBindsPkceStateAndTheExactLoopbackRedirect()
    {
        string? body = null;
        using var server = new CodexTestServer(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath == "/token")
            {
                Assert.Equal("https://oauth2.googleapis.com/token", request.RequestUri.AbsoluteUri);
                Assert.Equal("AiUsage/0.1", request.Headers.UserAgent.ToString());
                Assert.Null(request.Headers.Authorization);
                body = await request.Content!.ReadAsStringAsync(token);
            }
            return Route(request);
        });
        using var http = new HttpClient(server);
        var auth = Auth(http, new CodexTestServer.Clock());
        using var attempt = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query);
        Assert.Equal("accounts.google.com", attempt.AuthorizationUrl.Host);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.Contains("https://www.googleapis.com/auth/cloud-platform", query["scope"]);
        var redirect = new Uri(query["redirect_uri"]!);
        Assert.Equal("127.0.0.1", redirect.Host);
        Assert.Equal("/oauth-callback", redirect.AbsolutePath);

        var login = auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken);
        using var callback = new HttpClient();
        var page = await callback.GetStringAsync(redirect + "?code=synthetic-code&state=" + query["state"], TestContext.Current.CancellationToken);
        var grant = await login;
        Assert.Equal("104729", grant.AccountId);
        Assert.Equal(CodexTestServer.Clock.Now.AddMinutes(55), grant.RefreshAt);
        var sent = HttpUtility.ParseQueryString(body!);
        Assert.Equal("authorization_code", sent["grant_type"]);
        Assert.Equal("synthetic-code", sent["code"]);
        Assert.Equal(query["redirect_uri"], sent["redirect_uri"]);
        Assert.Equal(query["client_id"], sent["client_id"]);
        Assert.False(string.IsNullOrEmpty(sent["client_secret"]));
        Assert.Equal(query["code_challenge"], Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(sent["code_verifier"]!)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        Assert.DoesNotContain("synthetic", page);
        Assert.DoesNotContain("synthetic", grant.ToString());
        // One exchange plus one identity read; the attempt cannot be replayed.
        Assert.Equal(2, server.Calls);
        await Assert.ThrowsAsync<AntigravityException>(() => auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ForeignCallbackIsIgnoredAndDenialIsSanitized()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException("No exchange expected."));
        using var http = new HttpClient(server);
        var auth = Auth(http);
        using var attempt = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken);
        using var callback = new HttpClient();
        using var rejected = await callback.GetAsync(query["redirect_uri"] + "?code=synthetic-code&state=foreign", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.False(login.IsCompleted);
        await callback.GetStringAsync(query["redirect_uri"] + "?error=synthetic-secret&state=" + query["state"], TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => login);
        Assert.Equal(ProviderFailureKind.AccessDenied, error.Kind);
        Assert.DoesNotContain("synthetic-secret", error.ToString());
        Assert.Equal(0, server.Calls);
    }

    [Fact]
    public async Task CancellationClosesAnIncompleteCallbackAndNeverExchanges()
    {
        using var server = new CodexTestServer((_, _) => throw new InvalidOperationException());
        using var http = new HttpClient(server);
        var auth = Auth(http);
        using var attempt = auth.BeginBrowserLogin();
        using var cancel = new CancellationTokenSource();
        var login = auth.CompleteBrowserLoginAsync(attempt, cancel.Token);
        var redirect = new Uri(HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query)["redirect_uri"]!);
        using var socket = new System.Net.Sockets.TcpClient();
        await socket.ConnectAsync(IPAddress.Loopback, redirect.Port, TestContext.Current.CancellationToken);
        await socket.GetStream().WriteAsync("GET /oauth-callback HTTP/1.1\r\n"u8.ToArray(), TestContext.Current.CancellationToken);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => login);
        Assert.Equal(0, server.Calls);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"access_token":"synthetic-access","refresh_token":"synthetic-refresh","expires_in":-1}""")]
    [InlineData("""{"access_token":"synthetic-access\r\nsecret","refresh_token":"synthetic-refresh","expires_in":3600}""")]
    [InlineData("""{"access_token":"synthetic-access","expires_in":3600}""")]
    [InlineData("""{"access_token":"synthetic-access","refresh_token":"synthetic-refresh","expires_in":3600,"token_type":"mac"}""")]
    [InlineData("""{"access_token":"synthetic-access","refresh_token":"synthetic-refresh","expires_in":3600,"scope":"https://www.googleapis.com/auth/userinfo.email"}""")]
    public async Task InvalidTokenResponsesNeverPromoteAGrantOrEchoValues(string payload)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(payload)));
        using var http = new HttpClient(server);
        var auth = Auth(http);
        using var attempt = auth.BeginBrowserLogin();
        var query = HttpUtility.ParseQueryString(attempt.AuthorizationUrl.Query);
        var login = auth.CompleteBrowserLoginAsync(attempt, TestContext.Current.CancellationToken);
        using var callback = new HttpClient();
        await callback.GetStringAsync(query["redirect_uri"] + "?code=synthetic-code&state=" + query["state"], TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => login);
        Assert.Equal(ProviderFailureKind.InvalidResponse, error.Kind);
        Assert.DoesNotContain("synthetic", error.ToString());
        // A malformed grant is rejected before any identity or control-plane request.
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task RefreshRetainsAnUnrotatedTokenAndRejectsAnAccountSwitch()
    {
        var identity = "104729";
        var responses = new Queue<string>(["""{"access_token":"synthetic-next","expires_in":3600}""", Tokens()]);
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath == "/token"
            ? CodexTestServer.Json(responses.Dequeue())
            : CodexTestServer.Json("{\"id\":\"" + identity + "\"}")));
        using var http = new HttpClient(server);
        var auth = Auth(http);
        var next = await auth.RefreshAsync("synthetic-refresh", "104729", TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-refresh", next.RefreshToken);
        identity = "999";
        var error = await Assert.ThrowsAsync<AntigravityException>(() => auth.RefreshAsync("synthetic-refresh", "104729", TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.AccountMismatch, error.Kind);
    }

    [Fact]
    public async Task RejectedGrantRequiresReauthenticationAndHidesTheProviderBody()
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(
            """{"error":"invalid_grant","error_description":"synthetic-refresh"}""", HttpStatusCode.BadRequest)));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<AntigravityException>(() =>
            Auth(http).RefreshAsync("synthetic-refresh", "104729", TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.AuthenticationRequired, error.Kind);
        Assert.DoesNotContain("synthetic", error.ToString());
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task AProvisionedWorkspaceIsReadWithoutAnyProviderWrite()
    {
        var bodies = new List<string>();
        using var server = new CodexTestServer(async (request, token) =>
        {
            // Only reads: reaching onboardUser for an account that already has a tier would be a
            // provider-side write nothing asked for.
            Assert.Equal("https://daily-cloudcode-pa.googleapis.com/v1internal:loadCodeAssist", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer synthetic-access", request.Headers.Authorization!.ToString());
            bodies.Add(await request.Content!.ReadAsStringAsync(token));
            return Route(request);
        });
        using var http = new HttpClient(server);
        var workspace = await Quota(http).DiscoverWorkspaceAsync("synthetic-access", TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-project", workspace.ProjectId);
        Assert.Equal("free-tier", workspace.Tier);
        // The second read repeats OMP's project-scoped load, which is how the active tier is confirmed
        // before deciding whether provisioning is needed at all.
        Assert.Equal(["""{"metadata":{"ideType":"ANTIGRAVITY"}}""",
            """{"cloudaicompanionProject":"synthetic-project","metadata":{"ideType":"ANTIGRAVITY"}}"""], bodies);
    }

    [Fact]
    public async Task AnUnprovisionedAccountIsEnrolledInTheFreeTierAndPolledUntilTheOperationCompletes()
    {
        var paths = new List<string>();
        var bodies = new List<string>();
        var onboarded = false;
        var polls = 0;
        using var server = new CodexTestServer(async (request, token) =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            if (request.Content is not null) bodies.Add(await request.Content.ReadAsStringAsync(token));
            return request.RequestUri.AbsolutePath switch
            {
                "/v1internal:loadCodeAssist" => CodexTestServer.Json(onboarded
                    ? Workspace
                    : """{"allowedTiers":[{"id":"free-tier"}]}"""),
                "/v1internal:onboardUser" => CodexTestServer.Json("""{"name":"operations/synthetic-op","done":false}"""),
                "/v1internal/operations/synthetic-op" => CodexTestServer.Json(++polls < 2
                    ? """{"name":"operations/synthetic-op","done":false}"""
                    : Onboarded(() => onboarded = true)),
                _ => throw new InvalidOperationException("Unexpected endpoint.")
            };
        });
        using var http = new HttpClient(server);
        var workspace = await Quota(http).DiscoverWorkspaceAsync("synthetic-access", TestContext.Current.CancellationToken);
        Assert.Equal("synthetic-project", workspace.ProjectId);
        Assert.Equal("free-tier", workspace.Tier);
        Assert.Equal(["/v1internal:loadCodeAssist", "/v1internal:onboardUser", "/v1internal/operations/synthetic-op",
            "/v1internal/operations/synthetic-op", "/v1internal:loadCodeAssist", "/v1internal:loadCodeAssist"], paths);
        Assert.Contains("""{"tierId":"free-tier","metadata":{"ideType":"ANTIGRAVITY"}}""", bodies);
    }

    [Theory]
    [InlineData("""{"ineligibleTiers":[{"tierId":"free-tier","reasonMessage":"synthetic-reason"}]}""")]
    [InlineData("""{"allowedTiers":[{"id":"paid-tier"}],"ineligibleTiers":[{"tierId":"free-tier","reasonMessage":"synthetic-reason"}]}""")]
    public async Task AnIneligibleAccountIsReportedWithoutAttemptingToProvisionIt(string payload)
    {
        using var server = new CodexTestServer((_, _) => Task.FromResult(CodexTestServer.Json(payload)));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => Quota(http)
            .DiscoverWorkspaceAsync("synthetic-access", TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.ProjectUnavailable, error.Kind);
        Assert.DoesNotContain("synthetic", error.ToString());
        // Only the initial read; the declared ineligibility is not argued with.
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task FailedProvisioningReportsNoWorkspaceWithoutEchoingTheProviderReason()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/v1internal:loadCodeAssist" => CodexTestServer.Json("""{"allowedTiers":[{"id":"free-tier"}]}"""),
            _ => CodexTestServer.Json("""{"name":"operations/synthetic-op","done":true,"error":{"code":7,"message":"synthetic-reason"}}""")
        }));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => Quota(http)
            .DiscoverWorkspaceAsync("synthetic-access", TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.ProjectUnavailable, error.Kind);
        Assert.DoesNotContain("synthetic", error.ToString());
    }

    [Fact]
    public async Task ProvisioningThatNeverCompletesTimesOutInsteadOfPollingForever()
    {
        var clock = new CodexTestServer.Clock();
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/v1internal:loadCodeAssist" => CodexTestServer.Json("""{"allowedTiers":[{"id":"free-tier"}]}"""),
            _ => CodexTestServer.Json("""{"name":"operations/synthetic-op","done":false}""")
        }));
        using var http = new HttpClient(server);
        var client = new AntigravityQuotaClient(http, clock, (duration, _) => { clock.Current += duration; return Task.CompletedTask; });
        var error = await Assert.ThrowsAsync<AntigravityException>(() =>
            client.DiscoverWorkspaceAsync("synthetic-access", TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.Timeout, error.Kind);
        Assert.Equal(TimeSpan.FromSeconds(30), clock.Current - CodexTestServer.Clock.Now);
    }

    [Theory]
    [InlineData("""{"name":"../../elsewhere","done":false}""")]
    [InlineData("""{"name":"https://elsewhere.invalid/op","done":false}""")]
    [InlineData("""{"done":false}""")]
    public async Task AnUnsafeOperationNameIsRejectedInsteadOfRedirectingThePoll(string operation)
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/v1internal:loadCodeAssist" => CodexTestServer.Json("""{"allowedTiers":[{"id":"free-tier"}]}"""),
            "/v1internal:onboardUser" => CodexTestServer.Json(operation),
            _ => throw new InvalidOperationException("The poll must not leave the operations path.")
        }));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => Quota(http)
            .DiscoverWorkspaceAsync("synthetic-access", TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.InvalidResponse, error.Kind);
    }

    [Fact]
    public async Task ProvisioningThatYieldsNoWorkspaceIsStillReportedAsNoWorkspace()
    {
        using var server = new CodexTestServer((request, _) => Task.FromResult(request.RequestUri!.AbsolutePath switch
        {
            "/v1internal:loadCodeAssist" => CodexTestServer.Json("""{"allowedTiers":[{"id":"free-tier"}]}"""),
            _ => CodexTestServer.Json(Onboarded(null))
        }));
        using var http = new HttpClient(server);
        var error = await Assert.ThrowsAsync<AntigravityException>(() => Quota(http)
            .DiscoverWorkspaceAsync("synthetic-access", TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.ProjectUnavailable, error.Kind);
    }

    private static string Onboarded(Action? onRead)
    {
        onRead?.Invoke();
        return """{"name":"operations/synthetic-op","done":true,"response":{"@type":"synthetic","cloudaicompanionProject":"synthetic-project"}}""";
    }

    [Fact]
    public async Task QuotaRequestsTheDiscoveredProjectAndNeverTheLegacyOrSandboxEndpoint()
    {
        string? body = null;
        using var server = new CodexTestServer(async (request, token) =>
        {
            Assert.Equal("https://daily-cloudcode-pa.googleapis.com/v1internal:retrieveUserQuotaSummary", request.RequestUri!.AbsoluteUri);
            body = await request.Content!.ReadAsStringAsync(token);
            return Route(request);
        });
        using var http = new HttpClient(server);
        var quota = await Quota(http)
            .GetQuotaAsync(Credentials(), TestContext.Current.CancellationToken);
        Assert.Equal("""{"project":"synthetic-project"}""", body);
        Assert.Equal("free-tier", quota.PlanType);
        Assert.Equal(1, server.Calls);
    }

    [Fact]
    public async Task RateLimitedQuotaIsNotRepeatedBeforeTheProviderRetryTime()
    {
        using var server = new CodexTestServer((_, _) =>
        {
            var response = CodexTestServer.Json("{}", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromMinutes(5));
            return Task.FromResult(response);
        });
        using var http = new HttpClient(server);
        var clock = new CodexTestServer.Clock();
        var client = new AntigravityQuotaClient(http, clock);
        var first = await Assert.ThrowsAsync<AntigravityException>(() => client.GetQuotaAsync(Credentials(), TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.RateLimited, first.Kind);
        var second = await Assert.ThrowsAsync<AntigravityException>(() => client.GetQuotaAsync(Credentials(), TestContext.Current.CancellationToken));
        Assert.Equal(ProviderFailureKind.RateLimited, second.Kind);
        Assert.Equal(1, server.Calls);
        clock.Current = clock.Current.AddMinutes(6);
        await Assert.ThrowsAsync<AntigravityException>(() => client.GetQuotaAsync(Credentials(), TestContext.Current.CancellationToken));
        Assert.Equal(2, server.Calls);
    }

    internal static AntigravityCredentials Credentials() =>
        new("synthetic-access", "synthetic-refresh", "104729", "synthetic-project", "free-tier", CodexTestServer.Clock.Now.AddHours(1));

    /// <summary>The repository vendors no Antigravity registration, so tests supply a synthetic one.</summary>
    internal static AntigravityAuthClient Auth(HttpClient http, TimeProvider? clock = null) =>
        new(http, clock, () => new("synthetic-client.apps.googleusercontent.invalid", "synthetic-client-secret"));

    /// <summary>Provisioning waits on a provider operation, so tests drive its delay deterministically.</summary>
    internal static AntigravityQuotaClient Quota(HttpClient http, CodexTestServer.Clock? clock = null)
    {
        clock ??= new CodexTestServer.Clock();
        return new(http, clock, (duration, token) => { token.ThrowIfCancellationRequested(); clock.Current += duration; return Task.CompletedTask; });
    }

    [Fact]
    public void AnAbsentOrUnusableRegistrationIsReportedInsteadOfContactingTheProvider()
    {
        var id = Environment.GetEnvironmentVariable(AntigravityRegistration.ClientIdVariable);
        var secret = Environment.GetEnvironmentVariable(AntigravityRegistration.ClientSecretVariable);
        try
        {
            foreach (var (candidateId, candidateSecret) in new[]
            {
                (null, null), ("synthetic-client", null), (null, "synthetic-secret"),
                ("", "synthetic-secret"), ("synthetic client", "synthetic-secret")
            })
            {
                Environment.SetEnvironmentVariable(AntigravityRegistration.ClientIdVariable, candidateId);
                Environment.SetEnvironmentVariable(AntigravityRegistration.ClientSecretVariable, candidateSecret);
                var error = Assert.Throws<AntigravityException>(AntigravityRegistration.FromEnvironment);
                Assert.Equal(ProviderFailureKind.RegistrationUnavailable, error.Kind);
                Assert.DoesNotContain("synthetic", error.ToString());
            }
            Environment.SetEnvironmentVariable(AntigravityRegistration.ClientIdVariable, "synthetic-client");
            Environment.SetEnvironmentVariable(AntigravityRegistration.ClientSecretVariable, "synthetic-secret");
            Assert.Equal("synthetic-client", AntigravityRegistration.FromEnvironment().ClientId);
            Assert.DoesNotContain("synthetic", AntigravityRegistration.FromEnvironment().ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable(AntigravityRegistration.ClientIdVariable, id);
            Environment.SetEnvironmentVariable(AntigravityRegistration.ClientSecretVariable, secret);
        }
    }
}
