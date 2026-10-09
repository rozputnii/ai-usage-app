# T-031 verification

Date: 2026-09-24. Windows 11 Pro 10.0.26200 x64, .NET SDK 10.0.401 (global.json), display
scale 125 %. Base `205268f` (T-030); completed in the commit that adds this record. The owner's
packaged debug instance was running and was not touched; every agent-launched instance was a
separate unpackaged process with an isolated state directory under the session scratch folder.
Demo time is Monday 15 Sept 2026 12:00 UTC with weekly windows resetting Thursday 18 Sept 09:00.

## Results by acceptance criterion

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `OverviewTests.F02GroupsByProviderInManualOrderWithWindowNamesOncePerProvider` asserts sections, rows and the once-per-provider window names. Captured demo window at 760 × 600 effective px: no summary block, one line per account, sign-out icon at the right edge. |
| AC-02 | PASS | `CompactRowsShowOnlyPrimaryBarsWithPaceColorsAndDetailsInHoverText`: the hover text of Personal's weekly bar holds "61 % left", the reset and "You can use 27 % more today"; Experiments shows "Back in 1 m". Interactive hover on Research's weekly bar showed "Weekly window · 34 % left · Resets in 2 d 21 h (18 Sept, 09:00)" and the pace line; the wording for less than 1 % was then corrected to "less than 1 %". |
| AC-03 | PASS | `QuotaPaceTests.WindowsShorterThanADayUseOnlyTheTwentyPercentFloor` (20 and 8 critical, 21 and 95 OK, no mark); Research's 8 % session bar renders red without a mark. |
| AC-04 | PASS | `QuotaPaceTests`: full-day share 24/168 with the mark at 57/168; OK/warning/critical at 48.2/37/33.9/30 %; carry-over and overspend; partial first day 15/168 and a zero mark on the reset day; days follow the display time zone. Captures show Personal's weekly bar green with the faded reserved part and the mark, and Research's weekly bar orange. |
| AC-05 | PASS | `QuotaPaceTests.StaleUnknownUnlimitedAndUntimedReadingsGetNoAdvice`; Work (stale, network failure) has no pace mark, dimmed bars and the failure mark. |
| AC-06 | PASS | Row status priority and the sign-in prompt are covered by the compact row test and `AccountTests.RowSignOutIsImmediateKeepsIdentityAndHistoryAndHidesByDefault` (signed-out row offers Sign in). Captures show the error mark on Work and warning marks on Research and Experiments, colored after a token fix for icons. |
| AC-07 | PASS | Presentation 173/173, Infrastructure 330/330, ProjectValidation tests 80/80, documents valid. Windows smoke rows launch, navigation, theme, repeated-exit and capabilities: demo 5/5 and empty isolated product state 5/5; navigation now opens History from account detail. Unsigned MSIX `2026.9.2452.0` built (SHA-256 `A3E059594E0526A202ABE7516E57EBDD0C0D52E3E97B15235D6A850B370DE2D5`), not installed. |

## Defects found and fixed during the checks

- Status glyphs rendered as boxes because an escape was lost; restored the Segoe Fluent code points.
- `Tokens.Foreground` ignored icons, so marks were grey; it now colors `IconElement`.
- With a small share left today the bar read as neutral grey; the whole fill now keeps the pace
  color and only the reserved part is faded.

## Not run

- NOT_RUN: pace colors on live provider readings; demo data and deterministic tests only.
- NOT_RUN: smoke rows `tray-exit` and `close-to-tray` (the owner's instance was running and the
  tray icon is found by name).
- NOT_RUN: packaged install and update harnesses, and screen-reader acceptance beyond automation
  names.

## Owner closure - 2026-09-29

Owner decision, 2026-09-29: closed. Pace colors on live readings are not verified because the T-034 work-day budget replaces them. The remaining NOT_RUN rows are deferred to the T-034 implementation items. The NOT_RUN rows above stay NOT_RUN; they are not converted to PASS.

## Claude shared weekly limit correction - 2026-10-03

Base `d0e2f81`; changes in the commit containing this record. The owner reported that a
fully consumed personal Claude weekly limit appeared as a green "100 % left" session bar.
Existing app-owned T-043 sanitized captures and the running account detail confirmed
`five_hour.utilization = 0`, no session reset, and `seven_day.utilization = 100` with a
weekly reset. Parsing preserved both readings; Overview selected only the first group.

The live adapter now marks both shared Claude groups as headline windows. Overview renders
all marked groups and retains the original group/window identities for preferences and
detail. Model-specific limits stay separate. No quota values, provider requests, credentials
or durable formats changed. Existing sanitized response captures answered the debugging
question, so no additional runtime logging was needed for this presentation-only correction.

- PASS: synthetic regression first failed for both exhausted and partially used weekly
  limits (only `5h` rendered). After the fix, both `5h` and `7d` render; weekly remainder,
  exhaustion, reset text, attention status, model-group exclusion and hidden-group behavior
  are checked by `LiveAdapterTests.ClaudeOverviewIncludesSharedWeeklyLimitWithoutPromotingModelLimits`.
- PASS: Presentation suite 264/264; Infrastructure suite 590/590, Release .NET 10.0.401.
- PASS: independent read-only review of `bb60a94`, no actionable findings; no independent
  live execution was claimed.
- PASS: unpackaged Windows Release build, no warnings/errors. Unsigned MSIX `2026.10.355.0`
  built and manifest identity/version checked; SHA-256
  `E85FCC7BCBCC9322A1B9D36542AB2FDF67CC23F16A5AF8C8A72C452D8251DC2A`.
  Toolchain warning: unavailable `mspdbcmf.exe` prevents a symbols package, not the MSIX.
- FAIL then PASS: the first isolated Demo `ShellLaunchesNavigatesAndExits(navigation)`
  attempt could not find `RowOpen` immediately after returning from Settings. One rerun
  of the same build passed Settings, account, history, return navigation and confirmed exit.
  No product or harness changes were made for the retry; the first failure is retained in
  ignored local evidence, rather than treated as a passing run.
- PASS: actual corrected `bb60a94` Windows instance, 2026-10-03 17:03 Europe/Lisbon,
  resumed the same owner-operated app state after the old process exited. Claude was fresh,
  with separate `5h` and `7d` columns, `7d` at **0 % left / Exhausted**, a critical bar,
  attention mark and "Back in 2 d 18 h". Settings and return to Overview also worked.
  Codex and Copilot remained connected. Local evidence is retained under
  `.ai-usage-local/AIU-043/claude-overview-live.{txt,jpg}`; no session files are committed.
- NOT_RUN: package installation/update, unrelated UI scenarios, and inference attempts.

Next action: none for this correction. The corrected isolated instance remains open for
the owner. The five-hour reading is preserved as reported, not interpreted as permission
to use Claude while the shared weekly limit is exhausted.
