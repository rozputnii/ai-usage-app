# Document contracts

Project document format v1. Follow [CONTRIBUTING](../../CONTRIBUTING.md) for the development procedure.

## Sources of truth
- goals.md: product outcomes and goal membership; no session permissions or budgets.
- backlog.md: canonical AIU status, references and dependencies.
- spec.md: accepted intended behavior, acceptance criteria and document lifecycle.
- design.md: optional implementation alternatives and boundaries.
- tasks.md, when needed: internal task state and concise handoff, not feature-level status or an authorization ledger.
- verification.md: actual evidence, not another status mirror.

Do not add CURRENT.md or a duplicate tasks.yml. Document lifecycle and backlog execution statuses differ; validate permitted combinations rather than requiring identical labels.

## Specification metadata
YAML frontmatter includes id, type, status, goal and scope_version. Document statuses: draft, approved, implementing, implemented, superseded. Record approval provenance as an owner decision, an owner amendment or derivation within an authorized goal. Never fabricate human approval.

Acceptance criteria use AC-01 and subsequent identifiers with testable conditions. Golden fixtures do not replace targeted semantic assertions.

## Task blocks
Task decomposition is optional. Each `### T-xx - title` requires only `status`, `depends_on`, `acceptance` and `evidence`. Other fields below are optional for sequential primary work, but complete ownership metadata is required for explicitly parallel work:
- status: pending | ready | in-progress | blocked | done | dropped
- depends_on: local T identifiers
- ownership: a concise semantic domain
- writes: conservative relative roots/globs
- shared: shared contract paths/roots, or an empty list
- parallel: true | false (default false)
- isolation: required | none (default none)
- agent: declared worker name, or primary (default)
- acceptance: AC references
- evidence: actual check/artifact references, or not-run

Handoff records completed facts, the exact next action, blockers, tested commands/results, base reference and pending worker artifacts. Never store secrets or raw transcripts. Evaluate dependencies before marking a task executable. A write-worker task is done only after evidence and integration, not simply a successful yield message.

## Ownership checks
Use canonical relative paths. Reject traversal, absolute/home paths and .git access. Normalize separators/case for Windows and consider paths of files not yet created. If glob intersection cannot be proved safe, serialize. The primary owns shared contracts. An out-of-scope diff cannot be silently integrated.

Serialize shared resources such as migration ledgers, schema changes, central registries, interactive UI test desktops, signing and release feeds. Nonoverlapping files do not by themselves establish independent behavior.

## Pending decisions
Include ID, affected AIU/goal, precise question/options/recommendation/impact, evidence and when needed. Move durable resolved decisions to the appropriate spec or ADR. Do not turn the inbox into another full decision register.

## Research
Use provider, source_verified_at, live_verified_at, classification, confidence and source references. Verification dates may be null. A generic verified_at must not imply live success. Classify authentication and quota methods separately when they differ.

## Validator tests
Reject duplicate IDs, dependency cycles, missing references, invalid statuses, broken AC references, escaping paths, unsafe concurrent ownership and done-without-evidence. Accept explicit deferred unknowns without falsely marking the work ready. The validator must not make network/model calls or rewrite documents. Report file/task/error code.

## Authored scan and evidence
The validator reads docs except docs/archive, .agents/skills, and named root/adapter Markdown files. It does not scan local authentication or runtime directories. Archive reparse boundaries are rejected before contents are skipped. Shared skills require name/description metadata, unique names and matching paths.

A done feature requires an existing safe artifact in its backlog evidence field even without tasks. Done tasks require evidence and completed dependencies; explicit non-primary write workers also require integration evidence. Spec lifecycle metadata remains distinct from backlog execution status; do not add an execution_status field or repeat current progress in specs.
