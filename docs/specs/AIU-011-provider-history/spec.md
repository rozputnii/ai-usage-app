---
id: AIU-011
type: spec
status: draft
goal: G-003
scope_version: 1
approval_basis: The owner selected provider-supplied history on 2026-09-22, deferred local collection until product stability, confirmed all four providers and all available historical usage metrics, and requested fewer clicks. The interaction proposal and additional-access decision below are not yet approved implementation architecture.
---
# Provider-supplied usage history

## Intended outcome

Read historical usage from Codex, Claude, Copilot and Antigravity wherever a verified
provider surface supplies it. Display the supplied usage metrics, including counts,
tokens, credits, costs or percentages when available, with their original meaning.
Provider coverage is capability-dependent; researching all four does not promise that
each provider exposes history for the user's plan and current connection.

This is an architectural change to history contracts: the current presentation contract
only represents remaining percentages, and the live adapter returns an empty placeholder.
The existing History screen is reusable; the demo data is not evidence of provider support.

## Proposed interaction

- One navigation action opens History and starts loading. No initial Load button or
  required account, metric and date-selection sequence.
- The sidebar entry initially shows sections for connected accounts using each source's
  recent reporting period. Each section loads and fails independently. The exact default
  period follows the verified source contract, rather than promising a universal range.
- An entry from an account card opens History focused on that account immediately.
- Show available summaries and appropriate charts/tables together. Filters refine the
  view; selecting an ordinary preset applies it immediately. Advanced custom dates may
  require Apply where necessary to avoid requests for partially entered ranges.
- Preserve current selection and fetched results in memory during the app session.
  Reopening can show those results immediately with their fetch time, then refresh as
  permitted by the provider. Coalesce identical requests, cancel obsolete ones and honor
  throttling; clock ticks must not trigger network history queries.
- Distinguish unavailable access, unsupported/unverified capability, no records, loading,
  partial coverage, stale results and failures. A missing source is not zero consumption.

## Scope and boundaries

All available historical *usage* fields are in scope, including provider-supplied model,
product, period and unit breakdowns. Raw payload dumps, conversation content, prompts and
responses are not usage history. Preserve provider identifiers and metric semantics;
never sum incompatible units, synthesize a quota curve from spending, or price tokens to
invent subscription charges. Present aggregate-only reports as aggregates.

Core owns credential-free history queries, results and capability/failure states.
Infrastructure owns remote transport, parsing and account-scoped access. Windows owns
navigation and presentation. Reuse hardened provider transport and the existing session
authority when evidence proves the same grant works; never expose grants through Core.

The proposed initial storage policy is memory-only history results. Local sampling,
databases, retention, rollups, persistent history cache and offline history across restarts
belong to AIU-029. Existing last-quota caching remains unchanged. Export, forecasts, CLI
transcript ingestion, new billing products and organization administrator reporting are
not implied by this selection.

## Access decision PD-011-01

Initial [research](research.md) has not proven a history read with any existing AI Usage
grant. Copilot documents separate billing permissions; a source-observed Codex web path
uses a distinct web session. This changes connection scope and credential boundaries.

Options:

1. Existing connections only. Investigate their eligibility and show unavailable history
   where access cannot be established. This may yield little or no historical data.
2. Also allow optional, user-initiated history access for the same subscription account
   where required (recommended for the requested breadth): an app-owned web sign-in or
   narrowly scoped reporting credential, selected only after verifying its contract.
   Existing quota access remains independent. Do not import browser/CLI credentials.
3. Extend into organization administrator/API billing integrations. This is a different
   product/authentication scope and is not recommended for this personal quota feature.

No option has been selected yet. A choice authorizes design of that path, not automatic
sign-in, harvesting existing sessions or provider-side account changes. Any implemented
credential lifecycle requires the security-lifecycle skill and focused independent review.

## Acceptance criteria

- AC-01: Record a source-backed history capability assessment for all four providers,
  including method, authentication, account/plan restrictions, units, period, coverage,
  paging and failures where known. Separate source verification from live verification;
  a bounded search without a method is unverified, not proof of impossibility.
- AC-02: At least one real provider-supplied historical dataset can be fetched and shown
  through the selected access path. An all-unavailable UI cannot close this feature.
  Report unsupported or inaccessible providers explicitly without blocking working ones.
- AC-03: Opening History starts loading automatically; an account entry arrives scoped
  correctly. A user sees the default result without selecting a filter or pressing Load.
- AC-04: Available historical metrics retain provider units and time semantics. Unknown,
  missing, partial and aggregate-only data are represented truthfully; charts require
  actual time buckets and never fabricate observations or reset boundaries.
- AC-05: Loading, refresh, navigation cancellation, throttling, account changes and
  disconnect preserve account isolation and responsive UI. No per-clock-tick requests.
- AC-06: No persistent history or local collection is introduced. Existing authentication,
  quota, cache and tray behavior regressions pass; any approved new access path has its
  own lifecycle evidence.
- AC-07: Relevant deterministic tests, document validation, builds and actual Windows
  interaction checks pass. Live-provider claims require separately recorded real reads.

## Verification approach

Follow [verification policy](../../workflow/verification.md). Test source parsing and
failure boundaries with sanitized fixtures, plus automatic loading, cancellation, metric
semantics and independent provider failures. Verify actual Windows navigation and rendering;
compilation alone cannot establish low-click behavior. Evidence belongs in
[verification.md](verification.md); the current exact next action is in [tasks.md](tasks.md).
