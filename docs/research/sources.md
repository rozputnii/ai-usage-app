# Source records

Retrieved: 2026-09-12. Public source/code inspection only; no authenticated provider experiment and no local Windows/OMP execution in this handoff.

These sources support specific technical cautions, not every product decision. Preserve URLs and refs for OMP revalidation. Short summaries are paraphrases. This English revision translates the original evidence record; it does not claim a new live check of every source.

## S-001 — OMP release snapshot
- source: `https://github.com/can1357/oh-my-pi/releases/tag/v18.1.18`
- observation: The original release API inspection returned non-prerelease v18.1.18, published 2026-09-11T21:43:45Z. This is a research snapshot, not a permanent version pin.

## S-002 — OMP settings
- source: `https://github.com/can1357/oh-my-pi/blob/v18.1.18/docs/settings.md`
- observation: Project configuration is read from cwd/.omp; configuration CLI writes normally target the global/profile file; overlays and runtime settings have higher precedence.

## S-003 — OMP memory
- source: `https://github.com/can1357/oh-my-pi/blob/v18.1.18/docs/memory.md`
- observation: Local summary pipeline consumes persisted sessions through model roles; default min idle 12h; memory is heuristic. Learn requires autolearn.

## S-004 — OMP task semantics
- source: `https://github.com/can1357/oh-my-pi/blob/v18.1.18/docs/tools/task.md`
- observation: Batch/isolated tasks, artifacts, no whole conversation inheritance; failed patch stays artifact; native task is not a project DAG scheduler.

## S-005 — OMP permission boundary
- source: `https://github.com/can1357/oh-my-pi/blob/v18.1.18/docs/approval-mode.md`
- observation: YOLO no interactive approval; tool policies not containment, eval can execute commands outside bash pattern gate.

## S-006 — OMP commands
- source: `https://github.com/can1357/oh-my-pi/blob/v18.1.18/docs/slash-command-internals.md`
- observation: Native .omp/commands paths and built-in precedence; Markdown commands are expansion, not automatic project runtime.

## S-007 — OMP goal resume implementation
- source: `https://github.com/can1357/oh-my-pi/blob/v18.1.18/packages/coding-agent/src/goals/runtime.ts`
- observation: onThreadResumed can pause active goal; active scope/accounting must not be assumed to auto-transfer to arbitrary fresh session.

## S-008 — OMP extension runtime
- source: `https://github.com/can1357/oh-my-pi/blob/v18.1.18/docs/extensions.md`
- observation: TS/JS factories register handlers/tools/commands; runtime actions after initialization, not at import time.

## S-009 — EF SQLite limitations
- source: `https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations`
- observation: DateTimeOffset comparison/order restrictions, table rebuilds, possible abandoned __EFMigrationsLock.

## S-010 — OAuth security BCP
- source: `https://www.rfc-editor.org/info/rfc9700/`
- observation: Refresh token rotation invalidates prior tokens; copying a rotating grant does not establish independent session lifecycles.

## S-011 — SQLite async limitations
- source: `https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async`
- observation: Async ADO.NET methods execute synchronously; do not assume await alone prevents UI blocking.

## S-012 — SQLite consistent backup
- source: `https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup`
- observation: SqliteConnection.BackupDatabase supports online consistent backups; coordinated writes still matter.

## S-013 — Artifact Signing eligibility
- source: `https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart`
- observation: Public individual validation currently limited to USA/Canada; organization and billing identity eligibility requires actual verification.

## S-014 — Independent deployment choices
- source: `https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps`
- observation: WindowsAppSDKSelfContained and .NET publishing are distinct. Do not assume a Windows framework MSIX provides the .NET runtime.

## S-015 — HTTP resilience
- source: `https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience`
- observation: Standard handler includes limiter and retries all HTTP methods by default; custom provider/operation pipelines required for auth safety.

## S-016 — DPAPI
- source: `https://learn.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection`
- observation: CurrentUser ties decryption to user identity; not an app-only vault or protection from same-user malware.

## S-017 — Scheduled Actions behavior
- source: `https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows`
- observation: Schedules can be delayed/dropped; inactive public repositories can have schedules disabled after 60 days.

## S-018 — Uno navigation
- source: `https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Navigation/NavigationOverview.html`
- observation: Navigation framework exists; standalone WinUI 3 + chosen version integration must still be built and tested locally.

## S-019 — JSON source generation
- source: `https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation`
- observation: Serializer metadata generation is separate from tolerant domain mapping; reflection fallback is not a schema repair algorithm.

## S-020 — App Installer settings
- source: `https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings`
- observation: Update policy settings; local channel selection and actual OS-associated update source must be aligned/tested.

## S-021 — Package requirements
- source: `https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements`
- observation: Numeric package identity/version requirements. Calendar scheme still needs race/ordering/overflow controls.

## S-022 — MSIX state boundaries
- source: `https://learn.microsoft.com/en-us/windows/msix/msix-containerization-overview`
- observation: Package binaries/data behavior is not equivalent to deleting every external side effect or remote token grant.

## S-023 — Claude authentication restriction
- source: `https://code.claude.com/docs/en/legal-and-compliance`
- observation: Current policy explicitly restricts third-party Claude.ai login and subscription credential routing. Quota-only exception not established.

## S-024 — App data clearing
- source: `https://learn.microsoft.com/en-us/uwp/api/windows.storage.applicationdata`
- observation: ClearAsync requires closed file handles; clearing app-owned data is not literal reinstallation or remote grant revocation.
