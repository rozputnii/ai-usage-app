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

Integrated commits: T-06 `dbb0b8c`; T-05 `88b13fb`; T-03 `8ce42df`, `f20091a`.
