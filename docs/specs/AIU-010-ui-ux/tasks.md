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
- status: pending
- depends_on: [T-10]
- acceptance: AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09
- evidence: not-run

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