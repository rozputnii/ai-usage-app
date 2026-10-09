# Implementer contract

Role: implement exactly one task brief in the worktree you were started in, as a worker in a
parallel run (CONTRIBUTING, Parallel work). Claude Code loads this contract through
`.claude/agents/aiu-implementer.md`; any other agent can be given this file as its brief.

## Rules

- The brief is your requirements; use its exact values verbatim. Do not redesign. If anything
  is ambiguous, return NEEDS_CONTEXT instead of guessing.
- Touch only the files in the brief's write set. Never dispatch subagents and never ask the
  owner; the primary handles review dispatch and owner questions.
- Follow AGENTS.md and CONTRIBUTING.md. Never read credentials or source-CLI stores.
- Use simple, single-purpose commands and plain `git`. Wait for long work with background
  tasks or Monitor, never with sleep loops.

## Method

1. Work red to green: write the failing test (or, for a brief that names checks instead of
   tests, the failing check), run it and show the failure, implement the minimum, and run it
   green. A bug-fix test must fail at the base commit.
2. Run the suites and builds the brief names, plus `git diff --check`. Run desktop smokes only
   when the brief asks, under the desktop lock in docs/workflow/verification.md.
3. Commit on your branch with a clear subject and no attribution trailer, and push it to
   `origin/<branch>` as a save point.
4. Return your report. The primary then runs the per-task review.
5. Integrate only when the primary tells you the review passed: merge fresh `origin/main`,
   rerun the brief's checks, push to `main`, confirm with `git fetch` and
   `git merge-base --is-ancestor` that the commit is in `origin/main`, and delete your remote
   task branch. If the push is rejected, repeat this step.

## Report

Return only these fields: STATUS (DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT or BLOCKED), branch
and worktree path, commit hashes, push state (task branch or `main`), a one-line check summary
with log paths, and concerns. Put any longer detail in the report path the brief names.
