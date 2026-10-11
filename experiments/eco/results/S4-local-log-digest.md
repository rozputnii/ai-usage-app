# Installed-app log digest (2026-10-11)

Counts only, from the installed Preview's `application-*` logs on the owner's current PC
(the logs folder below the package LocalState, docs/workflow/logging.md). No message text,
account, path, identifier or provider payload was copied; `response-*` files were counted, not
opened. Leads for the performance pair (S4d) and BL3; they are observations, not diagnoses.

## Coverage

| Item | Value |
| --- | --- |
| Span | 2026-10-07 12:00 to 2026-10-11 14:51 UTC (the 168-hour retention) |
| Records | 26,492 in `application-*`; no `critical-*` or `trace-*` files |
| App sessions (processes) | 25 |
| `response-*` files | 4,829 files, 46.2 MB |

## Records per day and severity

| Day (UTC) | Information | Warning | Error |
| --- | --- | --- | --- |
| 2026-10-07 | 1,045 | 7 | 0 |
| 2026-10-08 | 3,427 | 13 | 1,700 |
| 2026-10-09 | 7,564 | 36 | 5,886 |
| 2026-10-10 | 1,978 | 4 | 3,404 |
| 2026-10-11 | 1,357 | 4 | 3 |

## Events

| Event | Severity | Count |
| --- | --- | --- |
| TrayFailure | Error | 10,971 (2026-10-08: 1,698; 10-09: 5,870; 10-10: 3,403; 10-11: 0) |
| CapturePersisted | Information | 4,809 |
| HttpCompleted | Information / Warning | 4,793 / 16 |
| OperationStarted / OperationCompleted | Information | 2,835 / 2,810 |
| OperationCompleted | Error | 21 (all Refresh) |
| OperationFailure | Warning | 21: Refresh RateLimited 16, NetworkFailure 3, Timeout 2 |
| SessionStarted / SessionExited | Information | 25 / 1 |
| PreviousExitUnknown | Warning | 23 |
| WindowShown / WindowHidden | Information | 27 / 22 |
| UpdateInstallStarted / UpdateApplied | Information | 21 / 20 |
| UpdateCheckFailed | Warning | 4 |
| UpdateInstallFailed | Error | 1 |
| OperationCancelled | Information | 4 |
| Migration started / checkpoint verified / completed, HistoryRelocated | Information | 1 each |

No watchdog or dispatcher-stall event appears in the span.

## HTTP by provider and status

| Provider | 200 | 429 | No status | 401 |
| --- | --- | --- | --- | --- |
| claude | 960 | 16 | 1 | 0 |
| copilot | 3,847 | 0 | 5 | 0 |

No Codex or Antigravity requests in the span.

## Operation durations (OperationCompleted)

| Operation | Count | p50 | p95 | Max |
| --- | --- | --- | --- | --- |
| Refresh | 2,838 | 1.17 s | 1.77 s | 19.5 s |
| Connect (interactive sign-in) | 4 | 33.8 s | 88.6 s | 88.6 s |
| Disconnect | 1 | 0.05 s | 0.05 s | 0.05 s |

## `response-*` files per day

| Day (UTC) | Files | MB |
| --- | --- | --- |
| 2026-10-07 | 317 | 2.6 |
| 2026-10-08 | 1,044 | 10.1 |
| 2026-10-09 | 2,408 | 23.1 |
| 2026-10-10 | 615 | 6.0 |
| 2026-10-11 | 445 | 4.3 |

## Leads

- **Tray failures:** 10,971 errors over three days, none on 2026-10-11, consistent with the
  T-056, T-061 and T-062 fixes reaching the installed app; S4d should confirm with the
  measurement tool that the tray path stays quiet over a long run.
- **Usage 401 (BL3):** no 401 at all in 4,809 provider requests over four days; the 401 path
  is rare in practice, so BL3 should rely on fixtures, and its value is correctness, not
  frequency.
- **Version in logs:** every session reports app version `1.0.0.0`, so the logs cannot tell
  Preview builds apart (20 updates were applied in the span). A diagnostics lead for S4d or a
  small separate item.
- **Unclean exits:** 23 of 25 sessions start with `PreviousExitUnknown` and only one
  `SessionExited` is recorded; most exits are updates or ends without an exit marker. A
  lifetime lead.
- **Capture volume:** about one `response-*` file per provider request (4,829 files against
  4,809 captures), up to 2,408 files and 23 MB a day: a disk and I/O lead against the 128 MiB
  budget.
- **Request mix:** Copilot requests are about four times the Claude requests; check whether
  that matches the configured accounts and cadence before treating it as waste.
- **Rate limits:** the 16 RateLimited refresh failures match the 16 Claude 429 responses.
