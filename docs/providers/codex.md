---
provider: codex
source_verified_at: 2026-09-13
live_verified_at: 2026-09-14
confidence: source-verified-live-verified
classification: source-observed-internal-endpoints
backlog: AIU-003
---
# Codex integration evidence

## Scope and provenance
Owner selected a reusable library with console/test verification before UI integration. No personal CLI credential store, token, browser session or authenticated provider response was inspected. Fixtures for this slice are synthetic. No model/inference, purchase or quota-reset redemption request is authorized or needed.

Official latest stable release observed through GitHub releases/latest: rust-v0.154.0, published 2026-09-09. Annotated tag 36eab01061df3cde5f95ec20a526777b430091ba resolves to commit 6b9826e3aa83b1a5947db50f4332cb9c65f1b340. Installed OMP 18.1.19 tag resolves to commit e4dd2ec3b487f216c569281e2cdb7ec476a81f2e. These references describe source behavior, not provider approval for this application.

## Authentication contract
- Official documentation describes ChatGPT subscription browser login and device-code login (beta); device login must be enabled in account security settings or workspace permissions. API-key billing is a separate product and is not substituted here.
- Both pinned implementations use public client ID `app_EMoamEEZ73f0CkXaXp7hrann` and issuer `https://auth.openai.com`. No client secret is embedded. This is the Codex public client, not an app-owned registration; third-party use remains permission-unknown. The console must disclose that limitation before initiating login. Public source and MIT licensing alone do not grant provider permission.
- Selected executable flow: POST `/api/accounts/deviceauth/usercode` with JSON `{ "client_id": "..." }`. Response requires `device_auth_id`, `user_code` (official parser also accepts `usercode`) and a seconds interval (official source parses a numeric string; OMP accepts number/string). Display only the fixed `https://auth.openai.com/codex/device` URL and one-time user code to the interactive user. Never log the opaque device auth ID.
- Poll POST `/api/accounts/deviceauth/token` with JSON `device_auth_id` and `user_code`. HTTP 403/404 means pending for this endpoint only, not quota or token exchange. Official deadline is fifteen minutes; honor interval and cancellation with no busy polling. A 404 at initiation means device login unavailable, not pending.
- Successful polling returns `authorization_code`, `code_verifier` and `code_challenge`. Exchange POST `/oauth/token` as form-urlencoded with `grant_type=authorization_code`, client ID, code, code_verifier and redirect_uri `https://auth.openai.com/deviceauth/callback`. Token response includes access_token, refresh_token and id_token; OMP additionally uses expires_in. The official token helper can derive expiry from JWT `exp`. No invented expiry if both are absent.
- Browser alternative (researched, not required for the console device slice): authorization endpoint `/oauth/authorize`, response_type code, PKCE S256 and random state, loopback callback normally localhost:1455, id_token_add_organizations=true and codex_cli_simplified_flow=true. Pinned OMP scope is `openid profile email offline_access api.connectors.read api.connectors.invoke`. The device flow has no caller-supplied scope field in either inspected implementation; do not invent one or add connector API operations.
- Workspace identity is `https://api.openai.com/auth.chatgpt_account_id`; it is not email or a token hash. OMP resolves access-token claim first, then id-token claim. Official ID-token type also contains chatgpt_user_id, plan type and a FedRAMP flag. JWT decoding is claim extraction, not signature validation. In this slice claims come only from the fixed HTTPS token endpoint; do not accept arbitrary supplied JWTs as authenticated identity. Reject mismatched workspace claims on refresh.
- Official refresh POST `/oauth/token` uses JSON client_id, grant_type refresh_token and refresh_token. Optional returned access/id/refresh fields are merged by official storage logic. A returned refresh token supersedes the prior one; an omitted replacement is not fabricated. `refresh_token_expired`, `refresh_token_reused`, `refresh_token_invalidated`, HTTP 401 and HTTP 400 invalid_grant are terminal reauthentication outcomes. Error strings may be nested or flat; never emit raw error_description/body.
- A copied rotating CLI refresh token is not an independent grant. Official login/refresh persists into CODEX_HOME or OS credential storage; this implementation must not call those storage operations. Console verification uses a separate interactive login and memory-only credentials, serializes refresh for each credential object and never automatically retries auth POSTs. Lost refresh responses cannot prove whether server rotation happened; require a new login rather than promise rollback or silently retry that grant.

## Subscription quota contract
- Passive request: GET `https://chatgpt.com/backend-api/wham/usage`, bearer access token, `ChatGPT-Account-Id` for the selected workspace and a truthful application User-Agent. Official backend also has a distinct CodexApi path `/api/codex/usage`; this slice does not expose arbitrary base URLs or switch backend families.
- Official get_rate_limits_with_reset_credits calls get_rate_limits_for_usage(false): passive readers do not send x-openai-codex-luna-reserve. Do not implement redemption, Reserve, model discovery/inference or purchase calls merely to obtain quota.
- Top-level plan_type is provider data, not an enum closed to current plans. rate_limit contains nullable/optional primary_window and secondary_window plus allowed and limit_reached. Windows contain used_percent, limit_window_seconds, reset_after_seconds and reset_at. Official wire types specify seconds, including Unix reset_at; preserve absolute reset_at precedence, falling back to fetchedAt plus reset_after_seconds only when needed. Do not infer a default five-hour/week duration when missing.
- additional_rate_limits is an optional list with limit_name, metered_feature, optional normal_model_slug and nullable rate_limit. Preserve each supplied group's identity and windows; do not hardcode Spark membership or aggregate unrelated pools. Missing windows/percentages mean unknown, not zero or unlimited. Group allowed/limit_reached flags remain independent of numeric window percentages.
- credits carries has_credits, unlimited and optional string balance. Unlimited is explicit and applies to that credit pool only; never infer unlimited chat from omitted windows. Preserve an unknown balance and avoid inventing credit/message conversion rates. Optional rate_limit_reset_credits.available_count is a count of reset credits, not usage remaining; no detail or redemption calls are needed.
- Optional spend_control.reached and rate_limit_reached_type.type affect eligibility and must not be hidden by an apparently positive window percentage. account_id/user_id exist in the official wrapper; a returned account_id differing from the request must fail closed. Do not expose personal payloads or optional upsell banners in console output.
- 401 means authentication required; 403 access denied (not device pending); 429 carries rate-limited status and Retry-After when present; 5xx provider unavailable. Unknown response values remain unknown or an explicit invalid-response outcome, never fabricated PASS. The endpoint is source-observed internal subscription infrastructure, not an established public monitoring API/SLA.

## Implemented conservative behavior and known limits
- The current memory-only implementation treats a usage-endpoint 401 as a terminal local session condition. This is stricter than the source meaning of authentication-required: it disables the still-held refresh grant and requires a fresh console login. It does not prove that the provider revoked that grant. Current console guidance becomes explicit when `refresh-auth` is attempted; immediate quota-path guidance is tracked in CR-AIU-003-03.
- Account-context claims are not read, per D-177: neither residency nor a FedRAMP flag is parsed, and no residency header is sent, matching the inspected client which extracts only the workspace identity. Conflicting workspace claims between access and id tokens are still rejected, and a workspace change on refresh still fails closed. A live session on 2026-09-14 read real quota for an account carrying `chatgpt_compute_residency` without declaring any region.
- The console obtains internal clients through AddCodexIntegration using explicit friend-assembly access; redirects, cookies and HTTP factory logging are disabled. Ordinary consumers use AddCodexProductSession and cannot construct clients or sessions with a caller-supplied HttpClient. CR-AIU-003-01 is resolved by this accessibility boundary; see [T-11 verification](../specs/AIU-028-architecture-remediation/verification.md#t-11-library-boundary---2026-09-22). No untrusted endpoint is accepted by the console.

## Evidence sources
- [Official release](https://github.com/openai/codex/releases/tag/rust-v0.154.0)
- [Official authentication documentation](https://learn.chatgpt.com/docs/auth)
- [Official device flow](https://github.com/openai/codex/blob/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/login/src/device_code_auth.rs): request_device_code, complete_device_code_login, poll_for_token.
- [Official token/refresh manager](https://github.com/openai/codex/blob/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/login/src/auth/manager.rs): request_chatgpt_token_refresh, classify_refresh_token_failure, RefreshResponse, oauth_client_id.
- [Official token claims](https://github.com/openai/codex/blob/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/login/src/token_data.rs): parse_chatgpt_jwt_claims, parse_jwt_expiration.
- [OMP OAuth](https://github.com/can1357/oh-my-pi/blob/e4dd2ec3b487f216c569281e2cdb7ec476a81f2e/packages/ai/src/registry/oauth/openai-codex.ts): loginOpenAICodexDevice, exchangeCodeForToken, createOpenAICodexAuthorizationUrl, getTokenProfile.
- [OMP quota](https://github.com/can1357/oh-my-pi/blob/e4dd2ec3b487f216c569281e2cdb7ec476a81f2e/packages/ai/src/usage/openai-codex.ts): openaiCodexUsageProvider.fetchUsage, parseUsagePayload, resolveResetTime. Its reset-credit detail fetch and raw payload retention are not copied into this passive console slice.
- [Official wrapper/context](https://github.com/openai/codex/blob/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/backend-client/src/types.rs): RateLimitStatusWithResetCredits, AdditionalRateLimitWithNormalModel.
- [Official generated quota models](https://github.com/openai/codex/tree/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/codex-backend-openapi-models/src/models): rate_limit_status_payload.rs, rate_limit_status_details.rs, rate_limit_window_snapshot.rs, additional_rate_limit_details.rs, credit_status_details.rs, spend_control_status_details.rs.

## Live verification boundary
Live-verified on 2026-09-14 through the console with a locally cloned OMP reference at `C:/Users/danii/projects/oh-my-pi`: browser sign-in, real subscription quota read, explicit in-memory refresh, second real quota read, clean exit. The observed account returned a five-hour primary window, a seven-day secondary window, explicit non-unlimited credits with a zero balance and two available reset credits. Still NOT_RUN: device-code login availability, multi-workspace switching, exhausted-quota and rate-limited responses, long-term refresh rotation and coexistence with a personal CLI session. Third-party reuse of the public Codex client remains permission-unknown; a successful request is not provider approval. No token or raw payload was persisted.

## Provider history (AIU-011)

- source_verified_at: 2026-09-22
- live_verified_at: null
- classification: source-observed-internal-development-endpoints
- confidence: source-verified; existing-session access and complete coverage unverified

The implementation follows official Codex development commit
`2c2a42e65de077c5518ea5b4c3999633ef6a12fc`, not the stable release contract.
The [pinned research](../specs/AIU-011-provider-history/research.md) links the analytics
client, models and account-bound session. OMP's history is local collection and is not used.

Existing ChatGPT bearer/account headers target fixed `/backend-api/wham/` routes for
`usage/daily-token-usage-breakdown`, `usage/credit-usage-events`,
`analytics/daily-workspace-usage-counts`, `analytics/daily-plugin-usage-metrics`,
`analytics/daily-skill-usage-metrics`, `usage/daily-workspace-user-token-usage-breakdown`
and `usage/daily-workspace-user-credit-usage`. Only current-user workspace reports are
requested. Model/product/speed/reasoning variants remain separate reports. Date requests
use inclusive UTC dates, daily grouping where supported, at most 731 days per query;
credit events have no date query and are filtered by returned date.

Numeric tokens, credits, signed credit events, costs, activity counts and returned
breakdown dimensions retain their provider meaning. This is an explicit typed projection,
not a dump of every metadata field. No subscription percentage or price is inferred.
Plugin/skill requests use a top limit of 100; acceptance of that limit is not live verified.
Neither top lists nor an unpaged credit response establish complete account history.
Retention, backend truncation, plan eligibility and summary-only metadata coverage remain
limitations; the UI labels available report coverage as potentially partial.

History uses the session gate and durable grant lease. A started rotating exchange finishes
its bounded transport and durable save before navigation cancellation takes effect. A lost
network response after server rotation remains ambiguous. External account replacement or
response mismatch clears historical cache; optional report denial does not revoke a working
quota session. Individual report failures remain visible; 401/429 stop further requests,
and Retry-After prevents immediate retries. Results are memory-only.

The quota live date above does not verify analytics. No eligible stored Codex grant was
available for the AIU-011 run; see [verification](../specs/AIU-011-provider-history/verification.md).
