---
id: T-NEW
schema_version: 1
---
# Defect-class sweep: worker plan

The owner asked on 2026-10-10 for a parallel sweep of four defect classes found in the T-056/T-057/T-058 run and
authorized fixing the confirmed instances, registering them as items and merging the verified fixes. Phase 1
research (four read-only lanes on `17c79c7`) produced the confirmed rows in the four specs: T-NEW (stale UI
smokes), T-NEW-2 (fragile smoke-harness input), T-NEW-3 (redundant work on unchanged state) and T-NEW-4 (tray icon
lifetime and lost repeated-failure counts). Each item's tasks.md holds its task blocks; the validator ties a task's
acceptance to its own spec.

## Workers

| Worker | Tasks | Write set (no overlap) |
| --- | --- | --- |
| W1 harness | T-NEW.1, then T-NEW-2.1 | `tests/windows/AiUsage.Windows.Tests/` smoke classes `SmokeKit.cs`, `LedgerSmoke.cs`, `LedgerActivationSmoke.cs`, `LedgerWindowSizeSmoke.cs`, `CardEditingSmoke.cs`, `RefreshIntervalSmoke.cs`, `DesktopTestEnvironment.cs`; both specs' status |
| W2 window and tray | T-NEW-3.2, T-NEW-4.1 | `LedgerWindow.xaml.cs`, `LedgerViewModel.cs`, `Controls/Ledger/LedgerTrayWindow.cs`, new `Platform/TrayMark.cs`; `AiUsage.Windows.Tests.csproj`, new `TrayMarkTests.cs`; new Presentation `TrayToneTests.cs` |
| W3 diagnostics | T-NEW-4.2 | `FileDiagnostics.cs`, `FileDiagnosticsTests.cs`, `docs/workflow/logging.md`; T-NEW-4 spec status |
| W4 presentation data | T-NEW-3.1 | `LiveLedgerSource.cs`, `LimitCardViewModel.cs`, `LedgerSettingsViewModel.cs`; Presentation `LiveLedgerSourceTests.cs`, new `UnchangedStateTests.cs`; T-NEW-3 spec status |

Desktop smokes, including each worker's C8, run under the desktop lock, which serializes them.

### T-NEW.1 - Stale cap-field lookup in the spending smoke
- status: ready
- depends_on: []
- ownership: smoke-harness
- writes: ["tests/windows/AiUsage.Windows.Tests/LedgerSmoke.cs", "docs/specs/T-NEW-stale-ui-smokes/spec.md"]
- shared: []
- parallel: true
- isolation: required
- agent: aiu-implementer
- acceptance: ["AC-01", "AC-02"]
- evidence: not-run

The cap stage finds "Cap amount in USD" (Edit) in the app's own windows, re-found per query. Runs in W1 before
T-NEW-2.1, which also edits LedgerSmoke.cs.

## Next action
Merge these records into `main`, then dispatch W1 to W4 from fresh `origin/main`.

## Review focus
- W1: the moved focus and click helpers keep T-058's behaviour; a guard is never added after the click it guards;
  no assertion is weakened and the case count is unchanged.
- W2: the tray tone is set after the cards update; a failed redraw is retried; icon ownership never disposes the
  shown icon before a successful update, and exit releases it after the tray.
- W3: the critical write stays outside the ordinary queue lock; the summary never carries message text; the
  T-056 tests stay unchanged.
- W4: skipping the second tick rebuild changes nothing when an account was due; kept cap rows still refresh
  `CanAct`; the card view still updates on a single notification.
