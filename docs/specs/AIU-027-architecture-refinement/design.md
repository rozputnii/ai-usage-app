---
id: AIU-027
type: design
status: implemented
goal: G-003
scope_version: 1
---
# AIU-027 design

Keep the existing three production projects. Core contains the credential-free Codex session state/contract and the dashboard workflow: startup cache/resume, command serialization, cancellation and drain on shutdown. Infrastructure implements the session contract with the existing transport and credential/storage lifecycle. Windows composes those services and owns dispatcher/resource access, presentation and native window/tray lifetime.

An interface-only change would remove a concrete reference but leave application coordination in presentation. Moving credential objects and serializers into Core would expose unnecessary security detail. A small dashboard workflow over the existing session port gives independent behavior tests without either tradeoff.

The workflow admits one operation at a time, rejects work after shutdown begins and drains admitted work before infrastructure disposal. Cancellation propagates to the existing session behavior; successful token exchange still persists before the next await. No new retries, payload mapping or disk formats are introduced. UI updates pass through an injected dispatcher and resource function. App owns startup/stop and retains the existing close/hide/restore implementation.

Tests link the actual presentation sources into a neutral test executable using the already pinned MVVM package, avoiding WinUI activation for view-model tests. Architectural checks inspect production project references and source boundaries. Existing Infrastructure regressions continue to cover credential rotation, storage and quota parsing. Final acceptance uses a freshly built package and the actual five-scenario Windows smoke.
