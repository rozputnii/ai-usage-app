---
id: T-061
schema_version: 1
---
# Redundant work on unchanged state: tasks

The worker plan is in [T-059 tasks](../T-059-stale-ui-smokes/tasks.md).

### T-061.1 - Presentation data: tick, card notification and cap rows
- status: ready
- depends_on: []
- ownership: presentation-data
- writes: ["src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerSource.cs", "src/windows/AiUsage.Windows/Features/Ledger/LimitCardViewModel.cs", "src/windows/AiUsage.Windows/Features/Ledger/LedgerSettingsViewModel.cs", "tests/windows/AiUsage.Presentation.Tests/LiveLedgerSourceTests.cs", "tests/windows/AiUsage.Presentation.Tests/UnchangedStateTests.cs", "docs/specs/T-061-unchanged-state-work/spec.md"]
- shared: []
- parallel: true
- isolation: required
- agent: aiu-implementer
- acceptance: ["AC-02", "AC-03", "AC-04", "AC-06"]
- evidence: not-run

W4: R-02, R-03 and R-06.

### T-061.2 - Window: tray tone, history, tray miniature and WindowShown
- status: ready
- depends_on: []
- ownership: window-tray
- writes: ["src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerWindow.xaml.cs", "src/windows/AiUsage.Windows/Features/Ledger/LedgerViewModel.cs", "src/windows/AiUsage.Windows/Controls/Ledger/LedgerTrayWindow.cs", "tests/windows/AiUsage.Presentation.Tests/TrayToneTests.cs"]
- shared: []
- parallel: true
- isolation: required
- agent: aiu-implementer
- acceptance: ["AC-01", "AC-05", "AC-06"]
- evidence: not-run

W2, together with T-062.1 (the same method): R-01, R-04, R-05 and R-07. R-07 is a logging change (T3 focus).

## Next action
Dispatch W4 and W2.
