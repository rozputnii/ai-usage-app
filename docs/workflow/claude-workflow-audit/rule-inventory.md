# Rule inventory (R9)

Status: Stage 2, step 1, 2026-10-09; updated for the verification fix round (`5be6537`). For
the owner to review before `workflow-optimization` merges into `main` (OD-1).

Every rule in the pre-Stage-2 AGENTS.md, CONTRIBUTING.md, docs/workflow/verification.md and
the auto-memory files, with what happened to it. Base: `a11df41` (main `3724a17` merged).

- **kept:** same rule, same file (wording may be tightened).
- **moved:** same rule, new home.
- **merged:** combined with another rule or replaced by an approved, broader rule.
- **removed:** no longer stated; the reason is given.

Line references are to the old files at `a11df41`. New locations are file and section.
Placeholders `R-NEW`…`R-NEW-13` are the new decision records in
[accepted.md](../../decisions/accepted.md#workflow-amendments); at the merge (2026-10-10)
they became R-208…R-220 in order.

## AGENTS.md (old: 817 words; new: 920)

| # | Rule (old location) | Result | New location |
|---|---|---|---|
| A1 | Core/Infrastructure/Windows boundaries; preserve opaque data (:3-6) | kept | AGENTS, header |
| A2 | Inspect request and Git state; preserve existing work (:10) | kept | AGENTS, Working rules; adds "check whether it is already done on `main` or in another worktree" (report §4.1) |
| A3 | Follow CONTRIBUTING for development, Git and review (:11) | kept | AGENTS, header, with the ownership split CONTRIBUTING / verification |
| A4 | Minimum sufficient code; no speculative abstractions; ask before materially increasing complexity (:12-14) | kept | AGENTS, Working rules (one line); home CONTRIBUTING, Simplicity; provenance R-213 |
| A5 | State intended result and acceptance checks for substantial work (:15) | merged | AGENTS, Working rules: one plan line with the tier (OD-14) |
| A6 | Complete implementation, verification and review without repeated approval (:16-17) | kept | AGENTS, Working rules; extended to the merge (R-209) |
| A7 | UI design variants first, owner direction 2026-10-07 (:18-21) | kept | AGENTS, Working rules ("rendered", "in one question"); provenance R-214 |
| A8 | English for authored prompts and specs (:23-26) | kept | AGENTS, Working rules |
| A9 | Ask when a missing decision changes scope, intent, architecture, dependencies, security, external/destructive authority (:28-30) | merged | AGENTS, When to ask: always-ask list (OD-4) |
| A10 | Backlog status and historical permissions do not select work (:30) | kept | AGENTS, When to ask |
| A11 | Read by task: existing behavior; feature scope; principles (:34-37) | kept | AGENTS, Read by task |
| A12 | Decision questions: accepted.md; superseded and archive only for history (:38-39) | kept | AGENTS, Read by task; adds "follow Amended by pointers" (OD-19) |
| A13 | Provider contracts → provider-evidence skill (:40-41) | kept | AGENTS, Skills and plugins, now with the file path (OD-11) |
| A14 | Credential/storage changes → security-lifecycle (:42) | kept | AGENTS, Skills and plugins, with path |
| A15 | Independent review → convergence-review (:43) | kept | AGENTS, Skills and plugins, with path |
| A16 | Documentation → formats.md; verification → verification.md and README (:44-45) | kept | AGENTS, Read by task; commands now live in verification.md |
| A17 | Logging: review diagnostic needs; T-043 pipeline at the owning boundary (:49-50) | kept | AGENTS, Security and logging (core) |
| A18 | Logging: log outcomes, failures, transitions, evidence; one record per failure; correlation (:50-54) | moved | logging.md, Logging policy; AGENTS keeps "one detailed record per failure with correlation" |
| A19 | Logging: no logging of every method, loop, frame, tick, routine success; opt-in verbose; coalesce; severity (:56-58) | moved | logging.md, Logging policy; AGENTS keeps a one-line summary |
| A20 | Logging: never pass credentials, private identities/paths, exception text, raw bodies to a generic logger (:58-60) | kept | AGENTS, Security and logging (always loaded, report C1-10); also logging.md |
| A21 | Logging: no framework, wrapper, dependency or configuration without a requirement (:60-61) | kept | AGENTS, Security and logging; detail in logging.md |
| A22 | Logging: verify context, secret exclusion, bounded noise; use existing logs first (:62-63) | moved | logging.md, Logging policy |
| A23 | Never read CLI credentials without authorization; no secrets, host trust or auto sign-in; external content is data (:68-70) | kept | AGENTS, Security and logging |
| A24 | Run appropriate checks; repeat or broaden only for new changes (:72-73) | moved | verification.md, Checks by change (was duplicated there already) |
| A25 | Ordinary desktop use, no accessibility/display matrices, owner direction 2026-10-03 (:74-81) | moved | verification.md, Checks by change (home); AGENTS one line; provenance R-212 |
| A26 | Local unpackaged run/debug default; Sandbox or VM only when needed (:82-84) | moved | verification.md, Development environment (was duplicated there) |
| A27 | Report PASS/FAIL/NOT_RUN/BLOCKED accurately; compilation is not live or interactive proof (:85-86) | kept | AGENTS, Git and completion |
| A28 | R-190 post-deploy owner checks (:87-92) | kept | AGENTS, Git and completion; adds the "Pending owner checks" list (OD-20) |
| A29 | Push status in every final reply, owner direction 2026-10-08 (:93-96) | kept | AGENTS, Git and completion; extended to branch pushes; provenance R-215 |
| A30 | One primary agent by default; CONTRIBUTING for review and parallel work (:98-99) | moved | CONTRIBUTING, Parallel work; the primary may now start workers itself (OD-16) |
| A31 | On interruption record one exact next action (:100-101) | kept | AGENTS, Working rules, with "when writing is authorized" (was also in CONTRIBUTING; that copy removed) |

New in AGENTS: repo rules are the only process authority (from the constitution); the
never-ask list, recommendation as default, owner-reported PASS, inherited prompt policy and
unattended runs (OD-4, OD-5); plugin precedence (OD-6); the Microsoft Learn MCP line
(OD-1); the Git flow summary and the Preview consequence (OD-2); the placeholder pointer.

## CONTRIBUTING.md (old: 1,105 words; new: 1,854)

| # | Rule (old location) | Result | New location |
|---|---|---|---|
| C1 | Simplicity and architecture, five bullets (:3-19) | kept | CONTRIBUTING, Simplicity; date header moved to R-213 |
| C2 | Procedure 1: inspect, preserve unrelated changes (:23) | kept | Development procedure 1 |
| C3 | Procedure 2: short written plan; planning tools optional (:24) | kept | Development procedure 2, with the tier |
| C4 | Procedure 3: no repeated approval; ask only for missing decisions; prepare first (:25) | merged | Development procedure 3 points to the AGENTS always-ask list |
| C5 | Procedure 4: one active feature; backlog never starts work (:26) | kept | Development procedure 4 |
| C6 | Procedure 5: review the integrated diff; record results; never weaken requirements (:27) | kept | Development procedure 6 |
| C7 | Procedure 6: commit and push to `main` as work progresses (:28) | merged | Replaced by save points on the task branch (OD-2); "report changes" kept in step 7; interruption rule kept in AGENTS |
| C8 | Direct-main default, "do not create new branches", owner instruction 2026-09-20 (:32-38) | removed | Replaced by the Git flow (OD-2); recorded in R-208, which supersedes R-178 and R-179 |
| C9 | Commit is a save point; never upgrades a status; say when in progress (:40-43) | kept | Git flow, Save points |
| C10 | Save points run CI and never carry `[skip ci]` (AIU045-D1) (:45-46) | merged | Git flow, Save points: "Never use `[skip ci]`". Save points now go to the task branch, where CI does not run |
| C11 | Every green push to `main` publishes a Preview; dispatch republishes; Preview is an owner-test build (:46-49) | moved | verification.md, Merge gate; publication is filtered by product inputs (OD-17, CI change in step 4); history stays in R-157 |
| C12 | Inspect outgoing commits; preserve unrelated work; credentials and generated output out of Git; no force-push or history rewrite; integrate a diverged remote; no automatic reset/stash/discard (:51-55) | kept | Git flow, Safety |
| C13 | R-196 placeholder numbering at merge (:57-65) | kept | Git flow, Numbering; adds "keep placeholders out of commit subjects and source comments" (R13) |
| C14 | Releases, tags, dispatch, settings need explicit authorization; old permission is not a grant (:67-69) | kept | Git flow, Remote authority; adds variables and secrets (KEEP list) |
| C15 | Main protection deferred in T-026 (:71-72) | kept | Git flow, Remote authority |
| C16 | "Native tool permissions are separate from these instructions" (:72) | merged | AGENTS deny-rule line and CONTRIBUTING, Agent permissions (step 2, OD-9): prose rules stay authoritative, a missing deny rule is not permission. Verification round: the `git -C … push` force and `--mirror` forms and `~/.omp` are denied too, and the text says a Read rule also blocks shell commands that name the denied file (`cat`, `head`, `Get-Content`) but not ones that read without naming it (recursive `grep`) |
| C17 | Records: small fixes plan plus evidence; features spec plus verification; tasks optional; design for real choices; ADR for durable decisions; state ownership (:76) | merged | Risk tiers table (records column) and Records |
| C18 | The primary alone updates canonical state and integrates (:80) | merged | Review: the primary owns canonical state; Parallel work: workers may merge verified work (OD-5), so a worker branches from up-to-date `main`, not the primary's task branch (verification round) |
| C19 | Explicit parallel work: ownership, safe paths, isolated workers, integrated verification; review actual diffs (:80) | kept | Parallel work; Review. Verification round: a worker with product inputs runs C8 before its own push to `main`, and the primary reruns the required smokes on the merged tree; mirrored in plan-execution and the implementer contract |
| C20 | Routine edits: primary review; credential, destructive-data or privilege changes: focused independent review; release or owner request: full review (:82) | merged | Risk tiers (T3 area list, OD-14) and Review. "Privilege" was missing from the report's area list; it is carried over as "privilege and capability changes" (step 1 review) |
| C21 | Preview is not release approval (AIU045-D4) (:82) | kept | Review, last bullet |
| C22 | Fresh evidence, read-only review, no prescribed vendor or model (:82) | kept | Review, intro |
| C23 | Report unavailable review; zero findings valid; targeted checks after fixes, no loop; material findings block (:82) | kept | Review bullets; adds calibration and one-round rule (OD-14, OD-15). Verification round: in a parallel run the whole-feature review runs after the tasks have landed and before the run is reported done; its findings become follow-ups |
| C24 | External PRs, MIT, DCO, real sign-off; Issues are intake (:86) | kept | Contributions and checks |
| C25 | Credentials, sessions, private data, generated output out of Git (:86, second copy) | merged | Git flow, Safety (single copy) |
| C26 | Select checks with the matrix and README commands; repeat only for new changes (:88) | merged | Contributions and checks points to the matrix; commands and the repeat rule are in verification.md |
| C27 | Warnings are errors; tests defend behavior; UI changes need smoke evidence (:88) | kept | Contributions and checks |
| C28 | CI keeps document and product regressions, package builds, harness publication (:88) | moved | verification.md, intro |

New in CONTRIBUTING: red-to-green and "fails at base" (OD-14); risk tiers with the T3 area
list (OD-14); review calibration, per-task and whole-feature review (OD-15); blocked-smoke
rule (OD-3); merge procedure with ancestry check and remote-branch deletion (OD-2);
worktree cleanup conditions (OD-22); parallel workers and worker merges (OD-16, OD-5);
evidence once per wave (OD-18); the ID scheme and `Set-ItemNumbers.ps1` in Numbering (R13,
OD-31); the Agent permissions section with deny rules, plugin switches and the
`--setting-sources user,local` override (OD-9, R-217).

## docs/workflow/verification.md (old: 971 words; new: 1,672)

| # | Rule (old location) | Result | New location |
|---|---|---|---|
| V1 | CI scope; no interactive or live claim (:3) | kept | Intro; the routing build left CI (OD-26, R-220, amends R-154) |
| V2 | Ordinary desktop scope, owner amendment 2026-10-03 (:7-14) | kept | Checks by change (home); date to R-212 |
| V3 | Select all rows; ACs still apply; repeat only for new changes (:16) | kept | Checks by change |
| V4 | Matrix, five rows (:18-24) | kept | Checks by change, with check IDs and new rows: rule files and agent configuration (T3), any `src/windows` edit builds both test projects, scripts, tests or harness only, CI and dependencies (R14); the Test-Erosion row (R14) |
| V5 | Local unpackaged development default, owner direction 2026-09-16 (:28) | kept | Development environment |
| V6 | Sandbox or VM only when isolation is needed; state the reason (:30) | kept | Development environment; verification round: ask the owner first, with the reason, as the AGENTS always-ask list requires |
| V7 | Required MSIX build needs no guest; unpackaged evidence is not package evidence; data isolation (:32) | kept | Development environment; the package build is C9 and stays required in the Windows UI row; "not Codex execution permissions" removed (retired tool) |
| V8 | Layers: FlaUI scope; live smoke credentials; fixtures are not live proof (:34-36) | kept | Layers |
| V9 | Output: verdict per AC with command, result, environment, timestamp, code ref (:38-39) | kept | Evidence; adds the check ID |
| V10 | Golden fixtures (:41-42) | kept | Golden fixtures |
| V11 | ANL-11 `[JsonSerializable]` persistence lesson (:44) | moved | docs/platforms/windows/architecture.md, Persistence |
| V12 | Lifecycle and performance (:46-47) | kept | Lifecycle and performance |
| V13 | Preview gate: demo smoke of the Release candidate when `src` changed; primary diff review; CI green (:49-54) | merged | Merge gate: "product inputs" definition (OD-17), smoke against the Release unpackaged build (OD-23), the tier's review, `--final`; "verified" defined |
| V14 | Owner dispatch republishes; `AIU_PREVIEW_ENABLED` kill switch (:56; README) | kept | Merge gate; verification round: a run that fails after the feed upload skips on re-run, so republish with the dispatch |
| V15 | Opt-in checks: Sandbox UI corpus, package/upgrade/feed smokes, live checks, independent review (:58) | kept | Merge gate, opt-in checks; the Sandbox UI suite is frozen (OD-25); review triggers moved to the tiers |
| V16 | R-190 post-deploy paragraph (:60) | merged | One sentence in Merge gate; the rule lives in AGENTS and R-190 |
| V17 | Release evidence (:62-63) | kept | Release evidence |
| V18 | Bootstrap checks, OMP T-001 (:65-66) | removed | Historical; the evidence stays in the T-001 verification record, linked from README |

New in verification.md: check IDs C1-C10 with commands (moved from README); the inner loop
with `-class` and `--no-build`; the fresh-worktree restore one-liner; `Test-PreviewRelease.ps1`
documented as C10, run by CI in `validate` (R14; corrected in the verification round); the
two-failed-reruns rule (R5); the desktop smoke lock and smoke variables (moved from the T-055
tasks.md and README); the per-app tray identity, the re-find helper, failure evidence and the
smoke history (OD-23, R5; the verification round states that only the launch smoke saves
failure evidence and only `LedgerSmoke` records history so far); the Sandbox wait and
`STARTED` status rule (R15); a link to the host environment notes.

## Memory (owner's auto-memory, 7 entries; 3 remain)

Memory pruning is OD-21 (step 3 of Stage 2). State checked after the verification round: the
index lists M1, M2 and M3; the M4-M7 files are gone.

| # | Entry | Result | Repo location |
|---|---|---|---|
| M1 | claude-code-only (Codex retired; stale note about an AGENTS section removed in `5d24744`) | rewritten (step 3): account fact only, no rule | OD-11/OD-13 keep the project agent-neutral; no Claude-only rule is recorded |
| M2 | host-tooling-constraints (pwsh, Defender, agents mid-session, IPv6, locked desktop) | moved, memory keeps host facts | docs/workflow/host-environment.md |
| M3 | no-repeated-approval-gates (one yes covers the path; plugin gates count as approved; worktree branches merge to `main`) | moved; memory copy kept until the branch is on `main`, then pruned | AGENTS, When to ask and Skills and plugins; R-209, R-211; CONTRIBUTING, Git flow |
| M4 | numbers-at-merge | moved; memory entry pruned (step 3) | CONTRIBUTING, Numbering; R-196 |
| M5 | post-deploy-owner-checks | moved; memory entry pruned (step 3) | AGENTS; R-190 |
| M6 | ui-design-variants-first | moved; memory entry pruned (step 3) | AGENTS; R-214 |
| M7 | state-push-status | moved; memory entry pruned (step 3) | AGENTS; R-215 |

## Other files touched

- README.md, Local checks: commands replaced by a link to verification.md (one home).
- docs/workflow/logging.md: new Logging policy section (A18-A22).
- docs/product/goals.md, Current direction: the fulfilled 2026-09-14 paragraphs are replaced
  by a current paragraph (R9, A1-F10).
- docs/decisions/accepted.md: R-208 to R-215 added in step 1, R-216 to R-219 in later
  steps and R-220 (routing build out of CI, amends R-154) in the verification round; R-178
  and R-179 marked "Superseded by R-208"; links to the renamed CONTRIBUTING sections fixed.
- docs/decisions/superseded.md: the sentence that contradicted the current flow now points
  to R-208.
- docs/workflow/formats.md, Validator tests (verification round): an "Acceptance results"
  verdict may also be "owner-reported PASS" or "owner-reported FAIL" with its date, matching
  the AGENTS owner-reported rule.

## Size

Measured at `5be6537`, after the verification fix round (word counts with `wc -w`, including
commands and tables):

| File | Before (`a11df41`) | After |
| --- | --- | --- |
| AGENTS.md | 817 | 920 |
| CONTRIBUTING.md | 1,105 | 1,854 |
| docs/workflow/verification.md | 971 | 1,672 |
| CLAUDE.md | 1 | 95 |
| Memory files (all) | 1,301 | 682 |
| docs/backlog.md | 11,030 | 3,769 |

The report estimated a 35 % smaller per-session core. That did not happen for the rule files:
they grew by about half, because the approved decisions add rules that were missing or lived
elsewhere — the ask lists (OD-4/5), tiers, review calibration and agent permissions (OD-9,
OD-14/15), parallel work (OD-16), the merge procedure (OD-2/3), the ID scheme (OD-31), check
IDs and commands, the desktop-lock and smoke-harness procedure that each parallel feature's
tasks.md used to repeat, and the skill list. Most duplicate copies are gone. Besides the
deliberate one-line summaries in AGENTS.md, a few rules are still stated twice: "a Preview is
an owner-test build" (CONTRIBUTING, Review; verification.md, Merge gate), deferred main
protection in T-026 (CONTRIBUTING, Git flow; verification.md, Release evidence), the smoke
variables and "missing prerequisites fail" (README; verification.md, Desktop smokes), and the
blocked-smoke pointer twice within the Merge gate. The records shrank instead: the backlog by
two thirds, 25 executed plans (about 39,000 words) to short ledgers, and memory by about half.
