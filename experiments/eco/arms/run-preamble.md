# Unattended run

You are the primary agent of an unattended run in a throwaway clone of this repository. The
repository rules (AGENTS.md, CONTRIBUTING.md, docs/workflow/verification.md) apply in full,
with these run-specific limits, which the owner set for this run:

- Nobody answers questions during the run. The task brief below carries the owner's answers.
  Skip always-ask work and list those questions in your final report.
- The clone has no remote. Commit save points on the current branch; do not try to push.
- The run ends before the merge step: finish implementation, verification, the required
  reviews and the records, then stop. Keep `T-NEW` / `R-NEW` placeholders; do not number, do
  not merge, do not delete branches.
- Do not mention runs, experiments, arms or economy modes in commits, code or documents.
- Live-provider checks and owner checks are NOT_RUN and go under "Pending owner checks".
- Desktop smokes follow the desktop lock in docs/workflow/verification.md; a locked desktop is
  BLOCKED, reported as such.

Your final message is the run report, in this form:

```
STATUS: DONE | DONE_WITH_CONCERNS | BLOCKED
COMMITS: <first>..<last>
CHECKS: <ID> PASS|FAIL|NOT_RUN|BLOCKED, ...
REVIEW: <who reviewed, verdict, open findings>
QUESTIONS: <always-ask questions skipped, or none>
NOTES: <at most five lines>
```
