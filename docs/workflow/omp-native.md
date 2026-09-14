# OMP-native execution

Implemented for AIU-001 and exercised on Windows with stable OMP 18.1.18. Re-probe native APIs and configuration after upgrades; this is not a historical OMP version pin. Criterion evidence is in [verification](../specs/AIU-001-omp-bootstrap/verification.md).

## Four boundaries

1. Git documents hold goals, the single backlog, specifications, tasks, decisions and evidence.
2. Native OMP owns profiles, Plan/Goal modes, task execution, isolation, Agent Hub, Advisor, sessions and local memory.
3. The thin project bridge owns explicit selection, bounded authorization, validation and checked primary integration. It does not implement another conversation engine or scheduler.
4. GitHub enforcement is separate from local policy. Main protection is explicitly deferred in low-priority AIU-026 and is not a current development PR prerequisite; protected production operations remain separate.

## Launch and configuration

Run `bun tools/start-work.ts` from the checkout root. The launcher delegates to native OMP with the `ai-usage` profile. Its process-local Windows PATH normalization avoids the observed MSYS `ps -o lstart` hang in native isolation ownership setup. OMP's native unavailable-start-token fallback keeps PID liveness checking; it is weaker than a process-birth token. No OMP installation or machine/profile PATH is modified. Do not rely on inherited MSYS-only executables. Recheck the upstream behavior after OMP upgrades and remove this workaround when it is no longer needed.

The actual supported project configuration is `.omp/config.yml`:

- Local memory; automatic learning disabled.
- Plan mode enabled and on at fresh interactive startup.
- Advisor enabled with the schema's string `syncBacklog: "1"`; `.omp/WATCHDOG.yml` explicitly grants only read/grep/glob, plus native advice delivery.
- Native async jobs and task batches enabled; at most four workers; a positive five-minute worker runtime limit.
- Isolation enabled, patch return selected and automatic application disabled. Every write worker still explicitly requests `isolated: true`.
- Stable ordinary/asynchronous compaction enabled; no experimental context system.
- Native tool approval is currently configured as tools.approvalMode: yolo in .omp/config.yml. Ordinary interactive sessions inherit this setting even without /work selection or resume, so tools may execute without per-call confirmation. YOLO also applies inside an explicitly authorized /work workflow, where the project bridge separately enforces bounded authorization. Native Plan approval and explicit tool-policy restrictions remain separate; YOLO is not a security sandbox.

Concrete model roles and authentication remain in the local profile. The bridge uses the native session-scoped settings adapter, not a process-global settings singleton. The five small skills are project-work, feature-delivery, provider-evidence, security-lifecycle and convergence-review.

## Owner interface

`/work` did not collide with the 86 installed built-in command names. Native owner UI supports ranked eligible work, Another AIU and cancellation. Ranking uses goal alignment, work unblocked, risk reduction, inspectable output, milestone timing and size, weighted 30/20/20/15/10/5. Unknown estimates are explicit neutral estimates, not invented measurements. Scope and dependency eligibility precede scoring.

Verified owner operations include `/work select`, `/work run`, `/work pause`, `/work resume`, `/work resume auto` and `/work verify`. `/work status` presents the current canonical state. The registered interface also composes selection/start as `/work auto` and exposes checked `integrate`, `finish` and `handoff` transitions. There is no implemented `/work add` operation and no shadow of native `/resume` or `/goal`.

Native Plan approval is separate. The successful implementation/automatic-handoff probes first paused native Plan mode with `/plan`; the bridge does not bypass its approval UI. Select the native execution mode before starting automatic work. External startups still honor `plan.defaultOnStartup`.

Ordinary interactive tools use native OMP permissions; `/work` opts into bounded execution. A local planning artifact or native proposal is not a bounded execution grant and does not select a product goal. Selection alone does not start the bounded primary. The first grant confirms a goal's explicit item list and an integer budget from 2 through 10000. A fresh external process/session is unarmed even when the repository says active. Resume confirms the displayed recorded scope and consumption. A file cannot silently grant bounded permission. Do not manually edit the execution authorization to add scope or replenish a budget.

## Authorization and accounting

One JSON section in `docs/product/goals.md` stores the goal ID, allowed items, starting branch/commit, granted capabilities, cumulative steps, current item, completed items, native session references and stop reason. Revision checks, an exclusive transition lock and atomic replacement prevent two callers from spending the same recorded revision. The live process binds the confirmed grant and rejects scope/limit changes and observed accounting/history rollback.

Within armed bounded execution, steps reserve guarded native tool calls at the primary runtime boundary, plus selection/resume/completion/handoff/integration transitions. Rejected task/patch validation after a successful reservation still consumes its step; unarmed, stale or lock-conflicting calls fail before execution/reservation. Ordinary interactive calls neither load an execution grant nor spend its budget. Unarmed denied calls are tool-local rejections, not whole-turn aborts. Native transport calls can also consume steps. Worker calls are not charged individually to this counter; native task deadlines and native usage reports are separate. This is not a provider-request, token, money or primary wall-clock ceiling. Read-only pre-authorization inspection is not an execution grant.

Budget exhaustion stops further guarded work and aborts the native turn. Owner pause disarms continuation before aborting and persisting pause. The displayed native async-job count is not a claim that all agents or subprocesses have already stopped. Returned/late isolated outputs cannot automatically apply to the parent; a paused primary cannot integrate them. A crash leaves unfinished tasks unfinished.

Successful selection/resume, explicit pause and internal transfer latch that session into bounded enforcement for this extension process. Pause, revocation, budget/scope exhaustion, cancelled handoff and switching away never remove the latch. Switching back to a stopped bounded session remains restricted; a distinct never-opted-in interactive session remains ordinary. Failed or cancelled selection/resume does not opt an ordinary session in. Explicit pause latches before persistence, including when persistence fails.

External resume needs confirmation. Only a proven internal `ctx.newSession` transfer preserves live authority without another prompt: the incoming session must have a different native ID and its native header must name the recorded parent session file. Transfer intent is consumed on every lifecycle event, including headless events, and cleared on cancellation, rejection or return from session creation. Both native session-start and session-switch lifecycle events are handled. A closed OMP process does not continue working. Completed-item history prevents re-selection even while coarse document-status reconciliation remains the primary's responsibility.

## Workers and primary integration

Use declared, eligible, parallel tasks with the `work-worker` role, task-derived names such as T01, explicit isolation and disjoint write paths. Shared tasks/backlog/configuration/CI ownership stays with the primary. The validator rejects unsafe ownership and dependencies before dispatch; the bridge checks actual patch paths and contents before applying anything.

`work-worker` is a native blocking role, not another scheduler: independent entries in its batch still run concurrently. Workers skip shared validation while the batch runs. They report the files changed and finish/yield; OMP then captures the patch and returns its path to the primary. Waiting for the parent to provide a future patch path would deadlock the batch, so the role explicitly forbids that protocol. Hub is available for the worker's own processes/jobs, not parent-reply waits.

The primary inspects the actual native patch, then uses `work_checkpoint` or `/work integrate T-01 <patch-path>`. Integration checks the current branch, eligible ownership, reparse points, case/path collisions, unsafe/binary/submodule/symlink patches and `git apply --check`, then applies the same captured bytes. It neither trusts a worker's prose nor marks the task complete. Run relevant checks on the integrated tree and record evidence in tasks.md.

## Completion and fresh context

The native Goal tool owns the current item's operational objective. Its completed label is not canonical project acceptance. The primary requests `work_checkpoint` finish only after task completion and real evidence, then completes the native Goal and yields. The deferred checkpoint waits for native idle and rechecks authorization/documents; it reports a request, not premature completion.

Automatic continuation records completion, spends a handoff step, creates a real native session, retains the grant and cumulative budget, records its native ID, selects only another eligible authorized item and starts the next objective. Pause and exhausted scope take precedence. It never starts another goal merely because a backlog entry exists.

The primary remains responsible for coherent backlog/spec/goal status documentation and Git checkpoints. The bridge does not automatically create commits, PRs or merges. The disposable feature-to-fresh-session proof is evidence for this local bounded path, not a claim of unattended GitHub delivery.

## Review and publication

The always-on Advisor is read-only advice, not merge approval. Under the 2026-09-13 owner amendment, ordinary PRs use primary diff/acceptance review and fast CI without a mandatory independent full review. Material auth/secret/destructive-data/privilege changes need focused independent review; public release approval or explicit owner requests retain the full review. When used, the reviewer has a different model family, memory off, read-only tools, no primary conversation and frozen evidence. Zero findings is valid; concrete fixes receive targeted verification, not a repeated full audit.

Current CI keeps validator regressions, document validation and workflow/patch regressions; formatting is local-only and obsolete runs of the same PR are cancelled. Main protection/required-check enforcement is deferred in low-priority AIU-026, so its absence does not block ordinary PRs. Relevant exact-head checks, authorized PR/squash merge and durable progress remain required by policy, not enforced by GitHub. This amendment performs no remote actions and does not weaken production signing, promotion or manifest permissions.

## Limits

- An unarmed headless primary cannot write, launch processes or dispatch workers through guarded tools. Native isolated workers are identified by native session metadata, not by headless mode alone.
- Ordinary interactive access does not authorize `work_checkpoint`: checked integration and completion retain their independent armed primary command-context checks. The per-session latch is in-process, not persistent native permission; a fresh external interactive process is ordinary until explicit bounded opt-in.
- These are critical-transition gates, not mediation of every operation inside an already permitted shell/eval/process call or an OS sandbox.
- The deterministic validator rejects non-Latin authored scripts and honors explicit opaque-data fences. It cannot prove that all Latin-script prose is English; review still owns that requirement.
- Initial .NET package restoration is outside the network-free validator runtime. CI has no OMP authentication, live model/provider calls or product UI tests.
- No cloud dashboard, memory database, agent server, release PKI or Windows product code was added. Further product work requires its own explicit selection and authorization.
