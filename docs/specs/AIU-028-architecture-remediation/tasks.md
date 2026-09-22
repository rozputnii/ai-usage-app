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
- status: done
- depends_on: []
- acceptance: AC-01, AC-02
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; independent review of cfb9ceb..2ddcc7f PASS in a fresh primary session on 2026-09-22; Infrastructure 254/254 and Presentation 129/129 PASS; original-writer compatibility PASS

- [x] Extract one transport-failure and HTTP-status translator and one handler-configuration
      helper; delete the four per-provider copies.
- [x] Introduce `ProviderException` over `ProviderFailureKind`; delete `ClaudeException`,
      `CopilotException` and `AntigravityException`.
- [x] Extract `ProviderStateLease<TState>` from the three identical stores, parameterised by
      file names, entropy, serializer context and validation predicate.
- [x] Confirm each provider keeps its existing file names, entropy string and record shape by
      loading a record written by the pre-change code in a test.
- [x] Check: `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release
      --no-restore -- -noLogo` passes with no reduction in test count.

### T-02 - Codex grant store onto the hardened lease
- status: done
- depends_on: [T-01]
- acceptance: AC-02
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; independent review of cfb9ceb..2ddcc7f PASS in a fresh primary session on 2026-09-22; Infrastructure 254/254 and Presentation 129/129 PASS; original-writer compatibility PASS

- [x] Move `CodexGrantStore` onto the shared lease: exclusive lock, reparse-point checks on
      directory and file, pending/committed generation, flush-to-disk, asynchronous I/O.
- [x] Keep the `codex.grant` name, the `AiUsage.Codex.Grant.v1` entropy and the `v`/`account`/
      `refresh` field names; read an existing record written by the current code without
      requiring reconnection.
- [x] Apply the same treatment to `CodexQuotaCache` path handling, which shares the unchecked
      `File.WriteAllBytes` and `File.Delete` shape.
- [x] Add a test, for all four providers including Codex, asserting a reparse point on the state
      directory is refused rather than followed.
- [x] Use the security-lifecycle skill and record the data-lifecycle reasoning before
      implementing. This task needs focused independent review under CONTRIBUTING.md, because it
      is a credential-storage change.
- [x] Check: Infrastructure Release suite, plus the new forward-compatibility and reparse tests.

### T-03 - Retire the Codex-only session contract
- status: done
- depends_on: [T-01, T-02]
- acceptance: AC-03
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; code 13d45a7; Infrastructure 257/257 and Presentation 129/129 PASS; ProviderConsole Release and Windows Debug unpackaged builds PASS; primary review PASS

- [x] Implement `IProviderSession` on `CodexSession`; delete `ICodexSession`,
      `CodexSessionState`, `CodexFailureKind` and `CodexDashboardSession`.
- [x] Update `LiveServiceRegistration` and `tools/AiUsage.ProviderConsole` to the single
      contract; no `Enum.Parse` remains in Infrastructure.
- [x] Confirm the off-dispatcher execution the deleted adapter provided is preserved by the
      asynchronous lease from T-01.
- [x] Deferral fallback is not applicable: the legacy contract and both `Enum.Parse` calls
      were removed, closing F-03 without a compatibility mapping.
- [x] Check: Infrastructure and Presentation Release suites, plus a Release build of
      `tools/AiUsage.ProviderConsole`.

### T-04 - Classify unclassified failures and add redacted diagnostics
- status: done
- depends_on: [T-03]
- acceptance: AC-04, AC-05
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; independent review of 56d6315..55bb169 PASS in a fresh primary session on 2026-09-22, no actionable findings; fresh Infrastructure 261/261 and Presentation 136/136 PASS; unchanged production implementation 9bbf13e retains Windows smoke 7/7 and unsigned MSIX evidence

- [x] Stop returning `ProviderUnavailable` for exceptions that are not classified provider
      failures; introduce a distinct failure kind whose presentation does not offer a retry that
      cannot succeed, and add its resource keys to the existing failure families.
- [x] Add the local, size-bounded, allowlisted, redacted diagnostics sink described in
      [design.md](design.md), wired to the three catches in `App.xaml.cs` and to the new
      classified catch. Keep `RemoveAllLoggers()` on all four provider transports.
- [x] Record the sink's location, retention and redaction rules against
      [security and lifecycle](../../platforms/windows/security-and-lifecycle.md) before
      implementing, using the security-lifecycle skill.
- [x] Add a redaction test covering nested unknown fields, a token-shaped value and an opaque
      provider identifier; assert none reaches a record.
- [x] Check: Presentation Release suite with a test injecting a non-provider exception, plus the
      redaction test.

### T-05 - One provider descriptor table
- status: done
- depends_on: [T-03]
- acceptance: AC-06
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; code fc402dd; Presentation 142/142 and Infrastructure 261/261 PASS; Windows Debug build, product and demo smoke 7/7 each, unsigned MSIX and primary integrated review PASS

- [x] Replace the six declarations with one descriptor table read by the composition root and by
      presentation; keep provider ids untranslated protocol values.
- [x] Drive manual-code capability from `IProviderSession.TrySubmitCode` rather than the
      `request.ProviderId == "claude"` literal.
- [x] Add a Presentation test that registers a synthetic fifth descriptor and asserts it reaches
      every derived surface, so a missed registry is a failing test rather than a fallback
      monogram.
- [x] Check: Presentation Release suite including `DependencyBoundaryTests`.

Implementation plan (2026-09-22, base `d90e90d`): introduce an immutable Windows-owned
descriptor catalog carrying identity, connection methods, brand styling and the existing
demo differences. Inject the same catalog into live composition, mapping, connection flow,
presentation context and provider tiles. Keep session registration in the live composition
boundary and resolve each catalog entry through its registration. Preserve the full/compact
Copilot labels, opaque ordinal provider identifiers, demo behavior and neutral/high-contrast
tile styling. Forward manual-code submission to the active session port. Add synthetic fifth
provider coverage across derived surfaces and negative submission cases, then run Infrastructure
and Presentation Release, document validation, Windows build/package and applicable local smoke.
Only T-05 was selected. Implementation and primary integrated review are complete; Presentation
142/142, Infrastructure 261/261, Windows Debug build, product/demo smoke 7/7 each and unsigned
package build pass. AC-06 is complete; evidence is recorded in verification.md.

### T-06 - Shared MSBuild and central package version roots
- status: in-progress
- depends_on: []
- acceptance: AC-07
- evidence: not-run

- [x] Add `Directory.Build.props` with the shared language and warning properties; remove the
      per-project copies.
- [x] Add `Directory.Packages.props` with `ManagePackageVersionsCentrally` pinning every package
      at its **current** version; strip `Version` attributes from `PackageReference` items.
- [x] Confirm no version string changed, by diff inspection, before running anything.
- [ ] Check: full offline restore from the local package cache, then all four suites and
      `git diff --check`. This task is independent of T-01..T-05 and may be sequenced earlier if
      convenient, but must not be combined with T-07.

Implementation plan (2026-09-22, base `34ee90b`): relocate the identical `ImplicitUsings`,
`Nullable` and `TreatWarningsAsErrors` properties from all ten projects, including the routing
spike, into the root build props. Centralize all ten distinct package versions without changing
project membership or values. Inspect the diff before restore/build, compare evaluated properties
and restored package graphs against the baseline, force an offline restore of every project,
then run all four test suites (including actual local Windows smoke), document validation and
diff checks. Use the existing task record as the execution ledger and work directly on main
under CONTRIBUTING. This configuration relocation needs no new behavior tests or independent
review under the project policy; primary integrated review applies. T-07 remains unselected.
Relocation, pre-restore version inspection, evaluated-property comparison and all ten offline
restores pass. A stale ProviderConsole assets file omitted an existing transitive package;
fresh offline restore of the original project files reproduces the current package graph.
Exact next action: finish the four suites and primary integrated acceptance review.

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
T-01 and T-02 are now done at code reference `2ddcc7f`. A separate, fresh primary Codex session
performed the owner-requested independent review of `cfb9ceb..2ddcc7f`, with no implementation
participation, source changes or subagents: PASS, no actionable findings. This resolves the
earlier unavailable-review blocker; its historical evidence remains in verification.md.
T-08 and T-09 were already complete. AIU-028 as a whole remains incomplete.

The owner selected T-03 on 2026-09-22 and prohibited subagents. Base: `11902a7` on `main`.
Plan: migrate Codex session state/failure types and consumers to the existing shared port,
transfer browser-launch classification from the removed adapter, preserve off-dispatcher
execution and lifecycle behavior, then run Infrastructure/Presentation Release suites,
ProviderConsole Release and Windows composition builds, document validation and diff review.
The new shared-port/cache-cancellation case and both browser-launch failure cases failed against
the original implementation as expected (18 Codex session cases, 3 failed).

Ruling: retain `CodexException` and its thin transport exception wrapper using the shared
`ProviderFailureKind`. Its allowlisted OAuth error metadata is consumed by ProviderConsole;
removing it would change protocol reporting outside T-03. No enum conversion remains.
Ruling: use this task document as the execution record, work directly on main under
CONTRIBUTING, and perform primary review without subagents as the owner requested.

T-03 checks: targeted Codex session tests 18/18 PASS; Infrastructure 257/257 PASS;
Presentation 129/129 PASS; ProviderConsole Release and unpackaged Windows Debug builds PASS
with zero warnings/errors; document validation and diff check PASS. Primary integrated review
found no actionable issues; no independent review or interactive/live run is claimed.

T-03 is done at `13d45a7`, committed and pushed to main. T-01, T-02, T-03, T-08 and T-09
are complete; AIU-028 as a whole remains incomplete.

T-04 selected for this session with no subagents; base `56d6315` on main. Plan: record the
diagnostic lifecycle, prove internal-failure classification and redaction with failing tests,
implement the bounded sink and desktop wiring, then run Infrastructure/Presentation suites,
Windows build/smoke, document validation and primary integrated review. Use this task record
as the execution ledger; no new branch or agent workspace is needed under CONTRIBUTING.

T-04 implementation is saved in `9bbf13e`. Classification/redaction tests failed before the
fix; the reauthentication-priority case also failed before its correction. Infrastructure
261/261, final Presentation 136/136, Windows Debug build and actual product UI smoke 7/7 PASS.
Unsigned MSIX 2026.9.2201.0 built successfully with one tooling warning about the missing optional
symbols-package utility. Primary integrated acceptance/diff review PASS; no actionable findings.
Production bytes are unchanged after that build; the final tests additionally confirm existing
provider-failure classifications remain recoverable and do not emit internal-error diagnostics.

Ruling: typed provider failures already reach the Core port as `ProviderSessionState.Failure`
through the concrete sessions' typed catches. Preserve that path and test it; do not introduce
an Infrastructure exception dependency into presentation. No provider protocol changes are made.

The implementation session could not supply independent review; that historical NOT_RUN result
is retained in verification.md. A separate fresh primary session on 2026-09-22 completed the
owner-requested read-only review of the full `56d6315..55bb169` range without subagents:
PASS, no actionable findings. Its verdict was recorded before closure edits. No production or
test fixes were needed. Fresh Infrastructure 261/261 and Presentation 136/136 checks passed.
Existing Windows build, product smoke 7/7 and unsigned MSIX evidence remain applicable because
production code is unchanged; the retained smoke results and package hash were checked again.

T-04 is done after AC-04/AC-05 acceptance and required independent review. AIU-028 remains
incomplete. No blocker remains for T-04; other pending tasks remain unselected. Exact next
action: obtain the owner's selection of a remaining AIU-028 task before starting further work.

Earlier T-01/T-02 review checks: Infrastructure 254/254 and Presentation 129/129 PASS on that code, including
the disconnect cancellation correction. Earlier Windows Debug unpackaged and ProviderConsole
Release builds PASS with zero warnings/errors; original-code writer/current-code reader
compatibility PASS for all four providers plus the cache. Document validation and diff check PASS.
Live provider and interactive/package checks NOT_RUN.

### T-05 closure (2026-09-22)

The owner selected T-05. Base `d90e90d`; implementation and tests `fc402dd`, committed and
pushed to `main`. AC-06 and routine primary integrated review pass. No Core/Infrastructure,
credential format, quota semantics, dependency version or close-to-tray changes were made.
Presentation 142/142, Infrastructure 261/261, Windows Debug, product/demo smoke 7/7 each,
unsigned MSIX 2026.9.2202.0, document validation and diff check pass. Live-provider and packaged
installation checks were not run. No blockers or worker artifacts remain.

T-05 is done. AIU-028 remains incomplete: T-06, T-07, T-10, T-11 and T-12 remain pending and
unselected. Exact next action: obtain the owner's selection of a remaining task before starting
further implementation.
