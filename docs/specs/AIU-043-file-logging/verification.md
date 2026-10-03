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
