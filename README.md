# AI Usage

Windows-first native WinUI/.NET 10 subscription-quota dashboard with Codex and Claude integrations. Shared provider libraries handle connection, refresh and disconnect; app-owned grants are protected with DPAPI CurrentUser. The dashboard displays quota and explicitly stale cached readings, with close-to-tray, restoration and explicit Exit.

[T-002](docs/specs/T-002-windows-msix/verification.md) records clean-guest installation and offline UI evidence; [T-003](docs/specs/T-003-codex-console/verification.md) records real Codex console sign-in, quota and in-memory refresh; [T-004](docs/specs/T-004-codex-dashboard/verification.md) records Codex dashboard/storage and guest UI/tray evidence. [CR-T-004-01](docs/specs/T-004-codex-dashboard/close-to-tray-verification.md) verifies close-to-tray, restoration and explicit Exit. [T-027](docs/specs/T-027-architecture-refinement/verification.md) records workflow and presentation boundaries. [T-007](docs/specs/T-007-claude-integration/verification.md) records Claude regressions, packaged Windows checks and owner-led live connection, refresh, renewal, resume and disconnect. Provider-specific limitations remain in those records: Claude is a private, unsupported integration, and successful testing does not establish provider approval or complete lifecycle coverage.

## Development prerequisites

- Git and the .NET SDK selected by [global.json](global.json), currently 10.0.401.
- Existing restored packages for offline checks; SDK installation or network restoration needs separate authorization.
- Windows for DPAPI tests, and the additional Windows tooling below for package builds.

AI clients and plugins are optional. Start with [AGENTS](AGENTS.md) and [CONTRIBUTING](CONTRIBUTING.md). Consult the [backlog](docs/backlog.md) for current feature status; no next feature starts automatically. The retired runtime is historical evidence only.

## Local checks

The check commands, their IDs and the [change-based matrix](docs/workflow/verification.md#checks-by-change) that selects them are in the [verification policy](docs/workflow/verification.md#checks). A missing restored asset is BLOCKED offline; do not silently enable network restoration. Optional local formatting checks use `dotnet format <project.csproj> --no-restore --verify-no-changes` on the two validator projects.

CI runs validator/document checks and deterministic product regressions on Windows, plus unsigned MSIX builds and smoke-harness publication. Those builds do not prove interactive UI execution. Review requirements and Git authority are owned by CONTRIBUTING.

## Local Windows run/debug

Use the local unpackaged app for routine development and debugger attachment, following the [development environment policy](docs/workflow/verification.md#development-environment):

```powershell
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore
& ./src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe
```

The Antigravity provider ships without an OAuth client registration, because the inspected one belongs to another project and reuse permission is unknown. Set `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET` in the session that starts the app to supply one; without them that provider reports an unconfigured registration and contacts nothing. Google's published terms restrict third-party access to Antigravity; see [the provider record](docs/providers/antigravity.md) before connecting.

Product startup uses live adapters. Pass `--demo` for the isolated synthetic frontend. Unpackaged product data lives under `%LOCALAPPDATA%/AiUsage/Development`; `AIU_DEVELOPMENT_STATE_DIRECTORY` can select an empty directory for offline development checks. Packaged startup keeps its existing package-local provider store. Never point smoke checks at a credential-bearing directory without authorization.

T-006 adds an exclusive startup lease and layout manifest. On the first upgrade,
`appearance.v1.json` moves to `preferences/appearance.v1.json`, preserving its contents.
A DPAPI CurrentUser checkpoint covers presentation preferences only; provider credentials
retain their existing location and rotation journal and are never rolled back by restore.
An interrupted migration opens the recovery screen before provider services start.
Retry completes the pending operation; confirmed Restore replaces preferences from the
verified checkpoint. Newer layouts refuse downgrade. Recovery diagnostics export contains
fixed status fields only and is saved as `recovery-diagnostics.txt` in the owned data folder.

T-036 records normalized local observations after successful live quota refreshes under
the separate `budget` directory. It keeps at least 35 days for daily budgets and five-hour
session estimates; the new display is connected in T-039. Cached startup readings are not
new observations, and sign-out preserves this history. Work days and personal caps have a
separate versioned configuration file. Corrupt or unsupported files are retained as recovery
copies and tracking restarts; estimates need fresh samples again. Storage capacity failure is
reported without discarding retained readings. This item supplies cleanup for these stores;
the currently unavailable product delete-data/factory-reset buttons are not enabled by it.

T-037 adds native normalized limit facts and forward quota-storage v2 migrations.
Compatible existing reading keys continue through an explicit alias; ambiguous old
scoped histories remain separate. Protected migrations preserve grants and generation
identities and retain encrypted v1 checkpoints, without automatic grant rollback.
The existing presentation continues to use its compatibility fields until T-039.
T-037's local checks pass, but its required independent security review is currently
blocked; see [verification and handoff](docs/specs/T-037-provider-limits-v2/verification.md).

Ledger's inline history uses only local quota readings recorded by T-036. T-040
removes the former provider-history clients and console command under R-184. Opening
history makes no provider-history request; existing observations remain available.
See [T-040 verification](docs/specs/T-040-remove-provider-history/verification.md).

For local interactive smoke, set `AIU_SMOKE_EXE` to the absolute path of that executable and `AIU_SMOKE_EVIDENCE_DIRECTORY` to a fresh local evidence directory. The launch smoke covers the demo path and the product path with an empty `AIU_DEVELOPMENT_STATE_DIRECTORY`. An unlocked interactive desktop is required. Reserve Windows Sandbox or a disposable VM for checks that need isolation or a clean machine; package builds alone do not need either.

## Native Windows package

Local structured logging, sanitized provider evidence, retention and diagnostic switches
are described in the [logging guide](docs/workflow/logging.md). Use **Settings → System
status → Open logs** for the current mode's folder. Provider bodies are sanitized before
they reach files; Debug tracing is opt-in. Logs are local and excluded from backups/Git.

Requires Windows 11 24H2+ x64, the selected .NET SDK and Visual Studio MSBuild. Pinned NuGet build tools supply XAML/MSIX tooling in the verified local environment. Core and Infrastructure remain platform-neutral .NET libraries.

From the repository root, in PowerShell:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
./tools/windows/Build-Package.ps1 -MsixVersion 2026.9.1306.0
```

This command was exercised with earlier reserved versions. Choose a fresh UTC `YYYY.M.DDNN.0` version, counter 01..99, for changed installable bytes; existing output is rejected without overwrite. Without `-CertificateThumbprint`, output is explicitly unsigned-validation-only. Signing requires an exact owned CurrentUser/My development code-signing certificate. No host trust is installed: verification fails closed if the self-signed root is untrusted, preserving bytes and public CER for guest-only verification. Never export the private key.

Pass `-NoRestore` when using existing restored assets without network restoration.
`tools/windows/Invoke-UpgradeSmoke.ps1` is a separate Windows Sandbox-only upgrade harness.
Its input directory contains `old.msix`, `new.msix`, the matching public
`AiUsage.Development.cer`, official offline `dependencies`, the existing .NET 10 runtime
installer and the published `smoke` suite. With `-InputDirectory` and an empty
`-EvidenceDirectory`, it installs the old package, seeds guest-only synthetic state,
checks the old UI, updates in place and exercises real recovery UI through a deliberate
filesystem sharing failure and process termination. Use a network-disabled Sandbox with
only those artifacts mapped read-only and an empty evidence folder mapped writable.

The separate executable UI suite publishes with `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true`. It accepts `AIU_SMOKE_EXE` for local unpackaged checks or an installed `AIU_SMOKE_AUMID` for packaged checks, and requires an unlocked interactive desktop and `AIU_SMOKE_EVIDENCE_DIRECTORY`; missing prerequisites fail, never silently skip. Do not run it as part of platform-neutral checks.

`tools/windows/Invoke-PackageSmoke.ps1` is a disposable-guest harness, not a host installer. It requires package, public CER, official offline dependencies, .NET runtime installer, published smoke executable and empty evidence directory. It changes trust only inside Sandbox or a disposable VM explicitly confirmed with `-ConfirmDisposableGuest`; an inherited environment variable does not authorize it. Retained T-002 and T-004 evidence records successful disposable-guest runs and inspected screenshots.

The default `-VerificationMode ProductUi` provisions the offline dependencies before the first app activation, so ordinary UI checks do not show missing-runtime dialogs. Missing-prerequisite negative checks are reported as NOT_RUN. Use `-VerificationMode InstallationContract` explicitly when testing installation failures; that mode deliberately activates without the runtime and can display the native missing-runtime dialog. ProductUi success does not claim the installation-negative contract passed.

`tools/windows/Invoke-FeedUpdateSmoke.ps1` is a Windows Sandbox-only harness for the
development Preview feed. Its `-Phase Install` step checks the public CER thumbprint and
trusts the CER only in the guest. It installs the runtime and the app through the
published `.appinstaller`, then seeds a synthetic Dark preference. Its `-Phase Update`
step runs after the feed advances: it launches the installed version, waits for the
Windows-managed update, then checks the package family, unchanged LocalState bytes and
the preference via the explicit `AppInstallerActivationPreservesPreferences` UI test.
The guest needs networking to reach the feed.

## Development Preview updates

The owner-selected T-014 test channel uses a dedicated self-signed CI certificate.
It is for the owner and anyone who explicitly chooses to trust its public certificate;
publicly trusted signing is deferred. Provisioning is
separate from ordinary build/push authority: review
`tools/windows/Initialize-PreviewSigning.ps1` before explicitly running it with `-Apply`
in PowerShell 7. It creates a new in-memory key, sends a password-protected PFX and its
random password to `AIU_CI_PFX_BASE64` / `AIU_CI_PFX_PASSWORD` Actions secrets, enables
GitHub Pages, then enables `AIU_PREVIEW_ENABLED`. It never exports the existing local
key or changes host certificate trust. Existing secrets/setup evidence stop a repeat;
partial setup must be inspected, not overwritten or rotated automatically.

A push to `main` publishes unless every path changed since the Preview the feed serves is
outside the product inputs (`docs/**`, `.claude/**`, `.agents/**` and root Markdown files);
the run's job summary records the decision and its reason. The run validates and packages
the pushed commit; only if both jobs pass does it publish a distinct development prerelease. It then replaces the feed
`https://github.com/rozputnii/ai-usage-app/releases/download/feed-preview/AiUsage.appinstaller`
and deploys the install page at `https://rozputnii.github.io/ai-usage-app/`. The owner
can republish the current commit with
`gh workflow run validation.yml --ref main -f PublishPreview=true`.
`AIU_PREVIEW_ENABLED` is the kill switch: unless it is `true`, no run publishes.

On the owner's machine, the IPv6 path to GitHub Pages resets TLS connections, and App
Installer failed with 0x80072EFE. Every measured download through `github.com` and
`release-assets.githubusercontent.com` succeeded; these hosts have no AAAA records, so
they are IPv4 only. For that reason:
- The package and Windows App SDK dependency URIs point at the immutable assets of the
  same GitHub release (ANL-12, 2026-10-06).
- Since 2026-10-07 the feed itself is on `github.com` too: App Installer fetches its
  address on install and on every update check. The moving `feed-preview` release holds
  only that file, and it is the only asset ever replaced.
- Pages keeps the install page, the public CER and a feed copy. Older installs follow
  that copy's new address after their next update.
- The install page links to the same release assets.
Draft releases reserve versions before building; a failure consumes its version. The
release queue retains up to 100 pending jobs. Only a candidate containing every prior
published source can update the feed; a late older source may publish an artifact but
cannot replace the feed. Published asset bytes are never overwritten. The UTC daily
counter has 99 slots; exhaustion or a backward clock stops publication explicitly.

For the first test-device installation, install the .NET 10 x64 runtime, explicitly
trust the published CER in Local Machine / Trusted People after verifying its
thumbprint, and open the `.appinstaller` link. Trust requires administrator consent.
Windows App SDK dependencies are referenced by the feed. Install through App Installer
to register the update source; directly installing an MSIX is not equivalent.
Windows checks on launch and every eight hours in the background without blocking
launch or forcing restart.

The in-app Updates section (T-046) also checks the same feed. You can switch it to
Off, On launch or Always (every 5 minutes). It installs with **Install and restart**, or
automatically while the window is closed to the tray, and Windows relaunches the app.
There is no separate downloader or Stable channel switch.

Unpackaged development runs use separate data and do not update through this channel.
Nothing copies provider credentials into the installed app. The test certificate
expires after two years; renewal needs a deliberate trust/rotation procedure. Stable,
publicly trusted signing and automatic release pruning remain deferred. Current
operational evidence: [T-014 verification](docs/specs/T-014-preview-updates/verification.md).

## Project records

- [Goals](docs/product/goals.md), [backlog](docs/backlog.md) and [accepted decisions](docs/decisions/accepted.md).
- [Environment history](docs/archive/workflow/environment.md) and [security reporting](SECURITY.md).
- [Historical bootstrap evidence](docs/specs/T-001-omp-bootstrap/verification.md).

Consult each feature's verification record for observed CI, interactive and live-provider results. Git publication and integration authority are defined in CONTRIBUTING.md; host installation, trust changes and releases require their applicable authorization.
