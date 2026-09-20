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

The separate executable UI suite publishes with `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true`. It accepts `AIU_SMOKE_EXE` for local unpackaged checks or an installed `AIU_SMOKE_AUMID` for packaged checks, and requires an unlocked interactive desktop and `AIU_SMOKE_EVIDENCE_DIRECTORY`; missing prerequisites fail, never silently skip. Do not run it as part of platform-neutral checks.

`tools/windows/Invoke-PackageSmoke.ps1` is a disposable-guest harness, not a host installer. It requires package, public CER, official offline dependencies, .NET runtime installer, published smoke executable and empty evidence directory. It changes trust only inside Sandbox or a disposable VM explicitly confirmed with `-ConfirmDisposableGuest`; an inherited environment variable does not authorize it. Retained AIU-002 and AIU-004 evidence records successful disposable-guest runs and inspected screenshots.

The default `-VerificationMode ProductUi` provisions the offline dependencies before the first app activation, so ordinary UI checks do not show missing-runtime dialogs. Missing-prerequisite negative checks are reported as NOT_RUN. Use `-VerificationMode InstallationContract` explicitly when testing installation failures; that mode deliberately activates without the runtime and can display the native missing-runtime dialog. ProductUi success does not claim the installation-negative contract passed.

## Project state

- [Goals](docs/product/goals.md), [backlog](docs/backlog.md) and [accepted decisions](docs/decisions/accepted.md).
- [Environment history](docs/workflow/environment.md) and [security reporting](SECURITY.md).
- [Historical bootstrap evidence](docs/specs/AIU-001-omp-bootstrap/verification.md).

Consult each feature's verification record for observed CI, interactive and live-provider results. Git publication and integration authority are defined in CONTRIBUTING.md; host installation, trust changes and releases require their applicable authorization.
