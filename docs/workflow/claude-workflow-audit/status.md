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
    field fix and R-207.
  - None of them touches AGENTS.md, CONTRIBUTING.md, `docs/workflow/verification.md`,
    `docs/workflow/formats.md`, `.claude`, `.agents` or `.github`, so no finding is
    invalidated.
  - R-207 is one more UI-preference decision, which supports A5-F9.
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
  - Skills stay in `.agents/skills`; R-034's location clause stands and the validator is
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
- **2026-10-09, OD-26 = yes.** The T-002 routing spike build step is removed from CI. The
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
  - Work items (features and bugs) become `T-nnn` and keep their numbers (T-055 →
    T-055).
  - Steps inside an item become `T-nnn.k`.
  - Decisions become `R-nnn` and keep their numbers (R-207 → R-207).
  - Branch placeholders become `T-NEW` / `R-NEW`.
  - Git history keeps the old prefixes, with the same numbers.

  Stage 2 work:
  - update the validator, formats.md, CONTRIBUTING (R-196 text), the backlog, spec folder
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
- **2026-10-09, OD-32 to OD-39 = recommendations accepted.** After the verification round the
  owner accepted OD-39 and took every other Stage 2 decision as recommended (see "New owner
  decisions (Stage 2)"). OD-10's trial start stays the owner's action: the agent cannot change
  the permission mode.

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
  it) and merged `origin/main` `3724a17` (merge `ae03f11`): backlog T-056 and a goals scope
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
  - Decisions R-NEW to R-NEW-8 added (Git flow, ask lists, tiers, plugin precedence, and four
    back-filled directions); R-178 and R-179 marked superseded.
  - Rule-inventory diff for the owner: [rule-inventory.md](rule-inventory.md).
  - Checks: C2 PASS; C6 PASS; C3 FAIL with only `PLACEHOLDER_ID` diagnostics (the two known
    stage-1-prompt mentions plus the new `R-NEW` records; see OD-32).
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
  sessions: all probed force forms blocked except a PowerShell `+refspec` to a remote other
  than `origin` (accepted: `origin` is the only remote); the verification round later found
  `git -C <path> push` with the flag right after `push` unprobed and open (fixed there); routine pushes with pipes or a second
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
    longer scans `.omp/AGENTS.md` (the two `.omp` test cases removed). The agent-neutral plan and
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

- **2026-10-09 — step 4, CI (OD-17, OD-26).**
  - `.github/workflows/validation.yml`: a new "Decide Preview publication" step in the
    `preview` job diffs the pushed commit against the source of the last published
    (non-draft) Preview and publishes only when `Test-PreviewInputsChanged` finds a product
    input; an owner dispatch always publishes; no ancestor Preview means publish. The decision
    and its reason go to the job summary. The signing and publishing step runs only when the
    decision is `publish`; the Pages steps already follow its `promote` output.
  - `tools/windows/PreviewRelease.psm1`: `Test-PreviewInputsChanged` (allowlist `docs/**`,
    `.claude/**`, `.agents/**`, root `*.md`; unknown paths publish).
  - OD-26: the T-002 routing-spike build step is removed; `spikes/windows/AIU-002-routing`
    stays.
  - README's Preview paragraph and R-NEW-9 (amends R-157, with an "Amended by" line there).
  - Checks: C10 red first (`Test-PreviewInputsChanged` not found), then PASS (80 assertions,
    Windows PowerShell 5.1; CI runs it under pwsh). The decision script, extracted from the
    workflow and run locally against the real release list with simulated push variables:
    `origin/main` → skip (0 paths), the step-1 commit `4206880` → skip (31 docs paths), this
    branch's HEAD → publish (PASS). The workflow YAML itself was not parsed by a YAML tool
    offline (NOT_RUN); CI first runs it after the merge into `main`. C2 PASS; C6 PASS.

- **2026-10-09 — step 3 review fixes.** The focused review (changes needed: 1 Important,
  5 Minor) found that the implementer's integration step named only "the brief's checks". Fixed:
  it now runs the merge gate on the merged tree including C3, and the primary integrates a
  worker that cannot be resumed. Also fixed: the reviewer deletes the added test before
  removing its temporary worktree; historical plain-text paths in the archived migration plan
  and the T-045 analysis and orchestration prompt are restored (only links were updated);
  live plain-text pointers in pending.md, superseded.md and the T-001 tasks now name the
  archive; the archived audit plan's header notes that its startup failure was fixed. C2 PASS.

- **2026-10-09 — step 4 review fixes.** The focused review (changes requested: 2 Important,
  3 Minor; it parsed the YAML with Bun's parser: PASS) found that `git diff` rename detection
  could hide a product file moved into `docs/`, and that the newest-release base let a re-run
  skip after a run failed between making its release public and updating the feed. Fixed: the
  base is now the source of the build the feed serves (`feed-preview`'s App Installer version →
  its `preview-*` release), `--no-renames`, case-sensitive folder allowlist, tests anchored to
  the publish step, and AGENTS/README/verification/R-NEW-9 say "since the served Preview".
  Checks: C10 PASS (85); the revised decision script against the real feed: `origin/main` skip
  (0 paths), `4206880` skip (31 docs paths), HEAD publish (PASS); C2 PASS; C6 PASS.

- **2026-10-09 — step 7, skills (OD-7, OD-20), done before steps 5-6 because it touches no
  code.**
  - Vendored from superpowers `v6.4.2` (commit `8ca22dba9a94f28898bbce59f2537ff4d87c747d`;
    every copied file's blob hash matched upstream before the documented local change): systematic-debugging (with its four
    technique files), verification-before-completion, receiving-code-review, plus the MIT
    licence and `.agents/skills/VENDORED.md`. Only change: two cross-references in
    systematic-debugging now point to repo equivalents.
  - New repo skills: planning-and-variants (the `/brainstorming` replacement, invocable as
    `/planning-and-variants`) and plan-execution; post-deploy-check (owner-invoked only,
    `disable-model-invocation` in its Claude wrapper). Each has a thin `.claude/skills/`
    wrapper; AGENTS.md lists them.
  - Backlog: one "Pending owner checks" table with eight open post-deploy checks
    (T-045, 048, 050-055).
  - Mapping of all 15 superpowers skills to replacements: [superpowers-mapping.md](superpowers-mapping.md).
    Then `"superpowers@synced": false` in `.claude/settings.json`. R-NEW-10 records the agent
    and skill layout and amends R-012 and R-034.
  - Checks: `claude plugin list` shows all five plugins disabled for the project (PASS); a
    fresh headless session lists the eight model-invocable repo skills and no `superpowers:`
    skill (PASS); C2 PASS; C6 PASS.

- **2026-10-09 — step 6a, lean records (OD-19), part 1.**
  - Validator, test-first (3 new tests red, then 86/86 PASS): Done index rows count as backlog
    items (status done or dropped, evidence required for done); a verification
    "## Acceptance results" table must cover every specification AC and use PASS, FAIL,
    NOT_RUN or BLOCKED (records without that heading are not checked, so history is not
    rewritten); decision IDs are unique; "Amended by" / "Superseded by" pointers must resolve.
  - Backlog: 35 done or dropped items became one Done index row each (11,434 → 3,719 words);
    21 live items keep their blocks. One link to a removed backlog anchor fixed.
  - Decisions: "Amended by" back-pointers on 17 earlier decisions (22 relations found by
    scanning "amends/replaces/supersedes" sentences; one false positive excluded).
  - formats.md: backlog, decisions and executed-plan rules, the spec and verification
    skeletons, and the new validator contract.
  - Not yet done: collapsing the 26 executed tasks.md plans into ledgers (next, as parallel
    docs tasks).
  - Checks: C1 PASS (86); C2 PASS; C6 PASS.

- **2026-10-09 — step 7 review fixes.** The focused review (changes requested: 2 Important,
  5 Minor) found a stale T-045 row in "Pending owner checks" (that smoke is Sandbox-only and
  was closed as not applicable) and that plan-execution required per-task review for sequential
  work too. Fixed both, plus: the owner-requested parallel path and the `status` field in
  plan-execution, precise provenance wording, the mapping's source and "one batch" wording,
  approach options with a recommendation when the owner asks to brainstorm, and the missing
  T-046 AC-06 owner check.

- **2026-10-09 — step 6a, lean records (OD-19), part 2.** Three parallel docs workers
  collapsed the 25 executed `tasks.md` plans of done items into an "Execution ledger" section
  of each verification record (one line per task: status, commits, review, check IDs, grant;
  facts found only in a plan kept under "Kept from the plan"; open next actions kept verbatim)
  and deleted the plans (4,621 lines removed, 322 added; Git history keeps them). Missing
  per-task commits, reviews or grants are written as "not recorded", never invented. Live or
  unfinished plans stay: T-046 (review), T-014 (paused), T-011 (dropped). Primary diff
  check: T-034, T-048 and T-055 ledgers read against their plans; C2 PASS.

- **2026-10-09 — re-check of the fixed lines (steps 1-4, 7).** One fresh reviewer re-checked
  only the fixes: all 10 Important findings RESOLVED (C10 PASS, 85). Five new Minor items:
  four fixed (PowerShell `push -u origin +ref`, `git -C <path> push ... -f` and
  `git -C <path> push origin +ref` forms added and re-probed: blocked, while routine pushes
  with a pipe or a second command still run; a malformed served feed now publishes instead of
  failing the step; the pending-check row order; three wrapped lines). Accepted: if a run fails
  after the feed upload but before the Pages deploy, a re-run skips and the install page stays
  one build behind until the next product push; App Installer updates are unaffected.

- **2026-10-09 — step 5, tests and smoke (OD-23, OD-24, OD-25, R14, R5).** Three parallel
  workers in worktrees from this branch (`stage2/s2-diag`, `s2-suite`, `s2-smoke`), each with a
  per-task review, merged here with `--no-ff`:
  - OD-24 (`01217c7`): the diagnostics probe prints `AIU_PROBE_READY` after its log is flushed;
    both DiagnosticCrashTests wait for it (120 s startup bound), then apply the unchanged 10 s
    and 20 s bounds. Red 2/2 against the old probe, green 2/2. Review: PASS, 2 Minor.
  - R14 (`4183444`, `24c481b`, `38b4e48`, plus `804096a`): the 235-case audit corpus runs in
    four parallel shard classes (917 tests, names identical before and after, export
    byte-identical; warm wall time 28.5 s → 17.4 s per the reviewer); xUnit1004 is an error in
    all four test projects (`.editorconfig`); `tools/windows/Test-Erosion.ps1` reports deleted
    tests, new skips (now including `Assert.Skip`), fewer asserts and changed fixtures, and
    exits 0. The 256-file cap test was not cheapened: that needs a product change (OD-36).
    Review: approve, 4 Minor.
  - OD-25, OD-23, R5 (`fc1bb2f`, `2ff314b`, `4f1daab`, plus a history fix): the `Audit*`
    suite is `Explicit` (35 tests, none run by default); an unpackaged app with a valid
    `AIU_SMOKE_TRAY_ID` names its tray icon `AI Usage <id>` and the harness matches that exact
    name (red: two "AI Usage" icons with the installed app running; green: one each); a
    re-find helper, failure screenshot plus UI-tree dump, and one host-wide result history
    `%LOCALAPPDATA%\AiUsage-smoke-history.csv`. Review: PASS, production path unchanged,
    3 Minor (two fixed: the history now survives worktree removal and marks dirty trees).
  - Worker friction to note for R15: a session guard refuses Edit/Write outside the session's
    own worktree, so workers had to switch into their worktree (EnterWorktree) or use shell
    edits.
  - Integrated checks on this branch: C4 917/917, C5 330/330, C1 86/86, C7 0 warnings, C8 3/3
    (Release build, desktop lock, tray identity), C10 85, C2 PASS, C6 PASS.
  - Found, not caused by this branch: five LedgerActivationSmoke cases
    (`FirstRunSignInButtonsRespondToMouseClicksWithSettingsOpen` ×4,
    `BusyStorageExplainsTheBlockAndRetryEnablesSignIn`) fail the same way at base `27a14b0`;
    see OD-37.

- **2026-10-09 — step 6b, ID scheme (OD-31) and numbering script (R13).**
  - Precondition checked after `git fetch`: `origin/main` was still `3724a17` (nothing to
    merge) and `git branch -a --no-merged origin/main` listed no in-flight work branches, only
    this effort's own merged `stage2/*` branches and the session's starting branch.
  - `tools/windows/Convert-IdScheme.ps1` (one-time, re-runnable) converted 193 files and
    renamed 38 spec folders: `AIU-nnn` → `T-nnn`, `T-xx` steps in a spec folder → `T-nnn.k`,
    `D-nnn` → `R-nnn`, `AIU-NEW`/`D-NEW` → `T-NEW`/`R-NEW`; Markdown in full, C#, PowerShell
    and YAML in comments only; XAML and manifest comments plus the theme generator's string
    by a follow-up pass. Kept on purpose: the archive, the Stage 1 audit inputs (report,
    lanes, both prompts, prompt-audit), `.ai-usage-local/AIU-nnn` evidence paths, the
    `spikes/windows/AIU-002-routing` folder and CI artifact paths.
  - Validator: item `T-nnn`, step `T-nnn.k`, decision `R-nnn` with three digits (OD-35
    interim), placeholders `T-NEW`/`R-NEW`; its tests converted (86/86 PASS).
  - `tools/windows/Set-ItemNumbers.ps1` (R13) numbers `T-NEW`/`R-NEW` from fresh `main`,
    skips code spans and fences, renames `T-NEW*` folders and runs `--final`;
    `tests/tools/Test-ItemNumbers.ps1` PASS (12). CONTRIBUTING, AGENTS, formats.md and the
    backlog intro describe the scheme; R-NEW-11 (scheme, amends R-196) and R-NEW-12 (lean
    records) added.
  - `--final` lists this branch's `R-NEW` placeholders (OD-32), which `Set-ItemNumbers.ps1`
    numbers at the merge, and the two known stage-1-prompt mentions (OD-30 still applies: the
    step 6 review made `--final` refuse leftover `AIU-NEW`/`D-NEW` placeholders too).
  - Checks: C1 86/86, C2 PASS, C4 917/917, C5 330/330, C7 0 warnings, UI test build
    0 warnings, C10 85, numbering test 12, C6 PASS.

- **2026-10-09 — step 8, trials (OD-27, OD-29; OD-28 running).** Results in
  [trials.md](trials.md): csharp-ls kept (projects discovered, cross-project references,
  XAML-generated members, diagnostics within 1 s, no WinUI false positives), roslyn
  prerelease removed (no project loaded); `winapp ui` read-only verbs useful (found a shared
  `SettingsButton` AutomationId). Host changes, each with its undo in trials.md: `csharp-ls`
  0.28.0 kept, `roslyn-language-server` installed then uninstalled, `winapp` 0.7.1 and the
  user variable `WINAPP_CLI_TELEMETRY_OPTOUT=1`, `dotnet-stryker` 5.0.0 (uninstalled after the
  OD-28 spike; see the verification round). R15: the Sandbox
  audit runner now writes a `STARTED` liveness marker, and verification.md says how to wait
  and when to stop (Sandbox run NOT_RUN: running it needs the owner, always-ask). The `/goal`
  trial is NOT_RUN until the next multi-part feature.

- **2026-10-09 — step 6 review fixes.** The focused review (changes requested: 4 Important,
  5 Minor; all 35 Done index rows and every added back-pointer verified) found that the
  migration gave other items' steps the folder's item (for example "T-045 T-039.3" for
  AIU-045 T-03), rewrote real paths and a branch name (`spikes/windows/AIU-002-routing`,
  `.ai-usage-local\AIU-...` with a backslash, `feature/AIU-002-...`), left two `AIU-nnn` /
  `D-nnn` forms in formats.md, and let leftover `AIU-NEW`/`D-NEW` pass `--final`.
  Partly fixed at the source (the verification round found 16 step references this rule gave
  the wrong item, fixed by hand there): `Convert-IdScheme.ps1` now takes a step's item from the last item named
  earlier on the same line, converts lowercase heading anchors, and excludes those paths and
  branch names; re-run on a pre-rename copy, its output replaced 18 migration-only files, and
  a re-run on the converted tree changes nothing. The validator refuses legacy placeholders
  under `--final` and legacy `## AIU-`/`### D-` headings, and checks both decision registers
  (3 new tests, 89/89). `Set-ItemNumbers.ps1` fetches first, uses one code-skipping routine for
  finding and replacing (CRLF-safe), never writes an empty replacement, refuses an unmatched
  `T-NEW` folder and moves untracked folders (test 15/15, covering suffix order 2 < 9 < 10, a
  maximum taken from a spec folder and `T-NEW-1` = `T-NEW`). Also: "Amended by R-181" on
  R-116, R-118 and R-122; formats.md names where the collapsed plans live in Git history. A
  slug check finds no broken heading link except one that predates this branch (T-048).
  Not done (Minor): tying a step ID `T-nnn.k` to its owning item in the validator.

- **2026-10-09 — OD-28 Stryker spike.** Results in [trials.md](trials.md): mutation score
  4.45 % on Core budget code (33 killed, 708 survived, 259 min), a setup artifact rather than a
  test weakness: every kill came from one load-sensitive diagnostic test and no budget test
  saw an active mutant. Not adopted; the parsers run is skipped; `dotnet-stryker` uninstalled
  and the throwaway worktree `zz-lsp` removed.

- **2026-10-09 — verification round (owner-requested).** Because Stage 2 ran in one long
  session, the owner asked for parallel fresh-context verification of the whole stage.
  - Six read-only lanes (rules, agent config, CI, tests, records, checks and process) plus
    the whole-branch review: 0 Critical, 15 Important (two found by two lanes each), 30 Minor. Confirmed done:
    24 of OD-1..OD-31; the process rules (prompt in the first commit, merges never rebases,
    branch pushed, `main` untouched, no PR); all 56 backlog items and 25 ledgers kept.
  - Corrected claims of this log: the step 2 deny rules left `git -C <path> push <flag>` open;
    the step 6 converter fix gave 16 step references the wrong item; `dotnet-stryker` had no
    undo; C9 was never recorded; C4 and C8 each failed once on a rerun under Stryker load.
  - Fixed by three parallel workers and integrated (fa9a657, 1552ba3, f0a2445), then 5be6537,
    1f49d6d, 6fe8e3a: `git -C` force forms and `~/.omp` denied (live headless probes: 8 force
    forms per shell reached git before, all denied after; routine pushes still run); the
    parallel-run wording aligned with OD-5/OD-15/OD-16 (whole-feature review after the tasks
    land, workers branch from `main`, C8 before a worker's own push); the Sandbox audit runner
    passes `-explicit on`; the Preview decision moved into `Get-PreviewPublicationDecision`,
    failing open on every lookup, diff or import error (C10 85 → 101); R-NEW-13 records OD-26
    and amends R-154; the 16 step references, 10 half-converted ranges and 9 archive links
    fixed by hand; the validator accepts `T-NEW` Done rows and dated "owner-reported" verdicts,
    resolves pointers across both registers and reports malformed Done rows (C1 89 → 98);
    `Set-ItemNumbers.ps1` covers `.claude/` and `.github/`, code-span folder paths and BOMs
    (numbering test 15 → 25); `Test-Erosion.ps1` reports moved tests; the rule inventory
    corrected and extended.
  - Re-check of the fixed lines (OD-15): approve, 0 Critical, 0 Important; three of its five
    Minor fixed (dated verdict, parallel-run exception, worker worktree command). Left as
    residue: a few rare force-push spellings (`push -vf`, PowerShell `-fu` after the refspec)
    stay outside the backstop, which AGENTS.md says is not the authority.
  - Checks at 6fe8e3a in a clean worktree, all on the first run: C1 98/98, C2 PASS, C4
    917/917, C5 330/330, C6 PASS, C7 0 warnings, C8 3/3, C9 PASS (unsigned MSIX), C10 22 + 101,
    numbering 25. C3 lists only placeholders: the two stage-1-prompt mentions (OD-30) and this
    branch's `R-NEW`…`R-NEW-13` (OD-32), all inside the files `Set-ItemNumbers.ps1` numbers.
  - Host: the stale `D-NEW-2` in the memory index now reads `R-NEW-2`.

## New owner decisions (Stage 2)

Raised during Stage 2 for things no approved decision covers. Each has a recommendation;
work continues on the recommended path unless it is marked as waiting.

- **OD-32 — Placeholders under `--final` on this branch.** The prompt allows only the two
  stage-1-prompt diagnostics under `--final`, but R-196 (standing policy) requires new
  decisions on a branch to use placeholders until the merge, and step 1 adds `R-NEW` records.
  Assigning numbers now would collide with parallel work merged meanwhile. *Recommendation:*
  keep placeholders (converted to the OD-31 scheme in step 6) and number them with the R13
  script at the final merge; until then `--final` also lists these placeholders.
  Accepted 2026-10-09.

- **OD-33 — Worktree cleanup that the strict conditions block.** A fresh audit of the 33
  worktrees besides this one: all branches are ancestors of `origin/main` and no stash refers
  to them, but every worktree keeps ignored smoke evidence (`.ai-usage-local/`, up to 150 MB)
  or local settings, and 20 of them belong to unarchived desktop sessions, whose folders would
  disappear under them. *Recommendation:* (1) move each worktree's `.ai-usage-local/` into the
  main checkout's `.ai-usage-local/worktree-archive/<worktree>/`; (2) archive the 20
  session-owned worktrees through the desktop app (reversible with unarchive), after checking
  on the first one that archiving keeps its branch; (3) remove the 13 subagent worktrees with
  `git worktree remove` (no `--force`); (4) then enable the 7-day inactive archive.
  Accepted 2026-10-09; carried out on a fresh audit (see the Stage 2 log).
- **OD-34 — Archived nested instruction file.** `docs/archive/omp/.omp/AGENTS.md` says "OMP
  remains the canonical full workflow" and may load as a nested instruction file when an agent
  reads that folder (prompt-audit F7). The archive README promises byte-for-byte preservation.
  *Recommendation:* rename it to `AGENTS.md.txt` and add one line to the archive README
  recording the rename. Accepted and done 2026-10-09.
- **OD-35 — `R-` prefix collision.** OD-31 renames decisions `D-nnn` to `R-nnn`, but 17
  specifications already use `R-01`…`R-xx` for requirements, and the T1 spec skeleton keeps
  `R-xx`. Decision numbers have three digits and requirement numbers two, so they are
  distinguishable but easy to confuse. *Recommendation:* keep OD-31 as decided and have the
  validator accept only three-digit `R-nnn` decision IDs in the decision registers; or rename
  spec requirements to `Q-xx`. Accepted 2026-10-09: the first option, which the validator
  already enforces.

- **OD-36 — Cheaper store-growth cap test needs a product change.** R14 asked to make
  `LocalBudgetStoreTests.ExcessSeriesCannotGrowTheOwnedStoreIndefinitely` (about 13 s, now the
  Infrastructure suite's critical path) cheaper without weakening it. The worker measured that
  about 9.7 s of 12.5 s is the per-write capacity check in `BudgetJsonFile.EnsureCapacity`,
  which re-checks every ancestor directory of every existing file for reparse points (about
  33k checks for 256 files). Checking the directory once and each file only would likely bring
  the test to 1-2 s and also speed real writes, but it is product code on the reparse-point
  safety boundary (T3, security-lifecycle review). *Recommendation:* approve it as a separate
  T3 item after this branch merges. Until then the test stays unchanged. Accepted 2026-10-09:
  registered as backlog item `T-NEW` (cheaper store-growth cap check).

- **OD-37 — Five activation smokes already fail on `main`.** See step 5. *Recommendation:*
  register a backlog bug item for them (owner selection decides when it is fixed); until then a
  change that needs these smokes records them FAIL with this note. Accepted 2026-10-09:
  registered as backlog item `T-NEW-2`, which now holds that note.

- **OD-38 — Enable the C# language server for this project.** The OD-27 trial recommends
  csharp-ls (see [trials.md](trials.md)). Enabling `csharp-lsp@claude-plugins-official` in the
  committed `.claude/settings.json` gives every session diagnostics after edits, but needs
  `csharp-ls` installed on each machine (a session without it just has no LSP) and is an
  agent-configuration change (T3). *Recommendation:* enable it at project scope and keep the
  pinned tool in the host environment notes. Accepted and done 2026-10-09: installed with
  `claude plugin install csharp-lsp@claude-plugins-official --scope project` (1.0.0), which
  wrote the switch into `.claude/settings.json`; host-environment.md records the tool and undo.

- **OD-39 — Tray test identity uses its own variable.** OD-23 names the existing
  `DesktopTestEnvironment` switch; the implementation reads a separate `AIU_SMOKE_TRAY_ID`
  (8 hex characters) and only when the app runs unpackaged, so a packaged or released build
  never changes its tooltip. The verification round found the behaviour sound but the
  mechanism different from the decision text. *Recommendation:* accept the variable as the
  OD-23 implementation (`DesktopTestEnvironment` is a test-side class the app does not read).
  Accepted 2026-10-09.

## Next action

Stage 2 is implemented and verified; the branch waits for the owner. Owner: review
[rule-inventory.md](rule-inventory.md) and decide OD-10 (start the auto-mode trial),
OD-33, OD-34, OD-35, OD-36, OD-37, OD-38 and OD-39, then say whether to merge.

At the merge, in order: merge fresh `origin/main`; re-run `tools/windows/Convert-IdScheme.ps1`
for items `main` added and review its step references; wrap the two stage-1-prompt
placeholder mentions in code spans (OD-30); run `tools/windows/Set-ItemNumbers.ps1` and C3;
delete `Convert-IdScheme.ps1`; run the merge gate; after the merge, prune the
no-repeated-approval-gates memory entry, back up and remove the untracked old
`.claude/agents/aiu-*.md` in the main checkout (they block `git pull` there and load the
removed simple implementer), remove the `s2-smoke` worktree and its branch, and watch the
first CI run of the new Preview decision step.
