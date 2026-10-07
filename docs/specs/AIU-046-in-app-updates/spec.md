---
id: AIU-046
type: feature
status: implementing
goal: G-004
scope_version: 1
approval_basis: Owner design conversation on 2026-10-06 selected approach A (Windows App Installer APIs called from the app), three update modes with Always as default, and automatic restart only while the window is hidden. The owner approved each design section and approved this written specification on 2026-10-06. Planning amendment 2026-10-06, presented in the owner's plan review - the exact install option name, the first automatic check at 60 seconds, the 30-second drain limit and the AIU-043 projection exception in Boundaries. Review amendment 2026-10-06 (independent review findings I-1..I-3, awaiting owner confirmation) - install failure suspends automatic installs, provider commands pause during the drain, and install-failure records are coalesced.
---

# In-app updates

## Problem

The installed Preview shows a hard-coded "Update checks unavailable" in Settings. The
app has no update code: Windows App Installer checks the feed at launch and every eight
hours, but its launch check does not block activation, so the new package stages and
then fails to register (`0x80073D02`) because the app is already running. A tray app is
rarely closed, so updates seldom apply. Observed on 2026-10-06: 2026.10.604.0 staged
and failed to register while 2026.10.602.0 was running.

## Intended result

The app checks the Preview feed itself, lets the owner install a found version with one
button, and in automatic mode installs and restarts on its own while the window is
hidden. Windows' own feed checks keep working unchanged. The title-bar Settings button
shows a gear instead of the current sun-like glyph.

## Behavior

### Update modes

A persisted preference `UpdateMode` with values `Off`, `OnLaunch` and `Always`
(default `Always`). Preference files written before this change read as `Always`.

- `Always`: first check once the process has run for at least 60 seconds, then every
  5 minutes.
- `OnLaunch`: one check once the process has run for at least 60 seconds.
- `Off`: no automatic checks.

The manual **Check for updates** command works in every mode.

### Check and install

- A check calls `Package.Current.CheckUpdateAvailabilityAsync()` against the feed the
  package is already associated with. Only one check runs at a time; a manual request
  during a running check joins it.
- When an update is available, Settings shows **Install and restart**.
- Automatic install happens only when the mode is not `Off`, an update is available,
  no sign-in is in progress, and the window is hidden (tray). If the window is open,
  the status is "Update ready" with the install button, and the install starts when the
  window is hidden or the button is pressed.
- Install: wait until the process has run for at least 60 seconds (the restart API's
  minimum); pause the refresh timer and wait for in-flight provider work; register
  restart with `RegisterApplicationRestart`; call
  `PackageManager.AddPackageByAppInstallerFileAsync(feed,
  AddPackageByAppInstallerOptions.ForceTargetAppShutdown, volume)`. Windows closes the
  process, installs and relaunches it. If provider work has not finished within 30
  seconds, the install does not start and the status returns to the available state.
- Relaunch arguments: `--updated-from=<version>` always; `--background` when the
  install started while the window was hidden, so the app returns to the tray without
  showing the window. A relaunch from the button shows the window.

### Failures

- Check failure: status "Check failed · will retry" (manual check shows the HRESULT);
  the next scheduled check retries.
- Install failure: the process stays alive, restart registration is removed, refresh
  resumes, and the status offers **Retry**. Automatic installs stop for that process, so a
  failing update is not retried every 5 minutes. Network-class failures (WinINet/WinHTTP
  12000-12199, such as 0x80072EFE) are the exception: the next automatic check retries
  them (owner decision 2026-10-07). An install call that returns without
  Windows closing the app is handled the same way and shows the "didn't apply" status.
- While an install drains, refresh, per-account refresh, sign-in and sign-out start no
  new provider work; an in-progress sign-in counts as provider work to wait for.
- Loop guard: if the app starts with `--updated-from` equal to its own version, the
  update did not apply. Automatic install is suspended for that process; the status says
  "Automatic update didn't apply · install manually"; the manual button still works.
- Unpackaged development build or a package without a feed association: the status
  reads "Updates unavailable in development build" (or "…without an update feed") and
  the check and install buttons are hidden.

### Settings presentation

The Updates section shows the current version (`Version 2026.10.604.0`), the status
("Up to date · checked 17:50", "New version available", "Update ready", failure
states), **Check for updates**, **Install and restart** when applicable, and an
`Off · On launch · Always` selector styled like Density. The hard-coded summary in
`LiveLedgerSource` is removed. `CheckUpdateAvailabilityAsync` returns no target version,
so the available-version text does not name one.

### Logging

Through the AIU-043 pipeline at the Windows boundary: `UpdateCheckFailed` (HRESULT only,
repeated failures coalesced with a count and reset after success), `UpdateInstallStarted`
(automatic or manual, current version), `UpdateInstallFailed` (HRESULT, coalesced the
same way with a count) and
`UpdateApplied` (from and to version, on launch). Routine up-to-date results and timer
ticks are not logged. No feed bodies, exception text or user paths.

## Boundaries

Windows layer only (`AiUsage.Windows`), except the shared AIU-043 projection: Core gains
the four update `DiagnosticEvent` values and Infrastructure gains one typed
`FileDiagnostics.Update` method, because the existing logging API cannot record an error
code or versions. A thin `IPackageUpdates` port wraps the Windows APIs; an `UpdateCoordinator` holds the mode,
timing, visibility and failure rules and is unit-tested with fakes. No custom
downloader, feed parsing, version comparison, new dependency, manifest capability
unless the spike proves one necessary, or change to the feed's `UpdateSettings`.
Windows App Installer's launch and background checks remain in place.

## Acceptance criteria

- AC-01: Old preference files load with `UpdateMode.Always`; the selected mode persists
  across restarts.
- AC-02: The coordinator checks every 5 minutes in `Always`, once after at least 60
  seconds of uptime in `OnLaunch`, and never automatically in `Off`; manual checks work
  in every mode and concurrent requests share one check.
- AC-03: An available update installs automatically only with the window hidden and no
  sign-in in progress; with the window open it waits and offers the button.
- AC-04: A failed check or install leaves the app running with refresh resumed, restart
  registration removed and a retry path; the loop guard suspends automatic install when
  the relaunched version equals `--updated-from`.
- AC-05: Unpackaged and feed-less runs show the unavailable status without check or
  install controls; the hard-coded "Update checks unavailable" summary no longer exists.
- AC-06: In an installed Preview, the manual button and the hidden-window automatic path
  each install a newer feed version and the app relaunches by itself on that version,
  in the tray for `--background` and with the window otherwise.
- AC-07: The title-bar Settings button shows a gear glyph.
- AC-08: The logging events above are emitted with only the listed fields.

## Verification

1. Spike (throwaway, before product code): in Windows Sandbox with a local two-version
   feed (`tools/windows/Invoke-FeedUpdateSmoke.ps1`), confirm the check sees the newer
   version, `AddPackageByAppInstallerFileAsync(ForceTargetAppShutdown)` updates the
   app's own package without `packageManagement`, and `RegisterApplicationRestart`
   relaunches it with arguments, including uptime under 60 seconds. A failure stops the
   work for an owner decision before implementation.
2. Unit tests for AC-01..AC-05 and AC-08 written before the coordinator code.
3. Release build and full test suites; local unpackaged run shows the unavailable status
   and the gear (AC-05, AC-07).
4. Sandbox packaged update for AC-06: manual install, `Always` with hidden window, open
   window waiting, `OnLaunch`.
5. Owner-installed Preview: the first build with this feature is installed the existing
   way; the next published Preview must then update and relaunch by itself. Until that
   happens, the live result is NOT_RUN, not PASS.
6. Independent review per CONTRIBUTING.
