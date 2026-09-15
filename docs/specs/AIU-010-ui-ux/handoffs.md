# Agent handoff prompts

The owner-selected sequence is Codex preparation -> Claude Design -> owner/Codex design review -> Claude Code frontend -> Codex integration. These prompts do not claim an external agent has been launched. Give each agent this directory or its complete contents, not the prompt alone. Keep repository instructions alongside code access.

## Prompt for Claude Design

You are designing AI Usage, a native Windows 11 WinUI subscription-quota dashboard. Read spec.md, design.md, screens.md, ui-contract.md and fixtures.json in this package. Follow their semantics and the repository's accepted product decisions. The desired style is minimal, beautiful, polished and animated, with clear quota indicators and calm information density.

Design the full screen inventory S01–S12, including planned features that currently have no backend. They must be reviewable with synthetic data. Do not omit History, multiple accounts/contexts, tray, notification settings, CLI import, data/recovery or update states merely because they are unimplemented. Do not invent verified provider behavior. The demo label belongs to the demo shell; technical implementation labels do not belong throughout the product UI.

First present two distinct visual directions using Overview and account detail, with concise tradeoffs. After the owner selects one, produce the complete design. Deliver editable source and exported views, navigation/interaction specification or clickable prototype, a token table, component/state catalog, motion timings/easing/reduced-motion alternatives, English copy, responsive behavior and accessibility notes. Map each S ID to artifacts and F scenarios. Include unknown, exhausted, stale, loading, error, empty and disabled states; do not design only a happy-path dashboard.

Use native-feasible components. No global usage percentage, invented quotas, fake trends, assumed currency or provider marks without rights review. Exact resets and freshness must remain discoverable. Keep provider-specific groups data-driven. Return any missing contract field as a proposed extension with rationale and a no-data fallback; do not silently change semantics. Do not implement production code or backend services. Finish with a coverage table and concrete unresolved visual choices for the owner.

## Prompt for Claude Code (use after approved design exists)

Implement only the native Windows presentation and deterministic demo for AIU-010. Read AGENTS.md, CONTRIBUTING.md and this entire package. Obtain the approved design artifact revision from tasks.md before implementation; if missing, report that design review is incomplete. Use existing C#/.NET 10/WinUI/XAML/MVVM conventions, not React, HTML or a WebView frontend.

Implement S01–S12, shared components/tokens, resource-based English copy, navigation, typed presentation records and a deterministic in-memory source following ui-contract.md and fixtures.json. The JSON catalog is scenario input, not a snapshot schema. Expand its seeds into valid typed states and cover all specified transitions. Provide a development demo entry point and scenario selector with the visible “Demo · sample data” label. Keep normal startup wired to existing real services until Codex integration. Demo composition must never initialize or call real providers, credential stores, production persistence or OS action services.

Own presentation paths under src/windows/AiUsage.Windows and relevant presentation/UI tests only. Do not edit Core, Infrastructure, provider evidence, project dependencies, authentication, persisted schemas or existing desktop lifetime semantics without a concrete reviewed contract request. Serialize App/MainWindow composition changes with Codex; do not overwrite existing work. Run in an isolated task checkout when writing independently. Canonical state documents are updated by Codex.

Implement actual interactions, including keyboard ordering, validation, cancellation, confirmations and simulated failure outcomes. No dead buttons or canned success for every action. Do not parse display strings into indicator values. Preserve timestamps, identities, nullable values and native units. Hide/disable unsupported product actions at the capability boundary, not just in the button template. Future destructive and external actions operate solely on demo state.

Run relevant presentation/infrastructure regressions, package build and applicable actual Windows checks under repository policy. Report unavailable interactive evidence as NOT_RUN. Return commit/diff, screenshots from the actual build labeled demo, S/F coverage, check results, design deviations and the exact composition point Codex must adapt. A frontend demonstration does not establish any future provider or backend completion.

## Prompt for Codex integration

Review the actual frontend diff against the approved design, contract and acceptance criteria. Connect available Codex/Claude workflows through Windows-owned adapters. Preserve credentials, quota semantics, account isolation, dispatcher ownership, cancellation and tray lifetime. Do not change provider authentication to satisfy a mock. Scope future real adapters to their owning AIUs and keep them unavailable in product mode until implemented and verified. Implement agreed AIU-010 presentation preferences with appropriate persistence review when needed. Verify demo isolation and actual Windows product behavior separately, record limitations and publish only completed scoped work under CONTRIBUTING.
