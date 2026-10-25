---
id: T-NEW
type: design
status: approved
goal: G-003
scope_version: 1
---
# Claude usage through the local Claude Code CLI - design

## Account kind

A Claude Code connection is a new registry provider key, `claude-code`, next to `claude`.
The key already separates everything the account service keys by provider: the session
factory, the `(provider, identity)` uniqueness rule, reconnects and pending-storage cleanup.
The presentation maps it to `ProviderKind.Claude`, so names, marks, the R-233 floor and the
limit cards need no change, and only the card caption and the reconnect path read the key.
Limit keys come from `ClaudeQuotaParser`, which names them `claude`, so budgets and caps
match browser accounts. The quota-observation recorder receives `claude` for both keys.

Alternative considered: a method field on `AccountRecord` and `PendingAccountStorage`. It
needs two schema changes and a switch in every place that creates a session, for the same
result. Either choice makes an older build reject the new registry entry (recovery screen);
the owner's install only updates forward.

## Session

`ClaudeCodeSession` implements `IProviderSession` with the same gate, lease and cached-state
pattern as `ClaudeSession`, over a `ClaudeCodeStateStore` (DPAPI, as every provider state)
that holds the bound identity, the plan type and the last reading; no secret exists to store.

- Connect: `auth status --json` must report `loggedIn` with a claude.ai subscription
  (`authMethod` `claude.ai`); then `/usage` must yield a report. Only then is state saved.
  Reconnect requires the same identity, as the account service already checks.
- Refresh: `auth status` first (about 150 ms, no network); a different email or organization
  returns `AccountMismatch` with the cached reading. Then `/usage` (about 1.5 s live).
- Disconnect: deletes the state file only.
- Identity: `ProviderIdentity(email, orgId)`, kept only in the protected registry and state.

## Runner

`ClaudeCodeRunner` (internal, replaceable in tests) locates `claude.exe` on PATH, then in
`%USERPROFILE%\.local\bin`, and starts it with `ArgumentList`, `UseShellExecute = false`,
`CreateNoWindow`, redirected output, closed input, the owned folder `claude-code` under the
app root as working directory, a 1 MB output cap and a 30-second deadline. On timeout or
cancellation it kills the process tree. The first live probe showed why no shell may sit in
between: Git Bash rewrote `/usage` into a path and the text reached the model as a prompt.

## Report parsing

`ClaudeCodeReport` reads the stream-json lines: the `result` line must be `success`, name
`local_command` `usage`, have `num_turns` 0, `total_cost_usd` 0 and an empty `modelUsage`;
the assistant line with `local_command_run.command` `usage` carries `usage_report`. Its
`rate_limits` object has the `limits[]` and `extra_usage` members of the OAuth usage payload,
so it goes to `ClaudeQuotaParser.Parse(JsonElement)` unchanged.

## UI

The provider flyout keeps one row per provider. The Claude row toggles two indented rows:
"Sign in with browser" (the existing flow) and "Use Claude Code on this PC" ("not found"
when the runner cannot locate `claude.exe`, checked when the menu opens). The live source
gets a Claude Code sign-in entry that calls the account service with `claude-code`; the strip
shows Claude as usual and never offers a code field for it.
