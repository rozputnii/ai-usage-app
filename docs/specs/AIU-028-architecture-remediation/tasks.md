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
- status: done
- depends_on: []
- acceptance: AC-07
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; code 97f0ef9; offline restore 10/10, unchanged package versions and evaluated properties, four suites 78/261/142/7 PASS; Windows, routing and console builds, document validation, diff check and primary integrated review PASS

- [x] Add `Directory.Build.props` with the shared language and warning properties; remove the
      per-project copies.
- [x] Add `Directory.Packages.props` with `ManagePackageVersionsCentrally` pinning every package
      at its **current** version; strip `Version` attributes from `PackageReference` items.
- [x] Confirm no version string changed, by diff inspection, before running anything.
- [x] Check: full offline restore from the local package cache, then all four suites and
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
All four suites and primary integrated acceptance review pass; AC-07 is complete. Detailed
commands, observed results and limits are recorded in verification.md.

### T-07 - Explicit analyzer level and code-style enforcement
- status: done
- depends_on: [T-06]
- acceptance: AC-08
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; code c9cdd6a; Recommended/10.0 and build style enforcement verified across ten projects, desktop build zero warnings/errors, four suites 80/261/142/7 PASS; analyzer negative/positive probe, document validation and primary integrated review PASS

- [x] Set `AnalysisMode` to `Recommended`, pin `AnalysisLevel` so an SDK upgrade is deliberate,
      and set `EnforceCodeStyleInBuild` in the shared props.
- [x] Move the intended C# style rules into `.editorconfig`, which today carries whitespace only.
- [x] Triage the resulting diagnostics within an agreed time box. Where a rule is not adopted,
      record an explicit severity with a reason rather than lowering the level silently.
- [x] If the triage exceeds the box, stop and report; do not weaken the level to obtain a green
      build.
- [x] Check: `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug
      -p:Platform=x64 -p:WindowsPackageType=None --no-restore` with zero warnings, plus all four
      suites.

Implementation plan (2026-09-22, base `0cd297b`): keep `Recommended`, pin analysis to
the current SDK's `10.0` rule set, and enable build-time C# style checks in the shared
props. Inventory diagnostics across all ten projects before choosing documented,
rule-specific severities; enforce the existing file-scoped namespace, using placement,
qualification and intrinsic-type conventions. Verify a warnings-visible desktop build,
all four suites, the other project builds, document validation and primary diff review.
Use this task record as the execution ledger and work directly on main under CONTRIBUTING.
Ruling: the plan supplies no numeric time box; use a 30-minute triage limit for this
bounded policy task, stopping with remaining diagnostics recorded if it is exceeded.
This is the primary's execution budget, not a claimed owner-approved duration. No new
runtime behavior tests are needed for configuration; existing suites guard behavior.
Only T-07 is selected, and routine primary review applies under repository policy.
Implementation `c9cdd6a` is committed and pushed to main. Triage completed within the limit;
Recommended remains enabled. Rule-specific suggestions and their reasons are recorded in
EditorConfig and verification.md, including deferred sparkline cancellation-source disposal.
Two new validator path cases failed before the ordinal-comparison fix and now pass.
All four suites, final desktop/routing/console builds, effective-property checks, the
negative/positive analyzer probe and primary integrated review pass. AC-08 is complete.
No remaining action for T-07; T-10, T-11 and T-12 remain pending and unselected.

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
- status: done
- depends_on: [T-05]
- acceptance: AC-12
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; production 829d38b; Presentation 147/147, Infrastructure 261/261, clock/tray observations and targeted restore smoke PASS, product/demo Windows smoke 7/7 each, unsigned MSIX 2026.9.2203.0 and primary integrated review PASS

- [x] Stop the `LiveClock` tick while the main window is hidden to the tray, using the existing
      `WindowVisible` signal, and re-apply once on restore.
- [x] Confirm tray quota text stays correct while hidden, since the tray view model is also a
      `SnapshotViewModel`.
- [x] Check: Presentation test over the visibility gate, **plus** interactive Windows smoke per
      [the verification policy](../../workflow/verification.md). Compilation and unit tests are
      not sufficient evidence for this task.

Implementation plan (2026-09-22, base `bf811fb`): stop the live presentation timer using
`WindowVisible`, invalidate queued ticks across hide/restore/disposal, and reapply once on
restore. Keep snapshot delivery active; refresh tray relative text on popup open and with a
popup-owned timer while it is visible. Verify deterministic visibility/restore/tray regressions,
Infrastructure and Presentation suites, unpackaged Windows smoke including clock observations,
unsigned MSIX, document validation and primary integrated review. Work on main without
subagents as requested. No credential or provider-contract change is in scope.
Four visibility/dispatch/relative-text regressions failed before the gate implementation.
The tray refresh test also failed before its fix; a late retired-timer callback regression
then exposed a duplicate restore notification and now passes. Presentation 147/147,
Infrastructure 261/261 and unpackaged Windows Debug build (zero warnings/errors) pass.
Actual Windows observations prove no main-clock events across 65 seconds hidden, popup-only
relative-text updates while still hidden, and one immediate restore event. The targeted
restore screen/exit check and final ordinary product/demo smoke (7/7 each) pass. Unsigned
MSIX 2026.9.2203.0 and final uninstrumented Debug build pass; the local observer is absent
from both final assemblies. Primary integrated review passes with no actionable findings.
AC-12 and T-10 are complete. No remaining action for T-10; T-11 and T-12 remain pending
and unselected. AIU-028 remains incomplete.

### T-11 - Close CR-AIU-003-01: hardening at the library boundary
- status: done
- depends_on: [T-01, T-03]
- acceptance: AC-01
- evidence: docs/specs/AIU-028-architecture-remediation/verification.md; Infrastructure 271/271, Presentation 147/147, console Release and Windows Debug builds, document validation and primary review PASS

- [x] Make the eight auth and quota clients `internal`, widening `InternalsVisibleTo` for
      `tools/AiUsage.ProviderConsole` as already done for the test assembly, or construct the
      hardened primary handler inside the clients.
- [x] Reduce the remaining Infrastructure public surface to what `AiUsage.Windows` and the
      console actually consume.
- [x] Update the CR-AIU-003-01 row in [the backlog](../../backlog.md) to resolved, with the
      evidence reference.
- [x] Check: Infrastructure Release suite and a Release build of `tools/AiUsage.ProviderConsole`.

Relevance check and implementation plan (2026-09-22, base `72cc323`): all eight raw
HTTP clients remain public, so F-14 and CR-AIU-003-01 still apply. Internalize provider
implementation types and integration-only registration; keep the four public sessions with
internal constructors and DI factories, the public product-registration extensions, and the
two persistence services consumed by Windows. Grant friend access only to ProviderConsole,
in addition to the existing Infrastructure tests. Preserve all protocol, handler, storage and
session behavior. Verify the public construction boundary, eight hardened handler pipelines,
session resolution, Infrastructure/Presentation Release suites, console/Windows builds,
document validation and primary integrated review. The boundary regression failed against
the original code as expected; existing registration checks passed.

Ruling: use this task document as the execution ledger, work on main and do not dispatch
any subagents, following the owner's explicit instructions. This is an accessibility-only
reduction inside the existing Infrastructure boundary; no authentication protocol, credential
lifecycle, durable-data or privilege behavior changes. Primary review applies under CONTRIBUTING;
no independent review or live-provider evidence will be claimed. T-12 is assessed only after
T-11 is completed, using its explicit drop condition.

### T-12 - Transport options
- status: in-progress
- depends_on: [T-01]
- acceptance: AC-01
- evidence: not-run

- [ ] Introduce `ProviderTransportOptions` for the request deadline, pooled-connection lifetime
      and retry-after fallback, bound once in the DI extensions, keeping today's values exactly.
- [ ] Check: Infrastructure Release suite. Lowest priority; drop it if T-01 already leaves a
      single call site per value.

Relevance check and implementation plan (2026-09-22, base `9f4d347`, after T-11 was
completed and pushed): the request deadline and pooling lifetime have one owner each,
but the one-minute retry-after fallback remains repeated in Claude, Copilot and Antigravity.
The explicit drop condition is therefore false. Bind one immutable-after-initialization
ProviderTransportOptions instance with TryAddSingleton in all four registrations. Pass it
through all eight clients and the shared send path; use it for handler pooling and each
quota throttle. Preserve defaults of 15 seconds, 5 minutes and 1 minute, explicit Retry-After
precedence, cancellation classification, body-read deadline and all nontransport timers.
Direct internal test/console construction falls back to the same default instance.

Verify real DI pooling, configured deadlines through all eight clients, fallback expiry at
the exact boundary for three providers and explicit header precedence. The new 15-case suite
produced 12 expected failures before wiring: pooling, all eight deadlines and three fallbacks;
the three explicit-header cases already passed. Infrastructure now passes 286/286.
Finish Presentation Release, consumer builds, document validation and primary integrated
review; preserve the primary-only review scope from T-11. No new public configuration API,
dependency, provider protocol, credential lifecycle or UI behavior is introduced.

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

### T-06 closure (2026-09-22)

Base `34ee90b`; configuration implementation `97f0ef9`, committed and pushed to `main`.
Shared properties now apply from one root to all ten projects, with eighteen package references
using ten unchanged central versions. Offline restore 10/10; validator 78/78, Infrastructure
261/261, Presentation 142/142 and actual product Windows smoke 7/7 pass. Windows and routing
Debug unpackaged builds and ProviderConsole Release build pass with zero warnings/errors.
Document validation, version/property/package-graph comparisons, diff check and primary
integrated review pass. No blockers or worker artifacts remain.

T-06 and AC-07 are complete. AIU-028 remains incomplete; T-07, T-10, T-11 and T-12 remain
pending and unselected. Exact next action: obtain the owner's selection of a remaining task
before starting further implementation.
