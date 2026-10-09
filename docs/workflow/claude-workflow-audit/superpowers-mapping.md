# superpowers → repository skills

Owner decision OD-7 (2026-10-09): every superpowers skill is mapped to its replacement before
the plugin is disabled for this repository. Plugin v6.4.2 (commit `8ca22dba`) had 15 skills.
Use counts are from the Stage 1 transcript audit (lane A4c), across all transcripts up to
2026-10-08.

| superpowers skill | Uses | Replacement in this repository |
| --- | --- | --- |
| brainstorming | 3 + 12 owner slash commands | `planning-and-variants` skill; invoke it as `/planning-and-variants`. Keeps the shared-understanding step, the path classification and batched design questions; adds rendered UI variants and treats bug reports as diagnosis. Its spec, plan and execution-mode approval gates are dropped (one owner yes covers the path, AGENTS.md). |
| writing-plans | 4 | `plan-execution` skill, Plan section (tasks.md only for parallel work; "Review focus" kept). |
| executing-plans | 3 | `plan-execution` skill, Execute section. |
| subagent-driven-development | 1 | `plan-execution` skill (parallel path) with the implementer and reviewer contracts in `.agents/agents/`. |
| test-driven-development | 2 | CONTRIBUTING, Development procedure step 5 (red to green; a bug-fix test fails at the base commit) and the implementer contract. |
| systematic-debugging | 3 | Vendored unchanged except two cross-references: `.agents/skills/systematic-debugging/`. |
| verification-before-completion | — | Vendored unchanged: `.agents/skills/verification-before-completion/`. |
| receiving-code-review | — | Vendored unchanged: `.agents/skills/receiving-code-review/`. |
| requesting-code-review | — | `convergence-review` skill and the reviewer contract; cadence in CONTRIBUTING, Review. |
| dispatching-parallel-agents | — | CONTRIBUTING, Parallel work, and the `plan-execution` skill. |
| finishing-a-development-branch | 2 | Dropped: CONTRIBUTING, Git flow (merge, push, ancestry check, branch deletion, cleanup) replaces its menu. |
| using-git-worktrees | — | Dropped: the desktop app creates worktrees; CONTRIBUTING, Git flow governs them. |
| using-superpowers | session-start injection | Dropped: AGENTS.md, Skills and plugins, lists the repository skills. |
| writing-skills | — | Dropped: not used for this repository; skill format is in docs/workflow/formats.md and `.agents/skills/VENDORED.md` describes updates. |
| diagnosing-superpowers | — | Dropped: plugin-specific. |

The vendored copies and their licence are described in `.agents/skills/VENDORED.md`. The
plugin is disabled for this repository only (`"superpowers@synced": false` in
`.claude/settings.json`); the owner's account and other projects keep it. Undo: set the value
to `true` or remove the line.
