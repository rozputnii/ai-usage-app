---
provider: antigravity
source_verified_at: 2026-09-20
live_verified_at: 2026-09-20
confidence: authentication-live-verified-quota-unproven-provider-restricted
classification: method-specific
backlog: AIU-009
---
# Antigravity implementation evidence

Antigravity quota is served by Google's Cloud Code Assist control plane for a discovered `cloudaicompanionProject`. It is not the Gemini app allowance, the Gemini CLI allowance or a paid Gemini API key, and those surfaces are never combined. Authorization through a Google OAuth client does not establish permission for a third-party client to read this quota.

## Official boundary: an explicit provider restriction

Google's published [Antigravity FAQ](https://antigravity.google/docs/faq) states, under "Why can't I use third party software (e.g. Claude Code, OpenClaw, OpenCode) with my Antigravity login?", that using third-party software, tools or services to access Antigravity violates its Terms of Service and "may be grounds for suspension or termination of your account". It directs third-party coding agents to a Vertex or AI Studio API key instead.

This is an explicit restriction naming account suspension as a consequence, not merely an undocumented endpoint. It is stricter than the Claude restriction recorded in [claude.md](claude.md), because the stated penalty reaches the signed-in account itself. No project-specific permission has been provided, and no quota-monitor exception was found in the inspected official pages. That absence is not proof that no agreement exists. Owner authorization to build this feature is separate from provider permission, and a successful request never establishes approval.

The same FAQ records that Antigravity is currently limited to personal Google accounts in approved geographies and to users aged 18 or over. The [plans page](https://antigravity.google/docs/plans) documents the official quota semantics this implementation must not contradict: a baseline quota with a five-hour refresh for Google AI Pro and Ultra, weekly rate limits on every plan, weekly-only refresh below those plans, unlimited Tab completions, third-party model access for Ultra, and separately purchased AI credits used for overage above the baseline under an "AI Credit Overages" setting. It also states that baseline quota usage across models is visible on the product's own settings page, which is the only official parity surface. [Cloud Code Assist](https://cloud.google.com/gemini/docs/codeassist/overview) documents the product family behind the control plane but not the `v1internal` methods used below.

## Source provenance

On 2026-09-20 GitHub's latest-release endpoint returned [OMP v18.2.6](https://github.com/can1357/oh-my-pi/releases/tag/v18.2.6), published at 2026-09-18T18:10:25Z; its tag resolves to commit `78b753124d11f8dd3ae73e2524125890ff7c977e`.

- [Auth rule](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/catalog/src/compat/rules/auth/google-antigravity.kdl): `auth "google-antigravity"`, its `login "oauth-code"` block, callback, token, userinfo and refresh mappings.
- [Project discovery](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/registry/oauth/google-antigravity.ts): `loadCodeAssist`, `postLoadCodeAssist`, `onboardUser`, `discoverProject`, `googleAntigravityProjectHook`, `ANTIGRAVITY_LOAD_CODE_ASSIST_METADATA`.
- [Quota adapter](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/usage/google-antigravity.ts): `fetchAntigravityUsage`, `fetchAntigravityEndpoint`, `buildQuotaSummaryReport`, `buildQuotaSummaryAmount`, `getQuotaSummaryCounterKeys`, `classifyWindow`, and the legacy `normalizeQuotaInfos`/`inferWindowDescriptors` path.
- [Wire constants](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/catalog/src/wire/gemini-headers.ts): `getAntigravityUserAgent`, `DEFAULT_ANTIGRAVITY_VERSION`, `ensureAntigravityVersion`.
- [Authorization engine](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/registry/engine/oauth-code.ts) and [refresh engine](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/registry/engine/refresh.ts): `DeclarativeOAuthCodeFlow.generateAuthUrl`, `exchangeToken`, `createRequestRefresh`.

The local read-only clone was clean at `10b867cb2eeb7809b883a88dfebe1919ff0c2764` (`v18.2.6-17-g10b867cb2e`). All four Antigravity-specific files above are byte-identical between that checkout and the stable tag, verified by comparing Git blob identifiers; their last change is commit `d54ec5ef8685f0466fec87ae20134dc1fdb28c23`. No clone checkout, update or upstream execution was performed.

## Authentication: OMP's Antigravity registration over Google's documented desktop flow

The transport shape is Google's documented [OAuth 2.0 for native apps](https://developers.google.com/identity/protocols/oauth2/native-app) authorization-code grant: authorization at `https://accounts.google.com/o/oauth2/v2/auth`, a loopback redirect on `127.0.0.1` whose port Google does not match exactly, a form-encoded exchange at `https://oauth2.googleapis.com/token`, `access_type=offline` for a refresh token, and PKCE.

The registration is the part AI Usage does not own. GitHub push protection refused the first commit that carried OMP's client id and secret, which is the correct outcome: a third party's OAuth registration does not belong in this repository, and bypassing that control was not done. The implementation therefore reads the client from the environment, and the values below describe what OMP does rather than what AI Usage ships.

| Item | OMP source behavior; not provider authorization |
| --- | --- |
| Client | A Google desktop client id and secret, both base64-encoded in the linked auth rule. They belong to OMP, not to AI Usage, and reuse permission is unknown, so this repository does not vendor them: the operator supplies a client through `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET` on the device, and an unconfigured device reports `RegistrationUnavailable` without contacting the provider. Google documents an installed application's secret as non-confidential; that does not make redistributing another project's registration acceptable |
| Scopes | `cloud-platform`, `userinfo.email`, `userinfo.profile`, `cclog`, `experimentsandconfigs`. `cloud-platform` is broad; a smaller quota-only set is NOT established and was not negotiable against a client AI Usage does not own |
| Authorize params | `response_type=code`, `access_type=offline`, `prompt=consent`, plus standard client/redirect/scope/state |
| PKCE | The OMP rule does not enable PKCE. AI Usage adds S256 PKCE, which Google's native-app guidance requires; this is a hardening difference from OMP, verified live rather than assumed |
| Callback | `http://127.0.0.1:51121/oauth-callback`. Google's loopback rule permits another port when that one is busy; AI Usage falls back to an ephemeral port and records the exact redirect in the exchange |
| Token endpoint | Form POST with `client_id`, `client_secret`, `code`, `redirect_uri`, `code_verifier`, `grant_type=authorization_code`; refresh uses `grant_type=refresh_token` |
| Expiry/rotation | `expires_in` seconds with a five-minute skew in the rule. Google normally omits a new refresh token on refresh; a returned one must replace the stored grant, and the previous value must never be replayed afterwards |
| Identity | `GET https://www.googleapis.com/oauth2/v1/userinfo?alt=json`; OMP maps `email`. AI Usage binds the account to the opaque `id` subject and treats a changed subject as a mismatch |

## Project discovery: read-only, without onboarding

OMP's after-exchange hook posts `v1internal:loadCodeAssist` to `https://daily-cloudcode-pa.googleapis.com` with `metadata.ideType = "ANTIGRAVITY"` and an Antigravity `User-Agent`, reads `cloudaicompanionProject`, `currentTier`, `paidTier`, `allowedTiers` and `ineligibleTiers`, and repeats the load with the discovered project when `paidTier` is absent. When `currentTier` is missing it calls `v1internal:onboardUser` with `tierId: "free-tier"` and polls the returned long-running operation until the free tier is provisioned.

`onboardUser` is a provider-side write that changes account entitlement state. It was excluded in the first implementation, and the first live run showed the cost of that: an unprovisioned account returns no project, so there is no quota to read at all, and nothing in a read-only flow can change that. On 2026-09-20 the owner chose OMP's behavior, so connecting now provisions the free tier when the provider reports no current tier.

The write is fenced: it happens only while connecting, never during resume or refresh; it is skipped when the provider already reports a tier; it is skipped when the provider declares the account ineligible for the free tier; the returned long-running operation is polled with the same thirty-second deadline and one-second interval OMP uses, and the poll target is constrained to the provider's operations path. A provisioning failure, timeout or a provisioning that yields no project all report `ProjectUnavailable`, and the provider's reason message and validation URL are never echoed into the app.

The discovered project identifier and the discovery-time tier are stored with the grant. The tier is presented as the plan label and is only ever as fresh as the last successful discovery; it is re-read on reconnection, not on every quota refresh.

## Quota: undocumented internal endpoint

OMP reads `POST {endpoint}/v1internal:retrieveUserQuotaSummary` with body `{"project": "<projectId>"}`, a bearer access token and the Antigravity `User-Agent`, falling back to `v1internal:fetchAvailableModels` and to a sandbox host. Its comment records that the quota-summary method is the one Antigravity's own quota surface uses and that it reports both the rolling five-hour and the weekly bucket even when neither is exhausted, which matches the official plans page.

| Payload | Interpretation supported by the inspected code |
| --- | --- |
| `groups[].displayName`, `groups[].description` | Opaque provider group labels, for example Gemini models versus shared Claude and GPT models. Membership is read from the response, never inferred from a model name |
| `groups[].buckets[]`, top-level `buckets[]` | Either shape can carry the limits; a grouped response takes precedence |
| `bucketId`, `displayName`, `description` | Opaque identifiers and labels. OMP derives counter families from `gemini-`/`3p-` prefixes for its own credential ranking; AI Usage does not, because a monitor must not relabel a provider's grouping |
| `window` | Window token such as `weekly` or `5h`. A recognized token supplies the displayed duration; an unrecognized token stays opaque with no duration |
| `remainingFraction` | Fraction remaining in 0..1. Absent means unknown, never zero |
| `remainingAmount` | Number or numeric string with no documented unit; retained as an explicit amount with an unknown unit, never converted to a percentage |
| `resetTime` | ISO reset timestamp for the bucket |
| `disabled` | The bucket does not apply to this account; OMP drops it. AI Usage drops it from the windows and records a not-allowed group only when every bucket in that group is disabled |

OMP's legacy `fetchAvailableModels` path infers daily and weekly windows from model-level reset timestamps, treats a missing `remainingFraction` with a present `resetTime` as exhausted, and duplicates one shared third-party bucket into separate Anthropic and OpenAI counters. Those are upstream ranking choices that manufacture structure the provider did not send. That path, its window inference and the sandbox host fallback are excluded from this implementation; an unavailable quota-summary endpoint is reported as unavailable.

The Antigravity `User-Agent` in OMP identifies as the real `antigravity/hub` client on a pinned darwin/arm64 version discovered from the vendor's update manifest, because the backend gates newer models on that version. AI Usage sends its own truthful `AiUsage/0.1` identity and does not imitate another application to overcome a rejection. Model gating does not apply to a quota read, but a provider may reject an honest identity; that outcome is reported, not worked around.

## Side effects and open proof

Authorization creates a grant and consumes the consent screen of whichever client the device is configured with. A refresh can rotate or invalidate the previous token. Quota reads and `loadCodeAssist` do not run inference or mint entitlements. Connecting an unprovisioned account does enrol it in the free tier, as described above. No model enablement, paid-tier purchase or upgrade, overage-setting change, CLI credential read or host trust change is included in this scope.

Live on 2026-09-20, with the owner's explicit decision to accept the restriction above on their own account: Google served the consent screen for the authorization request as built, the loopback callback and the token exchange succeeded with PKCE, and the userinfo identity read succeeded. `loadCodeAssist` then returned success with no `cloudaicompanionProject`, so the session reported `ProjectUnavailable`, stored nothing, and did not onboard. The account has no provisioned Cloud Code Assist workspace and no Antigravity client is installed on that machine.

That is the practical consequence of excluding `onboardUser`: OMP reaches a project on an unprovisioned account only by provisioning the free tier, and a monitor that refuses to write cannot reach one by itself. Enabling that write is an owner decision, still open. Until then the quota contract in the table above remains source-derived and unproven on a real account.

Live NOT_RUN: quota-summary field presence and units, reset semantics, the official settings-page comparison, refresh, rotation, resume, reconnect and local disconnect. No sanitized live fixture exists; the synthetic fixtures in the Infrastructure suite carry no account data. See the [verification record](../specs/AIU-009-antigravity-integration/verification.md).
