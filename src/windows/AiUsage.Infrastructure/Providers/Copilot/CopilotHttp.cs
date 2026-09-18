using AiUsage.Core.Usage;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Copilot;

internal static class CopilotHttp
{
    // Source: OMP v18.2.6. Reuse is owner-selected, not provider-approved.
    internal const string ClientId = "Ov23li8tweQw6odWQebz";
    internal const string DeviceUrl = "https://github.com/login/device/code";
    internal const string TokenUrl = "https://github.com/login/oauth/access_token";
    internal const string IdentityUrl = "https://api.github.com/user";
    internal const string UsageUrl = "https://api.github.com/copilot_internal/user";
    internal static async Task<ProviderHttpResponse> SendAsync(HttpClient client, HttpRequestMessage request,
        TimeProvider clock, CancellationToken token)
    {
        try { return await ProviderHttp.SendAsync(client, request, clock, token).ConfigureAwait(false); }
        catch (ProviderHttpException error)
        {
            throw new CopilotException(error.Kind switch
            {
                TransportFailure.InvalidResponse => ProviderFailureKind.InvalidResponse,
                TransportFailure.Timeout => ProviderFailureKind.Timeout,
                _ => ProviderFailureKind.NetworkFailure
            }, error.StatusCode);
        }
    }
    internal static CopilotException Failure(ProviderHttpResponse response) => new(response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => ProviderFailureKind.AuthenticationRequired,
        HttpStatusCode.Forbidden => ProviderFailureKind.AccessDenied,
        HttpStatusCode.TooManyRequests => ProviderFailureKind.RateLimited,
        >= HttpStatusCode.InternalServerError => ProviderFailureKind.ProviderUnavailable,
        _ => ProviderFailureKind.RequestRejected
    }, response.StatusCode, response.RetryAfter);
}
