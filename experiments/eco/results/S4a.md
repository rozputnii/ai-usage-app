# S4a results (account `main`)

Real backlog items ([briefs](../tasks/real-backlog.md)), arm A against the frozen arm E, base
`504526a` (`main` on 2026-10-12) for every run. BL2 is out of S4a (owner, 2026-10-12).

## Usage

| When | Session | Week (all models) |
| --- | --- | --- |
| Before BL1, 2026-10-12 15:15 | 70% | 86% |
| After BL1 and its judge, 15:19 | 71% | 86% |

## BL1 - CR-T-002-01 (T0)

| Run | Arm | Exit | Wall | Cost | Calls | Subagents | Commits |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `rb62e70` | A (opus high) | 0 | 1.3 min | $1.07 | 18 | 0 | 1 |
| `r9f8923` | E (opus medium, eco v2, roles) | 0 | 0.9 min | $0.81 | 12 | 0 | 1 |

Both runs found the routing scenario gone and retired the row in `docs/backlog.md`; both diffs are
one line.

Judge `j08b51f` ($0.51, X = A, Y = E): **TIE**, confidence medium. Checks C2 and C6 PASS for both,
acceptance 3/3 for both, no false claims. A: one Minor (it restored packages from a temporary
nuget.org source instead of the documented offline cache restore, disclosed in its report); E: no
findings.

Merged: E's commit as `65f9d01` (fast-forward onto `504526a`; on a tie the one without the Minor
finding). On the merged tree C3 PASS (`{"valid":true}`) and C6 PASS; pushed to `main`,
confirmed in `origin/main`. Documentation only, so no Preview.

E against A: cost -24%, calls -33%, same quality.
