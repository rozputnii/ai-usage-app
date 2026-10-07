# AIU-051 verification

Evidence for [the specification](spec.md). Host: the owner's Windows 11 Pro 10.0.26200
desktop, .NET 10, branch `users/subscription-tiles-layout-18d92f`, merged with `main` at
`c83e44e`. Times are local, 2026-10-07.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release -- -noLogo` | PASS, 257/257 on the merged tree |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 902/902 on the merged tree |
| App build (README) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors |
| Windows test build | `dotnet build tests/windows/AiUsage.Windows.Tests -c Release` | PASS, 0 errors |
| Local Ledger smokes | `AiUsage.Windows.Tests -method "*LedgerLaunchSettingsHistoryAndExit" -method "*ConfirmedDeletionAndInterruptedDeletionRestartCleanly"` against the Release unpackaged build, isolated temporary state | PASS, 4/4 (demo and live-empty launch, settings, ⋯ menu, Delete stored data cancel and confirm, interrupted deletion) |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, 82/82 |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, valid with no diagnostics after AIU-051 was added to the G-003 scope |
| Diff check | `git diff --check` | PASS |
| Physical-click UI audits and Sandbox corpus | `tools/windows/Run-UiAuditSandbox.ps1` | NOT_RUN; opt-in under the Preview gate. The audit sources were updated for the ⋯ menu, title-bar Used/Left and the + menu toggle, and compile. |
| MSIX package build | CI on push | NOT_RUN locally; CI builds the unsigned package |

## Acceptance

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 One column | PASS | Demo app (`--demo`, Brief) screenshots `closed.png` and `open.png`: every card is full width in one column; with settings open the cards narrow beside the sheet and widen again after Esc. |
| AC-02 Sliding sheet and edge | PASS | `open.png`: the sheet is a rounded, outlined surface lighter than the page and the cards. Rapid Esc, Esc, Ctrl+, ×3 ended with the sheet at its full width (`t1.png`). The slide itself was observed only as start and end states. |
| AC-03 Content, menu, delete | PASS | `open.png` shows only Work days, Caps with warning marks and edit/remove icons, View, Updates and the footer; `menu.png` lists the five menu actions; `delete.png` shows the short warning, Delete and Cancel with focus on Cancel. |
| AC-04 View model | PASS | `LedgerTests.SettingsListCapsWithTheirStatus`, `SettingsFooterShowsTheIntervalWhileAllSynced`, `UpdatesSectionShowsVersionStatusAndModeSelector`, `InstallButtonStaysVisibleAndBusyWhileInstalling` |
| AC-05 Checks | PASS | Commands above |
| Installed app after deployment | NOT_RUN (post-deploy owner check, D-190) | The owner checks the updated installed app. |

Screenshots are in the git-ignored `.ai-usage-local/AIU-051/` folder of the worktree.
They were captured from the Debug demo build before the merge with `main`, which changed
only card colours (D-192). UI Automation invoked Settings, More settings and Delete stored
data; keyboard input was sent to the app window.

## Review

Routine presentation change; no credential, destructive-data or privilege change (Delete
stored data keeps its in-place confirmation and only moves into a menu), so CONTRIBUTING
requires no independent review.
