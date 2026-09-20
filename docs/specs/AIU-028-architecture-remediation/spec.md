---
id: AIU-028
type: spec
status: draft
goal: G-003
scope_version: 1
approval_basis: Derived within the owner's 2026-09-20 request to audit the repository against modern .NET and WinUI 3 / MSIX practice and land the remediation as project documents. The audit session changed no production code; selecting this remediation remains an owner decision under CONTRIBUTING.md.
---
# Architecture and clean-code remediation

Derived from the analysis-only audit run on 2026-09-20 under
[the audit plan](../../workflow/architecture-audit-plan.md). Every finding below cites
`path:line` evidence read in the code at commit `6681b7a`, the head of `main` at the
time of the audit.

[AIU-027](../AIU-027-architecture-refinement/spec.md) settled the Core / Infrastructure /
Windows split, the credential-free `IProviderSession` port and the source-linked presentation
test project. This entry does not re-propose any of that. It records drift and debt that
accumulated while AIU-007, AIU-008, AIU-009 and AIU-010 were delivered on top of it, and cites
the AIU-027 design wherever a finding touches an accepted decision.

## Intended end state

Adding a fifth provider is an additive change: one session implementation, one registration and
one descriptor entry, with no new copy of the transport, error-translation, exception or
DPAPI-state code. All four providers share one hardened credential-store lease. A defect is
distinguishable from a provider outage, in the UI and in a local diagnostic record. Repository
build policy lives in one place and the "owned-code warnings are errors" rule is enforced over a
rule set that is deliberately chosen rather than inherited by default.

Authentication behavior, stored-data file names, DPAPI entropy strings, serialized record
shapes, quota semantics and close-to-tray behavior are unchanged by every task below. No task
adds, upgrades or removes a runtime dependency, analyzer package or SDK version.

## Acceptance

- AC-01: One shared provider transport component owns HTTP send, transport-failure translation
  and HTTP-status-to-failure-kind mapping for Codex, Claude, Copilot and Antigravity. No
  provider folder contains its own copy of that switch. Verified by inspection of
  `src/windows/AiUsage.Infrastructure/Providers/` and by the Infrastructure Release suite.
- AC-02: One shared DPAPI state lease owns the exclusive lock, reparse-point checks, the
  pending/committed revision generation and the flush-to-disk write, and all four providers use
  it. Each provider keeps its existing file names, entropy string and record shape. Verified by
  the Infrastructure Release suite plus a new test asserting that a reparse point on the state
  directory is refused for every provider, including Codex.
- AC-03: Codex implements `IProviderSession` directly. `ICodexSession`, `CodexSessionState` and
  `CodexFailureKind` are deleted, and no enum value crosses a boundary as a string. Verified by
  the Infrastructure and Presentation Release suites and by the absence of
  `Enum.Parse` in `src/windows/AiUsage.Infrastructure/`.
- AC-04: An exception that is not a classified provider failure is no longer reported to the
  user as `ProviderUnavailable`. Verified by a Presentation test that injects a non-provider
  exception and asserts a distinct failure kind and message key.
- AC-05: The application records redacted structured diagnostics for startup failure, shutdown
  failure and unclassified operation failure. No token, credential, provider payload or opaque
  provider identifier appears in a record. Verified by a test over the redaction boundary and by
  inspection against [security and lifecycle](../../platforms/windows/security-and-lifecycle.md).
  `RemoveAllLoggers()` on provider transports is retained.
- AC-06: Provider identity (id, display name, glyph, connection methods, brand colour) is
  declared once. Manual-code capability is read from the session port, not from a provider-id
  string literal. Verified by a Presentation test that adds a synthetic fifth descriptor and
  asserts it appears in every derived surface.
- AC-07: A `Directory.Build.props` carries the shared language and warning properties, and a
  `Directory.Packages.props` with `ManagePackageVersionsCentrally` carries every package
  version at its current value. No version changes. Verified by a full offline restore and all
  four suites, plus `git diff --check`.
- AC-08: `AnalysisMode` and `EnforceCodeStyleInBuild` are set explicitly in the shared props,
  and `.editorconfig` carries the C# style rules the repository intends to enforce. The
  warnings-visible desktop build passes with zero warnings at the chosen level. Verified by the
  build command in [verification](verification.md).
- AC-09: `DashboardWorkflow.Dispose` is idempotent and does not throw. Verified by a Core test
  that disposes with work outstanding and with Dispose called twice.
- AC-10: `LiveUsageSource` invokes snapshot subscribers outside its lock. Verified by a
  Presentation test whose subscriber re-enters the source during publication.
- AC-11: Preference state serializes through a source-generated `JsonSerializerContext`, keeping
  the existing `[JsonExtensionData]` forward-compatibility. Verified by the Presentation suite
  and by the absence of reflection-based `JsonSerializer` calls in `src/`.
- AC-12: The UI clock does not tick while the main window is hidden to the tray, and
  time-relative text is correct immediately after restore. Verified by a Presentation test over
  the visibility gate and by interactive Windows smoke.

## Findings

Severity: **S1** correctness, security or data-loss risk in shipped behavior; **S2** boundary
violation or a defect that will recur as features are added; **S3** maintainability, testability
or consistency; **S4** polish.

No S1 finding was identified. That is a result, not an omission: the credential stores for
Claude, Copilot and Antigravity, the loopback callback, the quota-value semantics and the
`x:Bind` and accessibility surfaces were all examined and found sound.

### F-01 - Provider transport, error translation, exception and state store are duplicated per provider

| Field | Content |
| --- | --- |
| severity | S2 |
| category | architecture |
| evidence | Transport and status mapping, four copies: `src/windows/AiUsage.Infrastructure/Providers/Codex/CodexHttp.cs:12`, `Providers/Claude/ClaudeHttp.cs:12`, `Providers/Copilot/CopilotHttp.cs:14`, `Providers/Antigravity/AntigravityHttp.cs:64`. Exception type, three identical copies: `Providers/Claude/ClaudeException.cs:7`, `Providers/Copilot/CopilotException.cs:6`, `Providers/Antigravity/AntigravityException.cs:6`. DPAPI state store, three near-verbatim copies of ~200 lines: `Providers/Claude/ClaudeStateStore.cs:1`, `Providers/Copilot/CopilotStateStore.cs:1`, `Providers/Antigravity/AntigravityStateStore.cs:1`. Handler configuration, four copies: `Providers/Codex/CodexServiceCollectionExtensions.cs:37`, `Providers/Claude/ClaudeServiceCollectionExtensions.cs:25`, `Providers/Copilot/CopilotServiceCollectionExtensions.cs:25`, `Providers/Antigravity/AntigravityServiceCollectionExtensions.cs:25` |
| principle | One place for one decision. Plan axis A: "whether a fifth provider is an additive change or a shotgun edit". Measured: after normalizing provider names, `ClaudeStateStore.cs` and `CopilotStateStore.cs` differ by file names and the enum namespace only; `ClaudeStateStore.cs` and `AntigravityStateStore.cs` additionally differ by one comment. |
| impact | A fifth provider costs roughly 250 copied lines before any provider-specific work begins. A correction to one store's recovery ordering must be applied three times or the copies silently diverge. F-02 is that divergence, already realised. |
| fix | Extract one generic `ProviderStateLease<TState>`, one transport-failure translator with the status map, one `ProviderException` carrying `ProviderFailureKind`, and one handler-configuration helper. Must preserve each provider's file names, entropy string, record shape and observable failure kinds. |
| cost | Medium. Infrastructure only; no public contract change required beyond F-14. Regression risk is covered by the 215-test Infrastructure suite, which asserts per-provider store and transport behavior. |

### F-02 - The Codex grant store lacks the storage hardening every later provider has

| Field | Content |
| --- | --- |
| severity | S2 |
| category | security |
| evidence | `src/windows/AiUsage.Infrastructure/Providers/Codex/CodexGrantStore.cs:42` (`Read`), `:72` (`Write`), `:91` (`Delete`) against `Providers/Claude/ClaudeStateStore.cs:25` (exclusive lease), `:41` and `:50` (reparse-point checks on directory and file), `:79` (parent-revision recovery), `:115` (`FileOptions.WriteThrough`), `:120` (`Flush(flushToDisk: true)`), `:143` (checked delete). Codex has none of these: it uses `File.ReadAllBytes` at `:50`, `File.WriteAllBytes` at `:85` and `File.Delete` at `:95` with no path check, no lock, no generation and no durable flush. |
| principle | [security and lifecycle](../../platforms/windows/security-and-lifecycle.md): "Filesystem deletion constrained to approved owned roots, normalized path and reparse/symlink protection", and "Every secret write interrupted at staged/replace/ref-update phase has defined recovery". |
| impact | A reparse point on the app-owned `providers` directory or on `codex.grant` redirects the protected-blob write outside the owned root; the same path is read back without a check. An interrupted write has no journal, so a crash between stage and move is undetectable rather than recoverable. Codex is the only provider where this is true, and it is the provider with the longest-lived stored grant. |
| fix | Move Codex onto the shared lease from F-01. Preserve the `codex.grant` file name, the `AiUsage.Codex.Grant.v1` entropy and the `StoredRecord` v1 field names (`v`, `account`, `refresh`), and keep reading an existing unversioned-layout record so a connected account survives the change. |
| cost | Medium. Touches Codex resume and disconnect; `CodexGrantStoreTests` (7 tests) and `CodexSessionTests` (10 tests) must be extended rather than rewritten. Migration of an existing on-disk record is the main regression risk and needs an explicit forward-compatibility test. |

### F-03 - Codex state crosses the boundary by string round-tripping two enums

| Field | Content |
| --- | --- |
| severity | S2 |
| category | dotnet |
| evidence | `src/windows/AiUsage.Infrastructure/Providers/Codex/CodexDashboardSession.cs:36` maps with `Enum.Parse<ProviderSessionStatus>(state.Status.ToString())` and `Enum.Parse<ProviderFailureKind>(failure.ToString())`. The two source enums are `src/windows/AiUsage.Core/Providers/Codex/CodexFailureKind.cs:3` and `src/windows/AiUsage.Core/Providers/Codex/CodexSessionState.cs:5`; the two targets are `src/windows/AiUsage.Core/Usage/ProviderSessionState.cs:10` and `:5`. No test constrains the member sets: `tests/windows/AiUsage.Presentation.Tests/DependencyBoundaryTests.cs:99` covers resource-key families only. |
| principle | A relationship the compiler can check should not be deferred to a runtime string lookup. Plan axis A, error model. |
| impact | Latent, not live: the member sets are currently contained. Adding a member to `CodexFailureKind` or `CodexSessionStatus` without an identically named member on the provider enum compiles cleanly and then throws `ArgumentException` at runtime on the first state transition that uses it. That exception is not caught by the `IOException` and `UnauthorizedAccessException` handlers at `CodexDashboardSession.cs:31`, so it reaches the catch-all in F-04 and is displayed as "provider unavailable" — with no record, because of F-05. |
| fix | Subsumed by F-03's real remedy, F-06: delete the Codex-only enums. If F-06 is deferred, replace both `Enum.Parse` calls with an explicit `switch` so the compiler reports an unmapped member, and add a test asserting set containment. |
| cost | Small either way. One file plus one test. |

### F-04 - Unclassified exceptions are flattened into a provider fault

| Field | Content |
| --- | --- |
| severity | S2 |
| category | dotnet |
| evidence | `src/windows/AiUsage.Windows/Adapters/Live/LiveUsageSource.cs:140` catches `Exception` and returns `ProviderFailureKind.ProviderUnavailable` at `:142` and `:143`. |
| principle | Plan axis A: "anywhere a failure is swallowed or flattened into a misleading value". |
| impact | A storage fault, a DPAPI fault, a mapping bug (F-03) and a genuine provider outage are indistinguishable to the user. The failure surface renders `ProviderUnavailable` as recoverable (`Adapters/Live/LiveMapping.cs:71`), so the user is offered a Retry that cannot succeed for the non-provider cases. Combined with F-05, the real cause is not recoverable after the fact either. |
| fix | Keep the catch so a refresh cannot crash the app, but classify: let already-typed provider failures through unchanged, and give everything else a distinct failure kind whose presentation does not promise a useful retry. Record it under F-05. Must preserve: no unhandled exception escapes the refresh path, and quota semantics are unchanged. |
| cost | Small to medium. One adapter, one new failure kind, one resource key, and the `Failure_Short_*` and failure-kind resource families in `tests/windows/AiUsage.Presentation.Tests/DependencyBoundaryTests.cs:99`. |

### F-05 - The application has no logging or diagnostics of any kind

| Field | Content |
| --- | --- |
| severity | S2 |
| category | build |
| evidence | No `ILogger`, `ActivitySource` or metric appears anywhere under `src/`. `src/windows/AiUsage.Windows/App.xaml.cs:31` builds the host with `DisableDefaults = true`, so no logging provider is configured at all. Three catches discard the exception entirely and set only an exit code: `App.xaml.cs:56`, `:89`, `:100`. `src/windows/AiUsage.Windows/Adapters/Live/UnavailableServices.cs:25` returns `string.Empty` from `PreviewLogsAsync`, so the Settings log surface is wired to nothing. |
| principle | [security and lifecycle](../../platforms/windows/security-and-lifecycle.md) Diagnostics section already specifies the shape of this record: "Failure diagnostics are safe allowlisted structure (field/type info, redacted values where safe)", seven-day retention, and "No automatic network telemetry". The policy exists; the implementation does not. |
| impact | A startup failure exits the process with code 1 and leaves no record anywhere, so a user report cannot be acted on. Combined with F-04, a product defect is indistinguishable from a provider outage in the only evidence that exists. AIU-013 diagnostics export has no source to draw from. |
| fix | Add structured logging to a local, size-bounded, redacted sink behind the existing allowlist rules. Startup failure, shutdown failure and unclassified operation failure are the minimum. No token, credential, provider payload or opaque provider identifier may be recorded, and `RemoveAllLoggers()` on the four provider transports must stay, because provider traffic is exactly what must not be logged. No automatic network telemetry. |
| cost | Medium. `Microsoft.Extensions.Hosting` is already referenced (`src/windows/AiUsage.Windows/AiUsage.Windows.csproj:26`), so this adds no dependency. The cost is in the redaction boundary and its tests, not the plumbing. |

### F-06 - Codex keeps a parallel provider contract that no other provider has

| Field | Content |
| --- | --- |
| severity | S3 |
| category | architecture |
| evidence | `src/windows/AiUsage.Core/Providers/Codex/ICodexSession.cs:4`, `src/windows/AiUsage.Core/Providers/Codex/CodexSessionState.cs:19` and `src/windows/AiUsage.Core/Providers/Codex/CodexFailureKind.cs:3` exist alongside `src/windows/AiUsage.Core/Usage/IProviderSession.cs:4`, `Usage/ProviderSessionState.cs:24` and `Usage/ProviderSessionState.cs:10`. `src/windows/AiUsage.Infrastructure/Providers/Codex/CodexDashboardSession.cs:10` adapts one to the other, while `Providers/Claude/ClaudeSession.cs:8`, `Providers/Copilot/CopilotSession.cs` and `Providers/Antigravity/AntigravitySession.cs` implement `IProviderSession` directly. The credential type diverges the same way: `Providers/Codex/CodexCredentials.cs:6` is mutable, `IDisposable` and carries a `SemaphoreSlim` at `:18`, against three immutable peers (`Providers/Claude/ClaudeCredentials.cs:7`, `Providers/Copilot/CopilotCredentials.cs:6`, `Providers/Antigravity/AntigravityCredentials.cs:9`). |
| principle | One port per concept. The [AIU-027 design](../AIU-027-architecture-refinement/design.md) states "Infrastructure implements the session contract"; the contract it introduced is `IProviderSession`. Codex was adapted to it rather than migrated onto it, and the three later providers were built on the contract directly. This finding completes AIU-027 rather than disputing it. |
| impact | Every new failure kind or status must be declared twice and kept name-identical, which is the trap in F-03. A reader must learn two contracts for one concept, and the Codex path carries an extra `Task.Run` hop at `CodexDashboardSession.cs:27` that the other three do not need. |
| fix | Implement `IProviderSession` on `CodexSession`; delete `ICodexSession`, `CodexSessionState`, `CodexFailureKind` and `CodexDashboardSession`. Preserve the Codex cache file `codex.quota.json`, the stored grant and the observable failure kinds. `ICodexSession` has no consumer outside this repository. |
| cost | Medium. `CodexSessionTests`, `CodexQuotaClientTests`, `CodexAuthClientTests` and `tools/AiUsage.ProviderConsole/Program.cs:40` move with it. Best sequenced immediately after F-01 and F-02, which already touch these files. |

### F-07 - Provider identity is declared in six independent places

| Field | Content |
| --- | --- |
| severity | S3 |
| category | architecture |
| evidence | `src/windows/AiUsage.Windows/Adapters/Live/Windows/LiveServiceRegistration.cs:43` (session map), `Adapters/Live/LiveConnectionFlow.cs:12` (descriptors and connection methods), `Adapters/Live/LiveConnectionFlow.cs:19` (`request.ProviderId == "claude"`), `Adapters/Live/LiveMapping.cs:9` (display names), `src/windows/AiUsage.Windows/Features/Presentation/ViewModelSupport.cs:222` (names and glyphs), `src/windows/AiUsage.Windows/Controls/DisplayControls.cs:65` (brand colours). |
| principle | Single source of truth. Plan axis A: adding a provider should be additive. |
| impact | A fifth provider requires six coordinated edits, and nothing — not the compiler, not a test — reports a missed one; the symptom is a provider that connects but renders with a fallback monogram and an untranslated name. Separately, `LiveConnectionFlow.cs:19` hard-codes manual-code support to Claude, although `src/windows/AiUsage.Core/Usage/IProviderSession.cs:15` already defaults `TrySubmitCode` to `false` and each session reports its own capability. The string literal is a second, silently divergent source of truth for that capability. |
| fix | One descriptor table read by the composition root and by presentation; drive manual-code capability from the port instead of the literal. Provider ids stay protocol values and stay untranslated (`ViewModelSupport.cs:230`). |
| cost | Small to medium. Presentation and adapters only, already covered by `DependencyBoundaryTests`. |

### F-08 - Nine project files repeat the same properties and versions with no shared root

| Field | Content |
| --- | --- |
| severity | S3 |
| category | build |
| evidence | The same `ImplicitUsings` / `Nullable` / `TreatWarningsAsErrors` block appears in `src/windows/AiUsage.Core/AiUsage.Core.csproj:4`, `src/windows/AiUsage.Infrastructure/AiUsage.Infrastructure.csproj:4`, `src/windows/AiUsage.Windows/AiUsage.Windows.csproj:18`, `tools/AiUsage.ProjectValidation/AiUsage.ProjectValidation.csproj:5`, `tools/AiUsage.ProviderConsole/AiUsage.ProviderConsole.csproj:5`, `tests/AiUsage.ProjectValidation.Tests/AiUsage.ProjectValidation.Tests.csproj:5`, `tests/windows/AiUsage.Infrastructure.Tests/AiUsage.Infrastructure.Tests.csproj:5`, `tests/windows/AiUsage.Presentation.Tests/AiUsage.Presentation.Tests.csproj:5` and `tests/windows/AiUsage.Windows.Tests/AiUsage.Windows.Tests.csproj:7`. `xunit.v3` version `3.2.2` is declared four times and `CommunityToolkit.Mvvm` version `8.4.2` twice. There is no `Directory.Build.props` and no `Directory.Packages.props` in the repository. |
| principle | [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management): `Directory.Packages.props` with `ManagePackageVersionsCentrally` is the documented way to manage common dependencies from one location. |
| impact | A policy change is a nine-file edit that is easy to apply incompletely. `xunit.v3` can drift between the four suites without any signal, which would make two suites' results non-comparable. |
| fix | Add `Directory.Build.props` for the shared properties and `Directory.Packages.props` pinning every package at **its current version**. This is a relocation, not an upgrade: no version string changes, so it does not add, upgrade or remove a dependency. |
| cost | Small, but it must be verified with a full offline restore and all four suites, because a mis-scoped property silently changes every project. |

### F-09 - "Warnings are errors" is enforced over the default minimal analyzer set

| Field | Content |
| --- | --- |
| severity | S3 |
| category | build |
| evidence | All nine project files set `TreatWarningsAsErrors=true` (see F-08 for the lines). None sets `AnalysisMode`, `AnalysisLevel` or `EnforceCodeStyleInBuild`. `.editorconfig:1` through `:15` contains whitespace conventions only: no `dotnet_diagnostic` or `dotnet_analyzer_diagnostic` severity, and no C# style rule. The warnings-visible desktop build in [verification](verification.md) produced zero warnings. |
| principle | [Overview of .NET source code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview): "In the default analysis mode (`Default`), only a small number of rules are enabled as build warnings." [MSBuild reference](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#code-analysis-properties): ".NET code style analysis is disabled, by default, on build for all .NET projects." |
| impact | `AGENTS.md` and `CONTRIBUTING.md` state that owned-code warnings are errors, which reads as a strong gate. It is enforced over whatever the SDK enables by default, and the rule set silently changes with every SDK upgrade because `AnalysisLevel` defaults to `latest`. The clean build is honest evidence about a small rule set, not about the code: the disposal and concurrency rules that would have surfaced F-10 are not running. |
| fix | Set `AnalysisMode` and `EnforceCodeStyleInBuild` explicitly in the F-08 shared props, pin `AnalysisLevel` so an SDK upgrade is a deliberate change, and move the intended C# style rules into `.editorconfig`. Start at `Recommended` and triage; do not jump to `All`. This enables analyzers already shipped in the SDK and adds no package. |
| cost | Triage cost is unknown until the level is raised, which is why this task is explicitly time-boxed and sequenced after F-08. If the triage is larger than the box, record the surviving rule severities as suppressions with reasons rather than lowering the level silently. |

### F-10 - `DashboardWorkflow.Dispose` throws

| Field | Content |
| --- | --- |
| severity | S3 |
| category | dotnet |
| evidence | `src/windows/AiUsage.Core/Dashboard/DashboardWorkflow.cs:89` throws `InvalidOperationException` at `:94` when work is outstanding. `src/windows/AiUsage.Windows/Adapters/Live/LiveUsageSource.cs:211` cascades into it for every entry, and the container reaches it through `host?.Dispose()` at `src/windows/AiUsage.Windows/App.xaml.cs:98`, inside the `finally` at `:93`. |
| principle | [Dispose Pattern](https://learn.microsoft.com/dotnet/standard/design-guidelines/dispose-pattern): "**X AVOID** throwing an exception from within `Dispose(bool)` … Users expect that a call to `Dispose` will not raise an exception. If `Dispose` could raise an exception, further finally-block cleanup logic will not execute." |
| impact | Bounded today, because `App.xaml.cs:100` catches it and the process still exits. The residual defect is ordering: `window?.CloseForExit()` runs before `host?.Dispose()` inside the same `try`, so a throw from either leaves the host undisposed and the grant-store and cache file handles held until process exit. |
| fix | Make `Dispose` idempotent and non-throwing: cancel the lifetime source and release. Keep the "await `StopAsync` first" contract as a documented precondition or a debug assertion rather than a thrown exception, and keep the existing drain ordering in `App`. |
| cost | Small. One Core file plus a Core test that disposes with work outstanding and disposes twice. |

### F-11 - Snapshot subscribers are invoked while the publisher's lock is held

| Field | Content |
| --- | --- |
| severity | S3 |
| category | dotnet |
| evidence | `src/windows/AiUsage.Windows/Adapters/Live/LiveUsageSource.cs:191` invokes `subscriber(current)`, inside `Publish`, which is only ever reached from within `lock (sync)`: `:55`, `:89`, `:153` and `:175`. The subscribers are UI-bound delegates registered at `src/windows/AiUsage.Windows/Features/Presentation/ViewModelSupport.cs:40` and `src/windows/AiUsage.Windows/MainWindow.xaml.cs:83`. |
| principle | Do not invoke unknown callbacks while holding a lock: it creates re-entrancy and lock-ordering hazards that the lock owner cannot reason about. |
| impact | Not observed with the current subscribers, which only marshal to the dispatcher and return. The hazard is structural: a subscriber that re-enters `LiveUsageSource` is admitted by the re-entrant `Monitor` and publishes a nested snapshot, so revision numbers assigned at `:184` interleave out of order; a subscriber that blocks on another thread needing `sync` deadlocks the whole refresh path. Both become reachable the moment a subscriber does more than enqueue. |
| fix | Capture the next snapshot and a copy of the subscriber array under the lock, assign the revision under the lock, then invoke outside it. `:191` already copies the array, so the change is to the invocation site, not the collection. |
| cost | Small in lines, but concurrency-sensitive. Verify with the Presentation suite plus a re-entrancy test. |

### F-12 - Preference state is the only reflection-based `System.Text.Json` site

| Field | Content |
| --- | --- |
| severity | S3 |
| category | dotnet |
| evidence | `src/windows/AiUsage.Windows/Adapters/Live/LivePreferenceStore.cs:27` calls `JsonSerializer.Deserialize<State>(json)` and `:83` calls `JsonSerializer.Serialize(next)`. Every other serialization site in `src/` uses a source-generated context: `Providers/Antigravity/AntigravityStateStore.cs:105` and `:165`, `Providers/Claude/ClaudeStateStore.cs:105` and `:165`, `Providers/Copilot/CopilotStateStore.cs:105` and `:165`, `Providers/Codex/CodexGrantStore.cs:59` and `:78`, `Providers/Codex/CodexQuotaCache.cs:39` and `:56`. |
| principle | Consistency, and trim/AOT safety. `src/windows/AiUsage.Windows/AiUsage.Windows.csproj:14` sets `PublishTrimmed=false` and `:15` sets `PublishAot=false`, so nothing is broken today. |
| impact | This is the single site that would fail if trimming or AOT is ever enabled, and it would fail by losing user preferences rather than by failing to build. It is also the only site running on default serializer options rather than the project's chosen ones. |
| fix | Add a `JsonSerializerContext` for `LivePreferenceStore.State`. Keep the `[JsonExtensionData]` member at `LivePreferenceStore.cs:113`, which is deliberate forward-compatibility for unknown preference members: do **not** apply `UnmappedMemberHandling.Disallow` here, unlike the credential stores. |
| cost | Small. One file plus the existing preference round-trip tests. |

### F-13 - The UI clock ticks every 30 seconds regardless of window visibility

| Field | Content |
| --- | --- |
| severity | S3 |
| category | winui |
| evidence | `src/windows/AiUsage.Windows/Adapters/Live/LiveClock.cs:8` constructs a `Timer` with a 30-second due time and period that raises `Changed` unconditionally. `src/windows/AiUsage.Windows/Features/Presentation/ViewModelSupport.cs:61` re-applies the entire snapshot on each tick for every `SnapshotViewModel`, of which there are twelve, re-running `CollectionSync.Sync` and all formatting. The gate already exists and is not consulted: `src/windows/AiUsage.Windows/Features/Presentation/HostAbstractions.cs:87` documents `IMotionSettings.WindowVisible` as "False while the main window is hidden to the tray; decorative animation and timers stop". |
| principle | A tray-resident application should not perform full presentation work while hidden. Plan axis B, performance; plan axis C, tray and lifetime ownership. The project's own abstraction states the intent. |
| impact | Continuous CPU and allocation every 30 seconds while the window is hidden to tray, which is this application's normal resting state. The work is entirely unobservable while hidden. |
| fix | Gate the tick on `WindowVisible`, and re-apply once on restore so relative-time text is correct immediately rather than up to 30 seconds stale. Must preserve: tray quota text stays correct, since the tray view model is also a `SnapshotViewModel`. |
| cost | Small in code. Needs interactive Windows smoke to confirm both the quiescence and the correct text on restore; compilation is insufficient evidence for this task. |

### F-14 - Provider client hardening lives only in DI; the public constructors take any `HttpClient`

| Field | Content |
| --- | --- |
| severity | S3 |
| category | security |
| evidence | `src/windows/AiUsage.Infrastructure/Providers/Codex/CodexAuthClient.cs:10`, `Providers/Claude/ClaudeAuthClient.cs:13`, `Providers/Copilot/CopilotAuthClient.cs:13` and `Providers/Antigravity/AntigravityAuthClient.cs:22` are all `public` and accept a raw `HttpClient`; the four quota clients do the same, for example `Providers/Claude/ClaudeQuotaClient.cs:6`. The no-redirect, no-cookie, no-logger and `PooledConnectionLifetime` policy exists only in the four `*ServiceCollectionExtensions` files (F-01 evidence). Infrastructure exposes 41 public types although its only consumers are `AiUsage.Windows` and `tools/AiUsage.ProviderConsole`. |
| principle | This is the repository's own deferred finding **CR-AIU-003-01** in [the backlog](../../backlog.md), recorded as "Before UI/persistent consumption, enforce hardened HTTP construction at the library boundary rather than relying on DI composition." That precondition is now met: AIU-004, AIU-007, AIU-008 and AIU-009 are all done, all with persistent UI consumption. The finding is being resumed, not re-proposed. |
| impact | No current caller constructs a client directly, so this is not a live defect. A future in-repo consumer that does gets `AllowAutoRedirect` and cookies enabled by default, which is precisely the configuration under which a bearer token can follow a redirect to another origin. |
| fix | Make the eight clients `internal` and widen `InternalsVisibleTo` — the mechanism already exists at `src/windows/AiUsage.Infrastructure/Properties/AssemblyInfo.cs:3` — or construct the hardened primary handler inside the client so no caller can supply an unhardened pipeline. Prefer the first; it also reduces the public surface. |
| cost | Small to medium. Touches `tools/AiUsage.ProviderConsole`, which constructs clients today. |

### F-15 - Timeouts and intervals are scattered literals with no options type

| Field | Content |
| --- | --- |
| severity | S4 |
| category | dotnet |
| evidence | `src/windows/AiUsage.Infrastructure/Providers/ProviderHttp.cs:23` (15-second request deadline), `Providers/LoopbackCallback.cs:75` (5-second header deadline), `src/windows/AiUsage.Windows/App.xaml.cs:85` (5-second host stop), `src/windows/AiUsage.Windows/Adapters/Live/LiveClock.cs:8` (30-second tick), `PooledConnectionLifetime = TimeSpan.FromMinutes(5)` at `Providers/Codex/CodexServiceCollectionExtensions.cs:42`, `Providers/Claude/ClaudeServiceCollectionExtensions.cs:30`, `Providers/Copilot/CopilotServiceCollectionExtensions.cs:30` and `Providers/Antigravity/AntigravityServiceCollectionExtensions.cs:30`, `Providers/Antigravity/AntigravityQuotaClient.cs:18`, and the triplicated retry-after fallback `TimeSpan.FromMinutes(1)` at `Providers/Claude/ClaudeQuotaClient.cs:35`, `Providers/Copilot/CopilotQuotaClient.cs:35` and `Providers/Antigravity/AntigravityQuotaClient.cs:196`. |
| principle | Options pattern over scattered constants. Plan axis B, configuration. |
| impact | There is no single place to read or tune the application's timeout behavior, and the retry-after fallback is stated three times, so a change to it is a three-file edit with no signal if one is missed. |
| fix | One `ProviderTransportOptions` bound once in the DI extensions, carrying the request deadline, the pooled-connection lifetime and the retry-after fallback. Keep the current values exactly. |
| cost | Small. Lowest priority; largely absorbed by F-01, which already consolidates these call sites. |

## Accepted as-is

These were examined against the audit plan's axes and need no change. They are recorded so a
later reader does not re-open them, and so "zero findings" is visible as a result rather than as
an absence.

- **Presentation sources linked into a neutral test project**
  (`tests/windows/AiUsage.Presentation.Tests/AiUsage.Presentation.Tests.csproj:16`). Settled by
  the [AIU-027 design](../AIU-027-architecture-refinement/design.md): "Tests link the actual
  presentation sources into a neutral test executable using the already pinned MVVM package,
  avoiding WinUI activation for view-model tests."
- **Typed `HttpClient` instances captured by singleton sessions.** `ClaudeSession`,
  `CopilotSession` and `AntigravitySession` are singletons holding transient typed clients,
  which is normally the captive-dependency defect. It is correctly mitigated here: all four
  registrations set a `SocketsHttpHandler` with `PooledConnectionLifetime` through
  `ConfigurePrimaryHttpMessageHandler`. That is exactly the remedy
  [IHttpClientFactory with .NET](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory#avoid-typed-clients-in-singleton-services)
  prescribes: "If you require the typed client approach, use `SocketsHttpHandler` with
  configured `PooledConnectionLifetime` as a primary handler." No finding.
- **Binding and accessibility.** All 21 views use `x:Bind` exclusively; there is not one
  `{Binding}` in the repository, and `AutomationProperties` appears across every view. No
  finding.
- **Event-handler symmetry and view-model leaks.** All five navigable pages set
  `NavigationCacheMode="Required"`, so the constructor subscriptions at
  `Features/Overview/OverviewPage.xaml.cs:13` and
  `Features/SystemStatus/SystemStatusPage.xaml.cs:13` are one-per-application-lifetime, and
  `Features/Accounts/AccountsPage.xaml.cs:29` guards its subscription with a first-navigation
  check. The child view models created by `CollectionSync` are plain `ObservableObject` and hold
  no subscriptions. `SnapshotViewModel` unsubscribes both its snapshot subscription and the
  clock handler in `Dispose` (`Features/Presentation/ViewModelSupport.cs:71`). No leak found.
- **`ConfigureAwait` policy.** Consistent by layer: Core and Infrastructure use
  `ConfigureAwait(false)` on every awaitable await, and the Windows application intentionally
  resumes on the UI thread. Not a defect.
- **`async void`.** The single occurrence, `src/windows/AiUsage.Windows/App.xaml.cs:27`, is the
  required WinUI `OnLaunched` override signature and wraps its entire body in a `try`/`catch`.
  Not a defect. Its catch is covered by F-05 for the missing record, not for the shape.
- **`x:Load` and deferred loading.** No page uses deferred loading. No startup measurement
  exists, so there is nothing to show it would pay for itself. Recorded as a preference with no
  authority behind it, per the audit plan's sourcing rule, not as a finding.
- **Golden-fixture form for Copilot and Antigravity.** Only Claude and Codex have file
  fixtures; Copilot and Antigravity assert over inline JSON. The required negative cases —
  unknown, missing, null, unlimited, exhausted, legacy, reset and credit — are present for all
  four parsers with independent semantic assertions, which is the substance
  [the verification policy](../../workflow/verification.md) asks for. Fixture form is a
  preference.
- **`LivePreferenceStore` fail-closed behavior.** A corrupt or invalid preference file leaves
  `writable` false for the session (`Adapters/Live/LivePreferenceStore.cs:28` and `:37`), so
  preference changes fail rather than overwrite an unreadable record. Deliberate and correct.

## Preservation

Infrastructure remains the only owner of credentials, refresh persistence, HTTP transport and
the DPAPI serializers. Core remains credential-free and free of transport, storage and
Windows-only types. No task reads or imports a source CLI credential, calls a live provider,
changes host trust, or alters a stored-data format: F-02 changes how the Codex record is written
and recovered, not what it contains, and must read an existing record written by the current
code.

No task adds, upgrades or removes a runtime dependency, analyzer package or SDK version. F-08
relocates existing version strings without changing them, and F-09 enables analyzers already
shipped inside the .NET SDK.
