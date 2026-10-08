---
id: AIU-NEW
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner direction, 2026-10-08, in conversation. The owner asked to enter today's money or credit use by hand when the app missed it (it was off or the account was just connected), and to show Copilot credits either natively or in US dollars at an editable rate, with the cap and its maximum converted both ways, as a per-limit setting rather than a global one. The owner chose design B of three (a settings popover on each limit) and accepted the stated assumptions. Recorded as D-NEW.
---

# Today's use by hand and Copilot credits in dollars

## Problem

A card's today figure is the period use now minus the use at the start of the local
day. When the app was off at midnight or the account was connected during the day,
the day starts at the first reading (for example 10:43), so everything used earlier
today is missing: the today strip, the day's share and the colour treat the day as
almost unused. Percent subscriptions are not affected by this change; there is no
source for a by-hand percentage.

Copilot reports the premium pool as `premium_interactions` and the app labels it
"requests". For the owner's Copilot Business account this is the AI-credit pool:
17,500 on 2026-10-08, which is $175.00 at GitHub's published price of $0.01 per AI
credit. The app shows only the count and has no conversion (checked in code on
2026-10-08). Older Pro and Pro+ plans bill premium requests at another price, so the
rate must be editable.

## Requirements

- **R-01 Limit settings popover.** Each card or section whose limit has at least one
  setting below shows a quiet sliders icon button in its header, before History, with
  the tooltip "Limit settings". It opens a popover anchored to the button; Esc or a
  click outside closes it. Each row applies on its own: Enter or Save saves a field,
  the unit switch applies at once. Percent windows show no icon. The existing footer
  click that opens the inline cap editor stays.
- **R-02 Units, Copilot premium pool only.** For the Copilot premium pool (family
  GH-P) the native unit is called "credits"; stored readings and caps keep the
  provider's unit. The popover offers "Show as credits | USD" and "1 credit = $ rate".
  The rate defaults to 0.01, must be above 0 and at most 1000, with at most 6
  decimals, and is kept while credits are shown. The scope label "Premium requests" is
  unchanged.
- **R-03 Dollar view.** With USD every figure of that limit is in US dollars: the
  card or section, the tray, inline history and every cap editor (card footer,
  popover and Settings › Caps). A dollar figure is the credit figure times the rate,
  rounded down to the cent. The pool's provider limit in dollars is the largest cap
  allowed (D-194).
- **R-04 Caps stay in credits.** A cap entered in dollars is stored as whole credits,
  the amount divided by the rate and rounded down. Switching units or changing the
  rate never rewrites a stored cap; its dollar value follows the rate (10,000 credits
  show as $100.00 at 0.01 and $400.00 at 0.04).
- **R-05 Today's use by hand.** A money or credit pool with a daily budget (its today
  strip is shown) has a "today" row in the popover. It opens with today's current
  figure; its hint gives the tracked figure and, when tracking began after midnight,
  its local start time. Enter or Save stores the value; it must be at least 0 and at
  most the period's use so far. Empty with Enter, or Reset, returns to the tracked
  figure. The value is entered in the limit's current unit; a dollar value becomes
  credits, rounded half away from zero and limited to the period's use.
- **R-06 Effect on today.** A stored value replaces today's day start with the period
  use at the moment of saving minus the entered amount, so later use adds to it.
  Today's share, the state, the colour, the pill and the footer follow from the
  corrected day start. It applies only on its local date and within the same provider
  period; after a reset later that day it is ignored. That day's bar in inline history
  uses the same day start.
- **R-07 Today's tooltip.** The today strip tooltip adds "Set by you" while a value
  applies, and "Tracked since HH:mm" when tracking began after midnight without one.
- **R-08 Storage.** Units, rates and today's values are window preferences per limit,
  kept with labels, order and hidden sections, and survive restart. Today's values
  older than 35 days are dropped when a today's value is next saved. An older build keeps
  the new fields through the preference file's extension data. Provider facts, reading
  series and the budget configuration format do not change, and nothing new is sent
  to a provider.

## Out of scope

Percent windows; dollars for any other pool; an automatic rate lookup; renaming the
"Premium requests" label; undo for today's value; changing what the provider reports.

## Acceptance criteria

- AC-01: For a Copilot premium pool with 3,240 of 17,500 used, the projection shows
  "credits" natively and, with USD at 0.01, $32.40 of $175.00 with a largest cap of
  $175.00; a 10,000-credit cap shows as $100.00, and as $400.00 at a rate of 0.04.
- AC-02: The live source stores a $50.00 cap entered in USD at 0.01 as 5,000 in the
  provider's unit, rejects a cap above the converted provider limit, converts inline
  history to dollars, and lists the cap in Settings › Caps in the card's unit.
- AC-03: With tracking that began at 10:43 and 3,240 used, entering 900 makes today's
  use 900 and a later reading of 3,300 makes it 960; a negative value or one above the
  period's use is rejected; empty resets to tracked; the value no longer applies on
  the next day or after a provider period restart; history's today bar uses it; a
  percent window rejects it.
- AC-04: A preference file without the new fields loads unchanged; an invalid rate,
  a duplicated today entry or an oversized list is rejected without being
  overwritten; today entries older than 35 days are dropped when a new one is saved.
- AC-05: In the demo work-budget scenario the Copilot Business card's popover switches
  units, edits the rate, today's use and the cap, and the card follows; the Claude
  spending popover shows today and cap rows only; percent windows show no icon.
- AC-06: Presentation and Infrastructure suites, document validation, the app build and
  the diff check pass, and the demo app shows AC-05. The owner's live Copilot Business
  and Claude spending checks in the updated installed app are post-deploy checks
  (D-190).
