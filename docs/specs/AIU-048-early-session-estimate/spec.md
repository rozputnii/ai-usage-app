---
id: AIU-048
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner direction, 2026-10-06, in conversation. The owner asked to show the five-hour window at once and to show a rough window count as early as possible, refining it automatically. The owner chose the interval estimator (approach B), the one-window state and a range for rough estimates (option a). Recorded as D-189. The owner approved this written specification, the design and the plan on 2026-10-06, as recorded in the verification execution ledger.
---

# Early five-hour session estimate

## Problem

A Claude, Codex or Antigravity subscription with a five-hour window paired with a
weekly window shows one weekly card. Its today strip is split into five-hour cells
only after the session estimator is ready (AIU-034 research section 7, AIU-035).
That rule requires three accepted five-hour instances, each moving at least 10
points, a median absolute deviation of at most 25 % and at least 5 points of total
weekly movement. Until then the card shows a single daily-allowance strip, and the
current five-hour window appears only in a tooltip.

Live Claude readings on 2026-10-06 show why this takes long. The provider returns
whole percentages: `five_hour.utilization` 16.0, 17.0 and `limits[].percent` 16, 17;
the weekly value stayed at 2 while the five-hour value moved 16 to 19. A weekly
change of 2 to 5 points per window carries up to ±1 point of rounding error, so
the median-of-ratios rule needs many windows and its precision does not improve
with more data.

## Intended result

1. A paired card shows the current five-hour window at once, as one cell, before
   any window count exists.
2. A rough window count appears as soon as the readings bound it within a factor
   of two, shown as a range, and narrows automatically as readings accumulate.
3. The estimator computes guaranteed bounds from integer-rounded readings. Every
   pair of readings in a window contributes, and weekly ticks make the weekly
   movement between them nearly exact. The bounds tighten as windows accumulate.
   The minimum data for any estimate is two weekly ticks inside one five-hour
   window.

The algorithm and its bounds are in [design.md](design.md).

## Requirements

- **R-01 One-window state.** When a five-hour window is paired with a weekly window,
  the current window is known (`CurrentWindowUsed` present, reset not passed) and
  there is no estimate, the today strip is exactly one cell for the current
  five-hour window. Its filled part is the window's used percent (or, in Left mode,
  its remainder), the rest is a neutral unfilled track, not the allowed or grey
  parts. A window that has not started shows the existing "Next 5h window · starts
  on first use" cell. The cell tooltip reads "Current 5h window · until HH:MM",
  "N % used" (or "N % left") and "Window count: collecting data". The footer has
  no window count. Today's allowance stays on the 7d bar as now. The tray miniature
  shows the same cell. Day-off, full-window and Left-mode rules of D-186 and D-187
  apply unchanged.
- **R-02 Estimate levels.** The estimator returns a point estimate `C` (weekly
  percent per full five-hour window) and bounds `[L, H]`. The level is:
  none when `L = 0`, `H` is unbounded or there is no data; rough when `H <= 2 L`;
  settled when `H <= 1.25 L`. Bounds that exist but are wider than rough display
  as none, the one-window state.
- **R-03 Rough display.** The today strip is split into five-hour cells by `C`, as
  the ready estimate is today. The footer shows " · ≈ a–b × 5h left" where
  `a = floor((100 - w) / H)` and `b = floor((100 - w) / L)`; when `a = b` it shows
  " · ≈ a × 5h left". The footer tooltip reads "One 5h window ≈ C % of 7d (L–H %) ·
  rough · from n windows".
- **R-04 Settled display.** As R-03, but the footer shows " · ≈ k × 5h left" with
  `k = floor((100 - w) / C)` and the tooltip omits "rough".
- **R-05 Estimator rules.** As specified in design.md:
  - Every pair of readings within one five-hour instance part gives guaranteed
    bounds for integer rounding by flooring or by rounding.
  - Weekly ticks narrow the bounds through an allowance refined by the current
    upper bound.
  - Bounds are intersected across parts, newest first. The pool stops at the
    first older part whose bounds do not intersect it.
  - Plan change, 28-day age, source, weekly instance, pool identity and readings
    at 100 are handled as today.
- **R-06 Truth rules kept.** Window figures are always marked "≈" and never change
  quota facts, card states other than the existing five-hour full state, or
  notifications (D-122). An unknown count is not shown as 0. A used-up weekly limit
  still shows no today strip and no 5h cells (D-187).
- **R-07 Records.** D-189 records the owner decision. Short notes mark the
  superseded parts of AIU-034 research section 7 and design-brief "estimate not
  ready", AIU-035's thresholds and AIU-038's `FiveHourModel` contract and "Without
  an estimate there is one strip".

## Out of scope

- New provider requests, transport, persistence format or retention.
- A prior for `C` from a plan name; no such evidence exists.
- New logging events: the estimate is a pure calculation over stored readings.
- Codex or Antigravity specific behavior beyond the shared estimator; their pairing
  rules are unchanged.

## Acceptance criteria

- AC-01: A paired card with no estimate renders exactly one today cell for the
  current window with the used percent filled, a neutral rest, the R-01 tooltip
  lines, and a footer without a window count; Left mode shows the remainder; a
  not-started window shows the "starts on first use" cell; the tray uses the same
  cell.
- AC-02: For simulated usage with a constant true `C`, polling every 5 to 6
  minutes and integer readings by flooring and by rounding, the bounds always
  contain the true `C` across randomized runs.
- AC-03: A part with fewer than two weekly ticks yields no estimate. For example,
  weekly movement 1 with five-hour movement 17 has `L = 0`. A small constructed part
  with two ticks yields `L` and `H` equal to values computed by hand, step by step,
  in the test, including the refinement pass. A simulated single window with
  `C = 10` and two points per poll reaches rough by 25 five-hour points.
- AC-04: Adding consistent parts never widens `[L, H]`, and the simulated
  sequences of design.md reach settled.
- AC-05: An older instance whose bounds do not intersect the pooled bounds, and
  all older ones, are excluded; a plan change discards earlier readings; parts with
  a different weekly instance or source are separate; readings older than 28 days
  and readings at 100 are excluded; mismatched pairs return no estimate.
- AC-06: Rough and settled footers and tooltips match R-03 and R-04, including
  the `a = b` case; the strip uses `C`; a used-up weekly limit shows no strip.
- AC-07: Infrastructure and Presentation suites, document validation and diff
  check pass; a local unpackaged run shows the one-window state on live readings,
  and demo scenarios show rough and settled cards.

## Verification

Evidence goes to `verification.md` in this folder. Interactive results not
observed are recorded as NOT_RUN.
