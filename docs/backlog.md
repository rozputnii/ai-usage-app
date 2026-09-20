---
schema_version: 1
---
# Canonical backlog

Statuses: idea / research-needed / blocked / ready / selected / in-progress / paused / review / done / dropped.

**AIU items are coherent outcomes; T items are internal execution plans.** The order below is an initial sequence, not a fabricated numeric ranking. The owner selects work under CONTRIBUTING.md; no status authorizes execution. Research-needed denotes unresolved research/spike scope. Provider order is preferred delivery order, not a reason to stop independent work when one provider is blocked. A ready label does not override unmet dependencies.

## AIU-001 - OMP-native bootstrap and workflow verification
- goal: G-001
- status: done
- depends_on: []
- trigger: now
- outcome: Historical bootstrap established instructions, validation and native workflow evidence. The executable bridge is now retired; its observed results remain history.
- evidence: docs/specs/AIU-001-omp-bootstrap/verification.md

## AIU-002 - First runnable Windows MSIX and smoke CI
- goal: G-002
- status: done
- depends_on: [AIU-001]
- trigger: next
- outcome: Create the WinUI/.NET 10/Host three-project skeleton, empty dashboard, packaged launch/exit, basic CI and a standalone native-routing spike. Do not build the entire shell upfront.
- evidence: docs/specs/AIU-002-windows-msix/verification.md
- outcome-note: Closed on 2026-09-14 after final evidence review. A signed development MSIX installed in a clean disposable guest, the offline packaged UI passed three inspected launch/exit scenarios, and the native routing spike navigated Main to Second and back under Microsoft.WinUI, exiting cleanly. Genuine missing-framework and missing-runtime negatives were observed rather than simulated. Remote GitHub Actions execution and interactive UI CI remain NOT_RUN by authorization; host installation and host certificate trust were never performed.

## AIU-003 - Codex authentication/quota feasibility and contract evidence
- goal: G-002
- status: done
- depends_on: [AIU-001]
- trigger: next
- outcome: Run a console-driven provider/library spike covering browser/device availability, scopes, redirects, quota groups and coexistence of rotating CLI credentials; retain sanitized evidence. This research does not depend on WinUI readiness.
- evidence: docs/specs/AIU-003-codex-console/verification.md
- outcome-note: Live-verified on 2026-09-14. The console completed a real browser sign-in, read this account's actual subscription quota, refreshed in memory and read quota again, exiting cleanly. Connection and usage mirror the locally cloned OMP implementation per D-177. Remaining provider scope - device login, multi-workspace, exhausted/rate-limited responses, long-term rotation and CLI coexistence - is NOT_RUN and belongs to AIU-004/005. Public-client reuse permission remains unknown.

## AIU-004 - Codex vertical slice: account to secure quota dashboard
- goal: G-002
- status: done
- depends_on: [AIU-003]
- trigger: after-evidence
- outcome: First implement the reusable UI-independent Codex integration library and verify its real account, quota, refresh and reauthentication behavior through a console application. Reuse that implementation for the secure quota dashboard, minimum cache/state, refresh/errors and tray integration afterward. Preserve DPAPI and multi-account identity boundaries; no duplicate console/UI provider clients.
- evidence: docs/specs/AIU-004-codex-dashboard/verification.md
- outcome-note: Closed on 2026-09-14. One Codex account persists under DPAPI CurrentUser in the app-owned LocalState root; the dashboard resumes it, renders quota with connect, refresh and disconnect, keeps unknown values distinct from zero, shows the last cached reading with an explicit staleness notice, and has tray presence with Open and Exit. The single provider client is reused; the UI adds no provider request. Verified by 72 deterministic tests and two clean-guest runs with inspected screenshots. Connecting a real account from the packaged UI is NOT_RUN: the guest has no networking and host installation is unauthorized. Close-to-tray from D-108 was completed as CR-AIU-004-01; see the follow-up verification record.

## AIU-005 - Codex CLI and other provider CLI integrations
- goal: G-003
- status: research-needed
- priority: low
- depends_on: [AIU-003, AIU-004, AIU-007, AIU-008, AIU-009, AIU-010]
- trigger: after-all-provider-connections-and-ui-polish
- outcome: Research and implement Codex and other supported providers' CLI discovery and import. Scan known Windows locations asynchronously, show progressive candidates and import individual/all/re-import where supported by evidence. Never mutate source stores; enable only a proven-safe token lifecycle.
- sequencing-note: Owner amendment, 2026-09-14: defer CLI integrations until all four providers connect using the same connection style as OMP (omp.sh), and a usable UI design and UI/UX polish are complete. CLI import is no longer the next task after the Codex slice. Reading or importing real source CLI credentials still requires explicit current authorization.

## AIU-006 - First verified upgrade and recovery checkpoint
- goal: G-002
- status: idea
- depends_on: [AIU-004]
- trigger: before-external-data
- outcome: Test real old/new development MSIX packages with durable data, a versioned journal, consistent backup, deliberately interrupted migration and recovery.

## AIU-007 - Claude end-to-end integration
- goal: G-003
- status: done
- depends_on: [AIU-004, AIU-027]
- trigger: provider-2
- outcome: Research provider policy, authentication and grouped subscription quotas; deliver the shared library and Windows connect/resume/refresh/disconnect/reauthentication flows from evidence. Preserve Codex behavior and AIU-027 boundaries; CLI import is excluded from this task.
- evidence: docs/specs/AIU-007-claude-integration/verification.md
- scope-note: The owner explicitly authorized a private, unsupported OMP-style integration on 2026-09-14 despite the documented restriction. Provider approval remains unestablished. Completed 2026-09-15 with independent review, deterministic and packaged Windows checks, and owner-led live connection/refresh/renewal/resume/disconnect verification. Integrated into local main; the owner's subsequent commit-and-push request authorizes repository publication, as recorded in the spec and verification.

## AIU-008 - GitHub Copilot end-to-end integration
- goal: G-003
- status: done
- depends_on: [AIU-004]
- trigger: provider-3
- outcome: Use an app-owned client where supported; preserve subscription credit/request entitlement semantics and evidence-based context/import capabilities.
- evidence: docs/specs/AIU-008-copilot-integration/verification.md
- scope-note: Owner-selected OMP-only public GitHub device flow, request-quota snapshots and protected local lifecycle implemented and live verified on 2026-09-18 in codex/aiu-008-copilot-integration. Provider approval, Included credits parity, paid/organization contexts and enterprise hosts are not established. AIU-010 remains paused. See the specification and verification for exact scope and evidence.

## AIU-009 - Antigravity end-to-end integration
- goal: G-003
- status: research-needed
- depends_on: [AIU-004]
- trigger: provider-4
- outcome: Prove authentication, projects, model groups and remote-versus-official quota parity. Do not combine Gemini app, CLI and API limits.

## AIU-010 - Usable UI design, UI/UX polish and dashboard/tray refinement
- goal: G-003
- status: in-progress
- depends_on: [AIU-004]
- trigger: incremental
- outcome: Add manual ordering/labels, Overview, group/hide controls, themes, accessibility, localization resources and shared UI state.
- evidence: docs/specs/AIU-010-ui-ux/verification.md
- pause-note: Owner paused this feature on 2026-09-18 to select AIU-008. Remaining T-11 live lifecycle, screen-reader and overall visual acceptance gates are preserved; the published snapshot is not feature completion.
- resume-note: Owner resumed AIU-010 on 2026-09-18 after AIU-008 was merged. Continue T-11 on codex/aiu-010-resume-acceptance from f4d0fbf; prior acceptance limits remain until new evidence closes them.
- scope-note: Owner selected staged Codex preparation, Claude Design, Claude Code frontend and Codex integration on 2026-09-15. Prepare the complete planned Windows UI with isolated interactive mocks, including future capabilities; real backend delivery remains in its owning AIUs. See docs/specs/AIU-010-ui-ux/spec.md and tasks.md. Preparation completion does not complete the feature.
- owner-direction: Owner amendment, 2026-09-14: the current UI is unattractive and not usable. Apply a coherent, usable UI design and polish core provider connection, quota, refresh and tray flows before CLI integrations. Existing functional smoke evidence does not establish satisfactory design or usability. All four provider connections should follow the OMP (omp.sh) connection style; exact design and provider-specific behavior will be established when that work is selected.

## AIU-011 - Usage history and responsive charts
- goal: G-003
- status: idea
- depends_on: [AIU-004]
- trigger: incremental
- outcome: Implement normalized history, reset/gap-aware retention and rollups, LiveCharts2 sparklines, History and custom ranges.

## AIU-012 - Threshold notifications and OS-aware refresh
- goal: G-003
- status: idea
- depends_on: [AIU-004]
- trigger: incremental
- outcome: Implement threshold inheritance, hysteresis, deduplication, aggregated activation and power/network/sleep/lock behavior with targeted tests.

## AIU-013 - Diagnostics, portable Replace import and resets
- goal: G-003
- status: idea
- depends_on: [AIU-006]
- trigger: incremental
- outcome: Implement local sanitized diagnostics, diagnose-only repair UX, plain export, safe staged Replace, settings reset and factory reset.

## AIU-014 - Trusted direct Preview/Stable distribution
- goal: G-004
- status: research-needed
- depends_on: [AIU-006]
- trigger: before-public
- outcome: Verify signing eligibility/identity and runtime prerequisite delivery. Implement monotonic version allocation, Releases/Pages/App Installer, channel switching and nonblocking updates.

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
- depends_on: [AIU-011]
- trigger: after-history
- outcome: Prove forecasting with sufficient history and reset/gap handling. Clearly label estimates; do not affect factual quota alerts.

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

## AIU-027 - .NET architecture refinement and cleanup
- goal: G-003
- status: done
- depends_on: [AIU-004]
- trigger: before-AIU-007
- outcome: Refine the existing Core, Infrastructure and Windows projects into clear, testable boundaries organized by feature.
- scope: Reduce concrete provider dependencies in view models; separate application workflows from transport/storage; clarify desktop service lifetimes, cancellation, dispatcher access and shutdown ownership.
- preserve: Current authentication behavior, stored-data formats, quota semantics and close-to-tray behavior.
- excludes: New providers, UI redesign, CLI integrations and speculative frameworks.
- acceptance: Independent application-workflow and view-model tests; enforced dependency boundaries; passing existing regression checks; actual packaged Windows lifecycle verification.
- evidence: docs/specs/AIU-027-architecture-refinement/verification.md
- outcome-note: Completed 2026-09-14. Core owns a credential-free dashboard workflow; Infrastructure retains authentication/storage behavior; Windows owns presentation and awaited dispatcher/lifetime handling. Verified by 162 deterministic regressions, independent review and all five packaged lifecycle scenarios in a clean guest. Stored formats, quota semantics and close-to-tray are preserved. Live packaged account sign-in and remote CI remain NOT_RUN.
- sequencing-note: Owner amendment, 2026-09-14: prioritize this intermediate task before AIU-007. This entry records the task only; refactoring starts separately under CONTRIBUTING.md.

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
| CR-AIU-003-01 | Before UI/persistent consumption, enforce hardened HTTP construction at the library boundary rather than relying on DI composition. | Current console uses fixed HTTPS origins, disabled redirects/cookies/logging through AddCodexIntegration. Public constructors trust consumer-supplied HttpClient pipelines; the provider index documents that requirement. |
| CR-AIU-003-02 | Superseded by D-177: provider-issued context is recorded, not locally refused. Remaining scope is opaque non-JWT refresh responses that carry no usable identity claims, and adding a region header only if a real provider rejection proves it necessary. | Region comes only from the presented token and is never inherited. Workspace mismatch between tokens or across refresh still fails closed. |
| CR-AIU-004-01 | Make window close hide to tray per D-108, with Exit as the only path that stops the Host. | Resolved 2026-09-14: close hides, tray activation restores the same window, Minimize remains normal, and explicit Exit terminates. All five UI scenarios passed in a fresh guest; see [verification](specs/AIU-004-codex-dashboard/close-to-tray-verification.md). |
| CR-AIU-002-01 | Capture routing scenario screenshots after the navigated route repaints, as the product smoke already does with its settling interval. | Routing acceptance rests on the UIA-observed `Main` to `Second` to `Main` sequence and a clean exit; `back.png` in the retained run shows the pre-repaint Second frame. Main and Second screenshots are correct. |
| CR-AIU-003-03 | Refine usage-401 versus terminal-refresh handling and surface immediate reauthentication guidance in console/UI. | Current memory-only slice conservatively invalidates the session after usage 401; refresh-auth then requires a new login, even when the old refresh grant might still work. No automatic auth retry or grant replay occurs. |
