---
id: AIU-036
schema_version: 1
---

# Local reading store implementation plan

Primary executes sequentially on main under CONTRIBUTING.md, using test-first behavioral
checks and the existing .NET 10 dependencies. Spec: [spec.md](spec.md).

### T-01 - Stores and observation contracts
- status: done
- depends_on: []
- acceptance: AC-02, AC-03, AC-04, AC-05
- evidence: docs/specs/AIU-036-local-reading-store/verification.md

- [x] Add Core `IReadingSeriesStore`, `ReadingObservation`, `IBudgetConfigurationStore` and
  credential-free configuration contracts; extend ReadingRun metadata without breaking callers.
- [x] Add failing LocalBudgetStoreTests covering persisted runs, coalescing/gaps, default and
  unmatched configuration, recovery and exact-path protection.
- [x] Implement Infrastructure `LocalBudgetStore` and internal bounded staged JSON persistence;
  AppendAsync accepts observations, ReadAsync returns runs, configuration uses Load/Save.
- [x] Verify targeted tests and save a reviewed commit.

### T-02 - Refresh recording and lifecycle
- status: done
- depends_on: [T-01]
- acceptance: AC-01, AC-04, AC-05
- evidence: docs/specs/AIU-036-local-reading-store/verification.md

- [x] Add tests for LiveUsageSource recording successful refreshes and draining writes, with
  cached/error/sign-out paths excluded; implement a Core recorder boundary and Windows adapter.
- [x] Register one Infrastructure store in live composition; no provider transport changes.
- [x] Complete the owner-selected cleanup scope with failure/ownership tests.
- [x] Verify targeted tests and save a reviewed commit.

### T-03 - Calculation replay and integrated review
- status: done
- depends_on: [T-01, T-02]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06
- evidence: docs/specs/AIU-036-local-reading-store/verification.md

- [x] Verify P01-P11 and paired session estimates through persisted round trips, with exact
  assertions; verify retention, limits, interrupted staging and corrupt configuration.
- [x] Run Infrastructure and Presentation Release suites, document validator and diff check.
- [x] Freeze reviewed code and request a fresh read-only Luna/max security-lifecycle review;
  fix material findings with targeted regressions, record final evidence and publish.

## Review focus

Midnight replay after coalescing; source/plan/unit changes; failure after staging but before
replace; cleanup with unknown files/reparse points; stop or sign-out while recording is pending.

## Handoff

Base: aeaf11e. Final implementation: ba1bf87. AIU-036 is complete within the owner-selected
store/cleanup scope. Existing AIU-034 task and verification edits and the unrelated 868da29
agent-configuration commit were preserved. No worker artifacts or material findings remain.
Next action: await owner selection of another backlog item; none is started automatically.
AIU-037 must explicitly map existing legacy-v1 series keys when it adopts native parser keys.
Product delete/reset UI wiring and live-provider acceptance remain deferred as documented.
