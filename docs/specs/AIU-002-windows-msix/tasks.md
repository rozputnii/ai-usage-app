---
id: AIU-002
schema_version: 1
---
# AIU-002 execution tasks

### T-01 - Establish scoped execution and prerequisites
- status: done
- depends_on: []
- ownership: primary execution and records
- writes: [docs/specs/AIU-002-windows-msix/**, docs/product/goals.md, docs/backlog.md]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-04, AC-06]
- evidence: docs/specs/AIU-002-windows-msix/verification.md

### T-02 - Create native application and coordinated lifetime
- status: blocked
- depends_on: [T-01]
- ownership: native product
- writes: [src/windows/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-02, AC-03]
- evidence: verification.md AC-01 build PASS; installed AC-02/AC-03 runtime proof blocked on disposable guest

### T-03 - Prove native routing candidate
- status: blocked
- depends_on: [T-02]
- ownership: standalone routing
- writes: [spikes/windows/AIU-002-routing/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-05]
- evidence: verification.md AC-05; inspected isolated patch integrated, native restore/build/package PASS, guest routing NOT_RUN

### T-04 - Produce reusable development package
- status: blocked
- depends_on: [T-02]
- ownership: local packaging
- writes: [tools/windows/Build-Package.ps1]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-04]
- evidence: verification.md AC-04; signed 2026.9.1305.0 candidate retained, host verification fails closed on untrusted root, version reuse rejected without byte changes

### T-05 - Implement disposable guest and UI smoke
- status: blocked
- depends_on: [T-02]
- ownership: guest smoke
- writes: [tests/windows/AiUsage.Windows.Tests/**, tools/windows/Invoke-PackageSmoke.ps1]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-02, AC-03, AC-04]
- evidence: verification.md AC-02/AC-03/AC-04; self-contained publish PASS, missing-AUMID and host-guard failures observed, offline staging ready, actual guest NOT_RUN

### T-06 - Wire CI and verify integrated acceptance
- status: blocked
- depends_on: [T-03, T-04, T-05]
- ownership: integration and verification
- writes: [.github/workflows/validation.yml, docs/specs/AIU-002-windows-msix/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
- evidence: verification.md AC-06; local package/checks PASS, focused source review PASS with targeted fixes, remote CI NOT_RUN; final acceptance blocked on guest

## Handoff
Current branch is feature/AIU-002-windows-msix; existing AIU-001 edits and unrelated shortcut remain untouched. Routing worker patch was inspected and integrated; smoke worker timed out without a captured patch, and primary implemented smoke. No pending worker patch remains. Native production/routing builds and package generation succeeded. Final candidate is 2026.9.1305.0 under the ignored AIU-002 staging root; it is not accepted as installable until guest trust/launch proof. Focused review completed with scoped fixes and targeted verification. Next: obtain a clean Windows 11 24H2+ x64 disposable guest and run the staged offline harness, then the routing scenario. Sandbox and Hyper-V optional features are disabled. Do not enable features, elevate, reboot, install on host or import host trust without a specific owner decision. Do not start AIU-003.

Owner handoff decision: explicitly leave AIU-002 BLOCKED, retaining code, signed candidate and offline inputs. Do not attempt to enable Sandbox or obtain another environment autonomously. Resume guest verification only after a new owner decision supplies or authorizes the environment; no AIU-003 start.

Owner resumption: explicitly enabled AIU-002 continuation and authorized Windows Sandbox component enablement with required dependencies and UAC elevation. No automatic reboot, firmware changes, host app installation or host trust import. Next: enable Containers-DisposableClientVM with no-restart semantics, capture the actual result, and proceed only if the guest is ready; otherwise preserve state and report the restart/prerequisite gate.

Sandbox enablement result: elevated Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All -NoRestart completed with exit 0, state Enabled and RestartNeeded=true. No automatic restart occurred. Current blocker is the owner-controlled host reboot, not missing enablement permission. After the owner reports restart, verify Sandbox readiness and resume the staged offline guest scenario; do not rerun component installation or change host trust.
