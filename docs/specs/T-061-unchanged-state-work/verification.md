# T-061 verification

Code reference: T-061.1 `f7e696e` and `0bf2ea8` (fast-forward into `main`); T-061.2 `f094f68`, `1878061` and
`b867d97`, merged into `main` as `e56ec3e`. Environment: the owner's Windows 11 Pro 10.0.26200 desktop, .NET 10,
Release, desktop unlocked. Date: 2026-10-10. Part of the parallel sweep recorded in the
[T-059 execution ledger](../T-059-stale-ui-smokes/verification.md#execution-ledger). Evidence is in the worker
worktrees' git-ignored `.ai-usage-local/T-061/`.

## Root cause

Lane C measured on `17c79c7` that one source change with identical data raised `RefreshTip`, `Preferences` and
`ValueModeName` on the view model, and the window redrew the tray glyph after each (1.22 ms per glyph plus a shell
call), before the cards were updated, so the drawn colour lagged one change. Each card raised `Visual` and then `""`,
and the generated binding code handles both by rebuilding every Visual binding. The live source published at least
twice a minute, because every tick rebuilt again after refreshing the due accounts even when none was due, re-reading
every series file. The tray miniature rebuilt all rows once per added row, also while hidden; open history rebuilt
the whole grid on each change; the Settings cap list recreated its rows and with them an open cap editor
(reproduced: typed text discarded); and `ShowAndActivate` logged `WindowShown` when the window was already visible.

## Checks

| Check | Tree | Result |
| --- | --- | --- |
| C4 Infrastructure suite | merged `e56ec3e` | PASS, 926/926 |
| C5 Presentation suite | merged `e56ec3e` | PASS, 337/337 (330 before, plus 5 from T-061.1 and 2 from T-061.2) |
| C7 App build (Release, unpackaged) | merged `e56ec3e` | PASS, 0 warnings, 0 errors |
| C6, C2, C3 | merged `e56ec3e` | PASS |
| C8 Launch smoke | `0bf2ea8`, merged `e56ec3e`, `016043d` | PASS 3/3 each, under the desktop lock |
| Required smokes after all items landed (primary) | `016043d` | PASS, as in the [T-059 checks](../T-059-stale-ui-smokes/verification.md#checks) |

An intermittent timeout of `DiagnosticCrashTests.ManagedChildCrashLeavesCriticalStackBeforeTermination` (the 20 s
wait for the probe's exit) appeared in some full C4 runs while several workers built and ran smokes at once. It did
not appear in any merge-gate run, it passes alone, and the reviewer could not reproduce it at base; it is host
variance, not a defect of this item, and the bound was not relaxed.

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `TrayToneTests`: an unchanged source change does not raise `TrayTone`; when cards turn critical, `TrayTone` changes once with the cards already critical. At base, a variant recording the key the window derives at each property change failed with "Expected CritM, Actual OkM"; the reviewer reproduced it at `179c0b5`. |
| AC-02 | PASS | `LiveLedgerSourceTests.ATickWithNoAccountDueBuildsOnceAndReadsEachSeriesOnce`: red "Expected 1, Actual 2", then green; `ATickWithAnAccountDueShowsTheRefreshedReading` guards the due path and passes before and after. |
| AC-03 | PASS | `UnchangedStateTests.ACardRaisesOnePropertyChangePerUpdate`: red ("Visual" then ""), then green. `Visual`, `HiddenCount` and `HiddenTone` are plain properties set only in `BuildVisual`; the reviewer checked that the generated `LedgerCardView.g.cs` refreshes all of them on "". |
| AC-04 | PASS | `AnOpenCapEditorKeepsItsTextAcrossAnUnchangedSourceChange` and `AKeptCapRowFollowsItsCardWhenTheCapsAreEqual`: red (rows recreated), then green; the cap row's `CanAct` binding is now `OneWay`. |
| AC-05 | PASS | R-04, R-05 and R-07 reviewed in the window code; C8 opens history, the tray miniature and settings, and passed on every tree above. |
| AC-06 | PASS | Checks above. |

## Review

- T-061.1 (`179c0b5..0bf2ea8`), per-task review by `aiu-reviewer`: approve, no findings. The reviewer confirmed the
  four new tests fail at base, reran C5 335/335 and C4 922/922, and checked that the early return in the tick hides no
  state change (sign-in, sign-out status, captures and the date each queue their own rebuild).
- T-061.2 with T-062.1 (`179c0b5..b867d97`), per-task review with a T3 look at the `WindowShown` line: approve, no
  findings. `1878061` alone does not build (a staged rename without content); `b867d97` completes it, and the
  published branch history was not rewritten.

## Whole-run review

After all four items had landed, `aiu-reviewer` reviewed the integrated diff `179c0b5..016043d` in a temporary
worktree at `016043d`. Verdict: approve, with 0 Critical, 0 Important and 4 Minor findings. All five merge commits
have an empty combined diff, and each worker's files on `016043d` are identical to its task tip. C4 926/926, C5
337/337, C7 with 0 warnings, `TrayIconOwnerTests` 2/2, `TrayGlyphHandleTests` 2/2, C2 and C6 pass. The reviewer read
the H.NotifyIcon IL again and confirmed that the library stores a new handle only after a successful shell update or
before the tray exists. The primary ran the desktop smokes on `016043d` at the same time (T-059 checks).

- M1, fix held on its branch: if a redraw threw and the tone then returned to the tone the tray already showed, the
  T-061 guard returned early and `TrayRecovered` was never called, so the T-056 failure streak stayed open and a
  later failure was only counted. See Follow-up fix below.
- M2, fixed in the records: the spec's Expected line claimed that unchanged input does no visual-tree or file work
  at all. It now names R-01 to R-07, and the remaining work is listed under Left open.
- M3, left open: `SmokeKit` gained `Wait` and `OwnedWindows`, but per-class `Wait` copies remain in `LedgerSmoke`,
  `CardEditingSmoke` and `RefreshIntervalSmoke`. This is duplication, not one of the four defect classes.
- M4, resolved by the closure: the collapsed tasks.md files named the tray class by its first name (`TrayMark`);
  the landed files are `TrayIconOwner.cs` and `TrayIconOwnerTests.cs`. T-061.1's one-line binding change in
  `LedgerSettingsView.xaml` was outside its declared write set; the primary extended the write set by that line
  before the review.

## Follow-up fix

M1 of the whole-run review. `UpdateTrayGlyph` remembers that a redraw threw and reports recovery when the tray
already shows the wanted tone (first written as `89d9839`, then held back by `0f2b0e7`). The window code has no unit
seam, so the fix rests on review and the smoke.

| Check | Result |
| --- | --- |
| Independent re-check of the fixed lines (`aiu-reviewer`) | Fixed; no new findings |
| C7, C4, C5 on `89d9839` | PASS: 0 warnings; 926/926; 337/337 |
| C8 on `89d9839`, under the desktop lock | BLOCKED, two runs at 11:05: the owner's installed app window covered the test's tray-flyout point, and the owned-point guard refused the click. A 30-minute wait for that window to close ended at 11:36 without it closing. The installed app was not touched. |

The fix touches tray code, so under the blocked-smoke rule it waits on branch `t061-tray-failure-recovery`, made from
`main` after these records. Next action: with the installed app's window hidden, run C8 on that branch, then merge
it under the merge gate.

## Left open

- A tick still publishes once a minute when nothing changed, so each card updates once a minute: its visual
  bindings and its bars rebuild once (halved by R-03, not removed), and the tick's first build reads every stored
  series file once. Skipping it needs value equality of the snapshot or of `CardVisual`, a larger change than this
  item.
- While the tray miniature is shown, it still rebuilds once per row of a change (R-04 covers the hidden window only;
  coalescing the collection events was optional and not done).
- Not confirmed, so not changed: `presenter.IsAlwaysOnTop` set on every change (WinUI may skip equal values).
- `LedgerPreferenceStore` writes the preferences when the already-active Used/Left or Compact segment is clicked;
  rare and user-driven, left as is.

## Not run

| Item | Reason |
| --- | --- |
| In the installed app: the tray colour follows a card change at once, the tray miniature shows current rows when opened, history stays open across a refresh, and a cap edit opened in Settings survives the next refresh | Post-deploy owner check (R-190) |
