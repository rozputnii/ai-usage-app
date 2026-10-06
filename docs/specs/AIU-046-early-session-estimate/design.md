---
id: AIU-046
type: design
status: draft
goal: G-003
scope_version: 1
---

# AIU-046 design: interval session estimator

## Quantity and model

`C` is the weekly percent consumed by one full five-hour window. Within one
five-hour instance and one weekly instance, the true weekly movement equals
`C / 100` times the true five-hour movement. The current estimator already
assumes this.

Providers report integers. A reported value `k` means the true value lies in
`[θ_k, θ_k + 1)`, where `θ_k = k` when the provider floors and `θ_k = k - 0.5` when
it rounds. The design does not need to know which, because only differences of
thresholds are used.

## Readings and parts

The stored series are runs `[FirstSeen, LastConfirmed]` with a constant value.
For a pair, a reading time is any `FirstSeen` or `LastConfirmed` of either series
at which both series have a covering run (`SessionEstimator.Cover`). Each reading
is `(t, s, w)` with integer five-hour value `s` and weekly value `w`. Both series
of a pair are recorded from the same snapshot, so their times coincide.

Readings are grouped into **parts** by five-hour instance, weekly instance, source
and plan. A weekly reset or source change inside a five-hour window therefore
splits it. Readings with `s` or `w` at 100, missing or out-of-range values,
readings before the last plan change and parts whose last reading is older than 28
days are excluded. The current, unfinished instance is included.

## Bound from one pair of readings

Write the true weekly value at reading `j` as `θ_{w_j} + e_j` with excess
`e_j ∈ [0, u_j)`:

- `u_j = 1` in general.
- When `r_j` is a **tick** (`j ≥ 1` and `w_j > w_{j-1}` in the same part), the
  true value at `r_{j-1}` was below `θ_{w_j}`, so the excess is less than the
  weekly movement between the two readings:
  `u_j = min(1, H (s_j - s_{j-1} + 1) / 100)`, where `H` is a valid upper bound
  for `C` (initially unbounded, which gives `u_j = 1`).

For readings `i < k` in one part, with `S = s_k - s_i ≥ 2` and `W = w_k - w_i`, the
true weekly movement lies in `(W - u_i, W + u_k)` and the true five-hour movement
in `(S - 1, S + 1)`. Therefore:

```
C ≥ 100 max(0, W - u_i) / (S + 1)
C ≤ 100 (W + u_k) / (S - 1)
```

Each bound is a strict worst case and needs no distribution assumption.

## Combining bounds

`L` is the largest lower bound and `H` the smallest upper bound over all reading
pairs of all pooled parts. Because the readings come from one constant `C`, every
pair's bounds contain it, so `L ≤ C ≤ H` stays guaranteed while the intersection
tightens with every window. Ticks make the weekly movement between them nearly
exact.

The tick allowance `u_j` depends on `H`, and a smaller `H` gives smaller
allowances. The estimator therefore repeats the pass with the new `H` until it
stops changing, at most four passes. Every pass uses a valid `H`, so the result
stays valid.

**Pooling and adaptation.** Parts are ordered by their last reading, newest first.
Each part's own bounds are intersected into the pool in that order. At the first
part whose bounds do not intersect the pool, that part and all older parts are
excluded. A change in the provider's weighting or a model violation therefore
removes stale readings without waiting 28 days. If the newest part's own pairs
conflict, there is no estimate.

**Point estimate.** `C = 100 Σ(w_last - w_first) / Σ(s_last - s_first)` over the
pooled parts, clamped into `[L, H]`.

**Cost.** A five-hour part has about 60 readings at the five-minute polling
cadence, so about 1,800 pairs. 28 days hold at most about 135 parts, so a full
pass is under a million simple operations.

## Levels and minimum data

The levels are:
- **none:** `L = 0`, `H` unbounded or no part;
- **rough:** `H ≤ 2 L`;
- **settled:** `H ≤ 1.25 L`.

`L > 0` needs a pair whose start reading has `u_i < 1` and whose weekly movement
reaches the next whole point. That is the **minimum data for any estimate**: the
weekly value ticks twice within one five-hour part while the app is polling, so
the weekly value moves at least 2 points inside that window. Until then the
one-window state is shown.

The following was simulated with constant `C`, steady usage of `step` five-hour
points per five-minute poll and flooring. The table shows the five-hour use within
the first window at which each level is reached.

| `C` | step 1 | step 2 | step 4 |
|---|---|---|---|
| 5 | rough and settled 35 | rough 33, settled 51 | rough 33, settled 69 |
| 10 | rough 17, settled 28 | rough 17, settled 37 | rough 25, settled 45 |
| 20 | rough 9, settled 24 | rough 13, settled 27 | rough 17, settled 25 |

**Further windows.** Additional windows of 20–60 points narrow `C = 10` to about
9.5–10.4 and `C = 20` to about 19.6–20.5 within ten windows. A slow pool
(`C = 3`) needs more than one window, because each window moves the weekly value
by about one point.

**Correctness.** In 20,000 randomized runs, the bounds never excluded the true `C`.
The runs used `C` from 1 to 40, one to eleven windows, random cadence and both
rounding modes.

## Output to the card

`SessionEstimator.Estimate` returns `C`, `L`, `H`, the level and the number of
pooled parts. The projection maps them to `FiveHourModel`:
- the window share and its bounds;
- the level;
- the count of parts;
- the weekly remainder as a range from `floor((100 - w) / H)` to
  `floor((100 - w) / L)`, collapsed to `floor((100 - w) / C)` when settled.

The today cells keep using `C` through the existing share calculation.
`FiveHourModel` belongs to the AIU-038 contract; a note there points to this
specification.

## Alternatives rejected

- **Lower the current thresholds.** The median of ratios keeps ±1 weekly rounding
  per window, so it does not converge.
- **One segment per window, worst-case errors summed across windows.** Valid, but
  simulation showed it plateaus at about ±30 %.
- **A starting `C` from the plan name.** There is no provider evidence for one.
- **Statistical bounds.** They would narrow faster but could exclude the true
  value. Guaranteed bounds keep "≈ a–b" honest.
