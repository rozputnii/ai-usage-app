using AiUsage.Core.Usage;
namespace AiUsage.Infrastructure.Providers.Copilot;

/// <summary>Provider policy over the shared app-owned DPAPI state lease.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CopilotStateStore
{
    private readonly string directory;
    private readonly Action? afterStage;
    private static readonly ProviderStatePolicy<CopilotStoredState> Policy = new(
        "copilot.state", "AiUsage.Copilot.State.v1"u8.ToArray(),
        CopilotStateJson.Default.CopilotStoredState, state => state.Revision, state => state.ParentRevision,
        (state, parent) => state with { Version = 1, Revision = Guid.NewGuid(), ParentRevision = parent }, Validate);

    public CopilotStateStore(string ownedDirectory) : this(ownedDirectory, null) { }
    internal CopilotStateStore(string ownedDirectory, Action? afterStage)
    {
        directory = Path.GetFullPath(ownedDirectory);
        this.afterStage = afterStage;
    }

    internal Task<ProviderStateLease<CopilotStoredState>> AcquireAsync(CancellationToken cancellationToken) =>
        Policy.AcquireAsync(directory, afterStage, cancellationToken);

    private static void Validate(CopilotStoredState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision ||
            !CopilotAuthClient.SafeIdentity(state.AccountId) ||
            !CopilotAuthClient.SafeToken(state.AccessToken))
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        if (state.CachedQuota is { } reading)
        {
            if (reading is null || reading.Groups is null || reading.Groups.Count > 1024 ||
                reading.Groups.Any(group => group is null || group.Id is null || group.Windows is null ||
                    group.Windows.Any(window => window is null || window.Id is null || !Percent(window.UsedPercent) || !Percent(window.RemainingPercent))))
                throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        }
    }

    private static bool Percent(double? value) => value is null || (double.IsFinite(value.Value) && value is >= 0 and <= 100);
}
