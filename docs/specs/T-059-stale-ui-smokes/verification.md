# T-059 verification

Code reference: `02eddb6` and `42719a1`, merged into `main` as `016043d`. Environment: the owner's Windows 11 Pro
10.0.26200 desktop, .NET 10, Release unpackaged app build, desktop unlocked. Date: 2026-10-10. Run as one of four
items of a parallel defect-class sweep at the owner's request of 2026-10-10 (T-059 to T-062); the worker plan and
execution ledger are below. Every smoke ran under the desktop lock.

## Root cause

Lane A ran every ordinary UI test class on `17c79c7` against the C7 build, one class at a time. One test was stale:
`LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions` searched the card section for the cap
field at `LedgerSmoke.cs:80`, but since R-203 the C key opens the limit-settings popover, outside the card's UI tree,
with "Cap amount in USD" focused. It failed twice, and its screenshot shows the popover open. The other two failing
tests (`CardEditingSmoke.DraggingTheGripReordersAccounts`, `RefreshIntervalSmoke.StepperChangesTheInterval`) failed
in the harness focus fallback (T-060). With that fallback replaced in a scratch copy, every other test passed, so no
other test is stale.

## Checks

| Check | Tree | Result |
| --- | --- | --- |
| The test before the fix | `17c79c7`, `179c0b5` | FAIL at `LedgerSmoke.cs:80`, three runs (lane A twice, the worker once) |
| LedgerSmoke class, two consecutive runs | `02eddb6` | PASS 17/17, 17/17 |
| Merge gate: C4, C5, C7, C6, C2, C3, C8 | merged `016043d` | PASS: 926/926, 337/337, 0 warnings, clean, valid, valid, 3/3 |
| Required smokes after all items landed (primary) | `016043d` | PASS: C8 3/3, LedgerSmoke 17/17, CardEditingSmoke 3/3, RefreshIntervalSmoke 1/1, TrayGlyphHandleTests 2/2, TrayIconOwnerTests 2/2 |

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Fails at base at `LedgerSmoke.cs:80`; passes in two consecutive class runs under the desktop lock and again on `016043d`. |
| AC-02 | PASS | Every ordinary UI class passes under the desktop lock on the merged tree (Checks); C8, C6 and C2 pass. |

## Review

`aiu-reviewer` reviewed T-059.1 together with T-060.1 (`179c0b5..02eddb6`); the verdict is in the
[T-060 verification](../T-060-smoke-harness-input/verification.md#review). The whole-run review is in the
[T-061 verification](../T-061-unchanged-state-work/verification.md#whole-run-review).

## Execution ledger

The sweep ran four workers from fresh `origin/main` (`179c0b5`, which carried these records and their numbers):

- T-059.1 and T-060.1, harness worker: `61df3d6`, `02eddb6`, `42719a1`; merges `4b6ea19`, `016043d`. Per-task review:
  approve, 3 Minor, all fixed. Checks: the merge gate and every ordinary UI class on `016043d`.
- T-061.1, presentation-data worker: `f7e696e`, `0bf2ea8` (fast-forward). Per-task review: approve, no findings.
  Checks: the merge gate on `0bf2ea8`.
- T-061.2 and T-062.1, window and tray worker: `f094f68`, `1878061` (does not build alone; `b867d97` completes the
  rename), `b867d97`; merges `bce8db1`, `e56ec3e`. Per-task review with the T3 look at the logging line: approve, no
  findings. Checks: the merge gate on `e56ec3e`.
- T-062.2, diagnostics worker: `3564996`, `49c7381`, `bee1252`; merge `180677c`. Per-task and focused T3 review:
  approve, 5 Minor, 4 fixed in `bee1252`, 1 recorded. Checks: the merge gate on `180677c`.

Grant: the owner's request of 2026-10-10 to sweep the four classes, fix the confirmed instances, register the items
and merge the verified fixes.

## Not run

| Item | Reason |
| --- | --- |
| Owner check in the installed app | None needed: only tests changed. |
