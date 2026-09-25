# AIU-033 verification

Date: 2026-09-25. Windows 11 Pro 10.0.26200 x64, .NET SDK from global.json. Base `016e0e8`;
implementation in the commit that adds this record.

## Diagnosis

The installed Dev package (started 16:21 local) had read Codex and Claude once at startup and
never again: at about 20:40 the UI Automation names of both rows still read "Fresh · 16:21", and
Claude's five-hour window showed "Reset passed · Awaiting update". The code base had no
scheduler for quota reads; D-099 was accepted but unimplemented (AIU-012 remained an idea).

## Results by acceptance criterion

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `LiveAutoRefreshTests.ConnectedAccountIsReadAgainOnlyOnceItsReadingIsFiveMinutesOld` and `ManualReadingPostponesTheNextAutomaticOne`. |
| AC-02 | PASS | `LiveAutoRefreshTests.FailedAttemptsBackOffAndASuccessRestoresTheNormalInterval` (attempts at 5, 15, 35 and 65 minutes, none in between, then 70). |
| AC-03 | PASS | `LiveAutoRefreshTests.SignInAndUnrecoverableFailuresAreLeftToTheUser` for `AuthenticationRequired` and `InternalError`. |
| AC-04 | PASS | `LiveAutoRefreshTests.UnrenewedReadingTurnsStaleUntilTheNextReadingArrives`. |
| AC-05 | PASS | `LiveAutoRefreshTests.UserOperationDuringBackgroundRefreshRunsAfterItInsteadOfConflicting`; the first test asserts no Refresh all summary. |
| AC-06 | PASS, partly NOT_RUN | `LiveAutoRefreshTests.TimerRunsEveryMinuteOnlyBetweenStartAndDispose`. The unpackaged Release build ran 140 s (two ticks) with an empty isolated state directory, stayed responsive and wrote no diagnostic record; it was then closed. NOT_RUN: automatic refresh of a real connected account (see below). |
| AC-07 | PASS | Presentation 176/176 (8 new). Windows project Release x64 unpackaged build: 0 warnings, 0 errors. Document validation `{"valid":true}`; `git diff --check` clean. |

## Not run

- NOT_RUN: a live account refreshing automatically. The owner's accounts are in the installed
  Dev package; installing a new package and signing in are the owner's actions. Next check:
  install the next Dev build, leave it open for more than five minutes and confirm the row's
  reading time advances without a click.
- NOT_RUN: Infrastructure suite (no Infrastructure change), package build and Windows smoke rows.
