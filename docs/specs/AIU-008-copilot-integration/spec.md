---
id: AIU-008
type: spec
status: implementing
goal: G-003
scope_version: 1
approval_basis: Owner goal objective of 2026-09-16 and explicit owner decisions on client, quota source and deferred live testing in the same session.
---
# GitHub Copilot end-to-end integration

Deliver GitHub Copilot subscription usage alongside Codex and Claude, from primary provider evidence. Preserve the Core, Infrastructure and Windows boundaries, and existing Codex/Claude behavior and stored formats.

## Owner decisions, 2026-09-16

- Authentication client: "Reuse OMP/OpenCode client", selected over an owner-registered GitHub App (the documented app-owned option) and over stopping at research. This is undocumented third-party public-client reuse; OpenCode or GitHub permission is not established.
- Quota source: "Documented only". Use GitHub's documented personal AI-credit and premium-request billing usage reports. The undocumented `copilot_internal/user` entitlement endpoint is excluded.
- Mismatch disposition: the documented reports list only fine-grained token types, and the owner chose "Live-probe first". The probe console was built. No provider request was observed, and the owner then instructed: "finish integration without it. i will test letter in the app". Live acceptance therefore remains outstanding and owner-led. Compatibility of this client with the documented report is unproven.

## Scope and boundaries

Included: GitHub device flow with the recorded client and `read:user` scope; stable numeric account binding; documented usage reports for the current month; app-owned DPAPI CurrentUser state; connect, resume, refresh and local disconnect; a Copilot dashboard card; and an offline/probe console. Excluded: model-policy mutation, inference, `copilot_internal` endpoints, CLI credential discovery/import, user-entered PATs, GitHub Enterprise domains, organization/enterprise billing, multiple accounts, token revocation (requires the client secret) and broad UI redesign.

## Acceptance

- AC-01: Authentication, usage, context and import capabilities are classified separately with exact OMP references and current official GitHub documentation. Restrictions, unresolved token-type compatibility and missing live evidence are recorded without implying permission.
- AC-02: Device connection shows the user code before opening the verification page, and binds the token to the numeric account id. Denial, expiry, `slow_down`, untrusted verification URIs, malformed identity, cancellation and overlap have safe outcomes without exposing tokens or provider payloads.
- AC-03: Protected persistence supports cached startup, resume, reconnect and local disconnect. Account mismatch, interrupted staging, corrupt records, revoked tokens and failed reconnects preserve the previous valid connection. Codex and Claude data remain readable.
- AC-04: Usage parsing keeps opaque product/SKU/model/unit names and non-negative provider quantities. It never adds different units or invents a remaining allowance, percentage or currency. A missing report is unknown rather than zero, and reports for another user are rejected.
- AC-05: Presentation covers the device code, usage text, cached/stale labeling, failure text and independent provider cards, with no regression in the Codex/Claude workflow suites.
- AC-06: Relevant automated regressions pass. The packaged Windows smoke includes Copilot controls on the final candidate. Live account evidence covers connection, report acceptance, refresh, restart resume and disconnect; until the owner performs it, those items remain NOT_RUN.
- AC-07: Primary review and focused independent credential/durable-state review complete with no unresolved material findings. Scoped changes are committed and the task branch is published under CONTRIBUTING. Main integration and release are not implied.
