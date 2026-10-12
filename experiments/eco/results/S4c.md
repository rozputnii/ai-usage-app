# S4c results (account `main`)

The [UI/UX spacing and motion task](../tasks/real-ui-motion.md#brief) (T2, Windows UI), arm A
against the frozen arm E, base `041919c` (`main` on 2026-10-12) for both runs, arm A first,
`-TimeoutMinutes 240`. The owner's picks in the brief were the approved variants; neither run
rendered variants or asked about them.

## Usage

| When | Session | Week (all models) |
| --- | --- | --- |
| Before the batch, 2026-10-12 18:28 (gate pass) | 9% | 90% |
| After arm A, 19:19 (gate pass) | 22% | 91% |
| After arm E, 20:10 (gate pass) | 29% | 92% |
| After the judge, 20:14 | 31% | 93% |
| After the merge checks, 20:25 | 32% | 93% |

S4c spent $59.21 API-equivalent (runs $55.61, judge $3.60), above the $20-35 estimate (a
UI task with smokes and reviews ran 51 minutes in both arms), for 22 session points (about $2.7
a point, as in S4a-S4b) and 3 weekly points (about $20 a point; integer rounding).

## Pair

| Run | Arm | Exit | Wall | Cost | Calls | Subagents | Commits | Output tokens |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `rf362d6` | A (opus high) | 0 | 50.7 min | $36.66 | 272 | 4 | 7 | 301k |
| `r334286` | E (opus medium, eco v2, roles) | 0 | 51.0 min | $18.95 | 180 | 3 | 4 | 181k |

Both runs built the one-strip bar fill (pure schedule with unit tests red to green), drag
variant C (lift, live gap, settle), the two durations and one curve in `LedgerMotion` and a few
spacing fixes, added end-state smokes, recorded the owner picks in the spec, put smoothness under
"Pending owner checks", and ran an independent review. Both reported C9 BLOCKED for the same host
reason: the Visual Studio Build Tools MSBuild that `Build-Package.ps1` selects has no .NET SDK
resolver (as in S3 RP1).

- A (1279 added lines in 19 files): `BarSweep` edge model with a doubled eased last move (a
  full bar stays at 450 ms), `BarFill` shared by the strip and the period bar, a tray fix (bars
  refilled on every data change) found by its reviewer with a red-then-green smoke, an exit crash
  found by C8 and fixed, R-205 amended by the new decision. Also ran the full UI suite: 65/66,
  the one failure (`WorkBudgetPopoverShowsCreditsInDollarsAndSetsToday`) also fails at the base.
- E (760 added lines in 17 files): smaller change; two review rounds; left four Minor deviations
  open. Its final message began "This was already done before the reminder fired", a report
  written as a reply to a late reminder rather than as the brief's report block.

Judge `jbb9fcb` ($3.60, 4.6 min, X = E, Y = A): **A better**, confidence medium. Both: C2, C4
943, C6, C7, C8 3/3 PASS, C9 BLOCKED (host), card smokes PASS (E 4/4, A 5/5); C5 E 353, A 359;
acceptance 14/15 each (C9); no false claims. E: 4 Minor (period-bar segments matched by index,
the dragged card's open history panel slides with the other cards, the eased tail runs past
450 ms to 675 ms, R-205 not amended). A: 2 Minor (AC-08 marked PASS while C9 BLOCKED; the tray's
edge dictionary is never pruned).

## Merge

A's seven commits on branch `feat/motion-spacing-polish` (worktree `C:\src\aiu-motion`); `main`
had not moved, so no merge commit. Numbered T-070 and R-224 with `Set-ItemNumbers.ps1`
(`df0e3a2`), after the offline restore the fresh worktree needed. On the merged tree: C3, C2, C6
PASS; C4 943/943, C5 359/359, C7 0 warnings, C8 3/3 (desktop lock, offline `win-x64` restore)
PASS. `CardEditingSmoke` first failed 4/5: in `TrayBarsHoldStillWhenTheRowsChange`, a second tray
row lookup sat outside the `COMException` guard the first one had (a test race, not product);
moved inside the guard, then 5/5 (`a527036`, Test-Erosion clean). `/code-review` (high): no
Critical or Important, six Minor; AC-08 corrected to BLOCKED (C9), one finding wrong (the 4 px
toggle margin is R-07's stated choice), four left as recorded. No T3 area, so no
`/security-review`. No Major finding.

C9 held the merge at first: the gate needs it for a Windows UI change, and C9 is not a smoke,
so the blocked-smoke rule does not cover it. The operator's `workflow_dispatch` of
`validation.yml` on the pushed branch (its `windows-package` job runs `Build-Package.ps1`;
`preview` runs only on `main`) was refused twice by the auto-mode classifier as a CI bypass, the
second time after the owner chose it, and not worked around. The owner started it: run
38299182671 on `a527036`, `validate` and `windows-package` success, `preview` skipped: **C9
PASS**. The owner also repaired the Build Tools components; the local `Build-Package.ps1` then
passed too (unsigned validation MSIX, 18 s). AC-08 back to PASS, C3 and C6 PASS (`f74d2ab`, docs
only). Fast-forwarded and pushed to `main` (`f74d2ab`, confirmed in `origin/main`). Product inputs
changed, so the push starts a Preview (CI not watched by the operator).

Pending owner checks (T-070, after deploy): the fill, lift, gap, settle, slide and fades feel
smooth with live accounts.

E against A: cost -48%, calls -34%, wall equal; quality lower (four Minor deviations from the
brief, among them the 675 ms tail and the missing R-205 amendment).

## Process notes

- A fresh worktree needs the offline restore one-liner from verification.md before
  `Set-ItemNumbers.ps1`; its `--final` check otherwise fails with NETSDK1004.
- The host's C9 blocker (S3 RP1, both S4c runs) is fixed: the owner repaired the Build Tools
  components on 2026-10-12, and `Build-Package.ps1` passes locally. Pairs judged before that
  (S3 RP1, S4c) had C9 BLOCKED equally in both arms.
- The auto-mode classifier refuses `gh workflow run` as a CI bypass even after the owner's
  answer in chat; the owner runs a dispatch, or adds a permission rule.
- The pre-existing `WorkBudgetPopoverShowsCreditsInDollarsAndSetsToday` failure (fails at
  `041919c`) is not tracked by an item yet.

## Summary

| Pair | A cost | E cost | Verdict | Merged |
| --- | --- | --- | --- | --- |
| UI motion and spacing (T2) | $36.66 | $18.95 | A better (medium) | A, T-070 (`f74d2ab`) |
