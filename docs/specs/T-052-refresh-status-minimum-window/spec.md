---
id: T-052
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner direction, 2026-10-08, in conversation. The owner reported that the caption buttons covered the title-row controls in a narrow window and asked for the smallest minimum width that keeps them apart and a minimum height that always fits one limit. The owner chose one limit section for the height and the title row alone for the width (the settings sheet may still squeeze the cards). The owner then asked to replace the title-row clock with a refresh button that is also the data status, and chose design B of three (the button right after the window title). Recorded as R-195.
---

# Refresh status button and minimum window size

## Problem

The main window had no minimum size. Narrowing it let the caption buttons cover Used/Left,
+ and Settings, and a short window could hide every card. The title row also showed a date
and time clock that said nothing about the data, yet took about 130 px of the width.

## Requirements

- **R-01 Refresh status.** The clock leaves the title row. A refresh icon button stands right
  after the window title. It is quiet (45 % opacity) like the card icons and opaque while
  pointed at. Activating it refreshes like F5 and the tray Refresh item.
- **R-02 Red when stale.** The icon is red and opaque while any connected account's data was
  not refreshed in time: a stale failed sync, an expired sign-in or a provider error. A failed
  attempt whose reading is still fresh and a signed-out account do not count.
- **R-03 Times without dates.** The tooltip gives reading times as `HH:mm`, never dates:
  "Updated 08:35" when every connected account read at the same minute and none is stale,
  otherwise one line per connected account, such as "Claude Pro · 08:35" or
  "Copilot business · failed 08:30, showing 08:10". With no connected account it says
  "Refresh (F5)".
- **R-04 Minimum width.** The window cannot be narrower than the title row's natural width:
  its padding, which includes the caption buttons, its column spacing and every visible
  element. The minimum follows what the title row shows (Day off, Work today, the demo
  button) and grows a window that is already narrower when the title row widens.
- **R-05 Minimum height.** The window cannot be shorter than the title row, the sign-in strip
  when shown, the body padding and the first card's header and primary limit, without its
  other sections. Without a card the body keeps 160 px. Both minimums stay within the work
  area. The settings sheet does not change the minimum.

## Acceptance criteria

- AC-01: The refresh status projection returns red and per-account lines for stale, expired
  and provider-error accounts, one "Updated" line when times agree, separate lines when they
  differ, and "Refresh (F5)" without connected accounts.
- AC-02: In the demo app the refresh icon replaces the clock, is red in the brief scenario
  (a stale Antigravity sync) and neutral on the day off.
- AC-03: Squeezed to 200 × 120 px, the window stops at its minimum with Settings clear of the
  caption buttons and Refresh clear of Used, in the brief scenario, on the day off and with
  Work today on; turning Work today on grows a window narrower than the new minimum; the first
  card's header and primary limit stay visible.
- AC-04: Presentation suite, the window-size smoke, document validation and the diff check pass.
