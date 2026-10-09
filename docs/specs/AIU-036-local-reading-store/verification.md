# AIU-036 verification

2026-10-02, local Windows. Base: aeaf11e. Final implementation: ba1bf87.

## Acceptance

| Check | Verdict | Evidence |
| --- | --- | --- |
| AC-01 refresh recording/drain | PASS | LiveReadingRecorderTests and production-recorder filesystem test |
| AC-02 persisted P01-P11 | PASS | PersistedBudgetCalculationTests, real writer/reader instances |
| AC-03 sessions/values | PASS | 12% paired-session sample cost, signed balance and money round trips |
| AC-04 configuration/retention on sign-out | PASS | Defaults/unmatched currency caps tests; disconnect exclusion; appearance reset owns a separate file |
| AC-05 safety/recovery/cleanup | PASS | Windows junction/held-file/capacity/recovery/cleanup tests; owner-approved store-only cleanup scope |
| AC-06 regressions and review | PASS | 416 Infrastructure + 179 Presentation tests; required review findings resolved below |

No live-provider success, installed-package update or complete product factory reset is
claimed. Compatibility keys stay explicitly `legacy-*-v1`; AIU-037 must map them to its
native limit keys, and AIU-039 supplies the new presentation. Source precision not exposed
by the current snapshot contract is not fabricated.

## Initial checkpoint

- NOT_RUN: implementation checks, full regressions and independent lifecycle review.
- NOT_RUN: live-provider and interactive Windows checks; no credentials read or sign-in started.
- Inspection: product delete-data and factory-reset services are currently unsupported.
  Owner selected stores and their cleanup only; UI wiring and complete factory reset are deferred.

## Development evidence

- PASS: 10 LocalBudgetStoreTests after missing-contract compilation failures, covering real
  filesystem round trips, gaps, period transitions, currency isolation, configuration,
  corruption/version recovery, cancellation, retention and selective cleanup.
- PASS: QuotaObservationRecorderTests, current normalized values only; no provider requests.
- PASS: 3 LiveReadingRecorderTests after the missing recorder integration was demonstrated:
  resume/manual/automatic refresh, cache/failure/sign-out exclusion, shutdown drain and storage
  failure classification. These use synthetic sessions, not live providers.
- PASS: document validation and diff whitespace check at the initial store checkpoint.
- NOT_RUN: independent lifecycle review and remaining safety/replay/full regression checks.

- PASS: 12 LocalBudgetStoreTests including two RED-to-GREEN fixes: a later derived start no
  longer invents a period restart; a 257th distinct series is refused while retained data stays.
- PASS: 11 PersistedBudgetCalculationTests covering P01-P11, three paired five-hour instances
  with a 12% weekly cost, plan-change invalidation, and signed-balance consumption/top-ups.
- PASS: 7 BudgetStorageSafetyTests: actual Windows junctions at root, lock, configuration and
  series paths; staged replace denied by a held file; bounded recovery with original bytes;
  oversized write and cross-process lease refusal. The held-file test initially expected
  IOException alone; Windows returned UnauthorizedAccessException. The assertion now accepts
  either denial and still checks unchanged committed bytes and removal of the stage.
- Lifecycle scope: sign-out never calls store cleanup. Appearance reset remains in its
  separate preference store. IBudgetDataCleanup removes these new stores only, after writers
  drain; future product delete/reset wiring remains outside the owner-selected scope.

## Integrated checks at 014fc31

- PASS: Infrastructure Release suite, 412/412; Presentation Release suite, 179/179. Commands
  from README.md, pinned .NET SDK, `--no-restore`, Windows; zero failures or skips.
- PASS: document validator (`valid: true`, zero diagnostics), `git diff --check`.
- PASS: unsigned MSIX build 2026.10.281.0, no restore, under
  `.ai-usage-local/AIU-036/packages`. SDK tooling reports the existing missing `mspdbcmf.exe`
  symbols-package warning; owned code produced no warning. This is not installation evidence.
- PASS: unpackaged Debug build, zero warnings/errors.
- PASS: actual Windows `CloseToTrayRestoresDashboard` smoke (1/1), product mode with a new
  empty `.ai-usage-local/AIU-036/smoke-state` root. Evidence in sibling `smoke-evidence`:
  close-to-tray.json reports passed, exited and exit code 0; restored screenshot inspected.
  No provider was connected. This proves startup/tray/exit composition, not live recording.
- NOT_RUN: live-provider reads, installed-package update/recovery. Neither is required for
  the additive noncredential store contract; installed/live presentation acceptance is AIU-039.

## Additional primary review

- Found selective cleanup could miss a quarantine just created during configuration loading,
  and did not reject orphan configuration stages. Both could retain old caps while returning
  success. Two regression cases failed first; cleanup now rescans and refuses before deleting
  account data when unparsed shared configuration exists. All 9 storage-safety tests pass.
- A fresh read-only Luna/max review covered aeaf11e..014fc31 under the model policy in force
  when dispatched. The unrelated 868da29 instruction/configuration change arrived afterward
  and was preserved; no additional reviewer was dispatched.

## Independent lifecycle review and disposition

Frozen-reference verdict: FAIL for one additional material finding. The reviewer also
independently confirmed the primary cleanup finding and accepted 2a46264 as its targeted
resolution. No credentials were read, no files changed by the reviewer, no live calls or
full suite reruns were performed by the reviewer. No deferred minor findings were reported.

- Finding: AppendAsync and SaveConfigurationAsync discarded the recovery signal, so the
  production recorder could restart history without an explicit result or safe notification.
- Fix in ba1bf87: Core StoreWrite reports Recovered; both write paths propagate the flag;
  recovery also emits BudgetStoreRecovered/InvalidData through the actual local diagnostic
  sink registered in production. No identifiers, paths, exception text or payload are logged.
- Verification: tests first failed for the missing write-result/diagnostic contract, then
  all 12 targeted storage-safety/recorder tests passed. The production-recorder test corrupts
  an actual file, records another successful synthetic quota, and checks the real sanitized
  diagnostic log; the write-result test covers both series and configuration recovery.
- Final: fixed recovery reporting; Infrastructure 416/416 and Presentation 179/179 PASS after
  the fix. Primary disposition: all material findings resolved, none outstanding. The frozen
  review FAIL is not relabelled PASS; no automatic full re-review was performed.
- PASS: final unsigned MSIX 2026.10.282.0 at ba1bf87, SHA-256
  C5592F32F86E2F7308355F8353393C3C1652379D9A67B4EFB2BF03206D9E2BFF. Same SDK symbols warning
  as the earlier build; no signing, installation, release dispatch or host trust change.
- PASS: final unpackaged Debug build at ba1bf87, zero warnings/errors. Repeated the one
  affected startup/tray/exit smoke after wiring the recovery diagnostic sink: 1/1 PASS,
  process 31452 exited with code 0. Evidence in `.ai-usage-local/AIU-036/smoke-final-evidence`,
  isolated empty `smoke-final-state`; restored screenshot inspected. No sign-in occurred.
- PASS: final document validator (`valid: true`, zero diagnostics) and whitespace check.

## Execution ledger

Collapsed from tasks.md on 2026-10-09 (OD-19); the full plan is in Git history at 85b6c09.

- T-01 Stores and observation contracts: done; commits not recorded (within aeaf11e..014fc31); review within the T-03 range review; checks targeted LocalBudgetStoreTests and QuotaObservationRecorderTests, C2, C6; grant owner-selected store and cleanup scope, 2026-10-02.
- T-02 Refresh recording and lifecycle: done; commits not recorded (within aeaf11e..014fc31); review within the T-03 range review; checks targeted LiveReadingRecorderTests; grant owner-selected store and cleanup scope, 2026-10-02.
- T-03 Calculation replay and integrated review: done; commits 014fc31, 2a46264, ba1bf87; review independent FAIL on aeaf11e..014fc31 with one material finding, resolved in ba1bf87 (primary disposition); checks C2, C4 (416/416), C5 (179/179), C6, unsigned MSIX build, Windows Debug unpackaged build, close-to-tray smoke; grant owner-selected store and cleanup scope, 2026-10-02.
