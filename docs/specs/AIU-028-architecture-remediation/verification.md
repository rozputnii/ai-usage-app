# AIU-028 verification

Date: 2026-09-20. This record holds the Phase 1 evidence from the analysis-only audit session
that produced [spec.md](spec.md), plus NOT_RUN placeholders for the remediation checks. It is an
evidence record, not a status mirror: no task in [tasks.md](tasks.md) has been started, and no
file outside `docs/` was changed by the session that wrote it.

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
to 6. All 94 file-and-line citations in the specification were confirmed to resolve to the
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

These are the checks each task must produce. All are NOT_RUN: no task has been started.

| Task | Acceptance | Required check | Status |
| --- | --- | --- | --- |
| T-01 | AC-01, AC-02 | Infrastructure Release suite, no reduction in test count | NOT_RUN |
| T-02 | AC-02 | Infrastructure Release suite; new per-provider reparse-point refusal test; Codex record forward-compatibility test; focused independent review | NOT_RUN |
| T-03 | AC-03 | Infrastructure and Presentation Release suites; `tools/AiUsage.ProviderConsole` Release build | NOT_RUN |
| T-04 | AC-04, AC-05 | Presentation Release suite with a non-provider exception test; redaction test over nested unknown fields, a token-shaped value and an opaque provider identifier | NOT_RUN |
| T-05 | AC-06 | Presentation Release suite including `DependencyBoundaryTests`; synthetic fifth-descriptor test | NOT_RUN |
| T-06 | AC-07 | Full offline restore; all four suites; `git diff --check`; diff inspection confirming no version string changed | NOT_RUN |
| T-07 | AC-08 | Warnings-visible desktop build at the raised analysis level with zero warnings; all four suites | NOT_RUN |
| T-08 | AC-09, AC-10 | Core disposal test (outstanding work, double dispose); Presentation re-entrant subscriber test | NOT_RUN |
| T-09 | AC-11 | Presentation Release suite; preference round-trip including unknown members | NOT_RUN |
| T-10 | AC-12 | Presentation visibility-gate test **and** interactive Windows smoke | NOT_RUN |
| T-11 | AC-01 | Infrastructure Release suite; `tools/AiUsage.ProviderConsole` Release build | NOT_RUN |
| T-12 | AC-01 | Infrastructure Release suite | NOT_RUN |

## Limitations

This is an audit and a plan. Nothing here establishes that any finding has been fixed, and the
`ready` status on the AIU-028 backlog entry does not authorize execution; selecting it remains an
owner decision under CONTRIBUTING.md.

Severity and cost in [spec.md](spec.md) are the auditor's judgement from static reading, not
measured. Three findings say so explicitly rather than implying evidence that does not exist:
F-03 and F-14 are latent, with no currently reachable failure; F-11 is structural, with no
observed misbehaviour in the present subscribers; and F-13's battery and CPU claim follows from
the code path but was not profiled, which is one reason T-10 requires interactive evidence.
