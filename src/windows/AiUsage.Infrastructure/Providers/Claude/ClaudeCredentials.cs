using AiUsage.Core.Providers.Claude;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>Immutable, in-memory credentials. Only the session owns refresh and durable cutover.</summary>
public sealed class ClaudeCredentials
{
    internal ClaudeCredentials(string accessToken, string refreshToken, ClaudeIdentity identity, DateTimeOffset refreshAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        Identity = identity;
        RefreshAt = refreshAt;
    }

    internal string AccessToken { get; }
    internal string RefreshToken { get; }
    [JsonIgnore] public ClaudeIdentity Identity { get; }
    [JsonIgnore] public DateTimeOffset RefreshAt { get; }
    public override string ToString() => "ClaudeCredentials (redacted)";
}
