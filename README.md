# AI Usage

Windows-first AI usage application, currently at the development-workflow bootstrap. There is no runnable Windows product package yet; [AIU-002](docs/backlog.md) is the next package milestone.

## Development prerequisites

- Git and Bun; the bootstrap was exercised with Git 2.55.0 and Bun 1.4.2.
- .NET SDK selected by [global.json](global.json), currently 10.0.401.
- Current stable Oh My Pi (OMP); the native integration was verified against 18.1.18.
- An owner-configured, authenticated local OMP profile named `ai-usage`, including primary/task and different-family advisor/review model roles. Authentication and concrete role mappings do not belong in this repository.

Review `.omp` and the launch script before running an unfamiliar checkout: OMP loads project extensions as executable code. This is a trusted-checkout workflow, not a sandbox.

## Start work

From the repository root:

```text
bun tools/start-work.ts
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
```

The validator returns 0 for valid documents, 1 for diagnostics and 2 for invocation/read failures. It performs no inference or network access; initial SDK/package restoration is a separate prerequisite. The extension checks `DOTNET_ROOT`, the optional user-local `.dotnet/ai-usage-sdk` installation, then PATH.

## Project state

- [Goals](docs/product/goals.md) and [single authoritative backlog](docs/backlog.md)
- [Accepted decisions](docs/decisions/accepted.md)
- [AIU-001 verification](docs/specs/AIU-001-omp-bootstrap/verification.md)
- [Environment evidence and limitations](docs/workflow/environment.md)
- [Contributing](CONTRIBUTING.md), [security reporting](SECURITY.md), [MIT license](LICENSE)

No remote push, PR, merge, protected release or provider-product integration is claimed by this local bootstrap. GitHub main protection and private vulnerability reporting were absent at preflight; publishing the local CI workflow does not configure either.
