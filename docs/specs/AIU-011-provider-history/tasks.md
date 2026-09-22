---
id: AIU-011
schema_version: 1
---
# AIU-011 implementation ledger

Implementation base: c38467a. Implementation and review-fix commits: 45eef10, d3817ed.
The owner authorized normal browser login for live verification on 2026-09-22, without
new scopes, browser-cookie extraction or source CLI credential import.

- T-01: Done. Credential-free contracts, hardened clients, native metrics and periods,
  truthful per-report outcomes. Infrastructure suite: 307/307.
- T-02: Done. Existing-session integration, sanitized console probe, shared scheduling,
  token-rotation persistence, cancellation/draining and account isolation. Focused
  independent review findings were fixed and targeted confirmation passed.
- T-03: Done. Automatic account-scoped/all-account history, date controls, refresh,
  memory cache and unsupported-provider states. Presentation suite: 154/154; final demo
  and product interactive Windows suites: 7/7 each. The physical-click automation
  limitation remains explicitly recorded in verification.md.
- T-04: Done. Unpackaged/unsigned-package builds passed. Authorized real Codex history
  loaded through the product and appeared in Windows, satisfying AC-02. Daily usage,
  activity, plugin and skill routes work on the observed account. Optional workspace
  routes returned 400/403. Copilot login/quota succeeded, but historical routes returned
  404 with the current grant. This is capability-dependent completion, not all-plan parity.

Presentation DTOs are mapped in Adapters/Live; no direct presentation/Core dependency.
Legacy demo sparklines remain separate. No durable history or local sampling was added;
AIU-029 stays deferred. Connections remain in normal app-owned protected storage.
Temporary status-only diagnostics were removed; no private response or credential was
committed. Detailed commands, outcomes and residual limits are in verification.md.

Follow-up verification: pending by owner direction on 2026-09-22, at low priority.
The completed implementation and observed results above are retained; the owner requested
further verification before treating the feature as complete. No new checks start now.

Next action when selected again: review the recorded provider-access and Windows smoke
limitations, then verify the remaining history behavior and record the actual outcomes.
