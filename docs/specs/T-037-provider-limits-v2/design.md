---
id: T-037
type: design
status: implemented
goal: G-003
scope_version: 1
---

# Design

Keep QuotaSnapshot as the transport-independent compatibility envelope and add an
optional LimitSnapshot. Existing presentation consumers keep their contract until
T-039. Core contains facts only; Infrastructure owns parsing and persisted codecs.
Add an opaque secondary amount triple for CX-I without assigning an unsupported unit.

Use small shared normalization helpers for reset/start and legacy conversion, with
provider-specific parsing where the wire shape contains information absent from v1.
Persist normalized facts with explicit JSON quantity and limit-value codecs owned by
Infrastructure, keeping serialization concerns outside Core.

Prefer a format migration in the existing state lease to a second credential store:
it serializes migration against refresh/sign-out and preserves revision lineage.
Retain the v1 ciphertext checkpoint, stage v2 separately from grant pending files,
validate it and promote. A format checkpoint is evidence, never automatic grant
rollback. A torn format stage is safely rebuilt from the still-committed generation.
Resolve pending rotating-grant writes first. Delete checkpoints on explicit sign-out.

History compatibility is explicit and conservative. Keep an optional legacy alias
with each normalized fact, and resolve that alias to an existing series key for
capture/read compatibility only when source semantics are equivalent. New limits
use native keys; ambiguous Claude scoped aliases are not guessed. Old files survive.

No dependency, provider request, presentation-contract or product lifecycle expansion.

Review corrections: when normalized cached quota exceeds the protected record bound,
retry serialization with only CachedQuota removed. Revalidate the state and unchanged
revision lineage; zero both plaintext buffers, retain the original encrypted checkpoint
and use the same staged cutover. A still-oversized state fails closed.

History aliases require every retained reading to match the native quantity kind/unit
and balance direction. Incompatible or mixed histories remain in their original files
while new observations start under the native key. Unknown Claude kinds include their
opaque kind and scope in the discriminator; uniqueness is scoped to the family.
Copilot native percentages are read independently from the source even when the
compatibility presentation window suppresses them for an unlimited entitlement.
