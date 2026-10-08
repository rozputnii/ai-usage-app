# AIU-055 verification

Evidence for [the specification](spec.md), recorded by the controller as tasks integrate
(see [tasks.md](tasks.md), "Execution model"). Host: the owner's Windows 11 Pro
10.0.26200 desktop, .NET 10.

## Status

In progress. The plan was written on 2026-10-08 from `main` at `b81ef9e` and committed at
`22e1324`. Wave 1 (T-01 to T-06) started from `22e1324`. Rows below are the workers'
reported results, recorded after the controller confirmed each commit on `origin/main`.

## Commands

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

Integrated commits: T-06 `dbb0b8c`; T-05 `88b13fb`; T-03 `8ce42df`, `f20091a`; T-02 `804945f`, `13fb1a7`; T-08 `999a188`; T-01 `dc9664f`, `88828c8`; T-07 `d94b7b8`, `10ed014`.
