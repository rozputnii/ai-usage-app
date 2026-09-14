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

The owner subsequently resumed AIU-002 and explicitly selected enabling Windows Sandbox, including its required official component dependencies and a UAC elevation request. This supersedes the prior stop only for obtaining the disposable guest and continuing AIU-002 verification. No automatic host reboot, firmware changes, host app installation or host certificate trust import is authorized. If a restart is required, preserve state and report it before continuing.

The authorized Windows Sandbox enablement completed successfully and required an owner-controlled restart. The owner reported completing that restart; subsequent disposable guest launches and installation verification succeeded. No automatic reboot or host app/trust installation occurred. Remaining acceptance and the later owner-directed focus change are recorded below and in AIU-002 verification.

On 2026-09-13 the owner changed the provider-development approach: build a reusable provider integration library, exercise it through a console application first, and integrate the verified implementation into the UI afterward. This removes UI readiness as a prerequisite for provider research and library/console work, but does not select AIU-003/004 for execution or authorize provider account access. G-002 remains selected and the current execution remains AIU-002.

The owner then explicitly redirected current execution to provider integration using a console application and appropriate integration/unit tests, rather than more UI work. Current item is AIU-003, Codex authentication/quota contract and an executable library/console proof. AIU-002 is paused with its guest evidence retained. This authorizes local provider research, library/console implementation and deterministic tests, not another provider, UI integration, paid resources, remote actions, unattended OAuth consent or reading personal CLI credentials without a specific import/verification decision. No bounded execution grant is fabricated.

The AIU-003 library/console slice is implemented and live-verified: on 2026-09-14 a real browser sign-in read this account's actual Codex quota, refreshed in memory and read quota again. Following an explicit owner instruction, the connection and usage read mirror the locally cloned OMP implementation per D-177. Device-code login, multi-workspace switching, exhausted and rate-limited responses, long-term rotation and CLI coexistence remain NOT_RUN, and third-party reuse of the public Codex client remains unresolved. This does not authorize CLI-token import, durable credential storage, AIU-004 UI/persistence work or another provider.

On 2026-09-14 the owner chose to finish AIU-002 before further provider work. Closure was acceptance only: retained clean-guest evidence was inspected, screenshots opened, the routing package hash recorded, criteria consolidated and temporary probes archived. No guest was launched, no product behavior changed, and no host installation, host trust change, elevation or remote action occurred. AIU-002 and AIU-003 are both complete; remote GitHub Actions execution and interactive UI CI remain NOT_RUN by authorization. Selecting the next item, including AIU-004 UI integration of the verified Codex path, still requires an explicit owner decision.
