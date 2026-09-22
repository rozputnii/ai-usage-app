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

Exact next action on an owner-selected resumption: inspect a specifically identified new
OMP provider-history method and confirm it uses the existing AI Usage authorization
before planning implementation. With the inspected revision there is no executable
history-fetch task. Do not repeat the same broad search, add an unavailable-only feature,
introduce additional credentials or start AIU-029 to bypass this scope boundary.

Actual checks and limitations are in [verification.md](verification.md).
