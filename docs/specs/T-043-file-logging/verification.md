# T-043 verification

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
- R-065 already chooses Serilog, but its packages are not in central package versions.
  R-137 and current security/lifecycle text restrict payload and exception persistence;
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
method/tick/frame spam and speculative logging wrappers/frameworks. R-137 and the
Windows security/lifecycle contract were reconciled with the selected scope.

## Completion verification - 2026-10-03

This section supersedes the incomplete checkpoint dispositions above. The owner
requested completion, authorized one Astra low subagent, and explicitly authorized
live checks through existing AI Usage sessions without CLI credential access or
new sign-in. Completion code: `c25a01d` and `4362346`, based on `99efa21`.
Environment remains the local interactive Windows desktop and pinned .NET 10.0.401;
no host trust/display/ACL changes, disk exhaustion, package install or release.

### Fixes and independent review

The fresh read-only Astra low review of `44cee2e..5c60dae` reported one P2 finding:
ProviderHttp discarded original transport/parser exceptions before domain conversion.
Five affected capture cases failed before the fix, then passed with safe type,
HResult, stack, inner chains and numeric JSON position in the capture envelope.
No arbitrary messages, JSON paths or exception Data are retained. Correlation uses
the existing capture/operation IDs; no second original-exception event is emitted.
The same reviewer performed a narrow supplemental review of the new projection,
ordinary-output fault seam/recovery and existing-session quota console in `c25a01d`:
PASS, no material findings. Reviewer inspected code and assertions; independent
execution of tests/live checks was NOT_RUN. The primary executed those checks.

A clock-controlled hourly/mixed-age test failed before switching the sweep to the
injected TimeProvider's monotonic clock; runtime defaults remain the system clock.
Injected partial event output exposed a recovery defect: the next event joined the
incomplete line. Closing the failed writer fixes it; the reproducing test now passes.
The I/O seam is one internal stream decorator on ordinary output, with all fault
behavior in tests. Emergency FileStream and forced-disk flush bypass that seam.

| Check | Result | Evidence |
| --- | --- | --- |
| Infrastructure | PASS | Release suite: 590 passed, zero failed/skipped; `completion-infrastructure.txt` |
| Presentation | PASS | Release suite: 262 passed, zero failed/skipped; `completion-presentation.txt` |
| Targeted fixes | PASS | 54 capture/storage cases; six initial expected failures (five capture cases and hourly sweep) then all passed; `completion-red.txt`, `completion-targeted.txt` |
| Storage faults/restart | PASS | 4 final cases: injected ERROR_DISK_FULL after partial output, UnauthorizedAccessException, uncommitted stage/restart, reentrant failure and blocked ordinary writer with bounded flush plus independent critical capture; `storage-faults-final.txt` |
| Startup | PASS | Actual invalid XAML and hosted-service StartAsync failure: nonzero child exit, one terminating StartupFailure each, canaries absent; `completion-lifecycle.json` |
| Late Host disposal | PASS | Actual DI-owned hosted-service Dispose failure: exit 1, one terminating DisposalFailure, canary absent; `completion-disposal.json` |
| Binding with debugger | PASS | Same Release broken-binding probe launched with native DEBUG_ONLY_THIS_PROCESS: exit 0, one BindingFailure, canary absent; `binding-debugger.json`, local `Invoke-BindingDebugger.ps1` |
| Ordinary Windows smoke | PASS | Eight selected ShellSmoke scenarios, two unrelated tests filtered/not run, 39.186 s; preview and actual Explorer navigation to the unique product logs directory; `completion-smoke.txt`, `completion-smoke` |
| Demo Open logs | PASS | Dedicated DemoLogsOpenIsolatedFolder: 1 passed, 10.632 s; actual Explorer navigation to Demo/logs; only that test folder window closed; `completion-smoke-demo.txt` |
| Release build | PASS | Final unpackaged build: zero warnings/errors; `completion-build.txt` |
| MSIX | PASS | Unsigned validation-only `2026.10.354.0`, UTC 2026-10-03T14:48:53.0572213Z; SHA256 `DD6B225B0053377CCB2C0D36FAE0E4941166EFAAA9E21B5D4F68BACC1A0A6845`; `completion-package.txt` |
| Independent review | PASS | Focused initial review plus resolved P2 and supplemental projection/storage/console review; no unresolved material findings |

Evidence files above are under ignored `.ai-usage-local/AIU-043`. Fault probe roots
are disposable temporary directories; none contain the owner's grants. The first
Host-disposal probe used an externally supplied singleton, which DI correctly did
not dispose. That harness attempt had no critical file and is not acceptance evidence.
Registering the probe with a DI-owned factory produced the actual disposal result above.
The MSIX tooling again omitted the optional symbols package because mspdbcmf.exe is
unavailable; the package succeeded. No package installation success is inferred.

The native debugger check follows the documented [BindingFailed debugger requirement](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.debugsettings.bindingfailed?view=windows-app-sdk-1.8)
and [Win32 debug-event handling](https://learn.microsoft.com/en-us/windows/win32/api/debugapi/nf-debugapi-waitfordebugevent).
Earlier ordinary no-debugger Release evidence remains the corresponding observed
platform blind spot. No host debugger settings or automatic dump policy changed.

### Authorized live evidence

Source contracts remain the existing provider records; no endpoint/auth contract
was changed. Auth classification remains source-observed public-client reuse;
quota classification remains undocumented/source-observed internal endpoints.
Source verification and this new live verification are distinct:

| Provider/route | source_verified_at | live_verified_at (UTC) | Result |
| --- | --- | --- | --- |
| Codex `/oauth/token` | 2026-09-13 | 2026-10-03T14:42:28Z | PASS: existing grant renewal, HTTP 200, sanitized committed artifact |
| Codex `/backend-api/wham/usage` | 2026-09-26 | 2026-10-03T14:42:29Z | PASS: HTTP 200, fresh QuotaAvailable, capture `3c030e10-8682-4491-b18f-4fcaf5a67ac6` |
| Copilot `/user` | 2026-09-18 | 2026-10-03T14:42:30Z | PASS: HTTP 200, identity values withheld |
| Copilot `/copilot_internal/user` | 2026-09-26 | 2026-10-03T14:42:31Z | PASS: HTTP 200, fresh QuotaAvailable, capture `34f6b0ee-2823-4137-ad2c-2609ee287be5` |
| Claude | 2026-09-26 | null | NOT_RUN: NotConnected in both existing development and installed-app stores; no HTTP request or sign-in |
| Antigravity | 2026-09-26 | null | NOT_RUN: NotConnected in both existing development and installed-app stores; no HTTP request, provisioning or sign-in |

Each of the four executed requests has exactly one HttpCompleted and one
CapturePersisted event. Both successful quota artifacts share their operation ID
with a Completed parser/session outcome and returned cached=False. All four
artifacts explicitly report withheld-values, never a raw/full body claim. Codex
renewal updated its existing app-owned grant through the normal session. Output
printed only fixed status/failure/cache flags. No provider body, token, account ID
or private path was copied into this report or Git. Local metadata-only evidence:
`live-summary.json`; private sanitized artifacts stay in `live-capture/Console/logs`.
Other auth flows/history/provisioning endpoints retain deterministic coverage only.

### Final acceptance disposition

| Criterion | Result | Basis and practical limits |
| --- | --- | --- |
| AC-01 | PASS | Updated endpoint/boundary inventory and evidenced platform blind spots |
| AC-02 | PASS | Existing JSON/correlation/exception tests plus original transport/parser failure regression |
| AC-03 | PASS | Deterministic attempt coverage and four correlated committed real captures; unchanged transport behavior |
| AC-04 | PASS | Original precision/native units/shape and explicit redactions; no policy expansion to guess live values |
| AC-05 | PASS | Existing canaries plus failure/partial-output/reentrant/startup/disposal/debugger probes; no raw generic logging |
| AC-06 | PASS | Owned UI/dispatcher/converter/animation/background probes plus binding with and without a debugger |
| AC-07 | PASS | Existing actual child/UI crashes and forced flush, plus blocked ordinary writer independence |
| AC-08 | PASS | Ordinary drain/forced kill evidence plus actual XAML/Host startup and late Host disposal failures |
| AC-09 | PASS | Existing age/month/size/restart/read tests plus clock-controlled hourly sweep and mixed-age earliest-record expiry |
| AC-10 | PASS | Existing queue/path/locked-file cases plus injected disk-full/access-denied, partial/stage/restart, reentrant and slow-output cases; real host disk exhaustion/ACL changes NOT_RUN and unnecessary for these fault-path checks |
| AC-11 | PASS | Existing exact-name/reparse/isolation/unknown-file tests plus orphan-stage cleanup/restart; no CLI store access |
| AC-12 | PASS | Earlier same-workload measurements and ordinary responsiveness remain applicable; no transport/body/deadline behavior changed |
| AC-13 | PASS | Updated English guide and actual Explorer navigation for ordinary product and Demo; packaged folder opening itself NOT_RUN (no package installation requested) |
| AC-14 | PASS | Local regressions, build/package/smoke and completed focused independent review; final document/diff check recorded below |
| AC-15 | PASS | Every available provider traced from successful real quota capture to parser outcome; unavailable Claude/Antigravity explicitly NOT_RUN as the criterion permits |

Extended sleep/debugger watchdog timing remains NOT_RUN; existing isolated stall and
recovery evidence stands. Native corruption/power loss/stalled kernel flush and
framework-internal faults outside available hooks remain documented limits, not
promises added by these tests. No remaining implementation/review gate is open.

Final documentation/diff check: PASS. The document validator returned valid=true,
no diagnostics after completed task evidence fields were made single repository-relative
artifact paths. `git diff --check` passed. This corrected record syntax only; no
acceptance criterion or validator requirement was weakened.

## Owner-operated sign-in follow-up - 2026-10-03

Owner requested an isolated ordinary app instance and manually signed in. Build
`c9c4e9a`, Release unpackaged; isolated root recorded only in ignored local
`manual-session.json`. No CLI credential reads or agent-driven sign-in occurred.

- PASS: Claude OAuth and `/api/oauth/usage` returned HTTP 200. A subsequent quota
  capture `b05e0552-11bc-4707-b3a0-ef065da8aa96` has one terminal event, one committed
  reference and a Completed operation. This supersedes the earlier unavailable-session
  result for the first owner-described work account. The second Claude account is NOT_RUN.
- PASS: Copilot device authorization completed, followed by identity and quota HTTP
  200. Capture `548d91bc-90fe-42ea-b52d-a9540b766993` has one terminal/committed reference
  and Completed outcome. Codex capture `71ba5257-f99c-4f20-952d-e86300aa5a5d` does too.
- BLOCKED: Antigravity connection reports RegistrationUnavailable before any HTTP
  request. This is missing operator OAuth client configuration, not a provider quota
  rejection or evidence that the owner's plan is unsupported.
- No critical incident was present at inspection. Raw/private response values,
  account identities, device codes and credentials remain outside this record.

The owner's missing-display observations are separate from capture success. Claude's
response contains enabled spend/extra_usage with non-null used and limit amounts.
Current LiveMapping projects these into Extra usage in account details; the old
main list does not render them as the planned unified limit row. Actual visibility
on this owner's screen was not independently exercised. Calculated workday budgets
are not wired into this live presentation: T-039 remains ready, unimplemented.
The inspected Copilot response/current parser describes a monthly quota with a
November 1 reset; the owner-described weekly display may refer to Codex. No weekly
Copilot contract is inferred from that description.

Owner reports late appearance of the device code. GitHub supplied it in about
313 ms; 36 authorization_pending polls preceded successful authorization. Those
polls do not measure code-render delay. LiveConnectionFlow opens the browser before
publishing WaitingForAuthorization/DeviceUserCode, which permits the browser page
to appear first. Exact delay/root cause is NOT_RUN pending an instrumented UI
reproduction; no code-render timestamp, UI fix or successful latency check is claimed.
The supplied screenshot establishes the GitHub device-entry page, not app timing.

## Post-done corrections (2026-10-06)

Recorded 2026-10-06 under AIU045-D6(a). This annotation does not reopen T-043, and its backlog status is unchanged. No FIX-01..FIX-14 defect belongs to T-043; its post-done change is the AC-08 startup ordering below.

**AC-08 startup-ordering change (recorded 2026-10-06).** The synthetic UI audit that started from `383644c` changed the startup order in the `App` constructor: `2942e6f` calls `AuditReplay.Open` before `diagnostics.Initialize` and the unhandled-exception hook, and `f4891b9` passes the audit root to `diagnostics.Initialize`. A failure inside `Open` therefore happens before diagnostics exist, so the AC-08 early-startup fault handling cannot record it. This is the AUD-01 and AUD-04 mechanism in section 3 of the T-045 [analysis record](../T-045-ui-audit-follow-up/analysis-2026-10-05.md). The path is reachable only with `--demo --audit-input=...`; plain `--demo` and live startup never reach it. Its correction is T-045 T-043.2. The AC-08 native startup and disposal probes recorded above predate this change and are NOT_RUN at current `main` (ANL-07); T-045 T-043.3 reruns them.

Never-accepted gaps (ANL-13), as of 2026-10-06:

- AC-13: packaged Open logs (opening the packaged log folder) is NOT_RUN; the final acceptance disposition above passed AC-13 without it because no package installation was requested. Status: AC-13 packaged Open logs PASS 2026-10-06 (2026.10.602.0): Settings > System status > Open logs opened the logs folder (T-045 T-043.3).
- The owner-reported device-code display delay (owner-operated sign-in follow-up above) was never diagnosed; its exact delay and root cause remain NOT_RUN. Status: not exercised on 2026-10-06 (no sign-in was performed); still open as a separate follow-up.

## Execution ledger

Collapsed from tasks.md on 2026-10-09 (OD-19); the full plan is in Git history at c9c4e9a.

- T-043.1 Bounded diagnostic storage: done; commits within 44cee2e..5c60dae (checkpoint b91da5d), completion fixes c25a01d; review independent Astra low review of 44cee2e..5c60dae (one P2, fixed) and supplemental review PASS; checks C4, STORAGE, MANAGED-CRASH, clock-controlled hourly sweep red then green; grant owner selection of implementation, 2026-10-03.
- T-043.2 Provider evidence: done; commits within 44cee2e..5c60dae, fix c25a01d; review independent P2 (lost original transport/parser failure evidence) fixed with five red-then-green capture cases, supplemental review PASS; checks C4, CAPTURE, live Codex and Copilot captures PASS, Claude and Antigravity NOT_RUN; grant owner selection, 2026-10-03, and owner authorization of live checks through existing sessions only.
- T-043.3 Windows and console integration: done; commits within 44cee2e..5c60dae (including 4dc291b), Windows probes and smoke 4362346; review supplemental independent review PASS; checks C5, C7, UI-FAULTS, UI-UNHANDLED, startup and late-disposal probes, binding with debugger, ShellSmoke (8 scenarios), Demo Open logs, PERFORMANCE; grant owner selection of implementation, 2026-10-03.
- T-043.4 Policy, verification and review: done; commits c25a01d, 4362346; review independent PASS, no unresolved material findings; checks C4 (590/590), C5 (262/262), C7, C9 (2026.10.354.0), C2, C6; grant owner completion request authorizing one Astra low reviewer, 2026-10-03.
