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
            throw new ProviderException(ProviderFailureKind.RegistrationUnavailable);
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
    internal const string OnboardUserUrl = CloudCodeEndpoint + "/v1internal:onboardUser";
    internal const string OperationsUrl = CloudCodeEndpoint + "/v1internal";
    internal const string QuotaSummaryUrl = CloudCodeEndpoint + "/v1internal:retrieveUserQuotaSummary";

    /// <summary>
    /// The identity the Cloud Code Assist control plane is given. It is the real Antigravity desktop
    /// client's string, not AI Usage's own, because the provider answered `UNSUPPORTED_CLIENT` and
    /// refused to provision a workspace for a truthfully identified client on 2026-09-20. The owner
    /// directed this deviation after being shown that it presents another application's identity and
    /// contradicts the rule kept elsewhere in docs/providers. It applies only to the control plane;
    /// Google's OAuth and userinfo endpoints still receive AI Usage's own identity.
    ///
    /// The version is the pinned reference captured by OMP, overridable for this device because the
    /// backend gates on it and a pinned value ages out. The os/arch/changelist stay as captured.
    /// </summary>
    internal static string ClientIdentity =>
        $"antigravity/hub/{Version} (aidev_client; os_type=darwin; arch=arm64; cl=963137146)";
    internal const string VersionVariable = "AIU_ANTIGRAVITY_CLIENT_VERSION";
    private const string DefaultVersion = "2.8.0";
    private static string Version =>
        Environment.GetEnvironmentVariable(VersionVariable) is { Length: > 0 and <= 32 } version &&
        version.All(character => char.IsAsciiDigit(character) || character == '.') ? version : DefaultVersion;

    /// <summary>The scopes the inspected registration requests; a smaller quota-only set is not established.</summary>
    internal const string Scopes = "https://www.googleapis.com/auth/cloud-platform https://www.googleapis.com/auth/userinfo.email " +
        "https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/cclog https://www.googleapis.com/auth/experimentsandconfigs";
    internal const string RequiredScope = "https://www.googleapis.com/auth/cloud-platform";

}
