---
id: AIU-011
schema_version: 1
---
# AIU-011 preparation and handoff

The owner selected provider-supplied history for all four providers, all available usage
metrics and a low-click interface. Local history is deferred to AIU-029. The draft
[specification](spec.md) and initial [research](research.md) preserve those decisions.
No product implementation plan has been selected; no production code was changed.

Exact next action: resolve PD-011-01 with the owner: whether optional, separate history
authorization for the same subscription account is in scope when the existing connection
does not expose historical data. Then finish source/access proof and the implementation
design for the selected boundary. Do not assume browser-cookie or CLI credential import.

Actual checks and limitations are in [verification.md](verification.md).
