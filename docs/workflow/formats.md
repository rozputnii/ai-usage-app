# Document contracts

This is project document format v1, not native OMP schema. OMP implements and tests its lightweight validator in AIU-001. All authored repository content uses English.

## Sources of truth
- goals.md: outcomes and authorization.
- backlog.md: canonical AIU status, references and dependencies.
- spec.md: accepted intended behavior, acceptance criteria and document lifecycle.
- design.md: optional implementation alternatives and boundaries.
- tasks.md: execution state, ownership and handoff.
- verification.md: actual evidence, not another status mirror.

Do not add CURRENT.md or a duplicate tasks.yml. Document lifecycle and backlog execution statuses differ; validate permitted combinations rather than requiring identical labels.

## Specification metadata
YAML frontmatter includes id, type, status, goal and scope_version. Document statuses: draft, approved, implementing, implemented, superseded. Record approval provenance as an owner decision, an owner amendment or derivation within an authorized goal. Never fabricate human approval.

Acceptance criteria use AC-01 and subsequent identifiers with testable conditions. Golden fixtures do not replace targeted semantic assertions.

## Task blocks
Each `### T-xx - title` has structured Markdown fields:
- status: pending | ready | in-progress | blocked | done | dropped
- depends_on: local T identifiers
- ownership: a concise semantic domain
- writes: conservative relative roots/globs
- shared: shared contract paths/roots, or an empty list
- parallel: true | false
- isolation: required | none
- agent: native/custom name resolved by discovery, or primary
- acceptance: AC references
- evidence: actual check/artifact references, or not-run

Handoff records completed facts, the exact next action, blockers, tested commands/results, base reference and pending worker artifacts. Never store secrets or raw transcripts. Evaluate dependencies before marking a task executable. A write-worker task is done only after evidence and integration, not simply a successful yield message.

## Ownership checks
Use canonical relative paths. Reject traversal, absolute/home paths and .git access. Normalize separators/case for Windows and consider paths of files not yet created. If glob intersection cannot be proved safe, serialize. The primary owns shared contracts. An out-of-scope diff cannot be silently integrated.

Serialize shared resources such as migration ledgers, schema changes, central registries, interactive UI test desktops, signing and release feeds. Nonoverlapping files do not by themselves establish independent behavior.

## Ranking
First check goal scope, hard dependencies, state and permissions. Exclude blocked, dropped and completed work. A research-needed item is eligible only for research/spike scope.

For eligible candidates, estimate dimensions on a 0..5 scale with evidence and rationale. Compute `100 * sum(weight * score / 5)` with weights totaling 1. This is deterministic arithmetic over agent estimates, not objective truth. Explain judgment-based reordering. Show the top five and allow another AIU, cancellation or goal feedback.

## Pending decisions
Include ID, affected AIU/goal, precise question/options/recommendation/impact, evidence and when needed. Move durable resolved decisions to the appropriate spec or ADR. Do not turn the inbox into another full decision register.

## Research
Use provider, source_verified_at, live_verified_at, classification, confidence and source references. Verification dates may be null. A generic verified_at must not imply live success. Classify authentication and quota methods separately when they differ.

## Validator tests
Reject duplicate IDs, dependency cycles, missing references, invalid statuses, broken AC references, escaping paths, unsafe concurrent ownership and done-without-evidence. Accept explicit deferred unknowns without falsely marking the work ready. The validator must not make network/model calls or rewrite documents. Report file/task/error code.

## Language check
Reject newly authored non-English repository prose while allowing opaque provider payloads, user data and explicitly approved localization resources. The current handoff uses English throughout. Language validation is not permission to translate protocol identifiers or user-supplied values.

Current automated coverage is partial: docs, selected root documents and configured .omp policy/agent/skill/library/extension files. It does not include every tools/tests/.github source file, and script detection cannot prove that Latin-script prose is English. Repository-wide policy still applies; review supplements the heuristic. Coverage follow-up CR-AIU-001-03 is recorded in the canonical backlog.
