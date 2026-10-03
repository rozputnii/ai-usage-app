# AIU-043 verification

## Task preparation - 2026-10-03

Scope: documentation only. The owner requested a task definition, not logging
implementation or live-provider access. Starting revision: `00007ca` on `main`;
working tree was clean. No credentials, private runtime files or provider bodies
were read. No provider requests, sign-in or Codex subagents were used.

Source inspection established:

- `LocalDiagnosticSink` rewrites a bounded 64 KiB seven-day fixed-code file;
  `IDiagnosticSink` carries only event/category enums.
- `ApplicationDiagnostics` is initialized from `OnLaunched`; `App` attaches its UI
  exception handler after `InitializeComponent`, always marks UI exceptions handled,
  and the Ledger demo branches before normal diagnostic initialization.
- `ProviderHttp` is the shared bounded transport/JSON seam; it currently converts
  network failures and rejects oversized or invalid successful JSON without keeping
  a response artifact. Existing provider registrations suppress default HTTP loggers.
- D-065 already chooses Serilog, but its packages are not in central package versions.
  D-137 and current security/lifecycle text restrict payload and exception persistence;
  the draft identifies the required future reconciliation explicitly.

External API references and their applicability limits are in [design](design.md).
They are source evidence only. No interactive or live behavior is inferred from them.

| Check | Result | Evidence |
| --- | --- | --- |
| Primary requirements/design review | PASS | Checked requested coverage, separate body evidence, forced flush, retention, existing policy conflicts, truthful platform limits and draft-only authority; 15 acceptance criteria |
| Document validator | PASS | Local pinned SDK ran `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`; exit 0, valid true, no diagnostics |
| Diff whitespace check | PASS | `git diff --check` and final staged diff check; exit 0 |
| Product implementation/regressions/build | NOT_RUN | Documentation-only request |
| Interactive Windows and crash probes | NOT_RUN | Future AC-06 through AC-08/AC-14 work |
| Live provider capture | NOT_RUN | Future separately authorized AC-15 work |
| Focused independent implementation review | NOT_RUN | No implementation exists; required before later integration |

The first validator run rejected Markdown-bold acceptance IDs because the repository
expects plain `- AC-NN:` entries. Corrected the document to that format; the subsequent
run passed. The validator implementation was not changed and no requirement was removed.

Next action: obtain the owner's review of the draft specification and design before
selecting implementation or writing its execution plan. No feature completion or
independent review approval is claimed by documentation publication.

## Implementation checkpoint - 2026-10-03

The subsequent owner request selected implementation and an agent rule for useful,
low-noise logging. Base `44cee2e`; initial implementation checkpoint `b91da5d`.
The preparation-only next action above is historical. No source CLI credentials or
live provider accounts were read. All new payloads and fault probes are synthetic.

Observed on local Windows, pinned .NET SDK 10.0.401/runtime 10.0.12:

| Check | Result | Evidence |
| --- | --- | --- |
| Initial diagnostics/capture tests | PASS | 9 tests; JSON, precision, canaries, malformed success, calendar-month retention, cleanup, critical persistence |
| Infrastructure regressions | PASS | 544 tests, Release, including dedicated managed crash child process and additional queue/link/correlation tests |
| Presentation regressions | PASS | 262 tests; corrected duplicate failure emission and kept backend names behind the adapter boundary |
| Windows Debug unpackaged build | PASS | No warnings/errors |
| Windows Release unpackaged build | PASS | No warnings/errors; ordinary Release probe binary, no debugger |
| Local ordinary Windows smoke | PASS | 7 actual Debug scenarios: launch, navigation, appearance, tray exit, repeated exit, capabilities, close-to-tray/restore; `.ai-usage-local/AIU-043/smoke-debug` |
| Managed fatal probe | PASS | Dedicated disposable process terminated nonzero; critical JSON contained stack, terminating flag, no canary; restart reported PreviousExitUnknown |
| Independent implementation review | BLOCKED | Current tools provide no authorized independent reviewer; Codex subagents remain disabled. Primary self-review is not independent review |
| Live provider captures | NOT_RUN | No current authorization/accounts used; synthetic replies do not establish AC-15 |

The first crash probe inside the xUnit executable stalled before the app's global
handler, while its normal queue completed. Replaced that runner-dependent probe with
a dedicated child executable; the real unhandled exception then terminated and left
the required file. This is test isolation, not a change to runtime termination policy.

Remaining checkpoint work: Release UI fault probes, final log-policy checks, package
build, performance comparison, final document validation and focused review disposition.
No complete-feature claim is made by this checkpoint or automatic main publication.

## Integrated implementation verification - 2026-10-03

Code scope: `44cee2e..5c60dae` (final implementation checkpoint), including
`b91da5d` and `4dc291b`. The implementation is saved on main under the standing
checkpoint policy; publication does not waive the independent-review gate. New
provider evidence is synthetic, offline and stored only in ignored local/temp roots.

Environment: local interactive Windows desktop, .NET SDK 10.0.401/runtime 10.0.12,
pinned WinUI/Windows App SDK packages, ordinary unpackaged Debug/Release. No
accessibility/display setting changes, sign-in, credential import, package install,
trust changes, release/tag or live-provider requests were performed.

| Check ID | Result | Command or observed evidence |
| --- | --- | --- |
| INFRA | PASS | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`: 585 passed, zero failed/skipped; `.ai-usage-local/AIU-043/infrastructure-final.txt` |
| PRESENTATION | PASS | Corresponding `tests/windows/AiUsage.Presentation.Tests` command: 262 passed, zero failed/skipped; `presentation-final.txt` |
| CAPTURE | PASS | Success/error responses, all 27 existing route cases, nested identity/unknown-name canaries, numeric precision/native units, duplicate keys, disabled/malformed bodies, network/timeout/cancellation/size rejection; one terminal record per failed attempt |
| STORAGE | PASS | Concurrent operation/account correlation, exception chains/stacks, month-end retention, future/invalid filenames, early size eviction, unknown-extension preservation, redirected directory rejection, locked/unavailable storage, queue losses, coalescing, truncated preview and late emergency write |
| MIGRATION | PASS | All three protected provider-generation fixtures execute migration with diagnostics enabled and preserve grant fields/lineage; stage/completion events contain neither synthetic identities nor root paths. Existing general-state/cache safety regressions pass |
| MANAGED-CRASH | PASS | Dedicated child crashes with saturated ordinary queue; readable critical record, safe stack/context, duplicate suppression and abnormal restart. Separate forced kill leaves prior evidence without inventing a final critical incident |
| UI-FAULTS | PASS | Isolated ordinary Release dispatcher/converter/animation probes each produced one critical file, nonzero termination and no canary. Observed background task produced BackgroundFailure and exited normally; `ui-probes-fixed.json` |
| UI-UNHANDLED | PASS | Actual async-void XAML exception now force-writes one critical incident and exits with code 1; `ui-explicit-exit.json`. Handler explicitly exits only after capture because the pinned SDK continued with Handled=false |
| BINDING | NOT_RUN | No-debugger broken-binding probe ran and produced no BindingFailed event. This is an evidenced platform blind spot; debugger-attached visibility has not been exercised |
| WATCHDOG | PASS | Isolated 14-second UI stall produced exactly one DispatcherStalled and one DispatcherRecovered, recovery duration 12062 ms; `ui-stall.json`. Harness later terminated that probe at its deadline, so this is not normal-exit evidence. Suspend/debugger suppression timing remains NOT_RUN |
| DEBUG-SMOKE | PASS | Seven actual ordinary Debug launch/navigation/appearance/tray/exit/capability/close-to-tray scenarios; `smoke-debug` |
| RELEASE-SMOKE | PASS | Final code: `dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-restore -- -noLogo -class '*ShellSmoke'`; eight selected scenarios passed, two unrelated tests filtered/not run, 44.148 s. Includes logging preview/Open logs availability; `smoke-final` and `smoke-final.txt`. No provider accounts loaded |
| BUILD | PASS | Final Release unpackaged build, zero warnings/errors; `build-release-final.txt` |
| DOCS | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`: valid true, no diagnostics; `git diff --check` passed |
| PACKAGE | PASS | `tools/windows/Build-Package.ps1 -MsixVersion 2026.10.353.0 -OutputDirectory .ai-usage-local/AIU-043/packages -NoRestore`; unsigned validation-only MSIX, UTC 2026-10-03T12:27:53.4493533Z, SHA256 `54D143E77A63DB35EA2F771FD0D954E18D0315FFA8ECD62A20DB5BAD5D93EDC1` |
| PERFORMANCE | PASS | `AiUsage.ProviderConsole measure-logging`, 200 identical in-memory replies per mode; `logging-measurements-fixed.json`, exit 0, both drains complete and zero lost records |
| PRIMARY-REVIEW | PASS | Reviewed integrated ownership, endpoint policies, fixed-code inputs, raw-data exclusion, retention/delete allowlist, fatal independence/termination and useful-logging rule. Fixed per-event directory scanning, incorrect unknown-extension cleanup, swallowed UI termination, missing persistence-stage visibility and account-reference continuity |
| INDEPENDENT-REVIEW | BLOCKED | CONTRIBUTING and AC-14 require focused independent review of sanitization and cleanup. No authorized independent reviewer is available through current tools; owner disabled Codex subagents. Primary review is not a substitute |
| LIVE-PROVIDERS | NOT_RUN | No authorization used for real accounts/endpoints. No claims about AC-15 from deterministic fixtures |

Package tooling reported missing `mspdbcmf.exe`, so the optional symbols package was
not generated. The MSIX itself built successfully. Earlier 2026.10.351.0 failed
compilation after a duration API edit; the public fixed/numeric-safe adapter corrected
it, and the final package passed. No failed run is counted as package evidence.

Performance observations (same offline workload, one local run, not a universal
latency guarantee): disabled/enabled startup 0.0009/23.6431 ms; average request path
0.08644/0.20888 ms; allocations 586,968/55,037,800 bytes; enabled background drain
750.429 ms. Queue caps are 16 MiB/4,096 entries. Ordinary event writes enqueue only;
response projection allocates bounded sanitized data before background file output.
Interactive smoke verifies ordinary responsiveness qualitatively, not frame timings.
An earlier drain exceeded ten seconds because every ordinary event rescanned all
retained files. Reserving a roll on open and enumerating class metadata removed that
regression; the failed measurement remains historical, not the reported success.

### Acceptance disposition

A NOT_RUN below means the full criterion is not established even where tested
subsets pass. Do not promote this task to done from build/test totals alone.

| Criterion | Result | Evidence and remaining limits |
| --- | --- | --- |
| AC-01 | PASS | `coverage.md` maps current clients/routes and owning code/platform blind spots; layer-boundary regressions pass |
| AC-02 | PASS | STORAGE: JSON/correlation/safe exception/truncated-record tests; process/session IDs and per-session sequences |
| AC-03 | PASS | CAPTURE and existing transport regressions; no new request/retry path; capture precedes DTO projection |
| AC-04 | PASS | CAPTURE: original numeric tokens/native units/shape, explicit redactions and duplicate-property flag |
| AC-05 | PASS | Secret/identity/header/query/exception/malformed canaries excluded in synthetic files and UI fault probes; raw bodies never passed to Serilog. This is tested exclusion, not a claim that arbitrary future schemas are approved |
| AC-06 | NOT_RUN | UI-FAULTS pass; no-debugger binding limitation documented; debugger-attached binding verification remains unrun |
| AC-07 | PASS | MANAGED-CRASH/UI-UNHANDLED and locked ordinary-store independence; source confirms synchronous `Flush(true)` before return/exit. Cannot guarantee a stalled kernel flush |
| AC-08 | NOT_RUN | Ordinary interactive exit, forced kill and post-disposal critical write pass; dedicated fallible-XAML/Host-startup and actual late-Host-disposal injection remain unrun |
| AC-09 | NOT_RUN | Calendar month, 72/168-hour age, clock reversal/future/invalid names, restart, preview filtering and size eviction covered; dedicated hourly-worker/mixed-age rotation timing probe remains unrun |
| AC-10 | NOT_RUN | Queue pressure, unavailable path, locked file, loss reporting and partial preview pass. Real disk-full/access-denied, injected partial output/reentrant logger/slow writer and interrupted response-stage write matrix not executed; no host disk exhaustion or ACL changes attempted |
| AC-11 | PASS | Exact owned-name/extension cleanup and reparse tests; mode root isolation in code and real temporary Debug/Release probes; unknown files preserved; outputs ignored; no credential/source-store reads |
| AC-12 | PASS | PERFORMANCE, transport regressions, ordinary interactive smoke; explicit measurement limits above |
| AC-13 | NOT_RUN | English guide and bounded preview/Open logs control delivered and smoke-tested; actual Explorer navigation to each mode's folder has not been exercised |
| AC-14 | BLOCKED | Required local tests/build/smoke passed; focused independent review unavailable |
| AC-15 | NOT_RUN | Requires separately authorized live-provider evidence |

The additional agent rule is in AGENTS.md, Useful logging. It asks for purposeful
operation/failure/recovery evidence, use of existing logs during debugging, one
owning boundary, correlation and appropriate severity. It explicitly rejects
method/tick/frame spam and speculative logging wrappers/frameworks. D-137 and the
Windows security/lifecycle contract were reconciled with the selected scope.
