---
id: AIU-006
schema_version: 1
---
# AIU-006 implementation plan

Goal: verify a real package upgrade and recover an interrupted first durable-state migration.
Architecture: Core maintenance contract; Infrastructure filesystem state machine; Windows startup/recovery adapter. Existing .NET 10, WinUI and DPAPI only.
Spec: [spec.md](spec.md). Execute sequentially in the primary session using executing-plans and test-driven-development. User requested automatic execution; repository main/commit policy overrides worktree and repeated approval defaults.

## Constraints and review focus

Preserve opaque preferences, provider files and unknown data; never downgrade. Test partial publication, malformed/tampered checkpoint, newer schema with old journal, redirected paths and concurrent processes. No source credential reads or host trust changes.

### T-01 - Durable maintenance state machine
- status: in-progress
- depends_on: []
- acceptance: AC-02, AC-03, AC-04, AC-05
- evidence: not-run

- [ ] Add failing behavioral tests in `tests/windows/AiUsage.Infrastructure.Tests/StateMaintenanceTests.cs` for byte preservation, interruption/restart/retry, explicit restore, tampering, newer schema, redirected files and lifetime exclusion.
- [ ] Implement `IStateMaintenance` in Core and `StateMaintenance` plus bounded records in Infrastructure; run Infrastructure tests to PASS.
- [ ] Route `PresentationPreferenceFile` to the committed preference directory in product composition. Preserve its standalone file API.
- [ ] Review diff and save progress to main.

### T-02 - Live recovery integration
- status: pending
- depends_on: [T-01]
- acceptance: AC-06
- evidence: not-run

- [ ] Add failing presentation tests for maintenance gating, retry/restore and shutdown.
- [ ] Implement `LiveRecoveryService`; gate `ProductLifecycle`, provider actions and live preference loading; wire Windows folder/diagnostics actions.
- [ ] Run both regression suites and build package; inspect actual recovery screen and explicit retry/restore.

### T-03 - Package proof and focused review
- status: pending
- depends_on: [T-02]
- acceptance: AC-01, AC-03, AC-07
- evidence: not-run

- [ ] Build a fresh signed new MSIX and use retained old signed MSIX with matching family in a disposable guest; seed only synthetic DPAPI/state there.
- [ ] Exercise upgrade, interrupted migration, retry and restore with actual product activation; retain sanitized evidence and screenshots locally.
- [ ] Freeze candidate and obtain focused read-only independent review. The repository-required Codex Luna model is absent from the listed subagent overrides; investigate an available non-Codex review mechanism without substituting a prohibited Codex model.
- [ ] Address material findings, run targeted checks, update verification/backlog, commit and push.

## Handoff

Base: main at task selection. No prior implementation. Next action: add and run failing maintenance tests. Checks: Git clean before task; other checks NOT_RUN.
