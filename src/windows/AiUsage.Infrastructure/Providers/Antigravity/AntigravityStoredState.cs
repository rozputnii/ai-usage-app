using AiUsage.Core.Usage;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// Grant, discovered workspace and last quota share one protected generation, so a cached reading
/// can never cross an account or project switch. The access token is memory-only.
/// </summary>
internal sealed record AntigravityStoredState
{
    public int Version { get; init; } = 1;
    public Guid Revision { get; init; }
    public Guid? ParentRevision { get; init; }
    public required string AccountId { get; init; }
    public required string RefreshToken { get; init; }
    public required string ProjectId { get; init; }
    public string? Tier { get; init; }
    public bool NeedsReauthentication { get; init; }
    public QuotaSnapshot? CachedQuota { get; init; }
    public override string ToString() => "AntigravityStoredState (redacted)";
}

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32)]
[JsonSerializable(typeof(AntigravityStoredState))]
internal partial class AntigravityStateJson : JsonSerializerContext;
