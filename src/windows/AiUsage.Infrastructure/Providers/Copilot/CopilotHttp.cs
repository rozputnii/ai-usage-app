using AiUsage.Core.Providers.Copilot;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Copilot;

internal static class CopilotHttp
{
    /// <summary>OpenCode's public OAuth App, reused as OMP v18.2.2 does. Not an AI Usage registration.</summary>
    internal const string ClientId = "Ov23li8tweQw6odWQebz";
    internal const string Scope = "read:user";
    internal const string DeviceCodeUrl = "https://github.com/login/device/code";
    internal const string TokenUrl = "https://github.com/login/oauth/access_token";
    internal const string ApiBase = "https://api.github.com";
    internal const string ApiVersion = "2022-11-28";

    internal static async Task<ProviderHttpResponse> SendAsync(HttpClient client, HttpRequestMessage request,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        try { return await ProviderHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false); }
        catch (ProviderHttpException error)
        {
            throw new CopilotException(error.Kind switch
            {
                TransportFailure.InvalidResponse => CopilotFailureKind.InvalidResponse,
                TransportFailure.Timeout => CopilotFailureKind.Timeout,
                _ => CopilotFailureKind.NetworkFailure
            }, error.StatusCode);
        }
    }

    internal static HttpRequestMessage ApiGet(string path, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ApiBase + path);
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", ApiVersion);
        return request;
    }

    /// <summary>GitHub reports secondary rate limits as 403 with Retry-After.</summary>
    internal static CopilotException Failure(ProviderHttpResponse response) => new(
        response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => CopilotFailureKind.AuthenticationRequired,
            HttpStatusCode.TooManyRequests => CopilotFailureKind.RateLimited,
            HttpStatusCode.Forbidden when response.RetryAfter is not null => CopilotFailureKind.RateLimited,
            HttpStatusCode.Forbidden => CopilotFailureKind.AccessDenied,
            HttpStatusCode.NotFound => CopilotFailureKind.ReportUnavailable,
            >= HttpStatusCode.InternalServerError => CopilotFailureKind.ProviderUnavailable,
            _ => CopilotFailureKind.RequestRejected
        }, response.StatusCode, response.RetryAfter);
}
