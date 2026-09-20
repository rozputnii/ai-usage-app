using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// One app-owned Google grant plus the workspace it was discovered against. No source CLI
/// credential is read, and the access token never leaves this process.
/// </summary>
public sealed class AntigravityCredentials
{
    internal AntigravityCredentials(string accessToken, string refreshToken, string accountId, string projectId,
        string? tier, DateTimeOffset refreshAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccountId = accountId;
        ProjectId = projectId;
        Tier = tier;
        RefreshAt = refreshAt;
    }

    internal string AccessToken { get; }
    internal string RefreshToken { get; }
    /// <summary>The opaque Google subject the grant is bound to; a changed subject is a mismatch, not a rename.</summary>
    [JsonIgnore] public string AccountId { get; }
    [JsonIgnore] public string ProjectId { get; }
    /// <summary>Tier as reported by the last successful discovery; it is never inferred from quota.</summary>
    [JsonIgnore] public string? Tier { get; }
    [JsonIgnore] public DateTimeOffset RefreshAt { get; }
    public override string ToString() => "AntigravityCredentials (redacted)";
}

/// <summary>
/// The project and tier a discovery resolved. Discovery reads first and provisions the free tier
/// only when the account has none and the provider offers it; see AntigravityQuotaClient.
/// </summary>
internal sealed record AntigravityWorkspace(string ProjectId, string? Tier);

/// <summary>
/// An exchanged or renewed Google grant before a workspace is known. Kept separate so the token
/// endpoint and the Cloud Code control plane stay independently testable.
/// </summary>
internal sealed record AntigravityGrant(string AccessToken, string RefreshToken, string AccountId, DateTimeOffset RefreshAt)
{
    public override string ToString() => "AntigravityGrant (redacted)";
}
