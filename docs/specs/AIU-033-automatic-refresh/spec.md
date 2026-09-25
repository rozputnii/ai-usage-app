---
id: AIU-033
type: spec
status: implementing
goal: G-003
scope_version: 1
approval_basis: Owner request, 2026-09-25, in the session conversation - the dashboard kept showing a Codex limit the owner had already reset, because nothing refreshed quota after startup; the owner asked to fix it with the minimal slice of D-099 proposed in that conversation.
---
# AIU-033 - Automatic quota refresh

## Outcome

A running app keeps connected accounts current without a click. Before this, the product read
quota only at startup and on Refresh, so a reading could sit for hours labelled "Fresh" while
the provider had already reset or renewed the limit.

## Scope

- Every connected account is read again once its last reading is five minutes old (D-099
  baseline), through the same session refresh path as per-account Refresh. A one-minute
  scheduler tick decides what is due; accounts are independent and run in parallel (D-100).
- A failed automatic attempt backs off by doubling (10, 20, then at most 30 minutes) while the
  account still shows the failure. A later successful reading, automatic or manual, restores the
  five-minute interval. A reported retry time is honored.
- Accounts that need sign-in, recovery or a fix no retry can make are not retried automatically
  (D-103). Nothing opens a sign-in or dialog.
- Background work never starts while another operation runs on the account, never cancels one
  and never reports a Refresh all summary. A user operation started during a background refresh
  runs after it instead of failing as a conflict. The background refresh is not cancelled
  because it may be rotating the grant (D-101).
- Ticks missed while the PC sleeps are not replayed and the PC is not woken (D-106); the first
  tick after resume catches up.
- A reading nobody renewed for 15 minutes is presented as stale instead of fresh until a new
  reading arrives.
- The scheduler starts after the initial load and stops before the product stops.

## Excludes

Battery Saver and metered-connection reduction with its setting (D-106), connectivity and
unlock triggers (D-104, D-107), provider-specific cadences and minimums, cancelling a background
quota read in favor of a manual one while keeping rotation intact (D-101), threshold
notifications and the rest of AIU-012.

## Acceptance criteria

- AC-01: A connected account with a fresh reading is not read again before five minutes and is
  read exactly once when five minutes have passed; a manual reading postpones the next automatic
  one.
- AC-02: Failed automatic attempts wait 10, 20 and then 30 minutes; a success restores the
  five-minute interval.
- AC-03: Accounts that need sign-in or show an unrecoverable failure are not retried
  automatically.
- AC-04: A reading older than 15 minutes shows as stale and returns to fresh with the next
  successful reading.
- AC-05: Refresh all during a background refresh waits for it and succeeds; background work
  publishes no Refresh all summary.
- AC-06: The scheduler ticks every minute only between start and stop; the product starts, runs
  across several ticks and stays responsive.
- AC-07: Deterministic tests, document validation and the Windows build pass.
