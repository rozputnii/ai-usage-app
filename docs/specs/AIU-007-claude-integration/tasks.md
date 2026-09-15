---
id: AIU-007
schema_version: 1
---
# AIU-007 execution and handoff

Spec: [spec.md](spec.md). Approved private implementation design: [design.md](design.md). The primary owns canonical documents and integration. Every subagent must use `gpt-5.6-luna` with `max` reasoning. No other feature is selected.

### T-01 - Establish provider and toolchain evidence
- status: done
- depends_on: []
- acceptance: AC-01
- evidence: docs/specs/AIU-007-claude-integration/verification.md

- [x] Inspect clean Git state and confirm AIU-027 at `0a0ffd4`.
- [x] Read repository boundaries and current official .NET guidance against the pinned SDK.
- [x] Inspect OMP source and current Anthropic authentication restrictions independently of live access.
- [x] Finish exact stable source references and record the unresolved material authorization decision in PROVIDER-002.

### T-02 - Claude quota semantics
- status: done
- depends_on: [T-01]
- ownership: Claude quota value model and parser with synthetic tests
- writes: [src/windows/AiUsage.Core/Providers/Claude/ClaudeQuotaReading.cs, src/windows/AiUsage.Infrastructure/Providers/Claude/ClaudeQuotaParser.cs, tests/windows/AiUsage.Infrastructure.Tests/ClaudeQuotaParserTests.cs, tests/windows/AiUsage.Infrastructure.Tests/Fixtures/claude-usage.synthetic.json]
- shared: []
- parallel: true
- isolation: required
- agent: claude_quota
- acceptance: AC-04
- evidence: docs/specs/AIU-007-claude-integration/verification.md (integrated as 683ba35, with primary semantic corrections)

Implement the documented parser contract in the isolated `codex/aiu-007-claude-quota` worktree. Cover legacy/current precedence, opaque/inactive groups, unknown values, reset units and explicit money. The primary reviews and integrates the actual commit before this task is done.

### T-03 - Claude authentication and protected state
- status: done
- depends_on: [T-01]
- acceptance: AC-02, AC-03
- evidence: docs/specs/AIU-007-claude-integration/verification.md

Primary: write failing protocol tests, extract the proven loopback receiver, implement OMP-style PKCE and deliberately submitted-code fallback, validate account/organization identity, and secure refresh/state transitions. Exercise the same clients in the console before desktop wiring. Do not edit the quota worker's owned files.

### T-04 - Application workflow and Windows presentation
- status: done
- depends_on: [T-02, T-03]
- acceptance: AC-04, AC-05
- evidence: docs/specs/AIU-007-claude-integration/verification.md

After library verification, extract only behavior actually shared with Codex and add the minimum Claude connection and quota presentation. Prove provider isolation, no overlapping refresh and shutdown drain before desktop wiring.

### T-05 - Review, actual verification and integration
- status: in-progress
- depends_on: [T-04]
- acceptance: AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07
- evidence: docs/specs/AIU-007-claude-integration/verification.md

Complete primary and required independent review, actual packaged Windows checks and live provider proof. Record every missing case honestly. Commit the private task branch, review/check the combined local-main candidate, and integrate locally only after required acceptance passes. The latest private scope keeps this work local; no remote publication, main push or release.

## Handoff

Base: `0a0ffd4`. Branch: `codex/aiu-007-claude-integration`. The initial checkout was clean. Existing local main is `1e4f0ce`, an ancestor of the selected architecture base; main has not moved. No source credentials have been read or imported and no Claude account request has been made.

Completed checks: 130 Infrastructure and 16 Presentation/workflow/boundary tests pass on Windows with SDK 10.0.401 and existing restored packages. The provider console normalized the synthetic fixture. All six actual installed Windows scenarios pass on signed package 2026.9.1416.0; the test harness verifies foreground keyboard ownership before input. These checks do not establish live Claude verification.

The owner resolved PROVIDER-002 by explicitly selecting private, unsupported OMP-style integration. Anthropic's restriction remains recorded. The quota worker's four-file commit is integrated and primary-reviewed. The fresh Luna max credential/durable-state review found two material issues; both were reproduced, fixed and passed targeted independent follow-up at bb25558. No worker artifact or material review finding remains pending. Live consent and acceptance are outstanding. The host's existing development registration was updated to 2026.9.1416.0 without changing host trust or resetting data; its Claude panel is open for the owner. The existing Codex connection still displays its quota.

The owner resumed on 2026-09-15 and completed live sign-in in the normal desktop app. Connection, initial quota, refresh, renewal/resume after full exit, disconnect and durable removal all passed; Codex remained connected. The live verification section supersedes the earlier consent blocker. Browser consent was reopened afterward for the owner to reconnect.

Exact next action: complete the combined-candidate checks and integrate the verified private task branch into local main, then finalize the acceptance records without remote publication.
