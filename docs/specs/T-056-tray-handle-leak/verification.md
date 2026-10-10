# T-056 verification

Code reference: `7da2935`, `d183b46`, `d30916b` and `1f458a9`, merged into `main` as `eab3fea`.
Environment: the owner's Windows 11 Pro 10.0.26200 desktop, .NET 10, Release, desktop
unlocked. Date: 2026-10-10. This item ran as one of three parallel workers (T-056, T-057,
T-058) at the owner's request on 2026-10-10. Logs are in the worker worktree's git-ignored
`.ai-usage-local/T-056/`.

## Root cause

`TrayGlyph.Create` returned `Icon.FromHandle(bitmap.GetHicon())`. An icon made that way never
destroys its handle. `UpdateTrayGlyph` runs after every view-model property change and every card
change, and each redraw leaked 3 GDI objects and 1 USER object (measured). The process runs out of
its 10,000 GDI objects after about 3,300 redraws. After that, every redraw fails with
`ExternalException` E_FAIL, which matches the 5.5 to 8 hours of uptime seen in the installed
app's logs, and each failure was logged.

## Checks

The merge gate ran on the merged tree `eab3fea`, which also contains T-057 and T-058:

| Check | Result |
| --- | --- |
| C4 Infrastructure suite | PASS, 922/922. An earlier gate on `34c87dc` timed out once in `DiagnosticCrashTests.ManagedChildCrashLeavesCriticalStackBeforeTermination` while another app instance was starting on the host. Its rerun and this gate passed, and the crash path is not touched. |
| C5 Presentation suite | PASS, 330/330 |
| C7 App build (Release, unpackaged) | PASS, 0 warnings, 0 errors |
| `TrayGlyphHandleTests` (UI test project, `-class`) | PASS, 2/2 |
| C6 Diff check | PASS |
| C2 / C3 Document validation | PASS, 0 diagnostics |
| C8 Launch smoke | PASS, 3/3, first try, under the desktop lock against the rebuilt app. The first gate on `34c87dc` failed 2/3 on the pre-existing `LedgerSmoke.Focus` defect, so the push to `main` waited for T-058's harness fix (`3fb85f4`). |

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Before the fix (at `7da2935`), `TrayGlyphHandleTests` fails: over 3,000 updates, GDI objects grew by 9,001 and USER objects by 3,000. With the fix it passes, and a 20,000-update probe stayed flat at 8-12 GDI and 5-6 USER objects. The reviewer reproduced the failure on the base code in a separate worktree. A pixel comparison showed the cloned icon is identical to the base icon, soft edges included. |
| AC-02 | PASS | Three `FileDiagnosticsTests` cases cover a recurring failure: it writes one detailed record and then one counted summary on recovery, a failure after recovery is logged in detail again, and a pending summary is written at stop. The reviewer confirmed the old behaviour fails them, with 5, 3 and 3 records where 2 are expected. |
| AC-03 | PASS | C4, C5, C7, C6, C2 and C8 above. |

## Review

`aiu-reviewer` ran the per-task review and the focused T3 review (logging and diagnostics) on
`1ce4a39..d30916b`. Verdict: approve, with 0 Critical, 0 Important and 5 Minor findings. The new
code is thread-safe, and taking the lock again inside `Event` cannot deadlock. No record is
written per attempt, other events behave as before, and the change follows the logging policy.
The reviewer read the upstream H.NotifyIcon.WinUI 2.4.1 source: the library disposes the old
icon itself when `Icon` changes, and after an Explorer restart it re-adds the stored handle of
the new icon, which stays alive.

- M1, wording fixed in `1f458a9`: `UpdateTrayGlyph` no longer disposes the replaced icon
  itself, and its comments and the spec now say the library releases it. Still open: if the
  shell update fails, the library has already disposed the old icon. An Explorer restart before
  the next successful redraw would then re-add a destroyed handle, which at worst shows a blank
  icon until that redraw. Possible follow-up: `TaskbarIcon.UpdateIcon(Icon)`, which returns a bool.
- M2, open: a failure streak is tracked by event code only, so a different exception later in the
  same streak is counted rather than logged in detail. The summary does not carry the detailed
  record's `incidentId`.
- M3, open and as specified: the count is written only on recovery or a normal stop. It is lost
  on a fatal exit or a kill.
- M4, fixed in `1f458a9`: `docs/workflow/logging.md` now describes the tray-failure summary.
- M5, open: the handle test copies the window's dispose sequence instead of driving
  `UpdateTrayGlyph`. It compiles against the System.Drawing.Common version that FlaUI brings in,
  not the app's own version.

The handle test lives in the UI test project, so C4, C5 and CI do not run it. The reviewer
accepted this as recorded in the spec.

## Follow-ups found

- Skip the tray redraw when the colour has not changed. `UpdateTrayGlyph` redraws after every
  view-model property change.
- The `UpdateIcon(Icon)` variant from M1.

## Not run

| Item | Reason |
| --- | --- |
| The installed app's logs after days of uptime show no TrayFailure flood, and the tray colour keeps updating | Post-deploy owner check (R-190) |

## Whole-run review

After all three parallel items had landed, `aiu-reviewer` reviewed the integrated diff `1ce4a39..eab3fea` in a temporary worktree at `eab3fea`. Verdict: approve, with 0 Critical, 0 Important and 1 Minor finding.

- C4 passed 922/922, C5 330/330 and C7 with 0 warnings. `TrayGlyphHandleTests` passed 2/2, both through `dotnet run` and from the self-contained published UI suite. C6 passed.
- The merges needed no hand-resolved hunks.
- The UI test project changes from T-056 and T-058 build together, and the suite runs one test at a time, so the handle reading is not disturbed by the other tests.
- No item breaks another item's assumption. The reviewer confirmed from the H.NotifyIcon 2.4.1 IL that `TaskbarIcon.OnIconChanged` disposes the old icon itself.
- Minor 1, open: a pending tray-failure count is also lost when an auto-update forcibly ends the app (`ForceTargetAppShutdown`). This extends M3. A possible follow-up is to write the count during the worker's hourly sweep or in `Fatal`.
