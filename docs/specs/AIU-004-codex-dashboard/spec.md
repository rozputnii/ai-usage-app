---
id: AIU-004
type: spec
status: implemented
goal: G-002
scope_version: 1
execution_status: done
approval_basis: owner-selected-AIU-004-after-live-verified-codex-path
---
# Codex quota dashboard on the verified provider path

## Authorization
On 2026-09-14, after AIU-002 closure and the live-verified Codex library, the owner selected AIU-004 and simplified the workflow: one task at a time, committed and pushed directly to main. This authorizes connecting the existing verified provider path to the Windows product UI with durable credentials under the accepted DPAPI boundary. It does not authorize another provider, CLI credential import, host package installation, host certificate trust, elevation, paid resources, releases or publication.

## Scope
Reuse `AiUsage.Infrastructure.Providers.Codex` unchanged as the single provider client. Add durable credential protection and a dashboard that connects one Codex account, shows its real quota and refreshes on demand.

In scope: DPAPI CurrentUser protection of one Codex grant in the app-owned LocalState root; resume on launch; sign-in, refresh and disconnect from the UI; honest typed states for loading, unavailable quota and required reauthentication; English UI resources.

Deferred to later items, deliberately not built here: tray integration, usage history and charts, background refresh scheduling, multiple accounts or workspaces, a database, CLI discovery and import, notifications and settings surfaces. The console remains the provider verification surface; no second provider client is introduced.

## Acceptance criteria
- AC-01: The dashboard consumes the existing Codex clients through dependency injection. No provider HTTP call, endpoint, header, token parsing or quota normalization is duplicated in the UI layer.
- AC-02: A completed sign-in persists exactly one Codex grant protected with DPAPI CurrentUser under the app-owned LocalState root. No token, refresh token or account identifier appears in plaintext in any file, log, error message, exported diagnostic or UI text.
- AC-03: Relaunch resumes the stored grant without a new browser sign-in and shows quota for the same workspace. A grant that the provider has invalidated results in an explicit reauthentication state, never a silent empty dashboard or a fabricated zero quota.
- AC-04: The dashboard distinguishes not connected, working, quota shown, quota unavailable and reauthentication required. Unknown, unlimited and exhausted quota values remain visually distinct; absent values are never rendered as zero.
- AC-05: Disconnect removes the stored grant and returns the app to the not-connected state without deleting anything outside the app-owned root, and without claiming provider-side revocation.
- AC-06: Verification records actual results: deterministic tests for the credential store and dashboard state transitions, an executed build, and an actual run of the changed surface. Any check that was not run is recorded as NOT_RUN rather than inferred.

## Fixed boundaries
The DPAPI record is app-owned; source CLI credential stores are never read or modified. Failures surface as typed states, not raw provider payloads. Sign-in requires explicit user action and discloses that the public Codex client's third-party reuse remains unresolved. No automatic retry of authentication requests and no replay of a grant whose refresh outcome is unknown.
