---
id: AIU-046
schema_version: 1
---

# AIU-046 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show the current five-hour window at once and a window count as soon as integer readings bound it, narrowing automatically.

**Architecture:**
- Core `SessionEstimator` is rewritten as an interval estimator: every reading pair inside a five-hour part gives guaranteed bounds, intersected across parts newest first.
- The Windows projection maps the level, the bounds and the count range into `FiveHourModel`.
- `CardVisuals` draws the one-window cell and the rough/settled footers.

**Tech Stack:** C#/.NET 10, xUnit (run as `dotnet run` test executables), WinUI.

**Spec:** [spec.md](spec.md) and [design.md](design.md). Read both before any task.

## Global Constraints

- Levels: none when `L = 0`, `H` is unbounded or no part; rough when `H <= 2 L`;
  settled when `H <= 1.25 L`. Bounds wider than rough display as none.
- Point estimate `C = √(L H)`.
- Pair `i < k` needs `S >= 2`. Readings with `s` or `w` at 100 are excluded. Parts
  whose last reading is older than 28 days are excluded. Readings before the last
  plan change are excluded.
- At most four refinement passes.
- Copy, exactly:
  - one-window tooltip: `Current 5h window · until HH:MM` (or `Next 5h window · starts on first use`), `N % used` / `N % left`, `Window count: collecting data`;
  - rough footer: ` · ≈ a–b × 5h left`, with an en dash U+2013;
  - settled footer: ` · ≈ k × 5h left`;
  - footer tooltip: `One 5h window ≈ C % of 7d (L–H %) · rough · from n windows`, with `· rough` only when rough and `1 window` singular.
- Estimates never change quota facts, card states (other than the existing
  `FiveHourFull`) or notifications. Unknown is never shown as 0.
- No new logging, dependencies, persistence or provider requests.
- Commit on the current worktree branch after each task with `feat(AIU-046): …`,
  `test(AIU-046): …` or `docs(AIU-046): …`, without attribution lines. Do not
  merge or push without the owner's yes.

## Review Focus

1. **Weekly and five-hour runs with different boundaries.** A weekly run spans many
   five-hour runs, or the reverse. Readings must come from the union of run
   endpoints, with each value taken through `Cover`. Pinned by T-01
   `ReadingsComeFromBothSeriesEndpoints`.
2. **App closed for an hour inside a window (gap).** Pairs across the gap must stay
   valid, and the bounds must still contain the true value. Pinned by T-01's
   property test, which randomly skips polls.
3. **A provider that starts returning decimal percents.** Quantization finer than 1
   keeps every error below 1, so the bounds stay valid. Pinned by T-01
   `DecimalReadingsKeepBoundsValid` (synthetic 0.25 steps).
4. **Left mode, day off and a full window while no estimate exists.** The one-window
   cell must keep the D-186/D-187 colouring. Pinned by T-03
   `OneWindowCellFollowsLeftModeDayOffAndFull`.
5. **A dense 28-day history.** The cost of the all-pairs pass must stay small.
   Measured in T-01 step 7 with the existing `measure-backend` worst case. No
   timing assertion is added.

---

### T-01 - Core interval estimator
- status: done
- depends_on: []
- acceptance: [AC-02, AC-03, AC-04, AC-05]
- evidence: tests/windows/AiUsage.Infrastructure.Tests/SessionEstimateTests.cs; commit ed89d1b; SessionEstimateTests 11/11, Infrastructure suite 882/882, Presentation suite 189/189; measure-backend sessions/four-pairs median 75.95 ms (19.0 ms per pair); independent task review approved

**Files:**
- Modify: `src/windows/AiUsage.Core/Budget/SessionEstimator.cs` (replace `SessionSample`, `SessionEstimate`, `Estimate`, `Figures`; keep `Percent`, `Cover`, `LastPlanChange`, `SessionPair`, `SessionFigures`)
- Rewrite: `tests/windows/AiUsage.Infrastructure.Tests/SessionEstimateTests.cs` (keep `ClaudeExtraUsageRequiresCurrentFullWindowAndComparableSpendAtFill` unchanged)
- Modify: `tests/windows/AiUsage.Infrastructure.Tests/BudgetScenarioTests.cs:53-55`, `tests/windows/AiUsage.Infrastructure.Tests/PersistedBudgetCalculationTests.cs:98-123`, `tools/AiUsage.ProviderConsole/BackendMeasurements.cs:96-98`

**Interfaces:**
- Produces:
  - `public enum SessionEstimateLevel { None, Rough, Settled }`
  - `public sealed record SessionEstimate(SessionEstimateLevel Level, decimal? Cost, decimal Low, decimal? High, int Windows)`
    - `public bool Ready => Level != SessionEstimateLevel.None;`
    - `public static SessionEstimate Empty { get; } = new(SessionEstimateLevel.None, null, 0, null, 0);`
    - `Cost` is non-null only when ready. `Low` and `High` are always the computed bounds; `High` is null when unbounded. `Windows` is the number of pooled parts.
  - `public sealed record SessionCount(decimal WholeSessions, bool LessThanOne, decimal? UpTo = null)`
  - `SessionEstimator.Estimate(IEnumerable<ReadingRun> readings, SessionPair pair, DateTimeOffset now) -> SessionEstimate`, with the same pair guard as today.
  - `SessionEstimator.Figures(SessionEstimate estimate, decimal weeklyUsed, decimal? todayShare) -> SessionFigures`:
    - not ready → `(null, null)`;
    - settled → `Weekly = Count((100 - w) / Cost)`;
    - rough → `Weekly = Count((100 - w) / High) with { UpTo = floor((100 - w) / Low) }`, but only when that floor is larger;
    - `Today = Count(todayShare / Cost)`;
    - `w = 100` → `Weekly (0, false)`.

- [ ] **Step 1: Write the failing tests.** Add the test helper
  `Part(string shortInstance, DateTimeOffset start, params (decimal S, decimal W)[] readings)`.
  It returns one single-point run per reading for `ShortKey` and `Key`, five minutes
  apart, with weekly instance `"week"`, plan `null` and source `ProviderApi`. Then add
  these tests:
  - `NoEstimateWithFewerThanTwoWeeklyTicks`: s = 0…17 in steps of 1, with w = 2 for
    s ≤ 9 and 3 after (one tick) → `Level == None` and `Low == 0`.
  - `TwoTickPartMatchesHandComputedBounds`: readings (10,2),(12,2),(14,3),(16,3),(18,3),(20,3),(22,3),(24,4).
    - Expected `Low == 100/13` (7.6923…) and `High == 100/7` (14.2857…), to 3 decimals.
    - Expected `Level == Rough`, `Cost == √(10000/91)` (10.483), `Windows == 1`.
    - Pass 2 tick allowance is `0.42857` at both ticks and changes neither bound.
    - Include these hand steps as comments.
  - `BoundsContainTrueCostUnderBothRoundingModes` (property test, seed 46, 2,000 runs):
    - true `C` uniform in 1–40;
    - 1–11 parts, each starting at a random weekly level 0–50 and five-hour 0;
    - per-poll five-hour step uniform in 0–2×(0.3–6);
    - polls dropped with probability 0.2;
    - integer readings by floor or by round (random per run).

    Assert `Low <= C` and, when `High` is not null, `C <= High`.
  - `SingleWindowReachesRoughByTwentyFiveWhenCostIsTen`: deterministic `C = 10`,
    weekly start 2.3, two five-hour points per poll, flooring, readings up to s = 25
    → `Level != None`.
  - `ConsistentPartsNeverWiden`: add ten simulated `C = 10` parts one at a time (seed 2).
    `Low` never decreases, `High` never increases, and the last state is `Settled`.
  - `ConflictingOlderPartAndOlderAreExcluded`:
    - newest part simulated with `C = 10`;
    - an older part with weekly +6 over five-hour +20, which conflicts;
    - the oldest part again with `C = 10`.

    Expected `Windows == 1`.
  - `PlanChangeWeeklyInstanceSourceAgeAndExhaustionExclusions`:
    - an earlier plan is discarded;
    - a weekly reset inside one short instance makes two parts;
    - a different source is not paired;
    - `now + 29 days` gives `Empty`-equivalent results (`Level None`, `Windows 0`);
    - readings at s = 100 are ignored.
  - `ReadingsComeFromBothSeriesEndpoints`: one weekly run covering five short runs gives
    the same bounds as single-point runs at the same times.
  - `DecimalReadingsKeepBoundsValid`: synthetic values in 0.25 steps with `C = 10`, so
    `Low <= 10 <= High`.
  - `FiguresUseRangeWhenRoughAndPointWhenSettled`:
    - rough `(Low 7, High 13, Cost 9.54)` at `w = 50` → `WholeSessions 3, UpTo 7`;
    - settled `(Cost 12)` at `w = 47` → `4` with `UpTo null`;
    - `w = 100` → `0`;
    - not ready → `null`.
- [ ] **Step 2: Run them and verify they fail.** Run
  `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo -method "*SessionEstimateTests*"`.
  Expected: compile errors for `SessionEstimateLevel`/`Low`/`High`. Restore once with
  `dotnet restore tests/windows/AiUsage.Infrastructure.Tests` if assets are missing.
- [ ] **Step 3: Implement `Estimate` in `SessionEstimator.cs`** following design.md.
  The algorithm is not fixed by the signature, so here is its shape:

```csharp
// 1. shortRuns/weeklyRuns = Ordered(...) after the plan cutoff (as today).
// 2. times = union of FirstSeen and LastConfirmed of both, ascending; for each t:
//    s = Cover(shortRuns, t), w = Cover(weeklyRuns, t); skip if either is null, Percent is null or >= 100,
//    or Source/PlanType differ. Group by (s.PeriodInstance, w.PeriodInstance, Source, PlanType) into parts,
//    collapse consecutive identical (s, w), drop parts whose last reading is older than now - 28 days,
//    order parts by last reading descending. Use double for values.
// 3. H = +inf; parts = all;
//    repeat up to 4 times:
//      per part: u[j] = j > 0 && w[j] > w[j-1] && finite H ? min(1, H * (s[j] - s[j-1] + 1) / 100) : 1
//                l = max over i<k, S>=2 of 100*max(0, W - u[i])/(S+1); h = min of 100*(W + u[k])/(S-1)
//      if newest part has l > h: return Empty
//      pool newest first: L = 0, Hp = inf; at first part with l > Hp or h < L truncate parts there; else L = max, Hp = min
//      if Hp == H: break; H = Hp
// 4. level from L and H; Cost = sqrt(L*H) when level != None; return decimals, Windows = parts.Count.
```

  Implement `Figures` as in Interfaces. Delete `SessionSample` and the
  median/MAD helpers.
- [ ] **Step 4: Update the old call sites.**
  - `BudgetScenarioTests`: `new SessionEstimate(SessionEstimateLevel.Settled, 12, 11, 13, 4)` keeps the asserted `(4, 1)`; the not-ready case uses `SessionEstimate.Empty`.
  - `PersistedBudgetCalculationTests`: three parts of short 10→60 and weekly +6.
    - Expect `Level == Rough`, `Low` 9.804 (500/51), `High` 14.286 (700/49) and `Windows == 3`.
    - Figures at 47: `WholeSessions 3, UpTo 5`; today at 18.3: `1`.
    - After the plan change: `Level == None`.
  - `BackendMeasurements`: `count += estimate.Windows;`.
- [ ] **Step 5: Run the Infrastructure suite and verify it passes.** Run
  `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`.
  Expected: all pass.
- [ ] **Step 6: Commit** with `feat(AIU-046): interval five-hour session estimator`.
- [ ] **Step 7: Measure cost.** Run
  `dotnet run --project tools/AiUsage.ProviderConsole -c Release -- measure-backend`.
  Record the `sessions/four-pairs` median in verification.md. If it exceeds 50 ms per
  pair, stop and report to the owner before optimizing.

### T-02 - Contract and projection mapping
- status: in-progress
- depends_on: [T-01]
- acceptance: [AC-06]
- evidence: not-run

**Files:**
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Contract/LedgerContract.cs:163-169`
- Modify: `src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerProjection.cs:66-81`
- Test: `tests/windows/AiUsage.Presentation.Tests/LiveLedgerProjectionTests.cs`

**Interfaces:**
- Consumes: T-01 `SessionEstimate`, `SessionCount.UpTo`.
- Produces: `FiveHourModel` gains init-only properties. Existing positional uses stay
  valid.
  - `decimal? WindowShareLow`, `decimal? WindowShareHigh`: `BudgetDisplay.Down(x, .1m)`, set only when ready.
  - `int? WindowsLeftMax`: from `UpTo`.
  - `bool Rough`.
  - `int Windows`.

  `WindowShare` and `WindowsLeftInPeriod` keep their names and meanings.
  `WindowsLeftInPeriod` is the range minimum when rough.

- [ ] **Step 1: Write the failing test `PairedCardCarriesRoughBoundsAndRange`.**
  - Build weekly facts at 40 % and short facts (`CL-S`, 5 h, 30 %, reset `Now + 2 h`).
  - Runs: three daily parts within the last week, short 10→60, weekly 20+6i→26+6i, short instances `"s0".."s2"`, weekly instance `"one"`.
  - Assert `FiveHour.Rough`, `WindowShareLow == 9.8m`, `WindowShareHigh == 14.2m`, `WindowShare == 11.8m`, `WindowsLeftInPeriod == 4`, `WindowsLeftMax == 6` and `Windows == 3`.
  - With the runs removed, assert `WindowShare == null`, `Rough == false`, `WindowsLeftMax == null` and layout `FiveHourAndPeriod`.
- [ ] **Step 2: Run and verify it fails.** Run
  `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo -method "*PairedCardCarriesRoughBoundsAndRange*"`.
  Expected: compile error on `WindowShareLow`.
- [ ] **Step 3: Implement.** Add the properties to `FiveHourModel` and set them in the
  `FiveHour = new(...) { … }` initializer in `LiveLedgerProjection`.
- [ ] **Step 4: Run the Presentation suite and verify it passes.** Same command
  without `-method`. Expected: all pass.
- [ ] **Step 5: Commit** with `feat(AIU-046): carry estimate bounds and range to the card`.

### T-03 - One-window cell, rough and settled footers
- status: pending
- depends_on: [T-02]
- acceptance: [AC-01, AC-06]
- evidence: not-run

**Files:**
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/CardVisuals.cs`:
  - `Part` enum at line 68;
  - `PaintOf`, `Rank`;
  - `Build` at lines 126-130;
  - `FooterOf` at lines 632-646;
  - `TodaySummary` at lines 678-684.
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerScenarios.cs`:
  - `FiveHour` helper at lines 107-112;
  - a new gallery case `H3` after `H2`.
- Test: `tests/windows/AiUsage.Presentation.Tests/LedgerTests.cs`:
  - update `FiveHourWithoutAnEstimateIsOneStrip` at lines 377-384;
  - change the gallery count at line 279 from 53 to 54;
  - add the new tests below.

**Interfaces:**
- Consumes: T-02 `FiveHourModel` properties.
- Produces:
  - `Part.Track`, painted `new Paint(Paint.Transparent)` in both modes and on a day off, ranked like `Gray`;
  - `private static StripCell OneWindowCell(FiveHourModel five, (string M, string P) tone, bool left, bool off)`;
  - demo `FiveHour(..., decimal? low = null, decimal? high = null, bool rough = false)`.

- [ ] **Step 1: Write the failing tests.**
  - `FiveHourWithoutAnEstimateIsOneStrip` (h2, Codex, 40 %, until 17:10):
    - one cell with `ShowLabel` true;
    - parts `[40, 60]` with paints `[Paint("OkM"), Paint(Paint.Transparent)]`;
    - tip `["Current 5h window · until 17:10", "40 % used", "Window count: collecting data"]`;
    - footer `"33 % used"`;
    - footer tip unchanged.
  - `OneWindowCellFollowsLeftModeDayOffAndFull`:
    - h2 in Left mode → tip line `"60 % left"`, with the solid part weight 60;
    - a5 (not started, `WindowShare` set to null via `with`) → tip `["Next 5h window · starts on first use", "Window count: collecting data"]`;
    - a4 with `WindowShare` null and 100 % used → one full cell, with the existing `5h full` pill;
    - a day-off copy → `Dashed` true and no red paint.
  - `RoughEstimateShowsRangeAndSettledShowsOneNumber`:
    - h3 is a new gallery case: Claude Max, `FiveHour("h3", OnTrack, 44, 50, 82.4m, 30, At(10,14,17,35), At(10,19,9,0), ws: 10, low: 7, high: 13, rough: true)`.
    - Its footer is `"50 % used · ≈ 3–7 × 5h left"`.
    - Its first footer-tip line is `"One 5h window ≈ 10 % of 7d (7–13 %) · rough · from 3 windows"`.
    - The same card with `rough: false`, `high: 10.5m`, `low: 9.5m` → footer `"50 % used · ≈ 5 × 5h left"` and a tip without `· rough`.
    - The today strip of h3 has more than one cell.
  - `TrayShowsTheOneWindowCell`: in a `LedgerTrayViewModel` over a `DemoLedgerSource` loaded with `States`, the strip whose `Tip` contains `"Current 5h window · until 17:10"` also contains `"Window count: collecting data"`.
- [ ] **Step 2: Run and verify they fail.** Run
  `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo -class "AiUsage.Presentation.Tests.LedgerTests"`.
  Expected: assertion failures on h2's tip and h3's footer.
- [ ] **Step 3: Implement.**
  - `Build`: when the layout is `FiveHourAndPeriod`, `FiveHour` is set, `WindowShare` is null and the card is not used up, `cells = [OneWindowCell(...)]`.
  - `OneWindowCell` parts:
    - Used mode: `(Used, cwU), (Track, 100 - cwU)`;
    - Left mode: `(Allow, 100 - cwU), (Track, cwU)`;
    - label `true`, plus the R-01 tip.
  - `FooterOf`: use `≈ n–max` when `WindowsLeftMax > WindowsLeftInPeriod`. The first tip line uses the new copy when `WindowShareLow`/`High` are set, and keeps the old `(estimate)` line otherwise.
  - `TodaySummary`: for the one-window case, return `"Current five-hour window " + Spoken(tip[1]) + "."`.
  - Demo helper:
    - set `WindowShareLow/High`, `Rough` and `Windows = low is null ? 0 : 3`;
    - `WindowsLeftInPeriod = floor((100 - u) / (high ?? ws))`;
    - `WindowsLeftMax = rough ? floor((100 - u) / low) : null`.
- [ ] **Step 4: Run the Presentation suite and verify it passes.** Expected: all pass.
  If any other gallery-layout assertion breaks only from the new H3 row, update it
  and name it in the commit body.
- [ ] **Step 5: Commit** with `feat(AIU-046): one five-hour window cell and estimate range footer`.

### T-04 - Records, full checks and live run
- status: pending
- depends_on: [T-03]
- acceptance: [AC-07]
- evidence: not-run

**Files:**
- Modify:
  - `docs/specs/AIU-034-limit-audit-design-brief/research.md` (section 7.2 readiness and 7.5 display);
  - `docs/specs/AIU-034-limit-audit-design-brief/design-brief.md` (the "estimate not ready" row);
  - `docs/specs/AIU-035-core-limit-budget/spec.md` (the session estimation bullet);
  - `docs/specs/AIU-038-ledger-presentation/spec.md` (the `FiveHourModel` block and "Without an estimate there is one strip").
- Create: `docs/specs/AIU-046-early-session-estimate/verification.md`
- Modify:
  - `docs/specs/AIU-046-early-session-estimate/spec.md` and `design.md` (status `implemented`);
  - this file (task statuses);
  - `docs/backlog.md` (AIU-046 `status: done` and `evidence`).

- [ ] **Step 1: Add one-line notes.** Directly under each superseded passage, add
  `> Superseded in part by AIU-046 (D-188): <one clause naming the replacement>.`, with AIU-046 linked by a correct relative path to `docs/specs/AIU-046-early-session-estimate/spec.md`
  Do not rewrite the historical text.
- [ ] **Step 2: Run all checks.**
  - `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo`
  - `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` (expect `{"valid":true,…}`)
  - both product suites
  - `git diff --check`
- [ ] **Step 3: Live unpackaged run.** Use README "Local Windows run/debug". With the
  owner's live Claude readings, observe:
  - the paired card's today strip as one 5h cell with the current used percent;
  - its tooltip;
  - a footer without a count, unless the live data already reached rough, in which case record the range shown.

  Then load demo `States` and observe H3 (rough) and A1 (settled). Record PASS,
  FAIL or NOT_RUN per observation with a screenshot path. Do not mark interactive
  checks PASS from tests alone.
- [ ] **Step 4: Write verification.md.** Include the commands with their results, the
  `measure-backend` figure from T-01, the live observations and the AC-by-AC status.
  Update the statuses.
- [ ] **Step 5: Commit** with `docs(AIU-046): record verification and supersession notes`.
  Then ask the owner whether to merge to `main` and push.

## Handoff

- 2026-10-06. Owner approved the spec, the design and this plan in conversation, and
  chose subagent-driven execution in a new primary session.
- Branch `users/5-hour-limits-display-1e0e91` in worktree
  `.claude/worktrees/5-hour-limits-display-1e0e91`, based on `main` at `ff1d788`.
  Docs commits `e76caf0` and `6c105d3` are local only, not merged or pushed.
- Done: spec, design, D-188, backlog and G-003 registration. T-01 done in `ed89d1b`
  (independent review approved; Minor findings deferred to the whole-branch review).
- Local state: the test projects and `tools/AiUsage.ProviderConsole` are restored.
- The design's figures come from a throwaway simulation that is not in the repo.
  T-01's tests re-establish them.
- Exact next action: T-02, Step 1. Dispatch one implementer subagent with the T-02
  block, Global Constraints, Review Focus, spec.md and design.md, then a fresh
  reviewer before T-03.
