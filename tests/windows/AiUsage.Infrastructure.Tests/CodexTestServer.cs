using System.Net;
using System.Text;
using System.Text.Json;
using AiUsage.Infrastructure.Providers.Codex;

namespace AiUsage.Infrastructure.Tests;

internal sealed class CodexTestServer(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    internal int Calls { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return respond(request, cancellationToken);
    }

    internal static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    internal static CodexCredentials Credentials() => new("synthetic-access", "synthetic-refresh", "synthetic-workspace", Clock.Now.AddHours(1));

    internal static string AccessToken(string account = "synthetic-workspace", string? residency = null, bool fedRamp = false) => Jwt(new Dictionary<string, object?>
    {
        ["exp"] = Clock.Now.AddHours(1).ToUnixTimeSeconds(),
        ["https://api.openai.com/auth"] = new Dictionary<string, object?>
        {
            ["chatgpt_account_id"] = account,
            ["chatgpt_compute_residency"] = residency,
            ["chatgpt_account_is_fedramp"] = fedRamp
        }
    });

    internal static string Jwt(object claims) => "synthetic-header." + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(claims)).TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".synthetic-signature";

    internal static string Tokens(string account = "synthetic-workspace", string? refresh = "synthetic-rotated", string? residency = null, bool fedRamp = false) =>
        JsonSerializer.Serialize(new { access_token = AccessToken(account, residency, fedRamp), refresh_token = refresh, expires_in = 3600 });

    internal sealed class Clock : TimeProvider
    {
        internal static readonly DateTimeOffset Now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        internal DateTimeOffset Current { get; set; } = Now;
        public override DateTimeOffset GetUtcNow() => Current;
    }
}
