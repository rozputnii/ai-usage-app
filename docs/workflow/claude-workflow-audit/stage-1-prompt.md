GOAL (Stage 1 of a multi-stage effort): Deep, broadly distributed analysis and research
to make AI-driven development of this repository with Claude Code higher in quality and
faster, with less routine owner involvement — removing what gets in the way while keeping
everything important. This stage produces analysis and recommendations only. Stage 2
(optimization) starts only after the owner reviews the report and approves specific items.

## Roles and orchestration (owner-authorized)
The owner explicitly requests multi-agent orchestration for this stage and authorizes a
broad fan-out (roughly 12-16 subagents, plus critics).
- Main session = orchestrator and architect. It plans the lanes, dispatches them in
  parallel, checks lane outputs against evidence, resolves conflicts and duplicates,
  maps internal problems to external solutions, designs the target workflow and writes
  the final report. It does not do lane work itself beyond spot-checks.
- Subagents = lane workers. Each gets a self-contained English brief: scope, sources,
  questions to answer, output schema and limits. Repository lanes are read-only
  (Explore); research lanes need web access (general-purpose). Lanes never edit the
  repository; they return their report to the orchestrator.
- Critics = 2 fresh read-only subagents that adversarially challenge the top
  recommendations and the KEEP list before the report is finalized (quality guard:
  does any recommendation weaken verification, review, security or owner control?
  is any claim unsupported?).
Lane output schema (each finding): id, lane, observation, evidence (paths, commits,
counts, URLs with dates), fact/inference/external-claim label, confidence, impact on
quality and speed, proposed direction, and anything that must be KEPT and why.

## Git — one long-lived branch for the whole optimization effort
All work of this effort, in every stage and iteration, lives on one dedicated branch:
workflow-optimization. Create it from fresh origin/main if it does not exist; otherwise
continue it. It is an owner-requested exception to the main-only default and stays
separate until the owner decides the optimization is complete and approves merging it.
- Commit as lanes complete and push the branch to origin after each meaningful step.
- Never merge it into main and never open a PR without an explicit owner decision.
- At the start of every session and before writing conclusions, fetch origin and merge
  origin/main into the branch, so the analysis reflects current main; resolve conflicts
  preserving both sides' intent and note anything that invalidates earlier findings.
- New backlog items or decisions created on this branch use AIU-NEW / D-NEW placeholders
  per CONTRIBUTING.md; real numbers are assigned only at the final merge.
- Save this prompt verbatim as docs/workflow/claude-workflow-audit/stage-1-prompt.md
  in the first commit.

## Continuity across stages and sessions
Maintain docs/workflow/claude-workflow-audit/status.md: current stage, what is done,
owner decisions taken (with dates), open owner decisions, and one exact next action.
Read it first in every session; update it before ending a session. Stage 1 is complete
when report.md, all lane reports and status.md are committed and pushed and the owner
decisions are listed. Later stages are started by separate prompts that reference the
approved decision numbers.

## Lanes
Part A — current state (evidence from this repository and its history):
A1 Rules and guidance: CLAUDE.md, AGENTS.md, CONTRIBUTING.md, docs/workflow/*,
   docs/constitution.md, docs/decisions/*. Contradictions, duplication, superseded or
   stale rules, scattered "Owner direction" entries, always-loaded vs read-by-task split.
A2 Work records: docs/backlog.md, recent docs/specs/* (spec, tasks, verification). Size,
   value, what agents and the owner actually use, template quality.
A3 Agent tooling: .claude/agents/*, .claude/settings*.json, .agents/skills/*, the owner's
   auto-memory (C:\Users\danii\.claude\projects\C--Users-danii-projects-ai-usage-app\memory\),
   dead OMP/Codex artifacts (.omp, .codex, docs/archive; Claude Code only since 2026-10-05).
A4 Session transcripts (~\.claude\projects\...\*.jsonl for this project; split into two
   lanes by date range if large): owner interruptions, questions the agent could have
   decided, re-asked settled questions, permission prompts, rework loops, long stalls.
A5 Git and CI history: fix-after-feat chains (e.g. AIU-055 T-09/T-10), merge and
   AIU-NEW/D-NEW renumbering churn, CI duration and failure causes (read-only gh).
A6 Tests and checks: suites, measured timings, smoke lanes, redundant or low-value tests,
   gaps that let bugs reach deploy, verification-matrix cost vs value.
Part B — external research (current, primary sources preferred; judge fit for a solo
owner, Windows-first C#/.NET 10 WinUI/MSIX app, the CONTRIBUTING.md simplicity rule and
its security boundaries):
B1 Claude Code official best practices: CLAUDE.md/memory, skills, subagents, hooks,
   plugins, permissions, worktrees/parallel sessions, headless and scheduled runs.
B2 Skills and plugin ecosystems, including the superpowers plugin already in use:
   adopt, adapt or retire.
B3 MCP servers: C# code intelligence/LSP and semantic navigation, .NET build/test,
   Windows UI automation for WinUI smoke tests, GitHub, Microsoft docs.
B4 Memory and knowledge: Claude Code memory, knowledge-graph and code-graph approaches
   (e.g. Graphiti-style graph memory, code-graph MCPs), and when they beat plain docs.
B5 Spec-driven and agentic workflows (e.g. Spec Kit, Kiro-style specs, BMAD), multi-agent
   orchestration patterns, review and verification practices.
B6 Test strategy for AI-written code and fast .NET/WinUI test feedback loops.
For every external candidate: source and date, maturity and maintenance, security and
supply-chain risk (a third-party MCP server is code with access), Windows support,
setup and upkeep cost, concrete benefit here, verdict (adopt / trial / adapt idea /
reject) with the reason. Fewer high-value candidates beat a long catalogue.

## Guiding principle
Quality first, then speed. A change that saves time but weakens verification, review,
security or owner control is not an improvement. "Clutter" = material that slows or
misleads agents or the owner without adding value (stale or contradictory rules,
duplication, dead artifacts, oversized records, unnecessary questions, redundant or
low-value tests and checks). Anything with real value goes to KEEP.

## Deliverables (all on the branch)
- docs/workflow/claude-workflow-audit/stage-1-prompt.md — this prompt, verbatim.
- docs/workflow/claude-workflow-audit/status.md — living stage/decision/next-action log.
- docs/workflow/claude-workflow-audit/lanes/<lane-id>.md — one report per lane.
- docs/workflow/claude-workflow-audit/report.md (follow docs/workflow/formats.md):
  1. Executive summary.
  2. Current-state findings, cross-validated across lanes.
  3. Research findings with verdicts.
  4. Target workflow: the architect's proposed design for how work on this project should
     flow end to end (task intake, spec, implementation, review, verification, merge,
     deploy checks), what agents/skills/MCP/memory it uses, and what it removes.
  5. Recommendations ranked by impact on quality and speed: problem and evidence, change,
     benefit, quality risk, effort, reversibility, suggested Stage 2 authority
     ("agent may do" / "owner decides"), dependencies and order.
  6. KEEP list with reasons.
  7. Critic findings and how each was resolved.
  8. Numbered owner decisions, each answerable yes/no, to scope Stage 2.

## Constraints
- No changes outside docs/workflow/claude-workflow-audit/ on the branch. Do not install,
  enable or configure any MCP server, plugin, skill, hook, permission or dependency; do
  not edit rules, docs, agents, code or tests. Prototypes stay in the scratchpad and are
  only described.
- Follow AGENTS.md and CONTRIBUTING.md otherwise. Never copy secrets, tokens,
  credentials or private data from transcripts or memory; cite patterns and counts.
- Separate observed fact, inference and external claim; mark uncertainty.
- Work autonomously; do not ask the owner anything before the final reply. Turn open
  questions into numbered owner decisions.

## Final reply
Top 5 recommendations in one line each, the owner decisions, the report path, and
explicit push status of the workflow-optimization branch (state that main is untouched).
