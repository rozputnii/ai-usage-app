# Design

Keep QuotaSnapshot as the transport-independent compatibility envelope and add an
optional LimitSnapshot. Existing presentation consumers keep their contract until
AIU-039. Core contains facts only; Infrastructure owns parsing and persisted codecs.
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
