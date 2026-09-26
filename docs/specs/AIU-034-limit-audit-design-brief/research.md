# AIU-034 Phase A research

## 1. Scope and evidence levels

Subscription-attached limits only: Claude Pro, Max, Team, Enterprise; Codex on ChatGPT
Plus, Pro, Business, Enterprise; Copilot Free, Pro, Pro+, Business; Antigravity Free,
Pro, Ultra, plus Google AI Plus as the owner's 2026-09-26 LC-22 addition.
API-key billing is excluded. This is research, not an implementation.

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

`provider: codex`; `source_verified_at: 2026-09-26`; `live_verified_at: null`.
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

`provider: antigravity`; `source_verified_at: 2026-09-26`; `live_verified_at: null`.
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

The owner added this personal low-cost tier on 2026-09-26. Its account tier has not yet
been read in the UI. It has a separate matrix: no Free, Pro or Ultra evidence is inherited.
The planned read-only surfaces are the provider quota UI and Google One AI-credit activity,
with the same observation boundary as LC-19. UI labels cannot establish hidden summary fields.

| Family / field | Google AI Plus | Qualification |
| --- | --- | --- |
| AG-5, AG-W / used | unknown/none | Window rows and unit not observed. |
| AG-5, AG-W / limit | unknown/none | Cap presence not observed. |
| AG-5, AG-W / remaining | unknown/none | Remaining display not observed. |
| AG-5, AG-W / period start | unknown/none | Period type and start not observed. |
| AG-5, AG-W / period end or reset | unknown/none | Reset form not observed. |
| AG-5, AG-W / currency | unknown/none | No unit observed for this tier. |
| AG-5, AG-W / exponent | unknown/none | No unit observed for this tier. |
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
- **G-AG-3:** actual paid-plan groups and reset behavior remain unobserved. Prior Free weekly
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

## 5. Normalized limit model

T-07 [opus] will propose the model and stored-format impact; no design is made in T-01 to T-06.

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
| LC-06 | ChatGPT Plus, provider Usage | Actual window durations, credit balance semantics/expiry; G-CX-2/4 | Read-only private usage page; no purchase or reset | NOT_RUN |
| LC-07 | ChatGPT Pro, provider Usage | Same for selected Pro tier; no Plus generalization | Read-only; owner opens account; no settings changes | NOT_RUN |
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
| LC-22 | Antigravity Google AI Plus, provider quota UI and Google One AI-credit activity | Displayed group/window labels, credit unit, cap/balance presence, period and reset/expiry form; G-AG-1/2/3 | Personal account only; same boundary as LC-19; no Free/Pro/Ultra generalization | BLOCKED; browser access interrupted before account observation |

## 11. Pending owner decisions

Later modeling tasks will record decisions requiring owner input; none is presumed resolved here.

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
