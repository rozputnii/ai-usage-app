---
id: T-011
schema_version: 1
---
# T-011 implementation ledger

Implementation base: c38467a. Implementation and review-fix commits: 45eef10, d3817ed.
The owner authorized normal browser login for live verification on 2026-09-22, without
new scopes, browser-cookie extraction or source CLI credential import.

- T-011.1: Done. Credential-free contracts, hardened clients, native metrics and periods,
  truthful per-report outcomes. Infrastructure suite: 307/307.
- T-011.2: Done. Existing-session integration, sanitized console probe, shared scheduling,
  token-rotation persistence, cancellation/draining and account isolation. Focused
  independent review findings were fixed and targeted confirmation passed.
- T-011.3: Done. Automatic account-scoped/all-account history, date controls, refresh,
  memory cache and unsupported-provider states. Presentation suite: 154/154; final demo
  and product interactive Windows suites: 7/7 each. The physical-click automation
  limitation remains explicitly recorded in verification.md.
- T-011.4: Done. Unpackaged/unsigned-package builds passed. Authorized real Codex history
  loaded through the product and appeared in Windows, satisfying AC-02. Daily usage,
  activity, plugin and skill routes work on the observed account. Optional workspace
  routes returned 400/403. Copilot login/quota succeeded, but historical routes returned
  404 with the current grant. This is capability-dependent completion, not all-plan parity.

Presentation DTOs are mapped in Adapters/Live; no direct presentation/Core dependency.
Legacy demo sparklines remain separate. No durable history or local sampling was added;
T-029 stays deferred. Connections remain in normal app-owned protected storage.
Temporary status-only diagnostics were removed; no private response or credential was
committed. Detailed commands, outcomes and residual limits are in verification.md.

Closed as superseded by R-184. T-040 removes the retrieval after T-039 switched
the product to local Ledger history. Earlier results and verification limitations above
remain historical evidence; no further T-011 verification is planned.
