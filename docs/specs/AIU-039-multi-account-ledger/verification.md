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

The implementation and synthetic/unpackaged checks below cover AC-01 through AC-08.
AC-09 remains incomplete for installed-package execution, and AC-11 remains NOT_RUN
for owner-led live accounts. AC-10 review findings are corrected with targeted evidence.
The feature stays in progress; fixture, source and build evidence never establish live success.

| Acceptance | Current evidence and limits |
| --- | --- |
| AC-01/02/04 | PASS fixtures: isolated sessions for every provider, protected stable references, duplicate/wrong-account rejection, selected refresh/sign-out and cancellation drain. Second Claude demo account and tray selection pass on Windows. Live provider behavior NOT_RUN. |
| AC-03/05 | PASS synthetic migration/storage: shared v1/v2 provider regression suite, registry stage recovery, legacy adoption boundaries, interrupted maintenance upgrade, unassigned old data, labels/caps/reading persistence and preferences-only recovery. Real owned-state migration NOT_RUN. |
| AC-06/07 | PASS deterministic parser, budget, history, preference and command tests. Real-provider rendering is NOT_RUN. |
| AC-08 | PASS unpackaged Windows: launch, second demo account, history/settings, tray restore at account, exit, recovery, confirmed deletion and resumed deletion/restart. Interactive external sign-in challenges remain part of AC-11. |
| AC-09 | PASS automated regressions, validation, Windows and unsigned package builds. Installed activation/update/automatic refresh NOT_RUN. |
| AC-10 | Independent reviews performed. Original findings and targeted fixes recorded below; no unresolved material findings. Live privacy evidence NOT_RUN. |
| AC-11 | NOT_RUN: requires current owner authorization and owner-led sign-in to two actual Claude accounts. |

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

## Integrated lifecycle review and retirement, 2026-10-03

- Independent review (convergence-review, GPT-6 Astra low), frozen
  `37eac6d083bf1cd3db1c92b7a8ed053de51b5168` versus `d92ebef`: FAIL with two P2 findings.
  A bounded diagnostic Dispose did not prove writer termination; a faulted account startup
  task permanently faulted shutdown and prevented reset retries. Source inspection otherwise
  passed startup ordering, ownership, recovery routing and forward-fence retention.
- Corrections: deletion awaits FileDiagnostics.StopAsync; account shutdown observes completed
  task failures after all writers have drained, and Ledger still drains its preferences after
  completed rebuild/tick failures. Failed-initialization/reset regression reproduced RED,
  then targeted account/blocked-diagnostic-worker tests PASS 12/12 (8.558s).
- Full Infrastructure before these corrections PASS 637/637 (12.811s); later full run pending.
- Removed retired Features/Controls/Platform presentation, MainWindow, old live adapters,
  composition and QuotaPace-specific tests. Core/Infrastructure provider-history transport
  remains intact for AIU-040. Retained Ledger and workflow coverage; Presentation PASS
  111/111 (0.367s), including new cap-near/reached/over/provider-used-up, rush and stale-failure
  cases. Subsequent command-feedback changes require another relevant run.
- Correct local unpackaged Windows Debug build PASS, zero warnings/errors (19.20s).
  Earlier invocation omitted WindowsPackageType=None: compiled but could not activate
  unpackaged; this was a verification configuration error, not a passed UI check.
- Real isolated destructive Windows checks PASS 2/2 (6.419s): confirm reset and resume a
  pending intent on startup, remove synthetic invalid grant, preserve an unrelated export,
  native restart to the empty Ledger and clean exit. Local evidence: artifacts/AIU-039/ledger-smoke.
- Unsigned MSIX validation build PASS at local output AIU-039/packages/2026.10.350.0;
  SDK warning: mspdbcmf unavailable, so no symbols package. No owned-code warnings.
  This is a disposable validation artifact, not a publishable reserved release version;
  installed activation/update and live provider checks remain NOT_RUN.
- Recovery UI smoke initially failed on a disappearing UIA node after restore. The harness
  now retries transient COM failures while waiting; rerun pending. New tray check pending.

## Final local implementation evidence, 2026-10-03

- Full Infrastructure PASS 640/640 (39.555s), zero errors/failures/skips. Five additional
  interrupted-legacy-maintenance-to-layout-2 cases were then added; the targeted migration
  suite PASS 13/13 (3.766s), including unchanged protected grant bytes and idempotent adoption.
- Presentation PASS 114/114 (0.377s): added independent retry scheduling and stopped-tick
  behavior, learned five-hour figures with unknown remainder retained, observed extra-spend
  coverage and conservative missing-evidence behavior. Old-presentation-specific tests were
  retired, so this count is not a claim of unchanged test inventory.
- Windows Debug unpackaged build PASS with zero warnings/errors (54.70s). Unsigned package
  build PASS for reserved local validation version 2026.10.355.0, SHA-256
  7D56F3E71F03F6F548626E61E9B8E94BA2947DC02B3EAD1005063C6DE886EE2A.
  Only the SDK missing-symbol-tool warning remains; installation/signing NOT_RUN.
- Actual Windows Ledger suite PASS 4/4 (17.039s) on the rebuilt executable: live-empty and
  demo launch/settings/history, second same-provider account, close-to-tray and exact-account
  focus on restore, drained exit, confirmed full deletion and pending-intent restart.
  Harness corrections include awaiting replaced UIA cards and selecting only the launched
  process's visible tray popup. The owner's separately running earlier build was preserved.
- Actual recovery suite PASS 1/1 (9.073s): sharing-failure interruption, process restart,
  retry, corrupt preferences restored from the same checkpoint, newer-layout refusal and
  secret-free recovery-summary export. This is unpackaged fixture evidence, not installed update.
- A combined class/method filter selected zero UI tests; that invocation establishes no
  evidence and was replaced by the separate runs above.
- Generated evidence is ignored at artifacts/AIU-039. No real credential or CLI store was
  opened, and no install, trust modification, release or automatic external sign-in occurred.
- Final copy clarification describes retained/unassigned older history and lists preferences
  and logs in the deletion confirmation. It requires the final build/normal-settings recheck.
- Final copy build PASS: unpackaged Debug, zero warnings/errors (34.12s); unsigned MSIX
  2026.10.356.0, SHA-256 79AC563974F0FDDD70F27C1EAC4DA1730670C4AB1E3A624B01CE444F8DBEC2CA.
  The package was not installed or published.
- Final normal-settings/tray recheck PASS 2/2 (11.021s). Transient button/menu replacement
  failures were corrected by waiting for the named UI actions and scoping sign-in selection
  to the launched process. The test process now uses DPI-aware input/capture coordinates;
  no host display or accessibility settings were changed. Evidence: artifacts/AIU-039/final-copy-smoke.
- Final primary acceptance/diff review: no unresolved material review findings; all generated
  output excluded. Document validator PASS (valid true, no diagnostics); full feature diff
  whitespace check PASS. Completion remains blocked on the explicitly unperformed owner-led
  live and installed-package acceptance, not on an inferred implementation approval.

## Owner-led Claude acceptance started, 2026-10-03

- Owner explicitly authorized two-account Claude verification and retained browser
  sign-in. Code reference: 35ca7d9. Started the current unpackaged Debug build with a
  fresh, ignored, isolated development state directory; existing app state was preserved.
- PASS: normal empty Ledger activation and starting Claude browser sign-in; the app
  displayed its waiting state and manual-code fallback. Browser completion is pending.
- Two-account admission, independent refresh, reconnect and durable restart remain
  NOT_RUN until observed. No source CLI credentials were read or imported. Existing-state
  migration, package installation/update and trust changes are outside this authorization.
