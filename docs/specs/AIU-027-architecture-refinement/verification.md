# AIU-027 verification

Date: 2026-09-14. Base: `4f2d2fb`. Task branch: `codex/aiu-027-architecture-refinement`. Initial tracked/untracked checkout was clean. No other feature was started.

## Implementation and preservation

Core now owns the credential-free session state/port and dashboard application workflow. Infrastructure implements the port and retains the original authentication, credential renewal/persistence, HTTP and storage behavior. The dashboard uses injected resources and awaited dispatcher access. Explicit Exit rejects new commands, cancels/drains workflow operations and waits for presentation continuations before Host disposal; native close/hide/restore behavior is retained.

The transport/client/store source changes are namespace imports only. Session implementation changes are the extracted state types and interface declaration. Grant/cache file names, schema versions, DPAPI entropy, serialized field names, quota interpretation, authentication endpoints and refresh order are unchanged. No credentials were read or imported from a CLI; no real account sign-in occurred.

## Executed local checks

Windows x64, user-local .NET SDK 10.0.401 and Visual Studio MSBuild 18.10.1. All deterministic commands use existing assets with `--no-restore`. The new test executable was restored from the existing local NuGet cache only, with no network source or new package version.

| Check | Verdict | Observation |
| --- | --- | --- |
| Project validator regressions | PASS | 78 tests, zero failures/skips |
| Infrastructure Release regressions | PASS | 72 tests, zero failures/skips; rerun after final source changes |
| Independent workflow/presentation/dependency regressions | PASS | 12 tests, zero failures/skips |
| Canonical document validation | PASS | Valid, no diagnostics after correcting new document metadata |
| Provider console Release build | PASS | Zero warnings/errors; no account execution |
| Final native Release package compilation and signing | PASS | Version 2026.9.1411.0; unchanged pinned toolchain and offline build path |
| Host signature trust verification | FAIL (expected) | Development root untrusted; no host trust imported; signed bytes retained for guest verification |
| Diff whitespace check | PASS | No whitespace errors |

The first independent test build failed because the requested Core types were absent. Linking actual presentation sources then failed on the existing Infrastructure dependency. After extraction, tests pass without Infrastructure or WinUI references. Behavioral tests cover first-run no request, cache-before-resume, overlap rejection, shutdown cancellation/drain, fault propagation, command state, dispatcher use, stale labels and unknown/exhausted quota. Project/source checks enforce dependency direction. The packaging tool warns that `mspdbcmf.exe` is unavailable, so no symbols package was generated; no owned-code warning occurred.

## Review

Independent read-only review used a frozen 70-file source snapshot and hash manifest at `.ai-usage-local/AIU-027/review-source` and `review-manifest.json`, with base `4f2d2fb` and the actual diff. All manifest hashes matched. Verdict PASS, zero actionable findings, covering AC-01 through AC-04, cancellation, dispatcher/shutdown ownership and preservation boundaries. The reviewer did not run tests or UI smoke and reported those as NOT_RUN by the reviewer.

Primary diff review additionally found that startup failure could skip Host disposal if draining faulted. The final candidate uses `finally` to attempt disposal and adds a faulted-work regression. A targeted independent follow-up reviewed that adjustment: PASS, zero findings. Other post-snapshot source changes only normalize line endings and trailing blank lines. No unresolved material review finding remains.

## Packaged Windows lifecycle

Final candidate: `2026.9.1411.0`, package SHA-256 `EF9CE5951BBB3717309460353175A1E107969373252DCA0959C3D386CA120E27`.

Executed in disposable Windows Sandbox `7e3eb8e6-4b97-4923-8b3c-b6e2c56273bf`; evidence root `.ai-usage-local/AIU-027/guest-evidence-final`. Run began at 19:15:03 UTC. Inputs were read-only; networking and clipboard were disabled. The unchanged smoke executable/hash and harness are recorded in `orchestration/input-hashes.json`. Guest-only provisioning used the existing owned development certificate public CER, official offline framework and .NET runtime inputs. No host installation or trust change occurred.

PASS: orchestration report FINISHED, product exit 0; guest report ProductUi positive PASS, package version/hash matching the final candidate, runtime installer exit 0 and smoke exit 0. All five scenarios passed, with each original process exiting with code 0. The runtime installation took several minutes; all three component MSI logs ended with return code 0. This was provisioning time, not app startup performance evidence.

| Scenario | Observed result |
| --- | --- |
| exit | Keyboard Exit terminates cleanly |
| title-bar | Close hides while the original process remains; actual tray activation restores the same window handle; Exit terminates |
| repeated-exit | Exit followed by two native close requests terminates cleanly |
| minimize | Normal minimized state; actual tray activation restores the same window handle; Exit terminates |
| tray-exit | Close hides; actual tray context-menu Exit terminates without reopening |

Inspected `product/positive/title-bar-restored.png` and `minimize-restored.png`: Dashboard, no accounts connected, Connect enabled, Refresh/Disconnect disabled, Exit visible. These images establish restored UI state; same-window identity and termination are established by the executed UI assertions and scenario JSON. No UI redesign acceptance is claimed.

Final production source manifest: `.ai-usage-local/AIU-027/final-source-manifest.json`, SHA-256 `511CAD1063DB9771A5FD2D865628C9CB4789E6282D16BCBD6AFD7C681087F095`; all entries matched before commit. Local binary/screenshot artifacts remain ignored; this report retains the portable evidence summary.

Earlier package 2026.9.1410.0 and its guest run were superseded before acceptance after the startup cleanup correction. They are retained locally and do not establish final lifecycle acceptance.

## Acceptance and limits

- AC-01: PASS — independent Core workflow tests.
- AC-02: PASS — actual presentation sources tested independently with injected resources/dispatcher.
- AC-03: PASS — dependency regressions and neutral compilation.
- AC-04: PASS — deterministic cancellation/drain checks and actual final packaged lifecycle.
- AC-05: PASS — local regressions, reviews, final package installation and inspected guest lifecycle evidence.

Packaged real-account sign-in/resume, installation-negative scenarios and remote CI are NOT_RUN. This change does not claim fresh live-provider verification, release approval or completion of deferred multi-account/background-refresh architecture.

No acceptance blocker remains. Publication is limited to the owner-authorized task branch, with no merge or release.
