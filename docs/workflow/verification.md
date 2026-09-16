# Verification policy

Follow [CONTRIBUTING](../../CONTRIBUTING.md) for the procedure and review requirements; use the [local commands](../../README.md#local-checks). CI runs validator regressions, document validation, deterministic product regressions on Windows, unsigned package and routing builds, and smoke-harness publication. It does not claim interactive UI or live-provider execution.

## Checks by change

Select all applicable rows for the requested change. Feature acceptance criteria and release requirements still apply; this matrix does not waive them. Use the commands in README.md. After required checks pass, repeat or broaden them only for new changes, failures or unresolved concerns.

| Change | Required local verification |
| --- | --- |
| Documentation or agent instructions only | Document validation and diff check; inspect links and instruction conflicts. |
| Validator implementation or document-contract behavior | Validator regressions, document validation, and diff check. |
| Core, provider, persistence, or presentation behavior | Infrastructure and presentation regression suites, applicable document validation, and diff check. |
| Windows UI, activation, tray, or lifetime | Relevant regressions, package build, and applicable actual Windows smoke scenarios. |
| Authentication or durable-state boundaries | Relevant regressions plus focused independent review; live checks only when required and authorized. |

## Development environment

Owner direction (2026-09-16): default to local unpackaged Windows run/debug, local regression tests and local interactive UI smoke for routine development, including provider integration. Do not launch Windows Sandbox or provision a disposable VM merely because a change affects the UI or providers.

Use Windows Sandbox or a disposable VM only when the specific check requires isolation or a clean machine, such as installation prerequisites, package install/update/uninstall, recovery with destructive fault injection, or changes to certificate trust. State the concrete reason before using it. Run those checks when the affected behavior requires them; they are not automatically deferred to final release.

Required MSIX build validation remains applicable and does not require installing the package or starting a guest. Local unpackaged UI evidence does not establish package installation, packaged activation or update behavior. Keep development data isolated from installed-app data and preserve existing credentials. Host installation, trust changes and live authentication retain their existing authorization boundaries. This policy concerns Windows test environments, not Codex execution permissions.

## Layers
Windows UI tests: xUnit v3 + FlaUI UIA3, critical launch/navigation/settings/tray/activation only; run where an interactive Windows desktop really exists. A hosted runner label alone is not proof UI automation works. Document NOT RUN when environment unavailable; don't mark all tests green.
Live smoke: explicit local existing credentials or dedicated safe test account. Never personal credentials in CI. Provider availability/auth consent remains external. Source-verified fixture alone is not live verified integration.

## Output
Each required AC has verdict PASS/FAIL/NOT_RUN/BLOCKED with command/check ID, observed result, relevant environment, timestamp and code ref. Screenshots are evidence only when captured from actual build. Tool claims and generated reports are not test execution.

## Golden fixtures
Sanitized input + normalized expected output, unknown/missing/legacy/new grouping/null/unlimited/exhausted/reset/credit cases. Critical independent assertions prevent both parser and expected JSON drifting together. Fixture update reason/source tracked, never blanket approve snapshots to get green.

## Lifecycle/performance
Historical PUBLIC schema/layout versions retained as sanitized fixtures, not every identical Preview build. Test skipped-version upgrades and crash fault-injection at boundaries. Real package install/update/reset proof separate from DB fixture tests. Performance target measured on described reference Windows Release conditions, cold vs warm separated; no universal 500ms guarantee.

## Release evidence
Public release review follows CONTRIBUTING. Release acceptance needs actual CI and applicable interactive evidence, with no unresolved material defects. Missing evidence is NOT_RUN or BLOCKED. Deferred main protection in AIU-026 does not waive release authority or protected signing/manifest operations.

## Bootstrap checks
AIU-001 records actual native OMP, validator, isolation, pause and fresh-session results in [its verification report](../specs/AIU-001-omp-bootstrap/verification.md). Local bootstrap acceptance is not release approval. At that bootstrap reference Windows product UI, packaging, provider-product authentication, GitHub CI and remote publication were NOT_RUN; absent protection/reporting configuration is explicitly recorded.
