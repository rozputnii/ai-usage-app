---
id: T-037
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Derived from the owner's selection of T-037 on 2026-10-03 and the accepted T-034 research; no separate specification review is claimed.
---

# Provider parser extensions and stored-format version 2

Implement research sections 5.5 and 5.7 using the existing provider requests and the
T-035 credential-free limit model. No new transport, authorization, scope, provider
request, CLI import, UI-only pool or presentation redesign is included.

## Contracts

- Each successful parser snapshot carries normalized limit facts in addition to the
  existing presentation fields. Native limit keys preserve opaque discriminators.
  Claude current scoped windows replace the matching legacy scope; current spend
  replaces legacy extra usage. Unknown, explicit null, unlimited and finite zero
  remain distinct where the source expresses those states.
- Codex credit balances are signed. Individual control percentages form one window;
  its raw used/limit/remaining strings remain secondary facts with unknown unit.
  Reset facts retain date/instant precision and explicit/assumed UTC zone. Missing
  duration never implies five hours. Provider UI-only families are not synthesized.
- All four quota stores use version 2. Valid v1 cached data migrates conservatively,
  with period start unknown until the next fetch and lost source precision unknown
  rather than invented. Unmigratable cache alone may be dropped. Identity, grant,
  revision and parent revision are unchanged by migration.
- Protected state migration uses the existing exclusive lease and DPAPI CurrentUser
  entropy, retaining a verified encrypted v1 checkpoint before staged replacement.
  Pending grant successors resolve before format migration. Recovery never rolls a
  rotated grant back from the checkpoint. Exact owned paths, size limits, cancellation,
  interrupted staging and cleanup include migration artifacts.
- Codex cache migration uses its existing lease, a bounded v1 checkpoint and staged
  replacement; its grant store and renewal journal are unchanged.
- Local history adopts native keys through an explicit compatibility mapping. Only
  provably equivalent series may continue; ambiguous legacy keys remain retained and
  separate. No destructive history rewrite or merging unlike counters.

## Acceptance

- AC-01: Synthetic parser tests cover every represented research 5.5 family and
  nonrepresentation of UI-only families, including absence/null/unlimited/zero,
  negative balance, individual control, scoped replacement and reset precision.
- AC-02: Opaque plan, currency, feature, model, scope and bucket values round trip
  verbatim; only allowlisted normalized facts are persisted, never provider payloads.
- AC-03: Each v1-to-v2 migration preserves identity/grant/generation and usable cache;
  malformed cache alone is recoverable. Interrupted writes, unsupported versions,
  reparse paths and explicit cleanup have tested safe behavior.
- AC-04: Native observation capture has an explicit tested legacy-key compatibility
  mapping and preserves old history without silently joining incompatible series.
- AC-05: Infrastructure and presentation regressions, document validation, diff review
  and focused independent security-lifecycle review pass. Live/UI/package results
  remain separate; synthetic success does not imply live verification.
