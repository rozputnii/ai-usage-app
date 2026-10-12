# Real tasks S4a: backlog items

Run on the new PC after S3 (so arm E is final). Owner answers 2026-10-11: Q10 yes (all three),
Q11 yes (re-sign-in guidance in the style of existing sign-in messages, no layout change).
Owner decision 2026-10-11: only the log digest needed the current PC (done:
[digest](../results/S4-local-log-digest.md)); the three items moved to the new PC and use the
digest through `-Appendix ../results/S4-local-log-digest.md` where they need it (BL3). Each
item is a pair (arms A and E).

Operator notes:
- Close any installed AI Usage app on the machine before a C8 run (an open one blocked C8 on
  2026-10-10).
- `t061-tray-failure-recovery` stays untouched until the owner has looked at it (owner,
  2026-10-11). If the owner resolved it before S4a, drop BL2.
- For BL2 the harness clone keeps only its base, so the operator first creates a local branch
  `exp/bl2-base` in the real repository: `main` merged into `t061-tray-failure-recovery`
  (no push), and uses it as `-Base`. If that merge conflicts, BL2 is BLOCKED; report it.
- CR-T-002-01 looked obsolete on 2026-10-11 (the routing spike build was dropped in
  `27a14b0`, and no routing scenario exists in the UI tests), so BL1 starts with that check.

## Log digest {#digest}

Done 2026-10-11 on the owner's current PC; kept for a later refresh there. Single operator
task, not paired.

> Read the installed AI Usage app's own log files on this PC (find their location in
> docs/workflow/logging.md). Write `results/S4-local-log-digest.md` in the experiment folder with
> counts only: records per day and level, failure categories and their counts, repeated-failure
> summaries, watchdog or dispatcher stalls, tray failures, update events, and the time span
> covered. Never copy message text, account names, paths, identifiers or provider payloads; if a
> category name itself carries such data, generalize it. Change nothing in the app or its files.

## BL1 - CR-T-002-01 routing screenshots (T0) {#bl1}

> Backlog finding CR-T-002-01 (docs/backlog.md, "Non-blocking review findings"): capture routing
> scenario screenshots after the navigated route repaints. First check whether the routing
> scenario still exists: commit `27a14b0` dropped the routing spike build, and the UI tests may
> no longer contain it. If it is gone, retire the finding: change its row to "Retired: ..." with
> the evidence (commit and the absence in the tests), as other retired rows do. If it still
> exists, make its screenshots wait for the repaint the way the product smoke does with its
> settling interval, and run that scenario. Tier T0: the primary reviews the diff; C2 and C6.

## BL2 - WR-T-061-M1 tray failure streak fix (T1) {#bl2}

Out of S4a (owner, 2026-10-12): deferred to normal work, outside the eco scope. The owner updated
the Visual Studio Build Tools the same day (C9 had failed with MSB4236 in every S3 run); C9 is not
re-checked here.

> Backlog finding WR-T-061-M1: the reviewed fix "end a tray failure streak when the tone
> returns to the shown one" (commit `ed2de85`, already merged with current `main` in this
> clone's base) waits only for the launch smoke C8. Verify it on the merged tree: C4, C5, C7,
> C9 and C8 under the desktop lock. When everything passes, update the records: the T-061
> verification record's follow-up section, and close the WR-T-061-M1 row in docs/backlog.md.
> If C8 fails, diagnose (systematic-debugging) and report; do not weaken the smoke.

## BL3 - CR-T-003-03 usage 401 versus terminal refresh (T3) {#bl3}

> Backlog finding CR-T-003-03: refine usage-401 versus terminal-refresh handling and show
> immediate re-sign-in guidance. When it was written, a usage 401 invalidated the session at
> once, so a new sign-in was needed even when the stored refresh grant might still work.
>
> 1. Read the provider-evidence and security-lifecycle skills, the provider records and
>    R-177. Re-assess the finding against the current code for each provider; if it is already
>    resolved, retire it with file:line evidence and stop there for that provider.
> 2. Where it still applies: after a usage 401, try one refresh with the stored refresh grant
>    and retry the usage request once; never replay a grant more than once, never loop. A
>    terminal refresh error (the provider's invalid or revoked grant responses as recorded in
>    the provider record) means re-sign-in is needed. A workspace or identity mismatch still
>    fails closed.
> 3. Show the re-sign-in guidance in the existing sign-in state message style (owner,
>    2026-10-11: same style as current sign-in messages, no layout change, no new controls).
> 4. The installed app's log digest (appended below; counts only) is the evidence of how
>    often usage 401 and refresh failures occur.
> 5. Diagnostics follow the logging policy: one detailed record per failure, no logging of
>    routine success.
>
> Tests red to green with fixtures for: 401 then successful refresh then success; 401 then
> terminal refresh error then re-sign-in state; 401 after a refresh in the same cycle (no
> second refresh). C4, C5, C7, C8, C2, C6; the focused independent review and
> `/security-review` where available. Live behavior is a pending owner check after deploy.
