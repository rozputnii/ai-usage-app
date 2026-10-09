---
id: AIU-045
schema_version: 1
---
# AIU-045 fix-run execution and handoff

Spec: [spec.md](spec.md). Binding record: [analysis-2026-10-05.md](analysis-2026-10-05.md) (accepted decisions AIU045-D1..D9 and the grouped tasks T-01..T-04). Execution design: [orchestration prompt](orchestration-prompt-2026-10-06.md). The primary session alone integrates onto `main`. Implementer subagents (`aiu-implementer`, `aiu-simple-implementer`) work from per-task briefs in isolated local worktrees that the primary merges and never pushes; independent `aiu-reviewer` subagents verify each task before integration. Briefs, reports, reviews, diff packages and the run ledger live under the git-ignored `.ai-usage-local/AIU-045/run-2026-10-06/`.

Primary-owned paths (`.github/`, `CONTRIBUTING.md`, `AGENTS.md`, `docs/backlog.md`, this file) change on `main` only through the primary's reviewed merge. Tasks whose write-set includes such a path, or whose worker runs in the main checkout, are declared `agent: primary` below; their worker briefs are named in the prose.

### T-01 - CI and publication safety
- status: done
- depends_on: []
- ownership: Preview publication gating, validator link rule, intermittent diagnostics tests and process records
- writes: [.github/workflows/validation.yml, tools/windows/PreviewRelease.psm1, tools/windows/Publish-Preview.ps1, tests/release/Test-PreviewRelease.ps1, README.md, docs/specs/AIU-014-preview-updates/spec.md, docs/specs/AIU-014-preview-updates/verification.md, docs/specs/AIU-014-preview-updates/bug-2026-10-04-installer-connection-aborted.md, docs/decisions/accepted.md, tools/AiUsage.ProjectValidation/ProjectValidator.cs, tests/AiUsage.ProjectValidation.Tests/**, tests/windows/AiUsage.Infrastructure.Tests/DiagnosticCrashTests.cs, tests/windows/AiUsage.Infrastructure.Tests/FileDiagnosticsTests.cs, src/windows/AiUsage.Infrastructure/Diagnostics/**, tools/AiUsage.DiagnosticsProbe/**, CONTRIBUTING.md, docs/workflow/verification.md, docs/backlog.md, docs/specs/AIU-039-multi-account-ledger/verification.md, docs/specs/AIU-043-file-logging/verification.md, docs/specs/AIU-044-account-monetary-usage/verification.md, docs/specs/AIU-045-ui-audit-follow-up/verification.md, docs/archive/workflow/ui-ux-audit-2026-10-04/report.md, docs/archive/workflow/ui-ux-audit-2026-10-04/gallery.md]
- shared: []
- parallel: true
- isolation: required
- agent: primary
- acceptance: AC-05
- evidence: docs/specs/AIU-045-ui-audit-follow-up/verification.md (section "Fix run 2026-10-06"; integrated c26f18b and effcb4b; CI run 37416927644 on c26f18b and CI run 37417959587 on 8364408, which covers effcb4b and 8cf68de, green with preview skipped)

Closes ANL-01, ANL-03, ANL-05 and ANL-08 (ANL-02 was fixed in `7efd28f`). Worker briefs A (dispatch-only Preview publication and AIU-014 records, AIU045-D1a), B (validator rejects links into the ignored local root), D (intermittent diagnostics tests, `systematic-debugging`) and G (process and cross-cutting records, D4a and D6a drafts) run as `aiu-implementer` in isolated worktrees with disjoint write-sets; each is independently reviewed, then merged by the primary. The merged push is the first CI run of the product suites on the WIP tree (main-green checkpoint).

### T-02 - Audit replay fix and harness hardening
- status: done
- depends_on: []
- ownership: Audit replay adapter, audit test harness and audit tooling scripts
- writes: [src/windows/AiUsage.Windows/Adapters/Live/Audit/**, src/windows/AiUsage.Windows/Adapters/Live/Windows/AuditLedgerRegistration.cs, src/windows/AiUsage.Windows/Adapters/Live/Windows/ApplicationDiagnostics.cs, src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerSource.cs, src/windows/AiUsage.Windows/App.xaml.cs, tests/windows/AiUsage.Presentation.Tests/AuditReplayTests.cs, tests/windows/AiUsage.Presentation.Tests/LiveLedgerSourceTests.cs, tests/windows/AiUsage.Presentation.Tests/Fixtures/**, tests/windows/AiUsage.Infrastructure.Tests/AuditContractScenarios.cs, tests/windows/AiUsage.Windows.Tests/AuditWindows.cs, tests/windows/AiUsage.Infrastructure.Tests/PersistedRecordOmittedPropertyTests.cs, tests/windows/AiUsage.Infrastructure.Tests/Fixtures/**, tools/windows/Run-UiAuditSandbox.ps1, tools/windows/New-UiAuditPages.ps1]
- shared: []
- parallel: true
- isolation: required
- agent: aiu-implementer
- acceptance: AC-02, AC-03
- evidence: docs/specs/AIU-045-ui-audit-follow-up/verification.md (section "Fix run 2026-10-06"; integrated c26f18b and 8cf68de; CI run 37416927644 on c26f18b and CI run 37417959587 on 8364408, which covers effcb4b and 8cf68de, green with preview skipped)

Closes AUD-01 (fix), AUD-02, AUD-04, ANL-09, ANL-11 and ANL-17 under AIU045-D2b and D3a. Worker briefs C (revert the scope-annotation seam, packaged gate, observable Open failure), E (omitted-property probes; no product code) and F (`aiu-simple-implementer`; audit tool scripts) run in isolated worktrees. If E reports a red probe, fixer I continues on E's branch and this write-set is amended with the named record files before I starts. Runs concurrently with T-01 by owner request; the integrated result is verified once at Gate 1 before anything is pushed.

### T-03 - Candidate verification and Preview
- status: done
- depends_on: [T-01, T-02]
- ownership: Candidate verification on the serialized desktop lane and WIP diff dispositions
- writes: [docs/specs/AIU-045-ui-audit-follow-up/verification.md, docs/specs/AIU-045-ui-audit-follow-up/evidence/**, src/**, tests/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: AC-02, AC-04, AC-05
- evidence: docs/specs/AIU-045-ui-audit-follow-up/verification.md (sections "Candidate verification rerun 2026-10-06", records `a4d33cf`, and "Post-install verification 2026-10-06"; integrated `4ea9667` and the task N records commit; the earlier section "Candidate verification 2026-10-06" recorded the desktop lane BLOCKED, records `525fc26`, with the Sandbox runner fix merged as `f409def`)

Closes AUD-01 (AC-02 closure), AUD-03, ANL-04, ANL-07, ANL-10 and ANL-21; ships FIX-01..FIX-14 as a candidate. Reviewer R0 reads the WIP product diff (`383644c..BASE -- src`) and proposes keep/test/fix dispositions for the ANL-21 notes; fixer H implements the ruled items in a worktree. Worker J then runs alone in the main checkout as the sole writer of `src/` and `tests/`: Release build and SHA-256, host `--demo` smoke, native smoke methods, the D9 live-empty smoke and one Sandbox batch (D7a). The owner dispatches the Preview at the checkpoint; installed-build checks are recorded afterwards.

Blocked (2026-10-06). R0 found nothing to fix (all four ANL-21 notes ruled keep; H skipped). J's Release build and the AIU-043 probes PASS, but the interactive desktop was locked for the whole lane, so the host smoke, the native methods and the D9 live-empty smoke are BLOCKED; the Sandbox batch stopped at the runner's readiness probe under Windows PowerShell 5.1 before any guest command (fixed by task F2, `3e0a509`). By primary ruling the one authorized D7a session is still available. That next action was carried out; see the rerun below.

Rerun (2026-10-06, `a4d33cf`; verification record section "Candidate verification rerun 2026-10-06"). The desktop lane reran on the candidate (`5bb0e3b`). Host Release `--demo` startup with Ctrl+Q exited 0 with `SessionStarted` (demo) and `SessionExited` and no crash events: PASS (Release hashes differ between builds of unchanged source, so each build is re-hashed). Native smoke: `LedgerLaunchSettingsHistoryAndExit` (demo: True), `AccountSpendingUsesNestedContentHistoryCapsAndAccountActions` and `WorkBudgetShowsMonthlyAndDailyBarsBesideSubscriptionWithoutCredits` PASS, with test-only fixes `02e0ca8` and `11645ac` (re-resolve the window instead of a stale cached UIA element; complete the recovery scan). The D9 live-empty `LedgerLaunchSettingsHistoryAndExit` (demo: False) PASS after `4c32153`; ANL-10 resolved as a test race (the test invoked Used/Left while live startup showed "Opening local data...", where they are disabled by design), not a product race. `ShellSmoke.PackagedLedgerLaunchesAndExits` is NOT_RUN (needs the installed package; owner checkpoint). The one authorized Sandbox batch (D7a, build `11645ac`) passed 2 of 3: `ReplayPagesRenderUsedAndLeft` (FIX-10, 29 pages) and `SettingsFormsUndoAndPreferencesSurviveAnIsolatedRestart` (FIX-09, FIX-11) PASS; `OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable` FAIL at a test check (the "Close settings" button was scrolled out of the open settings panel; the captured product state was correct), fixed in `e40c76d`, red 3/3 then green 5/5 on the host and re-run green by the reviewer. The app started in every guest scenario. Primary ruling: AUD-01 AC-02 is met; AUD-03 is "Sandbox FAIL on a test defect; corrected scenario PASS on the host"; a second Sandbox batch only with the owner's explicit authorization. T-03 stayed `in-progress` at that point. Exact next action then: the owner dispatches the Preview and runs the installed-build checks.

Done (2026-10-06, integrated `4ea9667` and the task N records commit). The owner dispatched the Preview (run 37454494231 published 2026.10.601.0, whose App Installer GUI install failed with `0x80072EFE` over the owner's IPv6 path to GitHub Pages); fix `c703c96`, `c8a98c1` and `f32ff36` (merge `4ea9667`) moved the feed's package and dependency URIs to the immutable GitHub release, and run 37475616318 on `4ea9667` published 2026.10.602.0, which the owner installed with the App Installer GUI. Packaged startup, close-to-tray with relaunch and Open logs passed; `ShellSmoke.PackagedLedgerLaunchesAndExits` is NOT_APPLICABLE on the owner's host by design. Sandbox batch 2 passed the corrected layout scenario (`b17937b`). Results are in the verification record, section "Post-install verification 2026-10-06".

### T-04 - Records, process and cleanup
- status: done
- depends_on: [T-03]
- ownership: Closing records, post-install records and local housekeeping
- writes: [docs/backlog.md, docs/specs/AIU-045-ui-audit-follow-up/spec.md, docs/specs/AIU-045-ui-audit-follow-up/tasks.md, docs/specs/AIU-045-ui-audit-follow-up/analysis-2026-10-05.md, docs/specs/AIU-045-ui-audit-follow-up/verification.md, docs/decisions/pending.md, AGENTS.md, docs/specs/AIU-038-ledger-presentation/spec.md, docs/specs/AIU-039-multi-account-ledger/verification.md, docs/specs/AIU-043-file-logging/verification.md, docs/specs/AIU-044-account-monetary-usage/verification.md, docs/specs/AIU-014-preview-updates/verification.md]
- shared: []
- parallel: true
- isolation: required
- agent: primary
- acceptance: AC-01, AC-04, AC-05
- evidence: docs/specs/AIU-045-ui-audit-follow-up/analysis-2026-10-05.md (section "Final status (2026-10-06, before the owner checkpoint)") and docs/specs/AIU-045-ui-audit-follow-up/verification.md (section "Post-install verification 2026-10-06"; integrated `4ea9667` and the task N records commit)

Closes AUD-05..AUD-10 (opt-in follow-ups and the frozen historical record), ANL-06 (records), ANL-12, ANL-13 (records), ANL-14, ANL-15, ANL-18, ANL-19 and the ANL-20 deferral note under D4, D5, D6 and D8. Worker K writes the closing records in a docs-only worktree while the primary performs the confirmed D5 cleanup and the ANL-19 renormalization (no tracked change). Worker N records the owner's installed-build results after the checkpoint.

In progress (2026-10-06). K's closing records are written: the final status table in the analysis record, the spec's owner checkpoint list, the backlog completion-note, the AIU-038 section 4.3 contract note and the AIU-044 AC-03 sentence (ANL-21, write-set extended by primary ruling), and the removal of the AGENTS.md Codex subagent policy section; no `pending.md` entry, because no decision is open. The primary's D5 cleanup and the ANL-19 renormalization are done (no tracked change). T-04 stayed in-progress until T-03 was done.

Done (2026-10-06, integrated `4ea9667` and the task N records commit). Task N recorded the owner's installed-build results (verification record, section "Post-install verification 2026-10-06"), the final statuses in the analysis record, the AIU-014 dispatch and install line and bug-note resolution, and the three D6 records' results, and closed AIU-045.

## Handoff

Base: `6161e3b` (= `origin/main` at kickoff, clean). Kickoff answers (2026-10-06): desktop lane unlocked; D5 cleanup confirmed as listed; AGENTS.md Codex section removal authorized; no consent to read installed-app logs (ANL-16 stays pending). Run ledger: `.ai-usage-local/AIU-045/run-2026-10-06/ledger.md` (git-ignored). Exact next action (updated 2026-10-06 after the Preview 2026.10.602.0 installation): none for AIU-045; opt-in follow-ups (AUD-05..AUD-09, ANL-20, ANL-23, the AIU-043 device-code delay) need a separate owner selection.
