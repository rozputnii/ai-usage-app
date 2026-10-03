---
id: AIU-039
schema_version: 1
---

# Preparation and handoff

Owner selected AIU-039 on 2026-10-03. Base: `9131e4729ef11e075e37a177d910931134b48e72`.
The working tree was clean on selection. Work is sequential on main under CONTRIBUTING;
no worktree, branch, external chat delegation or product-state access was performed.

Completed preparation: inspected the backlog, accepted design and Ledger contract,
single-slot live composition, session identity/rotation boundaries, observation keys,
preferences, startup maintenance and security policy. Wrote the draft specification
and architecture proposal with three explicit owner decisions. Product implementation
and a detailed implementation plan have not started.

## Continuation

Exact next action: apply the owner's answers to PD-039-01, PD-039-02 and PD-039-03 in
spec.md, recording actual provenance and any corresponding AIU-038 contract agreement.
Then write the sequential implementation plan against the settled scope and implement
with relevant regression checks. Do not infer approval from elapsed time.

The required decisions concern legacy identity attribution, changes to the presentation
contract/retirement agreement and previously deferred whole-data deletion. Live provider
and credential-bearing migration checks require separate current authorization later;
their absence does not block synthetic implementation after design decisions are settled.

Observed checks and limitations are in [verification.md](verification.md).
