---
id: AIU-043
schema_version: 1
---

# AIU-043 implementation plan

Goal: useful, bounded local diagnostic evidence without secrets or routine noise.
Spec: [spec.md](spec.md); architecture: [design.md](design.md).
The owner selected implementation and a concise agent logging rule on 2026-10-03.
Execution is sequential by the primary, using executing-plans and test-driven-development;
One Astra low read-only reviewer is authorized by the current owner request. Work directly on main under CONTRIBUTING.md.

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
- status: in-progress
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

Completion work resumed on 2026-10-03 from `99efa21`. The owner authorized one
Astra low independent reviewer and subsequently authorized live checks through
existing AI Usage sessions only; no CLI credential access or new sign-in.
Focused review found lost original transport/parser exception evidence. Targeted
regressions reproduced it and now pass with sanitized capture failure metadata.
Clock-controlled hourly/mixed-age retention now passes. Injected partial writes
exposed lost first-recovery events; rotating the failed writer fixes that regression.
Infrastructure: 590 passed; targeted storage faults: 4 passed. Release build passed.
Live Codex and Copilot quota reads succeeded; Claude/Antigravity had no stored session.

Next action: execute isolated Release XAML/Host startup and Host disposal probes,
then debugger binding/Open logs smoke, final package validation and acceptance review.
No feature completion claim yet; final evidence and task dispositions remain pending.
