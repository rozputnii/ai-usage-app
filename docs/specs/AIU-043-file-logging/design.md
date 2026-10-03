---
id: AIU-043
type: design
status: implementing
goal: G-003
scope_version: 1
---

# Design rationale

## Alternatives

| Approach | Benefit | Cost and decision |
| --- | --- | --- |
| Extend the existing fixed-code file only | Few changes; existing security boundary | Cannot reconstruct exceptions or preserve provider structure; insufficient |
| Structured event streams plus separate sanitized response artifacts and a small emergency writer | Correlated evidence, independent retention, crash path independent of queue | Recommended; follows D-065 and needs a reviewed sanitizer/lifecycle boundary |
| Generic raw HTTP/memory capture or remote observability stack | Broad low-level material | Risks credential persistence, adds infrastructure and noise; outside scope |

The recommendation captures normal provider replies by default at this development
stage; an errors-only or manually enabled body mode would miss successful schemas.
Trace remains opt-in because individual UI/animation steps add little routine value.
Full encrypted raw-body archives are not part of this design: they create another
secret store and an AI-readable export problem without satisfying the existing
no-secret-log contract. Withheld unknown values are a stated evidence limitation.

## Minimum architecture

Windows composition owns early bootstrap, hooks and final flush, with an emergency
writer available before Host creation and after disposal. Infrastructure owns file
storage, retention and provider capture/sanitization. Core retains a small
credential-free event boundary. Adapt current fixed-code calls to the same pipeline;
do not introduce a second public logging framework or provider-specific writers.
`ILogger<T>` may serve presentation/infrastructure events with approved structured
properties; exception/payload projection must precede the generic logging boundary.

Use the existing `ProviderHttp.SendAsync` seam, which already buffers at most 1 MiB
and parses JSON to depth 32. Capture classified evidence from those bytes before
DTO projection and before transport error conversion destroys context. Keep
classification outside DTO models so missing model fields are still discoverable.
Do not blindly log `HttpRequestMessage`, `HttpResponseMessage` or JSON roots.

Use the selected Serilog Host/file integration rather than implementing ordinary
rolling files from scratch. Serilog packages are not yet in Directory.Packages.props;
pin compatible stable versions when implementation is selected. Ordinary file-sink
flush behavior alone does not prove forced-disk durability; use the smallest explicit
emergency writer needed for that requirement. Do not assume a standard asynchronous
sink has bounded memory, priority, retention-by-age or loss reporting without checking.
References: [Host integration](https://github.com/serilog/serilog-extensions-hosting)
and [file sink](https://github.com/serilog/serilog-sinks-file).

## Proposed defaults to validate during implementation

These are engineering proposals, not additional owner-specified limits. Keep them
as a small internal configuration, not a user-facing matrix of settings.

- Disk budgets: Trace 32 MiB, Application 64 MiB, Provider responses 128 MiB,
  Critical 32 MiB; total 256 MiB including staging and emergency files. Provider
  artifacts are evicted oldest-first, never overwritten as a latest-only snapshot.
- Roll ordinary event files at 8 MiB or UTC day boundary, whichever comes first.
  Calendar-month retention is time-based; rolling file count alone cannot enforce it.
- Bound normal event records at 64 KiB and each critical record at 256 KiB; keep
  at most 32 recent safe operation breadcrumbs. Explicitly mark shortened chains.
  Bound serialized response artifacts separately (initially 8 MiB, allowing JSON
  escaping/metadata expansion of the accepted 1 MiB body); exceeding it means
  withheld/incomplete evidence, never a silently truncated JSON document.
- Bound all normal pending output at 16 MiB and 4,096 entries, using whichever
  limit is reached first. Drop/coalesce trace first; failed provider persistence
  produces an explicit capture-loss event/counter when a healthy sink is available.
  The separately bounded emergency path does not depend on this queue.
- Flush ordinary output at least every second while running, and on orderly exit.
  Use a shared two-second managed deadline for fatal draining after prioritizing
  the emergency write. No nested per-sink deadlines that multiply the wait.
  A blocked kernel operation is outside the bounded-wait guarantee.
- Check dispatcher liveness every two seconds only during active ordinary operation;
  warn once after ten seconds without acknowledgement and record recovery duration.
  Suppress checks around suspend/resume and debugger pauses. This is an observation
  of unresponsiveness, not proof of deadlock or a reason to kill the app.

Do not trade correctness for these numerical defaults. If ordinary workload evidence
requires changing them, record why while preserving the owner's maximum ages and
the bounded-resource contract. A materially more complex implementation requires
owner discussion under CONTRIBUTING.md.

## Evidence format and sanitizer

One response artifact contains a metadata envelope and a sanitized body; event
records link to its capture ID. Use locally generated safe filenames. Write through
an owned staging name and promote only a complete artifact, then emit its committed
reference. A crash can leave an orphan stage/reference gap; readers and cleanup must
recognize it without replaying the request or trusting a partial JSON file.

The body is explicitly a sanitized representation, never a fixture to feed straight
into the product parser. Keep redaction locations/original types in envelope metadata
so redacted strings/numbers/nulls cannot be confused with provider-sent values.
Preserve numeric tokens without float conversion, and flag ambiguous duplicate JSON
properties instead of silently dropping them during dictionary serialization.

Policies distinguish safe schema keys/values, sensitive fields and unclassified
content recursively, including dynamic keys. Unknown shapes remain visible where
their names are safe; values are withheld until classified. A compact source
inventory must map every current route to a policy and tests, including non-2xx
and OAuth success. This is a diagnostic policy, not a new quota/auth contract.

Exception projection keeps code locations while removing private paths and untrusted
message/data values. Stack and context bounds apply before enqueue. Never call
arbitrary user/provider object's `ToString()` on the crash path. Diagnostics itself
uses fixed-code emergency health markers, avoiding recursion and raw sink exceptions.

## Platform facts and verification implications

Sources below were consulted on 2026-10-03. They establish API semantics, not live
behavior of the repository's pinned SDK/runtime. Recheck applicability against that
version during implementation and report genuine gaps.

- WinUI warns against routinely setting `UnhandledException.Handled = true` and
  notes that the projected exception can lose original details. Catch at owned
  boundaries and leave unknown fatal cases terminating after diagnostic capture.
  [Application.UnhandledException](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception?view=windows-app-sdk-1.8).
- `DebugSettings.BindingFailed` requires tracing enabled and an attached debugger
  in the documented API. It cannot be the sole ordinary-Release solution.
  [BindingFailed](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.debugsettings.bindingfailed?view=windows-app-sdk-1.8).
- Managed unhandled-exception handlers can run while application locks are held.
  Avoid waiting on those locks or the dispatcher in fatal logging.
  [AppDomain.UnhandledException](https://learn.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception?view=net-10.0).
- Unobserved task faults have distinct escalation semantics; observe owned tasks
  promptly and retain the global hook only as a fallback.
  [TaskScheduler.UnobservedTaskException](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler.unobservedtaskexception?view=net-10.0).
- `FileStream.Flush(true)` explicitly flushes intermediate file buffers. Queue
  draining and writer disposal do not by themselves establish this requirement.
  [FileStream.Flush](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0).
- `FailFast` bypasses normal finalization and can trigger Windows error reporting
  and dumps. Do not add it as a diagnostic transport or promise a managed flush
  after it has begun. Do not change host WER policy in this feature.
  [Environment.FailFast](https://learn.microsoft.com/en-us/dotnet/api/system.environment.failfast?view=net-10.0).
- Structured, correlated, bounded logs with excluded secrets and tested logging
  failures follow the applicable principles in the
  [OWASP logging guidance](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html).

## Verification and integration boundaries

Use synthetic provider responses and canary secrets first; inspect every output
class including emergency files. Run fatal probes in dedicated child processes and
an isolated development state root, never the owner's running app or credentials.
Inspect actual files after process termination. Simulated logger calls alone cannot
prove crash survival. Use ordinary unpackaged Windows smoke; a VM is needed only
for a specific isolation requirement, not all logging tests.

Focused independent implementation review covers sensitive payload projection,
exception disclosure and owned cleanup. Codex subagents are disabled. Use an
authorized available independent reviewer or record the required review as blocked;
the primary's self-review is not independent review.

The subsequent owner request selected implementation on 2026-10-03. The sequential
plan is in tasks.md; D-137/security lifecycle now describe the selected pipeline.
No live-provider authority or independent-review waiver follows from that selection.
The current endpoint and boundary inventory is in coverage.md.

Implementation ruling: an isolated ordinary Release probe on the pinned SDK showed
that a raw DispatcherQueue callback bypasses both global hooks, while an async-void
UI fault can invoke Application.UnhandledException and then continue. This matches
the failure modes reported in [WinUI issue 10964](https://github.com/microsoft/microsoft-ui-xaml/issues/10964),
but the local probe is the evidence for this build. Owned dispatch/converter/animation
boundaries therefore force critical capture before rethrow. The global XAML handler
explicitly leaves Handled false and calls Environment.Exit(1) after capture/drain so
an unknown unsafe UI state cannot continue. This uses no FailFast, dump or WER changes.

Performance ruling: scanning every artifact for every event made a 200-response burst
take more than ten seconds to drain. Event files now reserve a full bounded roll on
opening; response pruning uses directory metadata and touches exact paths again only
when deleting. This retains size/age checks without repeated full-directory path walks.
