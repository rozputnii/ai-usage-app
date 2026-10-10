---
id: T-058
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner selection, 2026-10-10
---
# Five activation smokes fail

## Problem
Current: the LedgerActivationSmoke cases `FirstRunSignInButtonsRespondToMouseClicksWithSettingsOpen` (four
providers) and `BusyStorageExplainsTheBlockAndRetryEnablesSignIn` fail on `main` (`1ce4a39`). The app is not
at fault; the tests are stale, for three reasons:

1. Focus helper. When Windows refuses `SetForegroundWindow` to the test process (it did not send the last
   input), `Focus` clicked the window at a fixed offset in the title row. That point now lies on the T-052
   Refresh button, and any window that overlaps the app (here the owner's terminal) takes the click, so
   "Test window must own keyboard input" fails. The launch smoke C8 failed the same way here (2 of 3 at the base).
2. Settings sheet. Since R-197 (T-051) the settings sheet covers the whole body, and `CoverBody` disables the
   first-run buttons behind it. The first-run test clicked a sign-in button under the open sheet. The
   recovery test waited for an enabled "Sign in to Codex" while the sheet it opened was still covering it.
3. Success text. Since the compact one-line sign-in strip (`2653795`), the strip names the account and then
   says "added · N limits". The first-run test still waited for a name ending in " added". The audit suite
   was updated at that commit, but this smoke was not.

Expected: the five cases pass, on an ordinary desktop and in a normal run, against the R-197 sheet and the
current strip.

Unchanged: the app. The cases still click by mouse, keep their assertions, and keep their count.

## Requirements
- R-01: `Focus` lifts the foreground lock with an injected Alt tap and calls `SetForeground` again; it no
  longer clicks the window.
- R-02: The first-run case, renamed `FirstRunSignInButtonsRespondToMouseClicksAfterSettingsCloses`, checks
  that open Settings covers the sign-in buttons (disabled), closes the sheet by mouse, and then clicks the
  provider's sign-in button by mouse, waiting for the strip's "added" text.
- R-03: The recovery case checks that Retry replaces the recovery notice with first-run sign-in, closes
  Settings, and then waits for "Sign in to Codex" to be enabled.
- R-04: Both cases look up elements again on each query (`SmokeKit.Find` and a fresh window), and save a
  screenshot plus a UI-tree dump when they fail.

## Acceptance criteria
- AC-01: The root cause is found before the fix and named here and in the report.
- AC-02: The five cases pass twice in a row under the desktop lock.
- AC-03: The launch smoke C8 and the Presentation suite C5 pass, as do C4, C7 (0 warnings), C6 and C2.

## Out of scope
- App behavior, including whether a successful recovery should close the settings sheet.
- Other smoke classes and their own focus helpers.
