# Contributing

## Simplicity and architecture

Implement the requested functionality with the least code and complexity that remains
correct, readable and easy to change.

- Prefer direct solutions, clear names and small, cohesive responsibilities. Minimize
  code to maintain, not line count at the expense of clarity.
- Preserve the existing Core, Infrastructure and Windows boundaries. Make extension
  straightforward through clear responsibilities and explicit dependencies, without
  building speculative extension points or a framework for future requirements.
- Reuse existing code and platform capabilities. Add layers, interfaces, wrappers,
  dependencies or configuration only when a concrete current requirement needs them.
- Do not expand scope with unrequested features, exceptional scenarios or unrelated
  refactoring. Keep verification proportional to the actual change and owner-approved use.
- Before materially increasing complexity, explain the concrete need, the simplest
  viable alternative and the maintenance cost, then ask the owner. Routine choices
  within an already approved simple design do not require repeated approval.

## Development procedure

1. Inspect the request and Git state. Preserve unrelated tracked and untracked changes;
   read only the relevant requirements and evidence.
2. Choose the tier (below) and state the intended result and acceptance checks in a short
   plan. Planning tools are optional.
3. Implement and verify within the requested scope without repeated approval for internal
   steps. Ask only what the always-ask list in [AGENTS](AGENTS.md#when-to-ask) covers,
   after completing the independent preparation.
4. Keep one active feature unless the owner asks for a bounded batch. Backlog status or a
   historical permission never starts another task.
5. Test logic and bug fixes red to green: write the test, run it and see it fail for the
   expected reason, implement the minimum, and run it green. A bug-fix test must fail at
   the base commit.
6. Review the integrated diff against the acceptance criteria, run the required checks and
   record the actual results and limitations. Never weaken requirements to hide a failure.
7. Merge under the Git flow below and report changes, evidence and remaining work.

## Risk tiers

The tier is the highest one that any touched area triggers; the owner may raise it. The
reviewer checks the diff against the T3 area list.

| Tier | Typical change | Records | Review |
| --- | --- | --- | --- |
| T0 | A fix or small change in no T3 area | Plan and check evidence in the commit message | The primary reviews the diff |
| T1 | A small feature or UI variant | A one-page spec (Problem, R-xx, AC-xx, Out of scope; a bug may use current, expected and unchanged behavior) and a verification record; the owner's UI pick is one spec line | One fresh-context review of the integrated diff against the ACs, plus local `/code-review` where available |
| T2 | A multi-part feature | As T1; tasks.md only when parallel workers are used | As T1 |
| T3 | Any change in a T3 area | As its size requires, plus a design note when there is a real choice; use the security-lifecycle skill | As T1, plus a blocking focused independent review with the convergence-review skill and `/security-review` where available |

T3 areas: authentication and provider contracts; credentials and DPAPI stores; persisted
records, the preferences schema and migrations; destructive or owned-root cleanup; logging,
diagnostics, crash data and exports; privilege and capability changes (package manifest
capabilities, elevation, startup or protocol registration, certificate trust); new network
hosts; update, install, signing and Preview scripts; CI workflows; dependencies; rule files
(AGENTS.md, CONTRIBUTING.md, docs/workflow/verification.md, docs/constitution.md) and agent
configuration (`.claude/`, `.agents/`, CLAUDE.md).

## Git flow

- **Branches.** Each task runs on its own branch, normally the worktree branch that the
  desktop app creates. Commit save points and push them to `origin/<task-branch>` after
  each meaningful step and before ending a session, going idle or handing off. CI does
  not run on task-branch pushes and nothing is published.
- **Save points.** A commit is a save point, not a completion claim. It never upgrades a
  NOT_RUN, BLOCKED or FAIL result to PASS; say in the message when work is in progress.
  Never use `[skip ci]`.
- **Merge.** Only verified work reaches `main` (see the
  [merge gate](docs/workflow/verification.md#merge-gate)). At completion: fetch, merge
  fresh `origin/main` into the branch (never rebase a published branch), assign numbers,
  run the required checks and the validator with `--final` on the merged tree, and push
  to `main`. Confirm with `git fetch` and `git merge-base --is-ancestor` that the commits
  are in `origin/main`, then delete the remote task branch. If the push is rejected
  because `main` moved, repeat these steps.
- **Blocked smoke.** When a required smoke is BLOCKED, a change that touches Windows UI,
  tray, launch or lifetime code stays on its branch until the smoke passes or the owner
  approves that change. Other product changes covered by unit tests may merge with the
  smoke recorded BLOCKED.
- **Worktree cleanup.** Remove a worktree at the last merge step, and only when its
  branch is an ancestor of `origin/main` after a fresh fetch, its status is clean
  including ignored files, and no stash entry refers to it. Never use `--force` or
  `git branch -D` for this.
- **Safety.** Inspect outgoing commits. Keep credentials, local sessions, private account
  data and generated output out of Git. Never force-push, rewrite published history or
  bypass protection. If the remote has diverged, integrate it normally; if that is not
  possible or the target is ambiguous, stop and report. Do not reset, stash, stage or
  discard unrelated work automatically.
- **Remote authority.** Releases, tags, workflow dispatch, repository settings,
  variables, secrets and other remote actions beyond pushing branches and `main` require
  explicit owner authorization. An old task-specific permission is not a new grant.
  Main protection remains deferred in T-026; that waives no check or authority.

### Numbering

Numbers for new items are assigned only when the work merges into `main`, because the
owner runs tasks in parallel worktrees (R-196). Until then a new backlog item and its
specification folder use `T-NEW` (`T-NEW-2` for a second one) and a new decision
uses `R-NEW` (`R-NEW-2`, ...). Right before merging, after merging fresh `main`, replace
every placeholder with the next free number there: the specification folder,
frontmatter, backlog, goal scope, decisions and every reference. Keep placeholders out
of commit subjects and source comments. `tools/windows/Set-ItemNumbers.ps1` does the
replacement, renames the folder and runs the `--final` validation; review its diff before
committing. CI validates with `--final`, so a placeholder never stays on `main`. An item
already numbered on `main` keeps its number.

## Agent permissions

The committed `.claude/settings.json` holds deny rules only: force-push variants (including
`--mirror` and `+refspec`), `gh release` and `gh workflow run` in Bash and PowerShell, and
reads or edits of source-CLI credential stores through the agent's file tools. It also
disables plugins for this project: ux-superpowers, design-superpowers, desktop-commander and
design (OD-8), and superpowers, whose used skills have repository replacements (OD-7). Deny rules apply in every permission mode, and a
local allow cannot lift them. Changing this file is T3 and needs the owner's decision. The
patterns are a backstop only: other spellings, scripts and shell reads such as `cat` or
`Get-Content` are not blocked, so the prose rules stay authoritative.

When the owner explicitly authorizes a provider-evidence read of a CLI credential store in the
current request, the owner either performs the read or starts that one session with
`claude --setting-sources user,local`, which skips all project settings, including the other
deny rules and plugin switches. That session does only the authorized read; the next session
loads the deny rules again.

## Records

Follow [document formats](docs/workflow/formats.md) and size records by tier. Goals own
outcomes, the backlog owns feature status, tasks own internal state and handoff, and
verification owns observed evidence. Add a design for meaningful architecture,
authentication or data-lifecycle choices. Record a decision only for a rule that binds
future work. In parallel runs, record evidence once per wave or at the end of the
feature, not in separate per-task record commits.

## Review

The primary alone updates canonical state: goals, backlog, decisions and the feature's
verification record. Review actual diffs with fresh, relevant evidence; a worker's claim
is not completion. No model vendor or family is prescribed.

- Review by tier (above). A fresh-context, read-only subagent with no implementation
  transcript counts as independent; use the tracked reviewer agent, or a general-purpose
  agent given the same contract when the tracked one is not loaded.
- In parallel runs, each code, test or harness task gets a per-task independent review.
  Docs tasks get a primary diff check, except rule and agent-configuration edits, which
  are T3. One whole-feature review always runs before the merge.
- An Important finding violates an acceptance criterion, the spec, or a security or data
  boundary, or is a reproducible defect; it needs file:line evidence and a failing test or
  a reproduction. Report at most five Minor findings. Zero findings is a valid result.
- One review round, then a re-check of the fixed lines only, without an automatic
  full-review loop. Unresolved material findings block integration. Report an
  unavailable required review honestly.
- For a bug fix, the reviewer confirms in a temporary worktree outside the checkout that
  the new test fails at the base commit.
- A Preview is an owner-test build, not public release approval. Full independent review
  is required for public release approval or when the owner asks for it.

## Parallel work

One primary agent works by default. The primary may start parallel worktree workers
itself when there are at least three independent tasks with non-overlapping write sets,
running at most about four at once; the owner may also ask for a parallel run. Explicit
parallel work requires declared ownership, safe paths, isolated write workers and
verification of the integrated result.

- Worker briefs are self-contained. The tracked implementer agent holds the worker
  procedure: simple single-purpose commands, background tasks or Monitor instead of
  sleep, unit suites and builds per worker, reports returned as messages.
- Workers may merge verified work into `main` and push it under the Git flow above. Workers
  do not add backlog items or decisions; the primary owns those and their numbering.
- Desktop smokes run under the desktop lock in
  [verification](docs/workflow/verification.md#desktop-smokes); after integration the
  primary runs them once on the merged tree.

## Contributions and checks

External pull requests are welcome under MIT and the Developer Certificate of Origin. Sign
off only with your real authorized identity; never fabricate identity or sign-off. Issues
are intake, not execution authorization. Preserve opaque user and provider data.

Select required checks with the
[change-based verification matrix](docs/workflow/verification.md#checks-by-change). Owned-code
warnings are errors. Tests defend observable behavior and meaningful failure boundaries.
UI changes require applicable actual Windows smoke evidence; compilation alone is
insufficient.
