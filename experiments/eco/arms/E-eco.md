# Economy mode v2 (frozen arm E)

Economy mode is on for this session. It changes how the work is done, never what the rules
require: tiers, red to green, reviews (including local `/code-review` and `/security-review`
where available), the merge gate and the always-ask list stay as AGENTS.md and CONTRIBUTING.md
define them, and they win wherever this text seems to differ. This text replaces any eco skill
file in this clone. Quality comes first: a saving that risks rework or a missed defect is not a
saving.

## What costs

- Every call re-sends the whole context. Cached reads cost a tenth of input but are 60% of all
  spend, so cost is roughly context size times the number of calls. Output is about 13%.
- A subagent starts cold at 5-25k tokens with the roles below (55k for `general-purpose`), plus
  your brief and its report. Delegation pays only when it keeps much more than that out of your
  context, runs on a much cheaper model, or runs in parallel.
- Rework is the most expensive outcome. A too-weak model on a write or a review is false
  economy.

## Default: do it yourself, lean

- Put independent tool calls in one message; fewer calls is the main saving.
- Keep command output quiet: write it to a log, print the exit code and the first error lines.
  In the inner loop run one test class; run the full suites once.
- Read what you need: grep with line numbers, read ranges, never re-read a file you already
  hold.
- Run long builds and suites in the background and wait for the completion notice; never poll.
- Keep plans, briefs and replies short. Think hard at real decision points, not on routine steps.

## Roles

Use these agents, never `general-purpose`, `Explore` or `aiu-reviewer`: they carry only the
tools their role needs. Pass the model and effort on each call when they differ from the
default.

| When | Agent | Model, effort |
| --- | --- | --- |
| The answer is spread over many files or its place is unknown, and much work remains | `scout` | haiku; sonnet when it must explain why, not just locate. Ask for paths, line numbers and at most 200 words |
| A large, separable implementation with a clear spec | `implementer` | sonnet high; haiku xhigh for a small, fully specified mechanical brief; opus medium only for debugging an unexplained failure |
| Independent parts | parallel workers under CONTRIBUTING, Parallel work | as above |
| Full check runs (restore, build, suites, smokes) | `runner` | haiku; for each check you report PASS, read its log's summary line yourself |
| Every review the rules require (per-task, whole-feature, T3 focused) | `reviewer` | opus medium, T3 included |
| Stuck on one specific question | `advisor` | opus high, read-only, at most 300 words |

A brief is self-contained: exact paths, values, acceptance items, commands, the rule excerpts
the worker needs and which files to read; it asks for the role's short report. Excerpts never
replace reads that a contract requires.

A review brief names the spec, the acceptance items, the commit range and the checks to rerun,
and tells the reviewer to run local `/code-review` on the diff (its contract, step 4) and, for
T3, `/security-review` where available. Do not run `/code-review` yourself as well: once per
change is what the rules require.

## Context limit

When your context passes about 150k tokens and substantial work remains, record the state in
the task document (next action, check results), then hand the remainder to an implementer as a
self-contained brief. Do not let your own context pass about 300k.

## Floors

- Reviews: never below opus medium. Areas of the security-lifecycle skill are never downgraded.
- Diagnosis of an unexplained failure: opus medium or yourself.
- A delegated report is a claim. Confirm every write by observed state (tests, diff, build).

## Escalation

- NEEDS_CONTEXT calls for a better brief; an environmental BLOCKED calls for fixing its cause.
- When a delegated result needs more than one correction round, stop delegating that kind of
  step in this task: do it yourself or use the next stronger model.
- When stuck on one specific question, ask the advisor instead of rerunning a whole step one
  level up.

## Before the final report

Check the diff against the brief once: every path, name and record the brief names exists where
it says, and every required review step ran or is recorded NOT_RUN with its reason.

## Transparency

Add `eco: <solo | the delegation chain>` to your plan line, and one line to your final report:
the delegations made, their models and efforts, and any rework.
