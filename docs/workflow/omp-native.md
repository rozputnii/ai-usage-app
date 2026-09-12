# OMP-native execution design

Status: agreed requirements; runtime not implemented. The original source snapshot is v18.1.18, recorded on 2026-09-12. Revalidate the actual latest stable release during setup. This document does not claim that stock OMP already implements the project-specific workflow.

## Four layers
1. Git documentation: goals, backlog, specs, ADRs, tasks and verification are portable durable state.
2. OMP primitives: profile, configuration, Plan/Goal modes, skills/agents, task batching, isolation, Agent Hub, Advisor and local memory.
3. Thin project bridge: selection UI, document validation, handoff metadata and critical-transition dispatch. Do not build another scheduler framework, vector database or server.
4. GitHub controls: independent required checks and protected release authority. Ordinary agents cannot modify or bypass protection.

## Native capabilities versus project-specific work
| Behavior | Foundation | Additional work |
|---|---|---|
| Profiles/configuration | Native | Root working directory and effective-schema validation |
| Rules/skills | Native discovery | Short authored project files |
| Ranked coarse backlog | Project | Derived score, explanation and native selection UI |
| tasks.md dependency graph | Project | Tested parsing, eligibility and path checks |
| Parallel workers | Native task batch/isolation | Explicit packets, ownership and serial integration |
| Progress | Native Agent Hub/jobs/todo | Separately persisted project task state |
| Goal continuation | Native Goal primitives | Tested authorization/accounting across fresh sessions |
| Merge gate | GitHub and project validator | Actual head-SHA, CI and review checks |
| Stable/manifest publication | Protected workflow | Excluded from ordinary agent auto-merge authority |

## Intended settings, not a ready-to-copy configuration
- memory.backend: local; autolearn.enabled: false.
- plan.enabled: true; plan.defaultOnStartup: true for fresh interactive sessions, not repeated approval for already authorized handoffs.
- advisor.enabled: true; advisor.syncBacklog: the current schema value representing 1. Check its actual type instead of relying on YAML coercion.
- async.enabled: true; task.batch: true; task.maxConcurrency: initial target of four.
- task.isolation.enabled: true. Every write-worker invocation must explicitly select isolation; enabling the capability does not isolate all spawns.
- Configure isolated patch return without unreviewed automatic application. Read exact key/value semantics from the current schema.
- Keep normal stable compaction enabled, including supported asynchronous compaction; do not add experimental context-management features.
- Use the selected YOLO execution policy only within authorized project scope. Do not portray regex command policies as a sandbox.
- Keep concrete model mappings profile-local. Use strong primary/planning models, a different-family advisor/reviewer and a smaller maintenance model. Do not hardcode invented model IDs into the public repository.

OMP configuration commands may write global/profile settings instead of arbitrary project keys. Do not modify unrelated machine-wide defaults. Inspect only redacted configuration fields; do not retrieve credential values merely to report setup.

## Commands
Keep the owner interface short. Prefer one project namespace, /work, with status, add, select, auto, pause, resume and verify operations. If the installed native command registry conflicts, use /ai-work. Do not shadow native /resume, /goal or other built-ins.

A Markdown command can load a skill; it is not automatically a runtime operation. Use supported extension APIs for selection, authorization and budget behavior. An unattended prompt without necessary authorization must fail closed. Silence is not owner confirmation.

## Minimal skills and agents
Author only the useful repository skills: project-work, feature-delivery, provider-evidence, security-lifecycle and convergence-review. Do not copy the entire constitution into each skill. All skill bodies, descriptions and inter-agent prompts are English.

Use bundled agents where sufficient. Create project agent definitions only when explicit role/tool/output restrictions are required. Write workers do not edit shared tasks/backlog/.omp/CI files or push/merge the parent branch. The primary checks their results and actual diffs before updating state.

## Startup
Register handlers during extension load; perform runtime actions only through supported lifecycle callbacks. Bound session-start probes and avoid secret reads. Give the primary the active scope and relevant paths, not the entire documentation corpus. Do not let subagents/reviewers recursively start backlog selection, owner prompts, memory loops or Autopilot.

## Execution loop
Interactive: show ranked eligible work and active/paused status -> explicit selection/continue -> deliver the selected scope -> report.

Autopilot: authorize goal/scope/budget -> choose eligible work -> proportional planning -> isolated worker batch -> primary integration -> tests/documentation -> one-shot independent review -> required checks for exact head -> PR squash merge -> durable progress -> fresh context preserving authorization/budget -> rerank.

Ordinary tasks do not need repeated owner approval. Escalate a material intent/security boundary change or significant unapproved dependency; block the affected AIU and continue independent authorized work where safe. Do not keep inventing improvements after the goal is met.

## Session transfer
Preserve a stable goal ID, allowed item scope, starting reference, granted capabilities, cumulative accounting, current feature, pending decisions and stop reason. Canonical scope may be a structured section of goals.md; native session IDs are operational references, not authorization. Do not replace OMP's conversation engine with a project-owned orchestration server.

Fresh sessions recheck repository state before writing. Explicit owner pause takes priority. Ordinary interactive resume waits for confirmation. Only a proven internal Autopilot handoff may continue the same authorization without asking again. A closed local OMP process does not keep working by itself.

## Review and merge
The always-on Advisor supplies advice, not approval. The final reviewer receives frozen code/spec/test evidence, not the primary's implementation narrative, with memory disabled and read-only tools. Valid outcomes are PASS, BLOCKED and INSUFFICIENT_EVIDENCE. The latter is never silently converted to PASS.

Use one full review per feature. After concrete fixes, perform targeted verification, not a new search for unrelated defects. A material scope expansion may justify another explicitly scoped review, not an automatic loop. Merge checks must match the actual head SHA. Ordinary work does not require repository administration, protection bypass or signing/root-key access.

## Anti-overengineering
Bootstrap must not implement a cloud dashboard, new memory database, agent server, twenty custom tools, release PKI or the whole application. Add scripts/extensions only for verified gaps needed by a concrete acceptance criterion. The first execution is AIU-001 only; further product goals require their own selection/authorization.
