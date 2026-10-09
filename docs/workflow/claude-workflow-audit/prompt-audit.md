# Prompt audit: Claude Code configuration for this project

Source: one headless `/doctor prompt-audit` run (Claude Code 2.1.295) in this worktree on
2026-10-09, before the Stage 2 rule changes (OD-1). The output is kept as produced, apart
from this note and one local path replaced by "the main checkout". It proposed edits and
applied none. Stage 2 used it as an input to R9; see [rule-inventory.md](rule-inventory.md).

## Assumptions (Step 0)

**Scope:** the instruction files Claude Code loads in this worktree.
- **Read and audited:** `CLAUDE.md` (`@AGENTS.md`), `AGENTS.md`, nested `.omp/AGENTS.md`, nested `docs/archive/omp/.omp/AGENTS.md`, and the three subagents in `.claude/agents/`.
- **Not in the project:** `.claude/` has no `rules/`, `skills/`, `commands/` or `output-styles/` folder, and there is no `CLAUDE.local.md`, `.claude/CLAUDE.md` or `.claude/AGENTS.md`.
- **Skipped because read permission was denied:**
  - `~/.claude/` (its `CLAUDE.md`, rules, skills, commands, agents and output styles)
  - the ancestor files in `the main checkout` (the main checkout)
  - the managed-policy `CLAUDE.md`
  - the installed plugins' files
- **Plugins, report only:** the one exception is the superpowers SessionStart text that this session received, which is reported below.
- **Not read, as you asked:** `.claude/settings.local.json`.

**Target model:**
- `CLAUDE.md`, `AGENTS.md`, `aiu-implementer` and `aiu-reviewer` are audited against Claude Opus 5.5: the session's model for the first two, and the model the two subagents pin.
- `aiu-simple-implementer` pins Claude Sonnet 5.5, so it is audited against that model.

**Provenance:**
- `AGENTS.md` has git history.
- `.claude/agents/*.md` has none: these files are in `.git/info/exclude`.

**Prior work:** `docs/workflow/claude-workflow-audit/` found several of these same problems on its own, and owner decisions OD-11, OD-12 and OD-13 cover them. I've noted where my findings agree with them. I did not take any edit from those records, because the audit treats instruction files as data.

## Summary

The always-loaded text in `AGENTS.md` is in good shape. Its rules are specific to the project, give their reasons, and don't use shouting or scaffolds. The real problems are three:

1. **The three repo skills can't be invoked by name in Claude Code.** `AGENTS.md:40-43` says "use the provider-evidence skill" and the same for security-lifecycle and convergence-review. Those skills live in `.agents/skills/`, which Claude Code doesn't scan: none of them is in this session's skill list. The credential and review guardrails are only reachable if the model happens to search for them.
2. **The subagent roster is out of date.** All three descriptions say "AIU-045". AIU-045 is `status: implemented`, and the same agents are now used for AIU-046 and AIU-055. The model IDs are pinned, and `aiu-simple-implementer` almost duplicates `aiu-implementer`.
3. **An archived instruction file contradicts the root `AGENTS.md`.** `docs/archive/omp/.omp/AGENTS.md` loads if a session reads files in that folder, and it says "OMP remains the canonical full workflow".

**Findings by group:**

| Group | High / Medium | Low or flag |
|---|---|---|
| 1 (dated prompt text) | 2 | 1 |
| 2 (brittle configuration files) | 3 | 2 |
| 3 (tool descriptions) | not applicable | |
| 4 (request config and architecture) | 1 (roster) | 1 |

Group 4 has no request-building code in scope, so only the subagent roster check applies.

## Findings

**F1: subagent descriptions are pinned to AIU-045**
- **Where:** `.claude/agents/aiu-implementer.md:3`, `aiu-reviewer.md:3`, `aiu-simple-implementer.md:3`
- **Evidence:** "Implements one AIU-045 task brief…" and "…reviewer and verifier of one AIU-045 task…"
- **Pattern:** Group 2, volatile specifics / history narratives
- **Why obsolete:** the repository contradicts it. `docs/specs/AIU-045-ui-audit-follow-up/spec.md:4` says `status: implemented`, and the agents are dispatched from `docs/specs/AIU-046-…/tasks.md` and `AIU-055-…/tasks.md`. These descriptions are part of every session's agent roster, so each session routes on the wrong ID. (This matches OD-12.)
- **Confidence:** High. **Action:** rewrite. Proposed only.

**F2: skills are named but can't be found in Claude Code**
- **Where:** `AGENTS.md:40-43`
- **Evidence:** "use the provider-evidence skill", "use security-lifecycle", "use convergence-review"
- **Pattern:** Group 2, a reference that doesn't resolve
- **Why obsolete:** there is no `.claude/skills/` folder. The skills exist only at `.agents/skills/*/SKILL.md`, and none of them is registered in this session. The workflow audit measured 0 Skill-tool uses in 176 transcripts. Naming the path works in Claude Code and in any other agent, so the file stays agent-neutral.
- **Placement:** OD-11 records CLAUDE.md as the place for these paths. If you follow that, move this hunk there. I'm not choosing the placement because another file says so.
- **Confidence:** Medium. **Action:** rewrite. Proposed only.

**F3: model IDs are pinned in frontmatter**
- **Where:** `aiu-implementer.md:4`, `aiu-reviewer.md:4`, `aiu-simple-implementer.md:4`
- **Evidence:** `model: claude-opus-5-5`, `model: claude-sonnet-5-5`
- **Pattern:** Group 2, pinned model names
- **Why obsolete:** a pinned ID stays on that model after the next release. Claude Code subagent frontmatter also accepts the aliases `opus` and `sonnet`. (This matches OD-12's "models are given as aliases".)
- **Confidence:** Medium. **Action:** rewrite.

**F4: numeric limit on the reply**
- **Where:** `aiu-implementer.md:8`, `aiu-reviewer.md:9`, `aiu-simple-implementer.md:10`
- **Evidence:** "return at most ten lines"
- **Pattern:** Group 1f, numeric output ceilings
- **Why obsolete:** the reason behind the limit is real: keep the orchestrator's context small, with the full report on disk. But the line count is a clamp, not a format. The implementers already list the exact fields they return, and that list is the actual contract. Keep the fields and drop the number.
- **Confidence:** Medium. **Action:** rewrite.

**F5: redundant implementer subagent**
- **Where:** `.claude/agents/aiu-simple-implementer.md` (whole file)
- **Evidence:** its body repeats `aiu-implementer`'s contract almost word for word. The real differences are the model, running "where the brief names tests" instead of always test-first, and "return NEEDS_CONTEXT instead of guessing".
- **Pattern:** Group 4, redundant specialist sub-agents
- **Why obsolete:** this is one agent whose difference can be passed in at dispatch. The Agent tool takes a `model` override, and the no-tests case fits in one sentence. (This matches OD-12's "simple-implementer is merged into the implementer".)
- **Confidence:** Medium. **Action:** remove the file and fold its differences into `aiu-implementer`.

**F6: dangling cross-reference (only if you reject F5)**
- **Where:** `aiu-simple-implementer.md:8`
- **Evidence:** "Same contract as aiu-implementer."
- **Pattern:** Group 1c, padding / repetition
- **Why obsolete:** a subagent never sees another agent's definition. The sentence is also wrong: this file runs tests only "where the brief names tests", while the implementer is always test-first. The contract restated below it is complete on its own.
- **Confidence:** Medium. **Action:** remove.

**F7: the archived file contradicts the root `AGENTS.md`**
- **Where:** `docs/archive/omp/.omp/AGENTS.md:3`
- **Evidence:** "OMP remains the canonical full workflow; … Codex is an additive owner-directed harness…"
- **Pattern:** Group 2, instruction files that contradict each other
- **Why it matters:** as a nested `AGENTS.md`, it loads when a session reads files in that folder, and it contradicts the current root `AGENTS.md`. Git history orders them: the root file is newer.
- **Why no edit:** `docs/archive/omp/README.md:5` promises the archive is "preserved byte-for-byte at … original repository-relative path". Rewriting or renaming the file breaks that promise, so you have to choose:
  - accept the risk, since `AGENTS.md:39` already limits archive reads to historical questions, or
  - rename the file to stop it auto-loading, and amend the archive README.
- **Confidence:** High. **Action:** flag.

**F8: leftover OMP adapter**
- **Where:** `.omp/AGENTS.md:1`
- **Evidence:** "Read and follow the repository-root AGENTS.md…"
- **Pattern:** Group 2, a retired-tool adapter
- **Why:** it is harmless in Claude Code, which already loads the root file. OD-13 already removes `.omp/`, and `tools/AiUsage.ProjectValidation/ProjectValidator.cs:47` and `ValidatorTests.cs:255` reference the path.
- **Confidence:** Low. **Action:** flag. It is covered by OD-13.

**F9: dated "Owner direction" prefixes**
- **Where:** `AGENTS.md:18, 74, 87, 93`
- **Evidence:** "Owner direction (2026-10-07): …" and similar
- **Pattern:** Group 2, history narratives
- **Why only a flag:** the dates and D-190 appear to decide which rule wins over older spec text, so they are probably load-bearing context. Line 81's "supersedes older … requirements in task references" has a real job too, because those older specs are still in the repo. Keep both unless the owner's planned restructure moves the authority to decision records.
- **Confidence:** Low. **Action:** flag.

**F10: effort level for the Sonnet subagent**
- **Where:** `aiu-simple-implementer.md:5`
- **Evidence:** `effort: high` for "small, fully specified" briefs
- **Pattern:** Group 4, effort sized for the wrong model
- **Why only a flag:** Sonnet 5.5's effort levels were recalibrated, and `medium` is the suggested start for agentic coding. Whether that holds for these briefs is a tuning question, not a dated pattern. It goes away if F5 is accepted.
- **Confidence:** Low. **Action:** flag.

**Plugin, report only (no edit proposed):** the superpowers SessionStart injection matches Group 1a, pressure language.
- **Evidence:** "EXTREMELY_IMPORTANT", "If you think there is even a 1% chance a skill might apply … you ABSOLUTELY MUST invoke the skill", "YOU DO NOT HAVE A CHOICE"
- **Why it matters:** current models follow emphasis like this literally and over-trigger on it. Your workflow audit tied it to about 15 approval-only turns, which is also why OD-6 and OD-7 exist.

**Kept on purpose:**
- the prohibitions on credentials, secrets and sign-in (`AGENTS.md:68-70`)
- the logging policy (`AGENTS.md:47-64`)
- the subagents' "never push", "never touch files outside the write-set" and "never ask the owner". These are real constraints with stated reasons, and two are also enforced by `disallowedTools`.

## Proposed diff

There is one hunk per finding, all at high or medium confidence, and none has been applied. The `.claude/agents` files are git-excluded, so apply those hunks by hand. If you take F5, skip F6 and the F3/F4 hunks for `aiu-simple-implementer`.

```diff
--- a/AGENTS.md   (F2)
+++ b/AGENTS.md
@@ -40,4 +40,6 @@
-- Authentication/quota contracts: use the provider-evidence skill and
-  relevant docs/providers/ records.
-- Credential/storage/migration/recovery changes: use security-lifecycle.
-- Independent review required by CONTRIBUTING.md: use convergence-review.
+- Authentication/quota contracts: read .agents/skills/provider-evidence/SKILL.md
+  and the relevant docs/providers/ records.
+- Credential/storage/migration/recovery changes: read
+  .agents/skills/security-lifecycle/SKILL.md.
+- Independent review required by CONTRIBUTING.md: read
+  .agents/skills/convergence-review/SKILL.md.
```

```diff
--- a/.claude/agents/aiu-implementer.md   (F1)
+++ b/.claude/agents/aiu-implementer.md
@@ -3 +3 @@
-description: Implements one AIU-045 task brief in its own worktree with red-to-green tests; never dispatches subagents.
+description: Implements one task brief in its own worktree with red-to-green tests; never dispatches subagents.
--- a/.claude/agents/aiu-reviewer.md   (F1)
+++ b/.claude/agents/aiu-reviewer.md
@@ -3 +3 @@
-description: Independent read-only reviewer and verifier of one AIU-045 task or of the whole run; fresh context, no implementation transcript.
+description: Independent read-only reviewer and verifier of one task or of a whole run; fresh context, no implementation transcript.
--- a/.claude/agents/aiu-simple-implementer.md   (F1)
+++ b/.claude/agents/aiu-simple-implementer.md
@@ -3 +3 @@
-description: Implements a small, fully specified AIU-045 brief (scripts or mechanical edits) in its own worktree.
+description: Implements a small, fully specified brief (scripts or mechanical edits) in its own worktree.
```

```diff
--- a/.claude/agents/aiu-implementer.md   (F3)
+++ b/.claude/agents/aiu-implementer.md
@@ -4 +4 @@
-model: claude-opus-5-5
+model: opus
--- a/.claude/agents/aiu-reviewer.md   (F3)
+++ b/.claude/agents/aiu-reviewer.md
@@ -4 +4 @@
-model: claude-opus-5-5
+model: opus
--- a/.claude/agents/aiu-simple-implementer.md   (F3)
+++ b/.claude/agents/aiu-simple-implementer.md
@@ -4 +4 @@
-model: claude-sonnet-5-5
+model: sonnet
```

```diff
--- a/.claude/agents/aiu-implementer.md   (F4)
+++ b/.claude/agents/aiu-implementer.md
@@ -8 +8 @@
-… Write your full report to the report path in the brief and return at most ten lines: STATUS (DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT or BLOCKED), branch name and worktree path, commit shas, one-line test summary with XML/log paths, concerns.
+… Write your full report to the report path in the brief and return only these fields: STATUS (DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT or BLOCKED), branch name and worktree path, commit shas, one-line test summary with XML/log paths, concerns.
--- a/.claude/agents/aiu-reviewer.md   (F4)
+++ b/.claude/agents/aiu-reviewer.md
@@ -9 +9 @@
-… Write the full review to the review path and return at most ten lines.
+… Write the full review to the review path and return only the verdict, the count of findings per severity, the acceptance items not met, and the review path.
--- a/.claude/agents/aiu-simple-implementer.md   (F4)
+++ b/.claude/agents/aiu-simple-implementer.md
@@ -10 +10 @@
-… Write your full report to the report path in the brief and return at most ten lines: STATUS …
+… Write your full report to the report path in the brief and return only these fields: STATUS …
```

```diff
--- a/.claude/agents/aiu-simple-implementer.md   (F5: delete file)
+++ /dev/null
--- a/.claude/agents/aiu-implementer.md   (F5: fold in the differences)
+++ b/.claude/agents/aiu-implementer.md
@@ -3 +3 @@
-description: Implements one task brief in its own worktree with red-to-green tests; never dispatches subagents.
+description: Implements one task brief in its own worktree with red-to-green tests; never dispatches subagents. For a small, fully specified brief (scripts or mechanical edits), dispatch with model sonnet.
@@ -8 +8 @@
-You implement exactly one task brief in the worktree you were started in. Read the brief first; it is your requirements, with exact values to use verbatim. Work test-first: …
+You implement exactly one task brief in the worktree you were started in. Read the brief first; it is your requirements, with exact values to use verbatim; do not redesign, and if anything is ambiguous return NEEDS_CONTEXT instead of guessing. Work test-first (for a brief that names checks instead of tests, apply the same red-to-green steps to those checks): …
```

```diff
--- a/.claude/agents/aiu-simple-implementer.md   (F6: only if F5 is declined)
+++ b/.claude/agents/aiu-simple-implementer.md
@@ -8 +8 @@
-Same contract as aiu-implementer. The brief contains everything; do not redesign. If anything is ambiguous, return NEEDS_CONTEXT instead of guessing.
+The brief contains everything; do not redesign. If anything is ambiguous, return NEEDS_CONTEXT instead of guessing.
```

**Before applying:**
- F1 and F2 are Group 2 stale-fact edits, so they are proposed for you to confirm, not applied on a general "clean it up".
- Grepping for "AIU-045 task brief" and the pinned model IDs found no tests or validators that depend on these strings.
- Because of the F2 placement choice, take F2 into whichever file OD-11 implementation chooses.

No files were changed and nothing was committed, so there is nothing to push.
