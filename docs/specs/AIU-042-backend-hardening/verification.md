# AIU-042 verification

Execution selected by the owner's 2026-10-02 Goal instruction. Baseline code: 66e3eb8.
Initial Git state: clean main tracking origin/main; no unrelated changes found.

| Criteria | Result | Evidence |
| --- | --- | --- |
| AC-01 | NOT_RUN | Baseline below passes; complete subsystem inventory is in progress. |
| AC-02 to AC-08 | NOT_RUN | Remediation, measurements and integrated verification are pending. |

## Baseline checks

2026-10-02, local Windows, pinned stable SDK 10.0.401, Release, warnings as errors.
`dotnet` below is the existing user-local `~/.dotnet/ai-usage-sdk/dotnet.exe`.

| Check | Result | Observation |
| --- | --- | --- |
| `dotnet --version` | PASS | 10.0.401 |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 416 tests, 0 failed/skipped/not run; 14.947 s test execution. |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 179 tests, 0 failed/skipped/not run; 0.701 s test execution. |
| Live provider / interactive UI | NOT_RUN | Baseline uses synthetic fixtures and isolated stores; no live or UI claim. |

## Review coverage

In progress. Each completed row will identify reviewed files, relevant failure boundaries,
and disposition. Baseline tests alone do not establish whole-backend audit coverage.
