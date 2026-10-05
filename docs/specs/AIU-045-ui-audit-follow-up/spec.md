---
id: AIU-045
type: feature
status: draft
goal: G-003
scope_version: 1
approval_basis: The owner stopped the audit on 2026-10-04 and requested a preserved findings register and a follow-up task for fixes and discussion in later sessions. Execution remains paused pending owner resumption.
---

# Synthetic Windows audit follow-up

The owner stopped the comprehensive audit because it was taking too long. Preserve its work and evidence; do not automatically restart it. The canonical execution status is paused in the backlog.

## Intended result

Review the [findings and handoff](verification.md), agree on a small next batch, and fix demonstrated defects within existing product behavior. The first issue was repeatable startup termination of the latest synthetic audit build, before a window appears. Its root cause was observed on 2026-10-05 and is recorded in the [analysis record](analysis-2026-10-05.md); it is not yet fixed. Ordinary `--demo` startup of HEAD was unaffected, while Release, packaged and live startup remain NOT_RUN.

The earlier [audit report](../../workflow/ui-ux-audit-2026-10-04/report.md), [coverage matrix](../../workflow/ui-ux-audit-2026-10-04/coverage.csv), [additional scenarios](../../workflow/ui-ux-audit-2026-10-04/additional-scenarios.csv), [controls](../../workflow/ui-ux-audit-2026-10-04/controls.csv), [gallery index](../../workflow/ui-ux-audit-2026-10-04/screenshot-index.csv) and [independent reviews](../../workflow/ui-ux-audit-2026-10-04/review.md) remain the detailed evidence. They are incomplete audit records, not a final acceptance claim.

## Boundaries

Use synthetic accounts, mock authentication/provider boundaries, controlled budget clocks and marked temporary storage. Preserve Core, Infrastructure and Windows responsibilities. No real credentials, source credential import, real authentication, live quota endpoints, owner-app installation/update, release publication, host trust or display changes. Ordinary desktop behavior only. Explicit scope annotations are presentation-contract fixtures, not evidence that provider wire payloads establish personal/shared scope.

Do not rerun the full audit or expand the harness automatically. On resumption, discuss priority and use a bounded diagnostic or fix batch with appropriate targeted checks. The unresolved coverage and gallery are retained for a separately selected continuation.

## Acceptance criteria

- AC-01: Preserve every known finding with a stable ID, classification, status, evidence, affected code and exact next diagnostic or verification action.
- AC-02: When selected for execution, identify the cause of the audit-build startup failure, reproduce it with synthetic inputs, apply the minimum justified correction and actually rerun the failing Windows scenario. Do not infer ordinary-product impact without evidence.
- AC-03: Review the unfinished scope-annotation and native-layout changes; retain or revise them based on the selected batch. Preserve unknown/shared/disabled/currency restrictions and account isolation.
- AC-04: Keep previously fixed defects and independent-review dispositions discoverable. Missing interactive/visual evidence remains explicitly incomplete until executed; do not convert old passes into final-build passes.
- AC-05: For each selected fix, complete relevant verification and required review, record build/code references and publish checkpoints under CONTRIBUTING.md. Full gallery completion requires separate owner resumption of that work.

## Exact next action after resumption

The read-only diagnosis of AUD-01 and AUD-02 is complete; see the [analysis record](analysis-2026-10-05.md). Next, the owner records decisions AIU045-D1..D9 from its section 7. D1 option (c) has been in effect since 2026-10-05: `AIU_PREVIEW_ENABLED=false`, so a push to main no longer publishes a Preview. Then one primary session starts T-01 (CI and publication safety). Do not start T-02..T-04 before the decisions are recorded, do not launch another broad matrix, and do not resume the comprehensive audit.
