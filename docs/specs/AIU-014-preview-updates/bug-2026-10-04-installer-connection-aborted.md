# Bug report: App Installer aborts while opening the Preview package

**Status:** Open; cause not yet established

**Priority:** P2 (AIU045-D8, 2026-10-06): capture evidence on recurrence; no feed or
hosting change until evidence exists. Raise to P1 if feed-registered installs fail to update.

**First observed:** 2026-10-04 against the Pages-hosted package. The same code was
reported earlier against GitHub release downloads (see History).

**Affected feed version:** `2026.10.404.0`

## Impact

Windows App Installer can fail to install the AI Usage Preview from the published
App Installer feed. The owner reports that the development certificate is already
installed correctly. Host certificate trust was not changed during this investigation.

## Observed behavior

The attached Windows App Installer error dialog identifies publisher `AI Usage
Development`, version `2026.10.404.0`, and source `rozputnii.github.io`. It reports:

> Opening the package from location `AiUsage.Windows_2026.10.404.0_x64.msix` failed. (0x80072efe)

The expected behavior is for App Installer to retrieve and install the package from
the feed at <https://rozputnii.github.io/ai-usage-app/AiUsage.appinstaller>.

## Hosting topology

Since `efa9ea5`, each promoted Preview deploys one GitHub Pages site at
`https://rozputnii.github.io/ai-usage-app/`. It holds the feed `AiUsage.appinstaller`,
the versioned package `AiUsage.Windows_<version>_x64.msix`, the unversioned
dependency `Microsoft.WindowsAppRuntime.2.msix`, the public CER, `release.json` and
`index.html`. The feed's package and dependency URIs point at this site. Each deploy
replaces the whole site, so only the current version's package is served there. The
GitHub prerelease keeps its own copies of the same assets.

## History

On 2026-09-25, commit `efa9ea5` attributed `0x80072EFE` to GitHub release downloads,
which redirect to short-lived storage URLs, and moved the package and its dependency to
the Pages site. The README still gives that reason. The 2026-10-04 failure occurred with
the package already on Pages, so that redirect explanation does not cover it. The AIU-014
spec design text and AC-04 evidence still describe the earlier release-asset URLs.

The Pages-hosted App Installer GUI path is unverified. The 2026-09-23 Sandbox feed run
used release-asset URLs, and both it and the 2026-10-05 Sandbox registration used
`Add-AppxPackage -AppInstallerFile`, not the App Installer GUI.

## Investigation evidence

- The owner's freshly downloaded `AiUsage (7).appinstaller` matches the live feed
  manifest byte for byte. Both have SHA-256
  `C71124335B57F20339A3234BDBAB3909B16E381865D75FF7C3EC4F5843A8B0B9`.
- On 2026-10-05, the live manifest returned HTTP 200 with MIME type
  `application/appinstaller`. The versioned MSIX returned HTTP 200 with MIME type
  `application/msix` and length 31,177,240 bytes.
- A complete WinHTTP GET of that MSIX succeeded and its SHA-256 matched the
  `release.json` value `79CE5438AB20FD82B59A39BA26A136FF205C8C0E4EF26C01345B4F5E3532C249`.
  Five 64 KiB range requests also returned HTTP 206 with the requested ranges.
- A disposable Windows Sandbox registered the current feed using
  `Add-AppxPackage -AppInstallerFile`. This checked feed-based package registration,
  not the affected host's App Installer UI flow. The Sandbox did not have the
  Desktop App Installer package, so its GUI flow was not exercised.

## Assessment and limits

Microsoft identifies `0x80072EFE` as `WININET_E_CONNECTION_ABORTED`, meaning the
connection with the server was terminated abnormally ([Windows update error codes](https://learn.microsoft.com/en-us/troubleshoot/windows-client/installing-updates-features-roles/common-windows-update-errors)).
That is consistent with a transport interruption, but does not identify whether the
connection was terminated by the client, network path, or hosting edge. The successful
probes above show that the feed and package were reachable from the investigation
environment at that time; they do not disprove an intermittent failure on the owner's
machine or an App Installer-specific issue. No permanent feed or package defect, and no
root cause, has been established.

## Next diagnostic step

If the failure recurs, the owner captures on the affected machine, before retrying:

- the exact UTC time and the package URL App Installer attempted (from the dialog);
- the `Microsoft-Windows-AppXDeploymentServer/Operational` and
  `Microsoft-Windows-AppxPackaging/Operational` event logs around that time;
- the App Installer logs in
  `%LocalAppData%\Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\DiagOutputDir`;
- the WinINet proxy settings (Internet Options, or `ProxyEnable`, `ProxyServer` and
  `AutoConfigURL` under `HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings`),
  beside `netsh winhttp show proxy`, because the earlier probes used WinHTTP.

Compare a read-only fetch of that same URL from that machine at the same time. Keep the
captures local. Use that evidence to locate the interruption before changing the feed,
hosting arrangement, or installer design.
