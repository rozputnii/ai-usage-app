# Claude workflow audit — status

Living log for the workflow-optimization effort. Read this first in every session.

- **Branch:** `workflow-optimization`, a long-lived branch the owner requested. It is never
  merged into `main` or opened as a PR without an explicit owner decision.
- **Current stage:** Stage 1 (analysis and recommendations) is complete and awaits owner
  review. Stage 2 has not started.

## Done

- **2026-10-08 — branch and prompt.** The branch was created from `origin/main` at `4f32185`,
  and the Stage 1 prompt was saved verbatim in [stage-1-prompt.md](stage-1-prompt.md).
- **2026-10-08 — lanes.** 15 lanes ran in parallel as read-only subagents. A4 was split into
  A4a, A4b and A4c by transcript range. Reports are in `lanes/`: A1, A2, A3, A4a, A4b, A4c,
  A5, A6, B1, B2, B3, B4, B5, B6.
  - All lanes ran as general-purpose agents, not Explore. Each had to write its report to the
    session scratchpad, and Explore cannot write.
  - Every lane was briefed to change nothing in the repository.
  - The orchestrator copied each report into `lanes/` and scanned it for private data; there
    were no hits besides local paths already present in the prompt.
- **2026-10-08 — critics and report.** Two fresh read-only critics reviewed the draft. Every
  finding was accepted or resolved; there were two blocking findings, both fixed. The final
  report is [report.md](report.md), with 18 ranked recommendations and 30 owner decisions.
- **2026-10-08 — `main` check.** `origin/main` was re-fetched before the conclusions were
  written. It was still `4f32185`, so no merge was needed and no finding is invalidated.
- **2026-10-08 — validation.** Document validation on the branch passes in normal mode.
  - `--final` reports exactly two `PLACEHOLDER_ID` diagnostics, both in the verbatim prompt;
    they are kept on purpose (see OD-30).
  - Placeholder mentions in the lane reports were put in code spans, and that was the only
    change made to them.

- **2026-10-09 — merged `origin/main` (`6e169dd`) into the branch (merge `4e513e1`).** The
  owner asked for main's unrelated bug fixes. A rebase was not used because the branch is
  published and CONTRIBUTING forbids force-push.
  - The merge brought six commits: a five-hour window fix, hover-only tooltips, the interval
    field fix and D-207.
  - None of them touches AGENTS.md, CONTRIBUTING.md, `docs/workflow/verification.md`,
    `docs/workflow/formats.md`, `.claude`, `.agents` or `.github`, so no finding is
    invalidated.
  - D-207 is one more UI-preference decision, which supports A5-F9.
  - `LocalBudgetStoreTests` gained tests, so re-measure A6-F1's timings before acting on R14.
  - Normal-mode validation passes.

## Owner decisions taken

- **2026-10-08:** Stage 1 was authorized with a broad multi-agent fan-out (see the prompt).
- **2026-10-09, OD-2 = yes.** The new Git flow is approved.
  - Each task gets its own branch, and save points are pushed to `origin/<task-branch>`
    (no CI, no Preview).
  - Only verified merges reach `main`, and the remote task branch is deleted after merge.
  - The owner's condition: a Preview still publishes automatically from `main` immediately
    after every verified merge.
- **2026-10-09, OD-3 = alternative 2.** When a required smoke is BLOCKED:
  - A change that touches Windows UI, the tray, launch or lifetime code stays on its branch
    until the smoke passes or the owner approves that change.
  - Other product changes covered by unit tests may merge, with the smoke recorded as
    BLOCKED.
- **2026-10-09, OD-4 = yes.** The always-ask and never-ask lists from R7 go into AGENTS.md.
  Inside its authority the agent acts on its recommendation and records the reason. When
  the owner reports a result, it is recorded as owner-reported PASS without asking.
- **2026-10-09, OD-5 = alternative 3.**
  - Generated prompts for primary sessions inherit standing policy.
  - Unattended runs skip always-ask work and list those questions at the checkpoint.
  - Worker subagents may merge and push to `main` by default, without a per-run grant.
  - Consequence to handle in OD-17, OD-18 and OD-23: worker pushes publish Previews mid-run
    (A4c-F5).
- **2026-10-09, OD-6 = yes.** AGENTS.md gets a precedence line over superpowers.
  - Plugin skills are optional tools, and repo rules win.
  - The spec-review, execution-mode and finish-branch prompts are pre-answered.
  - No `.superpowers` or `docs/superpowers` artifacts outlive the session.
- **2026-10-09, OD-7 = yes.** Phase 2 replaces superpowers with repo skills.
  - New repo skills: planning-and-variants (a `/brainstorming` equivalent with rendered
    mockups) and plan-execution.
  - Vendored at a pinned upstream commit: systematic-debugging,
    verification-before-completion and receiving-code-review.
  - Every superpowers skill the owner used is first mapped to its replacement.
  - Then superpowers is disabled for this repo only.
- **2026-10-09, OD-8 = alternative 2.** ux-superpowers, design-superpowers,
  desktop-commander and the Anthropic design plugin are disabled for this repo only, through
  the project settings.
  - The owner's account and other projects keep them.
  - Personal connectors are not changed.
  - Stage 2 must confirm the exact `enabledPlugins` key for the design plugin, whose install
    location was not found (B2-F8).
- **2026-10-09, OD-9 = alternative 2.** A minimal deny-only project
  `.claude/settings.json`.
  - Covered, in Bash and PowerShell forms: force-push variants, `gh release *`,
    `gh workflow run *`, and `Read`/`Edit` on source-CLI credential stores.
  - Not covered: reset, clean, branch deletion, stash, variables and secrets.
- **2026-10-09, OD-10 = yes.** One-week trial of auto permission mode, starting after the
  OD-9 deny rules land. Revert to bypass at the first false block of a routine action.
- **2026-10-09, OD-11 = owner variant.** The project stays agent-neutral, because
  non-Claude agents may also work on it.
  - Skills stay in `.agents/skills`; D-034's location clause stands and the validator is
    unchanged.
  - AGENTS.md stays the canonical, mandatory entry point.
  - CLAUDE.md gets explicit paths to each skill with a mandatory instruction to read it when
    its topic applies, so Claude behaves as if the skills were native. Stage 2 may instead
    `@import` the three small `SKILL.md` files (about 600 tokens) into CLAUDE.md if that is
    more reliable.
  - Consequences:
    - OD-7's new repo skills also live in `.agents/skills`; a Claude slash command, if
      needed, is a thin `.claude/` wrapper pointing to the canonical file.
    - OD-13 must be reframed: no "Claude Code only" decision.
- **2026-10-09, OD-12 = yes.** One generalized reviewer and one implementer agent are
  versioned in Git.
  - simple-implementer is merged into the implementer.
  - The contracts are canonical under `.agents/`, and `.claude/agents/*.md` are thin Claude
    wrappers.
  - Models are given as aliases.
- **2026-10-09, OD-13 = yes, reframed.** The agent-neutral policy stays; no "Claude Code
  only" decision is recorded.
  - Removed: retired-tool configuration (`.codex/` with the astra subagent, `.omp/`, the
    `.omp` ignore lines).
  - Kept: the thin `.github/copilot-instructions.md` adapter.
  - Finished and paused workflow records move to `docs/archive/` with corrected status
    headers.
  - The personal email and session IDs are redacted from environment.md and pending.md.
- **2026-10-09, OD-14 = yes.** T0-T3 tiers go into CONTRIBUTING.
  - T3 is set by the touched-area list, the highest trigger wins, and the reviewer checks
    the diff against the list.
  - Local `/code-review` for T1 and T2; `/security-review` inside T3 review.
  - Bug-fix tests must fail at the base commit.
  - Red-to-green is written into CONTRIBUTING.

## Open owner decisions

OD-1 to OD-30 are listed in [report.md §8](report.md#8-owner-decisions-yesno). Each can be
answered yes or no and carries a recommendation.

## Notes for later stages

- **Before the final merge into `main`:** apply OD-30, which wraps the prompt's two
  placeholder mentions in code spans, or the CI `--final` step fails.
- **At the start of every session:** run `git fetch origin` and merge `origin/main` into this
  branch, then re-check any finding whose evidence files changed.
- **Lane scratch data:** the parser scripts and intermediate data stay in the Stage 1
  session's scratchpad and are not in Git. Those files contain private transcript extracts.

## Next action

The owner answers OD-1 to OD-30. A new session then starts Stage 2 with a prompt that cites
the approved decision numbers. It begins with R1 + R7 + R6 phase 1 (OD-2..OD-6) as one change
set on this branch.
