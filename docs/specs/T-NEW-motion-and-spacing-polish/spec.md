---
id: T-NEW
type: feature
status: implementing
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-11, in the request. The owner asked to polish spacing and motion in the main window and the tray without breaking any behavior, and chose every visible option in the request, so no new variants were rendered. Recorded as R-NEW.
---
# Motion and spacing polish

## Problem

The usage bars grew each part from its middle (an explicit width centred in its star
column), each part on its own, and only when a value changed. A dragged card showed a
static insertion line, and on a drop or a cancel it jumped into place. Fades and the
settings slide used 150, 200 and 250 ms. A few margins and gaps differed from the design
reference and from their siblings.

## Requirements

- **R-01 One-strip fill.** The today strip's cells (5h and today) and the 7d or month bar,
  in the window and in the tray miniature, fill as one strip:
  - every part grows from its left edge;
  - the parts' edges move one after another, left to right, each starting when the previous
    one ends, at one speed, so a move's time is proportional to its distance;
  - a full bar takes 450 ms, and only the end of the last move eases out (it starts at the
    same speed and slows to a stop);
  - an edge that another one passes is pushed ahead of it, and a part that did not exist
    before starts empty at the old end of the bar.
- **R-02 When the bars fill.**
  - From empty: when the window or the tray miniature opens, and when a bar first appears.
  - From the edges on screen to the new ones: when a value changes, also when it decreases
    (for example Used/Left).
  - A resize shows the end state at once. With system animations off
    (`LedgerTheme.AnimationsEnabled`) the bars show their end state at once.
- **R-03 Lift.** Dragging a card's grip lifts the card: it scales to 1.02 around its centre
  and casts a soft shadow in its squircle outline, in 200 ms. The dragged card no longer
  turns translucent.
- **R-04 Live gap.** While the card is dragged, the other cards slide in 200 ms to open the
  place where it would land, and back when the order would not change. This replaces the
  insertion line. The landing place is computed from the cards' own places, not from where
  the gap has slid them, so the drop rules are unchanged.
- **R-05 Settle.** On a drop the card moves from where it was released into its new place;
  on a cancel (Esc, lost pointer capture, a drop that changes nothing) it moves back; the
  slid cards return; each in 200 ms. The drop rules, Esc, pointer capture and saving the
  order are unchanged.
- **R-06 Motion constants.** `LedgerMotion` holds the durations and the one curve, a
  quadratic ease-out:
  - 150 ms for small elements (the sign-in strip and undo bar fade-ins);
  - 250 ms for panels (the settings sheet slide, the history fade-in);
  - 200 ms for each drag step (R-03 to R-05).

  The sign-in wave and the install dots are progress loops, not transitions, and keep
  their timing. Every transition honours `LedgerTheme.AnimationsEnabled`.
- **R-07 Spacing.** Only inconsistencies are fixed:
  - a money or note card's fact lines are 6 px apart, as the reference's `row-gap: 6px`;
    they touched;
  - the tray's empty text uses the rows' padding in the current density instead of
    14/12 px;
  - the title-row controls are 10 px apart, as the reference's title bar `gap: 10px`; the
    Used/Left toggle keeps its 4 px margin;
  - Settings › Caps has the 6 px label gap of the other sections instead of 4 px.

Owner picks 2026-10-11: the bars fill as one continuous strip from left to right (left-edge growth, parts in sequence at one speed, about 450 ms for a full bar, ease-out only at the end of the last part) on opening and on every value change, also on a decrease, honouring system animations; drag variant C (lift to about 1.02 with a soft shadow, a live gap instead of the insertion line, a smooth settle on drop and on cancel, about 200 ms each); other motion on two durations (about 150 ms small, 250 ms panels) and one curve in `LedgerMotion`; spacing fixes only where the window, cards, settings sheet and tray are inconsistent with the design reference or with each other.

## Acceptance criteria

- AC-01: The fill schedule moves the edges in sequence from left to right at one speed,
  450 ms for a full bar, eases out only at the end of the last move, moves only the edges
  that change, handles decreases and pushes, and starts a new part at the old end of the
  bar.
- AC-02: The edges follow the strip's cell and part weights with the gaps between cells,
  and the period bar's segments with the track between them.
- AC-03: The bars end at their layout: after two animated value changes (Left, then Used)
  the bars match the bars the opening fill drew, and the tray miniature's bars match
  between two openings.
- AC-04: The live gap puts the dragged card among the other cards with an open history
  panel after its card, and slides the others by the dragged card's height and the grid
  gap.
- AC-05: A drag still saves an open rename, a drop moves the account and the card settles
  in its new place, Esc puts the card back exactly where it was, and a release after Esc
  changes nothing.
- AC-06: Fades and the settings slide use the `LedgerMotion` durations and curve, and every
  transition is skipped without system animations.
- AC-07: The four spacing values of R-07 hold, and the title row still clears the caption
  buttons in a squeezed window.
- AC-08: No regression: both unit suites, the app and package builds, the launch smoke and
  the UI smoke suite pass.

## Out of scope

- Layout changes, new controls and the progress loops (R-06).
- Lower-confidence spacing candidates from the comparison, left as they are: the inline
  history's offset from its card and its padding in Compact, the card icons' 2 px gap, the
  settings footer's top padding and the recovery text margin.
- Smoothness itself, which the owner checks in the installed app (R-190).
