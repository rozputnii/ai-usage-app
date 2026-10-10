---
id: T-NEW
type: feature
status: approved
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-10 (fix confirmed defects of the four classes found in the T-056/T-057/T-058 run)
---
# Stale UI smokes

## Problem
- Current: every ordinary (non-Explicit) test in the UI test project was run one class at a time on `17c79c7`
  against the C7 build under the desktop lock. One test fails because the app changed by decision and the test did
  not: `LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions` looks for the cap field inside the
  card section at `LedgerSmoke.cs:80`, but since R-203 the C key opens the limit-settings popover, which is outside
  the card's UI tree, with the cap field ("Cap amount in USD") focused. It failed twice; the screenshot shows the
  popover open. The other three failures in the sweep (`CardEditingSmoke.DraggingTheGripReordersAccounts`,
  `RefreshIntervalSmoke.StepperChangesTheInterval`) came from the harness's fixed-point focus fallback, which the
  harness item fixes; with that fallback replaced in a scratch copy, every other test passed, so no other test is
  stale.
- Expected: the test finds the cap field where R-203 puts it, and the whole test passes.
- Unchanged: the app, and the test's input ("6005-x.25" gives "60.25") and its later stages.

## Requirements
- R-01: The cap stage looks up the field by its name "Cap amount in USD" (control type Edit) in the app's own
  windows, re-found on each query, as `WorkBudgetPopoverShowsCreditsInDollarsAndSetsToday` already does.

## Acceptance criteria
- AC-01: The test fails at base at `LedgerSmoke.cs:80` (sweep evidence) and passes with the fix, twice in a row under
  the desktop lock.
- AC-02: Every ordinary UI test class passes under the desktop lock on the merged tree, together with the harness
  item; C8, C6 and C2 pass.

## Out of scope
- The app's cap popover and R-203.
- The frozen Sandbox audit suite and the Explicit Sandbox-guest smokes.
