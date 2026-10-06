# AIU-046 verification

Evidence for [the specification](spec.md) and [the plan](tasks.md). Host: the owner's
Windows 11 Pro 10.0.26200 desktop, .NET 10, branch `users/5-hour-limits-display-1e0e91`.
Product source at `ce050f1`. The checks ran at `7e0f58e`, which adds only task records
after it. Times are local (+01:00), 2026-10-06.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS. 882/882 on the second run (18:09). The first run (18:07) had 881/882: `DiagnosticCrashTests.ManagedChildCrashLeavesCriticalStackBeforeTermination` timed out. This is the known first-start latency after a rebuild on this host (AIU-045 ANL-05). A plain rerun on the same binaries decides, so no timeout was changed. |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 193/193 (18:07) |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, see the [last section](#validation-and-diff-check-on-the-records-commit) |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, see the [last section](#validation-and-diff-check-on-the-records-commit) |
| Diff check | `git diff --check` and `git diff --check ff1d788..HEAD` | PASS, see the [last section](#validation-and-diff-check-on-the-records-commit) |
| App build (README) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors (18:09) |

## Estimator cost (T-01, commit `ed89d1b`)

T-01 Step 7 ran `dotnet run --project tools/AiUsage.ProviderConsole -c Release -- measure-backend`.
The `sessions/four-pairs` median was 75.9517 ms for four pairs, about 18.99 ms per pair.
That is under the 50 ms per-pair stop threshold. The synthetic data still reaches Ready.
The command creates its observations in a temporary store and makes no provider request.

## Interactive observations

Screenshots are stored under the git-ignored `.ai-usage-local/AIU-046/t04/` folder of the
worktree. They are not committed.

The owner was using the desktop during the run. Captures therefore used `PrintWindow` on
the app window, and scrolling used UI Automation. The cursor was not moved and focus was
not taken, so no tooltip was opened by hovering. Tooltip text was read from each element's
accessible name, which the app builds from the same tooltip lines.

| Observation | Status | Evidence |
| --- | --- | --- |
| Live: a paired card's today strip as one 5h cell with the current used percent | BLOCKED | The unpackaged build (`AiUsage.exe`, README command, no arguments) started beside the installed app without a redirect. It showed the first-run "Add an account" screen, because the development state `%LOCALAPPDATA%/AiUsage/Development` has no connected account. That launch initialised an empty development state; before it, the folder held only the console's `Console` logs. Signing in for the owner is not authorised, and the installed app (`2026.10.604`) predates this branch. Screenshot `live-unpackaged-01.png`. |
| Live: the cell tooltip | BLOCKED | Same reason. |
| Live: footer without a count, or the range if rough | BLOCKED | Same reason. |
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

## Acceptance criteria

| AC | Status | Evidence |
| --- | --- | --- |
| AC-01 | PASS (automated, demo); interactive tray/Left NOT_RUN; live BLOCKED | `LedgerTests`: `FiveHourWithoutAnEstimateIsOneStrip`, `OneWindowCellFollowsLeftModeDayOffAndFull` and `TrayShowsTheOneWindowCell` pass in the Presentation run. Demo H2 was observed. The interactive tray and Left-mode checks were NOT_RUN. The live case is BLOCKED (see AC-07). |
| AC-02 | PASS | `SessionEstimateTests.BoundsContainTrueCostUnderBothRoundingModes` (seed 46, 2,000 runs) in the Infrastructure run |
| AC-03 | PASS | `NoEstimateWithFewerThanTwoWeeklyTicks`, `TwoTickPartMatchesHandComputedBounds` and `SingleWindowReachesRoughByTwentyFiveWhenCostIsTen` |
| AC-04 | PASS | `ConsistentPartsNeverWiden` |
| AC-05 | PASS | `ConflictingOlderPartAndOlderAreExcluded` and `PlanChangeWeeklyInstanceSourceAgeAndExhaustionExclusions` |
| AC-06 | PASS | `RoughEstimateShowsRangeAndSettledShowsOneNumber` and `PairedCardCarriesRoughBoundsAndRange`. Demo H3 and A1 were observed. |
| AC-07 | BLOCKED (live run) | PASS: the suites, document validation and the diff check. PASS: the demo rough and settled cards were observed. BLOCKED: a local unpackaged run on live readings could not show a paired card, because no account is connected in the development state. |

Open item: AC-07 stays BLOCKED until the owner connects Claude in the unpackaged
development build and the live one-window state is observed, or the owner waives the
live clause.

## Validation and diff check on the records commit

The following ran on the working tree that became the records commit (about 18:15):

- `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo`:
  PASS, 82/82.
- `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`:
  PASS, `{"valid":true,"diagnostics":[]}`.
- `git diff --check` on the staged records and `git diff --check ff1d788..HEAD` after the
  commit: PASS, no output.

Status correction after review: AC-07 and AC-01 are relabelled above. T-04 is `blocked`,
AIU-046 is `in-progress`, and the spec and design are `implementing`. On that working
tree the validator printed `{"valid":true,"diagnostics":[]}`, and `git diff --check`
printed nothing.
