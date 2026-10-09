---
schema_version: 1
---
# Canonical backlog

Statuses: idea / research-needed / blocked / ready / selected / in-progress / paused / review / done / dropped.

**AIU items are coherent outcomes; T items are internal execution plans.** The order below is an initial sequence, not a fabricated numeric ranking. The owner selects work under CONTRIBUTING.md; no status authorizes execution. Research-needed denotes unresolved research/spike scope. Provider order is preferred delivery order, not a reason to stop independent work when one provider is blocked. A ready label does not override unmet dependencies.

## AIU-005 - Codex CLI and other provider CLI integrations
- goal: G-003
- status: research-needed
- priority: low
- depends_on: [AIU-003, AIU-004, AIU-007, AIU-008, AIU-009, AIU-010]
- trigger: after-all-provider-connections-and-ui-polish
- outcome: Research, then implement where evidence supports it, two separate ways of using locally installed provider CLIs (Codex, Claude Code, Copilot CLI and the Antigravity or Gemini CLI). (A) Credential import - discover CLI sign-ins in known Windows locations asynchronously, show progressive candidates and import individual/all/re-import, so the app uses the imported grant with its own provider transport. Never mutate source stores; enable only a proven-safe token lifecycle, including refresh-token rotation shared with the CLI. (B) CLI as a usage source - the app runs the installed CLI as a local process and reads usage, limits, resets and account context from its documented or observed non-interactive output or protocol, while the CLI keeps its own credentials and the app never reads them. Research each method per provider and per account type and classifies it separately: the command or protocol, the fields available relative to the AIU-034 limit matrix, whether a reading needs an inference request (disallowed), freshness, process cost, version drift, and failure and signed-out states. Method B is the preferred first implementation candidate because the app never handles CLI credentials.
- sequencing-note: Owner amendment, 2026-09-14: defer CLI integrations until all four providers connect using the same connection style as OMP (omp.sh), and a usable UI design and UI/UX polish are complete. CLI import is no longer the next task after the Codex slice. Reading or importing real source CLI credentials still requires explicit current authorization.
- scope-note: Owner amendment, 2026-09-26: the scope now covers both method A and method B. Start with a research gate that answers both methods for every provider before any implementation is selected. The work starts after the AIU-034 redesign and its implementation items, as a separate item; it is not selected by this note. AIU-034's normalized limit model is kept independent of the snapshot source (A-6) so that a CLI source needs no model or UI rework. Method A requires the security-lifecycle review. Method B requires evidence that no inference request, new grant or source-store mutation occurs.

## AIU-012 - Threshold notifications and OS-aware refresh
- goal: G-003
- status: idea
- depends_on: [AIU-004]
- trigger: incremental
- outcome: Implement threshold inheritance, hysteresis, deduplication, aggregated activation and power/network/sleep/lock behavior with targeted tests.
- scope-note: The baseline five-minute automatic refresh, its backoff and age-based staleness moved to AIU-033; power, network, lock and provider-specific cadence remain here.

## AIU-013 - Diagnostics, portable Replace import and resets
- goal: G-003
- status: idea
- depends_on: [AIU-006]
- trigger: incremental
- outcome: Implement local sanitized diagnostics, diagnose-only repair UX, plain export, safe staged Replace, settings reset and factory reset.
- scope-note: AIU-043 owns expanded file logging, provider-response evidence and crash diagnostics. Reuse that pipeline here; portable Replace, broad repair/export UX and reset delivery remain in AIU-013. This split does not select either implementation.

## AIU-014 - Trusted direct Preview/Stable distribution
- goal: G-004
- status: paused
- depends_on: [AIU-006]
- trigger: before-public
- outcome: Verify signing eligibility/identity and runtime prerequisite delivery. Implement monotonic version allocation, Releases/Pages/App Installer, channel switching and nonblocking updates.
- scope-note: Owner selected an initial development-only phase on 2026-09-22: publish every successful main push and automatically update the owner's test installations. Use self-signed CI packages with one-time explicit tester trust; official signing/public distribution and Stable remain deferred. See docs/specs/AIU-014-preview-updates/spec.md. That phase was operational on 2026-09-23: signed Previews publish from main, and a Sandbox feed install then updated automatically. Official signing, Stable, channel switching and public distribution remain open. Public signing research draft: docs/specs/AIU-014-public-signing/spec.md. Owner decision 2026-09-24: personal tool; public signing, Store and Stable deferred (paused). Owner decision 2026-10-06 (AIU045-D1): Previews publish only through an explicit owner workflow_dispatch of a green main commit, replacing the 2026-09-22 publish-every-push rule; pushes to main, including save points, never publish. Owner decision later on 2026-10-06 reversed this: every green main push publishes again, and a dispatch can republish.
- specification: docs/specs/AIU-014-preview-updates/spec.md
- evidence: docs/specs/AIU-014-preview-updates/verification.md

## AIU-015 - Signed compatibility manifest and release authority
- goal: G-004
- status: research-needed
- depends_on: [AIU-014]
- trigger: before-v1
- outcome: Design narrow disable-only policy, offline root, rotation/revocation/expiry, reset/bootstrap protections and human-approved publication.

## AIU-016 - Windows v1 security, performance and stability verification
- goal: G-003
- status: idea
- depends_on: [AIU-007, AIU-008, AIU-009, AIU-010, AIU-011, AIU-012, AIU-013]
- trigger: before-v1
- outcome: Verify cross-provider acceptance, reference-machine startup, long-running tray behavior, historical states and actual packaged results.

## AIU-017 - Compatibility watchdog intake
- goal: G-004
- status: idea
- depends_on: [AIU-014]
- trigger: after-working-flow
- outcome: Run hourly best-effort and explicit checks using dedicated credentials only. Route incidents to owner-authorized development work, not automatic global disabling or hosted coding.

## AIU-018 - Native Android implementation
- goal: G-005
- status: idea
- depends_on: [AIU-016]
- trigger: after-windows
- outcome: Make platform-specific decisions and reuse proven provider semantics/fixtures. Do not impose a shared UI implementation in advance.

## AIU-019 - Windows Widgets
- goal: G-005
- status: idea
- depends_on: [AIU-016]
- trigger: after-windows
- outcome: Prove the native widget provider and lifetime separately. Do not assume an out-of-process widget shares in-memory AppState.

## AIU-020 - WSL CLI discovery and import
- goal: G-005
- status: idea
- depends_on: [AIU-005]
- trigger: after-demand
- outcome: Define explicit per-distribution trust/execution scope. No silent WSL scan on first launch.

## AIU-021 - ARM64 support
- goal: G-005
- status: idea
- depends_on: [AIU-016]
- trigger: after-demand
- outcome: Test real ARM64 dependencies, packaging and devices. x86 is not a target without a new owner goal.

## AIU-022 - Microsoft Store acquisition and direct-to-Store migration
- goal: G-005
- status: research-needed
- depends_on: [AIU-014, AIU-016]
- trigger: after-stability
- outcome: Verify Store identity, name reservation and paid-acquisition requirements when selected. Transfer data safely and reauthenticate when necessary.

## AIU-023 - Multi-window and command-palette refinement
- goal: G-005
- status: idea
- depends_on: [AIU-010]
- trigger: after-feedback
- outcome: Split these into independent outcomes only when selected and useful, not speculatively.

## AIU-024 - Experimental usage forecasting
- goal: G-005
- status: research-needed
- depends_on: [AIU-029]
- trigger: after-history
- outcome: Prove forecasting with sufficient history and reset/gap handling. Clearly label estimates; do not affect factual quota alerts.
- sequencing-note: The AIU-011 scope split on 2026-09-22 moves the dependable local observation history prerequisite to AIU-029. Provider-supplied reports alone do not establish comparable quota samples or reset/gap coverage.

## AIU-025 - Always-on development fix runner
- goal: G-005
- status: research-needed
- depends_on: [AIU-001, AIU-017]
- trigger: later
- outcome: Define separate host, security, cost, secret and lifetime requirements only after local automation is proven.

## AIU-026 - Deferred main branch protection
- goal: G-004
- status: idea
- priority: low
- depends_on: [AIU-001]
- trigger: only-after-owner-reprioritization
- outcome: Configure main rulesets/protection, exact-head required checks and restricted bypass when the owner selects this work. Explicitly deferred on 2026-09-13; not a prerequisite for current development PRs or AIU-002.

## AIU-029 - Local usage history and retention after product stability
- goal: G-003
- status: idea
- priority: low
- depends_on: [AIU-004]
- trigger: after-stable-product-and-owner-selection
- outcome: Collect and persist normalized local quota observations; define retention, reset/gap-aware rollups, historical queries and offline chart access. Design any durable cache of provider-supplied history here when selected, preserving its provenance separately from local observations.
- scope-note: Split from AIU-011 by owner direction on 2026-09-22. Defer until the product is stable; neither completion of AIU-011 nor existing history preferences starts this work. CLI transcript ingestion remains separate from local quota sampling and is not authorized by this entry.
- sequencing-note: Local observation history does not technically depend on a remote history endpoint. The initial AIU-011 dependency was corrected after OMP source inspection established that these are independent data sources; the owner-selection and product-stability gates remain unchanged.
- direction-note: Owner direction, 2026-09-29 (D-184): local tracking is the only history source. AIU-034 specifies the local reading series that supplies the start-of-day amount, the five-hour session estimate and work-day splits; this item later extends its retention, rollups and history queries. Owner selection and the product-stability trigger are unchanged.

## AIU-041 - Paid API token spend (Anthropic API, OpenAI API)
- goal: G-005
- status: idea
- depends_on: [AIU-039]
- trigger: owner-selection
- outcome: The owner proposed this in the AIU-034 Claude Design round (D-186). The main window would show the month-to-date spend, and today's spend, of pay-as-you-go API tokens as an "Extra" group, separate from subscription quotas. The handoff draft showed Anthropic API and OpenAI API cards with a monthly total, today's spend and no cap. They were removed from the design reference, because D-183 and the constitution exclude API-key billing from consumer quotas.
- acceptance: (1) Before any work, an owner decision amends the constitution line "Do not confuse consumer quotas with API billing" and D-183's exclusion, and names the authorized credentials (admin or usage API keys), their storage under DPAPI and a security-lifecycle review. (2) Provider evidence, from source and live, establishes each usage or billing endpoint, its unit, its currency and minor units, its period and its freshness. (3) API spend is never summed with, converted into or shown as part of a subscription quota. (4) No figure appears without an established source; an unknown value is never 0.
- supersedes: Nothing. It needs an owner amendment of D-183.
- boundary-note: AIU-044 owns monetary subscription readings already available through existing account connections, including their budget presentation and the on-extra-usage mark. Those readings are not standalone API-key billing merely because they are denominated in money. The 2026-10-04 AIU-044 clarification does not select AIU-041 or settle its display period or access decisions.
- source: docs/decisions/accepted.md (D-186)

## AIU-046 - In-app update check, install and restart
- goal: G-004
- status: review
- depends_on: [AIU-014]
- trigger: owner-selection
- outcome: The app checks its Preview feed itself, installs a found version with one button, and in automatic mode installs and relaunches while the window is hidden. Windows App Installer's own feed checks stay unchanged. The title-bar Settings button shows a gear.
- acceptance: AC-01 through AC-08 in the specification cover the persisted Off/OnLaunch/Always mode, check timing, hidden-window automatic install, failure recovery and loop guard, unpackaged status, installed relaunch, the gear glyph and bounded logging.
- specification: docs/specs/AIU-046-in-app-updates/spec.md
- evidence: docs/specs/AIU-046-in-app-updates/verification.md
- registration-note: Owner requested this on 2026-10-06 after Preview 2026.10.604.0 staged but failed to register (`0x80073D02`) because the tray app was running. The owner approved the design, written spec and plan the same day. Implemented and verified in Sandbox on 2026-10-06. Awaiting owner review and integration; the owner live update check is NOT_RUN.

## AIU-049 - Measured five-hour window cost history and estimate accuracy
- goal: G-003
- status: idea
- depends_on: [AIU-048]
- trigger: owner-selection
- outcome: Record, for every completed five-hour window, how much of the weekly limit it actually consumed, using the AIU-048 per-window interval bounds from the stored readings. Show it as an additional per-day chart in the card's inline history, in the same style as the existing per-day use history (local days, gaps kept as gaps, reset ticks). Each day is labelled with the measured capacity, for example "9×5h in 7d", and below it an accuracy line, for example "accuracy 92 %". This tests the AIU-048 assumption that one window costs a constant share of the weekly limit, for example whether a provider weights windows by time of day.
- open-questions: The definition of the accuracy percentage. Recommended: the share of measured windows whose own bounds agree with the pooled estimate, which directly measures how often the constant-cost assumption holds; the alternative is the relative tightness of the bounds. Whether the per-window figures are derived on demand from the stored reading series, which needs no new persistence, or kept as their own records.
- registration-note: Owner proposed this on 2026-10-07 after the AIU-048 review noted that its bounds are guaranteed only while the window cost stays constant. Not selected for implementation yet.

## AIU-056 - Tray icon handle leak and TrayFailure log flood
- goal: G-003
- status: ready
- depends_on: []
- trigger: owner-selection
- outcome: The tray icon keeps updating its colour for days of uptime: each redraw releases the icon handle it replaces, and a tray failure that repeats is logged once with a count instead of on every attempt.
- acceptance: A test or a bounded probe shows the process handle count stays flat over thousands of tray glyph updates; a recurring failure produces one detailed record plus a coalesced count; the Infrastructure and Presentation suites, the Release build without warnings and the demo startup smoke pass. The owner checks the installed app's logs after deployment (D-190).
- registration-note: Found on 2026-10-09 in the installed app's logs (2026.10.837.0 and earlier): 5,778 TrayFailure errors (ExternalException, hResult -2147467259) from `TrayGlyph.Create` through `LedgerWindow.UpdateTrayGlyph` in three sessions. Each time they began 5.5 to 8 hours after launch, and every later attempt failed until the app restarted. `TrayGlyph.Create` returns `Icon.FromHandle(bitmap.GetHicon())`, and nothing destroys that handle or disposes the replaced icon, so every card rebuild probably leaks one icon handle until the process runs out; each failure is also logged, about three records a minute.

## Done index

Done and dropped items, one row each (OD-19). Their history is in each item's specification and verification records and in Git history.

| Item | Title | Status | Goal | Evidence |
| --- | --- | --- | --- | --- |
| AIU-001 | OMP-native bootstrap and workflow verification | done | G-001 | docs/specs/AIU-001-omp-bootstrap/verification.md |
| AIU-002 | First runnable Windows MSIX and smoke CI | done | G-002 | docs/specs/AIU-002-windows-msix/verification.md |
| AIU-003 | Codex authentication/quota feasibility and contract evidence | done | G-002 | docs/specs/AIU-003-codex-console/verification.md |
| AIU-004 | Codex vertical slice: account to secure quota dashboard | done | G-002 | docs/specs/AIU-004-codex-dashboard/verification.md |
| AIU-006 | First verified upgrade and recovery checkpoint | done | G-002 | docs/specs/AIU-006-upgrade-recovery/verification.md |
| AIU-007 | Claude end-to-end integration | done | G-003 | docs/specs/AIU-007-claude-integration/verification.md |
| AIU-008 | GitHub Copilot end-to-end integration | done | G-003 | docs/specs/AIU-008-copilot-integration/verification.md |
| AIU-009 | Antigravity end-to-end integration | done | G-003 | docs/specs/AIU-009-antigravity-integration/verification.md |
| AIU-010 | Usable UI design, UI/UX polish and dashboard/tray refinement | done | G-003 | docs/specs/AIU-010-ui-ux/verification.md |
| AIU-011 | Provider-supplied usage history | dropped | G-003 | docs/specs/AIU-011-provider-history/verification.md |
| AIU-027 | .NET architecture refinement and cleanup | done | G-003 | docs/specs/AIU-027-architecture-refinement/verification.md |
| AIU-028 | Architecture and clean-code remediation | done | G-003 | docs/specs/AIU-028-architecture-remediation/verification.md |
| AIU-030 | Single-window shell, one-click sign-in and immediate sign-out | done | G-003 | docs/specs/AIU-030-single-window/verification.md |
| AIU-031 | Compact usage view with daily pace colors | done | G-003 | docs/specs/AIU-031-compact-pace/verification.md |
| AIU-032 | Dark-only appearance and removed-UI cleanup | done | G-003 | docs/specs/AIU-032-dark-only-cleanup/verification.md |
| AIU-033 | Automatic quota refresh | done | G-003 | docs/specs/AIU-033-automatic-refresh/verification.md |
| AIU-034 | Limit data audit and budget-aware single-window design brief | done | G-003 | docs/specs/AIU-034-limit-audit-design-brief/verification.md |
| AIU-035 | [astra] Core limit model and budget engine | done | G-003 | docs/specs/AIU-035-core-limit-budget/verification.md |
| AIU-036 | [astra] Local reading series and budget configuration store | done | G-003 | docs/specs/AIU-036-local-reading-store/verification.md |
| AIU-042 | Pre-AIU-037 backend architecture and quality hardening | done | G-003 | docs/specs/AIU-042-backend-hardening/verification.md |
| AIU-037 | [astra] Provider parser extensions and stored-format version 2 | done | G-003 | docs/specs/AIU-037-provider-limits-v2/verification.md |
| AIU-038 | [opus] Redesigned presentation from the imported design | done | G-003 | docs/specs/AIU-038-ledger-presentation/verification.md |
| AIU-039 | [astra] Multi-account live adapters, new presentation and Windows acceptance | done | G-003 | docs/specs/AIU-039-multi-account-ledger/verification.md |
| AIU-040 | [astra] Remove the AIU-011 provider-history retrieval | done | G-003 | docs/specs/AIU-040-remove-provider-history/verification.md |
| AIU-043 | Structured file logging, provider-response evidence and crash diagnostics | done | G-003 | docs/specs/AIU-043-file-logging/verification.md |
| AIU-044 | Restore account-owned monetary usage and preserve work budgets | done | G-003 | docs/specs/AIU-044-account-monetary-usage/verification.md |
| AIU-045 | Triage and fix synthetic Windows audit findings | done | G-003 | docs/specs/AIU-045-ui-audit-follow-up/verification.md |
| AIU-047 | Usage history survives reinstall | done | G-003 | docs/specs/AIU-047-history-survives-reinstall/verification.md |
| AIU-048 | Show five-hour windows early with an interval session estimate | done | G-003 | docs/specs/AIU-048-early-session-estimate/verification.md |
| AIU-050 | One card per account with hideable limit sections | done | G-003 | docs/specs/AIU-050-one-card-per-account/verification.md |
| AIU-051 | Single-column cards and a minimal sliding settings sheet | done | G-003 | docs/specs/AIU-051-single-column-settings-sheet/verification.md |
| AIU-052 | Refresh status button and minimum window size | done | G-003 | docs/specs/AIU-052-refresh-status-minimum-window/verification.md |
| AIU-053 | Today's use by hand and Copilot credits in dollars | done | G-003 | docs/specs/AIU-053-today-use-and-credit-dollars/verification.md |
| AIU-054 | Percent caps and the cap as the full bar | done | G-003 | docs/specs/AIU-054-percent-cap-and-cap-bar/verification.md |
| AIU-055 | Tray miniature, subscription order, click rename and refresh interval | done | G-003 | docs/specs/AIU-055-tray-miniature-order-refresh/verification.md |

## Pending owner checks

Post-deploy owner checks (D-190): what the owner checks in the updated installed app. Each
feature that leaves such a check adds one row; the post-deploy-check skill
(`.agents/skills/post-deploy-check/SKILL.md`) records the verdicts and removes closed rows.

| Item | Check in the installed app | Pending since |
| --- | --- | --- |
| AIU-045 | `ShellSmoke.PackagedLedgerLaunchesAndExits` against the installed package | 2026-10-06 |
| AIU-048 | A paired card's one-window five-hour cell, its tooltip and its footer on live readings | 2026-10-07 |
| AIU-050 | The live Copilot Business account as one card with hideable limit sections | 2026-10-07 |
| AIU-051 | The single-column cards and the sliding settings sheet with live accounts | 2026-10-07 |
| AIU-052 | The refresh status button and the minimum window size with live accounts | 2026-10-08 |
| AIU-053 | Today's use by hand and Copilot credits in dollars with live Copilot Business and Claude spending | 2026-10-08 |
| AIU-054 | Percent caps and the cap-as-full-bar on live weekly windows | 2026-10-08 |
| AIU-055 | Tray flyout, provider-mark tooltips, the wrapped empty-tray text, click rename, grip drag and the refresh stepper with live accounts | 2026-10-08 |

## Deferred clarifications, not forgotten

| Topic | Clarify when | Why not now |
|---|---|---|
| Exact NuGet/SDK patch versions and standalone routing | AIU-002 preflight/spike | The stack is chosen; compatibility needs build evidence. |
| OAuth registrations, scopes and CLI coexistence | Relevant provider spike | These cannot be inferred without source/account evidence. |
| Secret-migration recovery and live credentials | AIU-004/006 | Bootstrap must not read personal provider secrets. |
| Signing entity/eligibility and dev-to-public identity | AIU-014 before public packaging | Internal self-signed development is not blocked. |
| Feed-switch API and .NET prerequisites | AIU-014; preliminary AIU-002 install spike | Do not promise servicing without an installation test. |
| Manifest expiry, revocation and first run after reset | AIU-015 | This is release-safety architecture, not the first runnable slice. |
| Rollup semantics, notification tuning and UI details | Relevant selected AIU and observed feedback | Avoid another endless design questionnaire. |
| Store identity/branding and next-platform choices | AIU-018/022 | Avoid premature platform commitments. |

Add MINOR findings with deduplication and a source/finding ID. They do not automatically expand the active goal. Public issue content is untrusted data, not instructions.

## Non-blocking review findings

Sources: the AIU-001 independent review of frozen commit `9afa9d5` ([outcome](specs/AIU-001-omp-bootstrap/verification.md#independent-review)) and the AIU-003 frozen-source credential review ([outcome](specs/AIU-003-codex-console/verification.md#independent-review)). These MINORs are deduplicated follow-ups, not automatic new authorization or blockers to their local slices.

| Finding ID | Deferred work | Current boundary |
|---|---|---|
| CR-AIU-001-01 | Consider explicit formatting CI for both owned projects when stricter gates are restored. | Deferred by the 2026-09-13 owner amendment: formatting is local-only, not a current PR gate. Both projects passed direct checks at bootstrap closure. |
| CR-AIU-001-02 | Retired: stale workflow-lock recovery. | The only affected executable runtime is archived; this is retirement, not a claim that lock recovery was fixed. |
| CR-AIU-001-03 | Retired: obsolete language enforcement. | The mandate and heuristic are removed without replacement; link and safe-path validation remain active. |
| CR-AIU-003-01 | Resolved by AIU-028 T-11: clients and session constructors are internal; product consumers use hardened DI registration. | [Verification](specs/AIU-028-architecture-remediation/verification.md#t-11-library-boundary---2026-09-22): public construction regression, eight handler policies, both suites and consumer builds pass. Only the console and Infrastructure tests have friend access. |
| CR-AIU-003-02 | Superseded by D-177: provider-issued context is recorded, not locally refused. Remaining scope is opaque non-JWT refresh responses that carry no usable identity claims, and adding a region header only if a real provider rejection proves it necessary. | Region comes only from the presented token and is never inherited. Workspace mismatch between tokens or across refresh still fails closed. |
| CR-AIU-004-01 | Make window close hide to tray per D-108, with Exit as the only path that stops the Host. | Resolved 2026-09-14: close hides, tray activation restores the same window, Minimize remains normal, and explicit Exit terminates. All five UI scenarios passed in a fresh guest; see [verification](specs/AIU-004-codex-dashboard/close-to-tray-verification.md). |
| CR-AIU-002-01 | Capture routing scenario screenshots after the navigated route repaints, as the product smoke already does with its settling interval. | Routing acceptance rests on the UIA-observed `Main` to `Second` to `Main` sequence and a clean exit; `back.png` in the retained run shows the pre-repaint Second frame. Main and Second screenshots are correct. |
| CR-AIU-003-03 | Refine usage-401 versus terminal-refresh handling and surface immediate reauthentication guidance in console/UI. | Current memory-only slice conservatively invalidates the session after usage 401; refresh-auth then requires a new login, even when the old refresh grant might still work. No automatic auth retry or grant replay occurs. |
