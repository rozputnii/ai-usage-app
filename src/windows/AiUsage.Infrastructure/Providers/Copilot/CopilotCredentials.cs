using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>OMP's GitHub OAuth grant, not a Copilot inference token or a CLI import.</summary>
public sealed class CopilotCredentials
{
    internal CopilotCredentials(string accessToken, string accountId, DateTimeOffset? expiresAt = null)
    { AccessToken = accessToken; AccountId = accountId; ExpiresAt = expiresAt; }
    internal string AccessToken { get; }
    [JsonIgnore] public string AccountId { get; }
    [JsonIgnore] public DateTimeOffset? ExpiresAt { get; }
    public override string ToString() => "CopilotCredentials (redacted)";
}
