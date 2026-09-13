# Windows architecture baseline

Status: accepted architecture; AIU-002 implements the minimal three-project native baseline. Build/package evidence exists; installed guest/UI proof remains blocked. The later runtime and persistence sections remain intended requirements, not implemented services.

## Stack
Windows 11 24H2+, x64; .NET 10; WinUI 3 / Windows App SDK / MSIX; Generic Host; CommunityToolkit.Mvvm; MVVM-first routing (Uno.Extensions.Navigation candidate, first build gate); EF Core 10 SQLite; HttpClientFactory/typed clients/System.Text.Json/Http.Resilience; Serilog; LiveCharts2; H.NotifyIcon.WinUI; native AppNotificationManager; xUnit v3/FlaUI UIA3.

Do not invent exact current package versions. AIU-002 resolves stable versions and proves restore, build, packaged launch and compatibility. Selecting a library does not require implementing its entire feature in the first build.

## Physical boundaries
```
src/windows/
  AiUsage.Windows/          # WinUI pages/VMs/navigation/dispatcher/composition
  AiUsage.Core/             # neutral usage/account/result/service contracts
  AiUsage.Infrastructure/   # providers, EF, secrets, lifecycle, local I/O
  AiUsage.slnx
tests/windows/
  AiUsage.Core.Tests/
  AiUsage.Infrastructure.Tests/
  AiUsage.Windows.Tests/    # Windows-specific tests as needed
```
The three production paths and Windows smoke project now exist. Core/Infrastructure test projects remain future paths until behavior needs tests. See the AIU-002 specification and actual verification; do not infer that later runtime services are implemented.

Core references neither WinUI, EF nor Windows APIs. Provider transport DTOs and authentication endpoints belong in Infrastructure provider slices. Core may expose a typed normalized provider extension required by consumers, not arbitrary wire payloads. Windows composition may reference Core and Infrastructure. Do not introduce an Application project or use-case layer merely to satisfy an architecture label.

## Runtime
```
manual/timer/resume/network triggers
       → RefreshCoordinator (keyed dedup/generation)
       → per-provider auth owner + quota clients
       → validate/normalize
       → Channel<PersistenceWork> → single DB writer
       → publish committed account snapshot
       → immutable AppState
       → main/tray ViewModels / notification evaluator
```
AppState holds only live/current snapshots. History query service returns range/resolution-aware read models outside UI thread. Store subscription gets current snapshot without subscribe/read gap; version ordering avoids lost concurrent updates and stale DispatcherQueue callbacks. Explicit IDisposable unsubscribe; catch/report subscriber faults without crashing unrelated consumers. Notification evaluator processes required transitions, not a dropping UI-only stream.

## Refresh safety
Independent accounts fetch concurrently, no arbitrary global cap. Dedup background triggers. Manual latest-wins for same-account quota fetch. Each operation carries logical account/credential generation and request generation; writer checks current generation before commit, store rejects old publication. Disconnect/reimport/reset invalidates generation before canceling producers. Newest refresh cannot restore a disconnected account.

Token renewal is NOT an ordinary cancelable quota fetch: one refresh authority per credential family, persists received rotated access/refresh pair safely. User cancellation may stop waiting, not abandon token persistence. Never blindly retry an ambiguous token exchange. After unrecoverable token loss/revocation use Re-auth required.

DB transaction and in-memory publication are ordered, not globally transactional: if crash after commit before publish, reload cache next startup. Per-account immutable update must merge with latest store state, not replace app snapshot built before another account finished.

## Persistence
Single writer includes settings/accounts/history/pruning mutations; dedicated maintenance ownership for migrations/import/reset. Short-lived contexts via IDbContextFactory; read-only projections AsNoTracking. SQL indices for identity/group/window/observed time; UTC instants mapped to SQLite-friendly ordered storage. WAL checkpoint policy native/controlled, never manual deletion of active -wal/-shm.

Queue capacity/backpressure is bounded independently of network concurrency; never silently drop critical data or create unbounded queued duplicates. Persist latest observation metadata even if historical value sample deduplicated. Startup must still know last successful fetch time.

## Quota semantics
Account identity, grant identity, context identity and shared pool identity are related but not interchangeable. Do not duplicate one shared entitlement into multiple independent totals. Common fields nullable and dimensioned: kind/unit/used/remaining/limit/reset/window/group/source/fetchedAt/freshness. Percentage available only when provider semantics justify it. `unlimited` explicit. Freshness/outcome independent from last good data.

Rolling-window replenishment can increase remaining without a calendar reset; group/model names discovered from response. History rollups preserve window/reset/entitlement change boundaries and gaps; no invented consumption sums or interpolation over offline spans. Display unknown/reset elapsed without pretending fresh quota. Server timestamp basis preserved; TimeProvider for scheduling/test clock.

## Startup and lifetime
Minimal package activation/single-instance handling, compatibility marker/journal check, off-thread cache read, show cached UI, then start permitted producers. Build Host without console blocking main STA/dispatcher; hosted-service startup not allowed to do full scans before first window. Autostart tray-only; normal launch main window; close hide; Exit truly stops workers+Host.

No splash, no network/whole filesystem integrity/history scan on normal launch. Pending migration has explicit progress/recovery screen, no normal services on incompatible schema. Reference release startup target ≤500ms is measured, not promised across all hardware.

## UI scope
Single main window Dashboard/Provider details/Accounts/History/Settings/Diagnostics, responsive grid, primary remaining, groups, user order/labels, theme/a11y/resources. Tray mini-dashboard shares state, no sparklines. First executable milestone needs only honest empty shell + activation/exit; full UI follows working Codex data.
