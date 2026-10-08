---
id: AIU-054
schema_version: 1
---

# AIU-054 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Weekly and monthly percent windows take an optional percent cap that the budget uses as the limit, and every applied cap is the full width of its period bar.

**Architecture:**
- Core: `EffectiveLimit.Resolve` accepts a `percent` cap on a percent window; the engine needs
  no other change (it already budgets against the effective limit and keeps "used up" on the
  provider). `SessionEstimator.Figures` takes the weekly limit instead of assuming 100.
- Windows projection/source: percent windows of at least one day get a cap target;
  `SetCapAsync` stores a whole-percent `CountQuantity(value, "percent")`.
- Presentation: `CardVisuals.BarMax` returns the applied cap, so the existing geometry draws
  the cap as the full bar; five-hour dividers move onto the bar's scale.

**Tech Stack:** C#/.NET 10, WinUI 3, CommunityToolkit.Mvvm, xUnit v3 (test projects run with `dotnet run`), FlaUI desktop smoke.

**Spec:** [spec.md](spec.md). Read it before any task.

## Global Constraints

- A percent cap is `CountQuantity(value, "percent")`, whole, `0 <= value <= 100`.
- Cap targets for percent windows: `facts.IsMonthly || facts.Duration >= TimeSpan.FromDays(1)`; never a five-hour window.
- Figures stay in provider percent: footer `63 % of 90 % cap` / `27 % left to cap`; tooltip
  `Custom cap 90 % · binds` and `Provider limit 100 % · 37 % left` (existing `FooterOf` wording).
- Only an applied cap (`CapStatus.Applied`) sets the bar's width; `AboveLimit`, `Inactive`,
  `CurrencyMismatch` and `Unmatched` leave it on the provider limit.
- No budget-store format change, no new dependency, no new logging event.
- Commit after each task as `feat(AIU-054): …` / `test(AIU-054): …` / `docs(AIU-054): …`, without attribution lines.

## Review Focus

1. **Cap above use but below today's end** (t > cap): today's allowance never runs past the cap,
   because the engine budgets against the cap. Pinned by T-01 `PercentCapIsTheBudgetLimit`.
2. **Use past the cap**: the bar scales to use and the cap tick appears at `cap/used`. Pinned by
   T-03 `CapIsTheFullBarAndATickMarksItOnceExceeded`.
3. **Five-hour dividers with a cap**: none may be drawn beyond the bar's end. Pinned by T-03
   `FiveHourDividersFollowTheCapScale`.
4. **A cap stored for a window that later turns into a five-hour-only window** or a `percent`
   cap on a count pool: rejected, not applied. Pinned by T-01 `PercentCapRules`.
5. **Settings › Caps listing a percent cap** shows `%`, not `percent` units. Pinned by T-02
   `PercentCapIsStoredWholeAndListedInPercent`.

---

### T-01 - Core: percent cap and five-hour count against the limit
- status: done
- depends_on: []
- acceptance: [AC-01, AC-02]
- evidence: tests/windows/AiUsage.Infrastructure.Tests/LimitModelTests.cs; PercentCapRules, PercentCapIsTheBudgetLimit and WindowsLeftCountAgainstTheCap red then green; Infrastructure suite 905/905

**Files:** `src/windows/AiUsage.Core/Budget/EffectiveLimit.cs`, `src/windows/AiUsage.Core/Budget/SessionEstimator.cs`;
tests `tests/windows/AiUsage.Infrastructure.Tests/LimitModelTests.cs`, `BudgetEngineTests.cs`, `SessionEstimateTests.cs`.

**Produces:** `SessionEstimator.Figures(SessionEstimate estimate, decimal weeklyUsed, decimal? todayShare, decimal weeklyLimit = 100)`.

- [ ] Replace the percent-window assertions in `CapSelectionPreservesZeroNullUnlimitedAndProviderTie` and add
  `PercentCapRules`: a 7d percent window with cap `90 percent` → `Value == CountQuantity(90,"percent")`,
  `Binding == PersonalCap`, `CapRejected == false`; cap `100 percent` → `Value` 100, `Binding == Provider`;
  cap `CountQuantity(90,"requests")` → `CapRejected`, value 100; cap 101 percent → `CapRejected`.
- [ ] Add `PercentCapIsTheBudgetLimit` (BudgetEngineTests): 7d window, cap 90, used 63 → `Limit == 90`,
  `Binding == PersonalCap`, `TodayShare` computed from 90; used 90 → `Remaining == 0`, `ProviderUsedUp == false`;
  used 95 → `State == AccountLimitState.Over`; used 100 → `ProviderUsedUp`.
- [ ] Add `WindowsLeftCountAgainstTheCap` (SessionEstimateTests): ready estimate cost 10, used 60 → whole sessions 4
  with no limit, 3 with `weeklyLimit: 90`.
- [ ] Run Infrastructure tests; expect the new tests to fail.
- [ ] Implement: in `Resolve`, a percent window keeps provider 100; a cap is valid when it is a `CountQuantity`
  of unit `percent` with `0 <= value <= 100`; below 100 it binds. In `Figures`, `left = weeklyLimit - weeklyUsed`
  and reject `weeklyLimit` outside `(0, 100]`.
- [ ] Run Infrastructure tests; all pass. Commit.

### T-02 - Projection and source: percent cap targets and storage
- status: done
- depends_on: [T-01]
- acceptance: [AC-03]
- evidence: tests/windows/AiUsage.Presentation.Tests/LiveLedgerSourceTests.cs; PercentCapIsStoredWholeAndListedInPercent and WeeklyPercentWindowTakesACapAndBudgetsAgainstIt red then green; Presentation suite 294/294

**Files:** `src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerProjection.cs`, `LiveLedgerSource.cs`;
tests `tests/windows/AiUsage.Presentation.Tests/LiveLedgerProjectionTests.cs`, `LiveLedgerSourceTests.cs`.

- [ ] Add `WeeklyPercentWindowTakesACapAndBudgetsAgainstIt` (projection): `Weekly(63)` has a `CapTargetId`;
  with cap 90 its `Cap == CapModel(90, true, Applied)`, `Figures.EffectiveLimit == 90`; a 5h window
  (`Duration = 5h`) has `CapTargetId == null`.
- [ ] Add `PercentCapIsStoredWholeAndListedInPercent` (source): `SetCapAsync(target, 90)` → Done and the saved
  configuration holds `CountQuantity(90, "percent")`; 101 and 90.5 → Rejected; the `CapSettingModel` scale kind is
  `ScaleKind.Percent`.
- [ ] Run Presentation tests; expect failures.
- [ ] Implement: target condition per Global Constraints; pass `result.Limit ?? 100` as `weeklyLimit` to
  `SessionEstimator.Figures` (only when the card's binding is the personal cap); in `SetCapAsync` drop the
  percent-window rejection and store `CountQuantity(value, "percent")` for `ScaleKind.Percent` when whole;
  `CapSettingModel` uses `ScaleModel.Percent` for a `percent` count.
- [ ] Run Presentation tests; all pass. Commit.

### T-03 - Visuals: the cap is the full bar
- status: done
- depends_on: []
- acceptance: [AC-04]
- evidence: tests/windows/AiUsage.Presentation.Tests/LedgerTests.cs; CapIsTheFullBarAndATickMarksItOnceExceeded and FiveHourDividersFollowTheCapScale red then green; Presentation suite 295/295

**Files:** `src/windows/AiUsage.Windows/Features/Ledger/CardVisuals.cs`; tests `tests/windows/AiUsage.Presentation.Tests/LedgerTests.cs`.

- [ ] Rename `ExtraUsageIsDrawnOnTheProviderScaleWithTheCapTick` to `CapIsTheFullBarAndATickMarksItOnceExceeded`:
  brief `claude-extra` (cap 300, provider 500, used 218) → `CapTick == null`, no segment with paint `CardTop`,
  footer unchanged; the same card with `Used = 320` → `CapTick == 93.75` (±0.001).
- [ ] Add `FiveHourDividersFollowTheCapScale`: brief `claude-week` with cap 90 applied (`EffectiveLimit` 90) →
  every divider `< 100` and equals `k / 90 * 100` for the unscaled position `k`; footer `47 % of 90 % cap`; bar tip
  second line starts `47 % of 90 % used`.
- [ ] Update `r6` in `FiveHourAndNoteCasesMatchTheReference` (or its owner) if its cap tick no longer applies.
- [ ] Run Presentation tests; expect failures.
- [ ] Implement: `BarMax` returns `AppliedCap(card)` first; dividers loop in display units while `k < displayMax - 0.5`
  and add `Pc(k)`; the bar tip's over-cap line stays, the "Hatched: above custom cap" line goes.
- [ ] Run Presentation tests; all pass. Commit.

### T-04 - Demo and desktop smoke
- status: done
- depends_on: [T-03]
- acceptance: [AC-05]
- evidence: tests/windows/AiUsage.Windows.Tests/LedgerActivationSmoke.cs; WorkBudget UI smoke 2/2 and LedgerLaunch smoke 2/2; Presentation suite 296/296

**Files:** `src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerScenarios.cs`, `DemoLedgerSource.cs`;
`tests/windows/AiUsage.Windows.Tests/LedgerActivationSmoke.cs`.

- [ ] Demo `Week`/`FiveHour` cards carry `CapTargetId = id`; `WithCap` for a percent card sets the cap and
  `EffectiveLimit` and keeps the layout.
- [ ] Smoke: replace `Assert.Null(Named("Limit settings, Codex subscription 7 day"))` with opening it, typing 101
  (refused), saving 90 and waiting for `of 90 % cap`; capture `percent-cap.png`.
- [ ] Run Presentation tests, the Release build and the smoke. Commit.

### T-05 - Verification, review and merge
- status: done
- depends_on: [T-01, T-02, T-03, T-04]
- acceptance: [AC-06]
- evidence: docs/specs/AIU-054-percent-cap-and-cap-bar/verification.md

- [ ] Record results in `verification.md`; one fresh whole-branch review; fix findings.
- [ ] Merge fresh `main`, assign numbers (D-196), run validation `--final`, merge into `main`, push.
