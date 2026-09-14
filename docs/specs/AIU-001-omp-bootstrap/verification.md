---
id: AIU-001
status: done
---
# Execution verification

AIU-001 local implementation and acceptance are complete. Native runtime probes, tests and independent review executed on 2026-09-12. This is not release approval or a claim of remote CI/publication.

Implementation fingerprint: SHA-256 `80a4657db727788488d31bf54b15fcdf3061f36aee6fe5064269d357b965e17a`, calculated from the sorted path/SHA-256 records for 26 implementation, test, configuration and workflow files. Independent review inspected frozen commit `9afa9d57e09b7e8af0d904d25c03774d6118352f`, its file inventory and actual captured check output. Subsequent changes close documentation and record non-blocking findings; implementation is unchanged. Environment, native session references and failed-probe history are in [environment.md](../../workflow/environment.md).

Historical scope notice: the matrix and fingerprint below describe bootstrap closure, not every later checkout. The owner amendment on 2026-09-13 supersedes the blanket ordinary-PR review/protection requirements and removes formatting from current CI; see [current policy](../../workflow/verification.md). Deferred main protection in AIU-026 is no longer a current development PR blocker. The original runtime/review evidence is not retroactively rewritten.

## Acceptance matrix

| AC | Verdict | Observed evidence |
|---|---|---|
| AC-01 | PASS | All 25 canonical documents matched the embedded baseline at adoption; D-001 through D-176 occur once in sequence. Only docs/backlog.md is maintained. Authored deliverables are English. |
| AC-02 | PASS | Actual Windows, stable OMP 18.1.18, schema, role availability, SDK, repository, identity and remote-permission facts recorded without credential dumps. |
| AC-03 | PASS | Fresh native SDK and actual repository-root terminal sessions discover configuration/rules/five skills and display G-001/AIU-001 without inherited primary chat. The final launcher loaded the current extension successfully. |
| AC-04 | PASS | Native ranked recommendation, Another AIU and cancellation exercised; cancelled dialogs left goals.md bytes unchanged. No built-in command collision. |
| AC-05 | PASS | 49 xUnit tests passed, zero errors/failures/skips. Invalid IDs/statuses/references/cycles/ACs/paths/ownership and valid documents covered; canonical root validates. |
| AC-06 | PASS | Two real native isolated workers returned inspected patches without parent application. The primary integrated exact outputs; wrong ownership, overlap, unsafe patches and main-branch integration were rejected. |
| AC-07 | PASS | Explicit pause during live isolated workers retained task/scope/base state and left parent outputs absent. Restart required confirmation. Unfinished tasks were not converted to done by interruption or patch integration. |
| AC-08 | PASS | AIU-903 completed once and handed off to a real fresh AIU-904 session. A final-guard handoff retained history/scope/base and reached owner pause at 16/24 steps. A separate two-step grant stopped and aborted at 2/2 without false completion. |
| AC-09 | PASS | Read-only Advisor attachment/yield observed. The owner-authorized replacement reviewer used Claude Opus 5, memory off, read/grep/glob only and zero initial messages, with no inherited primary narrative. Verdict PASS: no BLOCKER/MAJOR, three MINORs recorded in the backlog. Empty PASS accepted by the setup check. Review/capture failures and the explicit recovery exception are disclosed below. |
| AC-10 | PASS | Native headless write/process-launch denial, live-grant enlargement rejection, primary integration and budget/revision/scope/branch checks exercised. Remote ADMIN permission, absent main protection and disabled private reporting were actually checked, not treated as safe merge authority. |
| AC-11 | PASS | Git-portable source/configuration/docs inventory excludes local profiles, credentials, sessions, input archive and probes. Targeted private-path/key/token/secret-pattern scan had no matches. Real owner-supplied DCO identity and authorized MIT replacement used. |
| AC-12 | PASS | Tasks, specification, goal and backlog close AIU-001 local scope only. Verified commands, known limits, deferred MINORs and remote/product NOT_RUN/BLOCKED gates are explicit. AIU-002/003 remain unstarted and require new authorization. |

## Executed checks

Final captured run began at 2026-09-12T23:04Z. The optional user-local SDK was invoked through the verified PowerShell prefix `& "$HOME/.dotnet/ai-usage-sdk/dotnet.exe"`; PATH was not changed.

| Check | Result |
|---|---|
| `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore` | Exit 0; 49 tests, 0 failed/errors/skipped/not-run. |
| `bun test tests/omp-workflow` | Exit 0; 11 tests, 74 assertions, 0 failures. |
| `dotnet run --project tools/AiUsage.ProjectValidation --no-build -- --root . --json` | Exit 0; `{"valid":true,"diagnostics":[]}`. |
| `dotnet format tests/AiUsage.ProjectValidation.Tests/AiUsage.ProjectValidation.Tests.csproj --no-restore --verify-no-changes` | Exit 0; no formatting changes required. |
| `dotnet format tools/AiUsage.ProjectValidation/AiUsage.ProjectValidation.csproj --no-restore --verify-no-changes` | Exit 0 at 2026-09-12T23:51Z; direct post-review check of validator source formatting. |
| `bun tools/start-work.ts --no-title --no-lsp` | Actual native terminal startup, discovery, selection/resume/integration/handoff/status surfaces exercised. |
| Native `/work verify` | `Document validation PASS` after checked integration. |

The regression work included observed failures before fixes for unsafe wildcard tails, a reparse ancestor, uncertain ownership overlap and live-grant mutation. Historical unsuccessful runtime probes remain documented rather than being relabeled as passes.

## Explicitly unexecuted or blocked

- **NOT_RUN:** GitHub Actions execution, remote push/PR/merge, product Windows UI/MSIX/install/update, production signing/release and live provider-product integrations.
- **BLOCKED for publication:** Main has no independent branch protection; required exact-head remote checks/review are not established. Private vulnerability reporting is disabled. No bypass or configuration change was performed.
- **No unresolved local test failure or material review finding.** Three non-blocking follow-ups remain in the canonical backlog; PASS is not a guarantee of defect absence.

## Limits of the proof

Native isolation is not an OS sandbox; arbitrary actions inside an already permitted shell/eval/process call are not individually mediated. The budget is guarded call/transition accounting, not a token/cost/primary wall-clock cap. Native worker setup exposed an MSYS ps hang outside the effective worker deadline; the tested launcher avoids that specific Windows path, not every possible setup stall.

Native Plan approval remains separate. The successful execution/automatic-handoff probes initially paused it through the native `/plan` command; the bridge does not silently bypass that approval surface. The demonstrated automatic path is local bounded continuation, not unattended GitHub delivery. The primary remains responsible for canonical status reconciliation, meaningful acceptance evidence and Git checkpoints.

The automated language/link scan covers docs, the selected root documents and the configured .omp policy/agent/skill/library/extension files, not every tools/tests/.github source file. Repository-wide English policy still applies; review supplements this partial heuristic. The CI formatting step targets the test project; validator source formatting was additionally checked directly above. A process killed while holding the workflow lock can leave a stale lock requiring manual intervention; automatic stale-lock recovery is not implemented. These three MINOR follow-ups are recorded as CR-AIU-001-01 through CR-AIU-001-03 in [the backlog](../../backlog.md).

## Independent review

The completed authorized replacement returned **PASS**, with no BLOCKER or MAJOR findings and three MINORs. Session `01a09800-d575-7784-90d8-84fdb1a6a2be` used Anthropic Claude Opus 5 throughout, fresh in-memory context, memory/advisor off and only read/grep/glob. Its finite deadline was 20 minutes; it finished in 332.72 seconds. No implementation changes or post-fix full review followed.

Recovery history is part of the result, not hidden:
- The first bounded attempt ended after 601.54 seconds without a verdict.
- The next review completed, but the disposable runner disposed its in-memory session before saving the response. The final text was lost; empty post-disposal usage/messages were not accepted as evidence.
- The owner explicitly authorized **one replacement review**. Before it ran, a no-inference native lifecycle probe verified capture before disposal; terminal text was also captured independently in the event record.
- The replacement returned introductory prose plus a JSON fence. The strict whole-response parser exited 1, but the complete response had already been saved. Its single JSON fence was extracted and schema-validated without further inference. The actual verdict is PASS; this does not relabel the runner's parser exit as success.

Raw local evidence, frozen inventories and the recovery authorization remain in the ignored evidence directory. Durable findings and the redacted outcome are recorded here and in the canonical backlog; throwaway runners and fixture repositories are removed after capture.

## Ordinary interactive tool-gate repair

The owner authorized a narrow post-bootstrap repair: ordinary interactive sessions use native OMP permissions without selecting a product goal; `/work` explicitly opts a session into bounded execution. Unarmed headless denial and checked primary integration remain separate. No product execution grant or approval-setting change was made.

- Before the production edit, the actual extension-handler child harness failed all eight scenarios. Ordinary `bash` was blocked as unarmed; headless denial and later post-pause/revocation calls incorrectly aborted the turn.
- After repair, `bun test tests/omp-workflow/tool-gate.test.ts` passed its subprocess wrapper. Direct child execution passed eight scenarios and 59 assertions, including real durable spending, exact budget exhaustion, cancellation, pause/revocation, switched/transferred sessions and the independent checkpoint denial.
- `bun test tests/omp-workflow` passed 12 tests and 75 assertions. The child-local mocks replace only legacy SettingsManager registration plumbing and document validation; durable work transitions and disposable Git branch checks remain real. This harness does not claim production acceptance of malformed documents.
- Actual normal `bun tools/start-work.ts` startup through a managed PTY displayed the project `/work` summary and native Plan mode with extension discovery enabled. Without selection/resume, same-turn shell and configuration reads completed: `fnm --version` returned `fnm 1.39.0` in 0.14 seconds, and `.omp/config.yml` was read successfully. The answer reported both without the extension-induced interruption.
- The normal session wrote `local://gate-smoke-plan.md` and proposed it through `xd://propose`. Native Plan Review displayed approval choices; no execution was approved. An earlier proposal attempt referenced a not-yet-written artifact and was correctly rejected by native proposal validation before succeeding. The managed smoke process was then stopped.
- `git diff -- .omp/config.yml tools/start-work.ts docs/product/goals.md` was empty. The workflow documentation now explicitly states ordinary interactive YOLO execution without per-call confirmation; native Plan approval remains separate.
- `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` using the existing user-local SDK returned `{"valid":true,"diagnostics":[]}`.

Focused independent privilege review inspected the initial frozen extension SHA-256 `a6a93da318bd9a3ff9738abb069004b701e9cbabbb168c7c175b3d290fa79a8d` and boundary-test SHA-256 `409932db8aefdfff19a7553bdd4621dc2b9347b58d41de135141380a1828ed72`. The installed native SDK resolved the profile-local Anthropic Claude Opus 5 reviewer with exactly read/grep/glob, memory off, autolearn/advisor off, native Plan startup off and zero initial messages. The review used one prompt, no primary conversation, a 600-second deadline and an in-memory session. It completed in 141.57 seconds; recorded usage was 20 input, 9536 output, 171004 cache-read and 40910 cache-write tokens. Two setup attempts failed before prompting (runner string syntax and an invalid settings key); neither was treated as a review.

The review returned **BLOCKED** with one MAJOR and one related MINOR: rejected native session creation could leave a pending transfer, and a headless lifecycle event could leave the same intent unconsumed. Primary reproduction added three failing handoff scenarios for rejection, an intervening headless event and a mismatched native parent. The repair now clears transfer intent on all session events and session-creation exits, requires a distinct incoming native ID with the recorded parent session file, and disarms failed/cancelled handoffs. All eleven boundary scenarios then passed with 74 assertions. Existing successful transfer, cancelled handoff, ordinary access, pause/revocation and budget cases still passed. The two findings are resolved by primary verification; this is not an invented independent PASS or a repeated full review.

Final advisory closure additionally reproduced silent live-primary revocation from a headless lifecycle event. Headless lifecycle now consumes transfer intent without changing the primary's live authority. The child wrapper also rejects missing or fewer than twelve executed scenarios instead of trusting exit success alone. Final direct boundary execution passed 12 scenarios with 77 assertions; final `bun test tests/omp-workflow` passed 12 tests with 76 assertions. The suite count includes the single subprocess wrapper, not its nested scenarios.

Final extension SHA-256: `8542bf452b03b8d9bc978e75c8830789ff5ed63722505685d3a17d58e78b6ce6`. Final boundary-test SHA-256: `4e2280453ce58a4a84ef93593d776b9669a0a20a8d86f25c67e22a810939edf0`. Raw frozen references, review preflight/result/usage and post-fix evidence are retained under the ignored `.ai-usage-local/tool-gate-review-ugIqtf/` directory. No unresolved review finding remains. The live ordinary-tool/proposal smoke preceded the focused handoff hardening; the latter was verified by affected handler regressions, not claimed as another live OMP handoff.

## Next work

AIU-002 is the next runnable Windows package milestone. AIU-003 supplies Codex authentication/quota feasibility and contract evidence. Both require their own selection/authorization; neither was started under this bootstrap.
