---
id: AIU-004
schema_version: 1
---
# AIU-004 execution tasks

### T-01 - Protect one Codex grant with DPAPI
- status: done
- depends_on: []
- ownership: credential storage
- writes: [src/windows/AiUsage.Infrastructure/**, tests/windows/AiUsage.Infrastructure.Tests/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-02, AC-03, AC-05]
- evidence: docs/specs/AIU-004-codex-dashboard/verification.md

### T-02 - Show real quota on the dashboard
- status: done
- depends_on: [T-01]
- ownership: product UI
- writes: [src/windows/AiUsage.Windows/**, src/windows/AiUsage.Core/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-03, AC-04]
- evidence: docs/specs/AIU-004-codex-dashboard/verification.md

### T-03 - Verify behavior and record evidence
- status: done
- depends_on: [T-01, T-02]
- ownership: primary verification and records
- writes: [tests/windows/**, docs/specs/AIU-004-codex-dashboard/**, docs/backlog.md, docs/product/goals.md]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
- evidence: docs/specs/AIU-004-codex-dashboard/verification.md

### T-04 - Cache the last quota snapshot honestly
- status: pending
- depends_on: [T-02]
- ownership: product state
- writes: [src/windows/AiUsage.Core/**, src/windows/AiUsage.Infrastructure/**, src/windows/AiUsage.Windows/**, tests/windows/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-07]
- evidence: not-run

### T-05 - Add tray presence
- status: pending
- depends_on: [T-02]
- ownership: product shell
- writes: [src/windows/AiUsage.Windows/**, tests/windows/**]
- shared: []
- parallel: false
- isolation: none
- agent: primary
- acceptance: [AC-08]
- evidence: not-run

## Handoff
Selected by the owner on 2026-09-14 immediately after AIU-002 closure and the live-verified Codex path. Work proceeds directly on main, one task at a time, with a push after each completed task per D-178 and D-179. The provider client from AIU-003 is reused unchanged. No provider account action beyond an explicit owner-initiated sign-in is authorized.

Dashboard subtask delivered on 2026-09-14: DPAPI-protected grant storage, the session over the AIU-003 clients, and a dashboard with five distinct states. Verified by 68 deterministic tests, a Release build, and a clean disposable-guest run of signed package 2026.9.1402.0. The inspected screenshot shows the not-connected empty state with Connect enabled and Refresh and Disconnect disabled; quota rendering itself is covered by tests, not by that screenshot. Connecting a real account from the packaged UI is NOT_RUN because the guest has no networking and host installation is unauthorized; the provider path is live-verified through the console.

Still open in this item, kept from the original backlog outcome rather than deferred: T-04 minimum cached snapshot with an explicit staleness label, and T-05 tray presence. History, charts, background refresh scheduling, multiple accounts, CLI import and a database remain outside AIU-004.
