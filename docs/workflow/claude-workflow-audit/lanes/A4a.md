# Lane A4a — Session transcripts (to 2026-10-03)

## Scope and method (files/sessions parsed, counts, parser limits)

- **Files (fact):** 33 JSONL files under `C:\Users\danii\.claude\projects\C--Users-danii-projects-ai-usage-app\` with last-write ≤ 2026-10-03: 31 top-level session files and 2 subagent transcripts (`19922429/subagents/agent-a4a2…`, `c2038289/subagents/agent-a5e0…`). About 20,800 JSONL entries, about 91 MB. Entry timestamps run from 2026-09-15 to 2026-10-02.
- **Dedup (fact):** `26dae546` is a prefix copy of `c003b823`: 573 of its 576 entry UUIDs also appear in `c003b823`. It is left out of every total below. `247bd964` was abandoned after one prompt and one interrupt. The same prompt was restarted at once as `e2689193`.
- **Trivial sessions (fact):** 7 files have fewer than 25 entries and no owner work: `1b7b65ec`, `69668633`, `6d563668` (a `/design-login` run), `c7cdfd9b` (goal cleared), `033ef7a8` (a `/goal` stub), `79d0f780` ("checkout to main") and `45fd1295` (one status question).
- **Era context (fact):** Before 2026-09-20 work ran on `codex/aiu-010-*` branches. From 2026-09-20 it ran on `main` only. From 2026-09-26 the owner split work between Claude Opus (`[opus]`) and Codex "Astra" (`[astra]`). Codex sessions are not in this folder. Permission modes changed over time: plan/acceptEdits/auto on 09-15, bypassPermissions on 09-20…09-22, then auto from 09-23.
- **Parser (fact):** `work\A4a\parse.py`, `builds.py`, `misc.py`, `window.py` and `userkinds.py`, all run with `py -I`. Owner turns are `type:user` entries with `origin.kind=human` and string or text content, no `tool_result` and no `isMeta`. Compaction summaries ("This session is being continued…", 6) and auto-resume texts after a usage limit ("I hit my usage limit…", 4) also carry `origin=human`. They are subtracted to give **genuine owner turns = 110**. Interrupts are text containing `[Request interrupted by user`. Denials are error tool_results that carry `toolDenialKind` or match deny text. Gaps are consecutive entries more than 10 min apart where the later entry is not an owner message and the earlier one is not an `end_turn`.
- **Limits:**
  - Plain-text questions are counted only when the assistant's last text before the owner's reply ends with "?". "Write 'так'" gates without a "?" are missed, so I counted those separately with a regex (13).
  - Classifying owner messages as approvals, corrections and similar was done by hand from all 110 messages. Treat it as **inference, ±2 per category**.
  - Owner messages are in Ukrainian, often long run-on text (probably dictated).
  - Cost comes from `cost-state` entries, which only exist in later sessions (partial, about $193).
  - Codex/Astra transcripts are not visible, so cross-agent effort is undercounted.

## Metrics table (per session: date, owner turns, interruptions, agent questions, permission denials, max gap)

Owner = genuine owner turns. Appr = pure or near-pure approvals (manual). Int = `[Request interrupted]`. AUQ = AskUserQuestion calls. TQ = turn-ending "?" questions. Den = permission/classifier denials. Gap = longest agent-side gap in minutes, with its cause. Build = build/test/validator runs/failures.

| Session | Date | Topic (generic) | Owner | Appr | Int | AUQ | TQ | Den | Gap (cause) | Build |
|---|---|---|---|---|---|---|---|---|---|---|
| f6edd8aa | 09-15 | branch/worktree setup | 1 | 0 | 0 | 1 | 0 | 1 | – | 0/0 |
| 82b6026c | 09-15 | AIU-010 preparation | 3 | 0 | 0 | 0 | 0 | 0 | – | 4/0 |
| e26bd986 | 09-15 | design MCP access check | 3 | 2 | 0 | 0 | 1 | 0 | – | 0/0 |
| c003b823 | 09-15→16 | AIU-010 mock frontend (/goal) | 1 | 1 | 0 | 0 | 0 | 1 | 221 (usage limit) | 97/29 |
| d876360e | 09-20 | branch audit/cleanup | 2 | 0 | 1 | 1 | 0 | 1 | 69 (AUQ wait) | 0/0 |
| adf78332 | 09-20 | next task / AIU-010 close | 5 | 0 | 0 | 1 | 2 | 0 | – | 3/1 |
| c2038289 | 09-20 | AIU-009 Antigravity (/goal) | 0 | 0 | 0 | 3 | 0 | 2 | – | 33/2 |
| b978bef5 | 09-20 | architecture-audit prompt | 2 | 0 | 0 | 0 | 1 | 0 | – | 2/1 |
| 57a09aae | 09-20→22 | T-08/T-09 remediation (/goal) | 1 | 0 | 0 | 0 | 0 | 0 | – | 8/1 |
| e395a86d | 09-22→23 | status / review | 3 | 1 | 0 | 0 | 1 | 0 | – | 0/0 |
| 19922429 | 09-23→24 | AIU-014 preview updates | 10 | 1 | 0 | 1 | 4 | 0 | 9 (CI watch) | 2/0 |
| 28a83013 | 09-24→25 | single-window UI simplification | 10 | 0 | 2 | 0 | 2 | 1 | 10 | 39/9 |
| ff451c68 | 09-25 | crash on login (installed app) | 1 | 0 | 0 | 0 | 0 | 0 | 253 (build call hung) | 3/0 |
| e4cf28aa | 09-25 | auto-refresh bug | 2 | 1 | 0 | 0 | 1 | 0 | – | 0/0 |
| 213c6560 | 09-26 | redesign / limits spec | 12 | 4 | 0 | 4 | 4 | 0 | 15 (AUQ wait) | 9/0 |
| a3dd8788 | 09-29 | AIU-034 continuation | 12 | 1 | 0 | 0 | 1 | 0 | – | 6/0 |
| 91777cf5 | 09-29 | AIU-034 T-11 / Gate A | 6 | 0 | 1 | 7 | 1 | 6 | 18 (classifier outage) | 5/1 |
| 3c41d0cf | 09-29 | AIU-034 T-12 | 4 | 2 | 0 | 0 | 1 | 4 | – | 3/1 |
| eb8a1e04 | 09-29 | AIU-034 T-13 (pasted prompt) | 1 | 0 | 0 | 0 | 0 | 0 | – | 2/0 |
| f3fc556e | 09-29→10-01 | AIU-034 T-15…T-17 | 7 | 3 | 2 | 0 | 1 | 0 | – | 5/0 |
| 247bd964 | 10-01 | aborted duplicate start | 1 | 0 | 1 | 0 | 0 | 0 | – | 0/0 |
| e2689193 | 10-01→02 | design handoff import/edit | 5 | 1 | 0 | 1 | 0 | 1 | – | 5/1 |
| 3bd65369 | 10-02 | design iteration (tray/window) | 16 | 2 | 0 | 0 | 1 | 1 | – | 5/0 |
| 2 subagents | 09-20, 09-23 | review/verification helpers | 0 | – | 0 | 0 | 0 | 0 | – | 1/0 |
| 7 trivial | – | see scope | 4 | – | 0 | 0 | 0 | 0 | – | 0/0 |
| **Total** (excl. 26dae546) | | | **110** | **19** | **7** | **19** | **21** | **18** | 9 gaps >10 min | **228/46** |

Other totals (fact):
- 66 `git commit` and 66 `git push` invocations.
- 6 manual `/compact` runs, 6 compaction continuations and 5 resumes after a usage limit.
- 5 `/goal` launches.
- 17 hits of "Python was not found".
- 134 `perl -i` and 109 `sed -i` in-place edits, plus 46 heredoc writes.
- 17 "file modified since read" warnings.
- Only 1 Agent dispatch in the main sessions.

## Findings

### A4a-F1 — The owner acts as the message bus between sessions and agents
- **Observation:** The owner repeatedly asked for a prompt to start the next session, task or agent, then pasted large prompts in to start sessions. The goal-mode text limit (about 4,000 chars) turned prompt-writing into friction. In one case the owner swore because the generated `/goal` prompt was too long and repeated what the task file already said.
- **Evidence:**
  - 11 owner turns ask for a prompt or for how to launch the next session: 213c6560 ×4, 3bd65369, 3c41d0cf, a3dd8788, adf78332, b978bef5, e395a86d, f3fc556e.
  - At least 7 sessions start with a pasted prompt of 0.5–15k chars: 82b6026c 15.8k, 19922429 6.2k, 213c6560 Phase A, eb8a1e04, e2689193, e26bd986, f6edd8aa.
  - About 13 owner turns relay state between Opus and Astra (Codex), 09-26…10-02.
  - adf78332 (09-20) and b978bef5 (09-20): the owner corrected the goal-prompt length and asked for details to live in a repo file.
- **Label:** fact (counts); inference (cost to the owner).
- **Confidence:** high.
- **Impact:**
  - Quality: medium. Hand-written prompts drift from tasks.md.
  - Speed: high. Every task boundary costs the owner 2–4 turns plus a copy-paste.
- **Proposed direction:** Make the task's `tasks.md` Handoff section the only launch artifact. Provide one fixed launcher, such as a project skill or slash command like `/resume AIU-xxx`, or a one-line convention "Continue <AIU> from its Handoff". It reads the handoff and constraints, so no bespoke prompt is ever needed. Codex relaying is gone since 10-05; this removes the remaining pattern.
- **KEEP:** Durable plans and handoffs in the repo (they made the short launches possible). English prompts.

### A4a-F2 — Roughly 1 owner turn in 8 asks "what's next / what's the state / is it saved?"
- **Observation:** Many owner turns are status pulls rather than decisions.
- **Evidence:**
  - About 14 owner turns ask "next task / which tasks are open / current state / what can run in parallel". Examples: adf78332, 3c41d0cf, f3fc556e, 45fd1295, 19922429, 91777cf5, e395a86d, 3bd65369, a3dd8788.
  - 3 turns ask "is everything committed/pushed/saved": 57a09aae, 91777cf5, 3bd65369. The 3bd65369 case came after a session-limit cut-off.
  - The agent usually replied with 2–4 options plus a question, not a single recommended next action.
- **Label:** fact (counts); inference (cause).
- **Confidence:** high.
- **Impact:**
  - Quality: low.
  - Speed: medium. These are about 15 owner turns and round-trips in 18 days.
- **Proposed direction:** Every final report ends with a fixed footer: push status (already ruled 10-08), one recommended next item with its one-line launch, and the list of other ready items. AGENTS.md keeps "backlog status does not select work". Consider letting a single owner "так" to the recommendation count as selection.
- **KEEP:** The 10-08 push-status rule. These transcripts confirm the need for it.

### A4a-F3 — About a third of agent questions were avoidable under the written rules
- **Observation:** I classified all 40 question events (19 AskUserQuestion calls, 21 turn-ending "?"):
  - About 19 legitimate: product intent, ToS/security, destructive remote deletion, budget/certificate choices.
  - About 8 owner-selection-by-policy: which task next.
  - About 13 avoidable, routine continuation inside an approved plan or record-keeping. Examples:
    - "Run T-17?" after Gate B was approved (f3fc556e).
    - "Start planning Phase B?" (91777cf5).
    - "Start the review?" (e395a86d).
    - "Save it to a file?" (b978bef5).
    - "Record the decisions in verification.md before implementing?" (e2689193 AUQ).
    - "Minimal fix now or the whole AIU-012?" for an owner bug report; the answer was "fix it" (e4cf28aa).
    - "Merge the branch first or start AIU-009?" (adf78332).
    - "Would you like to run /design-login now?" when the agent could not run it (e26bd986, Haiku).
  - Separately, 213c6560 confirmed a spec section by section: 4 consecutive "Is this part right?" answered with 4 bare "так".
- **Evidence:** questions.txt classification; 213c6560 (09-26); f3fc556e (09-29); e395a86d (09-23); e4cf28aa (09-25).
- **Label:** fact (questions exist); inference (avoidability), judged against CONTRIBUTING §Development step 3 ("without repeated approval for internal steps").
- **Confidence:** medium.
- **Impact:**
  - Quality: low.
  - Speed: medium. Each costs a full owner round-trip, often 5–60 min of wall time.
- **Proposed direction:** Add a short "never ask" list to AGENTS.md:
  - continuing the next step of an approved plan;
  - writing records or decisions the owner already gave;
  - saving files;
  - merging or pushing to main;
  - running reviews and checks;
  - choosing the minimal fix for a reported bug.
  
  Batch spec review into one confirmation. Auto-memory "No repeated approval gates" already points this way. Promote it into the repo so every session sees it.
- **KEEP:** AskUserQuestion with a "(Recommended)" option. The owner's preference for one question at a time with suggested answers (91777cf5).

### A4a-F4 — Evidence integrity clashed with owner closure, and with verification the owner saw as redundant
- **Observation:**
  - Agents held completion for owner-observed or live checks. The owner pushed back:
    - "mark in main that everything is OK", then an interrupt (d876360e);
    - "Claude already works, mark it complete", answered with an AUQ about whether owner testimony may count as PASS (adf78332).
  - Agents offered Narrator/screen-reader follow-ups (adf78332).
  - Agents proposed re-checking light/contrast themes and sign-in that the redesign would replace. The owner replied that this is doing the same work twice (a3dd8788).
  - Assistant lines mention "contrast" 154 times, mostly in AIU-010 (c003b823 63, 28a83013 21). The owner removed light and contrast themes on 09-24.
- **Evidence:** d876360e, adf78332 (09-20); a3dd8788 (09-29); 28a83013 (09-24); about 103 assistant lines mention NOT_RUN.
- **Label:** fact (events); inference (that these led to the 10-03 accessibility/display direction and D-190).
- **Confidence:** medium-high.
- **Impact:**
  - Quality: positive (honest status).
  - Speed: high negative. Blocked closure, extra owner turns, and verification of UI that was about to be deleted.
- **Proposed direction:** The current AGENTS.md rules (10-03 no accessibility matrix; D-190 post-deploy owner checks recorded NOT_RUN) already address this. Add one more rule: when the owner reports a result in chat, record it as "owner-reported PASS (date)" without asking. Do not verify UI that a selected redesign will replace.
- **KEEP:** Honest NOT_RUN/BLOCKED reporting. Refusing to upgrade unobserved checks to PASS.

### A4a-F5 — Early over-scoping and big-bang UI writes caused rework
- **Observation:**
  - In AIU-010 the agent planned a 4th project (`AiUsage.Presentation`). The owner corrected this: stay in three projects unless a concrete need is shown (82b6026c).
  - The mock frontend implemented System/Light/Dark plus simulated high contrast, including a WinUI crash workaround. It was deleted 9 days later.
  - The UI was written in bulk (126 Write calls) before the first full compile. The result was a 16-run failing build streak, about 12 min of XAML/C# fix-ups (c003b823 22:19–22:31).
- **Evidence:** c003b823: 97 builds, 29 failures (13 XAML, 10 C#, 1 PRI, 2 file locks). 28a83013: 39 builds, 9 failures. These two sessions hold 38 of the 46 build failures in the range.
- **Label:** fact (counts); inference (root cause).
- **Confidence:** medium.
- **Impact:**
  - Quality: medium. Speculative surface area later removed.
  - Speed: medium.
- **Proposed direction:** The 10-03 simplicity rule in CONTRIBUTING covers architecture. Add a working habit to the implementer guidance: compile after each view or feature slice, not after the whole UI. Outside these two sessions the build failure rate is low (8/92), so no broader change is needed.
- **KEEP:** The red-to-green cadence in the later sessions (57a09aae, c2038289: 41 runs, 3 failures).

### A4a-F6 — Parallel sessions collided on one `main` checkout
- **Observation:** Several sessions or agents worked in the same working copy at once.
  - e4cf28aa found another session's uncommitted tray changes and a Debug build running; ff451c68 was active at the same minute.
  - 91777cf5 was still active when the owner started T-12 in a new session, so the owner had to interrupt.
  - e395a86d found another session on AIU-014 and stopped (the owner praised this).
  - 213c6560 warned that Astra had a stale copy and needed a `git pull` before pushing.
  - c2038289 had a push rejected (non-fast-forward).
- **Evidence:** e4cf28aa/ff451c68 (09-25 19:38); 91777cf5 (09-29 17:34); e395a86d (09-23); 213c6560 (09-26).
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: high risk. Mixed commits, edits overwritten; 17 "modified since read" warnings, partly from this.
  - Speed: medium.
- **Proposed direction:** This is now partly addressed by worktree use and D-196 (AIU-NEW numbering). Make it explicit:
  - A session that finds foreign uncommitted changes or a running build moves itself to a worktree instead of asking.
  - The launcher (F1) defaults to a worktree when another session is active.
- **KEEP:** The agent's habit of inspecting git state and concurrent work before acting.

### A4a-F7 — The auto-mode classifier stopped plan execution; it treated tasks.md steps as injected instructions
- **Observation:** 18 real denials, all from the classifier or tool rules. None were owner rejections of agent actions, apart from 1 plan rejection and 1 Bash reject during a message resend.
  - 7 auto-mode classifier blocks:
    - 4 "Instruction Poisoning" in 3c41d0cf. The owner's terse "execute t-12" made the agent edit tasks.md from the plan, and the classifier refused the edits, a scratch script and even `git status`. The session stopped until the owner re-authorized ("approve, create the brief").
    - 1 "Real-World Transactions" and 1 "Credential Exploration" in c2038289 (browser/OAuth for Antigravity).
    - 1 "Unverifiable Deletion Scope" in d876360e (bulk remote branch deletion).
  - 6 "classifier unavailable" transient errors in 91777cf5 (an 18-min stall).
  - 2 Claude Design `write_files` calls without a `plan_token` (tool misuse).
  - 1 false-positive "Remove-Item on system path" block for a PowerShell here-string.
- **Evidence:** denials.txt; 3c41d0cf (09-29 17:26–17:27); 91777cf5 (09-29 14:19); c2038289 (09-20 14:03/14:09); d876360e (09-20 13:04).
- **Label:** fact (denials); inference (trigger mechanism for "Instruction Poisoning").
- **Confidence:** medium-high.
- **Impact:**
  - Quality: positive for the credential and deletion blocks.
  - Speed: medium. One session lost entirely and one 18-min stall.
- **Proposed direction:**
  - Owner launch lines should name the authorization explicitly ("I authorize T-12 as written in tasks.md"). The eb8a1e04 prompt did this and had 0 denials.
  - The launcher from F1 can include that wording.
  - Use the Claude Design `finalize_plan` flow by default.
  - Consider the `fewer-permission-prompts` allowlist for read-only git and validator commands. This is owner-gated configuration; do not change it here.
- **KEEP:** Classifier blocks on OAuth/credential pages and bulk deletion. Agents stopped and reported instead of working around them.

### A4a-F8 — Host-tooling rediscovery in almost every session
- **Observation:** Agents tried `python` (the WindowsApps stub), `node` and `pwsh`, then fell back to other tools.
- **Evidence:**
  - "Python was not found": 17 hits in 16 sessions.
  - "node: command not found": 4.
  - `pwsh` missing: 2.
  - zoneinfo/tzdata failure under `py`: 1 (3c41d0cf).
  - Scratch scripts were recreated per session ($scratchpad scripts: about 120 commands).
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: low.
  - Speed: low-medium. 1–3 wasted calls per session and some silent `|| echo nopython` fallbacks that skipped intended checks (ff451c68 15:23).
- **Proposed direction:** Add a short host-tooling note to AGENTS.md or the verification doc: Python via `py`, which works; no node; no pwsh 7; the path of the dotnet SDK. Auto-memory has part of this (pwsh) but not Python or node.
- **KEEP:** —

### A4a-F9 — Bulk shell editing instead of Edit/Write
- **Observation:** Agents edited files heavily with `perl -0pi` (134), `sed -i` (109) and heredoc writes (46), especially in UI and design sessions.
- **Evidence:**
  - Shell edits: c003b823 65 perl; 28a83013 50 perl and 22 sed; 3bd65369 33 sed; c2038289 13 sed.
  - 17 "file modified on disk since last read" warnings.
  - 6 "String to replace not found".
- **Label:** fact (counts); inference (risk).
- **Confidence:** medium.
- **Impact:**
  - Quality: medium. In-place regex edits are hard to review, can match unintended text silently, and break Edit's read-before-write guarantees.
  - Speed: neutral to positive for genuinely mechanical bulk changes.
- **Proposed direction:** Use Edit/Write by default. Keep scripted rewrites for clearly mechanical multi-file changes, followed by `git diff` review. One sentence in the implementer guidance is enough.
- **KEEP:** Scripted bulk edits for genuinely mechanical renames.

### A4a-F10 — Long sessions hit usage limits and compaction, and work survived because of frequent pushes
- **Observation:**
  - 6 manual `/compact` runs, 6 continuation summaries, and 5 resumes after a usage limit.
  - c003b823 ran about 15 h with two limit waits (3.7 h and 3.1 h of agent-side gap).
  - 3bd65369 hit the session limit at the end. The owner had to ask whether everything was saved, after doing further work in Codex meanwhile.
  - A resume glitch in c003b823/26dae546: "Continue from where you left off" got the reply "No response requested", then a 55-min gap.
- **Evidence:** c003b823 (09-15/16); 28a83013 (09-24); c2038289 (09-20); 3bd65369 (10-02).
- **Label:** fact.
- **Confidence:** high.
- **Impact:**
  - Quality: medium. Context loss across compaction, mitigated by repo handoffs.
  - Speed: high in wall time, but mostly unavoidable.
- **Proposed direction:** Keep save-point pushes (66 commits and 66 pushes in the range, which made recovery possible). Before a long build, test or design step, update the Handoff so a cut-off session leaves an exact next action. One task per session reduces the need for compaction.
- **KEEP:** Frequent commit and push to `main` (09-20 rule) as save points. Compaction summaries that quote the owner's goal.

### A4a-F11 — Long stalls: a hung build call and foreground CI polling
- **Observation:**
  - In ff451c68 one `dotnet build` call ran 253 min of wall time before the harness moved it to the background (a 600 s timeout was set). No owner was involved; the cause is unknown, possibly host sleep or lock, or a file lock. **Uncertain.**
  - CI was watched in the foreground with `timeout 590 gh run watch …` (19922429 ×8, up to 9.4 min each; 17 `gh run` calls in total).
  - AUQ prompts waited 15–69 min for the owner (213c6560, d876360e).
- **Evidence:** gaps.txt; ff451c68 (09-25 15:23→19:36); 19922429 (09-23/24).
- **Label:** fact (durations); inference (cause of the hang).
- **Confidence:** medium.
- **Impact:**
  - Quality: low.
  - Speed: medium. The CI watches alone took about 60 min across the range.
- **Proposed direction:** Run long builds and tests in the background and keep working. Do not foreground-watch CI. Push, continue, and check the run once at the end, reporting it as pending if unfinished. Batch questions before long autonomous stretches so an AUQ does not sit idle.
- **KEEP:** Using `run_in_background` with Monitor polling (c2038289, 3bd65369).

### A4a-F12 — Owner corrections come from misunderstanding product ideas and from workarounds offered instead of fixes
- **Observation:** About 12 owner turns (about 11%) are corrections. Categories:
  - Misread intent:
    - The agent proposed removing provider history; the owner meant keeping history local only (a3dd8788).
    - "I asked about something else" (3bd65369).
    - Two reminders of the agent split already decided (91777cf5).
  - Over-scope or architecture: no 4th project (82b6026c); no worktree (f6edd8aa plan rejected).
  - Workaround instead of root fix: the agent offered a manual MSIX install; the owner wanted the Pages installer link fixed (28a83013).
  - Prompt format: too long and duplicating (adf78332).
  - Design detail: squircle corners; no today strip when the period limit is exhausted (3bd65369).
  - The 3bd65369 owner explicitly asked the agent to prove it understood a complex rule.
- **Evidence:** owner_msgs.txt manual classification.
- **Label:** inference (categorization); fact (events).
- **Confidence:** medium.
- **Impact:**
  - Quality: medium. Misunderstood rules would have shipped.
  - Speed: medium.
- **Proposed direction:** For non-trivial product or logic ideas, restate the rule as numbered points with one worked example before editing; the agent did this well in 3bd65369. For bug reports from the installed app, fix the root cause first and offer workarounds only as an addition.
- **KEEP:** The numbered-points-plus-"так" design loop. It is now codified as the 10-07 UI-variants rule.

### A4a-F13 — Owner effort profile: only about 17% of turns are pure approvals; goal-mode sessions need almost none
- **Observation:**
  - Of 110 genuine owner turns:
    - about 19 (17%) are pure or near-pure approvals ("так", "yes", "погоджую", "фікси", "continue");
    - about 14 (13%) are status or next queries;
    - about 11 (10%) are prompt requests;
    - about 12 (11%) are corrections;
    - about 13 relay Codex/Astra state;
    - the rest (about 40%) are new direction or product content.
  - The three `/goal` sessions (c003b823, c2038289, 57a09aae) ran 3–15 h with 0–1 owner turns. Their questions were legitimate: ToS risk, a push-protection secret, an account write.
  - Conversational design and spec sessions (3bd65369 16, 213c6560 12, a3dd8788 12) carry most of the owner load. That is inherent in product decisions.
- **Evidence:** metrics table.
- **Label:** fact (counts); inference (shares, ±2).
- **Confidence:** medium.
- **Impact:** Speed. The cheapest wins are the non-decision turns (status, prompts, relays, routine approvals), about 50 of 110, not the product conversations.
- **Proposed direction:** Target F1–F3 for reduction. Leave the product discussion as is.
- **KEEP:** Goal-mode execution from a durable repo plan, with questions limited to real security, ToS or external-authority decisions.

### A4a-F14 — Interruptions were session management, not stopping wrong actions
- **Observation:** 7 interrupts and 1 user-rejected tool call. 0 interrupts landed during an agent tool call to stop a harmful or wrong action.
- **Evidence:**
  - 3 times the owner interrupted to resend or edit their own message (28a83013 ×2, f3fc556e).
  - 1 aborted a duplicate session start (247bd964).
  - 1 came after telling the agent another session already started T-12 (91777cf5).
  - 1 came after "just mark it OK" (d876360e).
  - 1 more in f3fc556e before moving to a new session.
- **Label:** fact.
- **Confidence:** high.
- **Impact:** Low. It suggests agents rarely went off the rails mid-action in this range.
- **Proposed direction:** None needed beyond F6 (parallel-session awareness).
- **KEEP:** —

## KEEP — practices visible in transcripts that work well

- Goal-mode runs driven by a repo plan and handoff, with near-zero owner turns (c003b823, c2038289, 57a09aae).
- Checking git state and concurrent work before acting. The owner explicitly praised this (e395a86d).
- Honest NOT_RUN/BLOCKED, and refusing to bypass protections:
  - GitHub push protection on a vendored OAuth secret; the agent moved it to env configuration (c2038289);
  - ToS-risky live checks held for an owner decision;
  - an account-entitlement write excluded by default.
- Save-point commits and pushes, which allowed recovery after usage limits and compaction.
- AskUserQuestion with a "(Recommended)" option, and one question at a time when the owner asks for it.
- Numbered design points with an explicit owner "так" before design edits. Echoing understanding with worked examples.
- Ukrainian conversation with English repository artifacts and prompts.
- Background builds with notification instead of sleep loops (later sessions).

## Open questions for the owner

1. Would a fixed `/resume AIU-xxx` (or "continue from Handoff") launcher replace your requests for prompts, or do you still want bespoke prompts for some cases?
2. May a single "так" to the agent's recommended next item count as owner selection, so the agent can start it without a separate launch?
3. When you report a result in chat (for example "Claude works"), may agents record it as owner-reported PASS without asking?
4. Should agents ever watch CI in the foreground, or always push, continue, and report CI as pending or finished at the end?
5. When an agent finds another session's uncommitted changes in the main checkout, should it move itself to a worktree automatically?
