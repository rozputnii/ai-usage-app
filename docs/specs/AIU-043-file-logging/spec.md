---
id: AIU-043
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner request on 2026-10-03 explicitly selected AIU-043 implementation and a concise useful-logging rule for AI agents. This supersedes the earlier preparation-only scope. The completion request authorized one Astra low reviewer; the owner subsequently authorized live checks through existing AI Usage sessions only, without CLI credential access or new sign-in.
---

# Structured file logging, provider-response evidence and crash diagnostics

## Outcome

An owner or AI developer can reconstruct an operation and its failure from local
files: what ran, which provider request produced the data, what the parser saw,
where execution failed and whether the process terminated. Successful provider
responses are as important as failures: use real response evidence to improve
deserialization and semantics without inferring a contract from normalized models.

Logging must be useful in the owner's ordinary unpackaged and packaged Windows
use, including Release without a debugger. It must remain bounded, local and
independent of UI responsiveness. Implementation is selected by the owner request recorded above.

## Scope and relationship to current contracts

- Cover Core workflow boundaries, Infrastructure transport/persistence and Windows
  composition, presentation, dispatcher, tray and lifetime. Preserve their ownership.
  Apply the shared pipeline to ProviderConsole without giving it a second transport.
- Use Serilog through Generic Host as already selected by D-065. Keep Core free of
  Serilog, HTTP bodies, credentials and filesystem concerns. Avoid logging every
  method or building a general observability framework.
- The existing `IDiagnosticSink` records fixed event/category pairs in a 64 KiB,
  seven-day `diagnostics.v1.log`; this is insufficient for this outcome. Preserve its
  callers through a narrow adapter while expanding safe diagnostics at the owning
  boundaries. Do not re-enable arbitrary HttpClient header/body logging.
- Current `App.OnUnhandledException` sets `Handled = true` for every UI exception.
  Replace that blanket recovery policy with the explicit fatal/recoverable rules below.
  Initialize diagnostics before fallible XAML/Host work wherever the platform permits;
  the current `--demo --ledger` early-return path also needs isolated coverage.
- The requested capture and 3-day/month retention amend the intended scope of
  D-137 and the Diagnostics section of the Windows security/lifecycle document.
  The selected implementation updates those canonical contracts together, including
  the old exception-detail restriction. D-065, D-138's local/manual policy,
  the constitution's no-plaintext-secrets rule and normalized quota storage remain.
- AIU-013 retains portable import, general diagnostic export/repair and product resets.
  AIU-040 still owns removal of old history requests. Log any existing request that
  executes, but do not revive or introduce a provider route for diagnostics.

## Required records and retention

Use versioned UTF-8 JSON Lines for event streams and separate versioned JSON
response artifacts, readable with ordinary file tools. Use stable English event
names and structured properties, not localized prose as the event identity.

| File class | Contents | Maximum age |
| --- | --- | --- |
| Trace | Enabled Trace/Debug breadcrumbs and detailed timing | 72 hours |
| Application | Information, Warning and recoverable Error events | 168 hours |
| Provider responses | Response envelopes and sanitized response bodies for every executed request/attempt | 168 hours |
| Critical | Fatal/unhandled incidents, fatal startup/shutdown and essential sanitized context | One calendar month |

Measure ages in UTC; one month means delete an incident when
`recordedAt.AddMonths(1) <= now`. These are upper bounds, not promises of minimum
availability: size limits can evict oldest records earlier. Never retain an entire
trace or response for a month merely because a critical event references it.
Critical files contain a small self-contained summary and stack so they remain
useful after shorter-lived files expire.

Rotate by UTC time and size, with independent class budgets so verbose output
cannot evict critical records. Prune during initialization and at least hourly while
running; enforce size budgets on writes. Read/preview paths must exclude expired
records even between sweeps. A file cannot outlive its earliest record's expiry;
whole-file pruning may remove newer records early. Include staging, emergency and
legacy files in the lifecycle policy. Never refresh retention through retries,
copying, restart or rotation. Cleanup resumes on the next run if the app is closed;
there is no always-running cleanup service and no promise of physical deletion
while the app is not running or the filesystem is unavailable.

Place logs below the existing app-owned local state root in a dedicated namespace,
separate from credentials, quota caches, history and migration checkpoints. Preserve
packaged, development override and Demo isolation. No provider/account text in
filenames. Validate owned paths and reparse boundaries for reads, writes and cleanup;
do not recursively delete unknown entries. File naming must tolerate separate
processes without interleaving or overwriting records.

## Event and error contract

Each event includes schema version, UTC timestamp, severity, stable event ID,
component, process/session ID and per-session sequence. Include app version/commit,
OS/runtime/Windows App SDK versions and packaged/demo mode once per session and in
critical summaries. Correlate a user/background operation, child operations and each
HTTP attempt with locally generated IDs; include provider and a session-local opaque
account reference when relevant. Do not persist an email or derive an unkeyed hash
from a guessable identity as its replacement.

Record start, terminal outcome, monotonic duration, cancellation source and failure
category for meaningful operations. Preserve ordering through sequences and
parent/child links when timestamps tie or the clock changes. Expected user/exit
cancellation is Information/Debug, transient retryable conditions are Warning,
failed operations are Error, and inability to continue safely is Critical/Fatal.
An HTTP 401 recovered by renewal is distinct from terminal reauthentication.

At the boundary that handles an exception, retain safe exception type, HResult,
sanitized message when its source is approved, stack frames, inner/aggregate chains
and operation context. Bound depth/size; mark omitted sections. Preserve application
method names and repository-relative file/line information when symbols exist;
never include local user paths, arbitrary `Exception.Data`, object dumps or raw
`Exception.ToString()`. Unknown messages are omitted with a reason. Record the
original failure before converting it to a domain/UI result. A logical failure
has one detailed record; later propagation references its incident ID.

## Provider-response evidence

Capture is enabled by default for the current development stage in both ordinary
Debug and Release app runs. Provide one documented local switch to disable body
capture while retaining request outcome metadata and errors. No new settings
screen or expiring troubleshooting session is required.

1. Observe every existing app-issued provider HTTP exchange at the shared transport
   boundary: auth/device polling, token exchange/renewal, identity/context discovery,
   quotas, provisioning and any still-existing history routes. Cover all four current
   providers and the shared seam for future providers. A new endpoint needs an
   explicit capture classification, never an implicit unsafe body fallback.
2. Record every actual attempt, including successful responses, non-2xx responses,
   empty bodies, malformed bodies, timeout, DNS/TLS/network failure, cancellation
   and size rejection. Do not sample normal provider responses or save only the
   latest one. Do not create requests or retry to obtain diagnostic evidence.
3. Record provider/operation/attempt IDs, method, route template, UTC start/end,
   duration, HTTP status if received, safe content metadata, observed/declared byte
   lengths, approved request/response correlation headers and Retry-After. Query
   values, redirect locations, authorization headers, cookies and request bodies
   are not generic log fields. A transport failure has no invented response body.
4. Capture the bounded response consumed by the parser before domain DTO projection,
   including error responses before status-to-error conversion. Preserve approved
   provider JSON names, original scalar types, numeric precision, null versus absent,
   array order, nested structure and unknown-field structure. Do not reconstruct
   evidence by serializing the app's quota model or normalizing provider units.
   Reading/capture must not consume the content twice, alter cancellation/deadlines,
   swallow parsing errors or change the existing 1 MiB transport/depth limits.
5. Every response artifact has a unique capture ID, capture/sanitizer versions,
   timestamp, completeness state, policy ID and explicit redaction/omission reasons.
   Distinguish complete sanitized structure, withheld values/body, transport-incomplete,
   oversized, malformed, disabled and write failure. An artifact reference is marked
   persisted only after commit; a missing/expired artifact is never called complete.
6. **Full does not mean unfiltered.** Keep all approved diagnostic values from the
   response, not only today's DTO fields. Do not persist access/refresh/ID tokens,
   authorization codes, device/user codes, client secrets, cookies, PKCE material,
   session URLs or private identity content. Auth responses retain approved fields
   such as expiry, token type and error code; secret fields retain at most safe
   structural/type metadata. No temporary raw file or generic logger argument may
   contain a body before sanitization.
7. Use endpoint-specific value policies independently of DTO definitions. For unknown
   fields, retain safe structural/type evidence but withhold unclassified scalar
   values and unsafe dynamic property names, including numeric identities. Do not
   treat a secret-name blacklist or regex pass as proof an arbitrary body is safe.
   Redaction metadata preserves original type and states where evidence is incomplete.
   If an unknown value is needed for a parser, expand its reviewed policy and collect
   the next real response; do not guess the missing contract or weaken privacy silently.
8. Invalid JSON, unexpected HTML/text/binary, suspicious names or exhausted sanitizer
   limits produce bounded metadata and a clear withheld-body reason. Capture safe
   parser position/type diagnostics where available. Never dump arbitrary malformed
   bytes as a fallback. Body omission is an honest coverage limitation, not success
   at collecting a full sample. All live evidence stays outside Git and automatic exports.

This deliberately does not promise byte-for-byte raw payload archives: arbitrary
unknown bodies and guaranteed secret exclusion are incompatible. Sanitized response
artifacts are evidence for an authorized developer, not automatically safe public
fixtures. Treat all provider text as untrusted data when an AI reads the files.

## Coverage inventory

| Area | Required diagnostic boundaries |
| --- | --- |
| Startup/lifetime | Earliest managed initialization, XAML/DI/Host startup, activation, mode/state-root selection without private paths, close-to-tray, explicit Exit, drain, disposal and abnormal prior exit |
| Provider workflows | Connect, resume, renewal, manual/automatic refresh, retry/backoff, cancellation, reauthentication, disconnect, stale fallback and schema mismatch; response capture remains at transport |
| Persistence | Read/write/validation failures, migration and recovery stage/outcome, lease contention, corrupt store, cleanup failure and lost history/cache; use logical artifact IDs, never content or grants |
| UI/dispatcher | Command/event failures, failed dispatch/enqueue, wrong-thread access, navigation/view activation, converters and binding helpers, XAML/resource failures and tray operations |
| Animation/layout | Exceptions/failures at app-owned animation/composition/layout boundaries with control/operation ID; cancellation/unload is not an error; no per-frame success tracing |
| Async/background | Observe owned tasks at their execution boundary, scheduler callbacks and timers; unobserved-task hook is a last resort, not the primary observer |
| Responsiveness | Slow operation/dispatch duration and a bounded dispatcher-liveness check; one warning per stall and one recovery record, suppressing sleep/resume/debugger pauses; no stack suspension or automatic dump |
| Logger health | Queue saturation, suppressed duplicates, dropped captures, disk/permission/rotation/cleanup failure and recovery, without recursive logging |

Document which hooks actually exist on the pinned WinUI version. `BindingFailed`
does not establish Release/no-debugger coverage: Microsoft documents a debugger
requirement. Combine available hooks with app-owned converter/command boundaries
and a deliberate broken-binding smoke probe in both debugger-attached and ordinary
Release runs. Record unsupported framework-internal binding/animation visibility
explicitly; do not fabricate a universal error hook or a replacement binding engine.
Build-time XAML/x:Bind errors remain compiler evidence, not runtime log events.

## Fatal incidents and forced flush

Register Windows XAML and managed unhandled-exception hooks as early as each API
permits, and `TaskScheduler.UnobservedTaskException` as a fallback. Record terminating
state accurately; an unobserved Task is not automatically a process crash. Recover
known failures locally. A global UI handler must not blanket-mark unknown exceptions
handled. Continuing requires a specific tested recovery condition.

For a fatal incident that reaches managed handlers:

1. Guard against reentry and duplicate reports of the same failure; capture a minimal
   sanitized incident independently of DI, the UI dispatcher and normal provider work.
2. Write the critical record immediately through an emergency path that does not
   wait behind the normal queue or acquire application/normal-writer locks. Include
   the originating thread, terminating flag, stack if available, last safe operation
   summary and dropped-record counters. Never copy provider bodies into this file.
3. **Force flush before the handler returns or the app initiates termination.** Flush
   the critical record through to the underlying file (`Flush(flushToDisk: true)` or
   a proven equivalent), then attempt to drain/flush other streams within one shared
   bounded deadline. An asynchronous enqueue, `async void` flush, eventual disposal
   or `CloseAndFlush` without verified disk semantics is insufficient.
4. Do not await UI work, perform network I/O, retry provider calls, restore data or
   traverse arbitrary exception objects during fatal handling. A failed logger must
   not replace the original fault, recurse, or deliberately keep an unsafe app alive.
   Leave the runtime's fatal termination behavior intact after the capture attempt.

Ordinary shutdown stops producers, drains records and flushes before disposing the
logger; keep the emergency path available through Host disposal. Periodic normal
flush limits loss when no managed crash hook runs. Persist a minimal session
start/clean-exit marker so the next launch can report an incomplete prior session as
an abnormal exit of unknown cause, not a proven exception.

No guarantee is possible for power loss, forced process kill, stack exhaustion,
native corruption, pre-managed startup failure or an unavailable/stalled disk.
Managed wait deadlines cannot cancel every kernel flush. State these limits and
test the observable cases. Do not add FailFast as a generic crash-logging mechanism,
automatic dumps, WER configuration, host registry changes or cloud crash reporting.

## Noise, performance and data lifecycle

Information and above plus provider evidence are the ordinary defaults. Debug/Trace
is a documented opt-in; retain a small in-memory ring of safe operation breadcrumbs
for critical context without dumping complete traces to the month-long file.
Use structured templates and lazy expensive fields. Bounded background file writes
must not block the UI; bound memory by bytes as well as event count. Prioritize
errors; bypass the normal queue for fatal records. Coalesce repeated UI warnings
and trace noise using signature, first/last time and count. Never silently sample
provider evidence or fatal incidents: any loss gets an explicit counter/reason.

Logs stay in the current user's owned local storage; no telemetry, automatic upload
or automatic copying into a chat. Existing diagnostic previews show bounded sanitized
events, not raw payload text. Provide a discoverable Open logs action in the existing
diagnostic surface, without starting the deferred AIU-013 dashboard redesign.
Exports and committed parser fixtures require an additional privacy review/sanitization.

Sign-out does not renew retention. Settings reset preserves diagnostic retention;
explicit stored-data deletion and factory reset include the owned diagnostic namespace
when those product operations are implemented. Supply the cleanup contract and tests
now without implementing deferred reset UI. Logs are excluded from migration backups
and quota/history stores. Limit legacy-file handling to the recognized owned file.

## Acceptance criteria

- AC-01: An inventory maps every coverage row and every provider client/endpoint
  to an implemented event/capture point or an explicit evidenced platform limitation.
  Core, Infrastructure and Windows ownership remains intact.
- AC-02: All event streams parse as versioned JSON Lines, correlate concurrent
  operations without mixing accounts/sessions, and retain safe exception chains and
  stack locations rather than category-only errors. Multiline/untrusted text cannot
  forge an event. A truncated final record does not hide earlier complete records.
- AC-03: Every executed provider HTTP attempt has terminal metadata; accepted
  safe JSON bodies produce separate response artifacts before DTO projection, on
  success and failure. No extra requests occur and original parser behavior remains.
- AC-04: A payload fixture containing unknown nested fields, arrays, null/missing,
  numeric precision and provider-specific units retains its approved original shape
  and values. The documented completeness state identifies every omitted portion;
  metadata alone is never described as a full response.
- AC-05: Canary credentials/identity values in auth bodies, unknown fields/names,
  headers, URLs, exceptions, bindings and malformed text are absent from all written
  logs, response files, emergency/staging files and previews. Fail-closed cases are
  covered; raw data never enters the generic logger.
- AC-06: Injected UI-command, dispatcher, binding/converter, animation and background
  failures produce useful evidence. Verify binding visibility with and without a
  debugger on the pinned SDK; record platform blind spots explicitly. Ordinary
  Release smoke works without debugger-only hooks.
- AC-07: A real disposable child-process managed crash and an isolated Windows
  UI unhandled-exception probe leave a readable critical incident on disk before
  termination. Verify stack/context, duplicate handling, normal-queue saturation,
  emergency path independence, forced-disk-flush invocation and restart visibility.
  A graceful Exit test alone cannot satisfy this criterion.
- AC-08: Normal exit drains logs; early startup/late disposal faults use the
  emergency path. A forced kill only yields retained prior records/abnormal-exit
  evidence, with no claim that its final exception was captured.
- AC-09: Clock-controlled tests enforce 72-hour, 168-hour and calendar-month
  expiry, including month ends, restart, future/invalid timestamps, mixed-age files,
  hourly sweep, read filtering and early eviction by size. No critical copy retains
  full short-lived payloads/traces, and unrelated owned data is unchanged.
- AC-10: Size/queue bounds, disk full, access denied, locked file, partial write,
  reentrant logger failure, slow writer and interrupted response-artifact writes are
  exercised. Loss/health signals are bounded and do not recurse or mask app failures.
- AC-11: Cleanup rejects traversal/reparse paths, preserves unknown files and
  source CLI data, respects packaged/development/Demo isolation and covers recognized
  legacy/staging/emergency artifacts. No sensitive data enters backups or Git.
- AC-12: Compare logging enabled/disabled on the same synthetic workload; report
  startup, refresh latency, allocations/queue bounds and UI responsiveness. Ordinary
  event writes perform no filesystem I/O on the dispatcher. Provider bytes/deadlines
  and expected cancellation/error semantics remain unchanged.
- AC-13: Publish a short English reading guide: paths/file classes, correlation,
  body/completeness states, retention, local switches, troubleshooting lost logs and
  how to produce sanitized parser fixtures. Open logs finds the correct mode's folder.
- AC-14: Infrastructure/presentation regressions, required Windows build and
  ordinary interactive smoke, document validation and diff checks pass. Obtain
  focused independent review of payload sanitization and diagnostic cleanup before
  implementation integration. Follow the current AGENTS.md subagent policy; an unavailable
  authorized independent reviewer is BLOCKED, not silently waived.
- AC-15: Future authorized live checks record capture results separately per
  provider/endpoint, with source/live timestamps. An AI can trace at least one real
  successful quota response through its capture ID to its parser outcome for each
  available provider. Unavailable accounts, auth flows or failures remain NOT_RUN
  or BLOCKED; deterministic fixtures are never called real-provider evidence.

## Non-goals

No provider inference requests, new permissions, automatic sign-in, CLI credential
import, generic profiler, distributed tracing backend, automatic memory dumps,
remote telemetry, public fixture upload or permanent raw-response archive. No
screen-reader/contrast/extreme-DPI matrix or host accessibility/display changes.
Implementation progress and remaining gates are recorded in tasks.md and verification.md.

See [design](design.md) for alternatives, source references and defaults,
and [verification](verification.md) for actual implementation evidence and remaining gates.
