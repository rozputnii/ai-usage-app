# Claude workflow audit — Stage 1 report

Status: Stage 1 analysis complete, 2026-10-08. Read-only toward the repository: no rule, tool,
setting, test or code was changed. Base: `origin/main` at `4f32185`, fetched again before
writing and unchanged. Lane reports are in `lanes/<lane-id>.md`, for example
[A1](lanes/A1.md) and [B1](lanes/B1.md); lane A4 was split into A4a, A4b and A4c by
transcript range. Prompt: [stage-1-prompt.md](stage-1-prompt.md). Status: [status.md](status.md).

Labels: **fact** (observed in the repo, Git/CI history, transcripts or primary docs),
**inference** (the architect's or a lane's judgement), **external-claim** (stated by a third
party, not tested here). Counts from transcripts are approximate where the lane says so.

## 1. Executive summary

The repository's quality machinery is strong and worth keeping:
- AC-driven specs and honest PASS/FAIL/NOT_RUN/BLOCKED evidence;
- a cheap validator with a CI `--final` gate;
- fast unit suites (1,245 cases, about 35 s warm);
- a launch smoke as the Preview gate;
- D-190 post-deploy owner checks;
- conventional, check-annotated commits.

CI failed in about 15 % of runs before the Claude-Code-only switch and in 2 of 74 runs
(2.7 %) after it. That sample is small, and crediting the switch is inference: WIP pushes
and `[skip ci]` stopped at the same time (A5-F3/F4/F15).

What costs time and owner attention is mostly **contradictory, invisible or prose-only
process**, not missing rigor:

1. **The written Git policy contradicts practice.** CONTRIBUTING says to work directly on
   `main` and push work in progress after each meaningful step. Every green push to `main`
   publishes a Preview to the owner's auto-updating app. All recent work runs in worktree
   branches. These are facts in the policy text. No WIP push has happened since the switch
   (A5-F15), so the risk is latent (inference). Sources: A1-F1/F2, A3-F7, A5-F10, B5-F12.
2. **The repo's own guardrails are invisible to Claude Code.** The security-lifecycle,
   provider-evidence and convergence-review skills live in `.agents/skills`, which Claude Code
   does not scan. They had 0 Skill-tool uses in 176 transcripts. The main reviewer agent is
   git-excluded and unversioned (fact; A1-F5, A3-F3/F4, B1-F1/F4, B2-F13).
3. **Hard safety rules are prose only.** Since 2026-10-06 sessions run in `bypassPermissions`
   (`auto` before that) with no deny rules anywhere. Deny rules are enforced even in bypass
   mode, verified in the permission-modes doc. No force-push or credential read was observed,
   so this is a preventive fix (A3-F5, B1-F2/F3).
4. **Ceremony and review are not sized by risk.** AIU-055 produced about 11k record words
   for about 2.3k changed lines. AIU-045 ran 17 review agents, most of them returning Minor
   findings only. The T3-type triggers for focused review are worded differently in three
   files (A1-F12, A4b-F7, B5-F2/F5).
5. **The UI smoke is the dominant cost and flake source.** One AIU-045 task took 227 turns.
   Worktree sessions spent about 136 min on smoke commands. The causes are mostly
   environmental: a locked desktop, the owner's installed app's tray icon, stale UIA
   references (A6-F3, A4b-F6, A4c-F5, A5-F6).
6. **Plugin process competes with repo process.**
   - The superpowers injection ("1 % chance → MUST invoke") auto-updates unpinned.
   - Five `tasks.md` files carry override sections because of it (B2-F2).
   - Its spec-review, execution-mode and finish-branch gates coincided with about 15 pure
     approval turns before the owner's 2026-10-07 correction. Causation is inference
     (A4c-F1).
   - Four more plugins have zero uses and cost about 5k context tokens per session (A3-F2,
     B2-F1).
7. **Bookkeeping churn.**
   - 36-38 % of commits are records-only, 45 % if every Markdown path counts.
   - Each records-only push still builds, signs and publishes a Preview: 159 Preview tags in
     16 days, 37 on 2026-10-08.
   - The backlog is 78 % closed history (A2-F1/F4, A5-F1).

Several owner-turn sinks were already fixed in the last two days, by rules that live partly
in memory. Status questions dropped to 0 after the 2026-10-08 push-status rule, and pure
approvals dropped from about 15 to 3 after the 2026-10-07 no-repeated-gates direction
(A4c-F1/F2). Part of the remaining work is to make those fixes repo policy, so they hold in
fresh machines, sessions and subagents.

The target workflow (section 4) keeps every quality gate. It makes repo rules the single
authority, adds mechanical deny rules, sizes records and review by the **areas a change
touches**, and removes process noise.

Top five recommendations:
- **R1:** one Git and integration flow.
- **R2:** discoverable, versioned guardrail skills and reviewer.
- **R3:** deny rules.
- **R4:** area-based risk tiers and review.
- **R5:** a trustworthy smoke.

Section 8 lists 30 yes/no owner decisions.

## 2. Current-state findings, cross-validated

Each theme cites the lanes that independently support it. Lane disagreements are resolved in
section 7.

### 2.1 Rules and authority

- **Git policy vs practice.** Policy text, fact:
  - CONTRIBUTING.md:32-38 says "do not create new branches".
  - D-178/D-179 describe a third policy.
  - D-196 and memory assume worktrees.

  Practice, fact: since 2026-10-01 there were 30 merges of `main` into worktree branches
  plus 15 integration merges. Of 38 local branches, 37 are merged; the unmerged one is this
  audit branch. Sources: A1-F1, A3-F7, A5-F10, B1-F6, B5-F12.
- **Path for unverified work to reach the owner's app.** Fact:
  - CONTRIBUTING.md:28 and :33-35 ask for pushes of incomplete work to `main`.
  - CONTRIBUTING.md:45-49 publishes a Preview on every green push.
  - The AIU-045 analysis rated this P0 (ANL-01). Its fix was reversed the same day
    (`ff1d788`).

  Inference: no Claude-Code-era incident has happened; worktree practice currently prevents
  it. Sources: A1-F2, A5-F12/F15.
- **Duplication and accretion (fact; the 45 % share is a manual estimate).** 14 rules appear
  in 2-5 places, and about 45 % of AGENTS.md restates CONTRIBUTING or verification.md. Dated
  "Owner direction" paragraphs and reversal chains sit inside operative rules. Five process
  directions have no decision record (A1-F6/F7/F8).
- **Memory duplicates rules (fact).**
  - 5 of 7 memory entries restate repo rules.
  - Two rules exist only in memory: process-skill gates count as approved, and worktree
    branches merge to `main`.
  - One entry is stale: the Codex section it refers to was removed in `5d24744`.
  - Auto memory is shared across worktrees. Its detail files are not loaded into subagents.
    The index did reach this audit's general-purpose subagents (observed), so host facts
    arrive only as one-line hints.

  Sources: A1-F3, A3-F6, B1-F5, B4-F1/F2; critic C2-6.
- **Stale direction (fact).** goals.md keeps `active_goal: G-002`, which is done, though it
  states it deliberately keeps the last direction. AIU-046 is still `review` although it
  shipped (A1-F10, A2-F12).
- **Decisions without back-pointers (fact).**
  - 18 recent decisions amend 21 earlier ones, and none of the 21 says "Amended by".
  - 20 entries are identical stubs.
  - AIU-051's spec still describes the settings sheet that D-197 replaced.

  Sources: A1-F11, B4-F4, A2-F6.
- **Always-loaded text: the size is fine, the composition is not.**
  - Fact: AGENTS.md plus the memory index come to about 1,270 tokens.
  - Fact: the Git flow, the Preview consequence and the check commands live only in
    read-by-task files.
  - Inference: nearly every session needs them.
  - Estimate: plugin injections and skill listings add roughly 6-7k tokens.

  Sources: A1-F14, B1-F6, A3-F10.
- **Review triggers worded three ways (fact).**
  - CONTRIBUTING:82: "credential, destructive-data or privilege".
  - verification.md:24: "authentication or durable-state boundaries".
  - security-lifecycle adds migrations and recovery.

  Sources: A1-F12; critic C1-5.

### 2.2 Agent tooling

- **Repo skills cannot be discovered (fact, 5 lanes).** They had 0 Skill-tool uses and 7
  direct reads. The validator and formats.md still point at `.agents/skills`. D-034 made the
  location agent-neutral while Codex was in use (A1-F5, A3-F3, B1-F1, B2-F13, B4-F7).
- **Custom agents are untracked and run-specific (fact).**
  - `.claude/agents/` is in `.git/info/exclude`.
  - The descriptions say "AIU-045", and model ids are pinned.
  - aiu-reviewer has 26 runs.
  - The first 8 AIU-045 dispatches failed because the types were not loaded.

  Sources: A3-F4, B1-F4, B5-F9, A4b-F4.
- **Plugins (fact).**
  - superpowers injects about 850 tokens at every start, clear and compact. It auto-updates
    unpinned through claude.ai sync; v6.4.1 and v6.4.2 changed plan execution and plan
    format.
  - The `tasks.md` files of AIU-046, 048, 053, 054 and 055 declare superpowers sub-skills
    with override sections.
  - ux-superpowers, design-superpowers, desktop-commander and the design plugin: 0 uses.
  - The ux-superpowers hook errors in 26 of 27 worktree sessions.
  - desktop-commander runs `npx -y …@latest` and has 2026 CVEs.
  - Mitigating: no `docs/superpowers` files were ever written (A4b-F10), and the owner
    starts sessions with `/superpowers:brainstorming` deliberately (12+ times).

  Sources: A3-F1/F2, B2-F1..F9, A4c-F10.
- **Dead artifacts (fact).**
  - `.codex/` still sets a gpt-6-astra subagent.
  - `.omp/`, `.github/copilot-instructions.md` and `docs/archive/omp/` (84 KB) remain.
  - Two migration docs and the architecture-audit plan are headed "Ready for execution"
    although they were executed.
  - `environment.md` is an OMP bootstrap log containing personal identifiers.
  - In total about 18,000 words of non-live history sit in docs/workflow.

  Sources: A1-F4/F9, A3-F8, A6-F13.
- **Worktree sprawl (fact).**
  - 32 worktrees are registered besides the main checkout. 31 are on merged branches; the
    other is this audit.
  - Together they take 8.1 GB, including this audit's worktree (A5 measured ≥6.1 GB over 26
    folders).
  - Desktop auto-archive fires only when a PR closes, and the owner uses no PRs.

  Sources: A3-F7, A4c-F13, A5-F14, B1-F7.

### 2.3 Owner involvement (transcripts)

- **Owner turns.** A4a counted 110 turns up to 2026-10-03; A4b about 51 for 2026-10-04..08
  in the main folder; A4c 109 in worktrees. In the early range, about 50 of 110 were status
  pulls, prompt requests, Codex relays or routine approvals (A4a-F13).
- **Avoidable questions (inference).** A4a found about 13 of 40 to 2026-10-03, partly from
  the Codex era. A4b found about 13 of 27 in 2026-10-04..08. Examples: "write yes and I'll
  mark your review PASS", a second confirmation after a variant pick, and asking to merge or
  push. **After 2026-10-07 the rate is much lower** (A4c-F1/F2). The fixes live partly in
  memory, and memory does not reach fresh machines or subagent briefs (critic C2-4).
- **Prompts that narrowed standing policy.** Fact: a 1,404-minute wait for merge approval
  and a dev sign-in. Inference: the agent-written kickoff prompt ("do not merge or push
  without permission") was the cause. D-190 now covers the sign-in part (A4b-F1).
- **Unattended runs stalled on questions (fact).** AIU-045 kickoff questions blocked 85 min,
  and a mid-run batch 118 min. A desktop-lock question could have been checked mechanically
  instead of asked (A4b-F3).
- **What works (fact).**
  - The push-status rule.
  - Rendered design variants.
  - `/goal` runs from a repo plan, which needed 0-1 owner turns.
  - Honest BLOCKED reports.

  Sources: A4c-F2/F9, A4a-F13, A4b KEEP.

### 2.4 Records and history

- **Backlog (fact).** 627 lines and 11,030 words. Done items are 78 % of item words. There
  are 37 field names, 20 of them ad-hoc `*-note` fields (A2-F1).
- **Plans in the repo (fact).** tasks.md is the largest record, 1.4-2.8× the spec. It is
  referenced from outside its folder 0 of 28 times (A2-F3, B5-F2).
- **Records-only commits (fact).** 38 % of non-merge commits overall and 36 % since
  2026-10-05. There are 19 `record T-xx` commits (A2-F4, A5).
- **Numbering (fact).** D-196 ended collisions; before it, AIU-046 was used twice and D-192
  was taken at merge. It still costs a numbering commit and a register conflict per merge.
  One bulk replace rewrote D-196's own text (`d8b5efa` → `a0f0d22`). 25 commit subjects keep
  placeholders (A2-F7, A4c-F3, A5-F8).
- **Decision inflation (fact).** 16 decisions in 2 days, mostly UI variant picks (A2-F11,
  A5-F9).
- **Open post-deploy loop (fact).** 24 NOT_RUN mentions in the backlog and in 7+
  verification files, with no single list of what to check (A2-F9, B5-F11).
- **What is actually used (fact).** spec.md (35 of 38 are referenced externally), the
  verification AC table, and the validator, which appears in about 379 transcript commands
  (A2-F14).

### 2.5 Tests, smoke and CI

- **Local checks are cheap (fact, single sample).**

  | Check | Time |
  |---|---|
  | Validator | about 3 s |
  | Presentation suite | 4.6 s |
  | Infrastructure suite | 28 s; two serial classes take 22.7 s and 15.8 s |
  | App build when nothing changed | 19 s |
  | Full UI-change pass, warm | about 1.5 min |

  Source: A6.
- **Smoke is where the cost and flakes are.**
  - 389 smoke commands in worktree sessions, about 136 min.
  - One task edited the same smoke file 31 times.
  - NativeTrayClicks passed 19 of 44 recorded runs. That count includes red-first runs, so
    it is not a flake rate.
  - The installed app's tray icon and its auto-updates broke dev smokes.
  - 71 `ByName` locators against 22 `ByAutomationId`, and 25 `Thread.Sleep` calls.

  Sources: A6-F3, A4b-F6, A4c-F5, A5-F6, B6-F10.
- **Smoke is the only guard for about 3,500 lines** of code-behind, controls and lifetime
  code. Tray and launch regressions reached `main` (A6-F4, A5-F11).
- **Gate text vs practice (fact).**
  - The Preview gate names the Release candidate, but smokes run against Debug, so each pass
    builds twice.
  - The matrix has no rows for scripts, harness-only or test-only changes.
  - `Test-PreviewRelease.ps1` runs only in CI and is undocumented.
  - The smoke condition "when `src` changed" misses product inputs outside `src`, such as
    `Directory.Packages.props` (critic C1-6).

  Sources: A6-F5/F7.
- **Flakes (fact).** DiagnosticCrashTests caused 6 red CI runs and took 3 days to fix. It
  failed again on a cold local run. Host memory says to rerun and "never relax the bound"
  (A5-F4, A6-F2).
- **CI (fact).**
  - Run time p50 6.5 min.
  - The Preview job p50 is 3 min, and its queue reached 12 min during bursts.
  - The AIU-002 routing spike is built on every run.
  - CI runs only after code is on `main`.

  Sources: A5-F2/F13.
- **Most owner-found escapes are product intent, not test gaps (inference).** Examples:
  hand cursor, a closeable strip, cap validation, focus visuals (3×), the tray icon above
  100 % scale. Better tests would have caught only a few of them (A5-F7).

### 2.6 Parallel and unattended execution

- **AIU-055 worked (fact).** 11 tasks in 2h13m, one owner turn, every task reviewed. It
  produced one Important finding (T-10 flyout width, per-task review); the whole-feature
  review found no Critical or Important issue (AIU-055 verification.md).
- **AIU-055's friction was mechanical (fact).** 31 isolation-guard blocks, 7 blocked sleeps,
  misrouted reviewer results, push races, about 50 commits and 11 leftover worktrees
  (A4c-F7, A5-F5, B5-F4).
- **AIU-045 (fact).** 32 subagents, 17 of them reviews.
  - Most per-task reviews were Minor-only, including reviews of docs-only tasks.
  - The only Important finding came from the whole-run review: the primary's own status
    error in the records.
  - The run cost $197.5 and hit two usage-limit stalls.

  Source: A4b-F7/F8.
- **AIU-046 (fact).** Per-task reviews found real defects: cell order and rounding, and
  status honesty. Two Important issues came from the whole-branch review (A4b-F7).
- **Host tooling is rediscovered in every session (fact).**
  - `python` resolves to the Store stub (33 hits).
  - There is no node and no pwsh 7.
  - The first build fails with `NETSDK1004` in 17 of 22 worktree folders.

  Sources: A4a-F8, A4c-F4/F11.

## 3. Research findings with verdicts

Sources were accessed 2026-10-08. Versions and links are in the B lane reports.

| Candidate | Lane | Benefit here | Main risk/cost | Verdict |
|---|---|---|---|---|
| Move repo skills to `.claude/skills` with concrete trigger descriptions | B1-F1, B2-F13 | Guardrail skills become invocable by name in sessions and subagents | Validator path + tests change; supersedes D-034's location clause | **Adopt** |
| Committed project `.claude/settings.json`, deny rules only | B1-F3 | Backstop for destructive git, release/dispatch/variable/secret actions and source-CLI credential reads, in every mode and worktree | Patterns bypassable via `sh -c`; a project deny cannot be lifted by a local allow; settings diffs are security-relevant | **Adopt** |
| Auto permission mode instead of bypass | B1-F2 | Classifier blocks destructive git and credential printing | False blocks observed 09-23..10-06: "Merge Without Review" on push to `main`, `git status`, "Instruction Poisoning" on plan steps, an 18-min outage (A4a-F7, A4c-F8); classifier may have changed since | **Lanes disagree; trial only after deny rules (OD-10)** |
| One tracked, task-neutral reviewer agent | B1-F4, B5-F6/F9 | Review evidence no longer depends on an untracked file | `opus` alias moves with model releases | **Adapt** |
| Built-in `/code-review` (local) and `/security-review` | B1-F8, B5-F7, B2-F11 | Correctness lens next to AC review; security lens for T3 | False positives; triage per calibration | **Adopt `/code-review`; `/security-review` inside T3 review** |
| `/goal` with a machine-checked done condition | B1-F9, B5-F8 | Fewer "continue" nudges and premature "done" claims | Judges the transcript only | **Trial** |
| `/doctor prompt-audit` (read-only report) | B1-F6, B4-F10 | Input to rule consolidation | None | **Adopt once** |
| Path-scoped `.claude/rules/logging.md` for logging detail | B1-F6 | Detail loads only when editing code | A glob miss must not drop the secret-exclusion invariant, which stays always loaded | **Trial** |
| Desktop "archive inactive sessions" + archive at merge | B1-F7 | Ends worktree sprawl | Must verify it keeps unmerged branches | **Adopt after verification** |
| superpowers plugin | B2-F2..F5 | Good discipline content | Injection, gates, unpinned updates; skills cannot be disabled one at a time | **Adapt** (phase 1 precedence line; phase 2 repo skills, then disable per project) |
| ux-superpowers, design-superpowers, desktop-commander, Anthropic design plugin | B2-F6..F9, B3-F9 | None here | Context cost, unpinned npm, CVEs, auth noise | **Retire** |
| pr-review-toolkit silent-failure and test-gap lenses | B2-F10 | Fits logging and test-gap weak spots | None as copied checklists | **Adapt idea** |
| Official code-review/feature-dev/commit-commands/hookify plugins, managed Code Review, ultrareview | B2-F10, B5-F7 | PR-centric or duplicate built-ins | Cost, cloud upload | **Reject** |
| security-guidance plugin | B2-F11 | Possible secret/path catches | Per-turn Opus review; its own README advises against it for shared worktrees | **Reject now** |
| `csharp-lsp` plugin + `csharp-ls` 0.28.0 | B3-F1, B1-F12, B2-F12 | Diagnostics after each edit without a build | Global dotnet tool; WinUI/XAML accuracy and solution discovery unverified | **Trial, time-boxed** |
| Serena, Roslyn MCP servers | B3-F2 | Little over Grep at about 37k LOC | Needs pwsh 7; unpinned uvx; single maintainers | **Reject** |
| Microsoft `winapp ui` CLI, read-only commands | B3-F5 | Live UI tree for smoke locators and variant screenshots | Public preview; telemetry opt-out | **Trial, narrow** |
| Windows-MCP, FlaUI MCP wrappers, GitHub MCP | B3-F4/F7 | FlaUI harness and `gh` already cover these | Full desktop control; PAT in env | **Reject** |
| Microsoft Learn MCP (already connected) | B3-F8 | Authoritative WinUI/MSIX/.NET answers | No payloads or identifiers in queries | **Adopt guidance line** |
| Graph/vector memory, code-graph MCPs | B4-F8/F9 | Grep suffices for 268 C# files and about 326k words of docs | Databases, keys, egress, injection surface, licences | **Reject** |
| "Amended by" back-pointers + validator check | B4-F4 | Agents stop applying superseded decisions | Small validator change | **Adopt** |
| Spec Kit, BMAD, OpenSpec, Tessl | B5-F1 | The AIU system already covers spec-driven work | Second source of truth; installers | **Reject; borrow two ideas** |
| Ceremony sized after investigation (BMAD); bugfix spec current/expected/unchanged (Kiro) | B5-F2 | Sizes records to risk; cheap regression guard | None | **Adapt idea** |
| Agent teams | B5-F10 | — | Experimental; auto-approves plans | **Reject** |
| Parallel subagents / dynamic workflows for read-only audits | B5-F10 | This audit pattern | Token cost | **Trial for audits only** |
| "Fails at base" reviewer check in a temporary worktree | B6-F3 | Direct defence against tests that check nothing | Minutes per bug fix | **Adopt** |
| Test-erosion sentinel script + xUnit1004 as error | B6-F2/F11 | Flags deleted tests, new Skip, fewer asserts, edited expectations | Must ask for justification, not block | **Adapt** |
| Smoke reliability helpers (polling re-find, ID-first locators, failure dump) | B6-F10 | Fewer blind fix loops | Gradual adoption | **Adapt** |
| Stryker.NET 5.0.0 | B6-F4 | Measures assertion strength on Core math and parsers | xUnit v3 runner maturity unverified | **Trial: half-day spike, no gate** |
| CsCheck property tests | B6-F5 | Budget-math invariants | New dependency | **Defer** (decide after the Stryker spike) |
| Verify snapshots, ArchUnitNET/NetArchTest, FsCheck, Appium/WinAppDriver, coverage gates, test-impact tools | B6 | Duplicate existing tests or add toolchains | — | **Reject** |
| Hooks (Stop gate, PostToolUse build), `claude -p`, GitHub Action, cloud routines, output styles, OS sandbox | B1-F10/F11 | No current need; slow suites; no sandbox on native Windows | Upkeep, secrets | **Reject now** |

## 4. Target workflow

Design principles:
- Repo rules are the only process authority, and each rule has one home.
- Ceremony and review scale with the **areas a change touches**, not with its size.
- Mechanical guards back the hard boundaries.
- The owner is asked only about scope, product intent, significant architecture or
  complexity, security, destructive or external authority, dependencies, and visible UI.

### 4.1 Flow

1. **Session start.**
   - The desktop app creates a worktree branch (`users/*`).
   - Always loaded: a re-composed AGENTS.md. It contains the architecture boundaries, the
     ask and never-ask lists, a Git flow summary with the Preview consequence, the check
     commands, the security boundaries including the logging secret-exclusion invariant,
     truthfulness, the UI-variants rule, the push-status reply, and a read-by-task map with
     explicit paths.
   - No plugin injection.
   - Memory holds host facts and preferences only; the facts are also in a repo environment
     section.
   - The agent restores once with a documented one-liner. It then checks whether the request
     is already on `main` or in another worktree.
2. **Intake.**
   - The agent inspects the code and states one plan line: the tier, the intended result
     and the acceptance checks.
   - Inside approved scope and authority it decides for itself and records each decision
     with its reason.
   - It asks once, in one batch, and only about the always-ask categories.
   - Visible UI changes get 2-3 **rendered** variants in one question. Regressions against
     approved UI skip the variants.
   - Prompts for primary sessions inherit CONTRIBUTING, D-190 and D-196 unless the owner
     narrows them in the current request. Subagents never push to `main` unless the run's
     owner grant says so.
3. **Records by tier.** The tier is the **highest tier triggered by any touched area**, and
   the owner may raise it.
   - **T0 fix:** the plan and evidence go in the commit message.
   - **T1 small feature or UI variant:**
     - A one-page spec: Problem, R-xx, AC-xx, Out of scope. Bugs may use current, expected
       and unchanged behaviour.
     - A verification record.
     - The owner's pick goes in the spec as one line (variant, owner, date). There is no
       D-entry unless it binds future work.
   - **T2 multi-part:**
     - As T1.
     - tasks.md only when parallel workers are used, holding task blocks plus an open
       "Next action".
     - The worker procedure lives in the tracked implementer agent.
   - **T3:**
     - **Trigger areas** (published as a path and area list): authentication and provider
       contracts; credentials and DPAPI stores; persisted records, preferences schema and
       migrations; destructive or owned-root cleanup; logging, diagnostics, crash data and
       exports; new network hosts; update, install, signing and Preview scripts; CI
       workflows; dependencies; agent permission config (`.claude/settings.json`, skills,
       agents).
     - **What T3 adds:** a design note when there is a real choice, the security-lifecycle
       skill, and blocking focused independent review.
4. **Implement.**
   - One primary session by default.
   - Red-to-green for logic and bug fixes. A new test must fail at the base commit.
   - Class-filtered tests in the inner loop; full suites once on the merged tree.
   - Parallel worktree workers run only on owner request. A run allows at most about 4
     workers. Their briefs require:
     - simple single-purpose commands;
     - Monitor or background waits instead of sleep;
     - unit suites and builds per worker;
     - desktop smokes once by the controller on the integrated tree;
     - pushes only as the run's grant says (default: the controller integrates locally and
       pushes once per wave).
5. **Review.** One pass, scaled by tier.
   - **T0:** the primary reviews the diff.
   - **T1 and T2:** one fresh-context review of the integrated diff against the ACs by the
     tracked reviewer, plus local `/code-review`. In parallel runs, each code, test or
     harness task also gets a per-task review (OD-15).
   - **T3:** additionally a focused independent review (convergence-review and
     `/security-review`), which blocks merge. Docs-only tasks that edit rule files or agent
     config are T3.
   - **Reviewer calibration:**
     - An Important finding violates an AC, the spec, a security or data boundary, or is a
       reproducible defect. It needs file:line plus a failing test or a repro.
     - Report at most five Minor findings.
     - One round, then a re-check of the fixed lines only.
     - Unresolved material findings block integration.
6. **Verify.**
   - The change-based matrix, with new rows for scripts, harness-only and test-only changes.
     "Product inputs" has one definition shared by the smoke gate and Preview publication.
   - The smoke runs against the Release unpackaged build.
   - Evidence names checks by matrix ID.
   - Each AC is marked PASS, FAIL, NOT_RUN or BLOCKED.
   - **"Verified"** means every required check PASS. Only D-190 post-deploy owner items may
     stay NOT_RUN.
   - A BLOCKED required smoke on a product-input change keeps the work on its branch.
   - Optionally, `/goal` encodes the checks as the stop condition for unattended runs.
7. **Merge.**
   - Save points are commits pushed to `origin/<task-branch>`. That push runs no CI and
     publishes no Preview.
   - At completion: fetch, merge fresh `main`, and run the numbering script.
   - Then run validator `--final` and the required checks on the merged tree.
   - Push to `main` and verify ancestry after `git fetch`. Reply "Pushed to `main` <hash>".
   - Then delete the remote task branch, archive the session and remove the worktree, all
     under the strict cleanup conditions.
8. **Deploy and post-deploy.**
   - CI validates every push to `main`.
   - A Preview publishes unless every path changed since the last Preview is on a
     non-product allowlist (`docs/**`, `.claude/**`, root `*.md`).
   - Each feature adds 3-5 lines to one "Pending owner checks" list.
   - After updating, the owner may invoke a `post-deploy-check` skill. It reads the
     installed version and sanitized logs and records verdicts only.
   - Checks stay NOT_RUN until then and never block merge (D-190).

### 4.2 What the flow uses

- **Agents:** one tracked reviewer. One tracked implementer only if parallel runs are kept.
- **Skills, in `.claude/skills`:**
  - security-lifecycle;
  - provider-evidence;
  - convergence-review, plus the silent-failure and test-gap lenses;
  - systematic-debugging, verification-before-completion and receiving-code-review, vendored
    and pinned to an upstream commit;
  - new, if the owner approves: planning-and-variants (keeps a `/brainstorming`-style entry
    point and rendered mockups), plan-execution, post-deploy-check.
- **Built-ins:** `/code-review`, `/security-review`, Monitor and background tasks;
  `/doctor prompt-audit` once; `/goal` as a trial.
- **Tools:** Microsoft Learn MCP (with a guidance line), the `gh` CLI, the FlaUI harness, the
  built-in browser, claude_design, visualize. Trials: csharp-lsp, `winapp ui`.
- **Memory:** host facts and owner preferences only. Rules live in the repo.
- **Settings:** a committed, deny-only project `.claude/settings.json`.

### 4.3 What it removes

- the superpowers injection and gates, once replacement skills exist;
- four unused plugins;
- per-task record commits;
- executed plans kept as long documents;
- backlog history prose;
- D-entries for UI picks;
- memory copies of rules;
- OMP, Codex and Copilot adapters;
- stale workflow docs at the tip (Git history keeps them);
- docs-only Previews;
- review of routine docs tasks;
- foreground CI watching and sleep polling;
- if the owner agrees, the routing-spike CI build and the maintenance of the opt-in Sandbox
  UI audit suite.

## 5. Recommendations, ranked by impact on quality, then speed

**Authority:**
- **Agent may do:** Stage 2 may implement it once the owner approves the bundle in OD-1.
- **Owner decides:** it needs its own decision in section 8.

Effort: S < 2 h, M ≤ 1 day, L > 1 day. Everything is Git-reversible unless noted.

### R1 — One true Git and integration flow; Preview gate becomes a merge gate
- **Problem and evidence:**
  - The policy text asks for pushes of incomplete work to `main`, and every green push to
    `main` publishes to the owner's app.
  - Practice is worktree branches, and three texts disagree.
  - Sources: A1-F1/F2, A3-F7, A5-F10, B5-F12.
  - The risk is latent (inference): there have been 0 WIP pushes since 2026-10-05.
- **Change:**
  - Save points are commits on the worktree branch, pushed to `origin/<task-branch>` after
    each meaningful step and before going idle. That push runs no CI and publishes no
    Preview, and it keeps the owner's "no work lost" intent.
  - Only verified merges reach `main`, under step 6's definition of "verified".
  - Remote task branches are deleted after merge.
  - Merge D-178/D-179 into one current decision and move the history into decisions.
  - A BLOCKED required smoke on a product-input change keeps the work on the branch until
    the smoke passes or the owner says yes for that change.
- **Benefit:** removes the unverified-WIP path to the owner's app and the policy
  contradiction every session reads.
- **Quality risk:** remote task branches multiply. Deletion after merge and the cleanup
  conditions in R12 handle that.
- **Effort:** S, reversible.
- **Authority:** owner decides (OD-2: push grant to task branches; OD-3).
- **Order:** first. R9, R12 and R13 depend on it.

### R2 — Make repo guardrails discoverable and versioned
- **Problem and evidence:**
  - The three skills are invisible to Claude Code.
  - The reviewer agent is untracked and AIU-045-specific.
  - Sources: A1-F5, A3-F3/F4, B1-F1/F4, B2-F13, B5-F9.
- **Change:**
  - Move the three skills to `.claude/skills/` with concrete trigger descriptions.
  - Retarget the validator and formats.md.
  - Record a decision superseding D-034's location clause.
  - Track one generalized reviewer agent:
    - read-only tools;
    - explicit permission for a temporary worktree outside the checkout for the
      "fails at base" check;
    - the `opus` alias;
    - `skills: [convergence-review]`;
    - the calibration from step 5.
  - Track an implementer only if parallel runs are kept.
  - Name the reviewer and its general-purpose fallback in CONTRIBUTING.
- **Benefit:** security, provider-evidence and review rules load by name everywhere, and
  review evidence survives a fresh clone.
- **Quality risk:** none known. Note that the alias changes the reviewer model silently.
- **Effort:** S-M, reversible.
- **Authority:** owner decides (OD-11, OD-12).

### R3 — Mechanical deny rules for the hard boundaries
- **Problem and evidence:**
  - There are no deny rules, and the hard rules are prose only.
  - Sessions have run in bypass mode since 2026-10-06 (A3-F5, B1-F2/F3, A4c-F8).
  - Deny rules apply in every mode (fact, docs).
  - The fix is preventive: no force-push or credential read was observed. One stray remote
    branch push was observed (A4c-F2).
- **Change:** commit a deny-only project `.claude/settings.json` covering, in Bash and
  PowerShell forms:
  - force pushes, `+refspec`, `--delete`/`:branch` on `main`, `--mirror`;
  - `git reset --hard`, `git clean -fdx`, `git branch -D`, `git worktree remove --force`;
  - `git stash pop/drop/clear`;
  - `gh release *`, `gh workflow run *`, `gh variable *`, `gh secret *`, and mutating
    `gh api -X`;
  - `Read`/`Edit` on the source-CLI credential stores named in docs/providers.

  Alongside the file:
  - Verify each rule with a harmless test.
  - State in AGENTS.md that prose rules stay authoritative and that a missing deny rule is
    not permission.
  - Changes to this file are T3 and owner-decided.
  - Document how the owner temporarily authorizes a provider-evidence read, because a local
    allow cannot lift a project deny.
- **Benefit:** a backstop against injection-driven or mistaken destructive actions, with no
  new prompts.
- **Quality risk:**
  - False assurance: patterns are bypassable with `sh -c`. The AGENTS.md line addresses
    this.
  - A rule could block an authorized action. The documented override handles that.
- **Effort:** S, reversible.
- **Authority:** owner decides (OD-9). It needs security-lifecycle review.

### R4 — Area-based risk tiers and proportional review
- **Problem and evidence:**
  - Ceremony is chosen by habit.
  - Review triggers are worded three ways.
  - AIU-045 ran 17 reviews, mostly Minor-only, including reviews of docs tasks.
  - AIU-046 per-task reviews found real defects. AIU-055's one Important finding came from
    a per-task review.
  - Sources: A1-F12, A4b-F7, A5-F5, B5-F2/F5.
- **Change:**
  - Add the T0-T3 table, with T3 decided by the touched-area list (step 3), to CONTRIBUTING.
    The reviewer checks the diff against the list.
  - Add the review cadence and reviewer calibration (step 5).
  - Use local `/code-review` for T1 and T2, and `/security-review` inside T3 review.
  - Bug fixes need a test that fails at the base commit.
  - Put red-to-green in CONTRIBUTING, so it does not depend on a plugin or an agent file.
- **Benefit:** focused review becomes mandatory wherever credentials, persistence, logging
  privacy or deploy paths are touched, however small the change. Today small changes in
  those areas can get only a primary review. Docs and small UI work get less ceremony.
- **Quality risk:** less review on routine docs tasks. Rule and agent-config edits stay T3.
- **Effort:** S, reversible.
- **Authority:** owner decides (OD-14, OD-15).

### R5 — Make the smoke trustworthy and run it once
- **Problem and evidence:**
  - Failures are mostly environmental.
  - The installed app interferes with dev smokes.
  - Debug and Release builds are mixed.
  - Fixes are made in blind loops.
  - Sources: A6-F3/F5, A4b-F6, A4c-F5, A5-F6, B6-F10.
- **Change:**
  - **Tray identity (OD-23):** a test-only, per-run tray identity. It substitutes only the
    identity value inside the existing `DesktopTestEnvironment` switch and never touches
    data or credential roots. The reviewer confirms the production path is unchanged.
  - **One build:** run the smoke against the Release unpackaged build.
  - **Locators:** a polling re-find helper, ID-first locators, and an automatic screenshot
    plus UI-tree dump on failure, adopted as files are touched.
  - **Failing reruns:** after two failed reruns, record FAIL or BLOCKED with a diagnosis and
    stop; never skip the smoke.
  - **Desktop time:** run desktop smokes once per integration, with one desktop-use window
    per session.
  - **History:** keep per-test pass/fail history in one ignored CSV.
- **Benefit:**
  - Fewer false reds and owner interruptions.
  - The smoke stops touching the owner's real app.
  - It keeps guarding about 3.5k lines of lifetime code.
- **Quality risk:** a hook that changes more than the identity would stop the smoke from
  exercising the real registration path. It is bounded as described above.
- **Effort:** M, reversible.
- **Authority:** owner decides the app hook and the Release target (OD-23). The harness
  changes are agent may do (OD-1).

### R6 — Repo-owned process instead of plugin process
- **Problem and evidence:**
  - The injection and gates come with unpinned updates, and specs carry override sections.
  - Four plugins are unused.
  - Sources: A3-F1/F2, A4c-F1/F10, B2-F1..F9.
  - The owner deliberately uses `/superpowers:brainstorming`.
  - The approval-turn problem already dropped after 2026-10-07 (A4c-F1).
- **Change:**
  - **Phase 1:** one AGENTS.md precedence line. Plugin skills are optional tools. Repo spec
    locations, review cadence, Git policy and the ask lists win. The spec-review,
    execution-mode and finish-branch prompts are pre-answered. No `docs/superpowers` or
    `.superpowers` artifacts outlive the session.
  - **Phase 2, if the owner wants it:**
    - Map every superpowers skill the owner used to a replacement, or to "dropped" with a
      reason.
    - Vendor systematic-debugging, verification-before-completion and receiving-code-review,
      pinned to an upstream commit and reviewed line by line.
    - Write planning-and-variants (a `/brainstorming` equivalent with rendered mockups) and
      plan-execution.
    - Then set `"superpowers@synced": false` in the project settings. Historical tasks.md
      files stay as they are.
  - **Separately:** the owner retires the four unused plugins and any unneeded personal
    connectors for coding sessions.
- **Benefit:**
  - Process stays deterministic.
  - About 6k fewer tokens per session and per subagent.
  - No silent changes from upstream releases.
- **Quality risk:** losing discipline if phase 2 skips something. The mapping and vendoring
  happen before the plugin is disabled.
- **Effort:** phase 1 S; phase 2 M. Reversible with one settings line.
- **Authority:** owner decides (OD-6, OD-7, OD-8).

### R7 — Codify ask/never-ask in the repo
- **Problem and evidence:**
  - Avoidable questions were common up to 2026-10-06 (A4a-F3, A4b-F2).
  - The 2026-10-07 and 2026-10-08 fixes work but live partly in memory (A4c-F1/F2, B4-F2).
  - Prompts narrowed standing policy (A4b-F1).
  - Unattended runs stalled on questions (A4b-F3).
- **Change:** add two lists to AGENTS.md.
  - **Always ask:**
    - new scope or product intent;
    - significant architecture or a material complexity increase;
    - a security boundary;
    - destructive or external authority, including live-provider, credential-reading,
      Sandbox/VM and host-install checks;
    - dependencies;
    - visible UI variants.
  - **Never ask:**
    - continuing an approved plan;
    - saving files;
    - recording decisions the owner already gave;
    - running local checks or reviews;
    - numbering;
    - merging and pushing a change that is verified (step 6);
    - choosing the minimal fix for a reported bug;
    - confirming a variant the owner already picked.

  Also add these rules:
  - Inside authority, the agent's recommendation is the default, and it records the reason.
  - Prompts for primary sessions inherit standing policy. Subagents keep the run's grant.
  - Unattended runs skip always-ask work and list those questions at the checkpoint.
    Observable state is checked, not asked.
  - Results the owner reports are recorded as "owner-reported PASS (date)".
- **Benefit:** the rules hold for fresh machines, subagents and kickoff prompts, not only
  for sessions that load the memory index.
- **Quality risk:** an agent decides something the owner wanted to weigh. The explicit
  always-ask list and recorded reasons mitigate this.
- **Effort:** S, reversible.
- **Authority:** owner decides (OD-4, OD-5).

### R8 — Stop publishing Previews and commits for bookkeeping
- **Problem and evidence:**
  - 159 Preview tags in 16 days.
  - 96 of 197 pushes were docs-only.
  - A Preview published mid-run broke a dev smoke.
  - 19 `record T-xx` commits.
  - Sources: A5-F1/F5, A2-F4, A4c-F5.
- **Change:**
  - Publish a Preview **unless** every path changed since the last published Preview is on
    an explicit non-product allowlist (`docs/**`, `.claude/**`, root `*.md`).
  - Log the decision in the job summary.
  - Use the same "product inputs" definition for the smoke gate.
  - Record worker evidence once per wave or at feature end.
- **Benefit:**
  - Fewer meaningless updates.
  - Shorter CI queues.
  - Cleaner history.
  - Dependency bumps get smoked; today they do not.
- **Quality risk:** a wrong allowlist entry. The allowlist is inverted, so an unknown path
  publishes.
- **Effort:** S, reversible.
- **Authority:** owner decides (OD-17, OD-18).

### R9 — Consolidate rules: one home each, current wording only
- **Problem and evidence:** duplication, accretion, memory-only rules and stale goals
  (A1-F3/F6/F7/F10/F14/F16, B1-F5, B4-F2/F11).
- **Change:** after OD-2..OD-7, apply A1's target structure:
  - re-composed AGENTS.md of about 600-700 words, keeping the logging secret-exclusion
    invariant always loaded;
  - CONTRIBUTING without history;
  - verification.md reduced to the matrix, gate and evidence;
  - coding lessons moved to the architecture docs;
  - back-filled decision records for undocumented process directions;
  - host facts copied into a repo environment section;
  - a current goals.md paragraph.

  Before and during:
  - Run `/doctor prompt-audit` first.
  - Produce a rule-inventory diff showing every rule kept, moved or merged, for the owner to
    read.
- **Benefit:**
  - About 35 % smaller per-session core (estimate).
  - No drift between copies.
  - Host facts become available to subagents (to be verified with the tracked agents).
- **Quality risk:** a rule could be lost. The inventory diff addresses this.
- **Effort:** M, reversible.
- **Authority:** agent may do (OD-1), with the owner reading the inventory diff. Memory
  pruning is OD-21.

### R10 — Lean, honest work records
- **Problem and evidence:**
  - Backlog history, and executed plans kept in the repo.
  - Verbose verification and D-entry inflation.
  - Stale specs and scattered NOT_RUN records.
  - Sources: A2-F1..F13, A5-F9, B4-F4, B5-F3/F11.
- **Change:**
  - **Backlog:** live items in full, a one-line done index, and a "Pending owner checks"
    list.
  - **Executed plans:** they collapse into a ledger in verification.md. Each line keeps the
    task, commits, review verdict, check IDs and a grant reference, and there is an open
    "Next action" while work continues.
  - **Verification:** names checks by matrix ID.
  - **Skeletons:** formats.md gets 15-20-line skeletons that keep the verification.md:39
    fields (environment, timestamp, code ref).
  - **D-entries:** only for rules that bind future work. UI picks become one line in the
    spec.
  - **Amended decisions:** "Amended by" back-pointers.
  - **Validator:** checks D-ID uniqueness, AC coverage and the amended-by link.
- **Benefit (estimates):** the backlog shrinks by about 65 % and parallel-feature records by
  40-55 %, with fewer conflicts.
- **Quality risk:** lost traceability. Provenance, verdicts, NOT_RUN reasons, IDs and Git
  history are all kept.
- **Effort:** M, reversible.
- **Authority:** owner decides (OD-19).

### R11 — Close the post-deploy loop
- **Problem and evidence:** NOT_RUN owner checks are scattered with no queue (A2-F9,
  B5-F11).
- **Change:**
  - One "Pending owner checks" list.
  - An owner-invoked `post-deploy-check` skill with `disable-model-invocation`. It reads the
    installed version and sanitized logs.
  - It records only verdicts and sanitized event names, never raw log lines, paths or
    identities, and never reads credential stores.
- **Benefit:** live behaviour gets confirmed, and owner control improves.
- **Quality risk:** a privacy leak through logs. The verdict-only rule prevents this.
- **Effort:** S-M.
- **Authority:** owner decides (OD-20).

### R12 — Worktree hygiene
- **Problem and evidence:** 31 merged worktrees and 8.1 GB (A3-F7, A4c-F13, A5-F14, B1-F7).
- **Change:** a worktree is removed only if:
  - `git merge-base --is-ancestor <branch> origin/main` holds after a fresh fetch;
  - its status is clean, including ignored local evidence;
  - no stash entry refers to it.

  It is never removed with `--force` or `branch -D`. Removal happens at the last merge step,
  plus a one-time cleanup. Before setting the desktop's 7-day inactive archive, verify that
  it keeps branches with unmerged commits.
- **Benefit:** disk space, clarity, and no work in stale worktrees.
- **Quality risk:** deleting uncommitted or unmerged work. The conditions above guard it.
- **Effort:** S. The one-time removal cannot be undone for untracked files.
- **Authority:** owner decides (OD-22).

### R13 — Placeholder numbering as a tool
- **Problem and evidence:** numbering commits, register conflicts and one corruption of a
  rule's text (A4c-F3, A5-F8, A2-F7).
- **Change:**
  - A small script that assigns the next free numbers on fresh `main`.
  - It replaces placeholders outside code spans, renames the spec folder and runs `--final`.
  - Placeholders stay out of commit subjects and source comments.
- **Benefit:** removes a manual step and its error class.
- **Quality risk:** a script bug. Validator `--final` catches it.
- **Effort:** S.
- **Authority:** agent may do (OD-1).

### R14 — Faster, steadier local loop
- **Problem and evidence:**
  - Two serial Infrastructure test classes.
  - A diagnostics flake.
  - Gaps in the matrix.
  - Restore failures in fresh worktrees.
  - Host tools rediscovered in every session.
  - Sources: A6-F1/F2/F6/F7/F11, A4c-F4/F11, A4a-F8, A5-F4.
- **Change:**
  - **Suite speed:**
    - Shard the audit corpus and cheapen the 256-file cap test.
    - The case count and assertions must be identical before and after, shown by the
      sentinel output. The changes get independent review because they touch suites in the
      KEEP list.
    - A6 counts the corpus as both 235 and 257 cases; reconcile that first.
  - **Matrix and docs:**
    - Add matrix rows, including "Windows edits build both unit test projects".
    - Document `Test-PreviewRelease.ps1`.
    - Use `-class` filters with `--no-build` in the inner loop.
    - Add a worktree bootstrap one-liner.
    - Add the host-tool facts (`py -3.13 -I`, no node, no pwsh 7).
  - **Erosion guard:** the test-erosion sentinel and xUnit1004 as an error.
  - **DiagnosticCrashTests:** the 10 s bound stays, per host memory. Whether to start it from
    an explicit probe-ready signal is OD-24.
- **Benefit:** the Infrastructure suite drops from about 25 s to 8-10 s, with fewer false
  reds and fewer wasted calls.
- **Quality risk:** weakening a test while speeding it up. The count/assertion parity check
  and review guard against that.
- **Effort:** M.
- **Authority:** agent may do (OD-1), except OD-24.

### R15 — Unattended and parallel run discipline
- **Problem and evidence:** blocked sleeps, isolation-guard blocks, misrouted reviewer
  results, foreground CI watching, Sandbox waits with no liveness check, and usage-limit
  stalls (A4a-F11, A4b-F5/F8, A4c-F6/F7).
- **Change:**
  - **Worker-brief rules, in the tracked implementer agent:**
    - simple commands and plain git;
    - Monitor instead of sleep;
    - reviewers dispatched by the controller;
    - reports returned as messages;
    - no push unless the run's grant allows it.
  - **CI:** push, continue, and check CI once at the end.
  - **Sandbox:** a started marker plus a timeout.
  - **`/goal`:** trial it on the next multi-part feature.
- **Benefit:** fewer wasted calls and stalls.
- **Quality risk:** none known.
- **Effort:** S.
- **Authority:** agent may do (OD-1). The parallel trigger is OD-16.

### R16 — Retire dead artifacts and record "Claude Code only"
- **Problem and evidence:** agent-neutral wording and dead adapters mislead (A1-F4/F9,
  A3-F8, A2-F13, A6-F13).
- **Change:**
  - Record a decision that development uses Claude Code.
  - Amend the constitution's "Execution" section and D-012/D-034/D-036.
  - Delete `.codex/`, `.omp/` and the `.omp` ignore lines. Delete
    `.github/copilot-instructions.md` too, if it is unused.
  - Move finished, retired or paused workflow records and one-shot spec artifacts to
    `docs/archive/`, with corrected status headers.
  - Ask about redacting personal identifiers in environment.md and pending.md.
- **Benefit:** less search noise and contradictory guidance.
- **Quality risk:** none; Git history keeps everything.
- **Effort:** S.
- **Authority:** owner decides (OD-13).

### R17 — Targeted tooling trials
- **Change:** time-boxed trials, each with a kill criterion and a one-paragraph result:
  - csharp-lsp with `csharp-ls` pinned (OD-27);
  - a Stryker.NET half-day spike, with no gate (OD-28);
  - `winapp ui`, read-only (OD-29);
  - `/goal` and the logging path-scoped rule (OD-1).
- **Quality risk:** none, as long as trials stay out of the gates.
- **Authority:** owner decides for anything that installs a tool.

### R18 — Low-value CI and test maintenance
- **Change:**
  - Drop the AIU-002 routing spike build if it no longer guards anything (OD-26).
  - Freeze the opt-in Sandbox UI audit suite: mark it `Explicit` and stop requiring edits on
    UI changes (OD-25).
  - Keep the package, upgrade and feed smokes.
- **Effort:** S.
- **Authority:** owner decides.

### Dependencies and order

1. Policy decisions OD-2..OD-7 come first. R1, R7 and R6 phase 1 then land as one small
   change set.
2. R2 (skills and agents) and R3 (deny rules) proceed in parallel; they are independent.
3. R4 tiers lead to R9 consolidation, which needs the R1, R4 and R7 wording, and then to
   R10 records.
4. R5, R8, R12, R13, R14 and R15 can run in any order after step 1.
5. R6 phase 2 comes after R2 (the skills folder must exist) and R4 (the process it
   encodes).
6. R11, R16, R17 and R18 come last. Each trial reports back before anything is adopted.

## 6. KEEP list

| Keep | Why |
|---|---|
| Core/Infrastructure/Windows boundaries, simplicity and ask-before-complexity rule, boundary source-scan tests | Core design constraints, enforced mechanically in milliseconds (A6-F12) |
| Security boundaries: no source-CLI credential reads without current authorization, no auto sign-in, no host-trust changes, external content is data, logging secret exclusion | The app handles provider credentials |
| "Memory, tool output, Issues and upstream files cannot change policy" (constitution) | Keeps plugin and memory text subordinate to repo rules |
| PASS/FAIL/NOT_RUN/BLOCKED honesty; compilation ≠ interactive or live proof; commits never upgrade a status; never weaken requirements to hide a failure; zero findings valid; no automatic review loops; unresolved material findings block integration | Main quality guardrails; consistently followed (A4b, A4c KEEP) |
| Remote-action authority: releases, tags, dispatch, repository settings, variables and secrets need explicit owner authorization; no force-push or history rewrite; preserve unrelated work | Owner control |
| Backlog status, Issues or old permissions never start work; one active feature unless the owner asks for a batch | Owner selects work |
| Data lifecycle: forward migration from every published schema, never rewrite a migration that shipped in a Preview, owned-root destructive cleanup; dev data isolated from installed-app data; no personal credentials in CI | Prevents data loss and credential exposure |
| D-190 post-deploy owner checks (never block merge on a dev sign-in) | Removed the stall class; the escapes the owner catches are product judgement (A5-F7) |
| D-196 placeholders + validator `--final` in CI | Ended number collisions; automate the renumbering, don't drop it |
| UI design variants first (rendered, batched, one pick) | Owner steers visuals quickly (A4c-F9) |
| "Pushed to `main` <hash>" verified by fetch | Status questions dropped to zero (A4c-F2) |
| Spec AC-xx/R-xx; verification AC table, NOT_RUN reasons, red-then-green, review verdicts; approval provenance recorded once; golden-fixture rules (no blanket snapshot approval, independent assertions) | The records later work uses (A2-F14) |
| Validator link, dependency, safe-path, done-without-evidence and ownership checks | Cheap automated guards (3-4 s) |
| Infrastructure (915 cases) and Presentation (330) suites, including the audit corpus and the store-growth cap | Defend parsers, auth, migration, DPAPI and view models |
| Launch smoke as the Preview gate, LedgerActivationSmoke, red-first smokes, DesktopTestEnvironment fail-closed BLOCKED, the desktop smoke lock, ID/path-scoped process control | Only guard for about 3.5k lifetime/tray lines; prevents false PASS |
| Warnings-as-errors builds; CI validate + unsigned package build; post-push CI before Preview signing; `AIU_PREVIEW_ENABLED` kill switch; no `[skip ci]` on save points | Clean-machine gate and deploy safety |
| Automatic Preview on product changes | The owner's deploy channel; filter it, don't remove it |
| Sandbox package/upgrade/feed smokes for packaging, migration and update changes | Only evidence for install and update |
| Review structure: a whole-feature or whole-branch review before merge (in AIU-046 it found 2 Important issues; in AIU-045 it caught the primary's own record error); per-task review for code tasks in parallel runs (AIU-046 defects, AIU-055 T-10) | Each level caught something the other did not |
| Isolated worktree per task; one primary agent by default; push authority granted per parallel run by the owner | Isolation without a standing wider authority |
| Conventional commits with AIU ids and check results in the bodies | Searchable, reviewable history (A5-F15) |
| Host-tooling facts (Defender rerun rule and "never relax the bound", IPv6 diagnosis, locked desktop) | Costly to rediscover; move them into the repo, keep the content |
| Content of the three repo skills; the aiu-reviewer contract | Encode the credential, provider-evidence and review boundaries |
| Owner-initiated brainstorming with recommendation-plus-pick option lists | Fast, effective owner decisions |
| Git history as the archive | Remove from the tip only what nothing live references |

## 7. Critic findings and resolutions

Two fresh read-only critics reviewed the draft: C1 (quality, security, owner control) and
C2 (evidence and claims). Every finding was accepted, wholly or in part.

| ID | Target | Severity | Finding (short) | Resolution |
|---|---|---|---|---|
| C1-1 | R1 | important | Local-only save points lose the "no work lost" intent | Save points are pushed to `origin/<task-branch>`, which runs no CI and publishes no Preview; this needs an explicit owner grant (OD-2) |
| C1-2 | R1, OD-3 | important | A BLOCKED smoke that merges ships unsmoked lifetime code through auto-Preview | Default: work stays on its branch; merging needs a per-change owner yes. R5's rerun limit records FAIL/BLOCKED and never skips the smoke |
| C1-3 | R7 | important | Ask list misses architecture and complexity; never-ask is too broad ("run checks", "verified") | Lists amended; live-provider, credential, Sandbox/VM and host-install checks are always-ask; "verified" defined in step 6; unattended runs skip always-ask work |
| C1-4 | R7, R8, R15 | important | Inherited policy could hand workers push authority; worker push conflicts with once-per-wave push | Inheritance applies to primary sessions only; subagents keep the run's grant; default is a controller push once per wave |
| C1-5 | R4 | **blocking** | T3 triggers narrower than verification.md:24; tier chosen by the implementer by size | T3 is set by touched areas from a published list, and the highest trigger wins; the reviewer checks the diff against the list; rule and agent-config edits are T3 |
| C1-6 | R8 | important | A `src`-keyed filter misses product inputs (packages, build props, scripts, workflows) | Filter inverted: publish unless every changed path is on a non-product allowlist; one "product inputs" definition shared with the smoke gate |
| C1-7 | R3 | important | Deny patterns incomplete; local allow can't lift a project deny; false assurance | Patterns extended; AGENTS.md line "prose authoritative; missing deny ≠ permission"; settings edits are T3; override procedure documented |
| C1-8 | R6 | important | Disabling the plugin loses TDD discipline; vendored text becomes policy | Red-to-green and "fails at base" go into CONTRIBUTING; a skill-to-replacement map is required before disabling; vendored skills pinned and reviewed |
| C1-9 | R14 | important | ≥30 s probe bound contradicts "never relax the bound" | Bound kept; the readiness-signal idea is a separate owner decision (OD-24); corpus and cap-test changes require parity evidence and review |
| C1-10 | R9, R17 | important | Path-scoped logging rule could drop the secret-exclusion rule | The invariant stays always loaded; only the detail moves |
| C1-11 | R5 | minor/important | Tray hook could bypass the real registration path | Hook limited to the identity value inside the existing test switch; the reviewer confirms the production path is unchanged |
| C1-12 | R12 | important | "Check for uncommitted files" is not enough | Ancestry after a fresh fetch, clean status including ignored evidence, no stash refs, no `--force`/`-D`; auto-archive behaviour verified first |
| C1-13 | R10 | important | Ledger and D-entry slimming could drop provenance, verdicts and the next action | Ledger lines keep commits, verdict, check IDs and the grant; UI picks become one spec line; open "Next action" kept; skeletons keep the verification fields |
| C1-14 | R11 | minor | Logs could carry private data into Git | Verdicts and sanitized event names only |
| C1-15 | R2 | minor | A read-only reviewer can't run the "fails at base" check | Temporary worktree outside the checkout explicitly allowed; alias drift noted |
| C1-16 | KEEP | important | Missing guardrails | Added: material findings block, never weaken requirements, data lifecycle, dev-data isolation, no personal credentials in CI, golden-fixture rules, memory cannot change policy, backlog doesn't start work, ask-before-complexity, variables and secrets |
| C1-17 | KEEP | minor | "Archive, never delete" conflicts with R16; tool inventory isn't a guardrail | Reworded to "Git history is the archive"; tools moved to 4.2 |
| C2-1 | KEEP | **blocking** | Review-value justification misattributed (AIU-055's whole-feature review found nothing Important) | Rewritten per run with what each level actually found |
| C2-2 | R4 | important | Per-task review disagreement (A4b/B5 vs A4c/A5) not stated | Stated here and made an owner decision (OD-15) |
| C2-3 | §3 | important | Auto mode "not recommended" silently overrode B1 | Shown as a disagreement with dated evidence; trial only after deny rules (OD-10) |
| C2-4 | R4/R5 → R6/R7 | important | Benefit measured against the pre-2026-10-07 baseline | Re-ranked: ask-list (R7) and plugin (R6) moved down; their benefit restated as making memory-held fixes into repo policy |
| C2-5 | §1, R6 | important | Inference labelled fact; A4b-F10 cited one-sidedly | Relabelled; the mitigating facts and the "pre-answer only" alternative stated (OD-6 vs OD-7) |
| C2-6 | §2.1, R9 | important | Memory index does reach subagents (observed) | Corrected; R9's subagent benefit marked "to be verified" |
| C2-7 | §2.1 | minor | "45 merges into worktree branches" wrong | Corrected to 30 + 15 |
| C2-8 | §1 | minor | Records-only vs docs-only merged | Corrected to 36-38 % (45 % docs-only) |
| C2-9 | §1 | minor | CI baseline and cause | Corrected to about 15 % → 2.7 % (2/74); cause labelled inference |
| C2-10 | R3 | minor | No incident; preventive | Stated; stray remote push and `sh -c` caveat cited |
| C2-11 | §1 | minor | WIP risk labelled fact | Policy text fact; risk latent (inference) |
| C2-12 | R1 | minor | "Most re-asked question" uncounted | Dropped |
| C2-13 | various | important | Omissions: auto mode until 10-06, tasks.md sub-skill dependence, escapes mostly product judgement, corpus count 235/257 | All added (§1, §2.2, §2.5, R6, R14) |
| C2 claims | §1-2 | minor | Preview tags 159 (job ran 161), 16 days; 25 placeholder subjects; override sections only in five tasks.md; worktrees 31 merged + audit; 8.1 GB includes audit worktree | All corrected |

Disagreements between lanes, resolved by the architect:
- **Per-task review.** B5-F5 wants it only for T3; A4b-F7 wants it for code tasks; A4c and
  A5 keep it. Evidence shows value at both levels in different runs, so the recommendation
  is per-task review for code, test and harness tasks in parallel runs, plus one whole-feature
  review. The owner decides (OD-15).
- **Auto mode vs bypass.** B1 says adopt auto mode; A4a and A4c recorded false blocks before
  2026-10-06. Resolution: deny rules first, then an optional one-week trial (OD-10).
- **superpowers.** B2 wants to disable it after building replacements; A3 and A4c propose
  pre-answering the gates. Resolution: phase 1 pre-answers now (OD-6); phase 2 is a separate
  yes/no (OD-7).
- **Save points.** CONTRIBUTING wants frequent pushes to `main`; A1 and B5 want WIP off
  `main`. Resolution: push to the task branch (OD-2).

## 8. Owner decisions (yes/no)

Each item states the recommendation. A "yes" authorizes Stage 2 to implement exactly that
item. Items marked "agent may do" in section 5 are bundled in OD-1.

1. **OD-1 — Agent-may-do bundle.** Approve Stage 2 to do the following without further
   questions:
   - R9 rule consolidation, delivered with a rule-inventory diff for you to read before it
     merges;
   - R13 numbering script;
   - R14, except OD-24;
   - R15;
   - R5 harness-only changes;
   - one `/doctor prompt-audit` run;
   - the Microsoft Learn MCP guidance line;
   - host facts copied into a repo environment section;
   - the `/goal` and path-scoped logging-rule trials, with the secret-exclusion invariant
     staying always loaded.

   *Recommended: yes.*
2. **OD-2 — Git flow.** Replace "work directly on `main`" with this flow:
   - one worktree branch per task;
   - save-point commits pushed to `origin/<task-branch>`, which runs no CI and publishes no
     Preview;
   - only verified merges reach `main`;
   - remote task branches are deleted after merge.

   *Recommended: yes.*
3. **OD-3 — BLOCKED smoke.** A product-input change whose required smoke is BLOCKED stays on
   its branch until the smoke passes or you approve that specific change. *Recommended: yes.*
4. **OD-4 — Ask lists.** Add the always-ask and never-ask lists from R7 to AGENTS.md, with
   "recommendation = default inside authority, reason recorded" and owner-reported results
   recorded without asking. *Recommended: yes.*
5. **OD-5 — Prompts and unattended runs.**
   - Generated prompts for primary sessions inherit standing policy unless you narrow it in
     the current request.
   - Subagents never push to `main` without the run's grant.
   - Unattended runs skip always-ask work and list those questions at the checkpoint.

   *Recommended: yes.*
6. **OD-6 — superpowers phase 1.** Add the AGENTS.md precedence line that pre-answers the
   spec-review, execution-mode and finish-branch prompts. *Recommended: yes.*
7. **OD-7 — superpowers phase 2.** Replace superpowers with repo skills, including a
   `/brainstorming`-equivalent for rendered variants and vendored debugging, verification and
   review-reception skills. Then disable superpowers for this repo only. *Recommended: yes,
   after OD-11; "no" keeps phase 1 alone.*
8. **OD-8 — Unused plugins and connectors.** You retire ux-superpowers, design-superpowers,
   desktop-commander and the Anthropic design plugin, and disable personal connectors you
   don't need in coding sessions. This is your account action. *Recommended: yes.*
9. **OD-9 — Deny rules.** Commit the deny-only project `.claude/settings.json` from R3, with
   its AGENTS.md line, override procedure and T3 status for future edits. *Recommended: yes.*
10. **OD-10 — Auto-mode trial.** After OD-9 lands, run a one-week auto-mode trial, reverting
    if a routine push, merge or read is blocked. *Recommended: optional. Yes only if you want
    the classifier as an extra layer.*
11. **OD-11 — Skills location.** Supersede D-034's location clause and move the three skills
    to `.claude/skills/`, with sharper triggers and the validator retargeted. *Recommended:
    yes.*
12. **OD-12 — Tracked agents.** Version one generalized reviewer agent in Git. Keep an
    implementer agent only if parallel runs stay. *Recommended: yes.*
13. **OD-13 — Claude Code only.**
    - Record it as a decision.
    - Amend the constitution, D-012, D-034 and D-036.
    - Delete `.codex/`, `.omp/` and `.github/copilot-instructions.md`.
    - Archive the finished workflow records.
    - Redact the personal identifiers in environment.md and pending.md.

    *Recommended: yes.* Say whether you still use the Copilot coding agent.
14. **OD-14 — Risk tiers.** Adopt T0-T3 in CONTRIBUTING:
    - T3 is set by the published touched-area list, and the highest trigger wins;
    - the reviewer checks the diff against the list;
    - local `/code-review` for T1 and T2;
    - `/security-review` inside T3 review;
    - a "fails at base" check for bug fixes;
    - red-to-green written into CONTRIBUTING.

    *Recommended: yes.*
15. **OD-15 — Per-task review.** In parallel runs, give every code, test or harness task a
    per-task independent review. Docs tasks get a primary diff check, except rule and
    agent-config edits, which are T3. Always one whole-feature review. *Recommended: yes. "No"
    means per-task review only for T3 tasks, as B5 proposed.*
16. **OD-16 — Parallel workers.** Parallel worktree workers run only when you ask for them.
    *Recommended: yes.*
17. **OD-17 — Preview filter.** Publish a Preview unless every changed path is on the
    non-product allowlist, and use the same "product inputs" definition for the smoke gate.
    *Recommended: yes.*
18. **OD-18 — No per-task record commits.** Evidence is recorded once per wave or at feature
    end, and the controller pushes once per wave. *Recommended: yes.*
19. **OD-19 — Lean records.** Adopt the R10 package:
    - backlog done index;
    - plan ledger with the required fields;
    - D-entries only for binding rules, with UI picks as one spec line;
    - "Amended by" back-pointers;
    - validator checks for D-ID uniqueness, AC coverage and amended-by links;
    - spec and verification skeletons.

    *Recommended: yes.*
20. **OD-20 — Post-deploy loop.** One "Pending owner checks" list, plus an owner-invoked
    `post-deploy-check` skill that records verdicts only. *Recommended: yes.*
21. **OD-21 — Memory role.** Memory holds host facts and preferences only. Rules live only in
    the repo, and duplicate memory entries are pruned once their repo copies exist.
    *Recommended: yes.*
22. **OD-22 — Worktree cleanup.**
    - Agents remove merged worktrees at the last merge step, under R12's strict conditions.
    - A one-time cleanup of the 31 merged worktrees.
    - The desktop's 7-day inactive archive, once verified to keep unmerged branches.

    *Recommended: yes.*
23. **OD-23 — Smoke target.** Add a test-only per-run tray identity, limited to the identity
    value inside the existing test switch, and run smokes against the Release unpackaged
    build. *Recommended: yes.*
24. **OD-24 — DiagnosticCrashTests.** Keep the 10 s bound, but start it from an explicit
    probe-ready signal instead of from process launch. *Recommended: yes. "No" leaves it as
    is, with rerun-on-first-run.*
25. **OD-25 — Sandbox UI audit suite.** Freeze the opt-in suite (mark it `Explicit`; no edits
    required on UI changes), while keeping the package, upgrade and feed smokes.
    *Recommended: yes.*
26. **OD-26 — Routing spike.** Drop the AIU-002 routing spike build from CI. *Recommended:
    yes, unless you know it still guards a live WinUI routing risk.*
27. **OD-27 — csharp-lsp trial.** Trial the plugin with a pinned `csharp-ls` global tool on
    one task, time-boxed, with a written result. *Recommended: yes.*
28. **OD-28 — Stryker.NET spike.** A half-day spike on Core budget math and parsers, with no
    CI gate. *Recommended: yes.*
29. **OD-29 — `winapp ui` trial.** Use its read-only commands to debug smoke locators, with
    telemetry opted out. *Recommended: optional.*
30. **OD-30 — This branch at merge.** When the optimization branch finally merges, put the
    verbatim prompt's two placeholder mentions in code spans, with a note that this is the
    only change to it, so CI `--final` passes. Nothing else changes in the prompt.
    *Recommended: yes.*
