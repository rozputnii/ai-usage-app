# AIU-003 verification

## Scope and environment
- Date: 2026-09-13, live provider verification 2026-09-14.
- Owner-selected slice: UI-independent Codex library, console and synthetic protocol tests. AIU-002 UI acceptance remains paused and separate.
- Environment: Windows 11, OMP 18.1.19, user-local .NET SDK 10.0.401.
- Implementation: `AiUsage.Core.Usage` snapshots; `AiUsage.Infrastructure.Providers.Codex` browser and device authentication, refresh, read-only quota and typed failures; `tools/AiUsage.ProviderConsole` consumes that same library.
- Committed fixtures are synthetic. No personal CLI store was read or modified and no source credential was imported. A real browser sign-in and authenticated quota read were performed with in-memory credentials on 2026-09-14; no token or provider payload was persisted.

## Executed checks
| Check | Actual result | Scope |
| --- | --- | --- |
| `dotnet build tools/AiUsage.ProviderConsole/AiUsage.ProviderConsole.csproj -c Release` | PASS: zero warnings/errors | Console, Infrastructure and Core compile |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release -- -noLogo` | PASS: 54 tests, zero failures/errors/skips | Synthetic parser, browser/device authentication, loopback callback and HTTP protocol behavior |
| Actual console `--help` | PASS: exit 0 | Executable command surface, no network |
| Actual console `inspect tests/windows/AiUsage.Infrastructure.Tests/Fixtures/codex-usage.synthetic.json` | PASS: exit 0 | Separate pools; main remaining 75, exhausted additional pool 0, unknown additional pool null; explicit credit unlimited |
| Actual console malformed synthetic input | PASS: exit 3, typed InvalidResponse only | Raw synthetic-sensitive marker not emitted |
| Actual console noninteractive `login` | PASS: exit 2 before consent/network path | Refuses redirected execution |
| Actual interactive consent decline | PASS: disclosure shown; answer `n` exited 0 | No authorization request issued |
| Actual browser sign-in attempt 1, no `originator`, pre-parity build | BLOCKED: exit 3, callback carried a provider error value | Refused at the callback; no token exchange reached |
| Actual browser sign-in attempts 2-4, pre-parity build | BLOCKED: exit 3, no provider code and no HTTP status | The provider issued tokens; this implementation's own account-context policy refused them |
| Actual browser sign-in attempt 6, pre-parity build | BLOCKED: exit 3, `UnsupportedAccountContext`, restriction `ComputeResidency` | Live proof that authorization and token exchange succeed and the local policy was the blocker |
| Actual browser sign-in attempt 5 | BLOCKED: exit 3, `LoginAttemptExpired` | Consent not completed within the fifteen-minute deadline |
| **Actual live session on the parity build** | **PASS: exit 0** | Browser sign-in, real quota read, explicit refresh, second real quota read, clean exit |
| Focused independent security review | PASS: zero blocking findings, three low-severity follow-ups | Anthropic Claude Opus 5; fresh read-only session, memory off |
| Canonical project validation | PASS: `{"valid":true,"diagnostics":[]}` | `dotnet run --project tools/AiUsage.ProjectValidation -- --root . --json` |

Live verification succeeded on the parity build. A real browser sign-in completed, the console read this account's actual subscription quota twice - before and after an explicit in-memory refresh - and exited 0. The observed response carried `plan_type`, a five-hour primary window and a seven-day secondary window with distinct remaining percentages, explicit non-unlimited credits with a zero balance, and two available reset credits. No quota value was fabricated and no inference or purchase request was made. Account identifiers, tokens and raw payloads were not printed; nothing was persisted after the process exited.

Six earlier attempts failed on pre-parity builds. Attempt 6 identified the cause: the provider authorized the request and issued tokens, and this implementation then refused them because the account carries a `chatgpt_compute_residency` claim. That fail-closed policy was locally invented, not derived from either inspected client. Attempt 5 expired before consent.

Following an explicit owner instruction, the connection and usage read now mirror the locally cloned OMP implementation (`C:/Users/danii/projects/oh-my-pi`) instead of an independently derived policy, per D-177. The authorization URL, PKCE, loopback callback path, ports, token exchange, usage endpoint and request headers match `packages/ai/src/registry/oauth/openai-codex.ts` and `packages/ai/src/usage/openai-codex.ts`. Account-context claims are no longer read: that client extracts only the workspace identity, so residency and FedRAMP plumbing was removed rather than left as an untested second policy path. Local identity consistency remains fail-closed: conflicting access/id workspace claims and a workspace change on refresh are still rejected.

Deliberate wire deviations from that client, all in the direction of truthful self-identification: `User-Agent: AiUsage/0.1` instead of `omp/<version>`, `originator=ai_usage` instead of `omp`, and the standard .NET `Accept: application/json` transport header. The live session proves the provider accepts this combination.

The first implementation of the loopback callback used `HttpListener` with a `localhost` prefix. An actual non-elevated run showed HTTP.sys holding a wildcard `::` endpoint owned by the system process, which would accept off-machine requests carrying a loopback Host header. It was replaced with an explicit `TcpListener` bound to `127.0.0.1` with exclusive address use, verified by rerunning the flow.

Local console output is retained in `.ai-usage-local/AIU-003/console-smoke.json`. The SDK executable used for the commands above was `C:/Users/danii/.dotnet/ai-usage-sdk/dotnet.exe`; `dotnet` is notation for that verified executable, not a claim about global PATH installation.

## Behavior covered
- Unknown/missing, exhausted and explicitly unlimited pools remain distinct. No aggregation of unrelated additional groups, invented default windows or credit conversion.
- Absolute Unix-second reset precedence, relative fallback, overflow, nonnumeric/out-of-range percentages, duplicate group labels and malformed payloads.
- Selected workspace attribution, malformed/mismatched account IDs, expired credentials, 401 reauthentication, 403 denial, 429 Retry-After, 5xx unavailability and redirect rejection outcomes.
- Device initiation versus pending status semantics, consent polling, one-time code exchange, expiry before polling, deadline cancellation during a request and late-response rejection.
- Refresh-token replacement/omission, concurrent refresh coalescing, invalid grant, ambiguous network failure, cancellation before/after sending, and workspace-mismatch rejection across refresh.
- Bounded response size and secret-redacted exceptions, string representations and default JSON serialization of credentials.
- No source CLI credential read/write, token persistence, model/inference call or billing API substitution exists in this slice. HTTP clients registered through `AddCodexIntegration` disable redirects, cookies and factory request logging.

## Live account boundary
PASS on 2026-09-14: real browser consent, subscription endpoint access, account-specific response shape and an explicit in-memory refresh followed by a second successful quota read. Still NOT_RUN: device-code login availability, multi-workspace switching, exhausted-quota and rate-limited responses, long-term refresh rotation and coexistence with a personal CLI session. A successful request is not provider permission: third-party reuse of the public Codex client remains unresolved.

The console discloses client-reuse uncertainty and requires a separate interactive login. It accepts no token/API-key argument or arbitrary endpoint and never imports a CLI grant. Credentials remain process-local; closing the session drops references but does not promise secure zeroization of immutable managed strings or cloud grant revocation. Future durable credentials and UI lifecycle require their own accepted DPAPI implementation and verification.

## Acceptance matrix
| Criterion | Verdict | Evidence and boundary |
| --- | --- | --- |
| AC-01 | PASS | Immutable official Codex/OMP references and explicit source/live separation in `docs/providers/codex.md` |
| AC-02 | PASS, live | Real account quota retrieved twice through the console with no inference or billing-API call; parser tests keep unknown, unlimited and exhausted values distinct |
| AC-03 | PASS, live | Real browser authorization, code exchange and in-memory refresh executed without WinUI; cancellation, expiry and redaction covered by tests; no CLI store touched |
| AC-04 | PASS | One console consumes the shared library for offline fixture inspection and the authenticated live session; no second provider implementation |
| AC-05 | PASS | 54 deterministic tests plus actual console scenarios including the successful live session; remaining provider scope recorded NOT_RUN |
| AC-06 | PASS | Focused independent review PASS and canonical validator valid with no diagnostics |

## Independent review
- Frozen reference: `.ai-usage-local/AIU-003/focused-review-manifest.json`, 2026-09-13T18:09:06.443Z, 22 source/test/spec/evidence files with SHA-256 fingerprints. The review covered the pre-parity synthetic slice only. Browser login and the D-177 parity rewrite landed afterwards and were not part of that frozen reference; their evidence is the live session and the 54-test suite above.
- Fresh native OMP SDK session: `anthropic/claude-opus-5`, separate from the primary OpenAI model family. Exact tools `glob`, `grep`, `read`; memory off, autolearn/advisor off, zero initial messages, no inherited implementation chat, MCP/IRC/LSP or extension discovery. One review prompt, finite deadline, session disposed afterward.
- Actual result: PASS, no BLOCKER/MAJOR; three low-severity findings deduplicated into `CR-AIU-003-01` through `CR-AIU-003-03` in `docs/backlog.md`. One informational evidence-completion note is resolved by filing this review and running canonical validation. No full review/fix loop was run.
- Findings concern consumer-supplied HTTP pipeline hardening, unknown context claims on opaque refresh responses, and conservative usage-401/session guidance. They are recorded explicitly in provider evidence; the PASS is scoped to the current memory-only console, not future UI/persistence or unrestricted consumers.
- Retained actual output: `.ai-usage-local/AIU-003/focused-review-result.json`; 10 assistant responses and 30 tool calls. Reported usage: input 22, output 16,418, cache-read 270,139, cache-write 56,419 tokens; provider-reported cost 1.1098195.

## Delivery checkpoint
AIU-003 is complete: the owner-selected library and console are implemented and the provider path is live-verified end to end. AIU-002 remains paused; no other provider, UI integration, source-token import or durable credential work was started. The temporary malformed console fixture was removed after its output was retained; permanent synthetic regression fixtures remain under the test project. This verification record, the provider evidence file and D-177 are the change record; no separate release or changelog infrastructure was introduced.

Canonical validation rejected earlier metadata twice - an implemented spec paired with a blocked backlog item, then an unsupported status label - and the records were corrected rather than the validator weakened. With live verification complete, the backlog item is `done` and the spec is `implemented`, while provider scope that was never exercised stays explicitly NOT_RUN.
