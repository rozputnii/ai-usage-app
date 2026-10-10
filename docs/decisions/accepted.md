# Accepted decision register

Consolidated: 2026-09-12. Source: the owner conversation. These are normative product and engineering decisions, not claims of implemented code.

The latest explicit owner decision supersedes earlier alternatives. Technical corrections and experiments are separated in `technical-audit.md`; they must not silently rewrite product intent. Execution amendments are explicitly marked below; superseded originals, including R-176, are retained in the historical register. Unrelated product decisions remain in force.

## Product and boundaries

### R-001 - Product name
AI Usage; root namespace AiUsage; executable AiUsage.exe; repository slug ai-usage.

### R-002 - Platforms
One product with separate native implementations. Windows first; Android next. Do not carry Android-specific platform decisions into Windows.

### R-003 - Priority order
Security > native integration > reliability > privacy/local-first > maintainability > simplicity.

### R-004 - Initial target
Windows 11 24H2 or later, build 26100 or later, x64. .NET 10, C#, WinUI 3 and Windows App SDK; packaged MSIX.

### R-005 - Deployment
Framework-dependent deployment was explicitly selected. The .NET runtime and Windows App SDK runtime are separate prerequisites; prove installation on a clean machine.

### R-006 - Local-first operation
No proprietary account, backend, synchronization or telemetry server. Call providers directly; use GitHub/App Installer for distribution. The signed static compatibility manifest is a narrowly approved exception.

### R-007 - Providers
Deliver vertical slices in this order: Codex, Claude, GitHub Copilot, Antigravity. Do not claim complete ChatGPT or Gemini consumer coverage from a particular CLI quota source.

### R-008 - Release scope
Publish early runnable 0.x builds. Public v1.0 requires all four providers. An untested or nonfunctional provider login is not a completed integration.

### R-009 - License
MIT, with a public GitHub repository from the outset. Direct distribution is free. A future paid Store acquisition has identical functionality, without Pro feature gates.

### R-010 - Contributions
External Issues are intake; external PRs are accepted with DCO. Include CONTRIBUTING, SECURITY, private vulnerability reporting and license notices. Never invent Git author or DCO identity.

### R-011 - Android sharing
Share requirements, provider semantics, research and sanitized fixtures. Do not impose a shared .NET UI or Core implementation on Android in advance.

### R-012 - Development harness
Owner-approved agent-neutral amendment, 2026-09-14: Development is agent-neutral. Root AGENTS.md is the common entry point; specialized guidance has one active copy in .agents/skills. No workflow runtime or plugin is required.

Amended by R-217 (2026-10-09): agent contracts in `.agents/agents/`, thin Claude Code pointers, and project-level plugin switches.

## AI workflow and autonomy

### R-013 - Method
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-014 - Execution modes
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-015 - Approval
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-016 - Questions
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-017 - Granularity
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-018 - Proportional documentation
Owner-approved agent-neutral amendment, 2026-09-14: Use proportional records under CONTRIBUTING and docs/workflow/formats.md: small fixes need a brief plan and evidence; features need spec and verification with optional tasks. Designs explain meaningful choices; ADRs record durable decisions.

### R-019 - Canonical backlog
Owner-approved agent-neutral amendment, 2026-09-14: docs/backlog.md owns feature status and evidence references, using the existing status vocabulary. Status alone never authorizes execution.

### R-020 - Prioritization
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-021 - Goals and owner inbox
Owner-approved agent-neutral amendment, 2026-09-14: docs/product/goals.md owns outcomes and goal membership, without session permission chronology. docs/decisions/pending.md records unresolved owner decisions.

### R-022 - Execution state
Owner-approved agent-neutral amendment, 2026-09-14: Optional tasks.md owns internal task state and a concise handoff. Required structured fields are status, depends_on, acceptance and evidence; parallel ownership is defined in docs/workflow/formats.md.

### R-023 - Resume
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-024 - Pause
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-025 - Autopilot sessions
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-026 - Selective blocking
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-027 - Budgets
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-028 - No artificial pauses
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-029 - Plan Mode
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-030 - OMP version policy
Owner-approved agent-neutral amendment, 2026-09-14: AI client versions are optional environment context, not product build prerequisites. Historical OMP compatibility evidence remains historical.

### R-031 - Profile
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-032 - Memory
Owner-approved agent-neutral amendment, 2026-09-14: Memory is supplementary, never authority. The repository must support recovery without previous conversation history.

### R-033 - Portable behavior
Owner-approved agent-neutral amendment, 2026-09-14: Version authored instructions, shared guidance, documentation and validation in Git; keep credentials and generated local state out.

### R-034 - Skills
Owner-approved agent-neutral amendment, 2026-09-14: Keep essential instructions and the selective map in root AGENTS.md. Read specialized .agents/skills guidance on demand; native discovery is optional.

Amended by R-217 (2026-10-09): thin native wrappers in `.claude/skills/` point to the canonical `.agents/skills/` copies.

### R-035 - AI infrastructure changes
Owner-approved agent-neutral amendment, 2026-09-14: Respect current owner scope for workflow changes. Repository guidance does not grant native tool privileges.

### R-036 - Models
Owner-approved agent-neutral amendment, 2026-09-14: No model vendor or family is required. Use available tools proportionally and report unavailable required independent review.

### R-037 - Advisor
Owner-approved agent-neutral amendment, 2026-09-14: An Advisor is not required. Review scope and evidence follow CONTRIBUTING.

### R-038 - Parallelism
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-039 - Workers
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-040 - Integration
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-041 - Review
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-042 - Findings
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-043 - Git
Owner-approved agent-neutral amendment, 2026-09-14: The sole current Git policy is [CONTRIBUTING](../../CONTRIBUTING.md#git-flow). This decision grants no remote action or main integration.

### R-044 - Permissions
Owner-approved agent-neutral amendment, 2026-09-14: The former execution prescription is superseded by the shared [development procedure](../../CONTRIBUTING.md). Its original text is retained in the superseded register as historical context, not an active instruction.

### R-045 - Validator
Owner-approved agent-neutral amendment, 2026-09-14: The local validator checks IDs, statuses, links, acceptance, dependencies, safe paths and evidence without runtime dependencies. Main protection remains deferred in T-026; local evidence is not remote enforcement.

### R-046 - Documentation drift
Use docs-as-code. Specs and designs may evolve within the approved intent. Never weaken acceptance criteria or security requirements merely to make failing implementation appear successful.

### R-047 - Provenance
Research uses structured frontmatter and Markdown with verification dates, source URLs and exact refs, classification and confidence. Revalidate relevant provider contracts before changing them.

### R-048 - Spikes
Probe uncertain authentication, APIs or stack integration with a disposable spike answering one question. A throwaway proof is not production readiness.

### R-049 - Native workflow UX
Owner-approved agent-neutral amendment, 2026-09-14: The executable OMP bridge is retired. Thin adapters point to root AGENTS.md; no replacement orchestrator is introduced. Historical evidence is retained in docs/archive/omp.

## Windows stack and code

### R-050 - Solution
Use AiUsage.Windows, AiUsage.Core and AiUsage.Infrastructure. Organize by feature/provider; do not create a project for every provider.

### R-051 - Project boundaries
Core is UI/platform-neutral. Windows composition references Infrastructure; provider transport DTOs do not enter the UI. Minimize the public API; use internal classes and InternalsVisibleTo for tests.

Owner amendment (2026-09-13): develop provider integrations as a reusable UI-independent library and verify them through a console application before connecting them to WinUI. The console and UI must consume the same implementation, not separate provider clients. Start with the existing Core/Infrastructure library boundaries; a project per provider or a speculative universal framework is not required. Provider research and library/console verification must not depend on UI readiness. Console proof does not replace later UI lifecycle verification or authorize live account access, unsafe credential output, or source CLI credential-store mutation.

### R-052 - Host
Use Microsoft.Extensions.Hosting Generic Host for DI, configuration, logging and lifetime. Coordinate startup/shutdown with WinUI; do not block the dispatcher with console-style Run.

### R-053 - MVVM and routing
Use CommunityToolkit.Mvvm and MVVM-first routing. Uno.Extensions.Navigation is the selected candidate; prove standalone WinUI integration with a spike without silently switching to a cross-platform UI framework.

### R-054 - HTTP
Use IHttpClientFactory, typed clients, System.Text.Json and Microsoft.Extensions.Http.Resilience with provider- and operation-specific pipelines. Do not apply blanket retries to authentication POSTs.

### R-055 - Serialization
Use source generation for stable DTOs. Use explicit dynamic parsing or reflection only where needed. Reflection cannot automatically repair schema drift.

### R-056 - Database
Use SQLite, EF Core 10 and Microsoft.EntityFrameworkCore.Sqlite with migrations. Enable WAL, keep transactions short, use IDbContextFactory and one context per operation. No generic repository.

### R-057 - Single writer
Use Channel<T> for application database writes, including settings, history and maintenance. Network fetches run independently in parallel. Upgrades, imports and resets acquire exclusive maintenance ownership.

### R-058 - Publication order
API response -> normalization -> committed database transaction -> immutable store -> UI. A per-account update must preserve concurrent results from other accounts.

### R-059 - Messaging
Use BCL Channel<T> for work queues, not broadcast. Provide explicit keyed deduplication. No MediatR, Rx or global magic event bus.

### R-060 - Live state
Use immutable live/current AppState. IStateStore<T> exposes Current and Subscribe returning IDisposable. Do not keep complete history in live state.

### R-061 - UI threading
Implement IUiDispatcher over DispatcherQueue in the Windows layer. Manage subscription lifetimes explicitly. Domain logic does not know the UI thread.

### R-062 - History reads
Use IUsageHistoryQueryService with AsNoTracking projections filtered by account, context, limit, range and resolution. Run database work off the UI thread.

Owner scope amendment, 2026-09-22: [T-011](../specs/T-011-provider-history/spec.md)
now delivers provider-supplied history for all four connected provider types, with automatic
loading and minimal interaction. Database-backed observation collection, retention, rollups
and offline history are deferred to T-029 at low priority after product stability. R-062's
database query shape and R-135/R-136's local collection/retention rules remain future direction,
not requirements to add persistence to T-011. R-063's ranges and R-122's charts apply only
where the provider actually supplies suitable historical data; do not invent coverage.

Further owner amendment, 2026-09-22: T-011 may use existing authorization only and must
match OMP's provider-history method if one exists. Additional sign-in, web sessions,
reporting credentials and permission expansion are excluded. OMP's locally recorded
history is not provider-supplied history and does not reopen deferred T-029.

### R-063 - History resolution
Perform resolution-aware querying/downsampling in the query layer. Offer 24h, 7d, 30d, 90d and 1y presets plus custom ranges. Bound points to rendering needs.

### R-064 - Time
Persist UTC instants and display local time/culture. Use TimeProvider in application logic and tests.

### R-065 - Logging
Use Serilog through Generic Host with local rolling files. Apply token/cookie/PII-safe allowlisting and redaction, including in Debug builds.

### R-066 - Charts
Use LiveCharts2 for main-dashboard sparklines and History. Do not add charts to the tray popup.

### R-067 - Tray and notifications
Use H.NotifyIcon.WinUI and Windows App SDK AppNotificationManager.

### R-068 - Errors
Use a small in-house Result<T> with sealed typed error records. Model expected authentication, transient, schema and cancellation outcomes. Do not swallow programming errors.

### R-069 - Cancellation
Cancellation is an expected outcome, not an account warning/failure. At the boundary, catch only the relevant OperationCanceledException. Treat timeout separately.

### R-070 - Code quality
Enable nullable analysis, analyzers and repository .editorconfig. Owned-code warnings fail CI; third-party suppressions must be narrow and justified. Use xUnit v3.

### R-071 - Performance
Target a usable cached cold startup within 500 ms on a defined reference machine. This is a measured target, not an unverified guarantee. Do not use flaky hosted-CI timing gates.

### R-072 - AOT
Native AOT is not a v1 requirement. Start with normal framework-dependent .NET 10. Consider ReadyToRun/AOT only after profiling; no premature trimming constraints.

## Accounts, authentication and quotas

### R-073 - Multiple accounts
Support multiple accounts per provider in both the model and UI from the outset. Support custom labels. Keep provider identity separate from labels, email and tokens.

### R-074 - Contexts
Represent account -> organization/workspace/project contexts -> quota groups -> limit windows. Share credentials across contexts only when the actual grant permits it.

### R-075 - Context discovery
Add all available discovered contexts, with hide controls. Temporary disappearance must not delete history. Restore the same context when its stable ID returns.

### R-076 - Identity
Use a stable provider identity plus required grant/context scope. A fallback fingerprint must be evidence-based; email or a token hash is never the universal identity key.

### R-077 - Quota model
Use a common normalized model plus typed provider-specific extensions. Do not limit it to primary and secondary windows; support shared, model-specific and feature-specific pools.

### R-078 - Quota-group examples
Show Spark or other Codex buckets and Antigravity model groups only when the provider actually returns them. Do not hardcode group membership from conversation examples.

### R-079 - Units
Remaining percentage is primary, with native absolute credits/requests/other units secondary. Unknown, unlimited, exhausted and stale are distinct states. Never invent total counts.

### R-080 - No fabricated totals
Do not sum or average unrelated quotas or accounts. Do not double-count shared pools.

### R-081 - OAuth client strategy
Use an app-owned public client where supported and an OMP-compatible public-client flow where justified. Record support and policy uncertainty; MIT licensing does not grant provider authorization.

### R-082 - Browser authentication
Use the system browser and a provider-compatible loopback redirect, with PKCE/state where the protocol supports them; use device flow where appropriate. No embedded WebView login.

### R-083 - Client secrets
Do not embed confidential OAuth client secrets in the application. A desktop-client registration does not establish permission for another product.

### R-084 - Final application secret store
Use DPAPI CurrentUser with a separately versioned encrypted record per account/grant in package-owned LocalState/Secrets. Windows Credential Manager as the application secret store is superseded.

### R-085 - Secret persistence
Never write plaintext secrets to disk or logs. Store an opaque CredentialId in the database. Write temporary ciphertext and atomically replace, with per-credential serialization and recoverable format migrations.

### R-086 - CLI import
Provide explicit Import and Import all. Run incremental asynchronous discovery on first launch and on manual scan. Search known native Windows locations only; no full-disk scan or WSL integration in v1.

### R-087 - CLI execution
Passive reading is the default. CLI execution needs a justified provider-specific exception with a trusted executable, fixed arguments, timeout/cancellation and no shell, elevation or source-store writes.

### R-088 - Import all
Import new supported candidates only, handling partial failures independently. Existing accounts show Already added or require explicit Update credentials/Re-import.

### R-089 - Re-import
Preserve history, settings, labels and order. Verify replacement credentials before promoting them. Failed validation must not destroy a still-working record.

### R-090 - CLI coexistence
The goal is fewer clicks and an app-local credential copy. Prove token-lifecycle independence; copied rotating refresh tokens can conflict with the CLI.

### R-091 - Connection flow
Provider picker -> supported methods -> credential/identity verification -> initial quota. Temporary quota failure means connected/verification pending, not falsely healthy or automatic credential loss.

Amended by R-180 (2026-09-24): Single-window shell, one-click sign-in and immediate sign-out.

### R-092 - Reauthentication
An invalid or revoked refresh grant puts only that account into Re-auth required. Preserve stale cache/history and keep other accounts operational. No endless retries.

### R-093 - Disconnect
Delete application credentials and stop active monitoring; retain history and identity for reconnect. Hide disconnected accounts by default with Show disconnected accounts available.

### R-094 - Reconnect
Reattach history, settings, labels and order to the same stable identity. Delete account data is a separate destructive action.

### R-095 - Provider evidence
Review sources before authentication, quota or parser changes. Use real sanitized fixtures or clearly marked synthetic cases, golden outputs and critical semantic assertions.

### R-096 - Parsing
Ignore unknown fields, but leave missing optional quota values unknown rather than zero. Critical semantic mismatch returns SchemaMismatch with safe diagnostics; never fabricate quota data.

### R-097 - Provider capabilities
Bundle providers and register them explicitly in DI, using optional capability interfaces. No runtime DLL plugins. Create interfaces for independent behavior; quota groups may simply be data returned by usage capability.

### R-098 - Feature flags
Use typed flags plus detected capabilities in Advanced/Experimental settings. Retire temporary flags through migration; flags cannot bypass safety controls.

## Refresh and reliability

### R-099 - Refresh cadence
Use adaptive refresh with roughly five minutes as the starting baseline and provider-specific minimum/reset/backoff/stability behavior. Manual refresh is immediate except for real server Retry-After constraints.

### R-100 - Network concurrency
Do not impose an arbitrary global SemaphoreSlim(4) on independent accounts. Work is bounded by actual accounts and requests; respect observed 429/Retry-After and host constraints.

### R-101 - Background deduplication
Coalesce timer, network-recovery and sleep-resume triggers for the same account. Manual refresh cancels/replaces quota fetching, not the credential-rotation transaction.

### R-102 - Manual replacement
Use per-account/request generation checks to prevent superseded results from persisting, publishing or alerting. Do not cancel other accounts. Repeated clicks must not create uncontrolled duplicate work.

### R-103 - Refresh actions
Provide Refresh all and per-account Refresh. Skip disabled and Re-auth required accounts; isolate errors.

### R-104 - Sleep and connectivity
On resume or connectivity recovery, refresh only stale accounts under the same policy. Connectivity signals are not proof of provider reachability; actual HTTP outcomes remain authoritative.

### R-105 - Offline operation
Startup, dashboard and history remain usable from cache offline. Clearly mark stale data and update asynchronously. A provider error must not replace the whole dashboard with an error screen.

### R-106 - Power awareness
Reduce automatic work on Battery Saver or metered connections, showing the reason and allowing a settings override. Manual refresh remains available. Do not wake the PC.

### R-107 - Locked session
Continue background monitoring while Windows is locked. Do not open consent/UI dialogs. Unlock must not trigger redundant refresh when data is fresh.

### R-108 - Process lifetime
Use a tray-resident BackgroundService. Close hides the main window, Minimize behaves normally, and Exit stops the Host. No out-of-process background task component in v1.

### R-109 - Shutdown
Cancel network work and bound draining of pending writes. Do not delay logout with lengthy network operations. Fatal crashes terminate the process without a watchdog restart loop.

### R-110 - Crash recovery
Use a clean-shutdown marker and targeted checks for interrupted operations. Do not perform routine full-database or filesystem validation.

## UI and notifications

### R-111 - First run
Show an empty dashboard with Add account, not a wizard. Discover CLI candidates asynchronously and progressively; support individual and bulk import.

Amended by R-180 (2026-09-24): Single-window shell, one-click sign-in and immediate sign-out.

### R-112 - Window behavior
Use one application instance and one main window. Repeated launch/notification activation opens the existing window. Restore size, position, maximization and monitor while correcting off-screen bounds. Always on top defaults off.

### R-113 - Startup UX
No splash screen. Display cached data first, with no startup network or maintenance wait. Autostart is hidden-to-tray and opt-in; Exit does not clear the startup preference.

### R-114 - Dashboard layout
Use Overview and an adaptive one/two/three-or-more-column grid with balanced density. Preserve user order instead of dynamic risk sorting; highlight states.

Amended by R-193 (2026-10-07): Single-column cards and a minimal sliding settings sheet.

### R-115 - Overview
Show provider/account counts, the lowest remaining quota and nearest reset with source/freshness, plus warning/critical/reauth counts. No global usage percentage.

Amended by R-181 (2026-09-24): Compact usage view and daily pace colors.

### R-116 - Cards
Use account cards with nested primary windows and quota groups. Primary limits remain visible; groups default collapsed and expand for warning/critical states while respecting manual preference.

Amended by R-181 (2026-09-24): Compact usage view and daily pace colors.

### R-117 - Visibility
Hiding a group or limit does not stop monitoring/history. Ask Hide only or Hide and mute alerts. Do not override an explicit hide merely because auto-expansion would otherwise apply.

### R-118 - Reset display
Show relative reset time and the exact local timestamp. Reaching a reset time triggers observation, not an invented 100% remaining value.

Amended by R-181 (2026-09-24): Compact usage view and daily pace colors.

### R-119 - Appearance and accessibility
Offer System, Light and Dark themes, default System. Use Fluent styling, DPI support, keyboard navigation, accessible labels and high contrast. Status cannot rely on color alone.

Amended by R-182 (2026-09-24): Dark-only appearance.

### R-120 - Localization
Use resources immediately, with English-only v1 and English fallback. Do not localize provider IDs, protocol values or persisted keys. Format dates/numbers using Windows culture.

### R-121 - Branding
Use official provider assets only after brand/license review; use neutral fallback icons when redistribution is not permitted.

### R-122 - History UI
Provide main-dashboard sparklines and a dedicated History view. Forecasting is Experimental, explicitly labeled Estimate and cannot change factual quota or notification behavior.

Amended by R-181 (2026-09-24): Compact usage view and daily pace colors.

### R-123 - Thresholds
Default remaining thresholds are 25%, 10% and 0%, with global -> provider -> account -> limit overrides and reset notices.

### R-124 - Alert deduplication
Use stateful threshold crossing and hysteresis, rearming only after verified recovery/reset. Alert on fresh observations only. Aggregate simultaneous alerts into one notification.

### R-125 - Notification activation
Open/focus the affected account and group in the main UI. Do not add a separate persistent alert-history entity; use Windows Notification Center and local diagnostics.

### R-126 - Tray severity
Priority: Re-auth required > Offline/Error > Critical quota > Warning quota > Normal. Explain the cause, freshness and affected account in the tooltip; do not fabricate a minimum quota.

### R-127 - Tray interaction
Single left-click opens the mini-dashboard, double-click opens the main app, and right-click shows Open, Refresh all, Settings and Exit.

### R-128 - Tray popup
Show current quota, reset and status for all accounts/groups with per-account and global refresh. No charts. Keep it read-mostly; settings live in the main app. Use the same central state store.

Amended by R-187 (2026-10-02): T-034 tray miniature, no OK pill, day off, rush and on extra usage.

### R-129 - Diagnostics
Provide a System Status page with build/commit/schema/provider evidence/status, latest refresh/update/manifest state and Export diagnostics, Open logs and Run health check actions.

Amended by R-180 (2026-09-24): Single-window shell, one-click sign-in and immediate sign-out.

### R-130 - Health checks
Diagnosis only; repairs are separate explicit actions. Do not disguise state mutation as a health check.

### R-131 - No application lock
Rely on Windows user-session protection; no separate PIN or Windows Hello gate in v1.

## Data lifecycle and upgrades

### R-132 - Storage scope
Use per-user package-owned ApplicationData local storage through IAppPaths. Canonical roots: Data, Secrets, Cache, Logs, Diagnostics, Backups and Temp. User-selected exports are not silent cleanup targets.

Amended by R-188 (2026-10-07): Usage history survives reinstall.

### R-133 - Settings
Use SQLite/EF with strongly typed core entities and a typed extensible provider store. Keep immutable defaults in application configuration. Validate writes, imports and migrations, not the entire state at every startup.

### R-134 - Database privacy trade-off
Do not encrypt the whole SQLite database. Never put plaintext OAuth credentials in it. Account metadata and usage/history remaining unencrypted is an explicitly accepted trade-off.

### R-135 - History collection
Persist normalized samples only, enabled by default. Disabling collection stops new samples; old history is separately deletable. No routine raw-response archive.

### R-136 - History retention
Keep raw normalized observations for approximately 90 days, then hourly/daily rollups for approximately one year, configurable. Deduplicate unchanged values while preserving freshness/coverage. Do not sum percentages across resets.

### R-137 - Diagnostic retention
Owner amendment (2026-10-03, T-043 implementation selection): retain bounded local
structured events and sanitized evidence for every executed provider response. Maximum
ages are 72 hours for opt-in traces, 168 hours for application/response evidence, and
one calendar month for critical incidents. Size budgets may evict earlier. Unknown
scalar values and unsafe property names are withheld by endpoint policy; blacklist-only
redaction is insufficient. Safe exception type/stack/inner-chain projection is permitted,
but arbitrary messages, private paths, credentials and raw payload arguments are not.
See [T-043](../specs/T-043-file-logging/spec.md) and its verification for delivery gates.

### R-138 - Telemetry
No cloud or automatic crash telemetry, and no automatic memory dumps. Diagnostic export is manual and sanitized; exception details are allowlisted.

### R-139 - Migration versions
Version database, configuration, infrastructure and secret schemas independently from product version. Published migrations become immutable after any public Preview.

### R-140 - Forward upgrade contract
Upgrade directly from every published Stable/Preview persistent schema without intermediate binaries. Do not support downgrade. Refuse normal operation on a newer unsupported schema.

### R-141 - Upgrade coordination
Preflight -> consistent protected backup -> ordered database/configuration/filesystem/secret steps -> validation -> cleanup -> normal services. Apply pending steps only and recover interrupted phases.

### R-142 - Filesystem migration
Use idempotent, restart-safe detect/stage/verify/publish/cleanup steps with a journal. Do not pretend that a database transaction atomically covers filesystem changes.

### R-143 - Cleanup
Clean only canonical application-owned roots with path/reparse safeguards. Apply retention to temporary files, logs, cache and orphans. Never delete Windows-managed MSIX binaries or user Downloads.

### R-144 - Migration backup
Keep one last verified pre-migration durable-state bundle protected with DPAPI CurrentUser. Include database, configuration/layout manifests and irreplaceable nonsecret files; exclude cache, logs, temp, diagnostics and OAuth secrets.

### R-145 - Backup rotation
Do not replace the last good backup with a half-migrated state during retries. Retire the old checkpoint only after the new operation succeeds. Secret rollback retains its own encrypted generation, never portable-export secrets.

### R-146 - Restore
Validate decryption, manifest, hashes, schema and SQLite integrity. Explicitly restore a consistent previous durable state and rerun current migrations. No automatic endless restore/retry loop.

### R-147 - Recovery mode
Migration failure exposes Retry, Restore, Export diagnostics and Open data folder. Never silently wipe data or launch the current app against an incompatible old schema.

### R-148 - Upgrade tests
Retain historical schema/layout/configuration fixtures. Test interruption recovery, preserved values and credential-reference reconciliation. Back up WAL databases consistently rather than copying only a live database file.

### R-149 - Portable export
Use an open, unencrypted documented bundle with versioned manifest, settings, accounts and history. Warn about private metadata; exclude OAuth credentials, cookies and other secrets.

### R-150 - Portable import
Replace only, not merge. Validate, stage and migrate replacement state before cutover; create a pre-import backup and support recovery. Preserve credentials only for matching stable identities; remove orphan secrets after commit.

### R-151 - Settings reset
Reset settings preserves accounts, credentials, history, custom labels and order. Safely reset core preferences, including opted-in startup behavior.

### R-152 - Factory reset
Require explicit full-reset confirmation, stop producers, drain/cancel writes and close handles. Clear application-owned state, secrets, backups and owned notification/startup state, then restart into first-run UX. Do not literally reinstall or require a download.

### R-153 - Deletion boundaries
Disconnect/reset must not modify original CLI credentials or delete exports saved to user-selected locations. Local deletion does not promise remote grant revocation.

## Release, CI and operations

### R-154 - CI
Owner-approved agent-neutral amendment, 2026-09-14: CI retains validator regressions and document validation, adds deterministic product regressions on Windows, and retains unsigned package/routing builds and smoke-harness publication. Formatting remains local. Cancel obsolete PR runs; live provider and interactive UI execution are separate evidence.

Amended by R-220 (2026-10-09): CI no longer builds the routing spike.

### R-155 - Test-first policy
Use test-first for parsers, authentication, token refresh, repositories, security logic and bug fixes. Test UI/configuration proportionally. Never fabricate passing verification.

### R-156 - Live smoke tests
Keep them out of normal PR CI. Use explicit local provider tests or dedicated CI accounts with safe rotation. Never expose personal credentials or secrets to public-fork jobs.

### R-157 - Release channels
One direct installation and identity supports Stable and Preview. Every green main merge becomes Preview. Stable promotion manually selects the exact signed, tested artifact without rebuilding or resigning.

Owner amendment (2026-10-06, AIU045-D1): Previews publish only through an explicit owner dispatch of a green main commit; every green main merge is a candidate, not a release.

Owner amendment (2026-10-06, later the same day): reverses the AIU045-D1 amendment. Every green main push publishes a Preview automatically again; an owner dispatch with `PublishPreview=true` can republish.

Amended by R-216 (2026-10-09): a push publishes only when product inputs changed since the Preview the feed serves.

### R-158 - No downgrade on channel switch
Switching Preview to Stable changes the feed but keeps the newer installed binary until Stable catches up. Keep ForceUpdateFromAnyVersion disabled.

### R-159 - Distribution
Early internal 0.x uses self-signed MSIX and manual update. Before public v1, prove .appinstaller auto-updates using GitHub Releases assets and Pages feeds. No application-owned installer cache by default.

### R-160 - Update UX
Check asynchronously without blocking startup. Show Update available/ready and use explicit Restart & update. Never restart automatically, including from tray or during a security incident.

### R-161 - Signing
Prefer Azure Artifact Signing for public builds, subject to actual eligibility. Keep publisher identity stable and test rotation. Reuse the initial local development certificate; exclude its private key from Git.

Owner amendment (2026-09-24): the app is a personal tool. It is distributed only as the self-signed development channel through GitHub Releases and Pages, for the owner and anyone who explicitly chooses to trust its public certificate. Publicly trusted signing, Microsoft Store and Stable are deferred. The owner is ineligible for Artifact Signing and allows free options only; see RELEASE-001.

### R-162 - Microsoft Store
Add later as paid acquisition with the same features. A separate package identity may require a dedicated direct-to-Store migration. Do not assume an ordinary same-family update.

### R-163 - Version numbering
Separate SemVer-style product version from four-number MSIX YYYY.M.DDNN.0. CI must serialize allocation and guard against overflow and out-of-order publication.

### R-164 - Artifact retention
Keep the latest 50 Preview builds and retain Stable/persistent-state milestones indefinitely. Also retain currently referenced feeds/candidates regardless of count. Record commit, hash and version in release metadata.

### R-165 - Promotion criteria
Use risk-based evidence, with no mandatory soak. A compatibility-only hotfix may release hourly after verification. Authentication, storage or dependency scope expansion increases the risk class.

### R-166 - Release authority
Owner-approved agent-neutral amendment, 2026-09-14: Integration and remote authority follow CONTRIBUTING. The owner approves Stable promotion and emergency compatibility-policy publication. Never expose signing secrets to arbitrary PRs or agents.

### R-167 - Compatibility manifest
Use signed static GitHub-hosted policy with bundled and cached last-good versions, fetched asynchronously. It may only narrow capabilities, not configure executable code, endpoints, authentication or secrets.

### R-168 - Compatibility and security blocks
Stable hard-blocks compatibility incidents; Preview permits an explicit local override. Security-critical incidents hard-block all channels without override. Cached data and history remain accessible.

### R-169 - Manifest keys
Use an offline root and rotating signing key separate from the MSIX certificate, with versioning, expiry, revocation and anti-rollback. Publication is human-approved and audited. Policy rollback uses a new higher version.

### R-170 - Compatibility watchdog
Run hourly best-effort upstream/dedicated-account checks plus post-merge and manual checks. Confirmed incidents receive highest priority. Never disable a provider automatically on a weak signal.

### R-171 - Watchdog execution boundary
Owner-approved agent-neutral amendment, 2026-09-14: Future detection may run while the owner PC is off; fixes require a current authorized development task. No always-on coding runner initially. Scheduled Actions are not guaranteed continuous monitoring.

### R-172 - Dependency maintenance
Use Dependabot for NuGet and Actions intake, prioritizing security. No blind major upgrades. Do not repeatedly ask the owner to reselect already approved packages.

### R-173 - Feedback
Owner-approved agent-neutral amendment, 2026-09-14: The owner supplies goals and feedback. Translate authorized feedback into coherent outcomes and preserve deferred decisions using the shared development procedure.

## Deferred work

### R-174 - Deferred scope
Owner-approved agent-neutral amendment, 2026-09-14: Defer Android, ARM64/x86 expansion, Widgets, WSL import, multi-window, command palette, Store infrastructure and an always-on development runner.

### R-175 - Later experiments
Develop forecasting and advanced tuning only after measured history. Defer remote policy protocol details to the release-safety task. Do not build a complete platform framework first.

## Provider account context

### R-177 - Codex connection parity with the inspected OMP client
On 2026-09-14 a live console sign-in proved the owner's account carries a `chatgpt_compute_residency` claim, which this implementation's own fail-closed policy refused after a successful token exchange. The owner then explicitly directed that the Codex connection and usage read duplicate the locally cloned OMP implementation rather than deriving an independent policy, taking only the parts needed to authenticate and read usage. Account-context claims such as residency and FedRAMP are therefore not read at all: that client extracts only the workspace identity, and only provider responses decide account context. Local identity consistency stays fail-closed - conflicting access/id workspace claims and a workspace change on refresh are rejected. Quota requests send the same explicit headers as that client: `Authorization`, `User-Agent` and `ChatGPT-Account-Id`, plus the standard transport `Accept`. Self-identifying values stay truthful: `User-Agent: AiUsage/...` and `originator=ai_usage`, never impersonating OMP or the Codex CLI. A live session verified this combination end to end. This supersedes the earlier residency rejection policy and does not authorize arbitrary headers, other providers, durable credential storage or UI integration.

## Branching

### R-178 - Direct main development until the first release
Owner-approved agent-neutral amendment, 2026-09-14: The former direct-main default is superseded. Follow the sole Git policy in [CONTRIBUTING](../../CONTRIBUTING.md#git-flow); historical task permission is not current authority.

Superseded by R-208 (2026-10-09): task branches and verified merges into `main`.

### R-179 - Automatic publication of completed task branches
Owner amendment after the agent-neutral migration, 2026-09-14: automatically commit and push each owner-selected task after completion, review and successful required verification. This is a standing task-branch publication instruction, not a direct-main or automatic task-selection grant. The sole operative Git policy and its boundaries are in [CONTRIBUTING](../../CONTRIBUTING.md#git-flow).

Superseded by R-208 (2026-10-09): task branches and verified merges into `main`.

## Owner UI amendments

### R-180 - Single-window shell, one-click sign-in and immediate sign-out
Owner amendment, 2026-09-24: the interface had too many steps and transitions. The main window is one usage view with no navigation tabs; a Settings icon on the right shows all settings, with System Status as its last section rather than a separate page. Account detail and History open from the account panel with Back, not from tabs. There is no Add account dialog: a provider menu opens on hover, one provider click starts browser sign-in, progress stays inline, and success adds the account without a confirmation step. First run lists the providers directly. Each account panel has an icon-only sign-out that disconnects without confirmation. This amends R-091 (no method step; the manual code stays available inline where a provider supports it), R-111 (first run lists providers instead of an Add account button), R-129 (System Status becomes a settings section) and the T-010 navigation proposal. R-093 retention is unchanged: sign-out keeps history, labels, order and identity, and Delete stored data stays separate and confirmed. See [T-030](../specs/T-030-single-window/spec.md).

Amended by R-183 (2026-09-26): Budget-aware single-window redesign direction.
Amended by R-193 (2026-10-07): Single-column cards and a minimal sliding settings sheet.

### R-181 - Compact usage view and daily pace colors
Owner amendment, 2026-09-24: the usage view shows only the limit bars. One line per account holds its label, at most one status mark (failed refresh, stale reading, or pace attention), one bar per primary window and the sign-out icon; window names appear once per provider. The summary block, plan, History link, freshness pill, percentages, reset times and dates leave the line: bar hover text and accessible names carry the reading, the relative and exact reset and the pace advice, an exhausted bar shows "Back in …", and account detail keeps other groups, contexts and history. Bar color is pace advice: a window shorter than a day is red at 20 % or less remaining with no time pacing; a window of a day or longer splits its remainder into even local-calendar-day shares with carry-over, turning orange below 30 % of today's share and red once it is used, with a mark at the end-of-today pace position. Stale, unknown, unlimited and untimed readings get no advice. This amends R-115 (the summary leaves the Overview; the tray keeps it), R-116 (groups stay in detail), R-118 (the reset is shown on demand and when exhausted) and R-122 as it applies to the Overview (advice, not a forecast, and it never changes readings, detail, the tray or notification thresholds). See [T-031](../specs/T-031-compact-pace/spec.md).

Amended by R-183 (2026-09-26): Budget-aware single-window redesign direction.

### R-182 - Dark-only appearance
Owner amendment, 2026-09-24: the app has one appearance, the designed dark palette, in every window, dialog, the tray popup and the title bar, whatever the Windows app mode. The System and Light themes, the theme setting and the app's own high-contrast token set are removed, and the demo no longer simulates the Windows app mode or a contrast theme. Keyboard access, accessible names, display scaling, reduced motion and the rule that status never relies on color alone stay. This amends R-119. A stored theme value is kept as unknown data rather than rewritten. See [T-032](../specs/T-032-dark-only-cleanup/spec.md).

### R-183 - Budget-aware single-window redesign direction
Owner direction, 2026-09-26: the interface is redesigned as one laconic window with no account detail view, modal dialog or pop-up. Every limit of every account, including model-scoped weekly limits and credit or monetary pools, is shown inline with inline rename, status marks and settings; actions that need confirmation are confirmed in place and reversible ones offer undo. Supported limits are the subscription-attached windows and pools visible through the existing sign-ins; API-key billing stays excluded, so the constitution's line between consumer quotas and API billing holds. Countable and monetary pools accept an always-available personal cap, a local setting never presented as provider data, and the lower of the cap and the provider limit applies. A work-day calendar (Monday to Friday by default) drives a daily budget for every window or pool of a day or longer: an adaptive norm fixed for the day, shown with the fixed baseline and the deviation from it. A missing provider period defaults to the calendar month, marked as assumed. Weekly remainder and today's norm are also expressed in estimated five-hour sessions. Five-hour windows are amber at 30 % or less remaining and red at 10 % or less, with the reset countdown from amber, and an account's status reflects its binding limit. Provider data is audited first and a Claude Design brief with its own visual identity follows; see [T-034](../specs/T-034-limit-audit-design-brief/spec.md). This direction amends R-180 (confirmations become inline) and R-181 (the 20 % floor and even calendar-day shares) only through the implementation items T-034 proposes; until they land, current behavior stays.

Amended by R-191 (2026-10-07): One card per account with hideable limit sections.
Amended by R-194 (2026-10-08): A personal cap cannot exceed the provider limit.

### R-184 - Local-only usage history
Owner direction, 2026-09-29: usage history comes only from the app's own local tracking of the readings it already takes. Provider-supplied history is no longer a data source, because it can be less accurate than local tracking and adds a second source to reconcile. History is kept, not removed: the start-of-day amount, the five-hour session estimate, work-day budget splits and any history display all derive from one local reading series. T-034 specifies that series; T-029 later extends its retention, rollups and history queries. This supersedes T-011's provider-history retrieval as a data source. Removing the existing retrieval code is left to a proposed follow-up item and is not selected by this decision. No new transport, grant or provider request follows. See [T-034](../specs/T-034-limit-audit-design-brief/spec.md).

### R-185 - Limit model and budget rules accepted at Gate A
Owner decisions, 2026-09-29, at the T-034 Gate A review, which accepted the Phase A research. A credit pool that reports only a balance (Codex `credits.balance`) accepts a personal cap; its use in the budget period is tracked locally from observed balance decreases, ignoring top-ups, and labelled an estimate (PD-034-01, option b). Limits visible only in a provider's own UI (Copilot AI credits, Codex workspace credits and USD budgets, Claude prepaid balances and organization controls, Antigravity credits) are not shown until a current quota source is established through the existing connection; provider history is not such a source (PD-034-03, option a). Three readings of R-183 are confirmed. A window whose provider reset is known but whose duration is not has no budget and shows "period unknown" instead of the calendar-month default. On a day off only the daily budget is neutral; an exhausted or over-cap limit still shows that state. A zero limit, from the provider or a personal cap of 0, shows "not included" or "capped at 0" instead of a zero budget and "over" once used, and an explicit unlimited flag outranks any reported amount. The choice between two snapshot sources (PD-034-02) stays open until an T-005 implementation is selected. See [T-034 research](../specs/T-034-limit-audit-design-brief/research.md).

Amended by R-189 (2026-10-06): Early five-hour window display and interval session estimate.

### R-186 - T-034 design reference and brief amendments
Owner decisions, 2026-10-01, in the Claude Design round of T-034 (project `9a6b2cdd-1c9c-4abe-9477-2869aa10f9bd`). The owner developed the 1a Ledger direction with Claude Design and selected the page `Provider States Handoff.dc.html` as the design reference for implementation, after the corrections recorded in T-034 verification. The owner lifted the brief's visual-identity restrictions (design-brief section 6, DA-2 and R-08), including the ban on a Claude-like look, and kept dark-only (R-182). The reference amends the Gate B brief as follows. Each subscription is one card. A five-hour window is no longer its own bar: a "today" strip shows today's allowance split into the remaining five-hour windows, with the current window first, and the part of the last window that today's allowance does not cover is grey. Below it, the 7d or month bar rings today's span. A Used or Left setting chooses which value is drawn solid and which is hatched, and the hatching always sits to the right of the solid fill. Colours: green for OK; orange while today's allowance is almost but not yet used up (today low, today short, cap close, a full current five-hour window); red once today's allowance is reached or exceeded, for over cap, and for a limit or cap that is reached, including an exhausted 7d limit (owner, 2026-10-02: today reached is the same red as over). An over label appears only when today's allowance is exceeded. Percentages are whole numbers. This replaces R-09's colouring by a window's own remainder and the visible pace mark, deviation words and per-account status naming its limit. Secondary facts such as the window end time, the remainder beyond a cap and sync details move to tooltips, which must also be reachable by keyboard focus. These rules are unchanged: the truth rules of R-185 (a balance-only Codex pool is tracked as an estimate on its cap, and no plan size is invented), "assumed" on an assumed period, no zero for unknown, WCAG AA contrast, and words for every coloured state. On 2026-10-02 the owner also accepted the companion page `Surfaces Handoff.dc.html` and made Compact the default of Appearance › Density, because only Compact fits the design-brief scenario into the 760 × 600 window; Comfortable stays available. API-key billing stays excluded (R-183); showing it is proposed separately as T-041. See [T-034 verification](../specs/T-034-limit-audit-design-brief/verification.md).

Amended by R-187 (2026-10-02): T-034 tray miniature, no OK pill, day off, rush and on extra usage.
Amended by R-189 (2026-10-06): Early five-hour window display and interval session estimate.
Amended by R-207 (2026-10-09): Tooltips open only under the pointer.

### R-187 - T-034 tray miniature, no OK pill, day off, rush and on extra usage
Owner decisions, 2026-10-02, in the T-034 design conversation, applied to both pages of the Claude Design reference and re-imported. This amends R-186, R-128 (the tray no longer shows resets or per-account refresh) and R-11 of the T-034 specification. The main window shows no "OK" pill: a card without a pill is on track, and the accessible name still says OK. The tray flyout is a miniature of the window: a title row, then per account its name and one today strip per limit in the window's order, in the window's Used or Left mode, with no pills, captions, period bars or buttons; a click opens the window at that account and Refresh stays in the tray menu. Not included and no-cap pools are left out of the tray, and period unknown is an empty dashed track. An account name is red with a warning icon when the sign-in expired or the account is signed out, when a failed sync left a stale reading, or on a provider error; a failed sync with a fresh reading does not turn it red. On a day off no limit is hidden: the today strip shows today's would-be share, the remainder divided by the remaining work days plus today, drawn neutral with a dashed outline and with no orange, red or over label, while a used-up limit stays red. Work today is one switch in the title bar for the whole window; until local midnight it colours the same strips as on a work day and marks on-track cards "extra day", and it never changes the work-day settings. Over- or under-use on any day spreads evenly over the remaining work days through the adaptive norm (R-05, unchanged). On the last work day before a reset that the provider replenishes, with no custom cap on the limit, today's share is the whole remainder: the card shows a green "rush" pill, no 7d or month bar and no grey, and only the 5h windows that fit before the reset; the tray shows a lightning mark. Money (extra usage) and credit pools never rush, and a capped pool keeps its normal look on that day. Claude extra usage and Codex credits keep their own cards and budgets as in R-186 and R-185. While a Claude 5h or 7d window is full and extra-usage spend rose since then, the 5h + 7d card shows an orange "on extra usage" mark with the spend since the window filled; the tray shows a dollar mark. A prepaid balance that the provider shows only in its own UI is not shown. When a provider limit (not a custom cap) is fully used, the card shows no today strip, no 5h cells and no over label, only the red 7d or month bar and when it comes back; this wins over rush, and the tray shows one solid red strip for that limit. See [T-034 verification](../specs/T-034-limit-audit-design-brief/verification.md).

Amended by R-204 (2026-10-08): The tray is a focus-free miniature with provider icons and a five-hour ring.

### R-188 - Usage history survives reinstall
Owner decision, 2026-10-07: the packaged app keeps the local reading series and budget configuration (R-184) in `%LOCALAPPDATA%\AiUsage\History`, which the manifest excludes from MSIX file-system write virtualization with the `unvirtualizedResources` restricted capability, so uninstall leaves it. A DPAPI-protected identity map in the same folder records provider, provider-verified identity and app account ID; signing in again after a reinstall reuses that account ID, so the existing series re-attach without copying or merging, under the same exact-identity rule as R-094. Credentials, labels, order and other preferences still leave with the package. Delete stored data removes the history root as well; uninstall deliberately does not. Existing installs move their store once at startup, and the state layout becomes 3 so older builds refuse it. This amends R-132 (package-owned ApplicationData) for this data only. See [T-047](../specs/T-047-history-survives-reinstall/spec.md).

### R-189 - Early five-hour window display and interval session estimate
Owner decisions, 2026-10-06, in conversation (T-048). The live Claude provider returns whole percentages, so the T-034 estimator rule (three windows, 10-point movement, MAD at most 25 %, 5 weekly points, hidden until ready) kept the five-hour count hidden for long and did not converge. This amends R-185's estimator rule in T-034 research section 7, the design-brief "estimate not ready" row and R-186's "without an estimate there is one strip". A paired weekly card shows the current five-hour window as one today cell before any estimate exists. The estimator computes guaranteed bounds `[L, H]` for the weekly percent per five-hour window from every pair of readings within a window, intersected across windows newest first, and stops at the first conflicting older window. The count shows as soon as `H ≤ 2 L`, as a range "≈ a–b × 5h left", and as a single number once `H ≤ 1.25 L`. Estimates keep the "≈" label and never change quota facts or notifications (R-122); unknown is never shown as zero. See [T-048](../specs/T-048-early-session-estimate/spec.md).

### R-190 - Owner live checks happen after deployment
Owner direction, 2026-10-07: the owner's manual and live-provider checks happen after deployment, in the updated installed app on the owner's computer, not in the unpackaged development build. Agents do not ask the owner to sign in to a development build and do not hold completion, the merge into `main` or the push for such a check. A live clause that needs the owner's signed-in accounts is recorded NOT_RUN as a post-deploy owner check, and the item can be completed once the other required checks pass. After the update, the owner may ask an agent to check the updated installed version, including its logs. Synthetic, demo and unpackaged checks that need no owner sign-in run as before. This supersedes live-run requirements in task and specification acceptance criteria that would otherwise block integration.

### R-191 - One card per account with hideable limit sections
Owner direction, 2026-10-07, in conversation (T-050). R-186 makes each subscription one card; this extends that to every limit of an account. The account's primary limit heads its card, and every other limit, including countable pools such as Copilot Chat and Completions, Codex individual limits and Antigravity model groups, is a section of it, as model limits and spending already were. The owner may hide any section, Fable included, with one rule for all additional limits; the primary limit cannot be hidden. A hidden section leaves only the window: provider facts, readings, caps and the tray stay unchanged. The account card then shows "N hidden" with a dot in the most urgent hidden colour, and activating it shows the sections again. The choice is a local window preference kept with labels and order. This amends R-183's "every limit inline" only by this owner choice. See [T-050](../specs/T-050-one-card-per-account/spec.md).

### R-192 - Attention when a limit is reached, critical only when exceeded
Owner direction, 2026-10-07, in conversation (variant A of three). Card colour follows one rule for every subscription type: reaching or nearing a limit is attention (orange), exceeding or exhausting it is critical (red). `today low`, `today short`, `cap close`, `today used` and `cap reached` are attention; `over today`, `over cap` and a provider period used up are critical. The paired five-hour window warns as `5h low` (attention) when under 15 % of the current window is left and is `5h full` (critical) at 100 %. The `today low` threshold (under 30 % of today's share) is unchanged. This amends the T-038 tone table; see [T-038 section 4.4](../specs/T-038-ledger-presentation/spec.md#44-card-states).

### R-193 - Single-column cards and a minimal sliding settings sheet
Owner direction, 2026-10-07, in conversation (T-051, design B of three). Account cards always stack in one full-width column, whatever the window width and whether settings is open; inline history follows its card. Settings is a floating sheet lighter than the page and the cards, inset from the window edges, that slides in from the right edge and narrows the cards while it is open. The sheet carries as little text as possible: small labels (Work days, Caps, View, Updates), no title or "Esc closes" line, and no explanatory paragraphs; explanations become tooltips. Used/Left stays only in the title bar and Show signed-out accounts only in the + menu. The footer shows the refresh interval, or a local-data or sync problem in its place, and a ⋯ menu holds Preview diagnostics, Open logs, Open data folder, Export recovery summary and Delete stored data, which still confirms in place with a short warning. This supersedes the two-column grid of T-038 S1, S2 and S4 and the adaptive grid of R-114, and amends R-180's "System Status as its last section" and T-038 S5's section list and 400 px panel. See [T-051](../specs/T-051-single-column-settings-sheet/spec.md).

Amended by R-197 (2026-10-08): Settings drops down over the body; the window opens at its minimum width.

### R-194 - A personal cap cannot exceed the provider limit
Owner direction, 2026-10-08, in conversation. A personal cap may equal a known provider limit but never exceed it. The cap field accepts only digits, plus a decimal point and the currency's decimals for money, with no grouping commas, signs or symbols; a keystroke or paste that would exceed a known provider limit is simply not added. On save the editor still refuses a larger amount with "Enter at most <limit> · the provider limit", and both Ledger sources reject it. A cap saved earlier above the limit, or one the provider limit later fell below, is kept but not applied: the card shows the provider limit and names the kept cap in its footer tooltip, and Settings › Caps marks it "above the provider limit · kept, not applied" with Edit. Any cap is still accepted when the provider limit is unknown or unlimited. This amends R-183's "the lower of the cap and the provider limit applies".

### R-195 - Refresh status button and a content-based minimum window size
Owner direction, 2026-10-08, in conversation (T-052, design B of three). The title-row clock is removed; a quiet refresh icon button right after the window title takes its place. It refreshes like F5, turns red while a connected account's data was not refreshed in time (a stale failed sync, an expired sign-in or a provider error) and its tooltip gives reading times without dates. The main window's minimum width is the title row's natural width including the caption buttons, so they never cover the title controls; the settings sheet does not raise it. The minimum height fits the title row, the sign-in strip when shown and the first card's header and primary limit. Both follow the current content. This amends the T-038 window and title-row description. See [T-052](../specs/T-052-refresh-status-minimum-window/spec.md).

### R-196 - Item and decision numbers are assigned at the merge into main
Owner direction, 2026-10-08, in conversation. The owner runs tasks in parallel worktrees, and numbers taken from a stale `main` collided (T-048 and R-189 were renumbered on 2026-10-07, R-195 on 2026-10-08). Branch work names a new backlog item and its specification folder `T-NEW` and a new decision `R-NEW`, with `-2`, `-3` for more. Right before merging, the agent fetches and merges fresh `main`, replaces every placeholder with the next free number there, runs the document validation with `--final`, commits and pushes at once, and repeats if the push is rejected. The validator accepts placeholders for branch work, and its `--final` mode, which CI uses, refuses any placeholder outside code. The owner chose placeholders over provisional numbers. See the [numbering rule](../../CONTRIBUTING.md#numbering).


Amended by R-218 (2026-10-09): placeholders are `T-NEW` and `R-NEW`, numbered by a script.
### R-197 - Settings drops down over the body; the window opens at its minimum width
Owner direction, 2026-10-08, in conversation (settings at full width and the shortest history text, each the recommended variant of two or three). Settings is a sheet over the whole window body, below the title row and the sign-in strip, that rolls down from the top and back up; the cards stay where they are and leave the tab order while it covers them. Inline history shows only the account name, the period and the day count ("7d · 35 days") above the chart: the "use per local day · from this app's readings" text, the "← → day · Esc closes" hint and the legend paragraph are removed, and the keys still work. The main window opens at its content-based minimum width (R-195) instead of 760 px. This amends R-193's sheet that slides in from the right and narrows the cards, and T-038 S4's history header and legend.

### R-198 - Work-day changes apply at once
Owner direction, 2026-10-08, in conversation, after toggling work days showed no change on the cards. A work-day change in Settings applies at once: the cards immediately recalculate today's share, the day-off state and the rush from the new set, and Undo restores the previous set the same way. A change no longer waits for the next local midnight, and the Work days tooltip no longer says it does. Work days that an earlier version saved as pending apply on the first rebuild after the update. Work today in the title bar is unchanged. This amends the T-039 design ("persist work-day changes effective next local midnight") and adds a work-day change to the events after which T-034 R-05 recomputes the adaptive norm during the day.

### R-199 - Today's use by hand and Copilot credits in dollars
Owner direction, 2026-10-08, in conversation (design B of three). A money or credit limit with a daily budget accepts the owner's figure for today's use when the app missed part of the day; it replaces that day's start with the period use at saving minus the figure, applies only on that local date and provider period, also drives that day's history bar, and is kept 35 days. Percent windows have no such figure. The Copilot premium pool (`premium_interactions`, GH-P) is shown in "credits", the owner's identification of the pool as AI credits, and optionally in US dollars at an owner-editable rate per limit, defaulting to GitHub's published $0.01 per AI credit. Dollars are a display of the credit facts, rounded down to the cent; caps stay stored in whole credits, so switching units or changing the rate never rewrites them and the converted provider limit stays the largest cap (R-194). The settings live in a popover on each limit and in the window preferences, not in global settings, and nothing new is sent to a provider. This amends T-008 AC-03's rule that requests and credits are never relabelled, for this pool only. See [T-053](../specs/T-053-today-use-and-credit-dollars/spec.md).

Amended by R-203 (2026-10-08): A cap is set only in the limit settings.

### R-200 - Cards are not tab stops; Esc hides the keyboard focus frame
Owner direction, 2026-10-08, in conversation, after a focus frame stayed on a provider card after an action and Esc did not remove it. A card is no longer a tab stop: Tab moves between the controls inside it, and F2, C, Alt+↑/↓ and ← → work from any of them; Enter on the History button opens history, and its tooltip no longer says "(Enter)". When an action moves focus (closing history, hiding or showing a limit, reordering, ending a rename or cap edit, choosing an account in the tray, arming Delete stored data), focus goes to the card's first control or to Cancel and shows the frame only when the last input was the keyboard. With nothing left to close, Esc hides the frame and focus stays on the same control. This amends the T-038 keyboard section (cards in the tab order, Enter on a card opens history).

Amended by R-204 (2026-10-08): The tray is a focus-free miniature with provider icons and a five-hour ring.
Amended by R-205 (2026-10-08): Click a name to rename; drag a grip to order subscriptions.

### R-201 - Attention is a true orange
Owner direction, 2026-10-08, in conversation (variant A of three), after `cap close` read as red. The attention colours from the T-034 reference (#D97757 / #A35C44 / #E78E6E, hue about 15°) sat only about 7° from critical red (#E5604A) and read as red on the dark card. Attention becomes a true orange at hue about 28°: mark #E58A3C, hatch #A8652E, text #F0A060, and its pill the mark at 14 %. Critical red, the amber of a waiting sign-in (#E8B04B, hue about 39°) and every state-to-tone rule of R-192 are unchanged. Contrast on the card improves: attention text on its pill 5.67:1 (was 5.04:1), the mark on the card 5.80:1 (was 4.86:1). This amends the T-038 token table; see [T-038 section 7](../specs/T-038-ledger-presentation/spec.md#7-tokens-and-styles).

### R-202 - Percent caps and the cap as the full bar
Owner direction, 2026-10-08, in conversation (variant A of three). A weekly or monthly percent window, never a five-hour window, accepts an optional personal cap in whole percent of the provider's window, up to 100 %, stored as a `percent` count in the budget configuration; this replaces T-035 acceptance 3's rule that a percentage window never takes a cap. Below 100 % the cap is the effective limit for today's share, the states and the five-hour count left; "used up" stays the provider's 100 % and figures stay in provider percent ("63 % of 90 % cap"). For every limit with an applied cap, the period bar's full width is the cap, as if the provider's limit were the cap: the hatched part above a cap (R-186) is removed, and while use exceeds the cap the bar is scaled to the use with a tick at the cap. The cap row of the limit settings popover (R-199) now also appears on these percent windows. See [T-054](../specs/T-054-percent-cap-and-cap-bar/spec.md).

### R-203 - A cap is set only in the limit settings
Owner direction, 2026-10-08, in conversation (variant A of two, "without set/edit cap captions"). A card no longer has a cap editor of its own: the footer is plain text with its tooltip, without the dotted underline or a click, and the "Set cap" button on a card without a limit or cap is gone, as is the spoken "Press to edit the cap". The cap is set, changed and removed in the limit settings popover (R-199); the C key opens that popover with the cap field focused. Settings › Caps keeps its list of all caps with Edit and Remove. Pill tooltips that suggested setting a cap now name the limit settings. This replaces R-199's rule that the footer click opening the inline cap editor stays, and the inline cap editing of T-038.

### R-204 - The tray is a focus-free miniature with provider icons and a five-hour ring
Owner direction, 2026-10-08, in conversation (the thin ring arc of three variants, and the main limit only of two). The tray flyout has no tab stops or focus frames, and opening it focuses nothing; its arrow and Enter navigation is removed, and a click on a row still opens the window at that account. Each shown subscription is one row: a 16 px monochrome provider mark instead of its name, drawn red on the error states, with the display name in the tooltip. Next to the mark is one today bar for the subscription's primary limit, without a five-hour split, model, spending or period bars, 14 px tall in the window's today-strip style. A subscription with a five-hour window also shows a 16 px ring whose arc, from 12 o'clock clockwise, is the current window's used share (or left share in Left mode). Rows follow the Compact or Comfortable padding, and the flyout is 260 px wide. This amends R-187 (a name and one strip per limit) and the tray part of R-200. See [T-055](../specs/T-055-tray-miniature-order-refresh/spec.md).

### R-205 - Click a name to rename; drag a grip to order subscriptions
Owner direction, 2026-10-08, in conversation (the grip drag of three variants). A single click on a subscription's name starts the inline rename, which Enter, a click outside the box or focus loss saves and Esc cancels. A grip at the left of the card header, shown on hover, drags the card to a new place among the subscriptions, with an insertion line. Alt+↑/↓ on the header moves the subscription and, inside a section, still moves the section. The order is saved in the Ledger preferences (`AccountOrder`), applies to the window and the tray, and new subscriptions appear last. This amends R-200's Alt+↑/↓, which moved a limit within its account. See [T-055](../specs/T-055-tray-miniature-order-refresh/spec.md).

### R-206 - The refresh interval is a setting from 1 to 60 minutes
Owner direction, 2026-10-08, in conversation (a stepper with a 1–60 min range, chosen over 1–15 min). Settings › View has a Refresh stepper in whole minutes from 1 to 60, default 5, saved in the Ledger preferences; the footer's "every 5 min" caption is removed. An account is due once its reading is at least the interval minus 30 seconds old, and after a successful refresh the next is one interval later; the failure backoff is unchanged. Every fixed 15 minutes that judged readings current or continuous — staleness, a carried day start, incomplete tracking after a gap and extra-usage evidence — becomes max(15 min, 3 × the interval), so the default changes nothing. The budget store's 15-minute merge of identical readings is unchanged. This amends T-033's fixed five-minute refresh and 15-minute staleness. See [T-055](../specs/T-055-tray-miniature-order-refresh/spec.md).

### R-207 - Tooltips open only under the pointer
Owner direction, 2026-10-09, in conversation. A tooltip opens only when the mouse pointer rests on its element; keyboard focus, including Tab, opens none, and the tooltip WinUI itself opens on keyboard focus closes before it shows. Controls whose look already says what they do carry no tooltip: the close crosses of the settings sheet and the sign-in strip, the Settings and Add account buttons, Copy code, Check for updates and the Edit and Remove buttons of Settings › Caps; Used/Left already had none. Tooltips that add information stay, such as the bar and pill details, the refresh status, the card icons, Rename, the grip and the update modes. Accessible names are unchanged. This amends R-186's rule that tooltips are also reachable by keyboard focus.

## Workflow amendments

### R-208 - Task branches, save points on the branch, verified merges into main
Owner decisions OD-2 and OD-3 of the [workflow audit](../workflow/claude-workflow-audit/status.md), 2026-10-09. Each task runs on its own branch, normally the desktop app's worktree branch. Save-point commits are pushed to `origin/<task-branch>`, which runs no CI and publishes nothing, after each meaningful step and before going idle. Only verified work merges into `main`, and the remote task branch is deleted after the merge. A Preview still publishes automatically from `main` after a verified merge that changes product inputs (see the [merge gate](../workflow/verification.md#merge-gate)). When a required smoke is BLOCKED, a change that touches Windows UI, tray, launch or lifetime code stays on its branch until the smoke passes or the owner approves that change; other product changes covered by unit tests may merge with the smoke recorded BLOCKED. This replaces the 2026-09-20 direct-main instruction ("do not create new branches; push work in progress to `main`"), R-178 and R-179. See the [Git flow](../../CONTRIBUTING.md#git-flow).

### R-209 - When agents ask the owner
Owner decisions OD-4 and OD-5 of the workflow audit, 2026-10-09. AGENTS.md holds an always-ask list (new scope or product intent, significant architecture or complexity, security boundaries, destructive or external authority, dependencies, visible UI variants) and a never-ask list. Inside that authority the agent's recommendation is the default and the agent records the reason. A result the owner reports is recorded as owner-reported PASS without asking. Prompts written for primary sessions inherit standing policy unless the owner narrows it in the current request. Unattended runs skip always-ask work and list those questions at the checkpoint. Worker subagents may merge verified work into `main` and push it without a per-run grant. This also records the 2026-10-07 direction that one owner approval covers the path from design to a verified merge, without repeated approval gates.

### R-210 - Risk tiers and proportional review
Owner decisions OD-14, OD-15, OD-16 and OD-18 of the workflow audit, 2026-10-09. CONTRIBUTING defines tiers T0 to T3; the tier is the highest one that any touched area triggers, from a published T3 area list that the reviewer checks against the diff. T1 and T2 get one fresh-context review of the integrated diff and local `/code-review`; T3 adds a blocking focused independent review with `/security-review`. A fresh-context read-only subagent counts as independent. Bug-fix tests must fail at the base commit, and red-to-green testing is part of CONTRIBUTING. In parallel runs each code, test or harness task gets a per-task review, docs tasks a primary check (rule and agent-configuration edits are T3), and one whole-feature review always runs; one round, then a re-check of the fixed lines. The primary may start parallel worktree workers itself for at least three independent tasks with non-overlapping write sets, at most about four at once. Evidence is recorded once per wave or at feature end, without separate per-task record commits.

### R-211 - Plugin skills are optional tools
Owner decision OD-6 of the workflow audit, 2026-10-09. Repository rules win over plugin process. A plugin's spec-review, execution-mode and finish-branch prompts are answered in advance by the standing rules, and no `.superpowers` or `docs/superpowers` artifacts outlive the session.

### R-212 - Verification targets ordinary desktop use
Owner direction, 2026-10-03, recorded 2026-10-09. AI Usage is a personal app for the owner's ordinary desktop use. Agents do not run or expand scope for screen-reader, contrast-theme, extreme zoom/DPI or unusual-display checks unless the owner asks for that work again, do not change host display or accessibility settings for a test matrix, and keep the native window and tray chrome instead of adding custom chrome for those cases. Normal launch, core interactions and relevant regressions stay required. This supersedes older accessibility and display-matrix requirements in task references.

### R-213 - Minimum sufficient complexity
Owner direction, 2026-10-03, recorded 2026-10-09. Implement the requested functionality with the least code and complexity that remains correct, readable and easy to change; add layers, interfaces, dependencies or configuration only for a concrete current requirement; ask the owner before materially increasing complexity. See [CONTRIBUTING](../../CONTRIBUTING.md#simplicity-and-architecture).

### R-214 - UI design variants before implementation
Owner direction, 2026-10-07, recorded 2026-10-09. When a change visibly alters the UI (layout, card structure, controls, copy placement), the agent first proposes 2-3 short rendered design variants with a recommendation in one question and implements the owner's pick. Bug fixes that restore already approved UI need no variants. The pick is recorded as one line in the feature's spec.

### R-215 - Push status in every final reply
Owner direction, 2026-10-08, recorded 2026-10-09. Every final reply that follows commits states the push status, so the owner never has to ask: after `git fetch` confirms the commits are in `origin/main`, "Pushed to `main`" with the hash or hashes; otherwise what is pushed where, what is not, and why.

### R-216 - Previews publish only for product changes
Owner decision OD-17 of the workflow audit, 2026-10-09. A green push to `main` publishes a Preview unless every path changed since the Preview the feed serves is on the non-product allowlist: `docs/**`, `.claude/**`, `.agents/**` and Markdown files at the repository root. Any other path, including an unknown kind of file, is a product input and publishes. The same "product inputs" definition decides when the launch smoke is required before a merge. The workflow records each decision and its reason in the job summary; an owner dispatch with `PublishPreview=true` always publishes, and `AIU_PREVIEW_ENABLED` stays the kill switch. This amends R-157.

### R-217 - Repository skills replace the superpowers plugin
Owner decisions OD-7, OD-8, OD-11, OD-12 and OD-20 of the workflow audit, 2026-10-09. The project stays agent-neutral: AGENTS.md is the mandatory entry point, canonical skills live in `.agents/skills/` and agent contracts in `.agents/agents/`, and Claude Code files (`CLAUDE.md`, `.claude/skills/`, `.claude/agents/`) are thin pointers to them. One reviewer and one implementer contract are versioned; models are given as aliases. The repository skills planning-and-variants and plan-execution replace the superpowers planning and execution skills; systematic-debugging, verification-before-completion and receiving-code-review are vendored from superpowers v6.4.2 at a pinned commit; post-deploy-check runs only when the owner asks. Every superpowers skill is mapped to a replacement in the [mapping](../workflow/claude-workflow-audit/superpowers-mapping.md). The project settings disable superpowers, ux-superpowers, design-superpowers, desktop-commander and the design plugin for this repository only. This amends R-012 and R-034: discovery through thin native wrappers is now provided, while `.agents/skills` stays the only canonical copy.

### R-218 - Item, step and decision IDs
Owner decision OD-31 of the workflow audit, 2026-10-09. Work items (features and bugs) are `T-nnn` and keep their former `AIU-nnn` numbers; steps inside an item are `T-nnn.k`; decisions are `R-nnn` and keep their former `D-nnn` numbers. Branch placeholders are `T-NEW` and `R-NEW`, numbered at the merge by `tools/windows/Set-ItemNumbers.ps1`. Git history, the archive and the Stage 1 audit records keep the old prefixes with the same numbers, as do paths into the ignored `.ai-usage-local` root. Decision IDs have three digits, which tells them apart from the two-digit `R-xx` requirements inside specifications (OD-35). This amends R-196.

### R-219 - Lean work records
Owner decision OD-19 of the workflow audit, 2026-10-09. The backlog keeps live items in full, one "Done index" row per done or dropped item and one "Pending owner checks" table. An executed plan collapses into an execution ledger in its verification record (task, commits, review verdict, check IDs, grant, open next action). A decision record is written only for a rule that binds future work; an owner's pick among UI variants is one line in the spec. An amended decision carries an "Amended by" line. The validator checks the Done index, decision ID uniqueness, amendment pointers and "Acceptance results" coverage. See [document formats](../workflow/formats.md).

### R-220 - CI no longer builds the routing spike
Owner decision OD-26 of the workflow audit, 2026-10-09. CI no longer builds the T-002 standalone native routing reproduction; the `spikes/windows/AIU-002-routing` folder stays in the repository. The rest of the R-154 CI scope is unchanged. This amends R-154.

### R-NEW - Monthly peak-hours review
Owner direction, 2026-10-10, in conversation. The app shows one shared peak-hours hint from an announced schedule, not from account data, and names no provider. The schedule and its evidence live in [peak hours](../providers/peak-hours.md). When its `checked_at` date is more than a month old, the primary agent of a session already doing other work adds a short research pass over public provider pages and updates that file; reviewers and parallel workers do not. The pass uses no signed-in pages, provider APIs, inference requests or credentials. A change to the window in code is its own verified change under the usual tier and review rules; a change of shape (the window withdrawn, per-provider or per-plan windows, new wording) is asked first, and an unattended run only records what it found. The review is never started by a schedule or a scheduled job, and it does not select work on its own.
