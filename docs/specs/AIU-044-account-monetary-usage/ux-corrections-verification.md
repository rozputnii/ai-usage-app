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

## Verification in progress

Base: `031f699`. Local Windows desktop; isolated temporary app state; no CLI
credential reads/import, automated login, host installation or trust changes.

- PASS: regression tests first reproduced silent recovery actions, competing
  launches, native tray Exit no-op, missing monetary work budget and extra Credits.
- PASS: Presentation 127 tests; Infrastructure 613 tests after updating obsolete
  expectations to the owner's amended display policy. Subsequent demo fixture
  edits require the targeted/new checks below.
- PASS: document validator; unpackaged Debug build (zero warnings/errors).
- PASS: unsigned package build 2026.10.490.0. Tooling warns that mspdbcmf.exe is
  absent, so no symbol package was generated. This is not installation evidence.
- Windows run-01: 9/11 PASS. All four first-run provider buttons were clicked with
  settings open in synthetic mode; recovery retry, repeated launch, product empty
  launch, demo settings/history/tray restore, and visible-window native Exit passed.
  Hidden-window tray lookup and monetary rename lookup failed in the harness;
  fresh-element/wait fixes are pending rerun. These failures are not relabeled PASS.
- NOT_RUN: newly added monthly-work-budget Windows fixture, final independent review.
- NOT_RUN: provider consent/login completion and live quota endpoint revalidation.
  UI sign-in checks use synthetic adapters; parser-to-projection tests use fixtures.

Local artifacts: `.ai-usage-local/ux-20261004/run-01` and `packages` (ignored).
Next action: rebuild the amended demo fixture and rerun the two failed Windows
cases plus the new monthly work-budget case, then obtain independent diff review.
