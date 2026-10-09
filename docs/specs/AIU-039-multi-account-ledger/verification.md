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
AC-09 has installed update/recovery and synthetic automatic retry evidence; real-account
installed automatic refresh is NOT_RUN following the owner's refusal of duplicate sign-in.
AC-11 has live PASS evidence
for two-account admission, refresh, restart and selected sign-out/reconnect; induced
provider-failure injection is BLOCKED by automatic approval review. AC-10 findings have targeted correction evidence.
The feature is complete under spec scope version 2 with these coverage limitations;
fixture, source and build evidence never establish live success. Completion does not
change the failed harness invocations recorded below into PASS.

| Acceptance | Current evidence and limits |
| --- | --- |
| AC-01/02/04 | PASS fixtures: isolated sessions for every provider, protected stable references, duplicate/wrong-account rejection, selected refresh/sign-out and cancellation drain. PASS live Claude: two distinct accounts, refresh, restart and selected sign-out/reconnect with stable references. Live wrong-account/cancellation failures NOT_RUN. |
| AC-03/05 | PASS synthetic migration/storage: shared v1/v2 provider regression suite, registry stage recovery, legacy adoption boundaries, interrupted maintenance upgrade, unassigned old data, labels/caps/reading persistence and preferences-only recovery. Real owned-state migration NOT_RUN. |
| AC-06/07 | PASS deterministic parser, budget, history, preference and command tests. PASS live rendering of an exhausted subscription weekly limit and a separate second account's extra usage; other plan shapes NOT_RUN. |
| AC-08 | PASS unpackaged Windows: launch, second demo account, history/settings, tray restore at account, exit, recovery, confirmed deletion and resumed deletion/restart. Interactive external sign-in challenges remain part of AC-11. |
| AC-09 | PASS automated regressions, validation, Windows and unsigned package builds. PASS installed activation, same-family update, recovery and automatic offline retry/cache assertions with synthetic data. Real-account installed automatic refresh NOT_RUN; see final acceptance and harness limitation below. |
| AC-10 | Independent reviews performed. Original findings and targeted fixes recorded below; no unresolved material findings. Live privacy evidence NOT_RUN. |
| AC-11 | PASS live two-account admission, independent refresh, full restart, selected sign-out and reconnect without a duplicate. Live failure injection BLOCKED by automatic approval review; synthetic error coverage is separate. See the dated observations below. |

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

## Two-account Claude live observations, 2026-10-03

- Environment: the same isolated unpackaged Debug build at 35ca7d9, ordinary desktop;
  owner performed browser authentication. First and second Connect completed at 20:44
  and 20:46 Europe/Lisbon. No provider identifiers, grant contents or monetary values
  are retained in this report.
- PASS: both accounts appeared simultaneously with distinct stable opaque account/card
  references. The subscription account displayed an exhausted shared weekly limit,
  matching the owner's description; the second displayed a separate extra-usage card.
  This establishes these two accounts' observed shapes, not every Claude plan.
- PASS: F5 refresh at 20:47 produced two successful Refresh completions and preserved
  the separate cards. Clean Ctrl+Q process exit and launch with the same isolated state
  restored both accounts and their original references without another browser sign-in.
- PASS: selected Claude 2 sign-out at 20:49 completed, removed its current figures and
  retained its signed-out entry. The first account remained connected and its subsequent
  refresh completed successfully. The signed-out entry retained its original reference.
- PASS: selected Claude 2 reconnect completed at 20:50 following owner browser sign-in.
  Its original account/card reference returned without a duplicate, and the first account
  remained connected. Final F5 at 20:51 produced two successful Refresh completions.
- Induced provider-failure behavior and installed-package checks remain NOT_RUN.
  Successful quota refresh does not establish token renewal/rotation. The isolated
  instance is left open with both accounts connected for owner inspection.
- Evidence: observed Ledger UI and allowlisted timestamp/event/operation/outcome fields
  from only this isolated instance's diagnostic log. No raw response captures or grants
  were opened. UI helper menu-cache/geometry errors were recovered by fresh observation;
  an initial log date-filter type mismatch produced no evidence and was corrected by
  selecting completed operations directly.

## Owner-requested extra-usage display pause, 2026-10-03

- Owner requested hiding standalone extra-usage cards now and recording a future
  account-integrated redesign. Registered AIU-044 as an idea; no future work selected.
- Live projection skips CL-X cards while retaining transport, observations, caps and
  the owning account. An extra-usage-only account has a neutral account status card,
  no monetary figures and the existing sign-out action. Synthetic design-reference
  scenarios remain available; the new status is covered in their gallery.
- Regression RED: an account with weekly and extra-usage facts still had two cards.
  Presentation PASS 115/115 (0.415s), including enabled/disabled extra usage, an account
  with only extra usage and all gallery states. Updated gallery counts for the new state.
- Infrastructure run: 644/645 passed; the old parser-to-Ledger test expected the now-hidden
  money card. Updated it to assert preserved source money/null facts and absent card;
  targeted ParserLedgerTests PASS 4/4 (0.328s). No provider or persistence code changed.
- Unpackaged build PASS, zero warnings/errors (16.32s). Earlier build failed because
  the running verification app held its files; focused the app, exited cleanly and rebuilt.
- Actual updated Windows UI PASS: both existing accounts restored with their original
  references, the subscription weekly card remained, no separate Extra usage card was
  present, and the second account showed No subscription limits to display with Sign out
  available on hover. No account was signed out or deleted during this display check.
- Unsigned MSIX build PASS: 2026.10.357.0, SHA-256
  28C3CCFC99DB9AD5920076CF75559174CE4A4843961B4121A50AD19864110DDF.
  Only the existing SDK missing-symbol-tool warning; no installation or publication.
- Document validation PASS (valid true, no diagnostics); diff whitespace check PASS.
  Overall AIU-039 acceptance gaps remain unchanged.

## Final installed acceptance, 2026-10-03

- Owner asked to finish the remaining work. Product reference dcd9e52; the only new
  implementation changes in this step are the Windows acceptance harness and evidence.
  Full Infrastructure regression rerun PASS 645/645 (15.246s), resolving the earlier
  outdated parser expectation. Earlier Presentation 115/115 remains applicable.
- Environment: disposable Windows Sandbox, networking and clipboard disabled, only
  curated package/runtime/test input and an empty output directory mapped. No owner
  state or repository is mapped. All guest grants and readings are synthetic.
- Package copies 2026.9.2202.0 and 2026.10.357.0 were signed with a temporary development
  certificate. Its private key was removed after signing; trust was imported only in
  the guest. No host trust store, installed product, release or feed was changed.
- Prerequisite installer BLOCKED: the .NET MSI stalled in the first disposable guest.
  A fresh guest instead used the existing Microsoft .NET 10.0.12 runtime files;
  runtime inventory was verified. This does not establish installer success.
- Initial harness attempts FAIL: legacy Settings was a tab rather than a button,
  and the older window lacked current automation identifiers. Locators now support
  that historical UI while retaining native process ownership. Reinstallation after
  removal in the same guest failed with 0x80073CF9; final acceptance uses a fresh guest.
- PASS: actual old-package install and activation, loaded legacy Always on top,
  and clean exit (UpgradeRecovery old phase, 1/1, 5.734s).
- PASS: same-family installed update to 2026.10.357.0; unchanged synthetic protected
  grant and legacy preferences across package replacement.
- PASS: installed recovery (UpgradeRecovery new phase, 1/1, 12.142s): filesystem-sharing
  interruption, process restart and retry, corrupt-preference restoration from the
  original checkpoint, newer-layout refusal and secret-free recovery-summary export.
  Original grant bytes still decrypt to the synthetic fixture; preferences are preserved.
- Automatic offline retry occurred in the installed process at 20:41:05 and 20:51:04 UTC
  (599.933s apart), using the production timer with a synthetic Copilot grant and cached
  25-of-100 reading. No clock acceleration, manual refresh or product test switch was used.
  The combined test FAIL was its subsequent caption assertion: the historical cache lacks
  a period start, so the correct Not ready card displays "25 used", not "25 of 100".
  The installed recovery screenshot confirms the retained amount and sync-failed mark.
  Corrected the assertion and moved capture before it.
- Repeat functional assertions PASS: two automatic failures 599.910s apart; the cached
  "25 used" amount and sync-failed mark remain visible after the retry. Screenshot and
  functional result: evidence-03/automatic-recheck. The xUnit invocation still FAIL
  (603.659s) because its final keyboard cleanup could not focus the window
  (NoClickablePointException). This is not an all-green test invocation.
  Removed that cleanup's foreground dependency: the unattended synthetic-only timer
  scenario uses its existing finally-block process cleanup. Interactive drained exit
  remains covered by the passing recovery and ordinary shell tests. Harness publication
  PASS after this correction; a third full ten-minute timer run is NOT_RUN because the
  timer/cache assertions already passed and only post-assertion cleanup changed.
- Live Claude failure injection BLOCKED: automatic approval review rejected launching
  the verification app through a process-local proxy, stating only "blocked by policy".
  The proxy was never used by the app and was stopped; the live app was restored through
  ordinary launch. No alternative bypass was attempted. Offline synthetic behavior is
  separate evidence and cannot establish live Claude outage handling or token renewal.
- The backlog additionally requires automatic refresh and reading-series persistence
  for a real account in the installed package. This remains NOT_RUN. Prepared a separate
  network-enabled guest configuration with the same package and no mapped personal data;
  owner-led sign-in is required. The host permits only one Sandbox instance. Stopped
  the synthetic guest after collecting its results and started the prepared live guest.
- Live guest preparation PASS: installed 2026.10.357.0, observed the empty Ledger and
  Claude Sign in action, and asked the owner to perform browser sign-in. Existing host
  accounts remain connected in the minimized development instance. The installed real
  account check is waiting for owner input, not an inferred authentication success.
- Final document validation and diff check PASS. Canonical AIU-039 remains in progress.
- Local ignored evidence: .ai-usage-local/AIU-039/final-acceptance/evidence-03/run.
  Earlier environment/harness failures are retained in evidence and evidence-02.

## Owner declined duplicate authentication, 2026-10-03

- The owner declined another sign-in and pointed to the completed ordinary-mode checks.
  Removed the duplicate-authentication completion step under spec scope version 2.
  The separate installed real-account automatic refresh/history check remains NOT_RUN;
  no previous result has been relabeled as installed live evidence.
- Confirmed the empty live-check Sandbox was already closed: its ID was no longer
  available and the running-environment list was empty. No real grants were copied or imported; the
  existing ordinary-mode accounts and previously recorded live results are preserved.
- AIU-039 implementation is complete, with the documented live failure-injection BLOCKED
  result and harness cleanup limitation retained. Standalone extra usage stays hidden;
  its future account-integrated design remains unselected AIU-044.

## Post-done corrections (2026-10-06)

Recorded 2026-10-06 under AIU045-D6(a). This annotation does not reopen AIU-039, and its backlog status is unchanged. The synthetic UI audit that started from `383644c`, the source of the last published Preview `2026.10.404.0`, found the defects below in AIU-039 scope (AC-04, AC-06, AC-07 and AC-08, as mapped in ANL-06 of the AIU-045 [analysis record](../AIU-045-ui-audit-follow-up/analysis-2026-10-05.md)) after this task was done. As of 2026-10-06 the fixes are on `main`, but no published Preview contains them; shipping them is AIU-045 T-03. Defect descriptions and regression evidence are in the AIU-045 [verification record](../AIU-045-ui-audit-follow-up/verification.md) and the [audit report](../../archive/workflow/ui-ux-audit-2026-10-04/report.md).

| FIX | Defect | Fixing commit | Regression test added with the fix |
| --- | --- | --- | --- |
| FIX-01 | A successful empty provider reading rendered NotReady instead of NoDisplayedLimits. | `2942e6f` | `LiveLedgerProjectionTests.SuccessfulEmptyQuotaUsesTheSupportedNoDisplayedLimitsState` |
| FIX-02 | Reset, reading and failure timestamps displayed UTC clocks instead of the selected local timezone. | `2942e6f` | `LiveLedgerProjectionTests.TimestampResetAndReadingClocksUseTheSelectedLocalZone` |
| FIX-07 | Cancel was disabled during a retried sign-in. | `ed092f0` | `LiveLedgerSourceTests.RetryKeepsTheSharedStripCancelCommandAvailableWhileLoginIsPending` |
| FIX-08 | Workday accessible and control names stayed stale after toggling. | `ed092f0` | `LedgerInteractionTests.WorkDayNameNotifiesBindingsAndTheLastSelectionCannotBeRemoved` |
| FIX-09 | The final selected workday looked enabled while its click was silently rejected. | `ed092f0` | The FIX-08 test; the final native rerun is pending (AIU-045 T-03). |
| FIX-10 | Expired paired five-hour readings still appeared to be the current full window. | `2e50e69` | `LiveLedgerProjectionTests.ExpiredShortWindowDoesNotClaimCurrentUsageOrDiscardTheWeeklyBudget`; the final native rerun is pending (AIU-045 T-03). |
| FIX-13 | Open history did not refresh its readings or renamed account label. | `9e51fbb`, corrected in `2c0e52b` | `LiveLedgerSourceTests.OpenHistoryUpdatesAfterStoredReadingsChangeAndKeepsItsSelectedDay`; the `2c0e52b` pending-read navigation correction adds `AHistoryRefreshKeepsNavigationMadeWhileTheReadIsPending`. |
| FIX-14 | A pending history read could reopen the panel after it was closed. | `9e51fbb` | `LiveLedgerSourceTests.ClosingHistoryWhileItsReadIsPendingRejectsTheLateCompletion` |

The fixing commits are `[skip ci]` WIP save points in `git log 383644c..b84bf7c`. Each row's commit is the one that added its regression test and changed the fixed source file; the audit report gives no per-commit mapping. ANL-13 lists no never-accepted AIU-039 gap, and the gaps already recorded above are unchanged. The native smoke passes recorded above predate these changes and do not carry over to current `main` (ANL-07); AIU-045 T-03 reruns them.
