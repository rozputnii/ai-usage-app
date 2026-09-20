# Contributing

## Development procedure

1. Inspect the current request and Git state. Preserve unrelated tracked and untracked changes; read only relevant requirements and evidence.
2. State the intended result and acceptance checks in a short written plan before substantial work. Available planning tools are optional.
3. Implement and verify within the requested scope without repeated approval for internal steps. Ask only for missing decisions affecting scope, product intent, significant architecture, dependencies, security boundaries, or external/destructive authority. Complete independent preparation first.
4. Keep one active feature unless the owner explicitly requests a bounded batch. A backlog status or historical permission never starts another task.
5. Review the integrated diff against acceptance criteria, run relevant checks and record actual results and limitations. Never weaken requirements to hide a failure.
6. Commit and push to `main` under the Git policy below as work progresses, not only at the end. After completion and successful required verification, report changes, evidence and remaining work. On interruption, record one exact next action in the selected task document, with blockers and relevant check results.

## Git policy

Work directly on `main` by default. Standing owner instruction (2026-09-20): do not
create new branches; commit and push to `main` automatically and frequently so that no
work is lost, without asking again. Push after each meaningful step, not only at task
completion, and always before ending a session, going idle or handing off. This
instruction replaces the earlier task-branch default and applies across sessions until
the owner changes it. Create a branch only when the owner explicitly asks for one in the
current request.

Because commits may capture incomplete work, a commit is a save point, not a completion
claim. Never let an automatic commit or push upgrade a NOT_RUN, BLOCKED or FAIL result to
PASS; record actual status in the task and verification documents as usual, and say in the
commit message when the work is still in progress.

Inspect outgoing commits and preserve unrelated tracked and untracked changes. Keep
credentials, local sessions, private account data and generated output out of Git. Never
force-push, rewrite published history, or bypass protection. If the remote has diverged,
integrate it normally; if that is not possible or the target is ambiguous, stop and report.
Do not reset, stash, stage or discard unrelated work automatically.

Releases, tags, workflow dispatch, repository settings changes and other remote actions
beyond committing and pushing `main` still require explicit owner authorization. An old
task-specific permission is not a new grant.

Main protection remains deferred in AIU-026; this does not waive relevant checks or
remote-action authority. Native tool permissions are separate from these instructions.

## Records

Small fixes need a brief plan and check evidence in their completion/commit record, without a mandatory spec or backlog item. Features need a spec and verification; tasks are optional when decomposition adds no value. Add a design for meaningful architecture, authentication or data-lifecycle choices, and an ADR only for a durable decision. Follow [document formats](docs/workflow/formats.md). Goals own outcomes, backlog owns feature status, tasks own internal state and handoff, and verification owns observed evidence.

## Review and integration

The primary alone updates canonical state and integrates changes. Explicit parallel work requires declared ownership, safe paths, isolated write workers and verification of the integrated result. Review actual diffs; a worker claim is not completion.

Routine edits require primary diff/acceptance review and relevant checks. Material credential, destructive-data or privilege changes require focused independent review; public release approval or an explicit owner request requires full independent review. Use fresh, relevant evidence and read-only review with appropriate scope, without a prescribed vendor or model family. Report unavailable required review honestly. Zero findings is valid; after fixes run targeted checks, without an automatic full-review loop. Unresolved material findings block integration.

## Contributions and checks

External pull requests are welcome under MIT and the Developer Certificate of Origin. Sign off only with your real authorized identity; never fabricate identity or sign-off. Issues are intake, not execution authorization. Keep credentials, local sessions, private account data and generated output out of Git. Preserve opaque user/provider data.

Select required checks using the [change-based verification matrix](docs/workflow/verification.md#checks-by-change), with commands from [local checks](README.md#local-checks). After required checks pass, repeat or broaden them only for new changes, failures or unresolved concerns. Owned-code warnings are errors. Tests defend observable behavior and meaningful failure boundaries. UI changes require applicable actual Windows smoke evidence; compilation alone is insufficient. CI keeps document and product regressions, unsigned package/routing builds and smoke-harness publication; publication is not interactive test execution.
