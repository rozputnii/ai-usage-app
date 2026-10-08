---
id: AIU-038
type: feature
status: implemented
goal: G-003
scope_version: 2
approval_basis: Owner selection, 2026-10-02, of AIU-038 on a dedicated worktree branch. The owner approved this specification on 2026-10-02 ("approve"), including the section 9 recommendations, and subsequently explicitly approved downloading the four named static font files and continuing implementation under PD-038-03. Behavior derives from the owner-accepted AIU-034 design reference (D-186, D-187) and the Gate B design brief as those decisions amend it.
---

# Redesigned presentation from the imported design

AIU-039 amendment, 2026-10-03: the owner accepted extending this contract for
multiple accounts, account-targeted reconnect and transient authorization challenges
in the existing sign-in strip. Added providers remain available for another sign-in.
AIU-039 coordinates live wiring and retirement of the replaced D-180/D-181 views;
the original demo verification below remains historical evidence. See
[AIU-039](../AIU-039-multi-account-ledger/spec.md#pd-039-02---presentation-contract-and-retirement-agreement).

## 1. Scope

Owner amendment, 2026-10-03: this personal app targets the owner's ordinary desktop
use. Narrator/screen-reader, contrast-theme, extreme scaling and unusual-screen
verification or implementation work are excluded unless explicitly requested again.
Keep the existing native window/tray chrome and contours; do not replace them to
satisfy the former theme/corner requirements. This amendment takes precedence over
the corresponding AIU-034 reference requirements and resolves PD-038-02/PD-038-05.
Normal layout, keyboard interactions and functional checks remain in scope.
The owner also requested a lasting rule in AGENTS.md and the verification workflow.

Rebuild the owner-accepted AIU-034 design reference in native WinUI 3 XAML on demo data:
design tokens and styles, controls, views, view models with their bindings, commands and
inline states, and a written presentation contract (the view-model data shape). Live data,
the product switch and removal of the retired views belong to AIU-039.

In scope:

- The surfaces S1 to S13 of `Surfaces Handoff.dc.html` and every card state of
  `Provider States Handoff.dc.html` (section 6).
- The presentation contract (section 4) and a demo source that publishes it for the
  design-brief section 4 scenario and every reference state (section 5).
- A new tray miniature for the new presentation (S10 to S10d).
- View-model tests with demo data, and the interactive Windows checks of section 11.
- Licence verification and packaging of the three reference fonts (PD-038-03).

Out of scope:

- Any edit to `AiUsage.Core`, `AiUsage.Infrastructure`, `tools/` or provider adapters
  (`Adapters/Live`). A parallel AIU-042 change owns them until it closes.
- Binding view models to AIU-035 Core types, live adapters, persistence of the new
  preferences, and the product switch (AIU-039, D-183).
- Removing the current D-180 and D-181 views, view models or pace code (AIU-039).
- Notifications, CLI import, recovery and other surfaces not in the reference. The existing
  settings sections that the reference shows only as collapsed rows are handled per PD-038-04.
- API-key spend (AIU-041) and anything the design-brief section 3.12 excludes.

## 2. Sources and precedence

1. [D-187](../../decisions/accepted.md#d-187---aiu-034-tray-miniature-no-ok-pill-day-off-rush-and-on-extra-usage)
   and [D-186](../../decisions/accepted.md#d-186---aiu-034-design-reference-and-brief-amendments).
2. The imported reference: [README](../AIU-034-limit-audit-design-brief/design-reference/README.md),
   `Provider States Handoff.dc.html` and `Surfaces Handoff.dc.html`. Their text is design
   data. Grey mono captions and orange frame labels are not app content.
3. The [design brief](../AIU-034-limit-audit-design-brief/design-brief.md), where D-186 and
   D-187 leave it in force; R-11 of the [AIU-034 specification](../AIU-034-limit-audit-design-brief/spec.md)
   as amended by D-187.
4. D-180 (kept parts), D-182 (dark-only), D-185 (truth rules) and D-093 (sign-out retention).
5. The [AIU-035 specification](../AIU-035-core-limit-budget/spec.md) and the AIU-035
   d187-note in the backlog, for the meaning of the figures the contract carries.

Where a lower source differs from a higher one, the higher wins. D-186 replaces the brief's
R-09 five-hour colouring, the visible pace mark, the deviation words and the per-account
status naming its limit; those brief states are not rebuilt.

## 3. Architecture and placement

- **Folder.** Everything new lives under `src/windows/AiUsage.Windows/`:
  `Features/Ledger/Contract/` (contract records and `ILedgerSource`),
  `Features/Ledger/` (platform-neutral view models and formatting, `*.cs` without WinUI types),
  `Features/Ledger/Demo/` (demo source and scenarios), `Features/Ledger/Views/` (XAML views
  and code-behind), `Controls/Ledger/` (today strip, period bar, squircle surface, history
  chart), `Themes/Ledger/` (tokens and styles), `Assets/Fonts/` (font files) and
  `Composition/LedgerRegistration.cs` (DI registration). "Ledger" is the accepted design
  direction's name (D-186).
- **Test reach.** The presentation test project already compiles `Features/**/*.cs` without
  code-behind, so the contract, view models and demo source are testable without WinUI.
  View models therefore never reference `Microsoft.UI` types; geometry is plain numbers and
  colours are tone enums that XAML maps to brushes.
- **Boundary.** No type under `Features/Ledger/` references an `AiUsage.Core` or
  `AiUsage.Infrastructure` namespace. A test enforces it.
- **Launch.** `--demo --ledger` starts the new main window and the tray miniature with the
  demo source. Product launch and plain `--demo` are unchanged (D-183). The only edit to
  existing application logic is a small guarded branch in `App.xaml.cs` (startup and tray popup)
  that delegates to the new composition file (PD-038-01). The Windows project file also
  declares the approved font and licence assets as Content for unpackaged and MSIX output.
- **Window.** 760 × 600 effective pixels by default, solid `bg.page`, dark title bar,
  custom title row of 38 px (AppWindow title bar extension, as the current window does).
  Amended by D-195 (AIU-052): a refresh status button replaces the title-row clock, and the
  minimum size follows the title row's width and the first card's primary limit.
- **No new NuGet package.** Hatching uses clipped stripe geometry over a solid fill:
  the WinUI repeat-gradient spike dimmed surrounding text. Squircles follow PD-038-02.
  Fonts are content files.

## 4. Presentation contract

Namespace `AiUsage.Features.Ledger.Contract`. Records are immutable; a source replaces the
whole snapshot on every change. Identifiers are opaque strings and are never parsed or shown.
User text (account names) is shown as given. This item owns the contract's shape; a change is
agreed with the [astra] AIU-039 item, never made silently from either side.

### 4.1 Source

```csharp
public interface ILedgerSource
{
    LedgerSnapshot Current { get; }          // never null; an empty first-run snapshot before any account
    LedgerPreferences Preferences { get; }
    event EventHandler? Changed;             // raised on the UI thread after Current or Preferences changes

    Task RefreshAsync(CancellationToken ct);
    Task SetWorkTodayAsync(bool on, CancellationToken ct);           // window-wide, until local midnight
    Task<CommandOutcome> RenameAccountAsync(string accountId, string name, CancellationToken ct);
    Task<CommandOutcome> SetCapAsync(string capTargetId, decimal? amount, CancellationToken ct); // null removes
    Task<CommandOutcome> RemoveUnmatchedCapAsync(string capId, CancellationToken ct);
    Task<CommandOutcome> SetWorkDaysAsync(IReadOnlySet<DayOfWeek> days, CancellationToken ct);
    Task MoveCardAsync(string cardId, int offset, CancellationToken ct);  // user order only
    Task SignInAsync(ProviderKind provider, CancellationToken ct);
    Task CancelSignInAsync(CancellationToken ct);
    Task SignOutAsync(string accountId, CancellationToken ct);           // immediate, keeps history and caps
    Task<CommandOutcome> DeleteStoredDataAsync(CancellationToken ct);
    Task SetPreferencesAsync(LedgerPreferences preferences, CancellationToken ct);
    Task<HistoryModel?> GetHistoryAsync(string cardId, CancellationToken ct);
}

public enum CommandOutcome { Done, Rejected, Unavailable }
```

### 4.2 Snapshot

```csharp
public sealed record LedgerSnapshot(
    DateTimeOffset LocalNow,                    // the moment every figure and relative time refers to
    DayModel Day,
    IReadOnlyList<AccountModel> Accounts,       // user order; signed-out accounts included and flagged
    IReadOnlyList<ProviderOption> Providers,    // first run and the + menu
    SignInStripModel? SignInStrip,
    BudgetSettingsModel Budget,
    SettingsSummaries Summaries);

public sealed record DayModel(DayKind Kind, bool WorkTodayOn, DateTimeOffset? WorkTodayUntil);
public enum DayKind { WorkDay, DayOff }

public sealed record AccountModel(
    string AccountId, ProviderKind Provider, string DisplayName, AccountHealth Health,
    DateTimeOffset? LastReadingAt, DateTimeOffset? LastSyncFailedAt, DateTimeOffset? NextRetryAt,
    IReadOnlyList<LimitCardModel> Cards);       // window order; never reordered by state
public enum ProviderKind { Claude, Codex, Copilot, Antigravity }
public enum AccountHealth { Ok, SyncFailedFresh, SyncFailedStale, SignInExpired, SignedOut, ProviderError }

public sealed record ProviderOption(ProviderKind Provider, bool Added, bool SigningIn);
public sealed record SignInStripModel(SignInPhase Phase, ProviderKind Provider, string? AccountName, int? LimitsFound);
public enum SignInPhase { Waiting, Succeeded, Failed, Cancelled }
```

### 4.3 Limit card

One card per subscription limit group (D-186): a five-hour window is part of its period
card, not its own card.

```csharp
public sealed record LimitCardModel(
    string CardId,
    string? ScopeLabel,          // opaque scope or pool label: "7d · Opus", "group 1", "credits", "extra usage", "completions"
    CardLayout Layout,
    ScaleModel Scale,
    PeriodModel Period,
    CardState State,
    Freshness Freshness,
    IReadOnlyList<CardMark> Marks,
    LimitFigures Figures,
    FiveHourModel? FiveHour,     // FiveHourAndPeriod only
    ResetModel? Reset,
    CapModel? Cap,
    string? CapTargetId,         // set when a personal cap may be set or edited on this card
    CardAction Action,
    DayOffPreview? DayOff);      // day-off tooltip figures, optional

public enum CardLayout { FiveHourAndPeriod, Period, Pool, UsedOnly, Note }
public sealed record ScaleModel(ScaleKind Kind, string? UnitName, string? Currency, int? Exponent);
public enum ScaleKind { Percent, Count, Money }
public sealed record PeriodModel(PeriodKind Kind, TimeSpan? Duration, bool StartAssumed);
public enum PeriodKind { Days, CalendarMonth, Unknown }      // label "7d"/"Nd", "month", "window"

public sealed record Freshness(bool IsStale, DateTimeOffset? ReadingAt);
public sealed record CardMark(MarkKind Kind, DateTimeOffset? Since, DateTimeOffset? Until, decimal? Amount);
public enum MarkKind { OnExtraUsage, ExtraDay, SyncFailed, SignInExpired, PastReset }
public enum CardAction { None, SignIn, SetCap }

public sealed record LimitFigures(
    decimal? Used,               // U; null = unknown, never 0
    decimal? DayStart,           // U0 of the local day
    decimal? TodayEnd,           // U0 + T on a work day or in rush; U0 + T' on a day off (would-be share)
    decimal? UsualShare,         // a full day's share before a limit or cap cuts it; drives grey
    LimitValue ProviderLimit,
    decimal? EffectiveLimit,     // L; 100 for percent windows
    decimal? ProviderRemaining,  // Copilot remaining, a provider fact
    decimal? ProviderBalance,    // Codex credit balance, a provider fact
    TrackingModel? Tracking);
public sealed record LimitValue(LimitValueKind Kind, decimal? Amount);
public enum LimitValueKind { Known, Unknown, Unlimited, Zero }
public sealed record TrackingModel(DateOnly? TrackedSince, bool Incomplete);   // presence = estimate

public sealed record FiveHourModel(
    decimal? WindowShare,        // C: one 5h window as percent of the period (estimate); null = not estimated
    decimal CurrentWindowUsed,   // percent of the current 5h window
    bool CurrentWindowStarted,
    DateTimeOffset? CurrentWindowEndsAt,
    int? WindowsLeftInPeriod,    // ≈ n × 5h left (estimate, rounded down); null = estimate not ready
    int? FitBeforeReset);        // rush only: 5h windows that fit before the reset

public sealed record ResetModel(DateTimeOffset? At, DateOnly? DateOnly, ResetProvenance Provenance, DateOnly? AssumedStart);
public enum ResetProvenance { Provider, Assumed }

public sealed record CapModel(decimal Amount, bool Binding, CapStatus Status);
public enum CapStatus { Applied, Unmatched, CurrencyMismatch }

public sealed record DayOffPreview(DayOfWeek NextWorkDay, decimal ShareBefore, decimal ShareAfter);
```

> Superseded in part by [AIU-048](../AIU-048-early-session-estimate/spec.md) (D-189): `FiveHourModel` also carries `WindowShareLow`, `WindowShareHigh`, `WindowsLeftMax`, `Rough` and `Windows`, and `WindowsLeftInPeriod` is the range minimum while rough.

Contract note (2026-10-06): `CardMark` also carries `Currency`, `Exponent` and `ScopeLabel`, as implemented at `383644c..8364408` (`LedgerContract.cs`): the on-extra-usage amount keeps its pool's currency and minor-unit exponent, and `ScopeLabel` names the scope of a mark such as the five-hour past-reset mark. These members are part of this presentation contract by primary ruling under AIU-045 (ANL-21 #3, review R0). This note annotates the contract without reopening AIU-038 and changes no status.

### 4.4 Card states

`CardState` is computed by the source, never by the view model. The view model maps it to a
pill, a tone and the drawing rules of section 6.3. Tones: Ok, Attention, Critical, Neutral.

| CardState | Pill text | Tone | Reference |
| --- | --- | --- | --- |
| OnTrack | none (accessible name says OK) | Ok | A1, A5, B1, B2, C1, D1, G1, G3, H2, H10 |
| TodayLow | today low | Attention | A2 |
| TodayShort | today short | Attention | B5 |
| CapClose | cap close | Attention | C4, D4 |
| FiveHourLow | 5h low | Attention | A9 |
| FiveHourFull | 5h full | Critical | A4, A8 |
| TodayUsed | today used | Attention | B3, C2, D2, G2 |
| OverToday | over today | Critical | A3, B4, C3, D3, O6 |
| CapReached | cap reached | Attention | C5, D5 |
| OverCap | over cap | Critical | C6, D6 |
| UsedUp | 7d used up / month used up | Critical | A6, O4, R4 |
| DayOff | day off | Neutral | O1 to O3 |
| Rush | rush | Ok (pill shown) | R1 to R3, R5 |
| NotReady | not ready | Neutral | H5 |
| ValueUnknown | unknown | Neutral | H6 |
| PeriodUnknown | period unknown | Neutral | H1 |
| NotIncluded | not included | Neutral | G4 |
| LimitUnknown | limit unknown | Neutral | G5 |
| NoCap | no cap | Neutral | S3 after Remove |
| SignedOut | signed out | Neutral | H9 |

Semantics the source guarantees and the demo follows (they restate D-185 and D-187 for the
contract; the AIU-035 engine is their computation):

- **Used up** (`UsedUp`): a provider limit, not a custom cap, with `U >= L`. It wins over
  every today, day-off and rush state. A custom cap reached is `CapReached`.
- **Day off** (`Day.Kind == DayOff` and Work today off): every card whose state is a budget
  state shows `DayOff`; `UsedUp`, `CapReached` and `OverCap` stay. `TodayEnd` carries the
  would-be share `U0 + T'`, `T' = max(0, L - U0) / (Wr + 1)`.
- **Work today on**: the same figures as the day off, coloured as a work day; on-track cards
  carry an `ExtraDay` mark. It ends at local midnight and never changes the work days.
- **Rush**: the last work day before a provider-replenished reset with no custom cap, never
  for money or credit pools. `TodayEnd = EffectiveLimit`; `FitBeforeReset` is set for
  five-hour cards.
- **Five-hour window** (D-192): a current window with a known future end and `>= 100 %`
  used is `FiveHourFull` unless the card is used up, not ready, on a day off or unknown.
  Above 85 % used (under 15 % left) it is `FiveHourLow` when the card would otherwise be
  on track, rush or an attention state; critical and neutral states stay.
- **On extra usage**: an `OnExtraUsage` mark on the Claude 5h + 7d card while a window is
  full and extra-usage spend rose since; `Amount` is the spend since `Since`, in the extra
  usage pool's currency, and `Until` is when the window frees.
- **Stale**: `Freshness.IsStale` leaves the state in place for the accessible name; the
  view model shows an age pill instead (section 6.3).

Fields that need AIU-039 agreement before live wiring: `UsualShare`, `DayOffPreview`,
`HistoryModel.BaselinePerWorkDay` and the stale-pill wording. The demo fills them; a live
adapter may leave them null, and the view model then omits the grey part or tooltip line.

### 4.5 History, settings and preferences

```csharp
public sealed record HistoryModel(string CardId, PeriodModel Period, IReadOnlyList<HistoryDay> Days,
    decimal? BaselinePerWorkDay, IReadOnlyList<DateOnly> ResetDays, DateOnly Today);   // Days oldest first, at least 35
public sealed record HistoryDay(DateOnly Date, decimal? Used);   // Used null = no readings, drawn as a gap

public sealed record BudgetSettingsModel(IReadOnlySet<DayOfWeek> WorkDays, IReadOnlyList<CapSettingModel> Caps);
public sealed record CapSettingModel(string CapId, string? CapTargetId, string AccountName, string? ScopeLabel,
    ScaleModel Scale, decimal Amount, CapStatus Status, bool Binding, LimitValue ProviderLimit,
    string? ProviderCurrency, TrackingModel? Tracking);

public sealed record SettingsSummaries(TimeSpan RefreshInterval, string UpdatesSummary, int FailedSyncs,
    IReadOnlyList<ProviderKind> FailedProviders);

public sealed record LedgerPreferences(ValueMode Mode, Density Density, bool ShowSignedOut, bool AlwaysOnTop);
public enum ValueMode { Used, Left }
public enum Density { Compact, Comfortable }      // Compact is the default (D-186)
```

### 4.6 Numbers and text

- Values are in the card's scale: percent points, counts, or money in major units with the
  card's exponent. The source applies the AIU-035 display rounding; the view model formats
  only. Percentages show as whole numbers (D-186), counts with thousands separators, money
  with the currency's minor units (`$218.00`). Unknown values show an unknown mark.
- Geometry derived by the view model (bar segments, today cells, cell percentages) follows
  the reference's drawing rules exactly and rounds displayed cell percentages half away from
  zero. It never derives a budget figure.
- Relative times use `LocalNow`, never the process clock, so every view-model output is
  deterministic in tests.

## 5. Demo source

`DemoLedgerSource : ILedgerSource` holds synthetic scenarios only. No value comes from a real
account. Commands change the in-memory snapshot as a live source would, including undo
targets. Scenarios:

| Scenario | Moment | Content |
| --- | --- | --- |
| `brief` (default) | Wed 14 Oct 2026 14:20, Europe/London | The S1 scenario: four accounts, nine cards (design-brief section 4 as amended) |
| `states` | Wed 14 Oct 2026 14:20 | Every Provider States card A1 to A9, B1 to B5, C1 to C6, D1 to D6, G1 to G5, H1, H2, H5 to H10, O1 to O6, R1 to R8, one account per case named with its case ID |
| `last-work-day` | Fri 30 Oct 2026 16:10 | S10b: rush, extra usage, capped pools without rush, sign-in expired |
| `day-off` | Sat 17 Oct 2026 11:20 | S12; Work today turns it into S13 and the S10c/S10d trays |
| `first-run` | Wed 14 Oct 2026 14:20 | S6: no accounts |
| `sign-in` | Wed 14 Oct 2026 14:20 | S7 to S9: waiting, success, cancelled or failed, sign-in expired |

A demo-only "Demo" flyout in the title row switches scenarios; product builds never show it.

## 6. Surfaces and card rendering

### 6.1 Surfaces

| ID | Surface | Behavior |
| --- | --- | --- |
| S1, S2 | Main window, brief scenario, Used and Left, Compact | One column of full-width cards (D-193; originally a two-column grid where nine cards fit 760 × 600); the order is the user's; Alt+↑/↓ reorders |
| S3 | Inline rename, cap editing, undo, focus | F2 renames in place (Enter saves, Esc cancels); C or a click on the cap footer opens the cap editor in place of the footer; empty + Enter removes with undo; focused or hovered cards show History and Sign out |
| S4 | Inline history | Enter on a card expands history right under it (D-193; originally across both columns); local readings only; at least 35 days; gaps drawn dashed as "no readings", never zero; reset ticks; baseline line; ← → move the focused day; Esc closes |
| S5 | Settings panel | Ctrl+, or the gear slides a floating 320 px sheet in from the right, narrowing the cards; small labels Work days (with undo), Caps (applied, unmatched, currency mismatch; notes as tooltips), View (Density, Always on top), Updates; a footer with the refresh interval or a status problem and a ⋯ menu for diagnostics, logs, data folder, recovery export and Delete stored data (Confirm · Cancel in place); Esc closes. Amended by D-193; originally a 400 px panel with Appearance, Monitoring, Data and privacy and System status sections |
| S6 | First run | Providers listed with one-click Sign in; no bars or zero values |
| S7 | Sign-in strip and provider menu | + or Ctrl+N opens a light-dismiss provider menu (added providers disabled, Show signed-out accounts); progress in a 34 px strip under the title row with Cancel; cards stay usable |
| S8 | Sign-in success | The new account is appended with a 2 s outline; the strip says "added" with the limit count and hides after 4 s |
| S9 | Sign-in failed, sign-in expired | Failed or cancelled keeps the strip with Try again; expired marks every card of the account, dims readings and offers Sign in on the card |
| S10 | Tray miniature, brief scenario | 360 px; title row; per account its name and one today strip per limit in window order and value mode; no pills, captions, period bars or buttons |
| S10b | Tray, last work day | ⚡ (ok) on rush strips, $ (attention) on the on-extra-usage strip, red name with ⚠ for sign-in expired |
| S10c, S10d | Tray, day off and Work today | Neutral dashed strips; Work today colours them; a used-up limit stays one solid red strip |
| S11 | Tokens and components | Section 7 transfers them to `Themes/Ledger/` |
| S12, S13 | Main window, day off and Work today | Title row shows "Day off" and a Work today button; on, it is pressed, says "Extra work day · until midnight", and Ctrl+Z or a second click turns it off |

There is no modal dialog, pop-up window or account detail view. Menus and tooltips are
light-dismiss flyouts; the undo bar is an in-window overlay (36 px, 16 px from the bottom,
10 s, longer while focused).

Tray rules: signed-out accounts appear only with Show signed-out accounts; Not included cards
and pools with neither a cap nor a provider limit are left out; period unknown is an empty
dashed track; the name is red with ⚠ for SignInExpired, SignedOut, SyncFailedStale or
ProviderError, but not for SyncFailedFresh. A row click or Enter opens the window at that
account and focuses its first card; Refresh stays in the tray menu.

### 6.2 Card anatomy

Name (account name, serif 16/20) · scope label (not shown for the bare "5h + 7d" and "7d"
tags) · marks · pill (right). Then a two-column grid with a 40 px label column: row "today"
with the today strip or a today note; the over label; row "7d"/"month"/"window" with the
period bar; footer with the primary figure (left, dotted underline, tooltip) and the reset
(right), or the action button. Note-layout cards show fact lines instead of bars.

### 6.3 Drawing rules (from the reference, both value modes)

- **Used/Left.** Used mode draws used solid and the remainder hatched; Left mode the
  reverse; hatching always sits to the right of the solid fill. Hatch: 135°, 3 + 3 px.
- **Today strip.** 14 px high, cells 4 px apart, radius 6. For a five-hour card with a
  window estimate, today's allowance is split into 5h cells: the current window first
  (used part, allowed part, grey rest), then whole windows, and grey for the part of the
  last window that today's allowance does not cover; the current cell carries a "5h"
  label. Without an estimate there is one strip. Other cards have one strip from `DayStart`
  to `TodayEnd`; grey is `UsualShare - (TodayEnd - DayStart)` when positive.

  > Superseded in part by [AIU-048](../AIU-048-early-session-estimate/spec.md) (D-189): without an estimate, a paired card's today strip is one cell for the current five-hour window, its used part and a neutral track.

- **Over.** When `Used > TodayEnd` on a work day: the strip turns critical with the over
  part marked by a 2 px ink edge, and an over label shows the percent of today's share
  ("118 %" / "−18 %") for percent cards or the amount ("+$2.00" / "−$2.00") for pools. In
  Compact the label sits at the end of the strip row; in Comfortable below it.
- **Period bar.** 8 px track; used before today in `prev`; today's span ringed (16 px,
  1.5 px stroke, 4 px past each end, no glow); a cap tick (2 × 14, `ink3`) where a cap is
  below the provider limit, with the part above it hatched in card colours; an over tick
  (2 × 12, ink) at `TodayEnd` when today is over; 2 px card-colour dividers at future 5h
  window boundaries on five-hour cards.
- **Day off.** Strip outline 1 px dashed `prev`, used in neutral, rest transparent; no
  orange, red or over label. **Work today** uses work-day colours.
- **Rush.** No period bar and no grey; only the 5h windows that fit before the reset; the
  last cell is only as long as what is left.
- **Used up.** No today strip, no 5h cells, no over label, no ring; the period bar full red
  (Left mode: red hatch) and "back …" in the footer.
- **Stale.** Strip and bar at 70 % opacity, card text at `ink2`, and a neutral pill with
  the reading age ("2 h old") or "sync failed · HH:MM"; the tooltip names the last sync and
  the retry.
- **Marks.** `OnExtraUsage` "on extra usage" and `SyncFailed`, `SignInExpired`, `PastReset`
  in attention text; `ExtraDay` "extra day" in `ink2`.
- **Footer.** Percent cards: "47 % used" or "53 % left", plus " · ≈ 4 × 5h left" when
  ready, or " · 2 × 5h fit before the reset" in rush. Capped pools: "≈ $218.00 of $300.00
  cap" / "≈ $82.00 left to cap" / "≈ $12.60 over cap" (≈ only for tracked estimates).
  Countable pools without a cap: "1,210 of 2,000 used". Reset: "resets Mon 09:00",
  "resets 1 Nov", "resets 1 Nov (assumed)", "back Fri 09:30", "reset 14:00" when past.
- **Tooltips** repeat or add secondary facts (window end, remainder beyond a cap, provider
  limit, balance, tracked since, sync details) and are shown on keyboard focus as on hover.

## 7. Tokens and styles

Dark only (D-182): the window, panel, flyouts, tray and title bar use these tokens whatever
the ordinary Windows app mode; no theme dictionaries and no system brushes in owned content.
Native chrome is retained. Contrast-theme behaviour is outside the amended scope.

| Token | Value | Role |
| --- | --- | --- |
| bg.page | #1F1E1D | window, title row |
| bg.card / card.top | #262624 / #2A2927 | card, vertical gradient top to 60 % |
| bg.panel / bg.hist | #2F2D2A (D-193, was #232220) / #211F1D | settings sheet, history block |
| bg.input / bg.tip | #1A1918 / #151413 | text box; tooltip, menu, undo bar |
| line / line.card / line.ctl | #2E2D2A / #33322F / #55534E | dividers, card border, outline buttons |
| ink / ink2 / ink3 | #F0EEE6 / #B7B3A8 / #9D998E | text levels |
| rail / prev / grey | #34332F / #8C887E / #75716A | bar track, used before today, not available today |
| ok m / p / text | #7FB98F / #5E8A6B / #7FB98F | fill, hatch, text |
| att m / p / text | #E58A3C / #A8652E / #F0A060 (D-NEW, was #D97757 / #A35C44 / #E78E6E) | orange states |
| crit m / p / d / text | #E5604A / #B04A3A / #6E2E25 / #F78570 | red states; destructive Confirm #B04A3A |
| neutral m / p | #B7B3A8 / #6C6963 | no-budget states, stale |
| button.primary | #E8E4DA on #1A1410 | Save, Sign in, Undo, Try again |
| focus | #F0EEE6, 2 px, offset 2 px | every focusable element |
| pill bg | state m at 14 % (crit 16 %, neutral 12 %) over the card | pills |

Implementation derivation, 2026-10-02: the three adjusted tokens above satisfy AC-07
on fresh-card pills and page surfaces. Original attention/critical text failed 4.5:1;
the original neutral dashed track fell below 3:1. Acceptance thresholds are unchanged.

Type ramp (effective px): first-run heading Source Serif 4 20/26 600; card name and panel
title Source Serif 4 16/20 600; window title and tray name Source Serif 4 14/18 600; body,
figures and labels Hanken Grotesk 12/16 400 and 600 with tabular numerals; section heads
Hanken Grotesk 12/16 600 caps +4 %; intro text Hanken Grotesk 13/19; clock IBM Plex Mono
12/16 tabular; 5h cell label 10/12 600 (the only text below 12 px, as in the reference).
Names outside the fonts' scripts fall back to Segoe UI Variable.

Sizes: title row 38; Compact body padding 12, grid gap 8, card padding 8 12, inner gap 6,
strip–bar gap 3, card about 97 high; Comfortable body padding 14, gap 12, card padding
13 15 12, inner gap 10, strip–bar gap 8, card about 123 high; card radius 20, border 1;
pill 20 high, padding 0 9, dot 6, gap 5; buttons 22–24 high, radius 6; icon buttons 24
(28 in the title row); text boxes 24–26; switch 32 × 18; tray 360 wide, row padding 9 14,
name column 150 + 10, strips 8 high, 5 apart, cells 3 apart, radius 4, mark column 12.

Corners: squircle on every rounded element except pills and dots, which stay round
(PD-038-02). Motion: bar and strip widths 200 ms decelerate; panel and history 250 ms;
strip, undo bar and menu 150 ms fade; tooltip 400 ms delay, 100 ms fade; sign-in progress
1.4 s indeterminate. With Windows animation effects off every change is instant and
progress becomes static "waiting" text.

## 8. Keyboard and accessibility

- Tab order: Work today (day off only) → Used/Left → + → Settings → sign-in strip action →
  cards in reading order → undo bar.
- On a card: Enter history · F2 rename · C edit cap · Tab reaches History and Sign out ·
  Alt+↑/↓ reorder · ← → move between today cells, bar and footer with their tooltips.
- Window: Ctrl+, settings · Ctrl+N add account · Ctrl+Z undo · F5 refresh · Esc closes the
  panel, menu or history. Editors: Enter saves, Esc cancels. Confirm · Cancel: focus moves
  to Cancel; Enter on Confirm deletes; Esc cancels.
- Tray: ↑ ↓ between accounts, ← → between strips, Enter opens the window at that account,
  Esc closes.
- Accessible names follow the S11 table with state words, for example "Codex Pro, 7 day
  limit used up, back Friday 09:30" and "Claude Pro, 5 hour and 7 day, OK. Today 2
  five-hour windows, 28 percent of the current window allowed. 7 day 47 percent used,
  about 4 windows left, estimate. Resets Monday 09:00." Controls: "Add account",
  "Settings", "Show values: used", "Sign out Claude Pro", "History, Claude Pro 7 day",
  "Cap for Codex Pro credits, 17,000". State changes are announced through the existing
  live-region pattern.
- Every coloured state also has a word or a shape; the focus indicator is 2 px ink at
  offset 2.

## 9. Pending decisions

| ID | Question | Options | Recommendation | Impact |
| --- | --- | --- | --- | --- |
| PD-038-01 | How does the demo reach the new presentation without switching the product (D-183)? | (a) `--demo --ledger` with a small guarded branch in `App.xaml.cs`; (b) a separate demo executable project | (a): one isolated hook, rebased once after AIU-042 | `App.xaml.cs` is the only existing file touched besides the backlog entry |
| PD-038-02 | Corner implementation (resolved by owner amendment 2026-10-03) | Keep implemented squircle surfaces and small-radius controls; retain native window/tray contours | No custom native chrome or exceptional-scale/performance work for corner parity | The former remaining corner requirement is removed, not claimed implemented |
| PD-038-03 | Fonts (resolved 2026-10-02) | Source Serif 4, Hanken Grotesk and IBM Plex Mono static .ttf from their official repositories, SIL OFL 1.1, licence and tabular figures checked before merge | Owner explicitly approved the four named files after sources and sizes were presented | Package the files and licences, use explicit regular/semibold resources; provenance and validation are in verification.md |
| PD-038-04 | Monitoring, Updates and System status sections | (a) collapsed rows with the contract summaries, expanding inline to the existing section views; (b) summary rows only, full content in AIU-039 | (a) when the existing views host cleanly in the panel; otherwise (b), recorded | Settings completeness in the demo |
| PD-038-05 | Contrast-theme native chrome (resolved by owner amendment 2026-10-03) | Contrast-theme support/testing is excluded; retain native chrome | Follow ordinary personal desktop use; no replacement frame | Former contrast-theme failure is historical evidence, not a current acceptance gate |

## 10. Acceptance

- AC-01: The presentation contract of section 4 exists as written under
  `Features/Ledger/Contract/`, carries every D-187 state (today strip, would-be share on a
  day off, Work today, rush, on-extra-usage mark, used-up limit, tray miniature inputs), and
  no `Features/Ledger/` type references a Core or Infrastructure namespace (test).
- AC-02: View-model tests with demo data cover every card state of section 4.4 and the
  section 6.3 drawing rules in both value modes and both densities: pill text and tone,
  strip cells and grey, over label, period bar segments and ticks, used-up, day off, Work
  today, rush with `FitBeforeReset`, stale and marks, footer and reset text, and accessible
  names.
- AC-03: The S1 and S2 demo reproduces the reference scenario: design-brief 4.3 positions
  of `U0`, `U` and `U0 + T` as a share of `L` to 0.1 point for A2, A3, B2, B3, C1, C2 and
  D1 (A3's bar is drawn on its USD 500.00 provider scale with the cap tick at 60 %, as in
  the reference), the reference
  whole-percent and money figures, and the card pills OnTrack (none), "over today" (Claude
  extra usage), "7d used up" (Codex), "not included" (Copilot premium), "sync failed ·
  13:38" (Antigravity group 1) and "period unknown" (group 2) (PA-3 as amended by D-186).
- AC-04: Every surface S1 to S13 and every reference state is reachable in the running demo
  app through the scenarios of section 5, with no modal dialog, pop-up window or account
  detail view (PA-1, PA-11, backlog acceptance 5).
- AC-05: Rename, cap set, change and remove, unmatched-cap removal, work-day change, Work
  today, Confirm · Cancel for Delete stored data, undo (bar and Ctrl+Z), sign-in, cancel,
  sign-out, reorder and history open and close work through view-model commands with tests
  for Enter, Escape, invalid input and undo restoration.
- AC-06: The tray miniature view model follows section 6.1's tray rules, with tests for
  exclusions, the red name, ⚡, $, the dashed period-unknown track, the solid used-up strip
  and opening the window at an account.
- AC-07: Tokens of section 7 are the only colour source of the new views. A test reads
  `Themes/Ledger/Tokens.xaml` and checks WCAG 2.2 contrast, truncated to two decimals:
  text levels and state text at least 4.5:1 on every surface they use, state marks and the
  focus indicator at least 3:1 against what they touch (PA-8).
- AC-08: S1 and S2 fit the default Compact window without clipping in the owner's
  ordinary desktop configuration. No display-scale or unusual-screen matrix is required.
- AC-09: Ledger has its dark appearance in ordinary use. Retain native window/tray
  chrome and contours; contrast-theme support and testing are excluded.
- AC-10: Keyboard access per section 8, a visible focus indicator, tooltips on focus,
  and ordinary interactions work. No Narrator or accessibility-settings matrix is required.
- AC-11: Each packaged font's licence is verified against its official page and recorded
  before merge, the files are .ttf, and figures use tabular numerals (PA-12, backlog
  acceptance 6).
- AC-12: No edit outside section 3's paths except the PD-038-01 hook, approved font Content declaration, AIU-038
  records and the owner-amended AGENTS.md/verification policy; product launch and `--demo` behave as before; all suites, the document
  validator, the build and `git diff --check` pass.

Design-brief rows and their reading after D-186 and D-187:

| Row | Covered by | Reading |
| --- | --- | --- |
| PA-1 | AC-02, AC-04 | The reference state list (A to R, S1 to S13) replaces the brief section 3 list where D-186 superseded it |
| PA-2 | AC-10 | Decision figures are visible; secondary facts are in tooltips reachable by keyboard focus (D-186) |
| PA-3 | AC-03 | Reference figures, whole percentages; card pills replace the account status |
| PA-4 | AC-02 | Replaced by the today strip: 5h full is orange, used up red, no hour splits |
| PA-5 | AC-02, AC-03 | Unknown is a word, not 0; not-ready estimates hidden; reasons in words |
| PA-6 | AC-02 | ≈ for estimates, "(assumed)", tracked since, incomplete, period unknown, stale dimmed with time |
| PA-7 | AC-07 | Reference tokens and type pairing; D-186 lifted the DA-2 restrictions |
| PA-8 | AC-07, AC-10 | Contrast test plus words or shapes for every coloured state |
| PA-9 | AC-10 | Implemented and checked, not only specified |
| PA-10 | AC-08 | Ordinary desktop layout only, per owner amendment 2026-10-03 |
| PA-11 | AC-04, AC-05 | As written |
| PA-12 | AC-11, AC-12 | Built in WinUI 3, fonts .ttf with named licences |
| PA-13 | AC-07 | Token and component values transferred to XAML resources |

## 11. Verification plan

- **Automated.** Presentation tests (contract boundary, demo fixtures, view models, tray,
  commands, contrast), Infrastructure tests unchanged, document validator, Windows project
  build in Debug and Release, the package build per README, `git diff --check`.
- **Interactive, local unpackaged demo** (`--demo --ledger`), each recorded as PASS, FAIL,
  NOT_RUN or BLOCKED with environment and time:
  - S1 to S13 and the `states` scenario against the reference pages, side by side;
  - ordinary layout using the owner's current desktop settings; no host-setting changes;
  - keyboard-only walk-through of section 8, focus visibility and tooltips on focus;
  - product launch and plain `--demo` still show the current views.
- **Not claimed.** Live provider data, packaged install and the product switch (AIU-039).
