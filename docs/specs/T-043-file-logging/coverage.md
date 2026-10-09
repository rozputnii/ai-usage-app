# T-043 coverage inventory

Source inventory: 2026-10-03, original product/demo plus isolated Ledger demo;
T-039/040 have not replaced the current presentation/history clients.
All transport rows use ProviderHttp → ProviderCapture → EndpointPolicy/
ResponseSanitizer → FileDiagnostics. Policies are independent of DTO projection.
The route table is exercised by ProviderCaptureTests.ExistingRoutesHaveExplicitPoliciesWithoutPrivateSegments.

| Provider/client | Executed route(s) | Policy |
| --- | --- | --- |
| CodexAuthClient | auth.openai.com `/oauth/token`, `/api/accounts/deviceauth/usercode`, `/api/accounts/deviceauth/token` | codex/auth/v1 |
| CodexQuotaClient | chatgpt.com `/backend-api/wham/usage` | codex/quota/v1 |
| CodexHistoryClient | WHAM usage `/daily-token-usage-breakdown`, `/credit-usage-events`, `/daily-workspace-user-token-usage-breakdown`, `/daily-workspace-user-credit-usage`; analytics `/daily-workspace-usage-counts`, `/daily-plugin-usage-metrics`, `/daily-skill-usage-metrics` | codex/history/v1; query variants never logged |
| ClaudeAuthClient | api.anthropic.com `/v1/oauth/token`; `/api/claude_cli/bootstrap` | claude/auth/v1, claude/identity/v1 |
| ClaudeQuotaClient | api.anthropic.com `/api/oauth/usage` | claude/quota/v1 |
| CopilotAuthClient | github.com `/login/device/code`, `/login/oauth/access_token`; api.github.com `/user` | copilot/auth/v1, copilot/identity/v1 |
| CopilotQuotaClient | api.github.com `/copilot_internal/user` | copilot/quota/v1 |
| CopilotHistoryClient | `/user`; `/users/{user}/settings/billing/{ai_credit,premium_request}/usage` | copilot/identity/v1, copilot/history/v1 |
| AntigravityAuthClient | oauth2.googleapis.com `/token`; www.googleapis.com `/oauth2/v1/userinfo` | antigravity/auth/v1, antigravity/identity/v1 |
| AntigravityQuotaClient | daily-cloudcode-pa.googleapis.com `/v1internal:loadCodeAssist`, `/v1internal:onboardUser`, `/v1internal/operations/{operation}`, `/v1internal:retrieveUserQuotaSummary` | antigravity/provisioning/v1, antigravity/quota/v1 |
| Unclassified/future endpoints | Fixed `unclassified` route; no URL interpolation | unknown/unknown/v1; scalar values withheld |

Browser authorization URLs and the local callback listener are not app-issued provider
HTTP responses. They are deliberately not captured. No requests, redirects or retries
were added to collect evidence. Identity policies preserve structure and withhold identity
values. Auth policy retains approved expiry/interval/token-type/error values only.
Quota policies retain approved numeric/boolean fields, supported native units/currencies
and reset timestamps; unclassified labels/models/plan values remain explicitly withheld.
Unknown keys use placeholders because arbitrary names can themselves contain identity
or credentials. This is a limitation for schema discovery, not a claim of raw completeness.

| Area | Implemented boundary | Evidence/limit |
| --- | --- | --- |
| Core operations | DashboardWorkflow scopes around connect/resume/refresh/disconnect; terminal outcome, duration, parent ID, session-local account reference | Core stays free of transport/files/Serilog; presentation and correlation tests |
| Provider transport | One terminal HttpCompleted plus separately committed capture per actual SendAsync | Success/non-2xx, malformed, disabled-body, precision and secret-canary tests; existing transport regressions |
| Provider persistence/failure conversion | ProviderStatePolicy/Lease capture original lease/read/write/decode/cleanup failures; bounded migration/checkpoint/stage/recovery events; Codex cache loss and migration events; provider session conversion boundaries | DPAPI/state regressions and all three protected migration fixtures with logging enabled; no source credential reads |
| General state maintenance | StateMaintenance migration/checkpoint/recovery transitions and original failure before MaintenanceReport conversion; existing budget recovery event; T-047 HistoryRelocated, HistoryLeftInPlace (Warning) and HistoryReattached, identity-map failures as PersistenceFailure or BudgetStoreRecovered | Existing maintenance/budget regressions and HistoryPersistenceTests; no preferences, checkpoint contents, identities, account IDs or paths logged |
| Presentation work | LiveUsageSource observation failures, History scope/failure; LiveAutoRefresh observed task failures | Presentation regressions; no timer-success spam |
| Early startup/lifetime | ApplicationDiagnostics before InitializeComponent, global hooks before Host; startup/shutdown/disposal critical path, session start/exit/abnormal marker | Debug and Release builds; ordinary smoke; dedicated crash child; actual invalid-XAML, hosted-service startup and hosted-service disposal failures |
| UI dispatcher/commands | UiDispatcher callback failure captures synchronously before rethrow; rejected enqueue warning | Isolated Release dispatcher probe |
| Navigation/tray | NavigationCompleted, WindowShown/Hidden, existing TrayFailure owner | Ordinary navigation/tray/exit smoke |
| Resource/converter | Bind.Token catches before rethrow; stable code only, no resource key/value | Isolated Release converter probe |
| Animation | Ordinary Storyboard starts and Ledger animation start use one adapter boundary | Isolated failure of that owner boundary; no per-frame logging; framework-internal visibility is not promised |
| XAML binding | DebugSettings.BindingFailed with tracing enabled | Ordinary Release missing-binding probe produces no event; the same Release probe under a native debugger produced one safe event. No replacement binding engine |
| Background fallback | AppDomain.UnhandledException and TaskScheduler.UnobservedTaskException; owned background work catches promptly | Real managed child fatal and observed Release background fault |
| Responsiveness | One outstanding dispatcher ping, ten-second stall warning/recovery; debugger and long timer-gap suppression | Real isolated UI stall emitted one warning and one recovery with duration; suspend/debugger suppression timing NOT_RUN |
| Logger health | Bounded byte/count queue; trace-first eviction; fixed cumulative loss; coalesced binding/dispatch warnings | Queue-pressure, unavailable/locked storage, critical independence tests |
| Cleanup/preview | Exact owned namespace/names, no recursive cleanup, reparse rejection; expired/partial-line filtering | Retention, month-end, invalid/future names, size eviction, junction, partial preview tests |
| Console | ConsoleDiagnostics uses same registration/transport and a separate Console root | Release console build and offline synthetic measurement |

Framework-internal faults that bypass app-owned boundaries and global hooks remain a
platform blind spot. Power loss, native corruption, forced kill and stalled disks remain
outside forced-capture guarantees. Final verification records injected storage errors,
completed independent review and available live-provider captures. Real host disk
exhaustion, host ACL mutation, package installation and sleep/debugger watchdog timing
are not claimed; this inventory does not turn source inspection into a PASS.
