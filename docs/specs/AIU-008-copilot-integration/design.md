---
id: AIU-008
type: design
status: implemented
goal: G-003
scope_version: 1
---
# Copilot integration design

## Evidence-driven transport

Implemented path: GitHub device flow, authenticated numeric identity GET, then the OMP-observed internal quota GET. Fixed HTTPS origins, disabled redirects/cookies/HTTP logging, bounded bodies and sanitized errors use the existing transport conventions. AI Usage identifies itself truthfully; model discovery and model-policy POSTs are omitted. Device authorization displays the returned user code and exact GitHub verification URL. Core's transient AuthorizationChallenge passes through the drained DashboardWorkflow; existing providers use its default URL-only adapter. The code is cleared when the attempt ends or the sheet detaches.

The owner selected only OMP on 2026-09-18: use its OpenCode registration and read:user scope. Direct quota access was live verified on a personal Free account. The SDK and personal billing reports remain excluded. An omitted token type is compatible with the inspected OMP response; an explicit non-bearer type is rejected.

## Compatible shared contracts

Changes from base `bc67aae`:

- `IProviderSession.ConnectWithChallengeAsync` carries the transient device code and expiry while preserving Codex browser and Claude manual-code behavior.
- `QuotaWindow` adds optional native decimal amounts, Unlimited and typed QuotaSourceDetails. Older records remain readable. quota_id, quota_remaining and overage values stay independent of normalized amounts and restriction flags.
- Known groups use premium/chat/completions order, independent of JSON ordering. Future opaque group keys retain their names and unknown units. Missing values do not become zero or false; Allowed and LimitReached remain null because this endpoint does not report them.
- Zero entitlement retains source amounts/percentage, but Windows presents the ratio as unknown without exhaustion severity. Explicit unlimited takes precedence. The website's Included credits is a different metric; requests are never relabelled as credits.
- LiveMapping uses explicit provider names and preserves opaque identifier qualification. Product composition enables Copilot while demo composition remains isolated.

## Credential lifecycle

Keep the actual GitHub access token and optional provider expiry. OMP's access/refresh alias and synthetic ten-year expiry are not authoritative lifecycle evidence. This path performs no renewal; expiry or rejection requires reconnect. Resume/refresh verifies the numeric identity before accepting quota. Reconnect must match the existing identity before quota adoption or writing, preserving the previous slot and history on mismatch.

Reuse the existing app-owned DPAPI CurrentUser and versioned-record conventions with provider/account binding. This OMP path does not renew grants. Reconnect explicitly when expiry or rejection requires it; never pretend its access-token alias is a refresh grant. Local disconnect deletes only Copilot-owned state and does not claim server revocation. Keep prior valid connections during failed replacement. Use the security-lifecycle skill and focused independent review before completion.

## Verification plan

Protocol tests cover device intervals, slow-down, expiry, denial, cancellation, malformed bodies, optional token type and prohibited side effects. Synthetic quota tests assert native units, null/unlimited distinctions, independent details, ordering and restriction provenance. Windows-backed lifecycle tests cover DPAPI, interrupted writes, failed deletion, expiry, identity binding and scoped cleanup. The owner authorized agent-operated browser consent and local unpackaged lifecycle checks in an isolated profile. MSIX validation does not install the package or change host trust. Review findings and targeted fixes are recorded in verification. No Sandbox is needed for these checks.
