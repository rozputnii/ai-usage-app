# Document contracts

Project document format v1. Follow [CONTRIBUTING](../../CONTRIBUTING.md) for the development procedure.

## Sources of truth
- goals.md: product outcomes and goal membership; no session permissions or budgets.
- backlog.md: canonical AIU status, references and dependencies: live items in full, a one-row "Done index" for done and dropped items, and one "Pending owner checks" table.
- spec.md: accepted intended behavior, acceptance criteria and document lifecycle.
- design.md: optional implementation alternatives and boundaries.
- tasks.md, only for parallel work: internal task state and concise handoff, not feature-level status or an authorization ledger. Once executed, it collapses into the execution ledger.
- verification.md: actual evidence, not another status mirror, including the "Acceptance results" table and, for executed plans, the execution ledger.

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

## Backlog
A live item keeps its block (`## AIU-nnn - title` with goal, status, depends_on, trigger, outcome and the fields it needs). When an item is done or dropped, replace its block with one row in the "Done index" table: `| AIU-nnn | Title | done or dropped | G-nnn | evidence path |`; its history stays in its specification, verification record and Git history. A post-deploy owner check (D-190) is one row in "Pending owner checks" until the post-deploy-check skill closes it.

## Decisions
Add a decision record only for a rule that binds future work; an owner's pick among UI variants is one line in the feature's spec. When a decision changes an earlier one, add a line to the earlier entry: `Amended by D-nnn (date): what changed.` or `Superseded by D-nnn (date): what replaces it.` The validator checks that decision IDs are unique and that these pointers name an existing decision.

## Executed plans
When every task of a plan is done and merged, replace tasks.md with an "Execution ledger" section in verification.md: one line per task with the task ID and title, commits, review verdict, check IDs and the grant it ran under. Keep an open "Next action" line while work continues.

## Skeletons

A T1 specification:

```markdown
---
id: AIU-NEW
type: feature
status: approved
goal: G-003
scope_version: 1
approval_basis: owner decision, <date>
---
# <Title>

## Problem
<What is wrong or missing, for whom. A bug may state current, expected and unchanged behavior.>

## Requirements
- R-01: <behavior>

## Acceptance criteria
- AC-01: <testable condition>

## Out of scope
- <what this does not change>

Variant: <pick>, owner, <date> (visible UI changes only).
```

A verification record:

```markdown
# <Title> - verification

Code reference: <commit>. Environment: <OS build, SDK, Release/Debug, desktop locked or not>. Date: <UTC timestamp>.

## Checks
| Check | Result |
| --- | --- |
| C4 Infrastructure suite | PASS, <n>/<n> |

## Acceptance results
| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | <check ID, test name or screenshot> |

## Not run
| Item | Reason |
| --- | --- |
| <owner check in the installed app> | Post-deploy owner check (D-190) |

## Review
<Reviewer, scope, verdict, findings and their resolution.>
```

## Ownership checks
Use canonical relative paths. Reject traversal, absolute/home paths and .git access. Normalize separators/case for Windows and consider paths of files not yet created. If glob intersection cannot be proved safe, serialize. The primary owns shared contracts. An out-of-scope diff cannot be silently integrated.

Serialize shared resources such as migration ledgers, schema changes, central registries, interactive UI test desktops, signing and release feeds. Nonoverlapping files do not by themselves establish independent behavior.

## Pending decisions
Include ID, affected AIU/goal, precise question/options/recommendation/impact, evidence and when needed. Move durable resolved decisions to the appropriate spec or ADR. Do not turn the inbox into another full decision register.

## Research
Use provider, source_verified_at, live_verified_at, classification, confidence and source references. Verification dates may be null. A generic verified_at must not imply live success. Classify authentication and quota methods separately when they differ.

## Validator tests
Reject duplicate IDs, dependency cycles, missing references, invalid statuses, broken AC references, escaping paths, unsafe concurrent ownership and done-without-evidence. Treat Done index rows as backlog items. Reject a verification "Acceptance results" table that omits or invents a specification criterion or uses another verdict than PASS, FAIL, NOT_RUN or BLOCKED; reject duplicate decision IDs and "Amended by" or "Superseded by" pointers to a missing decision. Accept `AIU-NEW` and `D-NEW` placeholders for branch work, and with `--final` reject any placeholder outside code; numbers are assigned at the merge into `main` under the [numbering rule](../../CONTRIBUTING.md#numbering). Accept explicit deferred unknowns without falsely marking the work ready. The validator must not make network/model calls or rewrite documents. Report file/task/error code.

## Authored scan and evidence
The validator reads docs except docs/archive, .agents/skills, and named root/adapter Markdown files. It does not scan local authentication or runtime directories. Archive reparse boundaries are rejected before contents are skipped. Shared skills require name/description metadata, unique names and matching paths.

A done feature requires an existing safe artifact in its backlog evidence field even without tasks. Done tasks require evidence and completed dependencies; explicit non-primary write workers also require integration evidence. Spec lifecycle metadata remains distinct from backlog execution status; do not add an execution_status field or repeat current progress in specs.
