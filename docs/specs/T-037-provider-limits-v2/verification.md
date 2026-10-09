# T-037 verification

Initial execution: 2026-10-03, Windows, .NET SDK 10.0.401, Release. Base 954ae68, clean main;
planning save point c9dff85; implementation WIP save point a3ef130, pushed to origin/main.
Evidence applies to that implementation's source files, not to later code changes. All payloads/grants are synthetic;
filesystem tests use unique temporary owned directories.

## Final acceptance evidence

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | ProviderLimitParserTests: 37 cases cover all represented mapping rows, absent/null/zero/unlimited limit states, signed balance, nested spend_control.individual_limit, opaque scopes, UTC calendar arithmetic and reset precision. UI-only families are not synthesized. |
| AC-02 | PASS | QuotaV2StorageTests and ProtectedQuotaMigrationTests round trip native facts in all four stores, including money, explicit null, opaque currency/bucket/plan values, secondary strings and unknown units. Allowlisted normalized output excludes synthetic unknown payload fields. Offline console also preserves quantity values. |
| AC-03 | PASS | Frozen v1 compatibility records and protected migration tests preserve every nonquota grant field, revision and parent. Invalid/unmappable or oversized normalized cache alone drops; pending successors promote before checkpoint. Interrupted cutovers retry, future versions/checkpoint mismatches fail closed, cleanup includes migration artifacts, and redirected paths preserve outside sentinels. |
| AC-04 | PASS | QuotaObservationRecorderTests and persisted-history test reopen LocalBudgetStore and prove compatible legacy continuation, native CX-I capture, retention of unrelated history and no duplicate native copy. Ambiguous scoped identities have no guessed alias. Incompatible units, balance direction and mixed histories remain separate. |
| AC-05 | PASS | Independent review executed on the frozen implementation and identified four P2 findings. All are resolved in f844c33 with the correction checks below. Final primary acceptance follows CONTRIBUTING: targeted checks after fixes, without an automatic full-review loop. No second independent verdict is claimed. |

Research mapping coverage: CL-S/CL-W/CL-M use separate percentage facts; CL-X/CL-D are
one monetary extra-usage key with current-over-legacy precedence. CX-P/CX-S/CX-A are
independent windows; CX-B is signed balance; CX-I uses percentages and opaque secondary
amounts. GH-C/GH-I/GH-P retain request amounts, percentages and overage independently.
AG-5/AG-W preserve groups/buckets; AG-R remains secondary unknown-unit data. All four
providers have explicit tests showing absent UI-only pools are not invented.

## Initial implementation checks (a3ef130)

| Check | Result on 2026-10-03 |
| --- | --- |
| Infrastructure: dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo | PASS: 521 tests, 0 errors/failures/skips/not-run; 12.386 seconds. |
| Presentation: dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo | PASS: 179 tests, 0 errors/failures/skips/not-run; 0.635 seconds. |
| Windows consumer: dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore | PASS: 0 warnings, 0 errors; 35.19 seconds. Compilation only. |
| Offline console: dotnet run --project tools/AiUsage.ProviderConsole -c Release --no-restore -- inspect tests/windows/AiUsage.Infrastructure.Tests/Fixtures/codex-usage.synthetic.json | PASS: parsed output contains normalized Used.Value = 25, not an empty quantity object. |
| Document validator: dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json | PASS: final result valid=true, diagnostics empty. Initial missing design frontmatter corrected. |
| git diff --check and git diff --cached --check | PASS at publication after normalizing authored line endings and trailing blank lines, including new files. |
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

## Initial external review attempt (historical)

BLOCKED. On 2026-10-03 a fresh external Claude CLI invocation requested
claude-opus-5-5 with safe mode, no tools, no MCP servers and no session persistence.
Its frozen input SHA256 was
DEC57B8F29C3C38D394FCF56D24541BC1308326961B51FBBD0492BAA70413BC7.
The CLI returned HTTP 429 / weekly usage limit before any model tokens or review.
That attempt produced no verdict. Subsequent targeted primary fixes/tests and final consumer changes
must be included in the eventual review; the attempted bundle is not final approval.
At that save point, Codex subagents remained disabled, feature status was blocked, and the main commit was
an explicitly incomplete save point under CONTRIBUTING, not acceptance/release approval.

## Provider evidence boundary

Provider mapping is derived from the accepted T-034 research sections 3 and 5.5/5.7,
whose pinned source inspection is dated 2026-09-26. This task does not advance any
provider's source_verified_at or live_verified_at. Authentication and quota method
classifications/reuse limitations remain as recorded in docs/providers. Synthetic
parser/storage execution establishes neither current account availability nor
permission to reuse the provider clients.

## Independent review and resolution (2026-10-03)

The owner requested a sole-reviewer, read-only review in this session, without subagents,
other CLI reviewers or other conversations. The configured Codex reviewer inspected
a3ef130 against 954ae68, the actual surrounding code/tests and the required specifications.
The independent verdict was FAIL with four P2 findings. Verification.md was treated as
reported evidence. Reviewer execution independently passed Infrastructure 521/521,
Presentation 179/179, document validation, diff check and offline console serialization.

Synthetic probes reproduced all four findings. Additional DPAPI probes for all three
protected stores passed cancellation/retry and checkpoint non-replay checks after a
later synthetic grant save: corrupt committed data required recovery, and missing
committed data did not load the predecessor checkpoint. Cleanup retained only locks.

The owner subsequently authorized implementation and completion in this same session.
The reviewer then acted as primary implementer. This does not change the original
verdict or create a second independent review. CONTRIBUTING explicitly requires
targeted checks after fixes without an automatic full-review loop.

| Finding | Correction in f844c33 | Regression evidence |
| --- | --- | --- |
| P2: v1 cache expansion can exceed the protected v2 size bound and block a valid grant | Drop only the migration cache when serialization exceeds the bound, revalidate state/lineage, retain the verified v1 ciphertext and stage normally; still refuse a state that cannot fit without cache | OversizedNormalizedCacheDropsAloneDuringMigration: Claude, Copilot and Antigravity; every nonquota field preserved, interrupted replacement leaves original bytes, retry/reopen succeeds |
| P2: history alias joins percentages and request counts | Resolve an alias only when every retained reading has a compatible quantity kind/unit and balance direction | HistoryAliasRequiresCompatibleCounterUnitsAndDirection: percentage, balance-direction and mixed-series rejection; equivalent request series continues |
| P2: unknown Claude kinds share a native identity for the same scope | Include opaque kind plus scope in unknown-kind keys; scope uniqueness by family | ClaudeUnknownKindsKeepDistinctStableKeysForTheSameScope: distinct keys remain stable across reorder and omission |
| P2: unlimited Copilot rows lose independently reported percentages | Read native percentages directly from the source; retain the compatibility presentation behavior | UnlimitedCopilotRetainsIndependentNativePercentages: 73% remaining/27% used and unlimited state coexist |

Initial focused execution reproduced seven failing cases (69 total); one compatible
history case was already passing. After production fixes the same focused set passed
69/69. Final coverage also exercises interrupted oversized-cache recovery and a mixed
legacy series whose latest entry already uses requests. No product fix was implemented
before its reproducing regression failed.

## Correction verification (f844c33)

Windows, .NET SDK 10.0.401, Release, synthetic data and temporary owned directories.
No dependency/package installation, real credential access, live sign-in or host-trust
change was performed.

| Executed check | Result |
| --- | --- |
| dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo | PASS: 530 tests; 0 errors/failures/skips/not-run; 12.497 seconds |
| dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo | PASS: 179 tests; 0 errors/failures/skips/not-run; 0.626 seconds |
| dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore | PASS: 0 warnings/errors; 39.57 seconds; compilation only |
| Integrated source/diff and acceptance review | PASS: all four findings resolved; cache removal policies change only CachedQuota, repeat bounds/validation, retain checkpoint, zero plaintext and preserve lineage; aliases inspect all retained units/directions; native keys retain kind; presentation fields remain compatible |
| dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json; git diff --check; git diff --cached --check | PASS: valid=true, diagnostics empty, no whitespace errors |

Final acceptance is PASS after finding resolution and primary verification. The
independent baseline verdict remains FAIL; no fresh independent PASS is claimed.
Live providers, interactive UI, installed-package upgrades, cross-user DPAPI isolation,
actual power-loss durability and same-user filesystem substitution races are NOT_RUN
or outside the verified boundary. The earlier provider source/live dates are unchanged.

## Execution ledger

Collapsed from tasks.md on 2026-10-09 (OD-19); the full plan is in Git history at 04c2900.

- T-037.1 Native parser facts: done; commits a3ef130; review within the T-037.3 independent review; checks targeted ProviderLimitParserTests; grant not recorded.
- T-037.2 Stored v2 migration: done; commits a3ef130; review within the T-037.3 independent review; checks targeted QuotaV2StorageTests and ProtectedQuotaMigrationTests; grant not recorded.
- T-037.3 History and integrated verification: done; commits a3ef130, f844c33; review independent FAIL on 954ae68..a3ef130 with four P2 findings, resolved in f844c33 (primary acceptance, no second independent verdict); checks C2, C4 (530/530), C5 (179/179), C6, C7, offline console inspection; grant owner, 2026-10-03 (review, then correction and completion).

Kept from the plan:

- V1 reset precision stays Unknown; ambiguous scoped histories stay separate; CX-I raw amount strings remain opaque.
