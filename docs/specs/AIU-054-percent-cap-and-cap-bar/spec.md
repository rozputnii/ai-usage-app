---
id: AIU-054
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner direction, 2026-10-08, in conversation. The owner asked for an optional personal cap in percent on percent-only subscription windows (for example 90 % of the weekly limit, to keep a reserve), and for every applied cap to be the full width of its period bar, as if the provider's limit were the cap. The owner chose variant A of three and accepted the stated assumptions. Recorded as D-202.
---

# Percent caps and the cap as the full bar

## Problem

A percent subscription window (Claude and Codex 7d, Antigravity weekly groups, Claude
model windows) cannot take a personal cap: the Core rejects it (AIU-035 acceptance 3,
"a percentage window never takes a cap") and the projection offers no cap target. The
owner wants to keep part of a weekly limit in reserve, for example by budgeting
against 90 % of it.

Where a cap exists, the period bar spans the provider limit and the part above the cap
is hatched with a tick at the cap. A cap of 500 against a provider limit of 2000 fills
only a quarter of the bar. The owner wants the bar to look as if the provider limit
were the cap (checked in `CardVisuals.Build` and `BarMax` on 2026-10-08).

## Requirements

- **R-01 Percent cap.** A percent window whose period is at least one day (weekly or
  monthly; never a five-hour window) accepts an optional personal cap in whole
  percent of the provider's window, at least 0 and at most 100. It is stored in the
  existing budget configuration as a count of unit `percent`; the store format does
  not change. A stored percent cap on any other limit is rejected as a unit mismatch.
- **R-02 Budget against the cap.** With a cap below 100 % the cap is the effective
  limit: today's share, the today strip, the state words and colours (`cap close`,
  `cap reached`, `over cap`) and "≈ N × 5h left" are calculated against the cap. A cap
  of 100 % changes nothing. "Used up" still means the provider's 100 %, and rush stays
  off while a cap is set, as for other caps. Five-hour window cells, their share
  estimate and their states do not change.
- **R-03 Cap as the full bar, for every cap.** When a cap applies, the full width of
  the period bar is the cap, for percent windows and for count and money pools alike.
  There is no hatched part above the cap. While use exceeds the cap the bar is scaled
  to the use, a tick marks the cap and the part past it is drawn as over. Five-hour
  dividers on a percent bar are placed on the cap's scale. A cap kept above the
  provider limit (`AboveLimit`) or not applied leaves the bar on the provider limit.
- **R-04 Figures stay in provider units.** A percent cap is shown in provider percent:
  the footer reads "63 % of 90 % cap" in Used mode and "27 % left to cap" in Left mode;
  its tooltip gives "Custom cap 90 % · binds" and "Provider limit 100 % · 37 % left".
  The bar tooltip reads "63 % of 90 % used". Count and money pools keep their existing
  cap wording.
- **R-05 Editing.** The cap is set like other caps: the limit settings popover (D-199)
  shows its cap row with the unit "%" and the hint "per week · at most 100 %" (with the
  window's own period), the inline cap editor accepts whole numbers up to 100, and
  Settings › Caps lists the cap in percent. Removing it works as for other caps.
- **R-06 Demo.** The demo source accepts a cap on its weekly percent windows, so the
  popover and the bar can be checked without a provider.

## Out of scope

Caps on five-hour windows; figures rescaled to percent of the cap; changes to provider
facts, the reading series or the budget-store format; new logging events.

## Acceptance criteria

- AC-01: Core: a 7d percent window with a 90 % cap resolves to an effective limit of
  90 bound by the personal cap; a 100 % cap resolves to the provider; a cap in another
  unit is rejected. With used 63 the budget is calculated against 90; used 90 is
  `cap reached`, used 95 is `over cap`, and the provider is used up only at 100.
- AC-02: The five-hour count left in the week is `(cap − used) / window cost` with a cap
  and `(100 − used) / window cost` without one.
- AC-03: The live projection gives weekly and monthly percent windows a cap target and
  five-hour windows none. Setting 90 stores a `percent` count of 90; 101, a fraction,
  and a cap on a five-hour window are rejected. Settings › Caps shows it in percent.
- AC-04: Bars: a money pool with cap 300, provider 500 and use 218 spans 300 with no
  hatched segment and no tick; with use 320 the bar spans 320 with the cap tick at
  93.75 %; a percent window with a 90 % cap and use 63 fills 70 % and its five-hour
  dividers sit on the cap's scale; footer and tooltip text follow R-04.
- AC-05: The popover of a weekly percent window shows the cap row in "%", refuses
  101 and saves 90 in the demo app; the bar then spans the cap.
- AC-06: Core/Infrastructure and Presentation suites pass, the Release build has no
  warnings, and the Ledger desktop smoke passes. The owner's live check in the updated
  installed app is NOT_RUN until after deployment (D-190).
