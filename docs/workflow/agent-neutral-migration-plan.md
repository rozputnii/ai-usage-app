# Agent-Neutral Development Workflow Implementation Plan

**Status:** Ready for execution when the owner requests implementation. This document does not start implementation or authorize remote actions.

**Goal:** Make ordinary repository work simple, transparent, and independent of Codex, Claude, Copilot, OMP, or any workflow plugin.

**Architecture:** Root `AGENTS.md` is the common agent entry point. `CONTRIBUTING.md` owns the development procedure; existing product documents retain requirements and evidence. Tool-specific entry files only refer to the common instructions. No replacement orchestrator, scheduler, permission database, or adapter generator is introduced.

**Technology:** Markdown, the existing .NET 10 document validator and xUnit v3 regression executable, PowerShell, and the existing GitHub Actions workflow. No new package dependency.

**Design basis:** The owner selected “shared rules and a simple process independent of the tool” after the documentation analysis in this conversation, then requested this executable plan. The target contract below contains the approved direction and concrete implementation choices; no separate design document is required. Superpowers helped prepare this plan but is not an execution prerequisite. Execute sequentially with one integrator; do not dispatch implementation workers.

## Scope and safeguards

- Change development instructions, documentation consistency, validator behavior, and CI wiring only. Do not change application behavior, provider protocols, credentials, packaging behavior, or product acceptance criteria.
- Preserve unrelated tracked and untracked work. Do not reset, stash, stage, commit, move, or delete it automatically.
- Current explicit owner instructions take precedence over historical permission records. This plan grants no push, merge, release, account access, host trust change, installation, or network access.
- Keep provider-source provenance and historical OMP verification truthful. Removing a development dependency does not invalidate the provider research based on OMP source.
- Do not translate opaque payloads, identifiers, user data, or fixtures.
- Keep PASS, FAIL, NOT_RUN, and BLOCKED distinct. Existing reports are historical evidence, not newly executed checks.
- Do not add a repository language mandate or persist this preference in a new policy/memory file. Task 2 removes existing active language prescriptions and Task 3 removes their heuristic enforcement. Product localization requirements remain unchanged.

## Target contract

### One entry point and one procedure

`AGENTS.md` must contain only essential standing instructions and a selective reading map:

1. Inspect the current request and Git state; read only the relevant product requirements and task context.
2. State the intended result and acceptance checks briefly before substantial work.
3. Execute ordinary implementation and verification within the requested scope without repeated approval for internal steps.
4. Ask only when a missing decision changes scope, product intent, significant architecture, dependencies, security boundaries, or authority for an external/destructive action. Do independent preparation first.
5. Maintain one active feature by default. The owner may explicitly request a bounded batch; backlog status alone never starts work.
6. Review the integrated changes, run relevant checks, and record limitations honestly.
7. Finish with what changed, what was verified, and what remains. On interruption, record one exact next action in the selected task document.

No named Plan Mode, Advisor, model family, native command, plugin, or session-transfer mechanism is required. An agent may use available planning tools, but a short written plan satisfies the process. Native tool permissions remain separate from repository guidance.

`CONTRIBUTING.md` owns this procedure, the Git policy, and review requirements. Other active documents link to it rather than restating variants. Keep focused independent review for material credential, destructive-data, or privilege changes and public release review; require evidence and appropriate scope, not a particular vendor or model family. Unavailable required review is reported honestly. Do not add a mandatory independent review to routine edits.

### Git policy

Use one conservative, tool-neutral default: work on a short-lived task branch, commit only coherent verified changes, and require explicit current owner authorization for remote actions or integration into main. A current owner request may explicitly choose direct-main work or authorize a push; an old task's authorization never becomes an automatic grant to a new session. Never force-push or bypass protection.

For this migration, create `codex/agent-neutral-workflow` from the inspected current checkout before implementation. This is a branch, not a promise of filesystem isolation. Existing uncommitted work remains present and must be accounted for. If that branch already exists, inspect it before choosing whether it is the correct branch; do not reset it. Do not create a commit until migration-owned changes can be separated from unrelated changes.

### Sources of truth and proportional records

| File | Owns | Must not own |
| --- | --- | --- |
| `docs/constitution.md` | Product principles and authority boundaries | Tool-specific runtime behavior or duplicate procedural rules |
| `docs/product/goals.md` | Product outcomes and goal membership | Session IDs, budgets, current task, or chronological permission transcripts |
| `docs/backlog.md` | Feature status, dependencies, outcome, and evidence reference | Internal implementation microtasks or permission grants |
| Selected `spec.md` | Intended behavior and acceptance criteria | Repeated current-progress prose |
| Selected `tasks.md`, when needed | Internal task state and a concise handoff | A second feature-level status or runtime authorization ledger |
| Selected `verification.md` | Observed checks, environment, code reference, limitations | Inferred approval or unobserved live success |
| `docs/decisions/accepted.md` | Current durable decisions | Unmarked contradictory historical instructions |

Keep existing IDs and status vocabularies during this migration. A spec lifecycle label may remain as validated metadata; do not repeat execution status in prose or an `execution_status` field. Do not introduce another state file.

- Small fix: brief plan in the conversation and check evidence in the completion/commit record; no mandatory spec directory or backlog item.
- Feature: spec and verification; tasks are optional when no useful internal decomposition exists.
- Complex architecture, authentication, or data lifecycle: add a design when it explains meaningful choices; add an ADR only for a durable decision.
- Minimum structured task fields: `status`, `depends_on`, `acceptance`, `evidence`. Existing ownership/concurrency fields may remain in historical records; they are optional for sequential work.
- Explicit parallel work retains declared ownership, safe paths, isolation for write workers, and integrated-result verification. No new scheduler implements these rules.

### Thin adapters

Create `CLAUDE.md` containing exactly this import:

```text
@AGENTS.md
```

Create `.github/copilot-instructions.md` containing:

```text
Read and follow the repository-root AGENTS.md before working on this repository.
```

Replace `.omp/AGENTS.md` with the same sentence. Do not keep executable project extensions or OMP-specific policy alongside it. Root `AGENTS.md` serves tools that load it directly. Do not create speculative adapters for unused tools. Adapter loading varies by client; mark actual client loading NOT_RUN unless observed in that client.

## Task 1: Establish a safe baseline and retire the executable OMP bridge

**Inputs:** Current checkout and tracked source inventory. **Output:** A recoverable archive outside agent discovery paths, with no active OMP bridge.

**Files:**
- Archive `.omp/AGENTS.md`, `.omp/config.yml`, `.omp/RULES.md`, `.omp/WATCHDOG.md`, `.omp/WATCHDOG.yml`, `.omp/agents/work-worker.md`, `.omp/extensions/ai-usage.ts`, `.omp/lib/work.ts`, `.omp/lib/patch.ts`, `.omp/lib/validate.ts`. Copy the original OMP entry file into the archive before replacing its active contents.
- Archive the five `.omp/skills/*/SKILL.md` files, `tools/start-work.ts`, `tests/omp-workflow/work.test.ts`, `tests/omp-workflow/tool-gate.test.ts`, `tests/omp-workflow/patch.test.ts`, and repository-local `.codex/config.toml`.
- Create `docs/archive/omp/README.md`; preserve each archived source under `docs/archive/omp/` followed by its original repository-relative path.
- Keep `.omp/AGENTS.md` active only as the adapter defined above. Keep local runtime/credential files and protective ignore entries untouched.

- [ ] Record `git status --short --branch`, `git diff --stat`, and `git rev-parse HEAD`. Inspect the full diffs of migration-target files before editing them.
- [ ] Account for known pre-existing changes: `.omp/AGENTS.md`, `.omp/RULES.md`, constitution, accepted decisions, validator and its tests; untracked `AGENTS.md`, `.agents/`, `.codex/`; unrelated application and package-smoke changes and the shortcut file. Refresh this inventory rather than assuming it is unchanged.
- [ ] Create the task branch under the Git policy above. Do not use a clean checkout that silently omits relevant uncommitted policy changes.
- [ ] Run the baseline validator regressions and document validation using Task 5 commands. Record existing failures separately; do not attribute them to this migration.
- [ ] Archive only the explicitly listed authored files after confirming resolved source and destination paths stay inside the repository. Preserve their current bytes, including existing edits. Never recursively copy a tool directory: it may contain local sessions or credentials.
- [ ] Mark the archive README as historical, non-executable evidence excluded from active instructions and current-contract validation. Include the baseline commit and source-to-archive path rule. Archived tests are no longer runnable CI gates.
- [ ] Replace the OMP entry file with the thin adapter. Confirm no active `.omp/extensions`, `.omp/config.yml`, WATCHDOG, launcher, or OMP test gate remains after Task 4.

**Check:** Inspect exact moved paths and preserved diffs. This task does not claim OMP compatibility or run OMP. Document validation may temporarily fail until Tasks 2–4 update links and contracts.

## Task 2: Establish neutral instructions and reconcile documentation

**Inputs:** Target contract and preserved history. **Output:** One current procedure, consistent product status, and shared specialized guidance.

**Files:**
- Rewrite `AGENTS.md`, `CONTRIBUTING.md`, and the execution sections of `docs/constitution.md`.
- Update `README.md`, `SECURITY.md`, `docs/workflow/formats.md`, `docs/workflow/verification.md`, `docs/workflow/omp-native.md`.
- Update `docs/product/goals.md`, `docs/backlog.md`, `docs/decisions/accepted.md`, `docs/decisions/superseded.md`, and stale setup entries in `docs/decisions/pending.md`.
- Update `docs/specs/AIU-004-codex-dashboard/spec.md`; reconcile any changed current claims against its tasks and verification, preserving historical evidence.
- Create `CLAUDE.md` and `.github/copilot-instructions.md`; update `.omp/AGENTS.md`.
- Rewrite the five `.agents/skills/{project-work,feature-delivery,provider-evidence,security-lifecycle,convergence-review}/SKILL.md` files as their sole active copies.

- [ ] Implement the target contract in AGENTS and CONTRIBUTING. Reference specialized guidance by path so native skill discovery is optional.
- [ ] Remove `canonical_sha256` and the Codex-versus-OMP hierarchy from shared skills. Consolidate project-work and feature-delivery instructions around the simple cycle; preserve provider evidence, credential lifecycle, and scoped-review substance. Do not require another runtime to perform ordinary work.
- [ ] Replace `docs/workflow/omp-native.md` with a short retirement notice linking to CONTRIBUTING and the archive. Keep this stable path so existing links do not need wholesale rewrites.
- [ ] Amend affected execution decisions explicitly, including D-012 through D-049 where relevant, D-166, D-171, D-173, and D-178/179. Preserve unrelated product decisions. Move superseded text into the existing superseded register with clear historical labeling; do not leave two active Git policies.
- [ ] Remove existing active repository-language mandates from AGENTS, constitution, CONTRIBUTING, formats, skills, and decision D-176. Retain D-176 only as a superseded historical decision, without adding a replacement mandate. Keep D-120 product localization and opaque-data safeguards intact.
- [ ] Remove session permission chronology from goals. Preserve materially useful historical approvals in the existing superseded/history record, identified as past-task context rather than current grants. Keep G-002 as the recorded product direction; do not select AIU-005 or another feature.
- [ ] Reconcile AIU-002/003/004 completion with their retained evidence. Remove AIU-004 `execution_status` and stale “still open” cache/tray prose. Do not claim packaged live sign-in, real-grant resume, or close-to-tray was verified by this migration.
- [ ] Keep AIU-001/G-001 as completed historical work. Reword future AIU-017/025 automation scope without mandatory OMP; preserve IDs, deferred status, and lack of execution authorization. Retire CR-AIU-001-02 if its only affected runtime is removed, and CR-AIU-001-03 as obsolete language enforcement, with reasons rather than claims of fixed behavior.
- [ ] Update README to the documented delivered dashboard and actual remaining limitations. Separate product build/test prerequisites from optional AI tools. Remove mandatory Bun, OMP profile, Advisor, and startup commands.
- [ ] Update active links to moved files. Retain historical command text and source citations where clearly historical; do not globally replace every occurrence of OMP. Inspect references in `docs/workflow/environment.md`, `docs/decisions/technical-audit.md`, and AIU-001 records only for broken links or missing historical context.

**Check:** A reader starting at AGENTS can find the procedure, current backlog, selected feature evidence, and test commands without visiting `.omp` or the archive. No mandatory plugin, tool mode, or language rule remains in active instructions.

## Task 3: Make document validation independent of the runtime

**Files:** `tools/AiUsage.ProjectValidation/ProjectValidator.cs`, `tests/AiUsage.ProjectValidation.Tests/ValidatorTests.cs`, `docs/workflow/formats.md`.

**Interface:** Preserve `ProjectValidator.Validate(string root) -> IReadOnlyList<Diagnostic>` and `Diagnostic(File, Task, Code, Message)`. Preserve the CLI and stable diagnostics for unchanged contracts. No new service or package.

- [ ] Add regression tests first for a shared skill with no `.omp` counterpart, a spec with no tasks document, and a sequential task containing only the four required fields. The current validator must reject these cases before implementation.

```csharp
[Fact]
public void SharedSkillDoesNotRequireAnOmpCopy()
{
    using var root = new Fixture();
    root.Put(".agents/skills/example/SKILL.md",
        "---\nname: example\ndescription: Inspect evidence\n---\n# Example\n");
    Assert.Empty(ProjectValidator.Validate(root.Path));
}

[Fact]
public void FeatureMayOmitInternalTasks()
{
    using var root = new Fixture();
    File.Delete(Path.Combine(root.Path, Fixture.Tasks));
    Assert.Empty(ProjectValidator.Validate(root.Path));
}

[Fact]
public void SequentialTaskNeedsNoRuntimeMetadata()
{
    using var root = new Fixture();
    root.Put(Fixture.Tasks,
        "---\nid: AIU-001\nschema_version: 1\n---\n# Tasks\n" +
        "### T-01 - Implement behavior\n- status: pending\n" +
        "- depends_on: []\n- acceptance: [AC-01]\n- evidence: not-run\n");
    Assert.Empty(ProjectValidator.Validate(root.Path));
}
```

- [ ] Add negative coverage: a done feature without tasks still needs an existing evidence artifact referenced by its backlog entry; a done sequential task still needs evidence and completed dependencies; an explicitly parallel worker still needs safe ownership and integration evidence. Use existing `Fixture.Put`, `Replace`, and `Append` helpers.
- [ ] Replace skill fingerprint/OMP-command tests with metadata, duplicate-name, path-name, and valid shared-skill tests. Change `CheckSkills` to scan `.agents/skills` only; remove canonical/adapter pairing and hash imports when unused.
- [ ] Remove `CheckLanguage`, its invocation, and language-only tests. Preserve safety tests by asserting unsafe paths/reparse traversal directly, rather than using non-Latin text as a read detector. No replacement language detector.
- [ ] Scan only explicit authored roots: `docs` excluding `docs/archive`, `.agents/skills`, and named root/adapter Markdown files. Remove recursive `.codex` and executable `.omp` scans. Never enumerate authentication files or user profiles. Check the archive boundary itself for a reparse point before skipping its contents.
- [ ] If `tasks.md` is absent, validate the spec and backlog normally and skip task parsing. For a done feature, require at least one existing safe evidence artifact in its backlog evidence field. Absence of internal tasks must not bypass completion evidence.
- [ ] If tasks exist, require the four neutral fields. Treat absent `agent` as `primary`, absent `parallel` as `false`, and absent `isolation` as `none`. Validate supplied enum values and all supplied paths. Keep existing dependency, AC, done-without-evidence, and lifecycle consistency checks.
- [ ] Apply worker-specific checks only to explicitly declared non-primary write workers; require complete ownership fields for explicitly parallel work. Extend `PrimaryPath` to cover neutral instruction files and `.agents` shared guidance rather than making `.omp` the policy center. Retain conservative overlap and Windows path safeguards.
- [ ] Add an archive fixture with obsolete metadata and no current dependencies; verify it does not become active work. Add a synthetic missing Markdown link to each adapter filename in tests and assert `BROKEN_LINK`, proving those files enter the authored scan. Check the actual plain-text pointers and Claude import manually in Task 5; do not build a client-import parser. Preserve existing traversal, malformed ID, cycle, missing AC, duplicate ID, and broken-link coverage.
- [ ] Run validator regressions and current-tree document validation. Correct migration regressions without weakening product acceptance or hiding unrelated failures.

**Check:** Valid ordinary work needs no installed agent, OMP skill copy, runtime fields, or mandatory task decomposition. Malformed references, unsafe paths, and unsupported completion claims still fail.

## Task 4: Remove the OMP CI dependency and retain product checks

**Files:** `.github/workflows/validation.yml`, `README.md`, `CONTRIBUTING.md`, `docs/workflow/verification.md`, `.gitignore` only if an obsolete authored-path assumption requires adjustment.

- [ ] Remove the Bun setup action and `bun test tests/omp-workflow` step. Keep validator regressions and canonical document validation.
- [ ] Add an explicit deterministic product regression step using the existing executable test project:

```yaml
- name: Product regression tests
  run: dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release -- -noLogo
```

- [ ] Inspect the product tests before enabling this step: confirm they use synthetic data/local test servers and temporary owned storage, not real account grants or external endpoints. Existing DPAPI tests must run on Windows. Do not introduce provider credentials into CI.
- [ ] Keep Windows unsigned package build, routing reproduction build, smoke-harness publish, action pins, concurrency, and permissions unchanged. Do not turn publish/build into a claimed interactive smoke pass.
- [ ] Make the documented command list match the workflow and Task 5. Preserve ignore rules protecting sessions, caches, credentials, private keys, and generated outputs even when a tool is optional.

**Check:** No active CI step installs or invokes OMP/Bun for the retired workflow. This is a configuration inspection until an authorized remote run is observed; report remote CI as NOT_RUN.

## Task 5: Verify portability, review the result, and hand off

**Create:** `docs/workflow/agent-neutral-migration-verification.md` after execution, containing actual command results, code reference, baseline comparison, and limitations. Do not create a filled PASS report from this plan.

- [ ] Resolve the already installed .NET SDK. Use `dotnet` when it matches `global.json`; otherwise use the existing user-local SDK path documented in README. Do not install an SDK or restore packages from the network without authorization.
- [ ] Execute the following with existing restored assets. A missing asset is BLOCKED for offline execution; do not silently remove `--no-restore`.

```powershell
dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo
dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo
git diff --check
```

- [ ] Inspect active references using `rg -n 'OMP|omp|/work|canonical_sha256|Advisor|approval|English|language' AGENTS.md CLAUDE.md CONTRIBUTING.md README.md SECURITY.md .agents .github docs`. Classify results as current requirement, historical evidence, product/provider meaning, or this migration plan. Do not require zero matches across historical research.
- [ ] Check that only `.omp/AGENTS.md` remains as active authored OMP configuration; inspect named files rather than reading local credential/runtime contents. Confirm no retired code is referenced by CI or active launch instructions.
- [ ] Perform a fresh-context document walkthrough starting with AGENTS alone: identify the active product direction, completed AIU-002/003/004 evidence, no newly selected feature, required checks, and next-action location. This is a document inspection, not a live test of another AI client.
- [ ] Confirm the three adapters resolve to the same root file. Record Claude/Copilot/OMP client loading as NOT_RUN unless separately observed under owner-authorized client use. Do not install or authenticate clients to complete this migration.
- [ ] Review changed instructions for one Git policy, one execution procedure, no language prescription, no mandatory plugin, and no automatic backlog execution.
- [ ] Confirm no migration-authored changes to `src/windows` or product smoke behavior. Record pre-existing application diffs separately. Product test failures associated with that baseline must not be silently fixed as workflow work.
- [ ] Record all results in the verification file. A build is not required to re-prove unchanged UI behavior; if code scope expands, stop and revise the verification scope before changing application code.
- [ ] Re-run document validation after adding the verification report. Inspect both tracked diff and newly created files; `git diff` alone omits untracked adapters and documents.
- [ ] Commit only if allowed by the current request and the migration-owned changes can be isolated. Stage explicit reviewed paths/hunks; never use blanket staging in this dirty checkout. If separation is ambiguous, deliver the verified working diff and state the exact overlap instead of bundling unrelated work. Do not push or merge.
- [ ] Finish with a short report: common entry point, simplified procedure, retired runtime dependencies, observed checks, and outstanding limitations.

## Acceptance checklist

- [ ] Root AGENTS is neutral; tool adapters contain no independent policy.
- [ ] Ordinary repository work and document validation need no OMP installation, profile, plugin, or special runtime command.
- [ ] Shared skills have one active copy and can be read without native discovery.
- [ ] One current Git policy and one current procedure replace contradictory variants.
- [ ] Goals contain no runtime authorization ledger; feature status and handoff have clear owners.
- [ ] README, goals, backlog, and AIU-004 spec no longer contradict retained completion evidence.
- [ ] Optional task decomposition does not weaken evidence, dependency, path, or AC validation.
- [ ] Existing language prescriptions and heuristic enforcement are retired; product localization remains intact.
- [ ] OMP source/evidence is retained as history outside automatic execution paths; provider provenance is preserved.
- [ ] CI configuration retains product checks and has no active OMP workflow dependency.
- [ ] Verification reports actual local results and explicitly labels unobserved client loading and remote CI.
- [ ] Unrelated work, credentials, host trust, and product behavior are preserved.

## Recovery

Before any commit, undo only migration-owned hunks or restore explicitly archived files from their mapped copies. Never reset the checkout to the baseline commit: it contains pre-existing uncommitted work that must survive. After a clean migration-only commit, a reviewed revert on a task branch is the recovery path; no history rewrite or remote operation is implied.
