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
- status: done
- depends_on: []
- acceptance: AC-02, AC-05, AC-07, AC-08, AC-09, AC-10, AC-11
- evidence: docs/specs/AIU-043-file-logging/verification.md

- [x] Add failing Infrastructure tests for JSON events, safe exception projection,
  retention/cleanup and forced critical persistence.
- [x] Implement Diagnostics/FileDiagnostics, DiagnosticFiles and SafeException;
  preserve IDiagnosticSink fixed-code callers, add bounded context/operations.
- [x] Pin Serilog Host/file packages, verify tests, save an explicitly incomplete checkpoint.

### T-02 - Provider evidence
- status: done
- depends_on: [T-01]
- acceptance: AC-01, AC-03, AC-04, AC-05, AC-10, AC-12
- evidence: docs/specs/AIU-043-file-logging/verification.md

- [x] Test pre-projection success/error capture, unknown/nested canaries, malformed/
  oversized/cancelled replies, precision and unchanged request counts.
- [x] Implement endpoint policies and capture at ProviderHttp using the existing buffer;
  pass the shared diagnostics instance through ProviderTransportOptions.
- [x] Inventory every existing client route; run provider regressions and save checkpoint.

### T-03 - Windows and console integration
- status: done
- depends_on: [T-01, T-02]
- acceptance: AC-01, AC-06, AC-07, AC-08, AC-12, AC-13
- evidence: docs/specs/AIU-043-file-logging/verification.md

- [x] Connect early startup, managed/UI crash hooks, operation/dispatcher/lifetime
  boundaries, console composition and isolated Demo/Ledger paths.
- [x] Add bounded log preview/Open logs to the existing diagnostic surface.
- [x] Exercise child-process failure, ordinary launch/interactions/exit and Release
  limitations; compare enabled/disabled synthetic workload.

### T-04 - Policy, verification and review
- status: done
- depends_on: [T-01, T-02, T-03]
- acceptance: AC-01, AC-13, AC-14, AC-15
- evidence: docs/specs/AIU-043-file-logging/verification.md

- [x] Add the requested AGENTS logging rule, English reading guide and reconcile
  D-137/security lifecycle with the selected feature.
- [x] Run Infrastructure/presentation regressions, package build, applicable Windows
  smoke, document validation and diff review; record exact evidence and limitations.
- [x] Obtain focused independent review; resolve findings with targeted checks.

## Completion

Completed on 2026-10-03. Original implementation: `44cee2e..5c60dae`;
completion fixes: `c25a01d`; Windows probes and smoke: `4362346`.
The owner authorized one Astra low reviewer and existing-session live checks only.
No CLI credentials or new sign-in were used.

Focused independent review found lost original transport/parser failure evidence.
Regressions reproduced it and pass with safe capture details. Supplemental focused
review of new projection/storage/console boundaries returned PASS, no material findings.
Fault injection also found a partial-line recovery defect, fixed with writer rotation.
Final verification records 590 Infrastructure tests, 262 presentation tests, targeted
storage/restart checks, ordinary Windows smoke, binding with/without a debugger,
XAML/Host startup and late Host disposal, Open logs in product/Demo, and MSIX build.
Live Codex/Copilot responses correlate with committed captures and parser outcomes;
Claude/Antigravity are NOT_RUN because neither app-owned store has their sessions.

Next action: none within AIU-043. A future owner-authorized connection can extend
live evidence for the unavailable providers; no authentication or follow-up is scheduled.
