---
id: T-061
type: feature
status: implementing
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-10 (fix confirmed defects of the four classes found in the T-056/T-057/T-058 run)
---
# Redundant work on unchanged state

## Problem
- Current: a sweep on `17c79c7` measured that one source change with identical data raises three view-model
  property changes, each of which redraws the tray glyph (a new 128 px bitmap and HICON, about 1.2 ms, plus a
  shell call), two property changes per card, and five collection resets each in the tray miniature and the
  Settings cap list. The live source publishes at least twice a minute: every one-minute tick rebuilds once
  before and once after refreshing the due accounts, and the second rebuild re-reads every stored series file even
  when no account was due (four ticks out of five at the default interval). Code paths show the rest:
  - the tray glyph is drawn before the cards update, so its colour lags one source change behind;
  - the tray miniature rebuilds all its rows once per added row (N+1 rebuilds per change), even while hidden;
  - while history is open, each source change rebuilds the whole card grid and the chart;
  - the Settings cap list is recreated on each change, which discards a cap edit the user has open, typed text
    included, within about a minute (reproduced);
  - opening the window when it is already visible writes a `WindowShown` record.
- Expected: unchanged input does no native, file, log or visual-tree work, and the tray colour follows the cards
  of the same change.
- Unchanged: what the app shows, its colours, the refresh schedule and backoff, the stored formats, and the
  tray, history and settings behaviour for real changes.

## Requirements
- R-01: `LedgerViewModel` exposes the tray tone (Critical, Attention or Ok), set at the end of a rebuild after the
  cards are updated. `LedgerWindow.UpdateTrayGlyph` draws only when that tone differs from the last tone it drew
  successfully, so a failed redraw is still retried.
- R-02: A tick that found no account due does not rebuild a second time.
- R-03: A card raises one property change per update instead of two.
- R-04: The tray miniature does not rebuild while it is hidden; it rebuilds once before it is shown.
- R-05: While history stays open, a source change updates the history panel without rebuilding the card grid.
- R-06: The Settings cap list keeps its rows when the caps are equal (value equality of `CapSettingModel`);
  the kept rows still refresh their card-dependent state (`CanAct`).
- R-07: `WindowShown` is logged only when the window actually changes from hidden to shown.

## Acceptance criteria
- AC-01: Presentation tests show that an unchanged source change does not change the tray tone, and that when
  the cards turn critical the tone changes with the cards already updated (fails at base: the window draws the
  previous tone).
- AC-02: A Presentation test shows a tick with no account due raises `Changed` once and reads each series once
  (twice at base).
- AC-03: A Presentation test shows one card property change per update (two at base).
- AC-04: A Presentation test shows an open cap editor, with its typed text, survives an unchanged source change
  (discarded at base), and `CanAct` still follows card changes.
- AC-05: R-04, R-05 and R-07 are checked by review of the window code and by C8 (the launch smoke opens history,
  the tray miniature and settings).
- AC-06: C4, C5, C7 with 0 warnings, C6, C2 and C8 under the desktop lock pass.

## Out of scope
- `presenter.IsAlwaysOnTop` set on every change (not confirmed: WinUI may skip equal values).
- The per-refresh operation records and other logging volume (a different class).
- The appearance of the tray glyph, the miniature, the cards and the history chart.
