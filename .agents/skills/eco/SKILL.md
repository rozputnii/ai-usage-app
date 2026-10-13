---
name: eco
description: Owner-invoked economy mode. The primary works lean and mostly alone, delegates only where it pays to lean subagents on the cheapest model that keeps the required quality, and keeps a brief ledger and a path list so that no saving costs a missed detail. Use only when the owner types /eco or asks for economy mode.
---
# eco

Owner-invoked mode (decisions R-222, R-225, R-226). It changes who does the work, on which
model, and how the details are tracked, never what the rules require: tiers, red to green,
reviews, the merge gate and the always-ask list stay as AGENTS.md and CONTRIBUTING.md define
them, and they win wherever this text seems to differ. The mode lasts until the session ends or
the owner turns it off. The evidence is in [model costs](model-costs.md).

The mode was measured with the primary on opus at medium effort. The session's effort is the
owner's setting; this skill cannot change it.

## What costs

- Every call re-sends the whole context. Cached reads are about 60% of spend, so cost is
  roughly context size times the number of calls. The primary is 68-85% of a task's cost.
- A subagent starts cold: about 4-8k tokens as `aiu-scout` or `aiu-runner`, 18-55k as
  `Explore` or `general-purpose`, plus the brief and the report. Delegation pays only when it
  keeps much more than that out of your context, runs on a much cheaper model, or runs in
  parallel.
- Rework and a missed detail are the most expensive outcomes. A too-weak model on a write or a
  review is false economy.

## Primary: lean, mostly alone

- Do the work yourself unless a role below pays. Measured directionally (confounded with the
  primary's effort): delegating implementation to an opus worker saved little against the
  normal mode; a lean primary doing it cost about half.
- Put independent tool calls in one message; fewer calls is the main saving.
- Read only what the task needs: grep with line numbers, read ranges, never re-read a file you
  hold. For rules, read the skill for the area and the named sections of CONTRIBUTING.md or
  verification.md, found with grep.
- Keep command output quiet: full output to a log, then the exit code and the first error
  lines. In the inner loop run one test class; run the full suites in the background, not in
  the inner loop, and wait for the completion notice. The merge gate's run on the merged tree
  still applies.
- Keep plans, briefs and replies short. Think hard at decision points, not on routine steps.
- One task per session; start a fresh session for the next task.

## Brief ledger and path list

A cheaper session saves most of its cost by thinking less, and that is where details get
lost. Keep both lists on every task, T0 included, as CONTRIBUTING (Brief ledger and path list)
defines them, and write them right after the plan line.

## Roles

Delegate only to these agents, not to `general-purpose` or `Explore` directly: those start at
18-55k tokens against 4-8k. When one of these agents is not loaded, use a general-purpose agent
given its contract, as CLAUDE.md says. Pass the model and effort of the matching entry in
[eco models](../../models/eco.yaml) on every call (CONTRIBUTING, Subagent models).

| Work | Agent | Entry in eco models |
| --- | --- | --- |
| Locate code or answer "where and what" across many files, when much work remains | `aiu-scout` | `aiu-scout`; `aiu-scout/why` when it must explain why |
| Full check runs (restore, build, suites, validator, diff check, smokes) | `aiu-runner` | `aiu-runner` |
| A large, separable implementation with a clear brief | `aiu-implementer` | `aiu-implementer`; `aiu-implementer/small` for a small, fully specified mechanical brief; `aiu-implementer/diagnose` to diagnose an unexplained failure |
| Every fresh-context review the tier requires (per-task, whole-feature, T3 focused; a T0 diff stays the primary's review) | `aiu-reviewer` | `aiu-reviewer`, T3 included; `aiu-reviewer/security` when the diff touches a security-lifecycle area (a precaution, not measured) |

Outside a parallel run, the implementer works in the task's worktree on the task branch and
stops after its report (implementer contract, step 4). The primary then runs the review,
numbering, records and merge under CONTRIBUTING, Git flow. In a parallel run the contract
applies unchanged.

A brief is self-contained: exact paths, values, acceptance items, commands, the rule excerpts
the worker needs, which files to read, and the ledger lines and paths of its part; it asks for
the contract's short report. Excerpts never replace the reads a contract requires.

## Review brief

The review a tier requires is the whole set in CONTRIBUTING, Risk tiers, never a part of it.
The review brief names the spec, the acceptance items, the commit range, the checks to rerun,
the brief ledger and the path list, and asks the reviewer to check each ledger line and each
path against the diff. For T1 and up it tells the reviewer to run local `/code-review` on the
integrated diff where available (reviewer contract); for T3 it also asks for the blocking
focused review with the convergence-review skill and `/security-review` where available. A
step counts as NOT_RUN only with the reason the command itself gave. Do not run `/code-review`
yourself as well, unless the reviewer reports it unavailable or not run; then run it yourself
on the integrated diff. Per-task reviews in a parallel run do not replace it.

## Evidence

A delegated report is a claim. Confirm every write by observed state (tests, diff, build). A
reported PASS counts at the merge gate only after you confirm it from the log's last lines
(the `exit=<code>` line the runner contract adds, and the totals where the check prints them).
On a failure, read only the log's failing part. A runner brief names the worktree, the commands
with their check IDs and the log directory; the runner contract holds the rest.

## Escalation

- NEEDS_CONTEXT calls for a better brief, and an environmental BLOCKED (a locked desktop, a
  missing restore) for fixing the cause, not a bigger model.
- When a subagent fails the same step twice, or reports something the state contradicts,
  rerun that step one level up: haiku high, then sonnet high, then opus medium, then opus
  high. Do not retry the same level unchanged. When opus high fails too, take the step over or
  report it BLOCKED.
- A failing check found by a runner goes to you or the `aiu-implementer/diagnose` entry for
  diagnosis (systematic-debugging); the runner never fixes anything.
- Never review below the reviewer entries.
- An unavailable model is not a failure: use the entry's next model (CONTRIBUTING, Subagent
  models).
- Never run haiku below high effort (owner rule).
- Haiku costs five times more once a prompt passes 100k tokens: keep haiku briefs small and
  have it read excerpts, never whole large logs or files.

## Before the final report

- Tick the brief ledger and the path list.
- Every required review step ran, or is recorded NOT_RUN with the command's own reason.
- Your final message is the report the task asks for, complete in itself, also when a late
  reminder or notice arrives after the work is done.
- Add `eco: <solo | the delegation chain>` to the plan line, and one line to the final report:
  the delegations made, their models and efforts, and any rework.

## Data freshness

When `checked_at` in [model costs](model-costs.md) is more than three months old, or a new
model generation has shipped, say so when the mode starts. Refreshing the data and the roles
is its own verified change, never part of the task at hand.
