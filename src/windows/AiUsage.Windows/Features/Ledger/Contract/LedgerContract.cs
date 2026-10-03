namespace AiUsage.Features.Ledger.Contract;

// AIU-038 presentation contract (spec section 4). It owns the view-model data shape; a change is agreed with AIU-039.
// Every figure arrives computed and display-rounded by the source; the presentation formats and draws, never derives a budget.

/// <summary>The whole presentation state at one moment; a source replaces it as a whole on every change.</summary>
internal sealed record LedgerSnapshot(
    DateTimeOffset LocalNow,
    DayModel Day,
    IReadOnlyList<AccountModel> Accounts,
    IReadOnlyList<ProviderOption> Providers,
    SignInStripModel? SignInStrip,
    BudgetSettingsModel Budget,
    SettingsSummaries Summaries);

/// <summary>Work today is window-wide and lasts until local midnight; it never edits the work days.</summary>
internal sealed record DayModel(DayKind Kind, bool WorkTodayOn, DateTimeOffset? WorkTodayUntil);

internal enum DayKind { WorkDay, DayOff }

internal sealed record AccountModel(
    string AccountId,
    ProviderKind Provider,
    string DisplayName,
    AccountHealth Health,
    DateTimeOffset? LastReadingAt,
    DateTimeOffset? LastSyncFailedAt,
    DateTimeOffset? NextRetryAt,
    IReadOnlyList<LimitCardModel> Cards);

internal enum ProviderKind { Claude, Codex, Copilot, Antigravity }

internal enum AccountHealth { Ok, SyncFailedFresh, SyncFailedStale, SignInExpired, SignedOut, ProviderError }

internal sealed record ProviderOption(ProviderKind Provider, bool Added, bool SigningIn);

internal sealed record SignInStripModel(SignInPhase Phase, ProviderKind Provider, string? AccountName, int? LimitsFound);

internal enum SignInPhase { Waiting, Succeeded, Failed, Cancelled }

/// <summary>One card per subscription limit group (D-186); a five-hour window belongs to its period card.</summary>
internal sealed record LimitCardModel(
    string CardId,
    string? ScopeLabel,
    CardLayout Layout,
    ScaleModel Scale,
    PeriodModel Period,
    CardState State,
    Freshness Freshness,
    IReadOnlyList<CardMark> Marks,
    LimitFigures Figures,
    FiveHourModel? FiveHour,
    ResetModel? Reset,
    CapModel? Cap,
    string? CapTargetId,
    CardAction Action,
    DayOffPreview? DayOff);

internal enum CardLayout { FiveHourAndPeriod, Period, Pool, UsedOnly, Note }

internal sealed record ScaleModel(ScaleKind Kind, string? UnitName, string? Currency, int? Exponent)
{
    public static ScaleModel Percent { get; } = new(ScaleKind.Percent, null, null, null);
    public static ScaleModel Count(string unit) => new(ScaleKind.Count, unit, null, null);
    public static ScaleModel Money(string currency, int exponent) => new(ScaleKind.Money, null, currency, exponent);
}

internal enum ScaleKind { Percent, Count, Money }

internal sealed record PeriodModel(PeriodKind Kind, TimeSpan? Duration, bool StartAssumed)
{
    public static PeriodModel Week { get; } = new(PeriodKind.Days, TimeSpan.FromDays(7), false);
    public static PeriodModel Month(bool startAssumed) => new(PeriodKind.CalendarMonth, null, startAssumed);
    public static PeriodModel Unknown { get; } = new(PeriodKind.Unknown, null, false);
}

internal enum PeriodKind { Days, CalendarMonth, Unknown }

/// <summary>Source-computed card state (spec section 4.4); the view model maps it to a pill, a tone and drawing rules.</summary>
internal enum CardState
{
    OnTrack,
    TodayLow,
    TodayShort,
    CapClose,
    FiveHourFull,
    TodayUsed,
    OverToday,
    CapReached,
    OverCap,
    UsedUp,
    DayOff,
    Rush,
    NotReady,
    ValueUnknown,
    PeriodUnknown,
    NotIncluded,
    LimitUnknown,
    NoCap,
    SignedOut,
}

internal sealed record Freshness(bool IsStale, DateTimeOffset? ReadingAt)
{
    public static Freshness Fresh(DateTimeOffset? readingAt = null) => new(false, readingAt);
    public static Freshness Stale(DateTimeOffset readingAt) => new(true, readingAt);
}

/// <summary>A word beside the card name. OnExtraUsage carries the spend since the window filled in the pool's currency.</summary>
internal sealed record CardMark(MarkKind Kind, DateTimeOffset? Since = null, DateTimeOffset? Until = null, decimal? Amount = null, string? Currency = null, int? Exponent = null);

internal enum MarkKind { OnExtraUsage, ExtraDay, SyncFailed, SignInExpired, PastReset }

internal enum CardAction { None, SignIn, SetCap }

/// <summary>Values in the card's scale. Unknown is null, never zero.</summary>
internal sealed record LimitFigures(
    decimal? Used,
    decimal? DayStart,
    decimal? TodayEnd,
    decimal? UsualShare,
    LimitValue ProviderLimit,
    decimal? EffectiveLimit,
    decimal? ProviderRemaining,
    decimal? ProviderBalance,
    TrackingModel? Tracking)
{
    public static LimitFigures UsedOnly(decimal? used) => new(used, null, null, null, LimitValue.Unknown, null, null, null, null);
}

internal sealed record LimitValue(LimitValueKind Kind, decimal? Amount)
{
    public static LimitValue Unknown { get; } = new(LimitValueKind.Unknown, null);
    public static LimitValue Unlimited { get; } = new(LimitValueKind.Unlimited, null);
    public static LimitValue Zero { get; } = new(LimitValueKind.Zero, 0m);
    public static LimitValue Known(decimal amount) => new(LimitValueKind.Known, amount);
}

internal enum LimitValueKind { Known, Unknown, Unlimited, Zero }

/// <summary>Presence marks the used amount as a local tracking estimate.</summary>
internal sealed record TrackingModel(DateOnly? TrackedSince, bool Incomplete);

internal sealed record FiveHourModel(
    decimal? WindowShare,
    decimal CurrentWindowUsed,
    bool CurrentWindowStarted,
    DateTimeOffset? CurrentWindowEndsAt,
    int? WindowsLeftInPeriod,
    int? FitBeforeReset);

/// <summary>A provider reset with time (At) or date-only precision (Date), or an assumed period end.</summary>
internal sealed record ResetModel(DateTimeOffset? At, DateOnly? Date, ResetProvenance Provenance, DateOnly? AssumedStart);

internal enum ResetProvenance { Provider, Assumed }

internal sealed record CapModel(decimal Amount, bool Binding, CapStatus Status);

internal enum CapStatus { Applied, Unmatched, CurrencyMismatch }

/// <summary>Day-off tooltip figures: the next work day's share before and after today's use.</summary>
internal sealed record DayOffPreview(DayOfWeek NextWorkDay, decimal ShareBefore, decimal ShareAfter);

/// <summary>Local daily use, oldest first; a null day had no readings and is drawn as a gap, never as zero.</summary>
internal sealed record HistoryModel(
    string CardId,
    PeriodModel Period,
    IReadOnlyList<HistoryDay> Days,
    decimal? BaselinePerWorkDay,
    IReadOnlyList<DateOnly> ResetDays,
    DateOnly Today);

internal sealed record HistoryDay(DateOnly Date, decimal? Used);

internal sealed record BudgetSettingsModel(IReadOnlySet<DayOfWeek> WorkDays, IReadOnlyList<CapSettingModel> Caps)
{
    public static IReadOnlySet<DayOfWeek> MondayToFriday { get; } =
        new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
}

internal sealed record CapSettingModel(
    string CapId,
    string? CapTargetId,
    string AccountName,
    string? ScopeLabel,
    ScaleModel Scale,
    decimal Amount,
    CapStatus Status,
    bool Binding,
    LimitValue ProviderLimit,
    string? ProviderCurrency,
    TrackingModel? Tracking);

internal sealed record SettingsSummaries(TimeSpan RefreshInterval, string UpdatesSummary, int FailedSyncs, IReadOnlyList<ProviderKind> FailedProviders);

internal sealed record LedgerPreferences(ValueMode Mode, Density Density, bool ShowSignedOut, bool AlwaysOnTop)
{
    public static LedgerPreferences Default { get; } = new(ValueMode.Used, Density.Compact, false, false);
}

internal enum ValueMode { Used, Left }

internal enum Density { Compact, Comfortable }

internal enum CommandOutcome { Done, Rejected, Unavailable }

/// <summary>The only data port of the new presentation. The demo source implements it now; AIU-039 adds the live one.</summary>
internal interface ILedgerSource
{
    LedgerSnapshot Current { get; }
    LedgerPreferences Preferences { get; }

    /// <summary>Raised on the UI thread after Current or Preferences change.</summary>
    event EventHandler? Changed;

    Task RefreshAsync(CancellationToken ct);
    Task SetWorkTodayAsync(bool on, CancellationToken ct);
    Task<CommandOutcome> RenameAccountAsync(string accountId, string name, CancellationToken ct);
    Task<CommandOutcome> SetCapAsync(string capTargetId, decimal? amount, CancellationToken ct);
    Task<CommandOutcome> RemoveUnmatchedCapAsync(string capId, CancellationToken ct);
    Task<CommandOutcome> SetWorkDaysAsync(IReadOnlySet<DayOfWeek> days, CancellationToken ct);
    Task MoveCardAsync(string cardId, int offset, CancellationToken ct);
    Task SignInAsync(ProviderKind provider, CancellationToken ct);
    Task CancelSignInAsync(CancellationToken ct);
    Task SignOutAsync(string accountId, CancellationToken ct);
    Task<CommandOutcome> DeleteStoredDataAsync(CancellationToken ct);
    Task SetPreferencesAsync(LedgerPreferences preferences, CancellationToken ct);
    Task<HistoryModel?> GetHistoryAsync(string cardId, CancellationToken ct);
}
