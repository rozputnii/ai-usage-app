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
