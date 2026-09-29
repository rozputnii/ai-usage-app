# AIU-034 Phase A research

## 1. Scope and evidence levels

Subscription-attached limits only: Claude Pro, Max, Team, Enterprise; Codex on ChatGPT
Plus, Pro, Business, Enterprise; Copilot Free, Pro, Pro+, Business; Antigravity Free,
Pro, Ultra, plus Google AI Plus as the owner's 2026-09-26 LC-22 addition.
API-key billing is excluded. This is research, not an implementation.

Owner clarification, 2026-09-29: the observed Antigravity account is an ordinary personal
Google AI Plus subscription, not a work/API subscription. Do not implement Antigravity
API-credit billing. The owner ended the Google One credit lookup; retain its unresolved
fields as unknown, with no credit-integration requirement inferred for T-07 or implementation.
Google One product credits and API-key billing are distinct; neither is proved by weekly quotas.

- `source`: inspected repository/upstream code or official documentation. It proves the
  stated contract or product description, not access on any particular account.
- `live`: a specifically authorized observation, limited to the observed plan and surface.
- `none`: no evidence establishes the value or capability. Unknown is never zero or unlimited.

Every field cell uses an availability code and evidence level. `parsed` means the current
parser can project the field (including a stated derivation), not that every plan returns it.
`unparsed` means a field is established in the relevant response but the app drops it.
`other-endpoint` requires evidence of access with the existing grant; a source-only candidate
whose access is unresolved stays `unknown`. `provider-ui` means an official UI supplies the
information but an eligible app transport is not established. `unavailable` means absent in
the specified contract or inapplicable to that unit, not a claim of global impossibility.
`unknown` means research could not decide. A missing response field always has an unknown
value even when its field class is `parsed`. A class is not a value.

New source findings: `source_verified_at: 2026-09-26`; `live_verified_at: null` unless a
specific later LC result says otherwise. Historical live results are attributed to their
original dates and never promoted to a new plan or a new live verification date.
Authentication classifications remain in the provider records; quota classifications below
do not authorize client reuse. No account request or credential read is part of source research.

## 2. Current parsing and storage baseline

Source baseline: AI Usage commit `27564d851a6ef823741b0b4b260ce1587a8f6fb6`, read on
2026-09-26. Classification: application-source projection; confidence: high for code
behavior, no new live evidence. Paths below are relative to the repository root.

### Core contract and inventory conventions

`src/windows/AiUsage.Core/Usage/QuotaSnapshot.cs` defines `QuotaSnapshot`, `QuotaGroup`,
`QuotaWindow`, `QuotaAmount`, `QuotaSourceDetails` and `CreditBalance`. No current type has
a period start, personal cap or explicit reset provenance. Snapshot `FetchedAt` comes from
the client clock, not a provider field. IDs and several durations are synthesized below.
All four parsers are typed projections: arbitrary unknown JSON members are discarded, not
retained as extension data. The dropped-field lists cover the inspected schema; they cannot
enumerate future or unobserved payload fields.

### Claude

Source: `src/windows/AiUsage.Infrastructure/Providers/Claude/ClaudeQuotaParser.cs`,
`TryParse`, `ReadModernEntries`, `AddSharedGroup`, `CreateGroup`, `ParseExtraUsage`.
Money uses `src/windows/AiUsage.Core/Providers/Claude/ClaudeQuotaReading.cs`, outside
`QuotaAmount` and `CreditBalance`.

| JSON field or structural selector | Current destination / behavior |
| --- | --- |
| `five_hour`, `seven_day`, `seven_day_opus`, `seven_day_sonnet` | Object/null selectors; generated group IDs/names and window `Id` (`5h`/`7d`), `Duration` 5 hours/7 days. Null bucket omitted; object with unknown values retained. |
| Each legacy bucket's `utilization` | `QuotaWindow.UsedPercent`; `RemainingPercent = 100 - used` when valid 0..100. |
| Each legacy bucket's `resets_at` | `QuotaWindow.ResetsAt`, offset-bearing ISO timestamp required. |
| `limits[]`, `kind` | Selects shared `session`/`weekly_all` fallbacks, scoped or unknown groups. Scoped duration 7 days; unknown kind has no duration. Kind retained as `QuotaGroup.MeteredFeature` for nonshared entries; collision-safe generated IDs. |
| `limits[].percent`, `resets_at` | Window used/derived remaining and reset, as above. |
| `limits[].scope.model.display_name` | `QuotaGroup.Name`; used for generated scoped ID. |
| `limits[].is_active` | Read into intermediate `IsActive`, then deliberately discarded; never `Allowed` or `LimitReached`. |
| `spend.enabled` | `ClaudeExtraUsage.Enabled`; source `Current`. A present object supersedes legacy extra usage. |
| `spend.used.amount_minor`, `.exponent`, `.currency` | `ClaudeExtraUsage.Used` → `ClaudeMoneyAmount.AmountMinor` (nonnegative Int64), `Exponent` (nonnegative int), opaque `Currency`. |
| `spend.limit` and its `amount_minor`, `exponent`, `currency` | `ClaudeExtraUsage.Limit` with same shape; explicit JSON null sets `HasExplicitNullLimit`, not unlimited. Missing stays unknown. |
| `extra_usage.is_enabled` | `ClaudeExtraUsage.Enabled`; source `Legacy` if no current spend object. |
| `extra_usage.used_credits`, `monthly_limit` | Legacy money `Used.AmountMinor`, `Limit.AmountMinor`; no credit-unit inference from the field name. Explicit null monthly limit tracked separately. |
| `extra_usage.decimal_places`, `currency` | Exponent and currency on both legacy money objects; absent stays unknown (no USD/exponent-2 default). |

Dropped: extra unknown members; legacy money when current `spend` exists; nonselected shared
fallback entries; `is_active` after parsing. Known legacy and modern scoped windows are retained
independently, so identity overlap needs care later. No money remaining, money period/reset,
plan, credits or spend-control flag is synthesized. `QuotaWindow.Amount`, `Unlimited` and
`SourceDetails` are unused here. `QuotaGroup.Allowed`, `LimitReached`, `NormalModelSlug` are null.

### Codex

Source: `src/windows/AiUsage.Infrastructure/Providers/Codex/CodexQuotaParser.cs`,
`Parse`, `ParseGroup`/`AddWindow`.

| JSON field or structural selector | Current destination / behavior |
| --- | --- |
| `plan_type` | `QuotaSnapshot.PlanType`, opaque text. |
| `rate_limit`, `additional_rate_limits[].rate_limit` | Group selectors; main ID/name generated; null windows omitted. |
| `additional_rate_limits[].limit_name`, `metered_feature`, `normal_model_slug` | `QuotaGroup.Name`, `MeteredFeature`, `NormalModelSlug`; opaque values, unique generated group IDs. |
| Each rate limit's `allowed`, `limit_reached` | `QuotaGroup.Allowed`, `LimitReached`, independent nullable flags. |
| `primary_window`, `secondary_window` | `QuotaWindow.Id` = primary/secondary; no assumed duration. |
| Each window's `used_percent` | `UsedPercent`, derived `RemainingPercent`; out-of-range remains unknown. |
| `limit_window_seconds` | `Duration` if valid positive seconds. |
| `reset_at`, `reset_after_seconds` | `ResetsAt`; integral Unix seconds take precedence, otherwise fetched time plus nonnegative relative seconds. Relative source not retained separately. |
| `credits.has_credits`, `.unlimited`, `.balance` | `CreditBalance.HasCredits`, `Unlimited`, `Balance`; number/numeric string balance, negative becomes unknown. No allotment or period. |
| `rate_limit_reset_credits.available_count` | `QuotaSnapshot.AvailableResetCredits`, nonnegative integer; reset inventory, not usage. |
| `spend_control.reached`, `rate_limit_reached_type.type` | `QuotaSnapshot.SpendControlReached`, `LimitReachedType`. |

`CodexQuotaClient.GetQuotaAsync` additionally reads `account_id` and rejects mismatch; it is
not snapshot data. The parser ignores wrapper `user_id`, account identity, upsell/unknown
metadata and any nonselected reset representation. All `QuotaWindow.Amount`, `Unlimited`
and `SourceDetails` remain null; credit unlimited is separate from window unlimited.

### Copilot

Source: `src/windows/AiUsage.Infrastructure/Providers/Copilot/CopilotQuotaParser.cs`, `Parse`.

| JSON field or structural selector | Current destination / behavior |
| --- | --- |
| `copilot_plan` | `QuotaSnapshot.PlanType`, opaque text (not a reliable marketed-plan mapping). |
| `quota_reset_date` | Every window's `ResetsAt`; date/zone-less input is assumed UTC by the parser. Original text/precision is discarded. |
| `quota_snapshots` object keys | `QuotaGroup.Id`; known names premium_interactions/chat/completions become display names; unknown keys retained. Null entries omitted. Window ID generated as `monthly`; `Duration` remains null. |
| Each pool's `entitlement`, `remaining` | `QuotaAmount.Limit`, `Remaining`; negative becomes unknown; `Used = entitlement - remaining` only if both known and remaining <= entitlement. |
| `percent_remaining` | `QuotaWindow.RemainingPercent`, derived `UsedPercent`; invalid range or explicit unlimited gives unknown percentages. |
| `unlimited` | `QuotaWindow.Unlimited`, independent explicit flag; not inferred from omission. |
| `quota_id`, `quota_remaining`, `overage_count`, `overage_permitted` | `SourceDetails.QuotaId`, `QuotaRemaining`, `OverageCount`, `OveragePermitted`; independent of normalized amount and access restrictions. |

`QuotaAmount.Unit` is `requests` for the three known keys, `unknown` for future keys. No
conversion to AI credits. All other fields are ignored, including any unsupported period or
credit metadata. Group entitlement flags are not inferred from overage permission or balance.

### Antigravity

Source: `src/windows/AiUsage.Infrastructure/Providers/Antigravity/AntigravityQuotaParser.cs`,
`Parse`, `Group`, `Window`, `RemainingPercent`, `Reset`.

| JSON field or structural selector | Current destination / behavior |
| --- | --- |
| `groups[]`, `groups[].buckets[]`, top-level `buckets[]` | Grouped rows take precedence when groups exist; otherwise top-level buckets. |
| `groups[].displayName`, `.description`; root `description` | Group label (displayName before description), unique group ID or default ID. Nonselected description discarded. |
| Bucket `disabled` | True drops bucket; all buckets disabled sets group `Allowed = false`; missing is unknown. |
| `bucketId` | Unique `QuotaWindow.Id` seed and original `SourceDetails.QuotaId`. |
| `window` | ID fallback and `Duration`: weekly/week/7d = 7 days, daily/day/24h = 1 day, 5h = 5 hours, otherwise null. Token not separately retained if bucketId exists. |
| `remainingFraction` | `RemainingPercent = fraction * 100`, `UsedPercent = 100 - remaining`; only 0..1 valid. |
| `remainingAmount` | `QuotaAmount.Remaining`, unit `unknown`, `Used`/`Limit` null; also `SourceDetails.QuotaRemaining`. Number or numeric string; no unit inferred. |
| `resetTime` | `ResetsAt`, parsed with UTC assumption for zone-less input; raw reset text discarded. |

Bucket `displayName`/`description` and arbitrary fields are ignored. Disabled bucket values
are not parsed further. `QuotaWindow.Unlimited`, SourceDetails overage members, credit balance
and snapshot restrictions are null. Plan comes from discovery-time credentials, not this JSON.
`AntigravityQuotaClient` reads `paidTier` presence to decide a second discovery read,
`cloudaicompanionProject` and `currentTier.id` for binding/tier, and `allowedTiers[].id`
plus operation `done`/`error`/`response`/`name` on its provisioning path. These are not limit
amounts. That connect path can write entitlements and is not authorized by this audit.

### Transport, presentation and persistence

Each matching `*QuotaClient.cs` in the four parser directories was inspected. Fixed quota
routes: Claude GET `/api/oauth/usage`; Codex GET `/backend-api/wham/usage`; Copilot GET
`/copilot_internal/user`; Antigravity POST `/v1internal:retrieveUserQuotaSummary`. Reading
source is not an executed request. No new transport is proposed here.

`src/windows/AiUsage.Windows/Adapters/Live/LiveMapping.cs`, `Map`, consumes session quota
indirectly (a literal `QuotaSnapshot` search in Windows finds no match). It maps windows to
`WindowItem`, native amounts to string-valued `NativeAmount`, and money/credits to separate
`ExtensionItem`s. It drops `SourceDetails`, group `MeteredFeature`/`NormalModelSlug` and
Claude money source; it suppresses shared money currency/exponent when used and limit disagree.
Zero entitlement is shown as unknown percentage. It preserves separate unlimited flags,
restrictions, reset inventory and freshness. None of this establishes UI live parity.

| Store / source | Quota persistence and format |
| --- | --- |
| `Codex/CodexQuotaCache.cs`, `ReadAsync`/`WriteAsync` | `codex.quota.json`: plaintext normalized snapshot plus `retrievedAt`, envelope `v = 1`; 512 KiB bound, staged `.new` replace. No identity in this cache; grant is separate. Unknown JSON members are not round-tripped. Invalid/version-mismatched cache returns no cache. |
| `Claude/ClaudeStoredState.cs` and `ClaudeStateStore.cs` | `claude.state`: DPAPI-protected JSON, `Version = 1`, `CachedQuota` includes `ClaudeQuotaReading` and minor-unit money alongside identity/grant generation. Unknown members disallowed. |
| `Copilot/CopilotStoredState.cs` and `CopilotStateStore.cs` | `copilot.state`: DPAPI-protected JSON version 1; `CachedQuota` includes native amounts, unlimited and source details alongside grant generation. Unknown members disallowed. |
| `Antigravity/AntigravityStoredState.cs` and `AntigravityStateStore.cs` | `antigravity.state`: DPAPI-protected JSON version 1; cached quota alongside discovered project/tier and grant generation. Unknown members disallowed. |
| `src/windows/AiUsage.Infrastructure/Persistence/PresentationPreferenceFile.cs`; Windows `Adapters/Live/LivePreferenceStore.cs` | `preferences/appearance.v1.json`, State version 1: labels, order, hidden targets, expansion and appearance preferences. Unknown members preserved through `JsonExtensionData`. No quota amounts, caps, work days or day-start readings. |
| Session/presentation state and AIU-011 history | Current readings and presentation projections live in memory as well as the last-reading caches above. Provider history is memory-only; no local day-start series or estimator observations exist. No raw quota payload is persisted by these parsers. |

Paths such as `Claude/...` in this table are beneath
`src/windows/AiUsage.Infrastructure/Providers/`. Cache generations are latest observations,
not historical U0. Read-only lifecycle reference:
[security and lifecycle](../../platforms/windows/security-and-lifecycle.md). Any future
schema/migration/recovery work belongs to the implementation item and security-lifecycle review.

Coverage check: every `QuotaWindow` member (`Id`, `UsedPercent`, `RemainingPercent`,
`Duration`, `ResetsAt`, `Amount`, `Unlimited`, `SourceDetails`), every `QuotaAmount` member
(`Remaining`, `Used`, `Limit`, `Unit`), every `CreditBalance` member (`HasCredits`,
`Unlimited`, `Balance`) and all four `QuotaSourceDetails` members are inventoried above.

## 3. Limit matrix

Each family below has a field table. A row naming multiple family IDs applies separately to
each named family, not a summed pool. Plan columns are deliberately explicit. Conditional
parser support is source evidence; plan-specific payload presence remains unverified.

### Claude (T-02)

`provider: claude`; `source_verified_at: 2026-09-26`;
`live_verified_at: 2026-09-26` for the LC-01 Pro UI observations below only.
Wire fields and other plans retain their existing source/none evidence.
Quota classification: undocumented OAuth schema plus official product/UI descriptions.
Confidence: high for parser coverage; medium for matching monetary wire data to plan UI;
unknown for actual payload coverage across plans. Authentication: unchanged restricted,
undocumented client reuse; no new permission established.

| Family | Pro | Max (5x / 20x) | Team | Enterprise | Unit, period and reset; sources |
| --- | --- | --- | --- | --- | --- |
| CL-S session | Included | Included, different capacity | Standard and Premium seats | Legacy seat-based seats; not established for current usage-based Enterprise | Percentage; five-hour session cycle, API reset instant. Not a calendar-month pool. C1, C2, C3, C4. |
| CL-W shared weekly | Included | Included | Standard and Premium | Legacy seat-based; current consumption plans have no included allowance | Percentage; fixed account-assigned weekly reset, independent of subscription anniversary. C2, C3, C4. Do not silently call this a sliding seven-day total. |
| CL-M model-scoped weekly | Generic schema supported; included Fable allowance not established for Pro | Fable uses part of weekly allowance | Fable included on Premium, usage credits on Standard | Legacy Premium includes Fable; current usage-based billed as consumption | Percentage; separately scoped weekly counter with returned reset; do not add to shared weekly. Actual family names remain opaque. C1, C5. |
| CL-X legacy `extra_usage` | Optional usage-credit spending | Optional usage-credit spending | Optional seat overage spending | Legacy seats: optional overage; modern consumption payload mapping unknown | Money in minor units + exponent + currency. Monthly spending control documented; exact wire period bounds/reset/time zone absent. C1, C6, C7. |
| CL-D current `spend` | Parser supported if returned | Parser supported if returned | Scope (member/org) unknown | Scope and mapping unknown | Money; preferred representation of the same extra-spend reading, not an additional pool to sum. No wire period/reset. C1. |
| CL-O organization/member/seat/group spend controls | Unavailable for organization controls | Unavailable for organization controls | Org and member controls; seat-tier control documented for legacy Enterprise | Org/member, group controls and optional pooled monthly group budgets; consumption Enterprise differs from legacy seats | Currency-denominated monthly controls. Shared group budget is not a per-member allowance. UI scope known; OAuth projection unknown. C7, C8, C9. |
| CL-B prepaid usage balance / bundle inventory | Optional | Optional | Prepaid usage credits | Shared balance for self-serve consumption; sales-assisted uses invoice, not prepaid balance | Money/credit denomination must follow UI; purchased balance is not a monthly allotment. Expiry or purchase cycle must be distinguished from spend-cap reset. C6, C7, C8, C10. |

Field rows: `CL-X` and `CL-D` share the same field availability but use different source paths.
Enterprise `parsed/source` below means **conditional parser capability**, not evidence that a
current consumption plan returns that field. CL-S/W/M do not apply as an included allowance to
current usage-based Enterprise; legacy variants remain separately eligible in the family table.

| Family / field | Pro | Max | Team | Enterprise | Wire field / limitation |
| --- | --- | --- | --- | --- | --- |
| CL-S / used | parsed/source | parsed/source | parsed/source | parsed/source | `five_hour.utilization` or `limits[kind=session].percent`. |
| CL-W / used | parsed/source | parsed/source | parsed/source | parsed/source | `seven_day.utilization` or `limits[kind=weekly_all].percent`. |
| CL-M / used | parsed/source | parsed/source | parsed/source | parsed/source | Legacy family utilization or `weekly_scoped.percent`; presence conditional. |
| CL-S, CL-W, CL-M / limit | unavailable/source | unavailable/source | unavailable/source | unavailable/source | No absolute token/request limit; 100% is normalized scale, not supplied numeric entitlement. |
| CL-S, CL-W, CL-M / remaining | parsed/source | parsed/source | parsed/source | parsed/source | Derived 100 minus valid used percentage. |
| CL-S, CL-W, CL-M / period start | unavailable/source | unavailable/source | unavailable/source | unavailable/source | No start field; reset-minus-duration is an inference, not parsed start. |
| CL-S, CL-W, CL-M / period end or reset | parsed/source | parsed/source | parsed/source | parsed/source | Corresponding `resets_at`, offset-bearing ISO timestamp. |
| CL-S, CL-W, CL-M / currency | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Inapplicable to percentages. |
| CL-S, CL-W, CL-M / exponent | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Inapplicable to percentages. |
| CL-X, CL-D / used | parsed/source | parsed/source | parsed/source | parsed/source | `used_credits` or `used.amount_minor`. |
| CL-X, CL-D / limit | parsed/source | parsed/source | parsed/source | parsed/source | `monthly_limit` or `limit.amount_minor`; null/absent = unknown, never unlimited. |
| CL-X, CL-D / remaining | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Not parsed; future subtraction needs matching currency/exponent and scope. |
| CL-X, CL-D / period start | unknown/none | unknown/none | unknown/none | unknown/none | No inspected wire field; monthly product wording is not an exact start. |
| CL-X, CL-D / period end or reset | unknown/none | unknown/none | unknown/none | unknown/none | Monthly control, but calendar versus billing anniversary and clock unverified for this wire pool. |
| CL-X, CL-D / currency | parsed/source | parsed/source | parsed/source | parsed/source | Legacy `currency`; each current money object's `currency`. |
| CL-X, CL-D / exponent | parsed/source | parsed/source | parsed/source | parsed/source | Legacy `decimal_places`; each current money object's `exponent`; no default. |
| CL-O / used | unavailable/source | unavailable/source | provider-ui/source | provider-ui/source | Month-to-date spend at applicable scope; C7–C9. |
| CL-O / limit | unavailable/source | unavailable/source | provider-ui/source | provider-ui/source | Configured amount or explicit UI unlimited; not permission to interpret OAuth null as unlimited. |
| CL-O / remaining | unavailable/source | unavailable/source | unknown/none | unknown/none | Direct UI remainder not established; do not duplicate `spend` without scope proof. |
| CL-O / period start | unavailable/source | unavailable/source | unknown/none | unknown/none | Monthly label lacks exact boundary/zone. |
| CL-O / period end or reset | unavailable/source | unavailable/source | unknown/none | unknown/none | Group budget resets for new month; exact instant still unknown. |
| CL-O / currency | unavailable/source | unavailable/source | provider-ui/source | provider-ui/source | Monetary UI; exact billing currency must be observed. |
| CL-O / exponent | unavailable/source | unavailable/source | unknown/none | unknown/none | Display precision is not a wire exponent contract. |
| CL-B / used | unknown/none | unknown/none | unknown/none | unknown/none | Spending history is not necessarily consumption of a particular purchase lot. |
| CL-B / limit | unknown/none | unknown/none | unknown/none | unknown/none | No recurring allotment established; purchased amount is not a monthly limit. |
| CL-B / remaining | provider-ui/source | provider-ui/source | provider-ui/source | provider-ui/source | Usage balance; Enterprise self-serve only, not sales-assisted. |
| CL-B / period start | unknown/none | unknown/none | unknown/none | unknown/none | Purchase/expiry versus recurring period unresolved. |
| CL-B / period end or reset | unknown/none | unknown/none | unknown/none | unknown/none | No recurring refill implied; inspect UI expiry separately. |
| CL-B / currency | provider-ui/source | provider-ui/source | provider-ui/source | provider-ui/source | Verify actual denomination; no conversion to percentage windows. |
| CL-B / exponent | unknown/none | unknown/none | unknown/none | unknown/none | No eligible wire schema established. |

No newly inspected source establishes an additional monetary endpoint reachable by this
app's grant. Enterprise Admin API documentation is a separate authorization boundary, not
`other-endpoint` evidence. Its nullable-limit semantics must not be transplanted to OAuth.

#### Claude Pro UI observations (LC-01, 2026-09-26)

The signed-in personal Usage page displayed the plan name Pro. These cells describe visible
UI rows, independently of the wire-field matrix above. No hidden field is verified.

| UI family / field | Pro | Observation / limitation |
| --- | --- | --- |
| CL-S / used | provider-ui/live | Current-session percentage used row present. |
| CL-W / used | provider-ui/live | This-week percentage used row present. |
| CL-S, CL-W / absolute limit | unknown/none | No absolute token/request cap displayed; percentage meters are not absolute entitlement. |
| CL-S, CL-W / remaining | unknown/none | A direct remaining counter was not displayed; subtraction would be derived. |
| CL-S, CL-W / period start | unknown/none | No start displayed; the session row does not itself prove five-hour duration. |
| CL-S / period end or reset | provider-ui/live | Time of day with AM/PM; no explicit zone on this row. |
| CL-W / period end or reset | provider-ui/live | Weekday and time with AM/PM; no explicit zone on this row. |
| CL-S, CL-W / currency, exponent | unavailable/live | Display unit is percentage, not money. |
| CL-M / row presence | provider-ui/live | No model-scoped weekly row displayed on the observed Usage page; no wire absence inferred. |
| Monthly spending / used | provider-ui/live | Monetary usage displayed with a dollar symbol and this-month label. |
| Monthly spending / limit | provider-ui/live | Explicit no-spend-limit label; no configured finite cap displayed. This does not interpret a null OAuth limit. |
| Monthly spending / remaining | unknown/none | No finite cap remainder displayed. |
| Monthly spending / period start, end or reset | unknown/none | Monthly label present; exact boundaries, calendar versus anniversary and zone not displayed. |
| Monthly spending / currency | provider-ui/live | Dollar symbol displayed; ISO currency identity not separately established. |
| Monthly spending / exponent | unknown/none | Display formatting is not a wire exponent. |
| CL-B / remaining | provider-ui/live | Usage-credit balance present; no value retained. |
| CL-B / currency | provider-ui/live | Dollar symbol displayed; no conversion or ISO-code inference. |
| CL-B / used, limit, period start, end or reset, exponent | unknown/none | No purchase-lot consumption, recurring allotment, exact period or wire scale established by this observation. |
| CL-C cloud-session included credit / remaining | provider-ui/live | Separate included-credit balance present, restricted to cloud sessions; no value retained. |
| CL-C / limit | unknown/none | A credit total is displayed, but is not established as a recurring cap or monthly allowance. |
| CL-C / used | unknown/none | No separate consumed amount established. |
| CL-C / period start | unknown/none | No grant/start date displayed. |
| CL-C / period end or reset | provider-ui/live | Expiry displayed as time, explicit GMT offset, month and day; expiry is not replenishment. |
| CL-C / currency | provider-ui/live | Dollar symbol displayed; ISO identity not separately established. |
| CL-C / exponent | unknown/none | No wire representation observed. |

The separate cloud-session credit is a new UI family, CL-C, for T-07 mapping. It is not
merged with CL-B or CL-X/CL-D. The page says regular plan usage applies after this credit
is consumed or expires. A reset-offer section with an expiry date was also present; no
redemption was performed or recurrence inferred. Product usage breakdown rows were present;
they are breakdowns, not additional independent limits. No controls were changed.

### Codex (T-03)

`provider: codex`; `source_verified_at: 2026-09-26`;
`live_verified_at: 2026-09-28` for the LC-07 personal Pro UI observations below only.
Quota classification: official client internal schema, OMP adapter and official plan/UI
documentation; confidence high for field definitions, unknown for plan-specific response
presence and spend-control units. Authentication remains public-client reuse with permission
unknown. Source references O1-O8 below.

| Family | Plus | Pro | Business | Enterprise | Unit, period/reset and evidence |
| --- | --- | --- | --- | --- | --- |
| CX-P primary window | Included usage | Included usage, tier-dependent | Included seat usage; legacy Codex-only seats differ | Plan/contract-dependent; flexible credit plans need not have fixed caps | Percentage. Actual duration = returned `limit_window_seconds`, never hardcoded. Official pricing describes five-hour periods (O3). |
| CX-S secondary window | Conditional | Conditional | Conditional | Conditional | Percentage; actual duration from response. Prior account read had 604800 seconds; not evidence for every plan. No billing-anniversary assumption. O1/O2/provider record. |
| CX-A additional groups, each primary/secondary | Conditional | Conditional | Conditional | Conditional | Same window fields; independent opaque feature/name/model scope. No guarantee of a particular model group or duration. O1/O2. |
| CX-B `credits.balance` | Optional purchased credits | Optional purchased credits | Shared purchased workspace balance | Contract credit pool for credit-based agreements | Credits, not currency or total allotment. Purchased personal/Business credits have purchase-relative expiry; Enterprise contractual terms. No periodic refill field. O1/O4/O5. |
| CX-I individual spend control | Schema exists, applicability unknown | Schema exists, applicability unknown | Monthly credit limits by seat/user described | Monthly user credit or USD control described | `spend_control.individual_limit` is source-established but unparsed. Unit unknown on wire; resets are epoch/relative seconds. No duration/start. O1/O6/O7. |
| CX-W workspace allocation/overage control | Unavailable | Unavailable | Shared balance and separate controls | Contract allocation, expiry, overage limit | Credits. No universal recurring allotment. Current balance is not the workspace limit. Admin UI/contract is evidence, current-grant transport unresolved. O5/O7. |
| CX-D workspace monetary budget | Unavailable | Unavailable | Not established | Token-based Enterprise USD budget, separate user limit | Money in USD; monthly budget/control period must be checked independently of invoice/user-limit period. Not OpenAI Platform billing. O7/O8. |

Cells classified `unparsed/source` below mean present in the inspected **response schema**,
not a newly observed live body. Plan-specific presence remains unknown. The baseline's generic
ignored-fields statement includes `credits.approx_local_messages`, `approx_cloud_messages`
and the entire `spend_control.individual_limit`; these were identified in O1 during T-03.

| Family / field | Plus | Pro | Business | Enterprise | Field / qualification |
| --- | --- | --- | --- | --- | --- |
| CX-P, CX-S, CX-A / used | parsed/source | parsed/source | parsed/source | parsed/source | `used_percent`; conditional windows. |
| CX-P, CX-S, CX-A / limit | unavailable/source | unavailable/source | unavailable/source | unavailable/source | 100% scale only; no absolute entitlement. |
| CX-P, CX-S, CX-A / remaining | parsed/source | parsed/source | parsed/source | parsed/source | 100 minus used. |
| CX-P, CX-S, CX-A / period start | unavailable/source | unavailable/source | unavailable/source | unavailable/source | No start field; duration is not a start. |
| CX-P, CX-S, CX-A / period end or reset | parsed/source | parsed/source | parsed/source | parsed/source | Unix `reset_at`; fallback fetchedAt + `reset_after_seconds`. |
| CX-P, CX-S, CX-A / currency | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Inapplicable. |
| CX-P, CX-S, CX-A / exponent | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Inapplicable. |
| CX-B / used | unavailable/source | unavailable/source | unavailable/source | unavailable/source | No pool-consumed total. |
| CX-B / limit | unavailable/source | unavailable/source | unavailable/source | unavailable/source | No allotment; explicit `unlimited` is independent. |
| CX-B / remaining | parsed/source | parsed/source | parsed/source | parsed/source | `balance`; actual response presence conditional. |
| CX-B / period start | unknown/none | unknown/none | unknown/none | unknown/none | No purchase-lot start in current reading. |
| CX-B / period end or reset | unknown/none | unknown/none | unknown/none | unknown/none | No expiry/reset in current reading; purchase/contract terms differ. |
| CX-B / currency | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Credits; no currency conversion. |
| CX-B / exponent | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Decimal credit quantity, not money exponent. |
| CX-I / used | unparsed/source | unparsed/source | unparsed/source | unparsed/source | `individual_limit.used` string and `used_percent`; scope/unit require proof. |
| CX-I / limit | unparsed/source | unparsed/source | unparsed/source | unparsed/source | `individual_limit.limit` string. |
| CX-I / remaining | unparsed/source | unparsed/source | unparsed/source | unparsed/source | `remaining` string and `remaining_percent`. |
| CX-I / period start | unknown/none | unknown/none | unknown/none | unknown/none | Not in type; no duration from which to derive it. |
| CX-I / period end or reset | unparsed/source | unparsed/source | unparsed/source | unparsed/source | `reset_at`, `reset_after_seconds`; UI can select calendar UTC or billing-aligned Enterprise period. |
| CX-I / currency | unknown/none | unknown/none | unknown/none | unknown/none | No field in pinned schema; do not assume all plans use credits. |
| CX-I / exponent | unknown/none | unknown/none | unknown/none | unknown/none | No field; monetary scale not established by decimal strings. |
| CX-W / used | unavailable/source | unavailable/source | provider-ui/source | provider-ui/source | Workspace reports; not necessarily complete real-time consumption. |
| CX-W / limit | unavailable/source | unavailable/source | unknown/none | provider-ui/source | Enterprise allocation/control terms; Business balance is not allotment. |
| CX-W / remaining | unavailable/source | unavailable/source | provider-ui/source | provider-ui/source | Selected workspace credit balance; same-pool match to CX-B still needs proof. |
| CX-W / period start | unavailable/source | unavailable/source | unknown/none | unknown/none | Contract/purchase terms; no eligible endpoint established. |
| CX-W / period end or reset | unavailable/source | unavailable/source | unknown/none | unknown/none | Contract allocation expiry is not user-control reset. |
| CX-W / currency | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Credit-native; dollar estimates are not invoices or native currency. |
| CX-W / exponent | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Inapplicable to credit quantities. |
| CX-D / used | unavailable/source | unavailable/source | unknown/none | provider-ui/source | Metered workspace USD spend, contract-dependent. |
| CX-D / limit | unavailable/source | unavailable/source | unknown/none | provider-ui/source | Configured monthly USD budget. |
| CX-D / remaining | unavailable/source | unavailable/source | unknown/none | unknown/none | Direct remainder not established by source inspected. |
| CX-D / period start | unavailable/source | unavailable/source | unknown/none | unknown/none | Verify budget period separately. |
| CX-D / period end or reset | unavailable/source | unavailable/source | unknown/none | provider-ui/source | Displayed budget reset period; exact instant not observed. |
| CX-D / currency | unavailable/source | unavailable/source | unknown/none | provider-ui/source | USD; keep separate from estimated cost of credits. |
| CX-D / exponent | unavailable/source | unavailable/source | unknown/none | unknown/none | UI money does not establish minor-unit wire exponent. |

#### ChatGPT Pro credit UI observations (LC-07, 2026-09-28)

The owner initially reported Pro; the agent then observed ChatGPT Pro on Billing in
Google Chrome. The visible Usage Overview credit section supplies the following evidence.
Subscription percentage limits were not rechecked. No values or personal dates are retained.

| UI family / field | Pro | Observation / limitation |
| --- | --- | --- |
| CX-B / remaining, unit | provider-ui/live | Credits remaining, labelled Current balance; credit-native quantity. |
| CX-B / scope | provider-ui/live | Credit-section text names Work and Codex continuation after usage limits are reached. No broader shared-pool inference. |
| CX-B / used | unknown/none | No credit-pool consumption total displayed in the observed section. |
| CX-B / limit or allowance | unknown/none | No separate allowance or cap displayed; current balance is not an allotment. |
| CX-B / period start, end or reset | unknown/none | No credit period, refill clock or credit expiry displayed. Billing subscription renewal is not a credit reset. |
| CX-B / currency, exponent | unavailable/live | Displayed unit is credits, not money; no wire representation established. |
| Credit spending control / automatic reload | provider-ui/live | Automatic reload shown disabled; this does not establish a spending cap or unlimited spending. |
| Credit spending control / numeric cap, period | unknown/none | No numeric spending cap or control period displayed in the observed credit section. |

The adjacent usage-limit reset inventory is a separate action inventory: its expiry is
not credit-balance expiry. No purchase, reload toggle or redemption was performed.
UI sources: `https://chatgpt.com/settings/usage?tab=overview` and
`https://chatgpt.com/settings/billing`. Transport, CX-I mapping and other plans remain
source/none. Account settings did not establish a marketed plan; Billing did.

### Copilot (T-04)

`provider: copilot`; `source_verified_at: 2026-09-26`; `live_verified_at: null`.
Quota classification: undocumented internal request snapshots, official credit/billing UI
and reporting API; confidence high for source distinction, unknown for current paid-plan
payload parity. Authentication remains the existing device grant/client reuse boundary.
References G1-G7. No Free-account result is applied to a paid plan.

| Family | Free | Pro | Pro+ | Business | Native unit / period |
| --- | --- | --- | --- | --- | --- |
| GH-C `quota_snapshots.chat` | Source parser support; prior Free observation | Conditional wire support; credit plans differ | Conditional wire support | Conditional wire support | Requests in inspected adapter, monthly label; current AI-credit chat not proven equivalent. G1. |
| GH-I `completions` | Official 2000/month | Unlimited inline completions | Unlimited inline completions | Unlimited inline completions | Completion/request count; explicit unlimited flag is retained, not inferred from missing amount. G1/G2. |
| GH-P `premium_interactions` | Historical Free zero-entitlement row does not mean credits absent | Legacy annual request billing: 300/month | Legacy annual request billing: 1500/month | Current request-pool meaning unknown | Premium requests. Legacy Pro/Pro+ cycle resets first of month 00:00 UTC; not a current universal credit allowance. G1/G5. |
| GH-A included AI credits | Allowance exists; exact public amount unspecified in inspected current page | Base 1000 + variable flex 500 = current 1500/month | Base 3900 + variable flex 3100 = current 7000/month | 1900 per assigned license contributes to billing-entity shared pool, not an isolated user bucket | AI credits; calendar month, first day 00:00:00 UTC, no rollover. Figures are public plan terms, not account observations or constants to hardcode. G2/G3/G4. |
| GH-D additional usage / user spending budget | Eligibility not established | Optional USD budget | Optional USD budget | User, organization, cost-center and enterprise controls differ in scope | Money (USD); configured budget period requires UI scope confirmation; never substitute the credit pool or subscription price. G3/G4/G7. |

G3 describes base versus flex amounts: they are distinct components of the current allowance,
not guaranteed independent wire pools. G4 says pool contributions can change with seats;
the effective user budget may bind before the shared pool. The internal endpoint's opaque
`copilot_plan` cannot decide billing generation. No request-to-credit conversion is adopted.

| Family / field | Free | Pro | Pro+ | Business | Wire / evidence qualification |
| --- | --- | --- | --- | --- | --- |
| GH-C, GH-I, GH-P / used | parsed/source | parsed/source | parsed/source | parsed/source | Derived entitlement minus remaining when both valid; percentage separately derived from percent_remaining. Conditional response support. |
| GH-C, GH-I, GH-P / limit | parsed/source | parsed/source | parsed/source | parsed/source | `entitlement`; explicit `unlimited` independent; missing is unknown. |
| GH-C, GH-I, GH-P / remaining | parsed/source | parsed/source | parsed/source | parsed/source | `remaining`, percentage independently; `quota_remaining` retained only in SourceDetails. |
| GH-C, GH-I, GH-P / period start | unavailable/source | unavailable/source | unavailable/source | unavailable/source | No start field. Month start can follow documented semantics only after identifying the applicable pool. |
| GH-C, GH-I, GH-P / period end or reset | parsed/source | parsed/source | parsed/source | parsed/source | `quota_reset_date` string; wire precision unspecified, parser assumes UTC on date-only/zone-less input. |
| GH-C, GH-I, GH-P / currency | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Request counts have no currency. |
| GH-C, GH-I, GH-P / exponent | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Inapplicable. |
| GH-A / used | provider-ui/source | provider-ui/source | provider-ui/source | provider-ui/source | Usage dashboard; reporting endpoints exist but existing-grant eligibility unresolved. |
| GH-A / limit | provider-ui/source | provider-ui/source | provider-ui/source | provider-ui/source | Current account allowance/shared pool; public amounts above do not establish account-specific value. |
| GH-A / remaining | provider-ui/source | provider-ui/source | provider-ui/source | provider-ui/source | Available allowance/pool in UI, not `premium_interactions.remaining`. |
| GH-A / period start | provider-ui/source | provider-ui/source | provider-ui/source | provider-ui/source | Documented first-of-month UTC; source-established semantic boundary, not a current parser field. |
| GH-A / period end or reset | provider-ui/source | provider-ui/source | provider-ui/source | provider-ui/source | Next first-of-month UTC; current wire reset-to-credit binding unknown. |
| GH-A / currency | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Credits; keep money budget separate despite published billing rates. |
| GH-A / exponent | unavailable/source | unavailable/source | unavailable/source | unavailable/source | Credit-native quantity. |
| GH-D / used | unknown/none | provider-ui/source | provider-ui/source | provider-ui/source | Billing usage; meter and scope differ by control. |
| GH-D / limit | unknown/none | provider-ui/source | provider-ui/source | provider-ui/source | Configured USD budget; no current quota parser mapping. |
| GH-D / remaining | unknown/none | unknown/none | unknown/none | unknown/none | Direct remaining field not established in inspected UI documentation. |
| GH-D / period start | unknown/none | unknown/none | unknown/none | unknown/none | Verify selected budget's period, not subscription renewal by assumption. |
| GH-D / period end or reset | unknown/none | unknown/none | unknown/none | unknown/none | UI check must distinguish included-credit reset from budget control period. |
| GH-D / currency | unknown/none | provider-ui/source | provider-ui/source | provider-ui/source | USD budget. |
| GH-D / exponent | unknown/none | unknown/none | unknown/none | unknown/none | No established minor-unit wire schema. |

`unlimited: true` differs from absent/null; overage permission is a separate nullable flag,
not unlimited base usage. An exhausted amount does not prove access denied. The API reset
property is typed as a string by G1; no source inspected requires exclusively a date or
instant. Current parser accepts both and loses original precision. G5 establishes the UTC
instant for legacy premium-request reset; G3/G4 establish it for AI credits. Neither proves
all future internal pools share the same clock.

### Antigravity (T-05)

`provider: antigravity`; `source_verified_at: 2026-09-26`;
`live_verified_at: 2026-09-29` only for recording the owner-supplied Google AI Plus UI
image evidence below (capture time unknown; no agent live-session or transport verification).
Free, Pro and Ultra field evidence is unchanged.
Quota classification: undocumented internal summary and official product/UI documentation;
confidence high for observed source fields and unknown-unit treatment, unknown for paid-plan
live payload coverage or credit transport. Authentication/client reuse remains explicitly
restricted by the provider (A6); the audit does not change that boundary.

| Family | Free | Pro | Ultra | Native unit / period; sources |
| --- | --- | --- | --- | --- |
| AG-5 model-group five-hour | Not documented as included on Free | Baseline refresh every five hours until weekly cap | Baseline refresh every five hours | Fraction remaining normalized to percent; token `5h`, returned reset instant. Exact activation/rolling clock not publicly established by plan wording alone. A1/A2. |
| AG-W model-group weekly | Baseline refresh weekly | Weekly cap | Higher weekly cap | Fraction remaining normalized to percent; `weekly`/`week`/`7d`, provider reset. Preserve actual group membership, never split shared third-party group by vendor. A1/A2. |
| AG-R bucket `remainingAmount` | Conditional parser support | Conditional parser support | Conditional parser support | Number/string with **unit unknown**; bucket's window/reset are metadata, not proof of a separately replenished credit pool. A1. |
| AG-C AI-credit overage balance | Availability not established by plan docs | Purchased or promotional AI credits | Purchased or promotional AI credits | AI credits, possibly shared across products/family. Purchase/offer expiry, no universal monthly allocation established for Antigravity. A2/A3/A4. |
| AG-T Tab completions | Unlimited | Unlimited | Unlimited | Completion count; official entitlement, no corresponding summary counter established. A2. |

Google AI Pro/Ultra benefit pages confirm extra usage through AI credits. The separate Flow
page describes monthly **Flow** credits and billing-cycle refresh; it cannot establish an
Antigravity allotment (A4/A5). The CLI credits panel is documented as showing balance and
current-cycle consumption; no callable HTTP transport or period-start/reset schema is given.
No CLI was run and no CLI credential store was read.

| Family / field | Free | Pro | Ultra | Field / qualification |
| --- | --- | --- | --- | --- |
| AG-5 / used | unavailable/source | parsed/source | parsed/source | 100 minus valid remainingFraction*100; Free entitlement not established. |
| AG-W / used | parsed/source | parsed/source | parsed/source | Same percentage derivation, per group. |
| AG-5 / limit | unavailable/source | unavailable/source | unavailable/source | No absolute entitlement; normalized 100% only. |
| AG-W / limit | unavailable/source | unavailable/source | unavailable/source | No absolute entitlement. |
| AG-5 / remaining | unavailable/source | parsed/source | parsed/source | `remainingFraction`; actual paid-plan response presence unverified. |
| AG-W / remaining | parsed/source | parsed/source | parsed/source | `remainingFraction`; missing means unknown. |
| AG-5, AG-W / period start | unavailable/source | unavailable/source | unavailable/source | No start; reset-minus-duration is inference. |
| AG-5 / period end or reset | unavailable/source | parsed/source | parsed/source | `resetTime`; parser UTC assumption for zone-less text is not provider proof. |
| AG-W / period end or reset | parsed/source | parsed/source | parsed/source | `resetTime`; untouched Free bucket previously showed moving reset, not stable period proof. |
| AG-5, AG-W / currency | unavailable/source | unavailable/source | unavailable/source | Percentages; inapplicable. |
| AG-5, AG-W / exponent | unavailable/source | unavailable/source | unavailable/source | Inapplicable. |
| AG-R / used | unavailable/source | unavailable/source | unavailable/source | Not supplied in inspected summary. |
| AG-R / limit | unavailable/source | unavailable/source | unavailable/source | No denominator; do not infer using fraction. |
| AG-R / remaining | parsed/source | parsed/source | parsed/source | `remainingAmount`; unknown unit, independently retained. |
| AG-R / period start | unavailable/source | unavailable/source | unavailable/source | No field. |
| AG-R / period end or reset | parsed/source | parsed/source | parsed/source | Bucket `resetTime` retained; applicability to amount remains unknown. |
| AG-R / currency | unknown/none | unknown/none | unknown/none | Unit unknown; no currency field. |
| AG-R / exponent | unknown/none | unknown/none | unknown/none | No exponent field. |
| AG-C / used | unknown/none | provider-ui/source | provider-ui/source | Credit activity/consumption UI; not current quota summary. |
| AG-C / limit | unknown/none | unknown/none | unknown/none | Recurring Antigravity allotment not established. |
| AG-C / remaining | unknown/none | provider-ui/source | provider-ui/source | Google One AI credits activity / provider credits UI. |
| AG-C / period start | unknown/none | unknown/none | unknown/none | Billing-cycle summary wording not an exact start; mixed purchase lots possible. |
| AG-C / period end or reset | unknown/none | provider-ui/source | provider-ui/source | Acquisition-dependent expiry information, not an established recurring reset. |
| AG-C / currency | unknown/none | unavailable/source | unavailable/source | Credit-native; not currency and not remainingAmount. |
| AG-C / exponent | unknown/none | unavailable/source | unavailable/source | Credit quantity, not money exponent. |
| AG-T / used | unavailable/source | unavailable/source | unavailable/source | No counter established. |
| AG-T / limit | provider-ui/source | provider-ui/source | provider-ui/source | Official unlimited entitlement; no numeric limit. |
| AG-T / remaining | unavailable/source | unavailable/source | unavailable/source | Unlimited does not have a finite remainder. |
| AG-T / period start | unavailable/source | unavailable/source | unavailable/source | No quota period for this entitlement. |
| AG-T / period end or reset | unavailable/source | unavailable/source | unavailable/source | No reset established. |
| AG-T / currency | unavailable/source | unavailable/source | unavailable/source | Inapplicable. |
| AG-T / exponent | unavailable/source | unavailable/source | unavailable/source | Inapplicable. |

### Antigravity Google AI Plus (LC-22 addition)

The owner added this personal tier on 2026-09-26. Evidence recorded on 2026-09-29 is an
owner-supplied image of the provider's Models & Quota UI, labelled Google AI Plus. The agent
read the supplied image, not a live application session; capture time is unknown. No CLI was
accessed or executed and no image was copied into the repository. The `live` cells below
are limited to this owner-supplied UI evidence, not agent-operated account/transport proof.
No Free, Pro or Ultra evidence is inherited. UI labels cannot establish hidden summary fields.

Two groups are displayed: Gemini models (Gemini Flash, Gemini Pro), and Claude and GPT models
(Claude Opus, Claude Sonnet, GPT-OSS). Each has Weekly Limit Remaining in percent and a quota
availability label. The explanatory text says models share a weekly limit within each group
and the weekly limit depends on the individual tier. No cross-group pooling is inferred.

| Family / field | Google AI Plus | Qualification |
| --- | --- | --- |
| AG-W / used | unknown/none | No direct used counter displayed; subtraction would be derived. |
| AG-W / limit | unknown/none | No absolute cap displayed; percentage scale is not a numeric entitlement. |
| AG-W / remaining | provider-ui/live | Weekly remaining percentage per displayed group; owner-supplied image only, values omitted. |
| AG-W / period type | provider-ui/live | Weekly label and within-group sharing; rolling versus fixed boundary not established. |
| AG-W / period start | unknown/none | No start displayed. |
| AG-W / period end or reset | unknown/none | No reset date, countdown, time zone or expiry displayed in supplied image. |
| AG-W / currency, exponent | unavailable/live | Display is percentage, not money; no wire scale established. |
| AG-5 / used, limit, remaining, period start, end or reset, currency, exponent | unknown/none | No five-hour row in supplied image; neither full-page nor transport absence established. |
| AG-R / used | unknown/none | No UI-to-wire correspondence established. |
| AG-R / limit | unknown/none | No denominator established. |
| AG-R / remaining | unknown/none | Presence and unit unknown; never infer from a credit balance. |
| AG-R / period start | unknown/none | Not observed. |
| AG-R / period end or reset | unknown/none | Not observed; same-bucket correspondence required. |
| AG-R / currency | unknown/none | Unit unknown. |
| AG-R / exponent | unknown/none | Unit unknown. |
| AG-C / used | unknown/none | Credit activity not observed. |
| AG-C / limit | unknown/none | Recurring Antigravity allotment not established. |
| AG-C / remaining | unknown/none | Balance presence not observed. |
| AG-C / period start | unknown/none | Period type not observed. |
| AG-C / period end or reset | unknown/none | Expiry versus recurring reset not observed. |
| AG-C / currency | unknown/none | Account credit labels not observed. |
| AG-C / exponent | unknown/none | Account credit labels not observed. |
| AG-T / used | unknown/none | This check does not establish completion counters. |
| AG-T / limit | unknown/none | This tier's entitlement not established. |
| AG-T / remaining | unknown/none | Not established. |
| AG-T / period start | unknown/none | Not established. |
| AG-T / period end or reset | unknown/none | Not established. |
| AG-T / currency | unknown/none | Not established. |
| AG-T / exponent | unknown/none | Not established. |

## 4. Gap dispositions

### Antigravity

- **LC-22 closure (2026-09-29):** owner-supplied Google AI Plus UI image establishes two
  weekly percentage groups and their within-group sharing only. G-AG-3 is narrowed for
  this tier; absolute caps and reset boundaries remain unknown. G-AG-1's amount unit and
  G-AG-2's credit balance/allowance/unit/scope/period/expiry/transport remain unknown.
  The owner reports the proposed Google One activity section was not found; the lookup
  is ended by owner direction, not proof that credits do not exist. The agent had suggested
  a navigation path from A3 without verifying it for this tier. No further account lookup
  is pending. Antigravity API-credit billing is excluded from implementation; carry these
  limitations into T-07 without promoting Google One credits to API billing.
- **T-06 disposition (2026-09-26):** LC-22 separately covers Google AI Plus for
  G-AG-1/2/3; it cannot close Free, Pro or Ultra gaps. LC-18/19/20/21 are NOT_RUN,
  not authorized. No current UI or transport evidence for those tiers is added.
- **G-AG-1:** `remainingAmount` stays unit unknown. A1's `buildQuotaSummaryAmount` expressly
  returns `unit: unknown`; it does not establish credits, tokens or requests. LC-18–20 may
  show an explicit unit label; LC-21 can compare only the existing app projection. If no
  labeled same-bucket correspondence exists, the gap remains unknown; numerical coincidence
  is not unit proof.
- **G-AG-2:** overage AI credits exist in official UI, but no existing-grant credit endpoint
  is established. The pinned summary contains neither a credit balance nor an allotment
  schema. LC-19/20 inspect Google One activity and the provider's displayed credit period;
  they cannot establish an OAuth transport. Purchased-credit expiry is not monthly refill.
- **G-AG-3:** Pro/Ultra groups and reset behavior remain unobserved; Google AI Plus has only
  the owner-supplied UI evidence above. Prior Free weekly
  evidence (2026-09-20) does not validate Pro/Ultra five-hour windows, AI credits or model
  eligibility. Its moving untouched reset prevents blindly deriving a period start. LC-18–21
  record displayed/reset semantics without generating usage or waiting for induced exhaustion.
- **G-AG-4:** any generic calendar-month personal-budget fallback must be labelled assumed,
  and must not claim that a purchased balance replenishes. A5's monthly Flow allowance is
  explicitly outside this provider pool. Carry this evidence into T-07/T-09.

### Copilot

- **T-06 disposition (2026-09-26):** LC-12 through LC-17 are NOT_RUN, reason
  "postponed by owner: work account or manual lookup". G-GH-1/2/3/4 retain their
  source findings and explicit live/transport unknowns; no Copilot account was opened.
- **G-GH-1:** request versus credit billing generation is source-resolved: G5 limits legacy
  request guidance to eligible existing annual Pro/Pro+ subscriptions; current pages describe
  AI credits. Internal request keys do not establish credit parity. LC-12–15 inspect separate
  plans; LC-16 compares the existing app projection for one selected plan.
- **G-GH-2:** AI-credit amounts/period are documented in UI; current OAuth quota mapping is
  unknown. G6 reports have native `unitType`, quantities and money, but do not establish
  account allotment or current read:user-grant access. AIU-011 returned HTTP 404 for both
  personal reports on 2026-09-22, with `/user` HTTP 200; no definite cause is claimed.
  LC-17 is a separate paid-personal-account history check. Organization reports need their
  own access boundary and are not added to the existing transport.
- **G-GH-3:** included credits and legacy premium counters reset at calendar-month UTC,
  independent of renewal. Wire `quota_reset_date` precision and binding to paid-plan credit
  pools remain unknown. LC-12–16 record only date/instant/zone shape exposed by each surface.
  A date shown by an app that already normalized it cannot prove raw payload precision.
- **G-GH-4:** Business pool allocation belongs to the billing entity; user budgets can bind
  independently. LC-15 checks only authorized work-account pages for pool/control scope and
  period. Unknown configured budget is never zero or unlimited.

### Codex

- **LC-07 UI disposition (2026-09-28):** personal Pro plan, credit-native current balance,
  Work/Codex continuation scope and disabled automatic reload are agent-observed.
  No separate allowance/cap, credit period/reset/expiry or numeric spending control was
  displayed in the observed credit section. G-CX-2's balance-versus-allotment distinction
  is retained; workspace allocation, G-CX-1/3 transport and G-CX-4 negative-balance behavior
  remain unknown. No Plus evidence or percentage-window regression check is added.
- **T-06 disposition (2026-09-26):** LC-08/09/11 are NOT_RUN, reason
  "postponed by owner: work account or manual lookup"; LC-10 is NOT_RUN, not
  authorized. G-CX-1/2/3/4 transport and work-plan unknowns remain open regardless
  of the personal-plan UI check.
- **G-CX-1 (source-resolved candidate, live unknown):** O1 supplies the unparsed individual
  control with amounts, percentages, `source` and resets. It is not a workspace allotment;
  no unit/currency/exponent/start appears in that type. LC-08/09/10 must establish plan,
  unit and scope before treating it as a credit or monetary pool.
- **G-CX-2:** workspace allocation exists in official product/admin descriptions; Enterprise
  allocations/expiry are contractual, and user controls may be calendar-month UTC or
  billing-aligned. Business purchases are not a recurring refill. LC-08/09 inspect the UI;
  current `credits.balance` cannot provide allotment, used or period.
- **G-CX-3:** AIU-011 on 2026-09-22 observed HTTP 400 for both workspace token variants and
  HTTP 403 for four enterprise credit variants. Cause not established. Personal history
  success and empty credit events prove neither workspace access nor absence of allocation.
  LC-11 is a separately authorized bounded existing-grant history retry; consumption reports
  still may not contain an allotment. No alternative-grant endpoint is classified reachable.
- **G-CX-4:** O4 permits negative credit balances after concurrent work settles; our parser
  converts them to unknown. Retain this discrepancy for T-07; no parser change here. O7/O8
  also establish subscription-attached USD Enterprise controls, with wire mapping unresolved.

### Claude

- **LC-01 UI disposition (2026-09-26):** Pro session/weekly percentage rows, reset
  forms, usage-credit balance, no configured finite monthly spend cap, and separate
  cloud-session credit expiry are observed. G-CL-1's exact monetary period and wire
  mapping remain unknown; G-CL-3's balance presence is confirmed only on this Pro UI.
  Add CL-C to T-07 with unknown transport, recurring allowance and grant start.
  No scoped weekly row displayed does not prove its absence in the payload or on Max.
- **T-06 disposition (2026-09-26):** LC-03/04 are NOT_RUN, reason
  "postponed by owner: work account or manual lookup"; LC-05 is NOT_RUN, not
  authorized. Work subscriptions and G-CL transport unknowns remain open regardless
  of the personal-plan UI check.
- **G-CL-1:** CL-X/CL-D period and reset remain unknown at wire level. Official pages establish
  monthly spend controls, not a reset field or its clock in `/api/oauth/usage`. Close with
  LC-01/02 personal plan and LC-03/04 organization UI, then LC-05 existing-connection parity
  if independently authorized and observable without new transport. Local calendar-month
  fallback would be an assumption under R-06, never a provider reset.
- **G-CL-2:** Team and Enterprise scope is not resolved by `spend.limit`. UI documentation
  establishes member, organization and (Enterprise) group/seat controls; it does not identify
  which one the OAuth object represents. Current Enterprise consumption has no included
  seat allowance. LC-03/04/05 must distinguish plan generation and scope. Unknown stays unknown.
- **G-CL-3:** CL-B prepaid balance and CL-O pooled budgets are distinct from a monthly spending
  control; no app field is established for them. LC-01–04 inspect only existing UI. Do not
  use the spec's generic allowance example as observed provider evidence.
- **G-CL-4:** Weekly period semantics differ from a casual rolling-window description:
  official Pro/Max/Team pages say fixed assigned reset each week. T-07/T-09 must retain that
  distinction. A returned reset is authoritative; month fallback must not replace it.
- **G-CL-5:** New OMP v18.3.2 adds reset-credit inventory handling (including usage keys
  `cedar_ember`/`juniper_tide`) and separate inventory retrieval, while money interfaces and
  `buildClaudeExtraUsageLimit` still lack period/reset. Reset credits are action inventory,
  not monetary/usage allowance. No inventory endpoint or redemption is executed or adopted.
  The paused Agent SDK monthly-credit announcement is not an active allotment (C11).

### Model notes (T-07, 2026-09-29)

A critical read of sections 2 to 4 found these gaps or inconsistencies that affect the model.
Each is resolved in section 5, closed by a named live check, or raised as a decision.

- **M-01 Zero is not unknown.** `LiveMapping` shows a zero Copilot entitlement as an unknown
  percentage. The model treats a known limit of 0 as "not included": not unknown, not
  exhausted and without a budget (section 5.2).
- **M-02 Negative balance (G-CX-4).** O4 allows a negative Codex credit balance; the parser
  turns it into unknown. The model keeps remaining signed for balance pools. The parser change
  belongs to the implementation item.
- **M-03 Claude legacy and modern scoped windows.** Both are retained independently today.
  The model gives each limit one key (section 5.2); a modern entry replaces a legacy entry
  with the same key and the two are never shown or summed as separate limits.
- **M-04 Reset precision.** Copilot `quota_reset_date` and Antigravity zone-less `resetTime`
  are parsed with a UTC assumption and their original precision is lost. The model records
  reset precision (instant or date) and whether the zone was assumed. A Copilot date means
  00:00 UTC on that date, per G3/G5.
- **M-05 No parsed period start.** No provider supplies a period start. Budgetable windows use
  a start derived from the provider reset minus a provider or source-established duration. A
  window with no duration (unknown Claude `kind`, Codex without `limit_window_seconds`, an
  unrecognized Antigravity token) gets no budget.
- **M-06 Balance-only pools.** `credits.balance` (CX-B) reports only a remainder: no used and
  no limit. A budget needs a used amount; raised as PD-034-01.
- **M-07 Codex individual control (CX-I).** It is unparsed and its unit is unknown. The model
  admits it on the percentage scale until LC-08/09/10 establish the unit (section 5.5).
- **M-08 UI-only families.** CL-O, CL-B, CL-C, CX-W, CX-D, GH-A, GH-D, AG-C and AG-T have no
  app transport. They cannot be represented from a snapshot; raised as PD-034-03.
- **M-09 Claude `is_active`.** It is discarded today. The model does not use it; any use needs
  new evidence of its meaning.
- **M-10 Codex relative reset.** The fallback `fetchedAt + reset_after_seconds` is a provider
  reset with the fetch clock's error. The model keeps reset source `provider` for it.

## 5. Normalized limit model

Proposal by T-07 [opus], 2026-09-29. Prose and field tables only: no code, stored format or
parser changes are made. Vocabulary defined here is used unchanged in sections 6 to 9.

### 5.1 Principles

- **One limit is one counter.** A limit is one provider-reported counter of one account,
  identified by its limit key. Limits are never summed or converted across units, currencies,
  accounts or providers (R-02, R-15).
- **Four layers.** The model separates:
  1. provider facts, carried by the snapshot;
  2. local configuration: the personal cap and the work weekdays;
  3. local observations: day-start readings and estimator samples (sections 6 and 7);
  4. derived values: effective limit, budget, states.

  Only layer 1 comes from a snapshot source. Layers 2 to 4 never enter a provider snapshot,
  cache or provider state file.
- **Unknown is explicit.** Every field holds a value or unknown. Unknown is never 0 or
  unlimited. A limit additionally distinguishes an explicit provider null and an explicit
  provider unlimited flag from absence.
- **Derived is labelled.** A value computed by the app, such as a remainder by subtraction or
  a period start from a reset and a duration, carries a derived or assumed source and is never
  shown as provider data.

### 5.2 Vocabulary

| Field | Values | Rule |
| --- | --- | --- |
| limit key | provider, family, native discriminator | Stable identity across readings and snapshot sources. The discriminator is provider-native: Claude limit kind plus opaque scope name, Codex group (`main` or `limit_name`/`metered_feature`) plus `primary`/`secondary`, Copilot `quota_snapshots` key, Antigravity `bucketId`. Never an account identity or a display label. |
| kind | `percent-window`, `countable-pool`, `monetary-pool` | Chosen from the native unit. Percentage windows have no absolute limit. |
| unit | `percent`; count units `requests`, `credits`, `unknown`; money: the currency | Native unit only. A count unit comes from the provider or its documented contract, never from a field name. |
| used, remaining | quantity or unknown | Countable: decimal as reported. Money: minor units (below). Remaining may be negative for a balance (M-02). |
| limit | quantity, or state `unknown`, `explicit-null`, `unlimited`, `not-applicable` | `explicit-null` is a provider null, which is not unlimited. `unlimited` only from an explicit provider flag. `not-applicable` for percentage windows. A known 0 is "not included" (M-01). |
| used percent, remaining percent | 0 to 100 or unknown | Provider percentage, kept independently of amounts; for percentage windows these are the used and remaining values with `L = 100 %`. |
| duration | time span or unknown | Provider-reported or source-established (for example Claude `five_hour`); never assumed. |
| period start | instant or unknown | No provider supplies it (M-05). |
| period start source | `provider`, `derived`, `assumed` | `derived` = provider reset minus a known duration, or a documented calendar boundary (Copilot first of month, 00:00 UTC). `assumed` = R-06 fallback. |
| period end (reset) | instant or unknown | The next instant at which the counter replenishes. |
| reset source | `provider`, `assumed` | `assumed` only for the R-06 fallback. |
| reset precision | `instant`, `date`; zone `explicit` or `assumed-utc` | Preserves what the provider actually sent (M-04). |
| reset meaning | `replenish`, `expire`, `unknown` | Only `replenish` can end a budget period. An expiry (purchased credit, CL-C) is shown as a fact and never used as a reset. |
| flags | allowed, limit reached, overage permitted | Existing independent nullable flags; never inferred from amounts. |
| snapshot source | `provider-api`, `local-cli` | Snapshot metadata (section 5.6). |
| fetched at | instant | Existing client-clock reading time; staleness uses it for every source. |

**Money** is a triple of amount in minor units (signed 64-bit integer), exponent (nonnegative
integer) and currency (opaque provider text; an ISO 4217 code when the provider sends one).
There is no floating-point major-unit value, no default currency and no default exponent. Two
money values are compared or subtracted only when their currencies match exactly and both
exponents are known. The one with the smaller exponent is rescaled to the larger by an exact,
overflow-checked multiplication by a power of ten. It is never rescaled down. An overflow or a
mismatch gives unknown. This is a change of scale within one currency, not a conversion.

**Derived remaining** is used or limit subtracted within one unit, or `100 - used percent`. It
is computed only when both inputs are known and comparable, and it is labelled derived.

### 5.3 Extension of the Core types

| Current type | Proposed extension |
| --- | --- |
| `QuotaWindow` | Adds limit key, kind, period start and its source, reset source, reset precision and reset meaning. `Id`, `UsedPercent`, `RemainingPercent`, `Duration`, `ResetsAt`, `Unlimited` and `SourceDetails` keep their meaning. `ResetsAt` is the period end. |
| `QuotaAmount` | Used, limit and remaining become quantities: a decimal count or a money triple, never both. Adds the limit state. `Unit` keeps its role; for money it is the currency. |
| `CreditBalance` | Folded into a `countable-pool` limit with unit `credits`, remaining = signed balance, limit state `unknown` or `unlimited` from the explicit flag. `HasCredits` stays a flag. The snapshot-level `Credits` member becomes an ordinary limit in a credits group, keeping the separation of credit `unlimited` from window `unlimited`. |
| `ClaudeMoneyAmount`, `ClaudeExtraUsage` | Generalized into the Core money triple and a `monetary-pool` limit. `Source` (legacy or current) is kept as provenance. `HasExplicitNullLimit` becomes limit state `explicit-null`. `Enabled` stays a flag. |
| `QuotaSnapshot` | Adds snapshot source and an optional opaque source version for diagnostics. |
| `QuotaSourceDetails` | Unchanged: independent provider fields, never a substitute for normalized amounts. |

### 5.4 Personal cap and effective limit

A **personal cap** is local configuration, stored outside every provider snapshot, cache and
provider state. It is keyed by the account target ID the app already uses for labels plus the
limit key. It holds an amount in the pool's native unit, a count or a money triple in the
pool's currency, and the instant it was set. It is always labelled as the user's cap and is
never shown as provider data.

- **Eligible:** `countable-pool` and `monetary-pool` limits with a known unit or currency,
  including pools the provider reports as unlimited or without a limit (R-03).
- **Not eligible:** `percent-window` limits (R-03) and pools whose unit is unknown (AG-R, CX-I
  until its unit is established). A cap the user cannot interpret is not offered.

The **effective limit** `L` (R-03):

| Provider limit | Personal cap | Effective limit and binding source |
| --- | --- | --- |
| known, comparable | set | the lower of the two; binding source is the lower one, or `provider` when equal |
| known | not set | provider limit |
| `unknown` or `explicit-null` | set | personal cap |
| `unlimited` | set | personal cap |
| `unlimited` | not set | none: no budget, shown as unlimited |
| `unknown` or `explicit-null` | not set | none: no budget, shown as limit unknown |
| known 0 | any | 0: state "not included", no budget |
| known, currency differs from the cap | set | provider limit; the cap is flagged "currency mismatch" and not applied, never converted |

Percentage windows always have `L = 100 %`. A cap change recomputes `L` at once, and the budget
follows the recomputation rules of section 8. Only the current cap and its set time are kept.

A cap whose limit key is no longer reported is kept, not applied, and listed in settings as
unmatched. Sign-out keeps caps, like labels and order (D-093). Delete stored data removes them.

### 5.5 Provider mapping

"Derived" and "assumed" use the sources of section 5.2. "Budget" says whether section 8 can
compute a daily budget from this limit. Families with no app transport are listed as "not
represented" (M-08, PD-034-03).

| Family | Kind / unit | Used | Limit | Remaining | Period start | Reset (source, meaning) | Cap / budget |
| --- | --- | --- | --- | --- | --- | --- | --- |
| CL-S | percent-window / percent | `five_hour.utilization` or `limits[session].percent` | not-applicable | derived 100 - used | derived: reset - 5 h | `resets_at` (provider, replenish) | no cap; five-hour rules only (R-09, R-11) |
| CL-W | percent-window / percent | `seven_day.utilization` or `limits[weekly_all].percent` | not-applicable | derived | derived: reset - 7 d (fixed assigned weekly cycle, G-CL-4) | `resets_at` (provider, replenish) | no cap; budget with work days |
| CL-M | percent-window / percent, one limit per scope | legacy family utilization or `weekly_scoped.percent` | not-applicable | derived | derived: reset - 7 d | `resets_at` (provider, replenish) | no cap; budget; never added to CL-W; key = kind + opaque scope name, modern replaces legacy (M-03) |
| CL-X, CL-D | monetary-pool / currency | `used.amount_minor` or `used_credits` | `limit.amount_minor` or `monthly_limit`; null = `explicit-null`, absent = `unknown` | derived when currency and exponent comparable | assumed: local month start (R-06) | assumed calendar month (assumed, replenish) | cap yes; budget; one limit, current replaces legacy |
| CL-O, CL-B, CL-C | not represented | - | - | - | - | - | provider UI only; CL-C expiry is `expire` if ever represented |
| CX-P, CX-S, CX-A | percent-window / percent | `used_percent` | not-applicable | derived | derived: reset - `limit_window_seconds`; unknown without it | `reset_at`, or fetched at + `reset_after_seconds` (provider, replenish; M-10) | no cap; five-hour rules when duration is 5 h, budget when duration is 1 d or longer |
| CX-B | countable-pool / credits | unknown (PD-034-01) | `unlimited` flag or `unknown` | signed `balance` | assumed | assumed (assumed); provider meaning `unknown`, never replenish | cap yes; budget only per PD-034-01 |
| CX-I | percent-window scale until unit known; amounts unit `unknown` | `individual_limit.used`, `used_percent` | `individual_limit.limit` | `remaining`, `remaining_percent` | assumed: reset minus one calendar month | `reset_at` or `reset_after_seconds` (provider, replenish) | no cap until unit known; budget on the percentage scale; needs a parser extension (M-07) |
| CX-W, CX-D | not represented | - | - | - | - | - | provider UI only |
| GH-C, GH-I, GH-P | countable-pool / `requests` (parser unit) | derived entitlement - remaining | `entitlement`; `unlimited` flag; 0 = not included | `remaining`; percent from `percent_remaining` | derived: first of the reset's previous month, 00:00 UTC (G5) | `quota_reset_date` (provider, replenish; precision per M-04) | cap yes; budget with work days; `quota_remaining` and overage stay source details |
| GH-A, GH-D | not represented | - | - | - | - | - | provider UI only |
| AG-5 | percent-window / percent | derived 100 - remaining | not-applicable | `remainingFraction` x 100 | derived: reset - 5 h | `resetTime` (provider, replenish; zone may be assumed UTC) | no cap; five-hour rules only |
| AG-W | percent-window / percent, one limit per bucket | derived | not-applicable | `remainingFraction` x 100 | derived: reset - 7 d | `resetTime` (provider, replenish) | no cap; budget; group membership as reported, never split by vendor |
| AG-R | amount of its bucket, unit `unknown` | unknown | unknown | `remainingAmount` | - | the bucket reset is not attached to the amount | no cap, no budget; shown as a secondary amount of its bucket, never a separate limit |
| AG-C, AG-T | not represented | - | - | - | - | - | provider UI only; API-credit billing excluded (owner, 2026-09-29) |

Every family of section 3 has a row. The spec's owner examples fit as follows. A USD 500
allowance with a USD 300 cap is a `monetary-pool` where a represented family supplies the
money (CL-X/CL-D); otherwise the example is served only through PD-034-03. A 17,000-credit
monthly allotment is a `countable-pool` with unit `credits`: a personal cap on CX-B per
PD-034-01, or CX-I once its unit is established.

### 5.6 Snapshot source independence

Prepares AIU-005 without researching CLI capabilities.

- The snapshot carries its source, `provider-api` or `local-cli`, and an optional opaque
  source version. The source is shown as a secondary account attribute, for example "via CLI".
  It never labels an individual limit and never changes a color, state or figure.
- A CLI source must produce the same limit keys. When it cannot map a counter to an existing
  family and discriminator, AIU-005 records it as a new family; it is never merged by guess.
- Limit fields, personal caps (section 5.4), day-start readings (section 6) and estimator
  samples (section 7) are keyed by account target ID and limit key, never by source. A local
  observation records its source only as provenance.
- Staleness, unknown and assumed rules apply identically to both sources.
- Two sources for the same account and limit: PD-034-02. Whatever is decided, fields from two
  sources are never merged within one snapshot.

### 5.7 Stored-format impact

No format is changed now. The implementation item owns the change, with the security-lifecycle
review and the AIU-006 migration discipline (versioned, backed-up, interruptible, recoverable).

| Store (section 2) | Impact | Forward migration |
| --- | --- | --- |
| `codex.quota.json` v1 | New limit fields; credits become an ordinary limit | Envelope v2. A v1 reader maps old members, with period start `unknown` until the next reading. Discarding the cache is acceptable only as recovery, because it loses the offline last reading. |
| `claude.state`, `copilot.state`, `antigravity.state` v1 (DPAPI) | `CachedQuota` changes; unknown members are disallowed, so the version must change | Version 2 with an in-place forward migration of the whole state. Identity, grant and generation are preserved unchanged. A cached quota that cannot be migrated is dropped alone; the grant is never dropped to simplify a migration. |
| `preferences/appearance.v1.json` | None | Unchanged. |
| New budget configuration file | Work weekdays (R-04) and personal caps (section 5.4) | New versioned, size-bounded file in the owned preferences directory, with staged replace, reparse-point checks and extension-data preservation like the appearance file. Separate from appearance so that provider-adjacent amounts never mix with display state. |
| New local observation store | Day-start readings and estimator samples | Specified in sections 6 and 7. |

**Opaque values** such as plan type, currency text, metered feature, model slug, Claude scope
name and Antigravity `bucketId` are stored verbatim and never interpreted beyond documented
keys. No raw provider payload is persisted. Unknown JSON members are still not retained, by the
existing security rule. The absent versus explicit-null distinction is preserved through the
limit state.

## 6. Start-of-day amount

T-08 [opus] will decide U0 sources and minimal local observation retention.

## 7. Five-hour session estimator

T-08 [opus] will define paired inputs, confidence, invalidation and retained observations.

## 8. Budget rules

T-09 [opus] will define work-day, period, cap, reset and local-day rules.

## 9. Worked examples

T-09 [opus] will supply the numerical acceptance examples.

## 10. Live checks

Consolidated T-06 list, extended with LC-22 by the owner on 2026-09-26. Each ID is a
separate check; approval of one does not approve another or a different plan. UI
observations can establish displayed unit/scope/period, not hidden wire
fields. An existing app UI cannot prove a field is absent from a raw response if its parser
does not expose it; such a result leaves the transport gap open.

Execution boundary for this session: the owner authorized opening only their already
signed-in personal accounts. Read the plan in the UI first: run only LC-01 for Claude Pro
or LC-02 for Max, and only LC-06 for ChatGPT Plus or LC-07 for Pro. LC-22 covers Google AI
Plus only. Stop the affected check and ask the owner at sign-in, password, code, 2FA,
account selection or terms acceptance. Read visible page content only; no developer tools,
network traffic, cookies or local storage. Each authorized check reads
only the named usage/billing surface and records presence/absence, unit, scope, period type
and reset form without personal values. No screenshot, identity, actual balance or raw body
is committed. No terms acceptance, settings change, purchase, redemption, inference, new
grant/scope or CLI credential read. A denied/unavailable check remains NOT_RUN; failure to
observe a hidden field is not evidence of absence. Do not launch a multi-provider refresh
as a substitute for a single approved check. App checks require a way to limit requests to
the approved existing connection; otherwise report BLOCKED without changing product code.

Some provider quota surfaces are native applications. If an authorized check requires an
unavailable native surface, record BLOCKED or an explicitly labelled owner report, not
agent-observed live success.
Historical grants/permissions do not authorize any LC. The per-check verdicts below are
research outcomes, not an authorization ledger; actual run evidence belongs in verification.md.

Final T-06 scope, 2026-09-29: completed LC-01 was not repeated; LC-07 was narrowed to
unresolved personal Pro credit/billing fields, excluding percentage regression. LC-22 quota
evidence came from an owner-supplied image; its Google One activity part is NOT_RUN after
the owner reported no such section and ended the lookup. All other exclusions in the
verdict table remain effective. T-06 closes with explicit unknowns, not universal live proof.

Attempt on 2026-09-26: the in-app browser reached signed-out/public pages, without revealing
any account plan or usage. The owner then required the default PC browser and prohibited
further in-app-browser use. Windows identifies Edge as the default HTTPS browser; the native
control tool opened it but stopped the turn because it could not determine the current URL
with enough confidence to enforce its policy. No personal plan was observed. Claude and
ChatGPT plan selection are BLOCKED; LC-01/02 and LC-06/07 remain NOT_RUN because no matching
plan could be selected. LC-22 is BLOCKED before account observation. Existing matrix cells
remain source/none, every affected live/transport gap remains open, and no provider
`live_verified_at` was advanced at that checkpoint. This attempt is superseded for Claude
by the Chrome continuation below; it remains historical evidence of the access interruption.

Chrome continuation, 2026-09-26: the owner explicitly selected Google Chrome. LC-01
successfully observed the signed-in personal Pro Usage page; see the dedicated UI matrix.
LC-02 is NOT_RUN because the observed plan is Pro. When opening a subsequent browser tab,
native Computer Use stopped this turn because it could not confidently determine the
current URL to enforce policy. No further browser input occurred. ChatGPT plan selection
and LC-22 remain blocked before observation; transport gaps remain open. Continue in Chrome,
not Edge or the in-app browser.

| ID | Account type / surface | What it proves / gap | Risk / boundary | Verdict |
| --- | --- | --- | --- | --- |
| LC-01 | Claude Pro, provider Settings > Usage | Displayed session/weekly/scoped rows, usage-credit cap, balance, period/reset; G-CL-1/3 | Read-only private billing surface; owner opens signed-in account; no toggles or purchases | PASS, UI observation only; exact monetary periods and transport unknowns remain open |
| LC-02 | Claude Max, provider Settings > Usage | Same evidence for Max plus scoped weekly relationship; G-CL-1/3/4 | Same; a Pro result cannot fill Max cells | NOT_RUN; observed personal plan is Pro, not Max |
| LC-03 | Claude Team, provider member/admin Usage | Whether cap is member/org, period label and balance; G-CL-1/2/3 | Work-account approval and existing role required; no membership/settings changes | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-04 | Claude Enterprise, provider member/admin Usage | Legacy versus consumption plan, org/member/group pooled controls and period; G-CL-1/2/3 | Work-account approval; only already accessible pages; no terms acceptance | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-05 | One owner-selected Claude plan, existing AI Usage connection | Compare exposed money components/window reset with that plan's UI; G-CL-1/2 | Unsupported restricted OAuth boundary; refresh may rotate grant. No new connection/scopes; absent hidden fields remain unknown | NOT_RUN; not authorized |
| LC-06 | ChatGPT Plus, provider Usage | Credit balance semantics/expiry; G-CX-2/4; percentage regression excluded | Read-only private usage page; no purchase or reset | NOT_RUN; observed personal plan is Pro, not Plus |
| LC-07 | ChatGPT Pro, provider Usage | Credit balance, unit/scope, allowance/control and period/expiry presence; no percentage regression | Read-only; owner opens account; no settings changes | PASS, scoped UI observation; undisplayed credit periods/caps and transport remain unknown |
| LC-08 | ChatGPT Business, provider Billing/Usage | Seat/user monthly controls versus purchased workspace balance; G-CX-1/2 | Work-account approval and existing role; no auto-reload or purchase | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-09 | ChatGPT Enterprise, provider Usage/Admin billing | Credit or USD contract, user period, shared allocation/budget and reset; G-CX-1/2/4 | Work-account approval; no new role, key, settings or terms | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-10 | One selected ChatGPT plan, existing AI Usage connection | Actual returned window durations and exposed credits/restrictions; G-CX-1/2 | Grant refresh may rotate; no new scopes. Current UI drops individual_limit, so cannot close hidden schema presence by itself | NOT_RUN; not authorized |
| LC-11 | Existing eligible Business/Enterprise AI Usage connection, history surface | Bounded seven-day retry of existing current-user workspace history reports; G-CX-3 | Private work data and optional denial; no other-user requests, new transport or grants; record only status/field shape | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-12 | Copilot Free, provider Copilot/Billing usage | Included credits versus inline suggestions, reset display; G-GH-1/2/3 | Read-only private page; no upgrade or budget changes | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-13 | Copilot Pro, provider usage/billing | Legacy annual versus credit billing, cap and reset; G-GH-1/2/3 | Owner opens account; no billing changes | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-14 | Copilot Pro+, provider usage/billing | Same for Pro+, including base/flex allowance; G-GH-1/2/3 | Separate plan proof; no billing changes | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-15 | Copilot Business, provider authorized usage/billing UI | Shared entity pool versus member budget and reset; G-GH-2/3/4 | Work-account permission and existing role; no admin mutation or new role | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-16 | One selected Copilot plan, existing AI Usage connection | Parsed request pools, flags and reset versus that UI; G-GH-1/3 | Private grant read/possible renewal; no new scope; cannot prove dropped credit fields absent | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-17 | Personally paid Pro or Pro+, existing AI Usage history | Bounded one-day AI-credit and premium report eligibility/unit/period; G-GH-2 | Current grant only, at most existing identity check and two reports; stop on denial, no PAT | NOT_RUN; postponed by owner: work account or manual lookup |
| LC-18 | Antigravity Free, provider read-only quota UI | Weekly group/reset labels, any explicit amount unit; G-AG-1/3 | Owner opens already signed-in surface; no onboarding or settings | NOT_RUN; not authorized |
| LC-19 | Antigravity Pro, provider quota UI and Google One AI-credit activity | Five-hour/weekly labels, credit unit/expiry versus included allowance; G-AG-1/2/3 | Private credit/family data; no purchases, overage toggles or terms | NOT_RUN; not authorized |
| LC-20 | Antigravity Ultra, same provider surfaces | Same for selected Ultra tier; separate plan proof; G-AG-1/2/3 | Same read-only boundary; no activity details or identities retained | NOT_RUN; not authorized |
| LC-21 | One selected Antigravity plan, already provisioned existing AI Usage connection | Parsed groups/reset and unknown amount presence; G-AG-1/3 | Explicit provider third-party restriction/account risk; no reconnect, provisioning or client change; renewal may rotate grant | NOT_RUN; not authorized |
| LC-22 | Antigravity Google AI Plus, owner-supplied quota UI image; proposed Google One activity | Group/window/unit/scope evidence; unresolved cap/balance/period/reset/expiry; G-AG-1/2/3 | No agent CLI access; no Free/Pro/Ultra generalization | PASS for supplied-image structure only; credit activity NOT_RUN, section not found (owner-reported), lookup ended by owner; unknowns retained |

## 11. Pending owner decisions

Each decision affects AIU-034 and the follow-up implementation items (goal G-003). None is
presumed resolved; the recommendation applies only after the owner accepts it. T-11 completes
this section.

### PD-034-01 - Budget for a balance-only pool

- **Question:** CX-B reports only a signed credit balance, with no used amount or allotment
  (M-06). How can a personal cap and daily budget work on it?
- **Options:**
  - (a) Show the balance only; no cap and no budget.
  - (b) Allow a cap. Used in the budget period is accumulated locally from observed balance
    decreases; an increase is a top-up and is ignored. The figure is labelled an estimate,
    "tracked since" the first observation in the period.
  - (c) Allow a cap and compute used as cap minus balance.
- **Recommendation:** (b). (c) is wrong whenever the balance includes purchases or carry-over.
  (a) cannot serve the owner's 17,000-credit example.
- **Impact:** (b) needs the local observations of section 6. A top-up and consumption between
  two readings net out, so the estimate can undercount. It must never be shown as provider data.
- **Evidence:** section 3 CX-B rows, LC-07, O4/O5, M-02, M-06.
- **Needed:** at Gate A, because it decides which pools the Phase B brief shows with a budget.

### PD-034-02 - Two snapshot sources for one account

- **Question:** after AIU-005, the provider API and a local CLI may both report the same
  account and limit. Which reading is used?
- **Options:**
  - (a) The user selects one primary source per account; the other is used only when the
    primary fails, and the fallback is shown.
  - (b) The newest successful whole snapshot wins.
  - (c) The provider API always wins; the CLI is a fallback.
- **Recommendation:** (a). Alternating sources, as in (b), would add noise to day-start readings
  and estimator samples when sources round differently. (c) ignores users who connect only
  through a CLI. In every option fields are never merged within one snapshot.
- **Impact:** a per-account setting and a fallback indicator; no change to limit fields, caps,
  budgets or observations (section 5.6).
- **Evidence:** section 5.6; AIU-005 scope note in the backlog.
- **Needed:** when an AIU-005 implementation is selected; not a Gate A blocker.

### PD-034-03 - Limits visible only in the provider's UI

- **Question:** CL-O, CL-B, CL-C, CX-W, CX-D, GH-A, GH-D, AG-C and AG-T have no app transport
  (M-08). This includes Copilot AI credits on current paid plans. Does the redesign show them?
- **Options:**
  - (a) Omit them until a transport with the existing grant is established.
  - (b) Offer a manual pool: the user types the limit and the used amount.
  - (c) Show a row without figures that states the limit exists and is visible only in the
    provider's UI.
- **Recommendation:** (a). (b) makes the user the data source and goes stale silently, against
  R-15. (c) adds rows without data to the single window. A personal cap on a represented pool
  still covers the owner's examples where such a pool exists.
- **Impact:** GH-A, the main pool of current paid Copilot plans, stays invisible until LC-16/17
  or a later transport close G-GH-2. The Phase B brief must not design figures for these
  families.
- **Evidence:** section 3 `provider-ui` cells; G-CL-3, G-CX-2/3, G-GH-2, G-AG-2.
- **Needed:** at Gate A, before the Phase B brief lists the data and states (B-2).

## 12. Sources

Local source paths and immutable baseline are in section 2. Provider-specific public sources
will be added by T-02 to T-05. Historical context:
[provider records](../../providers/README.md) and [AIU-011 research](../AIU-011-provider-history/research.md).

### Claude sources (read 2026-09-26)

- C1: OMP `packages/ai/src/usage/claude.ts`, `parseApiLimitEntries`,
  `buildScopedWeeklyUsageLimits`, `parseLegacyExtraUsage`, `parseSpendExtraUsage`,
  `buildClaudeExtraUsageLimit`, `fetchClaudeUsage`: [pinned v18.1.22](https://github.com/can1357/oh-my-pi/blob/23a5b9ae38864d3f785dc6cbc96eb6d674a1d32d/packages/ai/src/usage/claude.ts)
  and [v18.3.2](https://github.com/can1357/oh-my-pi/blob/7853b4e499936f9dcc13c9b64adb55f6b342aabf/packages/ai/src/usage/claude.ts).
  GitHub releases/latest returned v18.3.2, published 2026-09-26T00:00:25Z; tag ref resolved
  to commit `7853b4e499936f9dcc13c9b64adb55f6b342aabf`. Public source only, never executed.
- C2: [Pro limits](https://support.claude.com/en/articles/8325606-what-is-the-pro-plan),
  [Max limits](https://support.claude.com/en/articles/11049741-what-is-the-max-plan).
- C3: [Team limits](https://support.claude.com/en/articles/9266767-what-is-the-team-plan).
- C4: [Usage UI and Enterprise distinction](https://support.claude.com/en/articles/9797557-usage-limit-best-practices),
  [pricing and plan generations](https://claude.com/pricing).
- C5: [Fable plan scope](https://support.claude.com/en/articles/15424964-claude-fable-models-on-your-plan).
- C6: [Personal usage credits](https://support.claude.com/en/articles/12429409-manage-usage-credits-for-paid-claude-plans).
- C7: [Team and legacy Enterprise spending controls](https://support.claude.com/en/articles/12005970-manage-usage-credits-for-team-and-seat-based-enterprise-plans).
- C8: [Consumption Enterprise billing](https://support.claude.com/en/articles/11526368-how-am-i-billed-for-my-enterprise-plan).
- C9: [Enterprise pooled group budgets](https://support.claude.com/en/articles/17005973-manage-pooled-group-budgets-on-enterprise-plans).
- C10: [Usage bundles](https://support.claude.com/en/articles/14246112-buy-usage-bundles).
- C11: [Paused Agent SDK monthly-credit announcement](https://support.claude.com/en/articles/15036540-use-the-claude-agent-sdk-with-your-claude-plan).

### Codex sources (read 2026-09-26)

- O1: official `openai/codex` commit `6b9826e3aa83b1a5947db50f4332cb9c65f1b340`,
  [generated models](https://github.com/openai/codex/tree/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/codex-backend-openapi-models/src/models):
  `rate_limit_status_payload.rs`, `rate_limit_status_details.rs`, `rate_limit_window_snapshot.rs`,
  `additional_rate_limit_details.rs`, `credit_status_details.rs`, `spend_control_status_details.rs`,
  and especially [SpendControlLimitDetails](https://github.com/openai/codex/blob/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/codex-backend-openapi-models/src/models/spend_control_limit_details.rs).
  [Wrapper types](https://github.com/openai/codex/blob/6b9826e3aa83b1a5947db50f4332cb9c65f1b340/codex-rs/backend-client/src/types.rs),
  `RateLimitStatusWithResetCredits`, `AdditionalRateLimitWithNormalModel`.
- O2: [OMP pinned Codex adapter](https://github.com/can1357/oh-my-pi/blob/e4dd2ec3b487f216c569281e2cdb7ec476a81f2e/packages/ai/src/usage/openai-codex.ts),
  `parseUsageWindow`, `parseUsagePayload`, `resolveResetTime`, `fetchUsage`. Its projection
  is narrower than O1 and does not establish a credit allotment.
- O3: [Official pricing](https://learn.chatgpt.com/docs/pricing),
  [plan usage guide](https://help.openai.com/en/articles/11369540-using-codex-with-your-chatgpt-plan).
- O4: [Personal flexible credits](https://help.openai.com/en/articles/12642688-using-credits-for-flexible-usage-in-chatgpt-personal-plans).
- O5: [Business/Enterprise flexible pricing](https://help.openai.com/en/articles/11487671-flexible-pricing-for-the-enterprise-edu-and-business-plans).
- O6: [Business monthly controls](https://help.openai.com/en/articles/20001155-managing-credits-and-spend-controls-in-chatgpt-business).
- O7: [Enterprise usage periods and independent controls](https://help.openai.com/en/articles/20001001-manage-usage-limits-and-overages-in-chatgpt-enterprise-and-edu).
- O8: [Enterprise USD rate-card scope](https://help.openai.com/en/articles/20001415).
- Historical result: [AIU-011 live verification](../AIU-011-provider-history/verification.md),
  2026-09-22 workspace HTTP 400 and enterprise HTTP 403. No new run in T-03.

### Copilot sources (read 2026-09-26)

- G1: [OMP v18.2.6 Copilot adapter](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/usage/github-copilot.ts),
  `CopilotQuotaDetail`, `CopilotUsageResponse`, `parseQuotaDetail`, `buildWindow`,
  `normalizeQuotaSnapshots`, `fetchInternalUsage`. Current parser path in section 2.
- G2: [Current plan table](https://docs.github.com/en/copilot/get-started/plans).
- G3: [Individual credit billing](https://docs.github.com/en/copilot/concepts/billing-and-usage/individuals/billing).
- G4: [Organization pooling and billing](https://docs.github.com/en/copilot/concepts/billing-and-usage/organizations-and-enterprises/billing).
- G5: [Legacy request plans](https://docs.github.com/en/copilot/reference/copilot-billing/request-based-billing-legacy/copilot-requests),
  [legacy reset clock](https://docs.github.com/en/copilot/reference/copilot-billing/request-based-billing-legacy/monitor-premium-requests).
- G6: [Billing reporting API](https://docs.github.com/en/rest/billing/usage); personal Plan-read
  and organization Administration-read are distinct from the existing OAuth grant.
- G7: [License changes and reset independence](https://docs.github.com/en/copilot/reference/copilot-billing/license-changes).
  Historical Free observation and HTTP 404 attempts remain in [Copilot record](../../providers/copilot.md)
  and [AIU-011 verification](../AIU-011-provider-history/verification.md).

### Antigravity sources (read 2026-09-26)

- A1: [OMP pinned summary adapter](https://github.com/can1357/oh-my-pi/blob/78b753124d11f8dd3ae73e2524125890ff7c977e/packages/ai/src/usage/google-antigravity.ts),
  `AntigravityQuotaSummaryBucket`, `classifyWindow`, `buildQuotaSummaryAmount`,
  `buildQuotaSummaryReport`, `fetchAntigravityUsage`. Current parser in section 2 differs
  deliberately from OMP legacy inference and duplicated ranking groups.
- A2: [Antigravity plans](https://antigravity.google/docs/plans).
- A3: [Google One AI credits and activity](https://support.google.com/googleone/answer/16287445?hl=en),
  [provider credits panel](https://antigravity.google/docs/cli/commands/credits),
  [provider credit guide](https://antigravity.google/docs/cli/credits).
- A4: [Google AI Pro benefits](https://support.google.com/googleone/answer/14534406?hl=en),
  [Ultra benefits](https://support.google.com/googleone/answer/16286513?hl=en).
- A5: [Flow credit scope and refresh](https://support.google.com/flow/answer/16526234?hl=en),
  inspected only to prevent importing another product's allotment.
- A6: [Antigravity FAQ restriction](https://antigravity.google/docs/faq).
