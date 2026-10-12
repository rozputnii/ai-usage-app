# Eco experiment

Measures economy mode and subagent roles on real and replayed work. Start with the
[plan](plan.md). This branch never merges into `main`.

## Status

| Stage | State | Notes |
| --- | --- | --- |
| S0 | done 2026-10-11 | plan, briefs, baseline, harness, arms, roles; self-tests in [results/S0.md](results/S0.md) |
| S4-local | done 2026-10-11 | [installed-app log digest](results/S4-local-log-digest.md); nothing else needs the current PC |
| S1 | done 2026-10-11 | new PC, account `second`: self-tests, calibration, pilot in [results/S1.md](results/S1.md) |
| S2 | done 2026-10-12 | micro-trials (runner, exploration, review, implementation) in [results/S2.md](results/S2.md) |
| S3 | running | replays on account `main`: [results/S3.md](results/S3.md) |
| S4-S5 | waiting | see the plan |

## Next action

S3 is running (owner's go 2026-10-12): finish the first runs (batch 2: RP1 B, RP3 D, RP2 A-D), judge
B, C and D against A per replay with `Start-Judge.ps1 -Brief ..\tasks\replays.md#rpN -DefectKey
..\tasks\replays.md#rpN`, run the held-out tests in a copy of each clone, repeat the two cheapest
arms on RP3, then freeze arm E. State and plan in [results/S3.md](results/S3.md).

Account switch (owner decision 2026-10-12): S3 and later run on account `main` (Max x5, $100) on the
same PC; S1 and S2 ran on `second` ($20). Measured on `main` over S3 batch 1
([results/S3.md](results/S3.md#batch-1-2026-10-12-1131-1217)): one session percent about $1.37
(a five-hour window about $137, not the assumed $90); one weekly percent $11-16 (integer
rounding allows up to $33), so the gate keeps $11 until more readings accumulate. Compare runs
only within one account. The environment on `main` was checked and
found equivalent on 2026-10-12 ([results/S3.md](results/S3.md#account-switch-main)).

## Layout

- [plan.md](plan.md): goals, hypotheses, arms, stages, metrics, decision rules, safety.
- [baseline.md](baseline.md): aggregated metrics from the owner's past sessions.
- [judge.md](judge.md): the blind pairwise judge.
- [setup-new-pc.md](setup-new-pc.md): environment checklist and the S1 procedure.
- `tasks/`: run briefs. Real tasks carry the owner's answers, so runs need no questions.
- `arms/`: prompt additions and agent definitions per arm.
- `tools/`: the harness (`Start-Run.ps1`), the batch driver (`Start-Batch.ps1`), the judge
  (`Start-Judge.ps1`) and the metrics parser (`Measure-Run.ps1`).
- `results/`: committed aggregates per stage. Raw data stays under `C:\aiu-exp`.

## Operator rules

The operator is the interactive Claude Code session on the new PC that runs the harness.

- Run one run at a time, in the background, and wait for its completion notice.
- Switch subscription accounts only between runs, and keep compared runs on one account
  ([plan](plan.md#accounts)).
- Record `/usage` (ask the owner for the number if the CLI cannot show it) before and after
  each stage in that stage's results file.
- Commit results to this branch and push `origin/exp/eco-roles` after each stage.
- The operator's own tokens are overhead, recorded separately, never added to an arm.
- Order a batch so every cell gets its first run before any cell gets its second; a budget stop
  then leaves comparable data. A batch can overshoot its budget by one run (up to about $4 for an
  opus review).
- Never delete a run folder unless its runId is known to belong to a dry run or a test; a
  cleanup by modification time once deleted a real run's evidence (S1).
