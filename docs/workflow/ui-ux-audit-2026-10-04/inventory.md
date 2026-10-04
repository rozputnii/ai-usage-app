# Supported behavior inventory

Audit base: `383644c`. Scope is the owner's ordinary Windows desktop use, exclusively synthetic data. This is an implementation inventory, not a new provider capability or a live-provider verification. [Coverage](coverage.csv) contains parser-tested numeric scenarios; [additional scenarios](additional-scenarios.csv) records transitions and combinations outside that corpus. [Controls](controls.csv) separates command tests from physical clicks. All native results currently remain BLOCKED by the locked desktop.

## Sources inspected

AGENTS.md, CONTRIBUTING.md, constitution, current verification/document formats; provider records for Claude, Codex, Copilot and Antigravity; AIU-034 research/design brief, AIU-035 calculations, AIU-036 readings, AIU-037 normalized facts, AIU-038 presentation, AIU-039 live wiring, and AIU-044 owner corrections. Executable contracts take precedence over historical statements about unimplemented features. In particular, CX-I is now parsed; AIU-039's historical monetary suppression is superseded by AIU-044.

Code: four quota parsers, QuotaLimitMapping, LimitFacts/Quantity, EffectiveLimit, PeriodResolver, WorkCalendar, BudgetEngine, ReadingCalculations, SessionEstimator, ExtraUsageEvidence, local stores/recorder, LiveLedgerSource/Projection, Ledger contract, formatting/visuals/view models, all three Ledger XAML views, card/history/bar controls, native tray, App composition/lifetime, and existing deterministic and Windows test suites.

## Provider families and account shapes

| Provider | Implemented families | Supported shapes and provenance |
| --- | --- | --- |
| Claude | CL-S session, CL-W shared week, CL-M model-scoped week, CL-unknown opaque kind, CL-X monetary pool | Legacy five_hour/seven_day/Opus/Sonnet and modern session/weekly_all/weekly_scoped. Modern matching scope replaces legacy, not a second entitlement. Exact model names stay opaque. `is_active` ranks severity, not entitlement. Modern spend supersedes legacy extra_usage; both normalize to CL-X. Currency/exponent must be known and compatible. No console credit balance or API billing family. |
| Codex | CX-P primary, CX-S secondary, CX-A additional group, CX-I individual control, CX-B balance | Durations are supplied seconds, including 5h/7d/other periods; absent duration stays unknown. Additional feature/name/model values are opaque, not plan inference. Individual percentages form a monthly window; raw amount strings retain unknown units. Signed credit balances retain native credit units. Any percentage subscription window suppresses the standalone CX-B card while retaining its facts/history. |
| GitHub Copilot | GH-P premium requests, GH-C chat, GH-I completions, GH-unknown future pool | Monthly counts; percentages and overage fields are independent facts. Entitlement states unknown/null/zero/finite/unlimited remain distinct. Date-only reset remains date-only, with assumed start. Unlimited counts can use a personal cap. Unknown pools do not gain a request unit. No invented dollar pool or administrator budget. |
| Antigravity | AG-5, AG-W, AG-unknown | Grouped/default buckets; exact provider group/bucket names and membership. Same-group 5h/week pairs; separate Gemini Models and Claude + GPT Models examples do not split shared counters into model accounts. Disabled buckets are dropped; all-disabled/empty readings produce no displayed limits. remainingAmount stays secondary with unknown unit. No inferred monthly credits, money conversion or legacy endpoint fallback. |

All four support separate opaque account references, repeated provider accounts, independent health/history/caps and mixed dashboards. Editable names never establish subscription types. Corpus labels are `SYNTHETIC · <scenario ID>`; Antigravity's plan is `synthetic-tier`, not a claimed paid plan.

Monetary-only accounts use monthly and today budgets, calendar fallback ending at local midnight on next month's first day. Subscription-plus-money accounts attach monetary content under the owning account, with explicit restrictions. A known shared monetary scope cannot become a personal allowance. Current provider wire mappings do not establish that scope; direct projection tests cover the restriction, and a presentation fixture would be needed for its native gallery case.

## Layout, scale and state applicability

All five layouts are represented in the corpus: FiveHourAndPeriod, Period, Pool, UsedOnly, Note. Scales are percentage, count/abstract credits, and native money. Used/Left changes labels, solid/hatch fill, strip order, bar direction, cap/over ticks and tooltips; it does not change facts or budget values.

| State | Representative corpus IDs | Applicability |
| --- | --- | --- |
| OnTrack | P-claude-ordinary, C-ordinary, M-ordinary | All supported budget scales. |
| TodayLow | P-codex-low-above | Percentage daily allowance below 30% remaining. |
| TodayShort | C-low-above, M-low-above | Count/money daily allowance below 30%; percent live projection uses TodayLow. |
| CapClose | C-cap-close, M-cap-close | Compatible personal cap binds and daily share is short. |
| FiveHourFull | W-claude-100, W-codex-100, W-antigravity-100 | Reported 100% short window with future known reset; except precedence states below. |
| TodayUsed | P-claude-today-at, C-today-at, M-today-at | Exactly exhausted daily allowance, distinct from provider exhaustion. |
| OverToday | P-claude-today-above, C-today-above, M-today-above | Daily allowance exceeded with provider remainder. |
| CapReached | CC-180-180, MC-180-180 | Compatible binding cap exactly reached. |
| OverCap | CC-180-181, MC-180-180.01 | Compatible binding cap exceeded. |
| UsedUp | P-codex-provider-at, C-provider-at, M-provider-at, M-provider-above | Provider limit exhausted; monetary spending may exceed it. |
| DayOff | D-off, CAL-last, CAL-short-last | Configured day off; neutral/dashed would-be allowance. |
| Rush | D-rush | Last provider-period work day, no cap, not money/credits, no provider exhaustion. |
| NotReady | R-at, R-after | Retained expired longer-period reading; no current daily budget. |
| ValueUnknown | P-claude-unknown, F-CX-I-unknown, F-AG-amount-only | Null/missing/out-of-range source percentage remains unknown. |
| PeriodUnknown | P-period-unknown, F-AG-unknown-window | No supported period; money unavailable variants say no budget. |
| NotIncluded | C-zero-limit, F-CX-disabled, M-disabled | Explicit denied/zero/disabled. Money says zero limit/disabled, not a fabricated plan exclusion. |
| LimitUnknown | C-unknown-pool, M-no-limit | Native facts retained; compatible cap may make a budget. |
| NoCap | B-no-cap, B-unknown, C-unlimited | Abstract balance/unlimited pool without a binding personal cap. |
| SignedOut | H-SignedOut | Retained account, no current figures; hidden by default and reconnect available. |
| NoDisplayedLimits | E-claude, E-codex, E-copilot, E-antigravity, F-AG-disabled | Successful empty normalized reading, distinct from startup/not ready. |

Health: Ok, SyncFailedFresh, SyncFailedStale, SignInExpired, SignedOut and ProviderError. All six have corpus inputs (H-*). Main card state and account health are independent; provider errors are also communicated in settings/tray. Marks: OnExtraUsage, ExtraDay, SyncFailed, SignInExpired, PastReset. Sync/auth/past-reset examples are in the corpus; extra-day/extra-spend have production projection regressions but still need native gallery fixtures/actions.

Reset representations: provider timestamp, provider date-only, unknown, and assumed calendar month; derived/assumed start remains separate. Freshness threshold is 15 minutes. Authentication strip phases: Waiting, Succeeded, Failed, Cancelled; reasons Duplicate, WrongAccount, Storage, AccessDenied, Expired, Browser, Registration, Provider. Manual callback code is Claude-only in the presentation contract; other providers use mocked completion.

## Boundaries and precedence

The corpus independently asserts 0.1-percentage, whole-request and native-cent boundaries around 30% remaining daily allowance, exact daily exhaustion, binding cap and provider exhaustion. Exact 30% is OnTrack; below it is attention. Provider exhaustion wins over a personal cap; a cap equal to or above the provider limit does not replace the provider binding. UsedUp precedes day off/rush; a binding cap reached/exceeded precedes day off. FiveHourFull does not replace UsedUp, NotReady, DayOff or ValueUnknown. Failed refresh/staleness/authentication retain usage rather than inventing zero.

Finite numeric inputs are not an infinite exhaustive domain. Separate threshold triples and distinct combinations are required. Remaining missing triples/combinations are explicitly listed in additional-scenarios.csv; the current corpus must not be described as exhaustive.

Invalid/unreachable/non-applicable combinations:

- Percent outside 0..100 is unknown, not provider overage. Antigravity fraction outside 0..1 is unknown. No valid over-100 percentage gallery is promised.
- Copilot negative remaining or remaining greater than entitlement yields unknown used; a valid count payload cannot represent used greater than entitlement. Monetary readings can exceed their finite provider limit.
- Live percentage cards do not expose a personal-cap editor. Percent TodayShort/cap states are not live scale combinations; hand-authored contract examples do not prove parser correctness.
- FiveHour.CurrentWindowStarted is derived from used > 0. No currently parsed field establishes “started at zero”; zero represents not started, and the distinction cannot be manufactured.
- Pairing requires exact supported 5h/7d identity sharing. Different opaque model groups, other durations and unknown durations cannot pair merely because both are present.
- Rush is deliberately unavailable for monetary pools, credits, any personal cap, and exhausted provider limits.
- An unknown currency/exponent or explicit incompatible currency cannot yield a converted budget. Abstract credits never imply USD or any other denomination.
- The main window has no visible per-account Refresh button and no main global Refresh button. F5 and the native tray Refresh are supported; ILedgerSource.RefreshAccountAsync alone is not a clickable control.
- Rename save/cancel and cap cancel are Enter/Escape interactions; there are no separate visible rename Save/Cancel or cap Cancel buttons. History closes by its toggle or Escape, not a dedicated close button.

## Remaining execution scope

Native window resizing/scrolling, hover tooltips, overlays, settings alongside cards, editors/history, first-run/add/reconnect/authentication, recovery, isolated delete/export, restart persistence and native tray/lifetime are listed separately. Existing tests using Invoke or direct commands are supporting regressions only, never physical hit-test passes. Package builds do not establish installation/update, and neither is authorized for the owner's installed application here.
