# Bug report: App Installer aborts while opening the Preview package

**Status:** Resolved 2026-10-06: packages served from IPv4-only GitHub release hosts; the
owner's App Installer GUI install of 2026.10.602.0 succeeded. Cause identified on the
owner's host: the IPv6 path resets connections to GitHub Pages. That underlying IPv6
network issue remains the owner's network matter.

**Recurrence 2026-10-07:** installing Preview 2026.10.605.0 from a downloaded
`.appinstaller` failed with 0x80072EFE again (AppXDeploymentServer events 404 and 651).
App Installer re-fetches the feed's own `Uri`, which was still on Pages.

On the host, Pages over IPv4 succeeded 10 of 10 times; over IPv6, 6 of 10 attempts
failed. The release hosts have no AAAA records. A Windows Sandbox installed from a feed
served through `github.com` release assets (HTTP 200 after a redirect,
`application/octet-stream`).

Owner decision: the feed moves to the `feed-preview` release on `github.com`. The app
also retries network-class (WinINet 12000-12199) install failures on its next check.

**Priority:** P2 (AIU045-D8, 2026-10-06): capture evidence on recurrence; no feed or
hosting change until evidence exists. Raise to P1 if feed-registered installs fail to update.
The recurrence evidence below was captured on 2026-10-06, and the owner chose the feed change.

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
`index.html`. Until 2026-10-06 the feed's package and dependency URIs pointed at this
site. Each deploy replaces the whole site, so only the current version's package is
served there. The GitHub prerelease keeps its own copies of the same assets.

The 2026-10-06 fix takes effect with the next Preview published from it; the live
`2026.10.601.0` feed still points its package and dependency at Pages. From that
Preview on, the feed, its `Uri` attribute, the CER, `release.json` and `index.html` stay
on Pages, while the feed's package and dependency URIs are the immutable assets of the
same release:
`https://github.com/rozputnii/ai-usage-app/releases/download/preview-<version>/<file>`.
The release is made public before the Pages deploy, so these URIs resolve when the new
feed goes live. The direct-download links in `index.html` use the same release assets,
and the site no longer carries the package or dependency.

## History

On 2026-09-25, commit `efa9ea5` attributed `0x80072EFE` to GitHub release downloads,
which redirect to short-lived storage URLs, and moved the package and its dependency to
the Pages site. The README gave that reason until 2026-10-06. The 2026-10-04 failure
occurred with the package already on Pages, so that redirect explanation does not cover
it. The AIU-014 spec design text described release-asset URLs throughout; since the
2026-10-06 amendment it again matches the publishing code. The published feed matches
it only from the next Preview published from that change.

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
root cause, had been established before the recurrence below.

## Recurrence evidence (2026-10-06)

The primary captured this read-only on the owner's host after the owner reported a new
App Installer failure for version `2026.10.601.0`, source `rozputnii.github.io`, error
`0x80072EFE`: opening the package `AiUsage.Windows_2026.10.601.0_x64.msix` failed.

- Time: 2026-10-06 11:31:15Z (12:31:15 local).
- URL App Installer attempted:
  `https://rozputnii.github.io/ai-usage-app/AiUsage.Windows_2026.10.601.0_x64.msix`.
- `Microsoft-Windows-AppXDeploymentServer/Operational`: event 465 (`0x80072EFE` opening
  the package), event 403 (failure to get a staging session for that URL), event 404
  (deployment failed, `0x80073CF0`).
- `Microsoft-Windows-AppxPackaging/Operational`: the reader (11:31:05Z) and the streaming
  reader (11:31:11Z) were created successfully for `AiUsage.Dev_2026.10.601.0`; the package
  and its manifest were readable and the stream broke about 4 seconds later.
- Proxy: none. WinINet `ProxyEnable` 0 and no PAC; WinHTTP direct.
- Published bytes: the Pages MSIX (31,242,176 bytes) and the release-asset MSIX have the
  same SHA-256 (prefix `DEDDD2BEE3ACBBED`).
- No AI Usage package was installed on the host at capture time.

Connection test, 11:32-11:34Z, HTTPS GET of the Pages feed and control hosts:

| Destination | Address family | Successful / attempts |
| --- | --- | --- |
| `rozputnii.github.io`, default address selection | mixed | 7 / 12 (5 reset during TLS, about 50 ms) |
| Pages `185.199.108.153` | IPv4 | 8 / 8 |
| Pages `185.199.109.153` | IPv4 | 8 / 8 |
| Pages `185.199.110.153` | IPv4 | 8 / 8 |
| Pages `185.199.111.153` | IPv4 | 8 / 8 |
| Pages `2606:50c0:8000::153` | IPv6 | 3 / 8 |
| Pages `2606:50c0:8001::153` | IPv6 | 3 / 8 |
| `github.com`, `objects.githubusercontent.com`, `api.github.com` | IPv4 only | 8 / 8 each |
| Google, Cloudflare, PyPI | IPv6 | 8 / 8 each |
| Fastly | IPv6 | 7 / 8 |
| Microsoft | IPv6 | 4 / 8 |

Failures were `curl (35) Recv failure: Connection was reset`. The host's IPv6 path resets
a share of connections to some networks, including GitHub Pages; IPv4 was clean.

Release download path, measured by the primary later on 2026-10-06 on the owner's host:
10 of 10 full MSIX downloads and 5 of 5 dependency downloads through the `github.com`
release-download redirect succeeded. All were served by `release-assets.githubusercontent.com`
at `185.199.109.133` over IPv4. `github.com` and `release-assets.githubusercontent.com`
have no AAAA records.

Assessment: on the owner's host, `0x80072EFE` is the client network's IPv6 path aborting
connections to GitHub Pages, not the package, the feed content or the signing. This
confirms the client-network-path hypothesis and refutes a Pages-edge fault over IPv4.

Mitigation (owner choice, 2026-10-06): the feed stays on Pages, and its package and
dependency URIs use the immutable GitHub release assets, whose hosts have no AAAA record
and are reached over IPv4 only (see Hosting topology).

Limits: the 2026-09-25 failure against GitHub release downloads remains unexplained; its
host or network path may have differed. The fix is verified only when the owner's App
Installer GUI install from the published feed succeeds.

Verification (2026-10-06): the owner installed 2026.10.602.0, whose feed takes the package
and dependency from the release assets, through the Pages `.appinstaller` with the App
Installer GUI, and it installed normally (installed package `AiUsage.Dev` 2026.10.602.0,
status Ok). The earlier 2026.10.601.0 feed, still Pages-hosted, had failed with `0x80072EFE`
on the same host.

## Next diagnostic step

The 2026-10-06 recurrence followed this procedure. If the failure recurs after the
release-asset fix, the owner again captures on the affected machine, before retrying:

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
