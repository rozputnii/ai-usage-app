---
id: AIU-011
schema_version: 1
---
# AIU-011 preparation and handoff

The owner selected provider-supplied history for all four providers, all available usage
metrics and a low-click interface. Local history is deferred to AIU-029. The draft
[specification](spec.md) and initial [research](research.md) preserve those decisions.
PD-011-01 is resolved: existing authorization only, matching OMP if it has a provider
history method. Stable OMP v18.2.8 was inspected at
`5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c`. Its four OAuth paths supply current quota;
its history is recorded locally. No eligible remote-history method was found, so no
product implementation plan was selected and no production code was changed.

The owner then requested broader feasibility analysis. Official Codex development source
at `2c2a42e65de077c5518ea5b4c3999633ef6a12fc` contains ChatGPT-authenticated historical
analytics requests. Copilot has documented reports with unresolved existing-grant access;
Claude and Antigravity describe credit history but no compatible transport was established.
See research.md for pinned source, release distinction and remaining uncertainties.

Exact next action on resumption: prepare a bounded read of Codex
`/backend-api/wham/usage/daily-token-usage-breakdown` through AI Usage's existing session
authority, using a short date range and sanitized outcome evidence, to establish actual
grant/plan eligibility before history implementation. This research did not execute a
live read. Do not import CLI credentials, introduce additional authorization, promise
universal coverage or start AIU-029 to bypass missing remote capabilities.

Actual checks and limitations are in [verification.md](verification.md).
