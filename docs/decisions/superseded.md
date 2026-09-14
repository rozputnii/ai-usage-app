# Superseded alternatives

## Automatic task publication amendment, 2026-09-14

After the migration was committed, the owner explicitly reinstated automatic commit and push after each completed and successfully verified task. This supersedes the migration's requirement to request a new push authorization for each task branch. It does not restore the earlier direct-main default. See D-179 and the sole Git policy in CONTRIBUTING.md for the current standing authorization and its boundaries.

This register is historical, not active instruction. The table records earlier resolutions, some themselves superseded by the 2026-09-14 amendments below, including its workflow and language rows. Use accepted.md and CONTRIBUTING.md for current decisions.

| Earlier proposal | Final decision |
|---|---|
| Android first | Windows 11 and .NET 10 first; Android follows. |
| One project or no ORM | Three projects with SQLite and EF Core 10. |
| DI without a Host | Generic Host. |
| Small custom navigation service | MVVM-first routing; Uno.Extensions.Navigation remains the selected candidate subject to a build/integration gate. |
| Credential Manager with DPAPI fallback | Only app-owned DPAPI CurrentUser records for application credentials. |
| Full configuration validation at every launch | Cheap schema/journal compatibility checks; full checks after migrations/restores or evidence of corruption. |
| Global HTTP concurrency of four | No arbitrary global network cap; respect actual provider throttling and retry constraints. |
| Manual refresh joins the background request | Manual refresh cancels/replaces quota fetching; token rotation remains a separate serialized lifecycle. |
| Used percentage as the main number | Remaining percentage is primary. |
| Automatic risk-based card ordering | User-controlled order with status highlighting. |
| Tray click simply opens the main window | Single click opens the mini-dashboard; double-click opens the main window; right-click opens the menu. |
| Separate dashboard/tray provider reads | One immutable current-state store, with separate history queries. |
| Portable import merges data | Replace-only import; preserve existing credentials only for matching identities. |
| Encrypted portable export | Open unencrypted export without secrets. Local migration backups remain DPAPI-encrypted. |
| Either factory reset or settings reset | Provide both. Factory reset clears mutable local state, not a literal reinstall. |
| Stable only or separate Preview installation | One direct package identity with Stable/Preview feeds and no downgrade. |
| Microsoft Store immediately | Direct distribution first; Store later with explicit identity/data migration. |
| Mandatory 24-72-hour soak | Risk/evidence-based promotion; hotfixes may have no waiting period. Stable promotion remains manual. |
| Self-contained deployment or AOT immediately | Framework-dependent deployment; Native AOT is not required. Measure first. |
| Owner approves every feature spec/design | Escalation-only within an authorized goal, with Interactive selection or explicitly scoped Autopilot. |
| Only one code-writing agent | Independent isolated write workers are allowed; the primary is the sole integrator. |
| Full repeated reviews after each fix | Owner amendment 2026-09-13: ordinary PRs use primary review and fast checks; focused independent review for material security/lifecycle changes, full review for public release or owner request. After fixes, targeted verification only. Zero findings is valid. |
| Automatic skill learning | Auto-learn is disabled. Authored repository skills are authoritative; local memory is supplementary. |
| Every fresh session automatically starts another feature | Interactive sessions ask for selection/resume; only internal Autopilot handoff carries existing authorization. |
| Absolutely no remote policy | No dynamic remote configuration; a narrow signed static disable-only compatibility manifest is approved. |
| Missing quota fields become zero/default quota | Unknown remains unknown. Critical semantic mismatch cannot publish fabricated quota data. |
| Every planned feature must exist in the first build | Ship an early runnable MSIX and add the v1 scope after the Codex slice. |
| Ukrainian or mixed-language repository documents | English-only repository, documentation, decisions and development artifacts. Ukrainian is conversation only. |

For a new conflict, compare accepted intent with actual evidence. Record an amendment or escalate rather than silently picking whichever option is easier to implement.

## Workflow decisions superseded on 2026-09-14

The following text is preserved as historical past-task context by the owner-approved agent-neutral migration. It is not current instruction or a permission grant. Current development procedure and Git policy are in [CONTRIBUTING](../../CONTRIBUTING.md). No language mandate replaces D-176.

### D-012 - Development harness
OMP on Windows remains the canonical full workflow; only OMP provides `/work` bounded authorization, native isolation/patch integration, session handoff and Advisor behavior. Under the owner-approved additive compatibility amendment, Codex may perform owner-directed repository work but must not infer or resume OMP authorization. This does not authorize installation, login, project trust changes or remote actions.


### D-013 - Method
Goal-driven, repository-first, proportional specification-driven development. The owner provides goals and feedback; OMP handles research, decomposition, implementation, verification, PRs and authorized merges.


### D-014 - Execution modes
Interactive mode presents a ranked backlog for owner selection. Autopilot works independently within an explicitly authorized goal, with defined escalation exceptions.


### D-015 - Approval
Per-spec and per-design approval was superseded by escalation-only approval for ordinary authorized work. Changes to product intent, security boundaries, significant architecture or a significant unapproved dependency require the owner.


### D-016 - Questions
Do not ask settled questions again. Record clearly superior minor technical choices automatically; ask about important trade-offs. A blank/space reply selects a recommendation only in response to an actual owner-facing question, never in unattended execution.


### D-017 - Granularity
AIU items represent coherent outcomes, not individual classes, DTOs or tests. T items are internal decomposition of the selected feature. Decompose just in time; no universal line or token limit guarantees quality.


### D-018 - Proportional documentation
Small fixes need a short plan and evidence; normal features need a spec and tasks; architecture, authentication and security changes need a design and an ADR only for durable decisions. Do not produce four large documents for every edit.


### D-019 - Canonical backlog
Use docs/backlog.md, not GitHub Issues or native OMP todo. Statuses: idea, research-needed, blocked, ready, selected, in-progress, paused, review, done, dropped.


### D-020 - Prioritization
Weighted scoring plus explained judgment. Initial weights: goal alignment 30%, work unblocked 20%, risk reduction 20%, user value 15%, urgency 10%, effort efficiency 5%. Scores are derived; evaluate eligibility and dependencies first.


### D-021 - Goals and owner inbox
Use docs/product/goals.md; every AIU belongs to a goal. docs/decisions/pending.md is the durable inbox for decisions requiring the owner.


### D-022 - Execution state
tasks.md is the sole feature execution record: status, dependencies, ownership, writes/shared paths, isolation, agent, acceptance criteria and handoff. A single coordinator updates it at meaningful transitions.


### D-023 - Resume
A fresh interactive session performs compatibility checks and shows either the active feature with a continue confirmation or the ranked backlog. Internal Autopilot handoff preserves valid goal authorization and cumulative budgets.


### D-024 - Pause
The owner may pause or switch. Create a safe checkpoint, perform targeted verification, update the handoff in tasks.md and commit/push recoverable state when authorized. Do not label work in progress as complete or passing. Settle worker state first.


### D-025 - Autopilot sessions
Use a fresh execution session for each feature. After merge, hand off through the repository to a fresh context. Preserve goal-level authorization, budget and accounting across sessions.


### D-026 - Selective blocking
A local issue blocks its AIU, not the entire goal. Continue other independent authorized items. Stop the goal for a global security/product blocker or when no safe work remains.


### D-027 - Budgets
Use adaptive overall and per-feature effort budgets, bounded recovery attempts and checkpoints for lack of progress or abnormal consumption. Budget exhaustion never means completion.


### D-028 - No artificial pauses
Autopilot does not wait for the owner after every merge. Stop for goal completion, escalation, budget exhaustion, fatal failure or explicit pause. Do not run an unbounded improvement loop.


### D-029 - Plan Mode
Fresh interactive sessions start in Plan Mode; a trivial fix needs only a short plan. Exit for authorized implementation. Do not require repeated approval at every internal handoff.


### D-030 - OMP version policy
Use the latest stable release, not main/nightly and not a permanent historical pin. Probe compatibility after upgrades and record the exact tested version. Do not upgrade OMP during an active feature.


### D-031 - Profile
Use the ai-usage profile and launch from the repository root. The profile contains authentication, model mappings, local sessions, cache and memory, not canonical project policy.


### D-032 - Memory
Use memory.backend=local as supplementary context, never authority. Disable auto-learn. The repository must support recovery without memory or conversation history.


### D-033 - Portable behavior
Version rules, skills, agents, documentation and the validator in Git. Do not commit generated local state, credentials or model-provider authentication.


### D-034 - Skills
Keep essential rules short in RULES.md and the project map in AGENTS.md. Load specialized skills on demand. Do not inject all documentation as always-apply context.


### D-035 - AI infrastructure changes
Protect configuration, RULES and changes to privileges or gates. Small navigation fixes in AGENTS, evidence-based WATCHDOG checks and existing-skill improvements may be made without weakening policy.


### D-036 - Models
Use strong primary/planning models, an advisor and fresh reviewer from a different model family, and a smaller model for inexpensive tasks. Discover actual available model IDs rather than inventing them.


### D-037 - Advisor
Always enable the advisor for the primary agent. Grant read/grep/glob only, not write/bash/eval. syncBacklog=1 means bounded catch-up, not completed review. Do not automatically attach an advisor to every worker.


### D-038 - Parallelism
One active feature may have several independent isolated write workers. The primary is the only integrator; shared contracts are handled sequentially. Research may run in parallel. Avoid an arbitrary 32-agent swarm.


### D-039 - Workers
Use native task batching and isolation, initially targeting up to four workers. Require explicit ownership and write globs; validate the actual diff. Workers must not edit the shared backlog, tasks, settings or CI.


### D-040 - Integration
Automatically integrate only after scope/diff checks, passing targeted tests and clean patch application. The primary resolves shared overlap and conflicts. Verify the integrated feature tree, not just isolated worker tests.


### D-041 - Review
Owner amendment, 2026-09-13: ordinary development PRs require primary diff/acceptance review and relevant automated checks, not a mandatory independent full review. Use focused independent review for material authentication, secret handling, destructive data lifecycle or privilege changes; retain the full independent review for public release approval or explicit owner requests. When used, keep context fresh, memory disabled, a different model family and read-only tools. Zero findings is valid; no automatic review loop.


### D-042 - Findings
BLOCKER and MAJOR findings block merge. Deduplicate MINOR findings into the backlog; omit NIT findings. After a fix, run targeted verification rather than restarting a full review loop.


### D-043 - Git
Use trunk-based main with short-lived feature/AIU branches and squash merge. OMP may commit, push, open PRs and merge after the required gates. No direct main push or force-push.


### D-044 - Permissions
YOLO execution is authorized within the selected project scope; it is not a sandbox. Retain human gates and protected release/manifest operations. Do not describe textual rules as hard security enforcement.


### D-045 - Validator
Use a lightweight repository validator and CI for IDs, statuses, links, acceptance references, dependencies and ownership. Gate critical transitions, not every keystroke. Owner amendment, 2026-09-13: defer main protection and required-check enforcement to low-priority AIU-026; their absence is not a blocker to current development PRs. Keep relevant exact-head check evidence and explicit remote-action authority. This intentionally accepts that GitHub does not independently enforce the local merge policy.


### D-049 - Native workflow UX
Use thin project commands, selection UI and hooks over OMP primitives, not a new agent framework. Check names for built-in collisions. Commands do not exist until implemented and verified.


### D-154 - CI
Owner amendment, 2026-09-13: current development PR CI runs the validator regressions (including compilation/analyzers), canonical document validation and workflow/patch regressions. Formatting stays a local check, not a PR gate. Cancel obsolete runs of the same PR; do not skip behavior checks or suppress failures. Add relevant provider/security/UI checks when those features exist. Public release evidence and genuinely interactive xUnit/FlaUI UIA3 smoke requirements remain unchanged.


### D-166 - Release authority
OMP may merge normal verified PRs. The owner approves Stable promotion and emergency compatibility-policy publication. Never expose signing secrets to arbitrary PRs or agents.


### D-171 - Watchdog execution boundary
Detection may run while the owner PC is off; fixes start in the next active authorized OMP session. No always-on coding runner initially. Scheduled Actions are not guaranteed 24/7 monitoring.


### D-173 - Feedback
The owner reviews installable results and supplies goals/feedback, not detailed implementation prompts. OMP turns feedback into coarse backlog outcomes and preserves deferred decisions.


### D-174 - Deferred scope
Defer Android, ARM64/x86 expansion, Windows Widgets, WSL import, multi-window, command palette, Store infrastructure and an always-on OMP runner.


### D-176 - Repository language
All repository documentation, decisions, goals, backlog items, specifications, plans, skills, agent instructions, prompts, code identifiers/comments, test descriptions, generated reports, commit messages, PRs and release notes must be in English. Ukrainian is for conversation with the owner only. Do not translate opaque provider/user data or previously approved identifiers. Deliberately added future product translations belong only in localization resources.


### D-178 - Direct main development until the first release
On 2026-09-14 the owner decided to merge completed work into `main` and continue development by committing directly to `main`, one task at a time, treating feature branches as unnecessary overhead at this stage. Local merges and commits are ordinary work; pushing, remote publication and branch protection changes remain separately authorized and are not implied. Each commit must still be a coherent, verified task with its canonical records updated, not a checkpoint of unfinished work. Proposal to revisit, recorded at the owner's request: before the first release, restore short-lived feature branches with pull requests for anything touching signing, release artifacts, credential or data lifecycle, so release candidates are reviewable and revertible; main protection remains deferred in AIU-026 until then.


### D-179 - Push each completed task to main
On 2026-09-14 the owner authorized pushing directly to `origin/main` after every completed task, to avoid accumulating unpushed work and to keep implementation steps minimal. A push is now part of finishing a task, not a separate approval. Conditions that still hold: push only a fast-forward of the exact verified head, never force-push, never rewrite published history and never change branch protection or repository settings. If the remote has diverged, stop and report instead of forcing. Nothing here authorizes releases, tags, remote workflow dispatch, secrets or publication; GitHub Actions execution remains unverified evidence until a run is actually observed.


## Past-task approval context (not current grants)

## Authorization is not a status label
The status records direction, not permission by itself. The owner explicitly launched AIU-001 local implementation in OMP Goal Mode on 2026-09-12 and approved the user-local SDK installation, MIT replacement and repository-local identity recorded in `../workflow/environment.md`. That local scope is complete, including bounded disposable workflow proofs and the explicitly authorized replacement review after a capture failure.

On 2026-09-13 the owner authorized a narrow post-bootstrap policy amendment: defer main protection at low priority and simplify current PR checks. This does not start another product goal or authorize remote mutation.

The owner subsequently selected AIU-002 and disposable Sandbox/VM installation verification, then approved execution of the First runnable Windows MSIX and smoke CI plan. G-002 is the selected direction; AIU-002 is the current item. This authorizes that feature's local implementation and development signing only, not other G-002 items, host package installation or certificate trust changes, elevation/reboot, provider account access, paid resources, remote publication, or unlimited bounded execution. No bounded execution record is created by this approval.

At the AIU-002 guest-environment gate, the owner explicitly chose to leave the item BLOCKED and retain the code, signed development candidate and offline verification bundle. This does not authorize Sandbox enablement, elevation, reboot, another item or automatic continuation past the missing guest proof.

The owner subsequently resumed AIU-002 and explicitly selected enabling Windows Sandbox, including its required official component dependencies and a UAC elevation request. This supersedes the prior stop only for obtaining the disposable guest and continuing AIU-002 verification. No automatic host reboot, firmware changes, host app installation or host certificate trust import is authorized. If a restart is required, preserve state and report it before continuing.

The authorized Windows Sandbox enablement completed successfully and required an owner-controlled restart. The owner reported completing that restart; subsequent disposable guest launches and installation verification succeeded. No automatic reboot or host app/trust installation occurred. Remaining acceptance and the later owner-directed focus change are recorded below and in AIU-002 verification.

On 2026-09-13 the owner changed the provider-development approach: build a reusable provider integration library, exercise it through a console application first, and integrate the verified implementation into the UI afterward. This removes UI readiness as a prerequisite for provider research and library/console work, but does not select AIU-003/004 for execution or authorize provider account access. G-002 remains selected and the current execution remains AIU-002.

The owner then explicitly redirected current execution to provider integration using a console application and appropriate integration/unit tests, rather than more UI work. Current item is AIU-003, Codex authentication/quota contract and an executable library/console proof. AIU-002 is paused with its guest evidence retained. This authorizes local provider research, library/console implementation and deterministic tests, not another provider, UI integration, paid resources, remote actions, unattended OAuth consent or reading personal CLI credentials without a specific import/verification decision. No bounded execution grant is fabricated.

The AIU-003 library/console slice is implemented and live-verified: on 2026-09-14 a real browser sign-in read this account's actual Codex quota, refreshed in memory and read quota again. Following an explicit owner instruction, the connection and usage read mirror the locally cloned OMP implementation per D-177. Device-code login, multi-workspace switching, exhausted and rate-limited responses, long-term rotation and CLI coexistence remain NOT_RUN, and third-party reuse of the public Codex client remains unresolved. This does not authorize CLI-token import, durable credential storage, AIU-004 UI/persistence work or another provider.

On 2026-09-14 the owner chose to finish AIU-002 before further provider work. Closure was acceptance only: retained clean-guest evidence was inspected, screenshots opened, the routing package hash recorded, criteria consolidated and temporary probes archived. No guest was launched, no product behavior changed, and no host installation, host trust change, elevation or remote action occurred. AIU-002 and AIU-003 are both complete; remote GitHub Actions execution and interactive UI CI remain NOT_RUN by authorization. Selecting the next item, including AIU-004 UI integration of the verified Codex path, still requires an explicit owner decision.


AIU-004 past-task handoff instructed direct-main work with a push after each completed task under D-178/179. It is superseded and grants no current authority.

## Historical AIU-002-windows-msix handoff

Past-task chronology only; none of the next actions or permissions below are current grants.

## Handoff
Current branch is feature/AIU-002-windows-msix; existing AIU-001 edits and unrelated shortcut remain untouched. Routing worker patch was inspected and integrated; smoke worker timed out without a captured patch, and primary implemented smoke. No pending worker patch remains. Native production/routing builds and package generation succeeded. Final candidate is 2026.9.1305.0 under the ignored AIU-002 staging root; it is not accepted as installable until guest trust/launch proof. Focused review completed with scoped fixes and targeted verification. Next: obtain a clean Windows 11 24H2+ x64 disposable guest and run the staged offline harness, then the routing scenario. Sandbox and Hyper-V optional features are disabled. Do not enable features, elevate, reboot, install on host or import host trust without a specific owner decision. Do not start AIU-003.

Owner handoff decision: explicitly leave AIU-002 BLOCKED, retaining code, signed candidate and offline inputs. Do not attempt to enable Sandbox or obtain another environment autonomously. Resume guest verification only after a new owner decision supplies or authorizes the environment; no AIU-003 start.

Owner resumption: explicitly enabled AIU-002 continuation and authorized Windows Sandbox component enablement with required dependencies and UAC elevation. No automatic reboot, firmware changes, host app installation or host trust import. Next: enable Containers-DisposableClientVM with no-restart semantics, capture the actual result, and proceed only if the guest is ready; otherwise preserve state and report the restart/prerequisite gate.

Sandbox enablement result: elevated Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All -NoRestart completed with exit 0, state Enabled and RestartNeeded=true. No automatic restart occurred. Current blocker is the owner-controlled host reboot, not missing enablement permission. After the owner reports restart, verify Sandbox readiness and resume the staged offline guest scenario; do not rerun component installation or change host trust.

Post-reboot checkpoint: guest installation and clean negative prerequisites were exercised; final product and native routing reports passed after scoped smoke/routing fixes. See verification.md Post-reboot execution checkpoint and owner redirection for exact artifacts and remaining visual review/cleanup. On the owner's explicit focus change, AIU-002 is paused, not complete. Current execution is Codex library/console work under AIU-003; the earlier no-AIU-003 handoff restrictions are superseded only by that new explicit selection. Do not resume UI work autonomously. Existing task completion labels are intentionally not advanced before consolidated evidence review.

Closure, 2026-09-14: on the owner's decision AIU-002 was resumed for final acceptance only. Retained clean-guest evidence was inspected, including actual screenshots; the routing 1302 guest hash is recorded; AC-01 to AC-05 are PASS and AC-06 is PASS locally with remote/interactive CI NOT_RUN by authorization. All tasks are done and the item is complete. One non-blocking follow-up remains: routing `back.png` captured the pre-repaint frame, so the return to Main rests on the observed UIA state sequence. No guest was launched and no product behavior changed during closure.


## Historical AIU-003-codex-console handoff

Past-task chronology only; none of the next actions or permissions below are current grants.

## Handoff
Paused by the owner on 2026-09-14 after live verification succeeded. AIU-003 is complete: browser sign-in, real quota read, in-memory refresh and a second real quota read all passed through the console, with 54 deterministic tests and canonical validation green. Connection and usage mirror the locally cloned OMP implementation per D-177; the earlier locally invented residency rejection was removed. Remaining provider scope - device-code login, multi-workspace switching, exhausted/rate-limited responses, long-term rotation and CLI coexistence - is NOT_RUN and belongs to AIU-004/005. Public-client reuse permission remains unresolved. No credential was persisted and no source CLI store was touched. AIU-002 remains paused with its guest evidence retained. Next session: read docs/specs/AIU-003-codex-console/verification.md before starting AIU-004.
