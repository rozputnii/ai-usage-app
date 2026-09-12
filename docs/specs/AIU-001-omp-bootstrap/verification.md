---
id: AIU-001
status: review
---
# Execution verification

Local implementation and native runtime probes executed on 2026-09-12. The independent full review and final completion record are the remaining local gates at this checkpoint. This is not release approval or a claim of remote CI/publication.

Implementation fingerprint: SHA-256 `80a4657db727788488d31bf54b15fcdf3061f36aee6fe5064269d357b965e17a`, calculated from the sorted path/SHA-256 records for 26 implementation, test, configuration and workflow files. The one-shot review receives the frozen file inventory and actual captured check output. Environment, native session references and failed-probe history are in [environment.md](../../workflow/environment.md).

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
| AC-09 | NOT_RUN | Advisor attachment/read-only tools and native yield observed. Reviewer setup proved different-family Claude Opus 5, memory off, read/grep/glob only and zero initial messages; empty PASS accepted. The one full review has not yet run at this checkpoint. |
| AC-10 | PASS | Native headless write/process-launch denial, live-grant enlargement rejection, primary integration and budget/revision/scope/branch checks exercised. Remote ADMIN permission, absent main protection and disabled private reporting were actually checked, not treated as safe merge authority. |
| AC-11 | PASS | Git-portable source/configuration/docs inventory excludes local profiles, credentials, sessions, input archive and probes. Targeted private-path/key/token/secret-pattern scan had no matches. Real owner-supplied DCO identity and authorized MIT replacement used. |
| AC-12 | NOT_RUN | This checkpoint separates local passes from the remaining review/closure and unexecuted remote/product checks. Final report and completion metadata follow the single review. |

## Executed checks

Final captured run began at 2026-09-12T23:04Z. The optional user-local SDK was invoked through the verified PowerShell prefix `& "$HOME/.dotnet/ai-usage-sdk/dotnet.exe"`; PATH was not changed.

| Check | Result |
|---|---|
| `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore` | Exit 0; 49 tests, 0 failed/errors/skipped/not-run. |
| `bun test tests/omp-workflow` | Exit 0; 11 tests, 74 assertions, 0 failures. |
| `dotnet run --project tools/AiUsage.ProjectValidation --no-build -- --root . --json` | Exit 0; `{"valid":true,"diagnostics":[]}`. |
| `dotnet format tests/AiUsage.ProjectValidation.Tests/AiUsage.ProjectValidation.Tests.csproj --no-restore --verify-no-changes` | Exit 0; no formatting changes required. |
| `bun tools/start-work.ts --no-title --no-lsp` | Actual native terminal startup, discovery, selection/resume/integration/handoff/status surfaces exercised. |
| Native `/work verify` | `Document validation PASS` after checked integration. |

The regression work included observed failures before fixes for unsafe wildcard tails, a reparse ancestor, uncertain ownership overlap and live-grant mutation. Historical unsuccessful runtime probes remain documented rather than being relabeled as passes.

## Explicitly unexecuted or blocked

- **NOT_RUN:** GitHub Actions execution, remote push/PR/merge, product Windows UI/MSIX/install/update, production signing/release and live provider-product integrations.
- **BLOCKED for publication:** Main has no independent branch protection; required exact-head remote checks/review are not established. Private vulnerability reporting is disabled. No bypass or configuration change was performed.
- **No unresolved local test failure** at this checkpoint. This does not pre-judge the independent review or guarantee absence of defects.

## Limits of the proof

Native isolation is not an OS sandbox; arbitrary actions inside an already permitted shell/eval/process call are not individually mediated. The budget is guarded call/transition accounting, not a token/cost/primary wall-clock cap. Native worker setup exposed an MSYS ps hang outside the effective worker deadline; the tested launcher avoids that specific Windows path, not every possible setup stall.

Native Plan approval remains separate. The successful execution/automatic-handoff probes initially paused it through the native `/plan` command; the bridge does not silently bypass that approval surface. The demonstrated automatic path is local bounded continuation, not unattended GitHub delivery. The primary remains responsible for canonical status reconciliation, meaningful acceptance evidence and Git checkpoints.

## Independent review

Pending one read-only different-family review of this frozen candidate. No full review has been consumed by the setup-only capability/empty-report check. Zero findings is valid; material findings will receive corrections and targeted verification without a repeated full review.

## Next work

After verified AIU-001 closure, present AIU-002 (runnable Windows package) and AIU-003 (secure lifecycle foundation) for their own selection/authorization. Neither item has been started under this bootstrap.
