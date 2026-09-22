---
id: AIU-011
schema_version: 1
---
# AIU-011 implementation ledger

Base: c38467a. Intermediate implementation commit: 45eef10. The owner selected
implementation after the broader feasibility assessment. Existing authorization only;
local observation history remains deferred to AIU-029.

- T-01: Done. Credential-free contracts and hardened Codex/Copilot transports/parsers
  preserve native metrics, date periods, access failures and partial coverage.
- T-02: Done. Existing-session integration, sanitized operator probe, shared scheduling,
  cancellation and draining. Token rotation and cross-process identity findings fixed
  and independently confirmed; external removal and per-report failure isolation tested.
- T-03: Done. Automatic all-account/scoped history, native report rows, date controls,
  refresh, memory cache, stale results and explicit unsupported providers. Actual Windows
  demo navigation displays synthetic native values without an initial Load action.
- T-04: Blocked only on real-provider acceptance. Infrastructure 307/307, Presentation
  154/154, final unpackaged build and unsigned MSIX pass. Windows and review details,
  including the physical-click smoke limitation, are in verification.md. AC-02 requires
  a real provider history dataset through the product; no eligible stored grant was found.

Ruling: presentation DTOs are mapped in Adapters/Live; presentation has no direct Core
reference. An initial architecture-test failure was fixed without weakening the boundary.
Legacy synthetic local-history contracts remain for demo sparklines and regression tests;
provider tokens/credits are never converted into a fabricated percentage curve.

No source CLI credentials, browser sessions, new login, extra scopes, local history store,
host trust change or installed-package mutation was used. No pending worker artifact remains.

Exact next action: when the owner identifies an existing AI Usage provider directory,
run the sanitized `history codex <owned-provider-directory>` (or `copilot`) console read,
then display the same authorized account's history in Windows and record real coverage and
access outcomes for AC-02. If the current grant lacks access, report that result without
starting a new login or changing permissions.
