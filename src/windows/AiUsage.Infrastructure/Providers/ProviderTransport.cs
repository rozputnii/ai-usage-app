using AiUsage.Core.Usage;
using System.Net;

namespace AiUsage.Infrastructure.Providers;

internal static class ProviderTransport
{
    internal static void ConfigureClient(HttpClient client) => client.Timeout = Timeout.InfiniteTimeSpan;

    internal static HttpMessageHandler CreateHandler(ProviderTransportOptions options) => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = options.PooledConnectionLifetime
    };

    internal static async Task<ProviderHttpResponse> SendAsync(HttpClient client, HttpRequestMessage request,
        TimeProvider clock, CancellationToken cancellationToken, ProviderTransportOptions? options = null)
    {
        try { return await ProviderHttp.SendAsync(client, request, clock, cancellationToken, options).ConfigureAwait(false); }
        catch (ProviderHttpException error)
        {
            throw new ProviderException(error.Kind switch
            {
                TransportFailure.InvalidResponse => ProviderFailureKind.InvalidResponse,
                TransportFailure.Timeout => ProviderFailureKind.Timeout,
                TransportFailure.NetworkFailure => ProviderFailureKind.NetworkFailure,
                _ => throw new InvalidOperationException("Unknown transport failure.")
            }, error.StatusCode);
        }
    }

    internal static ProviderException Failure(ProviderHttpResponse response) => new(response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => ProviderFailureKind.AuthenticationRequired,
        HttpStatusCode.Forbidden => ProviderFailureKind.AccessDenied,
        HttpStatusCode.TooManyRequests => ProviderFailureKind.RateLimited,
        >= HttpStatusCode.InternalServerError => ProviderFailureKind.ProviderUnavailable,
        _ => ProviderFailureKind.RequestRejected
    }, response.StatusCode, response.RetryAfter);
}
