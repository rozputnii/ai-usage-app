# Presentation contract v1

This is the proposed Windows presentation boundary to implement after design approval. It is not a provider transport schema or a claim that these production types already exist. Property names below are the shared vocabulary for Claude Design, Claude Code and Codex. Fixture JSON uses camelCase. All identities are opaque strings and scoped by their parent; user labels are never keys.

## Shapes

| Type | Required fields and semantics |
|---|---|
| UiSnapshot | revision: nonnegative integer; mode: Live or Demo; observedAt: UTC instant; accounts: AccountItem[]; capabilities: CapabilityItem[]; preferences: Preferences; system: SystemStatus |
| AccountItem | id; providerId; label; plan: nullable string; connection: NotConnected/Connecting/Connected/ReauthRequired/RecoveryRequired; operation: Idle/Loading/Refreshing/Disconnecting; freshness: Fresh/Cached/Stale/Unknown; fetchedAt: nullable UTC instant; failure: nullable FailureItem; contexts: ContextItem[]; extensions: ExtensionItem[] |
| ContextItem | id; label; kind: Account/Organization/Workspace/Project; hidden: boolean; groups: GroupItem[]; use a synthetic account-level context when current data has no context structure, never imply discovered workspaces |
| GroupItem | id; label; sharedPoolId: nullable opaque string; hidden: boolean; expansion: Auto/Expanded/Collapsed; windows: WindowItem[]; allowed/limitReached: nullable provider flags, independent of percentages |
| WindowItem | id; label; remainingPercent: nullable finite number 0..100; usedPercent: nullable finite number 0..100; valueState: Known/Unknown/Unlimited/Exhausted/Unavailable; absolute: nullable NativeAmount; durationSeconds: nullable positive number; resetsAt: nullable UTC instant; alertsMuted: boolean |
| NativeAmount | remaining/used/limit: nullable decimal strings; unit: provider-supplied nonempty string; no inferred currency, total or conversion; null limit is not unlimited |
| ExtensionItem | kind: Credits/ExtraUsage/Opaque; label; enabled: nullable boolean; amountMinor: nullable integer string; exponent: nullable integer; currency: nullable string; usedMinor/limitMinor: nullable integer strings; unlimited: nullable boolean; missing currency/exponent means no inferred money formatting |
| CapabilityItem | key; targetId: nullable string (null = global); availability: Available/Unavailable; reason: nullable resource key; origin: Existing/Planned; demo can enable Planned capabilities, Live cannot enable them until a real adapter exists |
| FailureItem | kind: stable resource key matching a safe failure category; messageKey; retryAt: nullable UTC instant; recoverable: boolean; never raw exception text or provider payload |
| Preferences | theme: System/Light/Dark; accountOrder: id[]; hiddenTargets: id[]; mutedTargets: id[]; showDisconnected: boolean; alwaysOnTop: boolean; historyEnabled: boolean; notificationRules: NotificationRule[]; mutations in Demo are memory-only |
| NotificationRule | scope: Global/Provider/Account/Window; targetId: nullable string; inherit: boolean; remainingThresholds: ordered unique number[] in 0..100; resetNotice: boolean; default thresholds [25,10,0]; inheritance is resolved without mutating parent values |
| HistoryQuery | accountId/contextId/groupId/windowId; from/to: UTC instants with from < to; resolution: Auto/Hour/Day; preset: 24h/7d/30d/90d/1y/Custom |
| HistoryPoint | at: UTC instant; remainingPercent: nullable finite 0..100; coverage: Observed/Gap; segmentId: opaque string; segment changes at resets; never interpolate across gaps or reset segments |
| SystemStatus | buildLabel; schemaLabel; health: Idle/Checking/Healthy/Warning/Failed; update: Unsupported/Checking/Current/Available/Ready/Failed/WaitingForStable; recovery: None/Interrupted/NewerSchema/RestoreFailed; compatibility: Normal/CompatibilityBlocked/SecurityBlocked |

The final C# types may split these records by feature but must preserve semantics. This is a screen-driven boundary, not a new universal Core framework. Extend the contract through a reviewed diff with fixtures when a design needs another field.

Capability keys are command kind names from the command table, plus ViewHistory, ViewContexts, ViewNativeAmounts and ViewSystemStatus. Resolve an exact target entry first, then global; absent means Unavailable. Origin is provenance, not permission. A live adapter advertises only implemented operations. Account identity is an app-owned stable reference supplied by that adapter; the current provider-slot reference must not masquerade as multi-account support. Snapshot publication occurs on the UI dispatcher; subscriptions are disposed when their view owner ends.

Optional account fields for current Codex metadata: availableResetCredits (nullable nonnegative integer), spendControlReached (nullable boolean) and limitReachedType (nullable opaque string). They are informational only; no credit redemption or purchasing command is introduced. ExtensionItem also retains hasExplicitNullLimit (boolean, default false) for Claude extra usage, independently of unlimited.

## Current source mapping

| Source | UI mapping / limit |
|---|---|
| `src/windows/AiUsage.Core/Usage/QuotaSnapshot.cs` | PlanType -> plan; FetchedAt -> fetchedAt; Groups/Windows -> nested quota items; optional credits/reset-credit/spend-control fields require explicit typed presentation, never imply a redeem action |
| `src/windows/AiUsage.Core/Usage/ProviderSessionState.cs` | Status + Windows operation -> connection/operation; FromCache and RetrievedAt -> freshness; Failure -> localized FailureItem; QuotaUnavailable does not alone prove disconnection |
| `src/windows/AiUsage.Core/Providers/Claude/ClaudeQuotaReading.cs` | ExtraUsage -> extension with original currency/exponent/null-limit semantics; keep separate from subscription percentage |
| `src/windows/AiUsage.Core/Dashboard/DashboardWorkflow.cs` | Existing load/connect/refresh/disconnect/cancel workflow; live adapter must preserve concurrency and shutdown ownership |
| `src/windows/AiUsage.Windows/Features/Dashboard/DashboardViewModel.cs` | Existing commands, busy and failure state, manual-code visibility; extend/map typed values rather than parse formatted strings |
| `src/windows/AiUsage.Windows/Features/Dashboard/QuotaWindowItem.cs` | Currently exposes formatted text only; numeric indicator data must come from quota records, not extraction from text |
| `src/windows/AiUsage.Windows/Features/Dashboard/DashboardShellViewModel.cs` | Currently one Codex and one Claude view model; multi-account identity/context enumeration is a future capability |

Provider availability is separate from value availability. Missing remaining percentage must not become zero. Clamp only a visual meter rendering after retaining a semantic invalid/unavailable state; never silently accept malformed data as valid quota. Unlimited requires explicit provider evidence. A fixture may exercise an unlimited state without claiming that a named provider supports it.

## Commands and outcomes

Presentation source exposes Current, Subscribe(callback) returning IDisposable, and ExecuteAsync(UiCommand, cancellationToken) returning UiCommandResult. History is queried separately with QueryHistoryAsync(HistoryQuery, cancellationToken); do not put all history into Current. These are proposed signatures to type in Windows during T-04.

UiCommand = { kind, targetId?, payload?, expectedRevision }. Result = { status: Succeeded/Cancelled/Failed/Unsupported/Conflict, failure?: FailureItem }. Revision prevents stale edit commands overwriting a newer state; an adapter may reload and return Conflict. Cancellation restores the prior stable view and does not create a warning. Account operations remain isolated. Repeated refresh must follow the existing live workflow policy until the owning future refresh task changes it; demo must not imply a new live scheduling guarantee.

| Command kinds | Payload and observable outcome |
|---|---|
| Connect, SubmitCode, CancelOperation, RefreshAccount, RefreshAll, Disconnect | providerId/method for Connect; transient manually entered code only for SubmitCode, never in snapshots/fixtures/logs; progress -> result. RefreshAll updates eligible accounts independently. Live consent remains explicit. |
| Rename, Reorder, SetVisibility, SetExpansion | label (trimmed, nonempty); ordered unique ids; hidden + mute choice; Auto/Expanded/Collapsed. Invalid inputs keep the prior state and show inline errors. |
| SetPreference, SetNotificationRule | typed preference key/value or NotificationRule; thresholds validated; inherited values remain distinguishable from overrides |
| DiscoverCli, ImportCli, ReimportCli | built-in candidate ids in demo; progressive results and per-item outcomes; future live implementation requires its own credential authorization |
| RunHealthCheck, PreviewDiagnostics, PreviewLogs | progress/result or a sanitized on-screen sample; diagnosis does not mutate unrelated state |
| PreviewExport, PreviewReplaceImport, ResetSettings, FactoryReset, DeleteAccountData, RetryRecovery, RestoreCheckpoint, PreviewDataFolder | synthetic bundle/checkpoint id and explicit confirmation for destructive actions; separate validation, confirmation, progress and outcome; demo remains in memory |
| CheckUpdates, SetChannel, RestartAndUpdate, PreviewNotification | channel Stable/Preview or synthetic notification target; demo state changes only, no actual restart/OS dispatch |

Read-only history filter changes use the query API. Theme/navigation can update presentation immediately; final persistence goes through an explicit preference adapter. Every button maps to a command or navigation destination. Unsupported live commands return Unsupported before any side effect, including when invoked without the UI button.

## Fixture consumption

[fixtures.json](fixtures.json) is a deterministic scenario seed catalog, not a serialized UiSnapshot and not real provider evidence. Claude Code builds complete snapshots from the documented defaults and overlays. Fixed time is 2026-09-15T12:00:00Z; advance a fake clock for countdown/reset tests. IDs prefixed `demo-` never join live state. Sample provider values are illustrative.

Default seed: revision 1, Demo mode; System theme; always-on-top false; history enabled; default notification thresholds; no error/recovery/update action; empty arrays for absent collections. Each account seed has one account-level context and one group unless groups are supplied. Missing measurements stay null/Unknown, not zero. Scenario transition arrays specify event -> expected state, not elapsed real network work. All screens additionally support deliberate failure and cancellation where listed in screens.md.

## T-10 live adapter mapping

Product startup selects live services; `--demo` selects only deterministic demo services. Existing Codex and Claude integrations each expose one app-owned connection slot. Multi-account acquisition, CLI import, history, notifications, automatic monitoring, portable data, recovery actions and updates remain unavailable. Appearance, used/remaining display, labels, manual order, visibility and group expansion are persisted separately from provider data.

Live adapters run existing `DashboardWorkflow` instances and map `IProviderSession` states. Cached timestamps remain unchanged; errors and reauthentication retain readings with stale freshness. Cancel publishes the session's authoritative post-cancellation state, because token rotation may already have committed; it does not roll a grant back. Refresh success requires an actual fresh quota result. Length-prefixed provider/group/window IDs preserve opaque identifiers without cross-provider collisions. Codex allowed/limit-reached and limit-reason metadata are independent detail information, never synthesized percentages. Credits and extra usage stay separate from subscription windows; no currency/exponent is inferred.

Claude manual code continues the same active browser authorization through `TrySubmitCode`; the UI clears the submitted code and does not cancel the authorization when opening the code field. Product shutdown drains both startup and active provider work before session disposal. Presentation metadata files never contain grants or provider payloads; invalid/newer files are retained and not overwritten.