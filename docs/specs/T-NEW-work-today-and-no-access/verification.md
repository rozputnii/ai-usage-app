# Work today state and a refused usage read - verification

Evidence for [the specification](spec.md). Code: `a922e2a` and `ce84715` on the task branch.
Environment: Windows 11 Pro 10.0.26300, .NET SDK from global.json, Release, desktop unlocked.
Date: 2026-11-01.

## Diagnosis

Read locally from the installed app (AiUsage.Dev 2026.10.2502.0, build `6a67f66`): its only
application log, `application-20261101T1926…jsonl` (19:26 to 19:30, the current session; no
earlier session's log was present), and the sanitized `response-` captures. Status codes,
routes and timings only; nothing was copied out.

- Three accounts refreshed at start-up and on three manual refreshes (19:26:30, :32, :37, :47),
  then every two minutes for the two healthy ones.
- The second Claude account: `/v1/oauth/token` 200, then `/api/oauth/usage` 403 on each of the
  first three reads (`OperationFailure`, `AccessDenied`); the fourth read, 17 seconds after the
  first, got 429 with Retry-After 3600 (`RateLimited`). It was not read again in the session,
  as the scheduler's backoff and the Retry-After gate hold it.
- The 403 body has the shape of an Anthropic error (`type`, `error.type`, `error.message`,
  `error.details` with two strings, a request id); the sanitizer withheld every value. The owner
  confirmed that this account's subscription ended and it is on the free plan.
- The other Claude account and the Copilot account answered 200 throughout. No other warning or
  error records; Windows Application events show no crash of the installed app in the last week
  (only the test-only `AiUsage.DiagnosticsProbe.exe`, 2026-10-25).
- `LiveLedgerProjection.Account` treated `AccessDenied` like any failure: the card kept the
  pre-cancellation figures with "past reset", "5h past reset", "sync failed" and "not ready".

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `AccessDeniedShowsOneNoAccessNoteInsteadOfTheOldFigures` (claude, copilot), `NoAccessRowSaysSoInsteadOfOfferingAReading`, `NoAccessAccountSaysSoInTheRefreshTip`. Red before the change: the card was `(Period, TodayUsed)` instead of `(Note, NoAccess)`, the tray had no H12 row, the refresh tip read "failed 14:18, showing 09:00" |
| AC-02 | PASS | `GalleryCoversEveryCardState` (56 accounts), `EveryGalleryStateHasBoundedGeometryAndSpokenMeaning` (4 cases) |
| AC-03 | PASS | `SqueezedWindowKeepsTheTitleRowClearOfTheCaptionButtons`: `size-day-off.png` shows the outlined button with no caption, `size-work-today.png` the green fill with the tick |
| AC-04 | PASS | Checks below |

A temporary, uncommitted UI capture of the "Every card state" demo showed the H12 card as the
picked variant: name, "no access" pill in orange, the note line, not dimmed.

## Checks

On the task branch before review.

| Check | Result |
| --- | --- |
| C4 Infrastructure suite | PASS, 986/986 |
| C5 Presentation suite | PASS, 374/374 |
| C7 App build | PASS, 0 warnings, 0 errors |
| C8 Launch smoke | PASS, 3/3 under the desktop lock |
| Window-size smoke | PASS, 1/1 under the desktop lock |

## Not run

| Item | Reason |
| --- | --- |
| The installed app shows the free-plan Claude account as "no access" and Work today green while on | Post-deploy owner check (R-190) |
| The 403 body's values (the provider's stated reason) | Withheld by the sanitizer; a live read would need separate authorization |

## Review

Pending.
