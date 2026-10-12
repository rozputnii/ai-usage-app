# S4a results (account `main`)

Real backlog items ([briefs](../tasks/real-backlog.md)), arm A against the frozen arm E, base
`504526a` (`main` on 2026-10-12) for every run. BL2 is out of S4a (owner, 2026-10-12).

## Usage

| When | Session | Week (all models) |
| --- | --- | --- |
| Before BL1, 2026-10-12 15:15 | 70% | 86% |
| After BL1 and its judge, 15:19 | 71% | 86% |
| After BL3 arm A, 15:44 | 77% | 87% |
| After BL3 arm E, 16:05 | 80% | 87% |
| After the BL3 judge and merge, 16:11 | 81% | 88% |

S4a spent $27.39 API-equivalent in runs and judges (runs $24.44, judges $2.95) for 11 session
points and 2 weekly points: one session point about $2.5 here, one weekly point about $14
(within the $11-16 of S3).

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

## BL3 - CR-T-003-03 (T3)

With `-Appendix ..\results\S4-local-log-digest.md`. The window had about 29% free before arm A.

| Run | Arm | Exit | Wall | Cost | Calls | Subagents | Commits |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `r2cc6eb` | A (opus high) | 0 | 23.8 min | $14.39 | 144 | 3 | 3 |
| `r00a86b` | E (opus medium, eco v2, roles) | 0 | 19.8 min | $8.17 | 96 | 2 | 4 |

Both runs re-assessed the finding per provider, retired it for Copilot (no refresh grant), and
built the same design in Codex, Claude and Antigravity: one renewal and one usage retry after a
401, a refused renewal shows the existing "Sign-in expired" state. Both ran C4, C5, C7, C8, C2 and
C6 and a focused `aiu-reviewer` review (APPROVE).

Judge `j234fe9` ($2.44, X = E, Y = A): **A better**, confidence medium. All checks PASS for both
(C4 935 E / 940 A, C5 347, C7, C8 3/3, C2, C6), acceptance 9/9 for both, no false claims, no
Important findings. E: 4 Minor, the decisive one that an Antigravity identity mismatch on the
renewal after a 401 is not persisted (later ticks send the grant again; A's mismatch test fails on
E's code), plus no test for it, the console guidance left unchanged while the row is marked
resolved, and `/security-review` recorded NOT_RUN although it was runnable. A: 2 Minor (a
red-at-base count out of date after review-added tests; a console change slightly beyond the app
session).

Process note: E's final message held only a postscript about a hung `cmd /c ver`, not its
structured run report; the judge read its verification record instead.

Merged: A's three commits, `main` (`65f9d01`) merged in (one `docs/backlog.md` conflict, the
adjacent CR-T-002-01 and CR-T-003-03 rows; both kept), numbered T-064 with `Set-ItemNumbers.ps1`
(`397c6cb`). On the merged tree: C3 PASS, C6 PASS, C4 PASS (940/940), C5 PASS (347/347), C7 PASS
(0 warnings), C8 PASS (3/3, desktop lock; the UI suite needed an offline `win-x64` restore before
`publish --no-restore`, NU1507 otherwise). The run's focused review (APPROVE, no Important) and
`/security-review` (no findings) cover the T3 gate; no Major finding. Pushed to `main`, confirmed
in `origin/main`; product inputs changed, so the push starts a Preview (CI not watched by the operator).

Pending owner checks (in the backlog, T-064): live renewal after a usage 401 in the installed app.
Open owner question from arm A (recorded in the T-064 spec): after a refused Codex renewal the next
manual refresh or launch shows "Provider error" with no Sign-in action instead of "Sign-in
expired"; changing it touches how the stored Codex renewal record recovers (security lifecycle).
A recommends showing an intact, unfinished renewal as "Sign-in expired".

E against A: cost -43%, calls -33%, wall -17%; quality lower (one durable fail-closed gap, Minor).

## Summary

| Pair | A cost | E cost | Verdict | Merged |
| --- | --- | --- | --- | --- |
| BL1 (T0) | $1.07 | $0.81 | TIE | E, `65f9d01` |
| BL3 (T3) | $14.39 | $8.17 | A better (medium) | A, `397c6cb` |
