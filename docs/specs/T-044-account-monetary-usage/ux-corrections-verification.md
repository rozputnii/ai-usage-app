# Owner-reported UX corrections, 2026-10-04

Scope: the owner's current reports select sign-in/recovery and ordinary UX checks,
repeat launch after close-to-tray, native tray Exit, suppression of Codex credits
beside subscription windows, and monetary-only monthly/daily work-budget bars.
The scope amendment is in [spec.md](spec.md#owner-correction-2026-10-04).

## Causes and acceptance

- Installed-app sanitized logs showed a first process hiding to the tray, then a
  second process reporting LeaseUnavailable (Windows sharing violation). Repeat
  launch must redirect to the existing window, preserving the storage lease.
- Startup/recovery showed enabled first-run actions while the source silently
  rejected sign-in. Blocked controls must reflect availability, recovery must be
  reachable, and system status must not claim synchronization during recovery.
- H.NotifyIcon 2.4.1's default native menu executes MenuFlyoutItem commands, not
  Click handlers. Open, Refresh and Exit must use commands; Exit must terminate
  the owning process whether the main window is visible or hidden.
- Monetary-only native USD usage must show the provider amount/limit, monthly bar
  ending on the first of next month, and daily budget. Preserve explicit shared,
  disabled and incompatible facts and keep assumed dates distinct from provider dates.
- Percentage subscription accounts must not display a separate Codex Credits card.
  Native credit facts/history remain stored; abstract credits are not made into USD.

## Verification results

Base: `031f699`. Local Windows desktop; isolated temporary app state; no CLI
credential reads/import, automated login, host installation or trust changes.

- PASS: regression tests first reproduced silent recovery actions, competing
  launches, native tray Exit no-op, missing monetary work budget and extra Credits.
- PASS: Presentation 128 tests; Infrastructure 613 tests after updating obsolete
  expectations to the owner's amended display policy and fixing calendar rollover.
- PASS: document validator; unpackaged Debug build (zero warnings/errors).
- PASS: unsigned package builds 2026.10.490.0, 2026.10.491.0 and final 2026.10.492.0
  (after the calendar rollover correction). Tooling warns that mspdbcmf.exe is
  absent, so no symbol package was generated. This is not installation evidence.
- Windows run-01: 9/11 PASS. All four first-run provider buttons were clicked with
  settings open in synthetic mode; recovery retry, repeated launch, product empty
  launch, demo settings/history/tray restore, and visible-window native Exit passed.
  Hidden-window tray lookup and monetary rename lookup failed in the harness.
- PASS: Windows run-02, 4/4 cases after fresh-element/wait fixes: native tray Exit
  with visible and hidden windows, monetary history/cap/rename/account actions,
  and monthly/daily work-budget bars beside a Codex subscription without Credits.
  Across both runs, all 12 distinct applicable Windows cases passed. The original
  failed attempts remain recorded above; they were followed by actual reruns.
- PASS: independent read-only review of `c8804cd` against `031f699` covered lifecycle,
  recovery, tray commands and monetary projection. It found one material P2 issue:
  periodless monetary history could carry previous-month spending into the new
  month's daily baseline. The regression reproduced a 1900 USD baseline instead
  of zero. The fix gives derived work-budget readings a calendar period while
  preserving raw runs and provider facts. The targeted regression now passes,
  including 10 USD first-day history and unchanged stored readings; the full
  regression suites above passed after the fix. No other material findings.
- NOT_RUN: provider consent/login completion and live quota endpoint revalidation.
  UI sign-in checks use synthetic adapters; parser-to-projection tests use fixtures.

Local artifacts: `.ai-usage-local/ux-20261004/run-01`, `run-02` and `packages` (ignored).
NOT_RUN: host package installation/update and packaged activation. The owner's
installed app was not replaced; unpackaged local smoke does not establish those checks.

Implementation and the applicable local checks are complete. No unresolved material
review findings remain. This record does not authorize a release or host installation.
