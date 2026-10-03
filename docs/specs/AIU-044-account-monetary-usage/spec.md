---
id: AIU-044
type: feature
status: implementing
goal: G-003
scope_version: 1
approval_basis: The owner selected implementation on 2026-10-04, following this specification, preserving supported monthly budgets and spending since window exhaustion, with verification, review, commit and push.
---

# Account-owned monetary usage

## Problem and intended result

AIU-039 temporarily hides every Claude `CL-X` card because a separate card looked
like another account. That family identifies a normalized monetary reading, not a
proven personal-extra-usage product. Suppression therefore also hides a finite
spending limit delivered through the same mapping. Whether a real work subscription
currently delivers that limit through this route has not been verified.

Replace the blanket exclusion with monetary information inside its owning account.
Preserve the accepted Claude Work monthly-budget design when its facts are available,
and preserve the separate `on extra usage` mark. The user must not need to classify
an account as personal or work to recover its available readings.

Sources: [AIU-034 reference](../AIU-034-limit-audit-design-brief/design-reference/README.md),
[Claude source matrix](../AIU-034-limit-audit-design-brief/research.md#claude-t-02),
[D-185 through D-187](../../decisions/accepted.md#d-185---limit-model-and-budget-rules-accepted-at-gate-a),
and [AIU-039 temporary suppression](../AIU-039-multi-account-ledger/spec.md).

## Evidence and classification rules

- Infrastructure currently prefers a `spend` object over legacy `extra_usage` and
  maps either to `CL-X`. Research calls these wire variants CL-D and CL-X; they are
  alternative representations, not separate pools. Do not display or add both.
- Neither variant, a finite limit, the absence of percentage windows, nor an editable
  account name such as Work proves personal overage versus work consumption. The
  Claude parser currently provides no plan classification. Do not infer an organization
  allowance, prepaid balance or API-key billing product from these facts.
- An unknown commercial label alone does not disable a valid budget: compatible
  monetary facts, scope, period and readings determine which calculations are supported.
- Use provider facts with their original currency, exponent, scope and provenance.
  Present returned used/limit values even when commercial purpose is unresolved, with
  a neutral Spending label and an explicit scope/period qualification where needed.
  Never turn a possibly shared organization total into a personal remaining allowance.
- Reuse established period and budget rules only where the reading has a compatible,
  evidenced scope. A calendar fallback permitted by the existing contract remains
  visibly assumed; it is not evidence of a provider reset or a work plan. Missing
  inputs leave derived figures unavailable, not zero. A personal cap is user data,
  never evidence of the provider's product or allowance.
- First inspect existing source evidence and sanitized fixtures for scope semantics.
  Record unresolved mappings honestly. Unknown classification must not block the
  neutral display of available facts or cause blanket hiding. A new transport,
  authorization scope or billing credential is outside this item.

## Proposed presentation

| Available evidence | Presentation within the owning account |
| --- | --- |
| Subscription windows plus monetary reading | Keep the window limits; attach one subordinate monetary section without another account header or sign-out action. |
| Monetary reading is the only available limit | Make that section the account's main limit content; do not substitute No displayed limits merely because its family is CL-X. |
| Compatible monetary pool with finite provider limit or personal cap and sufficient readings | Reuse the existing budget/history/cap presentation. Preserve the Claude Work reference's monthly bar and daily budget where supported; distinguish provider limit, personal cap and assumed period. |
| Unknown commercial purpose, otherwise compatible budget inputs | Use the neutral Spending label and retain the supported budget; personal/work classification is not a prerequisite. |
| Unresolved scope, or neither a known provider limit nor a compatible personal cap | Show available native facts neutrally. Do not invent a budget, prepaid balance, personal entitlement or product-specific name. Keep applicable local history; unavailable controls explain their missing prerequisite. |
| Explicitly disabled monetary usage | Show disabled state under the account. Do not interpret disabled as zero or erase retained data. |
| Missing monetary source or unknown amount | Show unavailable only where appropriate; never create a zero-spend reading or hide the account's other limits/actions. |

The account reference owns rename, reconnect and sign-out. Monetary history and caps
remain tied to the existing reading-series key, never to the display name. Tray
presentation follows the same grouping and the existing eligibility rules; a source
must not become another account or count twice in summaries.

The `on extra usage` mark is independent of the monetary section's budget display.
Retain the accepted calculation: a same-account 5h/7d window is full, and covered,
compatible spending readings show an increase since it filled. Keep its start time,
amount, currency and unknown/correction handling. Do not change the baseline to local
midnight, create a daily-spend requirement, or enable the mark merely because CL-X
exists. An unresolved or incompatible spending scope cannot establish that relationship.

## Boundaries and minimum implementation

Keep Core calculations, Infrastructure normalization/persistence and Windows presentation
responsibilities. Prefer adapting the existing account/limit hierarchy and projection;
do not create a personal/work account taxonomy, new store or provider-specific UI framework.
Add contract metadata only if concrete evidence and a current rendering decision require it.
Document any necessary presentation-contract adjustment before implementation.

Preserve credentials, account references, series keys, readings, caps, preferences and
recovery copies. This is not a cleanup or migration task. Do not rekey or reassign data to
obtain a prettier label. A newly discovered durable-format or credential-boundary change
requires a separate explicit design decision and the applicable security review.

AIU-041 remains separate standalone Anthropic/OpenAI API billing. A monetary subscription
reading obtained through the existing connection belongs here; its currency does not
make it an AIU-041 source. This proposal neither selects AIU-041 nor settles its display
period, credentials or previously recorded product-scope gates. Codex purchased-credit
presentation is unchanged unless the owner separately selects a change to it.

## Acceptance criteria

- AC-01: No blanket CL-X exclusion and no personal/work classification by label, wire
  variant or finite-limit presence. Available monetary facts remain visible under the
  correct account, including an account with only a monetary reading.
- AC-02: A supported finite monthly monetary pool retains its provider limit, compatible
  daily budget, local history and cap behavior. Unknown scope or period is explicit;
  no shared total is represented as a proven personal allowance. Current and legacy
  wire variants never create duplicate pools.
- AC-03: The on-extra-usage mark retains spend since window exhaustion, including when
  the full window spans midnight. Missing coverage, counter corrections, incompatible
  currency/scope and stale evidence do not fabricate a current spending amount.
- AC-04: Enabled, disabled, missing, unknown, zero, no-cap, finite-cap and currency-mismatch
  states are truthful. Two accounts retain separate monetary data and account actions;
  editing a label cannot change financial semantics. Unknown commercial purpose alone
  cannot suppress a budget whose required inputs and scope are otherwise established.
- AC-05: Existing account/series identities, grants, readings, caps and preferences survive
  restart and sign-out under their existing policies. No new provider-history request,
  billing credential, schema migration or data deletion is introduced.
- AC-06: Relevant Infrastructure and Presentation regressions, documents, builds and
  ordinary Windows main/tray/history/account-action smoke pass. Cover work-like finite
  monetary fixtures and personal-extra fixtures without treating their display names as
  classification. Record actual live scope verification separately; fixtures are not proof
  that a real work account exposes the modeled allowance.

## Execution preparation

When implementation is selected: confirm source/scope evidence, specify the smallest
account-owned rendering adjustment, then implement and verify against AC-01 through
AC-06. Record remaining provider uncertainty without replacing absent evidence with a
plan-name heuristic. Current preparation evidence is in [verification.md](verification.md).
