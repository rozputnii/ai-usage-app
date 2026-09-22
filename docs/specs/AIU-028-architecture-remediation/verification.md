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

These are the checks each task must produce. T-08 and T-09 have since been executed and are
recorded below; every other task is still NOT_RUN and none has been started.

| Task | Acceptance | Required check | Status |
| --- | --- | --- | --- |
| T-01 | AC-01, AC-02 | Infrastructure Release suite, no reduction in test count | NOT_RUN |
| T-02 | AC-02 | Infrastructure Release suite; new per-provider reparse-point refusal test; Codex record forward-compatibility test; focused independent review | NOT_RUN |
| T-03 | AC-03 | Infrastructure and Presentation Release suites; `tools/AiUsage.ProviderConsole` Release build | NOT_RUN |
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

This is an audit and a plan. Nothing here establishes that any finding has been fixed, and the
`ready` status on the AIU-028 backlog entry does not authorize execution; selecting it remains an
owner decision under CONTRIBUTING.md.

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
