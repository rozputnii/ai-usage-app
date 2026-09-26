# AIU-034 verification

Phase A source research has started. The owner approved the specification on 2026-09-26.

## Gates

| Gate | Status | Record |
| --- | --- | --- |
| Specification review | PASS | Owner approved [spec.md](spec.md) in the session conversation on 2026-09-26. |
| Gate A | NOT_RUN | Owner review has not run; Phase A research is in progress. |
| Gate B | NOT_RUN | Phase B has not started. |

## Results by acceptance criterion

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | NOT_RUN | |
| AC-02 | NOT_RUN | |
| AC-03 | NOT_RUN | |
| AC-04 | NOT_RUN | |
| AC-05 | NOT_RUN | |
| AC-06 | NOT_RUN | |
| AC-07 | NOT_RUN | |
| AC-08 | NOT_RUN | |
| AC-09 | NOT_RUN | |
| AC-10 | NOT_RUN | |

## T-01 - 2026-09-26

PASS: current-source inventory and Core member coverage in research.md section 2, reviewed against baseline 27564d8. All four parsers, clients, state/cache formats and Windows LiveMapping inspected. Document validator command from tasks.md printed valid=true with no diagnostics; git diff --check exited 0. These are document/source checks only; product tests and live requests NOT_RUN. No product or stored-format change.

T-01 bookkeeping correction: the initial draft validated, but marking done with prose in the evidence field produced DONE_WITHOUT_EVIDENCE. Commit 6ee7f42 was mistakenly pushed before resolving that failure. The follow-up uses a repository-relative evidence artifact path; final validation is recorded only after rerun. No source finding changed.

## T-02 - 2026-09-26

PASS: research.md Claude plan/field matrix, G-CL-1 through G-CL-5, C1-C11 and provider append reviewed together. Pinned OMP and latest v18.3.2 inspected; no Team/Enterprise live claims, null limits stay unknown, current consumption and legacy Enterprise distinguished. Validator --json valid=true with no diagnostics and git diff --check exit 0. Live LC-01 through LC-05 NOT_RUN; no account requests. T-01 corrected evidence reference also passed validation before commit 4ed5ee4.

## T-03 - 2026-09-26

PASS: Codex matrix, G-CX-1 through G-CX-4, O1-O8 and provider append reviewed. Official pinned spend-control nested type reveals unparsed amounts/resets; units and plan response presence remain unknown. Balance never used as allotment; actual duration is response-driven; historical HTTP 400/403 causes not inferred. Validator --json valid=true and diff check exit 0. LC-06 through LC-11 NOT_RUN; no new account request.

## T-04 - 2026-09-26

PASS: Copilot matrix, G-GH-1 through G-GH-4, G1-G7 and provider record reviewed. Explicit unlimited remains distinct from unknown; current credit billing differs from legacy annual premium requests; Free live evidence is not generalized. Parser date normalization distinguished from documented UTC monthly reset. Validator --json valid=true and diff check exit 0. LC-12 through LC-17 NOT_RUN; no authenticated request.
