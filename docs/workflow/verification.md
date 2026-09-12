# Verification policy

## Layers
Fast deterministic PR checks: build, owned-code analyzers/format, unit tests, provider golden+targeted semantics, relevant integration/security/lifecycle tests. No live provider requirement for ordinary PR CI.
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

## Bootstrap checks
AIU-001 has its own verification.md starting entirely NOT_RUN. This document packet is only structurally validated in this environment; OMP runtime, Windows app, auth, package, permissions, tests, CI and remote config are not run here.
