# Motion and spacing polish - verification

Evidence for [the specification](spec.md). Host: Windows 11 Pro 10.0.26300, .NET SDK
10.0.401, Release builds, interactive desktop unlocked; smokes ran under the desktop lock.
Branch `run` from `main` at `041919c`. Times are local, 2026-10-12.

## Checks

| Check | Result |
| --- | --- |
| Red to green, `BarSweepTests` and `ReorderMathTests` | FAIL 11/14 against stubs (`NotImplementedException`), then PASS 14/14 |
| C5 Presentation suite | PASS, 359/359 (adds `BarSweepTests`, 9 cases, and 2 `ReorderMathTests` cases) |
| C4 Infrastructure suite | PASS, 943/943 |
| C7 App build | PASS, 0 warnings, 0 errors |
| C8 Launch smoke, under the desktop lock | First FAIL 2/3: after Ctrl+Q both demo cases exited with code 1; the critical log named an `IndexOutOfRangeException` in `TodayStrip.Apply` from a fill that still drew frames while its closing window had no width. Fixed in `7456678`; then PASS 3/3 on two consecutive runs, no critical log |
| Card editing smokes (`*CardEditingSmoke*`) | PASS 4/4, twice: rename, drag (now also checks the settle), tooltips, and the new `UsageBarsFillToTheirLayout` |
| `UsageBarsFillToTheirLayout` repeated | PASS 5/5 consecutive runs after the comparison tolerance below |
| Window-size smoke (`*SqueezedWindowKeepsTheTitleRowClearOfTheCaptionButtons*`) | PASS 1/1 with the wider title-row gaps |
| C9 Package build | PENDING |
| UI smoke suite (`-method "*"`) | PENDING |
| C2 Document validation | PENDING |
| C6 Diff check | PENDING |

The bar end-state smoke first compared captures byte for byte and failed: the anti-aliased
right edge of the "5h" label over the first cell differed by up to 12 colour steps on 19
edge pixels between two renderings, while every part boundary matched exactly. It now
allows 24 per colour channel; a part edge even a fraction of a pixel off differs by far
more (the green fill against the dark hatch is about 140 apart).

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | C5: `BarSweepTests` `FromEmptyThePartsGrowOneAfterAnotherAtOneSpeed`, `EachPartGrowsFromItsLeftEdgeAndNoEdgeJumps`, `OnlyTheEndOfTheLastMoveEasesOut`, `AValueChangeMovesOnlyTheEdgesThatChange`, `ADecreaseMovesLeftToRightAsWell`, `AnEdgeThatPassesTheNextOnePushesIt`, `ANewPartStartsEmptyAtTheOldEndOfTheBar` |
| AC-02 | PASS | C5: `StripEdgesFollowTheCellAndPartWeightsWithGapsBetweenCells`, `PeriodBarEdgesFillTheTrackBetweenSegments` |
| AC-03 | PASS | `UsageBarsFillToTheirLayout`: the Claude card's today cells and 7d bar after Left then Used match the opening fill, and the tray miniature matches between two openings; `bars-open.png`, `bars-used.png` |
| AC-04 | PASS | C5: `TheDraggedCardLandsAmongTheOthersWithItsHistoryAfterIt`, `TheOtherCardsSlideByTheDraggedCardsHeightAndGap` |
| AC-05 | PASS | `DraggingTheGripReordersAccounts`: rename saved, drop reorders and Claude Pro settles below Codex, Esc returns it to its exact top, a release after Esc changes nothing; `drag.png` shows the lifted card and the open gap |
| AC-06 | PASS | Code review: `LedgerMotion.Small`, `Panel`, `Drag` and `Ease`; `FadeIn`, `SlideHeight`, `Tween` and `BarFill` skip motion when `LedgerTheme.AnimationsEnabled` is false |
| AC-07 | PASS | Diff: note lines `StackPanel Spacing="6"`, tray empty text `Margin = RowPadding`, `TitleControls` `Spacing="10"`, Caps `Spacing="6"`; window-size smoke PASS |
| AC-08 | NOT_RUN | C4, C5, C7, C8 PASS; C9 and the UI smoke suite pending |

## Not run

| Item | Reason |
| --- | --- |
| Smoothness of the fill, lift, gap, settle, slide and fades with live accounts | Post-deploy owner check (R-190), in the backlog's "Pending owner checks" |
| Live-provider checks | Not required: no provider, authentication or persistence change |

## Review

PENDING.
