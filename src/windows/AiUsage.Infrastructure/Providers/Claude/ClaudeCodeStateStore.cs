using System.Text.Json.Serialization;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Claude;

// The bound sign-in and the last reading share one protected generation, as for the browser method.
internal sealed record ClaudeCodeStoredState
{
    public int Version { get; init; } = 1;
    public Guid Revision { get; init; }
    public Guid? ParentRevision { get; init; }
    public required ClaudeCodeIdentity Identity { get; init; }
    public ClaudeQuotaReading? CachedQuota { get; init; }
    public override string ToString() => "ClaudeCodeStoredState (redacted)";
}

[JsonSourceGenerationOptions(Converters = [typeof(QuotaQuantityConverter), typeof(QuotaLimitValueConverter)], UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32)]
[JsonSerializable(typeof(ClaudeCodeStoredState))]
internal partial class ClaudeCodeStateJson : JsonSerializerContext;

/// <summary>Provider policy over the shared app-owned DPAPI state lease.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ClaudeCodeStateStore(string ownedDirectory, Action? afterStage = null, IDiagnosticSink? diagnostics = null)
{
    private static readonly ProviderStatePolicy<ClaudeCodeStoredState> Policy = new(
        "claude-code.state", "AiUsage.ClaudeCode.State.v1"u8.ToArray(),
        ClaudeCodeStateJson.Default.ClaudeCodeStoredState, state => state.Revision, state => state.ParentRevision,
        (state, parent) => state with { Revision = Guid.NewGuid(), ParentRevision = parent }, Validate);

    private readonly string directory = Path.GetFullPath(ownedDirectory);

    internal Task<ProviderStateLease<ClaudeCodeStoredState>> AcquireAsync(CancellationToken cancellationToken) =>
        Policy.AcquireAsync(directory, afterStage, diagnostics, cancellationToken);

    private static void Validate(ClaudeCodeStoredState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision || state.Identity is null ||
            !ClaudeCodeIdentity.Safe(state.Identity.Email) || !ClaudeCodeIdentity.Safe(state.Identity.OrganizationId))
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        ClaudeStateStore.ValidateReading(state.CachedQuota);
    }
}
