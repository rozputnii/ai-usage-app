---
id: AIU-010
type: design
status: draft
goal: G-003
scope_version: 1
---
# Architecture and delivery design

## Chosen approach

Use one native presentation implementation with interchangeable live and demo data sources. A static mockup alone cannot validate interactions; redesigning provider contracts after frontend delivery creates avoidable semantic drift. A Windows-owned presentation contract lets design proceed ahead of services while preserving existing Core/Infrastructure boundaries.

The documents define the contract for design and implementation; they do not introduce production types now. Claude Code implements the smallest necessary presentation types in `src/windows/AiUsage.Windows/Features/Presentation/`, demo behavior in `Features/Demo/`, screens in feature directories and tokens in `Themes/`. Existing `App.xaml`, `MainWindow.xaml` and their code-behind are composition entry points, not a place for provider logic. UI regression tests belong in `tests/windows/AiUsage.Presentation.Tests/`; actual UI scenarios belong in `tests/windows/AiUsage.Windows.Tests/`.

Codex later maps `DashboardWorkflow`, `ProviderSessionState`, `QuotaSnapshot` and Claude extra usage into the presentation contract. Core remains credential-free and platform-neutral; Infrastructure retains provider transport and persistence. Demo data may not become a fallback after a live error. Do not duplicate a live provider client or manufacture account identity from labels.

## Demo boundary

Create an explicit development/demo startup composition, off by default, with a persistent unobtrusive “Demo · sample data” label and scenario picker. The whole session uses demo services. It never initializes provider sessions, grant stores, production persistence or OS action services. Reset demo restores the in-memory seed. Production startup uses live capabilities only; an unsupported destination explains availability and never displays fictional measurements.

All future flows are interactive inside demo, including previews of destructive confirmations and simulated outcomes. Export uses an on-screen synthetic preview, import uses built-in candidates, consent uses a simulated handoff, and notification/update/recovery actions change in-memory state only. No real file picker, browser authorization, deletion, restart or notification is invoked by demo commands.

## Integration sequence

1. Codex completes this preparation package and confirms its source mappings.
2. Claude Design produces visual alternatives and the complete selected layout/state/component specification.
3. Owner selects the visual result; Codex checks semantic coverage and WinUI feasibility. Record the approved artifact revision in tasks before handoff.
4. Claude Code implements all inventory screens in demo mode. Retain existing live startup while adding the demo composition; do not replace real services with stubs globally.
5. Codex reviews actual diffs and connects available services to the approved presentation. Missing service adapters stay capability-disabled in product mode.
6. Verify the complete candidate on Windows, including demo isolation, product regressions and desktop lifetime. Track future real integrations in their owning AIUs.

No new package is selected by this document. Existing selected libraries may be reused; verify their presence and approved versions before implementation. If a selected charting library is not yet installed, resolve dependency availability before T-04 rather than silently substituting a framework. Do not use browser prototypes as the shipped frontend.

## Visual guidance for Claude Design

Aim for calm, premium utility: generous but efficient spacing, readable numbers, subtle surfaces and one restrained accent. Remaining quota is the dominant value. Favor horizontal meters for scanability; a ring may be used where it improves hierarchy. Do not repeat every measure in both a ring and a bar. Provider identities should not produce four competing color systems.

Propose two distinct compositions before polishing one. Suggested evaluation viewports: 560x720, 960x720 and 1440x900 effective pixels. Use one/two/three-plus columns as space allows, preserving manual order. These are design test sizes, not a new minimum-window promise. Avoid clipped primary actions at narrow widths.

Proposed motion targets: 120–180 ms hover/focus feedback; 180–240 ms expand/navigation; 250–400 ms quota interpolation after an actual new reading. Claude Design must specify final durations/easing and reduced-motion equivalents. Animate only visible elements; stop decorative animation and timers when hidden to tray. No pulsing warning loop, continuous decorative motion or animation that delays a command. Text values and accessible state update correctly even when motion is disabled.

Acceptance combines visual approval with actual navigation, keyboard, scaling and contrast checks. Compilation alone cannot establish quality or usability.
