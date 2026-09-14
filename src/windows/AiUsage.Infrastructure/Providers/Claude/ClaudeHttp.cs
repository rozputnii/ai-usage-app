using AiUsage.Core.Providers.Claude;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Claude;

internal static class ClaudeHttp
{
    internal const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    internal const string TokenUrl = "https://api.anthropic.com/v1/oauth/token";
    internal const string UsageUrl = "https://api.anthropic.com/api/oauth/usage";

    internal static async Task<ProviderHttpResponse> SendAsync(HttpClient client, HttpRequestMessage request,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        try { return await ProviderHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false); }
        catch (ProviderHttpException error)
        {
            throw new ClaudeException(error.Kind switch
            {
                TransportFailure.InvalidResponse => ClaudeFailureKind.InvalidResponse,
                TransportFailure.Timeout => ClaudeFailureKind.Timeout,
                _ => ClaudeFailureKind.NetworkFailure
            }, error.StatusCode);
        }
    }

    internal static ClaudeException Failure(ProviderHttpResponse response) => new(
        response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => ClaudeFailureKind.AuthenticationRequired,
            HttpStatusCode.Forbidden => ClaudeFailureKind.AccessDenied,
            HttpStatusCode.TooManyRequests => ClaudeFailureKind.RateLimited,
            >= HttpStatusCode.InternalServerError => ClaudeFailureKind.ProviderUnavailable,
            _ => ClaudeFailureKind.RequestRejected
        }, response.StatusCode, response.RetryAfter);
}
