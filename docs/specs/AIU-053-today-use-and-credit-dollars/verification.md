# AIU-053 verification

Evidence for [the specification](spec.md). Host: the owner's Windows 11 Pro 10.0.26200
desktop, .NET 10, branch `users/manual-spend-currency-conversion-2ee5a2` from `main` at
`dc7e8fd`. Times are local, 2026-10-08.

## Source checks before the change

- The app had no credit-to-dollar conversion: Copilot `premium_interactions` was shown as
  "Premium requests" in a count scale labelled "requests" (`CopilotQuotaParser`,
  `LiveLedgerProjection.Scale`).
- The installed app's sanitized capture of 2026-10-08 10:43 UTC shows `copilot_plan:
  business` with `premium_interactions` entitlement 17,500 and remaining 17,500; at GitHub's
  published $0.01 per AI credit that is $175.00.
- Today's use is the period use minus the day start from `ReadingCalculations.DayStart`;
  when the first reading of the day comes after midnight the day starts there
  (`DayStartOrigin.Since`), which loses earlier use.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 290/290 at `ea77e0b` (265 before the change) |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 902/902. The first run failed to compile because `TodayEntry` lived in a file that suite does not link; it moved next to the projection (`daf6b31`). |
| App build (README) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors |
| Feature UI smoke | `AiUsage.Windows.Tests -method "*WorkBudget*"` against the Debug unpackaged build, demo mode, isolated temporary state | PASS, 2/2 at `7d4e468` and again at `daf6b31` (the new popover smoke and the existing work-budget smoke). On `daf6b31` the existing smoke first failed to take keyboard focus while the owner was using the desktop; its rerun passed. |
| Ledger launch smoke | `AiUsage.Windows.Tests -method "*LedgerLaunchSettingsHistoryAndExit*"` against the same build | PASS, 2/2 (demo and live-empty) at `daf6b31`, after the owner unlocked the desktop; a first attempt was BLOCKED by the locked desktop |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, 85/85 |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, valid with no diagnostics |
| Diff check | `git diff --check` | PASS |
| MSIX package build | CI on push | NOT_RUN locally; CI builds the unsigned package |
| Merged tree | The same suites, validator tests, `--final` document validation, app build and diff check after merging `main` at `d8b5efa` (D-198) | PASS: Presentation 292/292, Infrastructure 902/902, validator tests 85/85, `--final` valid, 0 warnings |

## Acceptance

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 Dollar view | PASS | `LiveLedgerProjectionTests.GhPoolIsShownInCreditsAndCarriesUnitsAndToday` and `DollarsFollowTheRateAndCapsStayInCredits`: "credits" natively; $32.40 of $175.00 at 0.01; a 10,000-credit cap is $100.00, and $400.00 at 0.04. |
| AC-02 Caps in credits | PASS | `LiveLedgerSourceTests.DollarCapIsStoredInWholeCreditsWithTheProviderUnit` (a $50.00 cap is stored as 5,000 "requests", $175.01 is rejected, Settings › Caps shows $50.00 and 5,000 credits after switching back), `UnitsSwitchCardsCapsAndHistoryAndPersist` (history in dollars, the choice survives a restart), `DollarAmountsRoundToCreditsWithinTheLimits` ($1.01 at 0.04 is 25 credits, shown as $1.00) and `LimitSettingsTests.UndoRestoresARemovedCapInTheUnitShownNow`. |
| AC-03 Today's use | PASS | `LiveLedgerProjectionTests.StoredTodayValueReplacesTheDayStart` (900, then 960 after a reading of 3,300), `TodayValueIsIgnoredAfterAPeriodRestartOrOnTheNextDay`, `HistoryTodayBarUsesTheStoredDayStart`; `LiveLedgerSourceTests.TodayValueValidationRejectsInvalidAmounts` (−1 and 3,241 rejected, empty resets to 120, $1.005 rejected for Claude spending, a percent window rejected). |
| AC-04 Preference file | PASS | `LedgerPreferenceTests.NewFieldsPersistAndOldFilesLoad` and seven new `InvalidOrNewerFileIsNeverOverwritten` cases; the existing extension-data test keeps unknown fields for older builds. Entries older than 35 days are dropped when a new one is saved (`TodayValueValidationRejectsInvalidAmounts`). |
| AC-05 Popover | PASS | `LimitSettingsTests` (six view-model tests and `DemoPopoverSwitchesUnitsAndSetsToday`); the UI smoke opened the Copilot Business popover through UI Automation, switched to USD ("$32.40 of $175.00 used"), saved $9.00 for today and reopened it with "set by you"; the percent Codex card has no settings button. Screenshots are in the git-ignored `.ai-usage-local/AIU-053/` folder of the worktree. |
| AC-06 Checks | PASS | Commands above. |
| Live Copilot Business and Claude spending in the installed app | NOT_RUN (post-deploy owner check, D-190) | Needs the owner's signed-in accounts. |

## Review

The change adds window-preference fields and display conversion; it changes no credential,
destructive-data or privilege boundary, so CONTRIBUTING requires no independent review.
One fresh whole-branch review ran anyway, as the execution plan's final gate, on
`dc7e8fd..daf6b31`: ready to merge, no critical or important findings, six minor ones.

- Fixed: undoing a cap removal after switching units or changing the rate restored the
  amount in the wrong unit (a $100 cap came back as 100 credits).
  `LimitSettingsTests.UndoRestoresARemovedCapInTheUnitShownNow` failed (100 instead of
  10,000), then passed after the undo converts the amount to the unit shown at undo time.
- Deferred: re-saving an unchanged dollar cap at a rate whose credit product has more than
  two decimals can lower it by one credit; the "credits" label is applied before the
  unknown-unit cap guard (theoretical, the parser always reports "requests"); the dollar
  footer tooltip repeats the currency ("$175.00 USD"); small code nits; and test gaps for a
  replayed UTC+1 reset, oversized preference lists and the smoke's first Save button.
