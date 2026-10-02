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
