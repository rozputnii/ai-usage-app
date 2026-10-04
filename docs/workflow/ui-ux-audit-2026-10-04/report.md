# Synthetic Windows UI/UX audit — in progress

**The requested comprehensive audit is not complete.** The latest full deterministic suites pass 1045/1045 (859 Infrastructure + 186 Presentation). All 235 parser/recorder/budget/projection corpus scenarios and six composite dashboards have passed native Used/Left checks in isolated, network-disabled Windows Sandbox runs. All four first-run sign-in buttons and duplicate-provider menu entries, all four visible/hidden and idle/in-flight tray variants, three calendar transitions and five production recovery/deletion/activation checks have passed physical input and observable outcomes. Final-build recapture, complete rendered inspection and remaining cap/history/hover/resize/keyboard variants are pending. The host stays locked; no host unlocking or real authentication was attempted.

## Deliverable records

- [Supported behavior inventory](inventory.md): provider families, layouts, states, scales, precedence, invalid/unreachable combinations and source references.
- [Coverage matrix](coverage.csv): 235 stable parser-to-recorder-to-budget-to-projection scenarios, independent expected values, controlled clocks and native verdicts.
- [Additional scenario inventory](additional-scenarios.csv): 39 composite/transition/lifetime/history/recovery requirements, supporting tests and remaining exact replay gaps.
- [Control inventory](controls.csv): 69 mouse-action/variant rows, 21 keyboard rows and three non-applicable visible-control requests. Used/Left physically verified; remaining controls pending.
- [Gallery status](gallery.md) and [screenshot index](screenshot-index.csv): every corpus scenario mapped to a planned readable Used/Left page and exact synthetic card ID; captured rows remain separate from rendered inspection.
- [Implementation/handoff plan](plan.md).
- [Independent checkpoint review and finding dispositions](review.md).

## Product defects fixed

| Defect | Change | Regression evidence |
| --- | --- | --- |
| Successful empty provider reading rendered “not ready” | Uses existing NoDisplayedLimits state for a successful empty quota; startup remains distinct | LiveLedgerProjectionTests.SuccessfulEmptyQuotaUsesTheSupportedNoDisplayedLimitsState, all four providers; failed before fix |
| Timestamp resets/readings/failures were formatted as UTC clocks in a local-time UI | Convert presentation timestamps, marks and retry time to selected timezone, retaining their instants and date-only precision | LiveLedgerProjectionTests.TimestampResetAndReadingClocksUseTheSelectedLocalZone; Lisbon summer UTC/local difference; failed before fix |
| Supported money precision stopped at six decimals | Formatting/editing/parsing supports the contract's 0..18 exponents | AuditFormattingTests supported exponents and tiny known values; failed before fix |
| Cap input silently accepted a foreign symbol or malformed separators | Strip only the selected money denomination at the boundary; validate English grouping; reject currency symbols on abstract credits | AuditFormattingTests foreign USD/EUR, misplaced code, invalid comma grouping, credits; failed before fix |
| Money text lost cents through double drawing conversions, including small overflow disappearing | Decimal display arithmetic retained; double conversion restricted to drawing geometry/weights | AuditFormattingTests.CardTextKeepsNativeMoneyPrecisionWhenGeometryNeedsDoubles and SmallOverageOnALargeMoneyAmountKeepsItsOverflowLabel; all three cases failed before fix |
| Provider overage inflated the displayed limit to the used amount | Keep the actual provider limit for text/markers, allowing drawing geometry to contain overage | AuditFormattingTests.ProviderOverageDoesNotInflateTheDisplayedLimit, Used/Left red then green; native page-018 now shows $300.01 of $300.00 used and −$0.01 of $300.00 left |
| Cancel was disabled during a retried sign-in | Shared strip command remains available while the source controls attempt concurrency | LiveLedgerSourceTests.RetryKeepsTheSharedStripCancelCommandAvailableWhileLoginIsPending, red then green; native successful authentication rerun cancels twice and completes via synthetic manual code |
| Workday button name remained stale after toggling | Notify the computed name binding when selection changes | LedgerInteractionTests.WorkDayNameNotifiesBindingsAndTheLastSelectionCannotBeRemoved, red then green |
| Last selected workday silently rejected an enabled click | Disable that toggle until another workday is selected | Same regression independently checks command availability; actual native final rerun pending |
| Expired paired 5h reading remained presented as the current full window | Retain longer-period budget; show a qualified 5h past-reset mark until fresh replacement | LiveLedgerProjectionTests short reset before/at/after regressions failed at/after reset before correction; 12 parser corpus reset/replacement cases PASS; added native rerun in progress |
| Tab skipped Save in the inline cap editor | Use local Tab navigation inside bars while the cap editor is open | Actual preferences-check-3 native failure at Save focus; preferences-check-4/5 passed Save and reverse focus, then failed on a separate driver focus selector; corrected full rerun in progress |
| Uncapped/nonbinding money and count tooltips claimed a personal cap cut today's share | Name the applied binding cap or period limit | TodayTooltipNamesTheBindingLimit: four product failures reproduced; six count/money variants and both modes now PASS. A capitalization-only test expectation error is retained separately |

These are formatting/projection repairs within existing behavior. No provider endpoint, feature, dependency, credential lifecycle, product layout or installed application was changed. Trivial formatting needs no new logging; existing diagnostics remain in use.

## Test and build results

Audit base: `main` at `383644c`. Initial checkout was clean. Deterministic tests and product builds correspond to production/test sources saved at `2942e6feec44292763799b15660a9853f9087306`; subsequent review corrections affect only the native driver and its guards. The corrected driver was compiled and its blocked execution repeated from sources saved at `ae4f483129d99f8b176c306531b793738b1d0054`. Both checkpoints were pushed to main; the remote head was verified. Native executable: `src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe`. EXE SHA-256: `3A25A0E283144D66D6A58847ED3F4985E5DC41813911D4958402E11838D8763B`; application DLL SHA-256: `F8D1FFEBF30C5065BCF637E91C41D47F4F315CD1669FBF509180E3F925434C16`. Environment: Windows console session, .NET SDK 10.0.401, ordinary host display settings unchanged.

| Check | Actual result |
| --- | --- |
| Infrastructure baseline | PASS 613/613 |
| Presentation baseline | PASS 128/128 |
| Final deterministic Infrastructure so far | PASS 783/783, zero skipped; includes 170 corpus cases and serialized observation replay assertions |
| Final deterministic Presentation so far | PASS 169/169, zero skipped; 41 added tests relative to baseline; presentation-workday-fixes.xml |
| Project validator regressions | PASS 80/80 |
| Debug unpackaged app build | PASS, zero warnings/errors |
| Windows test driver compilation | PASS, zero warnings/errors; does not establish interactions |
| Unsigned MSIX validation build | PASS 2026.10.409.0, identity/version validated, no installation/signing; external tooling warns mspdbcmf.exe is unavailable so no symbols package |
| Actual initial Ledger smoke | BLOCKED after launching isolated unpackaged app: could not acquire foreground input; three foreground failures before stopping the suite; no valid app evidence |
| Read-only desktop prerequisite after harness correction | BLOCKED: LockApp/LogonUI input; xUnit reports one failed prerequisite, not a product defect |
| New replay/control/auth native driver | BLOCKED 11/11 attempted tests at the input guard; repeated after review corrections with the same blocker; no application interaction/capture executed |
| Windows Sandbox desktop prerequisite | PASS; independent guest session while host stays locked |
| Actual synthetic overview in Sandbox | PASS one native test: Used/Left clicks, four expected card states, ten captures and clean exit; Used/Left overview images inspected |
| Parser corpus in Sandbox | PASS 170/170 baseline scenarios across overview and 51 pages; page-014 through page-051 looping test PASS in 658.457 seconds, earlier pages retained as individually passing results |
| First-run and duplicate-provider physical controls | PASS one native test covering all eight provider actions, settings open during first run, service requests, preserved original cards and clean exits; first-run-debug.xml, 62.861 seconds |
| Synthetic authentication success | PASS one native test including cancellation, retry, second cancellation, invalid and valid manual code, success and clean exit; auth-debug.xml |
| Synthetic authentication failure reasons | PASS 8/8 native theory cases for Duplicate, WrongAccount, Storage, AccessDenied, Expired, Browser, Registration and Provider; exact explanation, retry/cancel and no incorrect account addition asserted |
| Settings, editors, history and support | PASS one native test in 70.827 seconds after the bounded physical-scroll correction; settings/workday actions, history key navigation, rename, cap validation/save/remove, support receipts, sign-out/reconnect and fake deletion cancellation/confirmation |
| Final screenshots and rendered visual inspection | In progress; overview captured, remaining corpus/control/gallery evidence pending |
| Document validation | PASS, no diagnostics |
| Integrated/fresh independent review | Source review executed against frozen checkpoint; two material driver findings corrected and compiled. Their guards now permit verified owned guest interaction; see review.md |

Local execution logs/results: `.ai-usage-local/ui-audit/infrastructure.xml`, `presentation.xml`, `desktop-prerequisite.xml`, `native.xml`, `native-after-review.xml`; earlier regression failures stay separately in `product-regressions-red.xml`, `native-precision-red.xml`, `overflow-precision-red.xml`. Generated output and all captures remain outside Git. Invalid lock-screen captures are excluded from deliverables and publication.

Harness corrections are distinct from product fixes. Shared-project concurrent compilation collided in compiler output; sequential reruns passed. The first input-desktop-name probe falsely considered Default sufficient while LockApp still owned input; a foreground process check was added and its actual read-only rerun correctly reported BLOCKED. Audit root validation now runs before diagnostics creates directories, requires explicit `--demo`, a synthetic marker and an isolated marked temporary root, and rejects redirection. Replay includes the observations actually submitted by the production recorder, so cached startup cannot lose first-observation baselines. PowerShell page generation retains ISO timestamp strings rather than converting them through the host timezone. Rehydrated corpus inputs were actually rerun against the real store and projection.

## Reproduction and continuation

Use PowerShell 7.5+ from the repository root. All provider data is generated from synthetic JSON in AuditScenarioTests; no credential fields or provider services are present in the explicit replay composition. The native app starts from the parser-normalized exported inputs. Parser execution and real Windows rendering are separate linked stages, not a claim that a mocked transport was exercised inside the native process. Live HTTP/quota/authentication coverage is excluded by the current request.

```powershell
$env:AIU_AUDIT_INPUT_DIRECTORY = Join-Path (Get-Location) '.ai-usage-local/ui-audit/inputs'
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -xml .ai-usage-local/ui-audit/infrastructure.xml
dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -xml .ai-usage-local/ui-audit/presentation.xml
./tools/windows/New-UiAuditPages.ps1 -InputDirectory .ai-usage-local/ui-audit/inputs -OutputDirectory .ai-usage-local/ui-audit/pages -CoverageDirectory docs/workflow/ui-ux-audit-2026-10-04
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore
dotnet build tests/windows/AiUsage.Windows.Tests -c Release --no-restore
$env:AIU_SMOKE_EXE = Join-Path (Get-Location) 'src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe'
$env:AIU_AUDIT_PAGE_DIRECTORY = Join-Path (Get-Location) '.ai-usage-local/ui-audit/pages'
$env:AIU_SMOKE_EVIDENCE_DIRECTORY = Join-Path (Get-Location) '.ai-usage-local/ui-audit/gallery'
dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-build -- -class '*AuditDesktopPrerequisite' -parallel none -xml .ai-usage-local/ui-audit/desktop-prerequisite.xml
# Only after the prerequisite passes on an unlocked desktop:
dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-build -- -class '*AuditWindows' -parallel none -xml .ai-usage-local/ui-audit/native.xml
```

The driver performs physical clicks, checks fake request receipts, and records native assertion results separately from visual inspection. Guest runs exposed harness issues: WinUI UIA ProcessId=0, denied programmatic activation, a replaced UIA provider during capture, and a Note-layout expectation incorrectly requiring retained usage to be visible. Native ownership, an owned-caption activation click, one fresh capture binding retry and contract-specific independent expectations correct these issues. Actual reruns are required and recorded; no compile/source check upgrades a row to PASS. Remaining controls/composite cases must be implemented and exercised as listed, including native tray Open/Refresh, physical caption close, restart persistence, all tooltips, overlay/snapshot transitions, actual isolated recovery/cleanup and in-flight exit.

Sandbox continuation uses Windows Sandbox CLI, guest Windows 11 build 26100, and a self-contained unpackaged Release build. Networking and clipboard/device sharing are disabled. Only the synthetic input staging directory (read-only) and an initially empty evidence directory (writable) are mapped. The host console stays locked; no authentication, trust, display settings or installed-app changes are involved. Source reference `f5adf034ebc70d91259b1d645dc56531d3a8bdf8`; app DLL SHA-256 `17EB1F30BE31A597B9472192F06882B8632628372CC71D4C3D15BE83069DFAE2`. Release build and driver publish commands:

```powershell
dotnet publish src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -r win-x64 -p:Platform=x64 -p:WindowsPackageType=None -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -o .ai-usage-local/ui-audit/sandbox-probe/input/app -v:minimal
dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true -o .ai-usage-local/ui-audit/sandbox-probe/input/smoke --no-restore -v:minimal
```

```powershell
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
git diff --check
./tools/windows/Build-Package.ps1 -MsixVersion 2026.10.409.0 -OutputDirectory .ai-usage-local/ui-audit/packages -NoRestore
```

The recorded package command already produced that version and intentionally refuses overwrite. A changed/repeated package build needs a fresh unused local version. Package SHA-256: `7508F40F7C82CFF7852C2BEADB64071D9F3018FE8CFADFFA7D5EEF620F914F1B`. It is unsigned validation evidence, not release/install evidence. Git save points use `[skip ci]` to avoid the main-push Preview release workflow; no release is authorized by this audit.

Completion requires real native outcomes and inspected readable final captures for every applicable row, plus the remaining exact scenario/control coverage and required review. No screenshot or full-coverage completion is claimed while these gaps remain.

## Current Sandbox checkpoint

The baseline gallery uses app DLL SHA-256 `F932E68956CDD7223BC9D012D1E024F2A02E8FDC4FD2CF9F4FC26885FAAFCFDE`, built from `9e60f773` plus the recorded overage fix. Native captures and reports are in `.ai-usage-local/ui-audit/sandbox-probe/evidence/corpus-gallery`. Inspected overview and pages 001–012 and 018 are readable; remaining images are NOT_RUN for visual inspection. This is interim evidence: the complete gallery must be rerun after the final fixes. Auth retry validation subsequently used app hash `402C9F6D92C46708C64EB47E529DB83869B7C245B5D9AC893EE458765B7FFFEB`. The latest workday-fix app hash is `10D9C462ABB77A11566B27458817C2F212F20F4ADC1665059038AB1135FDD769`; remaining control tests are running against it.

There are 952 passing deterministic product tests (783 Infrastructure + 169 Presentation), 211 more than the baseline. Unsigned package 2026.10.411.0 passed with the authentication/workday fixes, SHA-256 `4A2C1E6180E6DC84C112AFF30E0629AAF89FAD7DF47EBC18A75AC51EA3BA0EF6`; external symbol tooling warning only. Earlier product and harness failure evidence is retained under `sandbox-probe/evidence/failures` and the separate red-test XML files. Settings scrolling and physical text-entry timing exposed driver issues; corrected implementations were actually rerun successfully. A reusable Sandbox runner is implemented but its end-to-end reproduction check is still NOT_RUN.

## Expanded deterministic checkpoint

Current product suites PASS 1026/1026 (Infrastructure 848, Presentation 178), zero skipped or not run. Exact XML: `.ai-usage-local/ui-audit/infrastructure-checkpoint-2.xml` and `presentation-tooltips-green-2.xml`. The 235-case corpus adds 65 cases beyond the native baseline. The new 5h reset, cap Tab navigation and binding-tooltip corrections bring reproduced product defects to twelve. The complete native/gallery verdict remains in progress.

The reusable Sandbox runner's overview reproduction passed at `runner-check-2`; Used overview visually inspected. Each run records source/dirty status and application/driver hashes. Failed evidence-storage cleanup regression passed in the guest after failing before correction. New preference runs preserved empty-input, bootstrap and UIA-focus harness failures in separate directories; no failed run was promoted to PASS. `expanded-native-1` is executing added scenarios and control regressions against the latest compiled application. No host unlock, installed application change or live provider request occurred.

## History, composite and recovery checkpoint

The current deterministic total is PASS 1040/1040: Infrastructure 856 and Presentation 184, with zero skipped/not run. Evidence: `infrastructure-composites-transitions.xml` and `presentation-history-4.xml` under `.ai-usage-local/ui-audit`. Six composite cases use production provider parsing, recorder, store and projection; two new transition cases independently assert before/after amounts, monthly daily delta, cap applicability and stable monetary series identity. Only initial observations are seeded into the exported native transition; the later observation must come from refresh.

Fourteen product defects have now been reproduced and fixed. The two latest defects concern history: refreshed readings/names did not update an open panel, and a pending read could reopen it after closure. New tests also protect focused-date retention across midnight, switching to another account while refresh is pending, and cancellation on disposal. The view preserves keyboard focus when refreshing the existing history panel. New actual Windows evidence for these changes remains NOT_RUN until the transition driver executes.

`expanded-native-2` executed 23 tests in 563.498 seconds: 18 PASS / 5 FAIL / 0 skipped. It used source `2e50e69e7fa4355c14af2175c14485a9bfdc18c4` plus the recorded working diff, application SHA-256 `C38804356E3BDC3F97A766C8FA22F52DA71B52F69E1AE6E400069460022DD26B`, driver SHA-256 `7BC599ABEDD2C32CD101E251B11CC71197C61B6AB6E8C44A02C5A6FD47583335`. Actual results and captures are in `.ai-usage-local/ui-audit/expanded-native-2/evidence`. Authentication (9), held startup (2), mocked recovery (3), preferences/restart, settings/editors/history/support and owned-process cleanup passed; one tray case passed. Replay stopped at page 008 with a COM stale-root failure, first-run card lookup failed despite a visibly correct synthetic account, and three tray cases failed during shell input. These are retained as failed driver runs, not blanket provider/UI passes. Recovery service tests do not establish production checkpoint restore or deletion lifecycle correctness.

`tray-check-4` failed all four cases after a cached shell element lost bounds. `tray-check-5` preserved shell UIA diagnostics and actually passed visible/idle, hidden/idle and hidden/in-flight cases; visible/in-flight failed before a caption click because the restored native button was absent from that immediate UIA lookup. A fresh native root and bounded caption readiness lookup are implemented; their actual rerun is pending. No ownership/input guard was weakened. The host remains locked; network-disabled Sandbox supplies the interactive guest desktop.

The overview and existing state corpus remain interim captures. Final coverage still requires calendar/work-today transitions, cap statuses, history zero/gap/reset and isolation interactions, actual isolated recovery/data lifecycle, repeated activation, ordinary resizing, all applicable hover controls, completed replay and inspected captures from the final build. This report does not claim comprehensive completion.

## Reviewed history and native rerun checkpoint

The current full deterministic suites PASS 1044/1044: Infrastructure 859 and Presentation 185, with zero errors, skips or not-run tests. Evidence is `infrastructure-calendar-all.xml` and `presentation-history-navigation.xml`. The calendar cases independently assert new-month first-day history, retained prior-month facts, exact local-midnight rollover and Work today expiry. Their native driver is running; this is not yet interactive PASS evidence.

Fresh independent review of `9e51fbb` and the working driver diff found a history-refresh regression: selecting another day while the asynchronous read was pending was overwritten by the selection captured when the read began. The new test `AHistoryRefreshKeepsNavigationMadeWhileTheReadIsPending` failed with 4 Oct expected and 5 Oct actual (`history-navigation-red.xml`). The completion now takes the current selection after checking the request generation and account/limit identity. The full 185-test presentation suite passes. This is a correction of the recent history fix, not an additional defect discovered in the original baseline.

`expanded-native-3` executed 25 tests in 982.536 seconds: 19 PASS / 6 FAIL / 0 skipped. Replay completed all 29 pages: overview, the 65 added boundary/reset/DST cases and six parser-backed composite dashboards, each in Used and Left modes. First-run plus duplicate-provider buttons also passed. Transition-history tests failed before their history-icon clicks; settings failed on an unawaited focus before sign-out. Tray passed visible/in-flight only. These failed attempts remain separate evidence.

The corrected transition and settings drivers actually reran in `controls-regressions-5`: 8 tests in 263.395 seconds, 5 PASS / 3 FAIL / 0 skipped. Both gain/loss transitions, settings/editors/history/support, preferences/restart and hidden/idle tray passed. The three other tray cases failed during shell lookup/input. Source was `9e51fbb6782e4b4d2560140749fc9210346107a8` plus the recorded working diff; application SHA-256 `0D7130DB682877D5180E19FFB86A5BB6399F66AB80A36E3909FC9C921146C285`, driver SHA-256 `945FFAAB62288E4418E943172E127909275815163B5E0675B17675AE87E79086`. Physical settings Used/Left clicks were performed after actual scrolling; restart assertions waited for the visible settings panel and checked the native topmost flag.

`controls-regressions-4` unintentionally selected six older LedgerSmoke tests with the short `*Settings*` method filter, in addition to the intended eight audit tests: 14 total / 11 PASS / 3 FAIL. Its five older first-run/demo cases passed; the empty-root smoke failed on an early disabled control invocation. They do not prove the audit driver's physical-click acceptance criteria. Reproduction commands now use fully qualified `*AuditWindows...` filters and explicit repeated method arguments.

Fresh review also confirmed a driver defect: shell input requested a clickable point from a cached element before rebinding it. `tray-review-regressions-6` used the corrected first lookup and actually ran all four tray cases: 2 PASS / 2 FAIL / 0 skipped. Hidden/idle and hidden/in-flight passed; visible cases still encountered the shell overflow disappearing before Refresh. Shell evidence establishes transient closing/layout states; it does not establish dead-icon removal or a product tray defect. The next run records foreground/window identities and waits for the observed shell state and button position to settle, without weakening process ownership or hit-testing guards.

No complete final-build gallery or comprehensive completion is claimed. Remaining native/visual gaps are explicit in the matrices and plan; the earlier failure screenshots and XML are preserved.

## Calendar, native lifetime and production maintenance checkpoint

`calendar-tray-7` actually executed 7/7 PASS in 221.377 seconds: four native tray variants and CAL-01/CAL-02/CAL-04. It checked local month rollover, first-day history, retained prior-month readings, Work today on/off and expiry, and native Open/Refresh/Exit with both visible and hidden windows. Application SHA-256: `C31829B87A0FED0389B5A8DCC7532B1BFBDE4DC723BAA185C3F61D21D010772D`; driver: `F04798DEC606478E5F18515F1E913A78851D6CD0C3960ECF5794727E198B9AA3`. Source was `9e51fbb` plus the changes subsequently saved at `2c0e52b`. Reviewed shell position/lifetime waits fixed the harness failures; their actual rerun passed without relaxing ownership checks.

`maintenance-native-1` executed 5 tests: 3 PASS / 2 FAIL. The deletion restart discovery failed in the harness; evidence remains separate. Fresh focused independent review then found insufficient proof of a replacement process's synthetic context and a mismatched diagnostic storage root. The replacement now supplies a receipt bound to its PID, operating-system start time, fixture hash, controlled clock and timezone before attachment/input/cleanup. Diagnostics and maintenance use the same marked temporary root. Only explicitly launched or proven synthetic processes are eligible for failure cleanup.

The corrected `maintenance-native-2` actually executed 5/5 PASS in 96.011 seconds, with zero errors, failures, skips or not-run tests. It exercised the production storage lease, failed/successful Retry, failed/successful checkpoint Restore, newer-schema disabled controls, actual export, Delete cancellation/confirmation, interrupted deletion and native restart, repeated launch and Ctrl+Q during blocked synthetic refresh/authentication. Provider and recognized log sentinels were removed only on confirmation; a synthetic external export was retained. Restarted first-run controls and the selected Wed 7 Oct 12:00 clock were verified. Application SHA-256: `C26D6A236DD6CE68AE7185FCE7A2D841A18EEDC32DEEB3DF9966E6A68373A16A`; driver: `B7BBF0819923DE7926BA65B1916918B012975DFCF0E4C890FD213B1A49A0CF09`; source `2c0e52be770307cd475cf8a9047f1a36fc057d8a` plus the recorded working diff. Exact build/run/XML/capture evidence is under `.ai-usage-local/ui-audit/maintenance-native-2/evidence`.

The maintenance composition is audit-only and accepts empty account/reading/cap fixtures. It reuses production StateMaintenance and LedgerProductLifetime with mock credential migration, account and browser boundaries. Native restart retains the validated synthetic selection. Budget/account/calendar clocks are controlled; operating-system process, diagnostic and checkpoint metadata timestamps use real system time. No real credentials or provider adapters were used. Focused follow-up review passed the corrected safety boundaries; reviewer execution and primary native execution remain distinct records.

Full suites now PASS 1045/1045 (288 more than baseline): `infrastructure-maintenance-context.xml` 859/859 and `presentation-process-receipts.xml` 186/186. Unsigned MSIX 2026.10.412.0 passed before the newest maintenance composition; hash `197EE25FBE06098BF99EC03BA1C35A2CD504FB8E0E6D737AC64F04E98DF0DAE9`. The new Windows lifetime changes require a fresh package build. No package was installed or signed.

Visual inspection identified evidence gaps despite successful interactions: the diagnostic preview text and newer-schema disabled controls were outside the captured viewport. Additional captures must show those exact controls. Captures now wait for ordinary layout/tooltip motion to settle; the complete final gallery must actually rerun using this driver. No comprehensive completion is claimed.
