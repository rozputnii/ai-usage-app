---
id: T-NEW
type: bug
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-12, in conversation, answering the question that T-064 left open (its Out of scope): after a refused Codex renewal, show "Sign-in expired" instead of a provider error.
---
# Codex asks for sign-in after a refused renewal

## Problem

- Current: before a renewal the Codex session writes an intent that makes the stored grant
  non-replayable (T-028). When the renewal is refused, lost or interrupted, the intent stays.
  The refresh that saw a refusal shows "Sign-in expired", but the next manual refresh or launch
  loads the intact intent as `RecoveryRequired`: the card shows "Provider error", the tray says
  the reading stays "until the provider answers", and there is no Sign-in action, although the
  account can only recover through a new sign-in, which may supersede the intent.
- Expected: an intact intent of the stored grant loads as `ReauthenticationRequired` with
  `AuthenticationRequired`, without provider traffic, so the account shows "Sign-in expired"
  with its Sign-in action, as for any refused grant.
- Unchanged: the intent is never replayed; only explicit fresh authorization supersedes it, and
  the grant changes only after that sign-in succeeds. A corrupt or unreadable journal, an intent
  whose parent is not the stored grant, a legacy staging file and a locked store still load as
  `RecoveryRequired` or `StorageUnavailable` (provider error). Storage APIs (`CodexGrantStore`)
  still throw `RecoveryRequired` for an intact intent. The other providers, which do not use the
  journal. Layout and copy.

## Requirements

- R-01: A Codex refresh, resume or cached read whose load stops at an intact intent matching the
  stored grant returns `ReauthenticationRequired`, `AuthenticationRequired`, the cached reading
  as stale, and keeps `HasStoredGrant`.
- R-02: Every other journal failure keeps its current state.

## Design

`ProviderStateLease` already tells the intact-intent case apart from corruption (an empty
journal generation whose parent is the committed revision). It records that case in an
`IntentAwaitsFreshAuthorization` flag before throwing the same `RecoveryRequired`; only
`CodexSession` reads the flag. This changes no storage contract or file format, and every
renewal outcome that leaves an intent (refused, lost response, cancelled, network failure,
account mismatch) ends at the same state: the grant cannot be replayed, and a new sign-in is the
only way back.

## Acceptance criteria

- AC-01: After a refused renewal, the next refresh in the same process, a relaunch's cached
  read and its resume all report `ReauthenticationRequired` with one provider request in total;
  the test fails at the base.
- AC-02: The tests for a cancelled renewal and a declined fresh sign-in after an uncertain
  renewal now expect `ReauthenticationRequired`; corrupt storage, a locked store and a failed
  durable cutover keep `RecoveryRequired`.
- AC-03: The Infrastructure and Presentation suites, the app build and the launch smoke pass.

## Out of scope

- Live behavior, which is a post-deploy owner check.
