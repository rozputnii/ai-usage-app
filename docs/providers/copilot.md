---
provider: copilot
source_verified_at: 2026-09-18
live_verified_at: 2026-09-18
confidence: source-and-live-verified-private-unsupported
classification: method-specific
backlog: AIU-008
---
# Copilot implementation evidence

Distinguish documented personal usage reports from undocumented entitlement snapshots, AI credits from premium requests, and personally billed accounts from organization-paid contexts. Authorization with an app-owned client does not guarantee access to an internal quota endpoint.

## Source provenance

On 2026-09-18 GitHub's latest-release endpoint returned [OMP v18.2.6](https://github.com/can1357/oh-my-pi/releases/tag/v18.2.6), published at 18:10:25Z. Its tag resolves to commit `78b753124d11f8dd3ae73e2524125890ff7c977e`.

- [Authentication source](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/registry/oauth/github-copilot.ts): `resolveOAuthClientId`, `startDeviceFlow`, `pollForGitHubAccessToken`, `loginGitHubCopilot`, `refreshGitHubCopilotToken`, `enableAllGitHubCopilotModels`.
- [Quota source](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/usage/github-copilot.ts): `fetchInternalUsage`, `parseQuotaDetail`, `normalizeQuotaSnapshots`, `githubCopilotUsageProvider.fetchUsage`.
- [Wire source](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/catalog/src/wire/github-copilot.ts): `COPILOT_GITHUB_HEADERS`, `COPILOT_CAPI_IDENTITY_HEADERS`.

The local read-only clone was clean at `becbf82cb2c0b598e27cf0bf10453065645d915e` (`v18.1.21-6-gbecbf82cb2`). Quota and wire files match the stable source ignoring line endings. The stable auth file was separately read; its prompt type import differs. No clone checkout, update or executable upstream code was used.

## Authentication: official protocol, owner-selected OMP registration

[GitHub device-flow documentation](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps) documents `POST https://github.com/login/device/code` and `POST https://github.com/login/oauth/access_token`, a public client ID, device-flow enablement, transient device/user codes and interval/expiry metadata. Polling must respect the interval and increase it on `slow_down`; denial, expiry and disabled-flow errors are distinct. Device flow does not require a client secret, loopback redirect or PKCE/state callback. Validate identity with `GET https://api.github.com/user` after authorization. Expiring grants can include refresh metadata, and documented renewal invalidates the previous pair. Do not infer a non-expiring grant from OMP's implementation.

[GitHub's own-app setup](https://docs.github.com/en/copilot/how-tos/copilot-sdk/setup/github-oauth) supports application-owned OAuth/GitHub App user authorization for Copilot SDK clients and assigns token lifecycle responsibility to the application. This does not prove direct internal-quota access with an AI Usage registration.

OMP uses OpenCode public client ID `Ov23li8tweQw6odWQebz` on github.com and `read:user`; its enterprise path selects Copilot CLI ID `Ov23ctDVkRmgkPke0Mmm`. These are source-observed public identifiers, not secrets or approved AI Usage registrations. OMP stores the same GitHub token in access/refresh fields and assigns a synthetic ten-year expiry; its refresh helper makes no renewal request. AI Usage must preserve actual provider lifecycle semantics. Registration reuse permission is unknown. On 2026-09-18 the owner explicitly selected only OMP, resolving [PD-008-01](../specs/AIU-008-copilot-integration/spec.md) for a private unsupported implementation. No own-app registration or SDK is selected.

## Quota: undocumented direct endpoint

OMP's OAuth quota path calls `GET https://api.github.com/copilot_internal/user` with a GitHub bearer token and a Copilot CLI User-Agent. It may use `/user` to recover a login label. This is not a documented public quota contract. AI Usage successfully exercised the path on 2026-09-18 with its truthful `AiUsage/0.1` identity, the OpenCode device grant and a verified numeric GitHub account ID.

Observed fields: `copilot_plan`, `quota_reset_date`, `quota_snapshots` containing `premium_interactions`, `chat`, `completions`; each detail can carry `entitlement`, `remaining`, `quota_remaining`, `percent_remaining`, `unlimited`, `quota_id`, `overage_count`, `overage_permitted`. OMP requires four fields, derives used from entitlement minus remaining, labels all amounts as requests and omits unlimited chat/completion rows. These are upstream normalization choices, not evidence that newer AI-credit or organization contexts share those semantics. Preserve uncertainty and independent fields; do not copy defaults that turn missing data into zero/false.

OMP also has a personal billing branch for API-key credentials. That branch is not a substitute for subscription entitlement and is excluded from this task.

## Official SDK alternative

[GitHub account quota documentation](https://docs.github.com/en/copilot/how-tos/copilot-sdk/features/usage-and-billing) documents `account.getQuota`, quota-type maps, request entitlement/usage, remaining percentage and reset time; `-1` explicitly denotes unlimited entitlement there. It separately describes AI-credit session metrics. Those SDK values must not be assumed to have the same wire contract as the internal endpoint. SDK adoption would add its CLI runtime and requires an explicit dependency decision; it has not been selected or run.

## Side effects and open proof

OMP login discovers inference endpoints and enables models with policy POSTs. A quota monitor omits both inference setup and model-policy changes. Never imitate another application's identity to overcome a provider rejection. That rule still holds for Copilot and is unchanged here; the owner authorized one recorded exception for Antigravity on 2026-09-20, after that provider refused a truthfully identified client outright, and [its record](antigravity.md) states the scope and the cost. No automatic billing fallback, inference request, model enablement, token import or organization-policy change is authorized by selecting AIU-008.

Live requirements: chosen client's endpoint eligibility, stable numeric account identity, exact quota units/context and reset semantics, nullable/malformed groups, access denial versus expired auth, Retry-After, cancellation, applicable rotation, resume and local disconnect. No sanitized live fixture exists. Enterprise host trust/context switching and organization-paid parity remain unverified.

## Live scope and limitations

Authorized native Chrome/Windows checks on 2026-09-18 passed device authorization, quota refresh, Exit/relaunch resume, local disconnect, cancellation and reconnect. GitHub displayed the OpenCode application and read-only profile access. No raw account response, token, user identity or device code is retained in repository evidence.

The account's GitHub settings page identifies Copilot Free and shows Inline suggestions and Included credits, both 0% used. The OMP endpoint reports plan `individual` and separate request snapshots, including a zero-entitlement premium pool. These are different metrics: this implementation does not claim parity with Included credits or derive credit balances from requests. A zero entitlement has no meaningful consumed fraction and is presented as unknown with its explicit native amount. Known pools use OMP's premium/chat/completions order. Nullable quota identifiers, remaining and overage details are retained independently; no provider restriction is inferred from exhaustion.

Paid plans, organization contexts, enterprise hosts, live rate limits, revoked/expired grants and credits parity were not exercised. Synthetic tests cover the relevant failure semantics. Registration reuse permission remains unknown; successful access is not provider approval. See [verification](../specs/AIU-008-copilot-integration/verification.md).

## Provider history (AIU-011)

- source_verified_at: 2026-09-22
- live_verified_at: null
- classification: official-personal-billing-api; current-OAuth-eligibility-unverified
- confidence: source-verified; existing-session access unverified

GitHub documents `/users/{username}/settings/billing/ai_credit/usage` and
`/users/{username}/settings/billing/premium_request/usage` with year/month/day filters and
up to 24 months of personal paid-plan history. The [pinned research](../specs/AIU-011-provider-history/research.md)
links the official endpoint documentation and its fine-grained authorization requirements.
The existing device grant has read:user; no PAT, billing permission or additional login is
introduced. Whether that grant can read either report remains unverified.

The client first validates the numeric identity at `/user`, then uses the returned login
on fixed api.github.com routes with API version 2026-03-10. Complete months are requested
as aggregate periods; partial months use one request per day for each report. The maximum
selected range is 731 days, but backend retention can be narrower. Native quantities,
unit prices and gross/discount/net USD amounts remain separate. Missing values stay unknown;
malformed nonempty reports fail rather than becoming zero or empty history.

Each report stops its remaining periods on failure while allowing the other report type
to load. Authentication failure or throttling stops all further requests; Retry-After is
honored. Identity mismatch discards partial results and clears account history. External
removal clears session state. No durable history or permission escalation is introduced.
Partial-month ranges can require many requests; cancellation and per-response limits apply,
and incomplete or top-limited coverage is never promised as complete.

Earlier live quota checks do not verify historical billing access. No stored Copilot grant
was available for this run; see [verification](../specs/AIU-011-provider-history/verification.md).
