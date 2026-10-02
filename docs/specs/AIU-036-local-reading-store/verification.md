# AIU-036 verification

2026-10-02, local Windows. Base: aeaf11e.

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
- A fresh read-only Luna/max review is in progress against aeaf11e..014fc31. The primary
  cleanup finding and targeted fix were communicated for deduplication; no review result is
  inferred from the test pass.
