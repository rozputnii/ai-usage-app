---
id: AIU-039
type: feature
status: implementing
goal: G-003
scope_version: 1
approval_basis: Owner selected AIU-039 on 2026-10-03 and accepted legacy-data separation and Ledger contract additions. The subsequent explicit answer "Implement now in AIU-039" includes full local stored-data deletion. These decisions authorize sequential implementation within the existing architecture and design.
---

# Multiple accounts and live Ledger

## Outcome

Owner amendment, 2026-10-03: hide standalone Claude extra-usage cards in the live
Ledger because they look like separate accounts. Keep the owning account manageable
when it has no other displayed limits, without monetary figures. Preserve stored
readings/caps and provider transport. Account-integrated extra-usage presentation is
deferred to AIU-044; synthetic design-reference scenarios remain available for development.

Connect multiple distinct accounts of the same provider concurrently and make Ledger
the product interface. Every account has a stable app-owned reference, independent
credentials, cache, label, limits, caps, history and actions in the main window and
tray. Adding an account never replaces an existing connection. Core owns credential-free
contracts and calculations, Infrastructure owns identity verification and durable state,
and Windows adapts their results to the existing Ledger presentation.

Sources: [backlog](../../backlog.md), [AIU-038 contract](../AIU-038-ledger-presentation/spec.md),
[AIU-035 engine](../AIU-035-core-limit-budget/spec.md),
[AIU-036 storage](../AIU-036-local-reading-store/spec.md),
[AIU-037 parser facts](../AIU-037-provider-limits-v2/spec.md), and
[security lifecycle](../../platforms/windows/security-and-lifecycle.md).

## Scope and constraints

- Support Claude, Codex, Copilot and Antigravity through their existing transport and
  authorization implementations. No new provider endpoint, scope, OAuth registration,
  external identity service or dependency is proposed.
- Use opaque app-owned account references, distinct from provider identity, email,
  display name and credentials. Keep provider identity/context binding inside
  Infrastructure. Duplicate matching uses the provider-verified binding, never labels.
- A reconnect must prove the selected account's binding before replacing its grant.
  A different identity is rejected without rebinding that account's history or caps.
  Adding an already-connected identity reports a duplicate without replacing its grant;
  adding a retained signed-out identity restores its existing account reference.
- Serialize interactive sign-in through the existing single strip; account refresh and
  failure remain independent. Cancellation targets the active attempt, and shutdown or
  sign-out drains the selected account's work before disposal or credential removal.
- Preserve the existing provider grant-rotation safeguards, DPAPI CurrentUser boundary,
  state leases, reparse guards and fail-closed recovery. Do not restore predecessor grants
  after a token rotation. App startup migration is local and makes no provider request.
- Retain identity, names, order, history and caps on sign-out (D-093/D-094). Hide signed-out
  accounts by default. Unknown or incompatible data is preserved, not silently reassigned.
- Feed the engine from normalized parser facts and compatible account-owned reading series.
  Keep unknown, unlimited, zero, stale, assumed periods and tracking estimates explicit.
  History is local, at least 35 days with gaps; historical provider retrieval removal remains
  AIU-040. Do not invoke that retrieval from Ledger.
- Reuse the Ledger visual hierarchy, inline editors/history/settings and native chrome.
  Preserve available recovery and diagnostic operations during the switch. Do not enable
  deferred notifications, updates, CLI import or data-management operations as fake successes.
- Normal product launch and `--demo` use Ledger after the completed switch. Keep
  `--demo --ledger` compatible. Remove the retired D-180/D-181 main/detail/history views,
  their unused view models and old pace calculation after their consumers are replaced.
  Retain shared types still required by supported recovery, diagnostics or AIU-040.
- Follow ordinary desktop checks from AGENTS.md. Screen readers, contrast themes,
  extreme zoom/DPI and unusual display configurations are excluded. Package builds remain
  required; actual install/update/recovery evidence is separate from unpackaged smoke.
- No real sign-in, credential-bearing state migration, source CLI read/import, installation,
  host trust change, release or workflow dispatch is authorized by this specification.
  Live checks need current owner authorization and owner-led sign-in.

## Resolved scope decisions

### PD-039-01 - Legacy data attribution

Resolved 2026-10-03: owner accepted retention without automatic attribution. The
recommendation below is the implementation rule; no manual assignment UI is added.

Observed: `LiveMapping.Map` uses the provider name as account ID; `LiveUsageSource`
passes that same target to `QuotaObservationRecorder`. Reading series/caps have an
account-target field but no independently verified identity binding. Reconnecting can
replace the provider-slot identity while the budget namespace survives sign-out.
The currently stored grant therefore does not prove the owner of all legacy readings.

Recommendation: preserve provider-keyed history, caps and account preferences as
unassigned legacy data, without attaching them to a newly generated account reference.
Migrate the protected current grant and its identity-bound cache to that account; start
new account-bound capture. Keep legacy caps visibly unmatched and removable, and clearly
report that previous history remains retained but cannot be safely attributed. Preserve
global appearance/work-day preferences where their semantics remain compatible.

Alternative: add an owner-driven assignment workflow after migration. That needs an
additional product flow and explicit confirmation of what data is being assigned;
it cannot establish that a historically mixed series belongs to a single account.
No automatic attribution based only on the current login is acceptable.

Needed before storage migration implementation; affects AC-03 and AC-05.

### PD-039-02 - Presentation contract and retirement agreement

Resolved 2026-10-03: owner accepted the described Ledger additions with the existing
design retained. Direct owner agreement governs the coordinated replacement, without
messaging another chat or claiming another author's review.

The existing provider -> account -> limits shape is reusable. Its source currently
accepts only a provider for sign-in, and its strip has no authorization challenge.
`ProviderItem.IsEnabled` also disables providers already added. These are concrete
gaps for account-targeted reconnect and the existing Copilot device-code/Claude manual
code flows, not a request to redesign the page.

Recommendation: amend the AIU-038 contract in place with the owner agreement recorded
in both task records. Add an explicit account-targeted reconnect operation; carry the
active attempt reference and typed transient challenge/failure information in the strip;
submit manual codes to that attempt only. Keep authorization URLs/codes in memory,
out of preferences, snapshots persisted to disk, logs and diagnostics. Allow adding
another account of an already-added provider; only an active sign-in prevents another
interactive attempt. Retry retains its add/reconnect target. Account refresh remains
available through an account-targeted source operation without adding tray buttons.

Also agree the replacement of the old main/detail/history views and pace code after
Ledger has their supported operations. The old provider-history transport is not removed
here. No other chat has been messaged and no AIU-038 author agreement is presumed.

Needed before contract/UI edits and final retirement; affects AC-01, AC-04, AC-07, AC-08.

### PD-039-03 - Previously deferred data deletion

Resolved 2026-10-03: owner explicitly selected implementation now in AIU-039. Whole
local stored-data deletion is included, with inline Confirm/Cancel, shutdown of writers,
owned-root cleanup, interruption recovery and focused independent review. This changes
the earlier deferral; the recommendation below is retained as decision context only.

AIU-036 explicitly deferred product delete/reset wiring. Ledger's demo implements a
Delete stored data command, while the live product does not currently implement it.

Recommendation: keep this operation visibly unavailable in AIU-039, with truthful inline
feedback, retaining the confirmation design for later implementation. Sign-out, cap
removal and unmatched-cap removal are in scope. Alternative: explicitly include whole
owned-data deletion here, with writer shutdown, grant/checkpoint cleanup, retained-legacy
cleanup, interruption recovery and focused independent review. This alternative adds a
destructive lifecycle beyond the multi-account migration and requires a scope amendment.

Needed before data-settings wiring; affects AC-08 and AC-10.

## Acceptance criteria

- AC-01: Two distinct accounts of each provider coexist under synthetic fixtures and
  appear independently in Ledger and the tray. Adding the second does not sign out,
  overwrite, rename or change the first. No provider-wide occupied-slot restriction remains.
- AC-02: App account references survive restart, rename and reauthentication. Duplicate
  detection uses verified provider identity/context. Wrong-account reconnect and a
  cross-account grant/cache mismatch fail closed without changing either account's data.
- AC-03: Fresh, v1 and v2 single-slot fixtures forward-migrate under an exclusive lease.
  Protected grants and identity-bound cache survive. Tests inject interruption at each
  checkpoint, stage, reference publication and cleanup boundary. Retry is idempotent;
  newer/corrupt layouts and uncertain rotation lineage enter recovery, never auto-wipe.
  Legacy attribution follows the resolved PD-039-01 without mixing account histories.
- AC-04: Connect, refresh, renewal, cancellation, reconnect and sign-out target their
  selected account or attempt. Independent failure, rate limiting and expired grants do
  not block another account. Late completion cannot republish signed-out state. Exit
  drains writers and rotating-grant promotion before disposal.
- AC-05: Labels, order, supported preferences, grants, cache, caps and new history persist
  independently across restart. Sign-out removes only the selected grant artifacts and
  retains its identity/history/caps. Reconnecting that identity restores the same reference.
- AC-06: Adapter fixtures exercise actual parsers, reading-series calculations and budget
  engine through the Ledger contract, reproducing applicable AIU-034 section 4 figures
  and D-186/D-187 amendments. Test unknown/unlimited/zero, provider-used-up versus cap,
  currency mismatch, stale failure, work-day changes, midnight, Work today, day off, rush,
  five-hour estimates and observed extra usage. Cached startup is not a new observation.
- AC-07: Live source supports inline rename, cap editing/removal, unmatched caps, work days,
  Work today, undo-compatible commands, order, preferences and local history. State and
  figures are computed outside view models; Features/Ledger stays independent of Core
  and Infrastructure. Same-provider account/card identifiers never collide.
- AC-08: Product and demo use Ledger; ordinary main-window, inline settings/history,
  tray-open-at-account, close-to-tray and Exit interactions pass actual Windows smoke.
  Sign-in challenges and errors are usable inline. Existing recovery and diagnostic
  functions remain reachable; unsupported operations are visibly unavailable. Retired
  D-180/D-181 UI/pace code is removed after PD-039-02 agreement, not left as a parallel UI.
- AC-09: Infrastructure and Presentation regressions, document validation, diff checks,
  Windows build and required package build pass. Installed automatic refresh, install,
  update and recovery evidence is recorded individually; missing execution is not PASS.
- AC-10: Focused independent review covers identity isolation, DPAPI state, rotation,
  migration/recovery, sign-out cleanup and diagnostic privacy. Resolve material findings
  with targeted regression checks. Logs identify safe operation outcomes/failure stages,
  preserve correlation and bounded noise, and contain no account identity, credentials,
  private paths, provider bodies or arbitrary exception text.
- AC-11: With current authorization, owner-led checks connect both owner-held Claude
  accounts through Ledger, then verify independent readings, refresh/failure behavior,
  restart and selected-account sign-out/reconnect. Record unavailable live accounts or
  checks as NOT_RUN/BLOCKED, separately from fixture and unpackaged evidence.

## Verification and execution

Implementation proceeds sequentially on main, with coherent WIP save points under
CONTRIBUTING. Do not treat a save point as feature completion. A detailed implementation
plan includes the settled account/contract and full-deletion scope.
[design.md](design.md) records the
proposed boundaries. [verification.md](verification.md) owns observed results and
[tasks.md](tasks.md) contains the exact continuation action.
