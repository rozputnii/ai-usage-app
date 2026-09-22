# Provider-history assessment

## Broader feasibility assessment - 2026-09-22

The owner's follow-up asks whether provider history can be retrieved at all, reopening
research beyond OMP. Existing authorization remains mandatory. OMP's local implementation
does not establish global unavailability. No alternate implementation or additional access
is selected by this research. No real credentials or authenticated responses were read.

All entries in this assessment have source_verified_at=2026-09-22 and
live_verified_at=null. Authentication and current-quota classifications remain in the
existing provider records; the classifications below concern historical reporting.

| Provider | History classification and confidence | Existing-authorization conclusion |
| --- | --- | --- |
| Codex | Official development-source internal analytics client; source-verified candidate. | Concrete ChatGPT OAuth candidate exists. Eligibility with AI Usage's stored grant, plan coverage and retention are not live verified. |
| Copilot | Documented public historical billing reports; source-verified contract. | Access using AI Usage's existing read:user OAuth grant remains unverified. A fine-grained Plan-read credential is not interchangeable evidence. |
| Claude | Official product documentation describes credit-consumption history; no compatible transport contract established. | Personal subscription history retrieval with the existing OAuth grant remains unverified. Console/Enterprise reporting is a separate credential/product boundary. |
| Antigravity | Official CLI documentation describes a credit-consumption history panel; no compatible transport contract established. | Existing Google OAuth eligibility and a remote history endpoint remain unverified. Current quota summaries and local logs do not establish that contract. |

### Codex: a concrete OAuth candidate outside OMP

Official `openai/codex` development source was inspected at immutable commit
`2c2a42e65de077c5518ea5b4c3999633ef6a12fc`:

- [Analytics request implementation](https://github.com/openai/codex/blob/2c2a42e65de077c5518ea5b4c3999633ef6a12fc/codex-rs/backend-client/src/client/analytics.rs)
  issues GET requests beneath `/backend-api/wham/`. Relevant routes include
  `usage/daily-token-usage-breakdown`, `usage/credit-usage-events` and
  `analytics/daily-workspace-usage-counts`. Separate workspace token/credit, plugin and
  skill reports exist; their availability must not be generalized to every plan.
- [Analytics session](https://github.com/openai/codex/blob/2c2a42e65de077c5518ea5b4c3999633ef6a12fc/codex-rs/backend-client/src/analytics_session.rs)
  requires ChatGPT authentication, binds account and user identity, and discovers the
  server plan. The [shared client](https://github.com/openai/codex/blob/2c2a42e65de077c5518ea5b4c3999633ef6a12fc/codex-rs/backend-client/src/client.rs)
  adds the existing auth provider's headers, including bearer/account context. This is
  source evidence of OAuth analytics, not a requirement for browser cookies or an Admin
  API key. AI Usage must reuse its own session authority, never the CLI credential store.
- Date-based reports use inclusive UTC `start_date` / `end_date` and `group_by=day`.
  Messages/plugins/skills additionally use `workspace_user=true`. Credit events have
  no date parameters in this client. No paging or complete-retention guarantee was
  established. The upstream UI's 7/30-day choices are not server retention limits.
- [Response models](https://github.com/openai/codex/blob/2c2a42e65de077c5518ea5b4c3999633ef6a12fc/codex-rs/codex-backend-openapi-models/src/models/analytics.rs)
  include dated usage, provider units, model/product/client attribution, credit events,
  thread/turn counts and optional input/cache/output token and cost fields. Optional
  amounts remain unknown when absent; credit events can be signed. These are not a
  historical remaining-percentage curve. The upstream tests were inspected, not run.

This is official **development-source evidence**, not a documented stable public API or
proof of live backend support. The latest stable release returned by GitHub was
`rust-v0.155.1`, published 2026-09-18, resolving to
`be2951ea34f0d295ed0becf97079f92fa5f6950e`. The specific analytics client file above
returned 404 at that stable tag; this check does not exclude equivalent code elsewhere.

A separate [Codex Watch implementation](https://github.com/moebis/codex-watch/blob/dfd68dc5a795c3aceaebd8e5a28fe47de842beac/Sources/CodexWatch/Usage/CodexUsageClient.swift)
uses bearer/account headers for both current quota and daily workspace usage counts.
Its 365-day query is a client choice, not proof of provider retention. Its CLI credential
import and any local-history mechanisms are not selected for AI Usage.

**Next evidence needed:** a bounded read through AI Usage's existing Codex session,
starting with a short daily-usage range, recording only sanitized status/schema/coverage.
Confirm account binding, plan-specific fields, missing-data behavior and range limits
before promising all available history. No new sign-in is implied.

### Copilot: history exists, current OAuth access is unresolved

The [official billing reference](https://docs.github.com/en/rest/billing/usage)
documents user AI-credit and premium-request reports, up to 24 months, with date filters
and provider quantities/amounts per model/product. User reports apply to personally
billed plans; organization-paid usage requires the relevant organization/enterprise
report. The documented fine-grained permission is Plan read. The existing read:user
OAuth grant must be evaluated separately: neither this documentation nor OMP's api_key
branch proves that the current grant succeeds or that it is impossible. Reports are
period aggregates; request-per-day costs and throttling need verification for daily charts.
No new PAT or permission expansion is permitted to work around an access denial.

### Claude: credit history is described, compatible retrieval is unknown

[Claude's official paid-plan credit guide](https://support.claude.com/en/articles/12429409-manage-usage-credits-for-paid-claude-plans)
explicitly describes reviewing past credit-consumption patterns. This is evidence of a
product history surface, not an OAuth endpoint, retention policy or historical base-plan
quota curve. The existing `/api/oauth/usage` path remains a current-window snapshot.
The [Console usage/cost API](https://platform.claude.com/docs/en/manage-claude/usage-cost-api)
has separate reporting authorization and does not establish personal subscription
history with the existing grant. No suitable personal OAuth history endpoint was found
in the inspected sources; that is an unresolved capability, not a proof of impossibility.

### Antigravity: credit history is described, compatible retrieval is unknown

The official [/credits command documentation](https://antigravity.google/docs/cli/commands/credits)
describes credit-consumption history and a current-billing-cycle summary. It does not
specify an HTTP endpoint, retention, paging or whether the history is remotely fetched.
The public `google-antigravity/antigravity-cli` tree inspected at
`ad7d70342a108687a1b36db7573050de3e9c2c3e` did not expose the corresponding client source.
OMP's current `retrieveUserQuotaSummary` therefore remains insufficient, but the earlier
absence of a history method in OMP must not become a claim that no history exists.
Do not substitute local databases or unrelated Gemini API billing.

### Revised disposition

Provider history is feasible in principle, with a strong Codex OAuth candidate and a
documented Copilot reporting API. Universal coverage under existing authorization is
not established. AIU-011 returns to research-needed; the previous OMP-specific blocker
is not a global conclusion. Prioritize Codex existing-session validation, then Copilot
eligibility. Claude and Antigravity need transport evidence. Implementation remains
unstarted, and local collection stays deferred to AIU-029.

## Earlier OMP reference assessment: result on 2026-09-22

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

**Disposition at that stage:** No eligible OMP provider-history method was found for the four existing
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

PD-011-01 is resolved in [spec.md](spec.md). The broader follow-up at the top of this
document supersedes the OMP-only research conclusion. Additional web/reporting credentials
and unrelated provider products remain outside scope. No product implementation was started.
