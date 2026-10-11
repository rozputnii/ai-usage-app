# Economy mode (current, frozen for arm B)

Economy mode is on for this session. Follow the skill text below. It is a frozen copy of
`.agents/skills/eco/SKILL.md` and its `model-costs.md` from `main` at 1a8526e, and it overrides
any eco skill file in this clone.

# eco

Owner-invoked mode (decision R-222). It changes who does the work and on which model, never
what the rules require: tiers, red to green, reviews, the merge gate and the always-ask list
stay as AGENTS.md and CONTRIBUTING.md define them, and they win wherever this text seems to
differ. The mode lasts until the session ends or the owner turns it off. The evidence behind
the role map is in [model costs](#model-costs).

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

When `checked_at` in [model costs](#model-costs) is more than three months old, or a new
model generation has shipped, say so when the mode starts. Refreshing the data and the map is
its own verified change, never part of the task at hand.


## Model costs

Evidence for the eco role map (decision R-222).

- checked_at: 2026-10-10
- sources: [Anthropic pricing](https://platform.claude.com/docs/en/about-claude/pricing),
  [Artificial Analysis, Opus 5.5 vs Sonnet 5.5](https://artificialanalysis.ai/models/releases/comparisons/claude-opus-5-5-vs-claude-sonnet-5-5),
  [Artificial Analysis, Haiku 5.5 vs Sonnet 5.5](https://artificialanalysis.ai/models/releases/comparisons/claude-haiku-5-5-vs-claude-sonnet-5-5),
  [Anthropic, Claude Haiku 5.5](https://www.anthropic.com/claude-haiku-5-5)

## Prices per million tokens

| Model | Input | Output | Cache read |
| --- | --- | --- | --- |
| Opus 5.5 | $4 | $20 | $0.20 |
| Sonnet 5.5 | $2 | $10 | $0.10 |
| Haiku 5.5, prompt up to 100k | $0.10 | $0.50 | $0.01 |
| Haiku 5.5, prompt over 100k | $0.50 | $2.50 | $0.05 |

Opus 5.5 and Sonnet 5.5 have no long-context premium.

## Quality and cost per task

Index is the Artificial Analysis Intelligence Index, TB4 is Terminal-Bench 4.0, and cost is
per Index task. They come from a general test mix, not this repository's workload.

| Model | Effort | Index | TB4 | Cost |
| --- | --- | --- | --- | --- |
| Opus 5.5 | low | 42 | 31.3% | $0.55 |
| Opus 5.5 | medium | 51 | 52.5% | $1.34 |
| Opus 5.5 | high | 54 | 56.6% | $1.82 |
| Opus 5.5 | xhigh | 56 | 59.6% | $3.46 |
| Opus 5.5 | max | 58 | 59.6% | $5.98 |
| Sonnet 5.5 | low | 36 | 20.7% | $0.35 |
| Sonnet 5.5 | medium | 41 | 29.8% | $0.48 |
| Sonnet 5.5 | high | 47 | 43.9% | $0.88 |
| Sonnet 5.5 | xhigh | 52 | 57.1% | $2.01 |
| Sonnet 5.5 | max | 56 | 63.6% | $5.46 |
| Haiku 5.5 | low | 29 | 12.6% | $0.02 |
| Haiku 5.5 | medium | 34 | 15.2% | $0.05 |
| Haiku 5.5 | high | 38 | 21.7% | $0.08 |
| Haiku 5.5 | xhigh | 41 | 29.3% | $0.12 |
| Haiku 5.5 | max | 43 | 32.8% | $0.21 |

## Reading

- Haiku 5.5 at max beats Sonnet 5.5 at low and medium on both scores for less money, and
  Haiku at xhigh roughly ties Sonnet at medium for a quarter of the cost, so Sonnet below high
  is never the pick. Anthropic positions Haiku for subagents and summaries, not complex agentic
  coding, so the map uses it for exploration and check runs.
- Opus 5.5 at medium costs about 2.8 times Sonnet 5.5 at medium per task, but comes within a
  point of Sonnet at xhigh on the Index (51 against 52) for two thirds of the cost, though
  lower on TB4. It is the default for implementation and ordinary reviews.
- Sonnet 5.5 at high is the cheapest step near that level and suits small, fully specified
  briefs.
- Opus 5.5 at high stays the gate for T3 reviews.
