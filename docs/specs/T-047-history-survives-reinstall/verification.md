# T-047 verification

Started 2026-10-07 on branch `users/account-window-analytics-persistence-bde321`. Checks use
synthetic state only: no provider sign-in, credential or host install. Guest packages were
signed with the owner's local development certificate (`771CB0E8…E774`) and trusted only
inside a disposable Windows Sandbox guest (LocalMachine\TrustedPeople).

## Acceptance

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS (Sandbox) | The manifest declares `unvirtualizedResources` and one excluded directory. Sandbox: after the update, the store was in the real `%LOCALAPPDATA%\AiUsage\History\budget` as seen by a non-packaged guest shell, and it survived uninstall. |
| AC-02 | PASS | `History()` returns the state root when unpackaged or `--demo`; the audit composition keeps its own root. Covered by `RelocationWithSharedRootLeavesBudgetInPlace` and the unchanged audit and demo paths. |
| AC-03 | PASS | `IdentityMapFindsOnlyExactProviderAndIdentity`, `IdentityMapUpsertReplacesTheIdentitysAccountId`, `IdentityMapIsProtectedAndHoldsOnlyProviderIdentityAndAccountId` (decrypted fields are exactly Provider, Identity, AccountId), `UnsupportedVersionOrOversizedMapIsSetAside`, `CorruptIdentityMapIsSetAsideAndRebuilt`. |
| AC-04 | PASS | `ReinstalledStateReattachesTheSameIdentityToItsPreviousAccountId` (one `HistoryReattached` event; a different identity gets a new ID) and `UnavailableIdentityMapDoesNotFailSignIn`. Mutation check: removing the restored ID makes the reinstall test fail. |
| AC-05 | PASS | Tests: `RelocationMovesStateBudgetAndSeedsIdentitiesFromRegistry`, `RelocationKeepsBothCopiesWhenHistoryAlreadyExists`, `LockOnlyStateStoreIsNothingToMove`, `MaintenanceCommitsLayoutThreeAndOlderBuildRefusesIt`, `FailedRelocationIsInterruptedAndPreservesData`. Sandbox: updating 698 → 793 moved `budget` and wrote layout 3. |
| AC-06 | PASS | `DeletionClearsHistoryStoreAndIdentityMap`, `InterruptedDeletionFinishesHistoryOnNextRun`, `DeletionWithSeparateHistoryDoesNotRecreateTheRelocatedStore`. Sandbox: Delete stored data in the installed app removed the history configuration; only `budget.lock` remained. |
| AC-07 | PASS | The new events carry no context. Sandbox application logs show `HistoryRelocated` with no identity, account ID or path. |
| AC-08 | PASS (Sandbox, synthetic); owner live NOT_RUN | Sandbox covered uninstall, reinstall and deletion with a seeded configuration file. Re-attaching a real signed-in account after reinstall needs the owner's own sign-in and stays NOT_RUN. |

## Sandbox packaged lifecycle (2026-10-07)

Packages:
- Old: 2026.10.698.0, from `b7695ab`, before T-047.
- New: 2026.10.793.0, from `c18f1a7`.

Both were installed offline with the Windows App Runtime dependency. Guest script: `aiu047\in\guest.ps1`, kept in the session temp folder and not committed.

| Step | Verdict | Observation |
| --- | --- | --- |
| Old build baseline | PASS | Layout 2. `LocalState\budget` held `budget.lock` and the seeded `configuration.v1.json`. No history root existed. |
| Update to 793 | PASS (one check corrected) | Details below. |
| Uninstall | PASS | `LocalState` was removed. The history root and the seeded configuration remained. |
| Reinstall | PASS | The window opened and layout 3 was written to the new `LocalState`. The seeded configuration was unchanged, with no quarantine copy. |
| Delete stored data | PASS | Confirmed inline. The history configuration was gone within seconds; only `budget\budget.lock` remained. |

Update to 793, in detail:
- The window opened and layout 3 was written.
- `budget` moved to the real history root, and `LocalState\budget` was gone.
- The application log recorded `HistoryRelocated`.
- The script's strict check failed because a package-private `LocalCache\Local\AiUsage` directory existed. That directory is the virtualized parent the packaged process creates. The data itself was in the real excluded child: a non-packaged shell saw it there, it survived uninstall, and the packaged app later deleted it there. The check was stricter than AC-01 requires, so this is recorded as PASS.

The Windows App Runtime dependency deployment logged benign `0x80070490` and `0x80070003` AppX events in the first, aborted attempt. That attempt stopped because a host `tail -f` on the guest log blocked its writes; it is not product evidence.

## Independent review

A focused review of `1eb8c7c..efc19d4` (a fresh-context reviewer) returned "Approve with fixes": no Critical and two Important findings.
- I1: risk that the parent folder is virtualized. Resolved: Sandbox shows the excluded child is unvirtualized and persists.
- I2: deletion recreated the old store's lock, which caused a false `HistoryLeftInPlace` warning on every later start. Fixed in `c18f1a7`: deletion no longer recreates the relocated store, and a store holding only its lock is nothing to move.
- `LedgerSmoke` expected layout 2 after deletion. Fixed to 3; built but not run.
- Minor: design text, a test that checked encrypted bytes, missing version and reattach-event tests, an unlogged rejected map write, a misplaced doc comment, and AC-08 wording. All fixed in `c18f1a7`.

## Local checks (at `c18f1a7`)

- Infrastructure: 897/897
- Presentation: 232/232
- ProjectValidation: 82/82
- Validator: valid
- `git diff --check`: clean
- Windows Debug and Release builds: 0 warnings
- Release `--demo` startup smoke: window shown with Settings
- `ProviderCaptureTests.DisabledCaptureAndMalformedErrorAreNeverCalledComplete` failed once in one full run at `efc19d4`. It passed 3/3 in isolation and in both later full runs; it is unrelated to this change (response-capture file timing).
