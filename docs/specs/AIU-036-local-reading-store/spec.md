---
id: AIU-036
type: feature
status: implementing
goal: G-003
scope_version: 1
approval_basis: Derived from the owner's current instruction to execute AIU-036 and the accepted AIU-034 research; no separate specification approval is claimed.
---

# Local reading series and budget configuration

Persist the observations needed by the AIU-035 day-start, tracked-consumption and session
calculations across restarts. Follow AIU-034 research 5.4 and 6.2-6.6. The same series will
feed inline history through AIU-038/039. No provider request, credential import, new grant,
parser revision or presentation redesign belongs to this item.

## Contracts

- Core owns credential-free observation, series and configuration contracts. Infrastructure
  owns versioned JSON files under the app-owned `budget` directory, independent of provider
  state, appearance and the existing preferences-only migration checkpoint.
- Reading keys contain the existing account target ID and opaque limit key. Disk names are
  hashes of structured keys, never provider strings or account IDs. Values are decimal counts
  or money minor units/exponent/currency; optional provider percentage is independent.
- Each series has one bounded file (16 MiB, at most 25,000 runs). Keep every run confirmed
  within the last 35 days, plus the preceding run for boundary calculations. Capacity exhaustion
  rejects the new write instead of silently discarding retained observations. Configuration is
  separately bounded to 256 KiB and 1,024 caps. No new package dependency is needed.
  The owned namespace additionally allows at most 256 distinct series, including quarantined
  ones, and 512 MiB including the staged replacement and recovery copies.
- Coalesce only the latest run, with equal values, plan, source/version and period instance,
  at most 15 minutes between confirmations. Reset jitter updates the open run's latest reset;
  transition detection uses AIU-035. Older/equal fetch times cannot rewrite history. A unit or
  currency change starts an isolated period so incomparable values never share a baseline.
- Preserve provider reset precision when supplied; no assumed reset is stored. Keep known
  duration-derived starts for the first observation. Runs carry observed period boundaries,
  not invented reset events. Balance increases are top-ups, never early replenishment.
- A compatibility recorder consumes successful, noncached existing product snapshots after
  connect/resume/manual/automatic refresh. It records only established values already exposed
  by current contracts. It retains versioned legacy keys where parser identity cannot yet meet
  AIU-037's native-key contract; AIU-037 must explicitly map these rather than silently merging
  incompatible series. No provider payload, account identity, label or credential is persisted.
- Budget configuration defaults to Monday-Friday and no caps. Persist work days and caps by
  target/limit, preserving unmatched caps and currency mismatches. The store does not apply or
  convert a cap; AIU-035 decides applicability and AIU-039 consumes configuration.
- Atomic staged replacement applies to each file only. Exact files and every directory
  ancestor are checked for reparse points before reads, writes, moves and deletion. A process
  lease serializes store operations. Cancellation or write failure leaves the prior file.
- A corrupt or unsupported-version file is moved aside with its original bytes, then starts
  empty/default; at most three quarantines per active file, after which recovery fails without
  overwriting evidence. Reads and writes return an explicit recovery result. Production
  recovery records the fixed BudgetStoreRecovered/InvalidData diagnostic with no identifiers,
  paths or payload. Version 1 is additive on older installs:
  missing files mean empty/default; existing appearance and credentials are not migrated.
- Sign-out and appearance reset retain both stores. Cleanup deletes only recognized owned
  files, including quarantines, after writers drain; unknown files or redirected paths fail
  closed. Account cleanup removes only that target's series/caps. Whole-store cleanup removes
  these stores. Owner clarification on 2026-10-02 limits this item to the stores and their
  cleanup; wiring product delete/reset buttons belongs to later presentation work. This is
  not complete product factory reset. Corrupt shared configuration may contain any target's
  caps: selective cleanup reports failure while preserving it; whole-store cleanup removes it.

## Acceptance

- AC-01: Fresh product refreshes record observations; failed/cached/startup-only reads and
  sign-out do not append or erase observations. Stop drains recording work.
- AC-02: Persisted runs preserve values, provenance, gaps and period transitions; P01-P11
  day-start/period cases give the expected results after opening a new store instance.
- AC-03: Session estimation from persisted paired readings reproduces a known session cost;
  unknown values, money and signed credit balances survive without conversion or zero filling.
- AC-04: Configuration round trips work days, unmatched caps and mismatched currencies;
  absent configuration means Monday-Friday. Neither sign-out nor appearance reset deletes it.
- AC-05: Retention, bounds, interrupted writes, corrupt/version-mismatched recovery and
  reparse protection pass isolated synthetic filesystem tests; cleanup cannot escape ownership.
- AC-06: Infrastructure and presentation regressions, document validation, diff review and
  focused independent security-lifecycle review pass. Live/provider/UI results are recorded
  separately and are not inferred from synthetic tests.
