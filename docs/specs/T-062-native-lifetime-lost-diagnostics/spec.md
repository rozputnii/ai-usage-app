---
id: T-062
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-10 (fix confirmed defects of the four classes found in the T-056/T-057/T-058 run)
---
# Tray icon lifetime and repeated-failure counts that get lost

## Problem
- Current: three defects left open by the T-056 review were confirmed on `17c79c7`; a sweep of native handles,
  disposables and event subscriptions in src/windows found no other leak.
  - Tray icon (T-056 M1): assigning `TaskbarIcon.Icon` runs H.NotifyIcon 2.4.1's `OnIconChanged`, which disposes the
    old icon first and then ignores the result of the shell update (read from the library's IL). When the update
    fails (mostly while Explorer is down), the library keeps the destroyed handle, re-adds it after Explorer's
    restart, and the window still reports a recovered tray.
  - Lost counts (T-056 M3 and the whole-run Minor 1): `FileDiagnostics` writes a pending repeated-failure count only
    on recovery or a graceful stop. On a fatal exit, a kill or an auto-update with `ForceTargetAppShutdown` the
    count is lost (reproduced: four failures then `Fatal` leave only the detailed record).
  - Streak key (T-056 M2): a streak is keyed by event code only, so a different exception later in the streak is
    counted but never logged in detail, and the summary does not name the detailed record's `incidentId`
    (reproduced).
- Expected: the tray never holds a destroyed icon; a pending count reaches the log on a fatal exit and at least
  hourly; each distinct failure in a streak gets its own detailed record, and each summary names the incident it
  counts.
- Unchanged: the tray glyph's appearance, the detailed failure record's content and correlation, the policy that
  routine success is not logged, and every other event's behaviour.

## Requirements
- R-01: The window owns the shown tray icon and updates the shell with `TaskbarIcon.UpdateIcon(Icon)`. It disposes
  the new icon when the update fails or throws, and the previous icon only after a successful update; it releases
  the current icon after the tray at exit. A failed update writes no record and does not report recovery.
- R-02: A repeated-failure streak is keyed by event code and failure signature (exception type and HResult, no
  message). A new signature in a streak gets its own detailed record with a fresh `incidentId`.
- R-03: Each summary carries the `incidentId` of its detailed record, with `suppressedCount`, `firstAt` and
  `lastAt` as today; it is written only when the count is above zero.
- R-04: Pending counts are written as summaries, with the streak kept open, by the hourly maintenance sweep, before
  the `UpdateInstallStarted` record (an install can force-close the app), and by `Fatal` inside its existing
  two-second drain; recovery and stop keep their behaviour. A kill can lose at most the counts since the last sweep.
- R-05: docs/workflow/logging.md describes the summary's `incidentId`, the per-signature detail and the hourly and
  fatal flush.

## Acceptance criteria
- AC-01: A Windows-TFM test drives the icon-ownership code with a failing update and shows that the shown icon's
  handle stays valid and the new icon is disposed; it fails with the library's order (dispose first, then update).
  Thousands of alternating failed and successful updates keep the GDI and USER counts flat.
- AC-02: `FileDiagnosticsTests` show that a pending count is written on `Fatal` and on the hourly sweep (with the
  streak kept open and no second detailed record), and that a new exception type in a streak gets its own
  detailed record and each summary carries its incident's `incidentId`. Each test fails at base. The three T-056
  repeated-failure tests pass unchanged.
- AC-03: The logging policy holds: no message text, path or identity in the new fields, no record per attempt,
  and the critical write stays outside the ordinary queue lock (focused T3 review).
- AC-04: C4, C5, C7 with 0 warnings, C6, C2 and C8 under the desktop lock pass.

## Out of scope
- A guaranteed final record on a kill or power loss (not achievable; logging.md already says so).
- Logging a failed shell update (an expected transient while Explorer restarts).
- Other events' coalescing, and any new logging framework, wrapper or dependency.
