# AIU-044 verification

## Task-definition review, 2026-10-04

Base: `0e5d13d`. This is a documentation-only preparation step, not implemented behavior.

- PASS (source inspection): `LiveLedgerProjection.Account` skips every `CL-X` before
  card construction and substitutes NoDisplayedLimits when only those readings remain.
- PASS (source inspection): `ClaudeQuotaParser.ParseExtraUsage` prefers `spend` over
  `extra_usage`; `QuotaLimitMapping.FromLegacy` emits the same CL-X family. The parser
  returns null PlanType. These facts do not classify personal versus work subscriptions.
- PASS (record inspection): AIU-034's Claude (T-02) source matrix describes CL-X/CL-D as alternative
  extra-spend representations and leaves Team/Enterprise scope and wire period unknown.
  The imported Provider States reference has Claude Work D1-D6 with a finite monthly
  monetary pool, and A8 with spending since a window filled.
- PASS (source inspection): `ExtraUsageEvidence.Calculate` and the projection retain
  the window-fill baseline; it is not a day-start spending metric. The owner explicitly
  retained that meaning in this conversation.
- Inference, not live evidence: a work allowance supplied through the existing monetary
  mapping would also be hidden. No real work-account response was inspected in this step.
- Proposal: account-owned monetary sections based on available facts, with neutral
  presentation for unresolved commercial purpose/scope. No invented account taxonomy.

Provider evidence classification for this preparation: repository-source inspection only;
source_verified_at: 2026-10-04; live_verified_at: null. No claim of fresh upstream
protocol verification, provider approval or live work-plan coverage is made.

## Checks and implementation limits

- PASS: `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`
  returned `valid: true` with no diagnostics.
- PASS: primary diff/link/consistency review and `git diff --check`. Reviewed that the
  draft preserves the accepted mark baseline, does not guess plan type, and does not
  require a commercial label before allowing an otherwise evidenced budget.
AC-01 through AC-06 implementation verification: NOT_RUN. No product code, live provider,
credential store, user-state file or imported design artifact was changed.

Next action after owner selection: verify the monetary source's scope evidence and
define the minimal account-owned rendering against the draft specification.


## Implementation checkpoint, 2026-10-04

Base: `e97c7a5`. Implementation selected by the owner in the current request.
The presentation contract adjustment and source uncertainty are recorded in
[design.md](design.md). No storage schema or provider transport changed.

- PASS: Infrastructure suite, 611 tests, after stale extra-usage evidence regression.
- PASS: Presentation suite, 122 tests at the first integrated checkpoint; subsequent
  monetary-state, native-cap and parser tests passed in targeted runs.
- PASS: document validation after adding design metadata; no diagnostics.
- PASS: isolated unpackaged Debug build, zero warnings/errors.
- BLOCKED (default output only): an existing user app locks the standard Debug path.
  The separate `.ai-usage-local/AIU-044/app` output avoids that process.
- FAIL (under investigation): first monetary Windows smoke; screenshot cleanup masked
  its original failure. Harness now records its stage and performs best-effort capture.
- NOT_RUN: final package build and independent review at this checkpoint.
- NOT_RUN: live provider scope/work-plan verification. No source credentials accessed.

Checkpoint next action (completed below): diagnose the recorded monetary smoke stage,
then complete Windows verification, required independent review and final evidence.


## Final implementation evidence, 2026-10-04

Code reference: `2b34d6c` (implementation range `e97c7a5..2b34d6c`). Environment:
Windows 11 x64, .NET SDK 10.0.401, ordinary local interactive desktop, isolated
unpackaged Debug state and synthetic demo. Final package build completed at
2026-10-03T23:59:04Z; final desktop checks completed on 2026-10-04 local time.

| Criterion | Verdict and evidence |
| --- | --- |
| AC-01 | PASS: projection and parser regressions show mixed and monetary-only accounts with neutral Spending, independent of names and finite limits. Desktop smoke confirms nesting, one account header/action owner and both money-only/mixed transitions with stable IDs. |
| AC-02 | PASS (synthetic compatible scope): `MonetaryUsageTests` covers finite monthly bar, daily budget, local history, personal cap and assumed calendar period. Current and legacy parser fixtures produce one pool; unknown/shared scope keeps facts and no personal remaining allowance. Live work allowance mapping remains NOT_RUN. |
| AC-03 | PASS: Core and projection regressions retain spending since observed exhaustion across midnight, its start/reset and native money. Missing coverage, corrections, incompatible currency/scope, stale samples/snapshots and missing or changed current spending do not produce a mark. |
| AC-04 | PASS: unknown, disabled, zero, unknown/null limits, finite caps, currency mismatches and two-account separation. Settings retain native cap currency; replacement editors use the current scale without an implicit conversion or invalid removal undo. Rename does not change semantics. |
| AC-05 | PASS: source restart retains account names, series IDs and caps; the full Infrastructure suite retains existing storage/account lifecycle coverage. Diff inspection confirms no persistence format, grants, source credential access, migration, deletion or additional provider request. Desktop sign-out targets only its synthetic account. |
| AC-06 | PASS: full relevant regression suites, document validation, unpackaged and unsigned package builds, main/tray/history/account-action Windows smoke and required review with findings resolved. Live provider scope remains explicitly separate and NOT_RUN. |

### Executed commands

- PASS: `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`
  - 613 passed, zero failed/skipped.
- PASS: `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`
  - 125 passed, zero failed/skipped after review corrections.
- PASS: `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`
  - valid, no diagnostics; repeated after final documentation edits.
- PASS: `git diff --check` and integrated primary acceptance/diff review.
- PASS: `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None -p:OutputPath=C:/Users/danii/projects/ai-usage-app/.ai-usage-local/AIU-044/app/ --no-restore`
  - zero warnings/errors. The pre-existing app in the standard Debug output remained running.
- PASS: `./tools/windows/Build-Package.ps1 -MsixVersion 2026.10.302.0 -OutputDirectory .ai-usage-local/AIU-044/packages -NoRestore`
  - unsigned-validation-only MSIX, SHA-256
  `FCE6CD3888A6D9CDFD59DEBCB142DAC9D3A20A4EF7FA2B6339FC75F11D83AB07`.
  The SDK warned that optional `mspdbcmf.exe` was absent, so no symbols package was
  generated. No owned-code warnings, signing, installation or trust change.
- PASS: Windows test harness Release build, then
  `dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-build --no-restore -- -noLogo -method '*LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions' -method '*LedgerSmoke.LedgerLaunchSettingsHistoryAndExit'`
  - all three cases passed. `AIU_SMOKE_EXE` pointed to the isolated output above;
  `AIU_SMOKE_EVIDENCE_DIRECTORY` pointed to `.ai-usage-local/AIU-044/smoke-final`.
  Evidence includes `money-smoke.json`, `ledger-demo.json`, `ledger-live-empty.json`
  and actual screenshots for monetary history, cap editor and account actions.
  The screenshots were inspected. Main/tray restoration, settings, local history,
  account add/rename/sign-out, monetary nesting and transitions, and clean exit passed.

Earlier smoke failures were resolved: the harness initially searched for a nonexistent
Close history button (the UI uses Escape), its cleanup could mask failures, and the
new test initially failed to retain a process handle for exit-code inspection. The
actual history title omission was reproduced and fixed separately. These failures
are not presented as passing runs.

### Independent review

Fresh read-only GPT-6 Astra (`low`) review via `convergence-review`, frozen range
`e97c7a5..1e82ac7`, initially reported FAIL with two P2 findings:

1. `LiveLedgerSource.BuildAsync` / `CapRow.ActAsync`: a retained cap's display currency
   could also become the editor currency even though save used the current limit scale.
   Fixed in `2b34d6c`. `MismatchedRetainedCapsAreDisplayedNativelyButReplacedInTheCurrentScale`
   failed with EUR instead of USD, then passed with empty replacement fields, current
   currency, preserved native display and no invalid removal undo. Full suite passed.
2. `LedgerWindow.RebuildGrid`: a retained monetary view could be assigned a new parent
   before detaching its old one, including a removed account host. Fixed in `2b34d6c`.
   The actual Windows test failed on the money-only-to-mixed transition with WinUI's
   parent-attachment error, then passed both directions after explicit detachment.

The reviewer performed source/diff review and did not claim test or desktop execution.
Primary integrated review and targeted verification resolved both material findings;
no unresolved findings remain. No redundant second full review was required.

### Provider and operational limits

- Source classification: repository evidence and synthetic fixtures; source_verified_at:
  2026-10-04; live_verified_at: null for monetary scope. No fresh upstream or real work-plan
  mapping is claimed. Current live CL-X remains scope Unknown, displaying native facts
  and local history; compatible Account-scope budgets and marks are exercised by fixtures.
- NOT_RUN: new live authentication/provider scope checks and package installation/update.
  They were not required for this bounded presentation change; no source CLI credentials
  were read, imported or requested.
- Diagnostics review: pure projection/rendering adds no operational failure boundary.
  Existing refresh/capture/store/action diagnostics remain authoritative; no logging added.
- No Narrator, contrast-theme, unusual display/DPI matrix, Sandbox or VM checks were run.

No implementation work remains for AIU-044. Real monetary wire scope remains an explicit
provider-evidence limitation, not an inferred personal/work classification.

## Post-done corrections (2026-10-06)

Recorded 2026-10-06 under AIU045-D6(a). This annotation does not reopen AIU-044, and its backlog status is unchanged. The synthetic UI audit that started from `383644c`, the source of the last published Preview `2026.10.404.0`, found the defects below in AIU-044 scope (AC-02 and AC-04, as mapped in ANL-06 of the AIU-045 [analysis record](../AIU-045-ui-audit-follow-up/analysis-2026-10-05.md)) after this task was done. As of 2026-10-06 the fixes are on `main`, but no published Preview contains them; shipping them is AIU-045 T-03. Defect descriptions and regression evidence are in the AIU-045 [verification record](../AIU-045-ui-audit-follow-up/verification.md) and the [audit report](../../workflow/ui-ux-audit-2026-10-04/report.md).

| FIX | Defect | Fixing commit | Regression evidence added with the fix |
| --- | --- | --- | --- |
| FIX-03 | Money formatting and editing supported only six decimals despite the contract's 0..18 exponents. | `2942e6f` | `AuditFormattingTests.SupportedMoneyPrecisionIsNotTruncated`, `TinyKnownMoneyMustNotDisplayAsZero` and `CapEditingKeepsNativePrecision` |
| FIX-04 | Cap input accepted incompatible currency symbols and malformed separators. | `2942e6f` | `AuditFormattingTests.InvalidOrForeignCapAmountsAreRejected` and `AbstractCreditsCannotAcceptACurrencySymbol` |
| FIX-05 | Double conversion lost money cents and small overflow on large amounts. | `2942e6f` | `AuditFormattingTests.CardTextKeepsNativeMoneyPrecisionWhenGeometryNeedsDoubles` and `SmallOverageOnALargeMoneyAmountKeepsItsOverflowLabel` |
| FIX-06 | Provider overage increased the displayed provider-limit denominator. | `ed092f0` | `AuditFormattingTests.ProviderOverageDoesNotInflateTheDisplayedLimit` |
| FIX-11 | Tab skipped Save in the inline cap editor. | `2e50e69` | Native red/green evidence only, with no deterministic test (see audit report); the final native rerun is part of AIU-045 T-03. |
| FIX-12 | Uncapped or nonbinding today tooltips blamed a personal cap. | `2e50e69` | `LedgerCardTests.TodayTooltipNamesTheBindingLimit` |

The fixing commits are `[skip ci]` WIP save points in `git log 383644c..b84bf7c`. Each row's commit is the one that added its regression test (for FIX-11, its cap-editor Tab-navigation change) and changed the fixed source file; the audit report gives no per-commit mapping. The native smoke passes recorded above predate these changes and do not carry over to current `main` (ANL-07); AIU-045 T-03 reruns them.

AC-03 scope note (2026-10-06, AIU-045 ANL-21 #4): the on-extra-usage mark was verified at fixture and projection level only; live mode cannot reach it by design (`LiveLedgerProjection` adds the mark only for `MonetaryScope.Account` spending, and live readings stay `MonetaryScope.Unknown` because current wire mappings do not establish scope), and after the AIU045-D3(a) revert of the audit scope annotations no native or gallery evidence of that projected mark can exist on the final build.

Never-accepted gap (ANL-13), as of 2026-10-06:

- The installed-app repeat-launch and storage-lease fault (a second process reporting LeaseUnavailable after close-to-tray; see the [UX corrections record](ux-corrections-verification.md)) was fixed with unpackaged evidence only; host package installation and packaged activation were NOT_RUN. Status: pending the owner's installed-build check (AIU-045 T-03).
