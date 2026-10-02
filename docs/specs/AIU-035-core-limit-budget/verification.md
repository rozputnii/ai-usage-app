# AIU-035 verification

Implementation started 2026-10-02 from 73553fb. No product or live claims yet.

| Criteria | Verdict | Evidence |
| --- | --- | --- |
| AC-01 through AC-07 | NOT_RUN | Implementation in progress. |

The current owner request authorizes execution. The specification derives its behavior
from accepted AIU-034 research and D-187; it has not received a separate owner review.

## Development checks - 2026-10-02

- PASS: 3 LimitModelTests after the initial missing-contract build failure. Covers exact
  money scaling, overflow, currency mismatch, cap selection, zero and unlimited.
- PASS: 23 BudgetEngineTests covering the research E cases, period resolution, DST,
  day-off and rush, corrections and missing readings.
- PASS: 2 BudgetScenarioTests, including all design-brief figures and bars and the four
  account states. A failing mixed-money-exponent test exposed an incorrect scale carrier;
  it was fixed at QuantityMath.TryAlign and the combined 28 tests passed.
- One test invocation used an unsupported wildcard filter and failed before execution;
  the corrected explicit class filters ran the 28 tests above.
- These are targeted development checks; full required suites are still NOT_RUN.

- PASS: ReadingBudgetTests (8) and SessionEstimateTests (7), covering P01-P11, S01-S09,
  tracked consumption, pool/source isolation, age/sample limits and extra-spend evidence.
- PASS: Infrastructure Release regression suite, 376 tests, zero failures/skips, on local
  Windows with the pinned .NET SDK, 2026-10-02. Command from README.md, no restore.
- The first document validation failed because tasks.md omitted schema_version; added
  schema_version: 1. Revalidation is pending. A CA1720 identifier diagnostic was corrected
  before the estimator tests ran; owned-code warning policy was not suppressed.
- Additional failing boundary tests led to rejecting negative entitlements and to requiring
  provider-used evidence for the provider-used-up flag on tracked pools. Their targeted rerun
  passed (29 tests). Unknown reset meaning preserves period-unknown rather than assuming a reset.

- PASS: Presentation Release regression suite, 176 tests, zero failures/skips, 2026-10-02.
- PASS: document validator after metadata correction (`valid: true`, zero diagnostics).
- PASS: four additional E01/E05 money-cap cases, including unchanged day-start and independence
  from cap-change time, plus scenario session counts (BudgetScenarioTests: 6 tests).
- PASS: Work today keeps numerical outputs unchanged and affects coloring states only. The
  new assertion failed against the initial implementation; a separate DayOffLeftToday
  projection fixed it. BudgetEngineTests reran successfully, 25 tests. E11c now explicitly
  retains the provider's reported zero beside the winning unlimited state.
- Primary dependency inspection: new Core code has no file, transport, credential, UI,
  process-clock or local-timezone dependency; no project dependencies changed. All arithmetic
  uses decimal or checked integer/tick operations. SDK selection: 10.0.401/latestPatch.
- Independent read-only review requested from GPT-5.6 Luna, reasoning max, for the frozen
  73553fb..e2970ea range. Result pending; subsequent Work today fix communicated separately.
