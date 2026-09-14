---
id: AIU-007
type: spec
status: approved
goal: G-003
scope_version: 2
approval_basis: Owner goal objective and explicit amendment on 2026-09-14 to proceed with a private unsupported OMP-style Claude integration.
---
# Claude end-to-end integration

Deliver reliable Claude subscription authentication and quota display using inspected OMP connection behavior as evidence. Preserve the Core, Infrastructure and Windows boundaries delivered by AIU-027. The owner selected this feature; this specification does not establish provider permission for a particular authentication method.

## Scope and boundaries

Establish account and organization identity, supported authentication, quota groups, units, reset times, renewal and reauthentication before implementing the adapter. Reuse the provider library in the console and Windows app. Keep transport, credential objects and persistence out of presentation. Extract shared behavior only where both Codex and Claude need it.

Use app-owned DPAPI CurrentUser storage, asynchronous I/O, propagated cancellation and explicit disposal. Preserve existing Codex behavior and stored formats. Keep unknown, stale, exhausted and unavailable quota distinct; never aggregate incompatible groups or substitute API billing for subscription quota.

CLI discovery/import, other providers, broad UI redesign, new frameworks, unrelated upgrades, host trust changes and automatic sign-in are excluded. Actual provider consent remains with the owner. All subagents, including reviewers, must use `gpt-5.6-luna` with `max` reasoning; unavailable required review is a blocker.

## Acceptance

- AC-01: Authentication and quota have separate source classifications, exact OMP references and current official evidence. The account/organization contract, scopes, rotation, restrictions and any missing access are recorded without implying provider approval or live verification.
- AC-02: An authorized browser connection verifies stable identity and initial quota through the shared library. Denial, invalid callback/state, cancellation, malformed identity and ambiguous exchange have safe outcomes without exposing credentials or losing a previous valid connection.
- AC-03: App-owned protected persistence supports resume, credential renewal, reconnect and local disconnect. Identity mismatch, interrupted writes, unsupported stored versions, failed deletion and rotated-grant persistence across cancellation are covered by meaningful tests. Existing Codex data remains readable.
- AC-04: Quota parsing and display preserve independently returned shared and scoped limits, native units, unknown/null values, exhaustion and reset timestamps. Errors and malformed responses preserve a correctly identified stale reading without inventing percentages or requests.
- AC-05: Workflow and presentation tests cover connect, resume, refresh, disconnect, reauthentication, cancellation, overlapping refresh rejection/coalescing and shutdown. One provider's failure does not disable the other. Startup renders cached state without waiting for the network; shutdown drains admitted work before disposal.
- AC-06: Relevant automated regressions and actual packaged Windows UI checks pass on the final candidate. Live account evidence covers connection, quota, renewal, durable resume and disconnect; unavailable cases remain BLOCKED or NOT_RUN. Automated or source checks alone cannot satisfy live acceptance.
- AC-07: Primary review and required focused independent credential/durable-state review are complete with no unresolved material findings. Evidence and scoped changes are committed, the completed task branch is published under CONTRIBUTING, and the verified combined candidate is integrated into local main. No main push or release is implied.

## Owner amendment and delivery gate

The owner explicitly instructed: "Proceed with a private, unsupported Claude integration using OMP's connection flow. I understand Anthropic's documented restriction. Keep that limitation recorded." This resolves the local implementation decision; it does not establish Anthropic approval. Follow the inspected OMP browser/code flow and scope set, with truthful application identity and no inference or other side effects to collect quota. Actual account consent still belongs to the owner.

Keep this private work local, including the task branch. This amendment overrides automatic remote task-branch publication in AC-07 for this task; local main integration remains authorized only after all required verification and review passes. No public release or remote publication is part of this amended scope. The [provider evidence](../../providers/claude.md) retains the documented restriction. Source preparation alone cannot complete the feature.
