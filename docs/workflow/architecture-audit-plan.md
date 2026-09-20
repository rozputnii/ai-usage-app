# Architecture and Clean-Code Audit Plan

**Status:** Ready for execution when the owner requests it. This document is the task plan
for an analysis-only session. It does not start remediation, authorize code changes, or grant
remote, installation, live-provider or credential authority.

**Goal:** Produce an evidence-backed audit of this repository against modern .NET and Windows
desktop (WinUI 3 / MSIX) practice, and land the remediation work as project documents, without
changing any production code. Fixing the findings is a separate, later session driven by the
documents this plan produces.

**Why a separate plan file:** the session prompt is intentionally short. This document carries
the detail the prompt refers to; the prompt owns the goal and the stop conditions, this file
owns the checklist. If the two disagree, the current owner request wins, then CONTRIBUTING.md,
then this file.

**Relation to AIU-027:** [AIU-027](../specs/AIU-027-architecture-refinement/spec.md) already
refined Core, Infrastructure and Windows into feature-oriented boundaries and was closed with
evidence on 2026-09-14. This audit measures the code as it stands today, including everything
added by AIU-007, AIU-008, AIU-009 and AIU-010, and reports drift and newly accumulated debt.
Do not re-propose settled structure; cite the AIU-027 design when a finding argues against it.

## Definition of done

The audit session is complete when all of the following hold:

1. Every check selected from the [verification matrix](verification.md#checks-by-change) for a
   documentation change has been run, plus the deterministic regression suites, with actual
   PASS / FAIL / NOT_RUN / BLOCKED verdicts. A verdict is observed output, never inference.
2. Every finding carries a stable ID, severity, category, concrete `path:line` evidence, the
   principle or guideline it violates with an authoritative source, the user-visible or
   maintenance impact, a proposed fix, effort, risk and blast radius.
3. A backlog item and its spec folder exist and are valid under
   [document formats](formats.md): `spec.md` with AC identifiers, `tasks.md` with `T-xx`
   blocks, `design.md` when boundaries change, and `verification.md` holding the Phase 1
   results plus honest NOT_RUN placeholders for the remediation checks.
4. `git status` shows changes under `docs/` only. No edits under `src/`, `tests/`, `tools/`.
5. The project validator and document validation pass after the documents are written.
6. Work is committed and pushed to `main` under the CONTRIBUTING.md Git policy, with a message
   that states this is an audit and a plan, not a fix.

## Scope and safeguards

- Do not fix findings, refactor, rename, reformat or tidy anything under `src/`, `tests/` or
  `tools/`. Record the fix as a task instead.
- Do not add, upgrade or remove dependencies, analyzers or SDK versions in this session.
  Propose them as findings with the cost and the alternative considered.
- Do not create a branch; the standing owner instruction is to work on `main`.
- No live provider calls, no credential or source-CLI credential reads, no host trust changes,
  no packaging, signing or release actions.
- Preserve authentication behavior, stored-data formats, quota semantics and close-to-tray
  behavior as constraints on every proposed fix; a proposal that breaks one must say so.
- Keep opaque provider and user data opaque in every quoted example.
- Keep one primary agent. Read-only exploration subagents are allowed for breadth; all writing
  stays with the primary.
- Authored prompts, specifications and documents are written in English.

## Required reading

- `AGENTS.md`, `CONTRIBUTING.md`, [constitution](../constitution.md), [goals](../product/goals.md)
- [verification policy](verification.md), [document formats](formats.md), `README.md#local-checks`
- [backlog](../backlog.md) for status and the next free `AIU-` identifier
- [AIU-027 spec](../specs/AIU-027-architecture-refinement/spec.md),
  [design](../specs/AIU-027-architecture-refinement/design.md) and
  [verification](../specs/AIU-027-architecture-refinement/verification.md)
- [accepted decisions](../decisions/accepted.md) and the
  [technical audit](../decisions/technical-audit.md). A finding that contradicts an accepted
  decision must cite it and argue explicitly rather than ignore it.
- The security-lifecycle skill before reporting anything about credentials, storage, migration
  or recovery.

Layout under `src/windows/`: `AiUsage.Core` owns credential-free contracts and workflows,
`AiUsage.Infrastructure` owns provider transport and persistence, `AiUsage.Windows` owns
presentation and desktop lifetime. Audit all three, plus `tests/` and `tools/`.

## Phase 1 - Verification

Run before analysis, from the repository root, and capture real output. Use the SDK selected by
`global.json`; if `dotnet` is not on PATH use the existing user-local PowerShell form from
[local checks](../../README.md#local-checks).

```powershell
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo
dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo
git diff --check
```

Build the desktop project with warnings visible and treat every warning as audit input:

```powershell
dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore
```

Read-only formatting inspection is optional:
`dotnet format <project.csproj> --no-restore --verify-no-changes`.

A missing restored asset is BLOCKED offline; never silently enable network restoration.
Interactive Windows smoke, packaged activation and live-provider checks are NOT_RUN for an
analysis session by design, and the report says so explicitly.

## Phase 2 - Analysis

Evaluate the system, not one file at a time. Cite `path:line` for every claim; a claim not read
in the current code does not enter the report. Deduplicate to one finding per root cause with
the affected sites listed, rather than one finding per occurrence. Zero findings in a category
is a valid result and is reported as such.

### A. Architecture and boundaries

- Dependency direction across Core, Infrastructure and Windows; leakage of transport, storage,
  WinUI or Windows-only types into Core; the credential-free guarantee of Core.
- Cohesion of the feature folders under `src/windows/AiUsage.Windows/Features/` against
  cross-feature coupling and shared mutable state.
- Provider abstraction: whether a fifth provider is an additive change or a shotgun edit.
  Compare Codex, Claude, Copilot and Antigravity for duplicated transport, refresh, error
  translation and quota-mapping logic that belongs in one place.
- Composition root: registration hygiene, service lifetimes, captive dependencies, hidden
  service location, and the seams that make workflows testable without the desktop.
- Error model: exceptions against result types, where provider failures are translated into
  product state, and anywhere a failure is swallowed or flattened into a misleading value.
- Public surface of each library: what is `public` that should be `internal`, and whether
  `InternalsVisibleTo` is used deliberately.

### B. Modern .NET and C# practice, targeting .NET 10

- Nullable reference types, analysis level, warnings-as-errors for owned code, `.editorconfig`
  coverage, and whether the AGENTS.md rule that owned-code warnings are errors actually holds
  in the project files.
- Asynchrony: sync-over-async, `async void`, `ConfigureAwait` policy, `ValueTask` misuse,
  `CancellationToken` reaching every I/O boundary, timeouts, and retry or backoff placement.
- Lifetime and disposal: `IDisposable` and `IAsyncDisposable` ownership, `HttpClient` and
  handler reuse, `SemaphoreSlim` and lock scope, `IAsyncEnumerable` where streaming applies.
- Immutability and modelling: records, `sealed`, readonly structs, required members, primary
  constructors and collection expressions used where they clarify, not as cargo cult.
- `System.Text.Json`: source-generated contexts, no reflection on hot paths, and correct
  handling of unknown, absent, null, unlimited, exhausted and legacy quota values.
- Logging and diagnostics: structured logging, level discipline, no secrets or opaque provider
  payloads in logs, and whether `ActivitySource` or metrics would pay for themselves.
- Configuration: the options pattern against scattered constants, magic strings and duplicated
  environment-variable reads.
- Performance: allocation and repeated I/O on refresh paths, obvious quadratic work, and
  anything that delays first paint.

### C. WinUI 3 and Windows desktop practice

- MVVM discipline: work done in code-behind, view models referencing concrete providers or UI
  types, and whether a view model can be tested without a dispatcher.
- `DispatcherQueue` usage: marshalling correctness, awaited against fire-and-forget, thread
  affinity, and races between refresh, connect and disconnect.
- Binding: `x:Bind` against `Binding`, compiled-binding coverage, binding modes, converter
  cost, and `x:Load` or deferred loading for heavy panes.
- Leaks: event handler and `Loaded`/`Unloaded` symmetry, weak-event needs, timer ownership and
  view model disposal.
- Navigation, window, tray and lifetime ownership: single-instance activation, close-to-tray,
  shutdown ordering, and cancellation of in-flight work at exit.
- Resources and localization under `Strings/`, theming under `Themes/`, light, dark and
  high-contrast rendering, and accessibility: automation names, keyboard reachability, focus
  visuals and announcement of state changes.
- Packaged against unpackaged code paths and state roots, including anything that only works in
  one of them.

### D. Tests

- Whether tests defend observable behavior and meaningful failure boundaries rather than
  implementation detail.
- Coverage gaps at the seams the audit flags, golden-fixture drift risk, and missing negative
  cases: unknown, missing, legacy, new grouping, null, unlimited, exhausted, reset and credit.
- Whether an independent assertion exists wherever a parser and its expected JSON could drift
  together.

### E. Security and data lifecycle

Use the security-lifecycle skill. Cover credential handling, DPAPI scope, state-root isolation,
opacity of provider and user data, redaction, and migration or recovery safety. Report only;
change nothing, and read no credential store.

### Sourcing

Ground best-practice claims in authoritative guidance. The Microsoft Learn tools
(`microsoft_docs_search`, `microsoft_code_sample_search`, `microsoft_docs_fetch`) are available;
cite the specific page for any non-obvious rule instead of asserting it from memory. A
preference with no authority behind it is recorded as a preference.

## Finding record format

Each finding uses a stable identifier `F-xx` and these fields:

| Field | Content |
| --- | --- |
| id | `F-01`, `F-02` and onward, stable for the life of the remediation |
| severity | S1, S2, S3 or S4 as defined below |
| category | architecture, dotnet, winui, tests, security or build |
| evidence | one or more `path:line` references read in the current code |
| principle | the rule violated, with an authoritative source or an explicit preference label |
| impact | user-visible or maintenance consequence, concretely stated |
| fix | the proposed change, and what it must preserve |
| cost | rough effort, regression risk, and the files or features touched |

Severity, applied strictly:

- **S1** correctness, security or data-loss risk in shipped behavior.
- **S2** boundary violation or a defect that will recur as features are added.
- **S3** maintainability, testability or consistency.
- **S4** polish.

## Phase 3 - Deliverables

1. `docs/backlog.md`: a new AIU entry using the next free identifier, with goal, status,
   depends_on, trigger, outcome, scope, preserve, excludes, acceptance and evidence, matching
   the style of the existing entries and referencing AIU-027 as prior work.
2. The new spec folder's `spec.md`: frontmatter with id, type, status, goal and scope_version;
   the intended end state; and `AC-01` onward, each testable by a named command or a named
   inspection.
3. `design.md`: only when the remediation moves a boundary or introduces an abstraction,
   including the alternatives considered and why they were rejected.
4. `tasks.md`: the findings turned into ordered `### T-xx` blocks with status, depends_on,
   acceptance and evidence. Each task is independently shippable, small enough to verify with a
   named check, and sequenced so that S1 and S2 work precedes polish. Findings deliberately not
   being fixed go in an explicit "Accepted as-is" section with the reason.
5. `verification.md`: the Phase 1 results with commands, observed results, environment,
   timestamps and code references, plus NOT_RUN placeholders for the remediation checks. It is
   an evidence record, not a status mirror.
6. Re-run the project validator and document validation after writing; both must pass.
7. Commit and push to `main` per CONTRIBUTING.md.

## Ask before proceeding only if

A finding would require a new dependency or framework, change product intent, change a security
boundary or a stored-data format, deviate from an accepted decision, or need external or
destructive authority. Everything else: decide, record the assumption in the spec, continue.

## Final report

- Verification table: command, verdict, note.
- Finding counts by severity and by area, with the five most important summarized in one line
  each and their `path:line`.
- The recommended fix order for the first three tasks, with the rationale.
- Links to the created documents.
- An explicit list of what is NOT_RUN or BLOCKED, and why.
