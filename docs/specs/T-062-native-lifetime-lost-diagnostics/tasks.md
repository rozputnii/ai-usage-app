---
id: T-062
schema_version: 1
---
# Tray icon lifetime and lost repeated-failure counts: tasks

The worker plan is in [T-059 tasks](../T-059-stale-ui-smokes/tasks.md).

### T-062.1 - The window owns the shown tray icon
- status: ready
- depends_on: []
- ownership: tray-icon-lifetime
- writes: ["src/windows/AiUsage.Windows/Platform/TrayMark.cs", "tests/windows/AiUsage.Windows.Tests/TrayMarkTests.cs", "tests/windows/AiUsage.Windows.Tests/AiUsage.Windows.Tests.csproj"]
- shared: []
- parallel: true
- isolation: required
- agent: aiu-implementer
- acceptance: ["AC-01", "AC-04"]
- evidence: not-run

W2, together with T-061.2, which owns LedgerWindow.xaml.cs: R-01.

### T-062.2 - Repeated-failure streaks: signature, incident and flush
- status: ready
- depends_on: []
- ownership: diagnostics
- writes: ["src/windows/AiUsage.Infrastructure/Diagnostics/FileDiagnostics.cs", "tests/windows/AiUsage.Infrastructure.Tests/FileDiagnosticsTests.cs", "docs/workflow/logging.md", "docs/specs/T-062-native-lifetime-lost-diagnostics/spec.md"]
- shared: []
- parallel: true
- isolation: required
- agent: aiu-implementer
- acceptance: ["AC-02", "AC-03", "AC-04"]
- evidence: not-run

W3: R-02 to R-05. T3 (logging and diagnostics).

## Next action
Dispatch W2 and W3.
