---
id: AIU-008
schema_version: 1
---
# Copilot execution

### T-01 - Establish provider contract and registration
- status: done
- depends_on: []
- acceptance: AC-01
- evidence: docs/specs/AIU-008-copilot-integration/verification.md

Inspect stable OMP and official documentation, resolve PD-008-01 and establish the chosen registration contract before production authentication.

### T-02 - Shared authentication and quota implementation
- status: done
- depends_on: [T-01]
- acceptance: AC-02, AC-03
- evidence: docs/specs/AIU-008-copilot-integration/verification.md

Implement the UI-independent protocol and parser with synthetic tests and a shared console verification surface.

### T-03 - Protected lifecycle and Windows integration
- status: done
- depends_on: [T-02]
- acceptance: AC-04, AC-05
- evidence: docs/specs/AIU-008-copilot-integration/verification.md

Add protected provider state, transient device challenge presentation and native quota mapping. Preserve existing providers and demo isolation.

### T-04 - Integrated verification and publication
- status: in-progress
- depends_on: [T-03]
- acceptance: AC-06, AC-07
- evidence: docs/specs/AIU-008-copilot-integration/verification.md

Run required regressions/builds, local Windows smoke, owner-authorized live lifecycle and focused independent review; publish only completed verified scope.

## Handoff, 2026-09-18

Base: clean main at bc67aae. Branch: codex/aiu-008-copilot-integration. AIU-010 remains paused. The owner selected only OMP and explicitly authorized independent final checks using the existing signed-in GitHub browser, including consent. No source CLI credentials were read.

Implemented shared OMP device authorization, quota parsing, app-owned protected state, console commands and live Windows integration. Infrastructure 162/162 and Presentation 120/120 pass. Release unpackaged Windows and console builds pass with zero warnings/errors; unsigned MSIX 2026.9.1801.0 passes with one external symbol-tool warning. Live connection, quota refresh, restart resume, disconnect, cancellation and reconnect passed in an isolated profile. Focused review found four material defects; the primary fixed identity substitution, source-field loss, group ordering and inferred provider restriction, with regression coverage.

Final demo smoke passed 7/7; post-fix live resume/refresh and quota presentation passed. Final document validation and diff check passed. Exact next action: commit and push the reviewed task branch under CONTRIBUTING, then record publication. No merge or release is authorized.
