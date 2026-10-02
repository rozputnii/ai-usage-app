---
id: AIU-035
schema_version: 1
---

# Core limit model and budget engine plan

Execute sequentially in the primary session on main under CONTRIBUTING.md.
Use test-first behavioral checks in the existing Infrastructure test executable, which
already references Core. No new test dependencies or restore are needed.

### T-01 - Normalized facts and arithmetic
- status: done
- depends_on: []
- acceptance: AC-01, AC-07
- evidence: docs/specs/AIU-035-core-limit-budget/verification.md

- [x] Add quantity/cap tests, verify missing behavior, implement normalized immutable facts,
  checked comparison/subtraction and effective limit resolution.
- [x] Run targeted tests, inspect the diff, commit and push a save point.

### T-02 - Calendar, budget and display
- status: in-progress
- depends_on: [T-01]
- acceptance: AC-02, AC-05, AC-06
- evidence: not-run

- [x] Add E cases and the full design scenario with independently stated expected numbers.
- [x] Implement period resolution, local-day weights, budget results, state ranking,
  display rounding, bar positions, day-off and rush outputs; run targeted tests.

### T-03 - Reading calculations and sessions
- status: in-progress
- depends_on: [T-01, T-02]
- acceptance: AC-03, AC-04, AC-06
- evidence: not-run

- [x] Add P/S cases and failure-boundary tests; implement period transition, day-start,
  tracked consumption, session estimator and extra-spend evidence calculations.
- [ ] Run targeted tests, inspect the diff, commit and push a save point.

### T-04 - Integrated verification
- status: pending
- depends_on: [T-01, T-02, T-03]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07
- evidence: not-run

- [ ] Review actual diff and acceptance coverage, run required regression/document checks,
  record actual evidence, update canonical status, commit and push.

## Review focus

Checked money scaling and mismatched currencies; DST partial days and midnight resets;
unknown values versus zero; readings from another period/pool; reset jitter and stale readings.

## Handoff

Base: 73553fb. Existing uncommitted AIU-034 closure edits in backlog.md and its tasks and
verification are preserved. Next action: finish integrated review and record required check results.
