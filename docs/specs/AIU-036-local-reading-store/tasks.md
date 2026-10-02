---
id: AIU-036
schema_version: 1
---

# Local reading store implementation plan

Primary executes sequentially on main under CONTRIBUTING.md, using test-first behavioral
checks and the existing .NET 10 dependencies. Spec: [spec.md](spec.md).

### T-01 - Stores and observation contracts
- status: in-progress
- depends_on: []
- acceptance: AC-02, AC-03, AC-04, AC-05
- evidence: docs/specs/AIU-036-local-reading-store/verification.md

- [ ] Add Core `IReadingSeriesStore`, `ReadingObservation`, `IBudgetConfigurationStore` and
  credential-free configuration contracts; extend ReadingRun metadata without breaking callers.
- [ ] Add failing LocalBudgetStoreTests covering persisted runs, coalescing/gaps, default and
  unmatched configuration, recovery and exact-path protection.
- [ ] Implement Infrastructure `LocalBudgetStore` and internal bounded staged JSON persistence;
  AppendAsync accepts observations, ReadAsync returns runs, configuration uses Load/Save.
- [ ] Verify targeted tests and save a reviewed commit.

### T-02 - Refresh recording and lifecycle
- status: pending
- depends_on: [T-01]
- acceptance: AC-01, AC-04, AC-05
- evidence: docs/specs/AIU-036-local-reading-store/verification.md

- [ ] Add tests for LiveUsageSource recording successful refreshes and draining writes, with
  cached/error/sign-out paths excluded; implement a Core recorder boundary and Windows adapter.
- [ ] Register one Infrastructure store in live composition; no provider transport changes.
- [ ] Complete the owner-selected cleanup scope with failure/ownership tests.
- [ ] Verify targeted tests and save a reviewed commit.

### T-03 - Calculation replay and integrated review
- status: pending
- depends_on: [T-01, T-02]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06
- evidence: docs/specs/AIU-036-local-reading-store/verification.md

- [ ] Verify P01-P11 and paired session estimates through persisted round trips, with exact
  assertions; verify retention, limits, interrupted staging and corrupt configuration.
- [ ] Run Infrastructure and Presentation Release suites, document validator and diff check.
- [ ] Freeze reviewed code and request a fresh read-only Luna/max security-lifecycle review;
  fix material findings with targeted regressions, record final evidence and publish.

## Review focus

Midnight replay after coalescing; source/plan/unit changes; failure after staging but before
replace; cleanup with unknown files/reparse points; stop or sign-out while recording is pending.

## Handoff

Base: aeaf11e. Existing AIU-034 tasks and verification edits are unrelated and preserved.
Stores, recorder, cleanup and P01-P11 persisted replay are implemented; targeted checks pass.
Next action: run full regressions and the required independent lifecycle review, then resolve
material findings and update completion evidence. No live sign-in is needed.
