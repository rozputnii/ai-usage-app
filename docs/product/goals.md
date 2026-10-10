---
schema_version: 1
active_goal: G-002
---
# Goals

## G-001 - Verified OMP development workflow
- status: done
- scope: T-001
- outcome: Historical bootstrap proved fresh-session repository recovery in OMP; the executable bridge is now retired and its evidence retained.
- success: Historical bootstrap adopted decisions; verified stable OMP configuration, skill and command discovery; ranked selection; pause/resume; isolated workers; validator and reviewer checks; truthful redacted verification.

## G-002 - First Windows result: runnable package and Codex end-to-end
- status: done
- scope: T-002, T-003, T-004, T-006
- outcome: The Windows app launches offline and subsequently supports real Codex authentication, quota, cache, tray, reauthentication and clean forward upgrades.
- success: Installable MSIX and UI smoke; one verified authentication/quota path; fresh/cached state; safe manual/background refresh; update and recovery tests.
- non-goals: Other providers, production cloud signing, Microsoft Store, full charting or remote compatibility infrastructure.

## G-003 - Unified Windows v1
- status: idea
- scope: T-005, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-016, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039, T-040, T-042, T-043, T-044, T-045, T-047, T-048, T-049, T-050, T-051, T-052, T-053, T-054, T-055, T-056, T-057, T-058, T-NEW, T-NEW-2, T-NEW-3, T-NEW-4
- outcome: Four providers with required contexts/groups, multiple accounts, history, notifications, diagnostics and data lifecycle.
- success: Every completed provider has source evidence, deterministic tests and live verification. Record unavailable access as a blocker, not a completed provider. Advanced features must not displace core reliability.

## G-004 - Reliable public direct distribution
- status: idea
- scope: T-014, T-015, T-017, T-026, T-046
- outcome: Trusted signing where eligible, Preview for every green merge, manual exact-artifact Stable promotion, tested direct updates and best-effort compatibility detection.
- success: Published-schema upgrade coverage, clean-machine install, no-downgrade channel switching, identical promoted artifact hash and proven production trust/identity.

## G-005 - Later platforms and extensions
- status: idea
- scope: T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-041
- outcome: Stabilize Windows before implementing Android, Store distribution, Widgets, WSL import, ARM64, multi-window/palette, forecasting or always-on coding infrastructure.

## Current direction
G-001 and G-002 are done. Current work continues G-003 (Unified Windows v1) and G-004 (Reliable public direct distribution); see the [backlog](../backlog.md) for item status. No goal or feature is selected automatically: the owner selects work under [CONTRIBUTING](../../CONTRIBUTING.md). The `active_goal` value keeps the last formally selected goal, G-002. The 2026-09-14 direction paragraphs (T-027 before T-007; provider connections and UI before CLI integrations) are fulfilled and remain in Git history. Historical approvals are in the [superseded register](../decisions/superseded.md) as past-task context only.
