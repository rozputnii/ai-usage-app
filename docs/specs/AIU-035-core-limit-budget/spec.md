---
id: AIU-035
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Derived from the owner-selected AIU-035 and the current owner instruction on 2026-10-02 to execute it after design completion; behavior is the accepted AIU-034 research and D-187. This is not a claim of separate owner review of this specification.
---

# Core limit model and budget engine

## Scope

Implement credential-free normalized facts and pure calculations in AiUsage.Core.
The normative sources are [research sections 5-9](../AIU-034-limit-audit-design-brief/research.md),
[design brief section 4](../AIU-034-limit-audit-design-brief/design-brief.md), and
[D-187](../../decisions/accepted.md#d-187---aiu-034-tray-miniature-no-ok-pill-day-off-rush-and-on-extra-usage).
Provider parsing, storage, UI, source credentials and live integration are excluded.

## Contracts and behavior

- Add normalized types under Core/Usage and pure computations under Core/Budget.
  Preserve the existing QuotaSnapshot contracts and consumers until AIU-037/039.
  This additive transition avoids a premature provider or presentation migration.
- Facts distinguish unknown, explicit null, unlimited, not applicable and finite zero.
  Quantities retain decimal counts or signed Int64 money minor units, exponent and opaque
  currency. Arithmetic is checked, same-unit only; money aligns upwards without truncation.
  Unknown currency/exponent and overflow yield unknown, never a converted or zero amount.
- Separate provider facts, local caps and work weekdays, supplied observations, and derived
  results. Keys contain provider, family and opaque native discriminator. Snapshot source,
  version, timestamps, nullable flags and reset provenance remain independent facts.
- Resolve effective limits and period bounds using research 5.4 and 8.3. Calendar inputs
  explicitly provide now and TimeZoneInfo; no process clock, filesystem or timezone lookup.
- Budget calculations use decimal throughout, elapsed local-day weights including DST,
  fixed day-start usage and fixed period bounds supplied by the caller. The caller retains
  these until midnight, a new period or cap change. Reset jitter does not change an instance.
- Reading computations reconstruct day-start usage and tracked consumption from ordered,
  credential-free runs, retaining exact/carried/since and incomplete provenance. Period IDs
  isolate resets; plan and source remain explicit. Corrections never produce negative used today.
- Session estimation uses matching pool runs, one sample per five-hour instance, matching
  weekly instances and sources, plan-change invalidation, 28-day age and latest ten samples.
  Minimum three, positive median, MAD <= 25%, and total weekly movement >= 5 are required.

  > Superseded in part by [AIU-046](../AIU-046-early-session-estimate/spec.md) (D-188): interval bounds from every reading pair within a five-hour part, intersected across parts newest first, replace the samples and these thresholds.

- D-187 adds a separate would-be day-off share, provider used-up, last-work-day rush and
  count of whole five-hour intervals before reset, and Claude extra-spend detection.
  Work-today coloring is an explicit date-scoped input and never edits the calendar.
  Rush uses the day-start remainder (fixed daily share), is suppressed by used-up, any cap,
  monetary or credit pools, and requires a provider replenishment reset.
  Extra spend needs a known comparable spend observation when the currently full window
  first became observed full after a below-full observation in that instance. Missing
  transition evidence, a spend-period change or a counter decrease gives unknown, not zero;
  clearing/resetting the window ends it. The amount starts at the observed full transition,
  not at an invented instant between provider refreshes.
- Return numerical display projections, state precedence and bar positions, with no UI text
  or controls. Staleness remains an independent result marker and does not erase figures.

## Caller responsibilities

The AIU-036 store supplies valid, nonoverlapping runs for an account target and limit key.
Each run contains confirmed observations, not cached startup data. Period instance IDs are
assigned using Transition; StartedAt or After from a new instance is retained in its runs.
The same numeric rounding unit is supplied in the aligned native scale used for comparison.
DayStart derives the first observed baseline for a local date; the caller keeps it and the
period bounds for that day, recomputing bounds only at midnight, a new instance or cap change.
The effective timezone changes at the next local midnight, not while replaying an open day.
When replaying before a run's final confirmation, only its first observation is usable:
intermediate confirmation timestamps are not retained, so coverage is not extrapolated.
ProviderUsedUp requires Facts.Used or Facts.UsedPercent; the computational Used input alone
does not establish provider exhaustion. AllowsCalendarFallback is set only for the research
8.3 rule 4 families CL-X/CL-D and CX-B. It defaults to false and never applies to percent
windows. An expiry remains a fact and does not itself authorize a fallback period.
Track produces cumulative observation runs for assumed periods; its balance mode never
treats a first balance as consumption. Its estimate/incomplete provenance must remain visible.
SessionPair names explicit pool identity on both sides and source-established durations;
pairing is never guessed from a display label. A shared five-hour window and a model-only
weekly window have different pool identities. AIU-037/039 own these adapters.

BudgetResult numeric values use its Scale's native count unit or money exponent. Consumers
apply BudgetDisplay only at the display boundary. DayOffShare and DayOffLeftToday are the
stable preview figures; WorkToday affects the states only, not these figures or the calendar.

## Acceptance

- AC-01: Normalized quantities, cap selection and reset provenance preserve the distinctions
  above, reject incompatible arithmetic and keep percentage windows uncappable.
- AC-02: Research E01-E13 (including all subcases) reproduce the calendar weights, budget
  figures and states; rounding is applied only at display boundaries.
- AC-03: Research P01-P11 reconstruct day starts, period changes and corrections consistently
  from supplied readings; tracked consumption covers top-ups, gaps and calendar boundaries.
- AC-04: Research S01-S09 reproduce readiness, exclusions and session figures, including
  matching pools, plan/source changes, age, missing readings and exhausted sample endpoints.
- AC-05: Design brief 4.2 figures, 4.3 bar positions and 4.4 account states are deterministic tests.
- AC-06: D-187 day-off share, Work today expiry, used-up priority, rush eligibility and extra
  spend are tested, including missing and incompatible inputs.
- AC-07: Core retains no credential, transport, file or UI dependency. Infrastructure and
  presentation regressions, document validation and diff checks pass. No live/UI success is implied.
