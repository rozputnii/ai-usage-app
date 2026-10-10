---
id: T-056
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner selection, 2026-10-10
---
# Tray icon handle leak and TrayFailure log flood

## Problem
- Current: `TrayGlyph.Create` returned `Icon.FromHandle(bitmap.GetHicon())`, and `LedgerWindow.UpdateTrayGlyph` never
  released the icon it replaced. Every redraw (after each view-model or card change) leaked one icon: 3 GDI objects and
  1 USER object, measured. After about 3,300 redraws the process reached its GDI quota, and every later redraw failed
  with `ExternalException` (hResult -2147467259) until restart. The installed app's logs (2026.10.837.0 and earlier) show
  5,778 such TrayFailure errors in three sessions, starting 5.5 to 8 hours after launch, one record per attempt (about
  three a minute).
- Expected: redraws keep the handle count flat for days of uptime, and a tray failure that repeats is logged once in
  detail plus a count.
- Unchanged: the tray glyph's appearance and colours, and all other tray behavior.

## Requirements
- R-01: `TrayGlyph.Create` returns an icon that owns its handle; the handle `GetHicon` creates is destroyed with
  `DestroyIcon`. `UpdateTrayGlyph` disposes whichever icon the tray no longer holds: the replaced one after a
  successful assignment, the new one if the assignment fails.
- R-02: `FileDiagnostics.RepeatedFailure` writes the existing detailed failure record (operation correlation, safe
  exception projection) on the first call and only counts later calls (first time, last time, count).
  `FileDiagnostics.Recovered` writes one Warning record of the same event with `suppressedCount`, `firstAt` and
  `lastAt` when repeats were counted, then resets; a pending count is written at stop. Tray failures use it through
  `ApplicationDiagnostics.TrayFailure`, and `UpdateTrayGlyph` reports `TrayRecovered` after a successful redraw.
  Other events keep their behavior.

## Acceptance criteria
- AC-01: A test shows the process handle count (GDI and USER objects, `GetGuiResources`) stays flat over thousands of
  tray glyph updates: each created HICON is destroyed with `DestroyIcon` once the tray no longer uses it, and the
  replaced icon is disposed. The test fails at the base behavior. It is
  `AiUsage.Windows.Tests.TrayGlyphHandleTests` in the UI test project, the only Windows-TFM test project; C4, C5 and CI
  do not run it. Command: `dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-restore -- -noLogo -class "AiUsage.Windows.Tests.TrayGlyphHandleTests"`.
  It needs no interactive desktop.
- AC-02: A recurring tray failure produces one detailed record, with its operation correlation and no exception text
  beyond what the logging policy allows, plus one coalesced count through `FileDiagnostics` (on recovery or at stop),
  not one record per attempt. A success after failures resets the coalescing, so a later failure is logged in detail
  again. Covered by unit tests in `FileDiagnosticsTests`.
- AC-03: C4 Infrastructure suite, C5 Presentation suite, C7 Release build with 0 warnings, C6 and C2 pass; C8 launch
  smoke passes under the desktop lock before the push to `main`.

## Out of scope
- Skipping redraws when the colour is unchanged (a possible follow-up).
- Coalescing for other events, and any new logging framework, wrapper or dependency.
- The owner's check of the installed app's logs after days of uptime (post-deploy, R-190).
