# AIU-014 development Preview evidence

Started 2026-09-22; development-phase operation verified 2026-09-23. Public signing,
Stable promotion and trusted public distribution are outside the selected development
phase and remain open.

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Main pushes eb52980 and d20178f ran validate, windows-package, then the gated preview job: runs [35895908547](https://github.com/rozputnii/ai-usage-app/actions/runs/35895908547) and [35899151315](https://github.com/rozputnii/ai-usage-app/actions/runs/35899151315). PRs, failed jobs and disabled provisioning cannot reach the job; the new guard also requires job `preview`. |
| AC-02 | PASS | Actual reservations 2026.9.2301.0, then 2026.9.2302.0, above the failed run's retained draft 2026.9.2223.0. The second run promoted the feed only after the ancestry check (`promote feed: True`). A real late older source remains exercised only by local ancestry assertions. |
| AC-03 | PASS | Hosted SignTool: `Successfully verified`, 1 file, 0 errors, in both runs. Signer B4C73392759C80CA5D1AA4004486B6C957417609 is the dedicated CI certificate. Runner-only root trust fix d28e6be/eb52980 passed independent review. |
| AC-04 | PASS | Non-draft prereleases with five versioned assets each. The Pages feed returns HTTP 200 `application/appinstaller`, exact identity, WinAppRuntime dependency, HTTPS release URIs, and forward-only, nonblocking and background settings. A guest observed the launch-triggered forward update. |
| AC-05 | PASS | Trust was granted only inside the disposable guest (LocalMachine\TrustedPeople), after checking the thumbprint. No host trust, host install or credential copy: after the run, no host store contains the thumbprint and `Get-AppxPackage AiUsage.Dev` on the host is empty. |
| AC-06 | PASS | Windows Sandbox installed 2026.9.2301.0 through the feed and kept the synthetic Dark preference. It then updated through the feed to 2026.9.2302.0, with the same family, identical LocalState hashes and Dark shown after update. Release regressions and the independent signing-boundary review passed. |

## Hosted publication

- Failed run [35785324979](https://github.com/rozputnii/ai-usage-app/actions/runs/35785324979)
  at 265c489 signed correctly, but `signtool verify /pa` reported a root "not trusted by
  the trust provider". Root cause: the public CER was only in CurrentUser\TrustedPeople,
  which does not anchor the self-signed chain for that verification. Its draft
  `preview-2026.9.2223.0` remains a consumed reservation and was neither deleted nor reused.
- Fix d28e6be, with review follow-up eb52980. The hosted `preview` job adds only the
  signer's public, self-signed CER to LocalMachine\Root. It refuses outside the owned
  hosted main-push `preview` job or when the CER is already trusted. It removes and
  rechecks the trust in `finally`, before deleting the imported key. Verification is
  unchanged and still fails closed.
- Run 35895908547 (eb52980): all three jobs PASS. Released `preview-2026.9.2301.0`, MSIX
  SHA-256 94B164C3788A90F8C8694AF6020C19EA911B608802F5C7557BF39604FDB1FC6D. Pages
  deployment for eb52980.
- Run 35899151315 (d20178f): all three jobs PASS. Released `preview-2026.9.2302.0`, MSIX
  SHA-256 B2DF884DF0E911C82980739D690AA467DC40B37C7B804662832B5F979051BADF. The feed
  advanced to 2026.9.2302.0.
- Each release has these assets: `AiUsage.appinstaller`, `AiUsage.Development.cer`, the
  versioned MSIX, `Microsoft.WindowsAppRuntime.2.msix` and `release.json`. The MSIX URL
  redirects over HTTPS to GitHub release storage.
- Deployed feed at 2026.9.2301.0 and then 2026.9.2302.0: MainPackage `AiUsage.Dev` /
  `CN=AI Usage Development` / x64, `OnLaunch HoursBetweenUpdateChecks=0 ShowPrompt=false
  UpdateBlocksActivation=false`, `AutomaticBackgroundTask`, `ForceUpdateFromAnyVersion`
  false. The CER served from Pages has SHA-1 B4C73392759C80CA5D1AA4004486B6C957417609.

## Windows Sandbox feed update

Harness: `tools/windows/Invoke-FeedUpdateSmoke.ps1` plus the explicit
`AppInstallerActivationPreservesPreferences` UI test. The guest had networking enabled so
it could reach the feed. Only these were mapped: the release CER, the official .NET
10.0.12 runtime installer, the published smoke suite (read-only) and an empty evidence
folder. No personal state was mapped. Evidence is kept locally and ignored:
`.ai-usage-local/AIU-014/feed-run/`.

- Attempt 1 FAIL before any guest change: Windows PowerShell 5.1 returned the
  `application/appinstaller` response as bytes. The harness now decodes the raw stream.
  A fresh guest was used.
- Install PASS, 2026-09-23T17:50–17:57Z. The steps were:
  - Verify the CER thumbprint and trust it only in the guest.
  - Verify the runtime installer's Microsoft signature, then install the runtime.
  - Run `Add-AppxPackage -AppInstallerFile https://rozputnii.github.io/ai-usage-app/AiUsage.appinstaller`.

  The installed package was 2026.9.2301.0, family AiUsage.Dev_951d0pt9hnds0,
  SignatureKind Developer, with its AppInstaller URI set to the Pages feed.
  `Get-AppxPackageAutoUpdateSettings` reported CheckForUpdatesOnLaunch True,
  ShowPrompt False, UpdateBlocksActivation False, AutomaticBackgroundTaskUpdatesEnabled
  True, ForceUpdateFromAnyVersion False and HoursBetweenUpdateChecks 0. The
  synthetic Dark preference was seeded before first launch, and the UI test (1 passed)
  confirmed Dark in the 2026.9.2301.0 process.
- Update PASS, 2026-09-23T18:04:52–18:05:26Z, after the feed advanced:
  - The UI test launched the 2026.9.2301.0 process path, so the update did not block
    launch. `LastCheckedForUpdates` advanced to 18:05:04Z.
  - The app exited normally. Windows installed 2026.9.2302.0 by 18:05:19Z, keeping the
    same family and feed URI.
  - All eight LocalState file hashes were unchanged across the update.
  - The UI test (1 passed) confirmed Dark in the 2026.9.2302.0 process path. Screenshots
    were inspected.

## Local checks

- `tests/release/Test-PreviewRelease.ps1` RED before the fix: the runner-trust store
  policy was missing. GREEN after: 29 assertions. PowerShell 7 is not installed on this
  host, so these local runs used Windows PowerShell 5.1. The hosted validate job ran it
  under PowerShell 7 and passed. The regression checks, without opening any certificate
  store, that trust targets LocalMachine\Root and that the guard rejects other jobs with
  its own message. It also checks that the publication script no longer contains the old
  TrustedPeople/Import-Certificate trust and that trust is removed in `finally`. The
  certificate-rejection branches are covered only by review, not by execution.
- AST syntax parsing of tools/windows and tests/release: PASS. The smoke test project
  builds with 0 warnings, and it now contains UpdateSmoke.cs, which the previous session
  left uncommitted.
- Document validation and `git diff --check` pass on the final records.

## Limits

- The eight-hour background task is registered (`AutomaticBackgroundTaskUpdatesEnabled`)
  but was not observed firing. Only the launch-triggered update was observed.
- The guest installed with `Add-AppxPackage -AppInstallerFile`, which uses the same feed
  registration. The browser/App Installer UI flow described for testers was not
  exercised.
- Assets are immutable because of workflow policy (fresh versions, no `--clobber`), not
  because of the platform. GitHub reports these releases `immutable: false`, so an actor
  with contents write access could still replace an asset. Enabling immutable releases
  is a repository setting outside this change.
- The late-older-source guard is proven locally against real Git ancestry. The hosted
  runs exercised only the forward path.
- Each main push publishes one Preview, including documentation-only pushes.
- Certificate expiry and rotation (2028-09-22), official signing, Stable and public
  distribution remain deferred.
