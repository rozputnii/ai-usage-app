---
schema_version: 1
active_goal: G-002
---
# Goals

## G-001 - Verified OMP development workflow
- status: done
- scope: AIU-001
- outcome: Historical bootstrap proved fresh-session repository recovery in OMP; the executable bridge is now retired and its evidence retained.
- success: Historical bootstrap adopted decisions; verified stable OMP configuration, skill and command discovery; ranked selection; pause/resume; isolated workers; validator and reviewer checks; truthful redacted verification.

## G-002 - First Windows result: runnable package and Codex end-to-end
- status: selected
- scope: AIU-002, AIU-003, AIU-004, AIU-006
- outcome: The Windows app launches offline and subsequently supports real Codex authentication, quota, cache, tray, reauthentication and clean forward upgrades.
- success: Installable MSIX and UI smoke; one verified authentication/quota path; fresh/cached state; safe manual/background refresh; update and recovery tests.
- non-goals: Other providers, production cloud signing, Microsoft Store, full charting or remote compatibility infrastructure.

## G-003 - Unified Windows v1
- status: idea
- scope: AIU-005, AIU-007, AIU-008, AIU-009, AIU-010, AIU-011, AIU-012, AIU-013, AIU-016, AIU-027, AIU-028
- outcome: Four providers with required contexts/groups, multiple accounts, history, notifications, diagnostics and data lifecycle.
- success: Every completed provider has source evidence, deterministic tests and live verification. Record unavailable access as a blocker, not a completed provider. Advanced features must not displace core reliability.

## G-004 - Reliable public direct distribution
- status: idea
- scope: AIU-014, AIU-015, AIU-017, AIU-026
- outcome: Trusted signing where eligible, Preview for every green merge, manual exact-artifact Stable promotion, tested direct updates and best-effort compatibility detection.
- success: Published-schema upgrade coverage, clean-machine install, no-downgrade channel switching, identical promoted artifact hash and proven production trust/identity.

## G-005 - Later platforms and extensions
- status: idea
- scope: AIU-018, AIU-019, AIU-020, AIU-021, AIU-022, AIU-023, AIU-024, AIU-025
- outcome: Stabilize Windows before implementing Android, Store distribution, Widgets, WSL import, ARM64, multi-window/palette, forecasting or always-on coding infrastructure.

## Current direction
Owner amendment, 2026-09-14: add AIU-027 as an intermediate architecture refinement and cleanup task under G-003, prioritized before AIU-007 (Claude integration). Strengthen the existing Core, Infrastructure and Windows boundaries while preserving current behavior and stored-data formats. This addition records the task as ready; refactoring starts separately.

Owner amendment, 2026-09-14: connect all four providers using the OMP (omp.sh) connection style and deliver a usable UI design with UI/UX polish before CLI integrations. AIU-005 now covers Codex and other provider CLIs at lower priority under G-003, after provider connections and AIU-010. The current UI's functional completion does not mean its design or usability is accepted. This reprioritization does not select an implementation task.

G-002 remains the recorded product direction. AIU-002, AIU-003 and AIU-004 are complete within their retained evidence and limitations; see [backlog](../backlog.md). No next feature is selected by this migration. Historical approvals are preserved in the [superseded register](../decisions/superseded.md) as past-task context only. Follow [CONTRIBUTING](../../CONTRIBUTING.md) for current execution authority.
