---
name: plan-execution
description: Execute an approved multi-part (T2/T3) plan, sequentially or with parallel worktree workers, through review and a verified merge. Use after planning when work has several tasks, or when the owner asks for a parallel run.
---
# plan-execution

The repository's execution step (decision R-NEW-10). It replaces the superpowers
writing-plans, executing-plans and subagent-driven-development skills and follows
CONTRIBUTING.md (Development procedure, Git flow, Review, Parallel work), which wins wherever
this text seems to differ.

## Plan

- Sequential work needs no plan document: keep the task list in the session and the evidence
  in verification.md.
- Parallel work needs `tasks.md` in the feature folder (docs/workflow/formats.md, Task blocks):
  one `### T-nnn.k` block per task with `status`, `depends_on`, `ownership`, `writes`, `shared`,
  `parallel`, `isolation`, `agent`, `acceptance` and `evidence`, plus an open "Next action".
  Write decisions and exact values into the blocks, not code. End with a "Review focus" list:
  the places most likely to go wrong.

## Execute

- **Sequential:** one task at a time, red to green, the task's checks, commit, push the save
  point to the task branch.
- **Parallel:** when the owner asks, or when there are at least three independent tasks with
  non-overlapping write sets; at most about four workers at once. Give each worker a
  self-contained brief and the implementer contract (`.agents/agents/implementer.md`), in its
  own worktree branched from up-to-date `main`, not from the primary's task branch, because
  workers merge into `main` themselves.
  Workers do not add backlog items or decisions. Wait with background tasks or Monitor, never
  sleep loops; check CI once at the end, not in a foreground loop.
- Run desktop smokes under the desktop lock. A worker whose change has product inputs runs the
  launch smoke (C8) before its own push to `main`; after the tasks have landed, the primary
  runs the required smokes once more on the merged tree.

## Review

- In parallel runs, each code, test or harness task gets a per-task review by the reviewer
  contract (`.agents/agents/reviewer.md`), dispatched by the primary. Docs tasks get a primary
  diff check; rule and agent-configuration edits are T3. Sequential work gets the tier's review
  of the integrated diff.
- One whole-feature review runs, plus the focused T3 review when the tier requires it. In a
  parallel run it runs after the tasks have landed and before the run is reported done, and
  its findings are fixed as follow-ups. One round, then a re-check of the fixed lines only.
- Use the receiving-code-review skill to evaluate findings; unresolved material findings block
  the merge, or in a parallel run, reporting the run done.

## Record and merge

- Record evidence once per wave or at feature end in verification.md, by check ID, with the
  commits, review verdicts and the grant; no separate per-task record commits.
- Merge under CONTRIBUTING, Git flow: merge gate on the merged tree, numbering, push, ancestry
  check, remote branch deletion, worktree cleanup. State the push status in the final reply.
