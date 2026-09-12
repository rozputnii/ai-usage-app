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
Normal history contains normalized data only. Failure diagnostics are safe allowlisted structure (field/type info, redacted values where safe); unknown payload not blindly saved after regex substitutions. Seven-day retention; manual export re-sanitizes IDs/email/paths. Full exception objects and request/response bodies aren't generic log arguments. No automatic network telemetry.

## Security tests before relevant features merge
- Cross-account token/grant mismatch rejected; rotating refresh pair preserved through quota cancellation.
- Cancel/disconnect/reimport prevents old generation DB writes/store updates/alerts.
- Every secret write interrupted at staged/replace/ref-update phase has defined recovery.
- Backup during WAL writes is consistent; no live -wal deletion.
- Partial migrate/restore/replace/reset fails recoverably without scope escape.
- Sanitizer tested with nested unknown secret fields and malicious names; unsafe export blocked.
- Cleanup with malicious relative path/reparse points does not delete outside roots.
- Same package upgrade retains credentials; local remove/reset cleans owned records under tested Windows settings.

## Deferred security scope
Manifest PKI/expiry/replay and actual signing legal-identity eligibility are AIU-015/014, not bootstrap code. No custom crypto framework or secret manager server before needed.
