# T-058 verification

Code reference: `a8c8e03` and `05c8175`, merged into `main` as `3fb85f4`. Environment: the
owner's Windows 11 Pro 10.0.26200 desktop, .NET 10, Release unpackaged app build, desktop
unlocked. Date: 2026-10-10. Run as one of three parallel workers (T-056, T-057, T-058) at the
owner's request of 2026-10-10. Every smoke ran under the desktop lock. Evidence is in the
worker worktree's git-ignored `.ai-usage-local/T-058/`.

## Root cause

The app was not at fault; the tests were out of date. The [specification](spec.md#problem)
names the three causes:

- `LedgerSmoke.Focus` fell back to a fixed title-row click when Windows refused the foreground
  request. That point is now the T-052 Refresh button, and another window can take the click.
- Since R-197 the settings sheet covers the body and disables the first-run buttons behind it.
- The sign-in strip's success text changed in `2653795`, and the test still expected the old
  wording.

A probe confirmed the foreground refusal: `SetForegroundWindow` returned False while the
terminal had the last input, and True after an injected Alt tap.

## Checks

| Check | Tree | Result |
| --- | --- | --- |
| Five cases, before the fix | `1ce4a39` | FAIL 5/5 (`base-1/`) |
| Five cases, run 1 | `a8c8e03` | PASS 5/5 (`fixed-2/`) |
| Five cases, run 2 | `a8c8e03` | PASS 5/5 (`fixed-3/`) |
| Five cases, after the review fixes | `05c8175` | PASS 5/5, first try (`review-fix-1/`) |
| C4 Infrastructure suite | merged `3fb85f4` | PASS, 919/919 |
| C5 Presentation suite | merged | PASS, 330/330 |
| C7 App build (Release, unpackaged) | merged | PASS, 0 warnings, 0 errors |
| C6 Diff check | merged | PASS |
| C2 / C3 Document validation | merged | PASS, 0 diagnostics |
| C8 Launch smoke | merged, after a rebuild and republish | PASS 3/3 (`merged-c8/`); before the fix, `1ce4a39` gave FAIL 2/3 (`base-c8/`) |
| `tools/windows/Test-Erosion.ps1` | `a8c8e03` | One item: the renamed method `FirstRunSignInButtonsRespondToMouseClicksWithSettingsOpen`. It is justified by R-197 and recorded in spec R-02. The case count is unchanged at 5. |

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | The root cause above was found before the fix, and the reviewer confirmed it against the code and the failure evidence. |
| AC-02 | PASS | The five cases passed twice in a row under the desktop lock (`fixed-2/`, `fixed-3/`), and again after the review fixes. In one of the reviewer's two reruns another window covered the title row after `Focus` and took a click (4/5); the other rerun passed 5/5. Review fix 2 makes such interference report BLOCKED instead of a misleading failure. |
| AC-03 | PASS | C8 3/3 and C5 330/330 on the merged tree, plus C4, C7, C6 and C2. |

## Review

`aiu-reviewer` reviewed `1ce4a39..a8c8e03` as a per-task review, tier T1. Verdict: approve,
with 0 Critical, 0 Important and 3 Minor findings.

- R-197 justifies the rename: no live requirement says sign-in must stay clickable with
  Settings open. Every original assertion is kept after the sheet closes.
- The Alt tap is safe. It fires only after Windows refuses the foreground request, the app has
  no access keys, and `Focus` still asserts that the app owns the foreground.
- The waits are as strong as before: no fixed sleeps, and the re-find helper is used throughout.

The three Minor findings and what happened to them:

- Minor 1, fixed in `05c8175`: the wait for the sheet to close also passed when the window
  lookup returned null.
- Minor 2, fixed in `05c8175`: each mouse click now checks `DesktopTestEnvironment.RequireOwnedPoint`.
- Minor 3, fixed by this record.

"Fails at base" does not apply, because only tests changed. The base failures above are the
evidence instead.

## Follow-ups found

- `LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions` fails at
  `LedgerSmoke.cs:80`. This failure predates this change and is not caused by it: since R-203
  the cap editor is a popover outside the card's UI tree, and the test looks for it inside the
  card (`class-1/money-failure.png`).
- The other smoke classes still have the old click fallback for focus.

## Not run

| Item | Reason |
| --- | --- |
| Owner check in the installed app | None needed: only tests changed. |
