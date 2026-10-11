# Real task S4c: UI/UX spacing and motion

Paired run (arms A and E) from current `main`. Owner answers 2026-10-11: Q4 yes (bars animate
on open and on change), Q5 yes (spacing: consistency only, no redesign), Q6 variant C (lift,
live gap, settle), Q7 yes (unify other motion). The owner's picks count as the approved
variants (AGENTS.md: never re-confirm a picked variant), so the run renders no new variants.

Operator notes: the fill grows from the middle because `LimitBars.cs` `BuildCell` puts each
fill in a star-sized column with the default stretch alignment and `LedgerMotion.WidthOnLoad`
animates `Width`; `PeriodBar` animates all segments at once.

## Brief {#brief}

> Owner request, 2026-10-11: polish spacing and motion in the main window and the tray, without
> breaking any behavior. The owner already chose every visible option below; record them as
> one line in the spec ("Owner picks 2026-10-11: ...") and render no new variants.
>
> 1. Usage bars (the 5h cells in `Controls/Ledger/LimitBars.cs` and the 7d or month bar,
>    `PeriodBar`). The fill must read as one continuous strip moving left to right:
>    - every segment grows from its left edge, never from its middle;
>    - segments animate in order from left to right, each starting when the previous one ends,
>      at one constant speed (a segment's duration is proportional to its width), about 450 ms
>      for a full bar, with ease-out only at the end of the last segment, so the used part and
>      the parts after it look like one strip with no jumps;
>    - animate both when the window or tray opens (from empty) and when a value changes (from
>      the old widths to the new ones, also when a value decreases);
>    - keep honoring `LedgerTheme.AnimationsEnabled` (system animations off means no motion).
> 2. Spacing: compare margins, paddings and gaps in the window, cards, settings sheet and tray
>    with the design reference (`docs/specs/T-034-limit-audit-design-brief/design-reference/`)
>    and with each other; fix only inconsistencies. No layout redesign, no new controls.
> 3. Drag to reorder (R-205), owner variant C:
>    - lift: the dragged card scales to about 1.02 and gets a soft shadow;
>    - live gap: the other cards slide to open the place where the card would land, replacing
>      the insertion line;
>    - settle: on drop the card moves smoothly into its new place; on cancel (Esc, lost
>      capture, a drop that changes nothing) it moves smoothly back;
>    - about 200 ms each, honoring `AnimationsEnabled`; drop rules, Esc, capture handling and
>      order persistence stay exactly as they are.
> 4. Other motion (settings sheet slide, fade-ins): unify to two durations (about 150 ms for
>    small elements and 250 ms for panels) and one easing curve, in `LedgerMotion`.
>
> Verification: unit tests for the pure motion math (the segment schedule, the drag offsets)
> red to green; UI smokes for end states (final bar widths, the order after a drop and after a
> cancel) under the desktop lock; C4, C5, C7, C8, C9, C2, C6. Smoothness itself is an owner
> check after deploy: add it to "Pending owner checks". Tier T2 (Windows UI).
