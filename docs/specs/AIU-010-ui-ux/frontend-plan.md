# AIU-010 mock-first frontend plan

Executes the owner brief in [frontend-brief.md](frontend-brief.md) under [spec](spec.md) scope version 2. [tasks.md](tasks.md) is the only progress and resume record; this plan does not track status. After interruption or compaction read the brief, this plan and tasks.md before editing.

## Intended result and acceptance checks

A native WinUI 3 / .NET 10 application whose default startup is a fully bound, CommunityToolkit.Mvvm-based demonstration of every surface in the imported design, running only on committed synthetic data and deterministic mock services. Acceptance: AC-03–AC-06, AC-09–AC-11. Checks, in order of cost: presentation view-model tests, document validation, `git diff --check`, native Debug/Release build, MSIX package build, actual Windows launch with screenshots in Light and Dark, keyboard/scaling/tray/Exit checks. Backend integration (AC-07) stays with Codex.

## Design source

| Item | Value |
|---|---|
| MCP | claude_design, `https://api.anthropic.com/v1/design/mcp`, authorization via `/design-login` |
| Project | `https://claude.ai/design/p/67605f1d-8aa1-4297-8b8b-b8eeb62c902e?file=AI+Usage+App.dc.html` |
| Files | `AI Usage App.dc.html` (implement), `support.js` (imported by selection); whole project readable |
| Revision / identifier | "Quiet Editorial (1b) · revision 1 · 2026-09-15", with SHA-256 per file in [design-reference/README.md](design-reference/README.md). The MCP exposes no version field |
| Local snapshot | [design-reference/](design-reference/README.md): App prototype, `support.js`, Design Specification, Reference Views, Directions (historical). No authentication material |

The Design Specification is the authoritative written companion. It supplies tokens (§3), type and spacing, the component and state catalog (§4), motion (§5), loading vs refreshing (§6), themes (§7), responsive rules (§8), accessibility (§9), English copy keys (§10), S01–S12 coverage (§11), proposed contract extensions (§12) and open owner choices (§13). Implement it together with the prototype logic. Where the two disagree, the prototype's current behaviour wins, because the specification records it as the post-review state.

### Design structure (imported)

- Window: one layer on `bg`, top navigation Overview · Accounts · History · Settings · System Status (WinUI `NavigationView` `PaneDisplayMode=Top`, accent underline), header actions Refresh all and Add account, a compatibility banner under the header when blocked, and a dismissible Refresh-all result bar.
- Settings tabs: Appearance (S05), Monitoring & notifications (S06), Data & privacy (S10), Updates (S12).
- Overlays: Add account sheet with tabs Sign in (S03) and Import from CLI (S08); confirm dialog (plain, destructive, alternate action, typed `RESET`, busy); notification toast preview; tray popup and tray menu (S07); hidden-to-tray state; the blocking Recovery surface (S11) that replaces the shell.
- Demo shell (outside the app frame in the prototype): the "Demo · sample data" label, scenario selector (F01, F02, F03, F06, F13a/b/c, F14a/b, F15), simulated Windows mode Light/Dark, viewport width, Reduced motion and High contrast toggles, Clock +1 h, Reload (skeleton), Tray popup, Tray menu, Hide to tray/Restore window, and the demo clock text. In-page demo simulators: connect outcomes (Approve, Approve · no quota, Deny, Let it expire, Duplicate identity), replace-import candidate valid/invalid, update check failure, compatibility/security simulation, and OS notifications/quiet hours/battery saver toggles.
- The default scenario is F02. It embeds F04 (Work stale with NetworkFailure, then fresh 39), F07 (Research Workspace A/B with shared pool `demo-shared-1`), F08 (Antigravity exhausted with reset passed; Claude extra usage money and credits with no currency) and F09 (History `cl-a-w1` 24h gap and reset). F05, F10, F11 and F12 are reached through interactions rather than scenario selection.
- Timings in the prototype are mock latencies: load 1300 ms, refresh 1400 ms, refresh-all result 1700 ms, connect 900 ms, CLI candidate 550 ms each, import 750 ms each, history 600 ms, health step 500 ms, export 400 ms × 3, replace validation 800 ms and processing 1200 ms, disconnect 1000 ms, update check 1200 ms, download 350 ms × 5, restart 1300 ms, recovery retry 1700 ms, restore 1500 ms.

### Design decisions that differ from earlier AIU-010 documents

The imported design records these as owner review decisions. The owner `/goal` of 2026-09-15 settles precedence: visual approval does not authorize changing quota semantics, notification threshold defaults or removing required override scopes. Written functional requirements (accepted decisions, ui-contract.md, screens.md, fixtures.json) are preserved and the selected visual design is adapted to them. "Resolution" is what the implementation does. Contract extensions go into ui-contract.md in the phase-1 diff. Items marked "owner" are open choices reported at delivery; none blocks implementation.

| # | Earlier text | Imported design | Resolution (implemented) |
|---|---|---|---|
| D1 | Remaining-percent thresholds `[25,10,0]` with Global → Provider → Account → Window scopes (D-123, ui-contract, S06, F10) | Used-percent integers 1–99 of any count; default `[50,80]` used; Global plus provider × window type; highest threshold critical; per-account overrides dropped | Written semantics kept: `NotificationRule.remainingThresholds` stays remaining-percent, unique whole numbers 0–100, any count, default global `[25,10,0]`. Global, Provider, Account and Window are all editable. The design's provider × window type scope is kept as an extra `WindowType` scope (target `providerId::windowLabel`); a window resolves Window → Account → WindowType → Provider → Global. Values are entered and shown in the current usage-display unit and converted without mutating stored values. Severity: `0` means exhaustion; the smallest crossed nonzero threshold is critical and larger ones are warnings (the design's "last threshold is critical" applied to nonzero thresholds). Owner: placement of `WindowType` |
| D2 | No setting for used vs remaining; D-079 remaining percentage is primary | `Preferences.usageDisplay` Used/Remaining, default Used | Display-only extension, default **Remaining** (D-079). Used is derived for display only and never stored. Owner: default Used if D-079 is amended |
| D3 | S01/D-115 counts, lowest known remaining and nearest reset with source/freshness, warning/critical/reauth counts | Headline summary strip removed; tray "n accounts · m need attention" | Summary restored above the provider rows with the specification's headline-stat component (§4, display-40/300) and scope line (`Overview.Summary.Scope`); shared pools counted once. Provider grouping and manual order kept |
| D4 | D-119 status cannot rely on colour alone | ▲/● glyphs and used/left suffix removed | Glyphs restored in all themes: ▲ warning, ● critical, "0 %" exhausted, dotted hatch plus text for unknown/unavailable, dashed pattern plus "Unlimited", ◷ stale. Values carry "left"/"used". Ticks kept |
| D5 | Failure with Retry (spec §6 and §12 "no retryAt → immediate Retry") | Retry removed; row Refresh is the manual retry | Recoverable failure shows Retry. A future `retryAt` adds "Automatic retry at …"; `RateLimited` disables Retry until `retryAt` ("Retry available at …"). Sign-in required shows Reconnect |
| D6 | D-121 neutral fallback marks until brand review | Glyph tiles with provider hues pending rights review | Neutral tiles by default (card2 surface, ink glyph). Hue dictionary kept; demo switch "Provider tile hues (pending rights review)" shows the designed colours. Owner: hues after rights review |
| D7 | Scenario catalog F01–F15 | Selector splits F13 a/b/c and F14 a/b; omits F04/F05/F07–F12 | Selector lists every F ID plus the prototype sub-scenarios. F04–F12 load their seed and open the surface that demonstrates them |
| D8 | D-093 disconnected accounts hidden by default | `showDisconnected` default true | Default false; Show disconnected in Accounts and Settings › Appearance |
| D9 | D-126 tray: Re-auth > Offline/Error > Critical > Warning > Normal | sign-in › exhausted › critical › warning › stale › ok | Re-auth › failure › exhausted › critical › warning › stale › normal; tooltip names cause, freshness and account |
| D10 | D-122 dashboard sparklines; S04 "sparkline and detailed chart" | No sparkline | 24 h sparkline in the account detail hero and in expanded Overview rows, from `IHistorySource` |
| D11 | D-106 power override; D-168 Preview may override a compatibility block | Simulation toggles only | "Reduce automatic refresh on battery saver or metered connection" switch (default on). Preview channel offers a local compatibility-block override; security blocks never |
| D12 | Existing lifetime has an in-window Exit | Exit only in tray menu, confirmed | Tray menu Exit with confirmation as designed, plus "Exit AI Usage" in System Status and Ctrl+Q, both confirmed, so Exit never depends on the notification area |

### Imported surfaces → S IDs

| Design surface | S | Key states in the prototype |
|---|---|---|
| Overview route | S01 | Skeleton (4 rows), first-run empty, all-hidden note with "Show all", provider sections, account rows (hover-reveal refresh/pill, Updating… + Cancel, ✓ Updated, failure + auto-retry time, hidden tag, disconnected 60 %, Show all / Show less expansion with other groups, shared-pool note, extensions and contexts note), drag and Alt+↑/↓ reorder, compact two-line layout < 720 px, Refresh all busy/result bar, compatibility banner |
| Accounts route (list + detail) | S02 | Show hidden / Show disconnected filters, empty list, selection, hero value, pill, refresh/ack, Reconnect/Connect primary, "…" menu (Move up/down, Hide…, Mute/Unmute alerts, Delete stored data…, Disconnect…), inline rename with validation, context segmented control, groups with chevron, expansion label, shared-pool and hidden badges, Hide/Unhide, collapsed summary, window rows with ticks/abs amounts/muted tag/reset text, extensions, info lines, last-observation line, disconnected note, failure alert |
| Add account sheet · Sign in | S03 | Provider pick (Available / Planned · demo only), method segmented, Connect/Continue, Connecting…, Waiting for authorization with Cancel and "Enter a code instead", code entry with validation (≥ 8 chars), Verifying, result (ok/neutral, Open account), deny/expire/cancel notes, duplicate, reconnect, simulator buttons |
| Add account sheet · Import from CLI | S08 | Idle with explicit "Scan this PC", scanning with progressive rows and Stop, no candidates, found with checkboxes, Import n selected, per-item Importing…/results, cancel import, Re-import, Rescan |
| History route | S04 | Account and window selects, presets 24h/7d/30d/90d/1y/Custom, custom From/To with 3 validation errors, loading skeleton, empty with reason, disabled-collection banner, SVG chart with segments, gap bands, reset lines, focusable points, tooltip, axis ticks, range text |
| Settings › Appearance | S05 | Theme radio cards with miniature previews and System note, density Comfortable/Compact, always-on-top switch, show disconnected switch, order and visibility list (▲▼, Hide/Show, Mute/Unmute, Alt+↑/↓) |
| Settings › Monitoring & notifications | S06 | Usage display Used/Remaining, threshold rules (global plus provider × window type; Edit/Override, inline validation, Save/Cancel, Reset to inherited), reset notice switch, delivery status (OS notifications and quiet hours toggles), automatic refresh policy (battery toggle), Preview notification toast with deep link |
| Settings › Data & privacy | S10 | History collection switch and retention select, Export (Prepare export…, determinate bar with Cancel, preview list, Save file disabled with reason), Replace from bundle (candidate valid/invalid, Validate, invalid alert, valid summary, Replace… confirm, processing, done), Reset settings, Factory reset with typed RESET, Delete stored data per account |
| Settings › Updates | S12 | Channel Stable/Preview, Check (Checking + Cancel), check failure, Available with Download, Downloading n %, Ready with Restart & update confirm, WaitingForStable, compatibility and security simulation, last-checked text |
| System Status route | S09 | Build/schema, refresh status, storage, update summary with link, provider list, health check (5 progressive steps, bar, Cancel, Warning result, no repair), diagnostics preview and log preview toggles, link to Data |
| Tray popup / tray menu / hide to tray | S07 | Severity-sorted rows with mini meter, per-row refresh and open, Refresh all, count line, footer "Sorted by severity · no charts"; menu Open / Refresh all / Settings / Exit with confirm; hidden-to-tray placeholder with animations stopped |
| Recovery surface | S11 | Interrupted / NewerSchema (Retry disabled with reason) / RestoreFailed, Retry with determinate progress and failure message, checkpoint list with destructive restore confirm and progress (fails in F13c), diagnostics preview, data-folder preview note |
| Confirm dialog, toast, compatibility banner, skeleton | shared | Used across S01, S02, S06, S07, S10, S11, S12 |

Design coverage gaps against earlier written requirements, closed by extending established components:

- S01 filters are limited to show hidden/disconnected, plus the Accounts filters. No other Overview filter is designed, so none is added.
- S02 drag reorder exists only on Overview; detail uses menu Move up/down, which matches the design.
- S03 method-specific copy is generic.
- S04 context and group filters are folded into the window select labels ("Workspace A · Usage limits · Session window").
- S06 account and window overrides are not drawn in the design; they reuse the designed rule row inside a per-account expander (D1).
- S09 shows no separate storage/schema error state; `SystemStatus.health`/`recovery` cover it.
- S12 `Unsupported` update state appears only as text.
- F15 "Hide to tray → no decorative animation" is shown as a placeholder in the prototype; natively the window really hides.

## Architecture

The agreed three-project architecture is unchanged: `AiUsage.Core`, `AiUsage.Infrastructure` and `AiUsage.Windows`. No project is added. All presentation work lives in `AiUsage.Windows`, organized by feature. A fourth project needs a concrete requirement that cannot reasonably be met this way, plus explicit owner approval first (owner instruction 2026-09-15).

```text
src/windows/AiUsage.Windows/
  App.xaml(.cs), MainWindow.xaml(.cs)   composition root and native window lifetime only
  Features/
    Presentation/        shared presentation contracts (UiSnapshot, AccountItem, ContextItem, GroupItem, WindowItem,
                         NativeAmount, ExtensionItem, CapabilityItem, FailureItem, Preferences, NotificationRule,
                         HistoryQuery, HistoryPoint, SystemStatus, UiCommand, UiCommandResult), host abstractions
                         (IUiDispatcher, INavigationService, IDialogService, IThemeService, IMotionSettings,
                         IAnnouncer, ITextResources, IAppLifetime, IClock) and shared view-model helpers
    Demo/                DemoClock, DemoScenarioCatalog (F01–F15 seeds), DemoState, all mock service
                         implementations, DemoControlViewModel, DemoPanel view
    Shell/               ShellViewModel, navigation keys, ShellPage, banners, result bar, ConfirmDialog, ToastPreview
    Overview/            OverviewViewModel, ProviderSectionViewModel, AccountRowViewModel, OverviewPage
    Accounts/            AccountsViewModel, AccountDetailViewModel, Context/QuotaGroup/QuotaWindow view models,
                         IUsageSource, AccountsPage
    Connection/          AddAccountViewModel, IConnectionFlow, AddAccountDialog (Sign in and Import from CLI tabs)
    CliImport/           CliImportViewModel, CliCandidateViewModel, ICliImportService
    History/             HistoryViewModel, IHistorySource, HistoryPage
    Settings/            SettingsViewModel (tabs), Appearance/, Monitoring/, DataPrivacy/, Updates/ — each with its
                         view model, service interface (IPreferenceStore, INotificationPreview,
                         IDataManagementService, IUpdateService) and page
    SystemStatus/        SystemStatusViewModel, IDiagnosticsService, SystemStatusPage
    Recovery/            RecoveryViewModel, IRecoveryService, RecoveryPage
    Tray/                TrayViewModel, TrayPopup, tray menu wiring
  Platform/              WinUI implementations of host abstractions (navigation, dialogs, theme, motion settings,
                         announcer, dispatcher, ResourceLoader text resources, tray host via H.NotifyIcon, demo-only
                         preference file)
  Controls/              QuotaMeter, StatusPill, FreshnessLabel, SkeletonBlock, RefreshButton, OperationStatus,
                         ProviderGlyphTile, HistoryChart, Sparkline
  Themes/                token dictionaries (Light/Dark/HighContrast), type ramp, control styles
  Motion/                duration/easing resources and reduced-motion-aware animation helpers
  Strings/en-US/Resources.resw
tests/windows/
  AiUsage.Presentation.Tests/    compiles AiUsage.Windows Features/**/*.cs, excluding *.xaml.cs, into a platform-neutral
                                 assembly (the existing linked-source pattern); keeps Core reference for the retained
                                 DashboardWorkflowTests
  AiUsage.Windows.Tests/         FlaUI smoke scenarios updated to the new automation IDs
```

Testability rule: every non-XAML `.cs` file under `Features/` must compile without WinUI, and `DependencyBoundaryTests` enforces it. Views (`*.xaml` with `*.xaml.cs`) sit beside their view models in the same feature folder. Code-behind is limited to view concerns and is excluded from the test compile. WinUI-dependent service implementations go in `Platform/`, `Controls/`, `Themes/` and `Motion/`, never in `Features/`. The view models then stay testable in the existing test project without a new assembly.

### Adapter interfaces (Codex boundary)

All methods take a `CancellationToken`, return typed records, and never return formatted text or raw exceptions. Snapshot publication is marshalled to the UI thread by the consumer through `IUiDispatcher`.

| Interface | Members (planned signatures) | Mock |
|---|---|---|
| `IUsageSource` | `UiSnapshot Current`; `IDisposable Subscribe(Action<UiSnapshot>)`; `Task<UiCommandResult> ExecuteAsync(UiCommand, CancellationToken)` covering account, connection, ordering, visibility, expansion and refresh command kinds | `DemoUsageSource` |
| `IConnectionFlow` | `IAsyncEnumerable<ConnectionStage> ConnectAsync(ConnectRequest, CancellationToken)`; `Task<UiCommandResult> SubmitCodeAsync(string transientCode, CancellationToken)` | `DemoConnectionFlow` |
| `IHistorySource` | `Task<HistoryResult> QueryHistoryAsync(HistoryQuery, CancellationToken)` | `DemoHistorySource` |
| `IPreferenceStore` | `Task<Preferences> LoadAsync(...)`; `Task<UiCommandResult> SetPreferenceAsync(PreferenceChange, ...)`; `Task<UiCommandResult> SetNotificationRuleAsync(NotificationRule, ...)`; `Task ResetSettingsAsync(...)` | `DemoPreferenceStore` (memory plus optional demo-only file) |
| `INotificationPreview` | `Task<NotificationPreviewResult> PreviewAsync(NotificationTarget, ...)`; `NotificationEnvironment Environment` (quiet/OS-blocked/power/metered) | `DemoNotificationPreview` |
| `ICliImportService` | `IAsyncEnumerable<CliCandidate> DiscoverAsync(...)`; `IAsyncEnumerable<CliImportOutcome> ImportAsync(IReadOnlyList<string> candidateIds, bool reimport, ...)` | `DemoCliImportService` |
| `IDiagnosticsService` | `IAsyncEnumerable<HealthCheckStep> RunHealthCheckAsync(...)`; `Task<DiagnosticsPreview> PreviewDiagnosticsAsync(...)`; `Task<LogPreview> PreviewLogsAsync(...)` | `DemoDiagnosticsService` |
| `IDataManagementService` | `PreviewExportAsync`, `ValidateReplaceImportAsync`, `ApplyReplaceImportAsync`, `FactoryResetAsync`, `DeleteAccountDataAsync`, `PreviewDataFolderAsync` | `DemoDataManagementService` |
| `IRecoveryService` | `RecoveryState State`; `RetryAsync`, `ListCheckpointsAsync`, `RestoreCheckpointAsync` | `DemoRecoveryService` |
| `IUpdateService` | `UpdateState State`; `CheckAsync`, `SetChannelAsync(UpdateChannel)`, `RestartAndUpdateAsync` | `DemoUpdateService` |
| `IClock` | `DateTimeOffset UtcNow`; `ITimer CreateTimer(TimeSpan, Action)` | `DemoClock` (fixed 2026-09-15T12:00:00Z, advance/pause) |

Each interface lives in the feature folder that consumes it. Mocks live in `Features/Demo/`.

Host abstractions (`Features/Presentation/`) that `Platform/` implements and tests fake: `IUiDispatcher`, `INavigationService` (typed `PageKey`, back stack), `IDialogService` (typed dialog view models, confirmation), `IThemeService` (System/Light/Dark, system change events, high contrast), `IMotionSettings` (reduced motion), `IAnnouncer` (UIA notifications), `ITextResources` (resource key lookup), `IAppLifetime` (show, hide to tray, Exit). `DemoControlViewModel` and `IDemoScenarioController` (select scenario, advance clock, force outcome, reset) exist only in demo composition.

### Composition

`App.xaml.cs` builds the existing `Microsoft.Extensions.Hosting` container from `AddPresentationFeatures()`, `AddPlatformServices()` and `AddDemoServices()` only. These registration extensions live in `src/windows/AiUsage.Windows/Composition/` rather than `Features/`, so the platform-neutral test compile needs no dependency-injection package. The Windows csproj keeps its existing `ProjectReference`s to Core and Infrastructure, so project wiring stays unchanged for Codex. However, no new or retained Windows source outside the untouched backend projects may use `AiUsage.Core` or `AiUsage.Infrastructure` namespaces, register provider sessions or touch provider storage. `DependencyBoundaryTests` scans `AiUsage.Windows` sources (excluding `bin`/`obj`) to enforce this for this delivery. Close-to-tray, tray restore and explicit Exit keep the existing lifetime semantics through `IAppLifetime`. Codex later adds adapter implementations and an `AddLiveServices()` registration inside `AiUsage.Windows` behind the same interfaces, relaxing the boundary test only for that adapter folder.

## Dependencies

Existing and retained: `Microsoft.WindowsAppSDK 2.4.0`, `Microsoft.Windows.SDK.BuildTools 10.0.26100.4654`, `CommunityToolkit.Mvvm 8.4.2`, `H.NotifyIcon.WinUI 2.4.1` (tray icon, context menu, popup), `Microsoft.Extensions.Hosting 10.0.12`.

Assessment after import: every component in spec §4 maps to built-in WinUI controls (`NavigationView`, `SelectorBar`/`RadioButtons`, `ItemsRepeater`, `ListView`, `ContentDialog`, `InfoBar`, `ToggleSwitch`, `ProgressBar`/`ProgressRing`, `TeachingTip`, `MenuFlyout`) or to small custom controls (meter, skeleton, history chart drawn with `Path`). No package is required yet. The candidates below are added only if a concrete implementation step shows a material benefit. Before adding, verify licence, maintained version, WinUI 3/.NET 10/MSIX compatibility and the absence of telemetry, then pin it and record the reason in tasks.md:

| Candidate | Purpose | Default if not needed |
|---|---|---|
| `CommunityToolkit.WinUI.Controls.SettingsControls` (MIT) | Settings cards/expanders | Plain XAML templates |
| `CommunityToolkit.WinUI.Controls.Segmented` (MIT) | Segmented theme/range pickers | `RadioButtons` template |
| `CommunityToolkit.WinUI.Animations` / `.Behaviors` (MIT) | Implicit show/hide, expansion and number transitions | Composition/Storyboard helpers in `Motion/` |
| `CommunityToolkit.WinUI.Converters` (MIT) | Visibility/bool converters | `x:Bind` function bindings |
| Chart library (e.g. LiveChartsCore WinUI, MIT) | History detailed chart | Custom `Path` segments per `segmentId`, preferred unless the design requires interactions a custom control cannot reasonably provide |

Package restore needs network access. The brief authorizes restore of selected UI packages. Record any restore failure as BLOCKED.

## Phases and steps

Each phase ends with its checks passing and a tasks.md update. Phases run in dependency order. The task IDs are in tasks.md.

### Phase 0 - Design import (T-03)

1. Read the project metadata and file list, then read the prototype, `support.js`, the specification, the reference views and the directions study.
2. Save them under `design-reference/` with a README giving the revision and hashes.
3. Enumerate surfaces and map them to S IDs (above). Tokens, type, motion and copy stay in the specification; phase 2 transcribes them into XAML resources and `Resources.resw`, reading the saved specification directly.
4. Record design decisions that differ from earlier documents (D1–D7, extended to D12 and resolved per the owner `/goal`) and design gaps (above).

### Phase 1 - Presentation contracts, mock services and composition (T-04)

Paths: `src/windows/AiUsage.Windows/Features/{Presentation,Demo}/**`, service interfaces in the consuming `Features/<Feature>/` folders, `App.xaml(.cs)`, `tests/windows/AiUsage.Presentation.Tests/**`. The solution and project list do not change.

1. Update `tests/windows/AiUsage.Presentation.Tests/AiUsage.Presentation.Tests.csproj` to compile `../../../src/windows/AiUsage.Windows/Features/**/*.cs` excluding `**/*.xaml.cs`, and extend `DependencyBoundaryTests` with the testability and no-backend-usage rules above.
2. Implement the contract records/enums from ui-contract.md in `Features/Presentation/` with nullable measurements and native units, plus the design-required extensions from specification §12 as resolved in D1/D2: `Preferences.usageDisplay` (default Remaining), the additional `WindowType` rule scope with remaining-percent thresholds, `WindowItem.primary`, `ContextItem.available`, `SystemStatus.updateVersion/updateProgress/refreshPolicy/notificationsAllowed/quietHours` and a refresh-all result summary. Document them in ui-contract.md in the same diff.
3. Implement `DemoClock`, `DemoScenarioCatalog` (C# seeds matching fixtures.json IDs F01–F15, with a test asserting parity against the docs catalog) and `DemoState` (revisioned, resettable).
4. Implement every mock service with deterministic stages, success/failure/cancel/empty/unknown/stale/partial outcomes selected by scenario or forced outcome, and delays driven by the demo clock or short fixed `Task.Delay` injected through `IClock` so tests run instantly.
5. Replace the Windows composition with presentation, platform and demo services only. Keep the csproj references unchanged. Delete the obsolete `Features/Dashboard/*` presentation code and its obsolete view-model tests (`DashboardViewModelTests`, `ProviderDashboardTests`), replacing their behavioural intent with new tests. Retain `DashboardWorkflowTests` (Core).
6. Tests: contract semantics (null ≠ 0, unlimited requires explicit, stale keeps timestamp), mock determinism, reset, cancellation is not failure, `Features` non-XAML sources free of WinUI, and Windows sources free of Core/Infrastructure usage.

### Phase 2 - Theme resources, reusable controls, motion and navigation shell (T-05)

Paths (under `src/windows/AiUsage.Windows/`): `Themes/**`, `Controls/**`, `Motion/**`, `Platform/**`, `MainWindow.xaml(.cs)`, `Features/Shell/**`, `Features/Demo/**` (panel), `Strings/en-US/Resources.resw`.

1. Token dictionaries: `ThemeDictionaries` Light/Dark/HighContrast with semantic brushes (surface, card, border, text tiers, accent, severity ok/warning/critical/unknown/stale), type ramp, spacing and radii, all taken from the imported design. Dark is designed values, not inversion.
2. `ThemeService`: applies `RequestedTheme` on the root, follows `UISettings.ColorValuesChanged` for System, reports high contrast, and persists to a demo-only preference file under the app's local folder, `demo-preferences.json`, with a reset. The file is separate from `providers/` storage.
3. Reusable controls: `QuotaMeter` (Known/Unknown/Unlimited/Exhausted/Unavailable, stale overlay, noncolor glyph), `StatusBadge`, `FreshnessLabel` (relative and exact local timestamp, "Awaiting update"), `SkeletonBlock` with shimmer, `RefreshButton` (rotating glyph while pending, stable size), `OperationStatus` ("Updating…", stages, announcements), `ConfirmationDialog`, `ProviderMark` (neutral), `EmptyState`/`ErrorState`.
4. Motion: durations/easing from the design. The default when unspecified is design.md's ranges. Reduced-motion gating comes from `UISettings.AnimationsEnabled`, and decorative animation is suspended when the window is hidden.
5. Shell: top `NavigationView` Overview · Accounts · History · Settings (four tabs) · System Status, back stack, title bar, "Demo · sample data" marker, demo control panel (scenario selector, clock advance, forced outcome, reset), window sizing, close-to-tray and Exit.
6. Tests: shell navigation/back, demo control reset/scenario switching, theme preference view-model behaviour.

### Phase 3 - Overview, accounts, quotas, connection and tray (T-06; S01, S02, S03, S07)

1. `OverviewViewModel`: provider sections with per-window lines of the primary group, hover/focus-revealed refresh and pill, first-run empty and all-hidden states, refresh-all with per-account outcomes and result bar, compact layout below 720 px, manual order (Alt+↑/↓ plus drag), no global percentage. Summary computation (lowest known remaining with source, nearest reset, attention counts, shared pools once) is kept for the tray and tests even though D3 removes its visual strip.
2. `AccountCardViewModel` → `ContextViewModel` → `QuotaGroupViewModel` → `QuotaWindowViewModel`: expansion (Auto/Expanded/Collapsed respecting explicit user choice), context switch, rename with validation (`ObservableValidator`), hide only or hide+mute, show hidden/disconnected, refresh/reconnect/disconnect with confirmation, native amounts and money metadata without inference, reset countdown driven by `IClock`.
3. `AddAccountViewModel`: four-provider picker, methods, browser handoff simulation, waiting stage, manual code (transient, never stored), cancel, denied/expired/failed, connected-without-quota, duplicate identity focus.
4. Tray: `TrayViewModel` shared with the dashboard state, popup mini-dashboard without charts, per-account/all refresh, single click popup, double click main window, right-click Open/Refresh all/Settings/Exit, severity priority per D-126.
5. Tests: F01–F08 and F15 transitions at view-model level.

### Phase 4 - History, appearance, monitoring and notification settings (T-07; S04, S05, S06)

1. `HistoryViewModel`: account/context/group/window filters, presets 24h/7d/30d/90d/1y/Custom with range validation, loading/empty/disabled/partial, segments broken at gaps and resets, tooltip data with timestamp and unit, sparkline plus detailed chart control.
2. `AppearanceSettingsViewModel`: theme System/Light/Dark with immediate application, density if designed, always-on-top, order/visibility management with keyboard equivalents.
3. `MonitoringSettingsViewModel`/`ThresholdRuleViewModel` per resolved D1/D2: Used/Remaining display mode (default Remaining), global `[25,10,0]` remaining-percent default, Provider, WindowType, Account and Window overrides, validation of unique whole numbers 0–100 entered in the display unit, override and reset-to-inherited, quiet/OS-blocked explanation, power/metered reasons, manual refresh retained, preview notification in-app only.
4. Tests: F09, F10.

### Phase 5 - CLI import, diagnostics, data management, recovery and updates (T-08; S08–S12)

1. `CliImportViewModel`: explicit start, progressive discovery, candidate selection, per-item outcomes (new/duplicate/unsupported/failure), bulk and re-import, cancellation preserving completed items.
2. `SystemStatusViewModel`: build/schema/provider/refresh/update status, health check with progressive steps and warning without repair, diagnostics and log previews.
3. `DataPrivacyViewModel`: history collection/retention, export preview, Replace import with separate checking and processing stages and confirmation, settings reset vs factory reset, delete account data separate from disconnect.
4. `RecoveryViewModel`: blocking surface for Interrupted/NewerSchema/RestoreFailed, retry, checkpoint restore, diagnostics and data-folder previews.
5. `UpdatesViewModel`: checking/unsupported/current/available/ready/failed/waiting-for-stable, channel switch, Restart & update simulated, compatibility/security blocks keep cache and history readable.
6. Tests: F11–F14.

### Phase 6 - Scenario coverage, Windows verification and backend handoff (T-09)

1. Complete the coverage matrix evidence columns from actual results only.
2. Update `AiUsage.Windows.Tests` smoke scenarios to the new automation IDs (launch, demo marker, navigation to every page, theme switch, close-to-tray, tray restore, tray Exit, repeated Exit).
3. Build the package, launch the unpackaged Debug build on the host (no installation), capture screenshots of every page in Light and Dark, and check keyboard traversal, 100/150/200% scale and text scaling, high contrast and reduced motion.
4. Write the backend handoff section in tasks.md: adapter interfaces, composition point, capability rules, deviations from design.
5. Run primary diff review against brief acceptance, record results in verification.md, commit and push the task branch per CONTRIBUTING. Do not merge.

## Verification commands

Run from the repository root in PowerShell.

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet restore src/windows/AiUsage.slnx
dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
git diff --check
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 --no-restore
./tools/windows/Build-Package.ps1 -MsixVersion <fresh YYYY.M.DDNN.0>
```

Mock app run without host installation (verify in Phase 2; if unpackaged activation fails, record BLOCKED and ask before registering a package):

```powershell
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore
& src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe
```

Host facts observed 2026-09-15: SDK 10.0.401, Windows App Runtime 2.4.0.0 x64 installed, and an existing `AiUsage.Dev 2026.9.1416.0` package registration. Re-registering or reinstalling that package is a host install and requires owner authorization.

## Coverage matrix

Evidence values are `not-run` until a check actually passes. "Design" records whether the imported artifact covers the surface: `Designed` means the prototype renders it; `Partial` means the design covers it with the gaps or decisions noted above.

Evidence naming, filled on 2026-09-16 from the run recorded in [verification.md](verification.md): a bare class name is an xUnit class in `tests/windows/AiUsage.Presentation.Tests` (100 tests, all passing); "smoke &lt;scenario&gt;" is a scenario of `ShellSmoke` in `tests/windows/AiUsage.Windows.Tests` driving the built unpackaged app through UI Automation (6 scenarios, all passing); `evidence/…` paths are screenshots under the host-local, git-ignored `.ai-usage-local/AIU-010/evidence/` (`manual/` holds captures driven by hand through UI Automation, the rest are written by the smoke run).

### Imported design surfaces

| Design surface | S ID | View | View model | Evidence |
|---|---|---|---|---|
| Shell: top nav, header actions, compatibility banner, result bar | shared | `MainWindow`, `Features/Shell/*` | `ShellViewModel` | PASS ShellAndTrayTests; smoke launch + navigation; evidence/launch.png |
| Demo shell and in-page simulators | shared | `Features/Demo/DemoPanel` | `DemoControlViewModel` | PASS ScenarioCatalogTests; smoke asserts DemoMarker; evidence/manual/f15b.png |
| Overview route | S01 | `Features/Overview/OverviewPage` | `OverviewViewModel` | PASS OverviewTests; smoke navigation; evidence/page-NavOverview.png |
| Accounts route | S02 | `Features/Accounts/AccountsPage` | `AccountsViewModel`, `AccountDetailViewModel` | PASS AccountTests; smoke navigation; evidence/page-NavAccounts.png |
| Add account · Sign in | S03 | `Features/Connection/AddAccountDialog` (Sign in tab) | `AddAccountViewModel` | PASS ConnectionTests; evidence/manual/sheet-method.png, sheet-result.png |
| Add account · Import from CLI | S08 | `Features/CliImport/CliImportView` (CLI tab) | `CliImportViewModel` | PASS ConnectionTests; evidence/manual/cli-found.png, cli-done.png |
| History route | S04 | `Features/History/HistoryPage` | `HistoryViewModel` | PASS HistoryTests; smoke navigation; evidence/page-NavHistory.png |
| Settings › Appearance | S05 | `Features/Settings/Appearance/AppearanceSettingsView` | `AppearanceSettingsViewModel` | PASS SettingsTests; smoke theme; evidence/theme-ThemeLight.png, theme-ThemeDark.png |
| Settings › Monitoring & notifications | S06 | `Features/Settings/Monitoring/MonitoringSettingsView` | `MonitoringSettingsViewModel` | PASS SettingsTests; evidence/manual/s-monitoring.-1.png |
| Settings › Data & privacy | S10 | `Features/Settings/DataPrivacy/DataPrivacyView` | `DataPrivacyViewModel` | PASS OperationsTests; evidence/manual/export.png, factory.png, factory-done.png |
| Settings › Updates | S12 | `Features/Settings/Updates/UpdatesView` | `UpdatesViewModel` | PASS OperationsTests; evidence/manual/s-updates.-1.png, sc-f14.png |
| System Status route | S09 | `Features/SystemStatus/SystemStatusPage` | `SystemStatusViewModel` | PASS OperationsTests; smoke navigation; evidence/page-NavSystemStatus.png |
| Tray popup, tray menu, hide to tray | S07 | `Features/Tray/TrayPopupWindow`, tray `MenuFlyout` | `TrayViewModel` | PASS ShellAndTrayTests; smoke close-to-tray + tray-exit; evidence/manual/tray-menu.png, tray-f15.png |
| Recovery surface | S11 | `Features/Recovery/RecoveryView` | `RecoveryViewModel` | PASS OperationsTests; evidence/manual/sc-f13.png |
| Confirm dialog | shared | `Features/Shell/ConfirmDialog` | `ConfirmDialogViewModel` | PASS ConfirmDialogTests; every smoke scenario exits through it; evidence/launch-confirm.png |
| Notification toast preview | S06 | `Features/Shell/ToastPreview` | `ToastPreviewViewModel` | PASS SettingsTests; evidence/manual/toast.-1.png |
| Skeletons | shared | `Controls/SkeletonBlock`, `Features/Overview/SkeletonRows` | `IsLoading` on page view models | PASS OverviewTests/HistoryTests load states; evidence/manual/sheet-waiting.png |

### Screen inventory

| S | Design | View (planned) | View model | Key properties | Commands | Scenarios | Evidence |
|---|---|---|---|---|---|---|---|
| S01 Overview | Partial (D3) | `Features/Overview/OverviewPage` | `OverviewViewModel`, `ProviderSectionViewModel`, `AccountRowViewModel` | `ProviderSections`, `VisibleAccounts`, `Summary` (lowest/nearest/attention counts, not rendered per D3), `IsCompact`, `LoadState`, `IsRefreshingAll`, `RefreshAllResult`, `AllHiddenNote`, `IsFirstRun`, `CompatibilityBanner` | `RefreshAllCommand`, `DismissResultCommand`, `AddAccountCommand`, `ImportCliCommand`, `MoveUpCommand`, `MoveDownCommand`, `DropCommand`, `ShowAllFiltersCommand`, `OpenAccountCommand`, `ToggleExpandCommand`, row `RefreshCommand`/`CancelRefreshCommand` | F01, F02, F03, F04, F06, F15 | PASS OverviewTests (11); smoke navigation; evidence/page-NavOverview.png, manual/light-overview.-1.png, manual/ov-expand.-1.png |
| S02 Account detail | Designed | `Features/Accounts/AccountsPage` (list + detail), `Controls/QuotaMeter` | `AccountsViewModel`, `AccountDetailViewModel`, `ContextViewModel`, `QuotaGroupViewModel`, `QuotaWindowViewModel`, `RenameAccountViewModel` | `Label`, `Plan`, `Connection`, `Freshness`, `FetchedAt`, `SelectedContext`, `Groups`, `Expansion`, `ValueState`, `RemainingPercent`, `Absolute`, `ResetsAt`, `ResetText`, `Extensions`, `Failure`, `IsHidden`, `AlertsMuted` | `RefreshCommand`, `ReconnectCommand`, `DisconnectCommand`, `RenameCommand`, `ToggleExpansionCommand`, `HideCommand`, `HideAndMuteCommand`, `ShowHiddenCommand`, `MoveUp/DownCommand` | F02, F03, F04, F06, F07, F08, F15 | PASS AccountTests (10), QuotaSemanticsTests (10); evidence/page-NavAccounts.png, manual/accounts3.-1.png |
| S03 Add account | Designed | `Features/Connection/AddAccountDialog` | `AddAccountViewModel` | `Providers`, `SelectedProvider`, `Methods`, `SelectedMethod`, `Stage`, `ManualCode` (transient), `Failure`, `DuplicateAccountId` | `StartConnectCommand`, `SubmitCodeCommand`, `CancelCommand`, `RetryCommand`, `OpenImportCommand` | F01, F05, F06 | PASS ConnectionTests (8); evidence/manual/sheet-method.png, sheet-waiting.png, sheet-result.png |
| S04 History | Partial (context/group folded into window select) | `Features/History/HistoryPage`, `Controls/HistoryChart`, `Controls/Sparkline` | `HistoryViewModel` | `AccountFilter`, `ContextFilter`, `GroupFilter`, `WindowFilter`, `Preset`, `CustomFrom`, `CustomTo`, `RangeError`, `Segments`, `LoadState`, `CollectionEnabled`, `HoverPoint` | `ApplyRangeCommand`, `LoadCommand`, `CancelLoadCommand` | F09 | PASS HistoryTests (7); evidence/page-NavHistory.png, manual/history2.-1.png |
| S05 Appearance | Designed | `Features/Settings/Appearance/AppearanceSettingsPage` | `AppearanceSettingsViewModel` | `Theme`, `EffectiveTheme`, `Density`, `AlwaysOnTop`, `OrderItems`, `HighContrastActive`, `ReducedMotion` | `SetThemeCommand`, `MoveUp/DownCommand`, `ToggleVisibilityCommand`, `ResetDemoPreferencesCommand` | F10, F15 | PASS SettingsTests; smoke theme scenario; evidence/theme-ThemeLight.png, theme-ThemeDark.png, theme-ThemeSystem.png |
| S06 Monitoring/notifications | Partial (D1, D2) | `Features/Settings/Monitoring/MonitoringSettingsPage`, `Features/Shell/ToastPreview` | `MonitoringSettingsViewModel`, `ThresholdRuleViewModel`, `ToastPreviewViewModel` | `UsageDisplay`, `Rules` (global plus provider × window type), `IsInherited`, `EditText`, `ThresholdError`, `ResetNotice`, `OsNotificationsAllowed`, `QuietHours`, `ReducedRefreshPolicy`, `Toast` | `OverrideCommand`, `ResetToInheritedCommand`, `AddThresholdCommand`, `RemoveThresholdCommand`, `PreviewNotificationCommand`, `RefreshNowCommand` | F07, F10 | PASS SettingsTests (9); evidence/manual/s-monitoring.-1.png, toast.-1.png |
| S07 Tray | Designed | `Features/Tray/TrayPopup`, tray menu | `TrayViewModel` | `Accounts`, `Severity`, `ToolTip`, `IsPopupOpen` | `OpenCommand`, `RefreshAllCommand`, `RefreshAccountCommand`, `OpenSettingsCommand`, `ExitCommand` | F02, F04, F06, F15 | PASS ShellAndTrayTests (9); smoke close-to-tray, tray-exit; evidence/manual/tray-f15.png, tray-menu.png, close-to-tray-restored.png |
| S08 CLI import | Designed | `Features/Connection/AddAccountDialog` (Import from CLI tab) | `CliImportViewModel`, `CliCandidateViewModel` | `Stage`, `Candidates`, `SelectedCount`, `Outcomes` | `DiscoverCommand`, `ImportSelectedCommand`, `ReimportCommand`, `CancelCommand` | F11 | PASS ConnectionTests; evidence/manual/cli-found.png, cli-done.png |
| S09 System Status | Designed | `Features/SystemStatus/SystemStatusPage` | `SystemStatusViewModel` | `BuildLabel`, `SchemaLabel`, `ProviderStatuses`, `Health`, `HealthSteps`, `UpdateSummary`, `DiagnosticsPreview`, `LogPreview` | `RunHealthCheckCommand`, `CancelHealthCheckCommand`, `PreviewDiagnosticsCommand`, `PreviewLogsCommand` | F12, F14 | PASS OperationsTests (15); smoke navigation; evidence/page-NavSystemStatus.png, manual/health.-1.png |
| S10 Data and privacy | Designed | `Features/Settings/DataPrivacy/DataPrivacyPage` | `DataPrivacyViewModel`, `ReplaceImportViewModel` | `HistoryEnabled`, `Retention`, `ExportPreview`, `ImportStage`, `ImportError`, `ConfirmationPending` | `PreviewExportCommand`, `ValidateImportCommand`, `ConfirmReplaceCommand`, `ResetSettingsCommand`, `FactoryResetCommand`, `DeleteAccountDataCommand`, `PreviewDataFolderCommand` | F12 | PASS OperationsTests; evidence/manual/export.png, factory.png, factory-done.png, s-data.-1.png |
| S11 Recovery | Designed | `Features/Recovery/RecoveryPage` (blocking) | `RecoveryViewModel` | `State`, `Checkpoints`, `SelectedCheckpoint`, `Stage`, `Outcome` | `RetryCommand`, `RestoreCheckpointCommand`, `PreviewDiagnosticsCommand`, `PreviewDataFolderCommand` | F13 | PASS OperationsTests; evidence/manual/sc-f13.png |
| S12 Updates | Designed | `Features/Settings/Updates/UpdatesPage` | `UpdatesViewModel` | `State`, `Channel`, `AvailableVersion`, `Compatibility`, `Failure` | `CheckCommand`, `SetChannelCommand`, `RestartAndUpdateCommand` | F14 | PASS OperationsTests; evidence/manual/s-updates.-1.png, sc-f14.png |
| Shell + demo | Designed | `MainWindow`, `Features/Demo/DemoPanel` | `ShellViewModel`, `DemoControlViewModel` | `CurrentPage`, `CanGoBack`, `NavigationItems`, `DemoMarker`, `Scenarios`, `SelectedScenario`, `ClockNow`, `ForcedOutcome` | `NavigateCommand`, `GoBackCommand`, `SelectScenarioCommand`, `AdvanceClockCommand`, `ResetDemoCommand` | all | PASS ShellAndTrayTests, ScenarioCatalogTests (6); smoke launch/navigation; evidence/launch.png, manual/f15b.png |

### Scenarios

| F | Name | Surfaces | Required outcomes to demonstrate | Test (planned) | Evidence |
|---|---|---|---|---|---|
| F01 | First run | S01, S03 | Empty dashboard, Add account, cancel leaves empty | `OverviewTests.FirstRun*` | PASS OverviewTests.FirstRun*; evidence/manual/sc-f01.png, factory-done.png |
| F02 | Four providers, multiple accounts | S01, S02, S07 | Reorder persists across navigation; one refresh failure leaves other cards readable | `OverviewTests.Reorder*`, `RefreshAll*` | PASS OverviewTests reorder/refresh-all; evidence/manual/reduced-refresh.png |
| F03 | Measurement semantics | S01, S02 | Known/Unknown/Unlimited/Exhausted/Unavailable/Stale distinct; no zero meter for unknown | `QuotaWindowTests.*` | PASS QuotaSemanticsTests; evidence/manual/accounts3.-1.png |
| F04 | Offline cache | S01, S02, S07 | Refresh failure keeps stale 42 and timestamp; retry gives fresh 39 | `AccountCardTests.StaleRetry*` | PASS AccountTests stale/retry; evidence/manual/reduced-refresh.png |
| F05 | Connection | S03 | Waiting, manual code, cancel, denied, success without quota, duplicate focus | `AddAccountTests.*` | PASS ConnectionTests; evidence/manual/sheet-method.png, sheet-waiting.png, sheet-result.png |
| F06 | Reauth and rate limit | S01, S02, S03, S07 | ReauthRequired retains cache; RateLimited until retryAt; reconnect keeps identity; cancel restores | `AccountCardTests.Reauth*` | PASS AccountTests reauth/rate-limit |
| F07 | Contexts and shared pools | S02, S06 | Shared pool counted once; hide vs hide+mute; context absence retains preference | `OverviewTests.SharedPool*`, `ContextTests.*` | PASS OverviewTests shared pool, AccountTests contexts |
| F08 | Reset and native amounts | S02 | Passed reset shows Awaiting update and stays exhausted; new observation 100; no invented money | `QuotaWindowTests.Reset*`, `ExtensionTests.*` | PASS QuotaSemanticsTests reset/native amounts |
| F09 | History gaps and reset | S04 | Invalid range inline; disabled collection keeps samples; empty; segments not joined | `HistoryTests.*` | PASS HistoryTests; evidence/manual/history2.-1.png |
| F10 | Preferences | S05, S06 | Dark immediate; account and window-type overrides leave global unchanged; reset to inherited global `[25,10,0]` (D1); Used/Remaining flip; OS disabled; quiet hours; battery saver | `AppearanceTests.*`, `NotificationRuleTests.*` | PASS SettingsTests; smoke theme scenario; evidence/theme-ThemeDark.png |
| F11 | CLI discovery | S08 | Progressive candidates; per-item outcomes; cancel keeps completed; reimport same identity | `CliImportTests.*` | PASS ConnectionTests CLI; evidence/manual/cli-found.png, cli-done.png |
| F12 | Diagnostics and data | S09, S10 | Health warning without repair; export preview; invalid/valid replace; factory/settings reset | `SystemStatusTests.*`, `DataPrivacyTests.*` | PASS OperationsTests; evidence/manual/health.-1.png, export.png, factory.png |
| F13 | Recovery | S11 | Retry failure stays; restore valid checkpoint; newer schema disables operations | `RecoveryTests.*` | PASS OperationsTests recovery; evidence/manual/sc-f13.png |
| F14 | Updates and compatibility | S12, S09 | Available→Ready; simulated restart; WaitingForStable; blocks keep cache; check failure keeps dashboard | `UpdatesTests.*` | PASS OperationsTests updates; evidence/manual/sc-f14.png |
| F15 | Layout/accessibility stress | S01, S02, S05, S07 | 20 accounts × 12 groups; narrow width; keyboard reorder; high contrast; reduced motion; tray hides animation | `OverviewTests.Stress*` plus Windows manual/FlaUI | PASS OverviewTests stress; evidence/manual/f15b.png, compact560.-1.png, scale150b.png, scale200-wide2.-1.png, hc14.-1.png, keyboard-nav2.-1.png |

### Cross-cutting requirements

| Requirement | Planned implementation | Evidence |
|---|---|---|
| Default startup mock-only | Composition plus boundary test (no Core/Infrastructure reference) | PASS DependencyBoundaryTests (7); smoke asserts the demo marker at launch |
| System/Light/Dark, follows Windows | `ThemeService`, `ThemeDictionaries` | PASS smoke theme scenario (Dark/Light/System); evidence/theme-*.png |
| High contrast, keyboard, labels, scaling | HighContrast dictionary, `AutomationProperties`, access keys, tab order checks at 100/150/200% | PASS contrast evidence/manual/hc14.-1.png, hc-accounts.-1.png, hc-dialog2.png; keyboard evidence/manual/keyboard-nav2.-1.png; scaling evidence/manual/scale150b.png, scale200-wide2.-1.png, compact560.-1.png |
| Skeleton, Updating…, per-action motion, reduced motion | `SkeletonBlock`, `OperationStatus`, `RefreshButton`, `Motion/` gating | PASS evidence/manual/sheet-waiting.png (skeleton), reduced-refresh.png (Updating + reduced motion) |
| Tray/window lifetime and clean Exit | `IAppLifetime`, H.NotifyIcon, FlaUI smoke | PASS shell-smoke close-to-tray, tray-exit, repeated-exit (exit code 0) |
| Resources and culture formatting | `Resources.resw`, `ITextResources`, `CultureInfo.CurrentCulture` | PASS 723 resw keys resolved at runtime; PRI built into the package; dates/numbers via CurrentCulture |
| Core/Infrastructure unchanged | `git diff --stat 744e4e0 -- src/windows/AiUsage.Core src/windows/AiUsage.Infrastructure tests/windows/AiUsage.Infrastructure.Tests` empty | PASS git diff --stat 744e4e0 -- src/windows/AiUsage.Core src/windows/AiUsage.Infrastructure tests/windows/AiUsage.Infrastructure.Tests is empty |
