---
id: AIU-031
type: spec
status: implementing
goal: G-003
scope_version: 1
approval_basis: Owner requests, 2026-09-24, in the session conversation - make the usage view compact and show only the limit bars (remove the top summary panel, the captions left of the bars, and the percentages, reset times and dates right of them), then color weekly limits by an even daily share so the bar turns orange when today's share is nearly used and red once it is used, and for the 5-hour window simply turn red at 20 % or less remaining without time pacing. The owner accepted the proposed details (window names once per provider, reset time only when a limit is exhausted, a stale mark instead of error text, other groups left to account detail, a smaller window). The bar split with a pace mark, the 30 % warning level and deferring working-day settings are the recommended defaults, recorded as D-181.
---
# AIU-031 - Compact usage view with daily pace colors

## Outcome

The usage view shows almost nothing but the limit bars. One account is one line, and each bar's
color says whether today's use keeps pace with the limit: green on pace, orange to slow down,
red once today's share is used.

## Scope

- Remove the Overview summary block (accounts, lowest remaining, next reset, need attention)
  and its scope note. The tray keeps its own summary.
- One line per account: label, at most one status mark, one bar per window of the primary
  group, and the sign-out icon. Window names appear once per provider above the bar columns.
- No plan, History link, freshness pill, percentages, reset times or dates on the line. Each
  bar's hover text and accessible name carry the reading, the relative and exact reset and the
  pace advice. Account detail and its History button keep everything else.
- An exhausted bar shows only "Back in …" beside it.
- Status mark, most urgent first: a failed refresh (click retries), a stale reading (bars
  dimmed, never presented as current), or a warning/critical pace color.
- Signed-out and sign-in-expired accounts replace their bars with "Signed out" or
  "Sign-in expired" and an inline Sign in link.
- Other quota groups, contexts, extensions and the sparkline leave the usage view; they remain
  in account detail.
- The default window is 760 × 600 effective pixels.

## Pace colors

- Windows shorter than one day (the 5-hour window): red at 20 % or less remaining, otherwise
  green. No time pacing.
- Windows of one day or longer with a known length and reset (weekly, monthly): the remainder
  is split into even local-calendar-day shares from the window start to its reset. What may still
  be used today is the remainder minus the pace mark, the share that must be left at the end of
  today. Unused earlier days carry into today and overspending shrinks it; partial first and last
  days get proportional shares.
- Today is red once its share is used and orange when less than 30 % of it remains; otherwise
  green. The bar keeps that color across its whole fill, fades the part reserved for later days
  and draws the pace mark.
- Stale, unknown, unlimited, untimed or already-reset readings get no advice and keep the factual
  threshold color. Exhausted stays a full red bar.
- Advice only: readings, account detail, the tray and notifications keep factual thresholds.
  Pace advice is not a history-based forecast (AIU-024) and needs no stored history.

## Excludes

Working-day or custom schedules, pace notifications, per-hour pacing of short windows, and
changes to provider data, notification rules or stored formats.

## Acceptance criteria

- AC-01: The usage view has no summary block; each visible account is one line with its label,
  at most one status mark, one bar per primary window and the sign-out icon; window names
  appear once per provider.
- AC-02: No percentage, reset time, date, plan, History link or freshness pill appears on a line;
  the bar hover text and accessible name carry the reading, reset and pace advice; an exhausted
  bar shows "Back in …".
- AC-03: A window shorter than a day is critical at or below 20 % remaining and otherwise OK,
  with no pace mark.
- AC-04: A weekly window splits its remainder into even local-day shares with carry-over and
  proportional partial days; today is critical when its share is used, warning below 30 % of it,
  otherwise OK; the bar draws the end-of-today mark.
- AC-05: Stale, unknown, unlimited, untimed and passed-reset readings get no pace advice; stale
  bars are dimmed and marked.
- AC-06: Failure, stale and attention marks appear by priority; signed-out and expired accounts
  show an inline Sign in prompt instead of bars.
- AC-07: Deterministic tests and the interactive demo and product Windows smoke pass.
