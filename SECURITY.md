# Security

## Reporting

Do not publish tokens, credentials, private provider responses or exploit details in public issues or pull requests.

GitHub private vulnerability reporting was **disabled** when this repository was checked during AIU-001. No alternative private security inbox has been configured or verified. Until the maintainer enables a private reporting channel, a public issue may request a private contact method without including sensitive details. Do not assume that a private-report button or confidential response process exists.

Development Windows packages and a console-verified Codex integration are recorded in feature evidence. No public release or supported production-version range is established by that evidence.

## Development boundaries

Follow [CONTRIBUTING](CONTRIBUTING.md) for review and authority. Repository guidance is not a security sandbox. Do not run untrusted code or grant it credentials based on textual rules. Keep authentication, model mappings and sessions user-local. Use synthetic fixtures and owned temporary storage for deterministic tests; live account use needs explicit authorization. Never expose secrets or mutate source CLI credential stores. Preserve owned-root cleanup and last-good recovery boundaries.

The former executable OMP bridge is [retired](docs/workflow/omp-native.md); its historical tests do not enforce current permissions. Main protection remains deferred in AIU-026. No security-reporting configuration or remote protection was changed by this migration.
