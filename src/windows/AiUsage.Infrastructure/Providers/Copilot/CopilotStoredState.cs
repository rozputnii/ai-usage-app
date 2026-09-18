using AiUsage.Core.Usage;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Copilot;

internal sealed record CopilotStoredState
{
    public int Version { get; init; } = 1;
    public Guid Revision { get; init; }
    public Guid? ParentRevision { get; init; }
    public required string AccountId { get; init; }
    public required string AccessToken { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool NeedsReauthentication { get; init; }
    public QuotaSnapshot? CachedQuota { get; init; }
    public override string ToString() => "CopilotStoredState (redacted)";
}

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32)]
[JsonSerializable(typeof(CopilotStoredState))]
internal partial class CopilotStateJson : JsonSerializerContext;
