# T-035 verification

Completed 2026-10-02. Base 73553fb; final code b9e603b. Platform-neutral Core computations
only; no UI, storage, transport or live-provider behavior was switched.

| Criteria | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | LimitModelTests: quantity scaling, overflow, incompatible units/currencies, limit states and cap selection; E11c reported-zero/unlimited precedence. |
| AC-02 | PASS | BudgetEngineTests E01-E13 and all subcases, plus BudgetScenarioTests E01/E05 money-cap integration. Decimal arithmetic, DST and partial-day weights, states and rounding. |
| AC-03 | PASS | ReadingBudgetTests P01-P11, assumed-period consumption, balance top-ups, gaps, corrections, replay and future-confirmation boundaries. |
| AC-04 | PASS | SessionEstimateTests S01-S09, sample exclusions, matching pool/source, plan changes, exhausted endpoint, age and latest-ten limits. |
| AC-05 | PASS | BudgetScenarioTests: all 11 limits, section 4.2 numerical figures and session counts, all 4.3 bar marks and all 4.4 account states. |
| AC-06 | PASS | BudgetEngineTests day-off share, coloring-only Work today and expiry, explicit provider-used evidence and rush exclusions; SessionEstimateTests extra-spend transition/reset/currency evidence. |
| AC-07 | PASS | Final Infrastructure 381/381 and Presentation 176/176; document validation; diff review/check; no Core dependency changes. |

## Final checks

Local Windows, SDK selected by global.json (10.0.401/latestPatch), 2026-10-02, code b9e603b:

- PASS: `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`
  - 381 tests, zero failures, errors, skips or not-run cases. Includes 51 new T-035 cases.
- PASS: `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`
  - 176 tests, zero failures, errors, skips or not-run cases.
- PASS: `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`
  - Valid documents, no diagnostics; final metadata changes revalidated before publication.
- PASS: `git diff --check` and primary integrated diff/acceptance review.
- NOT_RUN, outside this Core-only change: interactive Windows, package and live-provider
  acceptance. No credential or source CLI store was read, and no sign-in was attempted.

## Independent review and disposition

Read-only GPT-5.6 Luna, reasoning max, reviewed frozen 73553fb..e2970ea against the research,
design scenario and R-187. The review verdict was FAIL for that reference, with four material
findings. The primary checked each against the contract, reproduced the boundary with a
failing assertion, and resolved it in b9e603b:

1. Future confirmation coverage: replay now retains only a run's first known observation
   when LastConfirmed is later than now, and future/inconsistent RestartAfter is rejected.
   This preserves an actual first observation without inventing intermediate confirmations.
2. Provider-used-up provenance: only explicit provider facts establish this flag, regardless
   of period origin. The separate budget counter no longer acts as provider evidence.
3. Calendar fallback eligibility: an explicit adapter-supplied capability, default false,
   restricts the fallback to the accepted families; percent windows cannot opt in.
4. Extra-spend attribution: a below-full observation is required before the observed full
   transition; changed spend instances or observed counter decreases invalidate subtraction.
   The exact transition between refreshes is unknown; the output starts at observed fullness.

Targeted reruns passed (ReadingBudgetTests 9, BudgetEngineTests 25, BudgetScenarioTests 6,
SessionEstimateTests 7), followed by the two final full regression runs above. The independent
frozen-reference FAIL is not relabelled PASS. Primary disposition: all findings resolved;
no material findings remain. No automatic repeat of the full independent review was requested.

The current owner request authorizes execution. The specification derives its behavior
from accepted T-034 research and R-187; it has not received a separate owner review.

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
  73553fb..e2970ea range. Result and final disposition are recorded above; subsequent Work
  today fix was communicated separately and the reviewer acknowledged it as resolved.

## Execution ledger

Collapsed from tasks.md on 2026-10-09 (OD-19); the full plan is in Git history at aeaf11e.

- T-035.1 Normalized facts and arithmetic: done; commits not recorded (save point within 73553fb..e2970ea); review within the T-035.4 range review; checks targeted LimitModelTests; grant owner request, 2026-10-02.
- T-035.2 Calendar, budget and display: done; commits not recorded; review within the T-035.4 range review; checks targeted BudgetEngineTests and BudgetScenarioTests; grant owner request, 2026-10-02.
- T-035.3 Reading calculations and sessions: done; commits not recorded (save point within 73553fb..e2970ea); review within the T-035.4 range review; checks targeted ReadingBudgetTests and SessionEstimateTests, C4 (376/376); grant owner request, 2026-10-02.
- T-035.4 Integrated verification: done; commits b9e603b; review independent FAIL on 73553fb..e2970ea with four material findings, all resolved in b9e603b (primary disposition); checks C2, C4 (381/381), C5 (176/176), C6; grant owner request, 2026-10-02.
