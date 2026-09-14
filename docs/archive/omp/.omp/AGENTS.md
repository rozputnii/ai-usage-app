# AI Usage project map

OMP remains the canonical full workflow; only OMP provides `/work` bounded authorization, native isolation/patch integration, session handoff and Advisor behavior. Codex is an additive owner-directed harness for repository work and must not infer or resume OMP authorization. Owner manages goals/feedback; do not repeat resolved A/B/C questions. Owner-facing conversation may be Ukrainian. All repository content must be English: documentation, decisions, goals, backlog, specs, tasks, skills, agent prompts, code/comments, tests, reports, commits, PRs and release notes. Product v1 UI uses English resources. Never copy Ukrainian conversational text into repository artifacts.

Read only the relevant canonical paths:
- `docs/constitution.md`: priorities and authority.
- `docs/product/goals.md`: selected goals/authorization.
- `docs/backlog.md`: coarse AIU work and deferred scope.
- `docs/decisions/accepted.md`: consolidated decisions; do not load all into every worker prompt.
- `docs/decisions/superseded.md`: old alternatives no longer valid.
- `docs/decisions/technical-audit.md`: evidence/corrections relevant to current work.
- `docs/decisions/pending.md`: owner-required exceptions.
- `docs/workflow/omp-native.md`, `formats.md`, `verification.md`: execution contracts.
- `docs/platforms/windows/`: selected Windows architecture/lifecycle/release policy.
- `docs/providers/`: per-provider current evidence.
- `docs/specs/<AIU-ID>-<slug>/`: selected spec/tasks/actual verification.

Repo state + current instructions, not local memory, determine current work. Treat intended requirements and observed code state separately. No prompt can fabricate a PASS test or owner approval. Proportional documentation; no microtasks in product backlog.

Fresh interactive startup: root/version/schema check, active summary or ranked candidates, wait selection. Autopilot internal continuation only with persisted goal authorization and unchanged privilege/budget. Research/code workers receive scoped task packets, not whole conversation. Stop/record escalation rather than invent missing provider permission/credentials.

Command names/test commands are published here only after actual AIU-001/002 verification. Do not use examples from previous chat as if installed.
