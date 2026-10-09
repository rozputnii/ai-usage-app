---
id: T-046
schema_version: 1
---
# In-app updates implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps
> use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The installed Preview checks its feed, installs a found version with one button
or automatically while hidden, and relaunches itself; the Settings button shows a gear.

**Architecture:** A platform-neutral `UpdateCoordinator` (mode, timing, visibility,
failure and loop-guard rules) talks to three small ports: `IPackageUpdates` (Windows App
Installer APIs), `IUpdateHost` (desktop lifetime: mode, window, sign-in, refresh pause,
publishing) and `IUpdateLog` (T-043 records). `LedgerProductLifetime` is the host;
`LiveLedgerSource` carries the status into the snapshot and the commands into the UI.

**Tech Stack:** C#/.NET 10, WinUI 3, Windows App SDK, WinRT `Windows.ApplicationModel` /
`Windows.Management.Deployment`, kernel32 restart API, xUnit v3.

**Spec:** [spec.md](spec.md) (approved 2026-10-06, with the planning amendment in its
approval_basis).

## Global Constraints

- Modes: `Always` (default; first check at ≥60 s uptime, then every 5 minutes),
  `OnLaunch` (one check at ≥60 s uptime), `Off` (no automatic checks). Manual check works
  in every mode.
- Automatic install only when mode ≠ `Off`, not suspended, window hidden, and no sign-in
  `Waiting`. Installs never start before 60 s uptime.
- Install call: `AddPackageByAppInstallerFileAsync(feedUri,
  AddPackageByAppInstallerOptions.ForceTargetAppShutdown, defaultVolume)` after
  `RegisterApplicationRestart(args, RESTART_NO_CRASH | RESTART_NO_HANG | RESTART_NO_REBOOT)`
  (flags `1 | 2 | 8`).
- Relaunch arguments: `--updated-from=<version>` always, plus ` --background` when the
  window was hidden at install time.
- Drain limit before install: 30 seconds; on timeout no install, status returns to the
  previous available state.
- Status copy (exact): `Updates unavailable in development build`, `Updates unavailable
  without an update feed`, `Not checked yet`, `Checking…`, `Up to date · checked HH:mm`,
  `New version available`, `Update ready`, `Downloading and installing · the app will restart`
  (was `Installing…` before 2026-10-07), `Check failed · will retry`,
  `Check failed · 0xXXXXXXXX`, `Install failed · 0xXXXXXXXX`,
  `Automatic update didn't apply · install manually`. Version line: `Version 2026.10.604.0`.
  Buttons: `Check for updates`, `Install and restart` (`Retry` after an install failure).
  Selector: `Off · On launch · Always`.
- Log events: `UpdateCheckFailed` (errorCode, consecutive), `UpdateInstallStarted`
  (trigger, version), `UpdateInstallFailed` (errorCode, consecutive), `UpdateApplied`
  (fromVersion, toVersion). Recurring failures are logged when the consecutive count is
  1 or a multiple of 12; success resets the count. No exception text, URIs or paths.
- No new dependency, manifest capability (unless T-046.1 proves one necessary, then stop for
  the owner), feed-setting change, feed parsing or version ordering.
- Coordinator calls run on the UI dispatcher; it holds no locks.
- Commits: no attribution lines; never bare `git stash`.

## Review Focus

1. An install racing a provider credential write: refresh must be paused and provider work
   drained before Windows kills the process. Pinned by T-046.2 `PauseRefreshWaitsForBusyAccounts`
   and T-046.4 `DrainTimeoutReturnsToAvailableWithoutInstalling`.
2. A `--background` relaunch must still create the tray icon and open the window from it.
   Pinned by the T-046.5 manual check and the T-046.6 Sandbox run.
3. A feed that is unreachable for hours (`0x80072EFE` every 5 minutes) must not flood the
   log. Pinned by T-046.4 `CheckFailuresShowCodeOnlyForManualAndCoalesceLogs`.
4. Switching to `Off` while an update is `Ready` and then hiding the window must not
   install. Pinned by T-046.4 `SwitchingToOffCancelsPendingAutomaticInstall`.
5. Windows reports success but relaunches the same version: no install-restart loop.
   Pinned by T-046.4 `LoopGuardSuspendsAutomaticInstall`.

### T-046.1 - Spike: App Installer API, capability and relaunch (throwaway)
- status: done
- depends_on: []
- acceptance: AC-06
- evidence: docs/specs/T-046-in-app-updates/verification.md

Answers three questions before product code: does `CheckUpdateAvailabilityAsync` see a
newer feed version; does `AddPackageByAppInstallerFileAsync(ForceTargetAppShutdown)`
update the app's own package without `packageManagement`; and does `RegisterApplicationRestart`
relaunch it with arguments, both after 60 s uptime and under 60 s. Windows Sandbox is
needed because this installs and updates packages and trusts a certificate.

- [x] **Step 1: Create the throwaway branch** `spike/aiu-046-update-api` from this branch.
  It is never merged or pushed.
- [x] **Step 2: Add the probe to `App.OnLaunched`.** Every start appends the package
  version and command line to `LocalState/spike.log`. If `LocalState/spike-trigger.txt`
  exists (content `wait` or `now`), delete it, wait 70 s (for `wait`) or 0 s (for `now`),
  then log `CheckUpdateAvailabilityAsync().Availability` and its `ExtendedError?.HResult`,
  call `RegisterApplicationRestart("--spike-relaunched", 11)` and
  `AddPackageByAppInstallerFileAsync(Package.Current.GetAppInstallerInfo().Uri,
  ForceTargetAppShutdown, new PackageManager().GetDefaultPackageVolume())`, and log the
  `ExtendedErrorCode?.HResult` if the call returns. A trigger file is used because the
  packaged app has no alias to pass arguments.
- [x] **Step 3: Build two signed packages**, `2026.10.698.0` and `2026.10.699.0`, with
  `./tools/windows/Build-Package.ps1 -MsixVersion <v> -CertificateThumbprint <owner's local
  development certificate> -NoRestore`. These are guest-only and never published. If the
  local development certificate is missing, mark the task BLOCKED and ask the owner.
- [x] **Step 4: Prepare a local file feed.** Copy the published feed's XML shape
  (`tools/windows/PreviewRelease.psm1` `New-PreviewFeed`), using `file:///` URIs to the
  mapped Sandbox folder. Write `feed.appinstaller` listing 698. Map read-only: packages,
  public CER, offline dependencies, runtime installer. Map writable: the feed folder and
  an empty evidence folder.
- [x] **Step 5: In the Sandbox,** trust the CER in the guest only, install the
  dependencies, `Add-AppxPackage -AppInstallerFile feed.appinstaller`, and launch the app
  once. Rewrite the feed to list 699 at the same URI. Write `wait` to the trigger and
  launch. Expected: the log shows `Available`, the process exits, and 699 starts with
  `--spike-relaunched`.
- [x] **Step 6: Repeat the under-60-s case.** In a fresh Sandbox, repeat Step 5 with the
  trigger `now`. Record whether the relaunch happened.
- [x] **Step 7: Record outcomes.** Write Availability values, HRESULTs, the relaunch command
  lines, the final version, and whether `0x80070005`/capability errors appeared to
  `docs/specs/T-046-in-app-updates/verification.md` under "T-046.1 spike", as PASS/FAIL per
  question. Commit only that file on the feature branch:
  `git commit -m "docs(T-046): record App Installer API spike"`.
- [x] **Step 8: Gate.** If the update needs `packageManagement` or fails, stop and ask the
  owner. If only the under-60-s relaunch fails, continue: the coordinator already delays
  installs to 60 s. Delete the spike branch.

### T-046.2 - Contract, preferences and source plumbing
- status: done
- depends_on: [T-046.1]
- acceptance: AC-01, AC-05
- evidence: docs/specs/T-046-in-app-updates/verification.md

**Files:**
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Contract/LedgerContract.cs`
- Modify: `src/windows/AiUsage.Windows/Adapters/Live/LedgerPreferenceStore.cs` (`Valid`)
- Modify: `src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerSource.cs` (lines 53, 61-74, 219, 397-400)
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/Demo/DemoLedgerScenarios.cs:157`
- Test: `tests/windows/AiUsage.Presentation.Tests/LedgerPreferenceTests.cs`, `LiveLedgerSourceTests.cs`

**Interfaces:**
- Produces (contract):
  - `internal enum UpdateMode { Always, OnLaunch, Off }`
  - `LedgerPreferences(ValueMode Mode, Density Density, bool ShowSignedOut, bool AlwaysOnTop, UpdateMode Updates = UpdateMode.Always)`
  - `internal enum UpdateState { NotPackaged, NoFeed, Idle, Checking, UpToDate, Available, Ready, Installing, CheckFailed, InstallFailed, NotApplied }`
  - `internal sealed record UpdateStatus(UpdateState State, string? Version, DateTimeOffset? CheckedAt = null, int? ErrorCode = null)` with `static UpdateStatus NotPackaged { get; } = new(UpdateState.NotPackaged, null)`
  - `SettingsSummaries(TimeSpan RefreshInterval, UpdateStatus Updates, int FailedSyncs, IReadOnlyList<ProviderKind> FailedProviders)` (replaces `string UpdatesSummary`)
  - `ILedgerSource`: `Task CheckForUpdatesAsync(CancellationToken ct) => Task.CompletedTask;` and `Task InstallUpdateAsync(CancellationToken ct) => Task.CompletedTask;`
- Produces (`LiveLedgerSource`): `Task SetUpdatesAsync(UpdateStatus value)`;
  `Func<CancellationToken, Task>? CheckUpdates { get; set; }`;
  `Func<CancellationToken, Task>? InstallUpdate { get; set; }`;
  `Task<bool> PauseRefreshAsync(TimeSpan busyLimit, CancellationToken ct)`;
  `void ResumeRefresh()`.

- [x] **Step 1: Write failing preference tests** in `LedgerPreferenceTests`:

```csharp
[Fact]
public async Task FilesWithoutUpdateModeLoadAsAlwaysAndModePersists()
{
    string? saved = """{"Version":1,"Preferences":{"Mode":0,"Density":0,"ShowSignedOut":false,"AlwaysOnTop":false},"Labels":{},"Order":[]}""";
    var store = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved), (json, _) => { saved = json; return Task.CompletedTask; });
    Assert.True(await store.LoadAsync(null, TestContext.Current.CancellationToken));
    Assert.Equal(UpdateMode.Always, store.Current.Preferences.Updates);
    Assert.Equal(CommandOutcome.Done, await store.ChangeAsync(s => s with { Preferences = s.Preferences with { Updates = UpdateMode.OnLaunch } }, TestContext.Current.CancellationToken));
    var reopened = new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved), (_, _) => Task.CompletedTask);
    Assert.True(await reopened.LoadAsync(null, TestContext.Current.CancellationToken));
    Assert.Equal(UpdateMode.OnLaunch, reopened.Current.Preferences.Updates);
}

[Fact]
public async Task UnknownUpdateModeIsRejectedWithoutOverwrite() // "Updates":7 → LoadAsync false, zero writes
```

- [x] **Step 2: Write failing source tests** in `LiveLedgerSourceTests` (use the existing
  `Accounts` fake; add a settable `Busy` flag to its snapshots if it has none):
  - `UpdateStatusSurvivesSnapshotRebuild`: `SetUpdatesAsync(new(UpdateState.UpToDate, "2026.10.604.0"))`,
    trigger a rebuild (`TickAsync`), assert `Current.Summaries.Updates.State == UpToDate`.
    A fresh source has `Summaries.Updates == UpdateStatus.NotPackaged`.
  - `PauseRefreshWaitsForBusyAccounts`: with one busy account, `PauseRefreshAsync(TimeSpan.FromMilliseconds(300), ct)`
    returns `false` and `TickAsync` refreshes again afterwards. With no busy account it returns `true`,
    and `TickAsync` makes no `RefreshAsync` call until `ResumeRefresh()`.
- [x] **Step 3: Run tests, expect FAIL** (compile errors on `UpdateMode`/`UpdateStatus`):
  `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`
- [x] **Step 4: Implement the contract, the `Valid` check
  (`Enum.IsDefined(state.Preferences.Updates)`) and the source members.** `SetUpdatesAsync`
  mirrors `SetRecoveryAsync`: it stores a field and publishes `Summaries with { Updates = value }`.
  `BuildAsync` and the constructor use that field instead of the hard-coded text.
  `CheckForUpdatesAsync`/`InstallUpdateAsync` invoke the delegates or complete.
  `PauseRefreshAsync`: set `paused` under `sync`, await the current `tick` (ignore its
  failure; the lifetime already logs it), then poll `accounts.Current.Any(a => a.Busy)`
  every 250 ms (`Task.Delay(…, time, ct)`) until `busyLimit`. On timeout, clear `paused` and
  return `false`. `TickAsync` returns `Task.CompletedTask` while paused. Demo summaries use
  `new UpdateStatus(UpdateState.UpToDate, "2026.10.604.0", now)`.
- [x] **Step 5: Run tests, expect PASS** (same command; the full suite stays green).
- [x] **Step 6: Commit**: `git commit -m "feat(T-046): carry update mode and status through the Ledger contract"`

### T-046.3 - T-043 update records
- status: done
- depends_on: []
- acceptance: AC-08
- evidence: docs/specs/T-046-in-app-updates/verification.md

**Files:**
- Modify: `src/windows/AiUsage.Core/Diagnostics/IDiagnosticSink.cs` (append four enum values)
- Modify: `src/windows/AiUsage.Infrastructure/Diagnostics/FileDiagnostics.cs`
- Test: `tests/windows/AiUsage.Infrastructure.Tests/FileDiagnosticsTests.cs`

**Interfaces:**
- Produces: `DiagnosticEvent.UpdateCheckFailed, UpdateInstallStarted, UpdateInstallFailed, UpdateApplied`,
  appended at the end of the enum so existing values keep their numbers.
- Produces: `public sealed record UpdateFacts(int? ErrorCode = null, bool? Automatic = null, Version? From = null, Version? To = null, int? Consecutive = null)`
  in `AiUsage.Infrastructure.Diagnostics`, and `public void FileDiagnostics.Update(DiagnosticEvent eventCode, UpdateFacts facts)`.

- [x] **Step 1: Write the failing test** `UpdateRecordsCarryOnlyTypedFacts`:
  - `log.Update(UpdateCheckFailed, new(ErrorCode: unchecked((int)0x80072EFE), Consecutive: 12))`
  - `log.Update(UpdateApplied, new(From: new(2026,10,602,0), To: new(2026,10,604,0)))`
  - `log.Update(DiagnosticEvent.OperationFailure, new(ErrorCode: 1))`
  - Flush and read with the existing `ReadShared` pattern. Assert that the `UpdateCheckFailed`
    record has severity `Warning`, `context.errorCode == "0x80072EFE"` and
    `context.consecutive == 12`. Assert that `UpdateApplied` has
    `context.fromVersion == "2026.10.602.0"` and `toVersion == "2026.10.604.0"`.
    Assert that no `OperationFailure` record was written.
- [x] **Step 2: Run, expect FAIL:**
  `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`
- [x] **Step 3: Implement `Update`.** It ignores any event outside the four. Severity:
  `UpdateCheckFailed` → Warning, `UpdateInstallFailed` → Error, otherwise Information.
  Context: `new { errorCode = $"0x{code:X8}" or null, trigger = "automatic"|"manual"|null, fromVersion, toVersion, consecutive }`
  via the existing `Event(…, context:)`.
- [x] **Step 4: Run, expect PASS** (full Infrastructure suite).
- [x] **Step 5: Commit**: `git commit -m "feat(T-046): add typed update records to the T-043 projection"`

### T-046.4 - UpdateCoordinator
- status: done
- depends_on: [T-046.2]
- acceptance: AC-02, AC-03, AC-04, AC-05
- evidence: docs/specs/T-046-in-app-updates/verification.md

**Files:**
- Create: `src/windows/AiUsage.Windows/Adapters/Live/UpdateCoordinator.cs` (compiled into
  Presentation.Tests by the existing `Adapters/Live/*.cs` glob; no WinRT types)
- Modify: `src/windows/AiUsage.Windows/Features/Ledger/LedgerFormat.cs` (status text)
- Test: `tests/windows/AiUsage.Presentation.Tests/UpdateCoordinatorTests.cs`

**Interfaces:**
- Consumes: `UpdateMode`, `UpdateState`, `UpdateStatus` (T-046.2); `ILedgerScheduler` and the
  test `ManualScheduler` (`LedgerTests.cs`).
- Produces (all in `AiUsage.Adapters.Live`):

```csharp
internal enum UpdateSupport { Supported, NotPackaged, NoFeed }
internal sealed record UpdateCheckResult(bool Available, int? ErrorCode);
internal interface IPackageUpdates
{
    UpdateSupport Support { get; }
    string? CurrentVersion { get; }
    Task<UpdateCheckResult> CheckAsync(CancellationToken ct);
    /// <summary>Returns the failure HRESULT; on success Windows ends the process, so null only reaches fakes.</summary>
    Task<int?> InstallAsync(string restartArguments, CancellationToken ct);
}
internal interface IUpdateHost
{
    UpdateMode Mode { get; }
    bool WindowHidden { get; }
    bool SignInActive { get; }
    Task<bool> PauseRefreshAsync(CancellationToken ct);
    void ResumeRefresh();
    Task PublishAsync(UpdateStatus status);
}
internal interface IUpdateLog
{
    void CheckFailed(int errorCode, int consecutive);
    void InstallStarted(bool automatic, string version);
    void InstallFailed(int errorCode, int consecutive);
    void Applied(string fromVersion, string toVersion);
}
internal sealed record UpdateLaunch(bool Background, string? UpdatedFrom)
{
    public static UpdateLaunch Parse(IEnumerable<string> args); // --background; --updated-from=<d.d.d.d> else null
}
internal sealed class UpdateCoordinator(IPackageUpdates package, IUpdateHost host, IUpdateLog log,
    ILedgerScheduler scheduler, TimeProvider time, UpdateLaunch launch) : IDisposable
{
    internal static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(60);
    internal static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    public UpdateStatus Status { get; }
    public void Start();
    public Task CheckAsync(bool manual);
    public Task InstallAsync(bool automatic);
    /// <summary>Idempotent: re-reads mode, window and sign-in; called on every source change and window show/hide.</summary>
    public void Reevaluate();
    public void Dispose();
}
```
- Produces: `internal static string LedgerFormat.UpdateText(UpdateStatus status)`, which
  returns the Global Constraints copy. `HH:mm` is `CheckedAt` in local time with the
  invariant culture.

Rules the tests pin (the algorithm the signatures leave open):
- `Start`: an unsupported package publishes `NotPackaged`/`NoFeed` with no timers.
  Otherwise, if `UpdatedFrom == CurrentVersion`, set suspended and state `NotApplied`; if
  `UpdatedFrom` is another value, call `log.Applied(from, current)` and use state `Idle`.
  Publish, then schedule `FirstCheckDelay`.
- First-delay callback: mark uptime reached. Run any pending install. If mode ≠ `Off`, check.
  If mode is `Always`, schedule `CheckInterval`. Each interval tick reschedules first, then
  checks.
- `CheckAsync`: a running check is returned to a second caller, and the manual flag is
  remembered. Exceptions map to `ErrorCode = e.HResult` (`OperationCanceledException` is
  rethrown). On error, increment the streak, log when the streak is 1 or a multiple of 12,
  and publish `CheckFailed` with the code only if a manual caller took part. On success,
  reset the streak and set `CheckedAt`. Not available → `UpToDate`. Available → `NotApplied`
  if suspended, else `Ready` if mode ≠ `Off`, else `Available`. Then `Reevaluate()`.
- `Reevaluate`: if the mode changed, reschedule (`Always` starts the interval, otherwise it
  is disposed), and `Ready` under `Off` becomes `Available`. If the state is `Ready`, the
  window is hidden, no sign-in is active and nothing is installing, start
  `InstallAsync(automatic: true)`.
- `InstallAsync`: only from `Available`/`Ready`/`InstallFailed`/`NotApplied`. An automatic
  install is refused when suspended or `Off`. Before uptime is reached, publish `Installing`
  and defer. Otherwise publish `Installing`. If `PauseRefreshAsync` returns false, call
  `ResumeRefresh` and republish the prior state. Otherwise call
  `log.InstallStarted(automatic, version)` and
  `package.InstallAsync("--updated-from=" + version + (host.WindowHidden ? " --background" : ""))`.
  A failure code or exception increments the install streak, logs as above, calls
  `ResumeRefresh` and publishes `InstallFailed` with the code. The next check can make it
  `Ready` again.

- [x] **Step 1: Write the failing tests** in `UpdateCoordinatorTests.cs`. Fakes:
  - `FakePackage`: `Support`, `CurrentVersion = "2026.10.604.0"`, `Result`, `Checks`,
    `Installs`, `LastArguments`, `InstallResult`, `Hold` with a `TaskCompletionSource`.
  - `FakeHost`: settable `Mode`, `WindowHidden`, `SignInActive`, `PauseResult = true`,
    `Resumed`, `Status`.
  - `FakeLog`: records lists.

  Tests:
  - `AlwaysChecksAfterSixtySecondsThenEveryFiveMinutes`: 0 checks after Start;
    1 after `Run(60 s)`; 2 after `Run(5 min)`.
  - `OnLaunchChecksOnceAfterSixtySeconds`: 1 after `Run(60 s)`; still 1 after two `Run(5 min)`.
  - `OffNeverChecksAutomaticallyButManualWorks`: 0 after `Run(60 s)` + `Run(5 min)`;
    1 after `await CheckAsync(true)`.
  - `ConcurrentChecksShareOneCall`: with `Hold`, `Assert.Same(CheckAsync(true), CheckAsync(false))`;
    release `(false, null)`; `Checks == 1`; state `UpToDate` with `CheckedAt` set.
  - `SwitchingToAlwaysAfterLaunchStartsInterval`: `Off`, `Run(60 s)` → 0; `Mode = Always`,
    `Reevaluate()`, `Run(5 min)` → 1.
  - `AvailableUpdateInstallsOnlyWhenHiddenAndNoSignIn` (Theory, Always, `Run(60 s)`,
    `Result = (true, null)`): hidden/no sign-in → `Installs == 1`; visible → 0 and `Ready`;
    hidden with sign-in → 0 and `Ready`.
  - `ReadyInstallsWhenWindowHides`: visible → `Ready`; `WindowHidden = true`, `Reevaluate()`
    → `Installs == 1`, `LastArguments == "--updated-from=2026.10.604.0 --background"`,
    `log.Started == [(true, "2026.10.604.0")]`.
  - `ManualInstallFromOpenWindowRelaunchesWithWindow`: `Off`, `Run(60 s)`,
    `await CheckAsync(true)` → `Available`; `await InstallAsync(false)` →
    `LastArguments == "--updated-from=2026.10.604.0"`.
  - `InstallBeforeSixtySecondsWaitsForUptime`: manual check before the delay → `Available`;
    `InstallAsync(false)` → `Installs == 0` and `Installing`; `Run(60 s)` → `Installs == 1`.
  - `FailedInstallResumesRefreshAndOffersRetry`: `InstallResult = unchecked((int)0x80073D02)`
    → `Resumed == 1`, `InstallFailed` with that code, `log.InstallFailed == [(code, 1)]`.
  - `DrainTimeoutReturnsToAvailableWithoutInstalling`: `PauseResult = false` →
    `Installs == 0`, `Resumed == 1`, state back to `Available`, `log.Started` empty.
  - `CheckFailuresShowCodeOnlyForManualAndCoalesceLogs`: an automatic failure gives
    `CheckFailed` with a null code. 13 failures log consecutive `[1, 12]`. A success then
    a failure logs `1` again. A manual failure carries `0x80072EFE`.
  - `SwitchingToOffCancelsPendingAutomaticInstall`: visible `Ready`; `Mode = Off`,
    `WindowHidden = true`, `Reevaluate()` → `Installs == 0`, state `Available`.
  - `LoopGuardSuspendsAutomaticInstall`: `UpdatedFrom == "2026.10.604.0"` → `NotApplied`.
    Hidden, `Run(60 s)` with an update available → `Installs == 0`, still `NotApplied`,
    `log.Applied` empty. `InstallAsync(false)` → `Installs == 1`.
  - `RelaunchAfterUpdateLogsApplied`: `UpdatedFrom == "2026.10.602.0"` →
    `log.Applied == [("2026.10.602.0", "2026.10.604.0")]`, state `Idle`.
  - `UnsupportedPackageNeverChecks` (Theory `NotPackaged`, `NoFeed`): matching state;
    0 checks after `Run(5 min)` and `CheckAsync(true)`.
  - `LaunchArgumentsParse`: `["AiUsage.exe", "--background", "--updated-from=2026.10.602.0"]`
    → `(true, "2026.10.602.0")`; `--updated-from=..\x` → `UpdatedFrom == null`.
  - `StatusTextMatchesSpec` (Theory over each `UpdateState`): `LedgerFormat.UpdateText` equals
    the Global Constraints copy; `UpToDate` matches `^Up to date · checked \d\d:\d\d$`.
- [x] **Step 2: Run, expect FAIL** (missing types):
  `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`
- [x] **Step 3: Implement `UpdateCoordinator.cs` and `LedgerFormat.UpdateText`** per the
  signatures and rules above.
- [x] **Step 4: Run, expect PASS** (full Presentation suite).
- [x] **Step 5: Commit**: `git commit -m "feat(T-046): add the update coordinator"`

### T-046.5 - Windows adapter, lifetime, launch, settings UI and gear
- status: done
- depends_on: [T-046.3, T-046.4]
- acceptance: AC-03, AC-05, AC-06, AC-07, AC-08
- evidence: docs/specs/T-046-in-app-updates/verification.md

**Files:**
- Create: `src/windows/AiUsage.Windows/Adapters/Live/Windows/AppInstallerUpdates.cs`
- Modify: `Adapters/Live/Windows/LedgerProductLifetime.cs`, `Adapters/Live/Windows/ApplicationDiagnostics.cs`,
  `Composition/LedgerRegistration.cs`, `App.xaml.cs`, `Features/Ledger/Views/LedgerWindow.xaml(.cs)`,
  `Features/Ledger/LedgerSettingsViewModel.cs`, `Features/Ledger/Views/LedgerSettingsView.xaml(.cs)`

**Interfaces:**
- Consumes: T-046.2 source members; T-046.3 `FileDiagnostics.Update`/`UpdateFacts`; T-046.4 ports and coordinator.
- Produces: `sealed partial class AppInstallerUpdates : IPackageUpdates`;
  `LedgerProductLifetime : IUpdateHost` plus `void StartUpdates(LedgerShell shell, UpdateLaunch launch)`;
  `ApplicationDiagnostics : IUpdateLog`; `LedgerShell.WindowHidden` and
  `event EventHandler? LedgerShell.VisibilityChanged`; `LedgerRegistration.Start(IServiceProvider, Func<Task>, bool background)`.

- [x] **Step 1: `AppInstallerUpdates`.**
  - `Support`: `Package.Current` throws `InvalidOperationException` → `NotPackaged`;
    `GetAppInstallerInfo()` null → `NoFeed`.
  - `CurrentVersion`: `Id.Version` as `Major.Minor.Build.Revision`.
  - `CheckAsync`: `Available`/`Required` → `(true, null)`; `NoUpdates`/`Unknown` →
    `(false, null)`; `Error` → `(false, ExtendedError?.HResult ?? unchecked((int)0x80004005))`;
    a thrown exception → its `HResult`.
  - `InstallAsync`: register restart (non-zero HRESULT → return it). Call
    `AddPackageByAppInstallerFileAsync(info.Uri, ForceTargetAppShutdown, pm.GetDefaultPackageVolume())`.
    A result with `ExtendedErrorCode` or a thrown exception → `UnregisterApplicationRestart()`
    and return the HRESULT.
  - P/Invoke via `[LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]`
    for `RegisterApplicationRestart(string, uint)` and `UnregisterApplicationRestart()`.
    If the analyzers require it, set `AllowUnsafeBlocks` in this csproj only.
- [x] **Step 2: `ApplicationDiagnostics : IUpdateLog`.** Each method maps to
  `sink?.Update(event, new UpdateFacts(...))`. Versions are parsed with `Version.TryParse`;
  invalid versions become null.
- [x] **Step 3: Window visibility and background start.**
  - `LedgerWindow` raises `VisibilityChanged` in `ShowAndActivate` and in the `OnClosing`
    hide, and exposes `IsHidden`.
  - `LedgerShell(…, bool background)` forwards both. When `background` is true it calls
    `TrayIcon.ForceCreate()` instead of `window.Activate()`; the window starts hidden.
  - `App.OnLaunched` parses `UpdateLaunch.Parse(Environment.GetCommandLineArgs())` and passes
    `launch.Background` to `LedgerRegistration.Start`. After `InitializeAsync` it calls
    `product.StartUpdates(shell, launch)`. Demo mode has no coordinator.
- [x] **Step 4: `LedgerProductLifetime` as host.**
  - `StartUpdates` creates the coordinator with `AppInstallerUpdates`, `this`, `diagnostics`,
    the `ILedgerScheduler`, `TimeProvider.System` and `launch`. It sets
    `source.CheckUpdates = _ => coordinator.CheckAsync(true)` and
    `source.InstallUpdate = _ => coordinator.InstallAsync(false)`, subscribes
    `source.Changed` and `shell.VisibilityChanged` to `Reevaluate`, and calls `Start()`.
  - `Mode` → `source.Preferences.Updates`.
  - `SignInActive` → `source.Current.SignInStrip?.Phase == SignInPhase.Waiting`.
  - `PauseRefreshAsync`: take `gate`. If not `started`, deletion has begun or shutdown is in
    progress, release and return false. Otherwise stop the timer and return
    `await source.PauseRefreshAsync(TimeSpan.FromSeconds(30), ct)`; on false, release
    `gate` and restart the timer. On true, keep `gate` held until `ResumeRefresh` or process
    exit, which blocks deletion and recovery.
  - `ResumeRefresh` → `source.ResumeRefresh()`, release `gate` if held, `timer.Start()`.
  - `PublishAsync` → `source.SetUpdatesAsync`.
  - `StopAsync`/`Dispose` dispose the coordinator first.
- [x] **Step 5: Settings view model and view.**
  - New observable properties: `UpdateVersionText`, `CanCheckUpdates` (not `NotPackaged`,
    `NoFeed`, `Checking`, `Installing`), `CanInstallUpdate` (`Available`, `Ready`,
    `InstallFailed`, `NotApplied`), `InstallUpdateText` (`Retry` for `InstallFailed`,
    otherwise `Install and restart`), `IsUpdateAlways`, `IsUpdateOnLaunch`, `IsUpdateOff`.
  - `UpdatesText` comes from `LedgerFormat.UpdateText`.
  - Commands: `CheckForUpdatesAsync`, `InstallUpdateAsync` (forward to the source) and
    `SetUpdateModeAsync(UpdateMode)` (same pattern as `SetDensityAsync`).
  - In `LedgerSettingsView.xaml`, replace the UPDATES grid (around line 177). Add the
    section label, the version line, the status caption, a `Check for updates` outline
    button and an `Install and restart` primary button, each hidden when not allowed.
    Add an `Updates` row with a three-segment `Off · On launch · Always` selector copied
    from the Density markup (Click handlers `OnUpdatesOff/OnLaunch/Always`; `Segment(...)`
    helpers) with automation names `Automatic updates: off|on launch|always`.
- [x] **Step 6: Gear.** In `LedgerWindow.xaml` (`SettingsButton`, lines 114-118), replace
  the `Path` with
  `<FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xE713;" FontSize="16" Foreground="{StaticResource LedgerInkBrush}" />`.
- [x] **Step 7: Build and run unpackaged.**
  `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore`
  → 0 warnings, 0 errors. Start it with an empty `AIU_DEVELOPMENT_STATE_DIRECTORY`. Expected:
  - the gear is in the title bar;
  - Settings shows `Updates unavailable in development build` and no check/install buttons;
  - the selector switches and survives a restart.

  Start again with `--background`. Expected: no window, a tray icon whose left-click popup
  and Open work. If `ForceCreate` leaves the tray inert, use `Activate()` followed by
  `AppWindow.Hide()` and record that.
- [x] **Step 8: Run all suites** (README "Local checks"), then commit:
  `git commit -m "feat(T-046): install feed updates from the app and relaunch"`

### T-046.6 - Verification, review and records
- status: done
- depends_on: [T-046.5]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-08
- evidence: docs/specs/T-046-in-app-updates/verification.md

- [x] **Step 1: Run the local checks.** Run the README "Local checks" block plus a Release
  build (`-c Release`). Expected: every suite passes, the validator returns 0, and
  `git diff --check` is clean.
- [x] **Step 2: Sandbox packaged update (AC-06).** Reuse the T-046.1 local-feed setup with the
  real feature: build 2026.10.695.0, 696 and 697, guest-only. Install 695 through the feed,
  then advance the feed once per case and record the version and command line:
  - (a) Manual: Settings → Check for updates → Install and restart. Expected: the app
    relaunches on the new version with the window shown.
  - (b) `Always`, window closed to the tray. Expected: within about 5 minutes the app
    relaunches on the new version, tray only (`--background`).
  - (c) Window open. Expected: `Update ready` and no install until the window is hidden.
  - (d) `OnLaunch`: restart the app. Expected: one check after 60 s.
- [x] **Step 3: Independent focused review** (CONTRIBUTING: forced process shutdown and
  restart registration). Dispatch the `aiu-reviewer` agent with this file, the spec and
  `git diff main...HEAD`. Fix material findings and rerun the targeted checks.
- [x] **Step 4: Records.**
  - In `verification.md`, list each check with PASS/FAIL/NOT_RUN/BLOCKED.
  - The owner live check (spec step 5) stays NOT_RUN until a later Preview updates the
    owner's install by itself.
  - Set backlog T-046 `status` and evidence per `docs/workflow/formats.md`.
  - Set this file's task statuses and add a one-line Handoff with the exact next action.
- [x] **Step 5: Preview gate and integration.** Run the `--demo` Release startup smoke and a
  primary diff review. Commit
  (`git commit -m "docs(T-046): record in-app update verification"`) and integrate to
  `main` per CONTRIBUTING, which publishes a Preview after CI is green. Tell the owner to
  install that one Preview the existing way: close the app, then use App Installer.

## Handoff

Implemented and verified 2026-10-06 on `users/updates-check-unavailable-f5396f`. The commits run from
`e2467ec` (spec) to the records commit. Evidence is in [verification.md](verification.md).

What changed from the plan:
- T-046.5 replaced `TaskbarIcon.ForceCreate` with `Activate()` plus `AppWindow.Hide()`, because the icon
  ignored clicks after a background start.
- The review fixes I-1..I-3 changed the spec (approval_basis records the review amendment).

Deferred minor findings (owner decides):
- M-1: install is refused while local-data recovery is pending.
- M-2: a pending automatic install queued before 60 s is not re-checked.
- M-3: scheduled checks briefly hide the Install button.
- M-4: spurious install records can be written at exit.
- M-5: `--background` is decided after the drain.
- M-6: unexpected Package API exceptions at startup.
- M-7: brief "development build" text at startup.
- M-8: a deletion requested during an install is lost.
- M-9: test gaps.

AC-06 owner live check: NOT_RUN.

Next action:
1. Owner reviews the branch and the review amendment.
2. Integrate to `main`, which publishes a Preview.
3. The owner installs that Preview once the existing way: close the app, then App Installer.
4. The next Preview must update and relaunch by itself.
