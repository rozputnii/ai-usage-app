---
name: eco
description: Owner-invoked economy mode. The primary keeps a thin context and delegates exploration, check runs, implementation and reviews to fresh subagents on the cheapest model and effort that keeps the required quality. Use only when the owner types /eco or asks for economy mode.
---
# eco

Owner-invoked mode (decision R-222). It changes who does the work and on which model, never
what the rules require: tiers, red to green, reviews, the merge gate and the always-ask list
stay as AGENTS.md and CONTRIBUTING.md define them, and they win wherever this text seems to
differ. The mode lasts until the session ends or the owner turns it off. The evidence behind
the role map is in [model costs](model-costs.md).

## Primary: orchestrate, do not absorb

- Keep only the request, the plan, the briefs and the subagents' short reports in your
  context. Delegate any read wider than a few files, every build or test run, and code
  implementation. A long context is paid again on every turn.
- Read only what the task needs: the skill for its area and the named sections of
  CONTRIBUTING.md or verification.md (for example Risk tiers, Git flow, Checks by change),
  found with grep, not whole files.
- Write self-contained briefs: exact paths, values, acceptance items, commands, and the rule
  excerpts the worker needs, so it does not have to search the rule set for them. Excerpts
  never replace the reads a contract requires.
- Ask for the contract's short report; full output goes to a log file. Read a log only on a
  failure, and only its failing part.
- Run independent read-only subagents (exploration, check runs) in parallel. Parallel write
  workers follow CONTRIBUTING, Parallel work, unchanged.
- One task per session; start a fresh session for the next task.

## Role map

Pass the model and effort on each subagent call (Claude Code aliases; another agent uses its
nearest equivalents).

| Work | Agent | Model, effort |
| --- | --- | --- |
| Locate code or answer "where and what" across many files | Explore | haiku, high |
| Run checks (restore, build, suites, validator, diff check, smokes) and report | general-purpose with the runner brief | haiku, medium |
| Implement code or tests; diagnose a failure | aiu-implementer | opus, medium |
| Small, fully specified brief (scripts, mechanical or docs edits, renumbering) | aiu-implementer | sonnet, high |
| Fresh-context review of a T1 or T2 change (a T0 diff stays the primary's review) | aiu-reviewer | opus, medium |
| Any review of a T3 change | aiu-reviewer | opus, high |

Outside a parallel run, the implementer works in the task's worktree on the task branch and
stops after its report (implementer contract, step 4). The primary then runs the review,
numbering, records and merge under CONTRIBUTING, Git flow. In a parallel run the contract
applies unchanged.

## Escalation

- A cheap agent's report is a claim, as always; check it against observed state.
- NEEDS_CONTEXT calls for a better brief, and an environmental BLOCKED (a locked desktop, a
  missing restore) for fixing the cause, not a bigger model.
- When a subagent fails the same step twice, or reports something the state contradicts,
  rerun that step one level up: haiku, then sonnet high, then opus medium, then opus high. Do
  not retry the same level unchanged. When opus high fails too, the primary takes the step
  over or reports it BLOCKED.
- A failing check found by a runner goes to an opus medium implementer or to the primary for
  diagnosis (systematic-debugging); the runner never fixes anything.
- Never review below the map. A review that runs into a T3 area moves to opus high.
- Haiku costs five times more once a prompt passes 100k tokens: keep haiku briefs small and
  have it read excerpts, never whole large logs or files.

## Runner brief

> Work in `<worktree>`. Run these commands one at a time: `<commands with check IDs>`. For a
> desktop smoke, follow the desktop lock in docs/workflow/verification.md. Write each command's
> full output to `<log dir>/<check ID>.log`. Do not edit, commit, push or fix anything. Return
> one line per check: ID, PASS, FAIL or BLOCKED, exit code, log path, and for FAIL or BLOCKED
> at most 20 first error lines.

## Data freshness

When `checked_at` in [model costs](model-costs.md) is more than three months old, or a new
model generation has shipped, say so when the mode starts. Refreshing the data and the map is
its own verified change, never part of the task at hand.
