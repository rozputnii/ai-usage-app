---
id: AIU-045
type: feature
status: implementing
goal: G-003
scope_version: 1
approval_basis: The owner stopped the audit on 2026-10-04 and requested a preserved findings register and a follow-up task for fixes and discussion in later sessions. On 2026-10-06 the owner accepted decisions AIU045-D1..D9 (analysis record, section 7) and resumed execution with the bounded fix run T-01..T-04.
---

# Synthetic Windows audit follow-up

The owner stopped the comprehensive audit because it was taking too long. Preserve its work and evidence; do not automatically restart it. The canonical execution status is in the backlog; the comprehensive audit stays paused (2026-10-06).

## Intended result

Review the [findings and handoff](verification.md), agree on a small next batch, and fix demonstrated defects within existing product behavior. The first issue was repeatable startup termination of the latest synthetic audit build, before a window appears. Its root cause was observed on 2026-10-05 and is recorded in the [analysis record](analysis-2026-10-05.md). On 2026-10-06 it was fixed (`5560205`) and the host A/B run starts; the AC-02 rerun ran on 2026-10-06 in the one authorized Sandbox batch, and the app started in every guest scenario. Ordinary `--demo` startup of HEAD was unaffected; Release `--demo` startup reached the window on 2026-10-06 without an observed exit; packaged startup remains NOT_RUN, and the D9 live-empty smoke was PASS on 2026-10-06 (`a4d33cf`).

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

Owner checkpoint (2026-10-06). The fix run reached it with T-01 and T-02 integrated and CI-green, T-03 in progress with the candidate verified by the desktop-lane rerun of 2026-10-06 (`a4d33cf`), and the final status of every AUD, FIX, ANL and T item recorded in the [analysis record](analysis-2026-10-05.md), section "Final status (2026-10-06, before the owner checkpoint)". `AIU_PREVIEW_ENABLED` stays `false` until step 1. In order:

1. The owner dispatches the Preview: confirm `git rev-parse origin/main` equals the gated candidate commit, or that the delta is docs/tools-only with green CI on it (2026-10-06); then `gh variable set AIU_PREVIEW_ENABLED --body true`, then `gh workflow run validation.yml --ref main -f PublishPreview=true`.
2. On the installed build, the owner checks, or authorizes checking: the update applied; packaged startup; close-to-tray and relaunch restore the window without `LeaseUnavailable`; packaged Open logs (AIU-043 AC-13); optionally `ShellSmoke.PackagedLedgerLaunchesAndExits` with `AIU_SMOKE_AUMID`. Task N records the results.
3. ANL-16: the owner did not consent at kickoff to reading the installed app's sanitized logs, so nothing was read; it stays open until the owner consents or declines.
4. The `New-UiAuditPages.ps1` dry run needs PowerShell 7, which is not installed on the host; it is offered to the owner.
5. (optional) The owner authorizes one more Sandbox batch for the corrected layout scenario (`*AuditWindows.OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable`, test fix `e40c76d`, passing on the host).

Do not launch another broad matrix, and do not resume the comprehensive audit.
