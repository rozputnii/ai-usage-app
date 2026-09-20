---
id: AIU-010
schema_version: 1
---
# AIU-010 staged execution

The primary owns shared contracts, canonical documents and integration. This file is the single progress and resume record for the mock-first frontend delivery defined by [frontend-brief.md](frontend-brief.md) and [frontend-plan.md](frontend-plan.md). After interruption or context compaction, read the brief, the plan and this file before editing. No write worker was launched; T-10 used one read-only independent reviewer.

### T-01 - Prepare current and planned UI contract
- status: done
- depends_on: []
- acceptance: AC-01, AC-02, AC-08, AC-09
- evidence: docs/specs/AIU-010-ui-ux/verification.md

Prepare the inventory, semantic contract, deterministic scenario catalog, agent prompts and acceptance gates. Validate documents and fixtures and review the scoped diff.

### T-02 - Claude Design visual system and complete mockup
- status: done
- depends_on: [T-01]
- acceptance: AC-03, AC-05, AC-06
- evidence: docs/specs/AIU-010-ui-ux/frontend-brief.md

The owner identified the selected Claude Design project and file in the brief (owner amendment 2026-09-15). The owner's selection counts as design approval. The artifact's content and revision are verified in T-03, not here.

### T-03 - Import selected design and record revision
- status: done
- depends_on: [T-02]
- acceptance: AC-03, AC-05, AC-06
- evidence: docs/specs/AIU-010-ui-ux/design-reference/README.md

Imported `AI Usage App.dc.html`, `support.js` and the rest of the project through the claude_design MCP. The local snapshot with revision and hashes is in `design-reference/`. All designed surfaces are enumerated in the plan's coverage matrix, and the design decisions that differ from earlier documents (D1–D7) and the design gaps are recorded there.

### T-04 - Presentation contracts, mock services and application composition
- status: done
- depends_on: [T-02]
- acceptance: AC-02, AC-06, AC-10, AC-11
- evidence: docs/specs/AIU-010-ui-ux/verification.md (2026-09-16)

Plan phase 1, including the design-required contract extensions. All code stays inside `AiUsage.Windows` under `Features/<Feature>/`, within the existing three-project architecture. Adding a project requires a demonstrated need and owner approval first.

### T-05 - Theme resources, reusable controls, motion and navigation shell
- status: done
- depends_on: [T-03, T-04]
- acceptance: AC-03, AC-05, AC-10, AC-11
- evidence: docs/specs/AIU-010-ui-ux/verification.md (2026-09-16)

Plan phase 2.

### T-06 - Overview, accounts, quotas, connection and tray
- status: done
- depends_on: [T-05]
- acceptance: AC-04, AC-05, AC-06, AC-10
- evidence: docs/specs/AIU-010-ui-ux/verification.md (2026-09-16)

Plan phase 3: S01, S02, S03, S07 and F01–F08, F15.

### T-07 - History, appearance, monitoring and notification settings
- status: done
- depends_on: [T-05]
- acceptance: AC-04, AC-05, AC-06, AC-10
- evidence: docs/specs/AIU-010-ui-ux/verification.md (2026-09-16)

Plan phase 4: S04, S05, S06 and F09, F10.

### T-08 - CLI import, diagnostics, data management, recovery and updates
- status: done
- depends_on: [T-05]
- acceptance: AC-04, AC-08, AC-10
- evidence: docs/specs/AIU-010-ui-ux/verification.md (2026-09-16)

Plan phase 5: S08–S12 and F11–F14.

### T-09 - Scenario coverage, Windows verification and backend handoff
- status: done
- depends_on: [T-06, T-07, T-08]
- acceptance: AC-03, AC-04, AC-05, AC-06, AC-09, AC-10, AC-11
- evidence: docs/specs/AIU-010-ui-ux/verification.md (2026-09-16)

Plan phase 6. Actual Windows screenshots in Light and Dark, keyboard, scaling, tray and Exit checks, coverage matrix evidence, verification.md record and adapter handoff. Commit and push the task branch after verification; do not merge.

### T-10 - Codex available-service integration
- status: done
- depends_on: [T-09]
- acceptance: AC-06, AC-07, AC-08
- evidence: docs/specs/AIU-010-ui-ux/verification.md

Codex maps existing workflows onto the presentation adapter interfaces, keeps future services unavailable in product mode and adds mapping/capability regressions. If durable-state or credential boundaries change, use security-lifecycle and focused review.

### T-11 - Integrated Windows and visual acceptance
- status: in-progress
- depends_on: [T-10]
- acceptance: AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09
- evidence: docs/specs/AIU-010-ui-ux/verification.md

Run required regressions/build and actual Windows UI scenarios on the integrated candidate. Record owner visual acceptance separately from deterministic checks. Do not mark future backend AIUs done based on demo success.

## Handoff

Base: `744e4e0` (identical to `codex/aiu-010-design-handoff`). Branch: `codex/aiu-010-mock-frontend`; checkout was clean at start. This section records the preparation phase, when no application source had changed yet; the mock frontend delivery below supersedes it.

Completed 2026-09-15 (preparation phase):
- Saved the owner brief verbatim as frontend-brief.md.
- Recorded the owner amendment in spec.md (scope_version 2, AC-10, AC-11), design.md and handoffs.md. The Claude Code prompt in handoffs.md is marked superseded.
- Wrote frontend-plan.md with architecture, adapter interfaces, dependencies, phases, commands and the coverage matrix. Every evidence cell is `not-run`.
- Observed host facts: SDK 10.0.401, Windows App Runtime 2.4.0.0 installed, existing `AiUsage.Dev 2026.9.1416.0` registration. Nothing was installed.
- First import attempt failed because design authorization was missing. After the owner ran `/design-login`, the import succeeded. The project "# AI Usage dashboard directions" has 11 paths. The prototype, `support.js`, the Design Specification, the Reference Views and the Directions study were read (none truncated) and saved with SHA-256 hashes in `design-reference/`. Design revision: "Quiet Editorial (1b) · revision 1 · 2026-09-15". `.thumbnail` and `uploads/` (copies of the package documents) were not saved.
- Surface inventory, the S mapping, design decisions D1–D7 and gaps are in frontend-plan.md. spec.md records the design precedence.
- Owner correction 2026-09-15: the proposed separate presentation project is withdrawn.
  - Contracts, view models, navigation and mock services live in `AiUsage.Windows/Features/<Feature>/`.
  - WinUI implementations go in `Platform/`, `Controls/`, `Themes/` and `Motion/`.
  - `AiUsage.Presentation.Tests` keeps compiling linked `Features/**/*.cs` sources, excluding `*.xaml.cs`.
  - The Windows csproj keeps its existing Core/Infrastructure references, and a source boundary test forbids their use in this delivery.
- Implementation has not started; it waits for the owner's `/goal`.

Open owner choices (not blockers; the implementation follows the design unless the owner overrides):
- D1: default thresholds `[50,80]` vs `[20,50]` used, and the dropped per-account overrides.
- D3: the Overview summary strip removed against S01's written counts.
- D4: glyphs in High Contrast.
- D6: provider glyph colours pending rights review.
- The remaining items in specification §13.

Risks:
- Unpackaged launch (`-p:WindowsPackageType=None`) for host screenshots is untested. If it fails, registering the dev package on the host needs owner authorization. Resolved 2026-09-16: unpackaged launch works, so nothing was installed or registered.
- No new package is currently required. Any later addition needs network restore.

Check results (preparation):
- PASS: `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` returned `{"valid":true,"diagnostics":[]}` (exit 0).
- PASS: `git diff --check` exit 0 with new files intent-added, after the design import (`design-reference/.gitattributes` keeps imported bytes verbatim). Validator re-run after the import also returned valid.
- An untracked root `install.cmd` appeared during the session. It was not created by this work and was left untouched.
- NOT_RUN: product regressions, builds and Windows UI checks, because no code has changed.

Owner `/goal` 2026-09-15 (implementation phase, T-04–T-09 only; T-10/T-11 stay with Codex):
- Baseline before code changes: Presentation.Tests 16/16 PASS, Infrastructure.Tests 130/130 PASS (Release, SDK 10.0.401).
- D1–D7 reconciled against written requirements and extended with D8–D12 in frontend-plan.md. Written quota semantics, `[25,10,0]` remaining defaults and Global/Provider/Account/Window scopes are kept; the design is adapted.

- T-04 progress: contracts (`Features/Presentation`), adapter interfaces in their feature folders, deterministic demo world and all mock services (`Features/Demo`), and every feature view model are implemented. `Features/Dashboard` and its two obsolete view-model test files were removed; their behaviour is covered by the new tests. The presentation test project compiles `Features/**/*.cs` without `*.xaml.cs`. Presentation tests: 94/97 pass; the 3 failures are the boundary tests that require the WinUI replacement (old App/MainWindow still reference Core/Infrastructure).
- Next action: T-05, which adds `Themes/`, `Controls/`, `Platform/` and `Composition/`, then replaces `App.xaml(.cs)`/`MainWindow` with the mock shell.

Previous next action (superseded by the progress entries above once work starts): T-04 step 1. Change `tests/windows/AiUsage.Presentation.Tests/AiUsage.Presentation.Tests.csproj` to compile `src/windows/AiUsage.Windows/Features/**/*.cs` excluding `**/*.xaml.cs`, and extend `DependencyBoundaryTests` with the Features-without-WinUI and no-Core/Infrastructure-usage rules. Then create the `Features/Presentation/` contracts.

## Mock frontend delivery (T-04 - T-09), 2026-09-16

Branch `codex/aiu-010-mock-frontend`, base `375c409`. Presentation, WinUI shell and demo services live in `src/windows/AiUsage.Windows`; Core, Infrastructure and their tests are untouched (`git diff --stat 744e4e0 -- src/windows/AiUsage.Core src/windows/AiUsage.Infrastructure tests/windows/AiUsage.Infrastructure.Tests` is empty). Check results are in [verification.md](verification.md); coverage evidence per surface, screen, scenario and cross-cutting requirement is in [frontend-plan.md](frontend-plan.md).

### Where the code is

- `Features/<Feature>/` - adapter interfaces, view models (CommunityToolkit.Mvvm) and the XAML views of that feature. `Features/Presentation/` holds the host abstractions and shared records; `Features/Demo/` holds the deterministic mock adapters, the scenario catalog and the demo panel.
- `Platform/` - WinUI implementations of the host abstractions (`UiDispatcher`, `NavigationService`, `DialogService`, `ThemeService`, `MotionSettings`, `Announcer`, `ResourceText`, `AppLifetime`, `DisplaySimulation`).
- `Controls/`, `Themes/` - reusable controls (`QuotaMeter`, `SkeletonBlock`, `ProviderTile`, `StatusPill`, `WrapPanel`, `ArrowNavigation`, token binding helpers) and the generated `Tokens.xaml` / `SimulatedHighContrast.xaml` plus `Styles.xaml`. Regenerate tokens with `tools/windows/New-ThemeTokens.ps1`.
- `Composition/ServiceRegistration.cs` - `AddPresentationFeatures()`, `AddPlatformServices()`, `AddDemoServices()`.
- `Strings/en-US/Resources.resw` - 723 keys; views bind through `x:Uid`, view models through `ITextResources`.

### Codex integration point

`App.OnLaunched` builds the host with `builder.Services.AddPresentationFeatures().AddPlatformServices().AddDemoServices()`. Backend integration adds `AddLiveServices()` in `Composition/` that registers live implementations of the same interfaces, and selects it instead of `AddDemoServices()` for the product build; the demo registration stays for the mock build. The interfaces and their members are listed in the plan's "Adapter interfaces (Codex boundary)" table and implemented by `Demo*` services:

| Interface | File | Demo implementation |
|---|---|---|
| `IUsageSource` | `Features/Accounts/IUsageSource.cs` | `DemoUsageSource` |
| `IConnectionFlow` | `Features/Connection/IConnectionFlow.cs` | `DemoConnectionFlow` |
| `ICliImportService` | `Features/CliImport/ICliImportService.cs` | `DemoCliImportService` |
| `IHistorySource` | `Features/History/IHistorySource.cs` | `DemoHistorySource` |
| `IPreferenceStore`, `INotificationPreview`, `IDataManagementService`, `IUpdateService` | `Features/Settings/SettingsServices.cs` | `DemoPreferenceStore`, `DemoNotificationPreview`, `DemoDataManagementService`, `DemoUpdateService` |
| `IDiagnosticsService` | `Features/SystemStatus/IDiagnosticsService.cs` | `DemoDiagnosticsService` |
| `IRecoveryService` | `Features/Recovery/IRecoveryService.cs` | `DemoRecoveryService` |
| `IClock` | `Features/Presentation/HostAbstractions.cs` | `DemoClock` (frozen 2026-09-15T12:00:00Z) |

Rules for a live adapter: publish snapshots from any thread (view models marshal through `IUiDispatcher`); return typed records only, never formatted text or raw exceptions; report an unavailable capability as `CommandStatus.Unsupported` before any side effect, which the UI renders as a disabled or planned control rather than an error; keep provider payloads opaque. `DependencyBoundaryTests` currently forbids `AiUsage.Core`/`AiUsage.Infrastructure` usage anywhere in `AiUsage.Windows`; relax it for the live adapter folder only, and keep the demo path free of provider services so the mock build stays credential-free.

### Deviations from the imported design, and why

- Implicit `ScalarTransition`/`BrushTransition` inside control templates crash WinUI layout in WindowsAppSDK 2.4.0 (access violation in `coreclr` a few seconds after launch). All implicit transitions were removed; the designed motion is played explicitly through storyboards in `Controls/Motion` and `SkeletonBlock`, gated by `MotionSettings.Allowed`.
- The shell nav and settings tabs are radio groups with one tab stop; WinUI's XY focus did not move between them, so `Controls/ArrowNavigation` moves focus with the arrow keys (Space or Enter still activates).
- The demo's simulated high contrast swaps the whole token dictionary and passes each root through the opposite theme, because WinUI re-resolves `ThemeResource` references only on a theme change; content created later is refreshed through `ThemeService.RefreshContrast`.
- D1-D7 resolutions from the plan are implemented as written there (thresholds stay remaining-percent, the Overview summary strip is restored, glyphs are kept in every theme, provider hues stay behind the demo switch).
- At 200 % content scale with the demo panel open the effective width drops below the design's compact minimum and header labels clip; the same scale is clean with the panel closed or a wider window.

### Mock delivery next action (superseded by T-10 completion below)

T-10: Codex maps existing Codex workflows onto the adapter interfaces above and adds `AddLiveServices()`, keeping unavailable providers `Unsupported`. Do not mark AIU-010 complete on the mock delivery alone.

## T-10 implementation, 2026-09-16

Base `3ba2c88` on `codex/aiu-010-live-adapters`; clean checkout. Since `ca61583`, only the local-development instruction change intervened. Baseline product tests were not repeated.

Plan: add Windows-owned live adapters over existing `DashboardWorkflow` / `IProviderSession`; map nullable quota and cached/error states; preserve the active Claude authorization for manual-code fallback; select product composition by default and `--demo` explicitly; drain work on Exit; persist presentation-only preferences through Infrastructure; reject and disable future capabilities; run targeted and full relevant regressions, unpackaged/package builds, applicable offline Windows smoke and focused independent review. Existing provider stores and protocols remain authoritative. One existing slot per provider; multi-account remains unavailable. T-11 and owner visual/live acceptance remain separate.

T-10 complete: live product adapters/startup, isolated --demo, active Claude manual-code fallback, authoritative cancellation/reauthentication state, drained startup/shutdown, independent restriction metadata, presentation persistence and unavailable-capability gating. PASS: Presentation 113/113; Infrastructure 131/131; product Windows smoke 7/7; final demo smoke 7/7 with no product-directory creation; unpackaged build; unsigned MSIX 2026.9.1634.0; focused review with its one finding corrected and regression-tested. See verification.md for failures found during development, final evidence paths and limits. Core and existing provider implementations are unchanged.

Exact next action: when T-11 is selected, run the integrated Windows/visual acceptance matrix and owner-authorized live provider scenarios from verification.md. T-11 remains pending; no live or owner visual acceptance is claimed. T-10 is published on its scoped task branch under CONTRIBUTING; no merge is authorized.

## T-11 acceptance pass, 2026-09-16

Started from clean `codex/english-prompts-specs` at `5358d2f`, containing T-10 `90b07c4`; the only intervening change is the English-writing instruction in AGENTS.md. Working branch: `codex/aiu-010-integrated-acceptance`. Reuse the unchanged T-10 regression, build, product/demo smoke and focused-review evidence, with the limits recorded in verification.md.

Plan: verify remaining integrated keyboard/accessibility, native Windows contrast/scaling, tray and applicable packaged behavior; coordinate owner-led Codex/Claude lifecycle and separate visual acceptance; fix observed defects, verify affected changes and perform required integrated review. Do not publish a blocked or incomplete task.

Completed: product keyboard/navigation/dialog subset, actual Windows Night sky contrast on Settings and Add account, native 100/150/200% scaling on Appearance and the connection dialog, scrolling at 200%, and Windows text enlargement to 153%. Original contrast (None), display scale (125%, 1920 x 1200) and text size (100%) restored. Product runs in `.ai-usage-local/AIU-010/t11-offline-state`; no agent-initiated sign-in or source CLI credential access occurred. Full screen-reader and populated live-account acceptance remain outstanding.

Desktop input was briefly paused after the tool detected user input. A fresh observation later confirmed an unchanged idle Connect screen with no authorization in progress; the authorized offline checks resumed. Owner availability for live sign-in and visual review is still pending.

Packaged acceptance: installed development-signed 2026.9.1635.0 in an offline disposable Sandbox. Initial smoke 6/7 exposed a harness selector matching the taskbar application button instead of the tray tooltip. Tightened the selector to `AI Usage · ` in ShellSmoke.cs. Corrected installed smoke 7/7; upgrade to 2026.9.1636.0 preserved package-local preferences and updated smoke passed 7/7. Separate application-ID activation, visible UI and explicit Exit observed. The guest was stopped; no host installation/trust change. Product source and dependencies are unchanged. Primary integrated review and final document/diff checks apply; no new credential/destructive-data/privilege change requiring independent review was introduced.

Owner follow-up: successful Codex and Claude sign-ins reported, followed by inability to add another account or reconnect after disconnect. Confirmed that the live adapter incorrectly labelled its one-provider-slot guard as verified duplicate identity. Added a distinct occupied-slot result and explicit one-account-per-provider help; no provider, credential or connection eligibility logic changed. Presentation 117/117, Infrastructure 131/131, unsigned MSIX 2026.9.1637.0, unpackaged Release build and targeted native capability smoke 1/1 pass. Actual updated provider-help layout inspected. The original credential-bearing Debug process remains running and has not received the source correction. Codex was observed disconnected; Claude had a displayed quota reading. Live reconnect failure is owner-reported and not yet reproduced; deterministic repeat authorization passes through both Add account and account-detail reconnect routes for both provider identifiers.

Exact next action: obtain the owner's exact post-disconnect Connect outcome (Already connected, another error, or no response), then reproduce the owner-led Codex reconnect without importing CLI credentials or automatically signing in. Continue the remaining live lifecycle, screen-reader and separate owner visual checks after fixing any confirmed defect. No commit/push while T-11 is incomplete.

## T-11 owner visual follow-up: tray mark, 2026-09-16

The owner reported that the system-tray icon was an unclear, tiny dot. Root cause confirmed: `MainWindow.xaml` used `GeneratedIconSource` with the placeholder `●` glyph. Replaced it with a bold, larger `↗` usage mark using `Segoe UI Symbol`; the existing attention-level colour mapping and tray commands are unchanged. The Windows smoke harness also now ignores taskbar elements that do not expose the optional UIA `Name` property, without weakening the tray assertions.

PASS: Release unpackaged build with zero warnings/errors; Presentation.Tests 117/117; actual isolated Windows smoke 7/7 after republishing the harness. Evidence is under `.ai-usage-local/AIU-010/t11-tray-icon-fix-smoke/`; no credential-bearing state was used.

Exact next action: owner visually rechecks the rebuilt tray icon at normal Windows scale and confirms it is recognizable and correctly aligned. T-11 remains blocked until that confirmation and any still-open live/screen-reader gates are explicitly accepted. No commit/push while T-11 is incomplete.

## T-11 tray mark correction, 2026-09-16

The owner then reported that the arrow correction rendered as an empty transparent tray slot. The arrow was the failing dependency: `GeneratedIconSource` did not reliably render `↗` with `Segoe UI Symbol` on the current Windows shell. Replaced it with the guaranteed-supported `AI` text mark in regular `Segoe UI`, removed the alignment margin that could clip the glyph, and retained the existing attention-level colour mapping and tray commands.

PASS: Release unpackaged build with zero warnings/errors; Presentation.Tests 117/117; the final fresh isolated Windows smoke passed 7/7. The first full rerun had one close-to-tray click timeout; a clean isolated rerun passed all 7 scenarios. Evidence is under `.ai-usage-local/AIU-010/t11-tray-ai-mark-smoke-rerun/`; no credential-bearing state was used.

Owner confirmation and integration request, 2026-09-16: the owner replied `good` after the rebuilt `AI` tray mark was presented and explicitly requested commit, push and merge into `main`. This is visual acceptance of the tray correction and publication authority for the current snapshot. The remaining live Codex/Claude lifecycle and full screen-reader gates are still open, so T-11 remains blocked and AIU-010 remains in-progress.

Exact next action: continue the live Codex/Claude lifecycle and full screen-reader acceptance. This owner-authorized merge does not claim those remaining checks passed.

Owner publication instruction, 2026-09-16: the owner explicitly instructed Codex to commit, push and merge all current tracked changes. This publishes the current T-11 snapshot by direct owner direction; it does not convert the remaining NOT_RUN/BLOCKED acceptance gates to PASS or mark T-11 done.

## Owner pause, 2026-09-18

The owner paused AIU-010 and selected AIU-008 in a separate branch. The inspected main checkout was clean at `bc67aae`; no AIU-010 implementation was changed. T-11 retains its blocked acceptance result. Existing passing checks and unresolved live reconnect report remain recorded above.

Exact next action on owner-requested resume: reproduce the owner-reported Codex disconnect/reconnect outcome with the owner completing sign-in, then finish the remaining live lifecycle, screen-reader and overall visual acceptance gates. Do not resume this work merely because AIU-008 changes shared provider presentation.

## Owner-requested resume, 2026-09-18

The owner resumed the unfinished feature after merging AIU-008. Clean main and origin/main agree at f4d0fbf; GitHub Validation run 35401416353 passed. Active branch: codex/aiu-010-resume-acceptance. The final AIU-008 Release executable and its passing local evidence are the unchanged starting candidate; do not repeat builds or suites without a new change or concern.

Plan: close the remaining screen-reader and populated-account accessibility coverage, reproduce the reported Codex/Claude reconnect issue through the product UI, verify applicable live lifecycle, and present a concrete final UI for owner visual acceptance. Preserve existing app and source CLI credentials; use isolated test profiles for newly authorized connections. Fix confirmed defects with focused regressions and applicable native checks. Record actual Narrator speech separately from UIA names and synthetic behavior separately from live observations.

Resumed results: actual Narrator speech confirms the demo populated row and refresh/failure announcements, the live account list, connection cancellation, selected theme/density and toggle state, System Status navigation and the Exit dialog. Live Codex fresh connect, refresh, Exit/relaunch with stored grant, and disconnect/account-detail Connect all passed in an isolated profile; the original reconnect report was not reproduced. Claude reached its browser Log in page without an active session. The owner was asked to sign in; the pending app authorization was then cancelled successfully while other accessibility checks continued, so start a fresh connection after sign-in. No product code was changed. Full screen-reader coverage and overall owner visual acceptance remain open; no publication is claimed.

Exact next action: after the owner confirms Claude browser sign-in, start a fresh Claude Browser sign-in from Add account in the current isolated Release app and complete refresh, Exit/relaunch, disconnect/reconnect and applicable reauthentication checks; then finish the remaining screen-reader and overall owner visual acceptance gates. Test profile: `.ai-usage-local/AIU-010/resumed-live-state`. Narrator is stopped. The original reconnect symptom clarification remains optional; do not block the verified current Codex route on that answer.
