using AiUsage.Core.Usage;
using AiUsage.Core.Providers.Claude;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>Private, unsupported OMP-style OAuth flow. See docs/providers/claude.md for the restriction.</summary>
public sealed class ClaudeAuthClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private const string CallbackPath = "/callback";
    private const string Scopes = "org:create_api_key user:profile user:inference user:sessions:claude_code user:mcp_servers user:file_upload";

    public ClaudeBrowserAuthorization BeginBrowserLogin()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var state = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        LoopbackCallback callback;
        try { callback = LoopbackCallback.Start([54545, 0]); }
        catch (IOException) { throw new ProviderException(ProviderFailureKind.BrowserCallbackUnavailable); }
        try
        {
            var redirect = $"http://localhost:{callback.Port}{CallbackPath}";
            var parameters = new Dictionary<string, string>
            {
                ["code"] = "true", ["response_type"] = "code", ["client_id"] = ClaudeHttp.ClientId,
                ["redirect_uri"] = redirect, ["scope"] = Scopes, ["state"] = state,
                ["code_challenge"] = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
                ["code_challenge_method"] = "S256"
            };
            var url = new Uri("https://claude.ai/oauth/authorize?" + string.Join('&', parameters.Select(
                pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value))));
            return new(callback, url, redirect, state, verifier, clock.GetUtcNow().AddMinutes(15));
        }
        catch { callback.Dispose(); throw; }
    }

    public async Task<ClaudeCredentials> CompleteBrowserLoginAsync(ClaudeBrowserAuthorization authorization, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        await authorization.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (authorization.Completed)
                throw new ProviderException(ProviderFailureKind.AuthenticationRequired);
            var remaining = authorization.ExpiresAt - clock.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
                throw new ProviderException(ProviderFailureKind.LoginAttemptExpired);
            using var deadline = new CancellationTokenSource(remaining, clock);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            try
            {
                var code = await ReadCodeAsync(authorization, linked.Token).ConfigureAwait(false);
                linked.Token.ThrowIfCancellationRequested();
                if (clock.GetUtcNow() >= authorization.ExpiresAt)
                    throw new ProviderException(ProviderFailureKind.LoginAttemptExpired);
                authorization.Completed = true;
                using var request = new HttpRequestMessage(HttpMethod.Post, ClaudeHttp.TokenUrl)
                {
                    Content = JsonContent.Create(new ClaudeTokenRequest
                    {
                        GrantType = "authorization_code", Code = code, CodeVerifier = authorization.CodeVerifier,
                        RedirectUri = authorization.RedirectUri, State = authorization.State
                    }, ClaudeAuthJson.Default.ClaudeTokenRequest)
                };
                using var response = await ProviderTransport.SendAsync(client, request, clock, linked.Token).ConfigureAwait(false);
                if (!response.IsSuccess)
                    throw ProviderTransport.Failure(response);
                // A returned pair reaches the session even if later quota work is canceled.
                return await ParseTokensAsync(response.Body!.RootElement, null, CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new ProviderException(ProviderFailureKind.LoginAttemptExpired);
            }
        }
        finally { authorization.Gate.Release(); }
    }

    private async Task<string> ReadCodeAsync(ClaudeBrowserAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization.ManualCode.Task.IsCompletedSuccessfully)
            return await authorization.ManualCode.Task.ConfigureAwait(false);
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var browser = ReadBrowserCodeAsync(authorization, waiting.Token);
        try
        {
            var winner = await Task.WhenAny(browser, authorization.ManualCode.Task).WaitAsync(cancellationToken).ConfigureAwait(false);
            return await winner.ConfigureAwait(false);
        }
        finally
        {
            await waiting.CancelAsync().ConfigureAwait(false);
            // Observe and close the losing socket read before a manually supplied code is exchanged.
            try { await browser.ConfigureAwait(false); }
            catch (OperationCanceledException) when (waiting.IsCancellationRequested) { }
        }
    }

    private async Task<string> ReadBrowserCodeAsync(ClaudeBrowserAuthorization authorization, CancellationToken cancellationToken)
    {
        while (true)
        {
            using var request = await authorization.Callback.AcceptAsync(cancellationToken).ConfigureAwait(false);
            if (request is null)
                continue;
            var separator = request.Target.IndexOf('?');
            var path = separator < 0 ? request.Target : request.Target[..separator];
            if (!StringComparer.Ordinal.Equals(path, CallbackPath))
            {
                await request.RespondAsync(HttpStatusCode.NotFound, "Not the sign-in callback.", cancellationToken).ConfigureAwait(false);
                continue;
            }
            var query = HttpUtility.ParseQueryString(separator < 0 ? "" : request.Target[separator..]);
            if (!authorization.MatchesState(query["state"]))
            {
                await request.RespondAsync(HttpStatusCode.BadRequest, "Sign-in state did not match; callback ignored.", cancellationToken).ConfigureAwait(false);
                continue;
            }
            await request.RespondAsync(HttpStatusCode.OK, "Sign-in response received. Return to AI Usage for the result.", cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(query["error"]))
                throw new ProviderException(ProviderFailureKind.AccessDenied);
            var code = query["code"];
            if (!SafeToken(code))
                throw new ProviderException(ProviderFailureKind.InvalidResponse);
            return code!;
        }
    }

    /// <summary>
    /// One exchange, without retries. A durable caller marks the old grant uncertain before
    /// sending, then persists the returned pair before checking cancellation again.
    /// </summary>
    internal async Task<ClaudeCredentials> RefreshAsync(ClaudeCredentials previous, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previous);
        using var request = new HttpRequestMessage(HttpMethod.Post, ClaudeHttp.TokenUrl)
        {
            Content = JsonContent.Create(new ClaudeTokenRequest { GrantType = "refresh_token", RefreshToken = previous.RefreshToken }, ClaudeAuthJson.Default.ClaudeTokenRequest)
        };
        request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
        using var response = await ProviderTransport.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            var error = Property(response.Body?.RootElement ?? default, "error");
            if (Text(error) == "invalid_grant" || Text(Property(error, "type")) == "invalid_grant")
                throw new ProviderException(ProviderFailureKind.AuthenticationRequired, response.StatusCode);
            throw ProviderTransport.Failure(response);
        }
        return await ParseTokensAsync(response.Body!.RootElement, previous, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<ClaudeCredentials> ParseTokensAsync(JsonElement root, ClaudeCredentials? previous, CancellationToken cancellationToken)
    {
        var access = Text(Property(root, "access_token"));
        var refreshField = Property(root, "refresh_token");
        var refresh = refreshField.ValueKind == JsonValueKind.Undefined ? previous?.RefreshToken : Text(refreshField);
        var expiry = Property(root, "expires_in");
        var scope = Property(root, "scope");
        if (!SafeToken(access) || !SafeToken(refresh) || expiry.ValueKind != JsonValueKind.Number ||
            !expiry.TryGetDouble(out var seconds) || !double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.FromDays(365).TotalSeconds ||
            (scope.ValueKind != JsonValueKind.Undefined && (Text(scope) is not { } granted || !granted.Split(' ').Contains("user:profile", StringComparer.Ordinal))))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        var account = IdentityField(root, "account", "uuid");
        var organization = IdentityField(root, "organization", "uuid");
        if (previous is not null)
        {
            if ((account is not null && account != previous.Identity.AccountId) ||
                (organization is not null && organization != previous.Identity.OrganizationId))
                throw new ProviderException(ProviderFailureKind.AccountMismatch);
            account ??= previous.Identity.AccountId;
            organization ??= previous.Identity.OrganizationId;
        }
        if (account is null || organization is null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/api/claude_cli/bootstrap?entrypoint=cli&model=claude-opus-4-8");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            request.Headers.Add("anthropic-beta", "oauth-2025-04-20");
            using var response = await ProviderTransport.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess)
                throw ProviderTransport.Failure(response);
            var identity = Property(response.Body!.RootElement, "oauth_account");
            var fallbackAccount = Text(Property(identity, "account_uuid"));
            var fallbackOrganization = Text(Property(identity, "organization_uuid"));
            if ((account is not null && fallbackAccount is not null && account != fallbackAccount) ||
                (organization is not null && fallbackOrganization is not null && organization != fallbackOrganization))
                throw new ProviderException(ProviderFailureKind.AccountMismatch);
            account ??= fallbackAccount;
            organization ??= fallbackOrganization;
        }
        if (!SafeIdentity(account) || !SafeIdentity(organization))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        return new(access!, refresh!, new(account!, organization!), clock.GetUtcNow().AddSeconds(Math.Max(0, seconds - 300)));
    }

    private static string? IdentityField(JsonElement root, string container, string name)
    {
        var parent = Property(root, container);
        if (parent.ValueKind == JsonValueKind.Undefined)
            return null;
        if (parent.ValueKind != JsonValueKind.Object)
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        var field = Property(parent, name);
        var value = Text(field);
        if (!SafeIdentity(value))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        return value;
    }

    internal static bool SafeIdentity(string? value) => value is { Length: > 0 and <= 1024 } && value.All(c => c is >= '!' and <= '~');
    internal static bool SafeToken(string? value) => value is { Length: > 0 and <= 65536 } && value.All(c => c is >= '!' and <= '~');
    internal static JsonElement Property(JsonElement root, string name) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) ? value : default;
    internal static string? Text(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
