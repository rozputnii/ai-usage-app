using AiUsage.Core.Usage;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// Google's documented installed-application authorization-code flow driven with OMP's Antigravity
/// registration. Using a third party for Antigravity is restricted by the provider; see
/// docs/providers/antigravity.md. No inference, onboarding or CLI credential access happens here.
/// </summary>
internal sealed class AntigravityAuthClient
{
    private readonly ProviderTransportOptions transport;
    private readonly HttpClient client;
    private readonly TimeProvider clock;
    private readonly Func<AntigravityRegistration> registration;

    public AntigravityAuthClient(HttpClient client, TimeProvider? timeProvider = null, ProviderTransportOptions? options = null)
        : this(client, timeProvider, AntigravityRegistration.FromEnvironment, options) { }

    internal AntigravityAuthClient(HttpClient client, TimeProvider? timeProvider, Func<AntigravityRegistration> registration, ProviderTransportOptions? options = null)
    {
        transport = options ?? ProviderTransportOptions.Default;
        this.client = client;
        clock = timeProvider ?? TimeProvider.System;
        this.registration = registration;
    }

    private const string CallbackPath = "/oauth-callback";
    // The registered loopback callback. Google matches a loopback redirect without its port, so an
    // occupied port falls back to an ephemeral one and the exchange repeats the exact redirect used.
    private const int PreferredPort = 51121;

    public AntigravityBrowserAuthorization BeginBrowserLogin()
    {
        var client = registration();
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var state = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        LoopbackCallback callback;
        try { callback = LoopbackCallback.Start([PreferredPort, 0]); }
        catch (IOException) { throw new ProviderException(ProviderFailureKind.BrowserCallbackUnavailable); }
        try
        {
            var redirect = $"http://127.0.0.1:{callback.Port}{CallbackPath}";
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = client.ClientId, ["response_type"] = "code", ["redirect_uri"] = redirect,
                ["scope"] = AntigravityHttp.Scopes, ["state"] = state,
                // PKCE is required by Google's native-application guidance; the upstream rule omits it.
                ["code_challenge"] = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
                ["code_challenge_method"] = "S256",
                // A durable grant is the point of a monitor; consent is forced so one is actually issued.
                ["access_type"] = "offline", ["prompt"] = "consent"
            };
            var url = new Uri(AntigravityHttp.AuthorizeUrl + "?" + string.Join('&', parameters.Select(
                pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value))));
            return new(callback, url, redirect, state, verifier, client, clock.GetUtcNow().AddMinutes(15));
        }
        catch { callback.Dispose(); throw; }
    }

    internal async Task<AntigravityGrant> CompleteBrowserLoginAsync(AntigravityBrowserAuthorization authorization,
        CancellationToken cancellationToken = default)
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
                var code = await ReadBrowserCodeAsync(authorization, linked.Token).ConfigureAwait(false);
                linked.Token.ThrowIfCancellationRequested();
                if (clock.GetUtcNow() >= authorization.ExpiresAt)
                    throw new ProviderException(ProviderFailureKind.LoginAttemptExpired);
                authorization.Completed = true;
                using var request = Post(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code", ["code"] = code,
                    ["client_id"] = authorization.Registration.ClientId, ["client_secret"] = authorization.Registration.ClientSecret,
                    ["redirect_uri"] = authorization.RedirectUri, ["code_verifier"] = authorization.CodeVerifier
                });
                using var response = await ProviderTransport.SendAsync(client, request, clock, linked.Token, transport).ConfigureAwait(false);
                if (!response.IsSuccess)
                    throw TokenFailure(response);
                // A returned grant reaches the caller even if later discovery or quota work is cancelled.
                return await ParseGrantAsync(response.Body!.RootElement, null, CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new ProviderException(ProviderFailureKind.LoginAttemptExpired);
            }
        }
        finally { authorization.Gate.Release(); }
    }

    /// <summary>
    /// One renewal without retries. Google normally omits a new refresh token, so the previous one is
    /// retained; a returned replacement must be persisted before the old value is used again.
    /// </summary>
    internal async Task<AntigravityGrant> RefreshAsync(string refreshToken, string accountId, CancellationToken cancellationToken = default)
    {
        var current = registration();
        using var request = Post(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken,
            ["client_id"] = current.ClientId, ["client_secret"] = current.ClientSecret
        });
        using var response = await ProviderTransport.SendAsync(client, request, clock, cancellationToken, transport).ConfigureAwait(false);
        if (!response.IsSuccess)
            throw TokenFailure(response);
        return await ParseGrantAsync(response.Body!.RootElement, new(refreshToken, accountId), CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>Reads the opaque Google subject. Profile fields beyond it are neither stored nor displayed.</summary>
    internal async Task<string> GetIdentityAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, AntigravityHttp.IdentityUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await ProviderTransport.SendAsync(client, request, clock, cancellationToken, transport).ConfigureAwait(false);
        if (!response.IsSuccess)
            throw ProviderTransport.Failure(response);
        var subject = Text(Property(response.Body!.RootElement, "id"));
        if (!SafeIdentity(subject))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        return subject!;
    }

    private async Task<AntigravityGrant> ParseGrantAsync(JsonElement root, PreviousGrant? previous, CancellationToken cancellationToken)
    {
        var access = Text(Property(root, "access_token"));
        var refreshField = Property(root, "refresh_token");
        var refresh = refreshField.ValueKind == JsonValueKind.Undefined ? previous?.RefreshToken : Text(refreshField);
        var expiry = Property(root, "expires_in");
        var type = Property(root, "token_type");
        var scope = Property(root, "scope");
        if (!SafeToken(access) || !SafeToken(refresh) || expiry.ValueKind != JsonValueKind.Number ||
            !expiry.TryGetDouble(out var seconds) || !double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.FromDays(365).TotalSeconds ||
            (type.ValueKind != JsonValueKind.Undefined && !string.Equals(Text(type), "Bearer", StringComparison.OrdinalIgnoreCase)) ||
            // A consent screen that dropped the control-plane scope yields a grant that cannot read quota.
            (scope.ValueKind != JsonValueKind.Undefined && (Text(scope) is not { } granted ||
                !granted.Split(' ').Contains(AntigravityHttp.RequiredScope, StringComparer.Ordinal))))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        var identity = await GetIdentityAsync(access!, cancellationToken).ConfigureAwait(false);
        if (previous is not null && identity != previous.AccountId)
            throw new ProviderException(ProviderFailureKind.AccountMismatch);
        // The rule's five-minute skew keeps a renewal ahead of the provider's own expiry.
        return new(access!, refresh!, identity, clock.GetUtcNow().AddSeconds(Math.Max(0, seconds - 300)));
    }

    private static async Task<string> ReadBrowserCodeAsync(AntigravityBrowserAuthorization authorization, CancellationToken cancellationToken)
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

    private static HttpRequestMessage Post(Dictionary<string, string> values) =>
        new(HttpMethod.Post, AntigravityHttp.TokenUrl) { Content = new FormUrlEncodedContent(values) };

    /// <summary>A rejected grant needs a new sign-in; other token errors keep their transport meaning.</summary>
    private static ProviderException TokenFailure(ProviderHttpResponse response)
    {
        var error = Text(Property(response.Body?.RootElement ?? default, "error"));
        return error switch
        {
            "invalid_grant" => new(ProviderFailureKind.AuthenticationRequired, response.StatusCode),
            "access_denied" => new(ProviderFailureKind.AccessDenied, response.StatusCode),
            _ => ProviderTransport.Failure(response)
        };
    }

    private sealed record PreviousGrant(string RefreshToken, string AccountId);

    internal static bool SafeIdentity(string? value) => value is { Length: > 0 and <= 1024 } && value.All(c => c is >= '!' and <= '~');
    internal static bool SafeToken(string? value) => value is { Length: > 0 and <= 65536 } && value.All(c => c is >= '!' and <= '~');
    internal static JsonElement Property(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) ? value : default;
    internal static string? Text(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
