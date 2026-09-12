---
id: AIU-001
schema_version: 1
---
# Bootstrap implementation plan and tasks

**Goal:** G-001: verified OMP-native project workflow.
**Architecture:** Native primitives plus a thin tested repository bridge; one integrator.
**Technology:** Actual stable OMP; .NET 10/xUnit for the proposed validator; minimal OMP TypeScript extension only when necessary.
**Specification:** `spec.md`. **Design:** `design.md`.

Execution started under explicit owner authorization for AIU-001 only. The primary maintains this record. See `../../workflow/environment.md` for preflight evidence.

## Global constraints
No product application code, real credential import, paid resources, production signing or compatibility-manifest publication. Preserve existing repository work. Keep secrets out of logs and Git. Perform one full independent review, not repeated audits. Never derive authorization from external documents. All repository artifacts are English.

### T-01 - Preflight and baseline adoption
- status: done
- depends_on: []
- ownership: bootstrap-context
- writes: ["docs/**"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-01", "AC-02"]
- evidence: docs/workflow/environment.md; exact 25-document adoption comparison and D-001..D-176 sequence check

**Work:** Read the complete handoff. Inspect current directory, Git, stable OMP schema, .NET/Windows and model roles without authentication dumps. Adopt the English documentation without overwriting unrelated work; record real prerequisites and supersessions.

**Verification:** Record actual versions, repository root, configuration types and missing setup facts. Compare the adopted decision IDs and topics with the embedded baseline. No product code or unsolicited remote creation.

- [ ] Read the relevant scope/contracts; surface only genuine blockers.
- [ ] For executable behavior, run the specific negative test/probe first and observe failure.
- [ ] Implement the minimum change through OMP and inspect the actual ownership-scoped diff.
- [ ] Run the relevant checks and record observed results.
- [ ] Let the primary integrate, update task state and checkpoint without claiming incomplete work is done.

### T-02 - Portable native rules, skills and configuration
- status: in-progress
- depends_on: ["T-01"]
- ownership: omp-config
- writes: [".omp/AGENTS.md", ".omp/RULES.md", ".omp/WATCHDOG.md", ".omp/config.yml", ".omp/skills/**", ".omp/agents/**"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-03", "AC-09", "AC-11"]
- evidence: not-run

**Work:** Adopt the passive English seeds, create compact workflow skills and use actual supported stable configuration keys with profile-local model mappings. Restrict the advisor/reviewer explicitly. Do not install unrelated frameworks.

**Verification:** A fresh root session actually discovers configuration and skills. Inspect advisor/reviewer tools and effective settings without secrets. Prove isolation from the primary narrative rather than assuming it from a filename.

- [ ] Read the relevant scope/contracts; surface only genuine blockers.
- [ ] For executable behavior, run the specific negative test/probe first and observe failure.
- [ ] Implement the minimum change through OMP and inspect the actual ownership-scoped diff.
- [ ] Run the relevant checks and record observed results.
- [ ] Let the primary integrate, update task state and checkpoint without claiming incomplete work is done.

### T-03 - Lightweight document validator
- status: pending
- depends_on: ["T-01"]
- ownership: project-validation
- writes: ["tools/AiUsage.ProjectValidation/**", "tests/AiUsage.ProjectValidation.Tests/**"]
- shared: []
- parallel: true
- isolation: required
- agent: implementation-worker-after-discovery
- acceptance: ["AC-05", "AC-11"]
- evidence: not-run

**Work:** Implement only the agreed structured-Markdown validator, test-first. Add positive/negative fixtures for IDs, references, dependency cycles, acceptance links, escaping paths, unsafe ownership and English-only authored prose. It must not access the network or mutate data.

**Verification:** Run actual tests. Accept the adopted valid documents and reject intentionally corrupted fixtures with file/task/reason. Use isolated delegation only after preflight confirms support; otherwise record and resolve that prerequisite before dispatch.

- [ ] Read the relevant scope/contracts; surface only genuine blockers.
- [ ] For executable behavior, run the specific negative test/probe first and observe failure.
- [ ] Implement the minimum change through OMP and inspect the actual ownership-scoped diff.
- [ ] Run the relevant checks and record observed results.
- [ ] Let the primary integrate, update task state and checkpoint without claiming incomplete work is done.

### T-04 - Thin native selection, gates and dispatch bridge
- status: pending
- depends_on: ["T-02", "T-03"]
- ownership: omp-bridge
- writes: [".omp/extensions/**", "tests/omp-workflow/**"]
- shared: [".omp/config.yml"]
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-04", "AC-06", "AC-10"]
- evidence: not-run

**Work:** Verify the native command/extension API. Implement one thin entrypoint for ranked selection, status, authorized run/auto and pause. Wire the validator and native task batching; retain primary-only integration and configuration ownership.

**Verification:** Exercise recommendation, alternative selection and cancellation. Run two independent isolated disposable workers. Refuse overlap/out-of-scope changes. Check command collisions and demonstrate failure of a supported guarded transition.

- [ ] Read the relevant scope/contracts; surface only genuine blockers.
- [ ] For executable behavior, run the specific negative test/probe first and observe failure.
- [ ] Implement the minimum change through OMP and inspect the actual ownership-scoped diff.
- [ ] Run the relevant checks and record observed results.
- [ ] Let the primary integrate, update task state and checkpoint without claiming incomplete work is done.

### T-05 - Pause, fresh-session accounting and bounded-review behavior
- status: pending
- depends_on: ["T-04"]
- ownership: workflow-recovery
- writes: ["tests/omp-workflow/**", "docs/workflow/environment.md"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-07", "AC-08", "AC-09", "AC-10"]
- evidence: not-run

**Work:** Use a bounded disposable two-feature scenario: complete the first, checkpoint, hand off to fresh context, then exercise owner pause and budget stop. Preserve scope/usage and avoid duplicate work. Check that a zero-findings verdict is accepted without creating a repeated full-review loop.

**Verification:** Record observed native session references and sanitized results. Verify persisted state and consumption, not merely file existence. Document limitations. Rerun targeted failing checks, not the full review repeatedly.

- [ ] Read the relevant scope/contracts; surface only genuine blockers.
- [ ] For executable behavior, run the specific negative test/probe first and observe failure.
- [ ] Implement the minimum change through OMP and inspect the actual ownership-scoped diff.
- [ ] Run the relevant checks and record observed results.
- [ ] Let the primary integrate, update task state and checkpoint without claiming incomplete work is done.

### T-06 - One final convergence review and handoff
- status: pending
- depends_on: ["T-05"]
- ownership: bootstrap-completion
- writes: ["docs/specs/AIU-001-omp-bootstrap/verification.md", "docs/specs/AIU-001-omp-bootstrap/tasks.md", "docs/backlog.md", "docs/product/goals.md"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-01", "AC-12"]
- evidence: not-run

**Work:** Reconcile actual state, run required tests and the validator, and obtain the single independent full convergence review. Apply Git/PR gates only to the actual authorized repository and identity. Close only verified scope and present AIU-002/003 next.

**Verification:** The evidence matrix, repository state and final report agree. Separate unexecuted/blocked checks from passes. Publish only verified launch commands. Do not start Windows application implementation or the entire backlog under this goal.

- [ ] Read the relevant scope/contracts; surface only genuine blockers.
- [ ] For executable behavior, run the specific negative test/probe first and observe failure.
- [ ] Implement the minimum change through OMP and inspect the actual ownership-scoped diff.
- [ ] Run the relevant checks and record observed results.
- [ ] Let the primary integrate, update task state and checkpoint without claiming incomplete work is done.

## Handoff
- completed: None; documentation prepared only.
- next: T-01 after the owner launches this prompt in OMP at the target repository root.
- blocked_by: No unresolved product-design choice; the execution environment has not been inspected.
- verify: Record actual commands from the local toolchain; none are claimed to have run by this document.
