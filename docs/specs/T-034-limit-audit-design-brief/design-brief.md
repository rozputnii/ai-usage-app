# T-034 design brief - budget-aware single window

## 1. How to use this brief

This brief is the design input for Claude Design. It asks for a redesign of the AI Usage main
window, a Windows desktop app that shows how much of each AI subscription limit is left.

- **Binding content.** Sections 2 to 5 fix what the interface must show and how it is
  organized. Sections 6 to 9 fix the visual identity, the deliverables, the acceptance rubric
  and the platform limits. A direction may choose layout, form and wording freely within them.
- **All data is synthetic.** Every account, amount, date and time in this brief is invented
  for design. No value comes from a real account. Prototypes use only the section 4 scenario
  or other invented values.
- **State names are identifiers.** The names in section 3 are used unchanged in the rubric,
  the review and the prototype's state list. Display wording may be shorter, but every state
  stays recognizable, and the labels "estimate", "assumed", "tracked since" and "period
  unknown" keep their meaning.
- **Figures follow the model.** Symbols such as `N`, `T` and `B` come from the accepted
  research: [research.md](research.md) sections 5 to 9. A design never computes a figure
  differently from section 4.

## 2. User and jobs (B-1)

The user is a developer with several work and personal AI subscriptions: Claude, Codex through
ChatGPT, GitHub Copilot and Antigravity. They open the window or the tray flyout many times a
day for a few seconds, and they want an answer without reading. This scope has one account per
provider.

Owner delivery amendment (2026-10-03): T-039 must implement multiple simultaneous
accounts of each provider under R-073. Reuse the provider -> account -> limits
hierarchy below, with independent account labels, limits, budgets, history, status
and actions in the main window and tray. Adding another account must not disconnect
the first. The one-per-provider counts in this original reference scenario remain
fixture descriptions, not a restriction on T-039 delivery. See
[T-039](../../backlog.md#done-index)
for lifecycle and acceptance requirements.

The four glance questions, verbatim from the specification, and the section 3 element that
answers each:

| Glance question | Answered by (section 3) |
| --- | --- |
| Can I keep this pace today? | The budget state of each limit (OK, attention, today used, neutral) with the deviation, "ahead by" or "behind by"; for a five-hour window, its R-09 colour. |
| How much of today's budget is left? | Left today, beside used today and today's share `T`; "used today since HH:MM" when the start of the day was not observed. |
| Which limit binds? | The account status, which is its most constraining limit (section 3.11), and the binding source of a pool's effective limit, the personal cap or the provider. |
| When does it come back? | The reset (provider or assumed), the five-hour countdown from amber, the return time of an exhausted limit and the session estimate. |

Secondary jobs, all inside the same window: rename an account, set work days and personal
caps, sign in and sign out, and look back at a limit's local history.

## 3. Data and states (B-2)

Every state the interface must be able to show, one row each. "Applies to" names the provider
families of [research.md](research.md) section 5.5. `L` is the effective limit, `U` the amount
used now, `U0` the amount used at the start of the local day, `S` and `R` the period start and
reset.

### 3.1 Limit kinds

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| five-hour window | Claude session (CL-S), Codex primary window with a 5 h duration (CX-P), Antigravity five-hour group (AG-5) | Used and remaining percent, the R-09 colour and, from amber, the countdown to its reset. | A daily budget, a pace mark, a session figure or hour segments. | 5.5, 8.1; R-09, R-11 |
| weekly window | Claude shared weekly (CL-W), Codex secondary window with a 7 d duration (CX-S), Antigravity weekly group (AG-W) | Used and remaining percent on a 100 % scale, the daily budget with work days, the reset, and sessions when the estimate is ready. | An absolute amount, or a sum with any other limit. | 5.5, 8.1 to 8.4 |
| model-scoped weekly window | Claude scoped weekly (CL-M), one per opaque scope; Codex additional groups (CX-A) | Its own row with the provider's scope name, the daily budget, and the binding mark when it is the most constraining limit. | A sum with the shared weekly window; a session figure taken from the shared pool (CL-M has none; a CX-A group only from its own pair). | 5.5 (M-03), 7.4 |
| other percentage window of a day or longer | Codex windows with another returned duration; the Codex individual control (CX-I) on its percentages | As a weekly window, with its own period; a start assumed from the reset is labelled "assumed". | A duration or unit that the provider did not return. | 5.5, 8.3 rules 1 and 2 |
| countable pool | Copilot request pools (GH-C chat, GH-I completions, GH-P premium), unit requests | Used of the limit in requests, the provider's remaining, the daily budget and the personal cap. | A conversion to percent of another limit or to money. | 5.2, 5.5 |
| balance-only credit pool | Codex credits (CX-B) | The provider balance as a fact. With a personal cap, the budget on tracked consumption, labelled "estimate" and "tracked since". | The balance as used or as an allotment; tracked use as provider data; a budget without a personal cap. | 5.5, 6.5; R-185 |
| monetary pool | Claude extra usage (CL-X, CL-D) | Amounts in the pool's currency with its minor units, the provider limit, the personal cap, the binding source, and the budget on tracked consumption in the assumed calendar month. | A default currency, a sum or conversion across currencies, or an amount with more precision than its minor unit. | 5.2, 5.5, 6.5, 8.3 rule 4 |
| secondary amount, unit unknown | Antigravity `remainingAmount` of a group (AG-R); raw Codex individual-control amounts (CX-I) | The number as a fact of its window, marked "unit unknown". | A bar, budget, cap, unit name or separate limit row. | 5.2, 5.5 |

### 3.2 Reading states

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| fresh | A reading at most 15 minutes old | Values at full emphasis, without an age mark. | A "stale" mark. | 6.2; R-099 |
| stale (dimmed) | A reading more than 15 minutes old | The reading and every figure computed from it dimmed, with its time, for example "as of 13:38". The day's `N` and `T` stay at full emphasis because they are fixed for the day. | Stale values at full emphasis; stale values hidden or replaced by zero. | 6.2, 8.8; R-15 |
| unknown | Any field without a value | An explicit unknown mark in place of the value. | 0, an empty bar that reads as 0 %, a full bar, or "unlimited". | 5.1; R-15 |
| refresh failed | An account whose latest refresh failed | A mark on the account that the refresh failed; the last valid reading stays and turns stale after 15 minutes. | The last reading removed or zeroed. | 6.2; R-099 |
| gap in history | The inline history of a limit | A gap drawn as a gap between observed stretches. | A gap drawn as zero use or bridged by interpolation. | 6.2; R-14, R-184 |

### 3.3 Period states

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| provider reset | A limit whose provider reports its reset | The reset: relative within the day, weekday and time beyond it; a date-only reset (Copilot) as a date. | More precision than the provider sent, such as a time for a date-only reset. | 5.2 (reset precision), M-04 |
| assumed reset | Limits without a provider reset: Claude extra usage, Codex credits | The period bounds (the calendar month, from local midnight on the 1st) labelled "assumed". | An assumed bound presented as the provider's. | 8.3 rule 4; R-06 |
| assumed start | Windows with a provider reset but no duration that are documented as monthly: Copilot request pools, the Codex individual control | The start, one calendar month before the reset, labelled "assumed"; the provider reset stays a fact. | The start presented as provider data. | 8.3 rule 2 |
| period unknown | A window with a provider reset but no known duration: an unknown Claude kind, a Codex window without `limit_window_seconds`, an unrecognized Antigravity token | Used, remaining and the provider reset as facts, and "period unknown" as the reason there is no budget. | A budget, a work-day split, a calendar-month default, or a name such as "weekly" that implies a duration. | 8.3 rule 3; R-185; E13 |
| past its reset | A reading whose reset instant has passed, before a reading of the new period arrives | The old reading marked as past its reset, and "budget not ready". | The old period's figures presented as current, or a zero budget. | 6.3, 8.6 |

### 3.4 Limit states

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| within | `U` below `L` | Used against the effective limit. | Nothing extra. | 8.8 |
| at limit (exhausted) | `U = L` and `L > 0`; for a percentage window, exhausted | "Exhausted" or "at limit" and when it comes back, on a day off too. | A neutral or OK look; an exhausted limit hidden behind a healthy five-hour bar. | 8.8; R-10; E10a |
| over limit | `U > L` where the provider limit binds (provider overage) | "Over limit by" `U - L` in the native unit. | A negative remainder presented as available. | 8.8; E11b |
| over cap | `U > L` where the personal cap binds | "Over cap by" `U - L`, the cap labelled as the user's. | The cap presented as the provider's limit. | 8.8; E05b, E05c, E10b, E11d |
| not included | A provider zero entitlement with `U = 0` | "Not included", neutral; it does not raise the account status. | A zero budget, a bar, or "unknown". | 8.1 (M-01); E11a |
| capped at 0 | A personal cap of 0 with `U = 0` | "Capped at 0", neutral. | A zero budget or a bar. | 8.1; E11d |
| unlimited | An explicit provider unlimited flag | "Unlimited" as the provider's fact. Without a personal cap: no bar, remainder or budget. With a personal cap: the cap is `L` and binds, and the pool shows its bar, budget and states on the cap like any capped pool, with "unlimited" beside it as the provider's fact. | A bar or remainder without a cap; a set cap ignored because the provider is unlimited; unlimited inferred from a missing limit; an amount the provider reports beside the flag used as `L`. | 5.4, 8.1; E11c |
| limit unknown | Provider limit unknown or explicit null, and no personal cap | The used amount as a fact, "limit unknown" as the reason there is no budget, and the offer to set a cap. | 0 or unlimited. | 5.4 |

### 3.5 Budget states

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| OK | Left today at or above 30 % of `T` | The state and left today. | Nothing extra. | 8.8 |
| attention | Left today above 0 and below 30 % of `T` | The attention state and left today. | Colour as the only signal. | 8.8 |
| today used | Left today at or below 0 | The state, and left today as 0 or negative. | A negative left today rounded to 0. | 8.8, 8.9 |
| neutral, day off | Today is not a work day | A neutral day-off state; used today, the remainder and the deviation as facts. | A norm, today's share, left today, or an attention or today-used look. | 8.5; R-11; E04a |
| neutral, no remaining work days | No work day remains before the reset (`Wr = 0`) | Only the remainder `L - U`, neutral. | A norm. | 8.5; E09 |
| budget not ready | Before the first valid reading of the day, or past its reset | "Budget not ready" with the limit's facts. | A zero norm or zero left today. | 6.4 rule 3, 8.6 |
| no budget, with reason | Limit unknown or unlimited without a personal cap, zero limit, or period unknown | The reason in words. | An empty budget that reads as zero; no budget for a pool whose personal cap gives it an effective limit. | 5.4, 8.1 |

### 3.6 Budget figures

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| adaptive norm `N` | A limit with a budget, on a work day | The norm per full work day, fixed for the day. | A value that moves during the day; it changes only at local midnight, after a reset and after a cap change. | 8.4 |
| today's share `T` | As `N` | Today's allowance: `N` on a full work day, less on a partial first or last day. | `N` shown as today's allowance on a partial day. | 8.4 |
| baseline norm `B` | As `N` | The fixed reference `L / W`. | `B` presented as today's allowance. | 8.4 |
| deviation | A limit with a budget | "Ahead by" when `B x E - U` is positive, "behind by" when negative, with the amount. | A bare sign without the words. | 8.4 |
| pace mark | A limit with a budget | A mark at `B x E`, where the fixed baseline puts use at the end of today; the deviation is measured to it. | A mark on a five-hour window. | 8.4 (deviation) |
| used today | A limit with a budget | `U - U0`, never below 0 after a provider correction. | Use before the day's start counted as today's. | 8.4, 6.3 |
| left today | A limit with a budget, on a work day | `T` minus used today; it can be negative. | A negative value clamped to 0. | 8.4, 8.9 |
| used today since HH:MM | `U0` taken from the first reading after midnight | "Used today since" with the time of that reading. | Use before that time counted as today's. | 6.4 rule 2.4; P03 |
| tracked estimate | `U` of Claude extra usage and Codex credits | The tracked used amount labelled "estimate"; the provider's own used value and limit as facts beside it; a decrease of the provider counter as a fact. When the provider's own used value reaches its limit, that limit state applies even if the tracked amount is lower. | Tracked use presented as provider data. | 6.5, 8.8 |
| tracked since | Tracking that began after the period start | "Tracked since" with the date of the first reading. | Tracked use presented as complete for the period. | 6.5 |
| incomplete tracking | Tracked use after a provider counter decrease that follows a gap | An "incomplete" mark on the tracked amount for the rest of the period. | The tracked amount presented as complete. | 6.5 |

### 3.7 Estimates

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| sessions ready | A weekly window paired with a five-hour window in the same pool, with a ready estimate `C` | "≈ n sessions (estimate)", rounded down, for the weekly remainder `(100 - U) / C` and for today's share `T / C`, today's allowance rather than what is left of it. When the weekly window is exhausted, both figures are 0 sessions: this takes precedence over "< 1 session". | A decimal session count, the figure without "estimate", or a today figure above 0 on an exhausted weekly window. | 7.5; S08 |
| estimate not ready | The same pairs before the confidence rule holds | Nothing: the session figure is hidden. | 0 sessions or a placeholder number. | 7.2, 7.5; S01, S03 |
| less than one session | A session figure above 0 and below 1, while the weekly window is not exhausted | "< 1 session". | 0, which is reserved for an exhausted weekly window. | 7.5; S09 |
| no today session figure | A day off, or no remaining work day | The weekly remainder in sessions only. | A today figure. | 7.5 |

> Superseded in part by [T-048](../T-048-early-session-estimate/spec.md) (R-189): "estimate not ready" now means bounds wider than the rough level `H ≤ 2 L`, and a paired card then shows the current five-hour window as one today cell.

### 3.8 Caps

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| personal cap unset | Countable and monetary pools with a known unit or currency | An inline way to set a cap. | A cap offer on percentage windows or on amounts of unknown unit. | 5.4; R-03 |
| personal cap set | As above | The cap labelled as the user's, beside the provider limit when known. | The cap presented as the provider's limit. | 5.4; R-03 |
| binding source | A pool with a personal cap | Which binds: the lower of the cap and a known provider limit, or the provider when equal; the cap when the provider limit is unknown or unlimited. | Both presented as limits that apply. | 5.4 |
| currency mismatch | A cap in a currency other than the provider limit's | The cap flagged "currency mismatch" and not applied; the provider limit applies. | A converted amount. | 5.4 |
| unmatched cap | A cap whose limit is no longer reported | The cap listed in settings as unmatched, kept and not applied. | The cap applied to another limit or silently deleted. | 5.4 |

### 3.9 Account states

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| signed out | An account after sign-out | Hidden by default, with a way to show signed-out accounts; when shown, no current figures, history and caps kept, and sign-in available. | A confirmation step before sign-out; history removed by sign-out. | 5.4, 6.6; R-093, R-180 |
| sign-in expired | An account whose sign-in no longer works | A mark on the account and an inline sign-in action; its readings turn stale. | A modal dialog; readings presented as fresh. | Spec B-2; R-180 |
| first run | No account yet | The providers listed directly, each with one-click sign-in. | Empty bars or zero values. | R-180 |
| sign-in in progress | An account being added | Inline progress in the sign-in strip; success adds the account without a confirmation step. | A dialog or a separate window inside the app. | R-180 |
| via CLI | An account read through a local provider CLI (future, T-005) | A secondary account attribute such as "via CLI". | A label on a single limit, or a change of colour, state or figure. | 5.6 |

### 3.10 Five-hour colours (R-09)

| State | Applies to | What the user must see | What must never be shown | Research |
| --- | --- | --- | --- | --- |
| five-hour, above 30 % left | A five-hour window with more than 30 % remaining | The window without a warning colour or countdown; it ranks as OK. | A countdown. | R-09 |
| five-hour amber | 30 % or less remaining | Amber and the countdown to its reset beside the bar; ranks with attention. | Hour segments; colour as the only signal. | R-09; 8.8 |
| five-hour red | 10 % or less remaining, or exhausted | Red and the countdown; ranks with today used. An exhausted window is also at limit, which ranks higher. | Colour as the only signal. | R-09; 8.8 |

### 3.11 Account status

An account's status is its most constraining limit, in this order: over (over limit or over
cap), at limit, today used (including five-hour red), attention (including five-hour amber),
OK, neutral (including "not included" and "capped at 0"), and not ready or no budget. The
status is visible on the account without expanding anything, and it names the limit that sets
it (R-10, research 8.8). A stale reading dims the status it produces.

### 3.12 Never

- No figure, bar or colour for limits visible only in a provider's own UI: Copilot AI credits,
  Codex workspace credits and USD budgets, Claude prepaid balances, organization controls and
  cloud-session credit, and Antigravity credits (PD-034-03, R-185).
- No zero for unknown, and no unlimited without the provider's explicit flag.
- No combined percentage across limits, accounts or providers (R-15).
- No conversion between units or currencies, and no sum of different limits (R-02).

## 4. Synthetic scenario

This scenario is the "four accounts" density reference of section 5: the default window must
show all of it without scrolling. It is invented; the figures follow research section 8.

- **Moment:** Wednesday 14 October 2026, 14:20, zone Europe/London (BST, UTC+1). Work days
  Monday to Friday.
- **Accounts:** four, one per provider, with eleven limits in total.
- **Rounding:** as research 8.9. Percentages to 0.1 point, money to the cent, credits and
  requests to whole units. `N`, `T`, `B`, left today and the share of `T` left round down; the
  deviation rounds toward zero. Bar marks round to the nearest 0.1.
- **Antigravity:** the account's latest refresh failed, so both of its readings are 42 minutes
  old and stale. Group 1 keeps its budget, dimmed. Group 2 reports a window token the app does
  not recognize, so its duration is unknown: it shows "period unknown" and is not called weekly.

### 4.1 Inputs

`S` and `R` are local times. "Derived" means the provider reset minus the provider's duration;
"assumed" is labelled in the interface.

| Limit | Kind, unit | Effective limit `L` | `U0`, `U` | `S`, `R` | Other inputs |
| --- | --- | --- | --- | --- | --- |
| A1 Claude Pro, five-hour | five-hour window, percent | 100 % | -, 72 | reset Wed 14 Oct 16:05 (provider) | - |
| A2 Claude Pro, weekly | weekly window, percent | 100 % | 38, 47 | Mon 12 Oct 09:00 (derived), Mon 19 Oct 09:00 (provider) | Estimate ready, `C` = 12 |
| A3 Claude Pro, extra usage | monetary pool, USD, exponent 2 | USD 300.00 personal cap, below the USD 500.00 provider limit | USD 209.00, USD 218.00 (tracked) | Thu 1 Oct 00:00, Sun 1 Nov 00:00 (assumed) | The provider counter reads USD 218.00: the app tracked it from the start of the month without a gap. |
| B1 Codex Pro, five-hour | five-hour window, percent | 100 % | -, 91 | reset Wed 14 Oct 15:48 (provider) | - |
| B2 Codex Pro, weekly | weekly window, percent | 100 % | 96, 100 | Fri 9 Oct 09:30 (derived), Fri 16 Oct 09:30 (provider) | Estimate not ready |
| B3 Codex Pro, credits | balance-only credit pool, credits | 17,000 credits personal cap; provider limit unknown | 6,050, 6,480 (tracked) | Thu 1 Oct 00:00, Sun 1 Nov 00:00 (assumed) | Provider balance 10,160 credits. First reading of the pool Sat 3 Oct 10:12, so tracking started then. |
| C1 Copilot Free, completions | countable pool, requests | 2,000 (provider) | 1,190, 1,210 | Thu 1 Oct 01:00 (assumed, one month before the reset in UTC), Sun 1 Nov 00:00 (provider date 1 Nov, taken at 00:00 UTC) | Provider remaining 790 |
| C2 Copilot Free, chat | countable pool, requests | 50 (provider) | 11, 12 | as C1 | Provider remaining 38 |
| C3 Copilot Free, premium | countable pool, requests | 0, a provider zero entitlement | -, 0 | as C1 | - |
| D1 Antigravity Google AI Plus, group 1 | weekly window, percent | 100 % | 30, 36 | Sat 10 Oct 11:00 (derived), Sat 17 Oct 11:00 (provider) | Last valid reading 13:38; latest refresh failed |
| D2 Antigravity Google AI Plus, group 2 | percentage window, duration unknown | 100 % | -, 19 | start unknown, reset Mon 19 Oct 04:00 (provider) | Last valid reading 13:38 |

Work-day weights (research 8.3): A2 `W` 5, `Wr` 3.375, `E` 2.625; A3 and B3 `W` 22, `Wr` 13,
`E` 10; B2 `W` 5, `Wr` 2.395833, `E` 3.604167; C1 and C2 `W` 21.958333 (1 October weighs
23/24), `Wr` 13, `E` 9.958333; D1 `W` 5, `Wr` 3, `E` 3.

### 4.2 Displayed figures

| Limit | Reading | Budget figures | Other facts | State |
| --- | --- | --- | --- | --- |
| A1 | 72 % used, 28 % left | none (five-hour window) | Resets in 1 h 45 min | within; five-hour amber |
| A2 | 47 % used, 53 % left | `N` 18.3 %, `T` 18.3 %; used today 9.0 %, left today 9.3 %; `B` 20.0 %; ahead by 5.5 % | Resets Mon 19 Oct 09:00; ≈ 4 sessions left this week, ≈ 1 session today (estimate) | within; OK (51.0 % of `T` left) |
| A3 | USD 218.00 used of the USD 300.00 cap (estimate); USD 82.00 left under the cap | `N` USD 7.00, `T` USD 7.00; used today USD 9.00, left today -USD 2.00; `B` USD 13.63; behind by USD 81.63 | Cap binds; provider limit USD 500.00; period 1 Oct to 1 Nov, assumed | within; today used |
| B1 | 91 % used, 9 % left | none (five-hour window) | Resets in 1 h 28 min | within; five-hour red |
| B2 | 100 % used, exhausted | Shown subordinate to the exhausted state, which replaces the budget state: `N` 1.6 %, `T` 1.6 %; used today 4.0 %, left today -2.4 %; `B` 20.0 %; behind by 27.9 % | Back Fri 16 Oct 09:30, in 1 d 19 h 10 min; sessions hidden (estimate not ready) | at limit (exhausted) |
| B3 | 6,480 credits used of the 17,000-credit cap (estimate, tracked since 3 Oct); 10,520 left under the cap | `N` 842, `T` 842; used today 430, left today 412; `B` 772; ahead by 1,247 | Provider balance 10,160 credits; cap binds, provider limit unknown; period 1 Oct to 1 Nov, assumed | within; OK (48.9 % of `T` left) |
| C1 | 1,210 of 2,000 requests used | `N` 62, `T` 62; used today 20, left today 42; `B` 91; behind by 302 | 790 remaining (provider); resets 1 Nov; start assumed | within; OK (67.9 % of `T` left) |
| C2 | 12 of 50 requests used | `N` 3, `T` 3; used today 1, left today 2; `B` 2; ahead by 10 | 38 remaining (provider); resets 1 Nov; start assumed | within; OK (66.6 % of `T` left) |
| C3 | Not included | none | - | not included (neutral) |
| D1 | 36 % used, 64 % left, dimmed, as of 13:38 | `N` 23.3 %, `T` 23.3 %; used today 6.0 %, left today 17.3 %, dimmed; `B` 20.0 %; ahead by 24.0 %, dimmed | Resets Sat 17 Oct 11:00 | within; OK, dimmed (74.2 % of `T` left) |
| D2 | 19 % used, 81 % left, dimmed, as of 13:38 | none: period unknown | Resets Mon 19 Oct 04:00 | no budget, period unknown |

### 4.3 Bar marks

Positions as a percentage of `L`, for the signature visualization of section 6. Today's
budget runs from `U0` to `U0 + T`; the pace mark is at `B x E`.

| Limit | `U0` | `U` | `U0 + T` | Pace mark |
| --- | --- | --- | --- | --- |
| A2 | 38.0 | 47.0 | 56.4 | 52.5 |
| A3 | 69.7 | 72.7 | 72.0 | 45.5 |
| B2 | 96.0 | 100.0 | 97.7 | 72.1 |
| B3 | 35.6 | 38.1 | 40.5 | 45.5 |
| C1 | 59.5 | 60.5 | 62.6 | 45.4 |
| C2 | 22.0 | 24.0 | 28.0 | 45.4 |
| D1 | 30.0 | 36.0 | 53.3 | 60.0 |

### 4.4 Account status

| Account | Status | Set by |
| --- | --- | --- |
| Claude Pro | today used | A3. The amber five-hour window and the OK weekly window do not hide it (R-10). |
| Codex Pro | at limit | B2, which outranks the red five-hour window. |
| Copilot Free | OK | C1 and C2; C3 is neutral and does not raise the status. |
| Antigravity Google AI Plus | OK, dimmed, with the refresh-failed mark | D1; D2 has no budget. |

## 5. Information architecture (B-3)

### 5.1 One window

- **Single window (R-01).** Every provider, account and limit of section 3, including
  model-scoped weekly windows and credit and monetary pools, is in the one main window. There
  is no account detail view, modal dialog or pop-up window.
- **Hierarchy.** Provider, then account, then limits. The original scenario has one
  account per provider; T-039 renders multiple accounts under the same provider
  under the owner delivery amendment above. Names, status and actions belong to
  each account, not to its provider.
- **Account line.** Each account shows its name, its status with the limit that sets it
  (section 3.11), its reading marks (stale, refresh failed, sign-in expired) and an icon-only
  sign-out. The status is readable without expanding the account.
- **Limit rows.** Each limit shows its reading and, when it has one, its budget: left today,
  the budget state and the deviation are visible on the row. A limit at or over its limit
  shows that limit state in place of its budget state (research 8.8); its budget figures and
  bar marks stay, subordinate to it (B2). The limit order stays the same between refreshes; a
  change of state never reorders rows.
- **History (R-14).** A limit's history expands inline under its row. It is built from the
  app's local reading series only (R-184), covers at least 35 days, and shows gaps as gaps.
  Provider-supplied history (T-011) is not shown.

### 5.2 Editing and confirmation

- **Inline rename.** The account name is edited in place; Enter saves and Escape cancels.
- **Inline cap editing.** A personal cap is set, changed or removed on the pool's row and in the
  settings panel, in the pool's unit or currency.
- **Confirmation in place (R-12).** An action that needs confirmation, such as Delete stored
  data, turns its control into "Confirm · Cancel" in place. There is no dialog.
- **Undo.** A reversible action, such as removing a cap or changing work days, offers undo.
- **Sign-out.** Sign-out is immediate, without confirmation, and keeps history, labels, order
  and caps (R-093).

### 5.3 Settings, sign-in and the tray

- **Settings (R-13).** Settings open as an inline panel in the same window, not a replacement
  view. They contain the work days (Monday to Friday by default), the personal caps including
  unmatched caps, and the existing sections with System status last (R-180).
- **Sign-in strip.** Adding an account uses the provider menu (R-180): one provider click starts
  browser sign-in, progress stays inline in a strip, and success adds the account.
- **Tray flyout.** The tray flyout, currently 360 effective pixels wide, stays a secondary
  surface. It summarizes each account's status; no information exists only in the flyout.

### 5.4 Density and disclosure

- **Tooltips.** No information needed for a decision lives only in a tooltip. Every figure that
  answers a section 2 question is visible, or one keyboard step away on the focused row.
  Tooltips may repeat visible information.
- **Fit.** The section 4 scenario, four accounts with eleven limits, fits the default window at
  100 % scaling without scrolling. The default window is 760 × 600 effective pixels, the current
  main window default size. A direction may propose another default size with its reason; that
  size must fit the work area of a 1920 × 1080 display at 150 % scaling, 1280 × 720 effective
  pixels less the taskbar (section 9). When the owner selects a direction, its accepted size is
  the default window for the prototype and for every later check (DA-6, PA-10, section 9).

### 5.5 Replaced behaviour

The redesign replaces these parts of R-180 and R-181, through the follow-up implementation
items only; until they land, the current behaviour stays (R-183):

- Account detail and history pages opened from the account panel with Back (R-180): replaced
  by inline limit rows and inline history.
- Settings replacing the usage view (R-180): replaced by the inline settings panel.
- Confirmation outside the control (R-180, Delete stored data): replaced by "Confirm · Cancel"
  in place.
- Readings only in bar hover text and accessible names (R-181): replaced by visible figures.
- One status mark per account for a failed refresh, a stale reading or pace attention (R-181):
  replaced by the account status of section 3.11 plus the reading marks.
- The 20 % red floor for windows shorter than a day (R-181): replaced by the R-09 amber and red
  thresholds.
- Even local-calendar-day shares with carry-over (R-181): replaced by the work-day adaptive norm
  with its baseline and deviation.

Kept from R-180 and R-182: no navigation tabs, the Settings icon, the provider menu with
one-click sign-in, first run that lists providers, immediate icon-only sign-out, and one dark
appearance.

## 6. Visual identity (B-4)

The design has its own identity and does not reuse Claude Design's default theme and colours
(R-08). The three lists below are copied verbatim from the specification. Sections 6.1 to 6.4
state what a direction delivers for each required item, section 9 states the platform rules
behind the kept items, and section 8 checks every item by name.

**Forbidden:**

- a near-black slate or zinc background with a single indigo, violet or blue accent;
- purple-to-blue gradients and glassmorphism;
- a grid of identical rounded cards with soft shadows;
- Inter or a system font as the only typographic idea;
- KPI tiles, pill badges everywhere, and decorative emoji or icons.

**Required:**

- a named design concept with its rationale;
- a palette derived from that concept, with semantic state colors (ok, attention,
  critical, stale, estimate, assumed) meeting WCAG AA contrast on the dark background;
- a distinctive type pairing with tabular numerals;
- a signature limit visualization that combines the bar, today's budget, the pace mark and
  five-hour sessions.

**Kept:**

- dark-only (R-182);
- status never relies on color alone;
- everything implementable in WinUI 3 on Windows 11;
- fonts licensed for embedding in an MSIX package.

### 6.1 Design concept

Each direction has a name and a concept of a few sentences: the one idea the whole interface
follows, and why it suits the user of section 2, who looks for a few seconds many times a day
and wants an answer without reading. The palette, the type pairing and the signature
visualization each state how they follow from the concept. A name alone, or a mood with no
consequence for form, is not a concept.

### 6.2 Palette

Each direction delivers its palette as named colour tokens with hex values, derived from its
concept:

- the window background and every surface, all dark (R-182) and solid (section 9);
- the text levels, including the dimmed level of stale readings;
- the neutral colours of the bar track and of the neutral states, which never use ok;
- the six semantic state colours, each tied to the section 3 states it marks:

| Colour | Section 3 states it marks |
| --- | --- |
| ok | OK. |
| attention | attention; five-hour amber. Attention is the amber of R-09. |
| critical | today used; five-hour red; at limit (exhausted); over limit; over cap. Critical is the red of R-09. |
| stale | stale (dimmed): the dimmed reading, every figure computed from it, and its time. |
| estimate | tracked estimate; tracked since; incomplete tracking; sessions ready; less than one session. |
| assumed | assumed reset; assumed start. |
| neutral, not one of the six | neutral, day off; neutral, no remaining work days; not included; capped at 0; budget not ready; no budget, with reason; unknown; limit unknown; period unknown; secondary amount, unit unknown. |

A value can carry a state and a label at once: A3 is today used and an estimate, and B3 is OK,
an estimate and tracked since. The direction shows how the estimate and assumed marks combine
with a state colour without replacing it.

Each direction also delivers a contrast table, computed with the WCAG 2.2 relative luminance
and truncated to two decimals, so that 4.49:1 fails 4.5:1:

- every text level, and every semantic colour used as text: at least 4.5:1 against each
  background or surface it appears on;
- every semantic and neutral colour used as a non-text mark, such as a bar fill, today's
  budget, the pace mark or a focus indicator: at least 3:1 against every colour it touches,
  the bar track included;
- the dimmed level of stale readings meets the same ratios: stale is dimmed, never illegible.

### 6.3 Type pairing

Each direction delivers a distinctive type pairing, two families with separate roles such as
figures and labels, and its type ramp: sizes, weights and line heights in effective pixels.

- **Tabular numerals.** Every figure uses tabular numerals: percentages, amounts, counts,
  times and countdowns, so that figures align in columns and keep their width when they
  change. The figure font supports OpenType tabular figures.
- **Not a system font alone.** Inter or a system font such as Segoe UI Variable may be one
  half of the pairing, never the only typographic idea.
- **Licence.** For each font packaged with the app, the direction names its licence and the
  source of its files (section 9).
- **Size.** No text is smaller than 12 effective pixels, the Windows 11 minimum for regular
  text.
- **Scripts.** Interface text is English. Account names are user text: the direction states
  which scripts its fonts cover, and a name outside them falls back to the Windows system font.

### 6.4 Signature limit visualization

Each direction delivers one visualization used on every limit row. It combines:

- **the bar:** used against the effective limit `L`, at the positions of section 4.3;
- **today's budget:** the stretch from `U0` to `U0 + T`, so that used today and left today
  read from the bar, and use past `U0 + T` visibly overruns it (A3 and B2);
- **the pace mark:** at `B x E`, with the deviation in words, "ahead by" or "behind by";
- **five-hour sessions:** "≈ n sessions (estimate)" for the weekly remainder and for today,
  where the estimate is ready, and 0 sessions for both on an exhausted weekly window
  (section 3.7).

Beside it, the row keeps left today, the budget state and the deviation visible (section 5.1).
Graphic session marks, if a direction draws any, follow the section 3.7 states of the session
figure: hidden when the estimate is not ready, and marked as an estimate.

The visualization is shown for every limit kind of the section 4 scenario:

| Scenario limits | Kind and state | Draws | Never draws |
| --- | --- | --- | --- |
| A2 | weekly window; OK; sessions ready | The bar, today's budget, the pace mark with "ahead by 5.5 %", and ≈ 4 sessions this week and ≈ 1 session today, labelled as an estimate. | A decimal session count. |
| D1 | weekly window; OK; stale (dimmed) | The used fill dimmed with "as of 13:38"; today's budget and the pace mark, fixed for the day like `N` and `T`, at full emphasis. | The stale reading at full emphasis; a session figure, because the scenario gives it no paired five-hour window. |
| B2 | weekly window; at limit (exhausted); estimate not ready | A full bar, "exhausted" and "back Fri 16 Oct 09:30", with the section 4.3 budget marks and the section 4.2 budget figures, subordinate to the exhausted state. | An OK or attention look; the budget state in place of the exhausted state; a session figure or a placeholder for one. |
| A1, B1 | five-hour windows; five-hour amber and five-hour red | The used bar in the R-09 colour, with the countdown beside it: 1 h 45 min and 1 h 28 min. | Today's budget, a pace mark, a session figure or hour segments. |
| A3 | monetary pool; today used; tracked estimate | The bar on the USD 300.00 cap labelled as the user's, today's budget overrun, the pace mark with "behind by USD 81.63", "estimate", the USD 500.00 provider limit as a fact, and the assumed period. | The cap as the provider's limit; a converted amount or more precision than cents. |
| B3 | balance-only credit pool; OK; tracked estimate, tracked since | The bar on the 17,000-credit cap labelled as the user's, today's budget, the pace mark with "ahead by 1,247", "estimate" and "tracked since 3 Oct", and the provider balance of 10,160 credits as a fact beside it. | The balance as used or as an allotment. |
| C1, C2 | countable pools; OK | The bar in requests, today's budget, the pace mark, the provider's remaining as a fact, and the assumed start. | A percentage of another limit, or money. |
| C3 | not included | The words "not included", neutral. | A bar or a zero budget. |
| D2 | percentage window; period unknown; stale (dimmed) | The used bar at 19 %, dimmed, with "as of 13:38", "period unknown" and the reset Mon 19 Oct 04:00. | Today's budget, a pace mark, or a name such as "weekly". |

## 7. Deliverables (B-5)

### 7.1 Directions first

Claude Design first delivers two or three distinct directions, and the owner selects one.
Directions are distinct when each has its own concept, palette, type pairing and signature
visualization; colour variants of one layout count as one direction. Each direction contains:

1. its name, concept and rationale (section 6.1);
2. its palette tokens and contrast table (section 6.2);
3. its type pairing, its type ramp, and the licence and source of each packaged font
   (section 6.3);
4. its signature visualization for every row of the section 6.4 table;
5. the main window with the whole section 4 scenario at 760 × 600 effective pixels and 100 %
   scaling, without scrolling, or at the direction's own default size with its reason
   (section 5.4);
6. for each forbidden item of section 6, one sentence on how the direction avoids it.

Section 8.1 decides whether a direction can be selected. Nothing else is needed at this stage.

### 7.2 Full prototype of the selected direction

After the owner selects a direction, Claude Design delivers its full prototype:

- **Main window in every state.** The section 4 scenario, and a state list that names every
  section 3 state by its identifier and shows it in place: in the scenario where the scenario
  has it, otherwise with other invented values. The list includes two combinations the
  scenario lacks: an unlimited countable pool with a personal cap, which has its budget on the
  cap, the cap as binding source and "unlimited" as the provider's fact; and an exhausted
  weekly window with a ready estimate, which shows 0 sessions for the week and for today.
- **Inline history.** The expanded history of one limit over at least 35 days, with a gap
  (section 5.1).
- **Tray flyout.** The secondary flyout, currently 360 effective pixels wide, with each
  account's status (section 5.3).
- **Settings panel.** The inline panel with the work days, the personal caps including an
  unmatched cap and a currency mismatch, and the existing sections with System status last.
- **Inline editing, confirmation and undo.** Rename with Enter and Escape; setting, changing
  and removing a cap on its row; "Confirm · Cancel" in place for Delete stored data; undo
  after removing a cap and after changing the work days.
- **First run.** The providers listed directly, each with one-click sign-in.
- **Sign-in strip.** Sign-in in progress and its success; sign-in expired with its inline
  sign-in action; sign-out, and showing signed-out accounts.
- **Scaling.** The main window at 100 % and 150 % display scaling.
- **Token specification.** Colour tokens with hex values and roles; the type ramp; spacing,
  sizes, corner radii and stroke widths in effective pixels; motion durations and easing.
- **Component specification.** The account line, the limit row, the signature visualization
  with each of its marks, the history expansion, the inline editors, "Confirm · Cancel",
  undo, the settings panel, the sign-in strip and the tray flyout row, each with its states
  and its sizes in effective pixels.
- **Keyboard, accessible-name and reduced-motion notes.** The focus order and a visible focus
  indicator; the key for every action; the accessible name of every control and every figure
  with its state words, for example "Codex Pro weekly, exhausted, back Friday 09:30"; and what
  replaces each motion when Windows animation effects are off.

### 7.3 Form

The directions and the prototype are Claude Design project files, as in the T-010 design
reference: a clickable prototype, a design specification and their support files. They use
synthetic data only and contain no screenshot or capture of a real account. They are a design
reference: the app recreates them in native WinUI 3 XAML (section 9).

## 8. Acceptance rubric (B-6)

The owner uses two pass/fail checklists: section 8.1 to accept or reject a direction, and
section 8.2 to accept or reject the final prototype. A direction or the prototype is accepted
only when every row passes, or when the owner accepts a listed exception by its row. A failed
row names the element that fails it. The rows use the section 3 state names unchanged.

### 8.1 Direction acceptance

| Row | Criterion | Passes when |
| --- | --- | --- |
| DA-1 | Concept and rationale | The direction has a name and a concept that answers section 2, and its palette, type pairing and signature visualization each state how they follow from it (section 6.1). |
| DA-2 | No forbidden element, each named | Each item is checked and absent: (a) a near-black slate or zinc background with a single indigo, violet or blue accent; (b) a purple-to-blue gradient; (c) glassmorphism; (d) a grid of identical rounded cards with soft shadows; (e) Inter or a system font as the only typographic idea; (f) KPI tiles; (g) pill badges everywhere; (h) decorative emoji or icons; (i) Claude Design's default theme and colours (R-08). |
| DA-3 | Palette contrast | The section 6.2 table gives every text level and all six semantic colours: text at least 4.5:1, non-text marks at least 3:1 against every colour they touch, the dimmed stale level included. Attention is amber, critical is red, and neutral states do not use ok. |
| DA-4 | Type pairing and font licence | Two families with separate roles; tabular numerals for every figure; no text below 12 effective pixels; each packaged font names a licence that permits embedding and redistribution in an application, and the source of its files; a system font is at most one half of the pairing (section 6.3). |
| DA-5 | Signature visualization | One visualization combines the bar, today's budget, the pace mark and five-hour sessions. It is shown for every row of the section 6.4 table, with everything in its "Draws" column and nothing in its "Never draws" column, and its marks sit at the section 4.3 positions. |
| DA-6 | Four accounts fit | The whole section 4 scenario, four accounts with eleven limits, fits the default window of section 5.4 at 100 % scaling without scrolling: 760 × 600 effective pixels, or the direction's stated default size with its reason, which also fits the section 9 work area at 150 %. Each account's status is visible without expanding anything; left today, the displayed state and the deviation are visible on each row with a budget; and the text meets DA-3 and DA-4 at that density. |
| DA-7 | Buildable in WinUI 3 | Every effect is available in WinUI 3 XAML or composition, the background is solid, nothing needs a WebView, and colours come from the direction's own tokens (section 9). |

### 8.2 Prototype acceptance

| Row | Criterion | Passes when |
| --- | --- | --- |
| PA-1 | Every section 3 state visible | The state list names every section 3 state by its identifier and shows each one in place, with the two combinations section 7.2 names (section 7.2). |
| PA-2 | No tooltip-only decision information | Every figure that answers a section 2 question is visible, or one keyboard step away on the focused row; tooltips only repeat visible information; no reading lives only in hover text (section 5.4). |
| PA-3 | Figures match section 4 | Every value of section 4.2, every bar position of section 4.3 to 0.1 point, and every account status of section 4.4 is shown as written, with the same rounding. B2's budget figures and marks are included, subordinate to its exhausted state (section 6.4). |
| PA-4 | R-09 colours | A1 is amber with "1 h 45 min" and B1 red with "1 h 28 min" beside the bar; a five-hour window above 30 % left has no countdown; no five-hour window is split into hours; the colour never stands alone. |
| PA-5 | No figures without data | No figure, bar or colour for the provider-UI-only limits of section 3.12; unknown shown by an unknown mark, never as 0 or unlimited; a not-ready estimate hidden; each no-budget state gives its reason in words, never an empty budget; not included and capped at 0 without a bar. |
| PA-6 | Labels and truthfulness | "estimate", "assumed", "tracked since", "incomplete" and "period unknown" appear wherever section 3 requires them; stale readings are dimmed with their time; there is no combined percentage, no sum of different limits and no conversion between units or currencies (section 3.12). |
| PA-7 | Identity kept | The prototype uses the selected direction's concept, tokens and type pairing, and DA-2 passes again on every screen. |
| PA-8 | Contrast and colour alone | On the final tokens, text is at least 4.5:1 and non-text marks at least 3:1, the dimmed stale level included; every coloured state also has a word or a shape. |
| PA-9 | Keyboard and screen-reader notes | The focus order, a visible focus indicator, a key for every action, the accessible name of every control and every figure with its state words, and the reduced-motion behaviour are specified (section 7.2). |
| PA-10 | Layouts at 100 % and 150 % | The section 4 scenario fits the default window accepted with the direction (section 5.4) without scrolling at 100 % and at 150 % display scaling, thin marks stay distinguishable at 150 %, and at 200 % the content scrolls vertically with nothing clipped (section 9). |
| PA-11 | Surfaces complete | Inline history with a gap, the tray flyout, the settings panel with work days, caps, an unmatched cap and a currency mismatch, inline rename and cap editing, "Confirm · Cancel", undo, first run and the sign-in strip are delivered, with no modal dialog, pop-up window or account detail view (R-01). |
| PA-12 | Buildable in WinUI 3 | Every effect and component maps to WinUI 3 XAML or composition, each packaged font is a .ttf or .otf file with a named licence, and nothing needs a WebView (section 9). |
| PA-13 | Token and component specifications | The tokens and components of section 7.2 are complete, with values and sizes in effective pixels that an implementer can transfer to XAML without measuring the prototype. |

## 9. Platform constraints

The shipped interface is native. These limits decide what a direction may propose.

- **Target.** A WinUI 3 desktop app on Windows 11, packaged as MSIX. Views are XAML; the
  Claude Design files are recreated in XAML and never shipped (section 7.3).
- **No WebView.** No part of the interface is HTML in a WebView.
- **Dark-only (R-182).** One dark appearance in the main window, the tray flyout and the title
  bar. It stays the same when Windows is in light app mode or a contrast theme is on; no light
  or high-contrast variant is designed. Colours come from the direction's own tokens, not from
  the Windows accent colour or the system theme brushes.
- **Solid background.** Backgrounds and surfaces are solid colours, without Mica or Acrylic,
  so that every contrast ratio of section 6.2 holds against a known colour.
- **Effects.** Only what WinUI 3 XAML and the Windows composition layer render: solid and
  gradient brushes, shapes and paths, opacity, clipping, theme shadows, and XAML or
  composition animations. CSS-only effects such as backdrop blur, blend modes, filters,
  masks, gradient text and scroll-driven animation are not used unless the direction names
  their XAML or composition equivalent.
- **Fonts.** Each font that is not part of Windows is packaged in the MSIX as a .ttf or .otf
  file, not WOFF or WOFF2, under a licence that permits embedding and redistribution in an
  application, for example the SIL Open Font License 1.1 or the Apache License 2.0. The
  direction names the licence for each font, and the licence is checked against its official
  page before import. A Windows 11 system font such as Segoe UI Variable is used from Windows
  and not packaged.
- **Tabular numerals.** XAML sets them with `Typography.NumeralAlignment="Tabular"`, so the
  figure font must provide tabular figures.
- **Keyboard.** Every action is reachable and operable by keyboard, with a visible focus
  indicator of at least 3:1 against the colours next to it.
- **Accessible names.** Every control and every figure has an accessible name for UI
  Automation and Narrator that includes its state words (section 7.2).
- **Reduced motion.** When Windows animation effects are off, state changes are instant, and
  no information is carried by motion alone.
- **Display scaling.** Layouts are in effective pixels and are checked at 100 %, 150 % and
  200 %. The section 4 scenario fits the default window of section 5.4 at 100 %: 760 × 600,
  unless the owner accepted another size with the selected direction. At 150 % the same window
  and layout fit a 1920 × 1080 display, which offers 1280 × 720 effective pixels less the
  taskbar, and thin marks stay distinguishable at 1.5 physical pixels per effective pixel. At
  200 % such a display offers 960 × 540 effective pixels less the taskbar, which can be smaller
  than the default window: the window then fits the work area and its content scrolls
  vertically, with nothing clipped.
- **Text size.** The fit applies at the Windows text size of 100 %. At a larger text size the
  content scrolls, and no figure is truncated.
- **Status never by colour alone.** Every coloured state also has a word, and marks differ in
  shape or position, not only in hue.

## 10. Sources

- [spec.md](spec.md): R-01 to R-15, B-1 to B-6 and Gate B.
- [research.md](research.md): sections 5 (limit model and personal cap), 6 (local reading
  series and tracked consumption), 7 (five-hour session estimator), 8 (budget rules, states and
  rounding), 9 (worked examples) and 11 (owner decisions PD-034-01 to PD-034-03).
- R-180 to R-185 in [accepted decisions](../../decisions/accepted.md): the single-window shell,
  the compact view and pace colours, the dark-only appearance, the redesign direction,
  local-only history, and the limit model accepted at Gate A.
- Current window sizes: `MainWindow.xaml.cs` (`DefaultWidth` 760, `DefaultHeight` 600) and
  `Features/Tray/TrayPopupWindow.xaml.cs` (`PopupWidth` 360) in `src/windows/AiUsage.Windows`.
- Import precedent: the [T-010 design reference](../T-010-ui-ux/design-reference/README.md).
- Licences named in section 9: the
  [SIL Open Font License 1.1, official text](https://openfontlicense.org/open-font-license-official-text/)
  and the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
- WCAG 2.2: [1.4.1 Use of Color](https://www.w3.org/TR/WCAG22/#use-of-color),
  [1.4.3 Contrast (Minimum)](https://www.w3.org/TR/WCAG22/#contrast-minimum) and
  [1.4.11 Non-text Contrast](https://www.w3.org/TR/WCAG22/#non-text-contrast).
- Windows: [Typography in Windows](https://learn.microsoft.com/windows/apps/design/signature-experiences/typography)
  (type ramp and the 12-pixel minimum) and
  [Typography.NumeralAlignment](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.documents.typography.numeralalignment).
