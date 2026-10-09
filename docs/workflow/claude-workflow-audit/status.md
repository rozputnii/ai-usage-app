# Claude workflow audit — status

Living log for the workflow-optimization effort. Read this first in every session.

- **Branch:** `workflow-optimization`, a long-lived branch the owner requested. It is never
  merged into `main` or opened as a PR without an explicit owner decision.
- **Current stage:** Stage 1 is complete, and all owner decisions OD-1 to OD-31 were taken
  on 2026-10-09. Stage 2 (implementation) is ready to start.

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

## Next action

Start Stage 2 in a new session on this branch, with a prompt that cites OD-1 to OD-31 as
recorded above. Suggested order:

1. **Rule text.** AGENTS.md, CONTRIBUTING and the CLAUDE.md skill pointers for OD-2..OD-6,
   OD-11, OD-14..OD-16 and OD-18, done as the R9 consolidation with its rule-inventory diff.
2. **Project settings.** OD-9 deny rules and OD-8 plugin disables; then start the OD-10
   one-week auto-mode trial.
3. **Agents and repo hygiene.** OD-12 agents; OD-13 cleanup; OD-21 memory and host facts.
4. **CI.** OD-17 Preview filter; OD-26 spike step removal.
5. **Tests and smoke.** OD-23, OD-24 and OD-25, plus the R14 and R5 bundle items.
6. **Records and IDs.** OD-19 records, OD-31 ID scheme and the R13 numbering script. Do this
   at a quiet point with no unmerged worktree branches.
7. **New skills.** OD-20 post-deploy skill; OD-7 phase 2 repo skills in `.agents/skills`,
   with thin Claude wrappers.
8. **Trials.** OD-27 (both language servers), OD-28 and OD-29, each with a written result.
