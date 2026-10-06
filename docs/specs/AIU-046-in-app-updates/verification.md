# AIU-046 verification

Started 2026-10-06 on branch `users/updates-check-unavailable-f5396f`. Checks use
synthetic state only: no provider sign-in, credential or host install. Guest packages were
signed with the owner's local development certificate (`771CB0E8…E774`) and trusted only
inside disposable Windows Sandbox guests (LocalMachine\TrustedPeople). Their versions
were never published.

## T-01 spike: App Installer API, capability and relaunch (2026-10-06)

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
