---
id: AIU-010
type: spec
status: implemented
goal: G-003
scope_version: 2
approval_basis: Owner selected the staged Codex / Claude Design / Claude Code workflow on 2026-09-15 and explicitly requested preparation for current and future mocked UI capabilities. Owner amendment 2026-09-15 (frontend-brief.md) selects the Claude Design project as the visual reference and authorizes a full mock-first Windows presentation replacement.
---
# AIU-010 - UI/UX design and frontend preparation

## Outcome

A minimal, beautiful, animated native Windows dashboard whose complete planned Windows experience can be reviewed with synthetic data before its services exist. The current delivery is the preparation package, not a claim that design, frontend or future backend features are complete.

Read [design](design.md), [screen inventory](screens.md), [UI contract](ui-contract.md), [scenario fixtures](fixtures.json), [agent handoffs](handoffs.md), [tasks](tasks.md) and [verification](verification.md). Product requirements derive from [accepted decisions](../../decisions/accepted.md), especially D-073–080, D-091–128, D-129–153 and D-160–168.

## Scope

Design and mock frontend scope includes four providers, multiple accounts and contexts, Overview, quota groups and windows, connection and reconnect, ordering/labels/visibility, themes, accessibility, tray, History, notification preferences, refresh preferences, CLI import, diagnostics, portable data, recovery and update states. The inventory defines each delivery tier.

The owner amendment expands UI preparation and mock frontend scope across planned capabilities. It does not mark AIU-005/006/008/009/011/012/013/014/015 complete or authorize their real backend execution. Product-mode integration in AIU-010 connects existing Codex/Claude capabilities and implements approved presentation preferences. Missing capabilities remain unavailable in product mode while fully interactive in an isolated demo.

### Owner amendment 2026-09-15 - mock-first frontend replacement

[frontend-brief.md](frontend-brief.md) is the verbatim owner brief and prevails over conflicting earlier AIU-010 text about old UI preservation, demo startup and UI dependencies. For this delivery:

- The whole Windows presentation layer (views, windows, presentation view models, presentation-only wiring) is rebuilt from the imported Claude Design project `AI Usage App.dc.html` and may replace or delete the existing UI.
- View models use CommunityToolkit.Mvvm throughout: `ObservableObject`/`ObservableValidator`, generated `[ObservableProperty]` state, generated synchronous and asynchronous `[RelayCommand]` commands with CanExecute invalidation, dependent-property notifications, observable collections and typed `x:Bind` bindings.
- Default startup resolves committed synthetic data and deterministic in-memory mock services only, with a restrained "Demo · sample data" marker, scenario selector, controllable clock and demo reset.
- UI-related NuGet packages may be added within the brief's boundaries: pinned, maintained, WinUI 3/.NET 10/MSIX-compatible, no paid licence, telemetry, external runtime service, different app framework or backend dependency.
- Existing Core/Infrastructure source and tests are preserved unchanged and are not called, initialized or referenced by the new default composition. Codex integrates adapters afterwards.
- Settings → Appearance offers System (default, follows Windows changes), Light and Dark using semantic theme resources, plus the brief's skeleton loading, refresh, per-action feedback and reduced-motion behaviour.
- The imported design revision is recorded in [design-reference/README.md](design-reference/README.md). Its recorded owner review decisions (used-percent thresholds by window type, Used/Remaining display, no Overview summary strip, colour-and-tick severity, no separate Retry button, custom provider glyph tiles) take precedence over earlier inventory wording. They are listed as D1–D7 in [frontend-plan.md](frontend-plan.md) and remain open to owner override.

Exclude Android, Widgets, WSL, Store acquisition, multi-window, command palette, forecasting and automated repair from this package (D-174/175). Do not add a proprietary backend or replace WinUI with a web stack.

## Acceptance

- AC-01: Preparation maps every screen to its owning AIU, current availability, required states and reviewable interactions; source mappings distinguish existing code from planned contracts.
- AC-02: The presentation contract defines stable identity, nullable measurements, units, freshness, capabilities, command outcomes and mock/live isolation. Synthetic scenarios cover normal, edge, failure and future flows without real personal data.
- AC-03: Claude Design delivers two visual directions, one selected complete design, reusable tokens/components, responsive layouts, motion rules and all inventory states. Owner visual approval and Codex feasibility review precede frontend implementation.
- AC-04: Claude Code builds native WinUI/XAML screens against the agreed contract and a deterministic in-memory demo source. Navigation, state transitions, validation and confirmations work; demo operations never touch network, credentials, startup registration, system notifications, user files or production storage.
- AC-10: Default startup composes only presentation contracts and mock services; no Core/Infrastructure workflow, provider session, credential store, transport client or production persistence is referenced or initialized. Every imported design surface is reachable, every visible field is bound to view-model state or resources, and every actionable control has a working command or navigation, including success, failure, cancellation, empty, unknown, stale and partial scenarios.
- AC-11: View models are CommunityToolkit.Mvvm-based and testable without WinUI controls; presentation-facing service interfaces are the documented adapter boundary for Codex. Added UI packages are pinned and justified; Core/Infrastructure source and tests remain unchanged.
- AC-05: Minimal Fluent-compatible design supports System/Light/Dark, high contrast, keyboard and screen readers, reduced motion, 100/150/200% display scaling and text enlargement. English UI strings use resources with English fallback; Windows culture formats numbers and dates.
- AC-06: Indicators distinguish unknown, unlimited, exhausted, unavailable and stale. Overview never sums/averages unrelated quotas. Reset countdowns do not create measurements. Errors remain scoped and prior readings retain freshness.
- AC-07: Codex connects existing workflows without duplicating provider clients or introducing transport/credentials into presentation. Unsupported product commands cannot mutate state or report simulated success. Shared dashboard/tray state and existing close-to-tray/shutdown behavior remain correct.
- AC-08: Future service integration is traceable to separate AIUs. Fixture success is never represented as live evidence. Product behavior, demo behavior and unimplemented capabilities have separate acceptance results.
- AC-09: Relevant regressions, package build and actual Windows UI evidence cover the final frontend and integrated candidate; screenshots, interaction checks and limitations are recorded. The preparation-only delivery requires document validation, fixture consistency checks and primary diff review.

## Design authority

The workflow and scope are owner-approved. Colors, typography details, layout composition and motion tuning are proposals until visual review. Codex owns semantic contract changes; Claude Design may request extensions with explicit missing data and fallback. Backend absence is not a reason to omit a planned mock screen, and a mock screen is not evidence that a provider supports its proposed flow.
