---
id: AIU-006
type: design
status: implementing
goal: G-002
scope_version: 1
---
# Upgrade design

Use a bounded filesystem migration over existing state. Alternatives were introducing SQLite now (unnecessary dependency and conversion of unrelated provider state), or only testing package preservation (would leave the required migration/recovery mechanism absent).

Core exposes credential-free maintenance status and operations. Infrastructure owns the lifetime lease, fixed paths, DPAPI checkpoint, bounded JSON records, flush/replace writes and immutable layout-0-to-1 step. Windows gates product initialization and commands, maps outcomes to the existing recovery screen, and owns shell/dialog actions.

Journal phases distinguish migration and restore. A verified checkpoint is published before a journal. Publishing a target preference file is not the commit: the layout manifest is. Cleanup follows commit and the journal is removed last. An interruption after checkpoint but before journal leaves legacy data intact; retry may reuse/replace a checkpoint only while no journal or changed generation exists. Any journal on normal launch enters recovery. Explicit retry validates the retained checkpoint and completes the idempotent operation. Restore writes a restore journal before changing the active generation and is itself resumable. A newer layout always wins over any checkpoint and refuses downgrade.

Keep the lifetime lease through shutdown, preventing concurrent new product instances from writing preferences during maintenance. Pre-AIU-006 binaries do not understand the lease: package update must stop the old process first; cross-version unpackaged concurrent access is unsupported. Provider files are never read or written by migration and retain their own locks.

Same-user malicious concurrent filesystem mutation is outside the DPAPI threat boundary; reparse checks prevent static path redirection, not an adversarial TOCTOU guarantee. Fixed allowlists, bounded reads, authenticated checkpoint encryption and content hashes prevent using a checkpoint as an arbitrary file operation. No recursive cleanup exists in product maintenance.
