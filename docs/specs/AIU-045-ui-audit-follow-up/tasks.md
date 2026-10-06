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
- writes: [.github/workflows/validation.yml, tools/windows/PreviewRelease.psm1, tools/windows/Publish-Preview.ps1, tests/release/Test-PreviewRelease.ps1, README.md, docs/specs/AIU-014-preview-updates/spec.md, docs/specs/AIU-014-preview-updates/verification.md, docs/specs/AIU-014-preview-updates/bug-2026-10-04-installer-connection-aborted.md, docs/decisions/accepted.md, tools/AiUsage.ProjectValidation/ProjectValidator.cs, tests/AiUsage.ProjectValidation.Tests/**, tests/windows/AiUsage.Infrastructure.Tests/DiagnosticCrashTests.cs, tests/windows/AiUsage.Infrastructure.Tests/FileDiagnosticsTests.cs, src/windows/AiUsage.Infrastructure/Diagnostics/**, tools/AiUsage.DiagnosticsProbe/**, CONTRIBUTING.md, docs/workflow/verification.md, docs/backlog.md, docs/specs/AIU-039-multi-account-ledger/verification.md, docs/specs/AIU-043-file-logging/verification.md, docs/specs/AIU-044-account-monetary-usage/verification.md, docs/specs/AIU-045-ui-audit-follow-up/verification.md, docs/workflow/ui-ux-audit-2026-10-04/report.md, docs/workflow/ui-ux-audit-2026-10-04/gallery.md]
- shared: []
- parallel: true
- isolation: required
- agent: primary
- acceptance: AC-05
- evidence: docs/specs/AIU-045-ui-audit-follow-up/verification.md (section "Fix run 2026-10-06"; integrated c26f18b and effcb4b; CI run 37416927644 green with preview skipped)

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
- evidence: docs/specs/AIU-045-ui-audit-follow-up/verification.md (section "Fix run 2026-10-06"; integrated c26f18b and 8cf68de; CI run 37416927644 green with preview skipped)

Closes AUD-01 (fix), AUD-02, AUD-04, ANL-09, ANL-11 and ANL-17 under AIU045-D2b and D3a. Worker briefs C (revert the scope-annotation seam, packaged gate, observable Open failure), E (omitted-property probes; no product code) and F (`aiu-simple-implementer`; audit tool scripts) run in isolated worktrees. If E reports a red probe, fixer I continues on E's branch and this write-set is amended with the named record files before I starts. Runs concurrently with T-01 by owner request; the integrated result is verified once at Gate 1 before anything is pushed.

### T-03 - Candidate verification and Preview
- status: pending
- depends_on: [T-01, T-02]
- ownership: Candidate verification on the serialized desktop lane and WIP diff dispositions
- writes: [docs/specs/AIU-045-ui-audit-follow-up/verification.md, docs/specs/AIU-045-ui-audit-follow-up/evidence/**, src/**, tests/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: AC-02, AC-04, AC-05
- evidence: not-run

Closes AUD-01 (AC-02 closure), AUD-03, ANL-04, ANL-07, ANL-10 and ANL-21; ships FIX-01..FIX-14 as a candidate. Reviewer R0 reads the WIP product diff (`383644c..BASE -- src`) and proposes keep/test/fix dispositions for the ANL-21 notes; fixer H implements the ruled items in a worktree. Worker J then runs alone in the main checkout as the sole writer of `src/` and `tests/`: Release build and SHA-256, host `--demo` smoke, native smoke methods, the D9 live-empty smoke and one Sandbox batch (D7a). The owner dispatches the Preview at the checkpoint; installed-build checks are recorded afterwards.

### T-04 - Records, process and cleanup
- status: pending
- depends_on: [T-03]
- ownership: Closing records, post-install records and local housekeeping
- writes: [docs/backlog.md, docs/specs/AIU-045-ui-audit-follow-up/spec.md, docs/specs/AIU-045-ui-audit-follow-up/tasks.md, docs/specs/AIU-045-ui-audit-follow-up/analysis-2026-10-05.md, docs/specs/AIU-045-ui-audit-follow-up/verification.md, docs/decisions/pending.md, AGENTS.md, docs/specs/AIU-039-multi-account-ledger/verification.md, docs/specs/AIU-043-file-logging/verification.md, docs/specs/AIU-044-account-monetary-usage/verification.md, docs/specs/AIU-014-preview-updates/verification.md]
- shared: []
- parallel: true
- isolation: required
- agent: primary
- acceptance: AC-01, AC-04, AC-05
- evidence: not-run

Closes AUD-05..AUD-10 (opt-in follow-ups and the frozen historical record), ANL-06 (records), ANL-12, ANL-13 (records), ANL-14, ANL-15, ANL-18, ANL-19 and the ANL-20 deferral note under D4, D5, D6 and D8. Worker K writes the closing records in a docs-only worktree while the primary performs the confirmed D5 cleanup and the ANL-19 renormalization (no tracked change). Worker N records the owner's installed-build results after the checkpoint.

## Handoff

Base: `6161e3b` (= `origin/main` at kickoff, clean). Kickoff answers (2026-10-06): desktop lane unlocked; D5 cleanup confirmed as listed; AGENTS.md Codex section removal authorized; no consent to read installed-app logs (ANL-16 stays pending). Run ledger: `.ai-usage-local/AIU-045/run-2026-10-06/ledger.md` (git-ignored). Exact next action: wave 1, dispatch briefs A..G and reviewer R0 in parallel, then Gate 1.
