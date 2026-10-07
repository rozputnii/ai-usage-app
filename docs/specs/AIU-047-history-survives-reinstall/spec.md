---
id: AIU-047
type: feature
status: draft
goal: G-003
scope_version: 1
approval_basis: Owner design conversation on 2026-10-07. The owner named the scenario (uninstall followed by a fresh install loses the accumulated account-window analytics), selected approach A (an unvirtualized history folder under the user's LocalAppData plus a stable account ID restored from a protected identity map) and approved each design section (storage location, re-attach, migration, deletion and logging, verification). This written specification awaits owner review.
---

# Usage history survives reinstall

## Problem

All app state, including the local reading series that drives the account-window
analytics (D-184), lives in the package-owned `LocalState` folder. An MSIX update keeps
it (AIU-014 AC-06), but uninstalling the package deletes it. After a reinstall every
estimate starts from nothing: the inline 35-day history is empty, the first local day
has no start-of-day amount, tracked consumption is marked incomplete, and the five-hour
session estimate stays hidden until at least three fully observed windows are collected,
which typically takes days.

Two causes have to be fixed together:

1. The series is stored inside the package folder that uninstall removes.
2. Series are keyed by the account's app ID, a random GUID created at first sign-in. The
   only map from the provider-verified identity to that GUID is `accounts.state`, which
   uninstall also removes, so a reinstalled app would assign a new GUID even if the
   series files survived.

## Intended result

After uninstalling and reinstalling the packaged app on the same Windows user profile,
the owner signs in to the same provider account again and immediately sees that
account's existing history and the analytics derived from it. Credentials, labels, card
order and other preferences are still removed by uninstall; only the analytics survive.

## Behavior

### History location

- The packaged app keeps its history root in `%LOCALAPPDATA%\AiUsage\History`. The
  manifest declares that directory in
  `virtualization:FileSystemWriteVirtualization/ExcludedDirectories` and declares the
  `unvirtualizedResources` restricted capability, so writes reach the real folder and
  Windows does not remove it at uninstall. No other location is unvirtualized.
- The history root holds the existing reading-series and budget-configuration store
  (`budget\`, format unchanged) and the identity map (`accounts.identities`).
- The folder name is independent of the package identity, so a later package-name
  change keeps the history.
- Unpackaged runs (development, `--demo`, `AIU_DEVELOPMENT_STATE_DIRECTORY`) and the
  synthetic audit build keep the history root equal to their existing state root. They
  never read or write `%LOCALAPPDATA%\AiUsage\History`.
- Everything else (credentials, `accounts.state`, preferences, logs, maintenance files)
  stays in the existing state root.

### Identity map

- `accounts.identities` is a versioned record protected with DPAPI CurrentUser and its
  own entropy. Each entry holds only the provider name, the provider-verified
  `ProviderIdentity` (subject and context) and the app account ID. It holds no
  credential, label, storage ID or provider payload.
- Every successful sign-in that admits or reconnects an account upserts its entry.
- Every startup, after state maintenance succeeds, upserts the entries of all accounts in
  `accounts.state`. When the map and the registry disagree on an identity's account ID,
  the registry wins.
- Sign-out does not remove entries (D-093). Only Delete local data removes the map.

### Re-attach after reinstall

- When a sign-in's verified identity matches no account in `accounts.state`, the app looks
  the identity up in the identity map. On an exact match of provider and identity it
  admits the account with the mapped account ID instead of a new GUID. The existing series
  keyed by that ID then apply without copying or merging.
- A different provider, subject or context gets a new account ID, as today.
- Series that are not keyed by an account ID, such as the unassigned legacy account
  series (PD-039-01), remain unassigned.

### Migration of existing installs

During startup state maintenance, after the existing account migration and before the
reading store opens:

- If `budget\` exists in the state root and not in the history root, the directory is
  moved to the history root as one rename on the same volume.
- If both exist, the history-root copy is kept, the state-root copy is left in place
  untouched, and the outcome is logged.
- The step is idempotent; a repeated start after an interruption finishes it.
- The stored layout version rises from 2 to 3 so that an older build refuses the state
  (D-140) instead of silently starting an empty history.
- If the move fails, maintenance reports Interrupted with the existing Retry path, and
  no data is deleted.

### Deletion

- Delete local data also deletes the history root's `budget\` contents and
  `accounts.identities`, with the same owned-path, reparse-point and resumable-intent
  rules as the state root.
- Uninstall deliberately leaves the history root. This is recorded as a documented
  exception in the security and lifecycle reference.

### Logging

- Migration: one record per outcome (moved, both present and left in place, nothing to
  move is silent, failed) using the existing migration events.
- Identity map read or write failure: `PersistenceFailure`, with no identity, account ID
  or path text.
- A sign-in re-attached through the identity map: one Information record without
  identity data.

## Acceptance criteria

- AC-01: The packaged manifest declares `unvirtualizedResources` and excludes exactly
  `$(KnownFolder:LocalAppData)\AiUsage\History` from file-system write virtualization.
  A packaged run writes series and budget configuration under
  `%LOCALAPPDATA%\AiUsage\History\budget` and nothing else outside the package data.
- AC-02: Unpackaged, demo, isolated-development and audit runs keep their history inside
  their own state root and never touch `%LOCALAPPDATA%\AiUsage\History` (test with an
  injected root).
- AC-03: The identity map round-trips, rejects unsupported versions and out-of-bounds
  content, contains only provider, identity and account ID, and is DPAPI-protected. A
  corrupt map is set aside and rebuilt from the registry at the next startup.
- AC-04: With an empty registry and a map entry for the verified identity, sign-in admits
  the account with the mapped ID; a different provider or identity gets a new ID. A
  failed map write does not fail the sign-in.
- AC-05: Startup moves an existing state-root `budget\` to the history root, keeps both
  untouched when both exist, is idempotent, writes layout 3, and on a move failure
  reports Interrupted without deleting data.
- AC-06: Delete local data removes the history root's store and identity map, and an
  interrupted deletion finishes on the next start.
- AC-07: Changed failure paths log as specified with no identity, account ID, path or
  exception text.
- AC-08: Live packaged check: after readings are recorded, uninstall leaves
  `%LOCALAPPDATA%\AiUsage\History`; after reinstall and sign-in to the same account,
  the account shows its previous history immediately. Delete local data afterwards
  removes the history folder's contents.

## Boundaries

- Credentials and preferences do not survive uninstall. Moving the whole state outside
  the package is out of scope.
- No transfer between Windows users or devices, no export or import (AIU-013), and no
  retention change (AIU-029). Retention stays 35 days.
- No new provider request, grant or transport.
- The restricted capability and the data outside the package root are privilege and
  data-lifecycle changes: a focused independent review is required before integration
  (CONTRIBUTING, Review and integration).
