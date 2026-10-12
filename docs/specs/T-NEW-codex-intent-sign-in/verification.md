# Codex asks for sign-in after a refused renewal - verification

Evidence for [the specification](spec.md). Environment: Windows 11 Pro 10.0.26300, .NET SDK
10.0.401, Release, desktop unlocked. Date: 2026-10-12.

## Checks

| ID | Result | Evidence |
| --- | --- | --- |
| Red at base | PASS | `ARefusedGrantStillAsksForANewSignInAfterARelaunchWithoutTraffic` failed before the fix: the next refresh returned `RecoveryRequired` |
