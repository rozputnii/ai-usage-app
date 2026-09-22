namespace AiUsage.Infrastructure.Providers.Codex;

internal static class CodexHttp
{
    internal const string ClientId = "app_EMoamEEZ73f0CkXaXp7hrann";
    internal const string AuthOrigin = "https://auth.openai.com";
    internal const string UsageUrl = "https://chatgpt.com/backend-api/wham/usage";

    // Retain Codex's protocol exception metadata while sharing transport classifications.
    internal static async Task<ProviderHttpResponse> SendAsync(HttpClient client, HttpRequestMessage request,
        TimeProvider clock, CancellationToken cancellationToken, ProviderTransportOptions? options = null)
    {
        try { return await ProviderTransport.SendAsync(client, request, clock, cancellationToken, options).ConfigureAwait(false); }
        catch (ProviderException error) { throw Translate(error); }
    }

    internal static CodexException Failure(ProviderHttpResponse response) => Translate(ProviderTransport.Failure(response));

    private static CodexException Translate(ProviderException error) => new(error.Kind, error.StatusCode, error.RetryAfter);
}
