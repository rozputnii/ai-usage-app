---
id: AIU-043
schema_version: 1
---

# AIU-043 implementation plan

Goal: useful, bounded local diagnostic evidence without secrets or routine noise.
Spec: [spec.md](spec.md); architecture: [design.md](design.md).
The owner selected implementation and a concise agent logging rule on 2026-10-03.
Execution is sequential by the primary, using executing-plans and test-driven-development;
Codex subagents remain disabled. Work directly on main under CONTRIBUTING.md.

## Constraints and review focus

Core owns credential-free codes/context; Infrastructure projects exceptions and HTTP
responses before Serilog or file output; Windows owns lifecycle and UI hooks.
Retain Trace 72 hours, application/responses 168 hours, critical one calendar month.
Unknown values and dynamic names, malformed bodies, exception messages and private
paths must not leak. Logger failure must not change provider or shutdown behavior.
Test month ends/clock reversal, links/unknown files, queue pressure, nested secrets,
and fatal handling independently of the ordinary writer.

### T-01 - Bounded diagnostic storage
- status: in-progress
- depends_on: []
- acceptance: AC-02, AC-05, AC-07, AC-08, AC-09, AC-10, AC-11
- evidence: verification.md; coverage.md

- [x] Add failing Infrastructure tests for JSON events, safe exception projection,
  retention/cleanup and forced critical persistence.
- [x] Implement Diagnostics/FileDiagnostics, DiagnosticFiles and SafeException;
  preserve IDiagnosticSink fixed-code callers, add bounded context/operations.
- [x] Pin Serilog Host/file packages, verify tests, save an explicitly incomplete checkpoint.

### T-02 - Provider evidence
- status: in-progress
- depends_on: [T-01]
- acceptance: AC-01, AC-03, AC-04, AC-05, AC-10, AC-12
- evidence: verification.md; coverage.md

- [x] Test pre-projection success/error capture, unknown/nested canaries, malformed/
  oversized/cancelled replies, precision and unchanged request counts.
- [x] Implement endpoint policies and capture at ProviderHttp using the existing buffer;
  pass the shared diagnostics instance through ProviderTransportOptions.
- [x] Inventory every existing client route; run provider regressions and save checkpoint.

### T-03 - Windows and console integration
- status: in-progress
- depends_on: [T-01, T-02]
- acceptance: AC-01, AC-06, AC-07, AC-08, AC-12, AC-13
- evidence: verification.md; coverage.md

- [x] Connect early startup, managed/UI crash hooks, operation/dispatcher/lifetime
  boundaries, console composition and isolated Demo/Ledger paths.
- [x] Add bounded log preview/Open logs to the existing diagnostic surface.
- [x] Exercise child-process failure, ordinary launch/interactions/exit and Release
  limitations; compare enabled/disabled synthetic workload.

### T-04 - Policy, verification and review
- status: blocked
- depends_on: [T-01, T-02, T-03]
- acceptance: AC-01, AC-13, AC-14, AC-15
- evidence: verification.md; coverage.md

- [x] Add the requested AGENTS logging rule, English reading guide and reconcile
  D-137/security lifecycle with the selected feature.
- [x] Run Infrastructure/presentation regressions, package build, applicable Windows
  smoke, document validation and diff review; record exact evidence and limitations.
- [x] Record focused independent review as BLOCKED if no authorized reviewer exists;
  do not claim self-review is independent or mark the feature complete.

## Handoff

Base: main, clean working tree at selection. No live-provider access authorized.
Next action: obtain a focused independent read-only review of the implementation diff
from 44cee2e to 5c60dae, using spec.md, coverage.md and the final
verification record. Do not re-enable Codex subagents without owner direction.
Required independent implementation review is unavailable with the current tools and
disabled Codex subagents; implementation/checkpoints do not claim review approval.

Implementation steps above are delivered; checked steps are not blanket acceptance.
T-01/T-03 retain unrun failure-injection/debugger checks listed in verification.md;
T-02 shares those storage acceptance dependencies. T-04 is blocked by required
independent review. No live-provider evidence or complete-feature verdict is claimed.
