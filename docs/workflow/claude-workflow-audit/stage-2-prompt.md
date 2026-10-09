GOAL (Stage 2 of the workflow-optimization effort): implement the owner-approved decisions
OD-1 to OD-31 from Stage 1 of the Claude workflow audit.

Read first:
- docs/workflow/claude-workflow-audit/status.md: the authoritative record of the decisions
  taken on 2026-10-09. Where it differs from report.md §8, status.md wins.
- report.md §4-6 (target workflow, recommendations R1-R18, KEEP list) and the lane reports
  in lanes/ as needed.

Git:
- Work on the existing branch `workflow-optimization` (on origin). Switch to it. If another
  worktree holds it, stop and tell me.
- At the start of every session, fetch and merge origin/main into it (merge, never rebase).
- Commit and push to origin/workflow-optimization after each meaningful step.
- Never merge into main or open a PR without my explicit decision.
- Save this prompt verbatim as docs/workflow/claude-workflow-audit/stage-2-prompt.md in the
  first commit.

Scope and order:
- Follow the "Next action" order in status.md.
- Change only what an approved decision covers: rules, docs, agents, skills, project
  settings, validator, CI workflow, tests, smoke harness.
- Host or account actions approved by OD-10, OD-22 and OD-27..OD-29 are allowed: pin
  versions, and record what was installed or changed and how to undo it.
- Anything not covered: write it into status.md as a new numbered owner decision. Don't ask
  mid-run unless it is an "always ask" item.

Principles:
- Agent-neutral (OD-11). AGENTS.md and .agents/ hold the canonical rules and skills. Claude
  files (CLAUDE.md, .claude/agents, .claude/skills wrappers) are thin pointers.
- Keep everything on the KEEP list (report.md §6).
- Changes to deny rules or permission settings, CI/Preview, and agent config are T3. They
  need a focused independent review before merge.
- OD-31 ID renaming only when `git branch -a --no-merged origin/main` shows no in-flight
  work branches. Otherwise defer it and record that.

Verification:
- Follow docs/workflow/verification.md, and report PASS/FAIL/NOT_RUN/BLOCKED honestly.
- The validator must pass in normal mode. Under --final, the only allowed failures are the
  two known stage-1-prompt placeholder mentions (OD-30).
- For R9, produce a rule-inventory diff (each rule: kept / moved / merged / removed, with
  its new location) for me to review before the branch merges.

Orchestration:
- One primary agent.
- Parallel workers and independent reviewers are allowed per OD-14..OD-16.

Continuity and reporting:
- Update status.md after each step: done items, check results, one exact next action.
- Every final reply: what changed, check results, remaining items, push status of the
  branch verified with git fetch, and confirmation that main is untouched.
