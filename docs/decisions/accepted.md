# Accepted decision register

Consolidated: 2026-09-12. Source: the owner conversation. These are normative product and engineering decisions, not claims of implemented code.

The latest explicit owner decision supersedes earlier alternatives. Technical corrections and experiments are separated in `technical-audit.md`; they must not silently rewrite product intent. D-001 through D-175 preserve the original register. D-176 records the explicit repository-wide English-language requirement.

## Product and boundaries

### D-001 - Product name
AI Usage; root namespace AiUsage; executable AiUsage.exe; repository slug ai-usage.

### D-002 - Platforms
One product with separate native implementations. Windows first; Android next. Do not carry Android-specific platform decisions into Windows.

### D-003 - Priority order
Security > native integration > reliability > privacy/local-first > maintainability > simplicity.

### D-004 - Initial target
Windows 11 24H2 or later, build 26100 or later, x64. .NET 10, C#, WinUI 3 and Windows App SDK; packaged MSIX.

### D-005 - Deployment
Framework-dependent deployment was explicitly selected. The .NET runtime and Windows App SDK runtime are separate prerequisites; prove installation on a clean machine.

### D-006 - Local-first operation
No proprietary account, backend, synchronization or telemetry server. Call providers directly; use GitHub/App Installer for distribution. The signed static compatibility manifest is a narrowly approved exception.

### D-007 - Providers
Deliver vertical slices in this order: Codex, Claude, GitHub Copilot, Antigravity. Do not claim complete ChatGPT or Gemini consumer coverage from a particular CLI quota source.

### D-008 - Release scope
Publish early runnable 0.x builds. Public v1.0 requires all four providers. An untested or nonfunctional provider login is not a completed integration.

### D-009 - License
MIT, with a public GitHub repository from the outset. Direct distribution is free. A future paid Store acquisition has identical functionality, without Pro feature gates.

### D-010 - Contributions
External Issues are intake; external PRs are accepted with DCO. Include CONTRIBUTING, SECURITY, private vulnerability reporting and license notices. Never invent Git author or DCO identity.

### D-011 - Android sharing
Share requirements, provider semantics, research and sanitized fixtures. Do not impose a shared .NET UI or Core implementation on Android in advance.

### D-012 - Development harness
OMP on Windows implements all executable product and workflow code. This handoff is documentation, not an implemented application.

## AI workflow and autonomy

### D-013 - Method
Goal-driven, repository-first, proportional specification-driven development. The owner provides goals and feedback; OMP handles research, decomposition, implementation, verification, PRs and authorized merges.

### D-014 - Execution modes
Interactive mode presents a ranked backlog for owner selection. Autopilot works independently within an explicitly authorized goal, with defined escalation exceptions.

### D-015 - Approval
Per-spec and per-design approval was superseded by escalation-only approval for ordinary authorized work. Changes to product intent, security boundaries, significant architecture or a significant unapproved dependency require the owner.

### D-016 - Questions
Do not ask settled questions again. Record clearly superior minor technical choices automatically; ask about important trade-offs. A blank/space reply selects a recommendation only in response to an actual owner-facing question, never in unattended execution.

### D-017 - Granularity
AIU items represent coherent outcomes, not individual classes, DTOs or tests. T items are internal decomposition of the selected feature. Decompose just in time; no universal line or token limit guarantees quality.

### D-018 - Proportional documentation
Small fixes need a short plan and evidence; normal features need a spec and tasks; architecture, authentication and security changes need a design and an ADR only for durable decisions. Do not produce four large documents for every edit.

### D-019 - Canonical backlog
Use docs/backlog.md, not GitHub Issues or native OMP todo. Statuses: idea, research-needed, blocked, ready, selected, in-progress, paused, review, done, dropped.

### D-020 - Prioritization
Weighted scoring plus explained judgment. Initial weights: goal alignment 30%, work unblocked 20%, risk reduction 20%, user value 15%, urgency 10%, effort efficiency 5%. Scores are derived; evaluate eligibility and dependencies first.

### D-021 - Goals and owner inbox
Use docs/product/goals.md; every AIU belongs to a goal. docs/decisions/pending.md is the durable inbox for decisions requiring the owner.

### D-022 - Execution state
tasks.md is the sole feature execution record: status, dependencies, ownership, writes/shared paths, isolation, agent, acceptance criteria and handoff. A single coordinator updates it at meaningful transitions.

### D-023 - Resume
A fresh interactive session performs compatibility checks and shows either the active feature with a continue confirmation or the ranked backlog. Internal Autopilot handoff preserves valid goal authorization and cumulative budgets.

### D-024 - Pause
The owner may pause or switch. Create a safe checkpoint, perform targeted verification, update the handoff in tasks.md and commit/push recoverable state when authorized. Do not label work in progress as complete or passing. Settle worker state first.

### D-025 - Autopilot sessions
Use a fresh execution session for each feature. After merge, hand off through the repository to a fresh context. Preserve goal-level authorization, budget and accounting across sessions.

### D-026 - Selective blocking
A local issue blocks its AIU, not the entire goal. Continue other independent authorized items. Stop the goal for a global security/product blocker or when no safe work remains.

### D-027 - Budgets
Use adaptive overall and per-feature effort budgets, bounded recovery attempts and checkpoints for lack of progress or abnormal consumption. Budget exhaustion never means completion.

### D-028 - No artificial pauses
Autopilot does not wait for the owner after every merge. Stop for goal completion, escalation, budget exhaustion, fatal failure or explicit pause. Do not run an unbounded improvement loop.

### D-029 - Plan Mode
Fresh interactive sessions start in Plan Mode; a trivial fix needs only a short plan. Exit for authorized implementation. Do not require repeated approval at every internal handoff.

### D-030 - OMP version policy
Use the latest stable release, not main/nightly and not a permanent historical pin. Probe compatibility after upgrades and record the exact tested version. Do not upgrade OMP during an active feature.

### D-031 - Profile
Use the ai-usage profile and launch from the repository root. The profile contains authentication, model mappings, local sessions, cache and memory, not canonical project policy.

### D-032 - Memory
Use memory.backend=local as supplementary context, never authority. Disable auto-learn. The repository must support recovery without memory or conversation history.

### D-033 - Portable behavior
Version rules, skills, agents, documentation and the validator in Git. Do not commit generated local state, credentials or model-provider authentication.

### D-034 - Skills
Keep essential rules short in RULES.md and the project map in AGENTS.md. Load specialized skills on demand. Do not inject all documentation as always-apply context.

### D-035 - AI infrastructure changes
Protect configuration, RULES and changes to privileges or gates. Small navigation fixes in AGENTS, evidence-based WATCHDOG checks and existing-skill improvements may be made without weakening policy.

### D-036 - Models
Use strong primary/planning models, an advisor and fresh reviewer from a different model family, and a smaller model for inexpensive tasks. Discover actual available model IDs rather than inventing them.

### D-037 - Advisor
Always enable the advisor for the primary agent. Grant read/grep/glob only, not write/bash/eval. syncBacklog=1 means bounded catch-up, not completed review. Do not automatically attach an advisor to every worker.

### D-038 - Parallelism
One active feature may have several independent isolated write workers. The primary is the only integrator; shared contracts are handled sequentially. Research may run in parallel. Avoid an arbitrary 32-agent swarm.

### D-039 - Workers
Use native task batching and isolation, initially targeting up to four workers. Require explicit ownership and write globs; validate the actual diff. Workers must not edit the shared backlog, tasks, settings or CI.

### D-040 - Integration
Automatically integrate only after scope/diff checks, passing targeted tests and clean patch application. The primary resolves shared overlap and conflicts. Verify the integrated feature tree, not just isolated worker tests.

### D-041 - Review
Owner amendment, 2026-09-13: ordinary development PRs require primary diff/acceptance review and relevant automated checks, not a mandatory independent full review. Use focused independent review for material authentication, secret handling, destructive data lifecycle or privilege changes; retain the full independent review for public release approval or explicit owner requests. When used, keep context fresh, memory disabled, a different model family and read-only tools. Zero findings is valid; no automatic review loop.

### D-042 - Findings
BLOCKER and MAJOR findings block merge. Deduplicate MINOR findings into the backlog; omit NIT findings. After a fix, run targeted verification rather than restarting a full review loop.

### D-043 - Git
Use trunk-based main with short-lived feature/AIU branches and squash merge. OMP may commit, push, open PRs and merge after the required gates. No direct main push or force-push.

### D-044 - Permissions
YOLO execution is authorized within the selected project scope; it is not a sandbox. Retain human gates and protected release/manifest operations. Do not describe textual rules as hard security enforcement.

### D-045 - Validator
Use a lightweight repository validator and CI for IDs, statuses, links, acceptance references, dependencies and ownership. Gate critical transitions, not every keystroke. Owner amendment, 2026-09-13: defer main protection and required-check enforcement to low-priority AIU-026; their absence is not a blocker to current development PRs. Keep relevant exact-head check evidence and explicit remote-action authority. This intentionally accepts that GitHub does not independently enforce the local merge policy.

### D-046 - Documentation drift
Use docs-as-code. Specs and designs may evolve within the approved intent. Never weaken acceptance criteria or security requirements merely to make failing implementation appear successful.

### D-047 - Provenance
Research uses structured frontmatter and Markdown with verification dates, source URLs and exact refs, classification and confidence. Revalidate relevant provider contracts before changing them.

### D-048 - Spikes
Probe uncertain authentication, APIs or stack integration with a disposable spike answering one question. A throwaway proof is not production readiness.

### D-049 - Native workflow UX
Use thin project commands, selection UI and hooks over OMP primitives, not a new agent framework. Check names for built-in collisions. Commands do not exist until implemented and verified.

## Windows stack and code

### D-050 - Solution
Use AiUsage.Windows, AiUsage.Core and AiUsage.Infrastructure. Organize by feature/provider; do not create a project for every provider.

### D-051 - Project boundaries
Core is UI/platform-neutral. Windows composition references Infrastructure; provider transport DTOs do not enter the UI. Minimize the public API; use internal classes and InternalsVisibleTo for tests.

Owner amendment (2026-09-13): develop provider integrations as a reusable UI-independent library and verify them through a console application before connecting them to WinUI. The console and UI must consume the same implementation, not separate provider clients. Start with the existing Core/Infrastructure library boundaries; a project per provider or a speculative universal framework is not required. Provider research and library/console verification must not depend on UI readiness. Console proof does not replace later UI lifecycle verification or authorize live account access, unsafe credential output, or source CLI credential-store mutation.

### D-052 - Host
Use Microsoft.Extensions.Hosting Generic Host for DI, configuration, logging and lifetime. Coordinate startup/shutdown with WinUI; do not block the dispatcher with console-style Run.

### D-053 - MVVM and routing
Use CommunityToolkit.Mvvm and MVVM-first routing. Uno.Extensions.Navigation is the selected candidate; prove standalone WinUI integration with a spike without silently switching to a cross-platform UI framework.

### D-054 - HTTP
Use IHttpClientFactory, typed clients, System.Text.Json and Microsoft.Extensions.Http.Resilience with provider- and operation-specific pipelines. Do not apply blanket retries to authentication POSTs.

### D-055 - Serialization
Use source generation for stable DTOs. Use explicit dynamic parsing or reflection only where needed. Reflection cannot automatically repair schema drift.

### D-056 - Database
Use SQLite, EF Core 10 and Microsoft.EntityFrameworkCore.Sqlite with migrations. Enable WAL, keep transactions short, use IDbContextFactory and one context per operation. No generic repository.

### D-057 - Single writer
Use Channel<T> for application database writes, including settings, history and maintenance. Network fetches run independently in parallel. Upgrades, imports and resets acquire exclusive maintenance ownership.

### D-058 - Publication order
API response -> normalization -> committed database transaction -> immutable store -> UI. A per-account update must preserve concurrent results from other accounts.

### D-059 - Messaging
Use BCL Channel<T> for work queues, not broadcast. Provide explicit keyed deduplication. No MediatR, Rx or global magic event bus.

### D-060 - Live state
Use immutable live/current AppState. IStateStore<T> exposes Current and Subscribe returning IDisposable. Do not keep complete history in live state.

### D-061 - UI threading
Implement IUiDispatcher over DispatcherQueue in the Windows layer. Manage subscription lifetimes explicitly. Domain logic does not know the UI thread.

### D-062 - History reads
Use IUsageHistoryQueryService with AsNoTracking projections filtered by account, context, limit, range and resolution. Run database work off the UI thread.

### D-063 - History resolution
Perform resolution-aware querying/downsampling in the query layer. Offer 24h, 7d, 30d, 90d and 1y presets plus custom ranges. Bound points to rendering needs.

### D-064 - Time
Persist UTC instants and display local time/culture. Use TimeProvider in application logic and tests.

### D-065 - Logging
Use Serilog through Generic Host with local rolling files. Apply token/cookie/PII-safe allowlisting and redaction, including in Debug builds.

### D-066 - Charts
Use LiveCharts2 for main-dashboard sparklines and History. Do not add charts to the tray popup.

### D-067 - Tray and notifications
Use H.NotifyIcon.WinUI and Windows App SDK AppNotificationManager.

### D-068 - Errors
Use a small in-house Result<T> with sealed typed error records. Model expected authentication, transient, schema and cancellation outcomes. Do not swallow programming errors.

### D-069 - Cancellation
Cancellation is an expected outcome, not an account warning/failure. At the boundary, catch only the relevant OperationCanceledException. Treat timeout separately.

### D-070 - Code quality
Enable nullable analysis, analyzers and repository .editorconfig. Owned-code warnings fail CI; third-party suppressions must be narrow and justified. Use xUnit v3.

### D-071 - Performance
Target a usable cached cold startup within 500 ms on a defined reference machine. This is a measured target, not an unverified guarantee. Do not use flaky hosted-CI timing gates.

### D-072 - AOT
Native AOT is not a v1 requirement. Start with normal framework-dependent .NET 10. Consider ReadyToRun/AOT only after profiling; no premature trimming constraints.

## Accounts, authentication and quotas

### D-073 - Multiple accounts
Support multiple accounts per provider in both the model and UI from the outset. Support custom labels. Keep provider identity separate from labels, email and tokens.

### D-074 - Contexts
Represent account -> organization/workspace/project contexts -> quota groups -> limit windows. Share credentials across contexts only when the actual grant permits it.

### D-075 - Context discovery
Add all available discovered contexts, with hide controls. Temporary disappearance must not delete history. Restore the same context when its stable ID returns.

### D-076 - Identity
Use a stable provider identity plus required grant/context scope. A fallback fingerprint must be evidence-based; email or a token hash is never the universal identity key.

### D-077 - Quota model
Use a common normalized model plus typed provider-specific extensions. Do not limit it to primary and secondary windows; support shared, model-specific and feature-specific pools.

### D-078 - Quota-group examples
Show Spark or other Codex buckets and Antigravity model groups only when the provider actually returns them. Do not hardcode group membership from conversation examples.

### D-079 - Units
Remaining percentage is primary, with native absolute credits/requests/other units secondary. Unknown, unlimited, exhausted and stale are distinct states. Never invent total counts.

### D-080 - No fabricated totals
Do not sum or average unrelated quotas or accounts. Do not double-count shared pools.

### D-081 - OAuth client strategy
Use an app-owned public client where supported and an OMP-compatible public-client flow where justified. Record support and policy uncertainty; MIT licensing does not grant provider authorization.

### D-082 - Browser authentication
Use the system browser and a provider-compatible loopback redirect, with PKCE/state where the protocol supports them; use device flow where appropriate. No embedded WebView login.

### D-083 - Client secrets
Do not embed confidential OAuth client secrets in the application. A desktop-client registration does not establish permission for another product.

### D-084 - Final application secret store
Use DPAPI CurrentUser with a separately versioned encrypted record per account/grant in package-owned LocalState/Secrets. Windows Credential Manager as the application secret store is superseded.

### D-085 - Secret persistence
Never write plaintext secrets to disk or logs. Store an opaque CredentialId in the database. Write temporary ciphertext and atomically replace, with per-credential serialization and recoverable format migrations.

### D-086 - CLI import
Provide explicit Import and Import all. Run incremental asynchronous discovery on first launch and on manual scan. Search known native Windows locations only; no full-disk scan or WSL integration in v1.

### D-087 - CLI execution
Passive reading is the default. CLI execution needs a justified provider-specific exception with a trusted executable, fixed arguments, timeout/cancellation and no shell, elevation or source-store writes.

### D-088 - Import all
Import new supported candidates only, handling partial failures independently. Existing accounts show Already added or require explicit Update credentials/Re-import.

### D-089 - Re-import
Preserve history, settings, labels and order. Verify replacement credentials before promoting them. Failed validation must not destroy a still-working record.

### D-090 - CLI coexistence
The goal is fewer clicks and an app-local credential copy. Prove token-lifecycle independence; copied rotating refresh tokens can conflict with the CLI.

### D-091 - Connection flow
Provider picker -> supported methods -> credential/identity verification -> initial quota. Temporary quota failure means connected/verification pending, not falsely healthy or automatic credential loss.

### D-092 - Reauthentication
An invalid or revoked refresh grant puts only that account into Re-auth required. Preserve stale cache/history and keep other accounts operational. No endless retries.

### D-093 - Disconnect
Delete application credentials and stop active monitoring; retain history and identity for reconnect. Hide disconnected accounts by default with Show disconnected accounts available.

### D-094 - Reconnect
Reattach history, settings, labels and order to the same stable identity. Delete account data is a separate destructive action.

### D-095 - Provider evidence
Review sources before authentication, quota or parser changes. Use real sanitized fixtures or clearly marked synthetic cases, golden outputs and critical semantic assertions.

### D-096 - Parsing
Ignore unknown fields, but leave missing optional quota values unknown rather than zero. Critical semantic mismatch returns SchemaMismatch with safe diagnostics; never fabricate quota data.

### D-097 - Provider capabilities
Bundle providers and register them explicitly in DI, using optional capability interfaces. No runtime DLL plugins. Create interfaces for independent behavior; quota groups may simply be data returned by usage capability.

### D-098 - Feature flags
Use typed flags plus detected capabilities in Advanced/Experimental settings. Retire temporary flags through migration; flags cannot bypass safety controls.

## Refresh and reliability

### D-099 - Refresh cadence
Use adaptive refresh with roughly five minutes as the starting baseline and provider-specific minimum/reset/backoff/stability behavior. Manual refresh is immediate except for real server Retry-After constraints.

### D-100 - Network concurrency
Do not impose an arbitrary global SemaphoreSlim(4) on independent accounts. Work is bounded by actual accounts and requests; respect observed 429/Retry-After and host constraints.

### D-101 - Background deduplication
Coalesce timer, network-recovery and sleep-resume triggers for the same account. Manual refresh cancels/replaces quota fetching, not the credential-rotation transaction.

### D-102 - Manual replacement
Use per-account/request generation checks to prevent superseded results from persisting, publishing or alerting. Do not cancel other accounts. Repeated clicks must not create uncontrolled duplicate work.

### D-103 - Refresh actions
Provide Refresh all and per-account Refresh. Skip disabled and Re-auth required accounts; isolate errors.

### D-104 - Sleep and connectivity
On resume or connectivity recovery, refresh only stale accounts under the same policy. Connectivity signals are not proof of provider reachability; actual HTTP outcomes remain authoritative.

### D-105 - Offline operation
Startup, dashboard and history remain usable from cache offline. Clearly mark stale data and update asynchronously. A provider error must not replace the whole dashboard with an error screen.

### D-106 - Power awareness
Reduce automatic work on Battery Saver or metered connections, showing the reason and allowing a settings override. Manual refresh remains available. Do not wake the PC.

### D-107 - Locked session
Continue background monitoring while Windows is locked. Do not open consent/UI dialogs. Unlock must not trigger redundant refresh when data is fresh.

### D-108 - Process lifetime
Use a tray-resident BackgroundService. Close hides the main window, Minimize behaves normally, and Exit stops the Host. No out-of-process background task component in v1.

### D-109 - Shutdown
Cancel network work and bound draining of pending writes. Do not delay logout with lengthy network operations. Fatal crashes terminate the process without a watchdog restart loop.

### D-110 - Crash recovery
Use a clean-shutdown marker and targeted checks for interrupted operations. Do not perform routine full-database or filesystem validation.

## UI and notifications

### D-111 - First run
Show an empty dashboard with Add account, not a wizard. Discover CLI candidates asynchronously and progressively; support individual and bulk import.

### D-112 - Window behavior
Use one application instance and one main window. Repeated launch/notification activation opens the existing window. Restore size, position, maximization and monitor while correcting off-screen bounds. Always on top defaults off.

### D-113 - Startup UX
No splash screen. Display cached data first, with no startup network or maintenance wait. Autostart is hidden-to-tray and opt-in; Exit does not clear the startup preference.

### D-114 - Dashboard layout
Use Overview and an adaptive one/two/three-or-more-column grid with balanced density. Preserve user order instead of dynamic risk sorting; highlight states.

### D-115 - Overview
Show provider/account counts, the lowest remaining quota and nearest reset with source/freshness, plus warning/critical/reauth counts. No global usage percentage.

### D-116 - Cards
Use account cards with nested primary windows and quota groups. Primary limits remain visible; groups default collapsed and expand for warning/critical states while respecting manual preference.

### D-117 - Visibility
Hiding a group or limit does not stop monitoring/history. Ask Hide only or Hide and mute alerts. Do not override an explicit hide merely because auto-expansion would otherwise apply.

### D-118 - Reset display
Show relative reset time and the exact local timestamp. Reaching a reset time triggers observation, not an invented 100% remaining value.

### D-119 - Appearance and accessibility
Offer System, Light and Dark themes, default System. Use Fluent styling, DPI support, keyboard navigation, accessible labels and high contrast. Status cannot rely on color alone.

### D-120 - Localization
Use resources immediately, with English-only v1 and English fallback. Do not localize provider IDs, protocol values or persisted keys. Format dates/numbers using Windows culture.

### D-121 - Branding
Use official provider assets only after brand/license review; use neutral fallback icons when redistribution is not permitted.

### D-122 - History UI
Provide main-dashboard sparklines and a dedicated History view. Forecasting is Experimental, explicitly labeled Estimate and cannot change factual quota or notification behavior.

### D-123 - Thresholds
Default remaining thresholds are 25%, 10% and 0%, with global -> provider -> account -> limit overrides and reset notices.

### D-124 - Alert deduplication
Use stateful threshold crossing and hysteresis, rearming only after verified recovery/reset. Alert on fresh observations only. Aggregate simultaneous alerts into one notification.

### D-125 - Notification activation
Open/focus the affected account and group in the main UI. Do not add a separate persistent alert-history entity; use Windows Notification Center and local diagnostics.

### D-126 - Tray severity
Priority: Re-auth required > Offline/Error > Critical quota > Warning quota > Normal. Explain the cause, freshness and affected account in the tooltip; do not fabricate a minimum quota.

### D-127 - Tray interaction
Single left-click opens the mini-dashboard, double-click opens the main app, and right-click shows Open, Refresh all, Settings and Exit.

### D-128 - Tray popup
Show current quota, reset and status for all accounts/groups with per-account and global refresh. No charts. Keep it read-mostly; settings live in the main app. Use the same central state store.

### D-129 - Diagnostics
Provide a System Status page with build/commit/schema/provider evidence/status, latest refresh/update/manifest state and Export diagnostics, Open logs and Run health check actions.

### D-130 - Health checks
Diagnosis only; repairs are separate explicit actions. Do not disguise state mutation as a health check.

### D-131 - No application lock
Rely on Windows user-session protection; no separate PIN or Windows Hello gate in v1.

## Data lifecycle and upgrades

### D-132 - Storage scope
Use per-user package-owned ApplicationData local storage through IAppPaths. Canonical roots: Data, Secrets, Cache, Logs, Diagnostics, Backups and Temp. User-selected exports are not silent cleanup targets.

### D-133 - Settings
Use SQLite/EF with strongly typed core entities and a typed extensible provider store. Keep immutable defaults in application configuration. Validate writes, imports and migrations, not the entire state at every startup.

### D-134 - Database privacy trade-off
Do not encrypt the whole SQLite database. Never put plaintext OAuth credentials in it. Account metadata and usage/history remaining unencrypted is an explicitly accepted trade-off.

### D-135 - History collection
Persist normalized samples only, enabled by default. Disabling collection stops new samples; old history is separately deletable. No routine raw-response archive.

### D-136 - History retention
Keep raw normalized observations for approximately 90 days, then hourly/daily rollups for approximately one year, configurable. Deduplicate unchanged values while preserving freshness/coverage. Do not sum percentages across resets.

### D-137 - Diagnostic retention
Keep sanitized failure/unknown-schema diagnostics only, for seven days. Blacklist-only redaction must not leak secrets in previously unknown fields.

### D-138 - Telemetry
No cloud or automatic crash telemetry, and no automatic memory dumps. Diagnostic export is manual and sanitized; exception details are allowlisted.

### D-139 - Migration versions
Version database, configuration, infrastructure and secret schemas independently from product version. Published migrations become immutable after any public Preview.

### D-140 - Forward upgrade contract
Upgrade directly from every published Stable/Preview persistent schema without intermediate binaries. Do not support downgrade. Refuse normal operation on a newer unsupported schema.

### D-141 - Upgrade coordination
Preflight -> consistent protected backup -> ordered database/configuration/filesystem/secret steps -> validation -> cleanup -> normal services. Apply pending steps only and recover interrupted phases.

### D-142 - Filesystem migration
Use idempotent, restart-safe detect/stage/verify/publish/cleanup steps with a journal. Do not pretend that a database transaction atomically covers filesystem changes.

### D-143 - Cleanup
Clean only canonical application-owned roots with path/reparse safeguards. Apply retention to temporary files, logs, cache and orphans. Never delete Windows-managed MSIX binaries or user Downloads.

### D-144 - Migration backup
Keep one last verified pre-migration durable-state bundle protected with DPAPI CurrentUser. Include database, configuration/layout manifests and irreplaceable nonsecret files; exclude cache, logs, temp, diagnostics and OAuth secrets.

### D-145 - Backup rotation
Do not replace the last good backup with a half-migrated state during retries. Retire the old checkpoint only after the new operation succeeds. Secret rollback retains its own encrypted generation, never portable-export secrets.

### D-146 - Restore
Validate decryption, manifest, hashes, schema and SQLite integrity. Explicitly restore a consistent previous durable state and rerun current migrations. No automatic endless restore/retry loop.

### D-147 - Recovery mode
Migration failure exposes Retry, Restore, Export diagnostics and Open data folder. Never silently wipe data or launch the current app against an incompatible old schema.

### D-148 - Upgrade tests
Retain historical schema/layout/configuration fixtures. Test interruption recovery, preserved values and credential-reference reconciliation. Back up WAL databases consistently rather than copying only a live database file.

### D-149 - Portable export
Use an open, unencrypted documented bundle with versioned manifest, settings, accounts and history. Warn about private metadata; exclude OAuth credentials, cookies and other secrets.

### D-150 - Portable import
Replace only, not merge. Validate, stage and migrate replacement state before cutover; create a pre-import backup and support recovery. Preserve credentials only for matching stable identities; remove orphan secrets after commit.

### D-151 - Settings reset
Reset settings preserves accounts, credentials, history, custom labels and order. Safely reset core preferences, including opted-in startup behavior.

### D-152 - Factory reset
Require explicit full-reset confirmation, stop producers, drain/cancel writes and close handles. Clear application-owned state, secrets, backups and owned notification/startup state, then restart into first-run UX. Do not literally reinstall or require a download.

### D-153 - Deletion boundaries
Disconnect/reset must not modify original CLI credentials or delete exports saved to user-selected locations. Local deletion does not promise remote grant revocation.

## Release, CI and operations

### D-154 - CI
Owner amendment, 2026-09-13: current development PR CI runs the validator regressions (including compilation/analyzers), canonical document validation and workflow/patch regressions. Formatting stays a local check, not a PR gate. Cancel obsolete runs of the same PR; do not skip behavior checks or suppress failures. Add relevant provider/security/UI checks when those features exist. Public release evidence and genuinely interactive xUnit/FlaUI UIA3 smoke requirements remain unchanged.

### D-155 - Test-first policy
Use test-first for parsers, authentication, token refresh, repositories, security logic and bug fixes. Test UI/configuration proportionally. Never fabricate passing verification.

### D-156 - Live smoke tests
Keep them out of normal PR CI. Use explicit local provider tests or dedicated CI accounts with safe rotation. Never expose personal credentials or secrets to public-fork jobs.

### D-157 - Release channels
One direct installation and identity supports Stable and Preview. Every green main merge becomes Preview. Stable promotion manually selects the exact signed, tested artifact without rebuilding or resigning.

### D-158 - No downgrade on channel switch
Switching Preview to Stable changes the feed but keeps the newer installed binary until Stable catches up. Keep ForceUpdateFromAnyVersion disabled.

### D-159 - Distribution
Early internal 0.x uses self-signed MSIX and manual update. Before public v1, prove .appinstaller auto-updates using GitHub Releases assets and Pages feeds. No application-owned installer cache by default.

### D-160 - Update UX
Check asynchronously without blocking startup. Show Update available/ready and use explicit Restart & update. Never restart automatically, including from tray or during a security incident.

### D-161 - Signing
Prefer Azure Artifact Signing for public builds, subject to actual eligibility. Keep publisher identity stable and test rotation. Reuse the initial local development certificate; exclude its private key from Git.

### D-162 - Microsoft Store
Add later as paid acquisition with the same features. A separate package identity may require a dedicated direct-to-Store migration. Do not assume an ordinary same-family update.

### D-163 - Version numbering
Separate SemVer-style product version from four-number MSIX YYYY.M.DDNN.0. CI must serialize allocation and guard against overflow and out-of-order publication.

### D-164 - Artifact retention
Keep the latest 50 Preview builds and retain Stable/persistent-state milestones indefinitely. Also retain currently referenced feeds/candidates regardless of count. Record commit, hash and version in release metadata.

### D-165 - Promotion criteria
Use risk-based evidence, with no mandatory soak. A compatibility-only hotfix may release hourly after verification. Authentication, storage or dependency scope expansion increases the risk class.

### D-166 - Release authority
OMP may merge normal verified PRs. The owner approves Stable promotion and emergency compatibility-policy publication. Never expose signing secrets to arbitrary PRs or agents.

### D-167 - Compatibility manifest
Use signed static GitHub-hosted policy with bundled and cached last-good versions, fetched asynchronously. It may only narrow capabilities, not configure executable code, endpoints, authentication or secrets.

### D-168 - Compatibility and security blocks
Stable hard-blocks compatibility incidents; Preview permits an explicit local override. Security-critical incidents hard-block all channels without override. Cached data and history remain accessible.

### D-169 - Manifest keys
Use an offline root and rotating signing key separate from the MSIX certificate, with versioning, expiry, revocation and anti-rollback. Publication is human-approved and audited. Policy rollback uses a new higher version.

### D-170 - Compatibility watchdog
Run hourly best-effort upstream/dedicated-account checks plus post-merge and manual checks. Confirmed incidents receive highest priority. Never disable a provider automatically on a weak signal.

### D-171 - Watchdog execution boundary
Detection may run while the owner PC is off; fixes start in the next active authorized OMP session. No always-on coding runner initially. Scheduled Actions are not guaranteed 24/7 monitoring.

### D-172 - Dependency maintenance
Use Dependabot for NuGet and Actions intake, prioritizing security. No blind major upgrades. Do not repeatedly ask the owner to reselect already approved packages.

### D-173 - Feedback
The owner reviews installable results and supplies goals/feedback, not detailed implementation prompts. OMP turns feedback into coarse backlog outcomes and preserves deferred decisions.

## Deferred work

### D-174 - Deferred scope
Defer Android, ARM64/x86 expansion, Windows Widgets, WSL import, multi-window, command palette, Store infrastructure and an always-on OMP runner.

### D-175 - Later experiments
Develop forecasting and advanced tuning only after measured history. Defer remote policy protocol details to the release-safety task. Do not build a complete platform framework first.

## Repository language

### D-176 - Repository language
All repository documentation, decisions, goals, backlog items, specifications, plans, skills, agent instructions, prompts, code identifiers/comments, test descriptions, generated reports, commit messages, PRs and release notes must be in English. Ukrainian is for conversation with the owner only. Do not translate opaque provider/user data or previously approved identifiers. Deliberately added future product translations belong only in localization resources.

## Provider account context

### D-177 - Codex connection parity with the inspected OMP client
On 2026-09-14 a live console sign-in proved the owner's account carries a `chatgpt_compute_residency` claim, which this implementation's own fail-closed policy refused after a successful token exchange. The owner then explicitly directed that the Codex connection and usage read duplicate the locally cloned OMP implementation rather than deriving an independent policy, taking only the parts needed to authenticate and read usage. Account-context claims such as residency and FedRAMP are therefore not read at all: that client extracts only the workspace identity, and only provider responses decide account context. Local identity consistency stays fail-closed - conflicting access/id workspace claims and a workspace change on refresh are rejected. Quota requests send the same explicit headers as that client: `Authorization`, `User-Agent` and `ChatGPT-Account-Id`, plus the standard transport `Accept`. Self-identifying values stay truthful: `User-Agent: AiUsage/...` and `originator=ai_usage`, never impersonating OMP or the Codex CLI. A live session verified this combination end to end. This supersedes the earlier residency rejection policy and does not authorize arbitrary headers, other providers, durable credential storage or UI integration.

## Branching

### D-178 - Direct main development until the first release
On 2026-09-14 the owner decided to merge completed work into `main` and continue development by committing directly to `main`, one task at a time, treating feature branches as unnecessary overhead at this stage. Local merges and commits are ordinary work; pushing, remote publication and branch protection changes remain separately authorized and are not implied. Each commit must still be a coherent, verified task with its canonical records updated, not a checkpoint of unfinished work. Proposal to revisit, recorded at the owner's request: before the first release, restore short-lived feature branches with pull requests for anything touching signing, release artifacts, credential or data lifecycle, so release candidates are reviewable and revertible; main protection remains deferred in AIU-026 until then.

### D-179 - Push each completed task to main
On 2026-09-14 the owner authorized pushing directly to `origin/main` after every completed task, to avoid accumulating unpushed work and to keep implementation steps minimal. A push is now part of finishing a task, not a separate approval. Conditions that still hold: push only a fast-forward of the exact verified head, never force-push, never rewrite published history and never change branch protection or repository settings. If the remote has diverged, stop and report instead of forcing. Nothing here authorizes releases, tags, remote workflow dispatch, secrets or publication; GitHub Actions execution remains unverified evidence until a run is actually observed.
