---
id: AIU-001
schema_version: 1
---
# Bootstrap implementation plan and tasks

Historical evidence: the executable workflow and its instructions were retired on 2026-09-14. Runtime commands, permission records and language prescriptions below describe the past bootstrap only; current development follows CONTRIBUTING.md. Archived sources map to docs/archive/omp/<original path>. Provider-source provenance remains unchanged.

**Goal:** G-001: verified OMP-native project workflow.
**Architecture:** Native primitives plus a thin tested repository bridge; one integrator.
**Technology:** OMP 18.1.18; .NET 10/xUnit v3 validator; thin native TypeScript extension.
**Specification:** `spec.md`. **Design:** `design.md`.

Execution completed under explicit owner authorization for AIU-001 only. The primary maintains this record. See `../../workflow/environment.md` for actual evidence and the owner-authorized review recovery exception.

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


### T-02 - Portable native rules, skills and configuration
- status: done
- depends_on: ["T-01"]
- ownership: omp-config
- writes: [".omp/AGENTS.md", ".omp/RULES.md", ".omp/WATCHDOG.md", ".omp/WATCHDOG.yml", ".omp/config.yml", ".omp/skills/**"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-03", "AC-09", "AC-11"]
- evidence: docs/workflow/environment.md; fresh native SDK project/skill discovery and explicit advisor tool inspection

**Work:** Adopt the passive English seeds, create compact workflow skills and use actual supported stable configuration keys with profile-local model mappings. Restrict the advisor/reviewer explicitly. Do not install unrelated frameworks.

**Verification:** A fresh root session actually discovers configuration and skills. Inspect advisor/reviewer tools and effective settings without secrets. Prove isolation from the primary narrative rather than assuming it from a filename.


### T-03 - Lightweight document validator
- status: done
- depends_on: ["T-01"]
- ownership: project-validation
- writes: ["tools/AiUsage.ProjectValidation/**", "tests/AiUsage.ProjectValidation.Tests/**"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-05", "AC-11"]
- evidence: docs/workflow/environment.md; primary integrated the isolated test-first contribution and completed the validator after a worker timeout; 49 xUnit tests pass and canonical root validates

**Work:** Implement only the agreed structured-Markdown validator, test-first. Add positive/negative fixtures for IDs, references, dependency cycles, acceptance links, escaping paths, unsafe ownership and English-only authored prose. It must not access the network or mutate data.

**Verification:** Run actual tests. Accept the adopted valid documents and reject intentionally corrupted fixtures with file/task/reason. Use isolated delegation only after preflight confirms support; otherwise record and resolve that prerequisite before dispatch.


### T-04 - Thin native selection, gates and dispatch bridge
- status: done
- depends_on: ["T-02", "T-03"]
- ownership: omp-bridge
- writes: [".omp/extensions/**", ".omp/lib/**", ".omp/agents/**", "tools/start-work.ts", "tests/omp-workflow/**"]
- shared: [".omp/config.yml"]
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-04", "AC-06", "AC-10"]
- evidence: docs/workflow/environment.md; native ranked selection and cancellation, isolated worker patches, primary integration, headless denial and live-grant mutation rejection; 11 Bun tests pass

**Work:** Verify the native command/extension API. Implement one thin entrypoint for ranked selection, status, authorized run/auto and pause. Wire the validator and native task batching; retain primary-only integration and configuration ownership.

**Verification:** Exercise recommendation, alternative selection and cancellation. Run two independent isolated disposable workers. Refuse overlap/out-of-scope changes. Check command collisions and demonstrate failure of a supported guarded transition.


### T-05 - Pause, fresh-session accounting and bounded-review behavior
- status: done
- depends_on: ["T-04"]
- ownership: workflow-recovery
- writes: ["tests/omp-workflow/**", "docs/workflow/environment.md"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-07", "AC-08", "AC-09", "AC-10"]
- evidence: docs/workflow/environment.md; native feature handoff and final guarded fresh-session continuation preserve scope and consumption; live-worker pause, external-resume confirmation and budget stop pass; isolated reviewer setup accepts zero findings

**Work:** Use a bounded disposable two-feature scenario: complete the first, checkpoint, hand off to fresh context, then exercise owner pause and budget stop. Preserve scope/usage and avoid duplicate work. Check that a zero-findings verdict is accepted without creating a repeated full-review loop.

**Verification:** Record observed native session references and sanitized results. Verify persisted state and consumption, not merely file existence. Document limitations. Rerun targeted failing checks, not the full review repeatedly.


### T-06 - One final convergence review and handoff
- status: done
- depends_on: ["T-05"]
- ownership: bootstrap-completion
- writes: ["docs/**", ".omp/**", "tools/**", "tests/**", ".github/**", "README.md", "CONTRIBUTING.md", "SECURITY.md", ".editorconfig", ".gitignore", "global.json", "LICENSE"]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: ["AC-01", "AC-09", "AC-12"]
- evidence: docs/specs/AIU-001-omp-bootstrap/verification.md; independent replacement review PASS with no material findings and three deferred MINORs; actual local checks and native transitions verified; scope closes without remote publication or product implementation

**Work:** Reconcile actual state, run required tests and the validator, and obtain the single independent full convergence review. Apply Git/PR gates only to the actual authorized repository and identity. Close only verified scope and present AIU-002/003 next.

**Verification:** The evidence matrix, repository state and final report agree. Separate unexecuted/blocked checks from passes. Publish only verified launch commands. Do not start Windows application implementation or the entire backlog under this goal.


## Handoff
- completed: T-01, T-02, T-03, T-04, T-05, T-06.
- next: Owner selection/authorization of AIU-002 for the runnable Windows package; AIU-003 supplies Codex feasibility evidence. No automatic next-goal start.
- blocked_by: No unresolved local material finding. Remote CI is not executed; main protection and private reporting are absent. No push or merge is claimed.
- verify: Validator 49/49 pass; canonical documents valid; Bun workflow/patch tests 11/11 pass with 74 assertions; direct formatting checks pass for both owned C-sharp projects. Review PASS; three MINORs are deferred in docs/backlog.md. Native evidence and recovery history are in docs/workflow/environment.md.
