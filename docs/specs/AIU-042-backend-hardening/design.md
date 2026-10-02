---
id: AIU-042
type: feature
status: implemented
goal: G-003
scope_version: 1
---

# AIU-042 bounded correction decisions

## Codex renewal uncertainty

The owner explicitly authorized this correction on 2026-10-02 after the synthetic
lost-response/relaunch regression sent the same grant twice. AIU-004 already forbids
replay after an unknown refresh outcome. This is remediation of that contract, not
a new sign-in flow or provider protocol.

Use the existing DPAPI-protected `codex.grant.pending` envelope before sending a
renewal. Its existing Version/ParentRevision/Ciphertext fields are retained. Empty
Ciphertext means no successor has returned: existing readers already reject that
state, so downgrade cannot silently replay the committed predecessor. No new file
namespace, grant-record field, entropy, migration or cleanup path is introduced.

The still-exclusive lease can replace its exact encrypted intent with a returned
successor. Subsequent ordinary loads fail closed. Explicit fresh authorization may
adopt an intact intent whose parent matches the committed revision; it never renews
that predecessor, and replaces the intent only after a successful new sign-in.
Failed or refused authorization retains the marker. Save rechecks the marker bytes
and predecessor revision. A partial successor write remains unreadable and blocks
replay; a complete successor follows the existing journal recovery/promotion path.
Successful rotation saves without request cancellation before quota/history proceeds.
Explicit disconnect uses existing exact-file cleanup. A known rate-limit rejection
can restore the unchanged predecessor through the same protected commit path, preserving
the existing retry semantics; ambiguous failures retain the intent.

Alternatives rejected: memory-only invalidation misses relaunch/crash; a new sidecar is
ignored by older readers and expands ownership; adding a grant flag changes the legacy
record shape. No generic persistence rewrite is needed. Focused independent review and
interruption/cancellation/compatibility regressions are required before integration.
