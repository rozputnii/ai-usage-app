---
id: T-064
type: bug
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-12, selecting backlog finding CR-T-003-03 with its acceptance fixtures; owner, 2026-10-11, for the guidance (the existing sign-in state message style, no layout change, no new controls).
---
# Renew once after a usage 401

## Problem

- Current: a usage request rejected with 401 marks the account as needing a new sign-in at
  once, although the stored refresh grant may still work. Codex, Claude and Antigravity keep
  such a grant; automatic refresh then skips the account, so it stays "Sign-in expired" until
  the owner signs in again. An Antigravity renewal refused with `invalid_grant` is not recorded
  as needing sign-in: the account shows "Sync failed" and later ticks send the refused grant
  again. The Codex console reports a usage rejection only at the next `refresh-auth`.
- Expected: a refresh that gets a usage 401 renews the stored grant once and reads usage once
  more. A refused renewal (the terminal responses in each provider record) or a second 401
  needs a new sign-in, shown at once in the existing "Sign-in expired" state.
- Unchanged: Copilot, which has no refresh grant (`CopilotSession.cs:37-53`): a usage 401
  still needs a new sign-in, so CR-T-003-03 is retired for it. Identity and workspace checks,
  the Claude pre-renewal marker, the Codex renewal journal, the sign-in screens, the card
  layout and all copy.

## Requirements

- R-01: In one refresh, a usage 401 renews the stored grant only when that refresh has not
  already renewed it or just signed in; it reads usage once more after a successful renewal.
  No refresh renews twice, sends a grant twice or loops.
- R-02: A refused renewal, a second usage 401 or an identity or workspace mismatch on renewal
  ends in the existing reauthentication state (Codex: a mismatch keeps its recovery state). For
  Claude and Antigravity this survives a restart without another provider request.
- R-03: The account shows the existing "Sign-in expired" mark, tooltip and Sign-in action; no
  new text, layout or control.
- R-04: Diagnostics: a renewed 401 writes no session-level record (the HTTP records already
  show both requests); a final failure writes one `OperationFailure` record, as before.
- R-05: The Codex console says a new login is needed as soon as a usage request makes its
  in-memory session unusable. It still never retries authentication by itself.

## Acceptance criteria

- AC-01: For Codex, Claude and Antigravity, fixtures show 401, then a successful renewal with
  the stored grant, then a usage reading.
- AC-02: For the same providers, 401 then a terminal renewal error ends in
  `ReauthenticationRequired`; Claude and Antigravity keep it after a restart without traffic.
- AC-03: For the same providers, a 401 after a renewal in the same refresh does not renew
  again.
- AC-04: A mismatch on the renewal after a 401 fails closed for each provider.
- AC-05: The Infrastructure and Presentation suites, the app build and the launch smoke pass.

## Out of scope

- Codex after a refused renewal: its intact renewal journal loads as `RecoveryRequired` on the
  next manual refresh or launch (provider error, no Sign-in action). Changing that is a change
  to the journal's recovery rule and is left to the owner. Resolved by T-066 (owner,
  2026-10-12).
- Claude's rule that any failed renewal, including a network failure, needs a new sign-in.
- Live provider behavior, which is a post-deploy owner check.
