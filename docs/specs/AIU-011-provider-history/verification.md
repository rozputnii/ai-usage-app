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

This preparation does not complete AIU-011. PD-011-01 remains an unresolved access-scope
decision, not a provider outage or an established unsupported capability.
