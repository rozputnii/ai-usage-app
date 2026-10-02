# AIU-042 verification

Execution selected by the owner's 2026-10-02 Goal instruction. Baseline code: 66e3eb8.
Initial Git state: clean main tracking origin/main; no unrelated changes found.

| Criteria | Result | Evidence |
| --- | --- | --- |
| AC-01 | NOT_RUN | Baseline below passes; complete subsystem inventory is in progress. |
| AC-02 to AC-08 | NOT_RUN | Remediation, measurements and integrated verification are pending. |

## Baseline checks

2026-10-02, local Windows, pinned stable SDK 10.0.401, Release, warnings as errors.
`dotnet` below is the existing user-local `~/.dotnet/ai-usage-sdk/dotnet.exe`.

| Check | Result | Observation |
| --- | --- | --- |
| `dotnet --version` | PASS | 10.0.401 |
| `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS | 416 tests, 0 failed/skipped/not run; 14.947 s test execution. |
| `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS | 179 tests, 0 failed/skipped/not run; 0.701 s test execution. |
| Live provider / interactive UI | NOT_RUN | Baseline uses synthetic fixtures and isolated stores; no live or UI claim. |

## Review coverage

In progress. Each completed row will identify reviewed files, relevant failure boundaries,
and disposition. Baseline tests alone do not establish whole-backend audit coverage.

| Subsystem | Reviewed code and boundaries | Disposition |
| --- | --- | --- |
| Core contracts/calculations | All Core production files: Budget, Usage, History, Dashboard, Diagnostics, Persistence and Claude contracts. Unit/exponent arithmetic, DST, reset/day-start provenance, replay, session readiness, workflow cancellation and credential-free dependency direction. | F-01/F-02/F-03 below; no new abstraction warranted. Calendar extreme-date input remains under investigation. |
| Budget persistence | LocalBudgetStore, BudgetJsonFile, BudgetStoreRecords, QuotaObservationRecorder; leases, coalescing, retention, recovery capacity, staged replacement, exact-path cleanup and frozen DTO schema. | No storage modification yet; measurements quantify full-file cost. F-01 affects the pure transition called by append. |
| Shared state/recovery/diagnostics | ProviderStatePaths/Lease/Policy, all provider StateStore/StoredState types, CodexGrantStore/QuotaCache, StateMaintenance/MaintenanceRecords, PresentationPreferenceFile, LocalDiagnosticSink. | Generation/journal/reparse/ownership contracts inspected. Credential renewal investigation remains open (F-04); preference path validation also under investigation. |
| Session orchestration | All four provider Session types, Codex/Copilot history partials, credentials and authorization holders. | F-04 under investigation; distinct provider renewal policies retained. |
| Shared transport and quota | ProviderHttp/Transport/Options, LoopbackCallback, HistoryJson; Codex/Copilot/Antigravity quota parsers and all quota clients. Claude parser review partly complete. | Bounded response/deadline, cancellation, fixed endpoints, throttling, opaque values and unknown states inspected. Remaining auth/history/composition files pending. |

## Findings and remediation

| ID | Severity | Evidence and disposition |
| --- | --- | --- |
| F-01 | Moderate correctness | ReadingCalculations.Transition adds 60 seconds to a valid near-maximum reset and subtracts signed decimals without guarding overflow. Two new ReadingBoundaryTests failed with ArgumentOutOfRangeException/OverflowException before the fix. Local fix compares reset differences and handles a positive difference beyond decimal range. Targeted 36-test reading/store suite PASS. Not yet integrated. |
| F-02 | Moderate correctness | Track exposes LastConfirmed beyond a closed period's end. The correction clamps output confirmation to period.End while retaining evidence known at the actual replay time. Initial horizon-clamping implementation was rejected by independent review because it lost valid unchanged-run coverage; the corrected closed-period/zero-consumption regressions PASS. |
| F-03 | Measured performance | SessionEstimator.Cover scans every weekly run for each session endpoint. Local replacement uses binary search over Ordered's chronological nonoverlapping runs via ReadOnlySpan; gap and inclusive-endpoint regression passes before/after. Four-pair median fell from 39.180 to 15.137 ms; allocated bytes 6,525,416 to 6,421,736. No persistence or provider semantics change. Not yet integrated. |
| F-04 | Material authentication correctness | LostRenewalResponseCannotReplayTheGrantAfterRelaunch sent the same grant twice before correction. The owner authorized an existing protected-journal intent before renewal; resume, expired-access refresh and history now share that path. Interrupted/cancelled exchange blocks replay, returned successor recovers, known rate limiting preserves retry. Independent review found that fresh sign-in was blocked by the first prototype; successful/refused fresh-sign-in regressions failed then passed after a revision-bound replacement path. Focused follow-up review is pending; auth changes are not integrated. |
| F-05 | Moderate correctness | Under ar-SA, Codex history requested year 1451 instead of Gregorian 2029. The regression failed before invariant request-date formatting. Existing endpoints and report fields retained. |
| F-06 | Moderate correctness | Codex activity totals with scalar/array shape were returned as empty usage. Two regressions failed; reject the malformed shape using the existing InvalidResponse contract. No parser expansion. |
| F-07 | Minor robustness, fixed | Both history clients overflowed on valid Retry-After delta with a clock near DateTimeOffset.MaxValue. Two corrected probes failed before a saturating shared retry-date helper; the same helper removes duplicated quota-client logic and reads the clock once. Initial probe used an invalid header construction and was corrected before drawing this conclusion. |
| F-08 | Moderate correctness | CodexQuotaCache accepted null group/window structures and invalid percentages, violating its unusable-cache contract and allowing nulls into LiveMapping. Five synthetic corrupt-shape regressions failed before non-destructive validation; all six cache tests now PASS, including compatible unknown fields and opaque values. |

## Remediation checks and independent review (in progress)

- Infrastructure Release after core/history/journal prototype: PASS 436 tests, no failures/skips, 28.854 s.
- After fresh-sign-in correction: PASS 32 Codex session/journal tests, 4.141 s.
- Cache correction: PASS 6 tests, 1.519 s. New changes require a final integrated suite.
- External read-only Claude reviewer, model claude-opus-5-5, no tools, no session persistence,
  fresh frozen input; no Codex subagents. Primary review is separate from this review.
- Core review input SHA256 `9F0F473BFBAB276F3AAB3F055DE4BE6E49F2BC967F683DF06675DD545ABBC849`:
  initial FAIL for F-02 overcorrection; Transition and binary coverage lookup PASS by inspection.
  Corrected F-02 regressions PASS. Reviewer did not execute tests or inspect the harness.
- Journal review input SHA256 `4A784169733DB3F68EB18072F2904DB67A79E1263531689F8FD4EAF4B11FB4BF`:
  initial FAIL for blocked fresh authorization. Non-replay, successor recovery, downgrade,
  lease and rate-limit restoration passed source review. Targeted follow-up input SHA256
  `7F63D5C201A8CEBE145F3C66DDCECADF994BBAF1D8A63E20C531EA120BBCD31D` includes
  the correction, cache validation and transport omitted from the first bundle; result pending.

## Initial Release measurements

Harness: `dotnet run --project tools/AiUsage.ProviderConsole -c Release --no-restore -- measure-backend`.
Local Windows 10.0.26200 x64, Intel Core i7-13620H, 10 cores/16 logical processors,
SDK 10.0.401/runtime 10.0.12. Four accounts, two limits each, 35 days, five-minute
sampling: 10,080 observations per series, 80,640 retained runs, 38,871,184 stored bytes.
Synthetic temporary app-owned stores only. Seeding excluded; first operation is process-cold
but filesystem cache is not flushed. Three warmups, nine measured repetitions, process-wide
managed allocation deltas; timing ranges expose local scheduling/GC variability.

| Workload | Baseline median ms (min-max) | After core fixes median ms (min-max) | Baseline/after allocated bytes |
| --- | --- | --- | --- |
| Read eight series | 444.209 (398.805-488.372) | 435.046 (407.340-469.587) | 100,749,848 / 100,748,728 |
| Append eight observations | 652.518 (626.956-668.340) | 660.118 (619.670-692.214) | 165,097,768 / 165,093,632 |
| Budget/history eight series | 87.508 (59.166-135.729) | 92.394 (55.001-118.708) | 32,454,920 / 32,454,920 |
| Estimate four session pairs | 39.180 (34.706-57.843) | 15.137 (14.784-19.431) | 6,525,416 / 6,421,736 |
| Four parsers x 1,000 payloads | 33.087 (28.579-42.578) | 31.944 (29.580-44.741) | 7,312,072 / 7,312,072 |

Codex/Claude/Copilot/Antigravity payloads are respectively 197/133/156/146 UTF-8 bytes,
with two normalized limits each. The duplicate-append scenario initially selected two
observations from one series despite its label; that result is excluded. Corrected harness
selects the final observation from each of two series (156.390 ms, 40,945,320 allocated bytes).
This harness correction does not affect the comparable workloads above. Raw output remains
local under `.ai-usage-local/AIU-042`; final evidence is pending. No end-to-end latency claim.

## Runtime/language assessment

Microsoft's [.NET 10 overview](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
and [C# 14 reference](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
were checked on 2026-10-02. Stable span support is suitable for the bounded coverage lookup;
no extension-member framework, field-property churn, preview features, SDK/dependency change
or floating language version is warranted. `decimal.Scale` was assessed as an allocation-free
alternative to GetBits in the recorder, but has not been adopted pending its own evidence.
