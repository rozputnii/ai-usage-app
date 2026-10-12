# No "sync failed" for a routine rate limit - verification

Evidence for [the specification](spec.md). Environment: Windows 11 Pro 10.0.26300, .NET SDK
10.0.401, Release, desktop unlocked. Date: 2026-10-12.

## Diagnosis

From the installed app's `application-` logs (counts only; read locally, nothing copied):
2026-10-11 20:46 to 2026-10-12 15:03 local, 747 HTTP records, all Claude: 742 usage requests
(670 status 200, 72 status 429) and 5 token renewals. Requests ran every 60 seconds per account.
Every 429 carried `Retry-After` 0 except a few with 300; 54 stood alone and 9 hit both accounts at once, and
each was followed by a 200 on the next tick. Each 429 produced one `OperationFailure`
(`RateLimited`) and a `SyncFailedFresh` health, which `LiveLedgerProjection.Account` turned into
the card's "sync failed" mark until the next success.

## Checks

| ID | Result | Evidence |
| --- | --- | --- |
| Red at base | PASS | `RateLimitedRefreshWithAFreshReadingIsNotASyncFailure` failed before the fix: expected `Ok`, actual `SyncFailedFresh` |
