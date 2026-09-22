# AIU-028 verification

Date: 2026-09-20. This record holds the Phase 1 evidence from the analysis-only audit session
that produced [spec.md](spec.md), plus NOT_RUN placeholders for the remediation checks. It is an
evidence record, not a status mirror. The session that wrote it started no task in
[tasks.md](tasks.md) and changed no file outside `docs/`; remediation evidence is appended per
task as tasks are executed, starting with [T-08 and T-09](#t-08-and-t-09---2026-09-20).

## Environment and base

Windows 11 Pro 10.0.26200, x64. .NET SDK 10.0.401, which is the SDK selected by
[global.json](../../../global.json) (`rollForward: latestPatch`, `allowPrerelease: false`);
`dotnet --version` reported `10.0.401` and `dotnet --list-sdks` showed that as the only installed
SDK, so no roll-forward occurred.

The session ran in a Git worktree at `.claude/worktrees/architecture-audit-plan-2be087`. It began
at `1464405`, and `main` advanced to `6681b7a` (AIU-009 closure) during the session. `main` was
merged in at `c2b6dd4` before the evidence below was captured, and every `path:line` citation in
the specification was then re-checked against that tree. Four citations had drifted in files the
AIU-009 commits touched and were corrected: `AntigravityHttp.cs` 45 to 64,
`AntigravityQuotaClient.cs` 197 to 196, and `CopilotException.cs` / `AntigravityException.cs` 7
to 6. All 93 file-and-line code citations in the specification were confirmed to resolve to the
construct they are cited for on `c2b6dd4`.

## Restore

The worktree had no restored NuGet assets, so the first run of every `--no-restore` command
failed with `NETSDK1004` (missing `project.assets.json`). Rather than report that as BLOCKED, the
projects were restored **offline** against the existing local package cache: each `dotnet restore`
was passed a `--configfile` pointing at a NuGet configuration whose `<packageSources>` contains
only `<clear />`, so no remote source was reachable and no package could be downloaded. Every
project restored from the existing cache in under a second. No network restoration was enabled,
no package version changed, and no NuGet configuration was added to the repository — the file
lives in the session scratchpad only.

## Executed local checks

Run from the repository root against `c2b6dd4`, 2026-09-20 17:14:44Z to 17:15:39Z for the
deterministic suites and 17:15 to 17:16Z for the desktop build. Verdicts are read from observed
command output and exit codes.

| Check | Command | Verdict | Observation |
| --- | --- | --- | --- |
| Project validator regressions | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS | 78 tests; 0 errors, 0 failed, 0 skipped, 0 not run; exit 0 |
| Canonical document validation | `dotnet run --project tools/AiUsage.ProjectValidation --root . --json` | PASS on re-run | See below: the first run correctly rejected the documents this session was writing |
| Infrastructure Release regressions | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 226 tests; 0 errors, 0 failed, 0 skipped, 0 not run; exit 0 |
| Presentation Release regressions | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 125 tests; 0 errors, 0 failed, 0 skipped, 0 not run; exit 0 |
| Diff whitespace check | `git diff --check` | PASS | No output, exit 0 |
| Warnings-visible desktop build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS | Build succeeded, **0 Warning(s), 0 Error(s)**, 47.06s; Core, Infrastructure and Windows assemblies emitted |

At the earlier base `1464405`, before the merge, the same four suites also passed, with the
Infrastructure suite at 215 tests. The count rose to 226 because the merge brought AIU-009's new
Antigravity tests, not because anything in this session changed a test.

### Document validation, both runs

The first run of the validator against the written documents returned `valid: false` with four
diagnostics, all caused by these new documents. That is recorded rather than hidden, because it
is the check doing its job:

- `docs/product/goals.md` / `G-003` / `GOAL_SCOPE`: "Goal scope omits one of its backlog items."
  AIU-028 declares `goal: G-003` but was not yet listed in the G-003 scope.
- `docs/specs/AIU-028-architecture-remediation/spec.md` / `BROKEN_LINK`, twice, and
  `docs/specs/AIU-028-architecture-remediation/tasks.md` / `BROKEN_LINK`, once. All three were
  links to this file, which had not been written at the time of the run.

Both causes were corrected — AIU-028 added to the G-003 scope, and this file written — and the
validator re-run. The re-run result is recorded in the table above.

### Warnings as audit input

The warnings-visible build produced zero warnings. That is reported as a fact about the build,
not as a conclusion about the code: finding F-09 in [spec.md](spec.md) records that no project
sets `AnalysisMode`, `AnalysisLevel` or `EnforceCodeStyleInBuild`, so the build ran with the SDK
default analysis mode, in which only a small number of rules are enabled as warnings, and with
code-style analysis disabled on build. A clean result at that level is not evidence that a
`Recommended` or `All` level would also be clean. Task T-07 exists to find out.

## Not run in this session

Each of the following is NOT_RUN by the design of an analysis-only session, and none may be read
as passing. The audit plan states this explicitly and the reason is recorded for each.

| Check | Status | Reason |
| --- | --- | --- |
| Interactive Windows UI smoke | NOT_RUN | An analysis session performs no interactive run. Required before AC-12 (F-13, the tray-hidden clock) can be accepted; compilation is not sufficient evidence for that task. |
| Packaged MSIX build and packaged activation | NOT_RUN | Packaging, signing and release actions were outside the session's authority. |
| Disposable-guest lifecycle verification | NOT_RUN | Requires a package and a guest; neither was built or provisioned. |
| Live provider connect, refresh, resume, disconnect | NOT_RUN | No live provider call, credential-store read or host trust change was authorized or attempted. |
| `tests/windows/AiUsage.Windows.Tests` (FlaUI UIA3) | NOT_RUN | Needs an unlocked interactive desktop and a published smoke executable; neither was prepared. |
| `tools/AiUsage.ProviderConsole` Release build | NOT_RUN | Not selected by the change-based verification matrix for a documentation change. It becomes required for T-03 and T-11, which both touch it. |
| `dotnet format --verify-no-changes` | NOT_RUN | Optional read-only inspection; skipped. Formatting remains local-only under the 2026-09-13 owner amendment recorded as CR-AIU-001-01. |
| Independent review | NOT_RUN | Not required for a documentation-only change under CONTRIBUTING.md. T-02 and T-04 will require focused independent review, because they change credential storage and the diagnostics boundary. |

## Security and data lifecycle

The credential and storage findings (F-02, F-14) and the diagnostics finding (F-05) were prepared
using the security-lifecycle skill and read against
[security and lifecycle](../../platforms/windows/security-and-lifecycle.md). The session read
source files only. No credential store was opened, no source CLI credential was read, no
provider was contacted, no host trust was changed and no stored data was written or removed. No
token, opaque provider identifier or user data appears in these documents; the two provider
constants quoted in the findings are endpoint and entropy strings already present in the
repository, not secrets.

## Remediation checks - placeholders

These are the checks each task must produce. Completed checks are recorded below; the T-01/T-02
regressions, compatibility checks and independent review passed as recorded at the end.
Unselected tasks remain NOT_RUN.

| Task | Acceptance | Required check | Status |
| --- | --- | --- | --- |
| T-01 | AC-01, AC-02 | Infrastructure Release suite, no reduction in test count | PASS regressions/compatibility and independent review; final-code Infrastructure 254/254 |
| T-02 | AC-02 | Infrastructure Release suite; new per-provider reparse-point refusal test; Codex record forward-compatibility test; focused independent review | PASS regressions/compatibility and independent review; final-code Infrastructure 254/254 |
| T-03 | AC-03 | Infrastructure and Presentation Release suites; `tools/AiUsage.ProviderConsole` Release build | PASS, see T-03 closure below |
| T-04 | AC-04, AC-05 | Presentation Release suite with a non-provider exception test; redaction test over nested unknown fields, a token-shaped value and an opaque provider identifier | NOT_RUN |
| T-05 | AC-06 | Presentation Release suite including `DependencyBoundaryTests`; synthetic fifth-descriptor test | NOT_RUN |
| T-06 | AC-07 | Full offline restore; all four suites; `git diff --check`; diff inspection confirming no version string changed | NOT_RUN |
| T-07 | AC-08 | Warnings-visible desktop build at the raised analysis level with zero warnings; all four suites | NOT_RUN |
| T-08 | AC-09, AC-10 | Core disposal test (outstanding work, double dispose); Presentation re-entrant subscriber test | PASS, see [T-08 and T-09](#t-08-and-t-09---2026-09-20) |
| T-09 | AC-11 | Presentation Release suite; preference round-trip including unknown members | PASS, see [T-08 and T-09](#t-08-and-t-09---2026-09-20) |
| T-10 | AC-12 | Presentation visibility-gate test **and** interactive Windows smoke | NOT_RUN |
| T-11 | AC-01 | Infrastructure Release suite; `tools/AiUsage.ProviderConsole` Release build | NOT_RUN |
| T-12 | AC-01 | Infrastructure Release suite | NOT_RUN |

## T-08 and T-09 - 2026-09-20

A second session implemented T-08 (F-10, F-11) and T-09 (F-12) and nothing else. It ran on `main`
at base `5c415d6`, in the repository working tree rather than a worktree, on the same machine and
SDK as the Phase 1 record above. NuGet assets were already restored, so no restore was needed and
no NuGet configuration was added; no dependency, analyzer or SDK version changed.

### Changes

- `src/windows/AiUsage.Core/Dashboard/DashboardWorkflow.cs`: `Dispose` no longer throws when work
  is outstanding. It is idempotent behind a `disposed` flag, cancels the lifetime outside the lock
  and then releases it. "Await `StopAsync` first" stays a documented precondition on `Dispose`; it
  is not a debug assertion, because a `Debug.Assert` would still break the shutdown path the
  finding is about. `RunAsync` now reads `lifetime.Token` under the lock and passes it to
  `ExecuteAsync`, so an operation that starts after disposal observes cancellation rather than an
  `ObjectDisposedException`, and `DrainAsync` tolerates an already-released lifetime.
- `src/windows/AiUsage.Windows/Adapters/Live/LiveUsageSource.cs`: `Publish` assigns the snapshot
  and its revision under `sync` and returns the subscriber array to invoke; each caller delivers
  after leaving the lock. No subscriber is called while the lock is held.
- `src/windows/AiUsage.Windows/Adapters/Live/LivePreferenceStore.cs`: both call sites use the new
  source-generated `PreferenceStateJson` context. `[JsonExtensionData]` is kept and
  `UnmappedMemberHandling.Disallow` was **not** added. The `State` members changed from `init` to
  `set` because the source generator turns init-only members into constructor parameters: extension
  data cannot bind to one, and members absent from a file would have arrived as `null` instead of
  their declared defaults. With settable members the generator emits a parameterless
  `ObjectCreator`, which preserves the previous reflection-based behavior. This was observed, not
  assumed: the init-only version failed three existing preference tests with
  `ExtensionDataCannotBindToCtorParam`.

### Executed local checks

Run from the repository root, 2026-09-20, against the working tree described above. Verdicts are
read from observed command output and exit codes.

| Check | Command | Verdict | Observation |
| --- | --- | --- | --- |
| Project validator regressions | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS | 78 tests; 0 errors, 0 failed, 0 skipped, 0 not run |
| Canonical document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS | `{"valid":true,"diagnostics":[]}`; exit 0 |
| Infrastructure Release regressions | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 226 tests; 0 errors, 0 failed, 0 skipped, 0 not run; unchanged from the Phase 1 count |
| Presentation Release regressions | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 129 tests; 0 errors, 0 failed, 0 skipped, 0 not run; 125 before, plus the four new tests |
| Warnings-visible desktop build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS | Build succeeded, 0 Warning(s), 0 Error(s), 36.72s |
| Diff whitespace check | `git diff --check` | PASS | No output, exit 0 |

### New tests and what they would have caught

Four tests were added. Three of them were run against the pre-change code to confirm they fail for
the reason claimed, rather than passing vacuously:

| Test | File | Covers | Observed against pre-change code |
| --- | --- | --- | --- |
| `DisposeWithOutstandingWorkCancelsItInsteadOfThrowing` | `tests/windows/AiUsage.Presentation.Tests/DashboardWorkflowTests.cs` | AC-09, F-10 | FAIL: `InvalidOperationException : Await StopAsync before disposing dashboard work` from `Dispose` |
| `DisposingTwiceIsIdempotentAfterDrain` | `tests/windows/AiUsage.Presentation.Tests/DashboardWorkflowTests.cs` | AC-09 | Not re-run against the old code; the old `Dispose` already tolerated a second call after a drain, so this test defends the new guard rather than reproducing a past failure |
| `ReentrantSubscriberIsInvokedWithoutTheSourceLockAndSeesIncreasingRevisions` | `tests/windows/AiUsage.Presentation.Tests/LiveAdapterTests.cs` | AC-10, F-11 | FAIL: a non-publishing thread could not enter the source for 10s while a subscriber ran |
| `PreferenceFileWithUnknownMembersRoundTripsUnchanged` | `tests/windows/AiUsage.Presentation.Tests/LiveAdapterTests.cs` | AC-11, F-12 | Round-trips a file with two unknown members and a nested unknown object; both survive a write, and the known values reload correctly |

### Not run for these two tasks

| Check | Status | Reason |
| --- | --- | --- |
| Interactive Windows UI smoke | NOT_RUN | No interactive run was performed in this session. Neither AC-09, AC-10 nor AC-11 requires it: the changes are a disposal contract, a lock boundary and a serializer binding, all covered by the deterministic suites. It remains required for AC-12 (T-10) |
| `tests/windows/AiUsage.Windows.Tests` (FlaUI UIA3) | NOT_RUN | Needs an unlocked interactive desktop and a published smoke executable; neither was prepared |
| Packaged MSIX build, live provider calls, credential-store reads | NOT_RUN | Outside this session's authority and not selected by the change-based matrix for these tasks |
| Independent review | NOT_RUN | Not required under CONTRIBUTING.md: neither task changes credential storage, destructive data handling or a privilege boundary |
| `dotnet format --verify-no-changes` | NOT_RUN | Optional read-only inspection; formatting remains local-only under CR-AIU-001-01 |

No other task in [tasks.md](tasks.md) was started, and no provider, transport, exception or
state-lease file was touched: that work belongs to T-01.

## Limitations

The original audit established findings, not fixes. Later remediation evidence is recorded in
the task-specific sections; the owner selected T-01 and T-02 on 2026-09-22. Remaining pending
tasks require separate selection under CONTRIBUTING.md.

Severity and cost in [spec.md](spec.md) are the auditor's judgement from static reading, not
measured. Three findings say so explicitly rather than implying evidence that does not exist:
F-03 and F-14 are latent, with no currently reachable failure; F-11 is structural, with no
observed misbehaviour in the present subscribers; and F-13's battery and CPU claim follows from
the code path but was not profiled, which is one reason T-10 requires interactive evidence.

## T-01 / T-02 work in progress - 2026-09-22

Owner-selected scope: T-01 and T-02 only, base `cfb9ceb`. The security-lifecycle skill and
Windows lifecycle policy were read before implementation; the design records the boundaries.
T-01 extracts the common state lease, transport/status translator and handler policy, replaces
the three duplicate exceptions and Claude's duplicate failure enum, and retains a temporary
explicit Codex transport adapter until T-03. Provider URLs, flow, quota parsing, transport logger
suppression, protected record shapes and entropy remain unchanged. No dependency versions change.

Observed: pre-change Infrastructure 226/226 PASS; post-extraction Infrastructure 226/226 PASS;
Presentation 129/129 PASS; ProviderConsole Release build PASS (zero warnings/errors).
The new compatibility test initially used a nonnumeric synthetic Copilot identity and correctly
failed validation; corrected to a numeric synthetic identity. Final expanded checks and focused
independent review are pending. This checkpoint is not task completion.

### T-02 candidate for focused review

Codex now holds the shared exclusive lease over the entire read/renew/persist operation; a
storage failure invalidates its in-memory credentials. Its original committed payload and
entropy are preserved. A DPAPI-protected pending journal holds the predecessor's content identity
and the successor ciphertext. Recovery handles interruption before and after promotion, refuses
torn/unrelated/legacy unjournaled records, and keeps evidence until explicit disconnect. The
truncated SHA-256 content identity stays inside encrypted state and is not an authentication
primitive. It does not promise server-side rotation rollback or eliminate same-user filesystem
check/use races. The cache retains its JSON format and now uses checked paths, exclusive access,
asynchronous I/O and flushed writes. Synchronous compatibility entry points remain until T-03;
product operations run off the UI dispatcher. Existing recovery presentation is reused.

Observed candidate checks: Infrastructure 252/252 PASS, Presentation 129/129 PASS, Windows Debug
unpackaged build PASS with zero warnings/errors. One added recovery test initially used an invalid
empty quota response and failed; using a valid synthetic quota response made it pass. Old tests
expecting silent absence for corrupt Codex data now assert recovery and no overwrite, as required
by the hardened lifecycle. Old single-file assertions now explicitly permit the empty lock file.
A final targeted regression additionally checks failed deletion retains a pending successor.
Focused independent review, final check recording and task closure remain pending.

### Additional observed acceptance evidence - 2026-09-22

- Infrastructure Release at `5462057`: PASS, 253 tests, 0 failed/errors/skipped/not-run.
  Includes 27 additional cases over the 226-test baseline: three frozen record-shape cases,
  twenty storage safety/recovery cases and four session boundary cases. The failed-disconnect
  case proves a pending successor survives when Windows refuses to delete the predecessor.
- Presentation Release: PASS, 129 tests, 0 failed/errors/skipped/not-run. Existing recovery
  rendering is reused; no view, activation, clock or tray code changed.
- ProviderConsole Release at `5462057`: PASS, zero warnings/errors.
- Canonical document validation: PASS, valid=true and diagnostics=[] at the candidate checkpoint.
- `git diff --check`: PASS.

A separate compatibility harness was built from the actual pre-change source archived by
`git archive cfb9ceb src/windows/AiUsage.Core src/windows/AiUsage.Infrastructure`. It restored
only from the local package cache using a configuration with all package sources cleared.
The original Claude/Copilot/Antigravity state stores and original Codex grant/cache writers
created synthetic records in an isolated temporary directory. A second harness referencing
candidate Infrastructure read and rewrote all four protected records and the quota cache,
checking their synthetic identities, grants and cached quota values: PASS, exit 0. This is
observed cross-version code execution, in addition to the committed frozen-shape regression
cases. Harnesses and encrypted synthetic output remain outside Git under
`%TEMP%/aiu028-compat-cfb9ceb`; no actual user credential store was read.

Live-provider requests, real credential reads, packaged install/update/recovery and interactive
Windows smoke: NOT_RUN. They are not established by these deterministic persistence checks.
The selected tasks change no Windows UI/lifetime behavior and require the relevant regression,
build, compatibility and focused-review evidence, not T-10's interactive clock acceptance.

The final warnings-visible unpackaged Windows Debug build at `5462057` also passed, with
zero warnings/errors (30.41 seconds). Primary diff/acceptance inspection confirmed one shared
transport/status map and handler configuration, unchanged provider-specific requests/parsers,
and all eight `RemoveAllLoggers()` registrations retained. Independent review is still pending.

### Primary review follow-up: cancellation after disconnect commit

Primary inspection found a new cancellation window between successful grant deletion and cache
cleanup: a cancelled cleanup could leave live credentials in memory after `stored` was cleared.
The new `CancellationAfterGrantDeletionFinishesDisconnectAndCannotReuseLiveCredentials` case
was run before the correction and failed with TaskCanceledException at that boundary (1 test,
1 failed). Disconnect now clears in-memory credentials immediately after durable grant deletion
and completes secondary cache cleanup with CancellationToken.None. The targeted CodexSessionTests
class then passed 15/15, including no further provider traffic after the cancelled disconnect.
The quota-cache internal constructor supplies a synthetic cancellation hook; the public
constructor and normal runtime behavior have no injected callback. This is a T-02 correction,
not a new feature or authentication flow. Independent review was notified of the finding.

### Earlier focused independent review attempts - BLOCKED (superseded below)

CONTRIBUTING.md requires focused independent review for material credential changes. The
convergence-review skill was read and applied; it instructs: "Report unavailable required review
honestly." Two fresh read-only Codex review agents were requested with GPT-5.6 Luna and reasoning
max, as required by AGENTS.md. The first received frozen `5462057` against `cfb9ceb`, then the
bounded cancellation correction `2ddcc7f`; it returned no progress, findings or verdict despite
status requests and a resumed request for its accumulated result. It was interrupted after about
20 minutes. A replacement received final code `2ddcc7f` against `cfb9ceb`, the same evidence and
a focused storage-only boundary, with an approximately ten-minute limit; it also returned no
progress or verdict before interruption. Tool acceptance established dispatch, not a completed
review. This record does not infer that the model itself is unavailable, only that no review
result was obtainable in these attempts. No substitute model was used.

Verdict: BLOCKED, not PASS or FAIL. No independent findings were delivered. Primary inspection,
regressions and cross-version compatibility checks are successful but do not replace independent
review. T-01 and T-02 remain blocked rather than done, and AIU-028 remains incomplete. The code
and evidence are committed and pushed under the standing save-point policy; publication does not
claim completion. Exact next action is a focused read-only review of `cfb9ceb..2ddcc7f`, followed
by targeted fixes/checks if needed and actual review evidence before task closure.

### Independent review and T-01 / T-02 closure - 2026-09-22

Verdict: **PASS**, no actionable findings in the selected diff. The earlier BLOCKED result
describes unavailable review attempts, not a code defect, and is superseded by this completed
review. T-01 and T-02 are done; this does not close the remaining AIU-028 tasks.

Reviewer independence: the owner requested a new independent review and prohibited subagents.
This separate primary Codex session did not implement the changes and had no implementation
conversation transcript. It applied convergence-review and security-lifecycle, inspected the
frozen source diff and relevant repository specifications/evidence, and made no production or
test source changes. No subagent, delegated task or external browser reviewer was used. After
completing the read-only code review, the primary updated only the canonical closure records.

Frozen base: `cfb9ceb6324051823d42dbd36c15d9136f853ec9`.
Frozen candidate: `2ddcc7fc8be25cf7e7003a190a07987a30eb43dc`.
Checkout at review start: `4ff6a69f5f9e85b19245794519f5f76643cff3ab`, clean `main`.
`git diff --exit-code 2ddcc7f HEAD -- src tests tools` returned 0, establishing that the
locally executed source/tests match the frozen candidate. Review completed on local Windows,
2026-09-22 (Europe/Lisbon), with the repository-pinned SDK and existing restored packages.

| Boundary | Reviewed evidence and result |
| --- | --- |
| AC-01 transport and exceptions | `ProviderTransport.SendAsync`, `Failure`, `ConfigureClient` and `CreateHandler` own the shared translation and handler policy. The retained `CodexHttp.Translate` adapts only the legacy enum. All eight `RemoveAllLoggers()` registrations remain; timeout, redirects, cookies, pooled lifetime, status codes and retry-after retain their prior meaning. PASS. |
| Provider behavior and scope | Auth/quota clients and parsers were compared with the base; the changed files are identical after substituting shared exception/transport names and imports. The corresponding changed protocol/parser/auth/store regression files also retain their assertions after those substitutions. No endpoint, scope, parser, dependency or Windows UI/lifetime change was introduced. PASS. |
| AC-02 protected records | `ProviderStatePolicy`, each provider's policy/validation, `CodexGrantStore.Record`/`Revision` and the unchanged serialized types retain file names, entropy, CurrentUser protection and committed shapes. Frozen-shape tests passed for all four providers. The previously observed actual original-writer/current-reader harness remains separate compatibility evidence and was not rerun in this review. PASS. |
| Exclusive ownership and paths | `ProviderStatePaths.Acquire`/`CheckDirectory`/`CheckFile` and `ProviderStateLease.CheckPaths` reject redirected roots, ancestors, lock files and owned data paths. The lease spans Codex load/renew/persist; revisions reject stale writes. Actual Windows junction tests cover all four provider roots and Codex grant/cache paths, with outside sentinels preserved. PASS. |
| Interrupted state and cleanup | `ProviderStateLease.SaveAsync`, `RecoverJournalAsync`, `PromoteJournalAsync` and `DeleteAsync` were inspected for staged, promoted, torn, unrelated and failed-delete states. The protected Codex journal retains a recoverable successor; ambiguous evidence blocks replay; deletion removes the predecessor first and touches only named owned files. The recovery and failed-deletion regressions passed. PASS. |
| Cancellation and session boundary | `CodexSession.PersistAsync` saves a returned rotating grant without request cancellation. Storage failures clear usable in-memory credentials, and a changed durable record invalidates them before provider traffic. `DisconnectAsync` clears them after durable deletion and completes cache cleanup without request cancellation. The full final-code suite includes both cancellation regressions. Existing recovery enum names map through `CodexDashboardSession.Map` to the existing presentation surface. PASS. |

Fresh observed checks (no source edits between these checks and the verdict):

| Check | Status | Observed result |
| --- | --- | --- |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 254 tests, 0 errors/failed/skipped/not-run; 5.441 seconds test execution. This is 28 above the recorded 226 baseline and includes the final disconnect correction. |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 129 tests, 0 errors/failed/skipped/not-run; 0.664 seconds test execution. |
| `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS | Closure records validated: valid=true, diagnostics=[]. |
| `git diff --check` | PASS | No whitespace errors in the closure diff. |
| New live-provider/browser-account check | NOT_RUN | Not required by T-01/T-02 acceptance or the change-based matrix; browser-account permission was available but unused. No source CLI credentials or existing account stores were read. |
| New interactive Windows or packaged lifecycle check | NOT_RUN | No UI/lifetime change in this scope; previous build results are retained above, not recast as interactive or packaged execution. |

Commands used the README-documented user-local SDK executable. Synthetic DPAPI records and
junctions were confined to the test suites' temporary directories. The review does not claim
rollback of server-side token rotation, atomicity across grant/cache/provider state, or protection
against all same-user filesystem check/use races; those are explicit existing design limits.
T-03's legacy-contract removal and other unselected remediation remain outside this closure.

## T-03 closure - 2026-09-22

Base: `11902a7`. Implementation: `13d45a7`, committed and pushed to `main`. The owner selected
T-03 and prohibited subagents. This session implemented and reviewed T-03 only; AIU-028 remains
incomplete. Commands ran on local Windows with the README-documented user-local .NET SDK and
existing restored packages. No existing account store or source CLI credentials were read.

`CodexSession` now implements `IProviderSession` directly and returns `ProviderSessionState`.
The old interface, state/status/failure types and `CodexDashboardSession` are removed. The
composition root and dashboard resolve the same concrete singleton, matching the other provider
registrations. ProviderConsole and Codex protocol code use `ProviderFailureKind` directly.
`CodexException` retains its allowlisted OAuth error metadata and distinct transport exception
identity; its wrapper forwards the shared failure kind without an enum conversion.

Browser-launch `InvalidOperationException` and `Win32Exception` classification moved from the
removed adapter into the session. Cached reads expose the asynchronous, cancellable shared port.
The existing session `Task.Run` boundary still covers lock acquisition, cache access and browser
launch; the shared state lease still performs asynchronous I/O. The synchronous cache convenience
reader remains inside that worker boundary, never on the dispatcher. No fully asynchronous cache
internals or interactive responsiveness measurement is claimed.

| Check | Status | Observed result |
| --- | --- | --- |
| New tests before implementation | PASS (expected red) | Targeted Codex session run: 18 cases, 3 failed. Direct shared-port assignment failed; both browser-launch failures escaped the original direct session. The existing 15 cases passed. |
| Targeted Codex session tests after implementation | PASS | 18/18, no errors, failures, skips or not-run cases. New cases cover the shared cache port, pre-cancelled reads preserving state, unsupported manual code, both browser-launch failure types and absence of provider traffic. |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 257/257, 0 errors/failed/skipped/not-run; 6.511 seconds test execution. Existing renewal, stale-cache, recovery, exclusive-lease and cancellation regressions remain present. |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 129/129, 0 errors/failed/skipped/not-run; 0.649 seconds test execution. |
| `dotnet build tools/AiUsage.ProviderConsole -c Release --no-restore` | PASS | Zero warnings/errors; 2.52 seconds. |
| `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS | Zero warnings/errors; 37.31 seconds. Verifies the actual Windows composition source, which the neutral Presentation suite does not compile. |
| Removed-contract and enum-bridge scan | PASS | No legacy session/failure type references in src/tests/tools and no `Enum.Parse` in Infrastructure. |
| Primary integrated acceptance/diff review | PASS | No actionable findings. Compared with the base, auth, credentials, exception metadata, quota clients/parsers, related protocol tests and ProviderConsole differ only in failure type/imports. Session changes preserve the existing persistence/rotation/cancellation branches and transfer adapter browser handling. DI resolves one Codex singleton for both consumers. |
| `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` and `git diff --check` | PASS | Document validator returned valid=true with no diagnostics; diff check passed. Rechecked after the closure documentation edits. |
| Independent review | NOT_RUN | No subagents, per owner instruction. This is primary self-review. No material credential-storage, destructive-data, privilege or authentication-boundary change was introduced, so focused independent review is not required by CONTRIBUTING for T-03. |
| Live provider, interactive Windows and package lifecycle checks | NOT_RUN | No live sign-in or UI/package execution in this task. T-03 acceptance requires deterministic suites and the console build; Windows build evidence is not recast as interactive evidence. |

No dependency versions, provider URLs/scopes, serialized formats, state-store implementation,
quota meanings or close-to-tray behavior changed. T-04 and all other pending remediation tasks
remain outside this closure.

## T-04 implementation and verification - 2026-09-22

Base: `56d6315`; production implementation: `9bbf13e`. The final evidence commit also adds
three classified-provider regression cases; it does not change production bytes. The owner
requested T-04 without subagents. Work used the local Windows desktop, the README-documented
user-local .NET SDK and existing restored packages. No source CLI credentials or existing
account state was read. Test data and package/smoke output stayed in synthetic temporary roots
and `.ai-usage-local/AIU-028/`.

Unclassified operation exceptions now produce `InternalError` and dedicated full/short resource
keys. The failure view model offers no retry/reconnect even when a preceding provider state
required reauthentication. Existing classified provider states retain their meaning. The Core
diagnostic port accepts only event/category enums; exception projection reads no payload fields.
Infrastructure owns the local 64 KiB file, seven-day pruning and strict reconstruction of retained
records. Windows wires the pre-host/post-disposal sink to startup, shutdown, disposal and live
operation catches. All eight auth/quota HTTP registrations retain `RemoveAllLoggers()`.

| Check | Status | Observed result |
| --- | --- | --- |
| Regression before fix | PASS (expected red) | Non-provider exception returned ProviderUnavailable instead of InternalError. Redaction-through-operation test recorded no event before wiring. Three sink tests failed against the no-op contract skeleton: no file/retention and unknown fields left intact. ReauthRequired presentation offered Reconnect before the priority fix. |
| Infrastructure Release suite | PASS | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`: 261/261, 0 failed/errors/skipped; 5.403 s. New tests exercise actual file output, undefined enums, nested unknown fields, token-shaped data, opaque identifier, expiry on restart, size eviction, exclusive-handle contention, invalid root and junction refusal. |
| Presentation Release suite | PASS | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`: final 136/136, 0 failed/errors/skipped; 0.626 s. Includes exception projection, non-recoverable UI, full/short resource keys and preservation of three existing provider-failure kinds without internal-error records. |
| Windows Debug unpackaged build | PASS | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore`: 0 warnings/errors; 40.16 s. Compiles real composition and all App catches. |
| Actual local product Windows smoke | PASS | `dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-build --no-restore -- -noLogo`: 7/7, 0 failed/errors/skipped; 50.885 s. Launch, navigation, themes, close/restore through tray, tray Exit, repeated Exit and unavailable capabilities all passed. Each process exited with code 0. |
| Unsigned MSIX build | PASS | VS MSBuild Release/x64 with GenerateAppxPackageOnBuild=true, signing disabled, generated manifest and no restore. Produced AiUsage.Dev 2026.9.2201.0 x64. One tooling warning: `mspdbcmf.exe` missing, so no symbols package was generated; no owned-code warnings or errors. |
| Primary integrated acceptance/diff review | PASS | Reviewed the complete implementation against AC-04/AC-05, data allowlist, retention/size, path checks, failure containment, DI lifetime and failure presentation. No actionable findings. Provider authentication, grant/cache formats, dependencies and quota semantics are unchanged. |
| Document validation and diff check | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` returns valid=true; `git diff --check` passes. Rechecked after final evidence edits. |
| Focused independent review | NOT_RUN | Required for the diagnostics boundary by CONTRIBUTING and the existing T-04 verification plan. The owner prohibited subagents; the implementation author cannot provide a fresh independent review. T-04 remains blocked on this requirement, not on a failing implementation check. |
| Live provider, packaged install/update and desktop fault injection | NOT_RUN | No sign-in, provider traffic or package installation requested. The normal desktop smoke does not prove injected startup/shutdown/disposal failures; their wiring is inspected and compiled, and sink/projection behavior is covered by deterministic tests. |

Smoke used `AIU_SMOKE_MODE=product`, `AIU_SMOKE_EXE` pointing at the current Debug executable,
`AIU_SMOKE_EVIDENCE_DIRECTORY=.ai-usage-local/AIU-028/t04-product-smoke` (absolute at runtime),
and a fresh `state` subdirectory through `AIU_DEVELOPMENT_STATE_DIRECTORY`. The actual
`launch.png` was inspected: the product empty state, disabled CLI import and expected navigation
are visible. Scenario JSON records and screenshots remain in that local evidence directory.

The package is retained under `.ai-usage-local/AIU-028/t04-package/2026.9.2201.0/`.
Its SHA-256 is `655C94335E3001A2EF1ED53690A32783B7A99CD012D4C026D42D6777D5D6C3C1`.
The package identity/version/architecture were read back from its embedded manifest. A first
inspection selected dependency packages as well as the product and failed; filtering to the
single AiUsage package corrected that inspection without rebuilding or modifying the package.

Retention runs on startup/write, not while the app is closed. Diagnostics are best effort:
storage failure or a torn write may lose records. Path checks do not promise protection against
all same-user check/use races. No generic exception/payload logging, export, network telemetry,
recursive deletion, credential migration or live-provider success is claimed. The next action
is fresh independent review of the frozen implementation and final regression tests.

### Independent review and T-04 closure - 2026-09-22

Verdict: **PASS**, no actionable findings in `56d6315..55bb169`. AC-04 and AC-05 are
satisfied. This completed review supersedes the earlier unavailable-review blocker; it does
not close AIU-028 or select another task.

Reviewer independence: this fresh primary Codex session did not author the implementation or
its tests and had no implementation conversation transcript. It used convergence-review and
security-lifecycle, reviewed the entire frozen range and relevant callers read-only, and
recorded its verdict before editing these closure documents. No subagents were used. No
production or test fixes were necessary or authored; subsequent changes are documentation only.

Frozen base: `56d6315e8fc2c44749618100bb7eb836763342d5`.
Production implementation: `9bbf13e2e47980884507eeb003a7a3c0f650c488`.
Frozen candidate and clean `main` at review start:
`55bb169fa5b0d799e2ce5a32a25a64fd821bb550`.
Review and fresh checks ran on local Windows on 2026-09-22, approximately 14:00 Europe/Lisbon,
using the README-documented user-local SDK and existing restored assets.

Paths in the boundary table are relative to `src/windows/` unless stated otherwise.

| Boundary | Independent assessment |
| --- | --- |
| AC-04 classification | `AiUsage.Windows/Adapters/Live/LiveUsageSource.ExecuteCoreAsync` maps unclassified operation exceptions to `InternalError` in both the command result and snapshot, while preserving cancellation. The concrete provider sessions classify failures into `ProviderSessionState.Failure` before the live adapter receives them; returned classifications pass through unchanged. The actual connection flow uses `ConnectWithChallengeAsync`, including Copilot. `LiveMapping.Failure` supplies the dedicated message and sets Recoverable=false. PASS. |
| Internal-error presentation | `AiUsage.Windows/Features/Presentation/ViewModelSupport.cs`, `FailureViewModel.Update`, resets ActionEnabled and handles InternalError before reauthentication, clearing action, label and wait text. Overview and account-detail XAML bind action visibility/enabled state to this model. Dedicated full/short English resources exist. The distinct-kind/message, connected/reauthentication and classified-failure regressions exercise these boundaries. PASS. |
| AC-05 lifetime wiring | `AiUsage.Windows/App.xaml.cs` initializes diagnostics before host construction and records all three existing startup/shutdown/disposal catches. `Adapters/Live/Windows/ApplicationDiagnostics` owns the sink outside host disposal, registers that same wrapper for operations, and resolves the existing owned state root with isolated demo/override behavior. The adapter records OperationFailure before presenting InternalError. PASS by source inspection; desktop fault injection remains NOT_RUN. |
| Allowlist and layer boundaries | `AiUsage.Core/Diagnostics/IDiagnosticSink` accepts only event/category enums. `DiagnosticProjection.Category` uses fixed type patterns, without reading messages, runtime names, stacks, inner exceptions or Data. `AiUsage.Infrastructure/Persistence/LocalDiagnosticSink.Record` rejects undefined codes. File records contain only UTC time and enum names, with no arbitrary fields, nested payloads, token values or provider/account identifiers. Synthetic nested-data tests cover projection and retained-file rejection. Core stays credential-free, Infrastructure owns I/O, and Windows owns desktop wiring. PASS. |
| Retention and bounds | `LocalDiagnosticSink.Rewrite` bounds input to 64 KiB, validates exactly three fields, time range and defined codes, then reconstructs canonical ASCII records. Oversized input is discarded rather than read; generated output evicts earliest queued records to remain within 64 KiB. Startup and writes remove records older than seven days and reject future dates. Closed-app pruning and crash durability are not promised. PASS. |
| Paths, concurrency and failure containment | `LocalDiagnosticSink.CheckPath` checks existing ancestors and the fixed file for reparse points before access and again after directory creation; the file check also handles dangling links. No provider-supplied filename or recursive cleanup is used. FileShare.None covers the complete read/prune/write operation; contention drops output rather than interleaving writes. Rewrite catches storage failures. Existing tests exercise actual Windows junction refusal, exclusive-handle contention, invalid storage, retention and size. Other reparse variants are source-inspected, not separately executed. Same-user check/use races and torn-write record loss remain explicit limitations. PASS. |
| Provider logging and scope | All eight auth/quota registrations retain `RemoveAllLoggers()`; host defaults remain disabled. No provider transport, authentication, credential format, dependency, telemetry or export change is present. PASS. |

Fresh verification on the unchanged candidate:

| Check | Status | Observed result |
| --- | --- | --- |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 261/261; 0 errors, failures, skips or not-run cases; 6.860 seconds test execution. |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 136/136; 0 errors, failures, skips or not-run cases; 0.632 seconds test execution. |
| Windows unpackaged build, actual product smoke and unsigned MSIX | PASS (existing evidence reused) | No production changes since 9bbf13e. Inspected the retained seven scenario JSON results: all passed, exited=true, exitCode=0; inspected launch.png. Recomputed the retained 2026.9.2201.0 package SHA-256, matching the value above. Existing build evidence and its single missing-mspdbcmf.exe tooling warning remain applicable; no new build or UI execution is claimed. |
| Live providers, desktop fault injection, packaged install/update | NOT_RUN | Neither executed nor inferred from deterministic tests or normal desktop smoke. No existing credentials were read, no sign-in or trust change occurred, and no packages were installed. |

The closure changes only verification, tasks, backlog and the obsolete review-blocker note in
design. `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`
returned `valid=true, diagnostics=[]`: PASS. `git diff --check`: PASS. Final closure diff and
links inspected; T-04 is done, AIU-028 remains incomplete, and no other task was selected.
