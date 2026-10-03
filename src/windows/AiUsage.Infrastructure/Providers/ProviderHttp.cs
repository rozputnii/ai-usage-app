using System.Net;
using System.Text.Json;
using AiUsage.Infrastructure.Diagnostics;

namespace AiUsage.Infrastructure.Providers;

internal sealed class ProviderHttpResponse(HttpStatusCode statusCode, JsonDocument? body, TimeSpan? retryAfter) : IDisposable
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public JsonDocument? Body { get; } = body;
    public TimeSpan? RetryAfter { get; } = retryAfter;
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
    public void Dispose() => Body?.Dispose();
}

internal static class ProviderHttp
{
    private const long MaximumResponseBytes = 1024 * 1024;

    internal static async Task<ProviderHttpResponse> SendAsync(
        HttpClient client, HttpRequestMessage request, TimeProvider clock, CancellationToken cancellationToken, ProviderTransportOptions? options = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter((options ?? ProviderTransportOptions.Default).RequestDeadline);
        // Truthful by default. A caller that has already set an identity owns it, so this never
        // appends a second product token to a header a provider may parse strictly.
        if (!request.Headers.Contains("User-Agent"))
            request.Headers.UserAgent.ParseAdd("AiUsage/0.1");
        request.Headers.Accept.ParseAdd("application/json");
        var capture = options?.Diagnostics is { } diagnostics ? new ProviderCapture(diagnostics, request) : null;
        HttpResponseMessage? response = null;
        long? observed = null;
        TimeSpan? retryAfter = null;
        var state = "transport-incomplete";
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter is null && response.Headers.RetryAfter?.Date is { } date)
                retryAfter = date - clock.GetUtcNow();
            if (retryAfter < TimeSpan.Zero)
                retryAfter = TimeSpan.Zero;
            if (response.Content.Headers.ContentLength > MaximumResponseBytes)
            {
                state = "oversized";
                throw new ProviderHttpException(TransportFailure.InvalidResponse, response.StatusCode);
            }
            await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, timeout.Token).ConfigureAwait(false);
            JsonDocument? body = null;
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                observed = stream.Length;
                state = observed == 0 ? "empty" : "malformed";
                body = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 32 }, timeout.Token).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                if (response.IsSuccessStatusCode)
                    throw new ProviderHttpException(TransportFailure.InvalidResponse, response.StatusCode);
            }
            capture?.Complete("complete-sanitized", response, body, observed, retryAfter);
            return new ProviderHttpResponse(response.StatusCode, body, retryAfter);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            state = "timeout";
            throw new ProviderHttpException(TransportFailure.Timeout);
        }
        catch (OperationCanceledException) { state = "cancelled"; throw; }
        catch (HttpRequestException)
        {
            throw new ProviderHttpException(TransportFailure.NetworkFailure);
        }
        catch (IOException)
        {
            throw new ProviderHttpException(TransportFailure.NetworkFailure);
        }
        finally
        {
            capture?.Complete(state, response, null, observed, retryAfter);
            response?.Dispose();
        }
    }

}

internal enum TransportFailure { InvalidResponse, Timeout, NetworkFailure }

internal sealed class ProviderHttpException(TransportFailure kind, HttpStatusCode? statusCode = null)
    : Exception("Provider transport failed.")
{
    internal TransportFailure Kind { get; } = kind;
    internal HttpStatusCode? StatusCode { get; } = statusCode;
}
