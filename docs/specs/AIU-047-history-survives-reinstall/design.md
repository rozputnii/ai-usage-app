---
id: AIU-047
type: design
status: implementing
goal: G-003
scope_version: 1
---
# AIU-047 design

## Alternatives considered

| Option | Result |
|---|---|
| A. Unvirtualized `%LOCALAPPDATA%\AiUsage\History` plus a protected identity map that restores the account ID | Selected. Documented MSIX mechanism (flexible virtualization, Windows 11; the manifest minimum is 10.0.26100). Data stays in the conventional per-user location, and keeping the same account ID needs no series merge. |
| B. The same layout under `%USERPROFILE%\.aiusage` without a manifest change | Rejected. A full-trust app writes there unvirtualized, but a dot-folder in the profile is unconventional on Windows; `Documents` is often synced by OneDrive. |
| C. Keep data in `LocalState` and copy snapshots outside, restoring them on first run | Rejected. Two copies, freshness arbitration and untrusted import overlap AIU-013 and exceed the need. |

## Boundaries

- `ApplicationStateDirectory` (Windows) resolves a second root, the history root: the
  real `%LOCALAPPDATA%\AiUsage\History` when packaged, the state root otherwise.
  Registration passes it to `LocalBudgetStore`, the identity map and `OwnedDataDeletion`.
  Infrastructure stays path-agnostic and receives roots as arguments.
- The identity map reuses the existing DPAPI record policy used by `accounts.state`
  (`ProviderStatePolicy`: lease, staged replace, revision, validation) with the file name
  `accounts.identities` and entropy `AiUsage.AccountIdentities.v1`. Bounds match the
  registry: at most 256 entries, subject and context at most 2048 characters, known
  providers only.
- `AccountService` admission: when `selected ?? match` is null, look up
  `(Provider, Identity)` in the map before `Guid.NewGuid()`. Upsert the map after the
  registry update succeeds; a map failure is logged and does not change the sign-in
  result.
- `StateMaintenance` commits layout 3 first, as it does for layout 2, so older builds are
  fenced out before any data moves. It then runs the account migration, the history
  relocation and the registry-to-map upsert while it still holds the root lease. A failure
  maps to Interrupted and the next start repeats the idempotent steps.
- `OwnedDataDeletion` takes the history root as a second owned root. When both roots are
  the same (unpackaged) the history step is a no-op beyond the existing budget deletion.

## Lifecycle notes

- The history root is outside Windows' uninstall cleanup by design. Delete local data is
  the only product path that removes it; the security and lifecycle reference records
  this exception.
- DPAPI CurrentUser keys belong to the Windows user, so a reinstalled package on the same
  profile can read the map. A different user or a reinstalled Windows cannot, which is
  outside this scope.
- The series files stay plain JSON as today (AIU-036): readings, no credentials. The
  identity map is protected because it holds provider identity.
- A downgrade to a build without AIU-047 refuses layout 3 instead of running with empty
  history.
