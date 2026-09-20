---
id: AIU-009
type: spec
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner selected AIU-009 on 2026-09-20 and directed an OMP-only implementation referencing the local oh-my-pi main clone, live verification through the already signed-in Chrome session in an isolated development profile, no CLI credential import, and automatic execution of the required checks. Detailed criteria derive from the existing backlog outcome, provider order and repository boundaries.
---
# Antigravity end-to-end integration

Deliver an Antigravity connection and subscription quota dashboard through the shared provider library, console and current Windows UI. Antigravity quota is served by Google's Cloud Code Assist control plane for a discovered `cloudaicompanionProject`, not by the Gemini app, the Gemini CLI or a paid Gemini API key. Those are different product surfaces and are never combined.

## Scope

Use the inspected OMP authorization-code flow with Google's published loopback desktop-client shape: the OMP-observed scope set, `access_type=offline`, a loopback callback on `127.0.0.1`, and the form token endpoint at `https://oauth2.googleapis.com/token`. The OAuth client registration itself belongs to OMP, so it is not vendored in this repository; the operator supplies one through `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET`, and an unconfigured device reports a distinct failure without contacting the provider. Establish stable account identity from Google's userinfo endpoint before adopting a grant, and re-establish it before accepting a later quota reading. Persist only the refresh grant, the discovered project, the discovered tier and the last quota reading; keep access tokens in memory.

Discover the project through `v1internal:loadCodeAssist` requests carrying the Antigravity IDE metadata, repeating OMP's project-scoped re-read so the active tier is confirmed before any decision.

Owner amendment, 2026-09-20: the first live run proved that an account with no Cloud Code Assist workspace yields no project and therefore no readable quota, and that OMP reaches one only by provisioning. The owner then selected OMP's behavior: connecting an account that has no tier enrols it in the free tier through `v1internal:onboardUser` and waits for the returned operation. This is a provider-side change to account entitlement. It is confined to the connect path, so resume and refresh can never provision anything; it proceeds only when the provider's own tier listing offers that tier, so anything unrecognized declines the write; and a failed or fruitless provisioning reports no workspace instead of echoing the provider's reason.

Owner amendment, 2026-09-20: the provider then refused a truthfully identified client, answering `UNSUPPORTED_CLIENT` and declining to provision. The owner directed that the Cloud Code Assist control plane be sent the real Antigravity client's `User-Agent`, which unblocked it immediately and established that the gate was the client identity. This is an explicit exception, scoped to that control plane, to the rule kept in the provider records that another application's identity is never presented to overcome a provider rejection. Google's OAuth and userinfo endpoints keep AI Usage's own identity, as do the other three providers, and the exception deepens the Terms of Service exposure recorded in the provider evidence.

Read quota from `v1internal:retrieveUserQuotaSummary`, the endpoint Antigravity's own quota surface uses. Preserve its group and bucket structure, opaque bucket identifiers, remaining fractions, reset times and disabled flags. Derive model groups from the response; never hardcode Gemini/Claude/GPT membership, never merge a shared third-party bucket into per-family quotas, and never present a missing value as zero or unlimited.

Use the existing Core, Infrastructure and Windows projects with their current boundaries. Core owns credential-free contracts; Infrastructure owns transport, parsing and DPAPI CurrentUser persistence; Windows owns browser launch, presentation and lifetime. The provider console exercises the same implementation. One active connection per provider follows the existing product limitation.

Excluded: the legacy `v1internal:fetchAvailableModels` model-catalog quota fallback and its window inference, the sandbox endpoint fallback, CLI discovery/import, inference or model routing, paid-tier purchase or upgrade, multi-project selection, and Gemini CLI or Gemini API quota. No new package or service is selected by this specification.

Owner direction, 2026-09-20: the agent performs live verification independently using an isolated development state directory, and runs all required checks at the end without further confirmation. This authorizes browser sign-in and consent for this task only, and the owner separately accepted the Terms of Service exposure on their own account after being shown it. The free-tier provisioning above is authorized by the amendment that introduced it. Nothing here authorizes reading or importing source CLI credentials, or changing unrelated Google account state.

In practice the agent's browser-automation tools were refused by the session's permission controls, so the owner approved each consent screen themselves and the agent drove everything else.

## Acceptance

- AC-01: Record exact stable OMP source references, the official Google OAuth and Cloud Code Assist boundary, separate authentication and quota classifications, registration and scope provenance, side effects, permission uncertainty and live verification dates in the provider evidence record.
- AC-02: Shared authentication performs a loopback authorization-code exchange with state and PKCE, reports an absent or unusable device registration without any provider request, rejects mismatched state and unsafe values, maps denial, cancellation, expiry, invalid grant and malformed responses to distinct outcomes, and verifies stable account identity before adopting a grant. No credential, code or account identifier enters logs, arguments, fixtures or persistent presentation state.
- AC-03: An already-provisioned account is discovered by reads alone. Provisioning happens only on the connect path, only when the provider reports no current tier and actively offers the free tier, and its operation is awaited with a bounded deadline whose poll target cannot be redirected by the response. A failed, timed-out or fruitless provisioning, and any tier listing that does not clearly offer the tier, report a distinct failure without echoing provider text.
- AC-04: App-owned protected state supports resume, refresh, reconnect and local disconnect. Identity mismatch, cancellation, failed writes or deletes, expiry, invalid grant and refresh-token rotation have tested outcomes. A failed new connection preserves the last valid state; existing Codex, Claude and Copilot records remain readable.
- AC-05: Product composition enables only the verified Antigravity capability. Windows shows the existing Antigravity provider slot with browser sign-in, quota, error and stale states, cancellation and tray/lifetime behavior. Demo isolation and the other three providers remain intact; future features remain unavailable.
- AC-06: Relevant Infrastructure and Presentation regressions, validator and document validation, diff check, MSIX build and applicable local unpackaged Windows smoke pass. Live evidence covers connection, quota reading, Exit/relaunch resume, refresh and local disconnect in an isolated development profile. Unavailable cases remain NOT_RUN or BLOCKED.
- AC-07: Primary integrated review and required focused independent credential/state review have no unresolved material findings. Commit and push to `main` under CONTRIBUTING as work progresses; no release is authorized by task selection.
- AC-08: Quota mapping preserves group and bucket structure, opaque identifiers, unknown, exhausted and disabled states, reset times and window durations, and never fabricates a percentage, amount or restriction from a missing field.
- AC-09: The Cloud Code Assist control plane receives the Antigravity client identity and nothing else does. Google's OAuth and userinfo endpoints, and the Codex, Claude and Copilot providers, keep AI Usage's own identity, and the per-device version override cannot inject a header.
