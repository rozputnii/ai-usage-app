using AiUsage.Core.Usage;
using System.Net;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// The OAuth client this device uses for Antigravity. AI Usage owns no Antigravity registration and
/// never vendors another application's client, so the operator supplies one through the environment.
/// Antigravity's published terms restrict third-party access; see docs/providers/antigravity.md.
/// </summary>
internal sealed record AntigravityRegistration(string ClientId, string ClientSecret)
{
    internal const string ClientIdVariable = "AIU_ANTIGRAVITY_CLIENT_ID";
    internal const string ClientSecretVariable = "AIU_ANTIGRAVITY_CLIENT_SECRET";

    internal static AntigravityRegistration FromEnvironment()
    {
        var id = Environment.GetEnvironmentVariable(ClientIdVariable);
        var secret = Environment.GetEnvironmentVariable(ClientSecretVariable);
        // A partial or unusable registration is reported as unconfigured, never sent to the provider.
        if (!AntigravityAuthClient.SafeIdentity(id) || !AntigravityAuthClient.SafeToken(secret))
            throw new AntigravityException(ProviderFailureKind.RegistrationUnavailable);
        return new(id!, secret!);
    }

    public override string ToString() => "AntigravityRegistration (redacted)";
}

internal static class AntigravityHttp
{
    internal const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    internal const string TokenUrl = "https://oauth2.googleapis.com/token";
    internal const string IdentityUrl = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";
    internal const string CloudCodeEndpoint = "https://daily-cloudcode-pa.googleapis.com";
    internal const string LoadCodeAssistUrl = CloudCodeEndpoint + "/v1internal:loadCodeAssist";
    internal const string QuotaSummaryUrl = CloudCodeEndpoint + "/v1internal:retrieveUserQuotaSummary";

    /// <summary>The scopes the inspected registration requests; a smaller quota-only set is not established.</summary>
    internal const string Scopes = "https://www.googleapis.com/auth/cloud-platform https://www.googleapis.com/auth/userinfo.email " +
        "https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/cclog https://www.googleapis.com/auth/experimentsandconfigs";
    internal const string RequiredScope = "https://www.googleapis.com/auth/cloud-platform";

    internal static async Task<ProviderHttpResponse> SendAsync(HttpClient client, HttpRequestMessage request,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        try { return await ProviderHttp.SendAsync(client, request, clock, cancellationToken).ConfigureAwait(false); }
        catch (ProviderHttpException error)
        {
            throw new AntigravityException(error.Kind switch
            {
                TransportFailure.InvalidResponse => ProviderFailureKind.InvalidResponse,
                TransportFailure.Timeout => ProviderFailureKind.Timeout,
                _ => ProviderFailureKind.NetworkFailure
            }, error.StatusCode);
        }
    }

    internal static AntigravityException Failure(ProviderHttpResponse response) => new(response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => ProviderFailureKind.AuthenticationRequired,
        HttpStatusCode.Forbidden => ProviderFailureKind.AccessDenied,
        HttpStatusCode.TooManyRequests => ProviderFailureKind.RateLimited,
        >= HttpStatusCode.InternalServerError => ProviderFailureKind.ProviderUnavailable,
        _ => ProviderFailureKind.RequestRejected
    }, response.StatusCode, response.RetryAfter);
}
