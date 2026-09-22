---
id: AIU-011
type: spec
status: draft
goal: G-003
scope_version: 3
approval_basis: The owner selected provider-supplied history on 2026-09-22, deferred local collection until product stability, confirmed all four providers and all available historical usage metrics, and requested fewer clicks. The subsequent owner amendment restricts access to existing authorization and selects OMP's provider-history method if present. The follow-up asks for general feasibility analysis beyond OMP; it does not select an alternate implementation. The interaction proposal remains a draft; implementation requires an eligible upstream method.
---
# Provider-supplied usage history

## Intended outcome

Read historical usage from Codex, Claude, Copilot and Antigravity wherever a verified
provider surface supplies it. Display the supplied usage metrics, including counts,
tokens, credits, costs or percentages when available, with their original meaning.
Provider coverage is capability-dependent; researching all four does not promise that
each provider exposes history for the user's plan and current connection.

The preferred reference is OMP. Reproduce its provider-history retrieval method
where OMP supplies one using the authorization already available in AI Usage.
The source assessment of stable OMP v18.2.8 found no such path for any of the four
providers. The owner's subsequent feasibility question reopens research beyond OMP;
official Codex OAuth analytics is now a concrete candidate. No alternate implementation
has yet been selected or live verified. See [research.md](research.md).

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

If an eligible upstream history path is established, Core owns credential-free history
queries, results and capability/failure states.
Infrastructure owns remote transport, parsing and account-scoped access. Windows owns
navigation and presentation. Reuse hardened provider transport and the existing session
authority when evidence proves the same grant works; never expose grants through Core.

The proposed initial storage policy is memory-only history results. Local sampling,
databases, retention, rollups, persistent history cache and offline history across restarts
belong to AIU-029. Existing last-quota caching remains unchanged. Export, forecasts, CLI
transcript ingestion, new billing products and organization administrator reporting are
not implied by this selection.

## Resolved access decision PD-011-01

Owner decision, 2026-09-22: existing authorization only. Inspect OMP and reproduce its
provider-history retrieval method for each provider if present. Additional sign-in,
browser-session access, API keys, organization reporting credentials and expanded
permissions are excluded. No browser/CLI credential import or OMP-store import is implied.

OMP's `usage --history` reads OMP-recorded observations; the broker endpoint reads the
broker's stored observations. Both are local collection from the recorder's perspective,
not a history service supplied by Codex, Claude, Copilot or Antigravity. Reproducing that
mechanism belongs to deferred AIU-029 and is not authorized now. The earlier CodexBar web
and official administrator/reporting API candidates remain research context only.
The subsequent broader feasibility request includes alternate methods that might work
with existing authorization; it does not permit extra access or select an implementation.

## Acceptance criteria

- AC-01: Record a source-backed history capability assessment of OMP and relevant
  alternate provider methods for all four providers using existing authorization,
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
- AC-06: No persistent history, local collection or additional authorization is introduced.
  Existing authentication, quota, cache and tray behavior regressions pass.
- AC-07: Relevant deterministic tests, document validation, builds and actual Windows
  interaction checks pass. Live-provider claims require separately recorded real reads.

## Verification approach

Follow [verification policy](../../workflow/verification.md). Test source parsing and
failure boundaries with sanitized fixtures, plus automatic loading, cancellation, metric
semantics and independent provider failures. Verify actual Windows navigation and rendering;
compilation alone cannot establish low-click behavior. Evidence belongs in
[verification.md](verification.md); the current exact next action is in [tasks.md](tasks.md).
