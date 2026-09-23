# AI Usage

Windows-first native WinUI/.NET 10 subscription-quota dashboard with Codex and Claude integrations. Shared provider libraries handle connection, refresh and disconnect; app-owned grants are protected with DPAPI CurrentUser. The dashboard displays quota and explicitly stale cached readings, with close-to-tray, restoration and explicit Exit.

[AIU-002](docs/specs/AIU-002-windows-msix/verification.md) records clean-guest installation and offline UI evidence; [AIU-003](docs/specs/AIU-003-codex-console/verification.md) records real Codex console sign-in, quota and in-memory refresh; [AIU-004](docs/specs/AIU-004-codex-dashboard/verification.md) records Codex dashboard/storage and guest UI/tray evidence. [CR-AIU-004-01](docs/specs/AIU-004-codex-dashboard/close-to-tray-verification.md) verifies close-to-tray, restoration and explicit Exit. [AIU-027](docs/specs/AIU-027-architecture-refinement/verification.md) records workflow and presentation boundaries. [AIU-007](docs/specs/AIU-007-claude-integration/verification.md) records Claude regressions, packaged Windows checks and owner-led live connection, refresh, renewal, resume and disconnect. Provider-specific limitations remain in those records: Claude is a private, unsupported integration, and successful testing does not establish provider approval or complete lifecycle coverage.

## Development prerequisites

- Git and the .NET SDK selected by [global.json](global.json), currently 10.0.401.
- Existing restored packages for offline checks; SDK installation or network restoration needs separate authorization.
- Windows for DPAPI tests, and the additional Windows tooling below for package builds.

AI clients and plugins are optional. Start with [AGENTS](AGENTS.md) and [CONTRIBUTING](CONTRIBUTING.md). Consult the [backlog](docs/backlog.md) for current feature status; no next feature starts automatically. The retired runtime is historical evidence only.

## Local checks

Select commands using the [change-based verification matrix](docs/workflow/verification.md#checks-by-change). Use the SDK selected by global.json. If it is not on PATH, the existing user-local PowerShell form replaces `dotnet` with `& "$HOME/.dotnet/ai-usage-sdk/dotnet.exe"`.

```powershell
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo
dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo
git diff --check
```

A missing restored asset is BLOCKED offline; do not silently enable network restoration. The validator returns 0 for valid documents, 1 for diagnostics and 2 for invocation/read failures. It makes no network or model calls. Optional local formatting checks use `dotnet format <project.csproj> --no-restore --verify-no-changes` on the two validator projects.

CI runs validator/document checks and deterministic product regressions on Windows, plus unsigned MSIX/routing builds and smoke-harness publication. Those builds do not prove interactive UI execution. Review requirements and Git authority are owned by CONTRIBUTING.

## Local Windows run/debug

Use the local unpackaged app for routine development and debugger attachment, following the [development environment policy](docs/workflow/verification.md#development-environment):

```powershell
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore
& ./src/windows/AiUsage.Windows/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AiUsage.exe
```

The Antigravity provider ships without an OAuth client registration, because the inspected one belongs to another project and reuse permission is unknown. Set `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET` in the session that starts the app to supply one; without them that provider reports an unconfigured registration and contacts nothing. Google's published terms restrict third-party access to Antigravity; see [the provider record](docs/providers/antigravity.md) before connecting.

Product startup uses live adapters. Pass `--demo` for the isolated synthetic frontend. Unpackaged product data lives under `%LOCALAPPDATA%/AiUsage/Development`; `AIU_DEVELOPMENT_STATE_DIRECTORY` can select an empty directory for offline development checks. Packaged startup keeps its existing package-local provider store. Never point smoke checks at a credential-bearing directory without authorization.

AIU-006 adds an exclusive startup lease and layout manifest. On the first upgrade,
`appearance.v1.json` moves to `preferences/appearance.v1.json`, preserving its contents.
A DPAPI CurrentUser checkpoint covers presentation preferences only; provider credentials
retain their existing location and rotation journal and are never rolled back by restore.
An interrupted migration opens the recovery screen before provider services start.
Retry completes the pending operation; confirmed Restore replaces preferences from the
verified checkpoint. Newer layouts refuse downgrade. Recovery diagnostics export contains
fixed status fields only and is saved as `recovery-diagnostics.txt` in the owned data folder.

History opens with automatic loading for connected accounts, or one selected account from
its card. Codex analytics and Copilot personal billing reports use existing AI Usage
sessions; availability depends on the provider, plan and current permissions. Claude and
Antigravity currently show unsupported history. Results remain in memory and preserve
provider units and aggregate periods. See [AIU-011 verification](docs/specs/AIU-011-provider-history/verification.md)
for the distinction between fixture/UI success and unverified real-account access.

For an authorized live check, `dotnet run --project tools/AiUsage.ProviderConsole --no-restore -- history codex <owned-provider-directory>`
(or `copilot`) reads the last seven UTC days through the product session and prints only
report statuses/counts. Select the existing AI Usage **provider directory**, never a CLI
credential directory. The command cannot sign in or request extra permissions; Codex may
renew and persist its existing grant. No credentials belong in command arguments.

For local interactive smoke, set `AIU_SMOKE_EXE` to the absolute path of that executable and `AIU_SMOKE_EVIDENCE_DIRECTORY` to a fresh local evidence directory. Set `AIU_SMOKE_MODE=demo` to verify the demo path; the default smoke mode is product with an empty `AIU_DEVELOPMENT_STATE_DIRECTORY`. An unlocked interactive desktop is required. Reserve Windows Sandbox or a disposable VM for checks that need isolation or a clean machine; package builds alone do not need either.

## Native Windows package

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

`tools/windows/Invoke-PackageSmoke.ps1` is a disposable-guest harness, not a host installer. It requires package, public CER, official offline dependencies, .NET runtime installer, published smoke executable and empty evidence directory. It changes trust only inside Sandbox or a disposable VM explicitly confirmed with `-ConfirmDisposableGuest`; an inherited environment variable does not authorize it. Retained AIU-002 and AIU-004 evidence records successful disposable-guest runs and inspected screenshots.

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

The owner-selected AIU-014 test channel uses a dedicated self-signed CI certificate.
It is for explicitly trusted test devices, not public distribution. Provisioning is
separate from ordinary build/push authority: review
`tools/windows/Initialize-PreviewSigning.ps1` before explicitly running it with `-Apply`
in PowerShell 7. It creates a new in-memory key, sends a password-protected PFX and its
random password to `AIU_CI_PFX_BASE64` / `AIU_CI_PFX_PASSWORD` Actions secrets, enables
GitHub Pages, then enables `AIU_PREVIEW_ENABLED`. It never exports the existing local
key or changes host certificate trust. Existing secrets/setup evidence stop a repeat;
partial setup must be inspected, not overwritten or rotated automatically.

Once enabled, successful main-push validation publishes distinct development
prereleases and deploys `https://rozputnii.github.io/ai-usage-app/AiUsage.appinstaller`.
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
launch or forcing restart. Exit from the tray menu to allow an update; closing the
window just hides it. The in-app Updates page currently reports externally managed
updates; it does not implement a separate downloader or Stable channel switch.

Unpackaged development runs use separate data and do not update through this channel.
Nothing copies provider credentials into the installed app. The test certificate
expires after two years; renewal needs a deliberate trust/rotation procedure. Stable,
publicly trusted signing and automatic release pruning remain deferred. Current
operational evidence: [AIU-014 verification](docs/specs/AIU-014-preview-updates/verification.md).

## Project records

- [Goals](docs/product/goals.md), [backlog](docs/backlog.md) and [accepted decisions](docs/decisions/accepted.md).
- [Environment history](docs/workflow/environment.md) and [security reporting](SECURITY.md).
- [Historical bootstrap evidence](docs/specs/AIU-001-omp-bootstrap/verification.md).

Consult each feature's verification record for observed CI, interactive and live-provider results. Git publication and integration authority are defined in CONTRIBUTING.md; host installation, trust changes and releases require their applicable authorization.
