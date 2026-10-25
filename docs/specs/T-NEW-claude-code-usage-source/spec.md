---
id: T-NEW
type: feature
status: approved
goal: G-003
scope_version: 1
approval_basis: owner decision, 2026-10-25, in conversation. The owner selected the Claude Code part of the ideas-inbox entry on legal usage sources as an additional connection method, chose the `/usage` report through `claude -p` from the research, picked UI variant B and authorized one live run of that report on the owner's PC.
---
# Claude usage through the local Claude Code CLI

## Problem
The only way to connect a Claude account reuses Claude Code's public OAuth client inside AI
Usage, which Anthropic's credential-use policy restricts ([claude.md](../../providers/claude.md)).
On a PC where the owner already uses Claude Code, the unmodified Claude Code binary can report
the same plan usage itself, with its own sign-in, so the app needs no Claude credential at all
([research](research.md)).

## Requirements
- R-01: The Add account menu offers Claude in two ways: the existing browser sign-in and
  "Use Claude Code on this PC".
- R-02: A Claude Code connection reads usage only by starting the installed `claude.exe`
  directly, without a shell, with the fixed arguments `auth status --json` and
  `-p /usage --output-format stream-json --verbose --no-session-persistence --safe-mode`, in
  an app-owned empty folder. The app never reads Claude Code's credential files or settings,
  never writes them, and never sends any other prompt.
- R-03: The structured `usage_report` of that run becomes the account's reading through the
  existing Claude limit parser, so its cards, budgets and history behave like those of a
  browser-connected Claude account.
- R-04: The account is bound to the Claude Code account it was connected with (email and
  organization from `auth status`); a different signed-in account is a mismatch, never a
  silent switch.
- R-05: A run that shows any sign of a model request is rejected and its data is not used.
- R-06: Browser-connected Claude accounts, their stored state and their flow are unchanged;
  both kinds can coexist.

## Acceptance criteria
- AC-01: Clicking Claude in the Add account menu expands it into "Sign in with browser" and
  "Use Claude Code on this PC"; clicking it again collapses it; the other providers are
  unchanged. When `claude.exe` is not found on PATH or in `%USERPROFILE%\.local\bin`, the
  Claude Code item is disabled and shows "not found".
- AC-02: Choosing "Use Claude Code on this PC" opens no browser; on success a new Claude
  account appears with the session, weekly and model-scoped weekly limits and usage credits
  that the report carries, parsed by `ClaudeQuotaParser`.
- AC-03: The account card shows "via Claude Code" after the account name; the plan that
  `auth status` reports (Pro, Max, Team, Enterprise) becomes part of the default account
  name, as for other providers that report a plan.
- AC-04: Automatic and manual refresh rerun the report; the R-233 two-minute floor for Claude
  applies to these accounts too.
- AC-05: The runner passes arguments through `ProcessStartInfo.ArgumentList` with
  `UseShellExecute = false`. A run that shows a model reply, turns, a non-zero cost or model
  usage yields `InternalError` and no reading, and automatic refresh stops retrying it; a run
  without a local `usage` command result, or that exits non-zero, yields `InvalidResponse`.
- AC-06: Failures map to the existing kinds and keep the last reading: `claude.exe` missing
  → `ProviderUnavailable`; not signed in, or signed in without a claude.ai subscription (no
  `usage_report`) → `AuthenticationRequired`; a different email or organization than the
  connected one → `AccountMismatch`; no plan rows (`rate_limits` null) → `RateLimited`; no
  exit within 30 seconds or cancellation → the process tree is killed and `Timeout` or
  cancellation is reported.
- AC-07: Disconnecting a Claude Code account deletes only the app's own state for it.
- AC-08: Existing browser Claude accounts load, refresh, reconnect and disconnect as before,
  and reconnecting a Claude Code account uses the Claude Code method again.
- AC-09: A failed run records one diagnostic failure through the T-043 pipeline with its
  kind only: no process output, email, organization or path.

## Out of scope
- The status line, plugins and hooks (research: no usage data, or only while a terminal
  session runs); other providers; npm `claude.cmd` installs; several Claude Code profiles
  (`CLAUDE_CONFIG_DIR`); WSL.
- Importing or replaying Claude Code credentials (T-005 method A).

Variant: B (Claude row expands into the two ways), owner, 2026-10-25. The first-run screen,
which the variants did not show, lists Claude Code as its own card ("Connect").
