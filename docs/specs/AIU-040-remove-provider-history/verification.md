# AIU-040 verification

Base: `c349e4c`. Selected by the owner on 2026-10-03.

Completed on 2026-10-03; final evidence recorded at 21:44 UTC. Product removal is
commit `44ae02e`; the final follow-up changes only the Windows smoke harness and records.

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Removed contracts, clients, parsers, session methods, registrations, console command and resources. Source scan of `src`, `tests`, `tools` has no removed types; runtime and tool sources contain no retired routes. Local Ledger history remains. |
| AC-02 | PASS | Lifecycle inventory below; no persisted formats or cleanup changed. Focused independent review passed with no findings. |
| AC-03 | PASS | Infrastructure 611/611, Presentation 115/115, validator regressions 80/80, document validation and diff check PASS. Console and isolated Windows builds PASS (0 warnings/errors); unsigned package PASS; targeted actual Windows smoke 2/2 PASS after correcting test tray discovery. |
| AC-04 | PASS | AIU-011 backlog status is dropped, spec superseded and task handoff closed with D-184/AIU-040 references. Historical results remain. |

## Security-lifecycle inventory (2026-10-03)

- AIU-011 report results, date/account selections and retry deadlines were memory-only;
  no report-specific persistent file, schema or migration exists. Evidence: previous
  `ProviderHistoryResult`, history clients/session methods and AIU-011 spec storage policy.
- The old live/demo provider-history sources and their registrations were already
  removed in AIU-039. Current `LiveLedgerSource.GetHistoryAsync` projects locally held
  reading series through `LiveLedgerProjection.History`; `DemoLedgerSource` stays synthetic.
- `appearance.v1.json` and legacy preferences remain intact. `LedgerPreferenceStore`
  imports only supported global settings and writes its separate Ledger state. Unknown
  legacy values are not interpreted, reassigned, migrated or deleted by this change.
- AIU-036 budget readings/caps, AIU-039 account registry/preferences, quota caches,
  protected grants, rotation journals, recovery checkpoints and cleanup rules are unchanged.
  DPAPI CurrentUser, per-account leases and existing grant-rotation safeguards remain.
- Historical AIU-043 diagnostic evidence stays in the owned logs namespace until the
  existing bounded retention policy removes it. No one-off log purge or rewrite is added.
  Retired routes now receive the unknown policy, which withholds report scalar values.
- No source CLI store, real credential or user-state file was read or modified. No
  new endpoint, authorization scope, persistence namespace or dependency was introduced.

## Executed checks (2026-10-03)

On Windows x64, .NET SDK 10.0.401, from the repository root, against this removal diff:

- RED: `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo -method '*RetiredReportRoutesWithholdNativeValues'`
  failed all three cases on the pre-removal policy because native scalar values remained.
- GREEN: full Infrastructure suite, same command without the method filter: 611/611 PASS.
  Includes the three new sanitizer cases, remaining provider/quota/storage/account regressions,
  Codex lost-response retry suppression and Copilot external-disconnect behavior on refresh.
- `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`:
  115/115 PASS, including local 35-day history/gap and Ledger interaction tests.
- `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo`:
  80/80 PASS.
- `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`:
  PASS, valid with no diagnostics. `git diff --check`: PASS.
- `dotnet build tools/AiUsage.ProviderConsole --no-restore -c Release`: PASS, 0 warnings/errors.
- Default Debug Windows output: FAIL (environment), MSB3027/MSB3021: a pre-existing running
  AiUsage process locks Core/Infrastructure DLLs. Leave that instance running and rebuild
  to the isolated `.ai-usage-local/AIU-040/build/` output instead.

## Windows and console acceptance

- PASS: isolated unpackaged build using
  `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None -p:OutputPath=C:/Users/danii/projects/ai-usage-app/.ai-usage-local/AIU-040/build/ --no-restore`.
  Zero warnings/errors; existing running instance left intact.
- PASS: `tools/windows/Build-Package.ps1 -MsixVersion 2026.10.351.0 -NoRestore -OutputDirectory .ai-usage-local/AIU-040/packages`.
  Unsigned validation only; package hash
  `69A9F185D7D60C4CB1E1253E1A070DFF27C8431BDDA6FF434C94F10C183DCFD3`.
  Packaging emitted the existing toolchain warning that `mspdbcmf.exe` is absent and no
  symbols package is generated; no owned-code warning. Evidence is retained locally in
  `.ai-usage-local/AIU-040/packages/2026.10.351.0/package-evidence.json`.
- PASS: console `--help` omits history; invoking `history codex` with a nonexistent
  synthetic provider directory returns exit 2 without creating that directory. Console
  logs were redirected to `.ai-usage-local/AIU-040/console-state`.
- Initial Windows smoke: 1/2 PASS, demo FAIL at tray account selection after launch,
  history and settings assertions passed. The old test invoked the first identically named
  tray icon. With another AI Usage instance running, it did not open the launched test
  process's popup. A first correction also encountered an overflow window before its
  icons became visible (`overflow icons=0`). Both observations are retained under local
  `smoke`, `smoke-owned-tray` and `smoke-tray-diagnostic` evidence folders.
- Corrected the smoke harness to try visible candidates, verify popup process ownership,
  dismiss nonmatching popups and wait for visible overflow icons. It retains the account,
  focus, hide/restore and exit assertions; no product or display setting changed.
- PASS: `dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-restore -- -noLogo -method '*LedgerLaunchSettingsHistoryAndExit'`
  with `AIU_SMOKE_EXE` pointing at the isolated build and `AIU_SMOKE_EVIDENCE_DIRECTORY`
  at `.ai-usage-local/AIU-040/smoke-tray-ready`: 2/2, zero failures. Actual ordinary desktop
  checks cover demo local history, second synthetic account, settings, delete cancellation,
  diagnostics preview, tray restore/focus and exit, plus empty live-composition launch,
  settings and exit. Both evidence JSON files record `passed=true`, `exited=true`.

## Integrated review and limits

Focused independent read-only review used GPT-6 Astra at low reasoning, fresh context,
and frozen diff `c349e4c..44ae02e`. Verdict PASS, no material or minor findings. It inspected
removed routes/types and consumers, local history, data/preferences, grant and quota
boundaries, remaining safety tests, sanitizer projection and historical closure. The
reviewer did not independently execute tests/builds or touch UI/user state. Primary
review covered the final smoke-harness correction and integrated acceptance/diff checks.

NOT_RUN: live-provider requests, real authentication, installed package/upgrade/recovery
checks. They are unnecessary for removing this capability and were not inferred from
synthetic checks. No release, installation, trust change or credential import was performed.
The initial output-lock and tray-test failures above remain recorded; the isolated build
and corrected targeted smoke establish the applicable final passes.

No implementation work remains for AIU-040.
