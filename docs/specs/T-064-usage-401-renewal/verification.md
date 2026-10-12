# Renew once after a usage 401 - verification

Evidence for [the specification](spec.md). Code reference: `7403e09` and the review fixes after `0cf2cae`, on branch `run` from
`504526a`. Environment: Windows 11 Pro 10.0.26300, .NET SDK 10.0.401, Release, desktop
unlocked. Date: 2026-10-12.

## Re-assessment of CR-T-003-03

- Codex: still applied. A usage 401 propagated to the session's catch-all and returned
  `ReauthenticationRequired` (`CodexSession.cs:216-222` at `504526a`); automatic refresh skips
  that state (`LiveLedgerSource.cs:523`), so the stored grant was never tried again.
- Claude: still applied. `ReadQuotaAsync` set `NeedsReauthentication` on a usage 401
  (`ClaudeSession.cs:126-131` at `504526a`).
- Antigravity: still applied, as for Claude (`AntigravitySession.cs:117-122` at `504526a`). A
  renewal refused with `invalid_grant` was also not recorded: it returned `QuotaUnavailable`
  and later ticks sent the refused grant again.
- Copilot: retired. It stores one GitHub access token and no refresh grant
  (`CopilotSession.cs:37-53`, [record](../../providers/copilot.md)), so a usage 401 has nothing
  to renew and correctly needs a new sign-in.
- Guidance: the existing "Sign-in expired" mark, tooltip and Sign-in action already follow
  `ReauthenticationRequired` (`LiveLedgerProjection.cs:53-54,106,122`, `CardVisuals.cs:256,364`,
  `LedgerTrayViewModel.cs:67`), so no presentation change was needed.

## Installed-app log digest

The owner's digest of 2026-10-11 (counts only, 168 hours, 4,809 provider requests) shows no
HTTP 401 for Claude or Copilot and no Codex or Antigravity traffic. The 21 failed refreshes were
16 rate limits, 3 network failures and 2 timeouts. The 401 path is rare; its value is
correctness, so the evidence here rests on fixtures.

## Checks

| Check | Result |
| --- | --- |
| Red at base | The new tests were run against the `504526a` sources: 9 of 12 failed for the expected reason (no renewal after the 401). The three "no second renewal" tests passed at base, as guards should: the base never renewed after a 401. |
| C4 Infrastructure suite | PASS, 940/940 on the final tree (938/938 before the review fixes) |
| C5 Presentation suite | PASS, 347/347, on both trees |
| C7 App build | PASS, 0 warnings, 0 errors, on both trees |
| C8 Launch smoke, under the desktop lock | PASS, 3/3, on both trees |
| Console build | PASS, 0 warnings, 0 errors (`tools/AiUsage.ProviderConsole`, Release) |
| C2 Document validation | PASS after this record |
| C6 Diff check | PASS |

## Acceptance results

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `AUsageRejectionRenewsTheStoredGrantOnceBeforeAskingForSignIn("renewed")` in `CodexSessionTests`, `ClaudeSessionTests` and `AntigravitySessionTests`: the renewal sends the stored successor grant once, and the second read succeeds |
| AC-02 | PASS | The same tests with `"terminal"`; Claude and Antigravity also restart with no provider request |
| AC-03 | PASS | `AUsageRejectionAfterARenewalInTheSameRefreshDoesNotRenewAgain` in the three classes; `ASecondUsageRejectionAfterItsRenewalAsksForSignInWithoutAnotherRenewal` (Codex, Claude) and `AnUnauthorizedRenewedQuotaRequiresReconnectWithoutAnotherProviderRequest` (Antigravity). With the Claude retry guard mutated to renew again, the guard test never finished (a renewal loop) |
| AC-04 | PASS | The same theory with `"mismatch"`: Codex `RecoveryRequired`, Claude and Antigravity `ReauthenticationRequired` with `AccountMismatch` |
| AC-05 | PASS | Checks above |

## Review

Focused independent review (`aiu-reviewer`, T3) of `504526a..0cf2cae`: APPROVE, no Important
findings. The reviewer reran the session classes (61/61) and C4 (938/938) and confirmed in a
temporary worktree that 9 of the 12 new tests fail at base. Its `/code-review` pass found
nothing beyond these Minor findings:

- M1, fixed in the record: Antigravity also treats an HTTP 401 from the token or userinfo
  request during renewal as a refusal. That fails closed; the provider record now says so.
- M2, accepted: a Codex rate-limited renewal after a usage 401 reports `NotConnected` with
  `RateLimited`, as the launch resume already does; the card reads the registry's connection.
- M3, accepted as existing: a Codex renewal that fails for a non-auth reason writes two failure
  records (`CodexSession.cs:137` and the session's catch). Unchanged by this work.
- M4, fixed: loop-guard tests for Codex and Claude.
- M5, fixed: the Codex retry reuses `ResumeCoreAsync`. The console's two checks stay, because
  the in-loop one also covers a failed `refresh-auth`.

`/security-review` of `504526a..0cf2cae` (with a local `origin/main` ref at the base, since this
clone has no remote): no findings. The renewal after a 401 keeps the workspace, organization
and subject checks; no grant is sent twice; nothing new logs credentials or provider bodies.

## Pending owner checks

In the installed app after deployment: a usage rejection that the stored sign-in can renew
keeps the account refreshing; a refused sign-in shows "Sign-in expired" with the Sign-in
action. NOT_RUN, post-deploy owner check (R-190).
