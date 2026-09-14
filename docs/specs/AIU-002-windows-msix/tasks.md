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
- status: done
- depends_on: [T-01]
- ownership: native product
- writes: [src/windows/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-02, AC-03]
- evidence: docs/specs/AIU-002-windows-msix/verification.md

### T-03 - Prove native routing candidate
- status: done
- depends_on: [T-02]
- ownership: standalone routing
- writes: [spikes/windows/AIU-002-routing/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-05]
- evidence: docs/specs/AIU-002-windows-msix/verification.md

### T-04 - Produce reusable development package
- status: done
- depends_on: [T-02]
- ownership: local packaging
- writes: [tools/windows/Build-Package.ps1]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-04]
- evidence: docs/specs/AIU-002-windows-msix/verification.md

### T-05 - Implement disposable guest and UI smoke
- status: done
- depends_on: [T-02]
- ownership: guest smoke
- writes: [tests/windows/AiUsage.Windows.Tests/**, tools/windows/Invoke-PackageSmoke.ps1]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-02, AC-03, AC-04]
- evidence: docs/specs/AIU-002-windows-msix/verification.md

### T-06 - Wire CI and verify integrated acceptance
- status: done
- depends_on: [T-03, T-04, T-05]
- ownership: integration and verification
- writes: [.github/workflows/validation.yml, docs/specs/AIU-002-windows-msix/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
- evidence: docs/specs/AIU-002-windows-msix/verification.md

## Handoff
All listed internal tasks have retained evidence. Read verification.md for observed results and limitations, and docs/backlog.md for feature status. The old session chronology is preserved in docs/decisions/superseded.md.

Next action: wait for the owner's next selected scope; this migration selects no product feature. Do not infer an account, guest, host or remote action from historical approval.
