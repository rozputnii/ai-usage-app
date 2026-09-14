---
id: AIU-027
schema_version: 1
---
# AIU-027 implementation plan

Goal: testable existing project boundaries with preserved product behavior. Execute sequentially under CONTRIBUTING; primary owns all changes. Spec: [spec.md](spec.md); design: [design.md](design.md). No new dependencies or production projects. Commit/push only after required verification passes.

### T-01 - Core workflow and session contract
- status: done
- depends_on: []
- acceptance: AC-01, AC-04
- evidence: docs/specs/AIU-027-architecture-refinement/verification.md

- [x] Add independent workflow tests for first run, cached resume, overlap, cancellation and shutdown drain.
- [x] Extract credential-free state/contract into Core; implement dashboard workflow and adapt Infrastructure session registration.
- [x] Run new workflow tests and existing Infrastructure regressions offline.

### T-02 - Presentation and desktop composition
- status: done
- depends_on: [T-01]
- acceptance: AC-02, AC-03, AC-04
- evidence: docs/specs/AIU-027-architecture-refinement/verification.md

- [x] Test the actual view-model sources with injected session workflow, resources and dispatcher.
- [x] Remove concrete session/static App resource dependencies from presentation and wire explicit dispatcher access.
- [x] Drain dashboard work before Host stop/disposal; retain close/hide/restore ownership in App.
- [x] Add dependency regression checks and include the independent executable in local/CI checks.

### T-03 - Review and packaged verification
- status: done
- depends_on: [T-02]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05
- evidence: docs/specs/AIU-027-architecture-refinement/verification.md

- [x] Review the integrated diff and perform required independent review.
- [x] Run regression/document checks, build a fresh package, and run/inspect disposable-guest lifecycle evidence.
- [x] Record results and prepare the verified task branch for policy-compliant publication.

## Handoff

Base: `4f2d2fb`. Initial tracked/untracked checkout clean. Completed: local regressions, independent review and final packaged guest lifecycle verification pass; no acceptance blocker remains. Publication is recorded by the task branch commit and upstream reference under CONTRIBUTING. No follow-on feature is selected.
