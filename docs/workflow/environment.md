# Bootstrap environment and evidence

Observed 2026-09-12 through OMP on the owner workstation. Repository paths below are relative; the checked Git root was the current `ai-usage-app` checkout under the owner's projects directory. No credential stores or tokens were inspected.

## Preflight

- `git rev-parse --show-toplevel`, `git status --short --branch`, `git log -1 --format=%H`: existing repository, main tracking origin/main, base `e96b735975ced39646ab16fe6f9d38897ae0cc3c`. Only the supplied bootstrap directory was untracked. Existing README and GPL-3.0 LICENSE found.
- Origin: `https://github.com/rozputnii/ai-usage-app.git`. `gh repo view rozputnii/ai-usage-app --json nameWithOwner,viewerPermission,defaultBranchRef,isPrivate`: public, main, ADMIN. This is broad ambient authority, not least privilege.
- `gh api repos/rozputnii/ai-usage-app/branches/main/protection`: HTTP 404, Branch not protected. No remote mutation is authorized or performed. Local guards cannot supply independent remote protection.
- `git var GIT_AUTHOR_IDENT`: initially failed (missing identity). Owner supplied `rozputnii` and `daniil.rozputnii@gmail.com`, authorizing repository-local configuration. Feature branch: `feature/AIU-001-omp-bootstrap`.
- Owner explicitly authorized replacing the existing license with MIT and installing a user-local official .NET 10 SDK. No machine-wide settings, elevation or paid resources.
- Owner clarified the CLI boundary: use already-authorized `gh` for all GitHub operations; local-only Git remains permitted for branches, commits and isolated patch checks. No authentication changes or protection bypass.
- `gh api repos/rozputnii/ai-usage-app/private-vulnerability-reporting` returned `enabled: false`. No private reporting channel was enabled or invented.
- Windows: x64, build 26200.9445, DisplayVersion 25H2. Windows registry ProductName returns legacy `Windows 10 Pro`; build and workstation identify Windows 11. This discrepancy is not hidden.
- `dotnet --info`: host 10.0.12, .NET and WindowsDesktop runtimes 9.0.5 and 10.0.12; initially no SDK. Alternate standard SDK roots and Windows Kits Include directory absent. Windows SDK remains an AIU-002 prerequisite, not a reason to change the stack.
- Official .NET metadata resolved SDK 10.0.401 at `https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json`. The win-x64 ZIP was SHA-512 verified before extraction: `24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430`. Installation is user-local under `.dotnet/ai-usage-sdk`; PATH unchanged.

## OMP compatibility

- `omp --help` and installed package metadata: 18.1.18. GitHub latest release API returned stable v18.1.18, not a prerelease. No upgrade or historical pin applied.
- Installed `src/config/settings-schema.ts`, `src/advisor/config.ts`, extension types and native task documentation are the API references. Public provenance: `https://github.com/can1357/oh-my-pi/tree/v18.1.18`.
- `omp --profile ai-usage config path` resolved the named profile. Only specific nonsecret settings were queried. No full settings/auth dump.
- Profile-local roles are configured: default/plan GPT-6 Astra; slow/advisor Claude Opus 5; smol Gemini 3.8 Flash. The model catalog lists these selectors. Catalog membership is NOT proof of authenticated inference; runtime checks are recorded separately.
- Verified settings: `memory.backend` enum; `autolearn.enabled`, `plan.enabled`, `plan.defaultOnStartup`, `advisor.enabled`, `async.enabled`, `task.batch`, `task.isolation.enabled`, `task.isolation.apply`, `compaction.enabled`, `compaction.asyncEnabled` booleans; `task.maxConcurrency` and `task.maxRuntimeMs` numbers; `task.isolation.merge` enum; `advisor.syncBacklog` enum requiring string `"1"`.
- Advisor tools are NOT an `advisor.tools` setting. Installed WATCHDOG YAML schema supports explicit `tools: [read, grep, glob]`. Concrete model selection stays in the ai-usage profile.
- Native isolation is explicit per spawn. `task.isolation.apply: false` retains patches without automatic parent integration; `merge: patch`. Isolation and textual ownership are not an OS sandbox.
- No language server configured at preflight.

## Baseline adoption

The full embedded goal was read. A bounded record parser found all 28 records: 25 canonical docs and three passive OMP seeds. All 25 docs matched their embedded text exactly immediately after adoption. Accepted headings contain every sequential D-001 through D-176 exactly once. The original handoff directory is preserved and ignored as immutable, non-authoritative input; only `docs/backlog.md` is maintained.

## Runtime verification

- SDK execution: user-local `dotnet.exe --version` returned 10.0.401. Bun 1.4.2; Git 2.55.0.windows.5.
- Negative validator probe: `dotnet run --project tools/AiUsage.ProjectValidation -- --root . --json` failed because the project did not yet exist. This proves the pre-implementation gap, not behavior-test coverage.
- The first .NET CLI invocation automatically created an ASP.NET development HTTPS certificate despite the first-experience flag. No trust command was run. Subsequent project invocations set `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`. This is not an MSIX signing certificate.
- Fresh installed native SDK session: GPT-6 Astra primary, Claude Opus 5 advisor active with native `advise` plus only `read`, `glob`, `grep`; memory local; project map and essential rules found; all five authored skills discovered; no extension-loading errors. This verifies attachment and tool grants, not a completed advisor inference or final review.
- Programmatic SDK probes must use `Settings.loadReadOnly({cwd, agentDir})`. `Settings.init` can reuse an already initialized global singleton; an initial probe observed the default profile's roles. The corrected probe resolved the exact ai-usage profile roles without changing either profile.
- Initial print-mode probe called `ctx.shutdown()` at session start but print mode still started its prompt and hit its 20-second deadline. It is not a PASS shutdown test. Native SDK disposal completed successfully in the corrected discovery probe.
- Native task batching returned isolated implementation patches with automatic application disabled. The primary checked and applied the test-first validator patch and workflow-engine patch. One corrected validator worker hit its eight-minute runtime limit without a captured patch; the primary implemented that source afterward. A first coordinator attempt and a context-misassigned batch were not successful implementation runs.
- The actual CLI inference probe returned `AIU_PROBE_OK` on GPT-6 Astra, session `01a09748-7248-7657-89be-44786402e2d0`, with 3,085 reported total primary tokens and a native advisor-yield event. This is not a provider-product test.
- Validator: 49 xUnit tests passed, zero failures/errors/skips; canonical root returned `{"valid":true,"diagnostics":[]}`. Wildcard-tail path tests first failed for `src/**/../outside` and `src/**/.git/config`, then passed after rejecting nonterminal wildcards. An outside-root read through a reparse `.omp` ancestor was reproduced and fixed; its regression passes.
- Bun workflow/patch tests: 11 passed, 74 assertions. They exercise explicit grants, stale revisions, pause/resume, bounded steps, completion replay, concurrent transitions, actual Git patch integration, main-branch denial, unsafe ownership and live-grant mutation. Uncertain-glob ownership and edited durable grant regressions failed before their fixes.
- Native extension discovery includes `/work`, with no load errors. Installed built-in registry: 86 reserved names; neither `work` nor `ai-work` is reserved. Native terminal verification in an isolated disposable repository displayed ranked AIU-901/902, recommended AIU-901, accepted the Another AIU input for AIU-902, and cancelled both dialogs without changing goals.md.
- A native terminal toast replaces another toast emitted in the same frame. The startup handler now emits goal/status and eligible work in one notification; a fresh actual terminal matched `AI Usage:` within 2.5 seconds. Earlier readiness attempts expecting an overwritten toast were not passes.
- Native `sendUserMessage` with `deliverAs: followUp` queued rather than started an idle session. The bridge now uses the ordinary native message-start path and explicitly activates the native Goal tool. Native Goal creation/completion and deferred project checkpoint execution were observed; they are separate states.

See `../specs/AIU-001-omp-bootstrap/verification.md` for criterion verdicts. Configuration discovery alone does not prove the complete workflow.

## Native isolation, integration and recovery

- A native SDK batch ran two real isolated workers concurrently with automatic application disabled, session `01a09783-ccea-74f6-8a7d-c2f2515f487e`. Native reported durations were 9.263 and 6.824 seconds, two requests each; batch duration 9.902 seconds. Reported aggregate usage was 61,272 tokens, including cache accounting, not a product-provider quota result.
- Both returned patches were read completely. The parent outputs were absent before primary integration. Actual `/work integrate` rejected the wrong task/path pairing, then correctly applied the left and right patches; exact parent bytes were `T01\\n` and `T02\\n`. Integration while paused was rejected.
- A compiled native CLI blocking-role probe also completed, session `01a09798-b605-73ba-844c-7f13794e9e17`. Both inspected one-file patches contained LEFT/RIGHT and remained unapplied in the parent.
- A terminal isolation attempt exposed a concrete Windows launch issue: native OMP ownership setup spawned two Git/MSYS `ps -o lstart= -p <native-pid>` processes that remained stuck before worker registration, beyond the configured worker runtime limit. `src/task/isolation-ownership.ts` falls through to Unix ps on non-Linux platforms. This preparation path is not a proven hard wall-clock bound.
- A process-local native Windows PATH, with directories containing MSYS/Cygwin runtime DLLs removed, left Git available and made Unix ps unavailable. Native isolation then reached real running workers. The portable `bun tools/start-work.ts --no-title --no-lsp` launch was exercised successfully; no OMP install, profile or machine PATH was modified.
- Worker inspection through native Agent Hub showed actual isolated file writes and an avoidable parent-reply wait for a patch path. OMP captures that path only after the worker finishes. The role now requires finish/yield without waiting for that future path. The corrected actual terminal batch, session `01a097b8-f398-759d-a37e-75fda4d1f398`, completed both work-worker entries: two requests each, native durations 6.6 and 9.4 seconds. Both returned patches were inspected and the parent outputs remained absent.
- Owner pause was issued while two native isolated workers were running. The durable G-902 state remained paused at 4/40 steps with the same starting reference and no completed items; parent outputs stayed absent after pause and later shutdown. The displayed zero async jobs was not treated as proof that all workers had terminated.
- Disposable helper commands only narrowed native tools for controlled probes; they did not mock sessions, workers, patches or provider responses. They are not production commands.

## Fresh sessions and bounded authority

- The first G-900 scenario reached its real 12/12 step limit without a false project completion. A native Goal completed, but the deferred work checkpoint rejected stopped authorization. This was a budget-stop result, not a successful fresh handoff.
- A separate G-901 grant authorized only AIU-903/904 with 24 steps. AIU-903 verified already integrated disposable outputs and was checkpointed once; it was not represented as a second implementation batch. Native session `01a09799-eb4c-71d5-85e9-4259af8a72e1` handed off to `01a097a0-599f-7105-b87b-e18b51822796`. The persisted state had AIU-903 completed, AIU-904 current, unchanged scope/starting reference, and 8/24 steps consumed when owner pause took effect.
- With the final live-grant guard and native launcher, the same G-901 record was explicitly resumed and handed off again: native session `01a097cb-d163-721d-b9c2-c1374f9af4ee` to `01a097ce-987d-704b-b445-b4d00d9d6597`. The transition preserved completed AIU-903, current AIU-904 and the original scope/base; consumption moved from 8 to 10 before new work, then to 16/24 when owner pause stopped the fresh turn. No budget or completed-item history was reset.
- A separate G-903 two-step grant exercised the final native-turn abort path. Session `01a097aa-1fe1-7549-8ce7-5093250e1cb0` stopped at 2/2, retained AIU-906 as unfinished and recorded no completed items. Native tool execution reported an aborted run rather than continuing after exhaustion.
- External restart of the paused G-902 state refused `/work run` with `Confirm selection or resume in this session first`. Confirmed resume preserved scope and increased consumption rather than resetting it.
- Actual headless primary attempts were denied for both file write (session `01a097a7-b327-776b-9db0-fda1bad903d0`) and native Hub process launch (session `01a097ad-2149-75d5-8808-0c338ac73afe`). Native tool results reported aborted execution; neither target file existed.
- Editing a live confirmed disposable limit from 40 to 41, without a new owner grant, caused `/work run` to reject `Authorization changed after owner confirmation`. The UI retained the confirmed 7/40 state and no parent output appeared. The original test record was restored; its budget was not replenished.
- After restoring that test record and explicitly reconfirming, the final guarded primary integrated both inspected LEFT/RIGHT patches. Exact parent bytes were `LEFT\n` and `RIGHT\n`; consumption reached 9/40. Native `/work verify` reported `Document validation PASS`; integration did not change pending tasks into done.
- A deliverable-only scan found no owner-home path literals or common private-key/API-token/secret-assignment patterns. The file inventory excluded bootstrap input, build output, disposable probes and local profile/session stores. This is a bounded check, not a guarantee against every possible secret encoding.

## Review readiness and CI

- The independent-review setup-only probe made no review inference. It resolved Anthropic Claude Opus 5, exactly read/grep/glob tools, memory off, advisor off and zero initial messages. The report parser accepted PASS with an empty findings array. The full one-shot review remains separately recorded in the criterion report.
- C-sharp formatting was applied through the existing .editorconfig and then passed `dotnet format ... --no-restore --verify-no-changes`. Both owned projects treat warnings as errors.
- The local GitHub Actions workflow pins official checkout v7.0.1, setup-dotnet v6.0.0 and setup-bun v2.2.0 commits, grants only contents-read permission and disables persisted checkout credentials. Its local test/validator/format commands were exercised. GitHub CI itself is NOT_RUN; main protection and private reporting remain absent.
- Final captured checks at 2026-09-12T23:04Z: xUnit 49 passed with zero failures/errors/skips; Bun 11 passed with 74 assertions. At 23:06Z canonical validation and the no-change formatting check exited 0. The PowerShell user-local SDK invocation documented in README was actually used.
