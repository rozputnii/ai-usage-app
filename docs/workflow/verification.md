# Verification policy

## Layers
Current development PR checks (owner amendment, 2026-09-13): validator regressions including compilation/owned-code analyzers, canonical document validation and workflow/actual-patch regressions. Formatting is local-only; cancel obsolete runs of the same PR. Keep relevant behavior/security checks as features are added. Ordinary PRs do not require a full independent review; primary diff/acceptance review remains, with focused independent review for material auth, secret, destructive data-lifecycle or privilege changes. No live provider requirement for ordinary PR CI.
Windows UI tests: xUnit v3 + FlaUI UIA3, critical launch/navigation/settings/tray/activation only; run where an interactive Windows desktop really exists. A hosted runner label alone is not proof UI automation works. Document NOT RUN when environment unavailable; don't mark all tests green.
Live smoke: explicit local existing credentials or dedicated safe test account. Never personal credentials in CI. Provider availability/auth consent remains external. Source-verified fixture alone is not live verified integration.

## Output
Each required AC has verdict PASS/FAIL/NOT_RUN/BLOCKED with command/check ID, observed result, relevant environment, timestamp and code ref. Screenshots are evidence only when captured from actual build. Tool claims and generated reports are not test execution.

## Golden fixtures
Sanitized input + normalized expected output, unknown/missing/legacy/new grouping/null/unlimited/exhausted/reset/credit cases. Critical independent assertions prevent both parser and expected JSON drifting together. Fixture update reason/source tracked, never blanket approve snapshots to get green.

## Lifecycle/performance
Historical PUBLIC schema/layout versions retained as sanitized fixtures, not every identical Preview build. Test skipped-version upgrades and crash fault-injection at boundaries. Real package install/update/reset proof separate from DB fixture tests. Performance target measured on described reference Windows Release conditions, cold vs warm separated; no universal 500ms guarantee.

## Release decision
One-shot independent reviewer after integration + evidence; no forced findings. PASS requires acceptance coverage, actual CI and no unresolved material defect. Local fix gets specific regression test + impacted checks, not recurring full-review prompts. Minor improvements backlog. New unknown can be BLOCKED, not invented success.

Main protection is low-priority deferred work in AIU-026, not a prerequisite to current development PRs. This temporary simplification does not waive public release approval, protected signing/manifest operations or actual release evidence. Existing bootstrap reports remain historical records of their frozen references.

## Bootstrap checks
AIU-001 records actual native OMP, validator, isolation, pause and fresh-session results in [its verification report](../specs/AIU-001-omp-bootstrap/verification.md). Local bootstrap acceptance is not release approval. Windows product UI, packaging, provider-product authentication, GitHub CI and remote publication remain NOT_RUN; absent protection/reporting configuration is explicitly recorded.
