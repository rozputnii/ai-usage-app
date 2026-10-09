# Claude workflow audit — status

Living log for the workflow-optimization effort. Read this first in every session.

- **Branch:** `workflow-optimization`, a long-lived branch the owner requested. It is never
  merged into `main` or opened as a PR without an explicit owner decision.
- **Current stage:** Stage 2 (implementation) is in progress, started 2026-10-09 with the
  prompt in [stage-2-prompt.md](stage-2-prompt.md). Stage 1 is complete, and all owner
  decisions OD-1 to OD-31 were taken on 2026-10-09.

## Done

- **2026-10-08 — branch and prompt.** The branch was created from `origin/main` at `4f32185`,
  and the Stage 1 prompt was saved verbatim in [stage-1-prompt.md](stage-1-prompt.md).
- **2026-10-08 — lanes.** 15 lanes ran in parallel as read-only subagents. A4 was split into
  A4a, A4b and A4c by transcript range. Reports are in `lanes/`: A1, A2, A3, A4a, A4b, A4c,
  A5, A6, B1, B2, B3, B4, B5, B6.
  - All lanes ran as general-purpose agents, not Explore. Each had to write its report to the
    session scratchpad, and Explore cannot write.
  - Every lane was briefed to change nothing in the repository.
  - The orchestrator copied each report into `lanes/` and scanned it for private data; there
    were no hits besides local paths already present in the prompt.
- **2026-10-08 — critics and report.** Two fresh read-only critics reviewed the draft. Every
  finding was accepted or resolved; there were two blocking findings, both fixed. The final
  report is [report.md](report.md), with 18 ranked recommendations and 30 owner decisions.
- **2026-10-08 — `main` check.** `origin/main` was re-fetched before the conclusions were
  written. It was still `4f32185`, so no merge was needed and no finding is invalidated.
- **2026-10-08 — validation.** Document validation on the branch passes in normal mode.
  - `--final` reports exactly two `PLACEHOLDER_ID` diagnostics, both in the verbatim prompt;
    they are kept on purpose (see OD-30).
  - Placeholder mentions in the lane reports were put in code spans, and that was the only
    change made to them.

- **2026-10-09 — merged `origin/main` (`6e169dd`) into the branch (merge `4e513e1`).** The
  owner asked for main's unrelated bug fixes. A rebase was not used because the branch is
  published and CONTRIBUTING forbids force-push.
  - The merge brought six commits: a five-hour window fix, hover-only tooltips, the interval
    field fix and D-207.
  - None of them touches AGENTS.md, CONTRIBUTING.md, `docs/workflow/verification.md`,
    `docs/workflow/formats.md`, `.claude`, `.agents` or `.github`, so no finding is
    invalidated.
  - D-207 is one more UI-preference decision, which supports A5-F9.
  - `LocalBudgetStoreTests` gained tests, so re-measure A6-F1's timings before acting on R14.
  - Normal-mode validation passes.

## Owner decisions taken

- **2026-10-08:** Stage 1 was authorized with a broad multi-agent fan-out (see the prompt).
- **2026-10-09, OD-2 = yes.** The new Git flow is approved.
  - Each task gets its own branch, and save points are pushed to `origin/<task-branch>`
    (no CI, no Preview).
  - Only verified merges reach `main`, and the remote task branch is deleted after merge.
  - The owner's condition: a Preview still publishes automatically from `main` immediately
    after every verified merge.
- **2026-10-09, OD-3 = alternative 2.** When a required smoke is BLOCKED:
  - A change that touches Windows UI, the tray, launch or lifetime code stays on its branch
    until the smoke passes or the owner approves that change.
  - Other product changes covered by unit tests may merge, with the smoke recorded as
    BLOCKED.
- **2026-10-09, OD-4 = yes.** The always-ask and never-ask lists from R7 go into AGENTS.md.
  Inside its authority the agent acts on its recommendation and records the reason. When
  the owner reports a result, it is recorded as owner-reported PASS without asking.
- **2026-10-09, OD-5 = alternative 3.**
  - Generated prompts for primary sessions inherit standing policy.
  - Unattended runs skip always-ask work and list those questions at the checkpoint.
  - Worker subagents may merge and push to `main` by default, without a per-run grant.
  - Consequence to handle in OD-17, OD-18 and OD-23: worker pushes publish Previews mid-run
    (A4c-F5).
- **2026-10-09, OD-6 = yes.** AGENTS.md gets a precedence line over superpowers.
  - Plugin skills are optional tools, and repo rules win.
  - The spec-review, execution-mode and finish-branch prompts are pre-answered.
  - No `.superpowers` or `docs/superpowers` artifacts outlive the session.
- **2026-10-09, OD-7 = yes.** Phase 2 replaces superpowers with repo skills.
  - New repo skills: planning-and-variants (a `/brainstorming` equivalent with rendered
    mockups) and plan-execution.
  - Vendored at a pinned upstream commit: systematic-debugging,
    verification-before-completion and receiving-code-review.
  - Every superpowers skill the owner used is first mapped to its replacement.
  - Then superpowers is disabled for this repo only.
- **2026-10-09, OD-8 = alternative 2.** ux-superpowers, design-superpowers,
  desktop-commander and the Anthropic design plugin are disabled for this repo only, through
  the project settings.
  - The owner's account and other projects keep them.
  - Personal connectors are not changed.
  - Stage 2 must confirm the exact `enabledPlugins` key for the design plugin, whose install
    location was not found (B2-F8).
- **2026-10-09, OD-9 = alternative 2.** A minimal deny-only project
  `.claude/settings.json`.
  - Covered, in Bash and PowerShell forms: force-push variants, `gh release *`,
    `gh workflow run *`, and `Read`/`Edit` on source-CLI credential stores.
  - Not covered: reset, clean, branch deletion, stash, variables and secrets.
- **2026-10-09, OD-10 = yes.** One-week trial of auto permission mode, starting after the
  OD-9 deny rules land. Revert to bypass at the first false block of a routine action.
- **2026-10-09, OD-11 = owner variant.** The project stays agent-neutral, because
  non-Claude agents may also work on it.
  - Skills stay in `.agents/skills`; D-034's location clause stands and the validator is
    unchanged.
  - AGENTS.md stays the canonical, mandatory entry point.
  - CLAUDE.md gets explicit paths to each skill with a mandatory instruction to read it when
    its topic applies, so Claude behaves as if the skills were native. Stage 2 may instead
    `@import` the three small `SKILL.md` files (about 600 tokens) into CLAUDE.md if that is
    more reliable.
  - Consequences:
    - OD-7's new repo skills also live in `.agents/skills`; a Claude slash command, if
      needed, is a thin `.claude/` wrapper pointing to the canonical file.
    - OD-13 must be reframed: no "Claude Code only" decision.
- **2026-10-09, OD-12 = yes.** One generalized reviewer and one implementer agent are
  versioned in Git.
  - simple-implementer is merged into the implementer.
  - The contracts are canonical under `.agents/`, and `.claude/agents/*.md` are thin Claude
    wrappers.
  - Models are given as aliases.
- **2026-10-09, OD-13 = yes, reframed.** The agent-neutral policy stays; no "Claude Code
  only" decision is recorded.
  - Removed: retired-tool configuration (`.codex/` with the astra subagent, `.omp/`, the
    `.omp` ignore lines).
  - Kept: the thin `.github/copilot-instructions.md` adapter.
  - Finished and paused workflow records move to `docs/archive/` with corrected status
    headers.
  - The personal email and session IDs are redacted from environment.md and pending.md.
- **2026-10-09, OD-14 = yes.** T0-T3 tiers go into CONTRIBUTING.
  - T3 is set by the touched-area list, the highest trigger wins, and the reviewer checks
    the diff against the list.
  - Local `/code-review` for T1 and T2; `/security-review` inside T3 review.
  - Bug-fix tests must fail at the base commit.
  - Red-to-green is written into CONTRIBUTING.
- **2026-10-09, OD-15 = yes.** Review in parallel runs:
  - Code, test and harness tasks each get a per-task independent review.
  - Docs tasks get a primary check; rule and agent-config edits count as T3.
  - One whole-feature review always runs.
  - One review round, then a re-check of the fixed lines only.
- **2026-10-09, OD-16 = alternative 2.** The agent may start parallel worktree workers
  itself when there are at least 3 independent tasks with non-overlapping write sets,
  running at most about 4 at once.
  - Combined with OD-5, workers push to `main` mid-run, which publishes Previews mid-run
    (see OD-17 and OD-23).
- **2026-10-09, OD-17 = yes.** A Preview publishes unless every path changed since the last
  Preview is on the non-product allowlist (`docs/**`, `.claude/**`, `.agents/**`, root
  `*.md`).
  - The same "product inputs" definition drives the smoke gate.
  - The decision is logged in the job summary.
- **2026-10-09, OD-18 = yes.** No separate `record T-xx` commits. Evidence is recorded in
  verification.md once per wave or at feature end.
  - The "controller pushes once per wave" part is dropped, because OD-5 lets workers push.
- **2026-10-09, OD-19 = yes.** The R10 lean-records package:
  - a backlog with live items plus a one-line done index;
  - executed plans collapsed into a ledger with commits, review verdict, check IDs and the
    grant;
  - D-entries only for binding rules, with UI picks as one spec line;
  - "Amended by" back-pointers;
  - validator checks for D-ID uniqueness and AC coverage;
  - spec and verification skeletons.
- **2026-10-09, OD-20 = yes.** One "Pending owner checks" list, plus an owner-invoked
  `post-deploy-check` skill.
  - The skill reads the installed version and sanitized logs.
  - It records verdicts only and never reads credentials.
- **2026-10-09, OD-21 = yes.** Rules live only in the repo, for all agents.
  - Host facts are copied into a repo environment doc.
  - Memory duplicates are pruned once their repo copies exist, and the stale entry is fixed.
  - Memory keeps host facts and owner preferences.
- **2026-10-09, OD-22 = yes.** Worktree cleanup under R12's strict conditions.
  - A worktree is removed only when its branch is in `origin/main` after a fresh fetch, its
    status is clean including ignored evidence, and no stash refers to it. Never `--force`.
  - One-time cleanup of the 31 merged worktrees.
  - The desktop's 7-day inactive archive is enabled after verifying it keeps unmerged
    branches.
- **2026-10-09, OD-23 = yes.** A test-only per-run tray identity: only the identity value
  changes, inside the existing `DesktopTestEnvironment` switch, and the reviewer confirms the
  production path is unchanged. Smokes run against the Release unpackaged build.
- **2026-10-09, OD-24 = yes.** DiagnosticCrashTests keeps its 10 s bound, but counting
  starts from an explicit probe-ready signal, so Defender's first-run delay is excluded.
- **2026-10-09, OD-25 = yes.** The opt-in Sandbox UI audit suite (`Audit*.cs`) is frozen:
  marked `Explicit`, with no edits required on UI changes. The package, upgrade and feed
  smokes stay.
- **2026-10-09, OD-26 = yes.** The AIU-002 routing spike build step is removed from CI. The
  `spikes/windows/AIU-002-routing` folder stays.
- **2026-10-09, OD-27 = owner variant: trial both.** Two language servers, compared on the
  same task:
  - the `csharp-lsp` plugin with a pinned `csharp-ls`;
  - Microsoft `roslyn-language-server` (prerelease).

  Both are time-boxed. The write-up compares solution discovery, WinUI/XAML false
  positives, diagnostic latency and memory, and recommends one to keep. The other is removed.
- **2026-10-09, OD-28 = yes.** A half-day Stryker.NET spike on Core budget logic and
  parsers, with no CI gate and no threshold. The output is a report of weak test spots.
- **2026-10-09, OD-29 = yes.** A trial of the Microsoft `winapp ui` CLI, read-only commands
  only (inspect and screenshot), with telemetry opted out. It is used to debug smoke
  locators and to screenshot UI variants.
- **2026-10-09, OD-30 = yes.** At the final merge, the verbatim prompt's two placeholder
  mentions are wrapped in code spans, with a note that this is the only change.
  - The owner also asked to simplify the ID scheme; that becomes new decision OD-31.
- **2026-10-09, OD-31 = yes (new, owner-raised).** A simpler ID scheme:
  - Work items (features and bugs) become `T-nnn` and keep their numbers (AIU-055 →
    T-055).
  - Steps inside an item become `T-nnn.k`.
  - Decisions become `R-nnn` and keep their numbers (D-207 → R-207).
  - Branch placeholders become `T-NEW` / `R-NEW`.
  - Git history keeps the old prefixes, with the same numbers.

  Stage 2 work:
  - update the validator, formats.md, CONTRIBUTING (D-196 text), the backlog, spec folder
    names, the decision registers, docs references and source comments;
  - run the rename only when no unmerged worktree branches are in flight, or migrate them
    in the same pass;
  - keep the numbering script (R13) in step with the new scheme.

  Note: the report's recommendation labels R1-R18 are audit-internal and unrelated to
  decision IDs.
- **2026-10-09, OD-1 = yes.** The agent-may-do bundle is approved without further
  questions:
  - R9 rule consolidation, delivered with a rule-inventory diff that the owner reviews before
    the branch merges;
  - R13 numbering script, built for the OD-31 scheme;
  - R14;
  - R15, including the `/goal` trial;
  - R5 harness-only changes;
  - one `/doctor prompt-audit` run;
  - the Microsoft Learn MCP guidance line;
  - host facts in a repo doc.

  Removed from the bundle under OD-11's agent-neutral principle: the Claude-only
  path-scoped `.claude/rules/logging.md` trial. Logging policy stays in AGENTS.md and
  logging.md.

## Open owner decisions

None. All decisions OD-1 to OD-31 were answered on 2026-10-09 (see above). The report's
§8 recommendations are superseded where the answers above differ.

## Notes for later stages

- **Before the final merge into `main`:** apply OD-30, which wraps the prompt's two
  placeholder mentions in code spans, or the CI `--final` step fails.
- **At the start of every session:** run `git fetch origin` and merge `origin/main` into this
  branch, then re-check any finding whose evidence files changed.
- **Lane scratch data:** the parser scripts and intermediate data stay in the Stage 1
  session's scratchpad and are not in Git. Those files contain private transcript extracts.

## Stage 2 progress

- **2026-10-09 — session start.** Switched to `workflow-optimization` (no other worktree held
  it) and merged `origin/main` `3724a17` (merge `ae03f11`): backlog AIU-056 and a goals scope
  line, no rule file touched. `git branch -a --no-merged origin/main` showed no in-flight work
  branches besides this effort. The Stage 2 prompt is saved in `4878aa7`.
- **2026-10-09 — step 1, rule text (R9; OD-2..OD-6, OD-11, OD-14..OD-16, OD-18).**
  - `/doctor prompt-audit` ran once headless; its report is [prompt-audit.md](prompt-audit.md).
  - AGENTS.md re-composed: ask lists, plugin precedence, skill paths, Git flow summary,
    logging core with the secret-exclusion rule. CONTRIBUTING.md: tiers T0-T3 with the T3
    area list, red-to-green and "fails at base", the task-branch Git flow, blocked-smoke rule,
    worktree cleanup conditions, review calibration, parallel work. verification.md: check
    IDs C1-C10, new matrix rows, merge gate with "product inputs", desktop lock, restore
    one-liner (tested in a fresh worktree: restore, Release build and validator pass).
  - logging.md gained the Logging policy; the ANL-11 lesson moved to architecture.md;
    goals.md has a current direction paragraph; README's commands moved to verification.md;
    host facts copied to [host-environment.md](../host-environment.md).
  - Decisions D-NEW to D-NEW-8 added (Git flow, ask lists, tiers, plugin precedence, and four
    back-filled directions); D-178 and D-179 marked superseded.
  - Rule-inventory diff for the owner: [rule-inventory.md](rule-inventory.md).
  - Checks: C2 PASS; C6 PASS; C3 FAIL with only `PLACEHOLDER_ID` diagnostics (the two known
    stage-1-prompt mentions plus the new `D-NEW` records; see OD-32).
  - CLAUDE.md is unchanged in this step; its skill pointers come with the `.claude` wrappers
    in step 3.

- **2026-10-09 — step 2, project settings (OD-8, OD-9, OD-10).**
  - New committed `.claude/settings.json`: deny rules for force-push variants (`--force*`,
    `-f*`, `+refspec`, also inside compound commands), `gh release *` and `gh workflow run *`,
    each in Bash and PowerShell form; `Read`/`Edit` deny on `~/.codex/**`,
    `~/.claude/.credentials.json`, `~/.gemini/**`, `~/.config/github-copilot/**`,
    `~/AppData/Local/github-copilot/**` and `~/AppData/Roaming/GitHub CLI/hosts.yml`.
    `enabledPlugins` sets `ux-superpowers@synced`, `design-superpowers@synced`,
    `desktop-commander@synced` and `design@synced` to false. The design plugin's key is
    `design@synced` (B2-F8 resolved via `claude plugin list`).
  - AGENTS.md: "deny rules are a backstop; a missing deny rule is not permission".
    CONTRIBUTING.md, Agent permissions: what the file holds, T3 status, and the override for
    an owner-authorized provider-evidence read (`claude --setting-sources user,local` for that
    one session).
  - Checks: `claude plugin list` shows the four plugins disabled and superpowers loaded
    (PASS). A fresh headless session in bypass mode blocked all 11 deny probes (force-push
    forms, `--force-with-lease`, `+refspec`, a compound `cd . && git push --force`, PowerShell
    form, `gh release list`, `gh workflow run --help`, reads in three denied folders) and ran
    the ordinary `git push --dry-run` (PASS; dry runs only, nothing pushed). The exact-file
    and space-in-path rule forms were proven on missing files with a temporary `--settings`
    file (PASS). The running session does not reload new project settings: its own dry-run
    force push was not blocked, so the rules apply from the next session.
  - OD-10 auto-mode trial: NOT_RUN, owner action. Permission mode is a security setting the
    agent cannot change. To start: choose Auto as the permission mode for new sessions in the
    desktop app (Settings → Claude Code, or the session's mode picker) from 2026-10-10 to
    2026-10-17. Revert to bypass at the first false block of a routine push, merge or read,
    and note the date and the blocked action here.

- **2026-10-09 — step 2 review fixes.** The focused review (verdict: changes requested, 2
  Important, 5 Minor) found that the PowerShell patterns `git push * -f*` and `git push * +*`
  blocked routine pushes piped to `Select-Object -First` or followed by a string `+`, and that
  the existing `~/.copilot` store was not denied. Fixed: tighter PowerShell forms, `~/.copilot`,
  `~/AppData/Local/agy` and `~/AppData/Local/Antigravity` denied, `-uf`, `--mirror` and
  `git -C <path> push` forms added, and the CONTRIBUTING wording now says shell reads are not
  covered and the override session drops every project rule. Re-probe in fresh headless
  sessions: all force forms blocked except a PowerShell `+refspec` to a remote other than
  `origin` (accepted: `origin` is the only remote); routine pushes with pipes or a second
  command and `git push origin --delete <branch>` ran (PASS, dry runs against a missing remote).
- **2026-10-09 — step 1 review fixes.** The focused review (changes needed: 3 Important,
  5 Minor) found the package build dropped from the Windows UI row, "privilege" changes
  dropped from focused review, and a "verified" definition that contradicted OD-3. Fixed: C9
  is back in the row, "privilege and capability changes" is a T3 area, "verified" names the
  blocked-smoke exception, plus the minor fixes (`AIU_SMOKE_MODE` was never read by code and
  is removed, the interruption rule keeps "when writing is authorized", workers do not add
  backlog items or decisions). The rule inventory records each change.
- **2026-10-09 — step 3, agents and hygiene (OD-11, OD-12, OD-13, OD-21, OD-22).**
  - Agents: canonical contracts `.agents/agents/reviewer.md` and `implementer.md`; thin
    tracked wrappers `.claude/agents/aiu-reviewer.md` and `aiu-implementer.md` with model
    aliases (`opus`); `aiu-simple-implementer` is merged into the implementer.
    `.claude/agents/` was removed from the shared `.git/info/exclude` (backup in the session
    scratchpad); old worktrees now show their untracked local agent copies as `?? .claude/`.
  - Skills: sharper trigger descriptions in `.agents/skills/*/SKILL.md`; thin wrappers in
    `.claude/skills/` make them invocable by name; CLAUDE.md names the paths and says reading
    the matching skill is mandatory.
  - OD-13: `.codex/` and `.omp/` deleted, the `.omp` ignore lines removed, and the validator no
    longer scans `.omp/AGENTS.md` (one test case removed). The agent-neutral plan and
    verification, the architecture-audit plan, environment.md, omp-native.md and the paused
    ui-ux audit moved to `docs/archive/workflow/` with corrected status headers; live links
    were rewritten. The owner's e-mail address (environment.md, pending.md) and 14 native
    session IDs (environment.md) are redacted.
  - OD-21 (host action): memory entries ui-design-variants-first, state-push-status,
    post-deploy-owner-checks and numbers-at-merge deleted, because main's repo text already
    holds them; claude-code-only rewritten without the stale note; host-tooling-constraints
    points to the repo copy; no-repeated-approval-gates stays until this branch is on `main`.
    Undo: copies of all eight files are in
    `~/.claude/projects/C--Users-danii-projects-ai-usage-app/memory-archive/2026-10-09/`.
  - OD-22: no worktree qualifies under the strict conditions; see OD-33. Nothing was removed.
  - Checks: C1 PASS (83/83); C2 PASS; C6 PASS.

## New owner decisions (Stage 2)

Raised during Stage 2 for things no approved decision covers. Each has a recommendation;
work continues on the recommended path unless it is marked as waiting.

- **OD-32 — Placeholders under `--final` on this branch.** The prompt allows only the two
  stage-1-prompt diagnostics under `--final`, but D-196 (standing policy) requires new
  decisions on a branch to use placeholders until the merge, and step 1 adds `D-NEW` records.
  Assigning numbers now would collide with parallel work merged meanwhile. *Recommendation:*
  keep placeholders (converted to the OD-31 scheme in step 6) and number them with the R13
  script at the final merge; until then `--final` also lists these placeholders.
  Not waiting.

- **OD-33 — Worktree cleanup that the strict conditions block.** A fresh audit of the 33
  worktrees besides this one: all branches are ancestors of `origin/main` and no stash refers
  to them, but every worktree keeps ignored smoke evidence (`.ai-usage-local/`, up to 150 MB)
  or local settings, and 20 of them belong to unarchived desktop sessions, whose folders would
  disappear under them. *Recommendation:* (1) move each worktree's `.ai-usage-local/` into the
  main checkout's `.ai-usage-local/worktree-archive/<worktree>/`; (2) archive the 20
  session-owned worktrees through the desktop app (reversible with unarchive), after checking
  on the first one that archiving keeps its branch; (3) remove the 13 subagent worktrees with
  `git worktree remove` (no `--force`); (4) then enable the 7-day inactive archive. Waiting for
  the owner.
- **OD-34 — Archived nested instruction file.** `docs/archive/omp/.omp/AGENTS.md` says "OMP
  remains the canonical full workflow" and may load as a nested instruction file when an agent
  reads that folder (prompt-audit F7). The archive README promises byte-for-byte preservation.
  *Recommendation:* rename it to `AGENTS.md.txt` and add one line to the archive README
  recording the rename. Not waiting; until decided, AGENTS.md already limits archive reads to
  historical questions.
- **OD-35 — `R-` prefix collision.** OD-31 renames decisions `D-nnn` to `R-nnn`, but 17
  specifications already use `R-01`…`R-xx` for requirements, and the T1 spec skeleton keeps
  `R-xx`. Decision numbers have three digits and requirement numbers two, so they are
  distinguishable but easy to confuse. *Recommendation:* keep OD-31 as decided and have the
  validator accept only three-digit `R-nnn` decision IDs in the decision registers; or rename
  spec requirements to `Q-xx`. Step 6 proceeds with the first option unless the owner picks
  the second.

## Next action

Stage 2, step 4: CI — the OD-17 product-input Preview filter and the OD-26 routing-spike
removal, with a focused review (T3). Then step 5 tests and smoke
(OD-23..OD-25, R14, R5); step 6 records and IDs (OD-19, OD-31, R13; re-check in-flight branches
first); step 7 skills (OD-20, OD-7); step 8 trials (OD-27..OD-29). Steps 1-4 are T3 and each
gets a focused independent review.
