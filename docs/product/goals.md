---
schema_version: 1
active_goal: G-002
---
# Goals

## G-001 - Verified OMP development workflow
- status: done
- scope: AIU-001
- outcome: A fresh OMP session can continue repository work without the previous chat or repeated long prompts.
- success: Adopted English decisions; verified stable OMP configuration, skill and command discovery; ranked selection; pause/resume; isolated workers; validator and reviewer checks; truthful redacted verification.
- stop: Completion, an unsafe environment, missing essential permission, budget exhaustion or an owner decision that cannot be inferred safely.
- next-goal: G-002 only after owner selection or an explicit extension of Autopilot authorization.

## G-002 - First Windows result: runnable package and Codex end-to-end
- status: selected
- scope: AIU-002, AIU-003, AIU-004, AIU-005, AIU-006
- outcome: The Windows app launches offline and subsequently supports real Codex authentication, quota, cache, tray, reauthentication and clean forward upgrades.
- success: Installable MSIX and UI smoke; one verified authentication/quota path; fresh/cached state; safe manual/background refresh; update and recovery tests.
- non-goals: Other providers, production cloud signing, Microsoft Store, full charting or remote compatibility infrastructure.

## G-003 - Unified Windows v1
- status: idea
- scope: AIU-007, AIU-008, AIU-009, AIU-010, AIU-011, AIU-012, AIU-013, AIU-016
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

## Authorization is not a status label
The status records direction, not permission by itself. The owner explicitly launched AIU-001 local implementation in OMP Goal Mode on 2026-09-12 and approved the user-local SDK installation, MIT replacement and repository-local identity recorded in `../workflow/environment.md`. That local scope is complete, including bounded disposable workflow proofs and the explicitly authorized replacement review after a capture failure.

On 2026-09-13 the owner authorized a narrow post-bootstrap policy amendment: defer main protection at low priority and simplify current PR checks. This does not start another product goal or authorize remote mutation.

The owner subsequently selected AIU-002 and disposable Sandbox/VM installation verification, then approved execution of the First runnable Windows MSIX and smoke CI plan. G-002 is the selected direction; AIU-002 is the current item. This authorizes that feature's local implementation and development signing only, not other G-002 items, host package installation or certificate trust changes, elevation/reboot, provider account access, paid resources, remote publication, or unlimited bounded execution. No bounded execution record is created by this approval.

At the AIU-002 guest-environment gate, the owner explicitly chose to leave the item BLOCKED and retain the code, signed development candidate and offline verification bundle. This does not authorize Sandbox enablement, elevation, reboot, another item or automatic continuation past the missing guest proof.
