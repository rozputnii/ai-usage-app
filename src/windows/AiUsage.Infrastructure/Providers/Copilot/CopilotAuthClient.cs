using AiUsage.Core.Usage;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Copilot;

internal sealed class CopilotAuthClient
{
    private readonly ProviderTransportOptions transport;
    private readonly HttpClient client;
    private readonly TimeProvider clock;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    public CopilotAuthClient(HttpClient client, TimeProvider? timeProvider = null, ProviderTransportOptions? options = null)
        : this(client, timeProvider ?? TimeProvider.System, null, options) { }
    internal CopilotAuthClient(HttpClient client, TimeProvider clock, Func<TimeSpan, CancellationToken, Task>? delay, ProviderTransportOptions? options = null)
    {
        transport = options ?? ProviderTransportOptions.Default;
        this.client = client; this.clock = clock;
        this.delay = delay ?? ((duration, token) => Task.Delay(duration, clock, token));
    }

    public async Task<CopilotCredentials> LoginAsync(Action<AuthorizationChallenge> authorize, CancellationToken cancellationToken = default)
    {
        using var start = Post(CopilotHttp.DeviceUrl, new() { ["client_id"] = CopilotHttp.ClientId, ["scope"] = "read:user" });
        using var device = await ProviderTransport.SendAsync(client, start, clock, cancellationToken, transport).ConfigureAwait(false);
        if (!device.IsSuccess) throw ProviderTransport.Failure(device);
        var body = device.Body!.RootElement;
        var code = Text(body, "device_code");
        var userCode = Text(body, "user_code");
        if (!SafeToken(code) || userCode is not { Length: > 0 and <= 32 } || userCode.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-') ||
            Text(body, "verification_uri") != "https://github.com/login/device" ||
            !Seconds(body, "interval", out var interval) || !Seconds(body, "expires_in", out var expires))
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        var deadline = clock.GetUtcNow().AddSeconds(expires);
        cancellationToken.ThrowIfCancellationRequested();
        authorize(new(new Uri("https://github.com/login/device"), userCode, deadline));
        var multiplier = 1.2; // OMP's conservative initial device polling interval.
        while (clock.GetUtcNow() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = deadline - clock.GetUtcNow();
            var wait = TimeSpan.FromSeconds(interval * multiplier);
            await delay(wait < remaining ? wait : remaining, cancellationToken).ConfigureAwait(false);
            if (clock.GetUtcNow() >= deadline) break;
            using var poll = Post(CopilotHttp.TokenUrl, new()
            {
                ["client_id"] = CopilotHttp.ClientId, ["device_code"] = code!,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            });
            using var response = await ProviderTransport.SendAsync(client, poll, clock, cancellationToken, transport).ConfigureAwait(false);
            if (!response.IsSuccess) throw ProviderTransport.Failure(response);
            var root = response.Body!.RootElement;
            if (Text(root, "access_token") is { } access)
            {
                if (!SafeToken(access) || (root.TryGetProperty("token_type", out _) &&
                    !string.Equals(Text(root, "token_type"), "bearer", StringComparison.OrdinalIgnoreCase)))
                    throw new ProviderException(ProviderFailureKind.InvalidResponse);
                DateTimeOffset? expiresAt = null;
                if (root.TryGetProperty("expires_in", out _))
                {
                    if (!Seconds(root, "expires_in", out var lifetime)) throw new ProviderException(ProviderFailureKind.InvalidResponse);
                    expiresAt = clock.GetUtcNow().AddSeconds(lifetime);
                }
                // OMP performs no OAuth refresh. Do not invent renewal from its alias named 'refresh'.
                var identity = await GetIdentityAsync(access, cancellationToken).ConfigureAwait(false);
                return new(access, identity, expiresAt);
            }
            switch (Text(root, "error"))
            {
                case "authorization_pending": break;
                case "slow_down":
                    interval = Seconds(root, "interval", out var next) ? Math.Max(interval + 5, next) : interval + 5;
                    multiplier = 1.4;
                    break;
                case "access_denied": throw new ProviderException(ProviderFailureKind.AccessDenied);
                case "expired_token": throw new ProviderException(ProviderFailureKind.DeviceCodeExpired);
                case "device_flow_disabled": throw new ProviderException(ProviderFailureKind.DeviceLoginUnavailable);
                default: throw new ProviderException(ProviderFailureKind.InvalidResponse);
            }
        }
        throw new ProviderException(ProviderFailureKind.DeviceCodeExpired);
    }

    internal async Task<string> GetIdentityAsync(string accessToken, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, CopilotHttp.IdentityUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await ProviderTransport.SendAsync(client, request, clock, token, transport).ConfigureAwait(false);
        if (!response.IsSuccess) throw ProviderTransport.Failure(response);
        var root = response.Body!.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out var value) || value <= 0)
            throw new ProviderException(ProviderFailureKind.InvalidResponse);
        return value.ToString(CultureInfo.InvariantCulture);
    }
    private static HttpRequestMessage Post(string url, Dictionary<string, string> values) =>
        new(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(values) };
    internal static string? Text(JsonElement root, string key) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var value) &&
        value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static bool Seconds(JsonElement root, string key, out int seconds)
    {
        seconds = 0;
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out seconds) && seconds is > 0 and <= 31_536_000;
    }
    internal static bool SafeToken(string? value) => value is { Length: > 0 and <= 32768 } && value.All(c => c is >= '!' and <= '~');
    internal static bool SafeIdentity(string? value) => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0;
}
