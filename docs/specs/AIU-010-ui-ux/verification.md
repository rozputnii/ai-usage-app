# AIU-010 verification

## Preparation delivery - 2026-09-15

Scope: documentation, presentation contract proposal and synthetic scenario catalog only. Base code inspected: `ba6b49f`. Existing source mappings were checked against the Core quota/session contracts and Windows dashboard view models. Accepted decisions were checked for future scope, tray, appearance, localization, history and data lifecycle.

| Check | Result | Evidence / limitation |
|---|---|---|
| Source and product-decision mapping | PASS | screens.md and ui-contract.md distinguish current Codex/Claude building blocks from future capabilities; no provider protocol changed |
| Document validation | PASS | SDK 10.0.401: dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json returned valid:true, diagnostics:[] after frontmatter correction. |
| Synthetic fixture consistency | PASS | PowerShell ConvertFrom-Json parsed the catalog; synthetic flag true; 15 unique scenario IDs; all S01–S12 covered. This checks the seed catalog, not future runtime transitions. |
| Primary diff/link/instruction review | PASS | Reviewed the nine-file preparation scope against source mappings and accepted decisions; links and future/live boundaries inspected. No implementation or dependency change. |
| Product regressions / package build / interactive Windows / live providers | NOT_RUN | No product code changed; these remain required at frontend/integration stages |
| Claude design / owner visual approval / Claude frontend | NOT_RUN | Handoff material prepared; no design or frontend artifact exists yet |

AC-01/02/08 preparation coverage is supplied by the inventory, contract and synthetic catalog; this does not establish implemented runtime behavior. AC-03–07 and final runtime AC-09 remain NOT_RUN. AIU-010 is not complete when T-01 finishes.

The first document validation found missing design frontmatter. Added the required id/type/status/goal/scope_version; final validation result is recorded above after rerun.
