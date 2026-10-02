# AIU-042 verification

Execution selected by the owner's 2026-10-02 Goal instruction. Baseline code: 66e3eb8.
Initial Git state: clean main tracking origin/main; no unrelated changes found.
Final primary integrated review: 2026-10-02, implementation commits 019836a and a0e4b75.
All final checks below use their combined code. Later closure changes are documentation only.

| Criteria | Result | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Complete subsystem inventory below; baseline 416 Infrastructure/179 Presentation tests pass. No unrelated initial work and no baseline failure. |
| AC-02 | PASS | F-01 through F-08 fixed; failing probes, corrected reviewer findings and final regressions recorded. Only minor D-01 through D-03 are deferred with explicit rationale. No unresolved material finding/owner decision. |
| AC-03 | PASS | Primary integrated diff review: credential-free Core retained; one renewal path owns Codex intent/exchange/save; shared retry-date helper removes duplicated arithmetic. No new layer, dependency or Windows behavior implementation. |
| AC-04 | PASS | Stable SDK 10.0.401/runtime 10.0.12, .NET 10/C# 14 assessment below; span lookup has measured benefit. Builds have zero warnings/errors; no SDK/package/analyzer relaxation. |
| AC-05 | PASS | Reproducible Release harness and comparable 35-day store, budget/history, session and parser measurements below. 61.4% session median improvement; other warm ranges overlap. |
| AC-06 | PASS | Final 448 Infrastructure/179 Presentation tests include provider fixtures, opaque values, budget/session semantics, persisted schemas, recovery and cancellation. Existing journal envelope and entropy retained; no AIU-037 through AIU-040 implementation. |
| AC-07 | PASS | Both suites, consumer Release builds, docs/diff checks pass. Required focused independent credential/cache review satisfied after targeted corrections; no missing required gate. Live/UI/package scenarios are honestly NOT_RUN and not triggered by this diff. |
| AC-08 | PASS | Primary reviewed the integrated diff against AC-01 through AC-07 and reconciled independent findings/limitations below. Bounded audit complete; this does not certify defect-free software or start AIU-037. |

## Baseline checks

2026-10-02, local Windows, pinned stable SDK 10.0.401, Release, warnings as errors.
`dotnet` below is the existing user-local `~/.dotnet/ai-usage-sdk/dotnet.exe`.

| Check | Result | Observation |
| --- | --- | --- |
| `dotnet --version` | PASS | 10.0.401 |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 416 tests, 0 failed/skipped/not run; 14.947 s test execution. |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 179 tests, 0 failed/skipped/not run; 0.701 s test execution. |
| Live provider / interactive UI | NOT_RUN | Baseline uses synthetic fixtures and isolated stores; no live or UI claim. |

## Review coverage

Inventory covers production Core and Infrastructure, Windows backend adapters/composition
and their orchestration callers, and ProviderConsole. Visual layout, XAML rendering and
design assets are outside this backend audit. Rows identify source inspection as well as
regression evidence; a passing suite alone is not claimed as source review.

| Subsystem | Reviewed code and boundaries | Disposition |
| --- | --- | --- |
| Core contracts/calculations | All Core production files: Budget, Usage, History, Dashboard, Diagnostics, Persistence and Claude contracts. Unit/exponent arithmetic, DST, reset/day-start provenance, replay, session readiness, workflow cancellation and credential-free dependency direction. BudgetEngine/Scenario, ReadingBudget, SessionEstimate, LimitModel and DashboardWorkflow tests. | F-01/F-02/F-03 fixed; extreme calendar-domain limitation D-02 below. No generic calculation framework warranted. |
| Budget persistence | LocalBudgetStore, BudgetJsonFile, BudgetStoreRecords, QuotaObservationRecorder; leases, coalescing, retention, recovery capacity, staged replacement, exact-path cleanup and frozen DTO schema. | No storage modification yet; measurements quantify full-file cost. F-01 affects the pure transition called by append. |
| Shared state/recovery/diagnostics | ProviderStatePaths/Lease/Policy, all provider StateStore/StoredState types, CodexGrantStore/QuotaCache, StateMaintenance/MaintenanceRecords, PresentationPreferenceFile, LocalDiagnosticSink. ProviderStateCompatibility/Safety, StateMaintenance, PreferenceFile and diagnostic tests. | F-04/F-08 fixed. Existing frozen record shapes, entropy, ownership and cleanup retained. Preferences use unique CreateNew staging and replacement; no new reparse or cleanup defect demonstrated. Same-user filesystem mutation remains the existing trust limitation. |
| Authentication and session orchestration | All four providers' AuthClient, Session, Credentials, Http, authorization holders, AuthJson DTOs; Codex/Copilot history partials. Browser/device polling, terminal versus ambiguous renewal, lease scope, cancellation, disposal after drain and persisted identity. Auth/browser/protocol/session suites and new journal tests. | F-04 fixed with focused independent review. Distinct Claude/Copilot/Antigravity protocols, identity and renewal policies retained; no endpoint, scope, token-format or registration change. |
| Shared transport and quota | ProviderHttp/Transport/Options, LoopbackCallback, HistoryJson; all four quota parsers/clients. Bounded body/depth/deadline, redirects/cookies, backoff, opaque fields, unknown/null/unlimited and allowlisted failures. ProviderBoundary/TransportOptions and all parser/client suites. | F-07 shares overflow-safe retry-date calculation; existing parser semantics retained. No middleware/retry dependency needed. |
| Remote history | CodexHistoryClient/Parser, CopilotHistoryClient/Parser and both session partials. Range bounds, request culture, account attribution, cancellation, partial reports, typed numeric and report-shape failures; ProviderHistory and HistoryBoundary tests. | F-05/F-06/F-07 fixed. No AIU-037 parser coverage or history feature expansion. |
| Windows live adapters | Every Adapters/Live source: LiveUsageSource and History, LiveMapping, LiveConnectionFlow, LiveProviderHistorySource, LivePreferenceStore, LiveRecoveryService, ProductLifecycle, LiveAutoRefresh, LiveClock, DiagnosticProjection, UnavailableServices, Windows registration/diagnostics. LiveAdapter/History/Recovery/Clock/AutoRefresh/ReadingRecorder suites. | Reviewed task ownership, serialized per-provider work, cancellation versus rotation, shutdown drain, stale mapping, immutable snapshots, capabilities and secret-free diagnostics. No material finding requiring Windows adapter changes. LoadAsync preference validation runs once on guarded startup; no supported reload path demonstrated. |
| Windows composition/lifetime/callers | App, MainWindow lifetime/command hooks, Composition/ServiceRegistration, PlatformBasics, ShellServices, DialogService, MotionSettings and native theme/tray helpers. HostAbstractions and account/overview/connection/history/settings/recovery/shell orchestration callers. DependencyBoundary, ShellAndTray, Operations, Connection and ProviderHistoryPresentation tests. | Core remains credential-free; Infrastructure has no presentation dependency. One session disposal owner and stop-before-dispose retained. No activation/lifetime/UI change; no interactive success claim. Native icon ownership concern is D-03 below. |
| Isolated demo adapters | All Features/Demo C# sources: model/state/catalog/controller, usage/connection/history sources, settings/operations services and control orchestration. ScenarioCatalog and presentation suites. | Memory-only seeds and generation cancellation inspected; no production filesystem/credential/network calls. Existing simulation behavior retained. |
| ProviderConsole and build | Program, ClaudeConsole, CopilotConsole, AntigravityConsole, HistoryConsole, new BackendMeasurements; all Core/Infrastructure/Windows/console project files, global.json, Directory.Build.props, Directory.Packages.props and friend assemblies. | Credential actions remain interactive commands. New measurement dispatch precedes session resolution and uses only synthetic temporary data. Stable pinned SDK, warning-as-error policy, source-generated transport/storage DTOs and dependency direction retained. |

## Findings and remediation

| ID | Severity | Evidence and disposition |
| --- | --- | --- |
| F-01 | Moderate correctness | ReadingCalculations.Transition adds 60 seconds to a valid near-maximum reset and subtracts signed decimals without guarding overflow. Two new ReadingBoundaryTests failed with ArgumentOutOfRangeException/OverflowException before the fix. Fix compares reset differences and handles a positive difference beyond decimal range. Targeted and integrated regressions PASS; integrated in 019836a. |
| F-02 | Moderate correctness | Track exposes LastConfirmed beyond a closed period's end. The correction clamps output confirmation to period.End while retaining evidence known at the actual replay time. Initial horizon-clamping implementation was rejected by independent review because it lost valid unchanged-run coverage; the corrected closed-period/zero-consumption regressions PASS. |
| F-03 | Measured performance | SessionEstimator.Cover scans every weekly run for each session endpoint. Replacement uses binary search over Ordered's chronological nonoverlapping runs via ReadOnlySpan; gap and inclusive-endpoint regression passes before/after. Four-pair median fell from 39.180 to 15.137 ms; allocated bytes 6,525,416 to 6,421,736. No persistence or provider semantics change. Integrated in 019836a. |
| F-04 | Material authentication correctness | LostRenewalResponseCannotReplayTheGrantAfterRelaunch sent the same grant twice before correction. The owner authorized an existing protected-journal intent before renewal; resume, expired-access refresh and history now share that path. Interrupted/cancelled exchange blocks replay, returned successor recovers, known rate limiting preserves retry. Independent review found that fresh sign-in was blocked by the first prototype; successful/refused fresh-sign-in regressions failed then passed after a revision-bound replacement path. Focused follow-up source review PASS; final 448-test suite PASS includes cancelled fresh authorization and unbound/damaged intent rejection. |
| F-05 | Moderate correctness | Under ar-SA, Codex history requested year 1451 instead of Gregorian 2029. The regression failed before invariant request-date formatting. Existing endpoints and report fields retained. |
| F-06 | Moderate correctness | Codex activity totals with scalar/array shape were returned as empty usage. Two regressions failed; reject the malformed shape using the existing InvalidResponse contract. No parser expansion. |
| F-07 | Minor robustness, fixed | Both history clients overflowed on valid Retry-After delta with a clock near DateTimeOffset.MaxValue. Two corrected probes failed before a saturating shared retry-date helper; the same helper removes duplicated quota-client logic and reads the clock once. Initial probe used an invalid header construction and was corrected before drawing this conclusion. |
| F-08 | Moderate correctness | CodexQuotaCache accepted null group/window structures and invalid percentages, violating its unusable-cache contract and allowing nulls into LiveMapping. Five synthetic corrupt-shape regressions failed before non-destructive validation; all six cache tests now PASS, including compatible unknown fields and opaque values. |

## Remediation checks and independent review

- Infrastructure Release after core/history/journal prototype: PASS 436 tests, no failures/skips, 28.854 s.
- After fresh-sign-in correction: PASS 32 Codex session/journal tests, 4.141 s.
- Cache correction: PASS 6 tests, 1.519 s; included in the final integrated suite.
- Final integrated Infrastructure Release: PASS 448 tests, 0 failed/skipped/not run, 45.113 s.
- Final Presentation Release: PASS 179 tests, 0 failed/skipped/not run, 1.740 s.
- ProviderConsole Release build: PASS, 0 warnings/errors. Native Windows Release x64 unpackaged
  build: PASS, 0 warnings/errors, 66.09 s; command:
  `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore`.
  Infrastructure/Core also compile in the suites and both consumer builds with warnings as errors.
- Document validator: initial FAIL for missing design frontmatter and closure FAIL for task-local
  evidence paths (validator requires repository-relative paths), both corrected; final PASS
  `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`,
  `valid:true`, no diagnostics. `git diff --check`: PASS.
- An intermediate build correctly failed CA1068 for cancellation-token parameter order;
  reordered the private helper and reran without suppressing the analyzer. Final builds PASS.
- Live-provider, actual interactive Windows and packaged install/update checks: NOT_RUN.
  No live access was authorized or needed; no Windows UI/activation/tray/lifetime or package
  change triggers those checks. Synthetic loopback callbacks are not live authentication.
- External read-only Claude reviewer, model claude-opus-5-5, no tools, no session persistence,
  fresh frozen input; no Codex subagents. Primary review is separate from this review.
- Core review input SHA256 `9F0F473BFBAB276F3AAB3F055DE4BE6E49F2BC967F683DF06675DD545ABBC849`:
  initial FAIL for F-02 overcorrection; Transition and binary coverage lookup PASS by inspection.
  Corrected F-02 regressions PASS. Reviewer did not execute tests or inspect the harness.
- Journal review input SHA256 `4A784169733DB3F68EB18072F2904DB67A79E1263531689F8FD4EAF4B11FB4BF`:
  initial FAIL for blocked fresh authorization. Non-replay, successor recovery, downgrade,
  lease and rate-limit restoration passed source review. Targeted follow-up input SHA256
  `7F63D5C201A8CEBE145F3C66DDCECADF994BBAF1D8A63E20C531EA120BBCD31D` includes
  the correction, cache validation and transport omitted from the first bundle: PASS for the
  focused code gate, no material code defect. Documentation mismatch corrected; the design
  had already been updated while the frozen input was being reviewed. Additional cancellation,
  parent mismatch, missing predecessor and torn-intent regressions now pass. This is targeted
  verification after the reported finding, not a repeated full-review loop.
- Reviewer could not establish cache producer invariants from its bundle. Primary inspected
  CodexQuotaParser.ParseGroup: non-finite/out-of-range percentages become unknown; group/window
  collections and IDs are always present. New read validation accepts those outputs and
  existing persisted-schema fixtures; no blanket write restriction or payload normalization added.
- Reviewer's speculative pooled-POST replay concern was checked against the pinned runtime's
  [HttpConnection.SendAsync source](https://github.com/dotnet/runtime/blob/v10.0.12/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/HttpConnection.cs#L581-L590).
  HTTP/1.1 EOF enables retry only with no content or an unsent Expect-continue body; these
  token requests have content, no Expect-continue and the default HTTP/1.1 version policy.
  Application transport has one send and no retry middleware. No speculative pooling change made.

## Deferred minor findings and limits

| ID | Severity | Disposition and trigger |
| --- | --- | --- |
| D-01 | Minor performance cost | LocalBudgetStore rewrites bounded per-series files; eight full-density 35-day appends allocate about 165 MB and take about 660 ms on this machine. Background five-minute collection has ample measured headroom, with no demonstrated contention/UI stall. Changing storage partitioning/indexes would need separate compatibility evidence; defer until sustained larger-account or faster-sampling evidence warrants it. |
| D-02 | Minor input-domain robustness | WorkCalendar.Weights/PeriodResolver assume representable adjacent calendar days/months and plausible replenishment durations. Artificial periods at years 0001/9999 can overflow or cause very long day iteration. Current budget integration/35-day workloads do not approach those bounds. Defer a general supported calendar-domain policy until a caller needs it; do not invent a product maximum period in this task. |
| D-03 | Minor native resource ownership, outside backend remediation | TrayGlyph.Create returns Icon.FromHandle(bitmap.GetHicon()); this merits a focused native icon ownership/DestroyIcon check under repeated tray redraw. It is visual-platform code with no measured resource growth here, not a demonstrated material backend defect. Defer to a tray-specific test with actual Windows smoke before changing ownership. |

Fail-closed renewal can require new authorization even if cancellation occurred just before
transmission. Refused fresh authorization can momentarily report NotConnected with a stored
grant before a subsequent load reports RecoveryRequired; the grant cannot be replayed, and
explicit fresh authorization/disconnect remain available. These bounded liveness/status limits
do not weaken durable safety. No required review is unavailable or BLOCKED.

## Initial Release measurements

Harness: `dotnet run --project tools/AiUsage.ProviderConsole -c Release --no-restore -- measure-backend`.
Local Windows 10.0.26200 x64, Intel Core i7-13620H, 10 cores/16 logical processors,
SDK 10.0.401/runtime 10.0.12. Four accounts, two limits each, 35 days, five-minute
sampling: 10,080 observations per series, 80,640 retained runs, 38,871,184 stored bytes.
Synthetic temporary app-owned stores only. Seeding excluded; the cold column is the first
measured invocation of each workload after seeding/materialization, not a cold disk or fresh
store. Filesystem cache is not flushed. Three warmups, nine measured repetitions, process-wide
managed allocation deltas; timing ranges expose local scheduling/GC variability.

| Workload | Baseline median ms (min-max) | After core fixes median ms (min-max) | Baseline/after allocated bytes |
| --- | --- | --- | --- |
| Read eight series | 444.209 (398.805-488.372) | 435.046 (407.340-469.587) | 100,749,848 / 100,748,728 |
| Append eight observations | 652.518 (626.956-668.340) | 660.118 (619.670-692.214) | 165,097,768 / 165,093,632 |
| Budget/history eight series | 87.508 (59.166-135.729) | 92.394 (55.001-118.708) | 32,454,920 / 32,454,920 |
| Estimate four session pairs | 39.180 (34.706-57.843) | 15.137 (14.784-19.431) | 6,525,416 / 6,421,736 |
| Four parsers x 1,000 payloads | 33.087 (28.579-42.578) | 31.944 (29.580-44.741) | 7,312,072 / 7,312,072 |

Codex/Claude/Copilot/Antigravity payloads are respectively 197/133/156/146 UTF-8 bytes,
with two normalized limits each. The duplicate-append scenario initially selected two
observations from one series despite its label; that result is excluded. Corrected harness
selects the final observation from each of two series (156.390 ms, 40,945,320 allocated bytes).
This harness correction does not affect the comparable workloads above. First measured-call
baseline/after timings (ms): read 447.399/463.483; append 647.592/642.767;
budget/history 88.502/91.496; sessions 88.722/28.063; parsers 98.407/122.339.
The parser first-call increase is not a steady-state regression: parser code is unchanged,
allocation delta is only 1,992 bytes, and warmed ranges overlap. Read/append/budget warmed
ranges overlap too. Session warmed ranges do not overlap; median improves 61.4% and allocations
fall 1.6%. Closed-period clamping correction does not change this open-period workload.
Raw output remains local under `.ai-usage-local/AIU-042`; the portable harness and numbers
above are committed evidence. No end-to-end latency claim.

## Runtime/language assessment

Microsoft's [.NET 10 overview](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
and [C# 14 reference](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
were checked on 2026-10-02. Stable span support is suitable for the bounded coverage lookup;
no extension-member framework, field-property churn, preview features, SDK/dependency change
or floating language version is warranted. `decimal.Scale` was assessed as an allocation-free
alternative to GetBits in the recorder but not adopted: no isolated benefit was measured,
and the observed workload cost is dominated by serialization. This is not an outstanding gate.
