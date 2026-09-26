---
id: AIU-034
type: spec
status: approved
goal: G-003
scope_version: 1
approval_basis: Owner request and answers, 2026-09-26, in the session conversation - the owner does not accept the current UI and asked for a global, laconic single-window redesign with no pop-ups or confirmation dialogs, every limit of every account shown inline instead of an account detail view, automatic "5-hour sessions left in the weekly window", support for credit and monetary limits besides subscription windows, a personal cap on credit and monetary pools, a work-day calendar (Saturday and Sunday off by default) and a daily budget for every limit type, with provider data analysed first and a Claude Design brief written afterwards that must not use Claude Design's default look. The owner chose subscription-attached limits plus an always-available personal cap (API-key billing excluded), an adaptive daily budget shown with the fixed baseline, remaining weekly quota expressed in 5-hour sessions, and one research item with two review gates. The recommended defaults in R-09 to R-15 were presented and accepted. Recorded as D-183. The owner approved the written specification on 2026-09-26 and confirmed that the Claude Design brief is written only after the Phase A analysis. Owner amendment, 2026-09-26: the agent split for the follow-up implementation items is recorded in the Closing proposal. Owner amendment, 2026-09-26: A-6 keeps the model independent of the snapshot source (provider API or local CLI) to prepare AIU-005.
---
# AIU-034 - Limit data audit and budget-aware single-window design brief

## Outcome

Two gated research outputs that the redesign builds on:

- **Phase A**: a verified picture of every limit each supported provider and plan can report, the
  data the app must supply itself, and a normalized limit and daily-budget model.
- **Phase B**: a Claude Design brief for a single-window, budget-aware interface with its own
  visual identity.

The item ends with proposed implementation items for the owner to select. It changes no product
code, stored format or provider transport.

## Background

- [AIU-030](../AIU-030-single-window/spec.md) made one usage window without tabs, but account
  detail, provider history and settings still replace the usage view.
  [AIU-031](../AIU-031-compact-pace/spec.md) colors bars by pace: windows of a day or longer are
  split into even calendar-day shares, and windows shorter than a day turn red at 20 % or less.
  D-182 made the app dark-only.
- Known provider data, from the provider records:
  - [Claude](../../providers/claude.md): five-hour and seven-day utilization percentages, scoped
    weekly `limits[]`, and `extra_usage`/`spend` monetary amounts with no stated period or reset.
  - [Codex](../../providers/codex.md): five-hour and seven-day windows, and a credit `balance`
    with no allotment or period.
  - [Copilot](../../providers/copilot.md): monthly request pools with `quota_reset_date`, and AI
    credits not yet mapped.
  - [Antigravity](../../providers/antigravity.md): five-hour and weekly fractions, a
    `remainingAmount` of unknown unit, and AI credits never observed.
- Owner examples of pools the design must serve: a work plan with a USD 500 monthly usage
  allowance and a workspace with a 17,000-credit monthly allotment.

## Owner requirements

These requirements bind both phases and the follow-up implementation items.

- **R-01 Single window.** Every provider, account and limit, including model-scoped weekly
  limits and credit or monetary pools, is shown in the one main window. Renaming and status marks
  are inline. There is no account detail view, modal dialog or pop-up window.
- **R-02 Limit kinds.** Percentage windows (five-hour, weekly, model-scoped), countable pools
  (requests, credits) and monetary pools keep their native units. Values in different units are
  never summed or converted into each other.
- **R-03 Personal cap.** The user can always set a personal cap on a countable or monetary pool,
  even when the provider reports a limit, for example USD 300 of a USD 500 allowance. The
  effective limit is the lower of the personal cap and the provider limit when both are known,
  otherwise whichever is known. A personal cap is local configuration and is never presented as
  provider data. Percentage windows of fixed-price subscriptions have no personal cap.
- **R-04 Work days.** One global set of work weekdays, Monday to Friday by default.
- **R-05 Daily budget.** For a limit with a known effective limit `L`, period start `S` and
  reset `R`:
  - `U0` is the amount used at the start of today's local day and `U` the amount used now.
  - `W` is the number of work days in `[S, R)`. `Wr` is the number of work days from today,
    counting today when it is a work day, until `R`.
  - The **adaptive norm** `N = max(0, L - U0) / Wr` is the primary figure. It is fixed for the
    whole day and recomputed at local midnight, after a reset and after a cap change. When `Wr`
    is zero there is no norm and only the remainder is shown.
  - The **baseline norm** `B = L / W` is shown with the deviation `B x (work days from S through
    today) - U`, read as "ahead by" when positive and "behind by" when negative.
  - **Used today** is `U - U0`, and **left today** is `N - (U - U0)`.
  - Percentage windows use the same rules with `L = 100 %`.
  - Phase A defines partial first and last days, consistent with the proportional handling in
    AIU-031.
- **R-06 Period and reset.** The provider's period and reset are used when supplied. Otherwise
  the period is the calendar month and resets at local midnight on the 1st, and the reset is
  marked as assumed. Phase A confirms this default per provider and plan.
- **R-07 Five-hour sessions.** Where a five-hour and a weekly window share a pool, the app
  estimates `C`, the weekly percentage consumed by one fully used five-hour window, from local
  observations. It shows "weekly remainder ≈ remaining weekly / C sessions" and "today ≈ today's
  weekly norm / C sessions". The figures are labelled as estimates and hidden until the estimate
  is sufficiently confident.
- **R-08 Visual identity.** The design has its own identity and does not reuse Claude Design's
  default theme and colors.
- **R-09 Five-hour colors.** A five-hour window is amber at 30 % or less remaining and red at
  10 % or less or when exhausted. From amber onward the countdown to its reset is shown beside the
  bar. The window is not split into hours.
- **R-10 Binding limit.** An account's status reflects its most constraining limit, so a green
  five-hour bar never hides an exhausted weekly limit or a used-up daily budget.
- **R-11 Work days scope.** Work days apply to every window or pool of one day or longer. They do
  not apply to five-hour windows. On a day off there is no norm and the state is neutral; usage on
  that day reduces the remainder and therefore the next work day's norm. Holiday and vacation
  calendars are deferred.
- **R-12 Destructive actions.** Actions that need confirmation are confirmed inline: the control
  turns into "Confirm · Cancel" in place. Reversible actions offer undo. Sign-out stays immediate
  and keeps history (D-093).
- **R-13 Settings and tray.** Settings, including work days and personal caps, are an inline
  panel in the same window. The tray flyout stays as a secondary surface.
- **R-14 Provider history.** Provider history ([AIU-011](../AIU-011-provider-history/spec.md))
  has no separate view. Phase B proposes an inline expansion or its removal from the main window.
- **R-15 Truthfulness.** Unknown is never zero or unlimited. Estimates and assumed resets are
  labelled. Stale readings are dimmed. There is no combined percentage across limits or
  providers.

## Phase A - provider limit-data audit

### Research questions

Plans in scope: Claude Pro, Max, Team and Enterprise; ChatGPT Plus, Pro, Business and
Enterprise for Codex; Copilot Free, Pro, Pro+ and Business; Antigravity Free, Pro and Ultra.
Answer each question per provider and plan.

- **A-1 Limit kinds.** Which percentage windows, countable pools and monetary pools exist, their
  native units, their period type (rolling, calendar month, billing anniversary) and their reset
  behavior.
- **A-2 Field availability.** For used, limit, remaining, period start, period end or reset,
  currency and exponent, classify each field as one of:
  - parsed today;
  - present in the current response but not parsed;
  - available from another endpoint reachable with the existing grant;
  - visible only in the provider's own UI;
  - unavailable.

  Cite the evidence level: source, live or none.
- **A-3 Known gaps.**
  - Claude: the period and reset of `spend` and `extra_usage`, and how a Team or Enterprise
    monetary allowance is reported.
  - Codex: whether a workspace credit allotment or period exists anywhere, not only the balance.
    The AIU-011 workspace and enterprise credit routes returned HTTP 400 and 403.
  - Copilot: premium requests and AI credits on paid plans.
  - Antigravity: AI credits and the unit of `remainingAmount`.
- **A-4 Start-of-day amount.** Per provider, whether `U0` can come from provider history (for
  example Codex daily usage or Copilot daily billing) or the app must record a local day-start
  reading. If local data is needed, specify:
  - the minimal record, its retention and schema impact;
  - its relation to [AIU-029](../../backlog.md);
  - a note that the security-lifecycle review applies before implementation.
- **A-5 Five-hour session estimator.** Which provider pools expose both windows. Specify:
  - the estimator for `C` from paired readings (the change in the weekly percentage divided by
    the change in the five-hour percentage over the same interval);
  - the minimum sample count, and handling of resets, window rollover and model-scoped limits;
  - the confidence rule and the label shown to the user;
  - the observations that must be kept locally.
- **A-6 Normalized limit model.** A proposal, not code, extending `QuotaWindow`, `QuotaAmount`
  and `CreditBalance`. It covers:
  - kind, unit, used, limit and remaining;
  - period start and end, and the reset source (provider or assumed);
  - the personal cap;
  - the snapshot source (the provider API through the app's connection, or a locally installed
    provider CLI), carried as metadata so that no limit field or budget rule depends on it.
    This was added by owner amendment on 2026-09-26 to prepare AIU-005. CLI capabilities are
    researched there, not here.

  Give the mapping for every provider and the impact on stored formats, preserving opaque
  provider values.
- **A-7 Worked examples.** A table that becomes the implementation's test cases, covering:
  - USD 300 personal cap within USD 500;
  - 17,000 credits a month;
  - a weekly window with work days;
  - usage on a day off;
  - a cap change mid-period;
  - a reset during the day;
  - a daylight-saving transition;
  - a month with fewer or more work days;
  - no remaining work days before the reset.

### Phase A outputs

- `research.md` in this directory, containing:
  - the limit matrix and gap dispositions;
  - the model proposal, budget rules and worked examples;
  - the live checks still required;
  - pending owner decisions.
- New findings recorded in the matching `docs/providers/*.md` files, following the research
  metadata in [document formats](../../workflow/formats.md).

### Gate A

The owner reviews the Phase A outputs. Phase B starts only after that review is recorded in
[verification](verification.md).

## Phase B - Claude Design brief

### Brief contents

`design-brief.md` in this directory, written in English and grounded in the Phase A outputs:

- **B-1 User and jobs.** A developer with several work and personal AI subscriptions checks at
  a glance:
  - Can I keep this pace today?
  - How much of today's budget is left?
  - Which limit binds?
  - When does it come back?
- **B-2 Data and states.** The real limit kinds and every state from Phase A:
  - unknown, stale, assumed reset, exhausted;
  - signed out, sign-in expired;
  - day off, estimate not ready;
  - personal cap set or unset, different currencies.
- **B-3 Information architecture constraints.**
  - R-01, R-12 and R-13, and a provider, then account, then limits hierarchy.
  - Inline rename and cap editing.
  - Information needed for a decision never lives only in a tooltip.
  - Four accounts fit the default window without scrolling.
- **B-4 Visual identity rules.**
  - **Forbidden**:
    - a near-black slate or zinc background with a single indigo, violet or blue accent;
    - purple-to-blue gradients and glassmorphism;
    - a grid of identical rounded cards with soft shadows;
    - Inter or a system font as the only typographic idea;
    - KPI tiles, pill badges everywhere, and decorative emoji or icons.
  - **Required**:
    - a named design concept with its rationale;
    - a palette derived from that concept, with semantic state colors (ok, attention,
      critical, stale, estimate, assumed) meeting WCAG AA contrast on the dark background;
    - a distinctive type pairing with tabular numerals;
    - a signature limit visualization that combines the bar, today's budget, the pace mark and
      five-hour sessions.
  - **Kept**:
    - dark-only (D-182);
    - status never relies on color alone;
    - everything implementable in WinUI 3 on Windows 11;
    - fonts licensed for embedding in an MSIX package.
- **B-5 Expected deliverables.**
  - Two or three distinct directions, of which the owner selects one.
  - Then a full prototype of the main window in every state, including the tray flyout, the
    settings panel, inline editing and confirmation, first run and the sign-in strip.
  - Token and component specifications.
  - Keyboard, accessible-name and reduced-motion notes.
- **B-6 Acceptance rubric.** The criteria the owner uses to accept or reject a direction and the
  final prototype.

### Gate B

The owner reviews the brief before it is sent to Claude Design. Sending requires a working Claude
Design connection; the connection was rejected on 2026-09-26 and needs `/design-login`. The
selected result is imported into the repository as in the
[AIU-010 design reference](../AIU-010-ui-ux/design-reference/README.md).

## Closing proposal

After Gate B, propose follow-up backlog items with outcomes and acceptance criteria, such as the
Core budget engine, provider parser extensions and the redesign implementation. They name the
parts of D-180 and D-181 they supersede. None of them is selected automatically.

Owner direction, 2026-09-26, on how the follow-up implementation work is split between
agents. Each proposed task title carries the recommended agent tag, as in Phase A.

- **`[opus]` (Claude Code, Opus 5.5)** builds the interface from the imported Claude Design
  result:
  - XAML views and controls, and design tokens and styles;
  - view models with their bindings, commands and inline states;
  - the presentation contract (the view-model data shape), exercised with demo data.
- **`[astra]` (ChatGPT Codex, GPT-6 Astra)** connects the backend to that interface:
  - the Core budget engine and the provider parser extensions, following the Phase A model;
  - persistence and migrations;
  - the live adapters that feed the presentation contract.

  Astra also runs interactive Windows checks and independent detail reviews.
- A change to the presentation contract is agreed between the two tasks. It is never changed
  silently from either side.

## Boundaries

- Research is read-only and uses the provider-evidence skill. There are no new authorizations,
  scopes, API keys, inference requests, purchases, reset-credit redemptions or spend-setting
  changes.
- Each live check needs the owner's explicit authorization for that check and an owner-led
  sign-in. The owner decides whether a work account may be connected. The provider restrictions
  recorded in the provider records apply.
- No credentials, raw provider payloads, account identities or personal account data enter Git.
  Only sanitized or synthetic fixtures do.
- All authored documents are in English.

## Excludes

- Implementing the budget engine, parsers or UI.
- API-key billing such as Anthropic Console or OpenAI Platform.
- Holiday and vacation calendars.
- Budget notifications.
- History-based forecasting beyond the five-hour session estimate (AIU-024).
- More than one account per provider.
- Platforms other than Windows.

## Acceptance criteria

- AC-01: `research.md` contains a limit matrix for every provider and plan in scope, with the
  A-2 availability class and evidence level for every field.
- AC-02: Every A-3 gap is resolved, or recorded as an explicit unknown together with the live
  check that would close it.
- AC-03: The normalized limit model proposal covers A-6, with a mapping for every provider and
  the impact on stored formats.
- AC-04: The budget rules implement R-03 to R-07 and R-11 without contradiction and are backed by
  the A-7 worked-example table.
- AC-05: The source of the start-of-day amount is decided per provider. Any new local data is
  specified with its retention, and the security-lifecycle review is noted as a precondition.
- AC-06: The five-hour session estimator defines its inputs, formula, minimum samples,
  invalidation and user label.
- AC-07: The Gate A owner review is recorded before any Phase B work.
- AC-08: `design-brief.md` covers B-1 to B-6, and the Gate B owner review is recorded.
- AC-09: The follow-up items are proposed with outcomes and acceptance criteria and are left
  unselected.
- AC-10: No secrets, raw payloads or personal account data are committed. Document validation
  and `git diff --check` pass.
