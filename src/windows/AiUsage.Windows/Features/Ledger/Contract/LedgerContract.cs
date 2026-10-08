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

internal sealed record SignInStripModel(SignInPhase Phase, ProviderKind Provider, string? AccountName, int? LimitsFound)
{
    public Guid? AttemptId { get; init; }
    public string? ReconnectAccountId { get; init; }
    public string? UserCode { get; init; }
    public bool AcceptsManualCode { get; init; }
    public SignInFailure? Failure { get; init; }
    public override string ToString() => "SignInStripModel (redacted)";
}

internal enum SignInFailure { Duplicate, WrongAccount, Storage, AccessDenied, Expired, Browser, Registration, Provider }

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
    DayOffPreview? DayOff)
{
    public MonetaryDetails? Monetary { get; init; }
    /// <summary>A limit of one model within the subscription (Claude Fable or Opus, Codex additional limits); drawn as a section of the account card.</summary>
    public bool ModelScoped { get; init; }
    /// <summary>The owner hid this section of the account card (D-191); its facts, tray strip and status stay.</summary>
    public bool Hidden { get; init; }
    /// <summary>A credit pool the owner may show in US dollars (D-199); null for every other limit.</summary>
    public UnitModel? Units { get; init; }
    /// <summary>A money or credit pool with a daily budget whose today's use the owner may set (D-199).</summary>
    public TodayUseModel? TodayUse { get; init; }
}

/// <summary>How a credit pool is shown: natively, or in US dollars at the owner's rate per credit.</summary>
internal sealed record UnitModel(bool Usd, decimal Rate)
{
    /// <summary>GitHub's published price of one AI credit.</summary>
    public const decimal DefaultRate = 0.01m;
}

/// <summary>Today's use as tracked from readings (in the card's scale), when tracking began after midnight, and whether the owner's figure applies.</summary>
internal sealed record TodayUseModel(decimal? Tracked, DateTimeOffset? TrackedSince, bool Manual);

/// <summary>D-191: one card per account. The primary limit heads it; every other limit is a section below it.</summary>
internal static class AccountCard
{
    /// <summary>The first shown card, preferring a subscription window over model, spending and note-only limits.</summary>
    public static LimitCardModel? Primary(AccountModel account)
    {
        var shown = account.Cards.Where(c => !c.Hidden).ToArray();
        return (shown.Length > 0 ? shown : account.Cards).OrderBy(c => (c.Monetary is not null || c.ModelScoped ? 2 : 0) + (c.Layout == CardLayout.Note ? 1 : 0)).FirstOrDefault();
    }

    /// <summary>Section order below the primary: limits with bars, then spending, then note-only limits.</summary>
    public static int SectionRank(LimitCardModel card) => card.Layout == CardLayout.Note ? 2 : card.Monetary is not null ? 1 : 0;

    /// <summary>AIU-055 R-10: the account order after placing one account before another, or last when before is null;
    /// null when either account is unknown or both are the same.</summary>
    public static string[]? Move(IReadOnlyList<string> order, string accountId, string? beforeAccountId)
    {
        if (!order.Contains(accountId) || accountId == beforeAccountId || beforeAccountId is not null && !order.Contains(beforeAccountId))
            return null;
        var next = order.Where(id => id != accountId).ToList();
        next.Insert(beforeAccountId is null ? next.Count : next.IndexOf(beforeAccountId), accountId);
        return [.. next];
    }
}

// Presentation-only native facts; separate scales prevent a mismatched limit being relabeled.
internal sealed record MonetaryAmount(long MinorUnits, int? Exponent, string? Currency);
internal sealed record MonetaryDetails(MonetaryAmount? Used, MonetaryAmount? ProviderLimit, LimitValueKind LimitKind,
    MonetaryAmount? PersonalCap, bool? Enabled, string Qualification, string? BudgetUnavailable);

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
    FiveHourLow,
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
    NoDisplayedLimits,
}

internal sealed record Freshness(bool IsStale, DateTimeOffset? ReadingAt)
{
    public static Freshness Fresh(DateTimeOffset? readingAt = null) => new(false, readingAt);
    public static Freshness Stale(DateTimeOffset readingAt) => new(true, readingAt);
}

/// <summary>A word beside the card name. OnExtraUsage carries the spend since the window filled in the pool's currency.</summary>
internal sealed record CardMark(MarkKind Kind, DateTimeOffset? Since = null, DateTimeOffset? Until = null, decimal? Amount = null, string? Currency = null, int? Exponent = null, string? ScopeLabel = null);

internal enum MarkKind { OnExtraUsage, ExtraDay, SyncFailed, SignInExpired, PastReset }

internal enum CardAction { None, SignIn }

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

    /// <summary>A personal cap may equal but never exceed a known provider limit.</summary>
    public bool AllowsCap(decimal cap) => Amount is not { } limit || cap <= limit;
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
    int? FitBeforeReset)
{
    /// <summary>Guaranteed share bounds, set only when the estimate is ready.</summary>
    public decimal? WindowShareLow { get; init; }
    public decimal? WindowShareHigh { get; init; }
    /// <summary>Largest window count when the count is a range starting at <see cref="WindowsLeftInPeriod"/>.</summary>
    public int? WindowsLeftMax { get; init; }
    public bool Rough { get; init; }
    /// <summary>Pooled five-hour parts behind the estimate.</summary>
    public int Windows { get; init; }
}

/// <summary>A provider reset with time (At) or date-only precision (Date), or an assumed period end.</summary>
internal sealed record ResetModel(DateTimeOffset? At, DateOnly? Date, ResetProvenance Provenance, DateOnly? AssumedStart);

internal enum ResetProvenance { Provider, Assumed }

internal sealed record CapModel(decimal Amount, bool Binding, CapStatus Status);

/// <summary>AboveLimit keeps a cap saved before the provider limit fell below it (or by an older build) without applying it.</summary>
internal enum CapStatus { Applied, Unmatched, CurrencyMismatch, Inactive, AboveLimit }

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

internal sealed record SettingsSummaries(TimeSpan RefreshInterval, UpdateStatus Updates, int FailedSyncs, IReadOnlyList<ProviderKind> FailedProviders)
{
    public string? LocalStatus { get; init; }
    public LedgerRecoveryModel? Recovery { get; init; }
    public bool DiagnosticsAvailable { get; init; }
    public bool IsStarting { get; init; }
}
internal sealed record LedgerRecoveryModel(string Message, bool CanRetry, bool CanRestorePreferences);
/// <summary>AIU-046 in-app update state; the version is the installed package version, never a feed target.</summary>
internal sealed record UpdateStatus(UpdateState State, string? Version, DateTimeOffset? CheckedAt = null, int? ErrorCode = null)
{
    public static UpdateStatus NotPackaged { get; } = new(UpdateState.NotPackaged, null);
}
internal enum UpdateState { NotPackaged, NoFeed, Idle, Checking, UpToDate, Available, Ready, Installing, CheckFailed, InstallFailed, NotApplied }
internal enum LedgerSupportAction { RetryRecovery, RestorePreferences, OpenDataFolder, OpenLogs, ExportRecovery }

internal sealed record LedgerPreferences(ValueMode Mode, Density Density, bool ShowSignedOut, bool AlwaysOnTop, UpdateMode Updates = UpdateMode.Always)
{
    public static LedgerPreferences Default { get; } = new(ValueMode.Used, Density.Compact, false, false);
}

internal enum ValueMode { Used, Left }

internal enum Density { Compact, Comfortable }

/// <summary>AIU-046: Always checks every 5 minutes, OnLaunch once per start, Off only on request.</summary>
internal enum UpdateMode { Always, OnLaunch, Off }

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
    /// <summary>D-199: the owner's figure for today's use in the card's shown unit; null returns to the tracked figure.</summary>
    Task<CommandOutcome> SetTodayUsedAsync(string cardId, decimal? amount, CancellationToken ct);
    /// <summary>D-199: shows a credit pool natively or in US dollars at the rate per credit.</summary>
    Task<CommandOutcome> SetUnitsAsync(string cardId, bool usd, decimal rate, CancellationToken ct);
    Task<CommandOutcome> SetWorkDaysAsync(IReadOnlySet<DayOfWeek> days, CancellationToken ct);
    Task MoveCardAsync(string cardId, int offset, CancellationToken ct);
    /// <summary>AIU-055 R-10: places an account before another one, or last when before is null; the order is the owner's
    /// and applies to the window and the tray.</summary>
    Task<CommandOutcome> MoveAccountAsync(string accountId, string? beforeAccountId, CancellationToken ct);
    /// <summary>Hides or shows a section of its account card; the primary limit cannot be hidden.</summary>
    Task<CommandOutcome> SetCardHiddenAsync(string cardId, bool hidden, CancellationToken ct);
    Task SignInAsync(ProviderKind provider, CancellationToken ct);
    Task ReconnectAsync(string accountId, CancellationToken ct);
    Task RefreshAccountAsync(string accountId, CancellationToken ct);
    bool TrySubmitSignInCode(Guid attemptId, string code);
    Task CancelSignInAsync(CancellationToken ct);
    Task SignOutAsync(string accountId, CancellationToken ct);
    Task<CommandOutcome> DeleteStoredDataAsync(CancellationToken ct);
    Task SetPreferencesAsync(LedgerPreferences preferences, CancellationToken ct);
    Task<HistoryModel?> GetHistoryAsync(string cardId, CancellationToken ct);
    Task<CommandOutcome> SupportAsync(LedgerSupportAction action, CancellationToken ct) => Task.FromResult(CommandOutcome.Unavailable);
    Task<string> PreviewDiagnosticsAsync(CancellationToken ct) => Task.FromResult("Diagnostics unavailable in demo mode");
    Task CheckForUpdatesAsync(CancellationToken ct) => Task.CompletedTask;
    Task InstallUpdateAsync(CancellationToken ct) => Task.CompletedTask;
}
