# AIU-008 verification

Current result: implementation, automated regressions, packaged Windows smoke and focused independent review PASS. Live account acceptance is NOT_RUN by owner direction, pending the owner's in-app test. The feature is in review, not done.

Date: 2026-09-16. Base: `ba6b49f` on `main`. Task branch: `users/github-copilot-e2e-integration-da3f95` (worktree), initially clean. First candidate commit: `c307416`; review fixes follow it on the same branch. Windows 11 Pro 10.0.26200 x64, user-local .NET SDK `10.0.401`. Restores used only the existing local NuGet package folder as the source, with no network restore and no dependency changes.

## Research and owner decisions

Primary sources: oh-my-pi v18.2.2 at `60c9a115b2e8decc0f75825459362d14188a8bc0` (fetched through GitHub's public API, local clone unchanged) and GitHub documentation for billing usage REST, usage-based billing, legacy billing, GitHub App device flow/refresh, authorizing OAuth apps and Copilot CLI authentication. The [provider evidence](../../providers/copilot.md) records exact references and classifications.

The owner chose the reused OpenCode OAuth App and documented reports only, then chose "Live-probe first" after the token-type mismatch was presented. Those decisions are recorded in the [spec](spec.md). The primary built `copilot-probe` and asked the owner to run it in the app's Terminal panel. Terminal inspection showed no probe output on two reads, and the owner reported "i do not see any requests. finish integration without it. i will test letter in the app". No device code was requested by this session, no token was issued or observed, and no GitHub account API was called. No source CLI credentials were read. `gh auth status` was run once while checking tool availability; it printed only the masked token prefix, and no token was retrieved or used.

## Automated checks (final tree)

| Check | Verdict | Observation |
| --- | --- | --- |
| `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS | 78 tests, 0 failures/skips |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 174 tests, 0 failures/skips (existing Codex and Claude cases plus the new Copilot cases) |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 19 tests, 0 failures/skips |
| `dotnet build tools/AiUsage.ProviderConsole --no-restore` | PASS | 0 warnings, 0 errors |
| `dotnet run --project tools/AiUsage.ProviderConsole --no-restore -- inspect-copilot tests/windows/AiUsage.Infrastructure.Tests/Fixtures/copilot-usage.synthetic.json` | PASS | Synthetic report normalized offline |
| `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS | `valid: true`, zero diagnostics |
| `git diff --check` | PASS | No whitespace errors |

The Copilot tests cover:
- Recorded client/scope request bodies and numeric binding.
- `authorization_pending`, `slow_down` interval growth, expiry, denial, `device_flow_disabled`, unknown errors and replay prevention.
- Untrusted/plain-HTTP verification URIs and incomplete device responses.
- Tokens without stable identity, login path-injection rejection, and 401/403/429/502 classification with no payload echo.
- Cancellation before polling, and cancellation after issuance still binding the token.
- Documented report parsing, invalid/negative/missing values and foreign-user reports.
- Protected-state round trip without plaintext token bytes, cached startup, resume, disconnect, one absent report, both absent, revoked tokens and failed different-account reconnect.
- Same-account reconnect recovering reauthentication, transient 5xx/429 preserving cached reports, interrupted stage discard, corrupt-record preservation, and overlap and cancellation during connection.
- Presentation: device code shown before the browser opens and then cleared, no cross-unit sums, missing and empty reports, and cached labeling.

One presentation run initially failed on banker's rounding of a displayed midpoint and on a test key assertion. Display now rounds away from zero. One review regression test initially failed and exposed token loss when cancellation hit a token poll response; fixed as described below.

## Packaged Windows smoke

- Package `2026.9.1610.0` (SHA-256 `2C97802E3398D892868ECD11D873E80F9ACB7B5AFFAD69423AE85CD8F853672B`) built and was signed with the existing owned `CN=AI Usage Development` CurrentUser certificate. As in AIU-007, host verification fails closed because the root is untrusted on the host; no host trust changed.
- In a disposable Windows Sandbox (networking, clipboard, audio, video and printer disabled), six scenarios passed. `copilot-controls` failed on a harness defect: UIA reports no Name for an empty TextBlock. Its failure screenshot showed correct product behavior: "The provider could not be reached.", no connection, and Refresh/Disconnect disabled.
- Package `2026.9.1611.0` (SHA-256 `F25DB1DA5AF4049B3DAA4383998E7DA93756588AFAA4489349AA66156BB6DB21`) contains the final product source, including review fixes and the token-cap notice. Its first guest run failed before any Copilot action: the UIA Window pattern was unavailable for a maximize call the scenario did not need, so that call was removed from the harness.
- Final guest run, `2026.9.1611.0`, self-contained harness DLL SHA-256 `d8dc21b92ea22a900bed64b02a2da021f4d6fee847c4babb665df7858843ed65`, ProductUi mode, offline dependencies provisioned first: **PASS**. Seven of seven scenarios passed, each with process exit code 0: exit, title-bar, repeated-exit, minimize, tray-exit, claude-controls and copilot-controls. The guest report status is `PASS_REQUIRES_EVIDENCE_REVIEW`.
- Evidence review: the `copilot-controls` screenshot shows the Copilot card, the OpenCode/ten-token notice and "No accounts connected.", and the post-switch capture shows Codex with a clean state. The offline-connect failure text lies below the captured region; it is established by the UIA assertion and the 1610 failure capture. The Claude manual-entry capture remains correct.

Installation-contract negatives were not run in this mode, and no host installation or registration occurred. Evidence is retained locally under `.ai-usage-local/AIU-008/` and not committed.

## Review

Primary review of the integrated diff and fix diff: PASS.

A fresh read-only general-purpose subagent received the frozen `c307416` diff under a credential/durable-state brief. Its result was **no material findings**. It verified token confinement, device-flow bounds, numeric binding and login safety, enum-name mapping, store revision/corruption/disconnect behavior and shutdown drain, and reran the suites (170/19 at that commit). Its minor findings and resolutions:

1. A same-account reconnect could not recover when no report exists. **Fixed:** a same numeric account reconnect persists the verified token, clears reauthentication and keeps its cache. A different account still must prove usage first. Test added.
2. An issued token was lost if identity binding was canceled. **Fixed:** identity binding, and each token poll response (the second gap, found by the new test), ignore caller cancellation within the 15-second request deadline. Test added.
3. OAuth token cap (reviewer's unverified recollection). **Verified** in GitHub's authorizing-OAuth-apps documentation: ten tokens per user/app/scope with revocation, a 10-per-hour creation limit and 50 code submissions per hour per application. Recorded in provider evidence and added to the on-screen notice.
4. Transient single-report failure overwrote the cache. **Fixed:** only 403/404 mark a report absent; other failures fail the read. Tests added for 5xx and 429.

Nits: login renames are now re-verified after any absent report. `StorageUnavailable` reporting a stored grant matches the existing Claude pattern and is unchanged. Per CONTRIBUTING, fixes received targeted checks and primary review, not a second full review.

## Acceptance

| Criterion | Verdict | Evidence or missing work |
| --- | --- | --- |
| AC-01 | PASS | Source/official classifications, restrictions, token-type compatibility risk and context/import capabilities recorded; no permission implied |
| AC-02 | PASS (synthetic) | Protocol and session tests; live consent NOT_RUN |
| AC-03 | PASS (synthetic) | Store/session tests; Codex/Claude suites unchanged and passing; live resume NOT_RUN |
| AC-04 | PASS (synthetic) | Parser, client and presentation tests |
| AC-05 | PASS | Presentation suite and packaged `copilot-controls` |
| AC-06 | NOT_RUN (live) | Automated and packaged checks PASS. Live connection, report acceptance for the OpenCode token, refresh, restart resume and disconnect await the owner's in-app test |
| AC-07 | PASS for review and publication | Independent review had no material findings; branch published under CONTRIBUTING. No main integration or release |

## Next action

The owner installs a build containing this branch and connects GitHub Copilot in the app. Record whether GitHub accepts the OpenCode `read:user` token for `/users/{login}/settings/billing/ai_credit/usage` (200 versus 403/404), then refresh, restart/resume and disconnect. If the report is refused, the documented app-owned alternative is an owner-registered GitHub App with "Plan" read permission.
