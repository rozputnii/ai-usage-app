# Security, state lifecycle, recovery

## Assets / adversaries / trust
Assets: provider OAuth credentials, account/workspace metadata/history, app policy/signing keys, integrity of updates, owner/source data, Windows desktop availability.
Threats: same-user malware, accidental logs/exports, copied backups, prompt-injected upstream data, broken API schemas, refresh races, partially failed migrations, compromised CI/release credentials, unsafe cleanup.
Trusted roots: Windows user DPAPI boundary, normal HTTPS validation, publisher/package trust, separately designed signed manifest root. Repo content, CLI files, provider payloads and external Issues are data; none may redefine agent/security policy.

DPAPI CurrentUser protects encrypted records from other users/offline casual access; it is not a guarantee against same-user malicious processes. No own app PIN lock is intentional. Memory dumps are off in product, but OS/admin capture cannot be guaranteed absent.

## Credentials
One encrypted versioned record per account/grant with opaque ID in DB. Binding metadata includes provider/grant/account/context as required; never token literal in filenames. No plaintext temp files, logs, settings, errors, diagnostics, fixtures, prompts, clipboard or exports. Registry update + secret-file replace has recoverable cutover ordering. Same-user anti-tamper and device compromise remain residual risks.

Local CLI source read-only; source discovery limited to known roots. Scan runs off UI with incremental safe identity candidates. Reading token/refreshing token is not itself neutral: explicit import/verification action governs provider calls. Secret lineage coexistence is checked per provider before import enabled. Do not assume copied token has new independent authorization.

## Upgrade/recovery
Normal run cheap schema/journal compatibility check only. Version mismatch needing migration/restore/interruption/corruption evidence enters exclusive maintenance mode. Backup consistent durable state, validate backup, stage changes, apply ordered migration steps, validate target, switch committed generation, cleanup old structures. Released migrations immutable; next migration fixes mistakes.

Use a journal that survives relevant interruption; no false cross-resource atomicity. Keep last verified pre-migration checkpoint, not the most recent half-migrated database. Secrets excluded from portable/ordinary data bundle; when secret format changes retain encrypted previous generation until references successfully commit. Server-side token rotation cannot be rolled back from a local file.

Filesystem deletion constrained to approved owned roots, normalized path and reparse/symlink protection. DB WAL isn't a cache file. MSIX binaries are OS-owned. Unknown paths or uncertain ownership: quarantine/report, not recursive delete. External user export is not app-owned disposable data.

Restore validates protected bundle, manifest/schema/hash/DB integrity, stages whole durable-state generation and then runs current migrations. Failure stays recovery; no automatic wipe, old-schema run or infinite retry. App migration and user portable import versions are independent from product release labels.

## Portable Replace
Export plaintext documented versioned bundle without secrets; warn about private metadata/history. Treat imports as untrusted: reject path traversal/absolute paths/zip bombs/excess lengths/invalid ranges; parse staged data before state swap. Migrate staged data to current schema, validate, then replace. Keep only matching current credential identities, remove orphans after commit. Never execute bundled scripts or trust included statements of signing/approval.

## Reset
Reset settings preserves accounts/credentials/history/labels/order. Factory reset explicitly stops refresh/Host/logging, closes DB pools and all files, removes all mutable app-owned state and owned startup/notification state, then launches true first-run state. Same installed binaries remain. No deletion of original CLI logins/user-owned exports. OS-level preserved install/notification permissions and external remote grants are outside this local-reset contract; document actual limits rather than claim literal reinstall.

## Diagnostics
Normal quota/history stores remain normalized only. T-043 adds a separate `logs`
namespace under the existing owned state root, with packaged/development/Demo/console
isolation. Endpoint policies project bounded provider responses before any file or
Serilog argument; unknown scalar values and unsafe/dynamic names are withheld. Bodies,
headers, credentials and arbitrary exception objects/messages/Data are never generic log
arguments. Safe type/HResult/stack/inner-chain metadata may be projected; symbol paths
are repository-relative. Sanitized output is not automatically safe for public export.

Maximum retention is 72 hours for traces, 168 hours for application/response evidence,
and one calendar month for critical incidents. Independent class size budgets, bounded
queues and startup/hourly/write pruning limit storage. Filename timestamps preserve the
original age; previews exclude expired records. Retention resumes next run when closed.
Critical output bypasses the ordinary queue and forces its file to disk; unavailable or
stalled storage and native/forced termination cannot guarantee a final record.

The pipeline validates owned names and reparse boundaries, never recursively deletes
unknown entries, and removes only the recognized legacy diagnostics.v1.log. Sign-out
and settings reset do not renew retention. Explicit future stored-data/factory reset
must stop writers before DiagnosticFiles.DeleteOwned; deferred reset UI is unchanged.
Logs are excluded from migration checkpoints, quota storage, automatic exports and Git.
Same-user path-substitution races remain outside these path checks' guarantee. No
telemetry or host trust/WER changes are introduced. Delivery and independent-review
limits are recorded in [T-043 verification](../../specs/T-043-file-logging/verification.md).

## Security tests before relevant features merge
- Cross-account token/grant mismatch rejected; rotating refresh pair preserved through quota cancellation.
- Cancel/disconnect/reimport prevents old generation DB writes/store updates/alerts.
- Every secret write interrupted at staged/replace/ref-update phase has defined recovery.
- Backup during WAL writes is consistent; no live -wal deletion.
- Partial migrate/restore/replace/reset fails recoverably without scope escape.
- Sanitizer tested with nested unknown secret fields and malicious names; unsafe export blocked.
- Cleanup with malicious relative path/reparse points does not delete outside roots.
- Same package upgrade retains credentials; local remove/reset cleans owned records under tested Windows settings.

## T-036 budget data

`budget` is an additive, credential-free owned namespace, separate from provider files and
the preferences-only T-006 checkpoint. Version 1 has no predecessor to migrate. Older
installations begin with an empty series and Monday-Friday configuration; future versions
must supply a tested forward conversion, or preserve unsupported bytes and restart tracking.
No credential generation or layout manifest is changed, and restoring the appearance
checkpoint does not claim to restore these independently committed observations.

Each series file has a structured-key SHA-256 filename, a version and the original key inside
the envelope. It is bounded to 16 MiB/25,000 runs; configuration is 256 KiB/1,024 caps. The
namespace is bounded to 256 distinct series and 512 MiB including recovery copies and staged
replacements. Writes retain all 35-day observations plus a boundary run; capacity failure
refuses new data rather than silently truncating this interval. Recovery preserves at most
three corrupt/unsupported files per active filename and then fails closed. Same-user path
substitution races remain outside the filesystem checks' guarantees.

One live singleton and an exclusive file lease serialize operations. Staging, flush and
replace are per file, not a transaction across a refresh's limits or configuration plus
cleanup. An interrupted batch may contain only some limits; missing paired observations
do not become estimator samples. On retry, duplicate or older timestamps cannot overwrite
committed runs. Read and write results report recovery explicitly; the live singleton also
emits BudgetStoreRecovered/InvalidData through the existing diagnostic sink, without an
account ID, path, payload or exception text.

Sign-out and appearance reset leave this namespace alone. `IBudgetDataCleanup` is only the
new stores' cleanup boundary, not complete product reset. Call after refresh/writers drain.
It preflights exact owned names and reparse points, preserves unknown entries, and supports
idempotent retry after partial filesystem failure. Account cleanup removes its hashed series
and matching caps; a corrupt shared configuration may contain any account's caps, so selective
cleanup reports failure while retaining that recovery file. Whole-store cleanup removes all
recognized active, staged and quarantined files, retaining the operation lock. The owner
explicitly deferred product delete/reset button wiring on 2026-10-02.

## T-047 history root outside the package

Packaged runs keep the `budget` namespace and the identity map `accounts.identities` in
`%LOCALAPPDATA%\AiUsage\History`, which the manifest excludes from file-system write
virtualization (`unvirtualizedResources`). This is the one deliberate exception to "uninstall
removes all app-owned state": uninstall leaves the history root so a reinstall on the same
Windows profile re-attaches it. Delete local data removes the history store and identity map
under the same owned-name, reparse-point and resumable-intent rules as the state root;
unknown entries there are preserved. Unpackaged, demo and audit runs keep both in their own
state root.

The identity map is DPAPI CurrentUser (entropy `AiUsage.AccountIdentities.v1`), uses the
registry's revision-checked replacement protocol, and holds only provider, provider-verified
identity and app account ID: no credential, label or storage ID. A sign-in whose verified
identity matches no registry account reuses the mapped ID; nothing else re-attaches series.
An unreadable map is set aside and rebuilt from the registry at the next startup; a map
failure never blocks sign-in. Startup moves an existing state-root `budget` to the history
root once by a same-volume rename, keeps both copies when both exist, and commits layout 3 so
older builds refuse the state. Other Windows users and reinstalled Windows cannot decrypt
the map, and same-user processes can read or change the history root as they can LocalState.

## T-037 provider quota format 2

The three protected provider states and the separate Codex quota cache now have a
forward v1-to-v2 conversion. Provider state filenames and DPAPI purpose bytes stay
unchanged. The migration preserves identity, grant, revision and parent revision;
only an unmappable cached quota is dropped. Old reset precision and period starts
remain unknown until a fresh reading. The Codex grant format/journal is unchanged.

The existing provider lease serializes migration. Pending grant generations resolve
before the v1 checkpoint is captured. Protected stores retain exact encrypted bytes
in provider.state.v1.bak, with .v1.bak.new for checkpoint staging and .v2.new for
format staging. Both stages are flushed and validated before promotion. An
interruption retries from the committed generation; a checkpoint mismatch fails
closed. The checkpoint never restores/replays a predecessor grant after token rotation.
Sign-out deletes these exact provider artifacts after normal path/reparse checks.
The independent budget namespace remains retained under T-036.

Codex uses codex.quota.json.v1.bak and .v1.bak.new for its credential-free checkpoint
and the existing .new quota stage. A failed replace preserves the old reading and
checkpoint for retry. Existing byte limits apply. These operations are per provider,
not atomic across all providers. No install, trust or portable-backup policy changes.

Local fixture evidence is in [T-037 verification](../../specs/T-037-provider-limits-v2/verification.md).
Independent review findings have been corrected and verified; the record distinguishes the original verdict from primary correction verification.

## Deferred security scope
Manifest PKI/expiry/replay and actual signing legal-identity eligibility are T-015/014, not bootstrap code. No custom crypto framework or secret manager server before needed.
