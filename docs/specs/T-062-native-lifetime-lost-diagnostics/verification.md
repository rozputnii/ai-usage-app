# T-062 verification

Code reference: T-062.1 `f094f68`, `1878061` and `b867d97`, merged into `main` as `e56ec3e`; T-062.2 `3564996`,
`49c7381` and `bee1252`, merged into `main` as `180677c`. Environment: the owner's Windows 11 Pro 10.0.26200 desktop,
.NET 10, Release, desktop unlocked. Date: 2026-10-10. Part of the parallel sweep recorded in the
[T-059 execution ledger](../T-059-stale-ui-smokes/verification.md#execution-ledger). Evidence is in the worker
worktrees' git-ignored `.ai-usage-local/T-061/` and `.ai-usage-local/T-062/`.

## Root cause

- Tray icon: the H.NotifyIcon 2.4.1 IL (read by lane D, the worker and the reviewer) shows `TaskbarIcon.OnIconChanged`
  calls `old.Dispose()`, then `UpdateIcon(new)` and discards its result. When the shell update fails, the core
  `TrayIcon` keeps the previous handle, which is already destroyed, and `OnTaskbarCreated` re-adds it after an
  Explorer restart.
- Lost counts: `FileDiagnostics` wrote a pending repeated-failure count only from `Recovered` or `StopAsync`.
  Reproduced at base: four `RepeatedFailure` calls and then `Fatal` left only the detailed record.
- Streak key: the streak was keyed by event only, so a second exception type was counted without detail, and the
  summary had no `incidentId` (reproduced).

The sweep found no other native-handle, disposable or subscription leak in src/windows. It examined and rejected,
among others: `TrayGlyph.Create`'s GDI+ objects (all disposed), undisposed `Process.Start` results (rare actions,
released by the finalizer), the linked `CancellationTokenSource`s in `AccountService` (disposed under the lock), the
single-instance window and service subscriptions (they live for the process), and the logger's queue-overflow and
capture-loss paths (counted in `LoggerHealth`).

## Checks

| Check | Tree | Result |
| --- | --- | --- |
| C4 Infrastructure suite | merged `180677c`; `e56ec3e` | PASS, 926/926 (922 before, plus 4); PASS, 926/926 |
| C5 Presentation suite | merged `180677c`; `e56ec3e` | PASS, 335/335; PASS, 337/337 |
| C7 App build (Release, unpackaged) | both | PASS, 0 warnings, 0 errors |
| `TrayIconOwnerTests`, `TrayGlyphHandleTests` | merged `e56ec3e`; `016043d` | PASS, 4/4 each time |
| C6, C2, C3 | both | PASS |
| C8 Launch smoke | merged `180677c`; `e56ec3e`; `016043d` | PASS 3/3 each, under the desktop lock |

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `TrayIconOwnerTests.AFailedUpdateKeepsTheShownIconAndReleasesTheNewOne` failed with the library's order (dispose first, then update): "The shown icon's handle was destroyed by a failed update"; with `TrayIconOwner` it passes, and a throwing update disposes the new icon and rethrows. 3,000 alternating failed and successful updates kept GDI and USER growth within 20 each. The reviewer reproduced the red. |
| AC-02 | PASS | Four new `FileDiagnosticsTests` (pending count on `Fatal`; hourly checkpoint with the streak kept open; count before `UpdateInstallStarted`; one detailed record per exception type with summaries naming their `incidentId`) fail at base for the expected reasons, confirmed by the reviewer in a base worktree, and pass. The three T-056 tests are unchanged and pass. |
| AC-03 | PASS | Focused T3 review: the critical write stays outside `gate` and the queue, the added work in `Fatal` runs inside the existing two-second bound, the failure signature is only an in-memory key, no record is written per attempt, and the hourly checkpoint writes at most one summary per open streak and signature. |
| AC-04 | PASS | Checks above. |

## Review

- T-062.2 (`179c0b5..49c7381`), per-task and focused T3 review by `aiu-reviewer`: approve, with 0 Critical,
  0 Important and 5 Minor findings. Fixed in `bee1252`, re-checked by the primary on the fixed lines:
  - M1: `Fatal` also writes the pending binding/dispatch summaries; kept on purpose (those counts were lost on a
    fatal exit too) and documented in logging.md.
  - M2: the count written before an update install was not in R-04; the spec now says so, and logging.md is
    rewrapped.
  - M3: `firstAt` is now defined in logging.md.
  - M4: a null exception in `RepeatedFailure` now counts as a lost record instead of throwing into the caller.
  - M5, recorded: the worker first called a crash-test timeout a base failure; the reviewer could not reproduce
    that at base (see the T-061 checks note), so it is recorded as host variance.
- T-062.1 was reviewed with T-061.2: approve, no findings; the reviewer checked the H.NotifyIcon facts in the
  package IL ([T-061 review](../T-061-unchanged-state-work/verification.md#review)).
- `/security-review` could not run on the target diff (it reviews only the current checkout's pending changes); the
  reviewers applied its questions by hand and found nothing.

The whole-run review is in the [T-061 verification](../T-061-unchanged-state-work/verification.md#whole-run-review).

## Not run

| Item | Reason |
| --- | --- |
| In the installed app: the tray icon keeps showing the current colour after Explorer restarts, and the logs show at most hourly TrayFailure summaries carrying their incidentId | Post-deploy owner check (R-190) |
| A guaranteed final count on a kill or power loss | Not achievable; up to an hour of counts can be lost (logging.md) |
