# AIU-040 verification

Base: `c349e4c`. Selected by the owner on 2026-10-03.

Implementation is complete; Windows checks and focused review are in progress.

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Removed contracts, clients, parsers, session methods, registrations, console command and resources. Source scan of `src`, `tests`, `tools` has no removed types; runtime and tool sources contain no retired routes. Local Ledger history remains. |
| AC-02 | PASS | Lifecycle inventory below; no persisted formats or cleanup changed. Focused independent confirmation pending. |
| AC-03 | NOT_RUN | Infrastructure 611/611, Presentation 115/115, validator regressions 80/80, document validation and diff check PASS. Console Release build PASS (0 warnings/errors). Windows checks pending. |
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

Next action: finish isolated Windows build/smoke, unsigned package validation and focused
independent review, then record final evidence and close AIU-040.
