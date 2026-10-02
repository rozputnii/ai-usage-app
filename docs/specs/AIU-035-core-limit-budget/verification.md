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
