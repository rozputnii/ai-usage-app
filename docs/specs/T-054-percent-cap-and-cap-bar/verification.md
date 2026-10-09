# T-054 verification

Evidence for [the specification](spec.md). Host: the owner's Windows 11 Pro 10.0.26200
desktop, .NET 10, branch `users/subscription-cap-optional-1f5c54` from `main` at
`a0f0d22`, merged with `main` at `65f7fcb` (R-201). Times are local, 2026-10-08.

## Source checks before the change

- `EffectiveLimit.Resolve` returned 100 % with `CapRejected` for any cap on a percent
  window, and the live projection gave percent windows no cap target (T-035 acceptance 3).
- `CardVisuals.BarMax` returned the provider limit when one was known, so a cap of 500
  against 2000 filled a quarter of the bar, with the rest hatched and a tick at the cap.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 905/905 on the merged tree (902 before the change) |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 297/297 on the merged tree (292 before) |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, 85/85 |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json --final` | PASS, valid with no diagnostics |
| App build (README) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors |
| Feature UI smoke | `AiUsage.Windows.Tests -method "*WorkBudget*"` against the Debug unpackaged build, demo mode, isolated temporary state | PASS, 2/2 before and after the merge. The popover smoke opened "Limit settings, Codex subscription 7 day", saved 90 in "Cap amount in %" and found "of 90 % cap"; the screenshot shows "100 % of 90 % cap" with the tick at 90 % of the used-up bar. |
| Ledger launch smoke | `AiUsage.Windows.Tests -method "*LedgerLaunchSettingsHistoryAndExit*"` against the same build | PASS, 2/2 (demo and live-empty) before and after the merge |
| Diff check | `git diff --check` | PASS |
| MSIX package build | CI on push | NOT_RUN locally; CI builds the unsigned package |

Every new test failed before its implementation (RED) and passed after it. Screenshots are
in the git-ignored `.ai-usage-local/AIU-NEW/` and `.ai-usage-local/AIU-054/` folders of the
worktree.

## Acceptance

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 Core | PASS | `LimitModelTests.PercentCapRules` (90 binds, 100 is the provider, another unit, 101, money, a five-hour or unknown-length window are rejected, a monthly window takes it) and `BudgetEngineTests.PercentCapIsTheBudgetLimit` (limit 90, norm from 90, 90 is at the cap without "used up", 95 is over, 100 is used up). |
| AC-02 Five-hour count | PASS | `SessionEstimateTests.WindowsLeftCountAgainstTheCap` (4 without a cap, 3 with 90, 0 at 95, none for a limit of 101). The projection passes the card's effective limit; no projection test drives a ready estimate (ruling below). |
| AC-03 Targets and storage | PASS | `LiveLedgerProjectionTests.WeeklyPercentWindowTakesACapAndBudgetsAgainstIt`, `LiveLedgerSourceTests.PercentCapIsStoredWholeAndListedInPercent` (90 stored as a `percent` count, 101 and 90.5 rejected, Settings › Caps in percent, removal) and `PercentCapOnAFiveHourWindowIsNotAppliedAndCanBeRemoved`. |
| AC-04 Bars | PASS | `LedgerCardTests.CapIsTheFullBarAndATickMarksItOnceExceeded` (cap 300 against 500 at 218: no tick, no hatched segment; 320: tick at 93.75 %), `FiveHourDividersFollowTheCapScale` (dividers at k / 90, "47 % of 90 % cap · ≈ 4 × 5h left", "43 % left to cap", tooltip "Custom cap 90 % · binds" and "Provider limit 100 % · 53 % left"). |
| AC-05 Popover | PASS | `LedgerCardTests.WeeklyPercentWindowTakesACapInItsPopover` (unit "%", hint "· at most 100 %", 101 and 90.5 refused, 90 saved, footer "47 % of 90 % cap") and the feature UI smoke above. |
| AC-06 Checks | PASS | Commands above. |
| Live weekly windows in the installed app | NOT_RUN (post-deploy owner check, R-190) | Needs the owner's signed-in accounts. |

## Review

The change adds a cap kind and drawing rules; it changes no credential, destructive-data
or privilege boundary, so CONTRIBUTING requires no independent review. One fresh
whole-branch review ran as the plan's final gate on `a0f0d22..033b6bf`: ready with fixes,
no critical finding, one important one.

- Fixed: a stored percent cap on a window that reports five hours was applied by Core and
  could not be removed in Settings › Caps. Core now owns the rule
  (`EffectiveLimit.TakesPercentCap`), and such a cap is listed as unmatched so Remove works.
  `PercentCapRules` and `PercentCapOnAFiveHourWindowIsNotAppliedAndCanBeRemoved` failed,
  then passed (`d8fb098`).
- Deferred minors: with a cap, the used-only footer says "X % left" without "to cap" and
  can go negative in the not-ready state; a 0 cap draws a tick at 0; the demo cap keeps
  today's end and the five-hour count; a capped percent card without a five-hour count
  drops its "Usual daily share" tooltip line; a rejected percent cap on a count pool is
  worded as a currency mismatch; no projection test drives a ready estimate.

Rulings made during implementation: the percent cap footer keeps the five-hour count after
the cap words, because R-02 calculates it against the cap; the zero-width hatched segment
above a cap was removed as dead code; the UI smoke sets 90 through UI Automation, and the
refusal of 101 is covered by a unit test.

## Execution ledger

Collapsed from tasks.md on 2026-10-09 (OD-19); the full plan is in Git history at 902b196.

- T-054.1 Core: percent cap and five-hour count against the limit: done; commits not recorded; review not recorded per task (whole-branch review at T-054.5); checks PercentCapRules, PercentCapIsTheBudgetLimit and WindowsLeftCountAgainstTheCap red then green, C4 (905/905); grant owner direction, 2026-10-08 (R-202).
- T-054.2 Projection and source: percent cap targets and storage: done; commits not recorded; review not recorded per task; checks two source and projection tests red then green, C5 (294/294); grant owner direction, 2026-10-08.
- T-054.3 Visuals: the cap is the full bar: done; commits not recorded; review not recorded per task; checks CapIsTheFullBarAndATickMarksItOnceExceeded and FiveHourDividersFollowTheCapScale red then green, C5 (295/295); grant owner direction, 2026-10-08.
- T-054.4 Demo and desktop smoke: done; commits not recorded; review not recorded per task; checks WorkBudget UI smoke 2/2, C8 (2/2), C5 (296/296); grant owner direction, 2026-10-08.
- T-054.5 Verification, review and merge: done; commits d8fb098 (review fix), merged with main 65f7fcb; review whole-branch review of a0f0d22..033b6bf ready with fixes, one important finding fixed, minors deferred; checks C4 (905/905), C5 (297/297), C1 (85/85), C3, C6, Debug app build, WorkBudget UI smoke 2/2, C8 (2/2), C9 NOT_RUN locally (CI), live weekly windows NOT_RUN post-deploy (R-190); grant owner direction, 2026-10-08.
