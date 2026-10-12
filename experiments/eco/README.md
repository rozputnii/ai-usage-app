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
| S4b | done 2026-10-12 | OMP review pair judged and merged: [results/S4b.md](results/S4b.md) |
| S4c | done 2026-10-12 | UI motion pair judged and merged: [results/S4c.md](results/S4c.md) |
| S5 | waiting | see the plan |

## Next action

S4c is done (2026-10-12, [results/S4c.md](results/S4c.md)): UI motion A better (medium); A merged
as T-070 / R-224 (`f74d2ab`); $59.21 in all (runs $55.61, judge $3.60), above the $20-35 estimate.
E cost 48% less with four Minor deviations from the brief. C9 ran on CI (owner-started dispatch,
run 38299182671) and, after the owner repaired the Build Tools components, locally as well.

Real pairs so far: BL1 TIE, BL3 A, OMP A, UI A (E won or tied 1 of 4). Under the
[decision rules](plan.md#decision-rules-fixed-before-running), E needs at least three quarters of
the pairs; with S4d-S4f left it can reach at most 4 of 7, so the real-task outcome is decided:
E (eco v2 as frozen in S3) does not pass. E saved 45-52% per pair (BL1 24%), but every loss was a
missed detail at Minor severity (a missed compatibility path, deviations from the brief), never
a Major finding or a false PASS claim. By the early-stop rule the remaining real tasks run
single-arm A while eco v2 is reworked, so S4d-S4f leave the experiment and become ordinary work.

S5 has the owner's go (2026-10-12) and runs in a new operator session. Scope, from the plan:
(1) the report, `results/S5-report.md` on this branch: per-stage aggregates S1-S4c, each
hypothesis H1-H9 with its verdict (supported, not supported, not proven) and evidence links,
the decision-rule outcome above, cost per quality, and threats to validity (small n, one judge
per pair, the S0-S3 judge base defect, C9 BLOCKED in S3 and S4c); (2) the eco v2 and roles
proposal: what to change in `.agents/skills/eco/SKILL.md` and the agent contracts so that the
savings stay and the missed details go, written as a T3 change on its own branch from `main`
under CONTRIBUTING. The proposal does not merge into `main` without the owner's review and own
testing (plan, Goal); the session asks that at its checkpoint. No paired runs unless the owner
asks; the five-hour and weekly gates still apply to any run.

Operator notes: set `$env:AIU_EXP_ACCOUNT='main'` and `$env:DISABLE_AUTOUPDATER='1'` in every
command that starts a run; launch batches as a detached process (`Start-Process powershell`), not
as a tool background task (2-hour limit), and watch the UTF-8 batch log, not the redirected output;
close any installed AI Usage app before a run (C8); write files with `[IO.File]` and absolute paths
(relative paths resolve against the process directory); watch a batch log with a polling
reader (`wc -l` and `sed -n` every 30 s), never `tail -f`, which locks the file against the
batch's writes (S4b). BL2 is out of S4a (owner, 2026-10-12).
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
