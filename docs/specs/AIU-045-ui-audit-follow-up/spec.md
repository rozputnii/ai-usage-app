---
id: AIU-045
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: The owner stopped the audit on 2026-10-04 and requested a preserved findings register and a follow-up task for fixes and discussion in later sessions. On 2026-10-06 the owner accepted decisions AIU045-D1..D9 (analysis record, section 7) and resumed execution with the bounded fix run T-01..T-04.
---

# Synthetic Windows audit follow-up

The owner stopped the comprehensive audit because it was taking too long. Preserve its work and evidence; do not automatically restart it. The canonical execution status is in the backlog; the comprehensive audit stays paused (2026-10-06).

## Intended result

Review the [findings and handoff](verification.md), agree on a small next batch, and fix demonstrated defects within existing product behavior. The first issue was repeatable startup termination of the latest synthetic audit build, before a window appears. Its root cause was observed on 2026-10-05 and is recorded in the [analysis record](analysis-2026-10-05.md). On 2026-10-06 it was fixed (`5560205`) and the host A/B run starts; the AC-02 rerun ran on 2026-10-06 in the one authorized Sandbox batch, and the app started in every guest scenario. Ordinary `--demo` startup of HEAD was unaffected; Release `--demo` startup reached the window on 2026-10-06 without an observed exit; packaged startup was PASS on 2026-10-06 (the owner's installed Preview 2026.10.602.0), and the D9 live-empty smoke was PASS on 2026-10-06 (`a4d33cf`).

The earlier [audit report](../../archive/workflow/ui-ux-audit-2026-10-04/report.md), [coverage matrix](../../archive/workflow/ui-ux-audit-2026-10-04/coverage.csv), [additional scenarios](../../archive/workflow/ui-ux-audit-2026-10-04/additional-scenarios.csv), [controls](../../archive/workflow/ui-ux-audit-2026-10-04/controls.csv), [gallery index](../../archive/workflow/ui-ux-audit-2026-10-04/screenshot-index.csv) and [independent reviews](../../archive/workflow/ui-ux-audit-2026-10-04/review.md) remain the detailed evidence. They are incomplete audit records, not a final acceptance claim.

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

None (2026-10-06). The fix run is complete: T-01..T-04 are done, Preview 2026.10.602.0 was published from `4ea9667` (owner dispatch, run 37475616318) and installed by the owner, and the installed-build checks passed (see the [verification record](verification.md), section "Post-install verification 2026-10-06"). The final status of every AUD, FIX, ANL and T item is in the [analysis record](analysis-2026-10-05.md), section "Final status (2026-10-06, before the owner checkpoint)".

Opt-in follow-ups, each needing a separate owner selection (none is started):

- AUD-05..AUD-09: the deferred audit follow-ups; the comprehensive audit and gallery are not resumed.
- ANL-20: Preview release retention, mutability, the unversioned dependency URL and the deploy race (deferred).
- ANL-23: the narrow-window layout finding (the history panel header's account title overlaps the hint text; the cap editor's Save button is clipped), for a later bounded UI batch.
- The AIU-043 device-code display delay, not exercised because no sign-in was performed.
- The `New-UiAuditPages.ps1` dry run, which needs PowerShell 7 (not installed on the host).

Do not launch another broad matrix, and do not resume the comprehensive audit.
