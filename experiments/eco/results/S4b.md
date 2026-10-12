# S4b results (account `main`)

The [OMP upstream review](../tasks/real-omp-review.md#brief) (target tag v18.9.0, T3), arm A
against the frozen arm E, base `23e48e0` (`main` on 2026-10-12) for both runs, arm A first,
`-TimeoutMinutes 240`. Both runs cloned OMP from GitHub themselves; no other network use.

## Usage

| When | Session | Week (all models) |
| --- | --- | --- |
| Before the batch, 2026-10-12 16:47 | 89% (gate not met; window reset 17:20) | 88% |
| Gate pass before arm A, about 17:28 | not recorded (log locked, see Process notes) | - |
| After arm A, 17:50 | 5% | 89% |
| After arm E, 18:01 | 7% | 89% |
| After the judge, 18:04 | 7% | 89% |

S4b spent $19.69 API-equivalent (runs $18.54, judge $1.15), well under the $40-70 estimate, for
7 session points in the new window (about $2.8 a point, close to S4a's $2.5) and 1 weekly point.

## Pair

| Run | Arm | Exit | Wall | Cost | Calls | Subagents | Commits | Output tokens |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `r66d380` | A (opus high) | 0 | 21.7 min | $12.51 | 150 | 1 | 4 | 80k |
| `r3f7101` | E (opus medium, eco v2, roles) | 0 | 11.1 min | $6.03 | 81 | 2 | 3 | 42k |

Both runs cloned OMP, listed the upstream changes per provider with commit, file and function,
moved all four provider records to v18.9.0 (`eec114a9`) with `source_verified_at` 2026-10-12 and
`live_verified_at` unchanged, adopted the Antigravity client version 2.8.0 -> 2.19.1 (OMP
7f2a17e5cf) red to green, found nothing relevant for Copilot, and ran a focused T3 review
(APPROVE, Minor record fixes only).

- A also adopted OMP 6d838a104b for Claude: the usage endpoint answers 429 with `Retry-After: 0`,
  so a zero delay now falls back to the one-minute gate, with a test from the upstream fixture.
  New capabilities: 2 backlog items (Codex credit-funded overage, Claude saved resets).
  `/code-review` and `/security-review` NOT_RUN (no `main` ref in the clone).
- E marked 6d838a104b not applicable (the scheduled path waits 10 minutes anyway). New
  capabilities: 3 backlog items (Codex credit-served window, Codex `verified_access`, Claude
  reset inventory). `/code-review` ran (nothing); `/security-review` NOT_RUN (clean tree).

Judge `jb0d791` ($1.15, 2.5 min, X = A, Y = E): **A better**, confidence medium. Both: C2, C4
(A 943, E 942), C5 348, C6, C7 PASS; acceptance 4/4; no false claims; no Important findings.
A: 1 Minor (three provider records add a "current reference" paragraph beside the old-pin source
tables instead of re-pointing them). E: 2 Minor, the decisive one that its not-applicable reason
for 6d838a104b misses the manual-refresh path (`LiveLedgerSource.RefreshAccountAsync` re-polls a
limited Claude endpoint at once after a zero Retry-After; A's test fails on E's code), plus two
merge commits missing from its Claude table.

## Merge

A's four commits on branch `fix/omp-v18-9-review` (worktree `C:\src\aiu-omp`); `main` had not
moved, so no merge commit. Numbered T-067 (the review), T-068 (Codex credit-funded overage) and
T-069 (Claude saved resets) with `Set-ItemNumbers.ps1` (`784df91`). On the merged tree: C3 PASS,
C2 PASS, C6 PASS, C4 PASS (943/943), C5 PASS (348/348), C7 PASS (0 warnings), C8 PASS (3/3,
desktop lock, after an offline `win-x64` restore). T3 gate: the run's focused review (APPROVE,
no Important) plus, at the merge, `/security-review` (no findings) and `/code-review` (two Minor,
left as they are and recorded: the zero-Retry-After rule is Claude-only; the first 429 exception
still carries a zero delay that no caller reads). No Major finding. Evidence recorded in the
T-067 verification record (`041919c`); pushed to `main`, confirmed in `origin/main`. Product
inputs changed, so the push starts a Preview (CI not watched by the operator).

Pending owner checks (T-067): a rate-limited Claude account recovers with no second 429 within
the minute, even after a manual refresh; Antigravity still reads both quota groups at 2.19.1.

E against A: cost -52%, calls -46%, wall -49%; quality lower (one missed compatibility change on
the manual-refresh path, Minor).

## Process notes

- Log lock: the operator's first watcher (`tail -F` from Git Bash) held the batch log open, so the
  batch's `Add-Content` calls failed from 16:58; the watcher's `tail` and `grep` outlived the
  monitor's expiry and were stopped by PID at 17:49. Lost: the usage polls and the gate pass that
  started arm A. The runs were unaffected. Watch a batch log with a polling reader that opens and
  closes the file (`wc -l` and `sed -n` every 30 s), never `tail -f`.
- Judge harness defect, fixed here: in `Start-Judge.ps1`, `$runA = ... | ConvertFrom-Json`
  overwrote the `[string] $RunA` parameter (PowerShell names are case-insensitive), so `.base` and
  `.account` were empty. Every judge prompt since S0 (18 judgments) said "Base commit:" with
  nothing after it, and the base and account guards never ran. Judges found the base from the
  clones' history, so verdicts stand; the guards themselves were never exercised. The variables
  are now `$infoA`/`$infoB` (checked: base and account read back for this pair).
- Neither arm can run `/security-review` inside a run clone (no `main` ref, or a clean tree), so
  for T3 work the operator runs it at the merge, as in S4a.

## Summary

| Pair | A cost | E cost | Verdict | Merged |
| --- | --- | --- | --- | --- |
| OMP review (T3) | $12.51 | $6.03 | A better (medium) | A, T-067 (`041919c`) |
