# Codex asks for sign-in after a refused renewal - verification

Evidence for [the specification](spec.md). Code: `311b85e` and the review fixes after it, merged
with `main` at `eb2be38`. Environment: Windows 11 Pro 10.0.26300, .NET SDK 10.0.401, Release,
desktop unlocked. Date: 2026-10-12.

## Acceptance results

| AC | Result | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `CodexSessionTests.ARefusedGrantStillAsksForANewSignInAfterARelaunchWithoutTraffic` (red at base: the next refresh returned `RecoveryRequired`) and `CodexIntentAccountTests.ARefusedRenewalStillAsksForSignInAfterARelaunch` through the account service (red before the identity fix: `RecoveryRequired` with `AccountMismatch` after the relaunch); one provider request in total |
| AC-02 | PASS | The cancelled-renewal, lost-response and declined-sign-in tests expect `ReauthenticationRequired` after reloading; the locked-store, failed-cutover and corrupt-storage tests keep `RecoveryRequired` |
| AC-03 | PASS | C4, C5, C7 and C8 below |

## Checks

On the merged tree before the push to `main`.

| ID | Result | Evidence |
| --- | --- | --- |
| C2 | PASS | valid, no diagnostics |
| C3 | PASS | `--final` valid, no diagnostics |
| C4 | PASS | 942/942 (two new tests) |
| C5 | PASS | 348/348 |
| C6 | PASS | clean |
| C7 | PASS | 0 warnings, 0 errors |
| C8 | PASS | 3/3 under the desktop lock |

## Review

Focused independent T3 review (`aiu-reviewer`, convergence-review) with local `/code-review`
and `/security-review`: first round CHANGES_REQUIRED with one Important finding (after a relaunch
the account lost its identity at the intent, so the account service showed a binding failure,
"Provider error") and four Minor ones (a repeated Error log record for the known state; docs
claiming the new state for lost renewals within the same process; missing account-service
test and a loose assertion; this record). All fixed. `/security-review`: no high-confidence
vulnerabilities; no path replays the refused grant, and only fresh authorization supersedes the
intent. Re-check of the fixed lines (`9c6f5ba` and the merge): APPROVE, no open findings.

## Pending owner checks

- Live: after a refused Codex renewal, a manual refresh and a relaunch show "Sign-in expired"
  with the Sign-in action, and signing in again restores the account.
