# Provider sources reviewed against OMP v18.9.0 - verification

Evidence for [the specification](spec.md). Code: `3422615` and the record commit after it, on the
task branch (not yet merged with `main`). Environment: Windows 11 Pro 10.0.26300, .NET SDK
10.0.401, Release. Date: 2026-10-12.

## Upstream reference

A read-only clone of `https://github.com/can1357/oh-my-pi` outside this repository, checked out
at tag `v18.9.0` = commit `eec114a9226da698ebc015fd8996a8ca3bc4625c` (committed
2026-10-11T09:42:14+02:00). The record pins resolve in that clone: v18.1.19 = `e4dd2ec3b487`,
v18.1.22 = `23a5b9ae3886`, v18.2.6 = `78b753124d11`, v18.3.2 = `7853b4e49993`. The changes were
read with `git log` and `git diff <pin>..v18.9.0` on the files each record names, and on the
files they were moved or split into: `packages/ai/src/auth-storage.ts` was split into
`packages/ai/src/auth/*.ts` (9013268ba1; usage coordination in `usage.ts` and `usage-cache.ts`,
OAuth refresh in `refresh.ts`), and the Claude adapter gained
`packages/ai/src/usage/claude-api.ts` and `claude-reset.ts`. No upstream code was executed, no
credential was read and no provider was called.

Paths below are relative to `packages/`. A commit appears under every provider whose path it
touches.

## Codex (pin v18.1.19)

`ai/src/registry/oauth/openai-codex.ts` (device flow, browser flow, token exchange and refresh)
has no commit since the pin: sign-in and refresh are unchanged.

| Commit | File | Function | Change | Decision |
| --- | --- | --- | --- | --- |
| 5dfdbf879d, 92c7434014, 089d08a9df | ai/src/usage/openai-codex.ts | `hasPlanCreditOverage`, `isCodexRequestAllowed`, `buildPlanMeterState`, `parseUsagePayload` | Reads a new `credits.overage_limit_reached` field; when the plan window reports `limit_reached` and credits (or unlimited) fund overage without `spend_control.reached`, the plan meter is treated as still serving | Backlog T-068: a new field and a new eligibility meaning. AI Usage already shows the plan window and the credit pool separately and reads `spend_control.reached` |
| 12d212c36a | ai/src/usage/openai-codex.ts | `parseCodexRateLimitHeaders` | A 2xx inference response's `x-codex-*` headers count as an allowed verdict | Not applicable: AI Usage never runs inference and reads no response headers for quota |
| 73a11421fe | ai/src/usage/openai-codex.ts | `fetchCodexDaybreakAccess`, `hasDaybreakAccess`, `codexPlanGate`, `classifyOpenAICodexPlan` | GET `backend-api/accounts/verified_access` for a "cyber" access-program badge; plan classes gate model routing | Not applicable: an access-program entitlement and a model-routing gate, not sign-in, refresh or quota |
| 9013268ba1 | ai/src/usage/openai-codex.ts | `codexRankingStrategy.healableBlockScopes` | Clears OMP credential blocks from a fresh report | Not applicable: OMP credential selection |
| d724165ae6 | ai/src/usage/openai-codex.ts | `buildUsageStatus` → shared `usageStatus` | Helper merge, same thresholds | Not applicable: no wire change |
| 9013268ba1 | catalog/src/compat/rules/auth/openai-codex.kdl | `auth "openai-codex"` | `org-scoped-identity #true`: OMP qualifies its stored credential and report identity by organization | Not applicable: AI Usage already binds a grant to its `chatgpt_account_id` workspace and fails closed on a change |
| 3bb803acb8, 00fd4a0e47, 37bb80e3b2 | catalog/src/wire/codex.ts | `CODEX_CLIENT_VERSION` | 0.153.0 → 0.159.0; the backend gates models on `/models?client_version=` and `/responses` | Not applicable: AI Usage calls neither route and sends no client version; `wham/usage` carries none in the upstream adapter |

Result: no compatibility change for Codex; no code changed (AC-04).

## Claude (pin v18.1.22; v18.3.2 for the T-034 money audit)

`ai/src/registry/oauth/anthropic-constants.ts` has no commit since the pin.

| Commit | File | Function | Change | Decision |
| --- | --- | --- | --- | --- |
| 6d838a104b | ai/src/usage/claude.ts; consumed in ai/src/auth/usage.ts | `claudeUsageProvider.failureBackoffMs` (`FAILURE_BACKOFF_MS` = 60 s), `#fetchUsageCached` | `/api/oauth/usage` answers 429 with `retry-after: 0` and keeps the account limited for minutes; OMP now waits 60 s before re-polling instead of its shared 10 s | **Adopted**: `ClaudeQuotaClient.GetQuotaAsync` treats a zero Retry-After as absent and gates the next read for the one-minute fallback. T-065's installed-app logs saw the same `Retry-After` 0 |
| 3d4d323009, e45b49c0d4, a834aa1b09, bde8fdd4cf, ec7818e0d3 | ai/src/usage/claude.ts, claude-reset.ts, claude-api.ts | `parseClaudeResetCreditsFromUsagePayload`, `listClaudeResetCredits`, `consumeClaudeResetCredit`, `looksLikeUsagePayload`, `hasUsageData` | Saved rate-limit resets: `cedar_ember` and `juniper_tide` program blocks in the usage payload, probe queries such as `?cedar_ember=1&skip_spend=1`, and redemption POST `/api/organizations/{org}/reset_rate_limits` | Backlog T-069 for a read-only inventory; redemption is a provider write and stays excluded. The current parser ignores the new keys |
| 3d4d323009 | ai/src/usage/claude-api.ts | `buildClaudeOAuthHeaders`, `claudeOAuthBaseUrls` | The usage headers and the configured-host fallback move into a shared helper; same host, path and `oauth-2025-04-20` beta | Not applicable: no wire change for the canonical host AI Usage uses |
| 2282226655 | ai/src/registry/oauth/anthropic.ts, ai/src/usage/claude.ts | `fetchAnthropicBootstrapIdentity`, `getClaudeCodeUserAgent` | Claude Code User-Agent version is discovered at run time | Not applicable: AI Usage keeps its truthful `AiUsage/0.1` identity (claude.md), which was accepted live |
| 6f90b2037d | ai/src/usage/claude.ts | `claudeUsageToken` | Accepts an OAuth token supplied as an environment API key | Not applicable: AI Usage reads no environment or CLI tokens |
| 5976388935, 7dda6296b1 | ai/src/usage/claude.ts | `claudeRankingStrategy.healableBlockScopes` | Clears tier blocks from a fresh report | Not applicable: OMP credential selection |
| 9013268ba1, d724165ae6 | ai/src/usage/claude.ts | `cacheVersion`, `usageStatus` | Cache version and helper merge; `buildClaudeExtraUsageLimit` keeps its fields | Not applicable: no wire change. The T-034 statement that `ClaudeExtraUsage`/`ClaudeSpend` carry no money period still holds at v18.9.0 |
| 9013268ba1 | catalog/src/compat/rules/auth/anthropic.kdl | `auth "anthropic"` | `org-scoped-identity #true` | Not applicable: AI Usage already binds account and organization and rejects a conflicting replacement |
| 147b7db71b | ai/src/registry/engine/oauth-code.ts, common.ts, refresh.ts | `DeclarativeOAuthCodeFlow.generateAuthUrl`, `exchangeToken`, `applyUserinfo`, `loginClient` | `{base}`/`{auth}` templates for rules with `base-url`/`auth-url` | Not applicable: the Anthropic rule declares neither, so its URLs are unchanged |
| 0bc99de927 | ai/src/registry/engine/refresh.ts | `createRequestRefresh` | Adds `organization_id: credentials.activeOrganizationId` to the refresh body | Not applicable: only the Factory Droid hook sets that value, and `postTokenRequest` drops undefined parameters, so the Anthropic refresh body is unchanged |
| c767616cfc | ai/src/registry/oauth/callback-server.ts | `serializeCallbackState` | Escapes `<` in the state embedded in OMP's callback result page | Not applicable: AI Usage's loopback page (`CallbackRequest.RespondAsync`) writes only constant messages |
| 9013268ba1, a834aa1b09, bde8fdd4cf, 0bc99de927, fde4d79303, 56e5bea2e1, f81261100a, 6962d5cd2e | ai/src/auth/usage.ts, ai/src/auth/usage-cache.ts (split from auth-storage.ts) | `#fetchUsageCached`, `#usageRequestInFlight`, `USAGE_FAILURE_BACKOFF_MS` | Broker polling, cache keys with organization, SQLite and settings changes | Not applicable apart from the per-provider backoff adopted above |
| 9013268ba1, 8dfd37d6d9, 65a333a337, d79f856677, 6de123da2b, 236f422df5, 0bc99de927, e185e9b586, 383cac4bf1, bf40ce2e3a | ai/src/auth/refresh.ts (split from auth-storage.ts) | `OAuthRefresher`, `#recentMint`, `OAUTH_REMINT_COOLDOWN_MS` | Credential-pool refresh leases, a five-minute cooldown after a fresh token is rejected so recovery does not rotate again, broker recovery reasons and mint provenance, disable events | Not applicable: OMP pool and broker coordination; the provider token requests are unchanged. AI Usage already renews at most once per refresh after a usage 401 (T-064) |
| a87ed11210 | ai/src/auth-storage.ts, ai/src/error/auth-classify.ts | Anthropic error classification | `oauth_not_allowed_for_organization` on inference rotates to another pooled account | Not applicable: inference path and account rotation |

## Copilot (pin v18.2.6)

`ai/src/registry/oauth/github-copilot.ts` (device flow, polling, refresh helper) and
`catalog/src/wire/github-copilot.ts` (`COPILOT_GITHUB_HEADERS`) have no commit since the pin.

| Commit | File | Function | Change | Decision |
| --- | --- | --- | --- | --- |
| d8b21928ec | ai/src/usage/github-copilot.ts | `normalizeBillingUsage` | Window id `billing-period` → `monthly` | Not applicable: the API-key personal-billing branch, excluded in copilot.md. `fetchInternalUsage`, `parseQuotaDetail` and `normalizeQuotaSnapshots` are unchanged |
| 0bc99de927 | ai/src/registry/engine/refresh.ts | `createRequestRefresh` | `organization_id` in declarative refresh bodies | Not applicable: Copilot does not use the declarative refresh |

Result: nothing relevant changed for Copilot; no code changed (AC-04).

## Antigravity (pin v18.2.6)

`catalog/src/compat/rules/auth/google-antigravity.kdl`, `ai/src/registry/oauth/google-antigravity.ts`,
`ai/src/registry/oauth/google-oauth-shared.ts` and `ai/src/registry/engine/oauth-code.ts` have no
commit since the pin: authorization, token exchange, userinfo, project discovery and provisioning
are unchanged.

| Commit | File | Function | Change | Decision |
| --- | --- | --- | --- | --- |
| 7f2a17e5cf | catalog/src/wire/gemini-headers.ts | `DEFAULT_ANTIGRAVITY_VERSION`, `getAntigravityUserAgent` | Pinned client version 2.8.0 → 2.19.1: with 2.8.0 the backend omitted newer models from its listing | **Adopted**: `AntigravityHttp` default version 2.19.1, so the control-plane identity is `antigravity/hub/2.19.1 (aidev_client; os_type=darwin; arch=arm64; cl=963137146)`; `AIU_ANTIGRAVITY_CLIENT_VERSION` still overrides it. The owner-directed identity exception (antigravity.md) is unchanged in scope |
| 7f2a17e5cf, 67667247e8 | catalog/src/wire/gemini-headers.ts | `ensureAntigravityVersion` | Failed manifest lookups are suppressed for 10 minutes; a caller's abort ends only its own wait | Not applicable: AI Usage does not poll the vendor's update manifest |
| 9013268ba1 | ai/src/usage/google-antigravity.ts | `antigravityUsageProvider.cacheVersion` | OMP cache version 2 | Not applicable: `fetchAntigravityUsage`, `buildQuotaSummaryReport` and the request body are unchanged |
| 0bc99de927 | ai/src/registry/engine/refresh.ts | `createRequestRefresh` | `organization_id` in the refresh body | Not applicable: undefined for this provider and dropped, so the Google refresh body is unchanged |

No OAuth client id or secret was copied; the registration still comes from the device
environment.

## Checks

| ID | Result | Evidence |
| --- | --- | --- |
| C2 | PASS | valid, no diagnostics |
| C4 | PASS | 943/943 (one new test) |
| C5 | PASS | 348/348 |
| C6 | PASS | clean |
| C7 | PASS | 0 warnings, 0 errors |
| C8 | PASS | Merged tree `784df91`, 3/3 against the Release unpackaged build, desktop lock |
| C3 | PASS | Merged tree `784df91`, valid, no diagnostics; C2, C4 (943/943), C5 (348/348), C6 and C7 repeated there and PASS |

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | The four tables above; codex.md, claude.md, copilot.md and antigravity.md name v18.9.0 with `source_verified_at: 2026-10-12` and unchanged `live_verified_at` |
| AC-02 | PASS | `ClaudeQuotaClientTests.AZeroRetryAfterStillDefersTheNextReadForTheFallbackDelay` (red before the fix: a second provider call at once; fixture `retry-after: 0` from upstream test `auth-storage-usage-cache.test.ts` at 6d838a104b) and `AntigravityProtocolTests.TheClientVersionIsOverridableAndRejectsAnUnusableValue` (red before the fix: 2.8.0; expected string built from v18.9.0 `getAntigravityUserAgent`); C4, C5, C7, C2 and C6 above |
| AC-03 | PASS | T-068 (Codex credit-funded overage) and T-069 (Claude saved resets) in the backlog with status research-needed; the diff adds no parsing of `overage_limit_reached`, `cedar_ember` or `juniper_tide` |
| AC-04 | PASS | Codex and Copilot: records state that nothing relevant changed, with the v18.9.0 reference; no source file of either provider changed |

## Not run

| Item | Reason |
| --- | --- |
| Live Claude rate-limit behavior and live Antigravity reading at 2.19.1 | Post-deploy owner checks (R-190); no live provider calls in this task |

## Review

Focused independent T3 review (`aiu-reviewer`, convergence-review) of `23e48e0..7b9067c`:
APPROVE, no Critical or Important findings. The reviewer re-ran `git log <pin>..v18.9.0` on every
named file and confirmed in a temporary worktree at `23e48e0` that both tests fail at the base
(one extra server call; `2.8.0`). Local `/code-review` and `/security-review` were not run: the
checkout has no `main` ref to diff against, so the reviewer read the code change by hand. Two
Minor findings, both fixed in the commit after `7b9067c`: the Claude test now also checks that
no request goes out 59 seconds after the 429, which pins the one-minute fallback; and the Claude
table now lists the `auth/refresh.ts` and `a87ed11210` commits. Re-check of the fixed lines:
APPROVE: the test now pins the one-minute fallback and the rows match `git log` on
`auth/refresh.ts`; one Minor (a function named `isCoolingDown` that does not exist at v18.9.0)
was fixed by citing `OAuthRefresher.#recentMint` and `OAUTH_REMINT_COOLDOWN_MS`. No open findings.

At the merge (2026-10-12), on the numbered branch against `origin/main`: `/security-review`
found nothing (the client version is a constant behind the existing header guard; the Claude change
only lengthens a backoff). `/code-review` found two Minor items, left as they are: the zero
Retry-After rule lives in the Claude client only, while Copilot and Antigravity keep the old
pattern without evidence that either sends zero; and the first 429 exception still carries a zero
delay, which no Claude caller reads.

## Pending owner checks

- Claude: a rate-limited account reads normally again by itself, and the logs show no second
  429 for it within the same minute, even after a manual refresh.
- Antigravity: the account still connects and reads both quota groups with the 2.19.1 identity.
