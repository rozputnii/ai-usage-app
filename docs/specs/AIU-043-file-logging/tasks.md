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
- evidence: not-run

- [ ] Add failing Infrastructure tests for JSON events, safe exception projection,
  retention/cleanup and forced critical persistence.
- [ ] Implement Diagnostics/FileDiagnostics, DiagnosticFiles and SafeException;
  preserve IDiagnosticSink fixed-code callers, add bounded context/operations.
- [ ] Pin Serilog Host/file packages, verify tests, save an explicitly incomplete checkpoint.

### T-02 - Provider evidence
- status: pending
- depends_on: [T-01]
- acceptance: AC-01, AC-03, AC-04, AC-05, AC-10, AC-12
- evidence: not-run

- [ ] Test pre-projection success/error capture, unknown/nested canaries, malformed/
  oversized/cancelled replies, precision and unchanged request counts.
- [ ] Implement endpoint policies and capture at ProviderHttp using the existing buffer;
  pass the shared diagnostics instance through ProviderTransportOptions.
- [ ] Inventory every existing client route; run provider regressions and save checkpoint.

### T-03 - Windows and console integration
- status: pending
- depends_on: [T-01, T-02]
- acceptance: AC-01, AC-06, AC-07, AC-08, AC-12, AC-13
- evidence: not-run

- [ ] Connect early startup, managed/UI crash hooks, operation/dispatcher/lifetime
  boundaries, console composition and isolated Demo/Ledger paths.
- [ ] Add bounded log preview/Open logs to the existing diagnostic surface.
- [ ] Exercise child-process failure, ordinary launch/interactions/exit and Release
  limitations; compare enabled/disabled synthetic workload.

### T-04 - Policy, verification and review
- status: pending
- depends_on: [T-01, T-02, T-03]
- acceptance: AC-01, AC-13, AC-14, AC-15
- evidence: not-run

- [ ] Add the requested AGENTS logging rule, English reading guide and reconcile
  D-137/security lifecycle with the selected feature.
- [ ] Run Infrastructure/presentation regressions, package build, applicable Windows
  smoke, document validation and diff review; record exact evidence and limitations.
- [ ] Record focused independent review as BLOCKED if no authorized reviewer exists;
  do not claim self-review is independent or mark the feature complete.

## Handoff

Base: main, clean working tree at selection. No live-provider access authorized.
Next action: write and run T-01 diagnostic storage regression tests.
Required independent implementation review is unavailable with the current tools and
disabled Codex subagents; implementation/checkpoints do not claim review approval.
