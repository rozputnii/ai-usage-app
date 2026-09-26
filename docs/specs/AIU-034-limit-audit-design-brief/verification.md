# AIU-034 verification

Phase A source research has started. The owner approved the specification on 2026-09-26.

## Gates

| Gate | Status | Record |
| --- | --- | --- |
| Specification review | PASS | Owner approved [spec.md](spec.md) in the session conversation on 2026-09-26. |
| Gate A | NOT_RUN | Owner review has not run; Phase A research is in progress. |
| Gate B | NOT_RUN | Phase B has not started. |

## Results by acceptance criterion

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | NOT_RUN | |
| AC-02 | NOT_RUN | |
| AC-03 | NOT_RUN | |
| AC-04 | NOT_RUN | |
| AC-05 | NOT_RUN | |
| AC-06 | NOT_RUN | |
| AC-07 | NOT_RUN | |
| AC-08 | NOT_RUN | |
| AC-09 | NOT_RUN | |
| AC-10 | NOT_RUN | |

## T-01 - 2026-09-26

PASS: current-source inventory and Core member coverage in research.md section 2, reviewed against baseline 27564d8. All four parsers, clients, state/cache formats and Windows LiveMapping inspected. Document validator command from tasks.md printed valid=true with no diagnostics; git diff --check exited 0. These are document/source checks only; product tests and live requests NOT_RUN. No product or stored-format change.

T-01 bookkeeping correction: the initial draft validated, but marking done with prose in the evidence field produced DONE_WITHOUT_EVIDENCE. Commit 6ee7f42 was mistakenly pushed before resolving that failure. The follow-up uses a repository-relative evidence artifact path; final validation is recorded only after rerun. No source finding changed.

## T-02 - 2026-09-26

PASS: research.md Claude plan/field matrix, G-CL-1 through G-CL-5, C1-C11 and provider append reviewed together. Pinned OMP and latest v18.3.2 inspected; no Team/Enterprise live claims, null limits stay unknown, current consumption and legacy Enterprise distinguished. Validator --json valid=true with no diagnostics and git diff --check exit 0. Live LC-01 through LC-05 NOT_RUN; no account requests. T-01 corrected evidence reference also passed validation before commit 4ed5ee4.

## T-03 - 2026-09-26

PASS: Codex matrix, G-CX-1 through G-CX-4, O1-O8 and provider append reviewed. Official pinned spend-control nested type reveals unparsed amounts/resets; units and plan response presence remain unknown. Balance never used as allotment; actual duration is response-driven; historical HTTP 400/403 causes not inferred. Validator --json valid=true and diff check exit 0. LC-06 through LC-11 NOT_RUN; no new account request.

## T-04 - 2026-09-26

PASS: Copilot matrix, G-GH-1 through G-GH-4, G1-G7 and provider record reviewed. Explicit unlimited remains distinct from unknown; current credit billing differs from legacy annual premium requests; Free live evidence is not generalized. Parser date normalization distinguished from documented UTC monthly reset. Validator --json valid=true and diff check exit 0. LC-12 through LC-17 NOT_RUN; no authenticated request.

## T-05 - 2026-09-26

PASS: Antigravity plan/field matrix, G-AG-1 through G-AG-4, A1-A6 and provider append reviewed. remainingAmount remains unit unknown; source credit UI distinguished from reusable transport and monthly Flow credits. No Free-to-paid generalization. Validator --json valid=true and diff check exit 0. LC-18 through LC-21 NOT_RUN; no CLI credential read, provisioning or provider request.

## T-06 preparation - 2026-09-26

Steps 1 and 2 completed: section 10 consolidates LC-01 through LC-21 with account, surface, evidence target and risk. The owner received per-ID authorization questions grouped by provider; each check requires its own response. Authorization is pending, and every live verdict remains NOT_RUN. No account access, sign-in or authenticated request occurred. T-06 remains in progress; T-07 has not been handed off.

Preparation checks PASS: the Phase A diff contains only eight Markdown files; added-line credential/identity pattern scan returned zero matches, and primary content review found no personal data, raw payloads or captures. Validator --json valid=true and git diff --check exit 0. These checks establish document consistency only and do not establish live-provider success. Repeat the privacy and document checks after any live-outcome edits.

## T-06 interruption checkpoint - 2026-09-26

Environment: Windows, default HTTPS browser Microsoft Edge; base f42247a. The initial
`git pull` reported already up to date and the working tree was clean. The owner's
session-scoped authorization covers only personal Claude Pro/Max after UI plan selection,
personal ChatGPT Plus/Pro after UI plan selection, and new LC-22 for Google AI Plus.

- BLOCKED: selecting the personal Claude and ChatGPT plan and observing LC-22. Initial
  in-app-browser navigation reached signed-out/public pages; no plan or usage was observed.
  At the owner's correction, in-app-browser use stopped. Native Computer Use opened the
  default Edge browser, then stopped this turn because it could not confidently determine
  the current browser URL to enforce policy. No browser input followed that stop.
- NOT_RUN: LC-01/02 and LC-06/07, because plan selection did not complete. Neither alternative
  is reported as the account's plan. LC-22 is separately BLOCKED before account observation.
- NOT_RUN: LC-03/04/08/09/11 and LC-12 through LC-17, reason
  "postponed by owner: work account or manual lookup".
- NOT_RUN: LC-05/10/18/19/20/21, not authorized. No AI Usage connection was accessed.
- No authenticated usage observation, sign-in, credential entry, account selection, terms
  acceptance, settings change or purchase occurred. No developer tools, network traffic,
  cookies, local storage or source CLI credentials were read.

The AI Plus matrix is separate and unknown/none. Existing provider matrices retain their
evidence levels, and all transport gaps remain open. Provider-record `live_verified_at`
values are unchanged because there are no UI-observed rows to date. T-06 remains in progress;
T-07 [opus] is the subsequent task, not ready for handoff on this checkpoint.

Checkpoint verification PASS: added-line secret/identity pattern scan found zero matches;
primary diff review found no account identities, balances, spend values, raw payloads or
screenshots. Only research.md, tasks.md and verification.md changed. Document validation
(`dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`,
using the existing user-local SDK) returned `valid: true` with no diagnostics;
`git diff --check` passed. Links and the retained T-07 amendment were reviewed. These are
document checks only. Live quota checks and product tests did not pass or run at this checkpoint.

## T-06 Chrome continuation - 2026-09-26

Environment: Windows, owner-selected Google Chrome, existing signed-in personal session;
base 355599e. The owner corrected the browser selection to Chrome in this same session.

- LC-01 PASS, UI observation only: `https://claude.ai/settings/usage` opened the personal
  Usage page, whose plan label was Pro. Session and weekly percentage-used rows were present;
  reset forms were time-of-day and weekday/time respectively. No model-scoped weekly row was
  displayed. A usage-credit balance and monthly spending section were present, with a dollar
  symbol and an explicit no-spend-limit label. No actual values were retained.
- A separate cloud-session included-credit row displayed a balance and expiry with time,
  GMT offset, month and day. Research now distinguishes this CL-C family from general
  usage credits and monetary spending. Recurring allotment, exact grant start, ISO currency,
  wire exponent and transport mapping remain unknown. Product breakdowns are not new limits.
- LC-02 NOT_RUN: the observed personal plan is Pro. No Max or work-plan inference is made.
- LC-06/07 remain NOT_RUN: ChatGPT plan selection is BLOCKED. LC-22 remains BLOCKED.
  After the Claude observation, the native control tool stopped this turn on the new-tab
  action because it could not confidently determine Chrome's current URL to enforce policy.
  No further browser input followed that stop. No Codex or Google account observation occurred.
- Other postponed/not-authorized verdicts remain unchanged. No sign-in, account selection,
  state-changing provider action, hidden storage/network inspection or credential access.

The Claude audit date is advanced for its explicitly scoped UI observations only. Its
wire-field matrix retains source/none levels; the separate UI matrix records live cells.
The Codex and Antigravity provider dates remain unchanged. T-06 is still in progress.

Continuation checks PASS: added-line secret/identity and monetary-value pattern scan returned
zero matches; primary content review confirmed only sanitized UI structure, units and reset
forms in the four changed Markdown files. No screenshots/captures or account values are
staged. Validator `--json` returned `valid: true` with no diagnostics, and `git diff --check`
passed. UI/source separation and the new provider-record link were reviewed. Product tests
NOT_RUN because this checkpoint changes documentation only.

## T-06 Chrome retry - 2026-09-26

Base 62b5e2f, same Windows/Chrome session; owner explicitly requested another attempt.
Navigation to `https://chatgpt.com/codex/settings/usage` was submitted through the existing
Chrome tab. Window inventory subsequently reported ChatGPT. Reading the page timed out
with `computer-use request timed out: get_window_state`, including one text-capture retry
after window reselection and a screenshot-only capture. No ChatGPT plan or quota content
was observed, so LC-06/07 stay NOT_RUN with plan selection BLOCKED. LC-22 stays BLOCKED;
LC-01 remains the already completed UI observation. No new provider live date or matrix
evidence is added. No authentication or provider-state mutation occurred. Any transient
new-tab screen observation was not saved to the repository.

Retry checkpoint checks PASS: validator `--json` returned `valid: true` without diagnostics;
`git diff --check` passed; added-line secret/personal-data scan returned zero matches.
Primary review confirmed the two changed Markdown files contain only the blocker and
handoff update, without private values or captures. Product tests NOT_RUN (documents only).
