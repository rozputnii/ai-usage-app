# AI Usage

Windows-first native WinUI/.NET 10 subscription-quota dashboard. The delivered slice connects one Codex account through the shared provider library, protects its grant with DPAPI CurrentUser, supports refresh/disconnect, displays an explicitly stale cached reading and provides tray Open/Exit.

[AIU-002](docs/specs/AIU-002-windows-msix/verification.md) records clean-guest installation and offline UI evidence; [AIU-003](docs/specs/AIU-003-codex-console/verification.md) records real console sign-in, quota and in-memory refresh; [AIU-004](docs/specs/AIU-004-codex-dashboard/verification.md) records deterministic dashboard/storage checks and guest UI/tray evidence. Packaged live sign-in, real-grant resume and close-to-tray remain unverified by this migration. Public-client reuse permission and broader provider lifecycle cases remain unresolved as recorded in the evidence.

## Development prerequisites

- Git and the .NET SDK selected by [global.json](global.json), currently 10.0.401.
- Existing restored packages for offline checks; SDK installation or network restoration needs separate authorization.
- Windows for DPAPI tests, and the additional Windows tooling below for package builds.

AI clients and plugins are optional. Start with [AGENTS](AGENTS.md) and [CONTRIBUTING](CONTRIBUTING.md). No next feature starts automatically; G-002 remains the direction and AIU-002/003/004 are recorded complete. The retired runtime is historical evidence only.

## Local checks

Use the SDK selected by global.json. If it is not on PATH, the existing user-local PowerShell form replaces `dotnet` with `& "$HOME/.dotnet/ai-usage-sdk/dotnet.exe"`.

```powershell
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo
git diff --check
```

A missing restored asset is BLOCKED offline; do not silently enable network restoration. The validator returns 0 for valid documents, 1 for diagnostics and 2 for invocation/read failures. It makes no network or model calls. Optional local formatting checks use `dotnet format <project.csproj> --no-restore --verify-no-changes` on the two validator projects.

CI runs validator/document checks and deterministic product regressions on Windows, plus unsigned MSIX/routing builds and smoke-harness publication. Those builds do not prove interactive UI execution. Review requirements and Git authority are owned by CONTRIBUTING.

## Native Windows package

Requires Windows 11 24H2+ x64, the selected .NET SDK and Visual Studio MSBuild. Pinned NuGet build tools supply XAML/MSIX tooling in the verified local environment. Core and Infrastructure remain platform-neutral .NET libraries.

From the repository root, in PowerShell:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
./tools/windows/Build-Package.ps1 -MsixVersion 2026.9.1306.0
```

This command was exercised with earlier reserved versions. Choose a fresh UTC `YYYY.M.DDNN.0` version, counter 01..99, for changed installable bytes; existing output is rejected without overwrite. Without `-CertificateThumbprint`, output is explicitly unsigned-validation-only. Signing requires an exact owned CurrentUser/My development code-signing certificate. No host trust is installed: verification fails closed if the self-signed root is untrusted, preserving bytes and public CER for guest-only verification. Never export the private key.

The separate executable UI suite publishes with `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true`. It requires an installed `AIU_SMOKE_AUMID`, an unlocked interactive desktop and `AIU_SMOKE_EVIDENCE_DIRECTORY`; missing prerequisites fail, never silently skip. Do not run it as part of platform-neutral checks.

`tools/windows/Invoke-PackageSmoke.ps1` is a disposable-guest harness, not a host installer. It requires package, public CER, official offline dependencies, .NET runtime installer, published smoke executable and empty evidence directory. It changes trust only inside Sandbox or a disposable VM explicitly confirmed with `-ConfirmDisposableGuest`; an inherited environment variable does not authorize it. Retained AIU-002 and AIU-004 evidence records successful disposable-guest runs and inspected screenshots. This migration does not repeat those runs.

## Project state

- [Goals](docs/product/goals.md), [backlog](docs/backlog.md) and [accepted decisions](docs/decisions/accepted.md).
- [Environment history](docs/workflow/environment.md) and [security reporting](SECURITY.md).
- [Historical bootstrap evidence](docs/specs/AIU-001-omp-bootstrap/verification.md).

Remote CI and other AI-client adapter loading are NOT_RUN for this migration. No push, merge, release, host installation or trust change is implied.
