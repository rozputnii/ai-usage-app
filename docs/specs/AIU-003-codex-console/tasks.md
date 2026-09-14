---
id: AIU-003
schema_version: 1
---
# AIU-003 execution tasks

### T-01 - Establish pinned provider contracts
- status: done
- depends_on: []
- ownership: primary evidence and authority
- writes: [docs/providers/codex.md, docs/specs/AIU-003-codex-console/**, docs/product/goals.md, docs/backlog.md]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01]
- evidence: docs/providers/codex.md

### T-02 - Implement reusable Codex integration
- status: done
- depends_on: [T-01]
- ownership: provider library
- writes: [src/windows/AiUsage.Core/**, src/windows/AiUsage.Infrastructure/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-02, AC-03]
- evidence: docs/specs/AIU-003-codex-console/verification.md

### T-03 - Implement console verification surface
- status: done
- depends_on: [T-02]
- ownership: console client
- writes: [tools/AiUsage.ProviderConsole/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-04]
- evidence: docs/specs/AIU-003-codex-console/verification.md

### T-04 - Verify behavior and credential boundaries
- status: done
- depends_on: [T-02, T-03]
- ownership: primary tests and acceptance
- writes: [tests/windows/AiUsage.Infrastructure.Tests/**, docs/specs/AIU-003-codex-console/**, docs/providers/codex.md, docs/product/goals.md, docs/backlog.md]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
- evidence: docs/specs/AIU-003-codex-console/verification.md

## Handoff
All listed internal tasks have retained evidence. Read verification.md for observed results and limitations, and docs/backlog.md for feature status. The old session chronology is preserved in docs/decisions/superseded.md.

Next action: wait for the owner's next selected scope; this migration selects no product feature. Do not infer an account, guest, host or remote action from historical approval.
