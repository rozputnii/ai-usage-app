# Reading local diagnostic files

Open **Settings → System status → Open logs**. The directory is `logs` below the
existing owned state root: package LocalState for installed use;
`%LOCALAPPDATA%/AiUsage/Development` for unpackaged use. `AIU_DEVELOPMENT_STATE_DIRECTORY`
overrides development only. Demo uses its `Demo` subdirectory; ProviderConsole uses
`Console`. No logs are uploaded or included in migration checkpoints.

| Prefix | Format/content | Maximum age | Size budget |
| --- | --- | --- | --- |
| `trace-` | Versioned JSON Lines; opt-in Debug | 72 hours | 32 MiB |
| `application-` | Versioned JSON Lines; operations, failures, logger health | 168 hours | 64 MiB including session markers |
| `response-` | Versioned sanitized provider JSON envelopes | 168 hours | 128 MiB including stages |
| `critical-` | Independent fatal incident with safe stack and recent event references | One calendar month | 32 MiB |

Each ordinary event file rolls at 8 MiB or a UTC day change. A filename records the
earliest timestamp; expiry can remove newer entries in that file early. Size eviction
also shortens availability. The app prunes at initialization, hourly and on writes;
there is no cleanup service while it is closed. Read previews exclude expired files
and records. Unknown files are preserved; redirected roots/files are rejected.

Events have `schemaVersion`, `timestamp`, `severity`, `eventId`, `processId`,
`sessionId`, and `sequence`. Use the sequence and `operationId`/`parentOperationId`
to follow concurrent work. `accountReference` is a random session-local reference,
never an email or hash of one. Operation outcomes include cancellation, failed work,
reauthentication and stale fallback. Safe exception details retain type, HResult,
bounded inner chains and application symbol locations. Unapproved messages and Data
are omitted. Source locations are repository-relative when symbols provide them.

Each `HttpCompleted` event names a capture ID and terminal status. A later
`CapturePersisted` confirms that the matching response artifact was committed; the
first event alone does not. Artifacts may subsequently expire. Metadata contains a
fixed route template, never query values, credentials, request bodies or headers.
The body is the bounded response seen before DTO projection, **after sanitization**.
It is not a raw-response archive or a fixture to feed directly into the parser.

`completeness` distinguishes complete sanitized structure, withheld values, disabled
capture, malformed/empty/oversized replies, cancellation, timeout and network failure.
Redactions identify the output location, original JSON type and reason. Unknown
property names become ordinal placeholders; their values remain withheld until an
explicit endpoint policy classifies them. Null replacements cannot be interpreted as
provider-sent nulls without checking redactions. Numeric tokens retain their original
precision; duplicate properties remain present and are flagged. No malformed text
fallback is saved. Bodies can contain untrusted provider data even after projection.

Local switches, set before launching the process:

- `AIU_LOG_TRACE=1`: enable Debug/Trace files; absent by default.
- `AIU_LOG_BODIES=0`: withhold bodies while retaining HTTP outcome metadata.

`LoggerHealth` reports cumulative dropped records; `CaptureLost` identifies rejected
captures when the event queue is available. Check these before concluding that an
absent response means no request ran. Storage failures do not replace application
faults. Repeated binding/dispatch warnings are summarized with count and first/last
times. Normal output is bounded to 4,096 pending entries and 16 MiB; critical output
bypasses that queue and forces the file to disk before a two-second best-effort drain.
Normal writes run off the UI thread. Forced kill, power loss, native corruption and
unavailable/stalled storage cannot guarantee a final event; `PreviousExitUnknown`
means an incomplete session, not proof of an exception.

Sign-out/settings reset do not restart retention. Future explicit stored-data/factory
reset must stop writers before calling `DiagnosticFiles.DeleteOwned`; reset UI remains
AIU-013. Cleanup recognizes only versioned names and the old `diagnostics.v1.log`.

For parser development, locate the successful quota request/capture and its operation
outcome. Inspect the endpoint policy and redactions before drawing conclusions. Use
separately authorized live evidence to classify missing fields; never guess withheld
values. Create a synthetic fixture and review privacy again before committing or
exporting anything. Never commit runtime logs or copy them into a chat automatically.
Coverage and unverified cases are in [AIU-043 verification](../specs/AIU-043-file-logging/verification.md).
