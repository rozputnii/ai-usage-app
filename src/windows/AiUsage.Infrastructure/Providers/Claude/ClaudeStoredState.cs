using AiUsage.Core.Providers.Claude;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Claude;

// Grant and last quota share one protected generation, so a cache cannot cross an account switch.
internal sealed record ClaudeStoredState
{
    public int Version { get; init; } = 1;
    public Guid Revision { get; init; }
    public Guid? ParentRevision { get; init; }
    public required ClaudeIdentity Identity { get; init; }
    public required string RefreshToken { get; init; }
    public bool NeedsReauthentication { get; init; }
    public ClaudeQuotaReading? CachedQuota { get; init; }
    public override string ToString() => "ClaudeStoredState (redacted)";
}

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32)]
[JsonSerializable(typeof(ClaudeStoredState))]
internal partial class ClaudeStateJson : JsonSerializerContext;
