---
provider: claude
source_verified_at: 2026-10-25
live_verified_at: null
classification: method-specific
confidence: official documentation for every surface; no live run yet
---
# Claude usage through the local Claude Code CLI - research

Question (owner, 2026-10-25): which native Claude Code surface lets AI Usage read a Claude
account's usage limits legally and without a ban risk, as a second connection method next to
the browser sign-in, when Claude Code and the app run on the same PC. A plugin that pushes
usage to the app was the owner's first idea.

Sources: Claude Code 2.1.296 installed on the owner's PC (`claude --help`, `claude auth --help`,
`claude plugin --help`), and the official documentation fetched on 2026-10-25 as Markdown from
code.claude.com/docs/en: `statusline`, `hooks`, `plugins-reference`, `plugins/components`,
`agent-sdk/typescript`, `costs`, `desktop`, `monitoring-usage`, `legal-and-compliance`. Nothing
was run against the provider; no credential store was read.

## Permission boundary

The [credential-use policy](https://code.claude.com/docs/en/legal-and-compliance#authentication-and-credential-use)
forbids third-party apps to offer Claude.ai login, to route requests through subscription
credentials, and to "collect, store, or intermediate Claude.ai credentials or session tokens".
It explicitly does not prevent "an end user from signing in to the unmodified Claude Code
binary with their own Claude subscription". The current T-007 browser connection reuses Claude
Code's public OAuth client inside AI Usage, which this policy restricts ([claude.md](../../providers/claude.md)).

A method in which AI Usage only starts the unmodified `claude` binary, or receives data that
binary hands to a user-configured script, keeps every credential inside Claude Code: the app
collects, stores and intermediates none, and Claude Code's own sign-in flow is the only one.
No inference is involved in the methods below. This is our reading of the public policy, not
a provider statement for this app; risk is rated low, against high for the T-007 method.

## Surfaces

| Surface | Data | Trigger and freshness | Inference | Changes on the owner's PC | Verdict |
| --- | --- | --- | --- | --- | --- |
| A. `/usage` report through `claude -p` | Full plan rows: `limits[]` with `kind` (`session`, `weekly_all`, `weekly_scoped`), `group`, `percent` 0-100, `resets_at` ISO, `scope.model`/`scope.surface` display names, `severity`, `is_active`; `extra_usage` (`is_enabled`, `monthly_limit`, `used_credits` in minor units, `utilization`, `currency`) | On demand, whenever the app asks; same server data as the browser method | None: `/usage` is a local command | None, if run with session persistence off in an app-owned folder | Recommended |
| B. Status line JSON `rate_limits` | `five_hour` and `seven_day` `used_percentage` and `resets_at` (epoch seconds) only; no scoped weekly rows, no usage credits | Only while an interactive terminal session runs, after its first model response; no data when Claude Code is idle; the Desktop app documents no status line | Uses responses of the owner's own work, none extra | Replaces or wraps the owner's `statusLine` setting with a script that writes a local file or calls the app | Optional live supplement later, not a full source |
| C. Plugin (hooks, monitors, MCP) | Hook inputs carry no rate-limit fields; `StopFailure` matcher `rate_limit` and `Notification` `quota_auto_resume_*` say only that a limit was hit or reset | Event-driven, in CLI, IDE and Desktop | None | Installs a plugin; plugin `settings` apply only `agent` and `subagentStatusLine`, so a plugin cannot install a status line | Not a usage source; at most a "limit hit" signal |
| D. Agent SDK `rate_limit_event` | `status` (allowed, allowed_warning, rejected), optional `resetsAt`, `utilization` | Only during an SDK query that sends prompts | Yes | - | Rejected (needs inference) |
| E. OpenTelemetry, `~/.claude` transcripts and `usage-data/report.html` | Tokens and cost estimates; no plan limits | Per session | - | Enables telemetry export | Rejected (no limits) |

### A in detail

The Agent SDK reference (`SDKAssistantMessage.usage_report`, type `SDKUsageReport`, SDK
0.3.273 and later, marked experimental) says that sending `/usage` as a prompt makes Claude
Code deliver the report as an assistant message whose `message.content` holds the text, with a
structured `usage_report` attached when the session authenticates with a claude.ai credential
that has a known plan type or the `user:profile` scope and the account is not on usage-based
billing. A `claude setup-token` token does not qualify. The SDK talks to the CLI over
`--output-format stream-json`, so the same message is expected from
`claude -p "/usage" --output-format stream-json`.

The `limits[]` rows match the `limits[]` array the browser method already parses
(`ClaudeQuotaParser`), and `extra_usage` matches its legacy usage-credit block, so the
normalized limit model needs no change (T-005 A-6). The report has no `spend` object and no
legacy `five_hour`/`seven_day` objects. Account identity is not in the report;
`claude auth status --json` is a local command that reports the signed-in account without
tokens (its exact fields are unverified).

Costs and limits:

- Each read starts a Claude Code process (a few hundred milliseconds to seconds and some
  hundred MB briefly). Run it with `--no-session-persistence` so no transcript is written, and
  in an app-owned empty working folder so no project settings, CLAUDE.md or trust prompt
  apply. `--safe-mode` skips the owner's hooks, plugins and MCP servers while keeping the
  normal sign-in; `--bare` cannot be used because it never reads the OAuth sign-in.
- Claude Code's request goes to the same rate-limited usage endpoint (R-233: about one read a
  minute per account). When it is limited, interactive `/usage` shows the last bars it loaded
  on this machine within 60 minutes with a "Showing last-known usage" note; whether a
  `-p` run marks such a snapshot in `usage_report` is unknown and must be checked live.
- The structured report is experimental and its shape may change; the text form stays as the
  documented fallback. Version drift is detected by a missing `usage_report`.
- Only the account Claude Code is signed in to can be read: one Claude Code account per
  Windows user, unless `CLAUDE_CONFIG_DIR` points at another profile (not in scope).

## Recommendation

Method A as the new connection "Claude Code on this PC": the app finds `claude.exe` on PATH,
runs `/usage` on its normal refresh schedule (R-233 floor), parses `usage_report` with the
existing limit parser, and shows the account from `auth status`. The owner's Claude Code
settings stay untouched and nothing is installed. A plugin is not needed: Claude Code exposes
no usage data to plugins or hooks. Method B can be added later as a free near-real-time
update while a terminal session runs, if A's refresh cadence proves too slow.

## Open questions for the live check

1. Does `claude -p "/usage" --output-format stream-json --no-session-persistence` emit an
   assistant message with `usage_report` on 2.1.296, and does `--safe-mode` keep it?
2. Wall time and exit code, signed out and rate-limited behaviour, and whether a last-known
   snapshot is marked.
3. Field names of `claude auth status --json` that identify the account without exposing
   anything secret.
