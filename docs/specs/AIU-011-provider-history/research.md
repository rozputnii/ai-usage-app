# Provider-history assessment

## Selected OMP-only scope: result on 2026-09-22

The owner resolved PD-011-01: existing authorization only, and the same provider-history
retrieval as OMP if present. GitHub's public latest-release API returned
[v18.2.8](https://github.com/can1357/oh-my-pi/releases/tag/v18.2.8), published
2026-09-21T17:31:56Z, resolving to `5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c`.
The complete repository tree and the relevant public sources were read at that immutable
commit. The local OMP checkout at `10b867cb2eeb7809b883a88dfebe1919ff0c2764` was read
only for discovery and left unchanged; it is not substituted for the newer stable source.

All four rows below have source_verified_at=2026-09-22, live_verified_at=null and
confidence=source-verified. Authentication and quota classifications retain the existing
provider records. The history classification is **OMP-recorded local observations**, not
a provider history endpoint. This is a conclusion about this source revision and scope,
not a claim that every possible provider API was examined.

| Provider | OMP fetch path with existing authorization | Historical provider data in that path |
| --- | --- | --- |
| Codex | OAuth `GET /backend-api/wham/usage`; optional reset-credit inventory detail read. [Adapter](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/src/usage/openai-codex.ts#L477), `openaiCodexUsageProvider.fetchUsage`. | Current quota windows and currently available reset credits. Grant/expiry timestamps of available reset credits are inventory metadata, not past usage. No history query. |
| Claude | OAuth `GET /api/oauth/usage`, optional profile identity read. [Adapter](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/src/usage/claude.ts#L682), `fetchClaudeUsage`. | Current session/weekly/model windows and extra-usage period totals. No historical buckets, period traversal or backfill. |
| Copilot | OAuth `GET /copilot_internal/user`, optional `/user` identity read. [OAuth branch](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/src/usage/github-copilot.ts#L389), `fetchInternalUsage`. | Current entitlement/request snapshots. The separate `fetchBillingUsage` branch is gated by `credential.type === "api_key"` at line 335. It cannot be adopted under the owner's existing-authorization-only scope or described as OMP's OAuth method. |
| Antigravity | OAuth `POST /v1internal:retrieveUserQuotaSummary`; OMP also has a current-model-quota fallback. [Adapter](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/src/usage/google-antigravity.ts#L465), `fetchAntigravityUsage`. | Current quota buckets and reset timestamps. Neither the summary nor the model fallback supplies past observations. This history request does not change AIU-009's existing exclusion of the legacy fallback and sandbox host. |

The providers return a current `UsageReport`. OMP adds history after the fetch:

1. [AuthStorage](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/src/auth-storage.ts#L3663)
   calls `#recordUsageHistory` when a fresh report succeeds. The method at line 3693
   creates one row per current limit, timestamped with the fetch time, and calls the
   store's `recordUsageSnapshots`.
2. [SqliteAuthCredentialStore](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/src/auth/sqlite-credential-store.ts#L1653)
   writes its own `usage_history` table and retains the latest observation within an
   hourly account/window bucket. `listUsageHistory` at line 1693 reads that table.
3. The [history CLI branch](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/coding-agent/src/cli/usage-cli.ts#L1068)
   calls `authStorage.listUsageHistory`; it does not request past data from providers.
4. The [broker history route](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/src/auth-broker/server.ts#L709)
   also calls `opts.storage.listUsageHistory`. A network request to the OMP broker is
   still a read of OMP-collected observations, not provider-supplied history.

The upstream [history tests](https://github.com/can1357/oh-my-pi/blob/5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c/packages/ai/test/auth-storage-usage-history.test.ts)
describe local snapshot recording and hourly replacement. Those tests were inspected,
not executed. No personal OMP databases or credentials were opened and no live provider
requests were made.

**Disposition:** No eligible OMP provider-history method was found for the four existing
authorization paths. AIU-011 implementation is blocked by that missing capability.
An empty-history implementation would not meet AC-02. The owner's deferred local-history
decision remains intact; AIU-029 is independent of remote-history availability.

## Earlier broad assessment: context only

The sources below were considered before the OMP-only/existing-authorization amendment.
They do not authorize additional access or alternate implementations.

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

PD-011-01 is resolved in [spec.md](spec.md). The selected OMP-only result above supersedes
the earlier access proposal. Additional web/reporting credentials and unrelated provider
APIs are outside scope. No product implementation was started.
