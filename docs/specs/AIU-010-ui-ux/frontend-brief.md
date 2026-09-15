Use the claude_design MCP (https://api.anthropic.com/v1/design/mcp, auth via /design-login) to import this project:
https://claude.ai/design/p/67605f1d-8aa1-4297-8b8b-b8eeb62c902e?file=AI+Usage+App.dc.html
Focus on these files (the whole project is readable):

* `AI Usage App.dc.html`

Also read these files the selection imports:

* `support.js`

Implement: `AI Usage App.dc.html`
Owner authorization and intended result
Rebuild the complete Windows presentation layer from scratch according to the imported design.
The finished application must build, launch and provide a fully navigable, interactive demonstration of every designed feature. All displayed fields, controls and actions must be bound to appropriate MVVM state and commands. Backend-dependent operations must use synthetic data and simulated workflows.
The owner explicitly authorizes:

* Replacing or deleting the existing Windows UI views, windows, presentation view models and presentation-only wiring as necessary.
* Making the new mock frontend the default application experience for this delivery.
* Committing synthetic data, fixtures and demo implementations.
* Adding UI-related NuGet packages needed to faithfully implement the design.

This supersedes earlier instructions to retain the old live UI startup path, limit implementation to existing screens, or seek approval for ordinary UI package additions.
Do not delete or modify the existing Core/Infrastructure backend implementation. Do not connect the new frontend to it. Codex will handle that integration afterward.
Repository and preparation
Target:
`C:\Users\danii\projects\ai-usage-app`
Before editing, inspect Git state and read:

* `AGENTS.md`
* `CONTRIBUTING.md`
* `docs/specs/AIU-010-ui-ux/spec.md`
* `docs/specs/AIU-010-ui-ux/design.md`
* `docs/specs/AIU-010-ui-ux/screens.md`
* `docs/specs/AIU-010-ui-ux/ui-contract.md`
* `docs/specs/AIU-010-ui-ux/fixtures.json`
* `docs/specs/AIU-010-ui-ux/handoffs.md`
* `docs/specs/AIU-010-ui-ux/tasks.md`
* `docs/workflow/verification.md`

The preparation package was published on `codex/aiu-010-design-handoff`, commit `744e4e0`. Locate it without overwriting unrelated work.
The imported design is the owner-selected visual reference. The current prompt supersedes conflicting preparation instructions concerning old UI preservation, mock startup and UI dependencies. Retain all other applicable repository boundaries.
Native implementation
Use C# / .NET 10 / WinUI 3 / XAML and CommunityToolkit.Mvvm.
The imported HTML and JavaScript describe appearance and behavior. Recreate them natively; do not embed the prototype in a WebView or replace the application with a web frontend.
Inspect the whole imported project, including screens, overlays, dialogs, settings, tray surfaces, states and interactions. Cover every designed surface and map it to the S01–S12 inventory. Identify missing design coverage explicitly and extend established components where written requirements supply the behavior.
Preserve the selected design’s layout, typography, spacing, colors, visual hierarchy and motion. Do not substitute an earlier suggested visual direction.
Complete MVVM architecture
Create a coherent view-model hierarchy covering the entire design:

* Application shell and navigation.
* Every page/tab.
* Account cards, contexts, quota groups and windows.
* Dialogs, sheets and reusable components where they need independent state.
* Settings, History, notifications, diagnostics, import/export, recovery and updates.
* Tray presentation.
* Demo scenario selection and simulated operations.

Use CommunityToolkit.Mvvm features appropriately:

* ObservableObject for observable view-model state.
* [ObservableProperty] source generation for bindable state, using syntax supported by the selected SDK and toolkit version.
* [RelayCommand] for synchronous actions.
* Generated asynchronous relay commands for asynchronous operations.
* CanExecute and generated invalidation notifications for action availability.
* Dependent-property notifications for derived values.
* ObservableValidator where editable forms need validation.
* ObservableCollection or suitable observable collections for mutable bound lists.

Use strongly typed bindings wherever practical, including x with explicit binding modes and typed DataTemplates. Bind editable fields TwoWay where appropriate.
Every visible field must come from view-model state or resources. Every actionable control must have a working command or explicit navigation behavior. Implement selection, filtering, expansion, validation, visibility, enablement, loading, cancellation and error states.
Avoid business logic and simulated workflows in code-behind. Keep code-behind limited to view-specific behavior such as focus, native window interaction or animation hooks that are awkward to express declaratively. View models must remain testable without instantiating Windows UI controls.
Do not create one giant view model or add interfaces for every trivial property. Organize by feature and responsibility. Use explicit navigation and service boundaries; avoid a global message bus unless a concrete requirement justifies it.
Mock services and future integration boundary
Define typed presentation-facing interfaces for data retrieval and operations. Implement them using deterministic, in-memory mock services.
These contracts must cover all designed features, not just current provider functionality. Follow the semantic requirements in ui-contract.md, extending them explicitly when the design requires more information.
Do not call, initialize or reuse existing backend workflows, provider sessions, credential stores, transport clients or production persistence. The app’s default composition for this delivery must resolve mock services only.
Keep the existing backend source intact. Design the presentation interfaces so Codex can later add adapters without rewriting views or view models.
Synthetic fixtures and mock implementations must be committed and available in a normal checkout. Never include real credentials, personal account data or copied provider sessions.
Display a restrained “Demo · sample data” marker. Provide an accessible scenario selector or demo control panel so the owner can inspect all meaningful states without editing source code.
Use a controllable clock and deterministic scenarios rather than random failures. Support resetting demo state.
Functional demo expectations
Navigation and presentation behavior must work:

* All tabs/pages open correctly, with meaningful back navigation where needed.
* Dialogs and sheets open, validate, submit and cancel.
* Filters, sorting, manual ordering, account selection and group expansion update displayed state.
* Settings change the demo immediately.
* Buttons exercise appropriate simulated operations and visibly change state.
* History ranges and filters affect synthetic results.
* Import/export, connection, recovery, notifications and updates demonstrate their documented stages and outcomes.

Backend-dependent actions must not perform real external work. Simulate them in memory:

* No actual sign-in or credential access.
* No provider requests or CLI discovery.
* No real import/export of user data or destructive cleanup.
* No actual notification delivery, update installation, restart or startup registration.

Normal application window behavior—including navigation, resizing, minimize, close-to-tray where required, restore and explicit Exit—must work.
“Mocked” does not mean dead buttons or unconditional success. Include success, failure, cancellation, empty, unknown, stale and partial-result scenarios.
Themes
Implement Settings → Appearance:

* System — default; follow Windows app color mode and respond to changes.
* Light — explicit override.
* Dark — explicit override.

Use semantic theme resources consistently across every surface and state. Do not implement Dark as a simple color inversion.
Retain theme selection throughout the session. If cross-launch retention is needed, keep it in a clearly separated demo-only preference store with a reset mechanism; do not use or alter production backend storage.
Support high contrast, keyboard access, accessible labels and display/text scaling. Use English localization resources and Windows culture for numbers and dates.
Loading and motion
Initial load without data:

* Layout-matched skeletons with subtle shimmer.
* No major layout shifts.
* Explicit empty/error/unavailable outcomes.

Refresh with existing data:

* Preserve values and their last successful observation time.
* Show “Updating…” on the affected section.
* Animate updated indicators only after new simulated data arrives.
* Preserve stale readings with retry guidance on failure.

Implement action-specific feedback:

* Refresh: rotating icon while the operation is pending.
* Refresh all: global activity and independent per-account outcomes.
* Connect/reconnect: connecting and authorization-waiting stages with cancellation.
* Validation/import/restore: distinct checking and processing stages.
* Discovery/health checks: progressive results.
* Save/apply: pending feedback only for simulated asynchronous work.
* Destructive actions: required confirmation, restrained processing and clear outcomes.
* Navigation, expansion and theme switching: short, meaningful transitions.

Keep buttons stable in size, prevent duplicate execution appropriately and keep unrelated actions available. Never invent measurable progress. Cancellation must not appear as failure.
Respect reduced-motion settings and provide static equivalents. Stop decorative animation when hidden to tray. Announce meaningful operation states accessibly without announcing animation frames.
Data semantics
Preserve:

* Stable identities independent of editable labels.
* Unknown, unlimited, exhausted, unavailable and stale as distinct states.
* Provider-specific native units and optional monetary metadata.
* Independent account errors and freshness.
* Reset countdowns that do not fabricate restored quota.
* No global sum or average of unrelated quotas.
* Shared quota pools without double counting.

Do not parse formatted text to recover numeric values.
UI dependency authorization
You may select, add and restore UI-related NuGet packages that materially improve faithful implementation: native controls, charts, animation, navigation or presentation utilities.
Use existing selected packages when suitable. Check official documentation, licensing, maintained versions and compatibility with WinUI 3, .NET 10 and the application’s packaging target. Pin versions and explain each addition briefly.
Do not introduce paid-license requirements, external runtime services, telemetry, a different application framework or backend dependencies without a separate owner decision.
Verification and delivery
Work autonomously within this scope. Begin with a concise plan and coverage assessment, then implement and verify the full frontend.
Update obsolete presentation tests to reflect the authorized replacement; do not remove tests simply to hide regressions. Leave backend implementation and its tests intact.
Run the repository’s applicable checks:

* Presentation and infrastructure regressions.
* Document validation and diff checks.
* Native application/package build.
* Actual Windows UI checks where authorized and available.

Verify:

* Default startup uses only mock services.
* Every designed page is reachable.
* Bound fields and commands behave correctly.
* Scenario coverage includes errors and cancellation.
* Light, Dark and System behavior.
* Keyboard navigation and scaling.
* Tray/window lifetime and clean Exit.

Do not install on the host or change certificate trust without applicable authorization. Report unavailable interactive checks honestly as NOT_RUN or BLOCKED.
Return:

1. Scoped commits/diff and changed-file summary.
2. Exact build/run instructions for the mock application.
3. Screenshots from the actual native implementation in both themes.
4. Screen and scenario coverage, with remaining gaps clearly identified.
5. Tests/build/interactive verification results.
6. New UI packages and why they were needed.
7. The view-model/service architecture and exact adapter interfaces for Codex backend integration.
8. Any material deviations from the imported design.

Follow repository publication policy for completed, verified scoped work. Do not merge into main or mark backend integrations complete. The deliverable is a runnable, fully bound native frontend with comprehensive interactive mocks.
