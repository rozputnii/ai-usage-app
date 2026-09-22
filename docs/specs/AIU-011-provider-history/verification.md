# AIU-011 verification

## Scope preparation - 2026-09-22

Base: `7d0bf06` on clean `main`. Documentation-only scope split and initial source
assessment; no product implementation, live-provider read or credential access.

| Check | Verdict | Evidence or limitation |
| --- | --- | --- |
| Owner scope captured | PASS | All four providers; provider-supplied usage history; fewer required clicks; local collection deferred to low-priority AIU-029 after stability. Proposed detailed interaction remains draft. |
| Initial public-source assessment | PASS | Sources, exact source references and access uncertainties recorded in research.md. This does not satisfy complete AC-01 contract discovery. |
| Product implementation and regression suites | NOT_RUN | No product code changed. |
| Authenticated provider history / Windows interaction | NOT_RUN | No live access or actual history UI implementation tested. |
| Document validation | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`: valid=true, diagnostics=[], exit 0. The initial run reported GOAL_SCOPE because the new AIU-029 membership was absent from G-003; adding that membership resolved the finding. |
| Diff and primary acceptance review | PASS | `git diff --check`: exit 0. Reviewed scope split, dependency change, existing-decision amendment, source provenance, explicit draft proposals and absence of invented live/support claims. No production code or credentials changed. |

This preparation did not complete AIU-011. PD-011-01 was unresolved at this point and
was subsequently resolved by the owner as recorded below.

## Existing authorization and OMP parity assessment - 2026-09-22

Base: `8779d55` on clean `main`. The owner excluded additional authorization and directed
matching OMP's history retrieval for each provider if it exists.

| Check | Verdict | Evidence or limitation |
| --- | --- | --- |
| Stable OMP reference | PASS | Public releases/latest returned v18.2.8, published 2026-09-21T17:31:56Z; tag resolved to 5e0fc867f8a58dfe8812b5e99b2e7b6a0313da6c. The recursive source tree was not truncated. Relevant raw files were fetched at that exact commit. |
| AC-01 selected-path assessment | PASS | Inspected all four current OAuth adapters, UsageReport/UsageHistoryEntry, AuthStorage recording and retrieval, SQLite history persistence, CLI history branch, broker route and upstream history test source. Exact references and provider-specific exclusions are in research.md. |
| AC-02 implementation feasibility | BLOCKED | OMP's selected OAuth paths return current quota, while history is accumulated locally. Copilot's billing branch requires api_key credentials. No selected path yields a provider-supplied historical dataset. |
| Credential and upstream preservation | PASS | No real credentials, browser sessions or OMP databases read; local OMP checkout unchanged. Public source inspection only. |
| Product implementation / upstream test execution / live history / Windows UI | NOT_RUN | No eligible provider-history implementation to execute. Source inspection does not establish live-provider behavior. |
| Document validation and diff review | PASS | README validator command: valid=true, diagnostics=[], exit 0. `git diff --check`: exit 0. Primary review confirmed existing-auth-only scope, pinned OMP source, resolved access decision, truthful blocking status and continued deferral of local collection. |

AIU-011 is blocked, not completed or silently expanded. Local sampling remains deferred
in AIU-029, whose unnecessary dependency on remote-history completion was removed.

## Broader provider-history feasibility - 2026-09-22

Base: `c46e708` on clean `main`. Documentation-only follow-up to the owner's question
about general feasibility; existing authorization remains the boundary.

| Check | Verdict | Evidence or limitation |
| --- | --- | --- |
| Codex historical transport | PASS | Inspected official development source at 2c2a42e65de077c5518ea5b4c3999633ef6a12fc: analytics routes, shared auth headers, account/user-bound session, dated response models and upstream test source. Also inspected the pinned Codex Watch client. This establishes a candidate, not live access. |
| Release distinction | PASS | GitHub latest release returned rust-v0.155.1, published 2026-09-18T20:03:04Z; annotated tag resolved to be2951ea34f0d295ed0becf97079f92fa5f6950e. The specific analytics client file returned 404 at that tag. No claim that the development feature shipped in that release. |
| Other provider evidence | PASS | GitHub documents historical reports and fine-grained access; current OAuth eligibility is unresolved. Official Claude and Antigravity help pages describe credit history but do not establish compatible historical transport. See research.md for references and limits. |
| Credentials, provider reads, product code and upstream tests | NOT_RUN | No real credential reads, authenticated requests, source-CLI execution, production edits or upstream test execution. Public code and documentation inspection only. |
| AC-02 real dataset through AI Usage | NOT_RUN | No live history dataset fetched. Candidate source code does not complete the feature. |
| Document validation and diff review | PASS | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`: valid=true, diagnostics=[], exit 0. `git diff --check`: exit 0. Primary review checked source provenance, development/release distinction, conditional OMP preference, unchanged authorization boundary and absence of live-success claims. |

The OMP-specific result remains true. It no longer justifies treating all remote history
as unavailable; the backlog returns to research-needed with Codex as the first concrete
existing-session candidate. The low-click UX and local-history deferral are unchanged.
