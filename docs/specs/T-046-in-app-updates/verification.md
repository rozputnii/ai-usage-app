# T-046 verification

Started 2026-10-06 on branch `users/updates-check-unavailable-f5396f`. Checks use
synthetic state only: no provider sign-in, credential or host install. Guest packages were
signed with the owner's local development certificate (`771CB0E8…E774`) and trusted only
inside disposable Windows Sandbox guests (LocalMachine\TrustedPeople). Their versions
were never published.

## T-046.1 spike: App Installer API, capability and relaunch (2026-10-06)

Throwaway branch `spike/aiu-046-update-api` (deleted, never merged or pushed) added a
probe to `App.OnLaunched`. Two packages, 2026.10.698.0 and 2026.10.699.0, were installed
through a local `file:///` App Installer feed with the published feed's update settings.
The feed was then advanced to 699 and a trigger file started the probe.

| Question | Verdict | Evidence |
| --- | --- | --- |
| `CheckUpdateAvailabilityAsync` sees the newer feed version | PASS | `check=Available err=0x00000000` in both runs. |
| `AddPackageByAppInstallerFileAsync(ForceTargetAppShutdown)` updates the app's own package without `packageManagement` | PASS | The manifest has only `runFullTrust`. 699 was registered 38 s (wait run) and 37 s (now run) after the call. The call never returned; Windows closed the process. Feed association was unchanged afterwards. |
| `RegisterApplicationRestart` relaunches with arguments after ≥60 s uptime | PASS | Wait run: install at 70 s uptime; `start 2026.10.699.0 … --spike-relaunched`. |
| The same under 60 s uptime | PASS | Now run: install at 0 s uptime; relaunched with `--spike-relaunched`. The coordinator's 60 s install delay is kept as a margin; it is not needed. |

Findings for implementation:
- `LibraryImport` needs `AllowUnsafeBlocks` (SYSLIB1062).
- The .NET runtime installer takes about 6 minutes in a fresh guest.
- Host SignTool verification fails as designed (the root is untrusted on the host).

Decision: no manifest capability is needed, so implementation continues as specified.

## Acceptance (2026-10-06)

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `FilesWithoutUpdateModeLoadAsAlwaysAndModePersists` and `UnknownUpdateModeIsRejectedWithoutOverwrite`. Local unpackaged run: the selector wrote `"Updates":1`, and On launch was still selected after a restart. Sandbox run (d) persisted `"Updates":1` in the installed package. |
| AC-02 | PASS | Coordinator tests cover Always (60 s, then every 5 minutes), OnLaunch (once), Off (manual only) and a shared concurrent check. Sandbox (d): with OnLaunch, "Update ready" appeared 64 s after launch. |
| AC-03 | PASS | Coordinator tests cover hidden or open window and sign-in. Sandbox (c): with the window open, Always found 696 at about 60 s and showed "Update ready"; it was still on 695 20 s later. Sandbox (b): after closing to the tray, it installed automatically. |
| AC-04 | PASS | Tests: `FailedInstallResumesRefreshAndOffersRetry`, `FailedAutomaticInstallIsNotRetriedAutomatically`, `InstallThatReturnsWithoutClosingTheAppResumesAndSuspends`, `DrainTimeoutReturnsToAvailableWithoutInstalling`, `LoopGuardSuspendsAutomaticInstall`, `PauseRefreshWaitsForBusyAccounts`, `ProviderCommandsDoNothingWhileRefreshIsPaused` and `PauseWaitsForAWaitingSignIn`. A real Windows install failure was not provoked in Sandbox. |
| AC-05 | PASS | Local unpackaged Debug run: "Updates unavailable in development build", with no Check or Install button and an empty version line. The hard-coded summary was removed. The no-feed state is unit-tested only. |
| AC-06 | PASS (Sandbox); owner live NOT_RUN | Windows Sandbox, a local `file:///` feed and packages 695→698 built from this branch (see below). The owner-installed Preview stays NOT_RUN until a later published Preview updates the owner's install by itself. |
| AC-07 | PASS | The gear (Segoe Fluent Icons E713) was seen in the local unpackaged run and in Sandbox screenshots. |
| AC-08 | PASS | `UpdateRecordsCarryOnlyTypedFacts`. Sandbox logs contain only `UpdateInstallStarted` (trigger, fromVersion) and `UpdateApplied` (fromVersion, toVersion) for each update. They contain no URI, path or exception text: the only privacy-grep matches were the empty `"exception":null` field. |

### Sandbox packaged update (T-046.6)

The guest trusted the CER only inside the Sandbox. Packages 2026.10.695.0–698.0 were built from commit `b7695ab`, and the feed was advanced while the app ran.

| Case | Verdict | Observation |
| --- | --- | --- |
| (c) Always, window open | PASS | "Update ready" with the install button. No install for 20 s; version 695. |
| (a) Manual Install and restart | PASS | 696 registered 36 s after the click. Relaunched with the window and `--updated-from=2026.10.695.0`, no `--background`. |
| (b) Always, closed to tray | PASS | 697 registered about 90 s later. Relaunched without a window and with `--updated-from=2026.10.696.0 --background`. |
| (b) Tray icon after background relaunch | FAIL → fixed | The tray icon did not open the popup. A control experiment with the unpackaged Release build showed that a normal start works, while a `--background` start using `TaskbarIcon.ForceCreate` ignored both UIA invoke and a real click. Fix `33760d4` activates the window and hides it at once. Rerun: all four trials open the popup, and the window stays hidden before the click. The packaged flow was not rerun after this fix. |
| (d) OnLaunch, then hidden | PASS | "Update ready" after 64 s. Closing to the tray installed 698, relaunched with `--updated-from=2026.10.697.0 --background`. |

Then the independent review found three Important issues: a returning install left refresh paused, a failing install was retried every 5 minutes, and provider work could start during the drain. All three are fixed with tests in `617104b`. The deferred minor findings are recorded in the handoff.

### Local checks

At `33760d4`:
- Presentation: 232/232
- Infrastructure: 880/880
- ProjectValidation: 82/82
- Validator: valid
- `git diff --check`: clean
- Debug and Release builds: 0 warnings
- Release `--demo` startup smoke: window shown, Updates section rendered

## Feed host change (2026-10-07)

Installing Preview 2026.10.605.0 on the owner's host failed with 0x80072EFE. App
Installer re-fetches the feed address, which was still on GitHub Pages, and the
owner's IPv6 path to Pages failed in 6 of 10 attempts. IPv4 succeeded in 10 of 10.

Owner decision: publish the feed through the `feed-preview` GitHub release.

Checks:
- Release policy tests: 64 assertions PASS under Windows PowerShell 5.1.
- `NetworkInstallFailureIsRetriedOnTheNextCheck`: RED→GREEN. Presentation suite 233/233.
- Windows Sandbox: `Add-AppxPackage -AppInstallerFile` with the github.com-served copy
  `preview-2026.10.605.0/AiUsage.appinstaller` (redirect, `application/octet-stream`)
  installed 2026.10.605.0: PASS.

Not run:
- A feed whose own `Uri` is the `feed-preview` address: NOT_RUN until the first Preview
  published with this change.
- The owner's host install from that address: NOT_RUN.
