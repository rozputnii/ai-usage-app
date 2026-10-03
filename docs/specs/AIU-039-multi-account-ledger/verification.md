# AIU-039 verification

## Preparation baseline

- Date: 2026-10-03 (Europe/Lisbon).
- Environment: local Windows repository, PowerShell; branch main.
- Base: `9131e4729ef11e075e37a177d910931134b48e72`.
- Change scope: draft specification, proposed design, handoff and backlog selection only.
- Source inspection: provider slots currently double as account references; budget series
  lack independent historical identity evidence; Ledger disables added providers and its
  sign-in contract lacks selected-account reconnect and authorization challenges.
- No credentials, provider-state files, local histories or CLI login stores were read.
- No product code, provider requests, live sign-in, UI launch or installation was performed.

## Checks

| Check | Verdict | Evidence |
| --- | --- | --- |
| Document validation | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`, exit 0, valid true, no diagnostics; existing user-local 10.0.401 SDK |
| Diff whitespace check | PASS | `git diff --check`, exit 0; staged new documents checked before commit |
| Primary specification/source review | PASS | Cross-checked spec/design against named source files; decisions remain explicitly pending |
| Infrastructure/Presentation regressions | NOT_RUN | No product change in this preparation step |
| Windows/package builds and interactive smoke | NOT_RUN | Product implementation has not started |
| Focused independent security review | NOT_RUN | Required for the eventual integrated implementation, not claimed from design inspection |
| Real two-account Claude and installed-package checks | NOT_RUN | No current live authorization; no product implementation |

## Acceptance

AC-01 through AC-11 remain incomplete. PD-039-01 and PD-039-02 are accepted by the owner;
PD-039-03 awaits clarification. No historical evidence is promoted to live Ledger evidence.

## T-01 account session construction, 2026-10-03

Added private verified-identity projections and an Infrastructure session factory selecting
an isolated GUID storage directory for each account. Product composition is not switched yet.
Six new tests check two independently protected accounts for each of the four providers,
restoration, selected sign-out, opaque context equality and invalid factory inputs.

- RED: focused MultiAccountSessionTests build failed because Infrastructure.Accounts did not exist.
- PASS: focused MultiAccountSessionTests, 6/6, using actual DPAPI stores and no provider requests.
- PASS: Infrastructure Release suite, 596/596, zero skipped/errors (14.607 seconds).
- PASS: Presentation Release suite, 264/264, zero skipped/errors (0.728 seconds).
- PASS: document validator, valid true with no diagnostics.
- NOT_RUN: UI, package, live authentication and independent security review; no product switch.

Commands used the existing user-local .NET 10.0.401 executable with `--no-restore` and the
README test projects. The focused command added `-class "*MultiAccountSessionTests"`.
These results cover T-01 only, not the account registry, migration or multi-account product.
