---
id: AIU-008
type: spec
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner selected the next provider task in a separate branch on 2026-09-18 while explicitly pausing AIU-010; detailed criteria derive from the existing backlog, provider order and repository boundaries. Owner subsequently selected only the OMP implementation.
---
# GitHub Copilot end-to-end integration

Deliver an app-owned Copilot connection and subscription quota dashboard through the shared provider library, console and current Windows UI. Preserve Codex and Claude behavior and stored formats. Use source evidence and an owner-authorized account experiment; do not present an internal endpoint as an official public quota API.

## Scope

Use browser-based device authorization, cancellation and safe polling. Use the inspected OMP public GitHub device flow with the OpenCode registration and read:user scope. Separate successful GitHub authentication from actual Copilot entitlement access. Establish stable account identity from the provider, represent supported account/context boundaries explicitly, and preserve request versus credit units, null values, unlimited flags and independent quota pools.

Use the existing Core, Infrastructure and Windows projects. Core owns credential-free workflow contracts; Infrastructure owns protocol, parsing and DPAPI CurrentUser persistence; Windows owns device-code presentation, browser launch and lifetime. The console exercises the same provider implementation. One active connection per provider follows the existing product limitation; multi-account work is not silently included.

CLI discovery/import, SDK/CLI runtime adoption, model inference, model-policy changes, billing substitution, quota purchases and host trust changes are excluded. Enterprise host support must have separate endpoint/trust evidence before activation. No new package or service is selected by this specification. AIU-010 acceptance remains paused.

Owner amendment, 2026-09-18: the owner explicitly authorized the agent to perform all final checks independently using the owner's already signed-in GitHub browser, including application authorization. This current instruction permits browser-driven sign-in and consent for this task without another routine confirmation. It does not authorize CLI credential access or unrelated GitHub/account changes. Browser credentials and tokens must not enter transcripts or fixtures. Stop only for a new material permission boundary, required owner-only challenge or unavailable access.

## Acceptance

- AC-01: Record exact stable OMP source, official authentication documentation and separate auth/quota classifications, including registration, scopes, token lifecycle, side effects, permission uncertainty and live verification dates.
- AC-02: Shared authentication presents the GitHub verification URL and transient user code, respects provider polling intervals and expiry, handles denial/cancellation/slow-down/malformed responses, and verifies account identity before adopting a grant. No secrets enter logs, arguments, fixtures or persistent presentation state.
- AC-03: Quota mapping preserves explicit unlimited, unknown, exhausted and unavailable states, native amounts/units, reset dates and opaque identifiers. Premium requests and AI credits are never relabelled as each other or combined without evidence. Missing entitlement is not zero; billing totals do not substitute for quota.
- AC-04: App-owned protected state supports resume, refresh, reconnect and local disconnect. Identity mismatch, cancellation, failed writes/deletes, expiry and applicable rotation have tested outcomes. A failed new connection preserves the last valid state; existing Codex/Claude records remain readable.
- AC-05: Product composition enables only the verified Copilot capability. Windows supports device-code entry in the browser, quota/error/stale states, cancellation and tray/lifetime behavior. Demo isolation and other providers remain intact; future features remain unavailable.
- AC-06: Relevant Infrastructure and Presentation regressions, document validation, MSIX build and applicable local unpackaged Windows smoke pass. Owner-authorized live evidence covers connection, quota refresh, Exit/relaunch resume, reconnect and disconnect. Unavailable cases remain NOT_RUN or BLOCKED.
- AC-07: Primary integrated review and required focused independent credential/state review have no unresolved material findings. Commit and push the completed scoped task branch under CONTRIBUTING; no main merge or release is authorized by task selection.

## Resolved implementation choice

Owner amendment, 2026-09-18: "Use only the implementation from OMP." PD-008-01 is resolved in favor of the inspected OMP device flow, OpenCode public client ID and direct internal quota GET. Do not create an AI Usage registration or introduce the Copilot SDK. This explicitly selects a private unsupported integration; it does not establish provider approval. Preserve OMP's connection/quota behavior while omitting inference setup and model-policy writes, using truthful AI Usage headers and the existing protected local storage boundaries. Live endpoint access was verified on a personal Free account; the endpoint reports request pools, while the website shows separate Included credits. Credits parity is not claimed.