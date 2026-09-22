using AiUsage.Core.Usage;
namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>Provider policy over the shared app-owned DPAPI state lease.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ClaudeStateStore
{
    private readonly string directory;
    private readonly Action? afterStage;
    private static readonly ProviderStatePolicy<ClaudeStoredState> Policy = new(
        "claude.state", "AiUsage.Claude.State.v1"u8.ToArray(),
        ClaudeStateJson.Default.ClaudeStoredState, state => state.Revision, state => state.ParentRevision,
        (state, parent) => state with { Version = 1, Revision = Guid.NewGuid(), ParentRevision = parent }, Validate);

    public ClaudeStateStore(string ownedDirectory) : this(ownedDirectory, null) { }
    internal ClaudeStateStore(string ownedDirectory, Action? afterStage)
    {
        directory = Path.GetFullPath(ownedDirectory);
        this.afterStage = afterStage;
    }

    internal Task<ProviderStateLease<ClaudeStoredState>> AcquireAsync(CancellationToken cancellationToken) =>
        Policy.AcquireAsync(directory, afterStage, cancellationToken);

    private static void Validate(ClaudeStoredState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision ||
            state.Identity is null || !ClaudeAuthClient.SafeIdentity(state.Identity.AccountId) ||
            !ClaudeAuthClient.SafeIdentity(state.Identity.OrganizationId) || !ClaudeAuthClient.SafeToken(state.RefreshToken))
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        if (state.CachedQuota is { } reading)
        {
            if (reading.Quota is null || reading.Quota.Groups is null || reading.Quota.Groups.Count > 1024 ||
                reading.Quota.Groups.Any(group => group is null || group.Id is null || group.Windows is null ||
                    group.Windows.Any(window => window is null || window.Id is null || !Percent(window.UsedPercent) || !Percent(window.RemainingPercent))))
                throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        }
    }

    private static bool Percent(double? value) => value is null || (double.IsFinite(value.Value) && value is >= 0 and <= 100);
}
