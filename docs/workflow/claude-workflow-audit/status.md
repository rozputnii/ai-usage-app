# Claude workflow audit — status

Living log for the workflow-optimization effort. Read this first in every session.

- **Branch:** `workflow-optimization`, a long-lived branch the owner requested. It is never
  merged into `main` or opened as a PR without an explicit owner decision.
- **Current stage:** Stage 1 (analysis and recommendations) is complete and awaits owner
  review. Stage 2 has not started.

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

## Open owner decisions

OD-1 to OD-30 are listed in [report.md §8](report.md#8-owner-decisions-yesno). Each can be
answered yes or no and carries a recommendation.

## Notes for later stages

- **Before the final merge into `main`:** apply OD-30, which wraps the prompt's two
  placeholder mentions in code spans, or the CI `--final` step fails.
- **At the start of every session:** run `git fetch origin` and merge `origin/main` into this
  branch, then re-check any finding whose evidence files changed.
- **Lane scratch data:** the parser scripts and intermediate data stay in the Stage 1
  session's scratchpad and are not in Git. Those files contain private transcript extracts.

## Next action

The owner answers OD-1 to OD-30. A new session then starts Stage 2 with a prompt that cites
the approved decision numbers. It begins with R1 + R7 + R6 phase 1 (OD-2..OD-6) as one change
set on this branch.
