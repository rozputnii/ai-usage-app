# AIU-037 verification

Execution: 2026-10-03, Windows, .NET SDK 10.0.401, Release. Base 954ae68, clean main;
planning save point c9dff85. Evidence applies to the AIU-037 implementation save point
and its final source files, not to later changes. All payloads/grants are synthetic;
filesystem tests use unique temporary owned directories.

## Acceptance evidence

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | ProviderLimitParserTests: 35 cases cover all represented mapping rows, absent/null/zero/unlimited limit states, signed balance, nested spend_control.individual_limit, opaque scopes, UTC calendar arithmetic and reset precision. UI-only families are not synthesized. |
| AC-02 | PASS | QuotaV2StorageTests and ProtectedQuotaMigrationTests round trip native facts in all four stores, including money, explicit null, opaque currency/bucket/plan values, secondary strings and unknown units. Allowlisted normalized output excludes synthetic unknown payload fields. Offline console also preserves quantity values. |
| AC-03 | PASS | Frozen v1 compatibility records and protected migration tests preserve every nonquota grant field, revision and parent. Invalid/unmappable cache alone drops; pending successors promote before checkpoint. Interrupted cutovers retry, future versions/checkpoint mismatches fail closed, cleanup includes migration artifacts, and redirected paths preserve outside sentinels. |
| AC-04 | PASS | QuotaObservationRecorderTests and persisted-history test reopen LocalBudgetStore and prove compatible legacy continuation, native CX-I capture, retention of unrelated history and no duplicate native copy. Ambiguous scoped identities have no guessed alias. |
| AC-05 | BLOCKED | All local checks below pass; primary integrated diff review completed. Required independent review did not execute because the external reviewer was rate limited. No independent PASS is claimed. |

Research mapping coverage: CL-S/CL-W/CL-M use separate percentage facts; CL-X/CL-D are
one monetary extra-usage key with current-over-legacy precedence. CX-P/CX-S/CX-A are
independent windows; CX-B is signed balance; CX-I uses percentages and opaque secondary
amounts. GH-C/GH-I/GH-P retain request amounts, percentages and overage independently.
AG-5/AG-W preserve groups/buckets; AG-R remains secondary unknown-unit data. All four
providers have explicit tests showing absent UI-only pools are not invented.

## Executed checks

| Check | Result on 2026-10-03 |
| --- | --- |
| Infrastructure: dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo | PASS: 521 tests, 0 errors/failures/skips/not-run; 12.386 seconds. |
| Presentation: dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo | PASS: 179 tests, 0 errors/failures/skips/not-run; 0.635 seconds. |
| Windows consumer: dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore | PASS: 0 warnings, 0 errors; 35.19 seconds. Compilation only. |
| Offline console: dotnet run --project tools/AiUsage.ProviderConsole -c Release --no-restore -- inspect tests/windows/AiUsage.Infrastructure.Tests/Fixtures/codex-usage.synthetic.json | PASS: parsed output contains normalized Used.Value = 25, not an empty quantity object. |
| Document validator: dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json | PASS after correcting design frontmatter; final documentation rerun recorded with publication. |
| git diff --check | PASS after normalizing authored line endings and trailing blank lines; staged check included with publication. |
| Live-provider authentication/quota | NOT_RUN; no live credentials/import/consent authorized or needed for these parser/migration fixtures. |
| Interactive Windows/package upgrade | NOT_RUN; no visual/activation/package changes. The consumer build does not establish UI or installed-package migration success. |

The first seven native parser probes failed before normalization; the five initial
storage probes failed before v2 migration/codecs. Source cross-check corrected the
initial CX-I fixture to use spend_control.individual_limit. A failing offset-boundary
test led to UTC month arithmetic. A failing duplicate-window v1 cache test led to
cache-only recovery after normalization, retaining the grant. Console inspection
initially printed empty abstract quantities; the two console contexts now register
the Infrastructure codecs. Initial migration-test failures were traced to a fixture
helper zeroing JsonDocument's backing buffer too early; corrected before drawing
production conclusions. No failing result is counted as successful evidence.

## Primary security-lifecycle review

Source inspected: ProviderStatePolicy, ProviderStateLease, ProviderStateMigration,
ProviderStatePaths, all four state/cache policies and codecs, parser mapping,
CompatibleReadingSeries and QuotaObservationRecorder; integrated tests/consumers.

- Core remains credential/transport/serialization independent. No request, endpoint,
  grant, scope, source credential import or UI-only pool was added.
- Protected migration retains DPAPI CurrentUser with the original purpose bytes.
  The verified v1 checkpoint and format stage contain ciphertext only. The exclusive
  lease serializes against refresh/sign-out; migration does not stamp a new revision.
- Pending grant successors are resolved first. Checkpoints are never automatic grant
  rollback sources and do not override a later committed generation. A foreign or
  mismatched checkpoint blocks migration rather than overwriting recovery evidence.
- Invalid v1 cached data is isolated from strict identity/token/version decoding.
  Duplicate native identities that become unmappable also discard only the cache.
  Unsupported protected versions remain intact and require recovery.
- Checkpoint and stage paths are exact owned names, size bounded and checked for
  reparse points with their ancestors. Sign-out removes these provider artifacts;
  independent budget history remains. Same-user path-substitution races remain the
  pre-existing residual limitation, not a promised security boundary.
- Per-file migration is not a cross-provider transaction. A failed cache-only Codex
  cutover preserves the previous bytes/checkpoint and retries on the next read.
- Compatible series are resolved, not copied/merged. New facts retain explicit
  aliases only where equivalence is known. Ambiguous Claude slugged scopes survive
  in separate old files; future UI work must not silently merge them.

This is the author's review, not the required independent verdict.

## Independent review attempt

BLOCKED. On 2026-10-03 a fresh external Claude CLI invocation requested
claude-opus-5-5 with safe mode, no tools, no MCP servers and no session persistence.
Its frozen input SHA256 was
DEC57B8F29C3C38D394FCF56D24541BC1308326961B51FBBD0492BAA70413BC7.
The CLI returned HTTP 429 / weekly usage limit before any model tokens or review.
No verdict exists. Subsequent targeted primary fixes/tests and final consumer changes
must be included in the eventual review; the attempted bundle is not final approval.
Codex subagents remain disabled. Feature status stays blocked, and the main commit is
an explicitly incomplete save point under CONTRIBUTING, not acceptance/release approval.

## Provider evidence boundary

Provider mapping is derived from the accepted AIU-034 research sections 3 and 5.5/5.7,
whose pinned source inspection is dated 2026-09-26. This task does not advance any
provider's source_verified_at or live_verified_at. Authentication and quota method
classifications/reuse limitations remain as recorded in docs/providers. Synthetic
parser/storage execution establishes neither current account availability nor
permission to reuse the provider clients.
