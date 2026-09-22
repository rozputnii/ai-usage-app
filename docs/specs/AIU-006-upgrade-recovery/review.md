# Focused independent review

- Date: 2026-09-22.
- Frozen product candidate: `3c4e0ee`.
- Reviewer: existing Claude Code 2.1.278, reported model `claude-sonnet-5`.
- Isolation: fresh nonpersistent session, safe mode, Read/Grep/Glob only; no inherited
  implementation transcript, no write tools, no worker or provider credential access.
- Reason for tool choice: CONTRIBUTING requires independent review of durable-state
  changes. The repository-required Codex subagent `gpt-5.6-luna` was not among the
  available overrides; no alternate Codex model was selected. The repository policy
  explicitly excludes non-Codex review tools from that model restriction.
- Verdict: PASS. No blocking or material findings. Static review does not establish
  real Windows activation, package update, physical power-loss durability or provider
  access.

The reviewer traced checkpoint-before-journal ordering, fixed-path and reparse checks,
layout commit/cleanup, explicit restore, newer-layout refusal, provider file isolation,
startup/write gates and shutdown drain. The primary checked the findings against the
actual code and retained the raw local response under the ignored AIU-006 evidence root.

Two observations required no change: explicit restore intentionally discards preference
edits after the pre-migration checkpoint; a failed restore while another process owns
the lease uses the general RestoreFailed label rather than a contention-specific label.
Neither permits a write without the lease or alters the accepted scope.

Post-review targeted fixes: actual UI exposed a missing CanExecute notification for the
newer-layout Restore button; a negative diagnostic-path regression exposed an exception
type not handled by the presentation boundary. The primary reviewed both small fixes;
their red-to-green regressions and final suites are recorded in [verification](verification.md).
No credential, migration-ordering, path-allowlist or privilege boundary was expanded.
