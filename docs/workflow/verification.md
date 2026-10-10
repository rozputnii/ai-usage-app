# Verification policy

Follow [CONTRIBUTING](../../CONTRIBUTING.md) for the procedure, tiers and review. CI runs
validator regressions, document validation, deterministic product regressions on Windows,
unsigned package builds and smoke-harness publication. It does not claim interactive UI or
live-provider execution.

## Checks

Run from the repository root with the SDK selected by global.json. If `dotnet` is not on
PATH, use `& "$HOME/.dotnet/ai-usage-sdk/dotnet.exe"`. Evidence names checks by these IDs.

| ID | Check | Command |
| --- | --- | --- |
| C1 | Validator regressions | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` |
| C2 | Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` |
| C3 | Final document validation (merge only) | C2 with `--final` |
| C4 | Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` |
| C5 | Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` |
| C6 | Diff check | `git diff --check` |
| C7 | App build (Release, unpackaged) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` |
| C8 | Launch smoke | Publish the UI suite with `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true`, then run `AiUsage.Windows.Tests.exe -method "*LedgerLaunchSettingsHistoryAndExit*"` against the C7 build under the [desktop lock](#desktop-smokes) |
| C9 | Package build | `./tools/windows/Build-Package.ps1` (see [development](../development.md#native-windows-package)) |
| C10 | Preview release script tests | `tests/release/Test-PreviewRelease.ps1`; CI runs it in `validate` |

In the inner loop, run one class with `-class "<Namespace.Class>"` after a single build, and
pass `--no-build` to `dotnet run`; run the full suites once on the merged tree. A missing
restored asset is BLOCKED offline; do not enable network restoration silently. The
validator returns 0 for valid documents, 1 for diagnostics and 2 for invocation or read
failures, and it makes no network or model calls. A fresh worktree has no restored
assets, and its first build fails with `NETSDK1004`; restore once from the local package
cache first:

```powershell
'tools/AiUsage.ProjectValidation','tests/AiUsage.ProjectValidation.Tests','tests/windows/AiUsage.Infrastructure.Tests','tests/windows/AiUsage.Presentation.Tests','tests/windows/AiUsage.Windows.Tests','src/windows/AiUsage.Windows' | ForEach-Object { dotnet restore $_ --source "$HOME/.nuget/packages" }
```

## Checks by change

Select every applicable row. Feature acceptance criteria and release requirements still
apply. After the required checks pass, repeat or broaden them only for new changes,
failures or unresolved concerns.

| Change | Required |
| --- | --- |
| Documentation only | C2, C6; inspect links. |
| Rule files or agent configuration (T3) | C2, C6; inspect instruction conflicts; focused independent review. |
| Validator | C1, C2, C6. |
| Core, provider, persistence or presentation behavior | C4, C5, C2, C6. |
| Any edit under `src/windows` | Build both unit test projects (C4 and C5 compile them). |
| Windows UI, activation, tray or lifetime | The relevant regressions, C7, C9, C8 and the applicable smoke scenarios. |
| Authentication or durable-state boundaries (T3) | The relevant regressions plus focused independent review; live checks only when required and authorized. |
| Scripts under `tools/` | The script's own test or dry run where one exists (`tests/tools/Test-ItemNumbers.ps1` for the numbering script), C6; Windows PowerShell 5.1 compatibility. Preview or release scripts: C10. |
| Tests or smoke harness only | The changed tests pass; the case count is unchanged or the difference is explained; run `tools/windows/Test-Erosion.ps1` and justify each item it lists; a harness change runs the affected smoke. |
| CI workflow or dependencies (T3) | C2, C4, C5, C7; check the CI run after the merge. |

Verification targets the owner's ordinary desktop use of this personal app. Do not run
screen-reader, Windows contrast-theme, extreme zoom/DPI or unusual-display matrices, or
change host display or accessibility settings for them, unless the owner asks for that
work again. Do not add custom window or tray chrome for those scenarios. Normal launch,
ordinary interactions, relevant functional regressions and data and security checks stay
required. Older accessibility or display requirements in task references are historical
observations, not future gates.

## Merge gate

"Product inputs" are every path except `docs/**`, `.claude/**`, `.agents/**` and Markdown
files at the repository root. The same definition decides when the launch smoke is
required and when a push to `main` publishes a Preview (compared with the Preview the feed
serves).

Work is **verified** when every required check is PASS, with two exceptions: a required
smoke may stay BLOCKED only under the blocked-smoke rule in the
[Git flow](../../CONTRIBUTING.md#git-flow), and post-deploy owner checks may stay NOT_RUN: the
owner's manual and live-provider checks run after deployment in the installed app (R-190),
and are recorded under "Pending owner checks" in the backlog.
Before pushing to `main`:

- the required checks from the matrix, on the merged tree;
- C8 against the Release unpackaged build when product inputs changed;
- the review that the tier requires;
- C3.

CI `validate` and `windows-package` must be green on the pushed commit before a Preview is
signed and published; the workflow enforces this. An owner dispatch with
`PublishPreview=true` can republish the current `main` commit. If a run fails after the feed
upload (Pages upload or deploy), its re-run skips because the feed already serves that commit;
republish with that dispatch. `AIU_PREVIEW_ENABLED` is the kill switch. A Preview is an
owner-test build, not public release approval.

A BLOCKED required smoke follows the blocked-smoke rule in the
[Git flow](../../CONTRIBUTING.md#git-flow). After two failed reruns of a smoke, record FAIL
or BLOCKED with a diagnosis and stop; never skip the smoke.

Opt-in checks: the Sandbox UI audit suite (frozen: its tests are `Explicit` and need no edits for UI changes); package, upgrade and feed smokes for
manifest, packaging, update or migration changes; live-provider checks for provider or
authentication changes, with authorization.

## Desktop smokes

FlaUI smokes take the mouse and keyboard and need an unlocked interactive desktop; check
the lock state before planning them. Run each smoke under this lock (Windows PowerShell
5.1), so that parallel sessions do not collide:

```powershell
$lock = Join-Path $env:LOCALAPPDATA 'AiUsage-desktop-smoke.lock'
while ($true) {
  try { New-Item -ItemType Directory -Path $lock -ErrorAction Stop | Out-Null; break }
  catch { if ((Get-Item $lock).CreationTime -lt (Get-Date).AddMinutes(-30)) { Remove-Item $lock -Recurse -Force } else { Start-Sleep -Seconds 20 } }
}
try { <run the smoke exe with its -method filter> } finally { Remove-Item $lock -Recurse -Force }
```

Set `AIU_SMOKE_EXE` to the Release unpackaged app executable (C7) and
`AIU_SMOKE_EVIDENCE_DIRECTORY` to a fresh local evidence directory. The launch smoke covers the
demo and product paths itself. Missing prerequisites fail; they never skip silently.

- Each launched test app gets its own tray identity: the harness sets `AIU_SMOKE_TRAY_ID` and
  finds the icon named `AI Usage <id>`, so a smoke never touches the installed app's icon. A
  packaged app ignores the variable.
- Find elements through the re-find helper in `SmokeKit.cs`, by AutomationId where one exists;
  do not hold UIA references across UI changes or wait with fixed sleeps.
- `SmokeKit.SaveFailure` saves a screenshot and a UI-tree dump into the evidence directory,
  and `SmokeKit.RecordResult` appends a result to `%LOCALAPPDATA%\AiUsage-smoke-history.csv`
  (test, outcome, duration, commit, worktree), which shows flaky tests across worktrees. So
  far the launch smoke saves failure evidence and the `LedgerSmoke` partial class records
  history; adopt both in other smoke classes when they are touched.

## Development environment

Use the local unpackaged app, local regression tests and local interactive smokes for
routine development, including provider integration. Use Windows Sandbox or a disposable
VM only when a check needs isolation or a clean machine: installation prerequisites,
package install, update or uninstall, recovery with destructive fault injection, or
certificate trust changes. Ask the owner first, with the reason
([AGENTS](../../AGENTS.md#when-to-ask)). Those checks run when the affected behavior
requires them; they are not deferred to the final release.

Wait for a Sandbox run with a background task, not foreground sleeps. The audit runner's guest
writes `run.json` with `STARTED` as soon as it begins and replaces it with the final status;
if the run exceeds an explicit bound stated beforehand, stop it with `wsb stop` and record
BLOCKED with the last status.

A required MSIX build (C9) needs no installation or guest. Local unpackaged evidence does not
establish package installation, packaged activation or update behavior. Keep development
data isolated from installed-app data and preserve existing credentials. Host
installation, trust changes and live authentication keep their authorization boundaries.
Host tooling facts are in the [host environment](host-environment.md) notes.

## Layers

- Windows UI tests use xUnit v3 and FlaUI UIA3 for critical launch, navigation, settings,
  tray and activation paths, and run only where an interactive Windows desktop exists. A
  hosted runner label alone is not proof that UI automation works. Record NOT_RUN when the
  environment is unavailable.
- Live smoke uses explicit local existing credentials or a dedicated safe test account,
  never personal credentials in CI. Provider availability and consent remain external. A
  source-verified fixture alone is not a live-verified integration.

## Evidence

Each required AC has a verdict PASS, FAIL, NOT_RUN or BLOCKED with the check ID or
command, the observed result, the relevant environment, a timestamp and the code
reference. Screenshots are evidence only when captured from the actual build. Tool claims
and generated reports are not test execution.

## Golden fixtures

Use sanitized input and normalized expected output, covering unknown, missing, legacy, new
grouping, null, unlimited, exhausted, reset and credit cases. Critical independent
assertions keep the parser and the expected JSON from drifting together. Track the reason
and source of every fixture update; never approve snapshots in bulk to get green.

## Lifecycle and performance

Keep historical public schema and layout versions as sanitized fixtures, not every
identical Preview build. Test skipped-version upgrades and crash fault injection at
boundaries. Real package install, update and reset proof is separate from database fixture
tests. Measure performance targets under described reference Windows Release conditions,
with cold and warm runs separated; there is no universal 500 ms guarantee.

## Release evidence

Public release review follows CONTRIBUTING. Release acceptance needs actual CI and
applicable interactive evidence, with no unresolved material defects. Missing evidence is
NOT_RUN or BLOCKED. Deferred main protection in T-026 does not waive release authority or
protected signing and manifest operations.
