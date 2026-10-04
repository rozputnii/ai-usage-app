# Independent checkpoint review

Reviewer: fresh Codex subagent, GPT-6 Astra, reasoning effort low, read-only and without the implementation conversation. Frozen range: `383644c..2942e6feec44292763799b15660a9853f9087306`. Review followed CONTRIBUTING.md and the convergence-review skill. No reviewer builds, tests, input, capture, credential access or live-provider calls were performed.

## Findings and disposition

| ID | Severity | Finding | Primary disposition | Verification |
| --- | --- | --- | --- | --- |
| REV-01 | HIGH | AuditWindows.Session.Click/ScrollToTop/Type/Exit checked a UIA element's process but did not recheck owned foreground or screen-point ownership before physical input; another window could receive it | Corrected: owned foreground before keyboard/chords, per-character foreground checks, WindowFromPoint process checks before pointer movement/click/scroll, wait for owned exit foreground, ownership checks around capture | Windows driver build PASS, zero warnings/errors. Actual rerun: all 11 native tests BLOCKED before launch/input at locked-desktop prerequisite. Successful/covered-window input guards still need unlocked runtime evidence |
| REV-02 | MEDIUM | A Session constructor failure after launching the isolated app happened before the caller acquired IDisposable ownership, leaving the launched process/resources running | Corrected: constructor catches startup/readiness/foreground failures, terminates only its specifically launched child, disposes child/app/automation resources and rethrows | Windows driver build PASS. Constructor-failure cleanup runtime NOT_RUN: locked desktop rejects before launch |

Primary source inspection also changed the single-key helper from Keyboard.Press to Keyboard.Type so each test key is released. Capture checks sample the window's hit-test ownership before and after capture; this does not replace visual inspection.

## Reviewer assessment

The reviewer found no material regression in the five inspected product fixes. The replay composition omits live provider registration and validates temporary storage before diagnostics initialization. Corpus tests use real parsers, recorder/store and projection with independently stated values, including serialized observation replay. Reporting correctly distinguishes supporting tests, missing exact scenarios, physical verification and zero final screenshots.

The comprehensive task remains incomplete: native interaction, visual inspection, gallery, composite transitions and remaining controls cannot be marked PASS. The reviewer inspected the submitted test/build evidence without independently rerunning it. Live-provider/authentication behavior, credentials, installed-app operations, host trust/settings and excluded accessibility/display matrices were set aside because the current request prohibits them.

No automatic full-review loop was run. Targeted compilation and a real blocked native rerun followed the corrections. Unlocked runtime verification of those corrections remains explicit, alongside the other gaps in report.md and the inventories.

## Native checkpoint review

Fresh read-only reviewer: GPT-6 Astra, low reasoning, frozen `9e60f773..ed092f0db4c96d3c70112ccb8eb1913ef0a086f6`, without implementation conversation. The reviewer inspected stored XML but executed no tests, desktop input, Sandbox actions or provider access. No material finding in the overage, retry/cancel, workday or synthetic authentication changes.

| ID | Severity | Finding | Disposition and actual targeted evidence |
| --- | --- | --- | --- |
| REV-03 | P2 | Single quotes around the powershell.exe -File FirstPage argument are literal and break the filter | Corrected: omit empty argument, pass validated nonempty value without quotes; actual runner-check-2 overview Used/Left native test PASS 1/1, 29.219 seconds. Earlier login bootstrap failure preserved separately |
| REV-04 | P2 | CTL-05 Escape/no-change and CTL-26 settings Left claims exceeded actions in the cited native test | Corrected both to NOT_RUN; exact physical actions still pending |
| REV-05 | P2 | Failed capture followed by failed diagnostic write skipped owned-process cleanup | Corrected with best-effort evidence and cleanup/disposal in finally; new actual guest regression failed before the fix (cleanup-red.xml) and passed after it (cleanup-green.xml, 1/1) |

The final independent verdict was FAIL for these three findings; dispositions above record subsequent primary corrections, not a rewritten reviewer verdict. Only targeted reruns follow the corrections. The comprehensive audit remains incomplete.

## Shell and history follow-up

Fresh read-only reviewer `/root/audit_shell_review_3`, GPT-6 Astra with low reasoning effort, inspected `9e51fbb` plus the current driver diff and stored evidence. Verdict: FAIL with two findings. The reviewer did not execute tests or operate the desktop.

| ID | Severity | Finding | Primary disposition | Verification |
| --- | --- | --- | --- | --- |
| REV-06 | P2 | ShellClick computed its first point from the cached element before its fresh lookup; a zero-bounds element failed before the correction could apply | Fresh positive-bounds lookup now precedes pointer movement and input, with native ownership retained. The unsupported dead-icon-removal comment was removed | Actual four-case `tray-review-regressions-6` rerun: 2 PASS / 2 FAIL. The stale-point failure no longer occurred; visible-case overflow lifetime remains unresolved and requires its own actual rerun |
| REV-07 | P2 | A history refresh retained the day selected when the read began, overwriting navigation made while it was pending | Select the current day only after the read completes and its generation/identity guards pass | New regression actually failed before correction; full Presentation PASS 185/185 in `presentation-history-navigation.xml`. Native gain/loss history transitions PASS 2/2 in `controls-regressions-5`, before this latest selection correction; final-build native recapture pending |

The reviewer also recommended checking the native XML for a nonzero executed count and zero skipped/not-run tests before the Sandbox runner reports PASS. This is a harness evidence improvement, not an additional reviewer product defect.
