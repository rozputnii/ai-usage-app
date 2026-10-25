# Claude usage through the local Claude Code CLI - verification

Code reference: 8373691 (final checks; 4e5aec0 for the first run), plus records. Environment: Windows 10.0.26300, .NET SDK 10.0.401, Release, desktop unlocked. Date: 2026-10-25.

## Checks
| Check | Result |
| --- | --- |
| C4 Infrastructure suite | PASS, 986/986 |
| C5 Presentation suite | PASS, 370/370 |
| C7 App build (Release, unpackaged) | PASS, 0 warnings |
| C9 Package build | PASS, unsigned validation-only package 2026.10.2552.0 |
| C8 Launch smoke | PASS, 3/3 (demo, comfortable, live empty, tray icons) |
| C2 Document validation, C6 diff check | PASS |
| Live `/usage` and `auth status` run on the owner's PC (owner-authorized) | PASS, field shape recorded in [claude.md](../../providers/claude.md#claude-code-local-usage-method-t-071) |

New tests: `ClaudeCodeReportTests` (14), `ClaudeCodeSessionTests` (15), `ClaudeCodeProcessTests` (11),
`ClaudeCodeAccountServiceTests` (3), `ClaudeCodeLedgerTests` (10); `OwnedDataDeletionTests` gained a Claude Code slot.

## Acceptance results
| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `ClaudeCodeLedgerTests.TheMenuOpensClaudeIntoItsTwoMethodsAndTheFirstRunListsEachMethod`, `ClaudeIsOfferedWithTheBrowserAndClaudeCodeMethods`; `ClaudeCodeProcessTests.LocateSearchesPathInOrderThenTheInstallerFolder` |
| AC-02 | PASS | `ClaudeCodeSessionTests.ConnectReadsTheSignedInAccountAndItsUsageWithoutABrowserAndKeepsOneProtectedRecord`, `ClaudeCodeReportTests.TheUsageReportBecomesAClaudeReadingWithEveryRow` |
| AC-03 | PASS | `ClaudeCodeLedgerTests.AClaudeCodeAccountIsPresentedAsClaudeAndSaysWhereItComesFrom` |
| AC-04 | PASS | `ClaudeCodeSessionTests.RefreshRereadsUsageForTheSameSignIn`; the R-233 floor keys on `ProviderKind.Claude`, to which `claude-code` maps (`LiveLedgerProjection.Provider`) |
| AC-05 | PASS | `ClaudeCodeProcessTests.ArgumentsReachTheProcessLiterallyInTheOwnedFolder`; `ClaudeCodeReportTests.AnySignOfAModelTurnRejectsTheRun`, `APromptThatReachedTheModelInsteadOfTheLocalCommandIsRejected` (`InternalError`), `ADifferentLocalCommandIsRejected`, `AnErrorResultIsRejected` (`InvalidResponse`); `ClaudeCodeSessionTests.ARejectedUsageRunKeepsTheLastReading`; automatic refresh skips `InternalError` (`LiveLedgerSource` tick) |
| AC-06 | PASS | `ClaudeCodeSessionTests` mismatch, subscription, lost sign-in, runner-failure and rejected-run theories; `ClaudeCodeProcessTests` timeout, cancellation, output cap and working-folder failure |
| AC-07 | PASS | `ClaudeCodeAccountServiceTests.AClaudeCodeAccountCoexistsWithABrowserAccountAndDisconnectsOnlyItsOwnState`; `OwnedDataDeletionTests.ConfirmedDeletionRemovesKnownGenerationsAndKeepsForeignFiles` |
| AC-08 | PASS | C4 browser Claude suites unchanged and green; `ClaudeCodeAccountServiceTests.ReconnectUsesClaudeCodeAgainAndRefusesAnotherSignIn`; `ClaudeCodeLedgerTests.ReconnectingAClaudeCodeAccountUsesClaudeCodeAgain`, `TheBrowserMethodKeepsTheProviderKey` |
| AC-09 | PASS | `ClaudeCodeSession` records `DiagnosticEvent.OperationFailure` with the `ProviderException` kind only; runner and parsers never put process output into an exception |

## Not run
| Item | Reason |
| --- | --- |
| The packaged app starting `claude.exe` and a live Claude Code account in the installed app | Post-deploy owner check (R-190) |
| Signed-out, API-key and rate-limited Claude Code output; Pro, Team, Enterprise and usage-credit payloads | Not available on the owner's PC; covered by synthetic tests |

## Deviations
- AC-03 as first written put the plan in the caption ("via Claude Code · Max", as in the
  variant preview). The app already adds a reported plan to the default account name, so the
  caption says "via Claude Code" and the name says "Claude Max", without repeating the plan.
- The first live probe ran through Git Bash, which rewrote `/usage` into a path; one prompt
  reached the model (about 21k input tokens, list-price estimate $0.18, on the owner's plan).
  The rerun without path conversion is the recorded result.

## Review
Independent T3 review with the reviewer's `security` entry (fable, extra-high effort), fresh context, with the
brief ledger and path list; it reran C4, C5, C2 and C6 and ran `/code-review` (`/security-review` saw the primary
checkout's empty diff, so its criteria were applied by hand). Verdict on c091469..ca79775: changes needed, with
1 Critical, 1 Important and 5 Minor findings, all fixed in 8373691 with tests that failed first:

- Critical: "Delete stored data" did not know `claude-code.state`, failed and blocked every later start. Fixed in
  `OwnedDataDeletion.ProviderNames`.
- Important: output past the 1 MB cap waited for the 30-second deadline and reported `Timeout`. Now fails at once
  with `InvalidResponse`.
- Minor: runner I/O failures escaped as raw exceptions (now `ProviderUnavailable`); a detected model turn kept being
  retried (now `InternalError`, never retried automatically); a lost Claude Code sign-in kept retrying (now
  `ReauthenticationRequired`); the PATH lookup ran on every rebuild (now reused for a minute); records (R-180
  "Amended by", design and research corrections).

Re-check of the fixed lines: all seven resolved, no new defects; approved. Optional follow-up from the re-check,
not done: parse the report before the exit-code check, so a billed turn that also exits non-zero stops automatic
refresh too (today it is retried on the failure backoff; the observed billed run exited 0).
