# Motion and spacing polish - verification

Evidence for [the specification](spec.md). Host: Windows 11 Pro 10.0.26300, .NET SDK
10.0.401, Release builds, interactive desktop unlocked; every smoke ran under the desktop
lock. Branch `run` from `main` at `041919c`; final code at `d863a2b`. Times are local,
2026-10-12.

## Checks

Results on the final code unless a row says otherwise.

| Check | Result |
| --- | --- |
| Red to green, `BarSweepTests` and `ReorderMathTests` | FAIL 11/14 against stubs (`NotImplementedException`), then PASS 14/14. The review's trailing-track fix updated `PeriodBarEdgesFillTheTrackBetweenSegments`: FAIL (4 edges where 3 were expected), then PASS |
| C5 Presentation suite | PASS, 359/359 (adds `BarSweepTests`, 9 cases, and 2 `ReorderMathTests` cases) |
| C4 Infrastructure suite | PASS, 943/943 |
| C7 App build | PASS, 0 warnings, 0 errors |
| C8 Launch smoke | First FAIL 2/3 (`738b896`): after Ctrl+Q both demo cases exited with code 1, and the critical log named an `IndexOutOfRangeException` in `TodayStrip.Apply`. A fill still drew frames while its closing window had no width. Fixed in `7456678`; since then PASS 3/3 on every run (two consecutive runs on the final code), with no critical log |
| Card editing smokes (`*CardEditingSmoke*`) | PASS 5/5 in the full suite run: rename, drag (now also checks the settle), tooltips, `UsageBarsFillToTheirLayout` and `TrayBarsHoldStillWhenTheRowsChange` |
| `TrayBarsHoldStillWhenTheRowsChange` (added for the review finding) | FAIL against the tray code of `2253c3c`: the Claude Pro tray bar was caught mid-refill at capture 25. Then PASS 3/3 on `d863a2b` |
| `UsageBarsFillToTheirLayout` repeated | PASS 5/5 consecutive runs after the comparison tolerance below, and 3/3 after the review fixes |
| Window-size smoke (`*SqueezedWindowKeepsTheTitleRowClearOfTheCaptionButtons*`) | PASS 1/1 with the wider title-row gaps; PASS again in the full suite |
| UI smoke suite (`-method "*"`) | 65 of 66 run cases PASS; 39 explicit audit cases not run. The one FAIL is `WorkBudgetPopoverShowsCreditsInDollarsAndSetsToday` (`LedgerActivationSmoke.cs:102`: the Copilot popover does not reopen after Save). It fails the same way against the base commit `041919c`, built in a temporary worktree, so it predates this change |
| C9 Package build | BLOCKED on this host. The latest Visual Studio Build Tools MSBuild that `Build-Package.ps1` selects has no .NET SDK resolver (`Could not resolve SDK "Microsoft.NET.Sdk"`). With `MSBuildSDKsPath` set, it then cannot resolve `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`. Installing the missing component is a host install, so it was not done. Supplementary evidence, not C9: the same packaging properties run through `dotnet msbuild` built an unsigned `AiUsage.Windows_2026.10.1203.0_x64.msix` (exit 0; one warning that `mspdbcmf.exe` is missing, so no symbols package) |
| `tools/windows/Test-Erosion.ps1 -Base 041919c` | No test erosion signals |
| C2 Document validation | PASS |
| C6 Diff check | PASS |

The bar end-state smoke first compared captures byte for byte and failed. Between two
renderings, the anti-aliased right edge of the "5h" label over the first cell differed on 19
edge pixels, by up to 12 colour steps, while every part boundary matched exactly. The smoke
now allows 24 per colour channel. A part edge even a fraction of a pixel off differs by far
more: the green fill against the dark hatch is about 140 apart.

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | C5 `BarSweepTests`: `FromEmptyThePartsGrowOneAfterAnotherAtOneSpeed`, `EachPartGrowsFromItsLeftEdgeAndNoEdgeJumps`, `OnlyTheEndOfTheLastMoveEasesOut`, `AValueChangeMovesOnlyTheEdgesThatChange`, `ADecreaseMovesLeftToRightAsWell`, `AnEdgeThatPassesTheNextOnePushesIt`, `ANewPartStartsEmptyAtTheOldEndOfTheBar` |
| AC-02 | PASS | C5 `StripEdgesFollowTheCellAndPartWeightsWithGapsBetweenCells` and `PeriodBarEdgesFillTheTrackBetweenSegments`. The period bar's fill ends on its last painted edge |
| AC-03 | PASS | `UsageBarsFillToTheirLayout`: after Left and then Used, the Claude card's today cells and 7d bar match the opening fill, and the tray miniature matches between two openings (`bars-open.png`, `bars-used.png`). `TrayBarsHoldStillWhenTheRowsChange`: a tray rebuild leaves an unchanged bar still |
| AC-04 | PASS | C5 `TheDraggedCardLandsAmongTheOthersWithItsHistoryAfterIt`, `TheOtherCardsSlideByTheDraggedCardsHeightAndGap` |
| AC-05 | PASS | `DraggingTheGripReordersAccounts`: the open rename is saved, the drop reorders and Claude Pro settles below Codex, Esc returns the card to its exact top, and a release after Esc changes nothing. `drag.png` shows the lifted card and the open gap |
| AC-06 | PASS | Code review: `LedgerMotion.Small`, `Panel`, `Drag` and `Ease` (one curve shared with `BarSweep.Ease`). `FadeIn`, `SlideHeight`, `Tween` and `BarFill` skip motion when `LedgerTheme.AnimationsEnabled` is false. The independent reviewer confirmed this |
| AC-07 | PASS | Diff: note lines `StackPanel Spacing="6"`, tray empty text `Margin = RowPadding`, `TitleControls` `Spacing="10"`, Caps `Spacing="6"`. Window-size smoke PASS |
| AC-08 | BLOCKED (C9 only) | C4, C5, C7 and C8 PASS. The UI smoke suite passes except one pre-existing failure that also fails at the base commit. C9 BLOCKED by the host's MSBuild (above), with a supplementary unsigned MSIX build through `dotnet msbuild`; see Merge for C9 |

## Not run

| Item | Reason |
| --- | --- |
| Smoothness of the fill, lift, gap, settle, slide and fades with live accounts | Post-deploy owner check (R-190), listed in the backlog's "Pending owner checks" |
| C9 with `Build-Package.ps1` | BLOCKED: the host's Visual Studio MSBuild lacks the .NET SDK resolver; repairing it is a host install |
| Live-provider checks | Not required: no provider, authentication or persistence change |

## Review

- Local `/code-review` (medium) on `041919c..5472b35` found nine items. Accepted and fixed in
  `2253c3c`:
  - the period bar's ease falling on the invisible trailing track;
  - fills not stopped on unload or zero width;
  - per-frame strip measuring;
  - slides for cards that do not move;
  - the duplicated curve.

  Not changed:
  - `DropAsync` has no failure path, but base has the same code;
  - fills in a hidden window run for up to 450 ms each and are replaced on show; this only
    costs a little work;
  - `BarFill` and `Tween` both keep stop bookkeeping, a design choice.
- An independent fresh-context reviewer (aiu-reviewer) reviewed `041919c..5472b35` against
  AC-01 to AC-08. Verdict: changes needed. It found one Important issue: the tray miniature
  refilled every bar from empty on any data change. It also found three Minors:
  - the first tray opening snapped to the end state;
  - the curve was duplicated;
  - the shadow kept its old size.

  It found no T3 area in the diff and no leak in the drag state. The Minors were fixed in
  `2253c3c`, and the re-check confirmed them resolved. The Important issue was not resolved
  in `2253c3c`, because a second rebuild in the same burst dropped the saved edges. It was
  fixed in `d863a2b` with a red-then-green smoke, together with the re-check's new Minor (a
  running fill now ends when the strip loses its width).
- Second re-check of `2253c3c..d863a2b`, by reading the code: approve. The tray refill and the
  new Minor are resolved, the R-02 text matches the code, and the fix lines add no new
  Important defect. No findings remain open.

## Merge

Merged tree: the branch's commits on `main` at `041919c` (fast-forward, `main` had not moved)
plus the numbering commit `df0e3a2` (T-070, R-224). Results on that tree, 2026-10-12:

| Check | Result |
| --- | --- |
| C3 Final document validation | PASS (`{"valid":true}`) |
| C2 Document validation | PASS |
| C6 Diff check | PASS |
| C4 Infrastructure suite | PASS, 943/943 |
| C5 Presentation suite | PASS, 359/359 |
| C7 App build | PASS, 0 warnings, 0 errors |
| C8 Launch smoke | PASS 3/3 under the desktop lock, after an offline `win-x64` restore |
| Card editing smokes (`-class CardEditingSmoke`) | First FAIL 4/5: `TrayBarsHoldStillWhenTheRowsChange` threw a UIA `COMException` (0x80040201) from the second tray-row lookup, which was outside the guard the first lookup already had for a row replaced by the rebuild. The lookup moved inside that guard (test only); then PASS 5/5 |
| `tools/windows/Test-Erosion.ps1` | No test erosion signals |
| C9 Package build | BLOCKED locally (host MSBuild, above) |

Local `/code-review` (high) on `origin/main..df0e3a2`: no Critical or Important findings,
six Minor. Corrected: AC-08 had been marked PASS while C9 was BLOCKED. Found wrong: the 14 px
gap after the Used/Left toggle is R-07's stated choice. Left as they are, recorded here:

- the fill matches old and new edges by index, so when a cell's part count or the strip's
  cell count changes, a part can start from another part's place
  (`LimitBars.cs` `BarFill.Show`);
- the tray keeps each account's last edges with no pruning, so a strip that disappears and
  returns while the miniature is open goes on from old edges (`LedgerTrayWindow.cs`);
- fills in a hidden window run their frames (already noted above);
- `LedgerMotion.Ease` borrows `BarSweep.Ease` while storyboards use `QuadraticEase`, two
  spellings of the one curve.
