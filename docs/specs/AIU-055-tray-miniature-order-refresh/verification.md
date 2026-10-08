# AIU-055 verification

Evidence for [the specification](spec.md), recorded by the controller as tasks integrate
(see [tasks.md](tasks.md), "Execution model"). Host: the owner's Windows 11 Pro
10.0.26200 desktop, .NET 10.

## Status

Implemented and verified on 2026-10-08. The plan was written from `main` at `b81ef9e` and
committed at `22e1324`. Eleven autonomous workers (T-01 to T-11) integrated into `main`;
the controller confirmed each commit on `origin/main` before recording it. The integrated
checks (T-12) passed on `87c34e6`, and a whole-feature review found no Critical or Important
issue. The owner's check of the updated installed app is NOT_RUN until after deployment
(D-190).

## Integrated checks (T-12)

Run by the controller on `main` at `87c34e6`, with all tasks integrated. Smokes ran under
the desktop lock against the Debug demo build; evidence is in the controller worktree's
git-ignored `.ai-usage-local/AIU-055/T-12/`.

| Check | Command | Result |
| --- | --- | --- |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS 85/85 |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json --final` | PASS, valid, no diagnostics |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS 915/915 |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 330/330 |
| Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors |
| Debug app build | the same with `-c Debug` | PASS, 0 warnings, 0 errors |
| Whitespace | `git diff --check 22e1324 HEAD` | PASS |
| Ledger launch smoke (Preview gate) | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` | PASS 3/3 (demo Compact, demo Comfortable, live-empty) |
| Card editing smokes | `-method "*CardEditingSmoke*"` | PASS 2/2 (click rename, grip drag) |
| Refresh stepper smoke | `-method "*RefreshIntervalSmoke*"` | PASS 1/1 |
| Work budget regression smokes | `-method "*WorkBudget*"` | PASS 2/2 |
| Whole-feature review | `aiu-reviewer` (opus) on `git diff 22e1324..87c34e6 -- src tests` and the deferred findings | Approve; AC-01 to AC-10 met; cross-task checks (tray and order, interval and staleness, rename and drag, tray focus and width) clean. Two Minor findings left: `TodayOnlyCell` keeps two unused parameters from the plan's interface; `SettingsSummaries.RefreshInterval` is no longer shown but R-14 keeps it. The one fix-before-done item was R-10's glyph wording, now clarified in the spec |

Screenshots opened by the controller: `tray-icons.png` and `tray-icons-comfortable.png`
(provider marks, one 14 px bar per row, rings for Claude and Codex, the Antigravity error
mark in `CritText`, density padding), `ledger-demo.png`, `ledger-demo-comfortable.png` and
`ledger-live-empty.png` (Settings with the `Refresh` row and no interval caption in the
footer), `rename-hover.png` and `rename.png` (dotted underline and `Rename` tooltip, saved
names), `drag.png` and `drag-cancelled.png` (lifted card, grip, insertion line; the order
unchanged after Esc), `settings-refresh.png` (− disabled at 1), and the regression
screenshots `work-budget-used.png`, `work-budget-left.png`, `credits-popover.png`,
`credits-today-set.png`, `credits-usd-popover.png`, `percent-cap.png` and
`percent-cap-popover.png` (unchanged behaviour).

## Known limits and follow-ups

- After a click outside an open rename, focus lands on the card's first icon and its
  tooltip shows briefly; a drag started while a rename is open can show the same tooltip.
- Alt+↑/↓ moves the account from any control in the account card, not only the header
  (one card per account, D-191). A held Alt+↓ can drop presses; each move is validated.
- An already open refresh tooltip keeps its old number until it closes.
- The launch smoke can invoke the owner's installed app's tray icon once and dismiss it
  with Esc, because UI Automation does not show which process owns a tray icon.
- The card tooltip "Not renewed for over 15 min" stays true for longer tolerances but is
  less precise.

## Not run

| Item | Reason |
| --- | --- |
| Owner check of the updated installed app: tray flyout, provider-mark tooltips, the wrapped empty-tray text, click rename, grip drag and the refresh stepper with live accounts | Post-deploy owner check (D-190) |
| `AuditTrayControls` | Windows Sandbox opt-in; compiled only. Esc closing the flyout was checked with throwaway probes (T-01) |
| A stale row with a five-hour ring | Not in the demo scenarios; the ring takes the strip's opacity in `LedgerTrayWindow`, checked by code review only |

## Per-task evidence

Reported by each worker and recorded after the controller confirmed the commit on
`origin/main`.

| Check | Command | Result |
| --- | --- | --- |
| T-06 Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS 915/915 (baseline 905/905; +10 `ReadingContinuityTests`) |
| T-06 Presentation suite (regression) | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 297/297 |
| T-06 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-06 demo startup smoke | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS 2/2 (demo and live-empty) |
| T-06 independent review | `aiu-reviewer` (opus) on `git diff origin/main...HEAD` | Approve; AC-10 Core part met; no findings. Each of the four replaced 15 min uses is pinned by exactly one new test |
| T-05 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 302/302 (baseline 297/297; new tests failed to compile first) |
| T-05 Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS 915/915 after merging T-06 |
| T-05 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-05 demo startup smoke | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS 2/2 on `88b13fb` and after the merge on `31a1870` |
| T-05 independent review | `aiu-reviewer` (opus) | Approve; AC-07 move/persist/reject, window and tray order, Alt+arrows met (drag is T-09). Minor, deferred to the final review: a held Alt+Down can drop presses; the order's duplicate check is case-sensitive; Alt+arrows work from any control in the account card |
| T-03 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 305/305 on the merged tree (baseline 297/297; 3 new `FiveHourRingTests` failed with CS0103 first) |
| T-03 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-03 demo startup smoke | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS 2/2, rerun after each merge |
| T-03 ring drawing probe | throwaway, uncommitted probe window, screenshots in the worker's git-ignored `.ai-usage-local/AIU-055/T-03/` | Arc clockwise from 12 o'clock; full circle at 1, 1.4 and 0.9995; rail only at 0, 0.0005, -0.2 and NaN |
| T-03 independent review | `aiu-reviewer` (opus) | Approve; AC-03 geometry met. Minor fixed: a doc comment on clamping (`f20091a`). Minor left: tests cover the maths only (T-07 shows the ring in the tray screenshot) |
| T-02 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 306/306 after the merge (baseline 297/297; `EveryProviderHasADistinctMark` failed with CS0103 first) |
| T-02 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-02 demo startup smoke | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS 2/2 |
| T-02 mark renders | each path as SVG rendered by `msedge --headless --screenshot` at 16 and 64 px; WPF `Geometry.Parse` of all four strings | Four distinct, recognisable marks (Claude spark, OpenAI knot, Copilot helmet, Antigravity arch); thinnest walls 1.60 (Copilot) and 1.90 (Codex) units; all parse within 0..24. In-app WinUI rendering is covered by T-10 |
| T-02 independent review | `aiu-reviewer` (opus), then a scoped re-review | Approve; two Minor findings (Copilot walls under 1.5 units, an overstated note) fixed in `13fb1a7` |
| T-08 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 316/316 after the merge (baseline 302/302; 10 new tests failed against an interface-only stub first) |
| T-08 Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS 915/915 (baseline 915/915) |
| T-08 Release and Debug app builds | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` (and `-c Debug`) | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-08 demo startup smoke | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS (demo and live-empty) before review and on merged `a208817` |
| T-08 independent review | `aiu-reviewer` (opus) | Approve; AC-08 stored value, AC-09 and AC-10 app part met; no thread-safety issue. Minor, deferred to the final review: no test pins the tolerance passed by `GetHistoryAsync`; the "over 15 min" tooltip copy is imprecise above a 15 min tolerance; the demo accepts `RefreshMinutes` outside 1..60 |
| T-01 Ledger launch smoke (red, then green) | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | FAIL on the old flyout (18 focusable XAML elements), then PASS 2/2 on merged HEAD. The flyout screenshot `tray.png` shows no focus frame; a row click opens the window at the account |
| T-01 Esc and click-away probes | throwaway, uncommitted probes on the demo app | Esc closes the flyout with nothing focused (3 of 3 runs); moving the foreground to the taskbar hides it |
| T-01 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 316/316 on merged HEAD |
| T-01 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-01 `AuditTrayControls` | `dotnet build tests/windows/AiUsage.Windows.Tests -c Release` | Compiles; run NOT_RUN (Windows Sandbox opt-in) |
| T-01 independent review | `aiu-reviewer` (opus) | Approve; AC-01 met. Minor fixed in `88828c8` (comment placement, audit wording) |
| T-07 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 322/322 (baseline 316/316; tray tests moved from `LedgerTests` to `TrayMiniatureTests`; new tests failed to compile first, and the dashed-track test failed with its rule broken) |
| T-07 Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS 915/915 |
| T-07 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-07 Ledger launch smoke | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS 3/3 (demo Compact, demo Comfortable, live-empty) before and after the merge. `tray.png` and `tray-comfortable.png`: one 14 px bar with radius 6 per row, rings at 72 % and 91 % in tone colours, no ring for Copilot and Antigravity, larger Comfortable padding, no focus frame. Reviewer measured 18 px bars at 125 % (14 logical) |
| T-07 independent review | `aiu-reviewer` (opus), then a scoped re-review | Approve; AC-02, AC-03 (projection, tooltip, colour, no ring), AC-04 (bar, radius, padding) met. Two Minor findings fixed in `10ed014` by controller ruling: the dashed and solid red tracks are pinned by a test; a stale ring dims with its bar. The dimmed stale ring is not in any screenshot (no stale row with a ring in the Brief scenario) |
| T-04 rename smoke (red, then green) | `AiUsage.Windows.Tests.exe -method "*ClickingTheNameRenamesTheAccount*"` under the desktop lock | FAIL before the change and FAIL without the focus-loss save, then PASS. Covers Enter, a click outside, focus loss by Tab, clicking a second card's name while renaming (saves the first, opens the second; Review Focus 3) and Esc. `rename-hover.png` shows the dotted underline and the `Rename` tooltip |
| T-04 card editing and launch smokes | `-method "*CardEditingSmoke*"` and `-method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS; launch smoke PASS 3/3 on the final merged tree |
| T-04 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 322/322 on the final merged tree |
| T-04 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-04 independent review | `aiu-reviewer` (opus), then a scoped re-review | Approve; AC-06 and every R-09 point met. Minor fixed in `c6bd591`: F2 resets the hover look; the smoke saves by focus loss alone. Known limit: after a click outside, focus lands on the card's first icon and its tooltip shows briefly |
| T-11 stepper smoke (red, then green) | `AiUsage.Windows.Tests.exe -method "*RefreshIntervalSmoke*"` under the desktop lock | FAIL on the pre-feature build ("Missing Refresh interval in minutes"), then PASS. `settings-refresh.png`: the `Refresh` row with − disabled at 1 and the box reading `1`; the footer keeps only the system status and ⋯ |
| T-11 launch smoke | `-method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS 3/3 before and after the merge |
| T-11 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 325/325 (baseline 322/322; `StepperSavesWithinOneToSixty`, `StepperTextEdgeCases` and one more) |
| T-11 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-11 independent review | `aiu-reviewer` (opus), two rounds | Approve. Minor fixed in `f6bf869`: focus loss saves unless the box's own menu is open; tests save 1 and 60. Known cosmetic point: an already open tooltip keeps its old number until it closes |
| T-09 card editing smokes (red, then green) | `AiUsage.Windows.Tests.exe -method "*CardEditingSmoke*"` under the desktop lock | Drag smoke FAIL before the code ("Missing Reorder Claude Pro"), then PASS 2/2 (rename and drag) before and after the merge. `drag.png` shows the lifted card, the grip and the 2 px insertion line; a drag released after Esc changes nothing |
| T-09 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 328/328 after the merge (baseline 322/322; `ReorderMathTests` failed to compile first) |
| T-09 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-09 launch smoke | `-method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | PASS 3/3 before the merge. FAIL 2/3 after it (both demo rows, `NullReferenceException` at `LedgerSmoke.cs:367`; live-empty PASS), the same with `main`'s versions of the T-09 files. Cause: the owner's installed app auto-updated to a Preview and its tray icon now precedes the test app's in the overflow. The smoke fix is assigned to T-10, which owns `LedgerSmoke.cs` |
| T-09 independent review | `aiu-reviewer` (opus), then a scoped re-review | Approve. Minor fixed in `c1e844c`: a dropped card stays in place while the order saves; the Esc step checks the lift. Left: a `Limit settings` tooltip can show during a drag started while a rename was open |
| T-10 launch smoke (tray lookup red, then green) | `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` under the desktop lock | `main`'s tray lookup FAIL (`NullReferenceException` at `LedgerSmoke.cs:367`, the installed app's icon first in the overflow); after the fix (chevron by AutomationId `SystemTrayIcon` and a `Show Hidden Icons` prefix, overflow opened only when closed) PASS 3/3, run twice on the merged tree. The smoke measures the tray content at 260 px (it measured 307 px against 325 expected before the frame fix). `tray-icons.png`: four provider marks, the Antigravity error mark in `CritText`, aligned bars and rings |
| T-10 Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS 330/330 on the merged tree (baseline 322/322; `RowsCarryTheProviderAndANamedTip`, `SameProviderRowsAreToldApartByTheirTooltip`) |
| T-10 Release app build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors; `git diff --check` clean |
| T-10 independent review | `aiu-reviewer` (opus), two rounds | Round 1 Important: the flyout drew about 247 px because the window width included invisible resize borders; fixed in `4f2b8a1` test-first. Round 2 approve. Left: the smoke can still invoke the installed app's tray icon once, then Esc (UIA does not expose a tray icon's process). The mark tooltip on hover and the wrapped empty-tray text are not in any screenshot |

Integrated commits: T-06 `dbb0b8c`; T-05 `88b13fb`; T-03 `8ce42df`, `f20091a`; T-02 `804945f`, `13fb1a7`; T-08 `999a188`; T-01 `dc9664f`, `88828c8`; T-07 `d94b7b8`, `10ed014`; T-04 `6231cbe`, `c6bd591`; T-11 `274091e`, `f6bf869`; T-09 `7114ed7`, `c1e844c`; T-10 `29f0fb3`, `4f2b8a1`, `918b1cd`.
