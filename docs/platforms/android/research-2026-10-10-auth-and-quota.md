---
subject: android-auth-and-quota-paths
source_verified_at: 2026-10-10
live_verified_at: null
classification: method-specific; see the per-provider tables
confidence: mixed; stated per finding
backlog: T-018
---
# Android: authentication and quota paths (research, 2026-10-10)

This record covers research only. It selects no Android work and approves no stack, and it does
not amend R-011 or the [Android README](README.md). T-018 stays `idea`.

## Method and limits

The owner asked for maximum-depth research with parallel agents. Seven independent research agents
covered one lane each:

- Claude
- Codex
- Copilot
- Antigravity
- Android platform constraints
- stack and code reuse
- prior art with alternative architectures

Each agent first read the matching [provider records](../../providers/README.md) and the
provider-evidence skill. The rules for every lane:

- Only public documentation, public source code and public issue reports were used.
- No agent signed in, called an authenticated provider endpoint or read a credential store.
- One anonymous request fetched a public file (`claude.ai/.well-known/assetlinks.json`).

Consequences:

- Every finding below is source-level evidence at best. Nothing is live-verified on Android.
- Third-party reports (issues, blogs, store snippets) are marked as such and carry medium or low
  confidence.
- The primary agent synthesized the lane reports. It did not re-fetch every cited page.

## Summary

There are two independent decisions, and they should not be mixed up:

1. **Where the phone gets its readings.** Either the phone holds its own provider grants and polls
   the providers itself, or it receives sanitized readings from the Windows app that already holds
   grants.
2. **For any provider the phone polls itself, which authentication path to use.**

The recommended order:

1. **A companion reader first.** The Windows app publishes end-to-end-encrypted, credential-free
   readings, and a thin Android app with a widget shows them.
   - No new provider grants and no extra provider traffic.
   - Covers all four providers, including the two with explicit third-party restrictions.
   - Cost: readings go stale while the PC sleeps, and staleness must be shown honestly.
2. **Optional hybrid.** The phone gets its own grants only for Codex (device code) and Copilot
   (GitHub device flow). These two have clean phone flows that need no redirect.
3. **Claude and Antigravity stay desktop-relayed.**
   - A standalone phone path exists for Claude (manual-code OAuth in a Custom Tab), but it doubles
     traffic to a throttled endpoint under an explicit Anthropic restriction.
   - Antigravity on a phone carries a documented account-suspension risk and needs the spoofed
     client identity on a second device.
4. **Never use WebView cookie scraping.** Every surveyed Android Claude app does it, but it has
   the widest credential exposure, Google sign-in fails in a WebView, and Cloudflare breaks it.

Stack: native Kotlin with Jetpack Compose, Glance and WorkManager. For a companion reader the
phone holds no provider code, so reusing the C# provider code has no value. See [Stack](#stack).

## Per-provider findings

### Claude

| Method | Android mechanism | Quota | Risk | Confidence |
| --- | --- | --- | --- | --- |
| Claude Code public client, manual code | Custom Tab to `claude.ai/oauth/authorize` with `code=true`, PKCE. The callback page `https://platform.claude.com/oauth/code/callback` (formerly `console.anthropic.com`) shows `CODE#STATE` for the user to paste. No listener is needed | `GET api.anthropic.com/api/oauth/usage`, the same parser as Windows | Explicit Anthropic restriction on third-party Claude.ai login (see [claude.md](../../providers/claude.md)). Usage endpoint 429 storms reported throughout 2026 | Medium-high that it works; manual path is live NOT_RUN on Windows as well |
| Same client, loopback | Custom Tab redirect to `http://localhost:<port>/callback` received by an in-app listener kept alive by a `shortService` foreground service | Same | Same, plus: any app can bind the port and read the code; process kill; future Chrome Local Network Access gating | Medium |
| claude.ai session cookie in a WebView | WebView login, keep `sessionKey` and `cf_clearance`, call `claude.ai/api/organizations/{org}/usage` | Same shape as OAuth usage | Directly violates the "collect, store or intermediate session tokens" clause and the Consumer Terms. Google sign-in blocked in WebView. Cloudflare challenges | High on mechanism (three open-source Android apps), medium on durability |
| Official APIs (Admin usage/cost, Enterprise analytics) | Not applicable | API-key and org usage only, not subscription percentages | None | High that they do not answer the question |

New Android-specific findings:

- **Claude app intercepts links.** The Claude Android app (`com.anthropic.claude`) claims all
  claude.ai links (`handle_all_urls` in the public assetlinks file), so an authorize URL may open
  in that app instead of the Custom Tab. The app may need to pin the intent to the browser.
- **One grant per login.** Each login is its own grant with single-use rotating refresh tokens, so
  a phone login should not disturb the desktop grant, as long as tokens are never copied between
  them.
- **Scope requirement.** Usage requires `user:profile`. Tokens from `claude setup-token` get
  `403 oauth_scope_insufficient`.

### Codex

The latest stable source checked is `rust-v0.162.1`, commit `092d3acd6bec3e3a14bdc7e7a2810ab628ab759d`.
[codex.md](../../providers/codex.md) is pinned to `rust-v0.154.0`.

| Method | Android mechanism | Quota | Risk | Confidence |
| --- | --- | --- | --- | --- |
| Device code with the Codex public client | Unchanged flow in `codex-rs/login/src/device_code_auth.rs`. The user approves at `auth.openai.com/codex/device` in any browser. No redirect or listener | `GET chatgpt.com/backend-api/wham/usage` with `ChatGPT-Account-Id`, the same parser as Windows | Reuse of the Codex public client is permission-unknown, as on Windows. Device code is still **beta**: it must be enabled in ChatGPT security settings (personal) or by a workspace admin | High on flow; medium on availability |
| Browser PKCE, loopback | Current source builds `http://127.0.0.1:{port}/auth/callback` with port 1455 and fallback port 1457 kept "in sync with the ... Hydra redirect URI allow-list". The app binds 127.0.0.1:1455 during a Custom Tab login | Same | Port collisions (for example the Codex CLI in Termux) and process kill | Medium |
| Codex access token (paste) | Validate with `GET auth.openai.com/api/accounts/v1/user-auth-credential/whoami` (`personal_access_token.rs`) | `wham/usage`, inferred from CodexBar only | Official credential type, but **Business and Enterprise only**, and not documented for monitoring | Medium |
| Sign in with ChatGPT (DevDay 2026-09-29) | `dynamic_agent_client`, loopback, audience `api.openai.com/v1` | **No usage endpoint**. It is documented for inference only | n/a | High that it does not serve quota |
| ChatGPT web session in a WebView | `/api/auth/session` access token | `wham/usage` | Full account session on the phone, Google sign-in blocked, Cloudflare | Not recommended |

More findings:

- **Prior art.** [guberm/Codex-Usage-Android](https://github.com/guberm/Codex-Usage-Android)
  already ships device code plus `wham/usage` with an honest User-Agent and Keystore storage. It
  also consumes reset credits, which a monitor must not copy.
- **Cloudflare.**
  - The official client keeps Cloudflare cookies (`__cf_bm`, `__cflb`, `__oailb`), which shows bot
    management runs on these routes.
  - One public report has an HTML 403 challenge on `/backend-api/codex/usage` with a
    `codex_cli_rs` UA while plain `/wham/usage` worked.
  - Keep `/wham/usage` and the truthful UA.
- **Grant independence is not proven.** One unanswered report (openai/codex#17340) claims a new
  login invalidated other hosts' refresh tokens.
- **Official Codex widget.** OpenAI shipped a Codex usage widget on iOS (reported 2026-10-06). If it
  reaches Android, Codex may leave scope.

### Copilot

| Method | Android mechanism | Quota | Risk | Confidence |
| --- | --- | --- | --- | --- |
| GitHub device flow, OpenCode client `Ov23li8tweQw6odWQebz`, `read:user` | Show the code, open `github.com/login/device` in a Custom Tab, poll. No secret, no redirect | Undocumented `GET api.github.com/copilot_internal/user`, the same parser as Windows | Reused client; consent screen names OpenCode | High (desktop live PASS on Free, 2026-09-18) |
| App-owned OAuth App or GitHub App | Device flow or redirect | `/copilot_internal/user` **probably rejects app-owned client IDs** (Jarvis PR #347; per-endpoint allow-listing suggested by opencode #20759) | Clean attribution | Medium-high that quota is refused |
| Fine-grained PAT or GitHub App with "Plan: read" | Paste a token, or use a GitHub App device flow | Official `GET /users/{username}/settings/billing/ai_credit/usage` and `premium_request/usage` (API 2026-03-10). **Usage only**: no entitlement, remaining or reset fields. Personally billed paid plans only | None | High on schema; GitHub App eligibility unverified |
| Copilot SDK `account.getQuota` | Not available: the SDK drives a spawned Copilot CLI process | n/a | n/a | High |

More findings:

- **AI-credit schema.** Since the 2026-06-01 move to AI credits, `/user` payloads add
  `token_based_billing` and `credits_used`. Free accounts show `has_quota:false`, and
  `quota_reset_at` can be 0. The parser needs these fields before any Android port.
- **Token cap.** GitHub keeps at most ten tokens per user, app and scope. A phone login with the
  same client could revoke an older desktop or OpenCode token.
- **No limits endpoint.** The newer token-based session and weekly limits have no known endpoint.

### Antigravity

| Method | Android mechanism | Quota | Risk | Confidence |
| --- | --- | --- | --- | --- |
| Existing Google desktop-type client, Custom Tab, loopback | Google still allows loopback for desktop client types; only the Android, iOS and Chrome types are blocked. Listen on `127.0.0.1:<any>`, with a pasted redirect URL as fallback. The client secret must be entered on the device, never shipped in the APK | `v1internal:retrieveUserQuotaSummary`, same as Windows, with the Antigravity User-Agent | **Highest.** The FAQ names suspension, and bans of third-party Antigravity OAuth users were reported February to May 2026. A spoofed `darwin/arm64` identity from a phone network adds an inconsistent signal. The pinned client version ages out | Medium on transport, high on risk |
| Owner's own Android OAuth client | Google Identity Services `AuthorizationClient` | The backend gates by client (`UNSUPPORTED_CLIENT`, `GOOGLE_TOS_NOT_SUPPORTED_BY_CLIENT`), so it is probably refused | High | Low |
| Gemini CLI client and `retrieveUserQuota` | Paste-code flow suits a phone | Wrong surface. Google stopped serving Gemini CLI for individuals, AI Pro and AI Ultra on 2026-06-18 | n/a | High; exclude |
| Local Antigravity process (CodexBar style) | Antigravity does not run on phones | n/a | n/a | High; exclude |
| Official quota API | None exists; only the in-product page and the CLI `/usage` panel | n/a | n/a | High |

## Android platform constraints

- **Redirects.**
  - **Chrome Auth Tab cannot be used.** It rejects `http` redirects.
  - **Custom Tab plus loopback listener.** This is the only way to honor the providers' fixed
    loopback URIs.
    - Chrome Local Network Access (Chrome 142 and later) does not currently gate top-level
      navigations, so the redirect should pass. That is a future-breakage risk.
    - Android 17's `ACCESS_LOCAL_NETWORK` does not cover same-profile loopback. A work-profile
      browser with a personal-profile app fails.
    - Bind both 127.0.0.1 and ::1 for `localhost` URIs.
    - Keep the listener alive with a `shortService` foreground service.
    - Persist the PKCE verifier and state so a killed process can still finish the exchange.
  - **Device code and manual paste.** These are the robust paths.
  - **AppAuth-Android.** It looks lightly maintained and lacks loopback support. Use a small
    hand-written PKCE flow with `androidx.browser` instead.
- **Storage.**
  - EncryptedSharedPreferences (security-crypto) is deprecated, and `datastore-tink` is alpha only.
    MAUI `SecureStorage` is built on the deprecated component.
  - Use a non-exportable Keystore AES-GCM key over a small encrypted file. StrongBox where
    available, with fallback.
  - No biometric binding, which would block background refresh.
  - Exclude the token and key files from backup and device transfer.
  - On a decrypt failure, show "sign-in required".
- **Background.**
  - WorkManager has a 15-minute periodic floor. Standby buckets (Android 16 quotas) reduce real
    cadence to roughly 15–30 min in use and hours overnight. The Rare bucket has no network.
  - Apps with an active widget are exempt from the Restricted bucket.
  - Do not use exact alarms or a `dataSync` foreground service.
  - Glance `updatePeriodMillis` has a 30-minute floor. Push updates with `updateAll()` from the
    worker instead.
- **Network.**
  - Use OkHttp with HTTP/2. Detect Cloudflare challenge HTML and report it as a typed "blocked by
    provider" state, never as quota.
  - No certificate pinning, and no user CAs in release builds.
- **Distribution.**
  - Play Store policy prohibits apps that use a service in violation of its terms, so this app
    belongs off Play.
  - Install a personal signed APK by ADB (exempt from developer verification) or through GitHub
    Releases with Obtainium.
  - Register a free limited-distribution developer account before verification enforcement
    reaches the owner's region (global expansion is planned for 2027).
  - Target SDK 36.

## Stack

Code inventory (non-test C#):

- **Core:** about 1,150 lines, fully portable.
- **Infrastructure:** about 8,260 lines, including about 5,380 lines of provider code. DPAPI is
  called in only two files, `Providers/ProviderStateLease.cs` and `Persistence/StateMaintenance.cs`.
  Because of them, `SupportedOSPlatform("windows")` spreads to about twenty types.
- **Fixtures:** only two JSON fixtures are files. Most provider payloads are inline C# strings in
  tests.

| Option | Logic reuse | Widget and background | Risk | Fit |
| --- | --- | --- | --- | --- |
| Kotlin, Compose, Glance, WorkManager | 0% of code; specs and fixtures reused | Best (Glance 1.2 stable, 2026-08-26) | Low | **Recommended** |
| .NET for Android, native views, shared Core | ~85–90% (needs an `IStateProtector` seam over DPAPI) | RemoteViews only; Glance requires Kotlin | Medium: .NET 11 moves mobile to CoreCLR (GA targeted November 2026); no Android NativeAOT | Only with an owner decision amending R-011 |
| Kotlin Multiplatform | 0% | Glance through androidMain | Low-medium | Only if iOS becomes a goal |
| Uno 6.6 or Avalonia 12 | As .NET, plus some UI | No framework widgets | Medium-high | No |
| Flutter or React Native | 0% | Native widget code still needed | Widget and background plugins | No |

For the companion reader, the phone parses one app-owned snapshot format, so the reuse argument
disappears and Kotlin is clearly right. For the hybrid, only Codex and Copilot are ported, about
1,800 lines of provider code.

Under any option, first extract the inline test payloads, plus the budget and pace input-output
vectors, into a language-neutral fixture corpus that both suites must pass.

## Architectures compared

| Option | Freshness | PC off | Credentials on phone | Provider-policy exposure | Effort |
| --- | --- | --- | --- | --- | --- |
| Standalone phone app | WorkManager, 15 min or more | Works | Second grant per account, four auth stacks | Highest; doubles Claude traffic | Large |
| **Companion reader (recommended first)** | Desktop cadence; stale while the PC sleeps | Last reading, marked stale | **None** | None added | Medium |
| Hybrid (Codex and Copilot on phone, rest relayed) | Fresh for two providers | Partly | Two device-flow grants | Medium for those two | Large |
| Server-side poller holding refresh tokens | 5 min | Works | Tokens on an internet-reachable host | High | Medium; **rejected**: account-takeover blast radius and rotation races |
| WebView scraping | 15 min or more | Works | Full session cookies | High | Medium, brittle; **rejected** |
| Alerts only (desktop sends threshold and reset notifications) | Event-driven | No | None | None | Small; Windows-only change |

Companion relay transports:

| Transport | Assessment | Cost |
| --- | --- | --- |
| LAN only (NSD plus local HTTPS) | Respects "data remains on their machine" best, but works only at home | n/a |
| Owner's OneDrive app folder (Graph `Files.ReadWrite.AppFolder`) | No server to run, but needs OAuth (MSAL) on both sides and an Entra registration | n/a |
| Owner's Cloudflare Worker plus KV | QR pairing carries a symmetric key and a read token, so the phone needs **no OAuth at all**. Ciphertext only on the server; about 100 lines | Free tier: 1,000 writes per day (a 5-minute cadence is about 288) |
| FCM data messages | Puts a powerful service-account credential on the PC | n/a |
| Tailscale | Requires the VPN on the phone | n/a |

Best prior art in this direction:

- [Starbridge](https://github.com/T0mSIlver/starbridge): an end-to-end-encrypted relay with an
  Android widget, fed by the CodexBar CLI.
- quota-ios: a Mac server with a paired phone.

Any relay conflicts with the [vision](../../product/vision.md) statement that "credentials and data
remain on their machine". It is defensible only if it carries readings, never credentials, and is
end-to-end encrypted on storage the owner controls. That is an owner decision.

A freshness caveat: Claude and Codex limits also drain from phone, web and cloud use. Relayed
readings can be wrong while the PC sleeps. A reset time passing may be shown as "window reset
expected since last reading", never as a new observation (constitution).

## Owner decisions needed (always-ask)

1. **Android goal.** Select T-018 now, or keep Windows first (G-005 says after Windows
   stabilizes).
2. **Relay.** Is a readings-only relay acceptable under the vision? If so, which store: the owner's
   Cloudflare Worker (recommended for no phone OAuth), the owner's OneDrive, or LAN only?
3. **Phone grants.** Should the phone ever hold provider grants? Recommendation: Codex and Copilot
   only, and only after the companion reader exists.
4. **Claude and Antigravity on the phone.** Recommendation: no; relay only.
5. **Shared .NET Core.** Amend R-011 for a shared .NET Core? Recommendation: no; Kotlin with a
   shared fixture corpus.

## Live checks needed before any phone-grant implementation

All are NOT_RUN. Each needs owner authorization and runs on the owner's device.

- **Codex:**
  - Device-code toggle behavior when on and off.
  - `wham/usage` from a mobile IP with an honest UA (Cloudflare).
  - Whether a phone login invalidates the desktop grant.
  - Whether the redirect allow-list still includes `localhost:1455` alongside `127.0.0.1:1455` and
    `127.0.0.1:1457`.
- **Copilot:**
  - App-owned client against `/copilot_internal/user`.
  - Plan-read PAT against `ai_credit/usage` on a paid plan.
  - The ten-token cap revoking the desktop token.
  - Paid-plan payloads under AI credits.
- **Claude:**
  - Claude app interception of the authorize link.
  - The manual `CODE#STATE` page in a phone browser.
  - Desktop and phone grant independence over several refreshes.
  - The 429 threshold for two pollers on one account.
- **Antigravity** (only if decision 4 changes): desktop-client loopback from a Custom Tab, and
  backend acceptance.
- **Platform:**
  - Chrome redirect to loopback on current stable, for `localhost` and `127.0.0.1`.
  - Listener survival during a long login on aggressive OEM builds.
  - Real WorkManager cadence by standby bucket.
  - Backup and restore yielding "sign-in required".

## Key sources (accessed 2026-10-10)

- **Claude:**
  - [Anthropic credential-use policy](https://code.claude.com/docs/en/legal-and-compliance#authentication-and-credential-use)
  - [authentication docs](https://code.claude.com/docs/en/authentication)
  - [usage 429 issue #31021](https://github.com/anthropics/claude-code/issues/31021)
  - [refresh reuse issue #100531](https://github.com/anthropics/claude-code/issues/100531)
  - [CodexBar Claude notes](https://github.com/steipete/CodexBar/blob/main/docs/claude.md)
- **Codex:**
  - [official source at rust-v0.162.1](https://github.com/openai/codex/tree/092d3acd6bec3e3a14bdc7e7a2810ab628ab759d/codex-rs/login/src): `device_code_auth.rs`, `server.rs`, `auth/manager.rs`, `auth/personal_access_token.rs`
  - [auth docs](https://learn.chatgpt.com/docs/auth)
  - [access tokens](https://learn.chatgpt.com/docs/enterprise/access-tokens)
  - [SIWC sessions](https://developers.openai.com/siwc/token-sharing-open-source/profiles-and-sessions)
  - [Codex-Usage-Android](https://github.com/guberm/Codex-Usage-Android)
  - [grant-invalidation report #17340](https://github.com/openai/codex/issues/17340)
- **Copilot:**
  - [GitHub OAuth device flow](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
  - [billing usage REST](https://docs.github.com/en/rest/billing/usage)
  - [usage-based billing changelog](https://github.blog/changelog/2026-06-01-updates-to-github-copilot-billing-and-plans/)
  - [Jarvis PR #347](https://github.com/rajbos/Jarvis/pull/347)
  - [opencode #20759](https://github.com/anomalyco/opencode/issues/20759)
- **Antigravity:**
  - [Antigravity FAQ](https://antigravity.google/docs/faq)
  - [Google native-app OAuth](https://developers.google.com/identity/protocols/oauth2/native-app)
  - [loopback migration](https://developers.google.com/identity/protocols/oauth2/resources/loopback-migration)
  - [Code Assist release notes](https://docs.cloud.google.com/gemini/docs/codeassist/release-notes)
  - [CodexBar Antigravity notes](https://raw.githubusercontent.com/steipete/CodexBar/main/docs/antigravity.md)
- **Platform:**
  - [Auth Tab](https://developer.chrome.com/docs/android/custom-tabs/guide-auth-tab)
  - [RFC 8252](https://www.rfc-editor.org/rfc/rfc8252.html)
  - [Android local network permission](https://developer.android.com/privacy-and-security/local-network-permission)
  - [Android 17 behavior changes](https://developer.android.com/about/versions/17/behavior-changes-all)
  - [security-crypto deprecation](https://developer.android.com/jetpack/androidx/releases/security)
  - [power buckets](https://developer.android.com/topic/performance/power/power-details)
  - [developer verification](https://android-developers.googleblog.com/2026/03/android-developer-verification-rolling-out-to-all-developers.html)
- **Prior art and architecture:**
  - [CodexBar](https://github.com/steipete/CodexBar/)
  - [Starbridge](https://github.com/T0mSIlver/starbridge)
  - [When Reset](https://github.com/iebb/when-reset)
  - [claude-usage-android](https://github.com/drknowhow/claude-usage-android)
  - [Cloudflare KV pricing](https://developers.cloudflare.com/kv/platform/pricing/)
  - [Glance releases](https://developer.android.com/jetpack/androidx/releases/glance)
