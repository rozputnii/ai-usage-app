# Technical audit and corrections

Historical evidence: the executable workflow and its instructions were retired on 2026-09-14. Runtime commands, permission records and language prescriptions below describe the past bootstrap only; current development follows CONTRIBUTING.md. Archived sources map to docs/archive/omp/<original path>. Provider-source provenance remains unchanged.

This is not a new product questionnaire or a change to accepted intent. It separates source-recorded facts, implementation safety reasoning and experiments. Address each item when its AIU needs it, not all before the first code commit.

S identifiers refer to `../research/sources.md`. This English revision preserves the prior evidence record; it does not claim fresh authenticated or Windows runtime verification.

## TA-01 - Commands are not a finished workflow
**Basis:** S-006, S-008.

**Correction:** The earlier /work, /auto, /status, /pause and /resume examples described desired UX, not implemented commands. Native built-ins take precedence.

**Implementation consequence:** AIU-001 must implement the thin command/extension surface and test discovery/collisions. Documentation alone is not a working dispatcher.

## TA-02 - The project DAG is not the native scheduler
**Basis:** S-004.

**Correction:** Native task batching and isolation do not automatically parse our tasks.md or validate an arbitrary writes/shared format.

**Implementation consequence:** Build a small tested project parser/coordinator for eligibility and diff validation. Nonoverlapping globs alone do not prove semantic independence.

## TA-03 - YOLO and hooks are not a sandbox
**Basis:** S-005.

**Correction:** Bash patterns do not cover every execution path. Eval or another program can bypass command-string checks. An isolated worktree does not remove access to the home directory, network or Git credentials.

**Implementation consequence:** Keep approved autonomy, but enforce main/signing/Stable/manifest boundaries through remote permissions and protected checks. Limit worker tools and validate actual diffs; do not promise hard containment from text rules.

## TA-04 - Local memory may be neither fresh nor offline
**Basis:** S-003.

**Correction:** Storage is local, but extraction/consolidation uses configured models. Idle/scan policies can delay a new summary.

**Implementation consequence:** Persist task state, authorization and gates explicitly. Auto-learn is disabled, so do not require the learn tool. Keep personal secrets out of transcripts.

## TA-05 - Fresh-session Autopilot needs explicit scope transfer
**Basis:** S-007.

**Correction:** Ordinary goal resume can pause the goal. Aggregate worker/advisor budget preservation across arbitrary new sessions has not been proven.

**Implementation consequence:** Test scoped authorization, handoff, accounting and pause in AIU-001. Never reset consumption by creating a new goal or replace the selected lifecycle with an unbounded loop. Report unavailable primitives honestly.

## TA-06 - Native todo is not concurrent project state
**Basis:** S-004.

**Correction:** The session HUD is not the canonical backlog or an adequate representation of multiple concurrent worker states.

**Implementation consequence:** Let the HUD show the current batch while tasks.md retains authoritative status/ownership. Only the primary writes shared execution state.

## TA-07 - A CLI token copy is not an independent OAuth grant
**Basis:** S-010.

**Correction:** A copied refresh token may remain in the same rotating token family as the CLI. Both consumers can invalidate one another.

**Implementation consequence:** Before enabling import, prove provider-specific coexistence and refresh behavior. Offer a separate browser grant when reuse is unsafe. Never modify the source CLI store or promise independence without testing.

## TA-08 - Manual cancellation has a boundary
**Basis:** S-010; implementation reasoning.

**Correction:** Canceling a quota request does not undo a server-side token exchange. Cancellation can lose a newly rotated token if persistence is abandoned.

**Implementation consequence:** Use one refresh authority per credential family. Safely persist a returned token pair despite cancellation of an observing UI caller, unless the account generation was deleted. Fence quota publication by account/request generation.

## TA-09 - Independent accounts do not imply no rate limits
**Basis:** S-015; implementation reasoning.

**Correction:** Servers may limit an endpoint, IP address, client or grant. A shared circuit breaker may also block healthy accounts unnecessarily.

**Implementation consequence:** Preserve the choice of no arbitrary global cap, but respect actual 429/Retry-After responses and partition policy appropriately. Do not hammer endpoints.

## TA-10 - Blanket POST retries are unsafe
**Basis:** S-015.

**Correction:** A standard resilience handler can retry all methods; authorization-code redemption and rotating-token POSTs are not universally retry-safe.

**Implementation consequence:** Use separate usage/authentication/update pipelines. Do not hedge token exchange or blindly repeat code redemption after an ambiguous timeout.

## TA-11 - SQLite Async can still block the UI
**Basis:** S-011.

**Correction:** Async naming in Microsoft.Data.Sqlite does not imply asynchronous storage I/O.

**Implementation consequence:** Run database operations, history queries and migrations off the dispatcher. Test responsiveness. ConfigureAwait(false) or AsNoTracking alone does not establish a worker-thread boundary.

## TA-12 - UTC semantics and database representation differ
**Basis:** S-009.

**Correction:** DateTimeOffset comparisons/order may not translate appropriately in SQLite.

**Implementation consequence:** Preserve UTC semantics but select and test a SQLite-friendly UTC DateTime or numeric epoch/tick representation with server-side filtering/indexing. Do not silently switch large queries to AsEnumerable.

## TA-13 - WAL backup and cleanup
**Basis:** S-012; implementation reasoning.

**Correction:** Copying only the main database during WAL writes can miss committed data. WAL and SHM files are not disposable garbage.

**Implementation consequence:** Use a consistent backup API or coordinated closed/checkpointed database. Never delete active WAL files manually. Coordinate retention with the writer.

## TA-14 - Database, files and secrets are not one transaction
**Basis:** Implementation reasoning.

**Correction:** An EF transaction does not atomically cover filesystem moves, encrypted records and server-side token rotation.

**Implementation consequence:** Use recoverable journals/staging and defined cutover order. Fault-inject crashes at boundaries. Never replace a good backup with a corrupted or partially migrated retry state.

## TA-15 - Source generation does not repair schema drift
**Basis:** S-019.

**Correction:** Reflection changes metadata access, not knowledge of new quota semantics.

**Implementation consequence:** Use source-generated stable DTOs and explicit dynamic adapters for known variants. Fail typed for critical unknown semantics. Missing optional quota numbers remain unknown.

## TA-16 - Deployment dependencies are separate
**Basis:** S-014.

**Correction:** Earlier discussion conflated .NET framework-dependent deployment with Windows App SDK framework packages.

**Implementation consequence:** Test both prerequisites on a clean machine in AIU-002/014. Do not describe App Installer as a universal installer for every .NET runtime prerequisite.

## TA-17 - Exact versions and routing remain untested
**Basis:** S-018.

**Correction:** The chat contained inconsistent claims about current Windows App SDK versions. Standalone WinUI integration of the selected navigation candidate has not been proven in this application.

**Implementation consequence:** Resolve actual stable versions and prove restore/build/launch in AIU-002. Preserve selected candidates; substantiate and escalate necessary substitution instead of silently changing to cross-platform Uno UI.

## TA-18 - Production signing eligibility is not automatic
**Basis:** S-013.

**Correction:** Public Trust individual onboarding has geographic/identity restrictions. A user location does not establish billing or legal identity.

**Implementation consequence:** Verify the actual entity before public distribution. Keep Azure as the preference without inventing eligibility or blocking local self-signed development.

## TA-19 - Date-based versions are not automatically monotonic
**Basis:** S-021; implementation reasoning.

**Correction:** DDNN needs an explicit limit after 99 builds in a day, and a delayed job or rerun may publish an older result.

**Implementation consequence:** Use UTC serialized allocation, bounded NN=01..99, unique versions for changed bytes and forward-only feed publication. Require a numbering amendment before exhaustion rather than silently rolling over.

## TA-20 - Stable promotion and channel switching
**Basis:** S-020; implementation reasoning.

**Correction:** Exact artifact promotion cannot rewrite a literal embedded preview label. Changing a SQLite channel setting alone does not necessarily update OS package-source association.

**Implementation consequence:** Keep the embedded version immutable and derive channel from user/feed policy. Test actual App Installer registration, Preview-to-Stable waiting and exact-byte artifact identity.

## TA-21 - DPAPI and uninstall boundaries
**Basis:** S-016, S-022.

**Correction:** CurrentUser is user-bound, not application-bound protection. Package paths do not magically protect against same-user processes or revoke remote grants.

**Implementation consequence:** Target normal cleanup of package-owned state without claiming secure erasure on SSDs/backups or remote revocation. Preserve user exports outside owned roots and exclude secrets from portable bundles.

## TA-22 - Factory reset requires coordinated handles
**Basis:** S-024.

**Correction:** Data clearing can fail while handles are open. A logger can recreate files after cleanup.

**Implementation consequence:** Stop producers, Host, logging and database pools; drain/cancel safely; clear owned data/registrations and restart into first run. Keep the same binary and bound retries.

## TA-23 - Quota history is not a spending counter
**Basis:** Implementation reasoning.

**Correction:** Percentages across resets, changed entitlements and shared pools cannot be summed or interpolated into usage demand.

**Implementation consequence:** Persist pool/window identity, observation freshness and gaps. Preserve relevant boundaries/extrema in rollups. Unknown/unlimited is not zero. Observe a reset before issuing factual reset/recovery claims.

## TA-24 - Fresh review is not automatic privilege isolation
**Basis:** S-005; implementation reasoning.

**Correction:** Changing models or disabling memory alone does not remove profile/global skills, advisor or tool access. One review can still miss defects.

**Implementation consequence:** Use explicit review overlays and read-only grants without inherited implementation narrative. Freeze the reviewed ref and retain evidence. PASS is not a security certification.

## TA-25 - Scheduled watchdog checks are best-effort
**Basis:** S-017.

**Correction:** Scheduled Actions may be delayed, dropped or disabled in inactive public repositories.

**Implementation consequence:** Expose check freshness/status and allow manual checks. No always-on runner is needed now; no code repair starts without an active authorized OMP session.

## TA-26 - Provider authorization remains separate
**Basis:** S-023.

**Correction:** MIT-licensed upstream code does not grant provider permission. The source record reports Anthropic restrictions; a quota-only exception was not established.

**Implementation consequence:** Do not restart a product-policy questionnaire during bootstrap. Record the concrete release risk in AIU-007 and recheck current terms. Existing OMP or iOS apps do not prove approval.

## TA-27 - Manifest trust still needs a complete threat model
**Basis:** Implementation reasoning.

**Correction:** An offline root does not prevent abuse by a compromised delegated signer authorized to disable capabilities. Expiry, offline behavior and replay after reset involve availability/security trade-offs.

**Implementation consequence:** Define delegation, revocation, version floors, expiry, offline and first-run behavior in AIU-015. Do not build a custom remote safety platform during bootstrap.

## TA-28 - 500 ms is a measurement target
**Basis:** Implementation reasoning.

**Correction:** A typical machine is not a reproducible benchmark. Activation, antivirus and runtime conditions vary.

**Implementation consequence:** Define reference hardware, OS, Release build, usable-window marker and warm/cold conditions. Profile actual behavior rather than removing required safety checks to satisfy an unverified number.
