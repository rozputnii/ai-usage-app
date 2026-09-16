# AIU-010 verification

## Preparation delivery - 2026-09-15

Scope: documentation, presentation contract proposal and synthetic scenario catalog only. Base code inspected: `ba6b49f`. Existing source mappings were checked against the Core quota/session contracts and Windows dashboard view models. Accepted decisions were checked for future scope, tray, appearance, localization, history and data lifecycle.

| Check | Result | Evidence / limitation |
|---|---|---|
| Source and product-decision mapping | PASS | screens.md and ui-contract.md distinguish current Codex/Claude building blocks from future capabilities; no provider protocol changed |
| Document validation | PASS | SDK 10.0.401: dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json returned valid:true, diagnostics:[] after frontmatter correction. |
| Synthetic fixture consistency | PASS | PowerShell ConvertFrom-Json parsed the catalog; synthetic flag true; 15 unique scenario IDs; all S01–S12 covered. This checks the seed catalog, not future runtime transitions. |
| Primary diff/link/instruction review | PASS | Reviewed the nine-file preparation scope against source mappings and accepted decisions; links and future/live boundaries inspected. No implementation or dependency change. |
| Product regressions / package build / interactive Windows / live providers | NOT_RUN | No product code changed; these remain required at frontend/integration stages |
| Claude design / owner visual approval / Claude frontend | NOT_RUN | Handoff material prepared; no design or frontend artifact exists yet |

AC-01/02/08 preparation coverage is supplied by the inventory, contract and synthetic catalog; this does not establish implemented runtime behavior. AC-03–07 and final runtime AC-09 remain NOT_RUN. AIU-010 is not complete when T-01 finishes.

The first document validation found missing design frontmatter. Added the required id/type/status/goal/scope_version; final validation result is recorded above after rerun.

## Mock frontend delivery (T-04 – T-09) - 2026-09-16

Scope: the presentation layer, WinUI shell and deterministic demo services inside `AiUsage.Windows`, on branch `codex/aiu-010-mock-frontend`. Backend integration (T-10/T-11) is not part of this delivery and stays NOT_RUN. Core and Infrastructure sources are untouched. Nothing was installed on the host: the app ran unpackaged and the MSIX was built for validation only.

| Check | Result | Evidence / limitation |
|---|---|---|
| Presentation view-model regressions | PASS | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` → Total: 100, Failed: 0 |
| Infrastructure regressions | PASS | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` → Total: 130, Failed: 0 |
| Project validation tests | PASS | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` → Total: 78, Failed: 0 |
| Document validation | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` → `{"valid":true,"diagnostics":[]}` |
| Whitespace/diff hygiene | PASS | `git diff --check` exit 0 (with new files intent-added) |
| Native Debug build (unpackaged) | PASS | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None` → 0 errors, 0 warnings |
| MSIX package build | PASS | `./tools/windows/Build-Package.ps1 -MsixVersion 2026.9.1601.0` → `AiUsage.Windows_2026.9.1601.0_x64.msix`, sha256 561ECA…F3DBE, status `unsigned-validation-only`. Unsigned and not installed; no host registration changed |
| Windows UI smoke (UI Automation, actual desktop) | PASS | `AIU_SMOKE_EXE` = built `AiUsage.exe`; `AiUsage.Windows.Tests` → Total: 6, Failed: 0 (launch, navigation, theme, close-to-tray, tray-exit, repeated-exit). Each scenario asserts a clean exit with code 0; screenshots and per-scenario JSON in `.ai-usage-local/AIU-010/evidence/` |
| Every designed page reachable and rendered | PASS | smoke `navigation` walks all five nav tabs and asserts a page-specific element each time; `page-Nav*.png` |
| Themes System / Light / Dark | PASS | smoke `theme` switches all three appearance cards and asserts the appearance note; `theme-ThemeLight.png`, `theme-ThemeDark.png`, `theme-ThemeSystem.png` |
| Tray behaviour and explicit Exit | PASS | smoke `close-to-tray` (close hides, tray icon stays, tray dashboard restores the same HWND) and `tray-exit` (context menu → confirmation → exit code 0); `manual/tray-menu.png`, `manual/tray-exit-dialog.png` |
| Simulated high contrast | PASS | demo toggle swaps the token dictionary and re-resolves every reference; sampled pixels changed from `#1F1E1B`/`#6CCB8A` to `#000000`/`#3FF23F` and back; `manual/hc14.-1.png`, `manual/hc-accounts.-1.png`, `manual/hc-dialog2.png` |
| Keyboard access | PASS | Tab order Overview → Refresh all → Add account → account row → row refresh → demo marker; arrow keys move focus across the nav group and Space activates (`manual/keyboard-nav2.-1.png`). Verified by UI Automation focus queries |
| Content scale and compact layout | PASS | 150 % and 200 % (`manual/scale150b.png`, `manual/scale200-wide2.-1.png`) and the 560 px compact layout (`manual/compact560.-1.png`). At 200 % with the demo panel open the effective width falls below the design's compact minimum and header labels clip; the same scale is clean once the panel is closed or the window is wider |
| Reduced motion | PASS | demo Motion = Reduced motion; refresh completes without spinner/shimmer animation (`manual/reduced-refresh.png`). Gating is `MotionSettings.Allowed`; this is a still-frame check, not a frame-by-frame measurement |
| Destructive and data flows | PASS | export preview, typed-`RESET` factory reset (returns to the F01 empty state), notification preview toast: `manual/export.png`, `manual/factory.png`, `manual/factory-done.png`, `manual/toast.-1.png` |
| Mock-only composition | PASS | `DependencyBoundaryTests` (7) forbid Core/Infrastructure use in this delivery and assert every adapter resolves to a `Demo…` service; the demo marker is asserted at launch by every smoke scenario |
| Core/Infrastructure untouched | PASS | `git diff --stat 744e4e0 -- src/windows/AiUsage.Core src/windows/AiUsage.Infrastructure tests/windows/AiUsage.Infrastructure.Tests` is empty |
| Live providers, credentials, notifications, updates, install | NOT_RUN | Out of scope by the owner brief: no provider call, no credential read, no Windows notification, no update and no install were performed |
| Backend integration (AC-07), owner visual acceptance | NOT_RUN | T-10/T-11; this delivery is mock-only and is not an acceptance of AIU-010 as a whole |

Defects found and fixed while verifying (each re-checked after the fix): implicit `ScalarTransition`/`BrushTransition` inside control templates crashed WinUI layout a few seconds after launch (removed, deviation recorded in tasks.md); a `GradientStop` reused across rebuilt shimmer brushes threw on the second theme change (`SkeletonBlock` now builds a fresh stop); a cancelled first History query left the chart area blank (the load key is reset on cancellation); reset text overflowed its column on account rows (star-sized column with trimming); pages resolved stale colours when the simulated contrast was toggled (`ThemeService.RefreshContrast` re-resolves new content); the shell nav had a single tab stop and no arrow navigation, so only Overview was keyboard-reachable (`Controls.ArrowNavigation`, applied to the shell nav and the settings tabs).

Known limitations: WinUI re-resolves `ThemeResource` references only on a theme change, so the simulated-contrast toggle passes each root through the opposite theme; content created afterwards (navigated pages, dialogs, tray popup) is refreshed explicitly. The page roots carry `AutomationProperties.AccessibilityView="Control"`, but WinUI still does not surface them, so the smoke test identifies pages by a page-specific control instead.
