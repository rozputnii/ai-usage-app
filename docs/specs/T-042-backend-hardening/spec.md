---
id: T-042
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: The owner's 2026-10-02 Goal instruction explicitly authorizes execution of this specification and AC-01 through AC-08. No separate design or independent review approval is claimed.
---

# Pre-T-037 backend architecture and quality hardening

Bring the existing backend to a defensible, maintainable baseline before extending
provider parsers. Review it with the judgment of an experienced .NET developer and
implement the corrections justified by that review. Deliver working code and evidence;
an architectural report alone does not resolve actionable in-scope defects.

## Scope and constraints

Inspect all production backend areas in `src/windows/AiUsage.Core`,
`src/windows/AiUsage.Infrastructure`, nonvisual Windows adapters, orchestration and
composition in `src/windows/AiUsage.Windows`, and `tools/AiUsage.ProviderConsole`.
Relevant tests and build/analyzer configuration are in scope when needed to defend
these changes. Record coverage by subsystem; sampling a few files is not a whole-backend
audit. Use the current implementation as evidence, including T-035 and T-036;
previous T-027/028 findings are context, not automatically reopened defects.

Preserve Core's credential-free contracts and computations, Infrastructure's transport
and persistence ownership, and Windows presentation and desktop lifetime ownership.
Keep provider and user values opaque. Preserve observable behavior except for demonstrated
bugs with tests against the intended contract. Preserve authentication, provider requests,
identity, stored formats, compatibility, retention and cleanup guarantees.

Code-only means backend implementation and useful tests or measurement code, with the
minimum documentation needed for decisions, findings and evidence. It excludes visual
changes, design work, new product features, T-037 parser expansion or v2 migration,
T-038/039 presentation work and T-040 provider-history removal. Do not rewrite working
subsystems merely for stylistic uniformity or add speculative abstraction layers.

Use the pinned stable .NET 10 SDK and supported C# 14 capabilities. Check official
Microsoft documentation for APIs or compatibility details that affect a proposed change.
Prefer features that reduce complexity, prevent errors or measurably reduce cost. Do not
require every new feature, use preview APIs, set a floating language version, or upgrade
the SDK/framework/dependencies as a substitute for improving the code. A new dependency,
significant architecture change or changed security/product contract needs an owner
decision before the dependent change; continue independent authorized work meanwhile.

## Review coverage

1. Dependency direction, public API size, responsibility boundaries, duplication,
   coupling, domain naming, cohesion and testability. Apply patterns only to concrete
   needs; avoid generic repositories, mediator layers or interfaces without a reason.
2. Correctness of limit and budget computations: decimal and money handling, unknown
   versus zero/unlimited, time zones and DST, resets, incomplete observations,
   tracked consumption and five-hour session estimates.
3. Async execution, cancellation propagation, overlapping refreshes, synchronization,
   lock scope, task ownership, disposal, dependency lifetimes and shutdown behavior.
4. Provider transport/parsing and error handling: bounded requests, retries/backoff,
   failure classification, stale data, malformed or unexpected input and secret-free
   diagnostics. This does not authorize new live requests or credential import.
5. Persistence and recovery: atomic writes, bounded growth, retention, corruption,
   path ownership, reparse checks, cancellation/crash behavior and concurrency. Use
   the security-lifecycle skill for changes or substantive review of these boundaries.
6. Performance: avoidable allocations, repeated parsing/enumeration, disk reads/writes,
   serialization, excessive locking and asymptotic costs at realistic retained-data sizes.

## Work and evidence

Start with an inventory and a baseline of applicable builds and regression suites.
Record findings concisely with location, violated contract or concrete maintenance cost,
severity, evidence and disposition. Implement small coherent fixes. Fix material
correctness/security defects and significant maintainability/performance problems within
scope; track minor deferred improvements with a reason. Do not invent findings to meet a
quota. Owner decisions and unavailable required evidence remain visible blockers.

For a bug, add a focused regression that fails before the fix when practical. Tests must
assert behavior, edge cases and failure boundaries, not mirror implementation. Preserve
existing coverage; never weaken tests or suppress analyzers merely to obtain green results.

Measure representative Release workloads for the reading store and budget/history
computations at a realistic 35-day observation volume, and provider parsing with synthetic
fixtures. Include the number of accounts/limits, sampling rate, input size, SDK, machine,
warmup/repetitions and cold/warm distinction. Compare before/after measurements under the
same conditions for performance changes, using elapsed time, allocations, I/O or contention
as applicable. Prefer existing tooling or a small reproducible harness; a benchmark library
is not mandatory. Investigate regressions beyond measurement variability. No universal
latency guarantee or end-to-end speed claim follows from a microbenchmark.

Follow the repository verification matrix for the actual diff, including Windows smoke
when desktop lifetime or activation changes require it. Code-only does not waive checks.
Use synthetic inputs and isolated temporary app-owned stores by default. Do not read CLI
credentials, sign in, change trust or mutate real user data. If an essential live check
needs an account, request owner-led Chrome sign-in and current authorization then.

The primary performs the integrated code review. Codex subagents are disabled. A primary
self-review is not independent review: if the diff triggers a required independent review,
use only an available authorized independent reviewer. If unavailable, record that gate
as BLOCKED and do not claim completion or integrate changes that require it. This does
not block safe preparation or unrelated remediation. Follow CONTRIBUTING for Git save
points and integration; do not bypass a review gate to satisfy automatic publication.

## Acceptance criteria

- AC-01: An inventory maps every in-scope backend subsystem to reviewed code and a
  disposition for each applicable coverage area. Baseline failures are distinguished
  from regressions. Existing unrelated work is preserved.
- AC-02: Evidence-backed material findings within authorized scope are fixed and covered
  by appropriate tests. Remaining findings have severity, rationale and an explicit
  disposition; unresolved material findings or required decisions prevent completion.
- AC-03: The integrated diff preserves the Core/Infrastructure/Windows boundaries and
  improves identified responsibility, coupling or maintainability problems without
  speculative layers, wholesale rewrites or unrelated formatting churn.
- AC-04: Modern .NET 10/C# 14 opportunities have been assessed. Adopted features have a
  concrete benefit and build under the pinned stable toolchain; no feature-count target,
  preview dependency or blanket analyzer suppression is introduced.
- AC-05: Reproducible Release measurements cover the reading store, budget/history
  computations and provider parsing. Performance changes include comparable before/after
  evidence and no unexplained material regression in the affected representative workload.
- AC-06: Regression evidence protects existing provider semantics, opaque values, budget
  and session rules, stored-data compatibility and lifecycle guarantees. No T-037 to
  T-040 functionality or visible redesign is included.
- AC-07: Applicable builds with warnings as errors, Infrastructure and Presentation
  regressions, document validation, diff checks and change-triggered checks pass. Required
  independent review is satisfied. Actual outcomes use PASS, FAIL, NOT_RUN or BLOCKED;
  compilation or fixtures never imply live-provider or interactive Windows success.
- AC-08: The final integrated review maps AC-01 through AC-07 to recorded evidence and
  limitations. Closure means this bounded audit and remediation are complete, not a claim
  of defect-free software. Do not begin T-037 automatically or continue speculative
  optimization after the criteria are met.

## References

- [Verification policy](../../workflow/verification.md)
- [Core engine specification](../T-035-core-limit-budget/spec.md)
- [Reading store specification](../T-036-local-reading-store/spec.md)
- [.NET 10 overview](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [C# 14 features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
