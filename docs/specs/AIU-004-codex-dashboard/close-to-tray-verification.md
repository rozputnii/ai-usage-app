# CR-AIU-004-01 verification

Date: 2026-09-14. Owner-selected follow-up to AIU-004, implementing D-108 and amended AC-08. Base: `93fb39482263f5741ce13c193e972cd894cf9a7a`; task branch: `codex/cr-aiu-004-01-close-to-tray`.

## Changes and review

Preserved and completed the existing App.xaml.cs and PackageSmoke.cs work. Dashboard close now hides its window without stopping the Host. Tray activation shows and restores that same window. Explicit Exit retains the awaited shutdown path, with overlapping close requests cancelled while shutdown runs. Startup failure remains closable without requiring a working tray.

The smoke exercises the actual tray button rather than launching another package instance. It checks the original process and window handle, normal minimized state, dashboard restoration, and successful process termination. The primary reviewed the integrated diff against AC-06 and AC-08. No provider, credential-storage or host-trust behavior changed.

The owner also requested unattended Sandbox runs. The guest harness now defaults to ProductUi, installing the supplied offline dependencies silently before app activation. InstallationContract remains an explicit mode for missing-dependency negative tests; ProductUi reports those checks as NOT_RUN. CONTRIBUTING records the owner's standing automatic commit and task-branch push instruction after completion and successful required verification.

## Local checks

| Check | Result | Evidence |
| --- | --- | --- |
| Validator regression executable, no restore | PASS | 78 tests, no failures or skips |
| Infrastructure Release regression executable, no restore | PASS | 72 tests, no failures or skips |
| Native Release MSIX build, version 2026.9.1406.0 | PASS for compilation and package creation | Existing restored assets; local offline copy of Build-Package.ps1 omits restore |
| Host signature verification | FAIL, expected untrusted development root | Signed bytes retained for disposable-guest verification; no host trust imported |
| Guest-only guard in ProductUi and InstallationContract | PASS | Both refused host execution before creating evidence or provisioning dependencies |
| PowerShell parser | PASS | No syntax errors in the changed harness |

The packaging tool could not generate a symbols package because mspdbcmf.exe is unavailable. No owned-code warning was reported. Generated artifacts remain local and ignored.

## Package and executed UI evidence

Package: `2026.9.1406.0`, SHA-256 `7104B353862AC84390054D86325B6F37D26D1746347E24B3D576DBE759EA95E6`.

Input source SHA-256:
- App.xaml.cs: `B4A108B8A4F2B782E60E2580914B5804E3558329AB13872A7E4B338DBE8A8CE8`.
- PackageSmoke.cs: `4E0C318FD385886FC1C131DE2B880DFA6AA971535F269A6F81D4A39C14A236DB`.

The corrected smoke passed all five scenarios in the first product guest (`product-evidence-01/final-smoke-02`). Earlier attempts are retained as failures: package reactivation created another instance, overflow UIA needed a visible control, and the native tray menu exposes its text rather than its XAML automation ID. These were corrected in the harness; no tray-icon product change was required.

Final fresh guest: `25a4048a-5c5c-46dc-b353-0e6412d9a05d`. Evidence root: `.ai-usage-local/CR-AIU-004-01/final-evidence/`. PASS: `orchestration/report.json` records FINISHED and product exit 0; `product/guest-report.json` records ProductUi, positive PASS, runtime installer exit 0 and smoke exit 0. All five tests passed with no errors or skips in 15.997 seconds. No manual runtime confirmation was needed.

| Scenario | Observed result |
| --- | --- |
| exit | Keyboard Exit terminates with exit code 0 |
| title-bar | Close hides while process/tray remain; actual tray activation restores the same HWND; Exit terminates |
| repeated-exit | Exit followed by two native close requests terminates cleanly |
| minimize | Normal minimized state; actual tray activation restores the same HWND; Exit terminates |
| tray-exit | Close hides; actual tray context-menu Exit terminates without reopening the dashboard |

Inspected `product/positive/title-bar-restored.png` and `minimize-restored.png`: restored Dashboard, not-connected state, enabled Connect and disabled Refresh/Disconnect. The tray-menu capture is not visually usable because of capture coordinates; tray Exit evidence is the executed UI action and process assertions, not that image. Scenario JSON records all five original processes exited with code 0. AC-06 and amended AC-08 are PASS. Canonical document validation and `git diff --check` also passed.

## Limitations

Installation-negative tests are NOT_RUN in this ProductUi run. Real-account packaged sign-in and stored-grant resume are NOT_RUN; this guest has networking disabled and no account credentials. Multiple-instance behavior and localized taskbar automation are outside this check; the UI harness uses the English guest shell. Remote CI is NOT_RUN locally. No merge or release is included.
