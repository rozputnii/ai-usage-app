---
id: T-051
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner direction, 2026-10-07, in conversation. The owner asked for cards shown only vertically, a settings panel that slides in from the right and narrows the cards, a clearer edge for the panel and as little text as possible in it. The owner chose full-width cards, the maximal reduction (rare actions in one ⋯ menu) and design B of three (a floating sheet with tiny labels), then approved the short design. Recorded as R-193.
---

# Single-column cards and a minimal sliding settings sheet

## Problem

The main window laid cards out in two columns, one while settings was open. The
settings panel appeared beside the cards with a background almost equal to the page,
so its edge was hard to see, and it carried eight sections with long explanatory
paragraphs and controls duplicated elsewhere (Used/Left in the title bar, Show
signed-out accounts in the + menu).

## Requirements

- **R-01 One column.** Account cards always stack in one full-width column, whatever
  the window width and whether settings is open. Inline history follows its card.
- **R-02 Sliding sheet.** Ctrl+, or the gear slides a 320 px settings sheet in from the
  right edge over about 200 ms; the cards narrow as it opens and widen as it closes.
  Without animations it appears and disappears at once. Esc still closes it.
- **R-03 Clear edge.** The sheet is a rounded surface inset from the window edges, with
  a background lighter than both the page and the cards and a hairline outline.
- **R-04 Minimal content.** The sheet has small labels Work days, Caps (only when caps
  exist), View and Updates, a close button and no title, "Esc closes" line or
  explanatory paragraph; explanations and cap notes are tooltips, and a cap that is
  not applied shows a warning mark. Used/Left and Show signed-out accounts leave the
  sheet; they stay in the title bar and the + menu. The update status is shown only
  when it needs attention or action; otherwise it is the version's tooltip.
- **R-05 Footer.** The footer shows the refresh interval, or a local-data or sync
  problem in its place, and a ⋯ menu with Preview diagnostics, Open logs, Open data
  folder, Export recovery summary and Delete stored data. Delete stored data still
  confirms in place with a short warning, focus on Cancel, and returns focus to ⋯.
- **R-06 Names kept.** Automation names of moved and restyled controls stay as before,
  except that the support actions and Delete stored data are menu items.

## Acceptance criteria

- AC-01: With settings closed and open, the demo app shows every card in one
  full-width column; opening settings narrows the cards and closing restores them.
- AC-02: The sheet slides in from the right, ends at its full width after rapid
  repeated toggling, and its edge is visibly distinct from the page and the cards.
- AC-03: The sheet shows only the R-04 labels and controls, the ⋯ menu lists the R-05
  actions, and arming Delete stored data shows the warning with Delete and Cancel.
- AC-04: The settings view model reports no status while all accounts are synced, a
  sync failure otherwise, and an update status as notable only when it needs attention.
- AC-05: Presentation and Infrastructure suites, the local Ledger smokes, document
  validation and the diff check pass.
