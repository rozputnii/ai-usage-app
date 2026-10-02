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
- status: done
- depends_on: []
- acceptance: AC-01, AC-03, AC-04
- evidence: docs/specs/AIU-042-backend-hardening/verification.md

- [x] Inspect Git state and policy. Base 66e3eb8, clean main tracking origin/main.
- [x] Run baseline Infrastructure and Presentation Release suites.
- [x] Read every in-scope production subsystem and related tests; record coverage and findings.
- [x] Assess stable runtime/language opportunities against official Microsoft documentation.

### T-02 - Regression-backed remediation and measurements
- status: done
- depends_on: [T-01]
- acceptance: AC-02, AC-03, AC-04, AC-05, AC-06
- evidence: docs/specs/AIU-042-backend-hardening/verification.md

- [x] Add a reproducible synthetic Release measurement harness using existing dependencies.
- [x] Measure 35-day reading-store, budget/history and parser baseline workloads.
- [x] Add failing bug regressions and behavior-preservation coverage for the performance change.
- [x] Run targeted checks and comparable measurements; assess review gates before integration.
- [x] Commit/push each safe coherent step; preserve gated changes until their review passes.

### T-03 - Integrated verification and closure
- status: done
- depends_on: [T-02]
- acceptance: AC-07, AC-08
- evidence: docs/specs/AIU-042-backend-hardening/verification.md

- [x] Review the integrated diff against all acceptance criteria and boundary contracts.
- [x] Run applicable warning-as-error builds, both regression suites, document and diff checks.
- [x] Obtain required independent review and resolve material findings with targeted verification.
- [x] Record each AC's actual outcome, remaining findings and limitations; all criteria satisfied.

## Handoff

AIU-042 complete. Published implementation: 019836a and a0e4b75; planning save point 3c0c30d.
Final evidence: 448 Infrastructure/179 Presentation tests PASS; consumer Release builds,
document validation and diff check PASS. Focused independent review satisfied; no Codex subagents.
Measurements, corrected initial FAIL results and deferred minor findings are in verification.md.
Live-provider/interactive UI/package checks NOT_RUN, not required for this diff.
Next action: none within AIU-042. Stop; AIU-037 requires separate owner selection.
