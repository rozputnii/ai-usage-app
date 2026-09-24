using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

// Mutable in-memory demo model. Only Demo services mutate it; view models see immutable UiSnapshot records.

internal sealed class DemoWindow(string id, string label, double? remaining, ValueState state)
{
    public string Id { get; } = id;
    public string Label { get; set; } = label;
    /// <summary>Cached reading shown to the user. Null means no measurement.</summary>
    public double? Remaining { get; set; } = remaining;
    public ValueState State { get; set; } = state;
    /// <summary>What the simulated provider reports on the next successful observation.</summary>
    public double? ProviderRemaining { get; set; } = remaining;
    public ValueState ProviderState { get; set; } = state;
    public double? NextObservation { get; set; }
    public double? DurationSeconds { get; set; } = 18000;
    public DateTimeOffset? ResetsAt { get; set; }
    public NativeAmount? Absolute { get; set; }
    public bool Primary { get; set; }

    public WindowItem ToItem() => new(Id, Label,
        State == ValueState.Exhausted ? 0 : State == ValueState.Known ? Remaining : null,
        State == ValueState.Exhausted ? 100 : State == ValueState.Known && Remaining is { } r ? 100 - r : null,
        State, Absolute, DurationSeconds, ResetsAt, false) { Primary = Primary };
}

internal sealed class DemoGroup(string id, string label, List<DemoWindow> windows, string? sharedPoolId = null)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public string? SharedPoolId { get; } = sharedPoolId;
    public ExpansionPreference Expansion { get; set; } = ExpansionPreference.Auto;
    public List<DemoWindow> Windows { get; } = windows;
}

internal sealed class DemoContext(string id, string label, ContextKind kind, List<DemoGroup> groups)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public ContextKind Kind { get; } = kind;
    public bool Available { get; set; } = true;
    public List<DemoGroup> Groups { get; } = groups;
}

internal sealed class DemoAccount(string id, string providerId, string label, List<DemoContext> contexts)
{
    public string Id { get; } = id;
    public string ProviderId { get; } = providerId;
    public string Label { get; set; } = label;
    public string? Plan { get; set; } = "Sample plan";
    public ConnectionState Connection { get; set; } = ConnectionState.Connected;
    public AccountOperation Operation { get; set; } = AccountOperation.Idle;
    public Freshness Freshness { get; set; } = Freshness.Fresh;
    public DateTimeOffset? FetchedAt { get; set; }
    public FailureItem? Failure { get; set; }
    public List<DemoContext> Contexts { get; } = contexts;
    public List<ExtensionItem> Extensions { get; } = [];
    public int FailNext { get; set; }
    public bool HistoryDeleted { get; set; }
    public int? AvailableResetCredits { get; set; }
    public bool? SpendControlReached { get; set; }
    public CancellationTokenSource? OperationCancellation { get; set; }
    public long ObservationRevision { get; set; } = 1;

    public IEnumerable<DemoWindow> AllWindows => Contexts.SelectMany(c => c.Groups).SelectMany(g => g.Windows);

    public AccountItem ToItem() => new(Id, ProviderId, Label, Plan, Connection, Operation, Freshness, FetchedAt, Failure,
        Contexts.Select(c => new ContextItem(c.Id, c.Label, c.Kind, false,
            c.Groups.Select(g => new GroupItem(g.Id, g.Label, g.SharedPoolId, false, g.Expansion, g.Windows.Select(w => w.ToItem()).ToArray())).ToArray())
        { Available = c.Available }).ToArray(),
        Extensions.ToArray())
    {
        AvailableResetCredits = AvailableResetCredits,
        SpendControlReached = SpendControlReached,
        ObservationRevision = ObservationRevision,
    };
}

public enum DemoRefreshOutcome { Scenario, Success, NetworkFailure, RateLimited, InvalidGrant }

internal sealed class DemoWorld
{
    public required string ScenarioId { get; init; }
    public List<DemoAccount> Accounts { get; } = [];
    public List<string> Order { get; } = [];
    public HashSet<string> Hidden { get; } = [];
    public HashSet<string> Muted { get; } = [];
    public Dictionary<string, string> ContextSelection { get; } = [];
    public Density Density { get; set; } = Density.Comfortable;
    public UsageDisplay UsageDisplay { get; set; } = UsageDisplay.Remaining;
    public bool ShowDisconnected { get; set; }
    public bool ShowHidden { get; set; }
    public bool AlwaysOnTop { get; set; }
    public bool HistoryEnabled { get; set; } = true;
    public HistoryRetention Retention { get; set; } = HistoryRetention.Days90;
    public bool ReduceRefreshOnBatterySaver { get; set; } = true;
    public List<NotificationRule> Rules { get; } = [Preferences.DefaultGlobalRule];

    public RecoveryState Recovery { get; set; }
    public CompatibilityState Compatibility { get; set; }
    public bool CompatibilityOverridden { get; set; }
    public HealthState Health { get; set; }
    public UpdateState Update { get; set; } = UpdateState.Current;
    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;
    public string? UpdateVersion { get; set; }
    public double? UpdateProgress { get; set; }
    public string? InstalledUpdateVersion { get; set; }
    public string? UpdateFailureKey { get; set; }
    public DateTimeOffset? LastUpdateCheck { get; set; }
    public bool NotificationsAllowed { get; set; } = true;
    public bool QuietHours { get; set; }
    public bool BatterySaver { get; set; }
    public RefreshAllSummary? LastRefreshAll { get; set; }
    public long HistoryRows { get; set; } = 18240;
    public DemoRefreshOutcome ForcedRefreshOutcome { get; set; }
    public string SchemaLabel { get; set; } = "Schema 4";

    public Preferences BuildPreferences() => new(Order.ToArray(), Hidden.ToArray(), Muted.ToArray(), ShowDisconnected, AlwaysOnTop, HistoryEnabled, Rules.ToArray())
    {
        Density = Density,
        UsageDisplay = UsageDisplay,
        ShowHidden = ShowHidden,
        Retention = Retention,
        ReduceRefreshOnBatterySaver = ReduceRefreshOnBatterySaver,
        ContextSelection = new Dictionary<string, string>(ContextSelection),
    };

    public SystemStatus BuildSystem() => new("1.0.0-demo · synthetic build", SchemaLabel, Health, Update, Recovery, Compatibility)
    {
        UpdateVersion = UpdateVersion,
        UpdateProgress = UpdateProgress,
        InstalledUpdateVersion = InstalledUpdateVersion,
        Channel = Channel,
        LastUpdateCheck = LastUpdateCheck,
        UpdateFailureKey = UpdateFailureKey,
        RefreshPolicy = BatterySaver && ReduceRefreshOnBatterySaver ? RefreshPolicy.Reduced : RefreshPolicy.Normal,
        NotificationsAllowed = NotificationsAllowed,
        QuietHours = QuietHours,
        CompatibilityOverridden = CompatibilityOverridden,
        HistoryRows = HistoryRows,
        CheckpointCount = 2,
    };

    public void ResetSettings()
    {
        Density = Density.Comfortable;
        UsageDisplay = UsageDisplay.Remaining;
        AlwaysOnTop = false;
        ReduceRefreshOnBatterySaver = true;
        Rules.Clear();
        Rules.Add(Preferences.DefaultGlobalRule);
    }
}
