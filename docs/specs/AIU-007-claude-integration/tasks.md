---
id: AIU-007
schema_version: 1
---
# AIU-007 execution and handoff

Spec: [spec.md](spec.md). Conditional design: [design.md](design.md). The primary owns canonical documents and integration. Implementation remains sequential unless an independent, bounded write task warrants an isolated worktree. Every subagent must use `gpt-5.6-luna` with `max` reasoning. No other feature is selected.

### T-01 - Establish provider and toolchain evidence
- status: done
- depends_on: []
- acceptance: AC-01
- evidence: docs/specs/AIU-007-claude-integration/verification.md

- [x] Inspect clean Git state and confirm AIU-027 at `0a0ffd4`.
- [x] Read repository boundaries and current official .NET guidance against the pinned SDK.
- [x] Inspect OMP source and current Anthropic authentication restrictions independently of live access.
- [x] Finish exact stable source references and record the unresolved material authorization decision in PROVIDER-002.

### T-02 - Shared Claude library, console and protected state
- status: blocked
- depends_on: [T-01]
- acceptance: AC-02, AC-03, AC-04
- evidence: not-run

Resolve PROVIDER-002 before production auth. Then write tests and implement the evidenced protocol, quota normalization and app-owned protected lifecycle in Infrastructure. Exercise the same implementation through the existing console. A runnable implementation plan depends on the authorization/scopes decision; no auth method is silently preselected here.

### T-03 - Application workflow and Windows presentation
- status: pending
- depends_on: [T-02]
- acceptance: AC-04, AC-05
- evidence: not-run

After library verification, extract only behavior actually shared with Codex and add the minimum Claude connection and quota presentation. Prove provider isolation, no overlapping refresh and shutdown drain before desktop wiring.

### T-04 - Review, actual verification and integration
- status: pending
- depends_on: [T-03]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07
- evidence: not-run

Complete primary and required independent review, actual packaged Windows checks and live provider proof. Record every missing case honestly. Commit and publish the completed task branch, review/check the combined local-main candidate, and integrate locally only after required acceptance passes. No main push or release is authorized.

## Handoff

Base: `0a0ffd4`. Branch: `codex/aiu-007-claude-integration`. The initial checkout was clean. Existing local main is `1e4f0ce`, an ancestor of the selected architecture base; main has not moved. No source credentials have been read or imported and no Claude account request has been made.

Completed checks: 72 Infrastructure, 12 Presentation/workflow/boundary and 78 document-validator regression tests pass on Windows with SDK 10.0.401 and existing restored packages. These are unchanged-baseline checks, not Claude feature verification.

Blocker: PROVIDER-002, the third-party authentication/storage restriction and lack of established permission for a quota-only application. No pending write-worker artifacts.

Exact next action: obtain the owner's disposition of PROVIDER-002 before implementing or launching any Claude OAuth flow.
