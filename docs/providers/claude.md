---
provider: claude
source_verified_at: 2026-09-14
live_verified_at: 2026-09-15
confidence: source-and-live-verified-private-unsupported
classification: method-specific
backlog: AIU-007
---
# Claude authentication and subscription quota evidence

Source inspection on 2026-09-14; owner-led live account verification on 2026-09-15. The owner explicitly authorized a private, unsupported OMP-style implementation in the [scope amendment](../specs/AIU-007-claude-integration/spec.md). Successful live behavior does not establish provider approval. The scope excludes CLI credential discovery/import, inference calls and billing-API substitution.

## Official boundary

Anthropic's current [authentication and credential-use policy](https://code.claude.com/docs/en/legal-and-compliance#authentication-and-credential-use) states: "Anthropic does not permit third-party developers to offer Claude.ai login into their own applications". It also restricts collecting, storing or intermediating Claude.ai credentials/session tokens. The adjacent allowance for customers' API keys and unmodified Claude Code does not establish a quota-monitor exception. No project-specific permission has been provided. This is an explicit restriction, not merely an undocumented endpoint; owner authorization to develop the app is separate from provider permission.

The official [authentication guide](https://code.claude.com/docs/en/authentication) describes native Claude Code browser login/manual-code fallback and several account types; it does not document an app-owned public OAuth registration for this monitor. [Statusline documentation](https://code.claude.com/docs/en/statusline#rate-limit-usage) exposes native five-hour/seven-day percentage and epoch-second reset fields after a session response, with independently absent windows. That is a CLI integration surface, outside this task, and is not a documented standalone quota polling API. The [error reference](https://code.claude.com/docs/en/errors#usage-limits) distinguishes shared session/weekly limits, family-specific limits, entitlement checks and server throttling. These support semantic interpretation, not permission for the private API below. No quota-only third-party exception was found in the inspected official pages; this is not a claim that all possible provider agreements have been examined.

## Exact upstream reference

Primary verification of GitHub's public release API on 2026-09-14 identified [v18.1.22](https://github.com/can1357/oh-my-pi/releases/tag/v18.1.22), published at 19:29:59 UTC, with tag commit `23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d`. The relevant files were fetched at that immutable commit and compared with the clean local OMP clone at `becbf82cb2c0b598e27cf0bf10453065645d915e`; all nine files below matched after normalizing line endings. No checkout, source credential store or upstream settings changed.

| Source at the stable commit | Relevant declaration/function |
| --- | --- |
| [Auth rule](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/catalog/src/compat/rules/auth/anthropic.kdl) | `auth "anthropic"`, login and refresh mappings |
| [Authorization engine](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/registry/engine/oauth-code.ts) | `DeclarativeOAuthCodeFlow`, `resolveCallbackOptions` |
| [Callback engine](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/registry/oauth/callback-server.ts) | `OAuthCallbackFlow`, `parseCallbackInput` |
| [Token mapping/request helpers](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/registry/engine/common.ts) | `mapCredentials`, `postTokenRequest` |
| [Refresh engine](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/registry/engine/refresh.ts) | `createRequestRefresh`, `createRefresh` |
| [Identity hook](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/registry/oauth/anthropic.ts) | `fetchAnthropicBootstrapIdentity`, `anthropicIdentityHook` |
| [Grant lifetime heuristic](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/registry/oauth/anthropic-constants.ts) | `ANTHROPIC_OAUTH_GRANT_TTL_MS` |
| [Quota adapter](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/usage/claude.ts) | `fetchClaudeUsage`, `fetchUsagePayload`, `parseApiLimitEntries`, `buildScopedWeeklyUsageLimits`, `buildClaudeExtraUsageLimit` |
| [Usage coordination](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/auth-storage.ts) | `#fetchUsageCached`, `#usageRequestInFlight`, cache epochs and failure cooldown |

The research agent initially identified v18.1.18 (`00085d4e7dfdcfbf302c122fa2682b410a0f43d1`) as stable. Fresh primary API verification superseded that claim. Auth/identity declarations matched that earlier tag, but its quota adapter lacked the current configurable-host fallback. The current contract below uses v18.1.22 throughout.

## Authentication method: undocumented public-client reuse, restricted

| Item | OMP source behavior; not provider authorization |
| --- | --- |
| Public client | `9d1c250a-e61b-44d9-88ed-5944d1962f5e`, base64 encoded in the auth rule; no client secret |
| Authorization | `https://claude.ai/oauth/authorize`, response type `code`, extra `code=true`, PKCE S256 and random state |
| Scopes | `org:create_api_key user:profile user:inference user:sessions:claude_code user:mcp_servers user:file_upload`; a minimum quota-only set is NOT established |
| Callback | Preferred `http://localhost:54545/callback`; engine allows another loopback port when busy. Provider acceptance of that fallback is NOT live verified |
| Manual fallback | Redirect URL or code input. Callback state is checked; manual input rejects a mismatching supplied state but can accept a raw code without state. Do not infer identical assurance for both paths |
| State/PKCE | State uses 16 random bytes encoded as 32 hex characters. The authorization engine includes challenge/method/state and sends verifier, redirect and state with exchange |
| Token endpoint | JSON POST to `https://api.anthropic.com/v1/oauth/token` for code exchange and refresh; refresh uses `grant_type=refresh_token` |
| Expiry/rotation | `expires_in` seconds with a five-minute skew. `mapCredentials` retains the prior refresh token if a new token is omitted; access/refresh rotation and persistence must have one authority |
| Refresh headers | `anthropic-beta: oauth-2025-04-20` and a Claude SDK-style user agent in OMP. AI Usage's own headers were accepted during the live restart/renewal check on 2026-09-15 |
| Grant lifetime | OMP's roughly 30-day family-expiry constant is explicitly an observed heuristic, not a guaranteed provider contract or timer for declaring a grant invalid |

Login maps `account.uuid`/`account.email_address` and `organization.uuid`/`organization.name`. When identity is incomplete, the hook tries GET `https://api.anthropic.com/api/claude_cli/bootstrap?entrypoint=cli&model=claude-opus-4-8` with a Claude Code-style user agent and OAuth beta header. It reads `oauth_account.account_uuid`, `account_email`, `organization_uuid`, and `organization_name`. Bootstrap failure is swallowed by OMP, so successful token exchange alone does not guarantee stable identity.

Refresh does not remap organization fields; the generic mapping returns only mapped optional fields and expects callers to merge existing identity. A monitor must preserve its verified account/organization binding explicitly and reject conflicting replacements. The OMP refresh engine also checks cancellation after receiving the token response: do not copy that as proof that cancellation is safe around a rotating grant. Likewise, upstream error helpers can include response excerpts; AI Usage must not echo provider bodies or credential-bearing values.

Authorization creates a grant; refresh can rotate/revoke its previous token. Those are side effects even when quota retrieval is read-only. Requesting `org:create_api_key` does not mean this monitor should create an API key. No inference, file/session/MCP mutation, key creation, organization creation, billing change or quota purchase is included in this scope. No supported device flow was established for this monitor.

## Quota method: undocumented OAuth API

`fetchClaudeUsage` sends GET `https://api.anthropic.com/api/oauth/usage` with the bearer access token, JSON accept/content-type headers, a Claude CLI-style user agent and a large beta-header set. If identity is missing, it may additionally GET `/api/oauth/profile` and parse top-level or nested account UUID/email. This profile enrichment does not establish organization identity. These requests do not mint a new grant or run inference, but permission for this external monitor and the minimum working headers/scopes are unverified.

| Payload | Interpretation supported by the inspected code |
| --- | --- |
| `five_hour.utilization`, `seven_day.utilization` | Percent consumed in shared five-hour/seven-day limits; no request/token total is implied |
| `seven_day_opus`, `seven_day_sonnet` | Legacy optional family-specific weekly windows; null does not mean zero or unlimited |
| `limits[]` | Entries carry `kind`, `percent`, `resets_at`, `scope.model.display_name`, optionally `is_active`; shared fallbacks are `session`/`weekly_all`, and `weekly_scoped` provides independently named model-family limits |
| `is_active` | OMP intentionally ignores it when deciding whether a returned group exists; it may indicate the currently binding limit, not presence/absence |
| `resets_at` | ISO timestamp for API payloads; no freshness or reset observation is created by a local countdown |
| `extra_usage` | `is_enabled`, `used_credits`, `monthly_limit`, `decimal_places`, `currency`; OMP assumes exponent 2 when omitted and accepts absent currency as USD. That legacy assumption needs explicit evidence before adoption |
| `spend` | `enabled`, `used`/`limit` monetary objects containing `amount_minor`, `currency`, `exponent`; a present current object takes precedence over legacy extra usage |

OMP uses valid legacy shared buckets before generic shared fallbacks, adds scoped weekly rows, and deduplicates scoped names by a slug. It clamps percentages to 0..100, drops rows lacking numeric utilization, and accepts USD extra usage only. These are upstream normalization choices: AI Usage must preserve unknown values and opaque names, avoid collision-based data loss, and validate its own precedence/invalid-value behavior. A null monetary limit is not a fabricated spending cap; currency amounts must remain distinct from subscription percentages. No combined provider or cross-group percentage is valid.

The header parser consumes `anthropic-ratelimit-unified-{5h|7d|7d_oi}-utilization` as fractions and `-reset` as epoch seconds. Those units differ from the API payload. Cached inference-response headers can be incomplete for model-scoped limits. This task must never issue inference solely to obtain them.

The current adapter tries a configured usage host first and falls back to the canonical host only for evidence of endpoint absence (404/405/410/501 or appropriate empty/non-JSON/non-usage responses). It does not switch hosts for 401/403/transient rejection. AI Usage has no requirement for a custom mirror, so this behavior does not justify introducing configurable credential destinations.

Quota retries are bounded to three attempts for supported transient failures and some malformed/missing responses, with cancellable delay and Retry-After handling. 429 is not retried within a fetch; the caller applies cooldown. `#fetchUsageCached` coalesces requests by cache key/epoch, caches successful reports and can retain the last good report on failure. These mechanics do not establish account-specific rate limits or guarantee a copied refresh token is independent.

## Fixture and verification requirements

The implemented parser and protocol tests use explicitly synthetic fixtures. No real fixture has been captured. Coverage includes valid/missing/conflicting identity, legacy and generic shared limits, multiple scoped rows including `is_active: false`, unknown kinds and duplicate names, null/invalid/exhausted percentages, ISO offsets and malformed resets, enabled/disabled/unknown extra spend, currency/exponent mismatch, malformed JSON and secret-bearing error payloads. The [verification record](../specs/AIU-007-claude-integration/verification.md) records executed checks separately from live evidence.

Live PASS on 2026-09-15: owner-led browser connection, identity validation through the shared client, initial grouped usage, manual refresh, renewal and durable resume after full process exit, local disconnect and disconnected state after another restart. The actual host development package was 2026.9.1416.0. The existing Codex connection remained usable. No credentials or raw provider payloads were captured. See the [live verification record](../specs/AIU-007-claude-integration/verification.md).

Live NOT_RUN: occupied-port callback fallback, isolated manual-code fallback, reduced/minimum scopes, server-side invalidation, throttling, malformed responses and transient-offline stale cache. Failure boundaries have synthetic regression coverage; no provider failures or billable inference were induced. The observed account does not prove every account type, quota group or entitlement. A source comment about OMP is not an AI Usage live result.
