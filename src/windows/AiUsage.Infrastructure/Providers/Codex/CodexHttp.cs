using System.Net;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Codex;

internal sealed class CodexHttpResponse(HttpStatusCode statusCode, JsonDocument? body, TimeSpan? retryAfter) : IDisposable
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public JsonDocument? Body { get; } = body;
    public TimeSpan? RetryAfter { get; } = retryAfter;
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
    public void Dispose() => Body?.Dispose();
}

internal static class CodexHttp
{
    internal const string ClientId = "app_EMoamEEZ73f0CkXaXp7hrann";
    internal const string AuthOrigin = "https://auth.openai.com";
    internal const string UsageUrl = "https://chatgpt.com/backend-api/wham/usage";
    private const long MaximumResponseBytes = 1024 * 1024;

    internal static async Task<CodexHttpResponse> SendAsync(
        HttpClient client, HttpRequestMessage request, TimeProvider clock, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        request.Headers.UserAgent.ParseAdd("AiUsage/0.1");
        request.Headers.Accept.ParseAdd("application/json");
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter is null && response.Headers.RetryAfter?.Date is { } date)
                retryAfter = date - clock.GetUtcNow();
            if (retryAfter < TimeSpan.Zero)
                retryAfter = TimeSpan.Zero;
            if (response.Content.Headers.ContentLength > MaximumResponseBytes)
                throw new CodexException(CodexFailureKind.InvalidResponse, response.StatusCode);
            await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, timeout.Token).ConfigureAwait(false);
            JsonDocument? body = null;
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                body = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 32 }, timeout.Token).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                if (response.IsSuccessStatusCode)
                    throw new CodexException(CodexFailureKind.InvalidResponse, response.StatusCode);
            }
            return new CodexHttpResponse(response.StatusCode, body, retryAfter);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CodexException(CodexFailureKind.Timeout);
        }
        catch (HttpRequestException)
        {
            throw new CodexException(CodexFailureKind.NetworkFailure);
        }
        catch (IOException)
        {
            throw new CodexException(CodexFailureKind.NetworkFailure);
        }
    }

    internal static CodexException Failure(CodexHttpResponse response) => new(
        response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => CodexFailureKind.AuthenticationRequired,
            HttpStatusCode.Forbidden => CodexFailureKind.AccessDenied,
            HttpStatusCode.TooManyRequests => CodexFailureKind.RateLimited,
            >= HttpStatusCode.InternalServerError => CodexFailureKind.ProviderUnavailable,
            _ => CodexFailureKind.RequestRejected
        }, response.StatusCode, response.RetryAfter);
}
