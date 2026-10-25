# Claude usage through the local Claude Code CLI - verification

Code reference: 4e5aec0 (checks), plus records. Environment: Windows 10.0.26300, .NET SDK 10.0.401, Release, desktop unlocked. Date: 2026-10-25.

## Checks
| Check | Result |
| --- | --- |
| C4 Infrastructure suite | PASS, 982/982 |
| C5 Presentation suite | PASS, 369/369 |
| C7 App build (Release, unpackaged) | PASS, 0 warnings |
| C9 Package build | PASS, unsigned validation-only package 2026.10.2551.0 |
| C8 Launch smoke | PASS, 3/3 (demo, comfortable, live empty, tray icons) |
| Live `/usage` and `auth status` run on the owner's PC (owner-authorized) | PASS, field shape recorded in [claude.md](../../providers/claude.md#claude-code-local-usage-method-t-new) |

New tests: `ClaudeCodeReportTests` (14), `ClaudeCodeSessionTests` (13), `ClaudeCodeProcessTests` (9),
`ClaudeCodeAccountServiceTests` (3), `ClaudeCodeLedgerTests` (9).

## Acceptance results
| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `ClaudeCodeLedgerTests.TheMenuOpensClaudeIntoItsTwoMethodsAndTheFirstRunListsEachMethod`, `ClaudeIsOfferedWithTheBrowserAndClaudeCodeMethods`; `ClaudeCodeProcessTests.LocateSearchesPathInOrderThenTheInstallerFolder` |
| AC-02 | PASS | `ClaudeCodeSessionTests.ConnectReadsTheSignedInAccountAndItsUsageWithoutABrowserAndKeepsOneProtectedRecord`, `ClaudeCodeReportTests.TheUsageReportBecomesAClaudeReadingWithEveryRow` |
| AC-03 | PASS | `ClaudeCodeLedgerTests.AClaudeCodeAccountIsPresentedAsClaudeAndSaysWhereItComesFrom` |
| AC-04 | PASS | `ClaudeCodeSessionTests.RefreshRereadsUsageForTheSameSignIn`; the R-233 floor keys on `ProviderKind.Claude`, to which `claude-code` maps (`LiveLedgerProjection.Provider`) |
| AC-05 | PASS | `ClaudeCodeProcessTests.ArgumentsReachTheProcessLiterallyInTheOwnedFolder`; `ClaudeCodeReportTests.AnySignOfAModelTurnRejectsTheRun`, `APromptThatReachedTheModelInsteadOfTheLocalCommandIsRejected`; `ClaudeCodeSessionTests.ARejectedUsageRunKeepsTheLastReading` |
| AC-06 | PASS | `ClaudeCodeSessionTests` mismatch, subscription, runner-failure and rejected-run theories; `ClaudeCodeProcessTests` timeout and cancellation |
| AC-07 | PASS | `ClaudeCodeAccountServiceTests.AClaudeCodeAccountCoexistsWithABrowserAccountAndDisconnectsOnlyItsOwnState` |
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
Pending.
