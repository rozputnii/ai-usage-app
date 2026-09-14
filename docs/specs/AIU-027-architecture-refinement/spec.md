---
id: AIU-027
type: spec
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner request on 2026-09-14 to complete AIU-027 within recorded scope.
---
# .NET architecture refinement and cleanup

Derived within the owner's 2026-09-14 request to complete AIU-027. Preserve authentication, stored-data formats, quota semantics and close-to-tray behavior. No new providers, UI redesign, CLI integration, production projects or speculative frameworks.

## Acceptance

- AC-01: Core owns a dashboard application workflow over a credential-free session contract, tested independently of transport, storage and Windows.
- AC-02: Dashboard presentation depends on Core contracts and injected resources/dispatcher access, with independent tests for commands, stale/unknown quota and failure state.
- AC-03: Dependency checks enforce neutral Core, inward Infrastructure references and no Infrastructure or Windows API dependency in dashboard presentation.
- AC-04: One desktop composition owns the session and workflow. Explicit Exit prevents new work, cancels and awaits outstanding dashboard operations before Host disposal; close still hides and tray restores the same window.
- AC-05: Existing deterministic regressions, document checks, native package build and actual packaged Windows lifecycle smoke pass. Required reviews and any limitations are recorded honestly.

## Preservation

Infrastructure remains the only owner of Codex credentials, refresh persistence, HTTP transport and the existing DPAPI/cache serializers. The workflow does not retry authentication or interpret provider payloads. No source CLI credentials are read. Packaged smoke uses synthetic/empty state in a disposable guest; no host installation or trust changes are authorized by this refinement.
