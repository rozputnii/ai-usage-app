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
PD-039-03 explicitly includes full local deletion. No historical evidence is promoted to live Ledger evidence.

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

## T-02 and partial T-03, 2026-10-03

- Registry tests: RED on absent implementation; PASS 8/8 after adding protected registry.
- Account workflow: RED on absent implementation; first five scenarios PASS; expanded
  tests reproduced failed-reconnect replacement and final busy-notification defects.
  Corrected targeted account suite PASS 9/9, including selected sign-out during refresh
  and identity substitution rejected before provider requests.
- Pre-review integrated regressions: Infrastructure 613/613, Presentation 264/264 PASS.
- Independent review: GPT-6 Astra low, read-only, frozen tree
  `fbc38c4c62b7d967e6366f145bd96b582f898915` versus `ddc81ea`, verdict FAIL with one P2:
  Codex cache lacked account binding and could be substituted across directories. No other
  material findings; reviewer did not run tests or inspect real data.
- Correction: swapped-cache test reproduced publishing the second account's quota as the
  first. Account-scoped version 3 cache now carries and checks its app storage reference;
  targeted suite PASS 7/7. Legacy cache records are not silently attributed.
- In-place legacy migration: RED on absent migration; PASS 7/7 across all provider stores,
  unchanged grant bytes, retained unassigned history, interrupted registration retry,
  corrupt-grant preservation and old-writer refusal after layout 2.
- New migration code is outside the earlier frozen independent review; review remains
  required for that later scope. Product composition, UI and live checks remain NOT_RUN.
- PASS: integrated Infrastructure after cache binding and migration, 621/621, zero
  errors/failures/skips (12.676 seconds); document validator and diff check PASS.

## T-03/T-04 adapter save point, 2026-10-03

- Independent follow-up review: GPT-6 Astra low, frozen `e7725d6` versus the prior
  review tree, PASS with no new material findings. Scope: in-place adoption, duplicate
  legacy-location rejection, layout 2 ordering and bound Codex v3 caches. Earlier P2
  addressed. Independent test execution NOT_RUN; immutable Git objects only.
- Preferences: separate account-keyed metadata, compatible global import only, protected
  refusal to overwrite invalid/newer files, extension-data roundtrip and deferred work days.
  Focused tests RED on missing implementation, then PASS 5/5.
- Ledger projection: Core budget/day-start/session/extra-usage calculations, conservative
  known-window pairing, account-scoped cards, unknown/zero/unlimited distinctions,
  currency mismatch, day off and 35-day history. Focused tests RED on missing implementation,
  then PASS 5/5. Initial fixture failures corrected to use explicit UTC midnight.
- Live source: capture only fresh successful readings, serialized commands and UI-dispatch
  publication, per-account refresh scheduling, persisted calendar/caps/order and typed
  transient sign-in challenges. Focused tests RED on missing implementation, PASS 3/3.
- Actual Claude/Codex/Copilot/Antigravity parsers through the same Ledger projection:
  PASS 4/4, synthetic payloads only. Browser-launch failure cleanup regression PASS 1/1
  against the existing provider failure handling; no credential workflow change needed.
- Presentation Release suite PASS 277/277, zero errors/failures/skips (0.762 seconds).
- Unpackaged Windows Debug x64 build PASS, zero warnings/errors (43.04 seconds).
- NOT_RUN: interactive Ledger smoke, package build/install and live providers. The live
  source is not activated in product composition yet. Full deletion, retained recovery/
  diagnostics surfaces and old-UI retirement remain implementation work.

## T-05 product/deletion save point, 2026-10-03

- Default live and demo startup now use Ledger with inline recovery/diagnostics and
  drained exit. New account composition does not register provider-keyed singletons.
- Full local deletion uses a durable intent, exclusive root/provider leases, known-file
  ownership, writer/log drain and retained layout-2 fence. Unknown data is preserved.
- Independent destructive-data review (convergence-review, GPT-6 Astra low), frozen
  `90480f15604ad028bbcbe0aed922d8ec03ac8a2a` versus `d92ebef`: FAIL, two findings.
  P1: provider artifacts enumerated before leases could leave a late grant behind.
  P2: rewriting a committed intent on retry could leave a truncated stage blocking resume.
  Both reproduced as failing regression tests, then corrected: enumerate after acquiring
  leases; preserve committed intent as authoritative. Targeted suite PASS 11/11 (1.429s).
  No real credentials were read. Later desktop integration requires its own focused review.
- Windows Debug x64 build PASS, zero warnings/errors (31.83s), before the subsequent
  visibility-diagnostic one-line fix; that fix remains pending rebuild.
- Actual isolated Windows smoke PASS 2/2 (10.417s): demo and live-empty launch, used/left,
  demo history and second Claude account, settings, deletion confirmation cancellation,
  diagnostic preview and drained Ctrl+Q exit. Initial harness failures were unsupported
  UIA names and hidden hover-only controls; corrected selectors and focus, then reran.
  Screenshots/results are local generated evidence under artifacts/AIU-039/ledger-smoke,
  excluded from publication. Actual destructive restart and close/restore tray NOT_RUN yet.
- Old presentation retirement, additional projection edges and packaged build remain open.
  Owner-led live account/migration and installed-package acceptance remain NOT_RUN.
