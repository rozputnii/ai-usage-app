# AIU-052 verification

Evidence for [the specification](spec.md). Host: the owner's Windows 11 Pro 10.0.26200
desktop at 125 % scale, .NET 10, branch `users/window-system-buttons-min-size-39c0a1` from
`main` at `05ee609` and merged with `main` at `ab7298d`. Times are local, 2026-10-08.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 265/265 on the merged tree |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 902/902 on the merged tree |
| App build (README) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors |
| Windows test publish | `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true` | PASS |
| Window-size smoke | `AiUsage.Windows.Tests -method "*SqueezedWindowKeepsTheTitleRowClearOfTheCaptionButtons"` against the Debug unpackaged build, demo mode, isolated temporary state | PASS, 1/1, repeated on the merged tree |
| Ledger launch smoke | `AiUsage.Windows.Tests -method "*LedgerLaunchSettingsHistoryAndExit"` against the same build | PASS, 2/2 (demo and live-empty) |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, 82/82 |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, valid with no diagnostics |
| Diff check | `git diff --check` | PASS |
| MSIX package build | CI on push | NOT_RUN locally; CI builds the unsigned package |

## Acceptance

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 Projection | PASS | `LedgerRefreshStatusTests` (four cases) and `BriefWindowListsNineCardsInUserOrder` |
| AC-02 Demo icon | PASS | `size-brief.png`: red icon after the title, with the per-account tooltip; `size-day-off.png`: neutral icon. |
| AC-03 Minimum size | PASS | The smoke squeezes the window to 200 × 120 px in the brief scenario, on the day off and with Work today on, and checks Settings against the DWM caption-button bounds and Refresh against Used. The day-off and Work today title rows (with the demo button) are wider than the launch width, so turning Work today on grew the window. The screenshots show the first card's header and primary limit in full. |
| AC-04 Checks | PASS | Commands above |
| Installed app after deployment | NOT_RUN (post-deploy owner check, D-190) | The owner checks the updated installed app with live accounts. |

Screenshots are in the git-ignored `.ai-usage-local/size-evidence/` folder of the worktree.
UI Automation is reported without bounds for the caption buttons of the extended title bar,
so the smoke reads them with `DwmGetWindowAttribute(DWMWA_CAPTION_BUTTON_BOUNDS)`.

## Review

Routine presentation change; no credential, storage, destructive-data or privilege change,
so CONTRIBUTING requires no independent review.
