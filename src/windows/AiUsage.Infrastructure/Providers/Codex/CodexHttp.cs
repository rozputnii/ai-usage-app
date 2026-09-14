using AiUsage.Core.Providers.Codex;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Codex;

internal static class CodexHttp
{
    internal const string ClientId = "app_EMoamEEZ73f0CkXaXp7hrann";
    internal const string AuthOrigin = "https://auth.openai.com";
    internal const string UsageUrl = "https://chatgpt.com/backend-api/wham/usage";

    internal static async Task<ProviderHttpResponse> SendAsync(
        HttpClient client, HttpRequestMessage request, TimeProvider clock, CancellationToken cancellationToken)
    {
        try { return await ProviderHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false); }
        catch (ProviderHttpException error)
        {
            throw new CodexException(error.Kind switch
            {
                TransportFailure.InvalidResponse => CodexFailureKind.InvalidResponse,
                TransportFailure.Timeout => CodexFailureKind.Timeout,
                _ => CodexFailureKind.NetworkFailure
            }, error.StatusCode);
        }
    }

    internal static CodexException Failure(ProviderHttpResponse response) => new(
        response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => CodexFailureKind.AuthenticationRequired,
            HttpStatusCode.Forbidden => CodexFailureKind.AccessDenied,
            HttpStatusCode.TooManyRequests => CodexFailureKind.RateLimited,
            >= HttpStatusCode.InternalServerError => CodexFailureKind.ProviderUnavailable,
            _ => CodexFailureKind.RequestRejected
        }, response.StatusCode, response.RetryAfter);
}
