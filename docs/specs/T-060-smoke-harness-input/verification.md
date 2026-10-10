# T-060 verification

Code reference: `02eddb6` and `42719a1`, merged into `main` as `016043d`. Environment: the owner's Windows 11 Pro
10.0.26200 desktop, .NET 10, Release unpackaged app build, desktop unlocked. Date: 2026-10-10. Part of the parallel
sweep recorded in the [T-059 execution ledger](../T-059-stale-ui-smokes/verification.md#execution-ledger). Every smoke
ran under the desktop lock. Evidence is in the worker worktree's git-ignored `.ai-usage-local/T-060/`.

## Root cause

Lane B found the instances in the code of the ordinary smoke classes on `17c79c7` (spec, Problem). Lane A's runs
showed the fixed-point focus fallback in action: `CardEditingSmoke` and `RefreshIntervalSmoke` failed twice each at
launch, and each failed run sent a real click into the window that covered the test window (the owner's Claude
desktop app; four clicks in all). The installed AI Usage app was not touched.

## Checks

| Check | Tree | Result |
| --- | --- | --- |
| Ordinary classes, run A and run B | `02eddb6` | PASS: LedgerSmoke 17/17 and 17/17, CardEditingSmoke 3/3 and 3/3, RefreshIntervalSmoke 1/1 and 1/1, TrayGlyphHandleTests 2/2 and 2/2 |
| After the review fixes | `42719a1` | PASS: CardEditingSmoke 3/3, LedgerSmoke 17/17 |
| Reviewer's confirming run | `02eddb6` | PASS: LedgerSmoke 17/17 |
| Merge gate: C4, C5, C7, C6, C2, C3, C8 | merged `016043d` | PASS: 926/926, 337/337, 0 warnings, clean, valid, valid, 3/3; every ordinary class once more: 17/17, 3/3, 1/1, 2/2, TrayIconOwnerTests 2/2 |
| Required smokes after all items landed (primary) | `016043d` | PASS, as in the [T-059 checks](../T-059-stale-ui-smokes/verification.md#checks) |
| `tools/windows/Test-Erosion.ps1` | `42719a1` | One item: `CardEditingSmoke.cs` Assert count 29 to 28. Three asserts moved unchanged into `SmokeKit` (the focus foreground check and the SendInput count check, now one assert in `SmokeKit.MoveTo`); two condition-wait asserts were added. Case counts unchanged. |

AC-01 grep, rerun on the merged tree in `tests/windows/AiUsage.Windows.Tests` over `LedgerSmoke.cs`,
`LedgerActivationSmoke.cs`, `LedgerWindowSizeSmoke.cs`, `CardEditingSmoke.cs` and `RefreshIntervalSmoke.cs`:

```
grep -nE 'Left \+ 120|Top \+ 18'                                                      -> none
grep -nE '\.Click\(|\.RightClick\(|Mouse\.(Click|RightClick|LeftClick|DoubleClick)|[^a-zA-Z.]Hover\(' -> none
grep -nE 'GetDesktop\(\)\.FindFirstDescendant|GetDesktop\(\)\.FindAllDescendants'     -> none
grep -nE 'ByName\("(Demo scenarios|Add account|More settings|Confirm deleting stored data|Cancel deleting stored data|Show values: (used|left)|Refresh interval in minutes|Account name|Work today, off)"|"Refresh\. "|StartsWith\("History,|ByName\(gripName|ByAutomationId\("SettingsButton' -> none
grep -nE 'Thread\.Sleep' -> only 100 ms polls inside wait loops, drag and SendInput pacing, and the two settle
                            delays before negative checks that the spec keeps
```

All mouse input goes through `SmokeKit.ClickOwned`, `RightClickOwned` or `HoverOwned`, which check the point first;
the only exception is the drag's button down and up at the grip, which `HoverOwned` has already checked.

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | The grep above on `016043d`. |
| AC-02 | PASS | With the history Invoke removed temporarily (not committed): the base wait (`Contains("History")`) passed all three cases, so it could never fail; the new wait failed both demo cases with "History did not open". |
| AC-03 | PASS | Two consecutive passing runs of every ordinary class, case counts unchanged, Test-Erosion item justified (Checks). |
| AC-04 | PASS | C8 3/3 on the merged tree and again after all items landed; C6 and C2 pass. |

## Review

`aiu-reviewer` reviewed T-059.1 and T-060.1 together on `179c0b5..02eddb6`, tier T2 (tests only). Verdict: approve,
with 0 Critical, 0 Important and 3 Minor findings, all fixed in `42719a1`:

- M1: the rename-hover wait passed on any open app tooltip; it now waits for the "Rename" tooltip.
- M2: the squeeze wait was already true after the first squeeze; it was removed, and the comment now says what
  `SetWindowPos` and `AssertClear` guarantee.
- M3: an older `ByAutomationId("SettingsButton")` lookup went against R-06; it now finds Settings by name.

The reviewer confirmed that focus keeps T-058's behaviour, that every guard runs before its input on the same point,
that no assertion was weakened, that owned-window searches filter by process and that each AutomationId used is
unique in its scope. "Fails at base" does not apply because only tests changed; the base failures above are the
evidence. The whole-run review is in the [T-061 verification](../T-061-unchanged-state-work/verification.md#whole-run-review).

## Left open

- The Explicit Sandbox-guest smokes (`ShellSmoke`, `AutomaticRefreshSmoke`, `RecoverySmoke`, `UpdateSmoke`) have the
  same patterns (`ShellSmoke.FocusForKeyboard` clicks the title bar when the foreground is refused; unguarded
  Settings clicks; held windows). They run only in a Sandbox guest, so a fix cannot be verified without an
  owner-authorized Sandbox run.
- Not confirmed, so not changed: the drag target points in `CardEditingSmoke`, the card element held in
  `LedgerSmoke`'s rename and scenario steps, and the content asserts right after launch in
  `WorkBudgetShowsMonthlyAndDailyBarsBesideSubscriptionWithoutCredits`.
- The frozen Sandbox audit suite (`Audit*.cs`) keeps its fixed caption-offset clicks and sleeps.

## Not run

| Item | Reason |
| --- | --- |
| Owner check in the installed app | None needed: only tests changed. |
