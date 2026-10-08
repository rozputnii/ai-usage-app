# Lane A4b — Session transcripts (from 2026-10-04, main folder)

## Scope and method

- **Files (fact).** 68 `*.jsonl` files under `~/.claude/projects/C--Users-danii-projects-ai-usage-app/` with last-write date on or after 2026-10-04. Of these, 2 are workflow journals (`journal.jsonl`) and 66 are transcripts: 14 top-level session files, 46 subagent transcripts (`<session>/subagents/agent-*.jsonl` with `.meta.json`), and 6 Workflow-tool agent transcripts (`<session>/subagents/workflows/wf_*/`).
- **Logical sessions (fact).** 13 main sessions. Two pairs share one kickoff: `9f0d1b6a` is an empty `/goal` stub of `d8af1475`, and `e3b3b623`/`8f275ad7` begin with the same owner prompt at the same minute, which is probably a fork or rewind. `c049f1a8` (design session, 2026-10-02) and `15ece2a2` (2026-09-25) are in scope only because their files were written later. Their owner messages predate 2026-10-04.
- **Parser (fact).** `work/A4b/parse.py` produces per-file metrics: owner turns, interrupt markers, AskUserQuestion, Agent/SendMessage/Skill calls, tool errors grouped by category, edit hotspots, command classes, gaps longer than 10 minutes, `usage` sums, and `cost-state.totalCostUSD`. `work/A4b/timeline.py` prints a per-transcript tool timeline. Owner messages are user entries that are neither tool results, `isMeta`, nor system or command wrappers. Five slash-command kickoffs (`/superpowers:brainstorming`, `/goal`) were added back by hand. I cross-checked against `queue-operation enqueue` entries, which match one-to-one.
- **Classification (inference, single coder).** I read about 50 owner messages and the agent turn that came before each one. I sorted them into categories and judged each question against AGENTS.md and CONTRIBUTING.md. The report has no verbatim private content. Owner wording appears only as short paraphrases.
- **Caveats.** Timestamps are UTC. Output-token counts in subagent transcripts look truncated, so cache-read tokens are the better effort proxy. I could not confirm whether `cost-state` includes subagent spend. Permission mode was `auto` until about 2026-10-06 and `bypassPermissions` after that, so classic permission prompts are rare by construction.

## Metrics table

Columns: owner turns are logical, kickoff included. Questions are AskUserQuestion calls (number of questions in brackets) plus agent turns ending in a question or "say X and I'll…". Denials are classifier or permission denials in the main session; subagent harness blocks are covered in F5. Subagents counts subagent transcripts. Max gap is the longest pause in minutes, with its cause.

| Session (date UTC) | Topic (paraphrase) | Owner turns | Interrupts | Questions (AUQ + text) | Denials | Subagents | Max gap (min, cause) | Cost USD |
|---|---|---|---|---|---|---|---|---|
| c049f1a8 (10-02, file 10-05) | Design rework in Claude Design | 13 | 0 | 0 + 10 | 0 | 0 | 20 (owner) | 30.9 |
| 5194e6a9 (10-05) | Brainstorming: dev-state analysis | 1 | 0 | 0 + 1 | 0 | 0 | — | n/a |
| d8af1475 (+9f0d1b6a) (10-05) | /goal read-only health analysis | 5 | 0 | 0 + 3 | 1 | 6 (Workflow) | 170 (owner) | 52.7 |
| f130201e (10-05/06) | Analysis triage → orchestration prompt | 5 | 0 | 0 + 4 | 0 | 1 | 340 (owner) | 13.2 |
| 3b229838 (10-06) | AIU-045 multi-agent orchestration | 8 | 0 | 4 (11) + 2 | 0 | 32 | 118 (waiting on AUQ answer); plus usage-limit stalls of 157 and 23 | 197.5 |
| e3b3b623 / 8f275ad7 (10-06) | Hover-icon blinking fix (fork) | 2 + 2 | 1 | 0 | 0 | 0 | — | 2.3 |
| e18a5c80 (10-06/07) | AIU-046 subagent-driven development | 4 | 0 | 0 + 2 | 0 | 13 | 1404 (agent blocked on merge or live check) | 27.8 |
| e900135c (10-06/07) | Agent-settings audit question | 2 | 1 | 0 + 1 | 0 | 0 | 985 (owner) | 0.4 |
| 15d0a9f2 (10-07) | Sign-in strip fix (brainstorming kickoff) | 2 | 0 | 0 + 1 | 0 | 0 | — | 2.3 |
| f7354699 (10-07) | Brainstorming kickoff, interrupted at once | 1 | 1 | 0 | 0 | 0 | — | 0 |
| 009ee03c (10-07/08) | Highlight colour logic (brainstorming kickoff) | 5 | 0 | 0 + 3 | 0 | 0 | 573 (owner, overnight) | 4.3 |
| 15ece2a2 (09-25, file 10-06) | "Where did we stop?" | 1 | 0 | 0 | 0 | 0 | — | n/a |
| **Total** | | **≈51** | **3** | **4 AUQ (11 q) + ≈27** | **1** | **52** | | **≈331** |

**Owner-turn categories (inference, about 51 messages).**

| Category | Count |
|---|---|
| Kickoffs or new tasks | 12 |
| Design feedback and answers to legitimate design or variant questions | 14 |
| New direction or ideas | 5 |
| Routine approvals or delegations ("yes", "do 1–2", "do it yourself", "I accept all your decisions") | 7 |
| Process corrections or complaints | 6 |
| Status or push checks ("merged and pushed?", "where did we stop?") | 3 |
| Owner-supplied environment or test results (desktop unlocked, origin hash, installer error, installed OK) | 4 |
| Auto-continue after usage limit | 3 |

## Findings

### A4b-F1 — Settled Git/verification policy re-asked or blocked by task prompts that narrowed authority
- **Observation.** Four sessions show the owner correcting work that stopped short of the standing CONTRIBUTING policy:
  - `d8af1475` asked for permission to commit and push before starting T-01. The owner answered, in paraphrase, "that permission is already at repo level, why ask?".
  - `8f275ad7` ended with nothing committed and changes left on `main`. The owner replied "push!".
  - `e18a5c80` finished AIU-046 code and review at 17:40, then asked the owner to sign in to the development build or waive the live check, and to approve the merge. It waited **1,404 minutes (about 23 hours)** until the owner asked why the branch was not merged. The owner then dictated the post-deploy-check rule that became D-190.
  - In `e18a5c80` and `d8af1475` the agent was obeying its kickoff prompt. One prompt said not to merge or push without explicit permission. The other was framed as a read-only analysis session. Both narrowed standing policy, which the owner did not intend.
- **Evidence.** `d8af1475` 10-05 (3 turn-ending asks, 1 correction). `8f275ad7` 10-06 (1 correction). `e18a5c80` 10-06/07 (2 asks, 2 corrections, 1,404-minute stall). The AIU-046 number collided at merge and was renumbered to AIU-048, which led to the D-196 placeholder rule.
- **Label.** Fact for the events. Inference that the prompt templates were the root cause.
- **Confidence.** High.
- **Impact.** Quality: neutral. Speed: high. The 23-hour stall alone exceeds the active time of most sessions, and 4 owner turns were spent on settled matters.
- **Proposed direction.** Generated kickoff and handoff prompts should not restate or narrow Git, merge or verification policy unless the owner asked for that in the current request. Add one line to the prompt templates (or AGENTS.md): "Standing CONTRIBUTING/D-190/D-196 policy applies; a prompt narrows it only when it says the owner asked." D-190 and the 2026-10-08 push-status rule already cover the symptoms. This change targets the cause.
- **KEEP.** The agent's honesty about why it stopped (it quoted its prompt constraint), and recording the new rule in AGENTS.md plus memory immediately.

### A4b-F2 — About half of turn-ending questions were decidable by the agent
- **Observation.** I counted about 27 turns that ended with a question or a "say X and I'll do it" handoff. About 13 of them were avoidable under CONTRIBUTING §Development 3 ("ask only for missing decisions affecting scope, product intent, …"):
  - Ceremonial review gates: "write *yes* and I'll mark your review PASS in verification.md", 4 times in `c049f1a8`. The owner never answered these; they replied with more design feedback.
  - A second "confirm this design?" after the owner had already picked variant A (`009ee03c`).
  - "Say *start T-01*" and "if you say so I'll do steps 1–2" (`f130201e`).
  - A batch of nine recommended decisions D1–D9. The owner first asked to see them in short form, then delegated all of them ("I accept all your decisions", `f130201e`).
  - Offering the owner to flip a GitHub variable themselves or have the agent do it (`d8af1475`). The owner said "do it yourself".
  - Commit, push and merge asks (see F1).
- **Legitimate asks (kept).** UI variant choices (owner rule of 2026-10-07), workflow dispatch or Preview publication (explicit-authority rule), destructive cleanup lists, and log-read consent for an installed app.
- **Evidence.** Per-session counts are in the table. Fully legitimate: `c049f1a8` design questions (6 of 10), `009ee03c` variant choices (2 of 3).
- **Label.** Inference. Judged by me against AGENTS.md and CONTRIBUTING.md.
- **Confidence.** Medium.
- **Impact.** Quality: low. Speed: medium to high. Each avoidable ask costs an owner round trip, and in unattended runs it costs hours (F3).
- **Proposed direction.** Add a "recommendation = default" rule. When the agent has a recommendation and the decision sits inside approved scope and authority, it acts and records the decision with its reason. It does not ask. Batch only true owner-authority items into one question set. Remove "mark your review PASS" gates: owner review of a design is recorded when the owner gives it, not requested.
- **KEEP.** Option lists with an explicit recommendation and one-word answers ("A", "yes"). The owner uses them efficiently.

### A4b-F3 — AskUserQuestion and kickoff questions stalled "run without stopping" orchestration; a checkable precondition was asked instead of verified
- **Observation.** The AIU-045 owner prompt said to ask the kickoff questions once and then run to the checkpoint without stopping.
  - The 4 kickoff questions blocked the run for about **85 minutes** (asked 00:26, work resumed 01:51).
  - A mid-run batch of 4 questions waited **118 minutes**. In between, the owner replied, in paraphrase, "can you fix it? and finish with the last questions".
  - One kickoff question was whether the desktop would stay unlocked. A reviewer later found that the lock screen had owned the desktop since the previous evening, before kickoff. The desktop lane (Task J) polled the lock state for about 1 hour, reported BLOCKED, and had to be rerun after the owner's "desktop unlocked".
- **Evidence.** `3b229838` 10-06: AUQ calls at 00:26, 11:08, 11:45 and 14:19 (11 questions). The gaps of 78.5 and 117.3 minutes both end in a tool_result. Task J (`agent-ab7ac967`) polled from 05:24 to 06:32 and was rerun at 08:31.
- **Label.** Fact for the timeline. Inference that the stalls were avoidable.
- **Confidence.** High.
- **Impact.** Quality: low. Speed: high (about 3.5 hours of wall time in one run).
- **Proposed direction.** Kickoff preflight should *verify* machine state the agent can observe (lock state via LogonUI/LockApp, origin/main, CI variable) instead of asking. Kickoff questions should cover only authority items and should carry stated defaults. For unattended runs, continue independent work while a question is pending, and put non-blocking asks into the checkpoint report.
- **KEEP.** Serialising the desktop lane and saying honestly that it was BLOCKED rather than faking a PASS.

### A4b-F4 — Custom subagent types were not available in the session that dispatched them
- **Observation.** The first 8 Agent dispatches of the AIU-045 run failed because the `aiu-implementer`, `aiu-simple-implementer` and `aiu-reviewer` types were not found. The primary re-dispatched all 8 as `general-purpose` with the role contract pasted inline. After the usage-limit resume, the custom types worked (24 later dispatches).
- **Evidence.** `3b229838` 10-06 01:58–02:02: 8 `Agent|not_found` errors, then 8 general-purpose re-dispatches. The definitions were authored in `f130201e` the evening before. Auto-memory already records ".claude/agents not loaded mid-session".
- **Label.** Fact.
- **Confidence.** High.
- **Impact.** Quality: low (role contracts were inlined). Speed: low (a few minutes, 8 wasted calls).
- **Proposed direction.** When a session creates or changes agent definitions, the run that uses them starts in a fresh session. The orchestration preflight checks the agent list once and falls back explicitly.
- **KEEP.** The fallback of inlining the role contract worked well.

### A4b-F5 — Orchestration briefs conflicted with worktree isolation and harness guards
- **Observation.** Isolated write workers kept trying to write their reports into the main checkout's `.ai-usage-local/AIU-045/run-…/reports/` path, which the worktree-isolation guard blocks. The pattern recurs because the brief prescribed that path.
  - **27 isolation-guard blocks** across 13 subagents (14 Write, 13 Bash).
  - 5 false-positive `Remove-Item on system path` blocks (regex or variable text parsed as a path).
  - 4 blocked `sleep N; cat` polling commands (3 in AIU-045, 1 in AIU-046).
  - 1 "subagents should return findings as text" block.
  - Separately, Fixer I was dispatched **without** isolation, edited a file in the main checkout, broke the merged-main build, and needed a correction message.
- **Evidence.** `3b229838` subagents: `work/A4b/errors.json` categories. Main session 10-06 05:05 "stray edit in main checkout restored".
- **Label.** Fact.
- **Confidence.** High.
- **Impact.** Quality: medium (the stray edit could have contaminated a gate). Speed: low to medium (about 40 wasted tool calls plus a recovery cycle).
- **Proposed direction.** The brief template says: reports are returned as the final message (or written inside the worker's own worktree), every write worker is isolated, and waiting uses `Monitor` or `run_in_background` instead of `sleep`. Put this in the `aiu-*` agent definitions, not in each prompt.
- **KEEP.** The primary checking the actual diff and catching the stray edit before pushing.

### A4b-F6 — Desktop smoke and flaky-test work dominated cost through fix-after-fix loops
- **Observation.**
  - **Task J** (candidate desktop verification): 227 turns, 8.4 hours wall time, about 62M cache-read tokens. That is about 30% of all AIU-045 subagent tokens. Within about 35 minutes (08:34–09:07), `LedgerSmoke.cs` was edited **31 times** across the task with roughly 10 consecutive failing smoke reruns, and there were 44 smoke invocations in total. The fixes were legitimate (waiting on observable conditions, confirmed by a reviewer), but the loop was a blind edit-run cycle against a slow desktop harness.
  - **Task D** (flaky diagnostics tests): 128 turns, 3.7 hours, 15 sleep-based waits. It concluded with an *environmental* cause (Defender first-run latency) and no code change, and the re-review only partly supported the attribution.
  - This range shows no AIU-055 T-09/T-10 transcripts; they are in lane A4c.
- **Evidence.** `3b229838/agent-ab7ac967` and `agent-a60610ef`. Edit hotspot lists are in `work/A4b/metrics.json`.
- **Label.** Fact for the counts. Inference for "blind loop".
- **Confidence.** Medium to high.
- **Impact.** Quality: medium (the smoke was fixed correctly in the end). Speed: high, and it was the main cost driver.
- **Proposed direction.** For UI-automation failures: capture the UI tree or state on the first failure and change one hypothesis per run (the systematic-debugging skill was loaded but the loop still happened). Time-box at N failed reruns, then write a diagnosis note and return. Keep a fast inner loop: run the single failing smoke case, not the full batch.
- **KEEP.** The reviewer check that test fixes wait on real conditions with unchanged bounds and weaken nothing.

### A4b-F7 — Per-task independent review exceeded the CONTRIBUTING review rule; whole-run review delivered the decisive catch
- **Observation.**
  - **AIU-045:** 32 subagents for about 12 tasks, of which **17 were reviews or re-reviews** and 15 were implementers or fixers. Most per-task reviews returned "clean, Minor only". That includes docs-only tasks (G, K, M, R2, N), which CONTRIBUTING would cover with a primary diff review. The single Important finding came from the **whole-run convergence review L**, and it was the primary's own wave-0 status error (AIU-014 flipped to in-progress).
  - **AIU-046:** 13 subagents for 4 tasks, of which 8 were reviews or re-reviews. Here reviews did find real defects: T-03 cell order and rounding, T-04 status honesty, and 2 Important issues in the whole-branch review.
  - The per-task two-stage review cadence came from the superpowers subagent-driven-development skill and the agent-generated orchestration prompt. CONTRIBUTING requires independent review only for material credential, destructive or privilege changes, release approval, or an explicit request, "without an automatic full-review loop".
- **Evidence.** `3b229838` (Agent call list), `e18a5c80` (Agent call list).
- **Label.** Fact for the counts. Inference for value per review.
- **Confidence.** Medium.
- **Impact.** Quality: positive for code-task and whole-branch reviews, neutral for docs-task reviews. Speed and cost: medium (each review adds 0.6–12.8M cache-read tokens and serial latency).
- **Proposed direction.** In multi-agent runs, require an independent per-task review only for code, test or harness tasks. Docs and record tasks get the primary diff check. Always keep one whole-branch or whole-run review before the final merge. Use cheaper models for re-reviews; AIU-046 already used Sonnet for those.
- **KEEP.** The whole-run or whole-branch convergence review, and scoped re-reviews after fixes instead of full re-review loops.

### A4b-F8 — The AIU-045 run hit session usage limits; token spend concentrated in one run
- **Observation.** `3b229838` hit the session limit twice. The first hit (02:20→04:57, about 2.6 hours) cut implementer D mid-run. The second came at 09:37→10:00. Recorded session cost was **$197.5** (about 60% of the ≈$331 summed over main sessions in range). Main plus subagents read about 310M cache tokens. The main orchestrator emitted a status message plus a stop-hook cycle after every notification (about 40 "waiting on X" turns).
- **Evidence.** `3b229838`: `cost-state`, usage sums, "You've hit your session limit" turns.
- **Label.** Fact for the numbers. Uncertain whether `cost-state` includes subagent spend.
- **Confidence.** Medium.
- **Impact.** Speed: high (2.6 hours lost and a mid-task cut). Quality: low.
- **Proposed direction.** Scale large runs to the subscription window: fewer review agents (F7), Sonnet for records-only tasks, no extra status turns when nothing changed, and split runs at natural gates.
- **KEEP.** Resuming the interrupted subagent through `SendMessage` with its context, instead of re-dispatching it.

### A4b-F9 — Oversized process artifacts pushed decision work back to the owner
- **Observation.**
  - The read-only health analysis produced an **80 KB** analysis record, using 6 Workflow agents and about 82M cache-read tokens.
  - The next session opened with the owner asking what to do with that document, and ended with the owner delegating all nine D1–D9 decisions.
  - The orchestration prompt generated from it was **48 KB**, too large to read in one call at kickoff. AIU-045 `verification.md` is 45 KB.
  - The 5194e6a9 brainstorming analysis was a single 14K-character reply.
- **Evidence.** `d8af1475`, `f130201e`, `3b229838` 10-05/06. File sizes come from the current worktree.
- **Label.** Fact for the sizes. Inference for causality.
- **Confidence.** Medium.
- **Impact.** Quality: neutral to slightly positive (thorough). Speed: medium (owner turns, plus context load in every later session).
- **Proposed direction.** Analyses lead with a decision table of at most about 1 page, with defaults applied unless the owner objects. Evidence stays in `.ai-usage-local`. Orchestration prompts are kept short and point to task files instead of embedding them.
- **KEEP.** Read-only analysis before a remediation run. It found real CI and publication issues that were then fixed.

### A4b-F10 — Superpowers plugin injections: mostly benign, but they add cadence and artifacts that diverge from repo policy
- **Observation.**
  - The superpowers SessionStart hook injected `using-superpowers` context into **all 13** main sessions.
  - The owner personally started 4 sessions with `/superpowers:brainstorming`. In those sessions the skill produced the variant questions the owner wants, plus an extra "confirm design" gate (`009ee03c`).
  - At AIU-045 kickoff the agent loaded **5 skills at once** (SDD, dispatching, verification, systematic-debugging, TDD), all into the orchestrator's context.
  - SDD created an untracked `.superpowers/sdd/` ledger in the AIU-046 worktree (43 references). At the end its fate was left to the owner ("until you decide").
  - The per-task two-stage review cadence (F7) comes from SDD.
  - No `docs/superpowers/` spec or plan files were created in this range, so brainstorming and writing-plans did not override repo spec locations here.
- **Evidence.** `hook_additional_context` attachments, Skill calls and path counts from the parser.
- **Label.** Fact for the counts. Inference for the cadence effects.
- **Confidence.** Medium.
- **Impact.** Quality: neutral. Speed: low to medium.
- **Proposed direction.** One AGENTS.md line: "Plugin skill workflows are optional tools; repo policy (spec locations, review cadence, Git/merge, question rules) wins on conflict; do not create `.superpowers/` artifacts that outlive the session." Consider loading skills on demand rather than in bulk.
- **KEEP.** Owner-initiated brainstorming for UI decisions (variant lists).

### A4b-F11 — The owner polls push and merge status across parallel sessions
- **Observation.** The owner asked "is everything merged and pushed?" in `009ee03c`, even though the final report already said committed and pushed with a hash. They asked it again in `e900135c`, a session with no work. In `3b229838` they relayed the origin/main hash themselves. They also asked "where did we stop?" (`15ece2a2`). These led to the 2026-10-08 explicit push-status rule.
- **Evidence.** 3 status-check turns and 1 relayed hash.
- **Label.** Fact for the events. Inference: the owner checks because they run many worktrees and want verified status, not a claim.
- **Confidence.** Medium.
- **Impact.** Speed: low per instance, recurring.
- **Proposed direction.** Keep the new rule (verified by fetch). Optionally add a read-only status helper, run on request, that lists unmerged worktrees and branches and ahead/behind against origin/main.
- **KEEP.** The final-report format with commit hash and NOT_RUN post-deploy items.

### A4b-F12 — Small UI fixes were reported before sibling cases and local interactive checks were done
- **Observation.**
  - In the hover-blink fix (`e3b3b623`/`8f275ad7`), the agent fixed the named icons and reported the in-app hover check as NOT_RUN. The owner found that the provider's sign-out dot still blinked and shifted the layout, and named the cause (Collapsed instead of Hidden).
  - In the design session (`c049f1a8`), the owner flagged that corner radii had drifted back after an earlier agreed change.
  - AGENTS.md allows and expects local interactive checks for UI changes; this is a local hover check, not a live-provider check.
- **Evidence.** `e3b3b623` 10-06 (1 correction, 1 interrupt). `c049f1a8` 10-02 (1 drift correction). Browser screenshot timeouts occurred 5 times in `c049f1a8`.
- **Label.** Fact for the events. Inference for the cause.
- **Confidence.** Medium.
- **Impact.** Quality: medium. Speed: low.
- **Proposed direction.** For UI fixes: (1) search for the same pattern in sibling elements before reporting, and (2) run the local unpackaged app or `--demo` and check the interaction (the host smoke harness exists) instead of handing a NOT_RUN hover check to the owner.
- **KEEP.** Precise NOT_RUN labelling instead of claiming a pass.

## KEEP — practices that work well

- **Honest status vocabulary.** PASS, FAIL, NOT_RUN and BLOCKED are used accurately. J's lane was reported BLOCKED, and the reviewer confirmed that the section was "honest and complete".
- **Whole-run and whole-branch convergence review.** It caught the primary's own record error (AIU-045) and two Important issues (AIU-046).
- **Recovery through SendMessage.** Subagents were resumed after a usage-limit cut, and fix rounds reused the same implementer's context.
- **Immediate capture of owner direction.** D-190, Claude-only, the IPv6 host fact and push status all went into AGENTS.md and memory within minutes.
- **Gate discipline.** Fresh-main merges, validator, tests and a CI watch before each push. In AIU-045 all nine CI runs were green with preview skipped, and merged worktrees were cleaned up.
- **Decision prompts.** Concise option lists with a recommendation, answerable in one word.
- **Model mix.** Sonnet for simple or records tasks and Opus for code worked without visible quality loss.

## Open questions for the owner

1. In multi-agent runs, should independent per-task review be limited to code, test or harness tasks, with docs and record tasks checked by the primary and one whole-run review kept?
2. May generated kickoff prompts ever narrow the standing Git policy (for example "do not merge without permission")? Or should they always inherit CONTRIBUTING unless you say otherwise in that request?
3. Do you want an agent-decided default for recommended decisions (act and record, with you able to object), instead of decision batches such as D1–D9?
4. After you pick a UI variant, do you want a second "confirm the design" step, or should implementation start right away?
5. Keep the superpowers SessionStart injection and SDD's `.superpowers/` ledgers, or disable or override them for this repo?
6. For overnight or unattended runs, should pending questions block, or should the agent proceed on documented defaults and list the questions at the checkpoint?
