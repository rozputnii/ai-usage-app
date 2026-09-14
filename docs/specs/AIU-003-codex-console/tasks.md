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
Paused by the owner on 2026-09-14 after live verification succeeded. AIU-003 is complete: browser sign-in, real quota read, in-memory refresh and a second real quota read all passed through the console, with 54 deterministic tests and canonical validation green. Connection and usage mirror the locally cloned OMP implementation per D-177; the earlier locally invented residency rejection was removed. Remaining provider scope - device-code login, multi-workspace switching, exhausted/rate-limited responses, long-term rotation and CLI coexistence - is NOT_RUN and belongs to AIU-004/005. Public-client reuse permission remains unresolved. No credential was persisted and no source CLI store was touched. AIU-002 remains paused with its guest evidence retained. Next session: read docs/specs/AIU-003-codex-console/verification.md before starting AIU-004.
