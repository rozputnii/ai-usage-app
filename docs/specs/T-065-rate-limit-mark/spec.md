---
id: T-065
type: bug
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner report and request, 2026-10-12, in conversation: both installed Claude accounts often show "Sync failed", alternating between them; find the cause in the logs and fix it.
---
# No "sync failed" for a routine rate limit

## Problem

- Current: any failed refresh marks the account "sync failed" at once, even while its reading
  is still current. The installed app's logs (2026-10-11 20:46 to 2026-10-12 15:03 local) show
  742 Claude usage requests at a one-minute refresh interval, 72 of them answered 429 with
  `Retry-After` 0 (or 300). The pattern repeats every 20 minutes: about ten successful
  readings, one 429, then the failed account waits the scheduler's 10-minute retry delay before
  it reads successfully again. Each 429 kept the mark on that card for those 10 minutes, so
  each account showed "sync failed" about half the time, often on one account or the other.
- Expected: a rate-limited refresh while the reading is still current is not a sync failure:
  the card keeps its reading without the mark, and the scheduler's retry (10 minutes, within
  the 15-minute floor) tries again. Once the
  reading is stale (the T-055 continuity tolerance, at least 15 minutes), a rate limit shows as
  "sync failed" like any other failure.
- Unchanged: every other failure kind with a current reading still shows the mark (T-034 H7);
  sign-in expired and provider error; the Retry-After cooldown in the quota clients; the
  refresh interval and the retry delays; logging (each 429 is still one failure record); layout and copy.

## Requirements

- R-01: An account whose last refresh failed with `RateLimited` and whose reading is within
  the continuity tolerance has health `Ok` and no `SyncFailed` mark.
- R-02: The same account with a stale reading, or with no reading, keeps `SyncFailedStale`.

## Acceptance criteria

- AC-01: A projection test shows R-01 and R-02 for a Claude account; it fails at the base.
- AC-02: The Infrastructure and Presentation suites, the app build and the launch smoke pass.

## Out of scope

- Fewer Claude usage requests. A one-minute interval is the owner's setting (stepper minimum,
  T-055); the 429s pass on the next tick, so the request rate stays.
- Live behavior in the installed app, which is a post-deploy owner check.
