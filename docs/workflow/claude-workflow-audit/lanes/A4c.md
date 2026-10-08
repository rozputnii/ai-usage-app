# Lane A4c — Worktree session transcripts

## Scope and method

- **Corpus (fact):** every `*.jsonl` under `~/.claude/projects/C--Users-danii-projects-ai-usage-app--claude-worktrees-*`. That is 22 worktree folders, 28 main transcripts and 37 subagent transcripts, from 2026-09-16 to 2026-10-08.
- **Excluded (fact):** `workflow-optimization-analysis-58ac82`, which is this audit's own session, and `goal-prompt-project-analysis-68068c`, which was modified less than 30 minutes before the scan.
- **Duplicate (fact):** in `5-hour-limits-display`, transcript `a04c0d4f` shares 338 of its 347 entry UUIDs with `3a9e73d6`. It is a fork of the same conversation, so totals below count it once. That gives **27 distinct sessions**.
- **Tray folder (fact):** the 27 files in `tray-miniature-order-refresh` are **one session** (`cb045d7e`) plus 26 subagent transcripts: 11 workers, 13 reviewers or re-reviewers, 1 read-only researcher and 1 final reviewer.
- **Parser (scripts in `work/A4c/`):** `parse.py`, `detail.py` and `cats.py` work as follows.
  - **Owner turns:** non-meta `user` entries whose text is not a `tool_result`, after removing `<system-reminder>` blocks. Slash-command invocations are included. Task notifications and the compaction summary are excluded.
  - **Agent questions:** `AskUserQuestion` calls, plus the last assistant text before each owner turn. I read that text to classify the turn.
  - **Gaps:** timestamp deltas over 10 minutes, labelled by the next event. If the next event is an owner turn, the agent was waiting on the owner. If it is a tool result, a tool was running or a question was pending.
  - **Commands:** Bash and PowerShell commands were regex-classified (build, test, smoke, git, gh, sleep, process-kill). Errors were classified by text pattern.
- **Owner-message classification:** I read all 109 owner turns myself and classified them by hand. Counts are approximate (±2 per category).
- **Limits:** the classifier is regex-based. Background-command durations are not measured, because they return immediately. Some keyword hits come from documents the agent read, not from live events, so I verified each cited incident by reading its context.

## Metrics table

Owner = owner turns, including slash commands. Int = `[Request interrupted by user]`. Ask = `AskUserQuestion` calls. Den = auto-mode classifier denials. Subs = subagent transcripts. Max gap is in minutes; "(owner)" means the agent waited for the owner, "(tool)" means a tool or a pending question.

| Worktree | Session | Start | Hours | Owner | Int | Ask | Den | Subs | Max gap |
|---|---|---|---|---|---|---|---|---|---|
| github-copilot-e2e-integration | 7d000676 | 09-16 | 88.6 | 5 | 0 | 5 | 0 | 1 | 5007 (tool; abandoned 3.5 d) |
| architecture-audit-plan | 0f5082e6 | 09-20 | 7.2 | 5 | 0 | 0 | 0 | 0 | 233 (owner) |
| architecture-audit-plan | 56292c03 | 09-29 | 0.5 | 3 | 1 | 0 | 0 | 0 | 17 (owner) |
| aiu-038-presentation-redesign | c10c9806 | 10-02 | 1.7 | 2 | 0 | 0 | 0 | 0 | 35 (owner) |
| 5-hour-limits-display | 01b3cc6d | 10-06 | 1.6 | 10 | 0 | 0 | 0 | 1 | 49 (owner) |
| fable-limit-single-card | 242eb4bc | 10-06 | 1.2 | 3 | 0 | 0 | 1 | 0 | 57 (owner) |
| minimalism-text | bb7892d8 | 10-06 | 1.0 | 6 | 0 | 0 | 2 | 0 | 29 (owner) |
| minimalism-text | d0a3daa4 | 10-06 | 23.1 | 19 | 0 | 1 | 0 | 1 | 674 (tool, overnight) |
| account-window-analytics | 4aaf5321 | 10-06 | 16.3 | 9 | 0 | 1 | 0 | 2 | 676 (tool, overnight) |
| fable-limit-single-card | f0880f22 | 10-07 | 0.0 | 1 | 1 | 0 | 0 | 0 | — |
| 5-hour-limits-display (fork a04c counted once) | 3a9e73d6 | 10-07 | 1.0 | 6 | 0 | 0 | 0 | 0 | 25 (owner) |
| account-window-analytics | 2e570ebf | 10-07 | 1.0 | 5 | 0 | 0 | 0 | 0 | 24 (owner) |
| kanban-ai-task-management | d2083512 | 10-07 | 0.0 | 1 | 0 | 0 | 0 | 0 | — |
| button-cursor-feedback | c68222ca | 10-07 | 1.2 | 5 | 0 | 0 | 0 | 0 | 27 (owner) |
| subscription-tiles-layout | df3b5d21 | 10-07 | 10.8 | 4 | 0 | 2 | 0 | 1 | 558 (owner, overnight) |
| custom-cap-provider-limit | 82668e49 | 10-08 | 2.2 | 4 | 0 | 0 | 0 | 0 | 99 (owner) |
| window-system-buttons-min-size | c8437c4e | 10-08 | 2.2 | 5 | 0 | 2 | 0 | 0 | 90 (tool: pending question) |
| window-width-settings-panel | f6731390 | 10-08 | 0.3 | 1 | 0 | 1 | 0 | 0 | 11 (tool) |
| weekday-limits-not-updating | 2f77916a | 10-08 | 0.3 | 2 | 0 | 0 | 0 | 0 | — |
| manual-spend-currency-conversion | 2ad47b4d | 10-08 | 1.2 | 3 | 0 | 0 | 0 | 1 | 10 (owner) |
| provider-frame-focus-issue | a6bb403d | 10-08 | 1.1 | 1 | 0 | 3 | 0 | 0 | 29 (tool: pending question) |
| custom-caps-provider-card | 120a6659 | 10-08 | 0.0 | 1 | 0 | 0 | 0 | 0 | — (abandoned) |
| confident-brattain | 18ad124b | 10-08 | 0.0 | 1 | 0 | 0 | 0 | 0 | — |
| cap-close-highlight-color | 3f9376e3 | 10-08 | 0.4 | 2 | 0 | 0 | 0 | 0 | 18 (owner) |
| subscription-cap-optional | 57a965df | 10-08 | 1.1 | 4 | 0 | 0 | 0 | 1 | 21 (owner) |
| subagents-implementation-plan | 3b03ec86 | 10-08 | 0.4 | 1 | 0 | 1 | 0 | 2 | — |
| tray-miniature-order-refresh | cb045d7e | 10-08 | 2.2 | 1 | 0 | 0 | 0 | 26 | 34 (waiting on subagents) |
| **Totals (27 sessions)** | | | | **109** | **2** | **16 (22 questions)** | **3** | **37** | |

**Owner-turn mix (my reading, approximate, 109 turns):**

| Category | Turns | Share |
|---|---|---|
| New task or requirement | ~32 | 29% |
| Design or option choice ("B", "a1", "h2") | ~18 | 17% |
| Pure approval ("так", "yes", "ok", "approve", "write the plan") | ~18 | 17% |
| Merge/push status question | 9 | 8% |
| Merge/push command | 6 | 6% |
| Visual-feedback correction | ~5 | 5% |
| Process-rule directive | ~6 | 6% |
| Unblocking the environment (unlocked screen, closed Sandbox, session limit, failed deploy) | ~7 | 6% |
| Admin slash commands (`/model`, `/compact`, `/goal clear`) | 3 | 3% |
| Information question | 3 | 3% |

## Findings

### A4c-F1 — Superpowers skill gates created repeated approval turns that the project policy says are not needed
- **Observation:**
  - The superpowers chain asks the owner at several fixed points. Brainstorming asks for approval of each section and of the written spec. Writing-plans asks which execution mode to use. Finishing-a-development-branch asks to choose between "merge locally / PR / keep".
  - In the early sessions the owner had to answer each of these points.
  - CONTRIBUTING already pre-authorises routine steps: "Implement and verify … without repeated approval for internal steps". It also sets direct merge to `main` as the default.
  - One session ended in an angry owner message: "I said yes 100 times! finish, auto-merge into main" (paraphrased).
- **Evidence:**
  - **5-hour-limits-display/01b3cc6d (10-06):** 7 of 10 owner turns were gating turns: two design choices and five approvals or mode choices. The agent also stated it would merge only after the owner's "yes".
  - **minimalism-text/d0a3daa4 (10-06):** 7 gating turns (yes ×3, ok, "native", finish option "1", "yes push"). After `/compact`, the owner had to ask whether the plan had been made with writing-plans.
  - **account-window-analytics/4aaf5321 (10-06/07):** three section approvals, then a spec-review request, then the outburst at 13:27. The agent then completed implementation, review and merge alone in about 70 minutes.
  - **Pure approval turns by period:** about 15 across the 6 sessions before 10-07 13:27, and 3 across the 14 sessions after it. This lines up with the "No repeated approval gates" memory entry.
- **Label:** observation = fact; causation (skill checklists drive the gates) = inference with high confidence. The agent texts explicitly cite "per the process, plan only after you approve the spec".
- **Confidence:** high.
- **Impact:**
  - Quality: neutral. The approvals added no information.
  - Speed: high. About 20 owner turns, plus 50–110 minutes of wall-clock waiting in d0a3daa4 and 4aaf5321 on merge/push questions.
- **Proposed direction:** state in AGENTS.md (one or two lines) that the superpowers spec-review, execution-mode and finish-branch prompts are pre-answered in this project. Keep design and product questions. Spec review happens only when the owner asks for it. The execution mode follows the plan's default. Finishing always means "merge to `main` per CONTRIBUTING". Alternatively, stop invoking those three skills by default.
- **KEEP:** brainstorming's real design questions (A/B/C choices). The owner answers them quickly and they change outcomes.

### A4c-F2 — Merge/push status consumed about 14% of owner turns until the "state push status" rule; after it, zero
- **Observation:**
  - The owner repeatedly asked "merged? pushed?" or ordered "push".
  - Agents asked permission to merge despite the standing instruction.
  - In one case the agent handed the owner a `git push origin HEAD:main` command to run himself, because the auto-mode classifier had denied the push as "Merge Without Review".
  - One agent accidentally pushed its worktree branch to the remote and then asked whether to delete it.
- **Evidence:**
  - 9 status questions and 6 push/merge commands in 10 sessions between 09-20 and 10-08 09:45.
  - Sessions with a status question: architecture-audit ×2, account-window ×2 (3 turns), minimalism ×2, custom-cap, subscription-tiles.
  - fable-limit/242eb4bc (10-06): classifier denial, then a hand-back to the owner.
  - button-cursor/c68222ca (10-07): stray remote branch.
  - Rule added in custom-cap/82668e49 at 10-08 09:45. In the 7 later sessions there were 0 status questions, and agents wrote "Pushed to `main`…" in each completion.
- **Label:** fact for the counts. Inference that the rule caused the drop; the sample is small and covers about 1 day.
- **Confidence:** medium-high.
- **Impact:**
  - Quality: low. The risk was claims without verification.
  - Speed: medium. About 15 turns, and each one interrupted the owner's attention.
- **Proposed direction:** keep the rule. Consider making the final-report push line mechanical: a fetch, then `merge-base --is-ancestor` evidence, in a shared completion snippet, so the claim is always checked.
- **KEEP:** the explicit "Pushed to `main` <hash>" closing line.

### A4c-F3 — Parallel worktrees collided on item and decision numbers and on `docs/decisions/accepted.md`
- **Observation:**
  - Before D-196, two parallel sessions used the same item number, and a decision number was already taken at merge.
  - After D-196, placeholder renumbering added mechanical work.
  - Once, the bulk replace also rewrote the placeholder name inside the D-196 rule text itself. A follow-up session (confident-brattain, 10-08) was needed to restore it.
  - `accepted.md` is the dominant conflict file.
- **Evidence:**
  - **Number collisions:**
    - `AIU-046` exists twice: `AIU-046-early-session-estimate` from 5-hour-limits/01b3cc6d and `AIU-046-in-app-updates` from minimalism/d0a3daa4, both on 10-06.
    - "D-192 was already taken, recorded as D-193" (subscription-tiles, 10-07).
  - **Merge conflicts:** 5 sessions had them. 4 were in `docs/decisions/accepted.md`; 2 were in `LedgerCardView.xaml(.cs)`.
  - **Renumbering cost:** 93 placeholder-related commands in 9 sessions, about 10 minutes of tool time. manual-spend alone ran 40 commands.
  - **Rule origin:** the owner had to dictate the rule (window-system-buttons, 10-08 08:15).
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: medium. Duplicate IDs, and a corrupted rule text that nearly shipped.
  - Speed: medium.
- **Proposed direction:**
  - A single deterministic tool command: "assign numbers at merge". It would replace `AIU-NEW*`/`D-NEW*` outside code-quoted rule text and run `--final`.
  - Reduce conflicts on the append-only decisions file, for example one file per decision or a `merge=union` attribute. Any structure change needs the owner's approval.
- **KEEP:** the D-196 placeholder rule itself.

### A4c-F4 — Every fresh worktree pays a restore/build tax that is discovered by failure
- **Observation:**
  - New worktrees start without restored assets.
  - The first build or validator run fails with `NETSDK1004`, then the agent restores and retries.
  - Builds are repeated per worktree, including every subagent worktree.
- **Evidence:**
  - `NETSDK1004` appears in 17 of 22 folders (21 hits). One agent noted it "had to restore NuGet packages … to run the validator".
  - Totals: 227 build commands taking about 151 minutes, and 44 restore commands taking about 16 minutes.
  - The tray session alone used 106 builds and about 77 minutes across its 11 worker worktrees.
- **Label:** fact for counts. Durations exclude background runs, so they are a lower bound.
- **Confidence:** high.
- **Impact:**
  - Quality: low.
  - Speed: medium. A failed first attempt in most sessions, plus duplicated builds across parallel worktrees.
- **Proposed direction:** document a one-line worktree bootstrap in README local checks or AGENTS (restore the solution and the validator once). Optionally run it from a lightweight SessionStart command hook.
- **KEEP:** the shared global NuGet cache; restores themselves are fast (about 20 seconds each).

### A4c-F5 — One shared interactive desktop is the main contention point for UI smoke
- **Observation:** desktop smoke needs the owner's single foreground desktop. Parallel sessions, the owner's own use and the owner's installed, auto-updating app all compete for it.
- **Evidence:**
  - **Volume:** 389 smoke-related commands, about 136 minutes, 112 of them in the tray session.
  - **Desktop lock:** a cross-process directory lock (`AiUsage-desktop-smoke.lock`) serialises smoke runs; 24 hits in the tray session.
  - **provider-frame/a6bb403d (10-08):** 3 `AskUserQuestion` calls asking to borrow the keyboard and mouse for 1–4 minutes each.
  - **Locked screen:** manual-spend (10-08) recorded the smoke as BLOCKED until the owner unlocked the screen. subscription-cap (AIU-054) recorded the same.
  - **Tray (10-08):** each worker's push to `main` published a Preview. The owner's installed app auto-updated and restarted, its tray icon displaced the test app's, and the launch smoke failed. That required an extra fix inside T-10.
  - **Killing by name:** early sessions killed `AiUsage` by process name (architecture-audit 09-29 ×3, aiu-038 10-02), which risks the owner's own app. Later sessions select by path or ID.
- **Label:** fact. That smoke contention is the largest UI-loop cost is an inference.
- **Confidence:** high.
- **Impact:**
  - Quality: medium. Flaky, environment-caused failures.
  - Speed: high for UI tasks.
- **Proposed direction:**
  - Ask once per session for a desktop-use window rather than once per smoke run.
  - Keep smoke robust to the owner's installed instance.
  - Batch the smoke runs at the end of the integration step instead of per worker.
  - Ask the owner whether a dedicated second Windows user or VM for smoke is acceptable.
- **KEEP:** the desktop smoke lock, ID- and path-scoped process handling, and BLOCKED-not-PASS reporting.

### A4c-F6 — A Windows Sandbox wait with no liveness check stalled about 35 minutes until the owner noticed
- **Observation:**
  - In account-window/4aaf5321 (10-07), the second Sandbox launch silently never started.
  - The agent reported "Sandbox still running" and kept waiting. The owner pointed out that the Sandbox window was closed.
  - Each Sandbox run also reinstalls .NET, which takes about 6–10 minutes.
- **Evidence:**
  - account-window/4aaf5321, 13:53–14:27: two owner nudges, followed by about 10 minutes for a re-run.
  - Long blocking poll loops elsewhere: github-copilot 7.1 and 7.6 minutes, minimalism 9.5, 8.1 and 5.7 minutes.
  - CI polling (`gh run`) took 12 minutes in minimalism/d0a3daa4.
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: low.
  - Speed: medium (owner involvement for a mechanical check).
- **Proposed direction:**
  - The Sandbox guest script should write a heartbeat or started marker. The host wait should fail fast if the process is absent.
  - Cache the .NET runtime in the mapped folder.
  - Use Monitor or background waits with a clear timeout rather than blind loops.
- **KEEP:** Sandbox use is reserved for checks that truly need a clean machine, per AGENTS.md.

### A4c-F7 — Subagent-driven execution delivered an 11-task feature with one owner turn; the friction was mechanical
- **Observation:**
  - tray-miniature/cb045d7e (AIU-055, 10-08) ran 11 workers in isolated worktrees with a per-task independent reviewer and a final whole-feature review.
  - Every worker merged and pushed its own part. The whole run took 2.2 hours with 1 owner turn (the slash command).
- **Evidence (friction):**
  - **Isolation guard:** 31 commands were blocked ("command too complex to verify it stays inside the worktree"). 22 of these were git commands; the rest involved `sed` with variables, heredocs or nested shells.
  - **Sleep blocks:** 7 `sleep 60–240` calls were blocked by the harness, with a hint to use Monitor instead.
  - **Misrouted results:** 3 reviewer results went to the controller instead of to the worker that dispatched the reviewer, so the controller relayed them with SendMessage.
  - **Push races:** 2 pushes were rejected because `main` moved between workers.
  - **Re-reviews:** 2 tasks needed a scoped re-review, for T-07 and T-11.
  - **Leftover worktrees:** 11 `agent-*` worktrees still exist. The controller did not remove them because "the app created them".
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: positive. Each task was reviewed, with 2 fix loops.
  - Speed: high positive. The friction cost minutes, not hours.
- **Proposed direction:** add a worker-prompt template that requires:
  - simple single-purpose commands
  - plain `git` commands without complex forms
  - Monitor instead of sleep
  - reviewers dispatched by the controller rather than by workers
  - fetch-merge-retry before every push
  - deletion of agent worktrees after integration.
- **KEEP:** the whole pattern (task briefs, isolated workers, per-task reviewer, controller integration, final review, records).

### A4c-F8 — Auto-mode classifier denials blocked routine policy actions; bypass mode removed them along with a guardrail
- **Observation:**
  - Until about 10-06 16:45, sessions ran in `auto` mode. The classifier denied the following:
    - `git push origin HEAD:main` ("Merge Without Review")
    - a CI workflow edit ("Production Deploy")
    - `git status` ("judged dangerous")
  - The owner spent several turns telling the agent to save the permission rules itself and restart.
  - Since then every session runs `bypassPermissions`.
- **Evidence:**
  - Denials: fable-limit 242eb4bc (1) and minimalism bb7892d8 (2).
  - Permission mode by session: `auto` in 6 sessions from 09-16 to 10-06, `bypassPermissions` in all 21 later ones.
  - Classifier denials after the switch: 0.
  - Guards that remain: the isolation guard (F7) and protected-path removal (3 hits).
- **Label:** fact. The quality risk of bypass is an inference.
- **Confidence:** high for the facts, medium for the risk.
- **Impact:**
  - Speed: high gain.
  - Quality/safety: the only remaining hard stops are harness guards.
- **Proposed direction:** owner decision. Either keep bypass, or move to a targeted allowlist (git push to `main`, dotnet, gh read-only, validator) plus deny rules for the truly dangerous operations (force-push, history rewrite, release dispatch).
- **KEEP:** the harness guards against worktree escape and protected-path deletion.

### A4c-F9 — The UI design-variant loop works and the owner values it, but variants must be visual, and bug-like UI fixes should skip it
- **Observation:**
  - After the 10-07 rule, every visible-UI session offered 2–3 variants before implementing.
  - The owner usually picks within one turn, often with a tweak ("B1 but no spinner", "A but not grey").
  - Two corrections show the owner expects rendered mockups, not text. One asked to "show on design examples right away"; the other said "these aren't real designs; another session showed animated cards in chat".
  - Brainstorming was also launched on bug-like reports (wrong highlight colour, weekday toggle not recalculating). That produced a variant or approval turn where AGENTS.md says bug fixes restoring approved UI need none.
- **Evidence:**
  - About 23 of 109 owner turns (21%) were design choices or visual feedback.
  - `show_widget` was used in 8 sessions.
  - Corrections: button-cursor/c68222ca (10-07), account-window/2e570ebf (10-07).
  - Bug-like cases: cap-close/3f9376e3 ("A"), weekday/2f77916a ("yes, do it"), fable/f0880f22 (the agent announced it would wait for "yes" before a bounded fix; the owner interrupted).
  - Batching worked: subagents-plan/3b03ec86 asked 4 design questions in one `AskUserQuestion` call, then proceeded autonomously.
  - The login UI session (5-hour/3a9e73d6) took 5 iterations, each about 1–25 minutes apart.
- **Label:** fact for the counts. "Helpful" is an inference from the speed and brevity of the owner's choices.
- **Confidence:** medium-high.
- **Impact:**
  - Quality: high positive (the owner steers visuals).
  - Speed: medium. Iterations are cheap when the variants are rendered.
- **Proposed direction:**
  - Default to rendered `show_widget` mockups (with states or animation when relevant), batched in one question.
  - Explicitly skip variants for regressions against approved UI.
  - Treat an owner's "/brainstorming" on a bug as a diagnosis request, not a design gate.
- **KEEP:** the variants-first rule and the recommendation-plus-pick format.

### A4c-F10 — Plugin side effects: a failing ux-superpowers hook in every session; design-superpowers and ux-superpowers are otherwise unused
- **Observation:**
  - The ux-superpowers SessionStart hook is prompt-type, which is unsupported for SessionStart. It fails with a non-blocking error at every session start, so its intended reminder never runs.
  - No design-superpowers or ux-superpowers skill was invoked in any worktree session.
  - The superpowers default output paths (`docs/superpowers/specs|plans`) were never written. Agents correctly used `docs/specs/<AIU>`.
- **Evidence:**
  - Hook error: 26 of 27 sessions. It is absent only in 09-16 and 09-20, which is probably before the plugin was installed.
  - Skill invocations in all transcripts:
    - superpowers brainstorming: 3 via the Skill tool plus 12 owner slash commands
    - writing-plans 4, executing-plans 3, test-driven-development 2
    - systematic-debugging 3, finishing-a-development-branch 2
    - subagent-driven-development 1 (slash command)
    - design-superpowers and ux-superpowers: 0
  - Writes under `docs/superpowers`: 0.
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: low. The hook error is noise.
  - Speed: low. The unused plugins add skill-listing context in every session (inference).
- **Proposed direction:** disable the ux-superpowers hook, or the plugin, and consider disabling design-superpowers. The owner should confirm, since it is a configuration change.
- **KEEP:** superpowers TDD, subagent-driven development and systematic-debugging, which were used productively.

### A4c-F11 — Repeated tool-discovery failures: `python` resolves to the Microsoft Store stub
- **Observation:**
  - Agents try `python` or `python3`, which are the WindowsApps stubs, and get exit code 49. They then search for an interpreter or rewrite the script in PowerShell.
  - `node` is also absent.
  - Working Python is available as `py -3.13`.
- **Evidence:**
  - 33 "Python was not found" results across 18 folders, including 12 in tray subagents.
  - 2 `node` misses.
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: low.
  - Speed: low to medium (a few minutes per occurrence, multiplied across sessions and subagents).
- **Proposed direction:** add one line in the host-tooling memory or README local checks: "use `py -3.13 -I`; there is no node".
- **KEEP:** the existing host-tooling memory file, which only needs this addition.

### A4c-F12 — The desktop app's "sync with base branch" tool fails, so agents fall back to manual merges
- **Observation:** the host-side `sync_with_base_branch` tool fails with "Committer identity unknown" when merging `origin/main`. Agents then merge manually.
- **Evidence:**
  - 6 failures in 5 sessions: account-window ×2, button-cursor, manual-spend, subscription-tiles and window-system, all 10-07 to 10-08.
  - The manual pattern is consistent: `git merge --no-edit origin/main` (34 times), then `git push origin HEAD:main` (30 times).
- **Label:** fact. That it is caused by the worktree's git identity is an inference.
- **Confidence:** medium.
- **Impact:** low. It costs one wasted call per session.
- **Proposed direction:** either the owner fixes the identity for that tool, or AGENTS.md says to use the manual git merge path directly.
- **KEEP:** the fetch, merge and verify-ancestor pattern (`merge-base --is-ancestor`, 30+ uses).

### A4c-F13 — Stale, superseded and abandoned worktree sessions
- **Observation:** worktrees and sessions outlive their purpose.
- **Evidence:**
  - **github-copilot/7d000676:** open 88 hours. The owner later did the same integration through another agent ("check if your branch is still needed"). The agent then deleted the branches but could not delete its own working folder.
  - **custom-caps-provider-card/120a6659 (10-08):** ended after the variant proposal with no reply. The same topic continued in subscription-cap the same afternoon.
  - **5-hour-limits-display:** the worktree folder was reused for a different branch (`login-ui-redesign`) and produced a forked duplicate transcript.
  - **Current state:** 33 worktrees are registered, 11 of them `agent-*`. They take 8.1 GB on disk under `.claude/worktrees`, mostly per-worktree `bin`/`obj` output (inference).
- **Label:** fact. That the owner loses track of parallel work is an inference, supported by the kanban session (10-07), where the owner said they cannot see tasks or remember where work stopped.
- **Confidence:** medium.
- **Impact:**
  - Quality: medium. Risk of duplicate work.
  - Speed: low.
- **Proposed direction:**
  - At session start, check whether the request is already on `main` or in another worktree.
  - Run periodic worktree cleanup.
  - Consider a lightweight visible task board; this is the owner's open kanban question.
- **KEEP:** the agents' honest "already in main, branch obsolete" detection.

### A4c-F14 — Long sessions with compaction drift
- **Observation:** a 23-hour session (minimalism/d0a3daa4) went through a manual `/compact`.
- **Evidence:**
  - After the compaction, the owner had to ask whether the plan had been made with writing-plans.
  - The agent later claimed an install error was fixed, and it recurred ("you said you fixed it").
  - A deploy failure was noticed by the owner, not the agent.
- **Label:** fact. Linking the drift to the compaction is an inference.
- **Confidence:** medium.
- **Impact:**
  - Quality: medium (unverified "fixed" claims).
  - Speed: medium.
- **Proposed direction:**
  - Keep sessions to one feature, as already done after 10-07.
  - Require that a "fixed" claim for install/update issues cites post-deploy evidence, or is labelled NOT_RUN.
  - Have the agent itself watch CI after a push before reporting.
- **KEEP:** the later practice of short, single-task worktree sessions (median about 1.1 hours on 10-08).

## KEEP — practices that work well

- **Subagent-driven development with per-task independent reviewers and controller integration (F7):** an 11-task feature with one owner turn and every task reviewed.
- **Variants-first UI rule, rendered visually and batched (F9).**
- **Explicit "Pushed to `main` <hash>" completion line (F2):** 0 status questions after it was introduced.
- **Placeholder numbering at merge (D-196) (F3).** It needs tooling, not removal.
- **Desktop smoke lock, ID- and path-scoped process control, and BLOCKED/NOT_RUN honesty (F5).** For example, smoke reported BLOCKED on a locked screen rather than PASS.
- **D-190 post-deploy owner checks:** no sign-in requests to the owner were found in the worktree sessions. Live checks were recorded as NOT_RUN.
- **No accessibility or display-matrix scope creep:** only 2 incidental mentions after 10-03.
- **Consistent integration pattern:** fetch, merge `origin/main`, tests on the merged result, push `HEAD:main`, verify ancestry.
- **Short single-feature worktree sessions** (10-08 median about 1.1 hours).

## Open questions for the owner

1. **Superpowers gates:** may AGENTS.md pre-answer the spec-review, execution-mode and finish-branch prompts, or would you rather stop auto-invoking those skills?
2. **Permission mode:** keep `bypassPermissions`, or switch to a curated allowlist with explicit deny rules for force-push and release actions?
3. **Desktop smoke:** is a separate Windows user, a VM, or a scheduled batched smoke window acceptable, so parallel sessions stop borrowing your keyboard and mouse?
4. **Decisions file:** may the decisions format change (per-decision files or a union-merge attribute) to end `accepted.md` conflicts?
5. **Plugins:** may the ux-superpowers hook or plugin and design-superpowers be disabled? Neither was used in worktree sessions.
6. **Preview auto-publish on every green push:** should parallel worker pushes during a multi-task run publish Previews, given that it disrupted local smoke on 10-08?
7. **Task visibility:** do you still want a visual task board (kanban session, 10-07)? This lane cannot see whether that was pursued.
