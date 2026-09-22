---
id: AIU-006
type: spec
status: implementing
goal: G-002
scope_version: 1
approval_basis: Derived within the owner's explicit 2026-09-22 instruction to execute AIU-006 automatically.
---
# First verified upgrade and recovery checkpoint

Owner direction, 2026-09-22: execute AIU-006 automatically. This specification derives the implementation within that authorized outcome; it does not claim separate human review of this document.

The current app stores durable presentation preferences at `appearance.v1.json` and app-owned encrypted provider records under `providers`. There is no database yet. Establish layout schema 1 by moving preferences to `preferences/appearance.v1.json`; preserve exact bytes, unknown fields, labels and opaque identifiers. An absent layout manifest denotes legacy layout 0. The preference content schema remains 1 and provider secret formats remain unchanged.

## Acceptance

- AC-01: A real same-family old/new development MSIX update preserves synthetic durable preferences and app-owned DPAPI credential records. Verify installed package versions and actual new product activation, separately from builds/tests.
- AC-02: Migration holds an exclusive lifetime state lease, creates and validates a DPAPI CurrentUser checkpoint before changing durable data, stages and validates target data, commits a versioned layout manifest, then removes only the exact obsolete preference file. Normal startup checks a bounded manifest/journal without enumerating state or contacting providers.
- AC-03: Interruption at checkpoint, journal, target publication, layout commit and cleanup boundaries is recoverable. Restart with an unfinished journal blocks normal services and requires explicit Retry or Restore. Retrying never overwrites the verified checkpoint with partially migrated data.
- AC-04: Explicit restore validates DPAPI, checkpoint version, layout, content hash and preferences, then restores the whole nonsecret durable generation and reruns the current migration. Failed or interrupted restore stays in recovery; no automatic wipe or loop. Unsupported newer layouts refuse retry and restore.
- AC-05: Paths are fixed and checked for redirected ancestors/files. Unknown files, provider grants, caches, logs and diagnostics are excluded from checkpoint/restore and cleanup. Checkpoint contains no provider secret; existing protected provider bytes remain unchanged even when restore follows a newer credential write. No SQLite/WAL claim is made because no database exists.
- AC-06: The live Windows recovery surface blocks provider initialization/actions and preference writes until maintenance succeeds, supports explicit retry/confirmed restore, and exposes sanitized diagnostic export and opening the owned data directory. Exit drains initialization and releases the lease.
- AC-07: Infrastructure and presentation regressions, document validation, Windows/MSIX build, actual recovery UI and disposable-guest update/fault checks pass. Focused independent review covers durable-state boundaries; limitations are reported explicitly.

## Boundaries

No new dependencies, credential format migration, provider calls, source CLI reads, portable import/reset, public release or host package/trust installation. Guest-only trust uses the existing development certificate's public CER; never export its private key. Synthetic credentials are generated inside the disposable guest, not copied from user storage. MSIX versions are independent of layout/content schema versions.

One checkpoint covers the only current nonsecret irreplaceable file. Provider records retain their established independent rotation journal; rollback of server-side token rotation is impossible and not attempted. Unknown user files remain untouched. Future database/configuration/secret schema migrations must expand the manifest and consistency protocol before changing those resources.
