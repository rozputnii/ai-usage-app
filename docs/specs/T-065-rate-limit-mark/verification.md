# No "sync failed" for a routine rate limit - verification

Evidence for [the specification](spec.md). Code: `916fbdc` and the review fixes `3d9c407`, merged
with `main` at `397c6cb`. Environment: Windows 11 Pro 10.0.26300, .NET SDK 10.0.401, Release,
desktop unlocked. Date: 2026-10-12.

## Diagnosis

From the installed app's `application-` and `response-` logs (counts and timings only; read
locally, nothing copied): 2026-10-11 20:46 to 2026-10-12 15:03 local, 747 HTTP records, all
Claude: 742 usage requests (670 status 200, 72 status 429) and 5 token renewals. Requests ran
every 60 seconds per account. Every 429 carried `Retry-After` 0 except a few with 300; 54 stood
alone and 9 hit both accounts at once. After each 429 the next request for that account came
10 minutes later (the scheduler's first retry delay) and succeeded; then about ten successful
readings preceded the next 429. Each 429 produced one `OperationFailure` (`RateLimited`) and a
`SyncFailedFresh` health, which `LiveLedgerProjection.Account` turned into the card's "sync
failed" mark for those 10 minutes.

## Acceptance results

| AC | Result | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `RateLimitedRefreshWithAFreshReadingIsNotASyncFailure`: fresh reading `Ok` without the mark; 20 minutes old and no reading `SyncFailedStale`. Red at base: expected `Ok`, actual `SyncFailedFresh` (also reproduced by the reviewer at `397c6cb`) |
| AC-02 | PASS | C4, C5, C7 and C8 below |

## Checks

On the merged tree before the push to `main`.

| ID | Result | Evidence |
| --- | --- | --- |
| C2 | PASS | valid, no diagnostics |
| C3 | PASS | `--final` valid, no diagnostics |
| C4 | PASS | 940/940 |
| C5 | PASS | 348/348 (one new test) |
| C6 | PASS | clean |
| C7 | PASS | 0 warnings, 0 errors |
| C8 | PASS | 3/3 under the desktop lock |

## Review

Fresh-context review (`aiu-reviewer`, T1) with local `/code-review`: APPROVE, no Critical or
Important. Three Minor items, all fixed in `3d9c407`: the spec's timing now follows the
scheduler's 10-minute retry seen in the logs; this record is complete; the test covers a rate
limit without a reading. Two `/code-review` items were dismissed with reasons (health is
derived in the projection by design; `LastSyncFailedAt` is shown only with a failure health).

## Pending owner checks

- Live: with both Claude accounts at a one-minute interval, the installed app no longer shows
  "sync failed" on a single rate-limited refresh; a reading older than 15 minutes still does.
