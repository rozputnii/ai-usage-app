# AIU-007 verification

Current result: implementation, automated regressions, packaged Windows controls/lifecycle, focused independent review and owner-led live Claude lifecycle pass. Final local-main integration checks are in progress. The latest live evidence below supersedes earlier access blockers; historical preparation results remain identified.

Date: 2026-09-14. Base: `0a0ffd4` (completed AIU-027). Task branch: `codex/aiu-007-claude-integration`. The initial tracked and untracked checkout was clean. No production code, credential storage, dependencies or Windows UI changed during the initial preparation recorded here.

## Preparation and environment

Read the owner's attached goal objective, CONTRIBUTING, relevant repository skills, product direction, provider assignments, accepted decisions, security/lifecycle policy and AIU-027 design/evidence. Confirmed that the checkout contains AIU-027. Existing local main is `1e4f0ce`, an ancestor of the selected base, and remains unchanged.

Reviewed existing Core workflow/session contracts, Infrastructure auth/session/DPAPI/cache ownership and Windows composition/presentation. The [spec](spec.md) retains full feature acceptance; the [design](design.md) records conditional implementation choices, current official .NET references and the verification plan. No provider permission was inferred from OMP licensing or source functionality.

Windows x64; user-local .NET SDK `10.0.401` was confirmed by `--version`, matching `global.json`. Existing restored packages only, with `--no-restore`. No dependency install or upgrade occurred.

## Executed checks

The checks below exercised the unchanged architecture baseline on 2026-09-14, before any Claude implementation. They do not establish Claude behavior or live acceptance.

| Check | Verdict | Observation |
| --- | --- | --- |
| `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS | 78 tests; zero errors, failures, skips or not-run tests |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 72 tests; zero errors, failures, skips or not-run tests |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 12 tests; zero errors, failures, skips or not-run tests |
| `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS | Final feature/provider records valid, zero diagnostics |
| `git diff --cached --check` | PASS | No whitespace errors; scoped changes consist of seven documentation files |

These commands used `C:/Users/danii/.dotnet/ai-usage-sdk/dotnet.exe` in place of `dotnet`.

Primary document/diff review: PASS. The records keep source preparation separate from implementation/live acceptance, preserve the full requested feature scope, classify the specific blocker and provide one exact next action. Only documentation changed. No claim of a reviewed or delivered Claude adapter is made.

## Provider access and review

Independent research was assigned to a fresh `gpt-5.6-luna` agent with `max` reasoning, restricted to public source/official documentation and no repository writes, credentials, account calls or nested agents. The primary independently inspected OMP's auth declaration, identity hook and quota implementation and the official authentication restriction. This is research, not an independent security review of an implemented adapter.

Primary review corrected the agent's initial current-stable claim (`v18.1.18`). A direct GitHub release API read identified `v18.1.22`, published at 19:29:59 UTC, whose tag resolves to `23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d`. Nine relevant files fetched at that immutable commit matched the local OMP clone after line-ending normalization. The [provider record](../../providers/claude.md) uses that current reference and distinguishes its host-fallback behavior from the older tag. The primary also narrowed the agent's state-check statement: OMP's manual path accepts a raw code without state, while a supplied mismatching state is rejected. No agent implementation diff existed to integrate.

The current restriction and unresolved authorization basis are recorded in [provider evidence](../../providers/claude.md) and PROVIDER-002 in the [owner inbox](../../decisions/pending.md). Authentication implementation/live access is BLOCKED on that material decision. No source CLI credentials, app-owned real credentials, browser cookies or account payloads were read. No Claude sign-in, quota request or token refresh occurred.

Required independent credential/durable-state review is NOT_RUN: there is no Claude implementation or credential/storage change to review. No new package was built or installed, no host trust changed, and actual Claude packaged UI/live tests are NOT_RUN. Prior AIU-027 screenshots and checks are historical baseline evidence only.

## Preparation acceptance (historical)

| Criterion | Verdict | Evidence or missing work |
| --- | --- | --- |
| AC-01 | PASS | Current source contracts, method classifications and unresolved permission/access are recorded; this is source preparation only |
| AC-02 | BLOCKED | No authorized Claude connection implementation or live consent |
| AC-03 | NOT_RUN | No Claude persistence implementation; Codex baseline regressions pass |
| AC-04 | NOT_RUN | Quota contracts researched; no Claude parser or presentation tests executed |
| AC-05 | NOT_RUN | Existing workflow regressions pass; two-provider behavior not implemented |
| AC-06 | BLOCKED | Claude automated, packaged UI and live acceptance remain outstanding |
| AC-07 | BLOCKED | No completed candidate for required review, publication or local-main integration |

## Integration and next action

AIU-007 is incomplete. The verified documentation preparation is committed on its task branch, identified by the Git history for this record; main has not moved. The incomplete feature branch is not published under CONTRIBUTING's completed-task rule. No remote publication, main push, release, signing or host installation occurred.

Exact next action: obtain the owner's disposition of PROVIDER-002 before implementing or launching any Claude OAuth flow. The remaining task sequence is in [tasks.md](tasks.md).

## Owner amendment and implementation run

The owner subsequently selected: “Proceed with a private, unsupported Claude integration using OMP’s connection flow. I understand Anthropic’s documented restriction. Keep that limitation recorded.” This resolves the local implementation decision above; it is not provider approval or live verification. The amended spec and design retain the restriction. Work stays local under the latest private scope.

Started an isolated `gpt-5.6-luna`/`max` quota implementation worker on `codex/aiu-007-claude-quota` from `44cfa81`, with the exact four-file ownership recorded in tasks. The primary owns authorization, protected state, integration and canonical evidence. Implementation checks and live acceptance are still outstanding at this point.

### Implemented candidate checks

The primary inspected all four worker files at `b3b1d98` and integrated them as `683ba35`. Primary review corrected the worker's use of `is_active` as an entitlement (the source says it ranks severity), removed broad exception swallowing and retained opaque kind names for display. The integrated parser preserves legacy/current shared-pool precedence, independent scoped rows and explicit money units.

PASS: Infrastructure Release suite now has 120 passing tests, zero failures/skips; Presentation has 15. These cover Claude PKCE/state/consent/manual input, identity mismatch, malformed payloads, throttling, protected generation recovery, exclusive leases, ambiguous-refresh replay prevention, rotation followed by quota cancellation, failed reconnect/removal, provider isolation and presentation cancellation. Early new-type tests failed before implementation. Two further regression tests failed against the candidate for stalled callbacks and failed account replacement, then passed after bounded header handling and quota-before-switch cutover.

PASS: the shared provider console normalized the synthetic Claude fixture offline. PASS: native unsigned package build `2026.9.1413.0`, SHA-256 `2066D95EBBF30E0A21FC7C02743DC408A678B39B9D1177A3C7E5DA1B9B08E1C7`. The initial `1412` build failed on an XAML static/instance binding and was corrected; it is not acceptance evidence. The existing packaging-tools warning about missing `mspdbcmf.exe` prevents a symbols package, with no owned-code warnings.

Independent credential/durable-state review, actual installed Windows smoke and live-provider acceptance remain NOT_RUN on this candidate. Main has not moved; no remote publication occurred. These local checks do not satisfy AC-06 or AC-07.

### Installed candidate and follow-up fixes

Frozen `97ca3f2` was supplied to a fresh `gpt-5.6-luna`/`max` read-only credential/durable-state reviewer in an isolated detached worktree. The review is in progress. Subsequent primary tests reproduced and corrected repeated idle disposal throwing; the active-operation drain guard remains. Infrastructure now passes 121 tests and Presentation 16; the console builds with zero warnings/errors.

Signed package `2026.9.1414.0`, SHA-256 `9B7B9AF437FF35EAE8AEFF7C9B90462766A24360D38877E54C2C75E9B65E6C99`, installed successfully in disposable Sandbox `cb7a95cf-328c-42c3-9fe1-5a9e93014394`. Host signature trust check failed as expected; no host trust changed. Networking and clipboard were disabled. The first harness failed before test discovery because it was mistakenly published framework-dependent and lacked the Windows Desktop runtime. Republishing the harness self-contained using the existing restored runtime packs fixed that prerequisite; no product dependency changed.

The actual rerun (`.ai-usage-local/AIU-007/guest-evidence-1414/product-retry`) passed five existing lifecycle scenarios and failed the new Claude controls scenario during the second browser connection. A guest-only probe confirmed that browser reuse returned no process handle and no exception. The app had incorrectly classified that successful shell activation as failure. The Windows launcher and console now accept this documented result; see [Microsoft Process.Start](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=net-10.0). The controls scenario also maximizes its window so the manual input can be visually reviewed within the guest display. Package 1414 is superseded; final package and interaction evidence remain pending.

### Packaged input verification and review regressions

Signed package `2026.9.1415.0` contains product source `05a469206aecdb9d882414b23c7274a8d0cd9505`; SHA-256 `2FD6EC4548E0F67222165886328644D04E113140C2440A45674BC313C98EB117`. It was installed in the same isolated guest. The first run passed four scenarios and failed two keyboard-driven exits because UIA focus alone did not make the app the foreground keyboard target. Targeted diagnostic records confirmed `foregroundWasTarget: false` in both cases. The harness now foregrounds the retained app window and verifies keyboard ownership before sending keys; no product shutdown workaround was added.

PASS: the rerun at `.ai-usage-local/AIU-007/guest-evidence-1414/focus-1415` executed all six actual Windows scenarios with zero failures/skips and exit code zero. The harness DLL hash was `53D704AEA75707ED092CA6B22CB421D9FE04BACD283E3B77C9B209A0FCA7C9C6`. Inspected screenshots show masked synthetic manual input and an empty field after cancellation/reopening. The guest has no working HTTPS browser association and displays a shell dialog; no provider request or consent occurred. The pass establishes app controls and lifecycle, not working browser authentication. The manual-form image is clipped below the form on the small guest display; buttons were separately exercised through UIA.

The frozen independent reviewer reported two material cases: same-identity reconnect replaced the prior grant before initial quota succeeded, and present account/organization objects without UUIDs inherited old identity on refresh. Primary regression tests reproduced six failures: three reconnect outcomes (401, network error, cancellation) and three malformed identity containers. The fixes preserve the previous generation for every failed reconnect and reject malformed present identity. Refresh-token rotation still persists before later quota cancellation. PASS: 130 Infrastructure tests and 16 Presentation tests, zero failures/skips, after these fixes. The frozen review is still finishing; final package verification and live acceptance remain outstanding after the fixes.

## Final reviewed implementation candidate

Product reference: `bb25558dd42743c45f52bcabcbd165a34d348aff`. The independent `gpt-5.6-luna` reviewer used `max` reasoning in a fresh, read-only detached worktree at `97ca3f20ab7734c5b3189707221c045fc573aafc`. Its original verdict was FAIL for the two material issues above; shared HTTP/loopback, protected staging/recovery, cancellation/overlap/shutdown, composition, console and redaction had no additional material finding. The targeted follow-up reviewed `97ca3f2..bb25558` and returned PASS for both fixes, with no new material finding. Reviewer `git diff --check`: PASS. Reviewer tests/UI: NOT_RUN because that isolated checkout lacked restored assets; it did not restore or perform account, network, credential, signing or installation work. Primary test execution is reported separately.

Final signed package: `2026.9.1416.0`, SHA-256 `CAC4D6DB605C87E01309CAAF83D934685A76EC4BFAC3F584D984408A4CCC1DC2`. Actual guest install/update and signature validation: PASS. Host signing verification remains untrusted as expected; no host trust was changed. The first 1416 run detected Windows refusing programmatic foreground activation. The harness now falls back to a real title-bar click and verifies the target owns keyboard input before sending keys; it still tests Enter rather than substituting button invocation.

PASS: final actual guest run started `2026-09-14T21:19:03Z`, `.ai-usage-local/AIU-007/guest-evidence-1414/activation-1416/report.json`, all six scenarios, zero failures/skips, all retained app processes exited with code zero. Harness DLL SHA-256: `24C02851A7BBBB0CA37353DB6228EED1BEFBB1C66BCE6AE14396348F299503CF`. Primary inspected the actual manual-input, canceled/reopened-input and restored-window screenshots. Synthetic input is masked and cleared; the unsupported notice is visible. The offline guest's missing HTTPS association and small screenshot viewport remain explicit evidence limitations; no live browser/provider claim follows from this smoke.

Final supporting checks: PASS, all 78 project-validator regressions with zero failures/skips; PASS, canonical document validation with zero diagnostics; PASS, ProviderConsole Release build with zero warnings/errors; PASS, primary scoped diff/acceptance review and `git diff --check`. These used the pinned local SDK and existing restored packages. The native packaging build retains only the previously recorded missing-symbols-tool warning.

Host preparation: updated the existing `AiUsage.Dev_951d0pt9hnds0` development registration from 2026.9.1301.0 to 2026.9.1416.0 using the verified Release manifest. No package reset, credential import, host trust change or dependency upgrade occurred. Native UI inspection confirmed the existing Codex account still displays quota and the Claude panel shows no connection with the unsupported notice. The app is open for owner-led sign-in. Live Claude connection, initial quota, renewal, restart/resume and disconnect remain NOT_RUN until that consent and subsequent checks occur.

| Criterion | Current verdict | Evidence or remaining gate |
| --- | --- | --- |
| AC-01 | PASS | Exact current source and official restriction recorded; private scope is not provider approval |
| AC-02 | BLOCKED | Protocol/failure regressions pass; actual Claude account consent and initial quota await the owner |
| AC-03 | PASS | Protected-state, identity, renewal/cancellation, reconnect and deletion regressions; existing Codex state remains usable. Live Claude lifecycle remains part of AC-06 |
| AC-04 | PASS | Synthetic parser and presentation checks preserve distinct groups/units/unknown/stale values; live values remain unverified |
| AC-05 | PASS | 16 workflow/presentation regressions and six actual Windows scenarios; provider isolation, cancellation and exit verified |
| AC-06 | BLOCKED | 130 Infrastructure tests, 16 Presentation tests and final packaged UI pass; live Claude lifecycle awaits consent |
| AC-07 | BLOCKED | Independent findings resolved and reviewed; final live gate and local-main integration remain outstanding |

The latest private scope keeps every commit local. Local main is still `1e4f0ce`; no remote publication, main push or release occurred. Exact next action: the owner completes Connect Claude in the open AI Usage window, enabling live quota, renewal/resume and disconnect verification before integration.

## Owner-led live host verification, 2026-09-15

Candidate: `e02732131c47245aab61e6d5fc04ccddbb18d3b0`, product source unchanged from reviewed `bb25558`. Installed host package `AiUsage.Dev` version `2026.9.1416.0` was confirmed before launch. The owner explicitly requested the normal desktop instance and completed Claude sign-in. Windows Computer Use observed the actual app; this was not Sandbox or a synthetic provider. No source CLI credentials, protected credential contents, browser codes, cookies or raw provider payloads were read or recorded.

| Check | Verdict | Direct observation |
| --- | --- | --- |
| LIVE-01 connection and initial quota | PASS | Owner-led sign-in produced Account connected, separate Claude 5 Hour and Claude 7 Day remaining values and reset times, and disabled extra-usage information with explicit USD units and no cap reported |
| LIVE-02 refresh | PASS | Clicking Refresh completed with current quota rows, enabled controls and no error or stale-cache notice |
| LIVE-03 renewal and durable resume | PASS | Exit closed the process, confirmed by the app inventory and process query. Relaunch created a new window and restored current Claude quota without browser consent or a stale-cache notice. In this implementation access tokens exist only in memory; startup ResumeAsync renews from the protected refresh grant before fetching current quota. Thus the observed fresh result exercises the real refresh exchange and persisted grant, rather than merely displaying cache |
| LIVE-04 disconnect | PASS | Clicking Disconnect removed the quota rows, showed No accounts connected and disabled Refresh/Disconnect |
| LIVE-05 durable removal and provider isolation | PASS | After another confirmed full process exit and relaunch, Claude still showed No accounts connected. Codex retained its existing connected quota display |

After LIVE-05, Connect Claude reopened browser consent for the owner to restore the connection. A subsequent actual app observation confirmed Account connected and current quota again; the app was left connected. Screens were inspected directly in the task; private account data and screenshots were not added to Git. Neither host trust nor installed dependencies changed during this run.

Live limitations: the specific callback return mechanism was not separately instrumented; occupied-port and isolated manual fallback, reduced scopes, deliberate invalidation/throttling, malformed provider responses and induced network failure were NOT_RUN live. Their applicable failure behavior remains covered by the protocol, persistence and presentation regressions. No billable inference or destructive remote account action was used to manufacture evidence. Private API behavior and provider permission remain distinct; Anthropic's documented restriction still applies.

AC-02 and AC-06 live gates now PASS. AC-01, AC-03, AC-04 and AC-05 retain their recorded source, test and UI evidence, supplemented by these live results. AC-07 independent review is PASS; the remaining action is final combined-candidate checks and local-main integration. The implementation diff has not changed since the reviewed and packaged product candidate.

Final combined-candidate checks on 2026-09-15: PASS, Infrastructure Release 130/130; Presentation Release 16/16; project-validator regressions 78/78, all with zero errors/failures/skips/not-run tests. PASS, document validation with zero diagnostics; PASS, `git diff --check`. The commands used SDK 10.0.401 and `--no-restore`. The reviewed product tree is unchanged; only these evidence records changed after live verification. Main is an ancestor of the candidate, so integration can preserve the complete selected AIU-027 base and intervening existing history through a fast-forward without conflict resolution or rewriting commits.
