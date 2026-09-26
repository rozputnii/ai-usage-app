# AIU-034 Phase A research

## 1. Scope and evidence levels

Subscription-attached limits only: Claude Pro, Max, Team, Enterprise; Codex on ChatGPT
Plus, Pro, Business, Enterprise; Copilot Free, Pro, Pro+, Business; Antigravity Free,
Pro, Ultra. API-key billing is excluded. This is research, not an implementation.

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

`provider: claude`; `source_verified_at: 2026-09-26`; `live_verified_at: null`.
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

## 4. Gap dispositions

### Claude

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

Draft checks, all NOT_RUN; no authorization is implied. T-06 will consolidate and request
each separately. UI observations can establish displayed unit/scope/period, not hidden wire
fields. An existing app UI cannot prove a field is absent from a raw response if its parser
does not expose it; such a result leaves the transport gap open.

| ID | Account type / surface | What it proves / gap | Risk / boundary | Verdict |
| --- | --- | --- | --- | --- |
| LC-01 | Claude Pro, provider Settings > Usage | Displayed session/weekly/scoped rows, usage-credit cap, balance, period/reset; G-CL-1/3 | Read-only private billing surface; owner opens signed-in account; no toggles or purchases | NOT_RUN |
| LC-02 | Claude Max, provider Settings > Usage | Same evidence for Max plus scoped weekly relationship; G-CL-1/3/4 | Same; a Pro result cannot fill Max cells | NOT_RUN |
| LC-03 | Claude Team, provider member/admin Usage | Whether cap is member/org, period label and balance; G-CL-1/2/3 | Work-account approval and existing role required; no membership/settings changes | NOT_RUN |
| LC-04 | Claude Enterprise, provider member/admin Usage | Legacy versus consumption plan, org/member/group pooled controls and period; G-CL-1/2/3 | Work-account approval; only already accessible pages; no terms acceptance | NOT_RUN |
| LC-05 | One owner-selected Claude plan, existing AI Usage connection | Compare exposed money components/window reset with that plan's UI; G-CL-1/2 | Unsupported restricted OAuth boundary; refresh may rotate grant. No new connection/scopes; absent hidden fields remain unknown | NOT_RUN |

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
