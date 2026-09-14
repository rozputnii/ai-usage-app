using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static AiUsage.Infrastructure.Providers.Codex.CodexQuotaParser;

namespace AiUsage.Infrastructure.Providers.Codex;

public sealed class CodexAuthClient(HttpClient client, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    private const string CallbackPath = "/auth/callback";
    private static readonly int[] CallbackPorts = [1455, 1457];

    /// <summary>
    /// Starts the browser authorization-code flow with PKCE on a loopback callback.
    /// Nothing is sent to the provider until the user completes consent in their browser.
    /// </summary>
    public CodexBrowserAuthorization BeginBrowserLogin()
    {
        var verifier = RandomUrlSafe(32);
        var challenge = Base64Url(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        var state = RandomUrlSafe(32);
        var callback = CodexLoopbackCallback.Start(CallbackPorts);
        try
        {
            // Official source binds the same loopback ports and allow-listed callback path.
            var redirectUri = $"http://localhost:{callback.Port}{CallbackPath}";
            var query = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = CodexHttp.ClientId,
                ["redirect_uri"] = redirectUri,
                ["scope"] = "openid profile email offline_access api.connectors.read api.connectors.invoke",
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256",
                ["state"] = state,
                ["id_token_add_organizations"] = "true",
                ["codex_cli_simplified_flow"] = "true",
                // Truthful client self-identification; the pinned OMP client sends its own name here
                // rather than the Codex CLI value. Public-client reuse remains permission-unknown.
                ["originator"] = "ai_usage"
            };
            var url = new Uri(CodexHttp.AuthOrigin + "/oauth/authorize?" + string.Join('&',
                query.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value))));
            return new CodexBrowserAuthorization(callback, url, redirectUri, state, verifier, clock.GetUtcNow().AddMinutes(15));
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Waits for the provider to redirect the user's browser back to the loopback callback,
    /// then exchanges the single-use code. A mismatched state never reaches the token endpoint.
    /// </summary>
    public async Task<CodexCredentials> CompleteBrowserLoginAsync(CodexBrowserAuthorization authorization, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        await authorization.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (authorization.Completed)
                throw new CodexException(CodexFailureKind.AuthenticationRequired);
            var lifetime = authorization.ExpiresAt - clock.GetUtcNow();
            if (lifetime <= TimeSpan.Zero)
                throw new CodexException(CodexFailureKind.LoginAttemptExpired);
            using var deadline = new CancellationTokenSource(lifetime, clock);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            try
            {
                while (true)
                {
                    using var request = await authorization.Callback.AcceptAsync(linked.Token).ConfigureAwait(false);
                    if (request is null)
                        continue;
                    var separator = request.Target.IndexOf('?', StringComparison.Ordinal);
                    var path = separator < 0 ? request.Target : request.Target[..separator];
                    if (!StringComparer.Ordinal.Equals(path, CallbackPath))
                    {
                        await request.RespondAsync(HttpStatusCode.NotFound, "Not the sign-in callback.", linked.Token).ConfigureAwait(false);
                        continue;
                    }
                    var parameters = System.Web.HttpUtility.ParseQueryString(separator < 0 ? string.Empty : request.Target[separator..]);
                    var returnedState = parameters["state"];
                    if (returnedState is null || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                            System.Text.Encoding.UTF8.GetBytes(returnedState), System.Text.Encoding.UTF8.GetBytes(authorization.State)))
                    {
                        await request.RespondAsync(HttpStatusCode.BadRequest, "Sign-in state did not match; this callback was ignored.", linked.Token).ConfigureAwait(false);
                        continue;
                    }
                    if (clock.GetUtcNow() >= authorization.ExpiresAt)
                    {
                        await request.RespondAsync(HttpStatusCode.OK, "This sign-in attempt expired. Start a new one from the console.", CancellationToken.None).ConfigureAwait(false);
                        throw new CodexException(CodexFailureKind.LoginAttemptExpired);
                    }
                    if (!string.IsNullOrEmpty(parameters["error"]))
                    {
                        await request.RespondAsync(HttpStatusCode.OK, "Sign-in was refused. Return to the console.", linked.Token).ConfigureAwait(false);
                        throw new CodexException(CodexFailureKind.AccessDenied, providerErrorCode: parameters["error"]);
                    }
                    var code = parameters["code"];
                    if (string.IsNullOrEmpty(code) || code.Length > 65536 || !SafeHeaderValue(code))
                    {
                        await request.RespondAsync(HttpStatusCode.BadRequest, "Sign-in response was incomplete.", linked.Token).ConfigureAwait(false);
                        throw new CodexException(CodexFailureKind.InvalidResponse);
                    }
                    // The code is single-use from here on, whether or not the exchange below succeeds.
                    authorization.Completed = true;
                    try
                    {
                        using var exchange = new HttpRequestMessage(HttpMethod.Post, CodexHttp.AuthOrigin + "/oauth/token")
                        {
                            Content = new FormUrlEncodedContent(new Dictionary<string, string>
                            {
                                ["grant_type"] = "authorization_code",
                                ["client_id"] = CodexHttp.ClientId,
                                ["code"] = code,
                                ["code_verifier"] = authorization.CodeVerifier,
                                ["redirect_uri"] = authorization.RedirectUri
                            })
                        };
                        using var tokens = await CodexHttp.SendAsync(client, exchange, clock, linked.Token).ConfigureAwait(false);
                        if (!tokens.IsSuccess)
                            throw CodexHttp.Failure(tokens);
                        var exchanged = ParseTokens(tokens.Body!.RootElement, null);
                        if (clock.GetUtcNow() >= authorization.ExpiresAt)
                            throw new CodexException(CodexFailureKind.LoginAttemptExpired);
                        await request.RespondAsync(HttpStatusCode.OK, "Sign-in complete. Return to the console; this page carries no credentials.", linked.Token).ConfigureAwait(false);
                        return new CodexCredentials(exchanged.Access, exchanged.Refresh, exchanged.AccountId, exchanged.ExpiresAt);
                    }
                    catch (CodexException)
                    {
                        await request.RespondAsync(HttpStatusCode.OK, "Sign-in could not be completed. Return to the console for the reported outcome.", CancellationToken.None).ConfigureAwait(false);
                        throw;
                    }
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new CodexException(CodexFailureKind.LoginAttemptExpired);
            }
        }
        finally { authorization.Gate.Release(); }
    }

    private static string RandomUrlSafe(int bytes) => Base64Url(System.Security.Cryptography.RandomNumberGenerator.GetBytes(bytes));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public async Task<CodexDeviceAuthorization> BeginDeviceLoginAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, CodexHttp.AuthOrigin + "/api/accounts/deviceauth/usercode")
        {
            Content = JsonContent.Create(new DeviceCodeRequest { ClientId = CodexHttp.ClientId }, CodexAuthJson.Default.DeviceCodeRequest)
        };
        using var response = await CodexHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new CodexException(CodexFailureKind.DeviceLoginUnavailable, response.StatusCode);
        if (!response.IsSuccess)
            throw CodexHttp.Failure(response);
        var root = response.Body!.RootElement;
        var id = RequiredText(root, "device_auth_id");
        var userCode = Text(Property(root, "user_code")) ?? Text(Property(root, "usercode"));
        if (string.IsNullOrWhiteSpace(userCode) || userCode.Length > 128 || userCode.Any(c => c is < ' ' or > '~'))
            throw new CodexException(CodexFailureKind.InvalidResponse);
        var intervalValue = Property(root, "interval");
        double interval = 5;
        if (intervalValue.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
        {
            var parsed = Number(intervalValue);
            if (parsed is null && intervalValue.ValueKind == JsonValueKind.String &&
                double.TryParse(intervalValue.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var textNumber))
                parsed = textNumber;
            if (parsed is null || !double.IsFinite(parsed.Value) || parsed < 0 || parsed > 900)
                throw new CodexException(CodexFailureKind.InvalidResponse);
            interval = Math.Max(1, parsed.Value);
        }
        return new CodexDeviceAuthorization(id, userCode, TimeSpan.FromSeconds(interval), clock.GetUtcNow().AddMinutes(15));
    }

    public async Task<CodexCredentials> CompleteDeviceLoginAsync(CodexDeviceAuthorization authorization, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        await authorization.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (authorization.Completed)
                throw new CodexException(CodexFailureKind.AuthenticationRequired);
            var lifetime = authorization.ExpiresAt - clock.GetUtcNow();
            if (lifetime <= TimeSpan.Zero)
                throw new CodexException(CodexFailureKind.DeviceCodeExpired);
            using var deadline = new CancellationTokenSource(lifetime, clock);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var remaining = authorization.ExpiresAt - clock.GetUtcNow();
                    if (remaining <= TimeSpan.Zero)
                        throw new CodexException(CodexFailureKind.DeviceCodeExpired);
                    await Task.Delay(authorization.PollInterval < remaining ? authorization.PollInterval : remaining, clock, linked.Token).ConfigureAwait(false);
                    if (clock.GetUtcNow() >= authorization.ExpiresAt)
                        throw new CodexException(CodexFailureKind.DeviceCodeExpired);
                    using var request = new HttpRequestMessage(HttpMethod.Post, CodexHttp.AuthOrigin + "/api/accounts/deviceauth/token")
                    {
                        Content = JsonContent.Create(new DevicePollRequest { DeviceAuthId = authorization.DeviceAuthId, UserCode = authorization.UserCode }, CodexAuthJson.Default.DevicePollRequest)
                    };
                    using var response = await CodexHttp.SendAsync(client, request, clock, linked.Token).ConfigureAwait(false);
                    if (clock.GetUtcNow() >= authorization.ExpiresAt)
                        throw new CodexException(CodexFailureKind.DeviceCodeExpired);
                    if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
                        continue;
                    if (!response.IsSuccess)
                        throw CodexHttp.Failure(response);
                    // A returned authorization code is single-use even if the subsequent exchange is interrupted.
                    authorization.Completed = true;
                    var code = RequiredText(response.Body!.RootElement, "authorization_code");
                    var verifier = RequiredText(response.Body.RootElement, "code_verifier");
                    using var exchange = new HttpRequestMessage(HttpMethod.Post, CodexHttp.AuthOrigin + "/oauth/token")
                    {
                        Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            ["grant_type"] = "authorization_code",
                            ["client_id"] = CodexHttp.ClientId,
                            ["code"] = code,
                            ["code_verifier"] = verifier,
                            ["redirect_uri"] = CodexHttp.AuthOrigin + "/deviceauth/callback"
                        })
                    };
                    using var tokens = await CodexHttp.SendAsync(client, exchange, clock, linked.Token).ConfigureAwait(false);
                    if (!tokens.IsSuccess)
                        throw CodexHttp.Failure(tokens);
                    var parsed = ParseTokens(tokens.Body!.RootElement, null);
                    return new CodexCredentials(parsed.Access, parsed.Refresh, parsed.AccountId, parsed.ExpiresAt);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                throw new CodexException(CodexFailureKind.DeviceCodeExpired);
            }
        }
        finally { authorization.Gate.Release(); }
    }

    /// <summary>
    /// Restores a session from a stored grant by exchanging its refresh token. The workspace the
    /// provider returns must match the stored one, so a swapped record cannot silently change accounts.
    /// </summary>
    public async Task<CodexCredentials> ResumeAsync(CodexStoredGrant grant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        using var request = new HttpRequestMessage(HttpMethod.Post, CodexHttp.AuthOrigin + "/oauth/token")
        {
            Content = JsonContent.Create(new RefreshRequest
            {
                ClientId = CodexHttp.ClientId, GrantType = "refresh_token", RefreshToken = grant.RefreshToken
            }, CodexAuthJson.Default.RefreshRequest)
        };
        using var response = await CodexHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            var error = Property(response.Body?.RootElement ?? default, "error");
            var code = Text(error) ?? Text(Property(error, "code"));
            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                code is "invalid_grant" or "refresh_token_expired" or "refresh_token_reused" or "refresh_token_invalidated")
                throw new CodexException(CodexFailureKind.AuthenticationRequired, response.StatusCode);
            throw CodexHttp.Failure(response);
        }
        var parsed = ParseTokens(response.Body!.RootElement, null);
        if (!StringComparer.Ordinal.Equals(parsed.AccountId, grant.AccountId))
            throw new CodexException(CodexFailureKind.AccountMismatch);
        return new CodexCredentials(parsed.Access, parsed.Refresh, parsed.AccountId, parsed.ExpiresAt);
    }

    public async Task RefreshAsync(CodexCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var revision = credentials.Revision;
        await credentials.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var sent = false;
        try
        {
            credentials.EnsureUsable();
            if (revision != credentials.Revision)
                return;
            cancellationToken.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Post, CodexHttp.AuthOrigin + "/oauth/token")
            {
                Content = JsonContent.Create(new RefreshRequest
                {
                    ClientId = CodexHttp.ClientId,
                    GrantType = "refresh_token",
                    RefreshToken = credentials.RefreshToken
                }, CodexAuthJson.Default.RefreshRequest)
            };
            sent = true;
            using var response = await CodexHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                var error = Property(response.Body?.RootElement ?? default, "error");
                var code = Text(error) ?? Text(Property(error, "code")) ?? Text(Property(response.Body?.RootElement ?? default, "code"));
                if (response.StatusCode == HttpStatusCode.Unauthorized || code is "invalid_grant" or "refresh_token_expired" or "refresh_token_reused" or "refresh_token_invalidated")
                {
                    credentials.RequireReauthentication();
                    throw new CodexException(CodexFailureKind.AuthenticationRequired, response.StatusCode);
                }
                throw CodexHttp.Failure(response);
            }
            var parsed = ParseTokens(response.Body!.RootElement, credentials);
            credentials.Update(parsed.Access, parsed.Refresh, parsed.ExpiresAt);
        }
        catch (OperationCanceledException) when (sent)
        {
            credentials.RequireReauthentication();
            throw;
        }
        catch (CodexException error) when (sent && error.Kind is not CodexFailureKind.RateLimited)
        {
            // A failed refresh response cannot prove that the server did not rotate its grant.
            credentials.RequireReauthentication();
            throw;
        }
        finally { credentials.Gate.Release(); }
    }

    private TokenValues ParseTokens(JsonElement root, CodexCredentials? previous)
    {
        var access = RequiredText(root, "access_token");
        var refresh = Text(Property(root, "refresh_token")) ?? previous?.RefreshToken;
        if (string.IsNullOrEmpty(refresh) || !SafeHeaderValue(access) || !SafeHeaderValue(refresh))
            throw new CodexException(CodexFailureKind.InvalidResponse);
        var accessClaims = ReadClaims(access);
        var idToken = Text(Property(root, "id_token"));
        var idClaims = idToken is null ? default : ReadClaims(idToken);
        // Parity with the inspected OMP client: account context claims are recorded, not used to
        // refuse a token the provider issued. Only provider responses decide access.
        if (accessClaims.AccountId is not null && idClaims.AccountId is not null && accessClaims.AccountId != idClaims.AccountId)
            throw new CodexException(CodexFailureKind.AccountMismatch);
        var account = accessClaims.AccountId ?? idClaims.AccountId ?? previous?.AccountId;
        if (account is null || account.Length > 1024 || !SafeHeaderValue(account))
            throw new CodexException(CodexFailureKind.InvalidResponse);
        if (previous is not null && account != previous.AccountId)
            throw new CodexException(CodexFailureKind.AccountMismatch);
        DateTimeOffset? expires = accessClaims.ExpiresAt;
        var seconds = Number(Property(root, "expires_in"));
        if (seconds is > 0 && seconds < (DateTimeOffset.MaxValue - clock.GetUtcNow()).TotalSeconds)
        {
            var fromResponse = clock.GetUtcNow().AddSeconds(seconds.Value);
            expires = expires is { } jwtExpiry && jwtExpiry < fromResponse ? jwtExpiry : fromResponse;
        }
        if (expires is null || expires <= clock.GetUtcNow())
            throw new CodexException(CodexFailureKind.InvalidResponse);
        return new TokenValues(access, refresh, account, expires.Value);
    }

    private static TokenClaims ReadClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3 || parts[1].Length > 65536)
            return default;
        try
        {
            var encoded = parts[1].Replace('-', '+').Replace('_', '/');
            encoded = encoded.PadRight(encoded.Length + (4 - encoded.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(encoded), new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            var auth = Property(root, "https://api.openai.com/auth");
            var account = Text(Property(auth, "chatgpt_account_id"));
            // Parity with the inspected OMP client: only the workspace identity and expiry are read.
            // Account-context claims such as residency or FedRAMP neither refuse a token nor change routing.
            var exp = Number(Property(root, "exp"));
            DateTimeOffset? expiry = exp is >= -62135596800 and <= 253402300799 && exp == Math.Truncate(exp.Value)
                ? DateTimeOffset.FromUnixTimeSeconds((long)exp.Value) : null;
            return new TokenClaims(account, expiry);
        }
        catch (FormatException) { throw new CodexException(CodexFailureKind.InvalidResponse); }
        catch (JsonException) { throw new CodexException(CodexFailureKind.InvalidResponse); }
    }

    private static string RequiredText(JsonElement root, string name)
    {
        var value = Text(Property(root, name));
        if (string.IsNullOrEmpty(value) || value.Length > 65536)
            throw new CodexException(CodexFailureKind.InvalidResponse);
        return value;
    }

    private static bool SafeHeaderValue(string value) => value.Length is > 0 and <= 65536 && value.All(c => c is >= '!' and <= '~');
    private readonly record struct TokenClaims(string? AccountId, DateTimeOffset? ExpiresAt);
    private sealed class TokenValues(string access, string refresh, string accountId, DateTimeOffset expiresAt)
    {
        internal string Access { get; } = access;
        internal string Refresh { get; } = refresh;
        internal string AccountId { get; } = accountId;
        internal DateTimeOffset ExpiresAt { get; } = expiresAt;
    }
}
