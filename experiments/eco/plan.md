# Eco experiment plan

Owner-approved plan (2026-10-11) for measuring and improving economy mode and subagent roles.
This branch (`exp/eco-roles`) never merges into `main`. Real-task results merge into `main`
through the normal process; experiment files and raw data stay here or local.

## Goal and priorities

Find an economy mode (eco v2) and a set of subagent roles that are, in this order:

1. **Not worse in quality** than the normal mode (the hard gate; savings never justify a loss).
2. Cheaper in tokens (measured as API-price-equivalent cost and as `/usage` percent).
3. Faster (wall-clock).
4. Using Claude Code well: subagents, current models, right effort, lean roles, parallel tool
   calls, background runs, context limits, optional code-intelligence tools.

The normal mode stays unchanged. Eco v2 is proposed as a separate change only after the
report (stage S5) and the owner's own testing; replacing the normal mode is a later owner
decision.

## Evidence so far

See [baseline](baseline.md). Key facts: the primary is ~70% of spend; cache reads are 59-66%
of spend (context size x calls); output is 12-13%; three long sessions are 29% of primary
spend; a subagent's cold start is 24k tokens (7 tools) to 55k (42 tools, MCP inherited).

## Hypotheses

- H1 Context limit: handing off or starting a new session when the primary context passes
  ~150k cuts cost most, without quality loss.
- H2 Solo T0/T1: for T0/T1 the primary alone is cheaper than delegating.
- H3 Runner: a haiku runner is not cheaper than the primary running quiet commands itself.
- H4 Implementation model: sonnet high matches opus medium on a fully specified brief, not on
  a brief that needs judgment.
- H5 Review model: review below opus medium misses known defects; opus high adds recall only
  on T3.
- H6 Lean roles: tool allowlists, `disallowedTools: mcp__*`, preloaded skills only, and
  `omitClaudeMd` for read-only roles cut cold start and calls without quality loss.
- H7 Advisor: a short opus high consultation is cheaper than rerunning a step one level up.
- H8 Judgment: "judgment + guardrails" eco (arm C) is cheaper than the current eco (arm B) and
  not worse than the normal mode (arm A).
- H9 Code intelligence: LSP (`csharp-ls`) or graphify answers code-location questions with
  fewer tokens than grep at equal accuracy.

## Arms

| Arm | Primary | Prompt addition | Agents |
| --- | --- | --- | --- |
| A | opus, high | none (normal mode) | repository agents |
| B | opus, high | current eco skill text ([arms/B](arms/B-eco-current.md) points to it) | repository agents |
| C | opus, medium | [eco v2 candidate](arms/C-eco-v2.md) | repository agents |
| D | opus, medium | C | C plus [lean roles](arms/D-agents.json) |
| E | per S3 result | the frozen winner of C/D, possibly tuned | per S3 result |

All arms use the same MCP configuration (the account default) so that the inherited-tool
effect is measured, not hidden. Arm B gets the current eco skill injected through
`--append-system-prompt-file`, because older replay bases do not contain it.

## Levels

1. **Micro-trials** (S2, cheap, 2-3 repeats, one variable at a time):
   [exploration](tasks/micro-exploration.md), [review](tasks/micro-review.md),
   [implementation](tasks/micro-implementation.md), runner, advisor.
2. **Replays** (S3, fictitious, never merged): [three historical tasks](tasks/replays.md)
   x arms A-D, from their base commits, with the reference solution as ground truth.
3. **Real tasks** (S4, merged): each task runs in arm A and arm E with the same brief from
   the same commit; the [blind judge](judge.md) picks the better result, which merges.

## Real tasks and stages

| Stage | Machine | Content |
| --- | --- | --- |
| S0 | owner's current PC | this folder: plan, briefs, baseline, harness, arms, roles |
| S1 | new PC | [setup](setup-new-pc.md), harness self-test, pilot, `/usage` calibration |
| S2 | new PC | micro-trials |
| S3 | new PC | replays; freeze arm E |
| S4-local | owner's current PC, after S3 | [backlog items](tasks/real-backlog.md) CR-T-002-01, WR-T-061-M1, CR-T-003-03, with read access to the installed app's logs; plus a sanitized log digest for the performance task |
| S4b | new PC | [OMP review](tasks/real-omp-review.md) |
| S4c | new PC | [UI/UX and motion](tasks/real-ui-motion.md) |
| S4d | new PC | [performance and memory](tasks/real-performance.md) |
| S4e | new PC | [test suite optimization](tasks/real-tests.md) (after S4c-S4d merged: tests are the safety net during refactors) |
| S4f | new PC | [architecture analysis](tasks/real-architecture.md) (last: sees the final code) |
| S5 | any | report, eco v2 and roles proposal as a separate change |

Branch `t061-tray-failure-recovery` is untouched until S4-local (owner decision 2026-10-11).

## Budget and gates

- The currency is the owner's weekly Max x5 limit. API-price cost is a proxy only; usage on
  other devices (for example Fable) shares the limit, so no fixed $-to-% factor is assumed.
- S1 calibrates: record `/usage` before and after a fixed calibration run.
- Before each stage, the operator records `/usage`; a stage starts only when the remaining
  weekly limit covers its estimate plus 10%. Otherwise wait for the weekly reset.
- Rough API-equivalent estimates: S1 $60, S2 $130, S3 $180, S4 about $800 paired.
- A run that hits a usage limit or fails for environmental reasons is invalid and reruns.

## Accounts

The owner may use a second subscription and switch between the two (2026-10-11). Nothing in
the experiment depends on one account: the plan, briefs and harness are in Git, no step uses
Claude Code memory, and the cost metric is computed from local transcripts. Rules:

- Each account gets its own Claude Code configuration folder through `CLAUDE_CONFIG_DIR`
  (settings, sign-in, session history and plugins live there), and a short label in
  `AIU_EXP_ACCOUNT` (`main`, `second`; never an email). Switch by opening a shell with the
  other pair of variables, never with `/login` while a run is active: the harness runs inherit
  the shell's variables, and `Measure-Run.ps1` reads the same folder.
- Switch only between runs, never during one.
- Runs that are compared (a pair A/E, the variants of one micro-trial question, the arms of one
  replay) use the same account: accounts can differ in connectors (which change subagent
  cold starts), prompt cache and plan limits. `Start-Judge.ps1` warns when a pair mixes them.
- Budget gates and the `/usage` calibration are per account; record the label with every
  `/usage` reading. If the second subscription is a different plan, check in S1 that it has the
  models the arms use (opus, sonnet, haiku) before giving it any compared runs.

## Metrics

Collected by [Measure-Run.ps1](tools/Measure-Run.ps1) from the run's transcripts (the primary
and every subagent). One API request is logged on several lines with the same `requestId`;
the final line per request counts.

- Cost: API-equivalent USD split into cache write (5m and 1h), cache read, input, output;
  tokens; thinking tokens; calls; maximum context; subagents and their cold starts; models.
- Speed: wall-clock from the harness.
- Quality, most objective first: held-out tests; the defect key (known defects present or
  avoided); the blind judge's findings by severity; rule compliance (checks really run, red to
  green, records); rework (correction rounds, escalations); false PASS claims.
- Behavior in C/D/E: the delegation decisions taken and whether they were justified.

## Validity controls

- Isolation: every run uses a throwaway clone under a short root (`C:\aiu-exp`), history
  truncated to the base commit, no remote, so no push and no view of later commits.
- Fixed conditions: CLI auto-update off, model IDs recorded, the same check set per task.
- Prompt-cache order effects: randomize arm order; report cost as observed and normalized to
  a cold start (first-call cache reads counted as writes).
- Paired design: arms compared on the same tasks; small n, so thresholds are large and
  conclusions directional.
- Blinding: run clones carry random IDs; commits must not mention the mode or the experiment;
  the judge sees X/Y in random order.
- Objective metrics outrank the LLM judge; the operator spot-checks judge verdicts.

## Decision rules (fixed before running)

- An arm wins when all hold: median cost <= 75% of arm A; no more Major/Critical findings than
  arm A; held-out tests pass no worse; zero false PASS claims.
- A model or role choice is adopted when it finds every known Major defect (review) or passes
  the held-out tests (implementation) at lower cost than the opus medium reference.
- Real tasks: E must win or tie in at least three quarters of the pairs (6 of 8 when all run),
  and no merged result may carry a Major finding.
- Early stop (owner decision 2026-10-11): after the first 3 real pairs, if the outcome is
  clear (E ties or wins on quality in all 3 and saves at least 40%, or E is clearly worse),
  the remaining real tasks run single-arm (E, or A while eco v2 is reworked). Otherwise
  pairs continue to the end.
- A tie goes to the simpler rule; a result that does not reproduce in its repeat is "not
  proven" and changes nothing.

## Safety

- Runs use `bypassPermissions` only inside throwaway clones without a remote (owner decision
  2026-10-11). Real-task results come into the real repository by `git fetch` from the
  clone; the operator session merges the winner under CONTRIBUTING, Git flow.
- No live-provider checks, no credential reads, no sign-in. Owner checks happen after deploy.
- Raw transcripts and logs stay local; only aggregated metrics without paths or identifiers
  are committed here.
- Installed-app logs (S4-local) are read locally; only counts and record types leave the PC.
