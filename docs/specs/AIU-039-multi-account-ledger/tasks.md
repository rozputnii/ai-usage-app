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
- [x] Inspect diff and commit/push a coherent WIP save point.

### T-02 - Protected registry and account workflow
- status: done
- depends_on: [T-01]
- acceptance: AC-01, AC-02, AC-04, AC-05, AC-10
- evidence: docs/specs/AIU-039-multi-account-ledger/verification.md

- [x] Add Core/Accounts/IAccountService.cs: credential-free account snapshots and
  initialize, connect/reconnect, refresh, cancel, code submission, disconnect and stop.
- [x] Add AccountRegistry using the existing protected state lease, with stable account
  references, private provider bindings and storage references. Persist pending admission
  and retired grant cleanup so restart never guesses which grant belongs to an account.
- [x] Add AccountService; tests assert duplicate rejection, reconnect identity matching,
  two-account independence, signed-out identity reuse, cancellation and shutdown drain.
- [x] Test registry corruption/newer version, redirected paths and staged-write interruption;
  failure must preserve recoverable data and keep private values out of generic diagnostics.
- [x] Run relevant regression suites, inspect and save.

### T-03 - Forward migration and persisted preferences
- status: done
- depends_on: [T-02]
- acceptance: AC-03, AC-05, AC-10
- evidence: docs/specs/AIU-039-multi-account-ledger/verification.md

- [x] Extend startup maintenance to layout 2 before account adoption, with old-writer
  refusal and resumable registration of existing grants in place (no credential copies).
- [x] Test legacy v1/v2 providers, interrupted grant lineage, every migration boundary,
  restart idempotence, unknown files and a historical account replacement.
- [x] Migrate grants and identity-bound cache; retain provider-keyed history/caps/preferences
  unassigned. Add account-keyed labels/order and Ledger preferences with compatible globals.
- [x] Preserve the prior preferences-only checkpoint meaning and provider-specific recovery.
- [x] Run migration and persistence regressions, inspect and save.

### T-04 - Live budget, history and Ledger contract
- status: done
- depends_on: [T-03]
- acceptance: AC-06, AC-07, AC-10
- evidence: docs/specs/AIU-039-multi-account-ledger/verification.md

- [x] Add Adapters/Live/LiveLedgerSource and a focused projection helper consuming actual
  LimitFacts, BudgetEngine, ReadingCalculations, SessionEstimator and ExtraUsageEvidence.
- [x] Add parser-to-Ledger fixture tests for design figures, scoped limits, unknown/zero/
  unlimited, stale data, capped balances, currency mismatch, day off, Work today and rush.
- [x] Test account-keyed capture and 35-day history gaps; cached startup and time changes
  never append readings, and failed capture does not claim completeness.
- [x] Implement settings/caps/order/history commands with persistence and midnight semantics.
- [x] Extend LedgerContract and demo source with selected-account reconnect, refresh and
  typed transient challenges/failures; update view-model tests and strip controls together.
- [x] Run relevant suites, inspect and save.

### T-05 - Product switch, lifecycle and retained surfaces
- status: done
- depends_on: [T-04]
- acceptance: AC-08, AC-09
- evidence: docs/specs/AIU-039-multi-account-ledger/verification.md

- [x] Switch live/default demo composition, startup, tray and refresh lifetime to Ledger;
  test shutdown, close-to-tray and opening the exact account from its tray row.
- [x] Preserve recovery and diagnostics inline; implement the resolved deletion scope and
  keep other deferred controls explicitly unavailable.
- [x] Remove retired UI/view-model/pace consumers and port relevant behavioral tests;
  retain AIU-040 provider-history transport for its separate task.
- [x] Run Infrastructure/Presentation regressions, document validation, Windows/package
  builds and ordinary local unpackaged interactive smoke using isolated synthetic state.
- [x] Inspect and save actual evidence, including NOT_RUN/BLOCKED limitations.

### T-06 - Independent review and authorized Windows/live acceptance
- status: in-progress
- depends_on: [T-05]
- acceptance: AC-09, AC-10, AC-11
- evidence: docs/specs/AIU-039-multi-account-ledger/verification.md

- [x] Freeze the integrated diff and request focused read-only independent review using
  convergence-review; correct material findings and run targeted regressions.
- [x] Obtain current authorization for owner-led two-account Claude checks.
- [x] Execute owner-led Claude admission, refresh, restart and selected sign-out/reconnect.
- [ ] Record remaining provider-failure and installed-package acceptance; obtain separate
  authorization before credential-bearing migration/package install/update/recovery tests.
- [x] Record actual per-AC verdicts; unavailable live/package evidence remains NOT_RUN or
  BLOCKED. Update canonical completion only when the required outcome is established.
- [x] Commit/push the verified implementation save point and report remaining acceptance limitations.

## Handoff

Exact next action: prepare the remaining installed-package acceptance against the built
2026.10.356.0 artifact and identify the specific isolated install/update check for owner
authorization. Live two-account admission, refresh, restart and selected sign-out/reconnect
passed; induced live provider failures remain NOT_RUN.
The owner authorized this live check on 2026-10-03 and performs browser sign-in personally.
Implementation, synthetic regressions, ordinary unpackaged Windows interactions and
independent review corrections are recorded in verification.md. Installed package activation,
update and real-account migration remain NOT_RUN; do not mark the feature complete.
All three scope decisions are resolved; PD-039-03 explicitly includes full local deletion.

Ruling: retain existing grant files in place during adoption, tracked by a protected
legacy-location flag, rather than copying them to new directories. This removes extra
secret checkpoints and cross-file cutover complexity; new accounts/reconnects use isolated
GUID directories. Layout 2 blocks older writers before registration. Migration tests
assert unchanged grant bytes, stable references on retry and separate legacy history.
