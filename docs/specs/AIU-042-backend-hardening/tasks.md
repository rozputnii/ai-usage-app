---
id: AIU-042
schema_version: 1
---

# AIU-042 implementation plan and handoff

Goal: complete the bounded backend audit and evidence-backed remediation in [spec.md](spec.md).
Architecture: preserve existing Core computations/contracts, Infrastructure transport/storage,
and Windows orchestration. Make small fixes backed by failing regressions; no new layers,
dependencies, stored formats, provider features or visual changes. SDK 10.0.401, stable C# 14.

The primary executes sequentially on main under CONTRIBUTING. The owner's explicit autonomous
execution and minimal-documentation instructions govern planning: this task record is the plan
and progress ledger. No Codex subagents. Required independent review gates are not waived.

### T-01 - Inventory and baseline
- status: in-progress
- depends_on: []
- acceptance: AC-01, AC-03, AC-04
- evidence: verification.md

- [x] Inspect Git state and policy. Base 66e3eb8, clean main tracking origin/main.
- [x] Run baseline Infrastructure and Presentation Release suites.
- [ ] Read every in-scope production subsystem and related tests; record coverage and findings.
- [x] Assess stable runtime/language opportunities against official Microsoft documentation.

### T-02 - Regression-backed remediation and measurements
- status: in-progress
- depends_on: [T-01]
- acceptance: AC-02, AC-03, AC-04, AC-05, AC-06
- evidence: verification.md

- [x] Add a reproducible synthetic Release measurement harness using existing dependencies.
- [x] Measure 35-day reading-store, budget/history and parser baseline workloads.
- [ ] For each confirmed finding, add and run a failing regression before a small fix.
- [ ] Run targeted checks and comparable measurements; assess review gates before integration.
- [ ] Commit/push each safe coherent step; preserve gated changes without publishing them.

### T-03 - Integrated verification and closure
- status: pending
- depends_on: [T-02]
- acceptance: AC-07, AC-08
- evidence: not-run

- [ ] Review the integrated diff against all acceptance criteria and boundary contracts.
- [ ] Run applicable warning-as-error builds, both regression suites, document and diff checks.
- [ ] Obtain any required authorized independent review or record BLOCKED; never substitute self-review.
- [ ] Record each AC's actual outcome, remaining findings and limitations, then close only if satisfied.

## Handoff

Next action: collect the focused journal follow-up review, resolve any material findings,
and finish the subsystem inventory before final integrated verification.
Baseline: Infrastructure 416/416 and Presentation 179/179 PASS on SDK 10.0.401, Windows Release.
Core boundary fixes, binary-search coverage and measurement harness are ready for a save point.
Journal correction is owner-authorized and awaiting targeted independent review; do not publish it
until that gate is satisfied. History/cache fixes have passing focused evidence in verification.md.
Live access, CLI credential reads and sign-in are unauthorized.
3c0c30d is the published planning/baseline save point. Audit remains incomplete; do not close.
