# Accepted decision register

Consolidated: 2026-09-12. Source: the owner conversation. These are normative product and engineering decisions, not claims of implemented code.

The latest explicit owner decision supersedes earlier alternatives. Technical corrections and experiments are separated in `technical-audit.md`; they must not silently rewrite product intent. Execution amendments are explicitly marked below; superseded originals, including D-176, are retained in the historical register. Unrelated product decisions remain in force.

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
Owner-approved agent-neutral amendment, 2026-09-14: Development is agent-neutral. Root AGENTS.md is the common entry point; specialized guidance has one active copy in .agents/skills. No workflow runtime or plugin is required.

## AI workflow and autonomy

### D-013 - Method
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-014 - Execution modes
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-015 - Approval
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-016 - Questions
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-017 - Granularity
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-018 - Proportional documentation
Owner-approved agent-neutral amendment, 2026-09-14: Use proportional records under CONTRIBUTING and docs/workflow/formats.md: small fixes need a brief plan and evidence; features need spec and verification with optional tasks. Designs explain meaningful choices; ADRs record durable decisions.

### D-019 - Canonical backlog
Owner-approved agent-neutral amendment, 2026-09-14: docs/backlog.md owns feature status and evidence references, using the existing status vocabulary. Status alone never authorizes execution.

### D-020 - Prioritization
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-021 - Goals and owner inbox
Owner-approved agent-neutral amendment, 2026-09-14: docs/product/goals.md owns outcomes and goal membership, without session permission chronology. docs/decisions/pending.md records unresolved owner decisions.

### D-022 - Execution state
Owner-approved agent-neutral amendment, 2026-09-14: Optional tasks.md owns internal task state and a concise handoff. Required structured fields are status, depends_on, acceptance and evidence; parallel ownership is defined in docs/workflow/formats.md.

### D-023 - Resume
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-024 - Pause
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-025 - Autopilot sessions
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-026 - Selective blocking
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-027 - Budgets
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-028 - No artificial pauses
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-029 - Plan Mode
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-030 - OMP version policy
Owner-approved agent-neutral amendment, 2026-09-14: AI client versions are optional environment context, not product build prerequisites. Historical OMP compatibility evidence remains historical.

### D-031 - Profile
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-032 - Memory
Owner-approved agent-neutral amendment, 2026-09-14: Memory is supplementary, never authority. The repository must support recovery without previous conversation history.

### D-033 - Portable behavior
Owner-approved agent-neutral amendment, 2026-09-14: Version authored instructions, shared guidance, documentation and validation in Git; keep credentials and generated local state out.

### D-034 - Skills
Owner-approved agent-neutral amendment, 2026-09-14: Keep essential instructions and the selective map in root AGENTS.md. Read specialized .agents/skills guidance on demand; native discovery is optional.

### D-035 - AI infrastructure changes
Owner-approved agent-neutral amendment, 2026-09-14: Respect current owner scope for workflow changes. Repository guidance does not grant native tool privileges.

### D-036 - Models
Owner-approved agent-neutral amendment, 2026-09-14: No model vendor or family is required. Use available tools proportionally and report unavailable required independent review.

### D-037 - Advisor
Owner-approved agent-neutral amendment, 2026-09-14: An Advisor is not required. Review scope and evidence follow CONTRIBUTING.

### D-038 - Parallelism
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-039 - Workers
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-040 - Integration
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-041 - Review
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-042 - Findings
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-043 - Git
Owner-approved agent-neutral amendment, 2026-09-14: The sole current Git policy is [CONTRIBUTING](../../CONTRIBUTING.md#git-policy). This decision grants no remote action or main integration.

### D-044 - Permissions
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### D-045 - Validator
Owner-approved agent-neutral amendment, 2026-09-14: The local validator checks IDs, statuses, links, acceptance, dependencies, safe paths and evidence without runtime dependencies. Main protection remains deferred in AIU-026; local evidence is not remote enforcement.

### D-046 - Documentation drift
Use docs-as-code. Specs and designs may evolve within the approved intent. Never weaken acceptance criteria or security requirements merely to make failing implementation appear successful.

### D-047 - Provenance
Research uses structured frontmatter and Markdown with verification dates, source URLs and exact refs, classification and confidence. Revalidate relevant provider contracts before changing them.

### D-048 - Spikes
Probe uncertain authentication, APIs or stack integration with a disposable spike answering one question. A throwaway proof is not production readiness.

### D-049 - Native workflow UX
Owner-approved agent-neutral amendment, 2026-09-14: The executable OMP bridge is retired. Thin adapters point to root AGENTS.md; no replacement orchestrator is introduced. Historical evidence is retained in docs/archive/omp.

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
Owner-approved agent-neutral amendment, 2026-09-14: CI retains validator regressions and document validation, adds deterministic product regressions on Windows, and retains unsigned package/routing builds and smoke-harness publication. Formatting remains local. Cancel obsolete PR runs; live provider and interactive UI execution are separate evidence.

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
Owner-approved agent-neutral amendment, 2026-09-14: Integration and remote authority follow CONTRIBUTING. The owner approves Stable promotion and emergency compatibility-policy publication. Never expose signing secrets to arbitrary PRs or agents.

### D-167 - Compatibility manifest
Use signed static GitHub-hosted policy with bundled and cached last-good versions, fetched asynchronously. It may only narrow capabilities, not configure executable code, endpoints, authentication or secrets.

### D-168 - Compatibility and security blocks
Stable hard-blocks compatibility incidents; Preview permits an explicit local override. Security-critical incidents hard-block all channels without override. Cached data and history remain accessible.

### D-169 - Manifest keys
Use an offline root and rotating signing key separate from the MSIX certificate, with versioning, expiry, revocation and anti-rollback. Publication is human-approved and audited. Policy rollback uses a new higher version.

### D-170 - Compatibility watchdog
Run hourly best-effort upstream/dedicated-account checks plus post-merge and manual checks. Confirmed incidents receive highest priority. Never disable a provider automatically on a weak signal.

### D-171 - Watchdog execution boundary
Owner-approved agent-neutral amendment, 2026-09-14: Future detection may run while the owner PC is off; fixes require a current authorized development task. No always-on coding runner initially. Scheduled Actions are not guaranteed continuous monitoring.

### D-172 - Dependency maintenance
Use Dependabot for NuGet and Actions intake, prioritizing security. No blind major upgrades. Do not repeatedly ask the owner to reselect already approved packages.

### D-173 - Feedback
Owner-approved agent-neutral amendment, 2026-09-14: The owner supplies goals and feedback. Translate authorized feedback into coherent outcomes and preserve deferred decisions using the shared development procedure.

## Deferred work

### D-174 - Deferred scope
Owner-approved agent-neutral amendment, 2026-09-14: Defer Android, ARM64/x86 expansion, Widgets, WSL import, multi-window, command palette, Store infrastructure and an always-on development runner.

### D-175 - Later experiments
Develop forecasting and advanced tuning only after measured history. Defer remote policy protocol details to the release-safety task. Do not build a complete platform framework first.

## Provider account context

### D-177 - Codex connection parity with the inspected OMP client
On 2026-09-14 a live console sign-in proved the owner's account carries a `chatgpt_compute_residency` claim, which this implementation's own fail-closed policy refused after a successful token exchange. The owner then explicitly directed that the Codex connection and usage read duplicate the locally cloned OMP implementation rather than deriving an independent policy, taking only the parts needed to authenticate and read usage. Account-context claims such as residency and FedRAMP are therefore not read at all: that client extracts only the workspace identity, and only provider responses decide account context. Local identity consistency stays fail-closed - conflicting access/id workspace claims and a workspace change on refresh are rejected. Quota requests send the same explicit headers as that client: `Authorization`, `User-Agent` and `ChatGPT-Account-Id`, plus the standard transport `Accept`. Self-identifying values stay truthful: `User-Agent: AiUsage/...` and `originator=ai_usage`, never impersonating OMP or the Codex CLI. A live session verified this combination end to end. This supersedes the earlier residency rejection policy and does not authorize arbitrary headers, other providers, durable credential storage or UI integration.

## Branching

### D-178 - Direct main development until the first release
Owner-approved agent-neutral amendment, 2026-09-14: The former direct-main default is superseded. Follow the sole Git policy in [CONTRIBUTING](../../CONTRIBUTING.md#git-policy); historical task permission is not current authority.

### D-179 - Automatic publication of completed task branches
Owner amendment after the agent-neutral migration, 2026-09-14: automatically commit and push each owner-selected task after completion, review and successful required verification. This is a standing task-branch publication instruction, not a direct-main or automatic task-selection grant. The sole operative Git policy and its boundaries are in [CONTRIBUTING](../../CONTRIBUTING.md#git-policy).
