---
id: AIU-028
schema_version: 1
---
# AIU-028 implementation plan

Goal: close the fifteen findings recorded in [spec.md](spec.md) without changing authentication
behavior, stored-data formats, quota semantics or close-to-tray behavior. Design:
[design.md](design.md). Evidence: [verification.md](verification.md).

Execute sequentially under CONTRIBUTING.md; the primary owns all changes. No task adds, upgrades
or removes a dependency, analyzer package or SDK version. Commit and push after each task once
its named check passes, recording honestly when a check is NOT_RUN.

Ordering rationale: T-01 and T-02 come first because F-02 is the only finding with a shipped
security-boundary consequence and because the shared seam they build is what makes T-03 and
T-04 small. T-03 and T-04 close the "a defect looks like a provider outage" pair, which is what
makes every later task diagnosable. Polish is last.

### T-01 - Shared provider transport, exception and state lease
- status: in-progress
- depends_on: []
- acceptance: AC-01, AC-02
- evidence: not-run

- [ ] Extract one transport-failure and HTTP-status translator and one handler-configuration
      helper; delete the four per-provider copies.
- [ ] Introduce `ProviderException` over `ProviderFailureKind`; delete `ClaudeException`,
      `CopilotException` and `AntigravityException`.
- [ ] Extract `ProviderStateLease<TState>` from the three identical stores, parameterised by
      file names, entropy, serializer context and validation predicate.
- [ ] Confirm each provider keeps its existing file names, entropy string and record shape by
      loading a record written by the pre-change code in a test.
- [ ] Check: `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release
      --no-restore -- -noLogo` passes with no reduction in test count.

### T-02 - Codex grant store onto the hardened lease
- status: pending
- depends_on: [T-01]
- acceptance: AC-02
- evidence: not-run

- [ ] Move `CodexGrantStore` onto the shared lease: exclusive lock, reparse-point checks on
      directory and file, pending/committed generation, flush-to-disk, asynchronous I/O.
- [ ] Keep the `codex.grant` name, the `AiUsage.Codex.Grant.v1` entropy and the `v`/`account`/
      `refresh` field names; read an existing record written by the current code without
      requiring reconnection.
- [ ] Apply the same treatment to `CodexQuotaCache` path handling, which shares the unchecked
      `File.WriteAllBytes` and `File.Delete` shape.
- [ ] Add a test, for all four providers including Codex, asserting a reparse point on the state
      directory is refused rather than followed.
- [ ] Use the security-lifecycle skill and record the data-lifecycle reasoning before
      implementing. This task needs focused independent review under CONTRIBUTING.md, because it
      is a credential-storage change.
- [ ] Check: Infrastructure Release suite, plus the new forward-compatibility and reparse tests.

### T-03 - Retire the Codex-only session contract
- status: pending
- depends_on: [T-01, T-02]
- acceptance: AC-03
- evidence: not-run

- [ ] Implement `IProviderSession` on `CodexSession`; delete `ICodexSession`,
      `CodexSessionState`, `CodexFailureKind` and `CodexDashboardSession`.
- [ ] Update `LiveServiceRegistration` and `tools/AiUsage.ProviderConsole` to the single
      contract; no `Enum.Parse` remains in Infrastructure.
- [ ] Confirm the off-dispatcher execution the deleted adapter provided is preserved by the
      asynchronous lease from T-01.
- [ ] If this task is deferred by the owner, the fallback is mandatory rather than optional:
      replace both `Enum.Parse` calls with an explicit `switch` and add a test asserting
      `CodexFailureKind` and `CodexSessionStatus` member containment, so F-03 does not stay open.
- [ ] Check: Infrastructure and Presentation Release suites, plus a Release build of
      `tools/AiUsage.ProviderConsole`.

### T-04 - Classify unclassified failures and add redacted diagnostics
- status: pending
- depends_on: [T-03]
- acceptance: AC-04, AC-05
- evidence: not-run

- [ ] Stop returning `ProviderUnavailable` for exceptions that are not classified provider
      failures; introduce a distinct failure kind whose presentation does not offer a retry that
      cannot succeed, and add its resource keys to the existing failure families.
- [ ] Add the local, size-bounded, allowlisted, redacted diagnostics sink described in
      [design.md](design.md), wired to the three catches in `App.xaml.cs` and to the new
      classified catch. Keep `RemoveAllLoggers()` on all four provider transports.
- [ ] Record the sink's location, retention and redaction rules against
      [security and lifecycle](../../platforms/windows/security-and-lifecycle.md) before
      implementing, using the security-lifecycle skill.
- [ ] Add a redaction test covering nested unknown fields, a token-shaped value and an opaque
      provider identifier; assert none reaches a record.
- [ ] Check: Presentation Release suite with a test injecting a non-provider exception, plus the
      redaction test.

### T-05 - One provider descriptor table
- status: pending
- depends_on: [T-03]
- acceptance: AC-06
- evidence: not-run

- [ ] Replace the six declarations with one descriptor table read by the composition root and by
      presentation; keep provider ids untranslated protocol values.
- [ ] Drive manual-code capability from `IProviderSession.TrySubmitCode` rather than the
      `request.ProviderId == "claude"` literal.
- [ ] Add a Presentation test that registers a synthetic fifth descriptor and asserts it reaches
      every derived surface, so a missed registry is a failing test rather than a fallback
      monogram.
- [ ] Check: Presentation Release suite including `DependencyBoundaryTests`.

### T-06 - Shared MSBuild and central package version roots
- status: pending
- depends_on: []
- acceptance: AC-07
- evidence: not-run

- [ ] Add `Directory.Build.props` with the shared language and warning properties; remove the
      per-project copies.
- [ ] Add `Directory.Packages.props` with `ManagePackageVersionsCentrally` pinning every package
      at its **current** version; strip `Version` attributes from `PackageReference` items.
- [ ] Confirm no version string changed, by diff inspection, before running anything.
- [ ] Check: full offline restore from the local package cache, then all four suites and
      `git diff --check`. This task is independent of T-01..T-05 and may be sequenced earlier if
      convenient, but must not be combined with T-07.

### T-07 - Explicit analyzer level and code-style enforcement
- status: pending
- depends_on: [T-06]
- acceptance: AC-08
- evidence: not-run

- [ ] Set `AnalysisMode` to `Recommended`, pin `AnalysisLevel` so an SDK upgrade is deliberate,
      and set `EnforceCodeStyleInBuild` in the shared props.
- [ ] Move the intended C# style rules into `.editorconfig`, which today carries whitespace only.
- [ ] Triage the resulting diagnostics within an agreed time box. Where a rule is not adopted,
      record an explicit severity with a reason rather than lowering the level silently.
- [ ] If the triage exceeds the box, stop and report; do not weaken the level to obtain a green
      build.
- [ ] Check: `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug
      -p:Platform=x64 -p:WindowsPackageType=None --no-restore` with zero warnings, plus all four
      suites.

### T-08 - Non-throwing disposal and lock-free publication
- status: done
- depends_on: []
- acceptance: AC-09, AC-10
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; tests/windows/AiUsage.Presentation.Tests/DashboardWorkflowTests.cs and tests/windows/AiUsage.Presentation.Tests/LiveAdapterTests.cs; Presentation Release suite 129 tests, 0 failed; each new test observed failing against the pre-change code before the fix

- [x] Make `DashboardWorkflow.Dispose` idempotent and non-throwing; keep the "await `StopAsync`
      first" contract as a documented precondition or debug assertion.
- [x] Assign the snapshot revision under the lock in `LiveUsageSource.Publish`, then invoke
      subscribers outside it.
- [x] Check: Core test disposing with work outstanding and disposing twice; Presentation test
      whose subscriber re-enters the source during publication.

### T-09 - Source-generated preference serialization
- status: done
- depends_on: []
- acceptance: AC-11
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; tests/windows/AiUsage.Presentation.Tests/LiveAdapterTests.cs; Presentation Release suite 129 tests, 0 failed; unknown members and known values preserved across a write and a reload

- [x] Add a `JsonSerializerContext` for `LivePreferenceStore.State` and use it at both call
      sites; keep `[JsonExtensionData]` and do not add `UnmappedMemberHandling.Disallow`.
- [x] Confirm an existing preference file round-trips unchanged, including unknown members.
- [x] Check: Presentation Release suite.

### T-10 - Gate the UI clock on window visibility
- status: pending
- depends_on: [T-05]
- acceptance: AC-12
- evidence: not-run

- [ ] Stop the `LiveClock` tick while the main window is hidden to the tray, using the existing
      `WindowVisible` signal, and re-apply once on restore.
- [ ] Confirm tray quota text stays correct while hidden, since the tray view model is also a
      `SnapshotViewModel`.
- [ ] Check: Presentation test over the visibility gate, **plus** interactive Windows smoke per
      [the verification policy](../../workflow/verification.md). Compilation and unit tests are
      not sufficient evidence for this task.

### T-11 - Close CR-AIU-003-01: hardening at the library boundary
- status: pending
- depends_on: [T-01, T-03]
- acceptance: AC-01
- evidence: not-run

- [ ] Make the eight auth and quota clients `internal`, widening `InternalsVisibleTo` for
      `tools/AiUsage.ProviderConsole` as already done for the test assembly, or construct the
      hardened primary handler inside the clients.
- [ ] Reduce the remaining Infrastructure public surface to what `AiUsage.Windows` and the
      console actually consume.
- [ ] Update the CR-AIU-003-01 row in [the backlog](../../backlog.md) to resolved, with the
      evidence reference.
- [ ] Check: Infrastructure Release suite and a Release build of `tools/AiUsage.ProviderConsole`.

### T-12 - Transport options
- status: pending
- depends_on: [T-01]
- acceptance: AC-01
- evidence: not-run

- [ ] Introduce `ProviderTransportOptions` for the request deadline, pooled-connection lifetime
      and retry-after fallback, bound once in the DI extensions, keeping today's values exactly.
- [ ] Check: Infrastructure Release suite. Lowest priority; drop it if T-01 already leaves a
      single call site per value.

## Accepted as-is

These findings-shaped observations were examined and are deliberately not being changed. The
reasoning is in [spec.md](spec.md#accepted-as-is); they are listed here so no task is created
for them later by accident.

- Presentation sources linked into the neutral test project — settled by the AIU-027 design.
- Typed `HttpClient` captured by singleton sessions — already carries the exact mitigation
  Microsoft prescribes for that case (`SocketsHttpHandler` with `PooledConnectionLifetime`).
- `x:Bind` coverage, automation properties, event-handler symmetry, view-model disposal,
  `ConfigureAwait` policy and the single `async void` — examined, no defect found.
- Deferred loading (`x:Load`) — a preference with no measurement behind it, not a finding.
- Inline-JSON parser tests for Copilot and Antigravity — the required negative cases and
  independent semantic assertions are present; fixture form is a preference.
- `LivePreferenceStore` fail-closed behavior on a corrupt preference file — deliberate.

## Handoff

The owner selected T-01 and T-02 on 2026-09-22. Base: `cfb9ceb` on `main`.
T-08 and T-09 were already complete. T-01 extraction is implemented with its compatibility
checks; focused independent review remains pending for the combined T-01/T-02 changes.

Exact next action: implement T-02's Codex adoption, recovery and path-safety tests, then obtain
focused independent review of the integrated diff. T-03 and the remaining tasks are not selected.
Blockers: none. Live provider and interactive/package checks have not been run for this change.