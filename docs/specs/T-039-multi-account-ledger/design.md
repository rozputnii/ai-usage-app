---
id: T-039
type: design
status: implementing
goal: G-003
scope_version: 1
---

# Proposed implementation boundaries

Owner accepted the account/migration and presentation recommendations recorded in
[spec.md](spec.md), and explicitly included full local deletion under PD-039-03. This design does
not claim that live identity or migration tests have run. No new dependency or generic
persistence framework.

## Alternatives

1. **Recommended: app account registry plus existing per-account provider stores.**
   Allocate a random app-owned account reference, use it for an isolated storage directory
   and reading-series target, and construct the existing provider session for that account.
   Add one focused Infrastructure coordinator for registry/identity and sign-in admission.
   Preserve provider-specific refresh and rotation semantics. Cost: a small versioned
   registry and recoverable migration/admission ordering, both required by durable references.
2. **One file per provider containing all accounts.** Fewer root files, but every refresh
   rewrites a shared secret container and expands failure/locking scope. It replaces the
   existing credential lifecycle and makes independent account failures harder to contain.
3. **Use provider identity as storage/account ID.** Fewer mappings, but conflates external
   identity/context with application identity and complicates privacy and retained signed-out
   accounts. It contradicts the explicit stable app-owned reference requirement.

## Account and provider boundary

Core exposes account references, provider type, account session state and credential-free
operations. Windows never obtains a token or raw provider identity. Infrastructure owns
the provider-verified identity binding, a bounded versioned registry, session creation and
the account-to-storage mapping. The registry preserves signed-out identity bindings under
DPAPI and separates them from grants, so sign-out retains identity without retaining a
credential. Labels and compatible UI preferences use app references and existing owned
preference storage; no provider identity or user label becomes a path component.

Reuse the current provider clients and stores. Replace singleton-per-provider product
registration with account session creation/disposal owned by the coordinator. Avoid a DI
container per account and avoid a generic rewrite of the four provider sessions.

Identity comparison preserves the provider's existing binding: Claude account plus
organization; Codex verified account/workspace binding; Copilot authenticated numeric user
ID; Antigravity authenticated subject with its validated project context retained. Existing
provider records are evidence for these implementations, not a new source/live verification.
Do not deduplicate by email, display text, decoded unvalidated arbitrary tokens or quota
values. Missing/conflicting binding fails admission without touching a retained account.
If current binding evidence proves insufficient for a provider, record and resolve the
gap before claiming that provider's AC-02; never broaden provider APIs silently.

## Admission, reconnect and independent operations

Create an isolated candidate for each interactive login. Persist any returned rotating
grant safely in that candidate before cancellable quota work. Compare its verified binding
under the registry's mutation lease before making it a visible account. A connected duplicate
leaves the existing connection untouched; dispose and remove only the owned candidate.
A matching signed-out account reuses its original reference. Explicit reconnect also checks
the selected reference's binding, and a mismatch leaves both the selected and other accounts
unchanged. Admission/reference publication must be recoverable if the process exits between
writing a protected grant and committing registry metadata. Do not claim atomicity across
files or roll back a rotated server grant.

One active interactive login matches the one-strip UI. Each account retains independent
operation cancellation, failure/retry state and a session gate. Refresh-all visits active
accounts without turning one provider failure into a batch failure. Account sign-out cancels
and drains only that account, deletes its owned credential artifacts, then publishes signed-out
state. Shutdown drains all work. Use account/attempt generations to reject late publications.

## Forward migration and recovery

Extend the current startup maintenance sequence before provider refresh starts. A new layout
version must prevent older writers from reopening the legacy slots after cutover. Do not
reinterpret or rewrite the already-delivered preferences-only checkpoint as a grant backup.

Implementation refinement: adopt each existing provider store in place instead of copying grants.
This removes the proposed cross-file grant cutover and extra credential checkpoints entirely.
Under the existing exclusive root lease, publish layout 2 first so older writers refuse to run.
Resolve legacy pending generations using their existing policy; register each verified binding
once with a random app reference and an explicit protected legacy-location flag. Only one legacy
location is permitted per provider. Persist completion after all slots are registered. A retry
uses committed references; no credential is moved, copied or replayed by the account migration.
An account reconnect later publishes an isolated new storage reference before cleaning the old
legacy grant through its normal disconnect path. Corruption or uncertain rotation enters recovery.
The existing provider format checkpoints keep their current meaning and cleanup rules.

Codex account-scoped cache envelopes now include an app storage binding in version 3.
Reject unbound legacy or foreign-bound caches rather than attributing them to the grant;
the grant remains intact and a fresh successful quota fetch populates a bound cache.
No provider identity is added to the plaintext cache. The other three providers already
keep cache and verified identity together inside the protected record.

Budget series were keyed by provider slots and carry no proof of historical identity.
The PD-039-01 recommendation retains those bytes separately and excludes them from new-account
budget/estimator calculations. Current protected cache can migrate only with its bound grant.
Global compatible preferences can migrate; account-specific values without identity evidence
remain unassigned. Fixtures must include a prior identity replacement with retained history.

## Live Ledger adapter

Add an `ILedgerSource` implementation under `Adapters/Live`, composing the account service,
`IReadingSeriesStore`, `IBudgetConfigurationStore` and existing Core calculations. It publishes
immutable snapshots on the UI thread. Stable account-plus-limit keys identify cards, cap targets
and history; no parsing of provider/user display strings. Record each successful fresh quota
once per account before deriving a new view. Cached startup, repaint and time changes never append
observations. A recording failure is visible and logged safely; it does not claim a complete series.

Use `BudgetEngine`, `ReadingCalculations`, `SessionEstimator`, `ExtraUsageEvidence` and
`BudgetDisplay` for computations; the adapter groups known compatible windows and projects states
to the Ledger contract. Preserve standalone unknown/scoped limits. Do not invent five-hour pairing,
period duration, plan size, currency or baseline. Optional `UsualShare`, `DayOffPreview` and history
baseline fields may remain null when evidence does not support them, as the existing contract permits.
Map R-186/R-187 presentation precedence explicitly and test it; do not reuse retired QuotaPace.

Persist work-day changes effective next local midnight, while Work today expires at midnight
without modifying the work-day schedule. Rebuild on relevant time boundaries, preference edits
and new observations. Display-round through Core once. Keep at least 35 local calendar days in
history and mark missing observations as gaps. Existing bounded store limits and recovery remain
in force; do not add unbounded per-account capture or silently evict another account's history.

## UI switch and verification

Apply the PD-039-02 contract amendment to the live and demo sources, view models and tests together.
Enable another sign-in for an added provider, route card reconnect to its account, and show device
codes/manual code submission within the existing strip. Use typed failures for duplicate, wrong-account,
storage/recovery and ordinary provider failures. No credentials or challenge codes enter generic logs.
Preserve real recovery/status/diagnostics and indicate unavailable settings actions truthfully.

Switch composition, startup and tray lifetime to Ledger after the adapter is tested. Remove only
the retired views/models/pace code and tests specific to those retired behaviors; preserve useful
behavioral coverage in Ledger tests. T-040 transport cleanup stays a separate selected task.

Verification uses synthetic two-account fixtures for every provider, migration fault injection,
independent account lifecycle/rotation tests, real parser-to-engine-to-presentation examples,
ordinary unpackaged Windows smoke and package build. Follow with focused independent review
using the repository's required GPT-6 Astra low reviewer. Obtain current authorization separately
for two real Claude accounts and package installation/update/recovery checks that need it.
