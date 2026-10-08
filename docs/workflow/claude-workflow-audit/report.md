# Claude workflow audit — Stage 1 report

Status: Stage 1 analysis, 2026-10-08. Read-only toward the repository; no rule, tool,
setting, test or code was changed. Base: `origin/main` at `4f32185` (fetched again before
writing; unchanged). Lane reports: `lanes/<lane-id>.md`, for example
[A1](lanes/A1.md) and [B1](lanes/B1.md); lane A4 was split into A4a, A4b and A4c by
transcript range. Prompt: [stage-1-prompt.md](stage-1-prompt.md). Status: [status.md](status.md).
Labels: **fact** (observed in repo, history, transcripts or primary docs), **inference**
(the architect's judgement), **external-claim** (stated by a third party, not tested here).

## 1. Executive summary

The repository's quality machinery is strong and worth keeping: AC-driven specs, honest
PASS/FAIL/NOT_RUN/BLOCKED evidence, a cheap validator with a `--final` gate, fast unit suites
(1,245 cases in about 35 s warm), a launch smoke as the Preview gate, D-190 post-deploy owner
checks, and conventional, check-annotated commits. The CI red rate fell from 10.5 % overall to
2.7 % since the switch to Claude Code only (A5-F3/F4, fact).

What slows work and pulls in the owner is not missing rigor but **contradictory or
invisible process**:

1. **The written Git policy contradicts practice and is unsafe if followed literally.**
   CONTRIBUTING says "work directly on `main`, push WIP after each step", while every green
   push to `main` signs and publishes a Preview to the owner's auto-updating app; all real
   work runs in worktree branches (A1-F1/F2, A3-F7, A5-F10, B5-F12; fact).
2. **Third-party process overrides repo rules.** The superpowers SessionStart injection
   ("1 % chance → MUST invoke"), its approval gates and per-task review cadence produced about
   20 avoidable owner approval turns and one angry correction (A4c-F1, A3-F1, B2-F3; fact).
   Four more plugins with zero uses cost about 5k context tokens per session and subagent
   (A3-F2, B2-F1).
3. **The repo's own guardrails are invisible to Claude Code.** The three skills
   (security-lifecycle, provider-evidence, convergence-review) live in `.agents/skills`, which
   Claude Code does not scan; the main reviewer agent is git-excluded and unversioned
   (A1-F5, A3-F3/F4, B1-F1/F4, B2-F13; fact).
4. **Hard safety rules are prose only.** Sessions run in `bypassPermissions` with no deny
   rules anywhere; "never force-push" and "never read source CLI credentials" have no
   mechanical backstop, although deny rules are enforced even in bypass mode (A3-F5, B1-F2/F3;
   fact, verified in the permission-modes doc).
5. **Avoidable owner turns.** About a third to a half of agent questions were decidable under
   the written rules (A4a-F3: 13 of 40; A4b-F2: 13 of 27); kickoff prompts narrowed standing
   policy and caused a 23-hour merge stall (A4b-F1); status polling took about 14 % of owner
   turns until the 2026-10-08 push-status rule, then dropped to zero (A4c-F2).
6. **Record and deploy churn.** 36-45 % of commits are records-only; per-task `record T-xx`
   commits and docs-only pushes each publish a Preview (161 Previews in 17 days, 37 on
   2026-10-08); the backlog is 78 % closed history (A2-F1/F4, A5-F1; fact).
7. **The UI smoke is the dominant cost and flake source**, mostly from the environment
   (locked desktop, the owner's installed app's tray icon, stale UIA references), not from
   product defects (A6-F3, A4b-F6, A4c-F5, A5-F6).

The proposed target workflow (section 4) keeps every quality gate, makes repo rules the
single authority, adds mechanical deny rules, sizes records and review by risk, and removes
the process noise. The top five recommendations are R1-R5 in section 5; section 8 lists
26 yes/no owner decisions that scope Stage 2.

## 2. Current-state findings, cross-validated

Each theme cites the lanes that independently support it. Disagreements between lanes are
resolved in section 7 and noted inline.

### 2.1 Rules and authority

- **Git policy vs practice (fact, 5 lanes).** CONTRIBUTING.md:32-38 "do not create new
  branches"; D-178/D-179 describe a third policy; D-196 and memory assume worktrees; 45 merges
  into `users/*`/`worktree-agent-*` branches since 2026-10-01; 37 local branches, 36 merged
  (A1-F1, A3-F7, A5-F10, B1-F6, B5-F12).
- **WIP-to-installed-app risk (fact + inference).** CONTRIBUTING.md:28/33-35 asks for WIP
  pushes to `main`; every green push publishes a Preview (CONTRIBUTING.md:45-49); the same
  risk was rated P0 as ANL-01 in the AIU-045 analysis and the fix was reversed the same day
  (`ff1d788`). Only the worktree habit currently prevents it (A1-F2, A5-F12).
- **Duplication and accretion (fact).** 14 rules appear in 2-5 places; about 45 % of
  AGENTS.md restates CONTRIBUTING or verification.md; dated "Owner direction" paragraphs and
  reversal chains sit inside operative rules; five process directions have no decision record
  (A1-F6/F7/F8).
- **Memory duplicates rules (fact, 4 lanes).** 5 of 7 memory entries restate repo rules; two
  rules exist only in memory (process-skill gates count as approved; worktree branches merge
  to `main`); one memory entry is stale (Codex section removed in `5d24744`). Auto memory is
  shared across worktrees but not loaded into subagents, so host facts such as the Defender
  first-run delay never reach implementer or reviewer subagents (A1-F3, A3-F6, B1-F5, B4-F1/F2).
- **Stale direction (fact).** goals.md `active_goal: G-002` is done; "Current direction" names
  finished items; AIU-046 still `review` though shipped (A1-F10, A2-F12).
- **Decisions without back-pointers (fact).** 18 recent decisions amend 21 earlier ones and
  none of the 21 carries "Amended by"; 20 identical stub entries; AIU-051's spec still
  describes the sheet D-197 replaced (A1-F11, B4-F4, A2-F6).
- **Always-loaded size is fine; composition is not (fact).** About 1,270 tokens from
  AGENTS.md plus the memory index; the Git flow, Preview consequence and check commands that
  nearly every session needs are only in read-by-task files (A1-F14, B1-F6). Plugin
  injections and listings add roughly 6-7k tokens per session (A3-F10, estimate).

### 2.2 Agent tooling

- **Repo skills not discoverable (fact, 5 lanes).** 0 Skill-tool invocations, 7 direct reads
  in 176 transcripts; validator and formats.md still point at `.agents/skills`; D-034 made the
  location agent-neutral when Codex was in use (A1-F5, A3-F3, B1-F1, B2-F13, B4-F7).
- **Custom agents untracked and run-specific (fact).** `.claude/agents/` is in
  `.git/info/exclude`; descriptions say "AIU-045"; model ids pinned; aiu-reviewer (26 runs) is
  the backbone of independent review evidence; the first 8 AIU-045 dispatches failed because
  the types were not loaded (A3-F4, B1-F4, B5-F9, A4b-F4).
- **Plugins (fact).** superpowers injects about 850 tokens at every start/clear/compact and
  auto-updates unpinned through claude.ai sync (v6.4.1 and v6.4.2 changed execution and plan
  formats); specs AIU-046..055 had to add override sections. ux-superpowers,
  design-superpowers, desktop-commander and the design plugin: 0 uses; the ux-superpowers hook
  errors in 26 of 27 worktree sessions; desktop-commander runs `npx -y …@latest` and has 2026
  CVEs (A3-F1/F2, B2-F1..F9, A4c-F10).
- **Dead artifacts (fact).** `.codex/` (sets a gpt-6-astra subagent), `.omp/`,
  `.github/copilot-instructions.md`, `docs/archive/omp/` (84 KB), two agent-neutral migration
  docs still headed "Ready for execution", `environment.md` (OMP bootstrap log with personal
  identifiers), architecture-audit-plan "Ready for execution" though executed; about 18,000
  words of non-live history in docs/workflow (A1-F4/F9, A3-F8, A6-F13).
- **Worktree sprawl (fact).** 32 worktrees, all merged, 8.1 GB; 11 `agent-*` worktrees of
  about 450 MB each; desktop auto-archive fires only on PR close and the owner uses no PRs
  (A3-F7, A4c-F13, A5-F14, B1-F7).

### 2.3 Owner involvement (transcripts)

- **Owner turns.** 110 (to 10-03), about 51 (10-04..08, main folder) and 109 (worktrees).
  About 17 % are pure approvals; the costly non-decision turns are status pulls, prompt
  requests, relays and routine approvals — about 50 of 110 in the early range (A4a-F13).
- **Avoidable questions (inference, consistent across 3 lanes).** Continuing an approved
  plan, saving files, recording decisions already given, merging or pushing, "write yes and
  I'll mark your review PASS", a second confirm after a variant pick (A4a-F3, A4b-F2, A4c-F1).
- **Prompts that narrow standing policy (fact).** Agent-written kickoff prompts said "do not
  merge or push without permission", leading to a 1,404-minute stall and D-190 (A4b-F1).
- **Unattended runs stall on questions (fact).** AIU-045 kickoff questions blocked 85 min, a
  mid-run batch 118 min; a lock-state question could have been checked mechanically (A4b-F3).
- **Owner as message bus (fact, early range).** 11 requests for launch prompts, pasted
  prompts up to 15.8k characters, `/goal` length friction (A4a-F1).
- **What already works (fact).** The 2026-10-08 push-status rule removed status questions;
  the 10-07 variants rule is valued, provided variants are rendered; `/goal` runs from a repo
  plan needed 0-1 owner turns (A4c-F2/F9, A4a-F13).

### 2.4 Records and history

- **Backlog (fact).** 627 lines, 11,030 words; done items are 78 % of item words; 37 field
  names, 20 ad-hoc `*-note` fields; about 15k tokens per full read (A2-F1).
- **Plans in repo (fact).** tasks.md is the largest record (1.4-2.8× the spec), referenced
  from outside its folder 0 of 28 times; AIU-055's embeds a whole worker manual and
  authorizations (A2-F3, B5-F2).
- **Records-only commits (fact).** 38 % of non-merge commits (A2-F4) / 45 % docs-only by path
  (A5); 19 `record T-xx` commits; each push publishes a Preview (A5-F1).
- **Numbering (fact).** D-196 ended collisions (AIU-046 used twice; D-192 taken) but adds a
  numbering commit and a register conflict per merge; one bulk replace rewrote D-196's own
  text (`d8b5efa` → `a0f0d22`); 27 commit subjects keep placeholders; `git log --grep AIU-053`
  finds nothing (A2-F7, A4c-F3, A5-F8).
- **Decision inflation (fact).** 16 decisions in 2 days, mostly owner UI variant picks
  (A2-F11, A5-F9).
- **Post-deploy loop open (fact).** 24 NOT_RUN mentions in the backlog and 7+ verification
  files; no single list of what the owner should check (A2-F9, B5-F11).
- **What is used (fact).** spec.md (most-read, 35/38 referenced externally) and the
  verification AC table; the validator runs in about 379 transcript commands (A2-F14).

### 2.5 Tests, smoke and CI

- **Local checks are cheap (fact).** Validator about 3 s, Presentation 4.6 s, Infrastructure
  28 s (two serial classes dominate), app no-op build 19 s; a full UI-change pass about 1.5 min
  warm (A6 timings).
- **Smoke is the cost and flake centre (fact + inference).** 389 smoke commands (about
  136 min) in worktrees; one AIU-045 task took 227 turns and 31 edits to one smoke file;
  NativeTrayClicks passed 19 of 44 recorded runs (includes red-first runs); the installed
  app's tray icon and auto-updates broke dev smokes; 71 `ByName` vs 22 `ByAutomationId`
  locators and 25 `Thread.Sleep` (A6-F3, A4b-F6, A4c-F5, A5-F6, B6-F10).
- **Smoke is the only guard for about 3,500 lines** of code-behind, controls and lifetime
  code; tray/launch regressions reached `main` (A6-F4, A5-F11).
- **Gate text vs practice (fact).** Preview gate says Release candidate; smokes run against
  Debug, so each pass builds twice; matrix lacks rows for scripts, harness-only and test-only
  changes; `Test-PreviewRelease.ps1` is CI-only and undocumented (A6-F5/F7).
- **Flakes (fact).** DiagnosticCrashTests caused 6 red CI runs and took 3 days to fix, and
  failed again on a cold local run (A5-F4, A6-F2).
- **CI (fact).** p50 6.5 min; Preview job p50 3 min, queue up to 12 min in bursts; AIU-002
  routing spike built every run; CI runs only after code is on `main` (A5-F2/F13).
- **Escapes found by the owner are mostly product intent, not test gaps** (A5-F7): hand
  cursor, closeable strip, cap validation, focus visuals (3×), tray icon at >100 % scale.

### 2.6 Parallel and unattended execution

- AIU-055: 11 tasks, 2h13m, one owner turn, every task reviewed — a real success (A4c-F7,
  A5-F5, B5-F4). Friction was mechanical: 31 isolation-guard blocks, 7 blocked sleeps,
  misrouted reviewer results, push races, ~50 commits, 11 leftover worktrees.
- AIU-045: 32 subagents, 17 of them reviews; most per-task reviews Minor only (incl. docs-only
  tasks); the decisive catch came from the whole-run review; $197.5 and two usage-limit stalls
  (A4b-F7/F8). AIU-046 per-task reviews did find real defects.
- Host tooling is rediscovered per session: `python` resolves to the Store stub (33 hits),
  no node, no pwsh 7, first build fails with `NETSDK1004` in 17 of 22 worktrees (A4a-F8,
  A4c-F4/F11).

## 3. Research findings with verdicts

Sources were accessed 2026-10-08; details, versions and links are in the B lane reports.

| Candidate | Lane | Benefit here | Main risk/cost | Verdict |
|---|---|---|---|---|
| Move repo skills to `.claude/skills` with sharper trigger descriptions | B1-F1, B2-F13 | Guardrail skills become invocable by name in sessions and subagents | Validator path + tests change; supersedes D-034 location clause | **Adopt** |
| Committed project `.claude/settings.json` with deny rules only | B1-F3 | Mechanical backstop for force-push, stash pop/drop, `gh release`/`gh workflow run`, source-CLI credential reads; applies in bypass mode and every worktree | Pattern rules are bypassable via `sh -c`; settings diffs are security-relevant | **Adopt** |
| Auto permission mode instead of bypass | B1-F2 | Classifier blocks destructive git and credential printing | Observed false blocks here: "Merge Without Review" on push to `main`, `git status`, "Instruction Poisoning" on plan steps, an 18-min classifier outage (A4a-F7, A4c-F8) | **Owner decides; not recommended now** |
| One tracked, task-neutral reviewer agent (`model: opus` alias, read-only tools, calibrated severity) | B1-F4, B5-F6/F9 | Review evidence no longer depends on an untracked file | Small upkeep | **Adapt** |
| Built-in `/code-review` (local) and `/security-review` | B1-F8, B5-F7, B2-F11 | Cheap correctness lens next to AC review; security lens for T3 changes | False positives; triage per reviewer calibration | **Adopt `/code-review`; trial `/security-review` on T3** |
| `/goal` with a machine-checked done condition | B1-F9, B5-F8 | Fewer "continue" nudges and premature "done" claims in unattended runs | Judges transcript only | **Trial** |
| `/doctor prompt-audit` (read-only report) | B1-F6, B4-F10 | Input to rule consolidation | None | **Adopt once** |
| Path-scoped `.claude/rules/logging.md` | B1-F6 | Logging policy loads only when editing code | Small | **Trial** |
| Desktop "archive inactive sessions" + archive at merge | B1-F7 | Ends worktree sprawl | Archive is reversible only by re-creating | **Adopt (owner setting)** |
| superpowers plugin | B2-F2..F5 | Good discipline content | Injection, gates, unpinned auto-update, per-skill disable impossible | **Adapt**: vendor systematic-debugging, verification-before-completion, receiving-code-review; write repo skills for planning, variants and plan execution; then disable per project |
| ux-superpowers, design-superpowers, desktop-commander, Anthropic design plugin | B2-F6..F9, B3-F9 | None for this app | Context cost, unpinned npm, CVEs, auth noise | **Retire** |
| pr-review-toolkit (silent-failure and test-gap lenses) | B2-F10 | Fits logging and test-gap weak spots | None if copied as checklists | **Adapt idea** |
| Official code-review/feature-dev/commit-commands/hookify plugins, managed Code Review, ultrareview | B2-F10, B5-F7 | PR-centric or duplicate built-ins | Cost, cloud upload | **Reject** |
| security-guidance plugin | B2-F11 | Possible secret/path catches | Per-turn Opus reviews; own README advises off for shared worktrees | **Reject now** (built-in `/security-review` on demand) |
| `csharp-lsp` plugin + `csharp-ls` 0.28.0 | B3-F1, B1-F12, B2-F12 | Diagnostics after each edit without a build | Global dotnet tool; WinUI/XAML accuracy and solution discovery unverified | **Trial, time-boxed** |
| Serena, Roslyn MCP servers | B3-F2 | Little over Grep at 37k LOC | Needs pwsh 7; unpinned uvx; single maintainers | **Reject** |
| Microsoft `winapp ui` CLI (read-only commands) | B3-F5 | Inspect live UI tree for smoke locators and variant screenshots | Public preview; telemetry opt-out needed | **Trial, narrow** |
| Windows-MCP, FlaUI MCP wrappers, GitHub MCP | B3-F4/F7 | FlaUI harness and `gh` already cover | Full desktop control, PAT in env | **Reject** |
| Microsoft Learn MCP (already connected) | B3-F8 | Authoritative WinUI/MSIX/.NET answers | Never put payloads or identifiers in queries | **Adopt guidance line** |
| Graph/vector memory (Graphiti, mem0, Basic Memory), code-graph MCPs | B4-F8/F9 | Grep suffices for 268 C# files and 326k words of docs | DBs, keys, egress, injection surface, licences | **Reject** |
| "Amended by" back-pointers + validator check | B4-F4 | Agents stop applying superseded decisions | Small validator change | **Adopt** |
| Spec Kit, BMAD, OpenSpec, Tessl | B5-F1 | AIU system already covers SDD | Second source of truth, installers | **Reject; borrow two ideas** |
| Risk tiers chosen after investigation (BMAD); bugfix spec current/expected/unchanged (Kiro) | B5-F2 | Sizes records and review to risk | None | **Adapt idea** |
| Agent teams | B5-F10 | — | Experimental, auto-approves plans | **Reject** |
| Dynamic workflows / parallel subagents for read-only audits | B5-F10 | This audit pattern | Token cost | **Trial for audits only** |
| "Fails without the fix" reviewer check (temp worktree at base) | B6-F3 | Direct defence against tests that check nothing | A few minutes per bug fix | **Adopt** |
| Test-erosion sentinel script + xUnit1004 as error | B6-F2/F11 | Flags deleted tests, new Skip, fewer asserts | Must ask for justification, not block | **Adapt** |
| Smoke reliability helpers (polling re-find, ID-first locators, failure dump) | B6-F10 | Fewer blind fix loops | Gradual adoption | **Adapt** |
| Stryker.NET 5.0.0 | B6-F4 | Measures assertion strength on Core math/parsers | xUnit v3 runner maturity unverified | **Trial: half-day spike, no gate** |
| CsCheck property tests | B6-F5 | Budget math invariants | New dependency | **Trial (owner)** |
| Verify snapshots, ArchUnitNET/NetArchTest, FsCheck, Appium/WinAppDriver, coverage gates, test-impact tools | B6 | Duplicates existing tests or adds toolchains | — | **Reject** |
| Hooks (Stop gate, PostToolUse build), `claude -p`, GitHub Action, cloud routines, output styles, OS sandbox | B1-F10/F11 | No current need; slow suites; sandbox not on native Windows | Upkeep, secrets | **Reject now** |

## 4. Target workflow

Design principles: repo rules are the only process authority; each rule has one home;
ceremony and review scale with risk; mechanical guards back the hard boundaries; the owner is
asked only for scope, product intent, security, destructive/external authority, dependencies
and visible UI choices.

### 4.1 Flow

1. **Session start.** The desktop app creates a worktree branch (`users/*`). Always loaded:
   a re-composed AGENTS.md (architecture boundaries; ask/never-ask list; Git flow summary
   including the Preview consequence; check commands; security boundaries; truthfulness;
   UI-variants rule; push-status reply; read-by-task map with explicit paths). No plugin
   injection. Memory holds host facts and preferences only. The agent restores once
   (documented one-liner) and checks whether the request is already on `main` or in another
   worktree.
2. **Intake.** The agent inspects code and states one plan line: tier, intended result,
   acceptance checks. It decides everything inside approved scope and authority itself and
   records the decision with its reason ("recommendation = default"). It asks once, batched,
   only for the "always ask" categories. Visible UI changes get 2-3 **rendered** variants in
   one question; regressions against approved UI skip variants. Kickoff and handoff prompts
   inherit CONTRIBUTING, D-190 and D-196 unless the owner narrows them in the current request.
3. **Records by tier** (agent-chosen after investigation, owner may override):
   - **T0 fix:** plan and evidence in the commit message.
   - **T1 small feature or UI variant:** one-page spec (Problem, R-xx, AC-xx, Out of scope;
     bugs may use current/expected/unchanged) plus verification. No D-entry unless it binds
     future work.
   - **T2 multi-part:** as T1; tasks.md only when parallel workers are used, task blocks
     only; the worker procedure lives in the tracked implementer agent.
   - **T3 credentials, storage, migration, destructive data, release/update path:** adds
     design.md, the security-lifecycle skill and blocking focused independent review.
4. **Implement.** One primary session by default; red-to-green for logic and bug fixes;
   class-filtered tests in the inner loop, full suites once on the merged tree. Parallel
   worktree workers only on owner request (or under the rule in OD-14), at most about 4,
   simple single-purpose commands, Monitor/background waits instead of sleep, workers run
   unit suites and builds, the controller runs desktop smokes once on the integrated tree.
5. **Review (one pass, risk-based).** T0: primary diff review. T1/T2: one fresh-context
   review of the integrated diff against the ACs by the tracked reviewer plus local
   `/code-review`; in parallel runs, per-task review for code, test and harness tasks only.
   T3: plus focused independent review (convergence-review, `/security-review`), blocking.
   Reviewer calibration: Important = violates an AC, spec, security/data boundary or
   reproducible defect, with file:line and a failing test or repro; at most five Minor; one
   round, then re-check of fixed lines only. For bug fixes the reviewer confirms the new test
   fails at the base commit.
6. **Verify.** Change-based matrix (with rows for scripts, harness-only and test-only
   changes); smoke against the Release unpackaged build; evidence names checks by matrix ID;
   PASS/FAIL/NOT_RUN/BLOCKED per AC; optional `/goal` encoding the checks for unattended
   runs.
7. **Merge.** Save points stay on the worktree branch. At completion: fetch, merge fresh
   `main`, run the numbering script (placeholders → next numbers, outside code-quoted rule
   text), validator `--final`, required checks on the merged tree, push to `main`, verify
   ancestry after `git fetch`, reply "Pushed to `main` <hash>", archive the session and
   remove the worktree.
8. **Deploy and post-deploy.** CI validates every push; a Preview publishes only when
   product inputs changed. Each feature appends 3-5 lines to one "Pending owner checks" list.
   After updating, the owner may invoke a `post-deploy-check` skill that reads the installed
   version and sanitized logs and records results; checks stay NOT_RUN until then and never
   block merge (D-190).

### 4.2 What the flow uses

- **Agents:** one tracked reviewer; one tracked implementer only if parallel runs are kept.
- **Skills (repo, `.claude/skills`):** security-lifecycle, provider-evidence,
  convergence-review (with silent-failure and test-gap lenses), vendored
  systematic-debugging / verification-before-completion / receiving-code-review, and
  owner-approved new ones: planning-and-variants, plan-execution, post-deploy-check.
- **Built-ins:** `/code-review`, `/security-review`, `/goal` (trial), `/doctor prompt-audit`
  (once), Monitor/background tasks.
- **MCP/tools:** Microsoft Learn MCP (guidance line), `gh` CLI, FlaUI harness; trials:
  csharp-lsp, `winapp ui`.
- **Memory:** host facts and owner preferences only; rules live in the repo.
- **Settings:** a committed deny-only project `.claude/settings.json`.

### 4.3 What it removes

The superpowers injection and its gates (after replacement skills exist); four unused
plugins; per-task record commits; executed plans kept in the repo; backlog history prose;
D-entries for UI picks; memory copies of rules; OMP/Codex/Copilot adapters; stale workflow
docs (archived, not deleted); docs-only Previews; per-task review of docs tasks; foreground
CI watching and sleep polling; and, if the owner agrees, the routing-spike CI build and the
maintenance burden of the opt-in Sandbox UI audit suite.

## 5. Recommendations, ranked by impact on quality, then speed

Authority: **agent may do** = Stage 2 may implement after the owner approves the bundle in
OD-1; **owner decides** = needs its own decision in section 8. Effort: S < 2 h, M ≤ 1 day,
L > 1 day. All changes are Git-reversible unless noted.

### R1 — One true Git and integration flow; Preview gate becomes a merge gate
- **Problem/evidence:** CONTRIBUTING asks for WIP pushes to `main` while each green push
  publishes to the owner's app; practice is worktree branches; three texts disagree (A1-F1/F2,
  A3-F7, A5-F10, B5-F12).
- **Change:** rewrite the Git policy to the real flow (section 4.1 steps 1 and 7): save
  points on the worktree branch, only verified merges reach `main`, push and verify at
  completion, archive the worktree. Merge D-178/D-179 into one current decision; move history
  to decisions. Decide what a BLOCKED agent smoke means for merge (OD-3).
- **Benefit:** removes the unverified-WIP-to-installed-app path and the most re-asked question.
- **Quality risk:** work held only on a local branch can be lost if a worktree is deleted
  before merge; mitigated by archive-only-after-merge and the uncommitted-changes check.
- **Effort/reversibility:** S; reversible. **Authority:** owner decides (OD-2, OD-3).
  **Order:** first; R10 and R12 depend on it.

### R2 — Mechanical deny rules for the hard boundaries
- **Problem/evidence:** bypass mode on the bare host, no deny rules; hard rules are prose
  (A3-F5, B1-F2/F3, A4c-F8). Deny rules apply in every mode including bypass (fact, docs).
- **Change:** commit a deny-only project `.claude/settings.json`: force-push variants
  (Bash and PowerShell), `git stash pop/drop`, `gh release *`, `gh workflow run *`, `Read`/`Edit`
  on the source-CLI credential stores named in docs/providers, `git reset --hard`. Verify each
  rule with a harmless dry run. Keep the owner's bypass choice (OD-9 asks about auto mode).
- **Benefit:** a backstop against injection-driven or mistaken destructive actions without
  adding prompts.
- **Quality risk:** a too-broad pattern could block a legitimate owner-authorized action
  (e.g. an approved provider-evidence check); the owner can override locally.
- **Effort/reversibility:** S; reversible by deleting lines. **Authority:** owner decides
  (OD-8; security configuration, needs security-lifecycle review). **Order:** early,
  independent.

### R3 — Make repo guardrails discoverable and versioned
- **Problem/evidence:** skills invisible; reviewer agent untracked and AIU-045-specific
  (A1-F5, A3-F3/F4, B1-F1/F4, B2-F13, B5-F9).
- **Change:** move the three skills to `.claude/skills/` with concrete trigger descriptions;
  retarget the validator and formats.md; record a decision superseding D-034's location
  clause. Track one generalized reviewer agent (read-only tools, `opus` alias, `skills:
  [convergence-review]`, calibration lines from B5-F6); track an implementer only if parallel
  runs stay (OD-12). Name the reviewer and its general-purpose fallback in CONTRIBUTING
  "Review and integration".
- **Benefit:** security, provider-evidence and review rules load by name in every session and
  subagent; review evidence survives a fresh clone.
- **Quality risk:** none known; the content is unchanged.
- **Effort/reversibility:** S-M; reversible. **Authority:** owner decides (OD-10, OD-11).

### R4 — Ask only what is the owner's; never narrow standing policy
- **Problem/evidence:** 13/40 and 13/27 avoidable questions; ceremonial "write yes" gates;
  kickoff prompts narrowing Git policy (23-h stall); unattended runs blocked on questions
  (A4a-F3, A4b-F1/F2/F3, A4c-F1, A1-F13).
- **Change:** add to AGENTS.md a short "always ask" list (new scope, product intent,
  security boundary, destructive or external authority, dependencies, visible UI variants) and
  "never ask" list (continue an approved plan, save files, record decisions already given,
  run checks or reviews, numbering, merge and push a verified change, choose the minimal fix
  for a reported bug, confirm a variant already picked). Recommendation = default inside
  authority, recorded with reason. Generated prompts inherit standing policy unless the owner
  narrows it in the current request. Unattended runs continue on stated defaults and list
  open questions at the checkpoint; observable state (desktop lock, CI variable, origin hash)
  is checked, not asked. Owner-reported results are recorded as "owner-reported PASS (date)".
- **Benefit:** the largest reduction of routine owner turns.
- **Quality risk:** an agent could decide something the owner wanted to weigh; mitigated by
  the explicit always-ask list and recorded reasons the owner can reverse.
- **Effort/reversibility:** S; reversible. **Authority:** owner decides (OD-4, OD-5).

### R5 — Repo-owned process instead of plugin process
- **Problem/evidence:** superpowers injection and gates, unpinned updates, per-spec overrides;
  four unused plugins (A3-F1/F2, A4c-F1/F10, A4b-F7/F10, B2-F1..F9).
- **Change:** phase 1 (immediate): one AGENTS.md precedence line — plugin skills are
  optional tools; repo spec locations, review cadence, Git policy and the never-ask list win;
  no `docs/superpowers` or `.superpowers` artifacts outliving the session. Phase 2: vendor the
  three aligned skills (MIT, with attribution); write repo skills for planning-and-variants
  (keeps the owner's `/brainstorming`-style variant loop, rendered mockups) and plan
  execution; then set `"superpowers@synced": false` in the project settings. Separately the
  owner retires the four unused plugins and unneeded personal connectors for coding sessions.
- **Benefit:** deterministic process, about 6k fewer tokens per session and subagent, no
  silent behaviour changes from upstream releases.
- **Quality risk:** losing useful discipline if phase 2 skips a skill; mitigated by vendoring
  first and disabling last. The owner currently starts sessions with
  `/superpowers:brainstorming` (12+ times), so phase 2 must provide an equivalent command.
- **Effort/reversibility:** phase 1 S; phase 2 M; reversible (one settings line).
  **Authority:** owner decides (OD-6, OD-7).

### R6 — Risk-tiered records and review
- **Problem/evidence:** ceremony chosen by habit (AIU-055: about 11k record words for about
  2.3k changed lines); 17 reviews in AIU-045 mostly Minor, incl. docs tasks; AIU-046 and
  AIU-055 per-task reviews did catch real defects (A4b-F7, A5-F5, B5-F2/F5, A2-F11).
- **Change:** add the T0-T3 tier table (section 4.1 step 3) and the review cadence and
  reviewer calibration (step 5) to CONTRIBUTING; adopt local `/code-review` for T1/T2 and
  `/security-review` for T3; add the "fails at base" check for bug fixes (B6-F3).
- **Benefit:** fewer documents and review rounds for small work; review strength unchanged
  or stronger where risk is.
- **Quality risk:** under-tiering a risky change; mitigated by objective T3 triggers and
  owner override.
- **Effort/reversibility:** S; reversible. **Authority:** owner decides (OD-13, OD-14).

### R7 — Make the smoke trustworthy and run it once
- **Problem/evidence:** environment-caused failures, installed-app interference, Debug vs
  Release mismatch, blind fix loops (A6-F3/F5, A4b-F6, A4c-F5, A5-F6, B6-F10).
- **Change:** a test-only per-run tray identity so the smoke never touches the installed app
  (small app hook; OD-21); smoke against the Release unpackaged build (one build); polling
  re-find helper, ID-first locators and an automatic screenshot plus UI-tree dump on failure,
  adopted as files are touched; after two failed reruns, write a diagnosis and stop; desktop
  smokes once per integration, with one desktop-use window per session; per-test pass/fail
  history in one ignored CSV.
- **Benefit:** fewer false reds and owner interruptions; smoke keeps guarding the 3.5k
  lifetime/tray lines.
- **Quality risk:** a test hook in the app must not change product behaviour; keep it
  behind the existing smoke environment switch.
- **Effort/reversibility:** M; reversible. **Authority:** owner decides the app hook (OD-21);
  the harness changes are agent may do (OD-1).

### R8 — Stop publishing Previews and commits for bookkeeping
- **Problem/evidence:** 161 Previews in 17 days, 96 of 197 pushes docs-only; a mid-run
  Preview broke a dev smoke; 19 `record T-xx` commits (A5-F1/F5, A2-F4, A4c-F5).
- **Change:** skip Preview publication when no product inputs changed since the last
  published Preview (path filter or a check in `Publish-Preview.ps1`); keep `validate` on
  every push. Record worker evidence once per wave or at feature end; in parallel runs the
  controller integrates locally and pushes once per wave.
- **Benefit:** fewer meaningless updates for the owner, shorter CI queues, cleaner history.
- **Quality risk:** a path filter that misses a product input would skip a needed Preview;
  mitigate with a conservative include list and the owner's manual republish dispatch.
- **Effort/reversibility:** S; reversible. **Authority:** owner decides (OD-15, OD-16).

### R9 — Consolidate rules: one home each, current wording only
- **Problem/evidence:** duplication, accretion, memory-only rules, stale goals (A1-F3/F6/F7/
  F10/F14/F16, B1-F5, B4-F2/F11).
- **Change:** apply A1's target structure after R1/R4/R6 are decided: AGENTS.md re-composed
  (about 600-700 words); CONTRIBUTING without history (about 650); verification.md matrix,
  gate and evidence only (about 600), coding lessons moved to architecture docs; logging
  policy core plus a "Logging policy" section in logging.md (or the path-scoped rule trial);
  back-filled decision records for undocumented process directions; host facts copied from
  memory into a repo environment section linked from AGENTS.md; memory pruned to host facts,
  preferences and pointers; goals.md current-state paragraph. Run `/doctor prompt-audit` first.
- **Benefit:** about 35 % smaller effective per-session core, no drift between copies,
  subagents get host facts.
- **Quality risk:** losing a rule in consolidation; mitigated by a rule inventory diff
  reviewed by the owner.
- **Effort/reversibility:** M; reversible. **Authority:** agent may do once OD-2..OD-7 are
  decided (OD-1 bundle), owner reviews the diff; memory pruning is OD-19.

### R10 — Lean, honest work records
- **Problem/evidence:** backlog history, plans in repo, verbose verification, D-entry
  inflation, stale specs, scattered NOT_RUN (A2-F1..F13, A5-F9, B4-F4, B5-F3/F11).
- **Change:** backlog = live items in full plus a one-line done index and a "Pending owner
  checks" list; executed plans collapse to a one-line-per-task ledger in verification.md at
  completion; verification names checks by matrix ID; 15-20-line skeletons for spec and
  verification in formats.md; D-entries only for rules that bind future work; "Amended by"
  back-pointers; validator gains D-ID uniqueness, AC coverage and the amended-by link check.
- **Benefit:** backlog about −65 %, parallel-feature records −40-55 %, fewer conflicts.
- **Quality risk:** losing traceability; mitigated by keeping IDs, links, AC tables,
  NOT_RUN reasons and Git history.
- **Effort/reversibility:** M; reversible. **Authority:** owner decides (OD-17, OD-18).

### R11 — Close the post-deploy loop
- **Problem/evidence:** NOT_RUN owner checks scattered with no queue (A2-F9, B5-F11).
- **Change:** one "Pending owner checks" list; an owner-invoked `post-deploy-check` skill
  (`disable-model-invocation`) reading installed version and sanitized logs within logging
  privacy rules, recording results.
- **Benefit:** live behaviour gets confirmed; owner control improves.
- **Quality risk:** log reading must never touch credentials or raw provider bodies.
- **Effort/reversibility:** S-M. **Authority:** owner decides (OD-18).

### R12 — Worktree hygiene
- **Problem/evidence:** 32 merged worktrees, 8.1 GB (A3-F7, A4c-F13, A5-F14, B1-F7).
- **Change:** desktop "Archive inactive sessions" = 7 days; archive and remove the worktree
  as the last merge step; agents remove `agent-*` worktrees after integration; a one-time
  cleanup after checking each for uncommitted files.
- **Benefit:** disk, clarity, no work in stale worktrees.
- **Quality risk:** deleting uncommitted work; mitigated by the check and archive semantics.
- **Effort/reversibility:** S; the one-time deletion is not reversible for uncommitted files.
  **Authority:** owner decides (OD-20).

### R13 — Placeholder numbering as a tool, not a manual pass
- **Problem/evidence:** numbering commits, register conflicts, one rule-text corruption
  (A4c-F3, A5-F8, A2-F7).
- **Change:** a small script that assigns the next free AIU/D numbers on fresh `main`,
  replaces placeholders outside code-quoted text, renames the spec folder and runs `--final`;
  keep placeholders out of commit subjects and source comments.
- **Benefit:** removes a manual step and its error class.
- **Quality risk:** script bug; covered by validator `--final`.
- **Effort/reversibility:** S; reversible. **Authority:** agent may do (OD-1).

### R14 — Faster, steadier local loop
- **Problem/evidence:** two serial Infrastructure classes, diagnostics flake, matrix gaps,
  per-worktree restore failures, host tooling rediscovery (A6-F1/F2/F6/F7/F11, A4c-F4/F11,
  A4a-F8, A5-F4).
- **Change:** shard the 235-case corpus and cheapen the 256-file test without losing the
  boundary; probe readiness signal with a ≥30 s bound; matrix rows for scripts, harness-only,
  test-only and "Windows edits build both unit test projects"; document `Test-PreviewRelease.ps1`;
  inner-loop `-class` filters with `--no-build`; a worktree bootstrap one-liner; host facts
  (`py -3.13 -I`, no node, no pwsh 7). Test-erosion sentinel script and xUnit1004 as error.
- **Benefit:** Infrastructure suite about 25 s → 8-10 s; fewer false reds; fewer wasted calls.
- **Quality risk:** weakening a test while speeding it; the sentinel and review guard this.
- **Effort/reversibility:** M; reversible. **Authority:** agent may do (OD-1).

### R15 — Unattended and parallel run discipline
- **Problem/evidence:** blocked sleeps, isolation-guard blocks, misrouted reviewer results,
  foreground CI watching, usage-limit stalls (A4a-F11, A4b-F5/F8, A4c-F6/F7).
- **Change:** worker brief rules in the tracked implementer agent (simple commands, plain
  git, Monitor not sleep, reviewers dispatched by the controller, fetch-merge-retry before
  push, reports returned as messages); push, continue and check CI once at the end; Sandbox
  waits with a started marker and timeout; trial `/goal` on the next multi-part feature.
- **Benefit:** fewer wasted tool calls and stalls.
- **Quality risk:** none known.
- **Effort/reversibility:** S; reversible. **Authority:** agent may do (OD-1); the
  parallel-run trigger itself is OD-14.

### R16 — Retire dead artifacts and record "Claude Code only"
- **Problem/evidence:** agent-neutral wording and dead adapters mislead (A1-F4/F9, A3-F8,
  A2-F13, A6-F13).
- **Change:** record a decision that development uses Claude Code; amend the constitution
  "Execution" section and D-012/D-034/D-036; delete `.codex/`, `.omp/`, the `.omp` ignore
  lines and (if unused) `.github/copilot-instructions.md`; move finished, retired or paused
  workflow records and one-shot spec artifacts to `docs/archive/` with status headers fixed;
  ask about redacting personal identifiers in environment.md and pending.md.
- **Benefit:** less search noise and contradictory guidance.
- **Quality risk:** none; Git history keeps everything.
- **Effort/reversibility:** S; reversible. **Authority:** owner decides (OD-22).

### R17 — Targeted tooling trials
- **Change:** time-boxed trials, each with a kill criterion and a one-paragraph result:
  csharp-lsp with pinned `csharp-ls` (OD-24); Stryker.NET half-day spike on Core budget math
  and parsers, no CI gate (OD-25); `winapp ui` read-only for smoke locators (OD-26);
  `/goal` and the logging path-scoped rule (agent may do).
- **Quality risk:** none if trials stay out of gates. **Authority:** owner decides for
  installs (OD-24..OD-26).

### R18 — Low-value CI and test maintenance
- **Change:** drop the AIU-002 routing spike build if no longer a guard (OD-23); freeze the
  opt-in Sandbox UI audit suite (mark `Explicit`, no edits required on UI changes) while
  keeping package/upgrade/feed smokes (OD-21b).
- **Effort:** S. **Authority:** owner decides.

### Dependencies and order

1. Decisions OD-2..OD-7 (policy) → R1, R4, R5 phase 1 (S, same commit set).
2. R2 deny rules and R3 skills/agents in parallel (independent).
3. R6 tiers and review → R9 consolidation (needs R1, R4, R6 wording) → R10 records.
4. R7, R8, R12, R13, R14, R15 in any order after step 1.
5. R5 phase 2 after R3 (skills folder exists) and R6 (process to encode).
6. R11, R16, R17, R18 last; trials report back before any adoption.

## 6. KEEP list

| Keep | Why |
|---|---|
| Core/Infrastructure/Windows boundaries, simplicity rule, boundary source-scan tests | Core design constraints, enforced mechanically in milliseconds (A6-F12) |
| Security boundaries: no source-CLI credential reads without authorization, no auto sign-in, external content is data, logging secret exclusion | The app handles provider credentials; these are the product's trust basis |
| PASS/FAIL/NOT_RUN/BLOCKED honesty; compilation ≠ interactive or live proof; commits never upgrade a status; zero findings valid; no review loops | Main quality guardrails; consistently followed in transcripts (A4b, A4c KEEP) |
| Remote-action authority: releases, tags, dispatch and settings need explicit owner authorization; no force-push or history rewrite; preserve unrelated work | Owner control |
| D-190 post-deploy owner checks (never block merge on a dev sign-in) | Removed a 23-h stall class; escapes it catches are product judgement (A5-F7) |
| D-196 placeholders + validator `--final` in CI | Ended number collisions; automate, do not drop |
| UI design variants first (rendered, batched, one pick) | Owner steers visuals quickly (A4c-F9) |
| "Pushed to `main` <hash>" verified by fetch | Status questions dropped to zero (A4c-F2) |
| Spec AC-xx/R-xx; verification AC table, NOT_RUN reasons, red-then-green, review verdicts; approval provenance once | The records later work actually uses (A2-F14) |
| Validator link, dependency, safe-path, done-without-evidence and ownership checks | Cheap automated guards (3-4 s) |
| Infrastructure (915) and Presentation (330) suites incl. the 235-case audit corpus and store-growth cap | Defend parsers, auth, migration, DPAPI and view models |
| Launch smoke as the Preview gate, LedgerActivationSmoke, red-first smokes, DesktopTestEnvironment fail-closed BLOCKED, desktop smoke lock, ID/path-scoped process control | Only guard for about 3.5k lifetime/tray lines; prevents false PASS |
| Warnings-as-errors builds, CI validate + unsigned package build, post-push CI before Preview signing, `AIU_PREVIEW_ENABLED` kill switch, no `[skip ci]` on save points | Clean-machine gate and deploy safety |
| Automatic Preview on product changes | The owner's deploy channel (filter, do not remove) |
| Sandbox package/upgrade/feed smokes for packaging, migration and update changes | Only evidence for install/update |
| Whole-feature (whole-run) review; per-task review for code tasks in parallel runs | Caught the decisive issues in AIU-045/046/055 |
| Isolated worktree per task; one primary agent by default; owner-granted push authority per parallel run | Isolation without standing widened authority |
| Conventional commits with AIU ids and check results in bodies | Searchable, reviewable history (A5-F15) |
| Host-tooling facts (Defender rerun rule, "never relax the bound", IPv6 diagnosis, locked desktop) | Costly to rediscover; move to repo, keep content |
| The three repo skills' content; aiu-reviewer contract | Encode credential, provider-evidence and review boundaries |
| Owner-initiated brainstorming with recommendation-plus-pick option lists | Fast, effective owner decisions |
| Microsoft Learn MCP, built-in browser, claude_design, visualize | Actually used for this app (A3-F2) |
| All historical evidence | Archive, never delete |

## 7. Critic findings and resolutions

Pending: two independent read-only critics challenge sections 5 and 6 before finalization.

## 8. Owner decisions (yes/no)

Pending finalization after the critics.
