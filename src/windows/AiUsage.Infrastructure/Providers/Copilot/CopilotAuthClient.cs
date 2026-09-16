using AiUsage.Core.Providers.Copilot;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>
/// GitHub OAuth device flow with OMP's reused OpenCode client (read:user). See docs/providers/copilot.md.
/// No model policy, inference or account mutation is performed.
/// </summary>
public sealed class CopilotAuthClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(30);
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task<CopilotDeviceAuthorization> BeginDeviceLoginAsync(CancellationToken cancellationToken = default)
    {
        using var request = FormPost(CopilotHttp.DeviceCodeUrl, new() { ["client_id"] = CopilotHttp.ClientId, ["scope"] = CopilotHttp.Scope });
        using var response = await CopilotHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) throw CopilotHttp.Failure(response);
        var root = RootObject(response);
        if (root.TryGetProperty("error", out _)) throw new CopilotException(CopilotFailureKind.RequestRejected, response.StatusCode);
        if (!TryString(root, "device_code", out var deviceCode) || deviceCode.Length > 256 ||
            !TryString(root, "user_code", out var userCode) || userCode.Length > 32 ||
            !TryString(root, "verification_uri", out var verification) ||
            !Uri.TryCreate(verification, UriKind.Absolute, out var verificationUri) ||
            verificationUri.Scheme != Uri.UriSchemeHttps || !verificationUri.IsDefaultPort ||
            !string.Equals(verificationUri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            !root.TryGetProperty("expires_in", out var expires) || !expires.TryGetInt32(out var expiresIn) || expiresIn <= 0 ||
            !root.TryGetProperty("interval", out var intervalValue) || !intervalValue.TryGetInt32(out var interval) || interval < 0)
            throw new CopilotException(CopilotFailureKind.InvalidResponse);
        var lifetime = TimeSpan.FromSeconds(expiresIn);
        return new(deviceCode, userCode, verificationUri, Max(TimeSpan.FromSeconds(interval), MinimumInterval),
            clock.GetUtcNow() + (lifetime < MaximumLifetime ? lifetime : MaximumLifetime));
    }

    /// <summary>Polls until approval, denial or expiry, then binds the token to the account it belongs to.</summary>
    public async Task<CopilotCredentials> CompleteDeviceLoginAsync(CopilotDeviceAuthorization authorization, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        await authorization.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (authorization.Completed) throw new CopilotException(CopilotFailureKind.AuthenticationRequired);
            var interval = authorization.Interval;
            while (true)
            {
                var remaining = authorization.ExpiresAt - clock.GetUtcNow();
                if (remaining <= TimeSpan.Zero) throw Finish(authorization, CopilotFailureKind.LoginAttemptExpired);
                await Task.Delay(interval < remaining ? interval : remaining, clock, cancellationToken).ConfigureAwait(false);
                if (clock.GetUtcNow() >= authorization.ExpiresAt) throw Finish(authorization, CopilotFailureKind.LoginAttemptExpired);
                using var request = FormPost(CopilotHttp.TokenUrl, new()
                {
                    ["client_id"] = CopilotHttp.ClientId,
                    ["device_code"] = authorization.DeviceCode,
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
                });
                string accessToken;
                string? scope;
                using (var response = await CopilotHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccess) throw CopilotHttp.Failure(response);
                    var root = RootObject(response);
                    if (TryString(root, "error", out var error))
                    {
                        if (error == "authorization_pending") continue;
                        if (error == "slow_down")
                        {
                            var next = root.TryGetProperty("interval", out var value) && value.TryGetInt32(out var seconds) && seconds > 0
                                ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
                            interval = Max(next, interval + MinimumInterval);
                            continue;
                        }
                        throw Finish(authorization, error switch
                        {
                            "expired_token" => CopilotFailureKind.LoginAttemptExpired,
                            "access_denied" => CopilotFailureKind.LoginDenied,
                            _ => CopilotFailureKind.RequestRejected
                        });
                    }
                    // A token was issued: this attempt cannot be replayed whatever happens next.
                    authorization.Completed = true;
                    if (!TryString(root, "access_token", out accessToken) || accessToken.Length > 4096 ||
                        !TryString(root, "token_type", out var tokenType) || !tokenType.Equals("bearer", StringComparison.OrdinalIgnoreCase))
                        throw new CopilotException(CopilotFailureKind.InvalidResponse);
                    scope = TryString(root, "scope", out var granted) ? granted : null;
                }
                var identity = await GetIdentityAsync(accessToken, cancellationToken).ConfigureAwait(false);
                return new CopilotCredentials(accessToken, identity, scope);
            }
        }
        finally { authorization.Gate.Release(); }
    }

    /// <summary>Re-reads the account behind existing credentials and rejects a different account.</summary>
    public async Task<CopilotCredentials> VerifyAsync(CopilotCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var identity = await GetIdentityAsync(credentials.AccessToken, cancellationToken).ConfigureAwait(false);
        if (identity.AccountId != credentials.Identity.AccountId) throw new CopilotException(CopilotFailureKind.AccountMismatch);
        return identity == credentials.Identity ? credentials : new CopilotCredentials(credentials.AccessToken, identity, credentials.GrantedScope);
    }

    private async Task<CopilotIdentity> GetIdentityAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CopilotHttp.ApiGet("/user", accessToken);
        using var response = await CopilotHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess) throw CopilotHttp.Failure(response);
        var root = RootObject(response);
        if (!root.TryGetProperty("id", out var id) || !id.TryGetInt64(out var accountId) || accountId <= 0 ||
            !TryString(root, "login", out var login) || !IsLogin(login))
            throw new CopilotException(CopilotFailureKind.InvalidResponse);
        return new(accountId, login);
    }

    internal static bool IsLogin(string login) =>
        login.Length is > 0 and <= 39 && login.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static CopilotException Finish(CopilotDeviceAuthorization authorization, CopilotFailureKind kind)
    {
        authorization.Completed = true;
        return new(kind);
    }

    private static JsonElement RootObject(ProviderHttpResponse response) =>
        response.Body?.RootElement is { ValueKind: JsonValueKind.Object } root ? root : throw new CopilotException(CopilotFailureKind.InvalidResponse);

    private static HttpRequestMessage FormPost(string url, Dictionary<string, string> fields) =>
        new(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(fields) };

    private static bool TryString(JsonElement root, string name, out string value)
    {
        value = root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString()! : "";
        return value.Length > 0;
    }

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;
}
