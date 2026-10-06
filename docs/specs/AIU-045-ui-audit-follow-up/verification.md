# Interrupted audit: findings and handoff

Owner pause: 2026-10-04. **The audit is not complete.** This task preserves work for discussion and fixes in later sessions. No new product fix or audit run is authorized by this handoff itself.

**Health analysis, 2026-10-05:** the [analysis record](analysis-2026-10-05.md) holds the observed AUD-01 root cause, the ANL findings register, the owner decisions AIU045-D1..D9 and the proposed tasks T-01..T-04. It supersedes the read-only diagnosis step below; the exact next action is in its section 11. The AUD-01..AUD-10 rows below remain the preserved handoff state at the pause.

## Saved code and execution boundary

- Original audit base: `383644c`; latest preceding committed checkpoint: `f4891b912b32609da5f1c212a5f2d43834e2cc30` on main.
- The interruption checkpoint preserves the subsequent scope-annotation, contract-scenario, layout-control and failure-diagnostic work. It is WIP with known failing native startup, not a verified build.
- All Sandbox runs had ended when the pause was recorded; `wsb list --raw` returned an empty environment list. No owner installation, credentials or account data were touched.
- Existing synthetic inputs, application/test builds, XML, logs and raw screenshots remain locally under `.ai-usage-local/ui-audit/`. They are intentionally excluded from Git. Key failing-run metadata is copied into this task's `evidence/` directory; detailed reproduction remains available in the audit report and tools.

## Open findings and follow-up actions

| ID | Classification / status | Finding and evidence | Exact follow-up when selected |
| --- | --- | --- | --- |
| AUD-01 | Observed startup failure; FAIL; cause unclassified | `layout-resize-9` and `layout-resize-10` both terminate before any window. `startup-failure.json`: ProcessExited=true, ExitCode=-1073741189 (0xC000027B), Windows=[]. Both use application SHA-256 `CB756EB359EF66A7DE47FD95E9D70BF4C0F69A93BC998437E88FC46C91CB7135`. Occurred after the scope-annotation changes; causation is not established. Ordinary product startup impact is NOT_RUN. | Obtain the exception/failure boundary using only the isolated synthetic guest. Compare AuditReplay.Open and its new annotation validation with the last passing build. Do not label this a provider or owner-installation defect. |
| AUD-02 | Incomplete implementation / verification; NOT_RUN for native annotations | WIP adds AuditInput.ScopeAnnotations, optional LiveLedgerSource metadata, AuditReplay validation and audit DI wiring. Three source-isolation tests and eleven parser/recorder/projection cases passed. No annotation gallery/native scenario has passed. Two newly authored annotation-rejection test cases have not been executed. No required package build or final review of this latest diff. | Review whether the minimal metadata path is appropriate; execute valid/invalid monetary target cases and the specific native scenario after AUD-01 is resolved. Keep annotation evidence distinct from real wire scope. |
| AUD-03 | Harness target-selection bug; corrected, successful full rerun missing | `layout-recovery-8`: 3 tests, 2 PASS / 1 FAIL. Maximize/restore, shrinking and diagnostic scrolling executed; cap-editor lookup failed because the test selected the first percentage card rather than the Claude monetary card. Selection now uses the owning provider/account ID. Reruns 9/10 failed at startup before reaching this correction. | Rerun OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable. Verify resize, menu, editor, history/settings Escape precedence, refresh and target isolation through the final assertions. |
| AUD-04 | Diagnostic evidence gap; NOT_RUN | Session.RecordStartupFailure now copies logs only from its marked synthetic root and saves request receipts. Run 10 still captured only startup-failure.json; no marked-root log/receipt evidence was available. The failure may precede the marker, but that is an inference. | Capture the earliest failure safely. Guest application-event evidence was considered but was not implemented or executed. Do not claim an exception type or stack trace that has not been obtained. |
| AUD-05 | Native cap coverage gap; NOT_RUN | Set-cap actions on count/abstract-credit/no-limit Note cards; settings applied/mismatched/inactive/unmatched cap edit/remove behavior; exact target and unchanged-account assertions remain incomplete. See CAP-01 and CTL-34/38/39/40. | Select a small cap-specific batch; use existing parser fixtures and independently expected persisted quantities. Do not invent currency conversions. |
| AUD-06 | History/control coverage gap; NOT_RUN or partial | HIST-01/02/03 still lack the complete native percentage/count/money, zero/gap/reset/correction/incomplete-tracking and isolation matrix. HistoryChart currently exposes keyboard-selected days, with no per-day pointer handler found. CTL-64's assumed hover behavior needs contract reconciliation, not an automatic new feature. | Confirm intended supported interaction; test actual keyboard navigation and visible values. Mark unsupported hover combinations non-applicable with source evidence. |
| AUD-07 | Authentication coverage gap; partial | All eight failure reasons plus success/cancel/retry/manual code passed earlier native runs. Expired/signed-out reconnect with a wrong account and retained cap/history isolation still needs its exact native scenario; deterministic late-completion checks are not mouse evidence. | Exercise AUTH-03 with fake account/browser boundaries and exact retained account IDs. |
| AUD-08 | Remaining ordinary controls; partial / NOT_RUN | Some workday, mode, density, topmost and history actions have successful native runs but stale matrix rows; remaining tooltip, text undo, unchanged rename, Alt+Up/Down reorder, form Space/Tab and multi-strip tray navigation variants are not fully established. CTL-63 and KEY-01/04/11/15/17/18/19/20 are relevant. | Reconcile source and existing XML before adding tests; distinguish missing record updates from missing execution. Verify newly selected actions with physical input and observable effects. |
| AUD-09 | Final gallery incomplete; NOT_RUN | 235 corpus scenarios and six composite dashboards passed Used/Left assertions across multiple earlier builds. Only part of the rendered gallery was inspected; there is no complete final-build screenshot gallery. Earlier capture timing could catch layout motion. The driver now waits for ordinary motion to settle, but full recapture was not done. | Resume only if selected; capture readable final-build pages, scrolled sections, tooltip/editor/auth/recovery variants and inspect each required case. Preserve failure evidence separately. |
| AUD-10 | Evidence/index maintenance; incomplete | Audit report includes historical checkpoints; some control/additional rows lag subsequent passes. CAL-01/02/04 supporting-test label was entered as CalendarRefreshResetsMonthlyHistoryAndExpiresWorkToday, but the actual Infrastructure method is CalendarRolloverAndWorkTodayUseLocalPeriodsWithoutChangingProviderFacts. Scope export initially used a relative output path and wrote beneath the test binary directory; it was rerun successfully with an absolute export path. | Reconcile exact test names, build references and per-row verdicts. Use absolute export destinations. Never count overlapping targeted tests as additional unique full-suite passes. |

No fix has been proposed as confirmed for AUD-01; its repeated failure must remain visible in the next session.

## Frozen audit records and opt-in follow-ups (2026-10-06)

- **AUD-10 frozen (2026-10-06).** Under AIU045-D5(a) and D7(a), AUD-10 is frozen as historical: the audit records in `docs/workflow/ui-ux-audit-2026-10-04/` are no longer maintained or reconciled, and the [audit report](../../workflow/ui-ux-audit-2026-10-04/report.md) and [gallery](../../workflow/ui-ux-audit-2026-10-04/gallery.md) carry a first-line freeze banner. The AUD-10 row above is kept as the handoff state at the pause.
- **AUD-05..AUD-09 opt-in (2026-10-06).** Under AIU045-D7(a), AUD-05, AUD-06, AUD-07, AUD-08 and AUD-09 are opt-in follow-ups: none runs unless the owner selects it, the comprehensive audit is not resumed, and each exact follow-up action in the table above is unchanged.
- **Generated CSV destination (2026-10-06).** Generated CSVs from `tools/windows/New-UiAuditPages.ps1` now default to `.ai-usage-local/ui-audit/coverage/` (AIU-045 fix-run task F, T-04); writing them into `docs/workflow/ui-ux-audit-2026-10-04/` requires passing `-CoverageDirectory` explicitly.

## Product defects already repaired during the audit

These fourteen baseline defects have fixes and regression evidence in the [audit report](../../workflow/ui-ux-audit-2026-10-04/report.md). Their final-build gallery and any remaining native variants are separate gates.

| ID | Fixed defect |
| --- | --- |
| FIX-01 | Successful empty provider readings rendered NotReady instead of NoDisplayedLimits. |
| FIX-02 | Reset, reading and failure timestamps displayed UTC clocks instead of the selected local timezone. |
| FIX-03 | Money formatting/editing supported only six decimals despite the contract allowing exponents 0..18. |
| FIX-04 | Cap input accepted incompatible currency symbols and malformed separators. |
| FIX-05 | Double conversion lost money cents and small overflow on large amounts. |
| FIX-06 | Provider overage incorrectly increased the displayed provider-limit denominator. |
| FIX-07 | Cancel was disabled during a retried sign-in. |
| FIX-08 | Workday accessible/control names stayed stale after toggling. |
| FIX-09 | The final selected workday looked enabled while its click was silently rejected. |
| FIX-10 | Expired paired five-hour readings still appeared to be the current full window. |
| FIX-11 | Tab skipped Save in the inline cap editor. |
| FIX-12 | Uncapped/nonbinding today tooltips incorrectly blamed a personal cap. |
| FIX-13 | Open history did not refresh its readings or renamed account label. |
| FIX-14 | A pending history read could reopen the panel after it was closed. |

The later pending-read navigation correction belongs to FIX-13: asynchronous refresh must preserve navigation made while the read is pending. Its red/green test evidence is retained; do not inflate the baseline defect count.

## Independent-review findings already addressed

See [review.md](../../workflow/ui-ux-audit-2026-10-04/review.md) for the original verdicts and targeted reruns. REV-01 input/point ownership; REV-02 constructor-failure cleanup; REV-03 runner argument quoting; REV-04 overstated control coverage; REV-05 cleanup after evidence-write failure; REV-06 stale shell point lookup; REV-07 pending history selection; REV-08 restarted-process identity proof; REV-09 diagnostic/deletion root mismatch. All have recorded dispositions. This does not constitute review of the latest uncommitted scope-annotation changes.

## Fix run 2026-10-06: T-01 and T-02 integration evidence

Executed by the primary session of the [orchestration prompt](orchestration-prompt-2026-10-06.md) with reviewed worktree implementers (briefs A..G, I; reviews under the git-ignored `.ai-usage-local/AIU-045/run-2026-10-06/`). Every launch used `--demo` and a fresh `%TEMP%` root.

| Item | Verdict | Evidence |
| --- | --- | --- |
| ANL-01, ANL-03 (dispatch-only publication, AIU045-D1a) | PASS | `2f85275`: `preview` job gated on `workflow_dispatch` + `PublishPreview`; `Test-PreviewRelease.ps1` 40 assertions (was 29); CI run 37416927644 on `c26f18b`: `validate` and `windows-package` success, `preview` skipped |
| ANL-08 (validator rejects links into `.ai-usage-local/`) | PASS | `8f05989`; validator tests 82/82 (1 red before the fix); canonical validation valid |
| ANL-05 (intermittent diagnostics tests) | PASS / NOT_REPRODUCED | `13814e1`: `ForcedKill...` reproduced 21/56 pre-fix full-suite runs (17 with the exact CI message, under CPU pressure or 4 CPUs, 0/20 idle), fixed by waiting on the killed probe's process object; 20 consecutive idle runs 870/870 and 0/20 failures under the same pressure afterwards; `EventsAreJson...` NOT_REPRODUCED in 96 runs, unchanged. Second, environmental failure mode (2026-10-06): on this host the first full-suite run after a rebuild that changes Core, Infrastructure or the DiagnosticsProbe may fail the 10 s precondition wait at `DiagnosticCrashTests.cs:66` (first-start cloud-reputation latency of freshly built binaries: Microsoft Defender Block at First Sight and/or Smart App Control evaluation; undetermined which), measured 4/10 after unique-stamp rebuilds and 0/10 after unchanged-bytes rebuilds and warm reruns; rule: a plain rerun on the same binaries decides, and no timeout is relaxed; a Defender exclusion is untested advice for the owner. Separate observation (2026-10-06): HourlySweep's 2 s flush assertion failed twice on this host right after rebuilds (round1-04, round1-19; round1-04 with no cloud events, so not shown to share the first-start mechanism) and never in CI |
| AUD-01 fix, AUD-02, ANL-09 (AIU045-D3a revert, D2b packaged gate) | PASS | `5560205`: page and maintenance fixtures without `ScopeAnnotations` open (red `ArgumentNullException`/`NullReferenceException` then green); packaged process ignores `--audit-input`; no `ScopeAnnotations` reference remains in `src`; host A/B on the Debug build: run A (original `combined-pages/overview.json`) now shows the window and writes the marker, run B unchanged |
| AUD-04 (observable `Open` failure) | PASS | Altered `SyntheticMarker` on the host: exit code 90, stderr `InvalidDataException`, no state directory or marker created; driver capture (`RecordStartupFailure` exit code and stderr): compiled, native NOT_RUN |
| ANL-11 (omitted-property probes) | PASS with a ruling | `2c0913c` + `f4c13fd`: the source generator overwrites every `init` initializer when a property is omitted, also for types without a parameterized constructor; `AccountRecord.Connected` and `ReadingObservation.RoundingUnit` now keep their defaults through constructor parameters; registry and stored-state `Version`/`Accounts`/`Pending` keep failing closed (RecoveryRequired) by primary ruling, asserted by six probes; Infrastructure 879/879. Hand-edit-only consequences of the `Connected` constructor default (2026-10-06): an entry with `Disconnecting` true and `Connected` omitted completes its disconnect instead of RecoveryRequired; an entry with `Connected` omitted and `Disconnecting` false loads as connected instead of signed out (cached read, non-destructive). The app never writes such files |
| ANL-17 and the CSV destination (task F) | PASS (static) / BLOCKED (dry run) | `cce0d60`: `source.diff` + `SourceDiffSha256` beside `build.json`; `New-UiAuditPages.ps1` `CoverageDirectory` defaults under `.ai-usage-local/ui-audit/coverage/`; parse checks 0 errors; the dry run needs PowerShell 7, which is not installed on the host |
| Suites on merged `main` (`c26f18b`) | PASS | Debug build 0 warnings; Infrastructure 870/870; Presentation 189/189 (191 - 2 deleted seam cases - 3 deleted annotation cases + 3 new); validator tests 82/82; canonical valid |
| Documentation records (task G) | PASS | `c24de6e`: CONTRIBUTING D1/D4 wording, Preview gate section, AIU-014 scope-note sentence, D6(a) annotations in the AIU-039/043/044 records, AUD-10 freeze banners, AUD-05..09 opt-in notes |

Deferred minor observations and every primary ruling are listed in the run ledger and in the final report at the owner checkpoint; none blocks the candidate. The wave 1.5 merges (`effcb4b`, `8cf68de`) and their CI run 37417959587 on `8364408` are recorded in the candidate verification section below; the desktop lane on that candidate is BLOCKED and is rerun after the owner unlocks the desktop. The primary rulings and deferred observations are also listed in the analysis record's "Final status" section.

## Last actual verification

- Last full suites: PASS 1045/1045 = Infrastructure 859 + Presentation 186, before the latest scope work. XML: `infrastructure-maintenance-context.xml`, `presentation-process-receipts.xml`.
- New targeted checks: scope source 3/3 PASS (`scope-source-green-2.xml`); combined scope/extra evidence 11/11 PASS (`scope-extra-export.xml`). These are not a fresh full-suite result. Earlier missing-API/fixture compile failures and a three-case no-history fixture expectation failure are harness/test-development failures, not additional product defects.
- `calendar-tray-7`: 7/7 PASS, including all four tray variants and three controlled calendar transitions.
- `maintenance-native-2`: 5/5 PASS, including real isolated Retry/Restore/Delete/native restart/repeated activation/in-flight shutdown. Reviewed synthetic boundaries; no real credential migration.
- `layout-recovery-8`: 2 PASS / 1 FAIL. The recovery button, diagnostic preview and disabled newer-schema controls now have actual capture evidence. `LIFE-04-resized-diagnostic-preview.png` and `REC-04-production-newer-schema-disabled-controls.png` were inspected and readable. This does not establish the failed layout scenario as PASS.
- `layout-resize-9`, `layout-resize-10`: each 0 PASS / 1 FAIL at startup, same application hash. No controls were reached in those two runs.
- Last unsigned package: 2026.10.412.0 PASS, before maintenance/scope changes; current package validation NOT_RUN. No package installed, signed or released.
- Final full-suite/native/gallery/review completion: NOT_RUN. Do not use the cumulative test count as completion evidence.

## Reproduction after explicit resumption

Read the evidence first. The latest failing scenario was invoked with:

```powershell
./tools/windows/Run-UiAuditSandbox.ps1 -PageDirectory .ai-usage-local/ui-audit/combined-pages -OutputDirectory .ai-usage-local/ui-audit/layout-resize-10 -TestMethod '*AuditWindows.OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable'
```

Choose a **new** output directory for any future rerun; existing evidence must not be overwritten. The runner rebuilds the application and driver, records source/dirty state and hashes, then operates only inside its network-disabled guest. It requires already exported synthetic pages. Do not run this command while the task remains paused.

The read-only diagnosis of AUD-01/AUD-02 was completed on 2026-10-05 on the host with synthetic input; see the [analysis record](analysis-2026-10-05.md), section 3. The exact next action is its section 11, not another comprehensive test run.

## Candidate verification 2026-10-06

T-03 desktop lane (task J), run in the main checkout at `836440889ccb5c8841b540e72aabbcf3138e03d6` (`8364408`, equal to `origin/main`, clean tree). Every app launch used `--demo` and a fresh `%TEMP%` root, verified on the real command line. No sign-in, credential, owner installation or real state was touched. Small result files are in [evidence/candidate-2026-10-06](evidence/candidate-2026-10-06/release-build.json); raw logs, XML and scripts stay in the git-ignored `.ai-usage-local/AIU-045/run-2026-10-06/evidence/candidate-2026-10-06/`. Provenance (2026-10-06): the lock log and the before/after `wsb list` records are summaries written by the task, and the committed `step2-attempt1.json` adds a `ForegroundOwner` field that the raw capture lacks.

**The interactive desktop was locked for the whole lane.** From the first host launch (06:25) to the last retry (07:25), `LockApp` owned the foreground and `LogonUI` was running, which is the project's own BLOCKED condition (`DesktopTestEnvironment.RequireUnlockedDesktop`). The host steps were retried three times at 20-minute intervals (06:45, 07:05, 07:25), with 30-second polling in between, and never saw an unlock. Every host step that needs desktop input is therefore BLOCKED, not FAIL.

| Item | Verdict | Evidence |
| --- | --- | --- |
| CI on the candidate (wave 1.5 merges `effcb4b` D, `8cf68de` E+I, docs `8364408`) | PASS | Run 37417959587 on `8364408`: `validate` success, `windows-package` success, `preview` skipped; [ci-37417959587.json](evidence/candidate-2026-10-06/ci-37417959587.json) |
| Step 1: Release build | PASS | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore`: 0 warnings, 0 errors. `AiUsage.dll` `C12DF590488E120ECE781D0495CC65017D42FB862235DE3C1D34E29FA9E99D9A`, `AiUsage.exe` `9FE362F631583D66ADB1E58C0566587BF4818C91BFB106478072ADD4C548ED84`; [release-build.json](evidence/candidate-2026-10-06/release-build.json) |
| Step 2: host Release `--demo` startup and Ctrl+Q exit | BLOCKED | One launch at 06:25: window appeared (title "AI Usage"), log `SessionStarted` with `mode` `demo`, no 1000/1001/1026 event for `AiUsage.exe`. Foreground could not be obtained: SetForegroundWindow, AttachThreadInput and a real caption click all left `LockApp` in front, so the guard sent no key and the process was killed (exit -1, no `SessionExited`). No Ctrl+Q exit was observed, so no PASS is claimed. Events 1000/1001/1026 in that window belong to `AiUsage.DiagnosticsProbe.exe` (the Infrastructure suite's crash canary from a concurrent run). [step2-attempt1.json](evidence/candidate-2026-10-06/step2-attempt1.json) |
| Step 3: native driver publish | PASS | `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true -o .ai-usage-local/AIU-045/run-2026-10-06/evidence/smoke`; `AiUsage.Windows.Tests.dll` `600960BD99D348277DBDA16E4FA7ACFA3CC841F72F35374CEF3A5759042FC821` |
| `LedgerSmoke.LedgerLaunchSettingsHistoryAndExit(demo: True)` | BLOCKED | Not run: desktop locked. Exact single-case command: `AiUsage.Windows.Tests.exe -preEnumerateTheories -id 5a3dff3a136c38a3a3444eff9e58f70f728f318dd0d4fc23a5036e9834030637 -parallel none -xml <file>` (the ID is the runner's listed unique ID of that case) |
| `LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions` | BLOCKED | Not run: desktop locked. Command: `-method AiUsage.Windows.Tests.LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions -parallel none -xml <file>` |
| `LedgerSmoke.WorkBudgetShowsMonthlyAndDailyBarsBesideSubscriptionWithoutCredits` | BLOCKED | Not run: desktop locked. Command: `-method AiUsage.Windows.Tests.LedgerSmoke.WorkBudgetShowsMonthlyAndDailyBarsBesideSubscriptionWithoutCredits -parallel none -xml <file>` |
| AIU-043 startup and late-disposal checks as Windows.Tests methods | NOT_APPLICABLE | The AIU-043 record names them as harness probes, not test methods: "Actual invalid XAML and hosted-service StartAsync failure" (`completion-lifecycle.json`) and "Actual DI-owned hosted-service Dispose failure" (`completion-disposal.json`); no method in `tests/windows/AiUsage.Windows.Tests` runs them |
| AIU-043 probes rerun on the candidate (supplementary) | PASS | Product entry `AIU_DIAGNOSTIC_PROBE` with a fresh `%TEMP%\aiu-ui-probe-*` root, `--demo`, no input needed: `xaml-startup` exit 0xC000027B, `host-startup` exit 1, each one terminating `StartupFailure`; `host-disposal` exit 1, one terminating `DisposalFailure`; no canary in any file. Same outcomes as the AIU-043 completion record. [aiu043-probes.json](evidence/candidate-2026-10-06/aiu043-probes.json) |
| `ShellSmoke.PackagedLedgerLaunchesAndExits` | NOT_RUN | Needs an installed package; belongs to the owner checkpoint |
| Step 4 (D9): `LedgerSmoke.LedgerLaunchSettingsHistoryAndExit(demo: False)` live-empty | BLOCKED | Not run: desktop locked. Prepared command: `AIU_SMOKE_MODE` unset, empty `AIU_DEVELOPMENT_STATE_DIRECTORY` under `%TEMP%`, `-preEnumerateTheories -id 45456c1d8b3c0b44ff8e51bbcc29d2aa4ff6636d35ab952c3ad5021806bf2c34`. No code change was made, because no failure was observed |
| Step 5 (D7a): Sandbox batch | BLOCKED (harness) | See below; no guest command and no test ran. [sandbox-build.json](evidence/candidate-2026-10-06/sandbox-build.json), [sandbox-run.json](evidence/candidate-2026-10-06/sandbox-run.json) |

**FIX scenario resolution (recorded before the Sandbox run).** The audit report's "Product defects fixed" table and `controls.csv` map the three FIX IDs to existing native scenarios:

- FIX-09 (final selected workday looked enabled while its click was rejected): `AuditWindows.SettingsFormsUndoAndPreferencesSurviveAnIsolatedRestart`. Its `controls.csv` row CTL-24 ("Last selected workday") names that method, and the method asserts that the last workday is disabled, re-enabled by selecting another day, and disabled again after Undo and Ctrl+Z (`AuditPreferenceControls.cs:55-68`).
- FIX-10 (expired paired five-hour readings still appeared current): `AuditWindows.ReplayPagesRenderUsedAndLeft`. The WRESET-01/WRESET-02 rows in `coverage.csv` name it as their native test; their pages are `page-015`..`page-018` of `combined-pages`. The method replays every page in the directory.
- FIX-11 (Tab skipped Save in the inline cap editor): `AuditWindows.SettingsFormsUndoAndPreferencesSurviveAnIsolatedRestart`, the method of the `preferences-check-3..5` runs cited in the report; it asserts Tab from the amount reaches Save and Shift+Tab returns (`AuditPreferenceControls.cs:88-92`).

The layout method lives in class `AuditWindows` (file `AuditLayoutControls.cs`), so its filter is `*AuditWindows.OrdinaryMouse...`; the brief's `*AuditLayoutControls.` form would match no test. FIX-09 and FIX-11 share one method, so the batch had three filters.

**Sandbox batch.** Command (Windows PowerShell 5.1; `wsb list --raw` was empty before and after):

```powershell
./tools/windows/Run-UiAuditSandbox.ps1 -PageDirectory .ai-usage-local/ui-audit/combined-pages -OutputDirectory .ai-usage-local/ui-audit/candidate-2026-10-06 -TestMethod '*AuditWindows.OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable','*AuditWindows.SettingsFormsUndoAndPreferencesSurviveAnIsolatedRestart','*AuditWindows.ReplayPagesRenderUsedAndLeft'
```

The runner published the app (`AiUsage.dll` `20B7E8F4E0C5E3F64CFAC5ED23A64E8E40E690FBE6E4A619BB5D717B55F9F481`, self-contained) and the driver, wrote `build.json` with `Dirty` false and an empty `source.diff` (`SourceDiffSha256` `E3B0C442...B855`), started the Sandbox, and then stopped it after a few seconds. Root cause: the readiness loop at `Run-UiAuditSandbox.ps1:113` runs `& wsb exec ... 2>&1` under `$ErrorActionPreference = 'Stop'`. The first probe normally fails while the guest logs in ("A specified logon session does not exist"); every earlier run retried it, and each `desktop-ready.log` shows three failed probes. Under Windows PowerShell 5.1, a redirected native stderr line becomes a terminating `NativeCommandError`, so the loop aborted on its first probe. This reproduces locally without Sandbox (`ps51-native-stderr-repro.txt`). The fix is in `tools/`, outside this task's write-set, so it was not made. The batch did not reach the guest, so this is not an AUD-01 startup failure. Another Sandbox start needs a primary ruling: was the authorized D7a session consumed? (Answered 2026-10-06: not consumed; see the exact next action.)

**Deviations and observations.**

- `AIU_SMOKE_MODE` is read by no test; every `LedgerSmoke` method creates its own fresh `%TEMP%` root through `start.Environment`, so the environment root is only a guard.
- The Sandbox runner's self-contained publish writes into the same `bin\x64\Release\...\win-x64` folder as step 1 and replaced the step-1 binaries. A rebuild with the step-1 command produced the same `AiUsage.exe` hash but a different `AiUsage.dll` hash (`98A4AA5759DFDFBF71F5D2B5A174C45065FF8C01FA7850C309A7B2426E31A6C9`, same length) from the same clean source. The Release `AiUsage.dll` differed between the two builds; the cause was not isolated (the Sandbox runner's self-contained publish rewrote the same output folder in between), so a hash identifies a build instance, not the commit. The supplementary probes ran on this rebuild.
- ANL-10 (source reading only, not a result): both title-bar Used/Left buttons bind `IsEnabled` to `CanUseAccounts = !NeedsRecovery && !IsStarting` (`LedgerWindow.xaml`, `LedgerViewModel.cs:111`). With zero accounts they are therefore enabled once live startup finishes. The test's `Button()` helper waits for existence, not enablement, before `Invoke`. That makes a test-side race during live startup the leading hypothesis for the 2026-10-04 `ElementNotEnabledException`, not a product defect. It stays unconfirmed until the method runs.

**Statuses after this lane.**

- AUD-01: the root cause and fix (`5560205`) and the host A/B are recorded above. The AC-02 closure rerun is BLOCKED (harness), so AUD-01 stays open for AC-02.
- AUD-03: open (same rerun).
- ANL-04: open. Release `--demo` startup reached a window and `SessionStarted`, but the Ctrl+Q exit is BLOCKED; live-empty is BLOCKED.
- ANL-07: the AIU-043 startup and disposal probes PASS on the candidate; the three native smoke methods are BLOCKED.
- ANL-10: open (BLOCKED).
- Dispatch gates (analysis section 6): gate 5 met apart from the AC-02 rerun; gate 6 BLOCKED; gate 7 is the primary diff review, outside this lane; gate 8 met (ANL-09 above).

Exact next action (updated 2026-10-06): the runner's readiness probe now tolerates native stderr under Windows PowerShell 5.1 (`3e0a509`, merged as `f409def`), and by primary ruling the aborted guest start did not consume the one authorized D7a session, so once the owner has unlocked the desktop the primary reruns steps 2 to 5 on the pushed candidate, including the single Sandbox batch with the three filters above into a new output directory, before the Preview dispatch (owner checkpoint list in [spec.md](spec.md)).

## Candidate verification rerun 2026-10-06

T-03 desktop lane (task J) rerun after the owner unlocked the desktop. Primary check at 09:31:23; the lane check at 09:31:50 found no `LogonUI`, and `wsb list --raw` was empty. Run in the main checkout from `5bb0e3bc55696c5652e713d38963cdbb0ec51983` (`5bb0e3b`, equal to `origin/main`; `src/` and `tests/` unchanged since `8364408`). The lane added three test-only commits and one more test-only commit after the Sandbox batch (below). No product code changed.

Every app launch used `--demo` and a fresh `%TEMP%` root, except the D9 live-empty case, which used an empty isolated root with zero accounts. No sign-in, credential, owner installation or real state was touched. The desktop stayed unlocked for the whole lane: every scripted loop checked `LogonUI` before each run, and none reported a lock.

Small result files are in [evidence/candidate-2026-10-06b](evidence/candidate-2026-10-06b/release-build.json). In the copied XML, the computer and user names are blanked and the repository and profile paths are shortened. Raw logs, XML, scripts and captures stay in the git-ignored `.ai-usage-local/AIU-045/run-2026-10-06/evidence/candidate-2026-10-06b/` and `.ai-usage-local/ui-audit/candidate-2026-10-06b/`.

| Item | Verdict | Evidence |
| --- | --- | --- |
| Step 1: Release build of `5bb0e3b` | PASS | Same command as above; 0 warnings, 0 errors. `AiUsage.dll` `5ACA08763B40FA1B984F89FBA956B3D973938C7D87924F0CF54032529BE605CA`, `AiUsage.exe` `4379D56CB0A57C3824D699118BC0992C05143CF553E39269C475ABC6C0F74D11`. Both differ from the `8364408` build (`C12DF590...`, `9FE362F6...`) although `src/` is unchanged, which again shows that a hash identifies a build instance. Steps 2 to 4 ran on this build; [release-build.json](evidence/candidate-2026-10-06b/release-build.json) |
| Step 2: host Release `--demo` startup and Ctrl+Q exit | PASS | Lock state checked before any input (foreground `AiUsage`, no `LogonUI`). SetForegroundWindow succeeded on the first step, Ctrl+Q was sent with the window verified foreground, and the process exited in 0.19 s with code 0. Log: `SessionStarted` with `mode` `demo` and build `1.0.0+5bb0e3b...`, then `SessionExited`. No 1000/1001/1026 event in the window; [step2-release-demo.json](evidence/candidate-2026-10-06b/step2-release-demo.json) |
| `LedgerSmoke.LedgerLaunchSettingsHistoryAndExit(demo: True)` | PASS | `-preEnumerateTheories -id 5a3dff3a...0637`; final run [native-ledgerlaunch-demo.xml](evidence/candidate-2026-10-06b/native-ledgerlaunch-demo.xml). Also passed on the unmodified driver (first run, 9.8 s). One stale-window failure in 17 runs was fixed in `11645ac`, after which it passed 20 of 20 |
| `LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions` | PASS after test fix | The unmodified test failed intermittently (5 of 25 runs) on a stale main-window element (below). After `02e0ca8` it passed 30 of 30 and 15 of 15; final run [native-account-spending.xml](evidence/candidate-2026-10-06b/native-account-spending.xml); red [red-account-spending-history.xml](evidence/candidate-2026-10-06b/red-account-spending-history.xml) |
| `LedgerActivationSmoke` / `LedgerSmoke.WorkBudgetShowsMonthlyAndDailyBarsBesideSubscriptionWithoutCredits` | PASS | First run 2.0 s, final run [native-work-budget.xml](evidence/candidate-2026-10-06b/native-work-budget.xml) |
| Step 4 (D9): `LedgerLaunchSettingsHistoryAndExit(demo: False)` live-empty | PASS after test fix (ANL-10 resolved) | Red 4 of 4 times: `ElementNotEnabledException` at the Used/Left invoke (`LedgerSmoke.cs:241` at `5bb0e3b`), [red-ledgerlaunch-live-empty.xml](evidence/candidate-2026-10-06b/red-ledgerlaunch-live-empty.xml). Fixed in `4c32153` and `11645ac`, then 15 of 15; final run [native-ledgerlaunch-live-empty.xml](evidence/candidate-2026-10-06b/native-ledgerlaunch-live-empty.xml) |
| `ShellSmoke.PackagedLedgerLaunchesAndExits` | NOT_RUN | Needs an installed package; owner checkpoint |
| Step 5 (D7a): the one Sandbox batch | FAIL (2 of 3 passed) | `candidate-2026-10-06b`; build `11645ac`, `Dirty` false, empty `source.diff`, app `BB85955C...`; guest desktop prerequisite passed and the app started in every scenario. `ReplayPagesRenderUsedAndLeft` PASS (840.6 s, all 29 combined pages); `SettingsFormsUndoAndPreferencesSurviveAnIsolatedRestart` PASS (53.5 s); `OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable` FAIL at `AuditLayoutControls.cs:47`. [sandbox-run.json](evidence/candidate-2026-10-06b/sandbox-run.json), [sandbox-native.xml](evidence/candidate-2026-10-06b/sandbox-native.xml), [sandbox-build.json](evidence/candidate-2026-10-06b/sandbox-build.json). No second guest was started |
| Layout scenario after the test fix (host, supplementary) | PASS | The same failure at `:47` reproduced 3 of 3 times on the host ([red-layout-host.xml](evidence/candidate-2026-10-06b/red-layout-host.xml)). After `e40c76d` it passed 5 of 5 on the host with `--demo --audit-input` on the same combined pages ([green-layout-host.xml](evidence/candidate-2026-10-06b/green-layout-host.xml)). This is not the Sandbox evidence AC-02 names |

**Failures and red-to-green fixes (all test-only).** Counts and runs are in [red-green.json](evidence/candidate-2026-10-06b/red-green.json).

- **Stale main-window element (`02e0ca8`, `11645ac`).** The account-spending smoke failed at the history, nested-content or scenario steps in 5 of 25 unmodified runs.
  - Diagnostic runs (instrumentation not committed) showed the following. Keyboard focus and foreground were correct. After Enter, the focused element was the history panel named "Mixed account history, ...". The launch HWND stayed alive as the process's only window throughout.
  - The test's cached `Window` element nevertheless returned COMException 0x80040201 for every property and an empty descendant list, while a freshly enumerated element for the same HWND found the history (diagnostic runs 49 and 59). The audit harness already notes this behavior ("WinUI can replace an ancestor's UIA provider during layout").
  - The smokes now resolve the window afresh for every query. With a fresh tree, the live-empty "needs recovery" check became a real scan and met elements replaced during live startup. It now repeats until one scan completes and asserts that scan's result. Before, a stale cached window returned an empty tree, so the check could pass without scanning anything.
- **ANL-10 (`4c32153`).** The failure capture shows "Opening local data...". During startup, Used/Left and Add are disabled by design (`CanUseAccounts = !NeedsRecovery && !IsStarting`). The test helper waited only for a button to exist. It now waits for the button to be enabled. With zero accounts, Used/Left become enabled once startup finishes and the case passes, so Used/Left was neither wrongly disabled nor enabled too late. This was a test race; there is no product race.
- **Layout scenario (`e40c76d`).** The Sandbox failure capture shows the expected product state: Escape closed only the cap editor, and history and the settings panel stayed open. The "Close settings" header button had been scrolled out of the settings panel by the earlier diagnostic-preview step, so the visibility check (not offscreen) failed. The check now scrolls that button into view and then requires it visible; a closed panel is collapsed and has no such element.
- No assertion or timeout was relaxed. The negative checks in the account-spending smoke now also require the expected element in the same fresh tree.

**FIX native reruns (Sandbox, `11645ac`).**

- FIX-09 and FIX-11 PASS: `SettingsFormsUndoAndPreferencesSurviveAnIsolatedRestart` passed. It covers the last workday disabled, re-enabled and disabled again, and Tab reaching Save in the cap editor.
- FIX-10 PASS: `ReplayPagesRenderUsedAndLeft` passed on all 29 combined pages, including the WRESET-01/02 pages `page-015`..`page-018`.
- The layout run reached and passed the AUD-03 target selection: the cap editor opened on the Claude monetary card, and Escape left no cap (`:41-46`).

**Statuses after the rerun.**

- AUD-01: no startup failure recurred; the guest started the app in all three scenarios. The AC-02 Sandbox rerun of the layout scenario is FAIL because of the `:47` test defect, which is now fixed (`e40c76d`) and passes on the host. Closing AC-02 needs either a primary or owner ruling that accepts the host PASS, or another authorized Sandbox batch.
- AUD-03: the corrected target selection passed in the Sandbox; the full scenario passes only on the host.
- ANL-04: PASS (Release `--demo` startup with Ctrl+Q exit 0, and live-empty).
- ANL-07: the three native smoke methods PASS on this candidate (two after test fixes); the AIU-043 probes PASS from the first lane.
- ANL-10: resolved (test race, `4c32153`).
- Dispatch gate 6: met.

Exact next action: the primary reviews `02e0ca8`, `4c32153`, `11645ac` and `e40c76d` (test-only), and rules on AC-02. Either the host PASS of the corrected layout scenario is accepted, or one more Sandbox batch of `*AuditWindows.OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable` is authorized into a new output directory.
