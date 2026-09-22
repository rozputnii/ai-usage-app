# Initial provider-history assessment

Assessment date: 2026-09-22. Scope: documentation and public implementation source only.
No personal credential stores, browser sessions or authenticated provider responses were
read. No live request was made. Findings identify candidates and boundaries, not an
exhaustive endpoint inventory or proof that other methods do not exist.

## Codex

- provider: codex
- source_verified_at: 2026-09-22
- live_verified_at: null
- classification: source-observed web extraction; separate official administrator API
- confidence: source-verified candidate, existing-grant history access unverified

The existing OMP-based `/backend-api/wham/usage` integration supplies a current quota
snapshot. The inspected OMP adapter does not establish historical usage retrieval.
[Official workspace analytics](https://learn.chatgpt.com/docs/enterprise/analytics-api)
requires enabled workspace access and an administrator key with the appropriate analytics
scope; that is not evidence that the current personal Codex grant can read it.

[CodexBar's pinned provider documentation](https://github.com/steipete/CodexBar/blob/f28ddcaf3483674dc009fd496e94a9968d6e3ebc/docs/codex.md)
describes an optional web session for the Codex usage dashboard, a usage-breakdown chart
and credit-usage rows. Its
[scrape implementation](https://github.com/steipete/CodexBar/blob/f28ddcaf3483674dc009fd496e94a9968d6e3ebc/Sources/CodexBarCore/OpenAIWeb/OpenAIDashboardScrapeScript.swift)
extracts the chart and history table from the rendered page. This is evidence of a web
candidate, not a proven reusable OAuth history API. Browser-cookie import in that project
is not adopted. Windows feasibility, data coverage, account/workspace binding and access
with a separate app-owned web sign-in remain unverified.

## Claude

- provider: claude
- source_verified_at: 2026-09-22
- live_verified_at: null
- classification: existing internal current-quota method; separate official reporting API
- confidence: source-verified boundaries, personal subscription history method unverified

The existing `/api/oauth/usage` integration supplies current quota windows. The inspected
[CodexBar OAuth implementation](https://github.com/steipete/CodexBar/blob/f28ddcaf3483674dc009fd496e94a9968d6e3ebc/Sources/CodexBarCore/Providers/Claude/ClaudeOAuth/ClaudeOAuthUsageFetcher.swift)
does not establish a remote history contract.
[Anthropic's usage and cost reporting documentation](https://platform.claude.com/docs/en/manage-claude/usage-cost-api)
distinguishes Console API reporting and Enterprise analytics, requires their reporting
credentials and states that the Admin API is unavailable to individual accounts. These
are not a substitute for personal Claude subscription history. Personal remote history
coverage, date granularity and access remain unverified; do not label local token logs
or API spend as subscription history.

## GitHub Copilot

- provider: copilot
- source_verified_at: 2026-09-22
- live_verified_at: null
- classification: official billing reports; current OAuth eligibility unverified
- confidence: documented candidate, not live verified

[GitHub billing usage reference](https://docs.github.com/en/rest/billing/usage)
documents user AI-credit and premium-request reports with year/month/day/model filters,
24-month availability and native quantities/amounts per model and product. Documented
fine-grained access requires Plan read permission. AI Usage currently uses an OAuth
read:user connection for the internal quota endpoint; its report eligibility is unproven.
The examples describe period aggregates, not a ready-made daily series: preserve their
period, and establish request/rate-limit costs before designing daily charts.

OMP's `packages/ai/src/usage/github-copilot.ts` at local read-only commit
`10b867cb2eeb7809b883a88dfebe1919ff0c2764` has a `fetchBillingUsage` premium-report branch
for API-key credentials, distinct from its OAuth quota path. It does not prove support
with AI Usage's current grant. Requests, AI credits and money remain distinct metrics.
The [report reference](https://docs.github.com/en/billing/reference/billing-reports)
also distinguishes web-only detailed reports from summarized REST data. No complete
coverage or token-detail parity is claimed.

## Antigravity

- provider: antigravity
- source_verified_at: 2026-09-22
- live_verified_at: null
- classification: source-observed current-quota methods
- confidence: current-quota evidence only, remote history method unverified

The existing OMP `retrieveUserQuotaSummary` path supplies current groups and buckets.
The [official plans page](https://antigravity.google/docs/plans) describes present quota
and overage semantics; it does not establish a historical reporting API.
[CodexBar's pinned provider documentation](https://github.com/steipete/CodexBar/blob/f28ddcaf3483674dc009fd496e94a9968d6e3ebc/docs/antigravity.md)
explicitly describes its token history as machine-local database reads, separate from
remote quota fetching. That path is outside AIU-011. No provider-supplied historical
method was established in this assessment; absence in these sources is not proof of
global unavailability. Gemini API billing must not be substituted.

## Product consequence

Resolve PD-011-01 in [spec.md](spec.md) before selecting authentication architecture.
History should load automatically, but an account cannot silently acquire new reporting
credentials. Continue capability research within the chosen access boundary, then design
the smallest useful provider implementation. Do not build a universal empty-history UI
and report the feature complete.
