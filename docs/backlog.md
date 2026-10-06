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
- outcome: Research, then implement where evidence supports it, two separate ways of using locally installed provider CLIs (Codex, Claude Code, Copilot CLI and the Antigravity or Gemini CLI). (A) Credential import - discover CLI sign-ins in known Windows locations asynchronously, show progressive candidates and import individual/all/re-import, so the app uses the imported grant with its own provider transport. Never mutate source stores; enable only a proven-safe token lifecycle, including refresh-token rotation shared with the CLI. (B) CLI as a usage source - the app runs the installed CLI as a local process and reads usage, limits, resets and account context from its documented or observed non-interactive output or protocol, while the CLI keeps its own credentials and the app never reads them. Research each method per provider and per account type and classifies it separately: the command or protocol, the fields available relative to the AIU-034 limit matrix, whether a reading needs an inference request (disallowed), freshness, process cost, version drift, and failure and signed-out states. Method B is the preferred first implementation candidate because the app never handles CLI credentials.
- sequencing-note: Owner amendment, 2026-09-14: defer CLI integrations until all four providers connect using the same connection style as OMP (omp.sh), and a usable UI design and UI/UX polish are complete. CLI import is no longer the next task after the Codex slice. Reading or importing real source CLI credentials still requires explicit current authorization.
- scope-note: Owner amendment, 2026-09-26: the scope now covers both method A and method B. Start with a research gate that answers both methods for every provider before any implementation is selected. The work starts after the AIU-034 redesign and its implementation items, as a separate item; it is not selected by this note. AIU-034's normalized limit model is kept independent of the snapshot source (A-6) so that a CLI source needs no model or UI rework. Method A requires the security-lifecycle review. Method B requires evidence that no inference request, new grant or source-store mutation occurs.

## AIU-006 - First verified upgrade and recovery checkpoint
- goal: G-002
- status: done
- depends_on: [AIU-004]
- trigger: before-external-data
- outcome: Test real old/new development MSIX packages with durable data, a versioned journal, consistent backup, deliberately interrupted migration and recovery.
- specification: docs/specs/AIU-006-upgrade-recovery/spec.md
- evidence: docs/specs/AIU-006-upgrade-recovery/verification.md
- scope-note: Owner selected automatic execution on 2026-09-22. The first migration moves existing presentation preferences into an explicitly versioned layout, with protected checkpoint and exclusive startup/recovery. Provider credential formats and locations remain unchanged; no live sign-in or source credential import is required.
- outcome-note: Completed 2026-09-22. Real same-family MSIX update from 2026.9.2202.0 to 2026.9.2222.0 in Windows Sandbox preserves synthetic preferences and DPAPI provider records. Actual interrupted migration, restart, explicit retry, confirmed restore, newer-layout refusal, sanitized export and folder actions pass. Infrastructure 330/330, Presentation 157/157, Release build and focused independent review pass. Physical power loss, live-provider access and public distribution are not established by this evidence.

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
- status: done
- depends_on: [AIU-004]
- trigger: provider-4
- outcome: Prove authentication, projects, model groups and remote-versus-official quota parity. Do not combine Gemini app, CLI and API limits.
- evidence: docs/specs/AIU-009-antigravity-integration/verification.md
- scope-note: Owner selected AIU-009 on 2026-09-20 with an OMP-only implementation, live verification on their own Google account, and no CLI credential import. The legacy model-catalog quota fallback, the sandbox host and CLI import are excluded. Free-tier provisioning through onboardUser was excluded at first and then included by owner decision, fenced to the connect path; see docs/specs/AIU-009-antigravity-integration/spec.md.
- outcome-note: Closed on 2026-09-20. Live PASS for connect, quota, refresh with renewal, resume in a new process, the Windows product UI and local disconnect, with two provider groups and weekly-only windows matching the published plans page for that tier. Two owner decisions shape the result and both carry real cost. Google's published FAQ states that third-party access to Antigravity violates its Terms of Service and may be grounds for account suspension, and the owner chose to proceed on their main account. The provider then refused a truthfully identified client outright, and the owner directed that the Cloud Code Assist control plane be sent the real Antigravity client's User-Agent; that single change unblocked it, proving the gate was the client identity. That is a recorded exception to the rule kept for every other provider, scoped to this control plane. Provider approval remains unestablished. Five-hour buckets, paid tiers, AI credits, rate limiting, revocation and packaged activation with a connected account are NOT_RUN. The OAuth client registration is not vendored: the operator supplies one per device.

## AIU-010 - Usable UI design, UI/UX polish and dashboard/tray refinement
- goal: G-003
- status: done
- depends_on: [AIU-004]
- trigger: incremental
- outcome: Add manual ordering/labels, Overview, group/hide controls, themes, accessibility, localization resources and shared UI state.
- evidence: docs/specs/AIU-010-ui-ux/verification.md
- outcome-note: Closed on 2026-09-20 by owner direction. The owner reported that Claude works and, after being told the remaining gates were not observed in this session, explicitly directed that they be marked complete. Agent-observed evidence covers the design, mock frontend, live product adapters, Presentation 117/117, Infrastructure 131/131, product and demo Windows smoke 7/7, unpackaged Release build, unsigned MSIX 2026.9.1634.0, main CI run 35401416353, the full live Codex lifecycle and two Narrator speech-recap subsets. The live Claude lifecycle, full screen-reader acceptance and overall owner visual acceptance are recorded as OWNER_ATTESTED, not agent-observed PASS. Provider approval for the private OMP-style integrations remains unestablished, multi-account remains unavailable, and the original Codex reconnect report remains NOT_REPRODUCED.
- pause-note: Owner paused this feature on 2026-09-18 to select AIU-008. Remaining T-11 live lifecycle, screen-reader and overall visual acceptance gates are preserved; the published snapshot is not feature completion.
- resume-note: Owner resumed AIU-010 on 2026-09-18 after AIU-008 was merged. Continue T-11 on codex/aiu-010-resume-acceptance from f4d0fbf; prior acceptance limits remain until new evidence closes them.
- scope-note: Owner selected staged Codex preparation, Claude Design, Claude Code frontend and Codex integration on 2026-09-15. Prepare the complete planned Windows UI with isolated interactive mocks, including future capabilities; real backend delivery remains in its owning AIUs. See docs/specs/AIU-010-ui-ux/spec.md and tasks.md. Preparation completion does not complete the feature.
- owner-direction: Owner amendment, 2026-09-14: the current UI is unattractive and not usable. Apply a coherent, usable UI design and polish core provider connection, quota, refresh and tray flows before CLI integrations. Existing functional smoke evidence does not establish satisfactory design or usability. All four provider connections should follow the OMP (omp.sh) connection style; exact design and provider-specific behavior will be established when that work is selected.

## AIU-011 - Provider-supplied usage history
- goal: G-003
- status: dropped
- priority: low
- review-note: Owner direction, 2026-09-22: this feature still needs verification; lower its priority and defer further checks until selected again. Existing implementation and observed live results are retained, but the feature is no longer marked complete.
- depends_on: [AIU-004, AIU-007, AIU-008, AIU-009, AIU-010]
- trigger: owner-selected
- outcome: Retrieve and display available historical usage supplied by Codex, Claude, Copilot and Antigravity, preserving native metrics, units, periods and access limitations. Open History with automatic loading and useful defaults; account/range selection is optional refinement, not a required sequence before seeing data.
- scope-note: Owner selected and narrowed this feature on 2026-09-22 to provider-supplied history, confirmed all four providers and all available historical usage metrics, and requested fewer clicks. The subsequent owner amendment permits only existing authorization and directs matching OMP's provider-history method if one exists. No new sign-in, browser session, API key or reporting permission is in scope. Local sample collection, retention, rollups and persistent history remain deferred to AIU-029 with low priority after product stability.
- direction-note: Closed as superseded by D-184 (2026-09-29). Local tracking is the sole history source. The owner selected AIU-040 on 2026-10-03 to remove this retrieval after AIU-039; earlier incomplete verification remains historical evidence, not a completion claim.
- research-note: OMP v18.2.8 has only local snapshot history. Codex development-source analytics and GitHub personal historical reports are now implemented with existing sessions and automatic Windows presentation. Claude and Antigravity remain unsupported because no compatible transport is established. Deterministic and interactive evidence is recorded separately from live access.
- live-note: On 2026-09-22 the owner explicitly authorized automatic use of the existing browser login for verification. Normal Codex and Copilot product connections succeeded without added scopes. Codex real daily usage/activity/plugin/skill history was fetched and displayed, satisfying AC-02. Optional Codex workspace routes returned 400/403; Copilot personal history routes returned 404 with the current connection. These outcomes do not establish universal plan coverage or the reason for GitHub's 404. No cookies or source CLI credentials were extracted; local history remains deferred.
- specification: docs/specs/AIU-011-provider-history/spec.md
- evidence: docs/specs/AIU-011-provider-history/verification.md

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
- scope-note: Owner selected an initial development-only phase on 2026-09-22: publish every successful main push and automatically update the owner's test installations. Use self-signed CI packages with one-time explicit tester trust; official signing/public distribution and Stable remain deferred. See docs/specs/AIU-014-preview-updates/spec.md. That phase was operational on 2026-09-23: signed Previews publish from main, and a Sandbox feed install then updated automatically. Official signing, Stable, channel switching and public distribution remain open. Public signing research draft: docs/specs/AIU-014-public-signing/spec.md. Owner decision 2026-09-24: personal tool; public signing, Store and Stable deferred (paused). Owner decision 2026-10-06 (AIU045-D1): Previews publish only through an explicit owner workflow_dispatch of a green main commit, replacing the 2026-09-22 publish-every-push rule; pushes to main, including save points, never publish.
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

## AIU-028 - Architecture and clean-code remediation
- goal: G-003
- status: done
- depends_on: [AIU-027]
- trigger: after-audit
- outcome: Fix the evidence-backed defects and duplication recorded by the 2026-09-20 architecture and clean-code audit, so that adding a fifth provider is an additive change and a failure is distinguishable from a provider outage.
- scope: Share the duplicated provider transport, error translation, exception type and DPAPI state-lease code; bring the Codex grant store onto the same hardened lease; remove the Codex-only session contract and its string-round-tripped enum mapping; stop flattening unclassified exceptions into a provider fault; add redacted structured logging; consolidate the six provider registries; add shared MSBuild and central package version roots and raise the analyzer set; fix the throwing Dispose, the callback-under-lock publish, the one reflection-based JSON site and the always-on UI clock.
- preserve: Authentication behavior, stored-data file names, DPAPI entropy strings and record shapes, quota semantics, close-to-tray behavior, the AIU-027 boundaries, and `RemoveAllLoggers` on provider transports.
- excludes: New providers, UI redesign, CLI integrations, new runtime dependencies, SDK or package version upgrades, and any change to product intent or stored-data formats.
- acceptance: See docs/specs/AIU-028-architecture-remediation/spec.md AC-01 onward. Existing deterministic regressions, document validation and a warnings-visible desktop build continue to pass; interactive Windows smoke is required for the clock-gating task.
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md
- audit-note: Recorded 2026-09-20 against `main` at `6681b7a` by an analysis-only session under docs/workflow/architecture-audit-plan.md, which changed no production code. Fifteen findings F-01..F-15 with `path:line` evidence are in the specification; areas verified clean, including `x:Bind` coverage, handler symmetry and the mitigated typed-`HttpClient` singleton capture, are recorded as accepted-as-is rather than as findings. AIU-027 settled the Core/Infrastructure/Windows split and the source-linked presentation test project; this entry reports drift and debt accumulated since, and does not re-propose that structure.
- completion-note: All twelve tasks are done. Earlier required independent review and Windows smoke evidence remain recorded in verification.md. T-11 (9f4d347) and T-12 (5095a1d) were assessed for relevance and completed sequentially without subagents on 2026-09-22; final Infrastructure 286/286, Presentation 147/147, console/Windows builds, document validation and primary review pass. No new live-provider, interactive UI or release evidence is claimed for these two tasks.
- prior-work: AIU-027 (.NET architecture refinement and cleanup); F-14 resumes the deferred non-blocking finding CR-AIU-003-01, whose "before UI/persistent consumption" boundary is now crossed.

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

## AIU-030 - Single-window shell, one-click sign-in and immediate sign-out
- goal: G-003
- status: done
- depends_on: [AIU-010]
- trigger: owner-selected
- outcome: One usage window without tabs and a Settings icon on the right that shows every settings section, System status included. Add account opens a provider menu on hover; one provider click starts browser sign-in with inline progress and no dialog or confirmation, and success simply adds the account. Each account panel has an icon-only sign-out that disconnects immediately and keeps history.
- specification: docs/specs/AIU-030-single-window/spec.md
- evidence: docs/specs/AIU-030-single-window/verification.md
- scope-note: Owner request, 2026-09-24, recorded as D-180. Provider connection flows, credential storage and D-093 retention are unchanged; only the presentation around them changes.
- review-note: Implemented and verified with deterministic tests, demo and empty-product Windows smoke and interactive demo checks on 2026-09-24. A live provider sign-in and sign-out through the new menu and panel button are NOT_RUN and await the owner's own check; the tray smoke rows were not run locally because the owner's instance was running.
- closure-note: Owner decision, 2026-09-29: closed. The owner reports that live sign-in and sign-out through the new menu and panel button work (owner's own check, not agent-observed). The remaining NOT_RUN rows (tray smoke rows, packaged harnesses, screen-reader acceptance) are not repeated on a UI that the AIU-034 redesign replaces; they are deferred to its implementation items.

## AIU-031 - Compact usage view with daily pace colors
- goal: G-003
- status: done
- depends_on: [AIU-030]
- trigger: owner-selected
- outcome: The usage view shows only the limit bars, one line per account with window names once per provider and details on hover. Bar color is pace advice: weekly and longer windows split the remainder into even daily shares and turn orange, then red, as today's share runs out; windows shorter than a day turn red at 20 % or less.
- specification: docs/specs/AIU-031-compact-pace/spec.md
- evidence: docs/specs/AIU-031-compact-pace/verification.md
- scope-note: Owner requests, 2026-09-24, recorded as D-181. Advice only; readings, detail, the tray and notification thresholds stay factual. History-based forecasting remains AIU-024.
- review-note: Implemented and verified with deterministic tests, demo and empty-product Windows smoke and interactive demo checks on 2026-09-24. Pace colors on live provider readings and the tray smoke rows are NOT_RUN.
- closure-note: Owner decision, 2026-09-29: closed. Pace colors on live readings are not verified because the AIU-034 work-day budget replaces them. The remaining NOT_RUN rows are deferred to the AIU-034 implementation items.

## AIU-032 - Dark-only appearance and removed-UI cleanup
- goal: G-003
- status: done
- depends_on: [AIU-031]
- trigger: owner-selected
- outcome: The app always uses the dark palette with no theme setting, and code, styles, strings and demo controls left over from removed screens and themes are deleted.
- specification: docs/specs/AIU-032-dark-only-cleanup/spec.md
- evidence: docs/specs/AIU-032-dark-only-cleanup/verification.md
- scope-note: Owner request, 2026-09-24, recorded as D-182. The account list beside account detail stays.
- review-note: Implemented and verified with deterministic tests, demo and empty-product Windows smoke including the tray rows, and interactive demo checks on 2026-09-24. Rendering under Windows light mode or a contrast theme and the packaged harnesses are NOT_RUN.
- closure-note: Owner decision, 2026-09-29: closed. Rendering under Windows light mode or a contrast theme and the packaged harnesses are deferred to the AIU-034 implementation items, which keep dark-only (D-182) and verify it on the new interface.

## AIU-033 - Automatic quota refresh
- goal: G-003
- status: done
- depends_on: [AIU-004]
- trigger: owner-selected
- outcome: Connected accounts are read again every five minutes with failure backoff, sign-in states are left to the user, and readings nobody renewed for 15 minutes show as stale.
- specification: docs/specs/AIU-033-automatic-refresh/spec.md
- evidence: docs/specs/AIU-033-automatic-refresh/verification.md
- scope-note: Owner request, 2026-09-25, after a Codex limit reset stayed invisible for hours. Minimal slice of D-099 split from AIU-012.
- review-note: Deterministic tests and an empty-state Release run pass. Automatic refresh of a real account in the installed package is NOT_RUN.
- closure-note: Owner decision, 2026-09-29: closed. Automatic refresh of a real account in the installed package is deferred to the AIU-034 implementation items, which rework the live adapters and whose local reading series depends on this refresh.

## AIU-034 - Limit data audit and budget-aware single-window design brief
- goal: G-003
- status: done
- depends_on: []
- trigger: owner-selected
- outcome: Phase A audits every limit each provider and plan can report and proposes a normalized limit and daily-budget model with work days, personal caps and five-hour session estimates; after owner review, Phase B writes a Claude Design brief for a single-window redesign with its own visual identity. Ends with proposed implementation items; no product code changes.
- specification: docs/specs/AIU-034-limit-audit-design-brief/spec.md
- evidence: docs/specs/AIU-034-limit-audit-design-brief/verification.md
- scope-note: Owner request and answers, 2026-09-26, recorded as D-183. API-key billing is excluded. Phase A starts only on the owner's instruction after the specification review.
- gate-note: Gate A passed on 2026-09-29 (D-185): Phase A is complete and accepted. The owner approved the Phase B plan on 2026-09-29, and Phase B started with T-12. Gate B passed on 2026-09-29: the owner approved design-brief.md, and AIU-035 to AIU-040 are proposed as its follow-up items, none of them selected. Phase B completed on 2026-10-02: the owner accepted the Claude Design reference (D-186), which is imported in docs/specs/AIU-034-limit-audit-design-brief/design-reference/; AIU-041 was added. None of AIU-035 to AIU-041 is selected.
- amendment-note: Owner amendment, 2026-10-02 (D-187): the design reference was updated and re-imported with the tray miniature, no OK pill, the neutral day-off share with Work today, the last-work-day rush and the on-extra-usage mark. The status stays done; AIU-038 implements the amended reference.

## AIU-035 - [astra] Core limit model and budget engine
- goal: G-003
- status: done
- depends_on: []
- trigger: owner-selection
- outcome: Credential-free Core types and pure computations for the AIU-034 limit model and budget rules, with no UI, persistence or provider transport. The model covers limit kinds and units; money as minor units, exponent and currency; limit values that are unknown, explicit null, unlimited or zero; the personal cap, effective limit and binding source; and resets from the provider, derived or assumed, including period unknown. The engine computes the work-day calendar; `N`, `T`, `B`, the deviation, used today and left today; limit, budget and account states; display rounding; the day-start amount and tracked consumption over supplied readings; and the five-hour session estimator. Sources: research 5.2 to 5.4, 6.3 to 6.5, 7 and 8.
- acceptance: (1) Research section 9 cases E01 to E13, P01 to P11 and S01 to S09 are deterministic Core tests with exact decimal arithmetic, and all pass. (2) The design-brief section 4 scenario reproduces every section 4.2 figure, every 4.3 bar position and every 4.4 account status. (3) Unknown is never zero or unlimited; no value is summed or converted across units or currencies; a percentage window never takes a cap. (4) Core keeps its boundary: no credential, transport, file or UI dependency.
- supersedes: As computation rules, D-181's even local-calendar-day shares with carry-over, its 20 % floor for windows shorter than a day, and its single account status mark. They are replaced by the work-day adaptive norm with baseline and deviation, the R-09 thresholds and the research 8.8 account status. The current rules stay in the product until AIU-039 switches it (D-183). No part of D-180.
- contract-note: The engine's outputs feed the presentation contract through AIU-039. A change to that contract is agreed between the [opus] AIU-038 item and the [astra] items, never made silently from either side.
- selection-note: Owner selection, 2026-10-02: AIU-035 is the next step, to be done by a Codex agent after the AIU-034 D-187 design round. It starts with a specification under docs/specs/ per CONTRIBUTING.md; this note selects the item and does not approve that specification.
- specification: docs/specs/AIU-035-core-limit-budget/spec.md
- evidence: docs/specs/AIU-035-core-limit-budget/verification.md
- d187-note: D-187 (2026-10-02) adds engine outputs that the presentation needs: (a) on a day off, today's would-be share T' = max(0, L - U0) / (Wr + 1), where L is the effective limit and Wr is the remaining work-day weight after today (research 8.2), as if today were one full work day, with the work-day calendar unchanged, so the same figure serves the neutral strip and the window-wide Work today switch, which only changes colouring until local midnight; (b) a used-up flag when U >= L for a provider limit (not a custom cap), which wins over every today and rush state; (c) a rush flag on the last work day before a reset with reset meaning replenish from the provider and no custom cap set, never for monetary pools or credit balances, with today's share equal to the whole remainder and, for 5h + 7d, the count of 5h windows that fit before the reset; (d) an on-extra-usage flag while a Claude 5h or 7d window is full and the extra-usage spend rose since the window filled, with the spend since then. R-05, R-11 as amended and research section 8 stay the base rules.
- source: docs/specs/AIU-034-limit-audit-design-brief/research.md

## AIU-036 - [astra] Local reading series and budget configuration store
- goal: G-003
- status: done
- depends_on: [AIU-033, AIU-035]
- specification: docs/specs/AIU-036-local-reading-store/spec.md
- evidence: docs/specs/AIU-036-local-reading-store/verification.md
- outcome-note: Completed 2026-10-02. Versioned bounded observation/configuration stores, successful-refresh recording, 35-day retention, recovery reporting and owned-data cleanup are implemented. Persisted P01-P11 and session estimates pass; Infrastructure 416/416, Presentation 179/179, Windows build/smoke and focused lifecycle review with resolved findings are recorded. Owner clarification limits cleanup to these stores; wiring currently unsupported product delete/reset buttons is deferred. Live-provider recording is NOT_RUN; the new presentation remains AIU-038/039.
- trigger: owner-selection
- outcome: Every automatic or manual refresh writes the local reading series of research 6.2 to 6.6. It is the only history source (D-184), is kept for at least 35 days, and supplies the day-start amount, tracked consumption, estimator samples and inline history. A new budget configuration file holds the work days (Monday to Friday by default) and the personal caps, including unmatched caps and currency mismatches (research 5.4, 5.7).
- acceptance: (1) A security-lifecycle review of both stores is recorded before merge. It covers app-owned storage, owned-root cleanup, sign-out retention under D-093, factory reset, forward migration and corrupt-file recovery (research 6.6 precondition). (2) Both stores are versioned and size-bounded, with staged replace and reparse-point checks, and are separate from provider state and appearance preferences. (3) Sign-out keeps the series and the caps; Delete stored data and factory reset remove them. A corrupt store is set aside and a new series starts, with the losses research 6.6 lists. (4) No credential, raw payload or identity beyond the existing account target ID is stored. (5) Research cases P01 to P11 pass on a persisted round trip of the series.
- supersedes: No part of D-180 or D-181. It supplies the local history that replaces D-180's History page through AIU-038 and AIU-039.
- contract-note: Series and configuration data reach the presentation contract only through AIU-039. A change to that contract is agreed between the [opus] AIU-038 item and the [astra] items, never made silently from either side.
- source: docs/specs/AIU-034-limit-audit-design-brief/research.md

## AIU-042 - Pre-AIU-037 backend architecture and quality hardening
- goal: G-003
- status: done
- depends_on: [AIU-035, AIU-036]
- trigger: owner-selection before AIU-037
- outcome: Audit the entire existing backend and implement evidence-backed corrections to architecture, correctness, maintainability, patterns and performance. Use stable .NET 10/C# 14 capabilities where they improve the implementation. This is a code-focused remediation task, with regression tests and reproducible performance evidence, not a report-only review.
- acceptance: AC-01 through AC-08 in the specification cover backend inventory, actionable findings and fixes, architectural boundaries, modern language/runtime choices, measured performance, behavior preservation, verification and honest closure.
- specification: docs/specs/AIU-042-backend-hardening/spec.md
- evidence: docs/specs/AIU-042-backend-hardening/verification.md
- registration-note: Owner request, 2026-10-02: register a pre-037 task and provide a Codex Goal launch prompt. Registration does not start implementation. Existing IDs are retained; AIU-042 is the prerequisite of AIU-037.
- scope-note: Existing backend only, including nonvisual Windows composition/adapters where necessary. No UI redesign, new provider behavior, v2 stored-format migration or implementation of AIU-037 to AIU-040. Codex subagents remain disabled.

## AIU-037 - [astra] Provider parser extensions and stored-format version 2
- goal: G-003
- status: done
- depends_on: [AIU-035, AIU-042]
- specification: docs/specs/AIU-037-provider-limits-v2/spec.md
- evidence: docs/specs/AIU-037-provider-limits-v2/verification.md
- outcome-note: Closed on 2026-10-03 after independent review of a3ef130 identified four P2 findings, all corrected in f844c33 and verified by targeted regressions, 530 Infrastructure tests, 179 Presentation tests and the Windows consumer build. Review findings and primary resolution evidence are recorded separately; no second independent verdict is claimed.
- integration-note: AIU-036 already captures existing normalized snapshots under explicit legacy-window-v1, legacy-balance-v1 and legacy-extra-v1 series keys. Establish an explicit compatible mapping to native limit keys when adopting this model; never silently merge unlike series or discard the recorded history. See the AIU-036 specification.
- trigger: owner-selection
- outcome: The Claude, Codex, Copilot and Antigravity parsers fill the AIU-035 model for every provider limit in the research 5.5 mapping. This includes the Codex negative credit balance (M-02), the reset precision the provider actually sent (M-04) and the Codex individual control (M-07): its percentages become a window, and its raw amounts become a secondary amount of unknown unit. The Codex quota cache moves to envelope version 2 and the Claude, Copilot and Antigravity states to version 2, each with a forward migration (research 5.7).
- acceptance: (1) Every research 5.5 mapping row has a parser test with synthetic payloads, and each test keeps absent, explicit null, unlimited and zero distinct. (2) Opaque provider values are stored verbatim, and no raw payload is persisted. (3) Each v1 to v2 migration is tested. It preserves identity, grant and generation, drops only a cached quota that cannot be migrated, and follows the AIU-006 discipline: versioned, backed up, interruptible and recoverable. (4) A security-lifecycle review is recorded before merge. (5) No new transport, grant, scope, provider request or UI-only pool figure (PD-034-03).
- supersedes: No part of D-180 or D-181.
- contract-note: Parser output reaches the presentation contract only through AIU-035 and AIU-039. A change to that contract is agreed between the [opus] AIU-038 item and the [astra] items, never made silently from either side.
- source: docs/specs/AIU-034-limit-audit-design-brief/research.md

## AIU-038 - [opus] Redesigned presentation from the imported design
- goal: G-003
- status: done
- depends_on: [AIU-034]
- trigger: owner-selection
- outcome: The owner-accepted AIU-034 design reference is rebuilt in native WinUI 3 XAML: design tokens and styles, controls, views and view models with their bindings, commands and inline states, and a written presentation contract (the view-model data shape). The surfaces are the single main window with inline limit rows and history, the inline settings panel, inline rename and cap editing, "Confirm · Cancel" in place, undo, first run, the sign-in strip and the tray flyout. All of it runs on demo data: the design-brief section 4 scenario and every section 3 state.
- acceptance: (1) The reference surfaces and states render correctly in the owner's ordinary desktop configuration, with PA-1 to PA-13 amended by spec scope version 2. (2) The presentation contract is documented and exercised by view-model tests with demo data. (3) Ledger stays dark in ordinary use with native window/tray chrome retained. (4) Ordinary keyboard and mouse interactions work; Narrator, contrast-theme and unusual zoom/display matrices are excluded by owner direction 2026-10-03. (5) No modal dialog or account detail view; history/settings/editors stay inline. (6) Every packaged font's licence is verified against its official source.
- supersedes: From D-180: account detail and history pages opened from the account panel with Back; settings replacing the usage view; confirmation outside the control for Delete stored data. From D-181: readings only in bar hover text and accessible names; one status mark per account; the 20 % floor and even-day pace colours with the end-of-today mark. It keeps D-180's missing tabs, Settings icon, provider menu with one-click sign-in, first run that lists providers and immediate icon-only sign-out, and D-182's single dark appearance. The replaced views leave the product only when AIU-039 switches it (D-183).
- contract-note: This item owns the presentation contract's shape. A change to it is agreed with the [astra] AIU-039 item, never made silently from either side.
- source: docs/specs/AIU-034-limit-audit-design-brief/design-brief.md
- selection-note: Owner selected this worktree branch on 2026-10-02 and requested continuation and commit there, with no Core, Infrastructure, tools or live-adapter edits. On 2026-10-03 the owner removed Narrator, contrast-theme and unusual zoom/display scope and rejected custom-chrome complexity; native window/tray contours are retained. This amendment resolves PD-038-02 and PD-038-05.
- specification: docs/specs/AIU-038-ledger-presentation/spec.md
- evidence: docs/specs/AIU-038-ledger-presentation/verification.md
- outcome-note: Completed under amended scope version 2 and integrated into main on owner request on 2026-10-03 from branch tip 8a53324, preserving AIU-037 and AIU-042. Integrated regressions: Presentation 262/262, Infrastructure 530/530 and validator 80/80. Branch records retain ordinary Windows smoke and font/licence evidence. Product switch/live wiring remain AIU-039; Ledger is available through --demo --ledger.

## AIU-039 - [astra] Multi-account live adapters, new presentation and Windows acceptance
- goal: G-003
- status: done
- depends_on: [AIU-035, AIU-036, AIU-037, AIU-038]
- outcome-note: Implementation and focused review are complete. Two-account Claude live checks passed in ordinary mode; installed update/recovery and automatic retry/cache assertions passed with synthetic data. On 2026-10-03 the owner declined duplicate sign-in in Sandbox; under spec scope version 2 this is not required for completion, and installed real-account automatic refresh remains NOT_RUN. Live failure injection remains BLOCKED by automatic approval review. These limitations and the smoke-harness cleanup failure are preserved in verification.md; completion is not an all-green test or release claim. The ordinary-desktop verification amendment supersedes the older display/accessibility matrix below.
- specification: docs/specs/AIU-039-multi-account-ledger/spec.md
- evidence: docs/specs/AIU-039-multi-account-ledger/verification.md
- trigger: owner-selection
- outcome: Implement simultaneous multiple accounts per provider under D-073, including the Core session/account contracts, Infrastructure persistence and Windows live adapters needed to feed the AIU-038 presentation from the engine, parsers, reading series and budget configuration. Render each account with its own label, limits, budget, history, status and actions within the existing provider -> account -> limits design; adding another account must not require signing out of the first. The product then switches from the current views to the new presentation. The retired D-180 and D-181 views, view models and pace code are removed, not left behind. The XAML and view-model removal is agreed with the [opus] owner of AIU-038.
- scope-note: Owner amendment, 2026-10-03: include multi-account support in this next technical implementation item, fulfilling D-073 rather than creating a separate follow-up. The one-account-per-provider limit in the original AIU-034 reference scenario and delivered AIU-038 demo is not a product delivery constraint for AIU-039. This request updates the planned scope; it does not start implementation or change completed historical evidence.
- multi-account-acceptance: (1) At least two distinct accounts of the same provider can be added and remain connected concurrently, with independent labels, readings and actions in the main window and tray; cover all four provider adapters with synthetic fixtures and both owner-held Claude accounts in an authorized live check. (2) Use stable app-owned account references distinct from provider IDs, editable labels, emails and credentials. Detect a duplicate only from verified provider identity; adding a different account never overwrites an existing session. (3) Persist and restore each account's protected grant, quota cache, history, caps and preferences independently across restart. Forward-migrate existing single-slot data without losing grants or assigning old history/caps to a different identity; perform security-lifecycle review and focused independent review. (4) Connect, refresh, renew, cancel, reconnect and sign out target the selected account. A failure or sign-out for one account does not affect another, and sign-out preserves that account's history/caps under D-093. (5) Regression and ordinary Windows checks cover two same-provider accounts, restart, independent refresh/failure/sign-out, duplicate detection and migration; unavailable live accounts remain NOT_RUN or BLOCKED.
- acceptance: (1) Adapter tests with synthetic fixtures reproduce the design-brief section 4 figures and statuses through the contract. (2) The checks deferred from AIU-030 to AIU-033 pass on the new interface: dark-only rendering (D-182) with Windows in light app mode and with a contrast theme; budget and five-hour colours on live provider readings; live sign-in and sign-out through the new controls, with history kept (D-093); automatic refresh of a real account in the installed package, including the reading-series rows each refresh writes (D-184); the tray smoke rows, the packaged install, update and recovery harnesses, and screen-reader acceptance. (3) Live checks run only with the owner's authorization and an owner-led sign-in. Their results are recorded as PASS, FAIL, NOT_RUN or BLOCKED, never inferred from demo data.
- supersedes: Retires in the product every D-180 and D-181 part listed under AIU-038, through the switch to the new presentation.
- contract-note: Reuse the AIU-038 provider -> account -> limits hierarchy and account-targeted commands for multiple accounts; do not key an account by its provider or impose one card per provider. A needed contract change is agreed with the [opus] AIU-038 item before either side changes, never made silently.
- source: docs/specs/AIU-034-limit-audit-design-brief/spec.md

## AIU-040 - [astra] Remove the AIU-011 provider-history retrieval
- goal: G-003
- status: done
- specification: docs/specs/AIU-040-remove-provider-history/spec.md
- evidence: docs/specs/AIU-040-remove-provider-history/verification.md
- selection-note: Owner selected implementation on 2026-10-03; remove retired retrieval after the completed AIU-039 product switch.
- outcome-note: Removed obsolete Core/Infrastructure/session/console retrieval and registrations; Ledger retains local history. Security-lifecycle inventory and focused independent review passed. Infrastructure 611/611, Presentation 115/115, validator 80/80, documents, builds and targeted ordinary Windows smoke 2/2 passed. Initial environment/test-harness failures and unrun live/package-install checks remain in verification.
- depends_on: [AIU-039]
- trigger: owner-selection
- outcome: Following D-184, the AIU-011 provider-history retrieval is removed: the Core provider-history contract, the Codex and Copilot history clients, parsers and session routes, and the live and demo history sources and their registration. No provider-history request remains. Local history from AIU-036 is the only history shown.
- acceptance: (1) No code path requests a provider-history route, and no test or composition registration refers to the removed types. (2) Any stored data or preference that belonged only to provider history is handled under a recorded security-lifecycle review: removed within the owned root, or kept as unknown data, never silently rewritten with other data. (3) All suites, the validator and `git diff --check` pass. (4) AIU-011 is closed in the backlog with a reference to D-184.
- supersedes: No further part of D-180 or D-181. The History page this retrieval fed is replaced by inline local history through AIU-038 and AIU-039, so this item follows that switch and the current History view never goes empty early (D-183).
- contract-note: No presentation-contract change is expected. If one is needed, it is agreed with the [opus] AIU-038 item, never made silently.

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

## AIU-043 - Structured file logging, provider-response evidence and crash diagnostics
- goal: G-003
- status: done
- depends_on: [AIU-028, AIU-037]
- trigger: owner-selection
- outcome: Make application failures diagnosable from correlated local files and retain current provider-response evidence for AI-assisted parser development. Cover normal operations, provider requests, UI/dispatcher and background faults, startup/shutdown and fatal crashes, with a dedicated forced-flush path. Separate traces (3 days), application and provider evidence (7 days), and critical incidents (one calendar month), with bounded disk use and secret-free persistence.
- specification: docs/specs/AIU-043-file-logging/spec.md
- evidence: docs/specs/AIU-043-file-logging/verification.md
- registration-note: Owner request on 2026-10-03 selects implementation of AIU-043 and a concise agent rule for useful logging without noise. Completion request authorized an Astra low independent review; subsequent explicit permission covered existing-session live checks. Final verification records the completed gates and unavailable accounts.
- scope-note: Full response capture means pre-model response structure and policy-approved original values, with explicit redaction/completeness metadata. It never means plaintext tokens, cookies or arbitrary unclassified payload values. AIU-039 is not a prerequisite; instrument whichever presentation is active and retain coverage across its later switch. Focused independent review followed the current one-subagent policy.

## AIU-044 - Restore account-owned monetary usage and preserve work budgets
- goal: G-003
- status: done
- depends_on: [AIU-039]
- trigger: owner-selection
- outcome: Replace blanket CL-X suppression with monetary information inside its owning account. Preserve supported monthly work-budget presentation and the design's separate on-extra-usage mark, which measures spending since a 5h/7d window filled. Render from evidenced facts without guessing personal versus work from a label, wire variant or finite limit; unresolved purpose/scope gets neutral factual presentation rather than disappearing.
- acceptance: AC-01 through AC-06 in the specification cover account-owned presentation, finite monthly monetary budgets, unchanged spend-since-full semantics, unknown/disabled states, multi-account and stored-data preservation, and relevant ordinary Windows verification. No second account-like card or duplicate pool; no fabricated personal allowance, reset or prepaid balance.
- specification: docs/specs/AIU-044-account-monetary-usage/spec.md
- evidence: docs/specs/AIU-044-account-monetary-usage/verification.md
- registration-note: Owner requested this follow-up on 2026-10-03 after live AIU-039 verification. The owner selected implementation on 2026-10-04.
- scope-note: Owner requested this task-definition revision on 2026-10-04 and retained the design's on-extra-usage baseline, withdrawing the proposed daily baseline for that mark. The implementation does not introduce a personal/work switch, new provider access, standalone API billing or data migration. Real work-plan mapping remains unverified; source research and synthetic examples must not be described as live proof. Implementation follows the selected specification and its bounded presentation design.

- completion-note: Implemented and verified on 2026-10-04. Spending is account-owned; compatible monthly budgets and window-exhaustion marks are retained. Current Claude wire scope stays explicitly unresolved. Required independent review findings were fixed and verified; see the evidence record.

## AIU-045 - Triage and fix synthetic Windows audit findings
- goal: G-003
- status: done
- depends_on: [AIU-039, AIU-044]
- trigger: owner-resumption-after-discussion
- outcome: Preserve the interrupted 2026-10-04 audit, diagnose the newly observed audit-build startup failure, review the unfinished scope-annotation changes, and agree on bounded fixes and verification batches. Keep confirmed product defects, harness faults, unverified hypotheses and missing evidence distinct.
- specification: docs/specs/AIU-045-ui-audit-follow-up/spec.md
- evidence: docs/specs/AIU-045-ui-audit-follow-up/verification.md
- registration-note: On 2026-10-04 the owner stopped the long-running audit and requested preservation of all findings and a follow-up task for fixes and discussion in later sessions. This registers that task; it does not resume implementation or the comprehensive audit.
- decision-note: On 2026-10-06 the owner accepted decisions AIU045-D1..D9 in docs/specs/AIU-045-ui-audit-follow-up/analysis-2026-10-05.md (section 7). Preview publication is off (`AIU_PREVIEW_ENABLED=false`). The next bounded batch is T-01 from that record; the comprehensive audit stays paused.
- completion-note: Fix run of 2026-10-06 complete. T-01..T-04 are done. Preview 2026.10.602.0 was published from `4ea9667` (owner dispatch, run 37475616318; the feed's package and dependency URIs now use the immutable GitHub release, which fixed the owner's `0x80072EFE` install failure of 2026.10.601.0) and was installed by the owner with the App Installer GUI; packaged startup, close-to-tray with relaunch, and Open logs passed. Sandbox batch 2 passed the corrected layout scenario (`b17937b`). Final statuses are in the analysis record, section "Final status (2026-10-06, before the owner checkpoint)", and the installed-build results are in the evidence record, section "Post-install verification 2026-10-06". The comprehensive audit continuation and the listed opt-in follow-ups (AUD-05..AUD-09, ANL-20, ANL-23, the AIU-043 device-code delay) stay separately selectable.

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
