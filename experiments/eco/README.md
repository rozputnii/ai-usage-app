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
| S3 | done 2026-10-12 | replays on account `main`, arm E frozen: [results/S3.md](results/S3.md) |
| S4a | done 2026-10-12 | BL1 and BL3 pairs judged and merged: [results/S4a.md](results/S4a.md) |
| S4b-S5 | waiting | see the plan |

## Next action

S4a is done (2026-10-12, [results/S4a.md](results/S4a.md)): BL1 TIE (E merged, `65f9d01`), BL3
A better (A merged as T-064, `397c6cb`); $27.39 in all. Next: S4b ([OMP review](tasks/real-omp-review.md))
needs the owner's go and a budget; the weekly limit on `main` was at 88% at 16:11 and resets on
2026-10-13 12:00. Two owner items from BL3 are open: the post-deploy live check of T-064, and the
Codex "Provider error" after a refused renewal (arm A recommends "Sign-in expired"; recorded in the
T-064 spec).

Operator notes: set `$env:AIU_EXP_ACCOUNT='main'` and `$env:DISABLE_AUTOUPDATER='1'` in every
command that starts a run; launch batches as a detached process (`Start-Process powershell`), not
as a tool background task (2-hour limit), and watch the UTF-8 batch log, not the redirected output;
close any installed AI Usage app before a run (C8); write files with `[IO.File]` and absolute paths
(relative paths resolve against the process directory). BL2 is out of S4a (owner, 2026-10-12).
Both eco-skill defects (the unread PASS from S2, the missing `/code-review` from S3) are fixed in `main`
(`504526a`, merged 2026-10-12 on the owner's request; T3 review APPROVE, C2/C3/C6 PASS).

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
