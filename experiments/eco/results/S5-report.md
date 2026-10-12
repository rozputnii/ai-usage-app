# S5 report (2026-10-12)

The eco experiment's final report: what each stage measured, a verdict per hypothesis, the
decision-rule outcome, cost against quality per pair, and the limits of the evidence. Every
number comes from the committed stage results ([S0](S0.md), [S1](S1.md), [S2](S2.md),
[S3](S3.md), [S4a](S4a.md), [S4b](S4b.md), [S4c](S4c.md)) or from the runs' `metrics.json` under
`C:\aiu-exp\results` (aggregated here; the raw data stays local). Costs are API-price
equivalents, a proxy for the subscription limit ([plan](../plan.md#budget-and-gates)).

## Outcome in one paragraph

Arm E (eco v2 as frozen in S3: primary opus medium, the eco v2 text, lean roles, every review
by a lean reviewer at opus medium) cost about half of the normal mode on real work (median 54%
of A; 24-52% saved per pair) with no Important or Critical finding and no false PASS claim. It
still fails the real-task rule: it won or tied 1 of 4 real pairs (BL1 tie; BL3, OMP and UI
lost to A), so eco v2 as frozen does not pass. Every loss was a missed detail at Minor
severity: a sibling or caller path left unhandled (BL3, OMP), and deviations from the brief's
exact values and implied records (UI). The savings came mostly from the primary (60% of the
saving) and the review set (40%), not from delegation: E's roles were barely used.

## Per-stage aggregates

| Stage | Account | What ran | Spend (runs + judges) | Result |
| --- | --- | --- | --- | --- |
| S0 | owner's PC | harness self-tests, smokes | $0.18 | harness PASS after 3 fixes |
| S1 | `second` | self-tests, calibration, pilot RP3 A against C | $1.94 | pilot TIE (medium); C 8% cheaper; judge blinding defect fixed |
| S2 | `second` | runner (4 runs), exploration (72), review (23 valid), implementation (10) | $42.84 | H3 not supported, H4, H5 (first half), H6 supported, H9 not supported |
| S3 | `main` | replays RP1-RP3 x A-D (14 valid + 2 repeats), 11 judgments, arm E frozen and validated (2 runs, 2 judgments) | $84.83 | C and D meet the cost rule (61% of A); A better on RP1 in every pair; E 48-61% of A, A better on both validations |
| S4a | `main` | BL1 and BL3 pairs | $27.39 | BL1 TIE, BL3 A better |
| S4b | `main` | OMP review pair | $19.69 | A better |
| S4c | `main` | UI motion pair | $59.21 | A better |

Operator overhead (not counted in any arm): about $4.5 in S1 and $15.55 in S3; the others are
in the stage files where measured. Weekly usage on `main` went from 79% (S3 start) to 93%
(S4c end).

### Replays, cost against A (S3)

| Arm | RP1 | RP2 | RP3 (median) | Median | Judge against A | Held-out (RP1, RP2) |
| --- | --- | --- | --- | --- | --- | --- |
| B (current eco, opus high) | 93% | 66% | 103% | 93% | A, B, A | 10/10, 19/20 |
| C (eco v2 text, opus medium) | 50% | 61% | 76% | 61% | A, C, TIE/A | 9/9, 19/20 |
| D (C plus lean roles) | 51% | 61% | 73% | 61% | A, D, A/TIE | 9/9, 19/20 |
| E (frozen, validation) | 48% | 61% | - | - | A, A | 9/9, 19/20 |

A: RP1 9/9, RP2 18/20. No arm had a false PASS claim. C and D skipped the required local
`/code-review` in 3 of 4 runs, which E's tuning fixed ([S3](S3.md#rule-compliance-found-by-the-operator)).

### Real pairs (S4)

| Pair | Tier | A cost | E cost | E saved | Calls saved | Wall | Verdict | Findings A / E (all Minor) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| [BL1](S4a.md#bl1---cr-t-002-01-t0) | T0 | $1.07 | $0.81 | 24% | 33% | 1.3 / 0.9 min | TIE (medium) | 1 / 0 |
| [BL3](S4a.md#bl3---cr-t-003-03-t3) | T3 | $14.39 | $8.17 | 43% | 33% | 23.8 / 19.8 min | A (medium) | 2 / 4 |
| [OMP](S4b.md#pair) | T3 | $12.51 | $6.03 | 52% | 46% | 21.7 / 11.1 min | A (medium) | 1 / 2 |
| [UI](S4c.md#pair) | T2 | $36.66 | $18.95 | 48% | 34% | 50.7 / 51.0 min | A (medium) | 2 / 4 |
| All | | $64.63 | $33.96 | 47% | | | E 1 of 4 | 6 / 10 |

Median E cost 54% of A. Zero Important or Critical findings and zero false claims in both arms;
every check the judges ran passed in both arms except C9, BLOCKED in both on the UI pair.

### Where the cost went (from the runs' `metrics.json`)

| Pair | A primary | E primary | A subagents | E subagents | A primary max context | E primary max context | A / E primary thinking tokens |
| --- | --- | --- | --- | --- | --- | --- | --- |
| BL3 | $9.72 | $5.99 | $4.67 | $2.17 | 271k | 190k | 26k / 9k |
| OMP | $8.95 | $4.81 | $3.56 | $1.21 | 250k | 172k | 16k / 6k |
| UI | $26.75 | $16.13 | $9.91 | $2.81 | 450k | 294k | 84k / 55k |

- The primary is 68-85% of every non-trivial run (BL1 had no subagent). About 60% of E's
  saving came from the primary (fewer calls, a shorter context, far less thinking at medium
  effort) and 40% from subagents: one review at opus medium ($1.06-1.50 against A's opus high $2.78-4.58) and `/code-review` once
  per change instead of in the primary and the reviewer.
- E's roles were barely exercised: across 4 real pairs and 2 validation replays, E dispatched
  one scout (sonnet, UI pair, $0.59), never a runner, an implementer or the advisor. The
  primary did all implementation itself.
- E never handed off at 150k although its text said so: its primary passed 150k in four of
  five non-trivial runs (up to 294k).

## What E missed

Every E loss, with the judge's decisive finding:

| Pair | Missed detail | Kind |
| --- | --- | --- |
| BL3 | An Antigravity identity mismatch on the renewal after a 401 is not persisted; no test for it | sibling failure branch not covered |
| BL3 | Console guidance unchanged while its row is marked resolved; `/security-review` recorded NOT_RUN although runnable | implied record; required step |
| OMP | "Not applicable" for the zero Retry-After commit ignored the manual-refresh path (`RefreshAccountAsync`) | caller path not enumerated |
| OMP | Two merge commits missing from the Claude table | completeness of a listed record |
| UI | Eased tail runs to 675 ms against the brief's about 450 ms; period-bar segments matched by index; the dragged card's history panel slides | exact values and stated behavior of the brief |
| UI | R-205 not amended by the new decision | implied record (formats.md amendment pointer) |
| RP1 (S3) | Spoken name on a `Border`; missing "Pending owner checks" row for a NOT_RUN owner check | stated behavior; implied record |

Two patterns cover all of them: (1) a path, caller or sibling branch next to the changed one
was not listed and checked; (2) an exact value, a stated behavior or a record that the brief or
the rules imply was not ticked off. E's final "check the diff against the brief once" did not
catch them, and neither did its opus medium reviewer, which approved every E change. E also
twice ended with a malformed final message instead of the report block (BL3, UI).

## Hypotheses

| ID | Hypothesis | Verdict | Evidence |
| --- | --- | --- | --- |
| H1 | Handing off past ~150k cuts cost most without quality loss | not proven | No trial isolated it; E's text had the rule and its primary never used it (4 of 5 non-trivial runs passed 150k). The lever itself is real: max context drives cache reads, and three long sessions were 29% of baseline spend ([baseline](../baseline.md)). |
| H2 | For T0/T1 the primary alone is cheaper than delegating | not proven | No arm delegated on T0 (RP3, BL1: all solo, $0.42-1.07), and no T1 task ran. Directional: on T2 RP1, delegating arm B cost 93% of A against 50-51% for the lean solo arms C and D ([S3](S3.md#decision-rules)). |
| H3 | A haiku runner is not cheaper than the primary running quiet checks | not supported | The haiku runner was 23% cheaper on quiet passing checks (2 runs each; cold start 7.7k) ([S2](S2.md#runner-h3)). Never used in a real task. |
| H4 | Sonnet high matches opus medium on a specified brief, not on a judgment brief | supported (stronger than stated) | Sonnet high matched opus medium on both briefs at 40-55% of the cost; haiku xhigh matched both on the specified brief at under 10% ([S2](S2.md#implementation-h4)). Small briefs only; no real-task implementer. |
| H5 | Review below opus medium misses known defects; opus high adds recall only on T3 | first half supported, second not supported | Sonnet found 7.5 of 12 Important items, opus medium 11 of 12, over two repeats; opus high found 2 of 4 on the T3 change in both repeats against 3-4 for opus medium ([S2](S2.md#review-h5-second-repeat-and-combined)). Real pairs cannot separate reviewer effort from primary effort. |
| H6 | Lean roles cut cold start and calls without quality loss | supported (micro); small real effect | Lean scout 23% cheaper than Explore at equal accuracy, cold start 4k against 18.6k ([S2](S2.md#exploration-h6-h9)); D tied C in S3 because roles were barely used ([S3](S3.md#decision-rules)); in S4 the lean reviewer was the only role used regularly. |
| H7 | A short opus high advisor is cheaper than rerunning a step one level up | not proven | No micro-trial by design; the advisor was never called in S3-S4. |
| H8 | Judgment eco (C) is cheaper than current eco (B) and not worse than A | not supported | Cheaper than B: yes (61% against 93% of A). Not worse than A: no; C lost RP1 to A, and its frozen descendant E lost 3 of 4 real pairs and both validation replays on Minor items ([S3](S3.md#judgments), S4). |
| H9 | LSP or graphify answer location questions with fewer tokens at equal accuracy | not supported | The LSP tool was never called in 36 runs; graphify gave no accuracy gain at 22% more cost ([S2](S2.md#exploration-h6-h9)). |

## Decision rules

- Arm rule ([plan](../plan.md#decision-rules-fixed-before-running)): E meets every part on the
  real pairs (median cost 54% <= 75%; no more Major or Critical findings than A, zero each; no
  false PASS). In S3, C and D met it too.
- Real-task rule: E must win or tie at least three quarters of the pairs. It won or tied 1 of
  4; with S4d-S4f it could reach at most 4 of 7, so **E fails**. No merged result carries a
  Major finding (each merge was A's, or E's on the BL1 tie).
- Early stop: after three pairs E was clearly worse on quality (1 of 3), so the remaining real
  tasks (S4d-S4f) run single-arm A as ordinary work while eco v2 is reworked.
- Consequence: eco v2 is not adopted as frozen. The rework below targets the two miss patterns
  and keeps the measured savings; it must be tested by the owner before it merges.

## Cost against quality per pair

| Pair | Saved by E | Extra Minor findings in E | Saved per extra Minor | Quality gap |
| --- | --- | --- | --- | --- |
| BL1 | $0.26 (24%) | -1 | - | none (E had fewer findings) |
| BL3 | $6.22 (43%) | +2 | $3.11 | one durable fail-closed gap (identity mismatch not persisted) |
| OMP | $6.48 (52%) | +1 | $6.48 | one missed caller path (manual refresh after a zero Retry-After) |
| UI | $17.71 (48%) | +2 | $8.86 | deviations from exact brief values and one missing amendment |
| RP1 (S3, E) | $7.45 (52%) | +1 to +3 | - | UI accessibility detail, missing owner-check row |
| RP2 (S3, E) | $2.20 (39%) | +1 to +2 (A had 1 Important in one of three judgments) | - | Minor only |

The gaps are each cheap to fix once known (A's tests fail on E's code in BL3 and OMP), but the
owner's priority order puts quality first, and a missed fail-closed branch in an
authentication path is the kind of gap that reaches users. The proposal therefore spends part
of the saving on targeted checks instead of on a stronger model everywhere.

## Threats to validity

- **Small n.** One run per arm per real task, one per arm per replay (RP3 two), two repeats per
  micro-trial cell. Verdicts are directional; a single pair can flip.
- **One judge per pair.** Each real pair had one blind opus high judge, at medium confidence.
  Judge depth varies between runs: A's RP2 Important item was seen by one of three judges. The
  operator spot-checked verdicts but did not re-judge.
- **S0-S3 judge base defect.** `Start-Judge.ps1` overwrote its `$RunA` parameter, so 18 judge
  prompts (S0 to S4a) said "Base commit:" with nothing after it and the base and account guards
  never ran. Judges found the base from history, so verdicts stand, but the guards were never
  exercised ([S4b](S4b.md#process-notes)). A second judge-input defect, an operator note leaking
  RP1's spec path, made one Minor finding against C, D and E void ([S3](S3.md#correction-the-spec-path-finding-is-a-judge-artifact)).
- **C9 BLOCKED** in S3 RP1 (every arm) and in both S4c runs, from the host's Build Tools; equal
  across arms, fixed after S4c, so the package build was never compared between arms.
- **Confounded arm.** E changes the primary's effort, the prompt, the roles and the reviewer's
  effort at once, so the quality loss cannot be attributed to one of them. In particular, A's
  reviewer ran at opus high and E's at medium; S2 says effort does not change review recall on
  seeded defects, but the real misses were of a different kind.
- **Order and cache.** A ran first in every S4 pair; costs are reported as observed, not
  normalized for a cold start.
- **Accounts.** S1-S2 ran on `second`, S3-S4 on `main` with different connectors; comparisons
  stay within one account, and absolute costs across the switch are not comparable.
- **Same family.** The runs, the judges and the operator are the same model family; the judge
  could share blind spots with both arms.
- **Proxy currency.** API-price cost is a proxy for the subscription limit; one weekly percent
  measured $11-20 on `main` with integer rounding.

## Proposal

The eco v2 and roles proposal is a separate T3 change on branch `feat/eco-v2-roles` from
`main` (`f74d2ab`). It is not merged: the owner reviews and tests it first ([plan](../plan.md#goal-and-priorities)).
In short:

- Keep what saved: the primary works lean and mostly solo; one review per change at opus
  medium for every tier with `/code-review` run once by the reviewer; lean read-only roles
  (`aiu-scout`, `aiu-runner`) instead of `general-purpose` and `Explore`; sonnet high as the
  default implementer.
- Fix what was missed: a brief ledger written at the start (every exact value, named path,
  stated behavior and implied record) and ticked off with evidence at the end; a path list for
  every changed or "not applicable" behavior (callers, sibling providers, failure branches);
  both go into the review brief, and the reviewer contract checks them when a brief carries
  them; the final message is the report block.
- Drop what was not proven: the 150k handoff rule and the advisor role.
- Added as a precaution, not measured: reviews of a diff in a security-lifecycle area stay at
  opus high.

State: commits `23e468e` and `1588699`, pushed to `origin/feat/eco-v2-roles`. C2 and C6 PASS.
Focused T3 review (`aiu-reviewer`, opus high, convergence-review, local `/code-review`):
CHANGES_REQUIRED with 2 Important (a wrong number in the model-costs evidence; the ban on
`general-purpose` contradicted the documented fallback when a role is not loaded) and 5 Minor
(among them a missing "Amended by" line on R-217, the very kind of implied record the ledger
targets); all fixed except one declined with its reason (the `aiu-implementer` description is
normal-mode guidance); re-check APPROVE, 0 open findings. `/security-review` NOT_RUN: launched
from the reviewer it bound to the experiment checkout, not the proposal branch; the reviewer's
manual security pass found nothing.

Merged 2026-10-12 on the owner's instruction, before own testing: the owner added that haiku
never runs below high effort (the runner moved from haiku medium to high; re-check APPROVE).
R-NEW became R-225; C2, C3 and C6 PASS; pushed to `main` as `e93c9e7`. Owner check pending:
use `/eco` at opus medium on ordinary tasks and watch the first few for the two miss patterns.
