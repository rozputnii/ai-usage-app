---
id: AIU-NEW
schema_version: 1
---

# AIU-NEW Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A settings popover on each money or credit limit sets today's use by hand and shows the Copilot premium pool in credits or US dollars, with caps kept in credits.

**Architecture:**
- The live projection stays native. It names the GH-P unit "credits", marks GH-P cards as
  convertible (`UnitModel`) and money/credit cards with a daily budget as editable today
  (`TodayUseModel`), and replaces today's day start with a stored `TodayEntry`.
- `CreditDollars` (presentation, pure) converts a native card or history to dollars; both
  sources apply it last, using the per-card choice from the preference file.
- Source commands convert entered dollars back to credits; the popover is a WinUI `Flyout`
  on a new header button that reuses the generalized inline amount editor.

**Tech Stack:** C#/.NET 10, WinUI 3, CommunityToolkit.Mvvm, xUnit v3 (test projects run with `dotnet run`).

**Spec:** [spec.md](spec.md). Read it before any task.

## Global Constraints

- GH-P means `LimitKey { Provider: "copilot", Family: "GH-P" }`. Its native display unit is
  `"credits"`; stored readings and caps keep `facts.Unit` (`"requests"`).
- Default rate `0.01m`; valid rate: `> 0`, `<= 1000`, `decimal.Round(rate, 6) == rate`.
- Dollars = `BudgetDisplay.Down(credits * rate, 0.01m)`, scale `ScaleModel.Money("USD", 2)`.
- Cap from dollars: `decimal.Floor(dollars / rate)` credits. Today from dollars:
  `decimal.Round(dollars / rate, 0, MidpointRounding.AwayFromZero)`, limited to the period use.
- A today entry applies only when its card id, local date and period instance all match.
  Entries older than 35 days (`Date < today.AddDays(-34)`) are dropped when one is saved.
- Percent windows get neither `UnitModel` nor `TodayUseModel`, and their commands are rejected.
- Copy, exactly: icon tooltip `Limit settings`; units label `show as`, buttons `credits` and
  `USD`, rate label `1 credit = $`; rate error
  `Enter a rate above 0, at most 1000, with up to 6 decimals`; today editor label `today`,
  remove button `Reset`, limit note `this period’s use`, empty error `Enter today’s use`,
  failure `Today’s use was not saved`; tooltip lines `Set by you` and `Tracked since HH:mm`.
- Preference lists are bounded at 4096 entries and keys at 8192 characters, as `Hidden` is.
- No new dependencies, logging events, provider requests or budget-store format changes.
- Commit on the worktree branch after each task as `feat(AIU-NEW): …`, `test(AIU-NEW): …`
  or `docs(AIU-NEW): …`, without attribution lines.

## Review Focus

1. **The rate changes while a cap exists.** The stored cap must stay in credits and only its
   dollar figure moves. Pinned by T-02 `DollarsFollowTheRateAndCapsStayInCredits` and T-03
   `DollarCapIsStoredInWholeCreditsWithTheProviderUnit`.
2. **Copilot's month resets at 00:00 UTC, after midnight local time, the same day a value
   was saved.** The value must stop applying. Pinned by T-02
   `TodayValueIsIgnoredAfterAPeriodRestartOrOnTheNextDay`.
3. **A dollar amount that is not a multiple of the rate** ($1.01 at 0.04). The cap becomes 25
   credits and shows $1.00; today's credits are limited to the period use. Pinned by T-03
   `DollarAmountsRoundToCreditsWithinTheLimits`.
4. **Switching units while the popover is open.** Both editors must show the new unit and
   limit at once. Pinned by T-04 `SwitchingUnitsRebuildsThePopoverInTheNewUnit`.
5. **Today's dollars for Claude spending with more decimals than the currency allows.** They
   must be rejected, not truncated. Pinned by T-03 `TodayValueValidationRejectsInvalidAmounts`.

---

### T-01 - Preference fields for units and today's values
- status: done
- depends_on: []
- acceptance: [AC-04]
- evidence: tests/windows/AiUsage.Presentation.Tests/LedgerPreferenceTests.cs; NewFieldsPersistAndOldFilesLoad and seven new invalid-file cases red (compile) then green; Presentation suite 273/273

**Files:**
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Contract/LedgerContract.cs` (add `UnitModel`, `TodayUseModel`, two `LimitCardModel` init properties)
- Create: `src/windows/AiUsage.Windows/Features/Ledger/CreditDollars.cs` (only `ValidRate` in this task)
- Modify: `src/windows/AiUsage.Windows/Adapters/Live/LedgerPreferenceStore.cs`
- Test: `tests/windows/AiUsage.Presentation.Tests/LedgerPreferenceTests.cs`

**Interfaces:**
- Produces:
  - `internal sealed record UnitModel(bool Usd, decimal Rate) { public const decimal DefaultRate = 0.01m; }`
  - `internal sealed record TodayUseModel(decimal? Tracked, DateTimeOffset? TrackedSince, bool Manual);`
  - `LimitCardModel.Units` (`UnitModel?`) and `LimitCardModel.TodayUse` (`TodayUseModel?`), init-only, default null.
  - `internal sealed record TodayEntry(string Card, DateOnly Date, string Instance, decimal DayStart);` in `LedgerPreferenceStore.cs`.
  - `State.Units` (`Dictionary<string, UnitModel>`, default empty) and `State.Today` (`TodayEntry[]`, default empty).
  - `CreditDollars.ValidRate(decimal rate): bool`.

- [ ] **Step 1: Write the failing tests**

`NewFieldsPersistAndOldFilesLoad`: load `{"Version":1}`; assert `Units` and `Today` are empty;
change to `Units = { ["card"] = new(true, 0.04m) }`, `Today = [new("card", new(2026,10,8), "one", 3120m)]`;
reopen from the saved JSON and assert both round-trip equal.

Extend `InvalidOrNewerFileIsNeverOverwritten` with these `InlineData` (base object as in the existing Hidden cases):
`"Units":null`, `"Today":null`, `"Units":{"a":{"Usd":true,"Rate":0}}`, `"Units":{"a":{"Usd":true,"Rate":1000.5}}`,
`"Units":{"a":{"Usd":true,"Rate":0.0000001}}`,
`"Today":[{"Card":"a","Date":"2026-10-08","Instance":"one","DayStart":1},{"Card":"a","Date":"2026-10-08","Instance":"two","DayStart":2}]`,
`"Today":[{"Card":"a","Date":"2026-10-08","Instance":"one","DayStart":-1}]`.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release -- -noLogo -method "*LedgerPreferenceTests*"`
Expected: FAIL (compile errors for `Units`/`Today`).

- [ ] **Step 3: Implement the records, state fields and `Valid` rules**

`Valid` adds: `Units` not null, `Count <= 4096`, every key length 1..8192, every value not null with
`CreditDollars.ValidRate(Rate)`; `Today` not null, `Length <= 4096`, every `Card` and `Instance`
length 1..8192, `DayStart >= 0`, `(Card, Date)` distinct.

- [ ] **Step 4: Run the Presentation suite**

Run: `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release -- -noLogo`
Expected: PASS, no regressions.

- [ ] **Step 5: Commit** `feat(AIU-NEW): preference fields for limit units and today's values`

### T-02 - Projection: credits unit, today correction and dollar conversion
- status: pending
- depends_on: [T-01]
- acceptance: [AC-01, AC-03]
- evidence: not-run

**Files:**
- Modify: `src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerProjection.cs`
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/CreditDollars.cs`
- Test: `tests/windows/AiUsage.Presentation.Tests/LiveLedgerProjectionTests.cs`

**Interfaces:**
- Consumes: T-01 types.
- Produces:
  - `LiveLedgerProjection.Card(..., bool stale, IReadOnlyList<TodayEntry>? today = null)`,
    `Account(..., DateOnly? workToday, IReadOnlyList<TodayEntry>? today = null)`,
    `History(LedgerLimit data, DateTimeOffset now, TimeZoneInfo zone, IReadOnlyList<TodayEntry>? today = null)`.
  - `internal sealed record TodayBasis(Quantity Used, string Instance);` and
    `LiveLedgerProjection.Basis(LedgerLimit data, DateTimeOffset now, TimeZoneInfo zone): TodayBasis?` — the
    same used quantity and period instance `Card` budgets with (shared private helper, no duplicate logic).
  - `LiveLedgerProjection.FromAmount(decimal amount, Quantity shape): Quantity?` — count keeps the unit;
    money needs a whole number of minor units, else null.
  - `CreditDollars.Dollars(decimal credits, decimal rate): decimal`, `CapCredits(decimal dollars, decimal rate): decimal`,
    `TodayCredits(decimal dollars, decimal rate): decimal`,
    `Apply(LimitCardModel card, UnitModel? stored): LimitCardModel` (no-op when `card.Units` is null; otherwise
    `Units = stored ?? card.Units`, and with `Usd` converts `Scale`, every `LimitFigures` amount, a `Known`
    provider limit, `Cap.Amount` and `TodayUse.Tracked`),
    `ToDollars(HistoryModel history, decimal rate): HistoryModel` (day amounts and baseline).

- [ ] **Step 1: Write the failing tests**

Use a GH-P fact: limit 17,500 `"requests"`, used 3,240, monthly, provider reset in 20 days; runs
`("one")` 3,120 first seen 10:43 UTC today and 3,240 at now (12:00 UTC).

- `GhPoolIsShownInCreditsAndCarriesUnitsAndToday`: card `Scale.UnitName == "credits"`, `Units == new(false, 0.01m)`,
  `TodayUse == new(120, 10:43 UTC, false)`; a percent card has both null.
- `DollarsFollowTheRateAndCapsStayInCredits`: `CreditDollars.Apply(card with cap 10,000, new(true, 0.01m))` gives
  `Scale == Money("USD", 2)`, `Figures.Used == 32.40m`, `ProviderLimit == Known(175.00m)`, `Cap.Amount == 100.00m`;
  at rate 0.04 the cap is `400.00m`; `Apply(card, new(false, 0.04m))` keeps credits with `Units.Rate == 0.04m`.
- `StoredTodayValueReplacesTheDayStart`: entry `(cardId, today, "one", 2340)` gives `Figures.DayStart == 2340`,
  today's use 900 and `TodayUse.Manual`; a further run of 3,300 gives today's use 960; `TodayUse.Tracked` stays 180.
- `TodayValueIsIgnoredAfterAPeriodRestartOrOnTheNextDay`: the same entry with instance `"old"` or date yesterday
  leaves `DayStart == 3120`.
- `HistoryTodayBarUsesTheStoredDayStart`: `History(..., [entry])` today's `Used == 900`; `CreditDollars.ToDollars(history, 0.01m)` gives `9.00m`.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release -- -noLogo -method "*LiveLedgerProjectionTests*"`
Expected: FAIL (missing members).

- [ ] **Step 3: Implement**

In `Card`: keep the automatic `DayStartValue`; when an entry matches card id, `WorkCalendar.Date(now, zone)` and
instance, use `FromAmount(entry.DayStart, used)`. Set `TodayUse` when `facts.Kind != PercentWindow`,
`result.DayStart` and `used` are known: `Tracked` = rounded `used − automatic day start`,
`TrackedSince` = local `Since` only for `DayStartOrigin.Since`. For GH-P with a count scale, use
`ScaleModel.Count("credits")` and `Units = new(false, UnitModel.DefaultRate)`. `History` applies a matching
entry per date and group instance.

- [ ] **Step 4: Run the Presentation suite** — Expected: PASS.

- [ ] **Step 5: Commit** `feat(AIU-NEW): project credits, today's day start and dollar conversion`

### T-03 - Live source commands, caps and history in the chosen unit
- status: pending
- depends_on: [T-02]
- acceptance: [AC-02, AC-03]
- evidence: not-run

**Files:**
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Contract/LedgerContract.cs` (`ILedgerSource`)
- Modify: `src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerSource.cs`
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerSource.cs` (temporary `Rejected` stubs; T-05 implements)
- Test: `tests/windows/AiUsage.Presentation.Tests/LiveLedgerSourceTests.cs`

**Interfaces:**
- Consumes: T-02.
- Produces on `ILedgerSource` (no default bodies):
  - `Task<CommandOutcome> SetTodayUsedAsync(string cardId, decimal? amount, CancellationToken ct);` — amount in the
    card's shown unit; null resets.
  - `Task<CommandOutcome> SetUnitsAsync(string cardId, bool usd, decimal rate, CancellationToken ct);`

- [ ] **Step 1: Write the failing tests** (use the existing `Accounts`/`Store`/`Clock` fakes; GH-P facts as in T-02, store runs as in T-02)

- `UnitsSwitchCardsCapsAndHistoryAndPersist`: `SetUnitsAsync(card, true, 0.01m)` is Done; the card shows `32.40m` in
  USD; `GetHistoryAsync` days are in dollars; after a restart with the saved preferences the card is still USD;
  `SetUnitsAsync` with rate 0 or on a percent card is Rejected.
- `DollarCapIsStoredInWholeCreditsWithTheProviderUnit`: in USD at 0.01, `SetCapAsync(target, 50.00m)` stores
  `new CountQuantity(5000, "requests")`; `175.01m` is Rejected; Settings › Caps lists the cap as `50.00m` in
  `Money("USD", 2)`; switching back to credits lists `5000`.
- `DollarAmountsRoundToCreditsWithinTheLimits`: at rate 0.04, cap `1.01m` stores 25 credits and shows `1.00m`;
  today `$129.60` (all 3,240 credits) is Done and today's use shows `129.60m`.
- `TodayValueValidationRejectsInvalidAmounts`: in credits, `SetTodayUsedAsync(card, 900)` is Done and today's use
  is 900; `-1` and `3241` are Rejected; `null` resets to 120; a Claude CL-X money card rejects `1.005m`; a
  percent card is Rejected; saving drops an entry dated 35 days ago and keeps one dated 34 days ago.

- [ ] **Step 2: Run to verify they fail** — `-method "*LiveLedgerSourceTests*"`; Expected: FAIL.

- [ ] **Step 3: Implement**

`BuildAsync`: pass `preferences.Current.Today` to `Account`, then map every card through
`CreditDollars.Apply(c, preferences.Current.Units.GetValueOrDefault(c.CardId))`; a matched cap row in
Settings › Caps takes its card's `Scale` and `Cap.Amount`. `SetCapAsync`: in USD convert with `CapCredits`; a
count cap always uses `limit.Facts.Unit`. `SetTodayUsedAsync`: require the shown card's `TodayUse`, get
`Basis`, convert dollars with `TodayCredits` and limit to the used amount, reject a negative or larger value
and a money value that `FromAmount` cannot represent, store `TodayEntry(card, today, basis.Instance, used − amount)`
replacing the same date and dropping old entries. `GetHistoryAsync`: pass `Today`, convert with `ToDollars` in USD.

- [ ] **Step 4: Run the Presentation suite** — Expected: PASS.

- [ ] **Step 5: Commit** `feat(AIU-NEW): source commands for limit units and today's use`

### T-04 - Limit settings popover and today tooltip
- status: pending
- depends_on: [T-03]
- acceptance: [AC-05]
- evidence: not-run

**Files:**
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/LimitCardViewModel.cs` (`CapEditorViewModel` options; card settings state)
- Create: `src/windows/AiUsage.Windows/Features/Ledger/LimitSettingsViewModel.cs`
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/LedgerViewModel.cs`
- Modify: `src/windows/AiUsage.Windows/Controls/Ledger/LedgerViews.cs` (`CapEditorView` label, remove text, `AutoFocus`)
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Views/LedgerCardView.xaml` and `.xaml.cs`
- Modify: `src/windows/AiUsage.Windows/Themes/Ledger/Styles.xaml` (`FlyoutPresenter` style: tip background, window line border, 12 px corners, 12 px padding)
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/CardVisuals.cs` (today tooltip lines)
- Test: `tests/windows/AiUsage.Presentation.Tests/LedgerTests.cs`

**Interfaces:**
- Consumes: T-03 commands.
- Produces:
  - `CapEditorViewModel(..., string label = "cap", string? hint = null, string limitNote = "the provider limit",
    string removeText = "Remove", string emptyError = "Enter a cap", string failure = "The cap was not saved")`
    with `Label` and `RemoveText` properties; existing callers unchanged.
  - `LedgerViewModel.SetTodayUsedAsync(string cardId, decimal? amount): Task<bool>` and
    `SetUnitsAsync(string cardId, bool usd, decimal rate): Task<bool>`.
  - `LimitCardViewModel.HasSettings` (`CanEditCap || Model.TodayUse is not null || Model.Units is not null`),
    `Settings` (`LimitSettingsViewModel?`), `OpenSettings()`, `CloseSettings()`.
  - `LimitSettingsViewModel`: `HasUnits`, `IsUsd`, `RateText`, `RateError`, `AcceptsRate(string)`,
    `ShowUsdCommand`, `ShowCreditsCommand`, `SaveRateCommand`, `Today` and `Cap` (`CapEditorViewModel?`). After a
    successful command the card rebuilds `Settings` from its updated model.

- [ ] **Step 1: Write the failing tests** (`LedgerTests`, demo-free fake source as the existing tests use)

- `SettingsAppearOnlyForLimitsWithASetting`: a percent card has `HasSettings == false`; a GH-P card true.
- `TodayEditorUsesThePeriodUseAsItsLimit`: `Settings.Today.Text == "120"`, `Label == "today"`, saving `3241`
  sets `Error == "Enter at most 3,240 · this period’s use"`, saving `900` calls `SetTodayUsedAsync(card, 900)`.
- `ResetAppearsOnlyForAManualValue`: `Settings.Today.HasCap` follows `TodayUse.Manual`; hint contains
  `set by you` only when manual and `since 10:43` when `TrackedSince` is 10:43.
- `SwitchingUnitsRebuildsThePopoverInTheNewUnit`: `ShowUsdCommand` calls `SetUnitsAsync(card, true, 0.01m)`; after
  the source publishes the dollar card, `Settings.Cap.Scale.Kind == Money` and its hint names `$175.00`.
- `RateInputAcceptsOnlyValidRates`: `AcceptsRate("0.0123")` true, `"1.1234567"` false; saving `"0"` sets the exact
  rate error copy.
- Today tooltip: a card with `TodayUse.Manual` has `Set by you` in its today cell tip; one with
  `TrackedSince` 10:43 and not manual has `Tracked since 10:43`.

- [ ] **Step 2: Run to verify they fail** — `-method "*LedgerTests*"`; Expected: FAIL.

- [ ] **Step 3: Implement the view models, then the view**

The header button (`LedgerQuietIconButton`, Segoe Fluent glyph `&#xE9E9;`, tooltip `Limit settings`) sits before
History and is visible when `HasSettings`. Its `Flyout` (`Placement="BottomEdgeAlignedRight"`) opens
`OpenSettings()` on `Opening` and `CloseSettings()` on `Closed`; content rows: units (segment buttons with
`LedgerSegmentButton`, rate box with `LedgerAmountBox`, error text), then today and cap as `CapEditorView`
with `AutoFocus="False"`. `Cancel` in a popover editor hides the flyout.

- [ ] **Step 4: Run the Presentation suite and build the app**

Run: `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore`
Expected: PASS, 0 warnings.

- [ ] **Step 5: Commit** `feat(AIU-NEW): limit settings popover with units, today and cap`

### T-05 - Demo work-budget scenario and demo commands
- status: pending
- depends_on: [T-04]
- acceptance: [AC-05]
- evidence: not-run

**Files:**
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerScenarios.cs`
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerSource.cs`
- Test: `tests/windows/AiUsage.Presentation.Tests/LedgerTests.cs`

**Interfaces:**
- Consumes: T-02 `CreditDollars`, T-03 commands.

- [ ] **Step 1: Write the failing test** `DemoPopoverSwitchesUnitsAndSetsToday`: the work-budget scenario has a
  "Copilot Business" account whose premium card shows 3,240 of 17,500 credits with `TodayUse(120, 10:43, false)`;
  `SetUnitsAsync` shows `$32.40`; `SetTodayUsedAsync(card, 9.00m)` makes today's use `9.00m`; a cap of `100.00m`
  in USD is kept as 10,000 credits when switching back; the Claude Work spending card has `TodayUse` and no `Units`.

- [ ] **Step 2: Run to verify it fails.**

- [ ] **Step 3: Implement** the demo account and commands: keep native cards, apply `CreditDollars.Apply` on
  publish, convert dollar caps and today values with the T-02 helpers, and shift `DayStart` and `TodayEnd` by the
  same amount for a today value.

- [ ] **Step 4: Run the Presentation suite; run the demo app** (`AiUsage.exe --demo`), open the work-budget
  scenario and drive the popover through UI Automation; save screenshots under `.ai-usage-local/AIU-NEW/`.

- [ ] **Step 5: Commit** `feat(AIU-NEW): demo Copilot Business credits and popover commands`

### T-06 - Records, full checks and merge
- status: pending
- depends_on: [T-05]
- acceptance: [AC-06]
- evidence: not-run

- [ ] **Step 1:** Write `verification.md` (commands, acceptance table, NOT_RUN post-deploy owner checks) and the
  backlog completion note; set the spec status to `implemented`.
- [ ] **Step 2:** Run Infrastructure and Presentation suites, validator tests, document validation, the app build
  and `git diff --check`; record results.
- [ ] **Step 3:** Fetch and merge `origin/main`, replace `AIU-NEW`/`D-NEW` with the next free numbers, run
  validation with `--final`, commit, merge into `main`, push and verify with `git fetch`.
