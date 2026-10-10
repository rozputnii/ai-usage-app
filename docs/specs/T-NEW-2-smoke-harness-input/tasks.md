---
id: T-NEW-2
schema_version: 1
---
# Fragile smoke-harness input: tasks

The worker plan is in [T-NEW tasks](../T-NEW-stale-ui-smokes/tasks.md).

### T-NEW-2.1 - Robust input in the ordinary smoke classes
- status: ready
- depends_on: []
- ownership: smoke-harness
- writes: ["tests/windows/AiUsage.Windows.Tests/SmokeKit.cs", "tests/windows/AiUsage.Windows.Tests/LedgerSmoke.cs", "tests/windows/AiUsage.Windows.Tests/LedgerActivationSmoke.cs", "tests/windows/AiUsage.Windows.Tests/LedgerWindowSizeSmoke.cs", "tests/windows/AiUsage.Windows.Tests/CardEditingSmoke.cs", "tests/windows/AiUsage.Windows.Tests/RefreshIntervalSmoke.cs", "tests/windows/AiUsage.Windows.Tests/DesktopTestEnvironment.cs", "docs/specs/T-NEW-2-smoke-harness-input/spec.md"]
- shared: []
- parallel: true
- isolation: required
- agent: aiu-implementer
- acceptance: ["AC-01", "AC-02", "AC-03", "AC-04"]
- evidence: not-run

W1, after T-NEW.1. The focus fix comes first, because the fixed-point fallback clicks whatever window covers the
test window.

## Next action
Dispatch W1.
