using AiUsage.Core.Providers.Codex;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Codex;

internal static class CodexHttp
{
    internal const string ClientId = "app_EMoamEEZ73f0CkXaXp7hrann";
    internal const string AuthOrigin = "https://auth.openai.com";
    internal const string UsageUrl = "https://chatgpt.com/backend-api/wham/usage";

    // Temporary compatibility adapter until T-03 removes the Codex-only contract.
    internal static async Task<ProviderHttpResponse> SendAsync(HttpClient client, HttpRequestMessage request,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        try { return await ProviderTransport.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false); }
        catch (ProviderException error) { throw Translate(error); }
    }

    internal static CodexException Failure(ProviderHttpResponse response) => Translate(ProviderTransport.Failure(response));

    private static CodexException Translate(ProviderException error) => new(error.Kind switch
    {
        AiUsage.Core.Usage.ProviderFailureKind.InvalidResponse => CodexFailureKind.InvalidResponse,
        AiUsage.Core.Usage.ProviderFailureKind.Timeout => CodexFailureKind.Timeout,
        AiUsage.Core.Usage.ProviderFailureKind.NetworkFailure => CodexFailureKind.NetworkFailure,
        AiUsage.Core.Usage.ProviderFailureKind.AuthenticationRequired => CodexFailureKind.AuthenticationRequired,
        AiUsage.Core.Usage.ProviderFailureKind.AccessDenied => CodexFailureKind.AccessDenied,
        AiUsage.Core.Usage.ProviderFailureKind.RateLimited => CodexFailureKind.RateLimited,
        AiUsage.Core.Usage.ProviderFailureKind.ProviderUnavailable => CodexFailureKind.ProviderUnavailable,
        AiUsage.Core.Usage.ProviderFailureKind.RequestRejected => CodexFailureKind.RequestRejected,
        _ => throw new InvalidOperationException("Unexpected transport classification.")
    }, error.StatusCode, error.RetryAfter);
}
