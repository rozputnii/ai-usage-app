---
name: post-deploy-check
description: Owner-invoked check of the installed Preview after an update. Use only when the owner asks to check the installed app, its version or its logs against the "Pending owner checks" list.
---
# post-deploy-check

Owner decision OD-20 (2026-10-09) and R-190: the owner's manual and live checks happen in the
installed app after deployment. This skill helps the owner close them. It runs only when the
owner asks for it.

## Boundaries

- Read only. Never sign in, never change the app's settings or data, never install or update.
- Never read credentials, DPAPI stores, provider stores, source-CLI credential stores, or
  `response-*` and `trace-*` log files (they hold sanitized provider bodies and verbose detail).
- Record verdicts and sanitized event names only. Never copy raw log lines, paths, account
  names, e-mail addresses or identifiers into Git, the chat or a commit.

## Steps

1. **Installed version.** `Get-AppxPackage -Name AiUsage.Dev` gives `Version` and
   `PackageFamilyName`. Map the version to its source with a read-only
   `gh api repos/rozputnii/ai-usage-app/releases/tags/preview-<version> --jq .target_commitish`,
   then confirm with `git merge-base --is-ancestor <feature commit> <source>` that each pending
   item's merge is included. An item whose merge is not included stays pending.
2. **Logs.** Read only `application-*.jsonl` and `critical-*.jsonl` under
   `%LOCALAPPDATA%\Packages\<PackageFamilyName>\LocalState\logs`, from the update time on.
   Count events by `eventId` and `severity`. Look for the item's expected outcomes and for
   failures (`critical-*` files, repeated warnings, `PreviousExitUnknown`, `LoggerHealth` drops).
   The reading guide is docs/workflow/logging.md.
3. **Owner observations.** For checks that need the owner's eyes or signed-in accounts, list
   them in one question and record the owner's answers as "owner-reported PASS (date)" or FAIL
   with the owner's words summarized.
4. **Record.** For each item in the backlog's "Pending owner checks" list: PASS (log evidence
   or owner-reported), FAIL, or still NOT_RUN, with the installed version and the date. Move
   PASS items into the feature's verification record (one line in its AC table or Not run
   table) and delete them from the list. A FAIL becomes a backlog bug item the owner selects.
5. Commit the record changes under the Git flow; this is a T0 docs change.
