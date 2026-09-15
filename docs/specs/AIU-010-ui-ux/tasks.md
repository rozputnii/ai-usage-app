---
id: AIU-010
schema_version: 1
---
# AIU-010 staged execution

The primary owns shared contracts, canonical documents and integration. Stages are sequential; Claude Design and Claude Code are external handoff roles, not claims of available or running models. No worker has been launched by the preparation delivery.

### T-01 - Prepare current and planned UI contract
- status: done
- depends_on: []
- acceptance: AC-01, AC-02, AC-08, AC-09
- evidence: docs/specs/AIU-010-ui-ux/verification.md

Prepare the inventory, semantic contract, deterministic scenario catalog, agent prompts and acceptance gates. Validate documents and fixtures and review the scoped diff.

### T-02 - Claude Design visual system and complete mockup
- status: ready
- depends_on: [T-01]
- acceptance: AC-03, AC-05, AC-06
- evidence: not-run

Use the design prompt in handoffs.md. Deliver two directions, then the selected complete design with S01–S12/F01–F15 coverage. Keep editable artifacts and exports identifiable by revision.

### T-03 - Owner visual approval and Codex feasibility review
- status: pending
- depends_on: [T-02]
- acceptance: AC-02, AC-03, AC-05, AC-06
- evidence: not-run

Review navigation, all states, semantic fidelity, WinUI feasibility and dependency needs. Resolve actual design gaps and record the approved artifact revision here before T-04 starts. No approved visual artifact exists at preparation time.

### T-04 - Claude Code native frontend and demo
- status: pending
- depends_on: [T-03]
- acceptance: AC-04, AC-05, AC-06, AC-08, AC-09
- evidence: not-run

Use the frontend prompt in handoffs.md. Implement tokens/components and shell first, then S01–S03/S07, then S04–S06, then S08–S12. Each group must have fixture-driven interactions, accessible keyboard behavior and relevant checks before moving on. Keep product composition operational. Return the actual diff and native screenshots for Codex review; primary integrates before marking done.

### T-05 - Codex available-service integration
- status: pending
- depends_on: [T-04]
- acceptance: AC-06, AC-07, AC-08
- evidence: not-run

Map existing workflows, implement agreed presentation preference adapters, and keep future services unavailable in product mode. Add regression coverage for mapping and command capability checks. If durable-state/credential boundaries change, use the required security-lifecycle and focused review workflow.

### T-06 - Integrated Windows and visual acceptance
- status: pending
- depends_on: [T-05]
- acceptance: AC-03, AC-04, AC-05, AC-06, AC-07, AC-08, AC-09
- evidence: not-run

Run required regressions/build and actual Windows UI scenarios. Check live/demo separation, keyboard/scaling/themes, quota semantics, navigation, tray restore/Exit and shutdown. Record owner visual acceptance separately from deterministic checks. Do not mark future backend AIUs done based on demo success.

## Handoff

Base: `ba6b49f`; initial checkout clean. Preparation branch: `codex/aiu-010-design-handoff`. No application source, credentials, provider transport or user data changed. No pending worker artifacts.

Exact next action after preparation: give Claude Design the full AIU-010 package and the first prompt in handoffs.md to produce two Overview/account-detail visual directions.
