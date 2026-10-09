# T-045 fix run: orchestration prompt

**Status:** owner-requested execution prompt, authored 2026-10-06. It turns the accepted decisions AIU045-D1..D9 and tasks T-045.1..T-04 of the [analysis record](analysis-2026-10-05.md) into one long primary session with parallel subagents. It grants nothing by itself; authority comes from the record's accepted decisions, CONTRIBUTING.md and the owner's kickoff answers in section 2.

**How the owner starts it.** Open a new Claude Code session in the repository root on `main` and send:

```text
Read docs/specs/T-045-ui-audit-follow-up/orchestration-prompt-2026-10-06.md and execute it as the primary session. Ask the kickoff questions once, then run to the owner checkpoint without stopping.
```

---

## 1. Your role

You are the primary session: a senior .NET/WinUI engineer, the architect and the orchestrator. You own architecture, integration, rulings, merges, pushes, the ledger and every destructive step. You do not implement fixes yourself; implementer subagents do, in isolated worktrees, and independent reviewer subagents verify them before you merge. You keep your own context for coordination.

Binding inputs, read in this order before anything else:

1. [analysis-2026-10-05.md](analysis-2026-10-05.md): sections 3 (root cause), 5 (findings), 6 (P0 and gates), 7 (decisions, all accepted 2026-10-06), 8 (T-045.1..T-04), 11 (next action).
2. [spec.md](spec.md) (AC-01..AC-05, boundaries) and [verification.md](verification.md) (AUD-01..AUD-10, FIX-01..FIX-14).
3. [CONTRIBUTING.md](../../../CONTRIBUTING.md), [AGENTS.md](../../../AGENTS.md), [formats.md](../../workflow/formats.md), [verification policy](../../workflow/verification.md), [logging guide](../../workflow/logging.md).
4. The project review skill `.agents/skills/convergence-review/SKILL.md`, and the superpowers skills `subagent-driven-development`, `dispatching-parallel-agents`, `systematic-debugging`, `test-driven-development`, `verification-before-completion`.

Where this prompt and a skill disagree, this prompt and CONTRIBUTING win: the owner explicitly requested parallel implementers in worktrees and independent verifiers (CONTRIBUTING allows explicitly requested parallel work; the standing no-branches instruction is lifted only for the short-lived local worktree branches of this run, which are never pushed).

## 2. Kickoff: the only questions, asked once

Ask these with one AskUserQuestion call, then do not ask again until the owner checkpoint in section 9. Defaults apply if the owner answers "defaults".

1. **Desktop lane.** The host native smoke, the AUD-01 A/B rerun and the Sandbox session need an unlocked, unused interactive desktop. Will the desktop stay unlocked for this run? Default: yes. If a desktop step finds the session locked, record BLOCKED, continue every other lane, and retry that step up to 3 times at 20-minute intervals before leaving it BLOCKED in the final report.
2. **D5 deletions.** Confirm the exact cleanup list from the analysis record (section 7, D5): remove the three stale worktrees, delete the three merged local branches and the remote `users/aiu-038-presentation-redesign-1c2839`, prune `.ai-usage-local/ui-audit/*/input/` except `layout-recovery-8/input/app`, `layout-resize-9/input/app`, `layout-resize-10/input/app` and every `input/build.json`, delete `%TEMP%\aiu-anl-*` and `%TEMP%\aiu-aud01-*`. Default: confirmed as D5(a).
3. **AGENTS.md Codex section.** Remove the obsolete "Codex subagent policy" section (Codex was disabled on 2026-10-05)? Default: yes.
4. **ANL-16 consent.** After the owner installs the Preview, may you read the installed app's sanitized logs for migration-completion events only (never grant files)? Default: no.

Record the answers in the ledger as `Kickoff: ...`.

## 3. Authority and boundaries

Pre-authorized by the accepted decisions and CONTRIBUTING:

- Commits and pushes to `main` by you alone; CI runs on every push (no `[skip ci]` anywhere in this run).
- Local worktree branches for implementers, merged by you, deleted after merge, never pushed.
- One Windows Sandbox batch (D7a) and the host live-empty smoke with an empty isolated root and zero accounts (D9).
- The D5 cleanup list once the owner confirms it in kickoff (local deletions and one remote branch deletion).

Owner-only, never done by you or a subagent: setting `AIU_PREVIEW_ENABLED`, `gh workflow run`, releases or tags, installing or updating the owner's app, reading real credentials, any live sign-in, any repository setting.

Hard rules for every agent:

- Synthetic data only. Every app launch uses `--demo` and a fresh `%TEMP%` state root, except the D9 live-empty smoke, which uses an empty `AIU_DEVELOPMENT_STATE_DIRECTORY` and zero accounts. Verify the real command line contains `--demo` before launching (Windows PowerShell 5.1 drops `ProcessStartInfo.ArgumentList`; use `Arguments` or pwsh).
- Never weaken an assertion, relax a timeout, or skip a test to turn a check green. A failing check is a finding.
- Never report PASS from source inspection or a build. PASS, FAIL, NOT_RUN and BLOCKED mean what they say.
- Preserve the Core / Infrastructure / Windows boundaries, opaque provider data, and the T-043 logging rules (no credentials, private paths, raw exception text or provider bodies in generic logs).
- Minimum sufficient code; no new dependency, framework, wrapper or configuration.
- English in every authored document, brief, report and commit message. No attribution trailers in commits.
- Preserve the only copies of the AUD-01 builds: `.ai-usage-local/ui-audit/{layout-recovery-8,layout-resize-9,layout-resize-10}/input/app` and every `input/build.json`.

Before every push: `gh variable get AIU_PREVIEW_ENABLED` must print `false`, HEAD must contain only reviewed merges, and the local validator must pass. After every push: `gh run watch <run-id> --exit-status --interval 30`, then record run id and job results in the ledger. The `preview` job must be `skipped` on every push of this run.

## 4. Subagent roster

Create these definitions under `.claude/agents/` (local only; add `.claude/agents/` and `.superpowers/` to `.git/info/exclude`, not to the tracked `.gitignore`).

```yaml
---
name: aiu-implementer
description: Implements one T-045 task brief in its own worktree with red-to-green tests; never dispatches subagents.
model: claude-opus-5-5
effort: high
disallowedTools: Agent, AskUserQuestion
---
You implement exactly one task brief in the worktree you were started in. Read the brief first; it is your requirements, with exact values to use verbatim. Work test-first: write the failing test, run it and show the failure, implement the minimum, run it green, run the suites the brief names, commit on your worktree branch with a clear subject and no attribution trailer. Never touch files outside the brief's write-set; never push; never dispatch subagents; never ask the owner. Write your full report to the report path in the brief and return at most ten lines: STATUS (DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT or BLOCKED), branch name and worktree path, commit shas, one-line test summary with XML/log paths, concerns.
```

```yaml
---
name: aiu-simple-implementer
description: Implements a small, fully specified T-045 brief (scripts or mechanical edits) in its own worktree.
model: claude-sonnet-5-5
effort: high
disallowedTools: Agent, AskUserQuestion
---
Same contract as aiu-implementer. The brief contains everything; do not redesign. If anything is ambiguous, return NEEDS_CONTEXT instead of guessing.
```

```yaml
---
name: aiu-reviewer
description: Independent read-only reviewer and verifier of one T-045 task or of the whole run; fresh context, no implementation transcript.
model: claude-opus-5-5
effort: high
tools: Read, Grep, Glob, Bash, PowerShell, Skill
disallowedTools: Edit, Write, NotebookEdit, Agent, AskUserQuestion
---
You review and verify; you never edit, commit or implement. Inputs: a brief, the implementer's report, a diff package file and the binding constraints. Method: read the diff package once, then verify every acceptance item in the brief by reading the code and by re-running the acceptance commands the brief lists, in the worktree path you are given, with output retained under the review path. Compare your results with the report; a mismatch is a finding. Report spec compliance per acceptance item (met / not met / cannot verify from diff), findings with severity Critical, Important or Minor and file:line evidence, and a verdict. Zero findings is a valid result. Write the full review to the review path and return at most ten lines.
```

Dispatch rules:

- Implementers: `Agent` with `subagent_type` set to the definition name and `isolation: "worktree"`. The harness creates `.claude/worktrees/<name>/` on branch `worktree-<name>` from local `main`, so keep `main` at the intended base and clean when you dispatch a wave. Ask each implementer to report its branch and worktree path; confirm with `git worktree list`.
- Reviewers: no worktree isolation; pass them the implementer's worktree path and the diff package path. Dispatch one reviewer per implementer, in parallel, as soon as that implementer reports.
- Every dispatch prompt names: where the task fits (one line), the brief path ("read this first"), the report or review path, the write-set, and the global constraints block from section 3. Nothing else from your history. Hand artifacts over as files, never pasted.
- Fix loops follow `subagent-driven-development`: rounds 1-3 resume the same implementer with the open findings verbatim; rounds 4-5 dispatch a fresh `aiu-implementer`; after round 5 adjudicate and ledger each open finding. Never fix findings yourself.
- Model policy from the owner: every subagent runs Opus 5.5 at high effort, except briefs marked **simple**, which run `aiu-simple-implementer`.

## 5. Workspace, ledger and tasks.md

- Workspace (git-ignored): `.ai-usage-local/AIU-045/run-2026-10-06/` with `ledger.md`, `briefs/`, `reports/`, `reviews/`, `packages/`, `evidence/`. If a superpowers script insists on `.superpowers/sdd/`, let it, since that path is excluded locally.
- Ledger first line: `# T-045 run ledger - prompt: docs/specs/T-045-ui-audit-follow-up/orchestration-prompt-2026-10-06.md`. Record BASE, kickoff answers, the pre-flight conflict table, every dispatch (task, agent, branch, worktree), every report status, every review verdict, every fix round, every merge sha, every push and CI run, and every `Ruling: <what> - <why> - <cost if wrong>`. After context compaction trust the ledger and `git log`, never your recollection.
- Create `docs/specs/T-045-ui-audit-follow-up/tasks.md` (tracked) in wave 0 from section 7 below, with the complete ownership metadata that [formats.md](../../workflow/formats.md) requires for explicitly parallel work: `status`, `depends_on`, `ownership`, `writes`, `shared`, `parallel`, `isolation`, `agent`, `acceptance`, `evidence`. The validator rejects unsafe concurrent ownership, so run it after writing the file; if it rejects a pair, serialize that pair and ledger the ruling. Update task statuses as waves complete; mark `done` only with an evidence path.
- Pre-flight conflict table (ledger, before wave 1): one row per pair of wave-1 tasks sharing a file or interface, and one row per task checking its own text against itself. The write-sets in section 7 are designed to be disjoint; prove it with `git diff --name-only` after each wave, before merging.
- Briefs: copy each task's block from section 7 into `briefs/<task>-brief.md` and add the global constraints block. The brief is the single source of requirements for that implementer and its reviewer.

## 6. Wave plan

```text
Wave 0  primary: preconditions, agents, workspace, tasks.md, BASE, push
Wave 1  parallel implementers A B C D E F G (worktrees) + R0 read-only WIP diff review
Gate 1  per-task reviewers -> fix loops -> merge A..G -> suites + A/B on host -> push -> CI green
Wave 1.5 fixers H (R0 fix dispositions) and I (ANL-11 defects, only if E is red) -> reviewers -> merge -> push -> CI
Wave 2  desktop lane, serialized: J candidate verification (Release smoke, native smoke, D9, one Sandbox batch)
Wave 3  K closing records (worktree)  ||  primary: D5 cleanup, ANL-19 renormalize
Final   L whole-run convergence review -> one fix wave (M) -> scoped re-review -> push -> CI green
Owner checkpoint: dispatch Preview, installed-build checks; then N post-install records -> push
```

### Wave 0 (you, on main)

1. Verify: `git status` clean; `git rev-parse HEAD` equals `origin/main`; `gh variable get AIU_PREVIEW_ENABLED` prints `false`; no other agent session is writing to this repository; `wsb list --raw` returns no running Sandbox.
2. Create the agent definitions (section 4) and the workspace and ledger (section 5). Record `BASE=<sha>`.
3. Write `tasks.md`, run `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` (expect `{"valid":true,...}`), commit `docs(T-045): register fix-run tasks T-045.1..T-04 with parallel ownership`, push, watch CI.
4. Write the pre-flight conflict table into the ledger.

### Wave 1 (parallel)

Dispatch A, B, C, D, E, F, G as implementers in worktrees and R0 as a reviewer, all in one response. D runs long (stress loops); that is expected. Nobody uses the interactive desktop in wave 1.

### Gate 1 (you)

1. For each implementer report: build the review package (`git log --oneline BASE..<branch>`, `git diff --stat BASE..<branch>`, `git diff -U10 BASE..<branch>` into `packages/<task>.diff`), dispatch its reviewer, run the fix loop to a clean verdict or the round-5 breaker.
2. Merge in this order, each with `git merge --no-ff worktree-<name> -m "merge(T-045): <task title>"`: B, A, C, D, F, G. Keep E unmerged if its report is DONE_WITH_CONCERNS with red probes (section 7, E). Remove each merged worktree and branch.
3. On merged main: Debug build, validator tests, canonical validation, `./tests/release/Test-PreviewRelease.ps1`, Infrastructure suite, Presentation suite (commands in README.md and the analysis record section 2). Then the desktop step: run the host A/B `.ai-usage-local/AIU-045/analysis-2026-10-05/aud01-ab.ps1` against the fresh Debug build. Expected: run A (original `overview.json`) now starts, shows the window and writes the marker; run B unchanged. Then run the AUD-04 negative check from task C's brief. Retain outputs under `evidence/gate1/`.
4. Push, watch CI: `validate` and `windows-package` succeed, `preview` skipped. This is the first CI run of the product suites on the WIP tree and the T-045.1 main-green checkpoint. If `ForcedKill...` or `EventsAreJson...` fails here, record it under ANL-05 and send it to task D's fix loop; never relax the assertion.

### Wave 1.5 (conditional, parallel)

- H: one fixer for every R0 disposition you ruled `fix` or `test` (section 7, R0 and H). Skip H if there are none; ledger that.
- I: only if E reported red probes; works on E's branch and makes them green by normalizing at the deserialization boundary; then E+I merge together.
Reviewers, merge, suites, push, CI as in Gate 1 (no desktop step).

### Wave 2 (serialized desktop lane)

J runs alone in the main checkout; nothing else writes `src/` or `tests/` meanwhile. Dispatch J without worktree isolation; it may commit red-to-green fixes to `main` only for failures it observes, and reports every commit. You push after J, then watch CI.

### Wave 3 (parallel)

- K: closing records, in a worktree (docs only).
- You: the D5 cleanup and ANL-19 renormalization from section 7, item "Primary cleanup", after J has finished and before L. Destructive steps run only after the ledger holds the exact list that will be deleted and that list matches kickoff answer 2.
Merge K, validator, push, CI.

### Final review

L: one `aiu-reviewer` over `BASE..HEAD` applying `.agents/skills/convergence-review/SKILL.md` and the superpowers final code-review rubric, with the deferred-minor and parked ledger lines as input. If it returns findings: one fixer M with the complete list, one scoped re-review, adjudicate residuals with rulings, merge, push, CI green.

Then stop at the owner checkpoint (section 9).

## 7. Task briefs

Each block is copied verbatim into a brief. "Writes" is the exclusive write-set; anything outside it is out of scope for that agent. Commands run from the worktree root unless stated.

### A - Dispatch-only Preview publication and T-014 records (T-014.1, closes ANL-01, ANL-03 code side; D1a)

- agent: aiu-implementer; worktree
- writes: `.github/workflows/validation.yml`, `tools/windows/PreviewRelease.psm1`, `tools/windows/Publish-Preview.ps1`, `tests/release/Test-PreviewRelease.ps1`, `README.md` (Preview section only), `docs/specs/T-014-preview-updates/spec.md`, `docs/specs/T-014-preview-updates/verification.md`, `docs/specs/T-014-preview-updates/bug-2026-10-04-installer-connection-aborted.md`, `docs/decisions/accepted.md` (R-157 entry only)
- steps:
  1. Red first, in `tests/release/Test-PreviewRelease.ps1`, before the existing `exit 0`: save and restore `GITHUB_ACTIONS`, `GITHUB_EVENT_NAME`, `GITHUB_REF`, `GITHUB_JOB`, `RUNNER_ENVIRONMENT`, `GITHUB_REPOSITORY`; set them to the owned hosted main `preview` job values; with `GITHUB_EVENT_NAME='push'` assert `Reject { Assert-PreviewPublicationRunner } 'owner dispatch'`; with `GITHUB_EVENT_NAME='workflow_dispatch'` assert it does not throw (`$script:count++`). Add text assertions on `.github/workflows/validation.yml` (regex on the file text, like the existing `Publish-Preview.ps1` check): the `preview:` job `if:` contains `github.event_name == 'workflow_dispatch'`, `inputs.PublishPreview == true`, `github.ref == 'refs/heads/main'` and `vars.AIU_PREVIEW_ENABLED == 'true'`, and does not contain `github.event_name == 'push'`; `workflow_dispatch.inputs.PublishPreview` has `type: boolean` and `default: false`; `MsixVersion` has `required: false`. Run `./tests/release/Test-PreviewRelease.ps1`; expected: the new assertions fail.
  2. `validation.yml`: add input `PublishPreview` (boolean, default false, description "Publish a signed development Preview from this main commit"); make `MsixVersion` optional; gate `preview` as asserted above; keep `needs: [validate, windows-package]`, the concurrency group and the environment. `PreviewRelease.psm1` `Assert-PreviewPublicationRunner`: require `GITHUB_EVENT_NAME -eq 'workflow_dispatch'`, message `Preview publication requires the owned hosted main owner dispatch.` `Publish-Preview.ps1` line 1 comment: update to the dispatch wording. Run the test script: expected `PASS: N release policy and source ancestry assertions` with N greater than before.
  3. Records: README Preview section (pushes to main never publish; the owner dispatches `gh workflow run validation.yml --ref main -f PublishPreview=true`; `AIU_PREVIEW_ENABLED` is the kill switch). T-014 `spec.md` AC-01: amend in place with "(amended 2026-10-06, AIU045-D1)" wording: publication runs only from an owner dispatch with `PublishPreview=true` on `main` after the same commit's validate and windows-package jobs pass. T-014 `verification.md`: replace the sentence "Each main push publishes one Preview, including documentation-only pushes" with the dispatch rule and the date; add a short "Rollback" note (ANL-14: uninstall, or sideload an older release asset; `ForceUpdateFromAnyVersion=false`; forward-only feed) and a "Retention and hosting" deferral note (ANL-20: 106 releases vs the 50 of R-164, `immutable: false`, unversioned dependency URL, Pages deploy race hypothesis; revisit when T-014 resumes). Bug note: add the Pages topology, the `efa9ea5` (2026-09-25) history, that the Pages-hosted App Installer GUI path is unverified, and the recurrence capture checklist (exact UTC time and URL, `AppXDeploymentServer/Operational` and `AppxPackaging/Operational` logs, App Installer `DiagOutputDir`, WinINet proxy settings); priority P2 per AIU045-D8. `accepted.md` R-157: append an amendment line in the register's existing amendment style: Previews publish only through an explicit owner dispatch of a green main commit; every green main merge is a candidate, not a release (2026-10-06, AIU045-D1).
  4. Run `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`; expected valid. Commit.
- acceptance: the test script passes with the new dispatch assertions; `grep -n "github.event_name == 'push'" .github/workflows/validation.yml` prints nothing for the preview job; the validator passes; every record above carries the 2026-10-06 amendment.
- reviewer re-runs: the test script, the validator, and reads the four amended records.

### B - Validator rejects links into the ignored local root (T-045.1, closes ANL-08)

- agent: aiu-implementer; worktree
- writes: `tools/AiUsage.ProjectValidation/ProjectValidator.cs`, `tests/AiUsage.ProjectValidation.Tests/**`
- steps:
  1. Red first: a validator test that builds a temporary root with `.ai-usage-local/evidence/a.png` present and `docs/x.md` containing a Markdown link whose target is `../.ai-usage-local/evidence/a.png`; expect one `BROKEN_LINK` whose message contains `.ai-usage-local/evidence/a.png`. A second case: `docs/x.md` linking an existing `docs/y.md` still reports nothing. Run `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo`; expected: the first case fails.
  2. In `CheckLinks` (around `ProjectValidator.cs:244-258`): treat a target whose repository-relative path starts with `.ai-usage-local/` as broken; make the message name the target, for example `Local Markdown link target '<relative>' is missing, escapes the repository or points into the ignored .ai-usage-local root.` No Git dependency, no new package.
  3. Run the validator tests (expected all pass, count increases) and the canonical validation on the worktree root (expected valid, since `gallery.md` was fixed on 2026-10-06). Commit.
- acceptance: both new cases pass; all validator tests pass; canonical validation passes on the worktree.
- reviewer re-runs: validator tests and canonical validation.

### C - Audit replay: revert the scope-annotation seam, packaged gate, observable Open failure (T-045.2, closes AUD-01 fix, AUD-02, AUD-04, ANL-09; D2b, D3a)

- agent: aiu-implementer; worktree
- writes: `src/windows/AiUsage.Windows/Adapters/Live/Audit/**`, `src/windows/AiUsage.Windows/Adapters/Live/Windows/AuditLedgerRegistration.cs`, `src/windows/AiUsage.Windows/Adapters/Live/LiveLedgerSource.cs`, `src/windows/AiUsage.Windows/App.xaml.cs`, `tests/windows/AiUsage.Presentation.Tests/AuditReplayTests.cs`, `tests/windows/AiUsage.Presentation.Tests/LiveLedgerSourceTests.cs`, `tests/windows/AiUsage.Presentation.Tests/Fixtures/**` (new fixtures only), `tests/windows/AiUsage.Infrastructure.Tests/AuditContractScenarios.cs`, `tests/windows/AiUsage.Windows.Tests/AuditWindows.cs`
- shared contract (read, do not redesign): `AuditInput` is also compiled into the Infrastructure tests via `<Compile Include>` in `tests/windows/AiUsage.Infrastructure.Tests/AiUsage.Infrastructure.Tests.csproj`; keep that project compiling.
- steps:
  1. Red first, in `AuditReplayTests`: `OpenAcceptsPageFixtureWithoutScopeAnnotations` and `OpenAcceptsMaintenanceFixtureWithoutScopeAnnotations`. Build the JSON by serializing the existing `Empty` fixture (and a `UseProductMaintenance = true` variant) with `AuditJson.Default.AuditInput`, removing the `ScopeAnnotations` property with `JsonNode`, writing it to a temp file, and calling `AuditReplay.Open(["--demo", "--audit-input=<path>"], <new %TEMP% root>)`. Assert a non-null replay and that `synthetic-audit.marker` exists. Run `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo -method "*AuditReplayTests*"`; expected: `ArgumentNullException` (page) and `NullReferenceException` (maintenance), matching `AuditReplay.cs:48` and `:43`.
  2. D3(a) revert: delete `AuditInput.ScopeAnnotations`; delete the `ScopeAnnotations` clause at `AuditReplay.cs:43` and lines 45-49; remove the `scopeAnnotations` constructor parameter and field of `LiveLedgerSource` (its `:23`, `:49`) and restore the plain `MonetaryScope.Unknown` default at `:191`; remove `scopeAnnotations: input.ScopeAnnotations` in `AuditLedgerRegistration.cs:41`; delete the two-case rejection theory in `AuditReplayTests` and the three-case `MonetaryScopeAnnotationsAreExplicitAndLimitedToTheirAccount` theory in `LiveLedgerSourceTests`; rewire `AuditContractScenarios.cs:47, 66, 97, 109` to a test-local `Dictionary<string, MonetaryScope>` that sets `MonetaryScope` on the projection input directly, keeping the account-isolation assertion. Run step 1 tests: green.
  3. D2(b): change the signature to `public static AuditReplay? Open(string[] arguments, string? selectedRoot, bool packaged)`; return `null` before any filesystem access when `packaged` is true; `App.xaml.cs:30` passes the project's existing packaged-process detection (find it with `grep -rn "Packaged\|PackageIdentity\|WindowsPackageType" src/windows/AiUsage.Windows --include=*.cs`; reuse it, do not add a new detection). Test `OpenIgnoresAuditInputInPackagedProcess`: valid input plus `packaged: true` returns null and creates no directory or marker. Red, then green.
  4. AUD-04 observability: in `App()` wrap the `Open` call in `try/catch (Exception exception)` that writes only `exception.GetType().Name` to standard error and calls `Environment.Exit(AuditReplay.OpenFailureExitCode)` with `internal const int OpenFailureExitCode = 90`. No storage is touched. In `AuditWindows.cs` `RecordStartupFailure` (around line 412): record the exit code and captured standard error even when no marker exists, and copy `critical-*.jsonl` when present. No new logging event.
  5. Run both suites in full (`-xml` retained). Confirm `grep -rn ScopeAnnotations src tests` prints nothing. Commit.
- negative check for Gate 1 (the primary runs it on the host, Debug build): launch `AiUsage.exe --demo --audit-input=<temp copy of overview.json with "SyntheticMarker" altered>` with a fresh `%TEMP%` root and redirected stderr; expected exit code 90 and stderr `InvalidDataException`.
- acceptance: step 1 and 3 tests are red then green with XML retained; both suites pass; no `ScopeAnnotations` remains in `src` or `tests`; the host A/B run A passes at Gate 1.
- reviewer re-runs: the `AuditReplayTests` filter and the full Presentation suite; confirms the Infrastructure suite still compiles and passes.

### D - Intermittent diagnostics tests (T-045.1, closes ANL-05)

- agent: aiu-implementer; worktree; use `systematic-debugging`
- writes: `tests/windows/AiUsage.Infrastructure.Tests/DiagnosticCrashTests.cs`, `tests/windows/AiUsage.Infrastructure.Tests/FileDiagnosticsTests.cs`, `src/windows/AiUsage.Infrastructure/Diagnostics/**` (only with a failing reproduction that proves a product race), `tools/AiUsage.DiagnosticsProbe/**` (only if the probe is the cause)
- facts: `ForcedKillLeavesPriorRecordsAndUnknownExitWithoutInventedCritical` failed in CI runs 37130755136, 37132742760, 37138798054, 37152530318, 37163134018 (`PreviousExitUnknown` not found, `DiagnosticCrashTests.cs:71`); `EventsAreJsonAndExceptionValuesNeverReachDisk` failed in 37149521238 (file in use). Both passed 25/25 in isolation on the host, twice.
- steps:
  1. Read the failing logs: `gh run view <id> --log-failed` for each run above; retain them under the report folder.
  2. Reproduce: loop the full Infrastructure suite at least 20 times (`dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo -xml <run-i>.xml`), then with CPU pressure (a parallel `dotnet build` of the Windows project, or a busy loop on all but one core). Budget: 3 hours wall-clock. Retain every XML.
  3. Inspect `DiagnosticCrashTests.cs:55-71` (forced kill versus the previous-exit marker being flushed by the probe) and `FileDiagnosticsTests.cs:15-30` (`ReadShared` versus the `FileDiagnostics` writer's share mode). Form one hypothesis per failure and test each against the reproduction.
  4. Fix the actual cause. If the race is in the test (for example killing before the probe has flushed the record the assertion needs), make the test wait for the observable precondition, never for time. If the race is in the product (marker ordering or file sharing), fix it with a red-to-green regression test. Never relax the assertions `Assert.Contains("PreviousExitUnknown", ...)` or `Assert.Empty(... "critical-*.jsonl")`.
  5. Verify: at least 20 consecutive full-suite runs green after the fix, XML retained. If no reproduction occurred within the budget, report `NOT_REPRODUCED` with the hypotheses, and change the tests only where a race is demonstrable from the code.
- acceptance: a recorded failing reproduction (or an explicit NOT_REPRODUCED with evidence), then 20 consecutive green full-suite runs; the next CI runs of this fix run stay green.
- reviewer re-runs: 5 full-suite runs; reads the retained reproduction log.

### E - Omitted-property probes for persisted records (T-045.2, closes ANL-11)

- agent: aiu-implementer; worktree
- writes: `tests/windows/AiUsage.Infrastructure.Tests/PersistedRecordOmittedPropertyTests.cs` (new) and new fixtures under `tests/windows/AiUsage.Infrastructure.Tests/Fixtures/`; no product code
- steps:
  1. Enumerate persisted `[JsonSerializable]` record types in Infrastructure and Core whose properties have non-default `init` initializers (start with `AccountRegistry.cs:10` `Connected = true`, `AccountRegistry.cs:24` `Accounts = []`, `IBudgetStores.cs:14` `RoundingUnit = 1`; find the rest with `grep -rn "{ get; init; } = " src/windows/AiUsage.Infrastructure src/windows/AiUsage.Core`).
  2. For each, a test that deserializes JSON omitting that property through the production `JsonSerializerContext` and asserts the initializer value survives (`Connected` true, `Accounts` empty not null, `RoundingUnit` 1). Also one probe for a type without a parameterized constructor, to answer the record's open question.
  3. Run the Infrastructure suite. Report the observed behavior per type in the report, including the exact mechanism (constructor parameter versus property setter). Do not change product code: a red probe is a finding for the primary (wave 1.5, fixer I).
- acceptance: every persisted type with a non-default initializer has a probe; the report states per type whether the initializer survives; red probes are listed under concerns with STATUS DONE_WITH_CONCERNS.
- reviewer re-runs: the new test class; checks the enumeration against the grep.

### F - Audit tool scripts (T-045.2 ANL-17, T-045.4 CSV destination) - simple

- agent: aiu-simple-implementer; worktree
- writes: `tools/windows/Run-UiAuditSandbox.ps1`, `tools/windows/New-UiAuditPages.ps1`
- steps:
  1. `Run-UiAuditSandbox.ps1`, after the `build.json` block (lines 34-39): write `git diff HEAD --binary` to `<input>/source.diff` (write the file even when the diff is empty) and add `SourceDiffSha256` (SHA-256 of that file) to the `build.json` object. Keep `Source` and `Dirty`.
  2. `New-UiAuditPages.ps1`: the CSV outputs at lines 87, 89 and 101 must default to a directory under `.ai-usage-local/ui-audit/coverage/` (new parameter `CoverageDirectory` with that default), never under `docs/`. Writing into `docs/workflow/ui-ux-audit-2026-10-04/` now requires passing the parameter explicitly.
  3. Verify: `pwsh -NoProfile -Command { Get-Help ./tools/windows/New-UiAuditPages.ps1 -Parameter CoverageDirectory }` shows the parameter; a dry run of `New-UiAuditPages.ps1` into a temporary `OutputDirectory` writes CSVs under `.ai-usage-local/ui-audit/coverage/` and nothing under `docs/`; `Run-UiAuditSandbox.ps1` is not executed (it needs Sandbox), but `pwsh -NoProfile -Command { $null = [Management.Automation.Language.Parser]::ParseFile('tools/windows/Run-UiAuditSandbox.ps1', [ref]$null, [ref]$e); $e.Count }` prints 0.
- acceptance: the three checks above; `git status` shows no change under `docs/` from the dry run.
- reviewer re-runs: the parse check and the dry run.

### G - Process and cross-cutting records (T-045.1 docs, T-045.4 D4, D6 drafts, AUD-10, AUD-05..09)

- agent: aiu-implementer; worktree
- writes: `CONTRIBUTING.md`, `docs/workflow/verification.md`, `docs/backlog.md`, `docs/specs/T-039-multi-account-ledger/verification.md`, `docs/specs/T-043-file-logging/verification.md`, `docs/specs/T-044-account-monetary-usage/verification.md`, `docs/specs/T-045-ui-audit-follow-up/verification.md`, `docs/workflow/ui-ux-audit-2026-10-04/*.md`
- steps:
  1. CONTRIBUTING Git policy: save points run CI normally and never carry `[skip ci]`; Previews publish only through an explicit owner `workflow_dispatch` (AIU045-D1); a green push to `main` is a candidate, not a release. CONTRIBUTING review paragraph: a Preview is an owner-test build; its gate is the D4 list; full independent review stays for public release approval or explicit owner request.
  2. `docs/workflow/verification.md`: a "Preview gate" section (D4a): required on the exact commit are CI `validate` and `windows-package` green, an ordinary `--demo` startup smoke of the Release candidate when `src` changed, a primary diff review, and an explicit owner dispatch; opt-in only are the Sandbox UI corpus, gallery and control matrices, package, upgrade and feed smoke (for manifest, packaging, update or migration changes), live-provider checks (provider or auth changes, with authorization) and independent review on the CONTRIBUTING triggers.
  3. `docs/backlog.md`: amend the T-014 `scope-note` with one sentence dated 2026-10-06 (dispatch-only publication per AIU045-D1). Do not change any status.
  4. D6(a) annotations: add a "Post-done corrections (2026-10-06)" section to the T-039, T-043 and T-044 verification records, listing the FIX IDs that belong to each task (T-044: FIX-03, 04, 05, 06, 11, 12; T-039: FIX-01, 02, 07, 08, 09, 10, 13, 14; T-043: the AC-08 startup-ordering change), the fixing commits (derive from `git log 383644c..HEAD` and the audit report), and the never-accepted gaps: T-043 AC-13 packaged Open logs NOT_RUN and the undiagnosed device-code display delay; T-044 installed-app repeat-launch and lease fault fixed with unpackaged evidence only. Mark each gap "pending the owner's installed-build check (T-045 T-045.3)". Do not reopen the tasks.
  5. T-045 `verification.md`: AUD-10 frozen as historical; AUD-05..AUD-09 recorded as opt-in follow-ups with their exact next action unchanged; note that generated CSVs now default to `.ai-usage-local/ui-audit/coverage/` (task F). `docs/workflow/ui-ux-audit-2026-10-04/report.md` and `gallery.md`: add a first-line banner "Historical record, frozen 2026-10-06 (T-045 AUD-10); not maintained."
  6. Run the canonical validator; commit.
- acceptance: validator passes; every sentence above is present with its date; no status field changed.
- reviewer re-runs: the validator; reads each changed section against this brief.

### R0 - Read-only review of the WIP product diff (T-045.3 step 1, ANL-21, T-045 AC-03/AC-04)

- agent: aiu-reviewer (no worktree; reads `main` at BASE)
- inputs: `git diff 383644c..BASE -- src` written to `packages/wip-src.diff`; the T-045 spec ACs; the ANL-21 notes.
- output `reviews/R0-wip-diff.md`: for each ANL-21 note a proposed disposition keep / test / fix with evidence: FIX-06 percent-bar geometry without a dedicated test; `LedgerViewModel.OnSourceChanged` being `async void` and re-reading history on every source change; `CardMark.ScopeLabel` added without the backlog contract agreement; the T-044 AC-03 on-extra-usage mark unreachable in live mode. Plus any Critical or Important defect in the WIP diff against AC-03 (unknown/shared/disabled/currency restrictions and account isolation preserved).
- the primary rules on each disposition (ledger `Ruling:`), and feeds `fix` and `test` items to H.

### H - WIP diff fix dispositions (wave 1.5, conditional)

- agent: aiu-implementer; worktree
- writes: `src/windows/AiUsage.Windows/Features/Ledger/**`, `tests/windows/AiUsage.Presentation.Tests/**` except the files owned by C
- steps: for each ruled item, red test first, minimal change, green, full Presentation suite, commit. For `async void OnSourceChanged`, prefer an `async Task` method awaited through the existing UI-thread publication path with a deterministic test; do not add a framework or a scheduler abstraction.
- acceptance: each ruled item has a disposition recorded in the report with test evidence; both suites pass.

### I - ANL-11 defect fix (wave 1.5, only if E has red probes)

- agent: aiu-implementer; works in E's worktree and branch
- writes: the persisted record types E named, in `src/windows/AiUsage.Infrastructure/**` or `src/windows/AiUsage.Core/**`, plus E's test file
- steps: make the red probes green by normalizing at the deserialization boundary (for example a null-coalescing `init` accessor or a post-read normalization in the store that owns the record), never by relaxing the probe. Use the `security-lifecycle` skill if a credential or migration record is involved. Full Infrastructure suite green; commit.
- acceptance: E's probes all pass; no other test changed.

### J - Candidate verification on the desktop lane (T-045.3, closes AUD-01 AC-02 closure, AUD-03, ANL-04, ANL-07, ANL-10, FIX native reruns; D7a, D9)

- agent: aiu-implementer; **no worktree**: works in the main checkout, sole writer; use `systematic-debugging` for any failure
- writes: `docs/specs/T-045-ui-audit-follow-up/verification.md` (new section "Candidate verification <date>"), `docs/specs/T-045-ui-audit-follow-up/evidence/**` (small JSON/XML only), and `src/**` or `tests/**` only for a red-to-green fix of a failure observed in this task
- steps:
  1. Record `HEAD` and build Release: `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore`; record the SHA-256 of the built `AiUsage.dll` and `AiUsage.exe`.
  2. Host ordinary `--demo` startup of that Release exe with a fresh `%TEMP%` root, window observed, Ctrl+Q exit code 0, log shows `SessionStarted` (mode demo) and `SessionExited`, no Application events 1000/1001/1026 for `AiUsage.exe` in that window (model it on `.ai-usage-local/AIU-045/analysis-2026-10-05/smoke-full.ps1`, pointing it at the Release exe).
  3. Publish the native driver: `dotnet publish tests/windows/AiUsage.Windows.Tests -c Release -r win-x64 --self-contained true -o .ai-usage-local/AIU-045/run-2026-10-06/evidence/smoke`. Set `AIU_SMOKE_EXE` to the Release exe, `AIU_SMOKE_EVIDENCE_DIRECTORY` to a fresh folder per run, `AIU_SMOKE_MODE=demo`. Run, one method per invocation with `-method`, XML retained: `LedgerSmoke.LedgerLaunchSettingsHistoryAndExit` (demo true), `LedgerSmoke.AccountSpendingUsesNestedContentHistoryCapsAndAccountActions`, `LedgerActivationSmoke.WorkBudgetShowsMonthlyAndDailyBarsBesideSubscriptionWithoutCredits`, and the T-043 startup and disposal native checks named in `docs/specs/T-043-file-logging/verification.md` (locate them; if a named check has no Windows.Tests method, record NOT_APPLICABLE with the quote). `ShellSmoke.PackagedLedgerLaunchesAndExits` needs an installed package: record NOT_RUN here; it belongs to the owner checkpoint.
  4. D9 live-empty: unset `AIU_SMOKE_MODE`, set an empty `AIU_DEVELOPMENT_STATE_DIRECTORY` under `%TEMP%`, zero accounts, run `LedgerSmoke.LedgerLaunchSettingsHistoryAndExit` with `demo: false`. If it fails with `ElementNotEnabledException` at the Used/Left invoke (`LedgerSmoke.cs:241`, ANL-10): determine whether Used/Left is correctly disabled with zero accounts (then fix the test expectation with evidence) or enabled too late (then fix the product race red-to-green). Commit the fix on `main` and report it.
  5. One Sandbox batch (D7a), new output directory: `./tools/windows/Run-UiAuditSandbox.ps1 -PageDirectory .ai-usage-local/ui-audit/combined-pages -OutputDirectory .ai-usage-local/ui-audit/candidate-2026-10-06 -TestMethod '*AuditLayoutControls.OrdinaryMouseResizeAndCaptionControlsKeepOpenFormsHistoryAndMenusUsable', '<FIX-09 scenario>', '<FIX-10 scenario>', '<FIX-11 scenario>'` where the three scenario method names come from the FIX mapping in `docs/workflow/ui-ux-audit-2026-10-04/report.md` and `controls.csv` (resolve them before the run and record them). The runner accepts several `-TestMethod` values; this is one guest session. Expected: every method PASS with captures written; if the run fails at startup, stop the lane and report, since that reopens AUD-01.
  6. Write the "Candidate verification" section: commit, binaries' SHA-256, every method with PASS/FAIL/NOT_RUN/NOT_APPLICABLE, evidence paths, deviations; copy the result JSON/XML (not screenshots) into `evidence/candidate-2026-10-06/`. Final statuses: AUD-01 closed (root cause, fix commit, AC-02 rerun), AUD-03 closed, ANL-04, ANL-07, ANL-10 as observed. Commit.
- acceptance: dispatch gates 5-8 of the analysis record section 6 are met or each miss is recorded with its cause; the Sandbox scenario passes; the verification section exists with build references.
- reviewer (post-wave, read-only): checks that every PASS has a retained artifact and that no assertion was weakened (`git diff` of `tests/` in J's commits).

### K - Closing records (T-045.4 AC-01, AC-04, AC-05)

- agent: aiu-implementer; worktree
- writes: `docs/backlog.md` (T-045 entry), `docs/specs/T-045-ui-audit-follow-up/spec.md` (status and next action), `docs/specs/T-045-ui-audit-follow-up/tasks.md` (statuses), `docs/specs/T-045-ui-audit-follow-up/analysis-2026-10-05.md` (append a "Final status" table only), `docs/decisions/pending.md` (only if a decision remains open), `AGENTS.md` (Codex section removal only, if kickoff answer 3 is yes)
- steps:
  1. Append to the analysis record a "Final status (2026-10-xx)" table: every AUD-xx, FIX-xx, ANL-xx and T-xx with its final status and the commit or evidence path; ANL-12, ANL-16 and the installed-build checks stay "pending owner checkpoint" until N.
  2. `spec.md`: lifecycle status to the value formats.md permits for a completed fix batch with deferred opt-in follow-ups (`implemented` if the validator accepts it with the backlog status you set; otherwise the nearest permitted pair, ledgered as a ruling by the primary); "Exact next action after resumption" becomes the owner checkpoint list of section 9.
  3. `backlog.md` T-045: status per the validator's permitted combination with the spec status (target `done` with `evidence:` pointing at `verification.md`; the comprehensive audit continuation stays a separately selected follow-up noted in the entry). Add a `completion-note` with the date, the Preview candidate commit and the owner checkpoint items.
  4. `tasks.md`: every task `done` with evidence or `dropped` with the reason; no `pending` left.
  5. AGENTS.md: remove the Codex subagent policy section if authorized; nothing else.
  6. Validator; commit.
- acceptance: the validator passes; every AUD and ANL ID has a final status; statuses form a permitted combination.

### Primary cleanup (T-045.4 D5, ANL-18, ANL-19), done by you after J, before L

1. Ledger the exact list, compare with kickoff answer 2, then: `git worktree remove` for `.claude/worktrees/aiu-038-presentation-redesign-1c2839`, `.claude/worktrees/architecture-audit-plan-2be087`, `.ai-usage-local/AIU-007/worktrees/security-review` and this run's leftover worktrees; `git branch -d users/aiu-038-presentation-redesign-1c2839 users/architecture-audit-plan-2be087 users/tray-icon-fix-c87db2`; `git push origin --delete users/aiu-038-presentation-redesign-1c2839`.
2. Prune `.ai-usage-local/ui-audit/*/input/` except `layout-recovery-8/input/app`, `layout-resize-9/input/app`, `layout-resize-10/input/app` and every `input/build.json`; list what will be removed before removing; never touch `combined-pages`, `pages`, `composites`, `candidate-2026-10-06` or `sandbox-probe`.
3. Delete `%TEMP%\aiu-anl-*` and `%TEMP%\aiu-aud01-*`.
4. ANL-19: for every file `git ls-files --eol` reports as `w/mixed` or `w/crlf`, remove the working copy and `git checkout -- <file>`; afterwards `git status` is clean and no `w/mixed` remains. No commit results from this step.

### L - Whole-run independent review

- agent: aiu-reviewer; inputs: `packages/run.diff` (`git diff BASE..HEAD` with `-U10`, plus `git log --oneline BASE..HEAD`), the analysis record, the spec ACs, CONTRIBUTING, the ledger's deferred-minor and parked lines.
- method: `.agents/skills/convergence-review/SKILL.md` (acceptance and material safety boundaries: credential paths, destructive data, privilege, publication gating) plus the superpowers final code-review rubric. Verdict PASS, PASS_WITH_CORRECTIONS, FAIL or BLOCKED with path/symbol evidence; zero findings is valid.
- M (conditional): one fixer with the complete findings list, in a worktree with a write-set you define from the findings; one scoped re-review; residuals adjudicated with rulings.

### N - Post-install records (after the owner checkpoint)

- agent: aiu-simple-implementer; worktree; writes: `docs/specs/T-045-ui-audit-follow-up/verification.md`, `docs/specs/T-045-ui-audit-follow-up/analysis-2026-10-05.md` (Final status rows), the three D6 records' "pending" lines, `docs/specs/T-014-preview-updates/verification.md` (dispatch run id and version)
- steps: record the owner's reported results verbatim with the Preview version and run id; set the pending rows to PASS, FAIL or ACCEPTED_GAP per the owner's words; if kickoff answer 4 was yes, record the sanitized migration-event result you read. Validator; commit.

## 8. Reviewer and report contracts

- Implementer report (`reports/<task>-report.md`): what changed and why, every command with its exit status and retained output path, red-to-green evidence (the failing output and the passing output), deviations from the brief, concerns. Return at most ten lines.
- Reviewer review (`reviews/<task>-review.md`): acceptance table (item, met / not met / cannot verify, evidence), re-run results with paths, findings (Critical, Important, Minor; file:line), verdict. "Cannot verify" items are yours to resolve before you mark the task complete.
- Ledger lines: `Dispatch:`, `Report:`, `Review:`, `Fix round R/5:`, `Merge:`, `Push:`, `CI:`, `Ruling:`, `Task <X>: complete (...)`, `Kickoff:`.
- Severity for the final report: P0 blocks main green or allows publication or harms the owner's installation or data; P1 real defect or risk for the next batch; P2 deferrable; P3 hygiene.

## 9. Owner checkpoint and final report

Stop here, once, and give the owner this, in this order:

1. **State**: HEAD sha, CI run id and result, `preview` skipped, the candidate's binaries' SHA-256, the Sandbox and host smoke verdicts.
2. **What the owner does now** (exact commands):

```bash
gh variable set AIU_PREVIEW_ENABLED --body true
```

```bash
gh workflow run validation.yml --ref main -f PublishPreview=true
```

   Then, on the installed build: the update applied to the new version; packaged startup; close-to-tray and relaunch restore the window without `LeaseUnavailable`; packaged Open logs (T-043 AC-13); optionally `ShellSmoke.PackagedLedgerLaunchesAndExits` with `AIU_SMOKE_AUMID` on the unlocked desktop (you may run that one if the owner gives the AUMID). Say that `AIU_PREVIEW_ENABLED` may stay `true` afterwards because pushes no longer publish, or be set back to `false` as a kill switch.
3. **Rulings I made**: every `Ruling:` line from the ledger, in order, each with its cost if wrong.
4. **Findings register**: final status of every AUD, FIX, ANL and T ID, with PASS, FAIL, NOT_RUN, BLOCKED or ACCEPTED_GAP, and the evidence path.
5. **Deviations and limitations**: anything NOT_RUN or BLOCKED, with cause and the retry count.

Wait for the owner's reply, run N, push, watch CI, and end with the final state line and the list of anything still open.
