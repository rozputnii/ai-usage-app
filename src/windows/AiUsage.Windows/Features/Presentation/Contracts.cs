namespace AiUsage.Features.Presentation;

// Presentation contract v1 (docs/specs/AIU-010-ui-ux/ui-contract.md). Records carry typed, nullable
// measurements and opaque identities; formatted text is produced only by view models.

public enum UiMode { Live, Demo }
public enum ConnectionState { NotConnected, Connecting, Connected, ReauthRequired, RecoveryRequired }
public enum AccountOperation { Idle, Loading, Refreshing, Disconnecting }
public enum Freshness { Fresh, Cached, Stale, Unknown }
public enum ContextKind { Account, Organization, Workspace, Project }
public enum ExpansionPreference { Auto, Expanded, Collapsed }
public enum ValueState { Known, Unknown, Unlimited, Exhausted, Unavailable }
public enum ExtensionKind { Credits, ExtraUsage, Opaque }
public enum Availability { Available, Unavailable }
public enum CapabilityOrigin { Existing, Planned }
public enum Density { Comfortable, Compact }
public enum UsageDisplay { Remaining, Used }
public enum RuleScope { Global, Provider, WindowType, Account, Window }
public enum HistoryRetention { Days30, Days90, Year1, KeepEverything }
public enum HealthState { Idle, Checking, Healthy, Warning, Failed }
public enum UpdateState { Unsupported, Checking, Current, Available, Downloading, Ready, Failed, WaitingForStable }
public enum UpdateChannel { Stable, Preview }
public enum RecoveryState { None, Interrupted, NewerSchema, RestoreFailed }
public enum CompatibilityState { Normal, CompatibilityBlocked, SecurityBlocked }
public enum RefreshPolicy { Normal, Reduced }
public enum CommandStatus { Succeeded, Cancelled, Failed, Unsupported, Conflict }

public sealed record UiSnapshot(
    long Revision,
    UiMode Mode,
    DateTimeOffset ObservedAt,
    IReadOnlyList<AccountItem> Accounts,
    IReadOnlyList<CapabilityItem> Capabilities,
    Preferences Preferences,
    SystemStatus System)
{
    /// <summary>Extension: aggregate outcome of the latest Refresh all, if one completed.</summary>
    public RefreshAllSummary? LastRefreshAll { get; init; }

    /// <summary>Extension: false until the first cached state is read; surfaces show layout-matched skeletons meanwhile.</summary>
    public bool Loaded { get; init; } = true;
}

public sealed record AccountItem(
    string Id,
    string ProviderId,
    string Label,
    string? Plan,
    ConnectionState Connection,
    AccountOperation Operation,
    Freshness Freshness,
    DateTimeOffset? FetchedAt,
    FailureItem? Failure,
    IReadOnlyList<ContextItem> Contexts,
    IReadOnlyList<ExtensionItem> Extensions)
{
    public int? AvailableResetCredits { get; init; }
    public bool? SpendControlReached { get; init; }
    public string? LimitReachedType { get; init; }

    /// <summary>
    /// Extension: increases with every successful observation, so a new reading is distinguishable from a restored
    /// (cancelled) state even when two observations share a timestamp. Drives "✓ Updated" and meter animation only.
    /// </summary>
    public long ObservationRevision { get; init; }
}

/// <summary>A synthetic account-level context is used when a provider reports no context structure.</summary>
public sealed record ContextItem(string Id, string Label, ContextKind Kind, bool Hidden, IReadOnlyList<GroupItem> Groups)
{
    /// <summary>Extension (spec §12): false while a known context is temporarily absent; preferences are kept.</summary>
    public bool Available { get; init; } = true;
}

public sealed record GroupItem(string Id, string Label, string? SharedPoolId, bool Hidden, ExpansionPreference Expansion, IReadOnlyList<WindowItem> Windows)
{
    // Independent provider restrictions; never replace or synthesize window measurements.
    public bool? Allowed { get; init; }
    public bool? LimitReached { get; init; }
}

/// <summary>Null percentages are unknown, never zero. Unlimited requires an explicit <see cref="ValueState.Unlimited"/>.</summary>
public sealed record WindowItem(
    string Id,
    string Label,
    double? RemainingPercent,
    double? UsedPercent,
    ValueState ValueState,
    NativeAmount? Absolute,
    double? DurationSeconds,
    DateTimeOffset? ResetsAt,
    bool AlertsMuted)
{
    /// <summary>Extension (spec §12): headline window for Overview; absent means the first window of the first visible group.</summary>
    public bool Primary { get; init; }
}

/// <summary>Decimal strings in the provider's native unit. A null limit is not unlimited.</summary>
public sealed record NativeAmount(string? Remaining, string? Used, string? Limit, string Unit);

/// <summary>Money is formatted only when both currency and exponent are present.</summary>
public sealed record ExtensionItem(
    ExtensionKind Kind,
    string Label,
    bool? Enabled,
    string? AmountMinor,
    int? Exponent,
    string? Currency,
    string? UsedMinor,
    string? LimitMinor,
    bool? Unlimited)
{
    public bool HasExplicitNullLimit { get; init; }
}

public sealed record CapabilityItem(string Key, string? TargetId, Availability Availability, string? Reason, CapabilityOrigin Origin);

/// <summary>Safe failure category and resource key; never raw exception text or provider payload.</summary>
public sealed record FailureItem(string Kind, string MessageKey, DateTimeOffset? RetryAt, bool Recoverable);

public static class FailureKinds
{
    public const string NetworkFailure = nameof(NetworkFailure);
    public const string RateLimited = nameof(RateLimited);
    public const string InvalidGrant = nameof(InvalidGrant);
    public const string SchemaMismatch = nameof(SchemaMismatch);
    public const string InternalError = nameof(InternalError);
}

public sealed record Preferences(
    IReadOnlyList<string> AccountOrder,
    IReadOnlyList<string> HiddenTargets,
    IReadOnlyList<string> MutedTargets,
    bool ShowDisconnected,
    bool AlwaysOnTop,
    bool HistoryEnabled,
    IReadOnlyList<NotificationRule> NotificationRules)
{
    public Density Density { get; init; } = Density.Comfortable;
    /// <summary>Extension (D2): display-only; stored measurements stay remaining/used as reported.</summary>
    public UsageDisplay UsageDisplay { get; init; } = UsageDisplay.Remaining;
    public bool ShowHidden { get; init; }
    public HistoryRetention Retention { get; init; } = HistoryRetention.Days90;
    /// <summary>Extension (D-106): reduce automatic refresh on battery saver or metered connections.</summary>
    public bool ReduceRefreshOnBatterySaver { get; init; } = true;
    public IReadOnlyDictionary<string, string> ContextSelection { get; init; } = new Dictionary<string, string>();

    public static IReadOnlyList<int> DefaultRemainingThresholds { get; } = [25, 10, 0];

    public static NotificationRule DefaultGlobalRule { get; } = new(RuleScope.Global, null, false, DefaultRemainingThresholds, true);
}

/// <summary>
/// Remaining-percent thresholds (D-123). An inheriting rule carries no values of its own; resolution
/// never mutates a parent rule.
/// </summary>
public sealed record NotificationRule(RuleScope Scope, string? TargetId, bool Inherit, IReadOnlyList<int> RemainingThresholds, bool ResetNotice);

public enum HistoryResolution { Auto, Hour, Day }
public enum HistoryPreset { Hours24, Days7, Days30, Days90, Year1, Custom }
public enum HistoryCoverage { Observed, Gap }

public sealed record HistoryQuery(string AccountId, string? ContextId, string? GroupId, string WindowId, DateTimeOffset From, DateTimeOffset To, HistoryResolution Resolution, HistoryPreset Preset);

/// <summary>Segments change at resets; renderers never join a gap or two segments.</summary>
public sealed record HistoryPoint(DateTimeOffset At, double? RemainingPercent, HistoryCoverage Coverage, string SegmentId);

public enum HistoryOutcome { Points, Empty, NoComparablePercentage, DataDeleted }

public sealed record HistoryResult(HistoryOutcome Outcome, IReadOnlyList<HistoryPoint> Points, bool CollectionEnabled, bool Partial);

public sealed record SystemStatus(
    string BuildLabel,
    string SchemaLabel,
    HealthState Health,
    UpdateState Update,
    RecoveryState Recovery,
    CompatibilityState Compatibility)
{
    public string? UpdateVersion { get; init; }
    public double? UpdateProgress { get; init; }
    public string? InstalledUpdateVersion { get; init; }
    public UpdateChannel Channel { get; init; } = UpdateChannel.Stable;
    public DateTimeOffset? LastUpdateCheck { get; init; }
    public string? UpdateFailureKey { get; init; }
    public RefreshPolicy RefreshPolicy { get; init; } = RefreshPolicy.Normal;
    public bool? NotificationsAllowed { get; init; }
    public bool? QuietHours { get; init; }
    /// <summary>D-168: a local override on the Preview channel for a compatibility (never security) block.</summary>
    public bool CompatibilityOverridden { get; init; }
    public long HistoryRows { get; init; }
    public int CheckpointCount { get; init; }
}

public sealed record RefreshAllSummary(int Updated, IReadOnlyList<string> FailedAccountIds, DateTimeOffset CompletedAt);

public enum UiCommandKind
{
    Connect, SubmitCode, CancelOperation, RefreshAccount, RefreshAll, Disconnect,
    Rename, Reorder, SetVisibility, SetMute, SetExpansion, SelectContext,
    SetPreference, SetNotificationRule,
    DiscoverCli, ImportCli, ReimportCli,
    RunHealthCheck, PreviewDiagnostics, PreviewLogs,
    PreviewExport, PreviewReplaceImport, ResetSettings, FactoryReset, DeleteAccountData, RetryRecovery, RestoreCheckpoint, PreviewDataFolder,
    CheckUpdates, SetChannel, RestartAndUpdate, PreviewNotification,
}

public static class CapabilityKeys
{
    public const string ViewHistory = nameof(ViewHistory);
    public const string ViewContexts = nameof(ViewContexts);
    public const string ViewNativeAmounts = nameof(ViewNativeAmounts);
    public const string ViewSystemStatus = nameof(ViewSystemStatus);
    public static string For(UiCommandKind kind) => kind.ToString();
}

/// <summary><paramref name="Payload"/> is one of the typed payload records below, matching <paramref name="Kind"/>.</summary>
public sealed record UiCommand(UiCommandKind Kind, string? TargetId, object? Payload, long ExpectedRevision);

public sealed record RenamePayload(string Label);
public sealed record ReorderPayload(IReadOnlyList<string> Order);
public sealed record VisibilityPayload(bool Hidden, bool MuteAlerts);
public sealed record MutePayload(bool Muted);
public sealed record ExpansionPayload(ExpansionPreference Expansion);
public sealed record ContextSelectionPayload(string ContextId);

public sealed record UiCommandResult(CommandStatus Status, FailureItem? Failure = null)
{
    public static UiCommandResult Succeeded { get; } = new(CommandStatus.Succeeded);
    public static UiCommandResult Cancelled { get; } = new(CommandStatus.Cancelled);
    public static UiCommandResult Unsupported { get; } = new(CommandStatus.Unsupported);
    public static UiCommandResult Conflict { get; } = new(CommandStatus.Conflict);
    public static UiCommandResult Failed(FailureItem? failure = null) => new(CommandStatus.Failed, failure);
}
