using AiUsage.Core.Usage;
namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>Provider policy over the shared app-owned DPAPI state lease.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class AntigravityStateStore
{
    private readonly string directory;
    private readonly Action? afterStage;
    private static readonly ProviderStatePolicy<AntigravityStoredState> Policy = new(
        "antigravity.state", "AiUsage.Antigravity.State.v1"u8.ToArray(),
        AntigravityStateJson.Default.AntigravityStoredState, state => state.Revision, state => state.ParentRevision,
        (state, parent) => state with { Version = 1, Revision = Guid.NewGuid(), ParentRevision = parent }, Validate);

    public AntigravityStateStore(string ownedDirectory) : this(ownedDirectory, null) { }
    internal AntigravityStateStore(string ownedDirectory, Action? afterStage)
    {
        directory = Path.GetFullPath(ownedDirectory);
        this.afterStage = afterStage;
    }

    internal Task<ProviderStateLease<AntigravityStoredState>> AcquireAsync(CancellationToken cancellationToken) =>
        Policy.AcquireAsync(directory, afterStage, cancellationToken);

    private static void Validate(AntigravityStoredState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision ||
            !AntigravityAuthClient.SafeIdentity(state.AccountId) ||
            !AntigravityAuthClient.SafeIdentity(state.ProjectId) ||
            !AntigravityAuthClient.SafeToken(state.RefreshToken) ||
            state.Tier is { Length: 0 or > 128 })
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        if (state.CachedQuota is { } quota)
        {
            if (quota.Groups is null || quota.Groups.Count > 1024 ||
                quota.Groups.Any(group => group is null || group.Id is null || group.Windows is null ||
                    group.Windows.Any(window => window is null || window.Id is null ||
                        !Percent(window.UsedPercent) || !Percent(window.RemainingPercent))))
                throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        }
    }

    private static bool Percent(double? value) => value is null || (double.IsFinite(value.Value) && value is >= 0 and <= 100);
}
