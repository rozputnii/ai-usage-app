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
