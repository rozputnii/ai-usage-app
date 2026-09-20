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

## T-10 available-service integration — 2026-09-16

Base `3ba2c88`, branch `codex/aiu-010-live-adapters`. The clean intervening commit after `ca61583` only changed development instructions, so the requested baseline investigation/tests were not repeated. Evidence below belongs to T-10; it does not mark T-11 or AIU-010 complete.

Implementation: Windows-owned `Adapters/Live` uses the existing Core workflows and Infrastructure Codex/Claude sessions. Provider protocol, DPAPI formats and source CLI stores are unchanged. Product startup resumes only app-owned grants. Packaged paths remain package-local; unpackaged development uses a separate owned root. `--demo` does not register provider or persistence services. Presentation metadata is versioned and atomically replaced separately from grants; invalid/newer metadata remains untouched. Unimplemented mutation capabilities return Unsupported and controls are disabled. Existing provider slots do not imply multi-account support.

| Check | Result | Evidence / limitation |
|---|---|---|
| Presentation regressions | PASS | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`: 113/113. Covers cached resume, unknown/exhausted readings, independent restrictions, no false refresh success, reauthentication after cancellation, manual-code authorization continuity, duplicate/unsupported connections, disconnect, stream cancellation/drain, startup/Exit race, preference persistence and failure preservation, and retained demo regressions. |
| Infrastructure regressions | PASS | Same Release/no-restore command for Infrastructure.Tests: 131/131; includes staged preference cancellation, last committed bytes and unrelated-file preservation. Existing provider suites remain intact. |
| Unpackaged Windows build | PASS | Debug/x64, `WindowsPackageType=None`, no restore; zero warnings/errors after final product-lifecycle change. |
| Focused independent review | PASS (primary resolution) | Fresh read-only reviewer inspected frozen tree `d10d21b7457f6edb44b774cf9a86a2dbbc3e04ed` against `3ba2c88`, using convergence-review. One P2 finding: dropped Codex group allowed/limit-reached flags and undisplayed limit reason. Added nullable group fields, mapping and independent account-detail messages; regression first failed for the missing contract then passed. Reported percentages remain unchanged. Primary integrated review also added/tested startup draining during preference load. No unresolved material finding. Reviewer ran no independent tests or live checks. |
| Earlier offline Windows candidates | FAIL, superseded | First and second product smoke attempts each passed 3/6. Screenshots exposed the absent-demo-view-model visibility fallback; corrected by a product-hidden parent. Empty first-run slots were removed. UIA page-root/demo-only markers did not identify the empty product view; harness now checks existing visible controls, maximizes its owned window, and tolerates disappearing unrelated desktop elements. One tray restoration failure and one transient COM enumeration failure were retained in local evidence. |
| Corrected offline product smoke | PASS | `t10-product-smoke-3`: 6/6 actual local WinUI/FlaUI scenarios (launch, navigation, themes, close-to-tray/restoration, tray Exit and repeated Exit). Empty isolated app data; no provider login or real quota request. Later final-candidate checks are recorded below. |

Local evidence is under `.ai-usage-local/AIU-010/`; screenshots and activation/result JSON are not committed because they can include desktop context. They are actual captures, not generated mockups. No app package was installed, no trust changed, no source CLI credentials read/imported, and no live authentication was started.

### T-11 acceptance remains separate

T-11 remains pending. NOT_RUN for the integrated candidate: owner visual acceptance; full keyboard/screen-reader/high-contrast/100–150–200% acceptance matrix; packaged installation/activation/update behavior; live Codex/Claude sign-in, renewal/resume, quota refresh, reconnect and disconnect. The historical provider results and current synthetic regressions are not new live evidence. The offline smoke subset above does not establish those outcomes or complete any future backend AIU.
Product candidate checks before the isolated demo resource-key correction (unsigned package `2026.9.1633.0`):
- PASS: presentation 113/113 after the demo help-key regression/fix; Infrastructure 131/131 remains applicable because no Infrastructure changes followed that run.
- PASS: Debug/x64 unpackaged build, zero warnings/errors. Final local product smoke `t10-product-accepted`, 7/7: launch, available-page navigation and disabled History, System/Light/Dark changes, close-to-tray/restoration, tray Exit, repeated Exit, and capability gates in Add account and Data settings. Each owned process exited with code 0. The run used an empty isolated development root, no grant or authentication. Screenshots were inspected for first-run state and product connection options; no demo panel is visible.
- PASS: offline Release/x64 unsigned MSIX `2026.9.1633.0`, built with VS MSBuild and existing restored packages (no `/restore`). Archive identity/version checked. SHA-256 `FE69ED9C972EF4B1F6AC019362D750692D9E57B0DAC20272B94E24D90B37C996`. SDK-only warning: optional `mspdbcmf.exe` missing, so no symbols package was produced; no owned-code warnings. This is build validation, not installation or release evidence.
- PASS: prior demo candidate `t10-demo-final`, 7/7, and its configured product data directory remained absent. The later demo run `t10-demo-accepted` passed 6/7 but failed opening Add account due to a missing demo help-text resource key; the process exited with XAML error 0xc000027b. A focused regression reproduced the missing key, and the fix gives demo help its own valid resource. This failure and its final correction remain recorded separately below.
- PASS: project document validator returned `valid:true`, `diagnostics:[]` before the final evidence update; final validation and diff check are required before commit.

No new CI run or remote merge is claimed. T-10 publication is a task-branch commit/push only; T-11 and all NOT_RUN limitations above remain unchanged.
Final correction verification:
- PASS: `DemoProviderHelpUsesAnExistingResource` reproduced the missing key, then the complete Presentation suite passed 113/113 after correction.
- PASS: final Debug/x64 unpackaged build, zero warnings/errors; final demo Windows smoke `t10-demo-verified` passed 7/7, including Add account and capability checks. All owned processes exited with code 0, and `t10-demo-isolation-verified` remained absent. Product smoke `t10-product-accepted` remains 7/7 applicable to the unchanged product code path; the last correction changes only the demo resource lookup.
- PASS: rebuilt unsigned MSIX `2026.9.1634.0` after the demo correction; archive identity/version verified. SHA-256 `78512D58FD8C77411CFE9ACBE9EC3496E22DFDE101D327CDF013C71B89FB8B7A`. Same optional SDK symbols-tool warning as above; no owned-code warnings, installation or signing.
- PASS: final primary acceptance/diff review; independent finding resolved through the restriction-metadata regression. Provider implementations and Core source are unchanged. Final document validation and staged diff check recorded by the T-10 commit.

T-10 is complete. T-11 remains pending with the explicit NOT_RUN acceptance above.

## T-11 integrated acceptance pass — 2026-09-16

Candidate: `5358d2f` on `codex/english-prompts-specs`, including T-10 `90b07c4`. Clean initial checkout; work continues on `codex/aiu-010-integrated-acceptance`. `git diff 90b07c4..5358d2f` contains only five added instruction lines in AGENTS.md. Application, provider, dependency and packaging source are unchanged. This pass changes only the Windows smoke tray selector and acceptance records. Reused results below are baseline evidence, not newly executed checks.

| Check | Result | Evidence / limitation |
|---|---|---|
| Intervening-change inspection | PASS | Git history and full diff inspected; only the English prompt/specification rule changed. |
| Reused deterministic regressions and native builds | PASS | T-10 Presentation 113/113, Infrastructure 131/131, final unpackaged build and unsigned MSIX 2026.9.1634.0 remain applicable to unchanged source. No redundant regression run is claimed. |
| Retained package identity | PASS | Recomputed SHA-256 `78512D58FD8C77411CFE9ACBE9EC3496E22DFDE101D327CDF013C71B89FB8B7A` matches T-10. Authenticode reports NotSigned. This is not installation evidence. |
| Reused offline tray/lifetime and capability smoke | PASS | T-10 product 7/7 and final demo 7/7 remain applicable. Read retained `close-to-tray.json`, `tray-exit.json` and `repeated-exit.json`: passed true, exited true, exitCode 0. No new tray run is claimed. |
| Product launch and accessibility names/states | PASS | Actual unpackaged Debug app, fresh isolated `t11-offline-state`, Windows 1920 x 1200 at initial 125% scale. Empty first-run view; named navigation/provider controls; disabled History, CLI import and empty Refresh all. `t11-acceptance/product-initial.*`. This is a UI Automation tree check, not screen-reader acceptance. |
| Product keyboard subset | PASS | From Overview, Right then Space opens Accounts; Right skips disabled History to Settings; Space opens Settings; Tab skips disabled Refresh all to Add account; Enter opens the dialog; Shift+Tab wraps from Close to Claude within it; Escape dismisses. Visible focus outlines captured in `keyboard-accounts`, `keyboard-skip-disabled-history`, `keyboard-settings`, `keyboard-tab-add`, `keyboard-dialog-open`, `keyboard-dialog-wrap` and `keyboard-dialog-dismissed`. Enter did not activate the Settings radio; Space did. The tool's focused-element field remained a root pane despite visible focus changes, so it is not used as focus proof. |
| Settings keyboard navigation | PASS | Focused Appearance, then Right/Space successively reached Monitoring, Data & privacy and Updates with visible focus outlines; `keyboard-monitoring.*`, `keyboard-data.*`, `keyboard-updates.*`. UIA exposes unavailable monitoring, history/export and update commands as disabled. No data/security settings were changed. |
| Actual Windows high contrast subset | PASS | Changed Windows Accessibility > Contrast themes from None to Night sky. Existing Settings and newly opened Add account dialog responded without app restart: readable text, control boundaries, provider names/glyphs and focus styling; Settings explicitly states contrast takes precedence. `windows-night-sky-settings.*`, `windows-night-sky-dialog.*`. None restored afterwards. This does not cover populated quota rows or every contrast theme. |
| Native display-scale subset | PASS | After a brief desktop-input pause, fresh observation confirmed the same idle Connect screen and no authorization in progress; offline checks resumed. Windows Display settings selected 100%, 150% and 200% at 1920 x 1200. Appearance and Add account remained readable; the 200% window/maximized layout kept header actions visible and lower settings reachable by scrolling. Evidence: `scale100-appearance`, `scale100-connect`, `scale150-appearance`, `scale150-connect`, `scale200-appearance-restored-window`, `scale200-appearance-scrolled`, `scale200-connect`. The earlier `windows-scale100-settings` capture is occluded and excluded; `scale200-appearance` includes the OS snap flyout and is not clean visual evidence. Restored the original 125%. This covers these empty-profile surfaces, not every populated account state. |
| Windows text enlargement subset | PASS | Windows Accessibility > Text size changed from 100% to the observed 153%. App navigation wrapped; Appearance text and connection provider controls enlarged and remained readable. `text153-appearance.*`, `text153-connect.*`. Restored 100% and confirmed the disabled Apply button. This is native text scaling, not the demo simulation. |
| Screen reader and full accessibility matrix | NOT_RUN | Named UIA controls and visible keyboard focus are only a subset. Narrator announcements, populated quota semantics and full page/control coverage remain unperformed. |
| Owner visual acceptance | BLOCKED | Owner participation requested, with running-app or screenshot review options. No owner verdict received; agent inspection and prior design selection do not substitute for final owner acceptance. |
| Live Codex and Claude connect/resume/refresh/reauthentication/disconnect | BLOCKED | Requested owner availability for manual browser sign-in using a fresh isolated profile. No response or completed sign-in observed. No agent-initiated authorization, provider quota request, automatic sign-in or CLI credential import. Existing provider live evidence remains historical. |

Screenshots and accessibility snapshots are local under `.ai-usage-local/AIU-010/t11-acceptance/`; captures may contain desktop context and are not committed. The product app is left open for owner handoff. The native contrast and display-scale changes were restored; no security/privacy setting or host trust was changed.

### Packaged acceptance and harness correction

The concrete reason for Sandbox was installation, package identity/storage and upgrade acceptance. The guest mapped only staged binaries/scripts read-only and a dedicated evidence directory writable; networking, clipboard, audio/video input and printer redirection were disabled. No credential directory was shared. Guest ID `87e70594-48b9-4c66-ae03-57ba493f58f9`; local artifacts in `t11-guest-input/`, `t11-guest-evidence/` and `t11-acceptance/` under `.ai-usage-local/AIU-010/`.

| Check | Result | Evidence / limitation |
|---|---|---|
| Development package builds/signing | PASS | Offline VS MSBuild using existing restore, versions 2026.9.1635.0 and 2026.9.1636.0, existing owned development certificate `771CB0E8F982D2DCB02484CD2D1E148B6DD7E774`. No owned-code warnings; existing optional missing-symbols-tool warning. `package-build.log`, `package-update-build.log`. Same application source as T-10. These are local acceptance artifacts, not releases. |
| Host signature trust verification | FAIL, expected | Both build wrappers returned exit 1 because the development certificate is untrusted on the host; original `package-evidence.json` files retain `failed-not-installable`. No host certificate import or trust bypass. Signature validation succeeded only after guest-only TrustedPeople provisioning. |
| Fresh guest installation | PASS | Microsoft-signed offline prerequisites validated; .NET 10.0.12 installed and Add-AppxPackage installed 2026.9.1635.0. SHA-256 `F3C5DCB4D23411E41CAEC221BF35BE5D189409E311BCB5561401C03C52E00B18`. Initial `t11-guest-evidence/report.json`. Provisioning took several minutes; installer logs established progress, not a product hang. |
| Initial installed smoke | FAIL, superseded | 6/7; `installed/tray-exit-failure.png` shows the taskbar jump list (Pin to taskbar / Close window), not the notification-area menu. `FindTrayButton` searched the entire taskbar for a name starting with `AI Usage`, so it selected the visible application button. Close-to-tray passed because that button disappears when hidden. |
| Tray selector correction and installed smoke | PASS | Matched the existing tooltip prefix `AI Usage · `, which all Tray_Tip resource variants use and the taskbar button does not. Kept the tray/menu/lifetime assertions unchanged. Published the corrected self-contained harness offline, then actual guest smoke passed 7/7, all owned app processes exit 0: `corrected/installed.stdout.txt` and scenario JSON. The observed initial failure is the red test; the corrected run is green. No application code changed. |
| Package-local storage and upgrade | PASS | Corrected harness changed Appearance; `LocalState/appearance.v1.json` existed and the deliberately configured unpackaged fallback directory stayed absent. Upgraded to 2026.9.1636.0; preference file hash unchanged. Update SHA-256 `86DCC1E410B970DE935105257E4131217D00A45E2CBB6A90B70C12D73F5326AE`. `corrected/report.json`. This tests MSIX upgrade retention with identical application source, not the unavailable in-app updater or grant migration. |
| Updated installed smoke | PASS | 7/7, all owned app processes exit 0; `corrected/updated.stdout.txt`, `corrected/upgraded/*.json`. Some screenshots were cropped as host scaling changed the remote display; they are not full visual acceptance. |
| Packaged application-ID activation and manual Exit | PASS | Launched `AiUsage.Dev_951d0pt9hnds0!App` through `shell:AppsFolder`, with no app process already running. Guest reported version 2026.9.1636.0 and visible PID 3392. Inspected the first-run view, maximized it, pressed Ctrl+Q, observed confirmation and clicked Exit. Later guest process inspection found no AiUsage process. `aumid-activation.json`, `inspection.json`; fresh stable-scale screenshots `packaged-aumid-maximized.png`, `packaged-aumid-exit-confirm.png`. No exit code was captured for this separate manual activation. |
| Guest teardown | PASS | `wsb stop` targeted the recorded guest; `wsb list --raw` returned an empty environment list. Evidence retained on the host. No host app registration, dependencies or trust changed. |

Primary integrated review: PASS for the three-file tracked diff, preserving application behavior, provider/credential boundaries and future capability gates. The harness correction excludes the observed wrong target without weakening assertions; all tooltip variants were checked against Resources.resw. No credential, destructive-data or privilege implementation change requires a new independent review under CONTRIBUTING; T-10's focused review remains applicable to unchanged product source. Relevant post-change checks: corrected Windows harness publish and installed/upgraded smoke above. Document validator returned `valid:true, diagnostics:[]`; `git diff --check` exited 0, repeated after the final evidence edit. These passes do not close the outstanding owner/live acceptance gates.

Environment note: the first local package wrapper had an incorrect root and failed before building, creating an empty output directory at `C:/Users/danii/projects/.ai-usage-local/AIU-010/packages/2026.9.1635.0`. Corrected the wrapper root and used repository-local outputs. Automatic approval review rejected cleanup of that empty external directory with only `blocked by policy`; it was left untouched. No application evidence is attributed to the failed wrapper invocation.

T-11 is incomplete. AC-05's screen-reader and populated live-account accessibility coverage, final owner visual acceptance and the integrated live lifecycle are not established. Offline keyboard/contrast/scaling subsets and packaged evidence above do not replace those gates. No future backend capability was enabled or marked complete. Publication remains withheld under CONTRIBUTING's completed-task rule.

### Owner-reported connection findings and message correction, 2026-09-16

Candidate: `5358d2f` plus the working diff on `codex/aiu-010-integrated-acceptance`. This supersedes the earlier statement that product source is unchanged: the connection result contract, live guard's result kind, presentation result mapping and English resources now distinguish an occupied provider slot from a verified duplicate identity. Provider storage, authentication protocols and connection eligibility are unchanged. Earlier native contrast/scaling and packaged installation/upgrade evidence remains baseline evidence, not execution against this correction.

| Check | Result | Evidence / limitation |
|---|---|---|
| Initial live sign-ins | PASS, owner-reported | Owner reports successful Codex and Claude sign-ins. No agent-initiated authentication or source CLI credential access. This is not independent verification of refresh, resume or reauthentication. |
| Additional account message | FAIL, corrected in source | Owner screenshots show Add account and Already connected. `LiveConnectionFlow.ConnectAsync` returned Duplicate solely because a provider slot was occupied, without identity verification or opening a browser. A distinct ProviderSlotOccupied result now explains the current single-slot limit and existing account actions. Multi-account remains unavailable. |
| Regression reproducing misleading message | FAIL, superseded | New occupied-slot presentation test expected One account per provider and observed Already connected before the correction; targeted adapter run was 13 pass, 1 fail. |
| Final presentation regressions | PASS | Release suite 117/117. Covers occupied-slot wording and no browser launch, plus repeat connect/disconnect/authorize through Add account and account-detail reconnect routes for both provider identifiers, using synthetic sessions only. |
| Infrastructure regressions | PASS | Release suite 131/131; no Infrastructure or Core source changes. |
| Live disconnect/reconnect report | FAIL, unresolved owner report | Owner reports inability to sign in after disconnect. Read-only account inspection of original Debug PID 28972 found Codex labelled Disconnected, disabled Refresh and enabled Connect; Claude had a displayed quota reading. This establishes current UI state, not successful reconnect or the root cause of the reported failure. Requested the exact post-disconnect outcome. No live Connect or Disconnect action was performed by the agent. |
| Updated unpackaged build and native smoke | PASS | Release Windows build; capability smoke 1/1, owned PID 19404 exit 0. Isolated fresh profile `t11-message-fix-state`; evidence `t11-message-fix-smoke/capabilities.json`. Preserved the owner's original Debug instance and its state. |
| Updated native provider help | PASS | Separate isolated Release PID 14160: actual Add account help clearly shows the one-account-per-provider limit and UIA exposes the complete text. Stable screenshot `t11-message-fix-smoke/provider-help.png`. Esc dismissed the dialog; Ctrl+Q/Exit closed the isolated instance, process absence verified. Occupied-slot result wording itself has deterministic presentation evidence, not updated live native execution. |
| Unsigned package build | PASS | 2026.9.1637.0, SHA-256 `130779CB1BA8DF0544959C77FB46D2BF71DDDB248EEBAAD633E0829079870A4A`; local package-evidence.json is unsigned-validation-only. Existing missing mspdbcmf.exe tooling warning prevents a symbols package. No installation or trust change; installation/upgrade baseline remains 1635/1636. |
| Integrated review | PASS | Primary reviewed the complete scoped diff: existing slot guard and reconnect eligibility retained; actual duplicate handling in demo unchanged; no credential/state mutation added, no future capability enabled. Security-lifecycle and provider-evidence boundaries inspected. No material credential/destructive-data/privilege change requiring new independent review was made. |
| Owner visual approval / remaining live lifecycle | NOT_RUN / BLOCKED | Sign-in report is not visual approval. Full screen-reader, refresh, Exit/relaunch resume, reauthentication and successful reconnect remain unestablished in this pass. T-11 remains incomplete and unpublished. |

### Owner visual follow-up: tray mark — 2026-09-16

| Check | Result | Evidence / limitation |
|---|---|---|
| Owner-reported tray defect | CONFIRMED | The owner observed an unclear tiny dot in the system tray. Source inspection identified `GeneratedIconSource Text="●"` as the cause; this was a placeholder glyph, not an asset-offset problem. |
| Tray icon correction | PASS | Replaced the dot with a bold, larger `↗` mark using `Segoe UI Symbol`; existing attention-colour mapping remains in `MainWindow.xaml.cs`, and tray click/menu/lifetime behaviour is unchanged. |
| Presentation regressions after correction | PASS | Release suite 117/117. |
| Unpackaged Windows build after correction | PASS | Release/x64 `WindowsPackageType=None`, zero warnings/errors. |
| Actual Windows tray/lifetime smoke after correction | PASS | Republished the self-contained harness and ran the fresh isolated product profile; all 7/7 scenarios passed, including close-to-tray, tray restore, tray menu Exit and repeated Exit. Evidence: `.ai-usage-local/AIU-010/t11-tray-icon-fix-smoke/`. |
| Harness compatibility correction | PASS | The first host smoke attempt hit an optional FlaUI `Name` property on an unrelated taskbar element. The harness now treats that property as unavailable and still requires the exact `AI Usage · ` tray tooltip prefix; the rerun passed 7/7. |
| Owner visual confirmation | NOT_RUN | The owner must recheck the rebuilt icon at normal Windows scale. T-11 remains incomplete; this correction does not close the live provider or screen-reader gates. |

### Owner-reported blank tray mark correction — 2026-09-16

The owner reported that the arrow correction appeared as an empty transparent tray slot. Source inspection confirmed that the problem was the `↗` glyph/font combination, not an asset offset. `GeneratedIconSource` now uses the ASCII `AI` mark in regular `Segoe UI` with no clipping margin; the existing severity colour mapping and tray behavior remain unchanged.

| Check | Result | Evidence / limitation |
|---|---|---|
| Owner-reported blank icon | CONFIRMED | The previous arrow-based correction was not visibly rendered in the owner's tray. |
| Visible-mark source correction | PASS | `MainWindow.xaml` now uses `Text="AI"`, `FontFamily="Segoe UI"`, a large bold size and no `TextMargin`; `MainWindow.xaml.cs` still maps the worst attention level to the foreground colour. |
| Presentation regressions after correction | PASS | Release suite 117/117. |
| Unpackaged Windows build after correction | PASS | Release/x64 `WindowsPackageType=None`, zero warnings/errors. |
| Actual Windows tray/lifetime smoke after correction | PASS | Final fresh isolated run passed 7/7 scenarios, including close-to-tray, tray restore, tray menu Exit and repeated Exit. Evidence: `.ai-usage-local/AIU-010/t11-tray-ai-mark-smoke-rerun/`. One earlier full run had a close-to-tray click timeout; the clean rerun passed. |
| Owner visual confirmation | PASS | The owner replied `good` after the rebuilt `AI` tray mark was presented. This confirms the tray correction only; live provider and screen-reader gates remain open. |

### Owner-authorized integration of the current snapshot — 2026-09-16

The owner explicitly requested commit, push and merge of the current AIU-010 snapshot into `main` after confirming the tray correction. This is direct integration authority, not evidence that the remaining live Codex/Claude lifecycle or full screen-reader checks passed. T-11 remains `blocked` and AIU-010 remains `in-progress` until those gates are separately completed.

### Owner-directed publication — 2026-09-16

The owner explicitly requested that all current tracked changes be committed and pushed. This is publication authority for the current T-11 snapshot, not evidence that the remaining NOT_RUN/BLOCKED acceptance gates passed; T-11 remains incomplete unless those gates are separately accepted and recorded.

## T-11 resumed acceptance, 2026-09-18

Candidate f4d0fbf on codex/aiu-010-resume-acceptance. Main CI run 35401416353 completed successfully. The unchanged final AIU-008 Release build and passing local regressions/package/product/demo smoke remain baseline evidence; no new source change has been made in this pass.

Actual Narrator subset: PASS. Launched the installed Windows Narrator, used its Speech recap window, and inspected the actual speech transcript while running --demo. The transcript included the Add account heading, Close button, selected Sign in radio control, and a populated account row with provider, freshness, percent remaining, reset time and list position. Refresh all announced its start, then the partial failure and retained cached readings. This is speech-recap evidence, not merely UIA names; remaining full-page/populated live coverage is still NOT_RUN. Synthetic refresh produced 4 updated and 1 failed with preserved cached data. Local evidence: .ai-usage-local/AIU-010/resumed-acceptance/narrator-refresh.png. Narrator was stopped after this subset; no accessibility preference was deliberately changed.

Reference for the invoked screen-reader commands: [Microsoft Narrator keyboard commands](https://support.microsoft.com/en-us/accessibility/windows/narrator/appendix-b-narrator-keyboard-commands-and-touch-gestures). The helper could only expose the Narrator window title through UIA because Narrator runs at higher integrity, but its native speech recap was visibly readable. No privilege or security setting was changed.

### Live Codex lifecycle, resumed pass

The owner authorized independent final checks using the existing browser. The current Release app used the fresh isolated development root `.ai-usage-local/AIU-010/resumed-live-state`; no source CLI credentials or original application grants were read or imported. Browser account selection and consent used the existing OpenAI session.

| Check | Result | Evidence / limitation |
|---|---|---|
| Fresh browser connection | PASS | Native result was Connected and First reading recorded; a populated Codex account displayed the live plan, quota, reset and observation timestamp. |
| Account refresh | PASS | Updating transitioned to Fresh and Codex updated with a newer observation timestamp. |
| Exit and relaunch | PASS | Ctrl+Q / Exit stopped PID 2232. Relaunch as PID 15316 restored the same single account without browser authorization; Refresh all completed with 1 account updated and All accounts updated. |
| Disconnect and account-detail Connect | PASS | Confirmed Disconnect in this isolated profile. Refresh became disabled, state became Disconnected and Connect became available. Connect opened Reconnect; browser authorization finished with Reconnected and Monitoring resumed, returning the same single account with a fresh reading. Screenshot: `.ai-usage-local/AIU-010/resumed-acceptance/codex-reconnected.png`. |
| Original reconnect report | NOT_REPRODUCED | The observed current Release account-detail route succeeded. The original Debug process is absent; this does not establish the historical failure's cause or every alternative route. |
| Claude browser connection | BLOCKED | The browser reached Claude's Log in page with no active Claude session. Requested owner sign-in; no password, mailbox, source CLI grant or account-creation flow was accessed. The remaining Claude lifecycle requires that session. |

Full screen-reader acceptance, applicable reauthentication and overall owner visual acceptance remain open. These results supersede the previous unexecuted Codex lifecycle subset only.

Additional actual Narrator subset: PASS. Speech recap confirmed the live Codex account list name, remaining percentage, selected state and position; hidden/disconnected checkbox labels and unchecked states; the connection dialog's manual-code fallback and Cancel controls; the cancellation announcement; selected Settings/System Status navigation; System theme and its explanation; Comfortable selected, Compact non-selected and Always on top off; and the Exit AI Usage dialog/group and Exit button. Evidence: `.ai-usage-local/AIU-010/resumed-acceptance/narrator-live-settings.png` and `narrator-exit-dialog.png`. The Claude wait was cancelled through the app and reported Cancelled. Nothing was changed. Exit was cancelled, Narrator was stopped and its process absence verified. Claude login may be completed in the browser, but a fresh app authorization must follow. This representative subset does not establish exhaustive page-content reading, every dialog, or Claude populated-account accessibility.

Documentation checks: PASS, project validator returned `valid:true, diagnostics:[]`; `git diff --check` passed. Primary review found only the scoped backlog/task/evidence changes, with private account identifiers, grants and screenshots kept out of Git. Product source is unchanged, so unchanged product regression/build suites were not repeated. T-11 remains incomplete and this resumed evidence has not been committed or pushed.
