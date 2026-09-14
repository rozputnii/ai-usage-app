---
id: AIU-003
type: spec
status: implemented
goal: G-002
scope_version: 1
approval_basis: owner-directed-provider-library-and-console-integration
execution_status: done
---
# Codex integration library and console verification

## Authorization
On 2026-09-13 the owner explicitly redirected development from Windows UI work to provider integration using a console application and appropriate integration/unit tests. Codex remains the first selected provider. AIU-002 is paused separately; its completion is not a prerequisite for this work. This authorizes local research, library/console implementation and deterministic verification. It does not authorize personal CLI credential access, source-store mutation, unattended account consent, paid resources, inference requests, remote publication or other providers.

## Scope
An executable Codex subscription integration behind the existing UI-independent Core/Infrastructure library boundaries, consumed first by a console application. Establish current source-backed authentication and quota contracts before implementation. Keep authenticated sessions in memory for this verification slice; do not introduce plaintext persistence, a credential manager, database, tray, WinUI integration or a speculative multi-provider framework. A future persistent application adapter must use the accepted DPAPI CurrentUser boundary and independently verified token lifecycle.

## Acceptance criteria
- AC-01: Auth and quota evidence identify immutable official Codex and OMP source references, endpoints/fields, classification, account context and refresh/coexistence risks, with source and live verification recorded separately.
- AC-02: The library retrieves and normalizes Codex subscription quota without a model/inference request or billing-API substitution; absent, unknown, unlimited and exhausted values remain distinct, and provider-specific additional groups are preserved only when supplied.
- AC-03: The source-supported authentication flow and refresh behavior are executable without WinUI, respect cancellation and expiry, and do not expose credentials through normal/error output or durable files. Source CLI credentials are neither read nor modified.
- AC-04: A console application consumes the same library for offline fixture inspection and interactive authenticated verification. It provides honest typed outcomes and does not contain a second provider implementation.
- AC-05: Deterministic unit/integration tests exercise observable parsing, transport failures, cancellation and authentication transitions. Run the actual console against synthetic input; record authenticated live checks as NOT_RUN or BLOCKED until consent/access exists, never as PASS from fixtures.
- AC-06: Focused independent auth/secret-boundary review and canonical validation complete with actual evidence. Missing live account proof prevents claiming verified end-to-end provider access.

## Fixed boundaries
No raw provider payload, authorization header, token or personal account dump in errors, logs, tests or committed fixtures. Use synthetic fixtures labeled as such. No blanket retry of authentication POSTs. No user-provided arbitrary endpoint that can redirect credentials to an untrusted origin. No silent token refresh on a copied CLI grant. Console success does not establish later UI lifecycle correctness or provider permission for third-party client reuse.
