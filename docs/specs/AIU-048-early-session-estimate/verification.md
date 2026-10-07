# AIU-048 verification

Evidence for [the specification](spec.md) and [the plan](tasks.md). Host: the owner's
Windows 11 Pro 10.0.26200 desktop, .NET 10, branch `users/5-hour-limits-display-1e0e91`.
Product source at `bbe71fe`. The checks ran on that tree after the whole-branch review
fixes; the records commit that follows adds only documents. Times are local (+01:00),
2026-10-06.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 884/884 on the first run (18:36). An earlier run of the same tree before the last test edit had one failure in the new Cover test, which was fixed before this run; it was not the diagnostics timeout. |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 193/193 (18:36). The first run at this tree had 192/193: `TrayShowsTheOneWindowCell` expected the label without the demo account name; the assertion was corrected and the second run passed. |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, see the [last section](#validation-and-diff-check-on-the-records-commit) |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, see the [last section](#validation-and-diff-check-on-the-records-commit) |
| Diff check | `git diff --check` and `git diff --check ff1d788..HEAD` | PASS, see the [last section](#validation-and-diff-check-on-the-records-commit) |
| App build (README) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors (18:36) |

## Estimator cost (T-01, commit `ed89d1b`)

T-01 Step 7 ran `dotnet run --project tools/AiUsage.ProviderConsole -c Release -- measure-backend`.
The `sessions/four-pairs` median was 75.9517 ms for four pairs, about 18.99 ms per pair.
That is under the 50 ms per-pair stop threshold. The synthetic data still reaches Ready.
The command creates its observations in a temporary store and makes no provider request.

## Interactive observations

Screenshots are stored under the git-ignored `.ai-usage-local/AIU-048/t04/` folder of the
worktree. They are not committed.

The owner was using the desktop during the run. Captures therefore used `PrintWindow` on
the app window, and scrolling used UI Automation. The cursor was not moved and focus was
not taken, so no tooltip was opened by hovering. Tooltip text was read from each element's
accessible name, which the app builds from the same tooltip lines.

| Observation | Status | Evidence |
| --- | --- | --- |
| Live: a paired card's today strip as one 5h cell with the current used percent | NOT_RUN (post-deploy owner check, D-190) | Attempted before D-190: The unpackaged build (`AiUsage.exe`, README command, no arguments) started beside the installed app without a redirect. It showed the first-run "Add an account" screen, because the development state `%LOCALAPPDATA%/AiUsage/Development` has no connected account. That launch initialised an empty development state; before it, the folder held only the console's `Console` logs. Signing in for the owner is not authorised, and the installed app (`2026.10.604`) predates this branch. Screenshot `live-unpackaged-01.png`. |
| Live: the cell tooltip | NOT_RUN (post-deploy owner check, D-190) | Same as above. |
| Live: footer without a count, or the range if rough | NOT_RUN (post-deploy owner check, D-190) | Same as above. |
| Demo `states`, H2 (no estimate, demo stand-in for the live case) | PASS | `--demo --scenario=states`. One today cell with the "5h" label, filled to 40 %, with a neutral track. The footer reads "33 % used" with no count. Cell name: "Current 5h window · until 17:10. 40 % used. Window count: collecting data". Screenshot `demo-states-02-h2-h3.png`. |
| Demo `states`, H3 (rough) | PASS | Four 5h cells. The footer reads "50 % used · ≈ 3–7 × 5h left", and the dash is U+2013 (read through UI Automation). Footer name: "One 5h window about 10 percent of 7d (7–13 percent) · rough · from 3 windows". Screenshots `demo-states-03-h3.png` and `demo-states-03-h3-zoom.png`. |
| Demo `states`, A1 (settled) | PASS | Three 5h cells. The footer reads "50 % used · ≈ 4 × 5h left". A1 sets no bounds, so its tooltip keeps the existing "(estimate)" line. Screenshot `demo-states-01.png`. |
| Visual tooltip display, the tray miniature and Left mode | NOT_RUN | Hovering and tray clicks were not done while the owner used the desktop. The Presentation tests cover these. |

## Rulings made during execution

- **R3.** The demo count uses `rough ? high : ws`, so H3's range starts at
  `floor(50 / 13) = 3`, as observed.
- **R7.** In Left mode the one-window cell draws `[Allow | Track]`, following D-186.
  This is pinned by `OneWindowCellFollowsLeftModeDayOffAndFull`.
- **R8.** The tooltip bounds round outward to whole numbers, as in H3's "(7–13 %)".
- **R10.** The tooltip upper bound `WindowShareHigh` rounds up to 0.1, the lower bound down, so
  the shown range always contains the guaranteed bounds (`PairedCardCarriesRoughBoundsAndRange`).

## Acceptance criteria

| AC | Status | Evidence |
| --- | --- | --- |
| AC-01 | PASS (automated, demo); interactive tray/Left and live NOT_RUN (post-deploy owner check, D-190) | `LedgerTests`: `FiveHourWithoutAnEstimateIsOneStrip`, `OneWindowCellFollowsLeftModeDayOffAndFull` and `TrayShowsTheOneWindowCell` pass in the Presentation run. Demo H2 was observed. The interactive tray and Left-mode checks were NOT_RUN. The live case is BLOCKED (see AC-07). |
| AC-02 | PASS | `SessionEstimateTests.BoundsContainTrueCostUnderBothRoundingModes` (seed 46, 2,000 runs) in the Infrastructure run |
| AC-03 | PASS | `NoEstimateWithFewerThanTwoWeeklyTicks`, `TwoTickPartMatchesHandComputedBounds` and `SingleWindowReachesRoughByTwentyFiveWhenCostIsTen` |
| AC-04 | PASS | `ConsistentPartsNeverWiden` |
| AC-05 | PASS | `ConflictingOlderPartAndOlderAreExcluded` and `PlanChangeWeeklyInstanceSourceAgeAndExhaustionExclusions` |
| AC-06 | PASS | `RoughEstimateShowsRangeAndSettledShowsOneNumber` and `PairedCardCarriesRoughBoundsAndRange`. Demo H3 and A1 were observed. |
| AC-07 | PASS; live clause NOT_RUN (post-deploy owner check, D-190) | PASS: the suites, document validation and the diff check. PASS: the demo rough and settled cards were observed. The live one-window state is checked by the owner in the updated installed app after deployment (D-190); the earlier unpackaged attempt had no connected account. |

Post-deploy owner check (D-190): after the Preview built from this merge updates the
installed app, the owner checks the paired card's one-window cell, its tooltip and its
footer on live readings. An agent can check the installed version and its logs on
request.

## Validation and diff check after the whole-branch review fixes

The following ran at `bbe71fe` plus the edited records (about 18:38):

- `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo`:
  PASS, 82/82.
- `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`:
  PASS, `{"valid":true,"diagnostics":[]}`.
- `git diff --check ff1d788..HEAD` and `git diff --check` on the edited records: PASS, no
  output.

The review fixes were: a restored `Cover` gap and endpoint test, the tick allowance clamped
at 0 (`TickAllowanceNeverGoesNegativeWhenFiveHourFalls`), the tray tip of the one-window
cell keeping its window line, `WindowShareHigh` rounded up (R10), and the specification's
approval line. Mutation checks: bridging `Cover` fails the new coverage test, an exclusive
end fails it and eight more, and removing the clamp fails the allowance test.

## Integration with main, 2026-10-07

`main` had meanwhile assigned AIU-046 and D-188 to other work, so this item was
renumbered to AIU-048 and D-189; commits before the merge carry `AIU-046`. `main` at
`55eb2db` was merged into the branch; the code merged without conflicts and the backlog,
decisions and goals conflicts were resolved by keeping both sides. Under D-190 the live
clauses are post-deploy owner checks, so T-04 is `done`, AIU-048 is `done` and the
specification and design are `implemented`.

Checks on the merged tree:

- Infrastructure suite: PASS, 902/902.
- Presentation suite: PASS, 239/239.
- `tests/AiUsage.ProjectValidation.Tests`: PASS, 82/82; `tools/AiUsage.ProjectValidation`:
  `{"valid":true,"diagnostics":[]}`.
- Release app build (`-p:Platform=x64 -p:WindowsPackageType=None`): 0 warnings, 0 errors.
- Release `--demo` startup smoke: PASS. The process stayed up and showed the visible
  "AI Usage" window with its card tree (UI Automation); it was then closed.
- `git diff --check` on the merge: PASS.
