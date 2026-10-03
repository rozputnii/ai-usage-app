---
id: AIU-039
schema_version: 1
---

# Multi-account Ledger implementation plan

Use executing-plans and test-driven-development. Primary implements sequentially on
main; CONTRIBUTING overrides generic skill branch/worktree and repeated approval gates.
Focused independent review uses one GPT-6 Astra low subagent after integrated changes.

Goal: isolated simultaneous provider accounts with durable state and live Ledger.
Architecture: existing provider sessions/stores, a protected account registry, a
credential-free account service and a Windows Ledger adapter. No new dependency.
Spec: [spec.md](spec.md). Design: [design.md](design.md).
Base: `ddc81ea`; original source baseline `9131e47`.

## Global constraints and review focus

- Never read real app/CLI credentials or run live sign-in without current authorization.
- Preserve provider rotation lineage; no predecessor grant replay after migration/recovery.
- Retain unassigned legacy history/caps; never join them to the current provider identity.
- Check wrong-account reconnect, duplicate candidate cleanup and canceled late completion.
- Preserve UI-thread publication and shutdown drain; do not widen logging with identities.
- No external packages, custom chrome or excluded accessibility/display matrices.
- PD-039-03 includes full local deletion, explicitly accepted by the owner.

### T-01 - Account-scoped provider sessions
- status: done
- depends_on: []
- acceptance: AC-01, AC-02, AC-04
- evidence: docs/specs/AIU-039-multi-account-ledger/verification.md

- [x] Add MultiAccountSessionTests with two independently protected stores per provider;
  assert verified bindings remain distinct after reload, refresh and selected disconnect.
- [x] Add an internal ProviderIdentity value and identity access on existing provider
  sessions, keeping raw provider identity outside Core and Windows contracts.
- [x] Add ProviderSessionFactory under Infrastructure/Accounts; Create(provider, storageId)
  accepts only app-generated GUID storage references and uses existing hardened clients.
- [x] Run focused tests (expect initial failure, then pass); run applicable regressions.
- [ ] Inspect diff and commit/push a coherent WIP save point.

### T-02 - Protected registry and account workflow
- status: in-progress
- depends_on: [T-01]
- acceptance: AC-01, AC-02, AC-04, AC-05, AC-10
- evidence: not-run

- [ ] Add Core/Accounts/IAccountService.cs: credential-free account snapshots and
  initialize, connect/reconnect, refresh, cancel, code submission, disconnect and stop.
- [ ] Add AccountRegistry using the existing protected state lease, with stable account
  references, private provider bindings and storage references. Persist pending admission
  and retired grant cleanup so restart never guesses which grant belongs to an account.
- [ ] Add AccountService; tests assert duplicate rejection, reconnect identity matching,
  two-account independence, signed-out identity reuse, cancellation and shutdown drain.
- [ ] Test registry corruption/newer version, redirected paths and staged-write interruption;
  failure must preserve recoverable data and keep private values out of generic diagnostics.
- [ ] Run relevant regression suites, inspect and save.

### T-03 - Forward migration and persisted preferences
- status: pending
- depends_on: [T-02]
- acceptance: AC-03, AC-05, AC-10
- evidence: not-run

- [x] Extend startup maintenance to layout 2 before account adoption, with old-writer
  refusal and resumable registration of existing grants in place (no credential copies).
- [ ] Test legacy v1/v2 providers, interrupted grant lineage, every migration boundary,
  restart idempotence, unknown files and a historical account replacement.
- [ ] Migrate grants and identity-bound cache; retain provider-keyed history/caps/preferences
  unassigned. Add account-keyed labels/order and Ledger preferences with compatible globals.
- [ ] Preserve the prior preferences-only checkpoint meaning and provider-specific recovery.
- [ ] Run migration and persistence regressions, inspect and save.

### T-04 - Live budget, history and Ledger contract
- status: pending
- depends_on: [T-03]
- acceptance: AC-06, AC-07, AC-10
- evidence: not-run

- [ ] Add Adapters/Live/LiveLedgerSource and a focused projection helper consuming actual
  LimitFacts, BudgetEngine, ReadingCalculations, SessionEstimator and ExtraUsageEvidence.
- [ ] Add parser-to-Ledger fixture tests for design figures, scoped limits, unknown/zero/
  unlimited, stale data, capped balances, currency mismatch, day off, Work today and rush.
- [ ] Test account-keyed capture and 35-day history gaps; cached startup and time changes
  never append readings, and failed capture does not claim completeness.
- [ ] Implement settings/caps/order/history commands with persistence and midnight semantics.
- [ ] Extend LedgerContract and demo source with selected-account reconnect, refresh and
  typed transient challenges/failures; update view-model tests and strip controls together.
- [ ] Run relevant suites, inspect and save.

### T-05 - Product switch, lifecycle and retained surfaces
- status: pending
- depends_on: [T-04]
- acceptance: AC-08, AC-09
- evidence: not-run

- [ ] Switch live/default demo composition, startup, tray and refresh lifetime to Ledger;
  test shutdown, close-to-tray and opening the exact account from its tray row.
- [ ] Preserve recovery and diagnostics inline; implement the resolved deletion scope and
  keep other deferred controls explicitly unavailable.
- [ ] Remove retired UI/view-model/pace consumers and port relevant behavioral tests;
  retain AIU-040 provider-history transport for its separate task.
- [ ] Run Infrastructure/Presentation regressions, document validation, Windows/package
  builds and ordinary local unpackaged interactive smoke using isolated synthetic state.
- [ ] Inspect and save actual evidence, including NOT_RUN/BLOCKED limitations.

### T-06 - Independent review and authorized Windows/live acceptance
- status: pending
- depends_on: [T-05]
- acceptance: AC-09, AC-10, AC-11
- evidence: not-run

- [ ] Freeze the integrated diff and request focused read-only independent review using
  convergence-review; correct material findings and run targeted regressions.
- [ ] Obtain current authorization for owner-led two-account Claude checks and any
  credential-bearing migration/package install/update/recovery tests; never infer it.
- [ ] Record actual per-AC verdicts; unavailable live/package evidence remains NOT_RUN or
  BLOCKED. Update canonical completion only when the required outcome is established.
- [ ] Commit/push final verified state and report remaining limitations.

## Handoff

Exact next action: verify and save the T-02 workflow and T-03 in-place migration,
then implement account-keyed Ledger preferences and the live budget projection.
All three decisions are resolved; PD-039-03 explicitly includes full local deletion.

Ruling: retain existing grant files in place during adoption, tracked by a protected
legacy-location flag, rather than copying them to new directories. This removes extra
secret checkpoints and cross-file cutover complexity; new accounts/reconnects use isolated
GUID directories. Layout 2 blocks older writers before registration. Migration tests
assert unchanged grant bytes, stable references on retry and separate legacy history.
