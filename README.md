# AI Usage

Windows-first AI usage application. The native WinUI/.NET 10 empty dashboard now builds and produces development MSIX candidates. AIU-002 remains incomplete until clean disposable-Windows installation, offline UI and exit proof; see [actual evidence](docs/specs/AIU-002-windows-msix/verification.md).

## Development prerequisites

- Git and Bun; the bootstrap was exercised with Git 2.55.0 and Bun 1.4.2.
- .NET SDK selected by [global.json](global.json), currently 10.0.401.
- Current stable Oh My Pi (OMP); bootstrap runtime verified against 18.1.18, project discovery/settings re-probed against 18.1.19.
- An owner-configured, authenticated local OMP profile named `ai-usage`, including primary/task and different-family advisor/review model roles. Authentication and concrete role mappings do not belong in this repository.

Review `.omp` and the launch script before running an unfamiliar checkout: OMP loads project extensions as executable code. This is a trusted-checkout workflow, not a sandbox.

## Start work

From the repository root:

```text
bun tools/start-work.ts --no-title --no-lsp
```

The launcher uses the native `omp --profile ai-usage` entrypoint. On Windows it removes MSYS/Cygwin runtime directories from the child process's PATH, avoiding an observed OMP 18.1.18 Unix `ps` hang during isolation setup. It does not change the machine PATH, install software or patch OMP. Native Git, Bun and .NET remain required; do not depend on inherited MSYS-only utilities in this launch environment.

A fresh session displays project state but does not infer permission from it. Work requires an identified `feature/AIU-...` branch and explicit native UI confirmation:

```text
/work status
/work select
/work run
/work pause
/work resume
/work verify
```

Native Plan mode starts active. The verified execution probes used `/plan` to pause it before running work; alternatively complete native plan review. Choose that native mode before starting automatic execution. `/work` does not bypass native Plan approvals.

`/work resume auto`, followed by `/work run`, explicitly permits internal fresh-session continuation within the displayed goal, scope and remaining budget. An external restart requires confirmation again. Budget units are guarded native tool-call events plus execution transitions, not money or tokens; native worker usage is reported separately. Pausing prevents primary integration but is not a guarantee that every subprocess has already terminated.

The primary inspects native returned patches, then uses `work_checkpoint` or `/work integrate T-01 <patch-path>`. Integration is not completion. Completion requires canonical task evidence, validation and the bounded review policy. See [native workflow behavior and limits](docs/workflow/omp-native.md).

## Local checks

These snippets assume the selected SDK is on PATH. The bootstrap's optional user-local SDK was deliberately not added to PATH. In that setup, the verified PowerShell form replaces `dotnet` with `& "$HOME/.dotnet/ai-usage-sdk/dotnet.exe"`; the OMP extension locates it automatically.

```text
dotnet run --project tests/AiUsage.ProjectValidation.Tests
dotnet run --project tools/AiUsage.ProjectValidation -- --root . --json
bun test tests/omp-workflow
dotnet format tests/AiUsage.ProjectValidation.Tests/AiUsage.ProjectValidation.Tests.csproj --no-restore --verify-no-changes
dotnet format tools/AiUsage.ProjectValidation/AiUsage.ProjectValidation.csproj --no-restore --verify-no-changes
```

The validator returns 0 for valid documents, 1 for diagnostics and 2 for invocation/read failures. It performs no inference or network access; initial SDK/package restoration is a separate prerequisite. The extension checks `DOTNET_ROOT`, the optional user-local `.dotnet/ai-usage-sdk` installation, then PATH.

Current PR CI runs the test/validator commands above plus an independent unsigned Windows MSIX build and smoke-harness publish, not desktop UI tests or formatting. Obsolete runs of the same PR are cancelled. Ordinary PRs do not need a full independent review; see [current review policy](CONTRIBUTING.md#review-and-integration). The owner deferred main protection at low priority in AIU-026; its absence is not a current development PR prerequisite.

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

`tools/windows/Invoke-PackageSmoke.ps1` is a disposable-guest harness, not a host installer. It requires package, public CER, official offline dependencies, .NET runtime installer, published smoke executable and empty evidence directory. It changes trust only inside Sandbox or a disposable VM explicitly confirmed with `-ConfirmDisposableGuest`; an inherited environment variable does not authorize it. The local feature evidence records the ready offline bundle and missing guest prerequisite; no successful guest run or screenshot is claimed.

## Project state

- [Goals](docs/product/goals.md) and [single authoritative backlog](docs/backlog.md)
- [Accepted decisions](docs/decisions/accepted.md)
- [AIU-001 verification](docs/specs/AIU-001-omp-bootstrap/verification.md)
- [Environment evidence and limitations](docs/workflow/environment.md)
- [Contributing](CONTRIBUTING.md), [security reporting](SECURITY.md), [MIT license](LICENSE)

No remote push, PR, merge, protected release or provider-product integration is claimed by this local bootstrap. GitHub main protection and private vulnerability reporting were absent at preflight; publishing the local CI workflow does not configure either.
