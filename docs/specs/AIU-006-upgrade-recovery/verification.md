# AIU-006 verification

Completed 2026-09-22. Product implementation is in `3c4e0ee`, with final fixes in
`64d196f`; final packaged product version is `2026.9.2222.0`.

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Actual same-family 2026.9.2202.0 to 2026.9.2222.0 update in Windows Sandbox; old/new product activation, exact preference bytes and synthetic DPAPI grant preservation. |
| AC-02 | PASS | 23 Infrastructure maintenance cases cover protected checkpoint, exclusive lease, bounded validation and ordered publication. |
| AC-03 | PASS | Fault-boundary regressions plus actual packaged migration blocked by an exclusive file handle, process termination, restart into recovery and explicit Retry. |
| AC-04 | PASS | Actual confirmed restore after corrupted preferences and newer-layout refusal; regressions cover damaged journal/checkpoint and interrupted restore. |
| AC-05 | PASS | Exact-byte/opaque-data preservation, synthetic credential ciphertext and decryption equality, tampering, redirected paths, lifetime exclusion and secret-free checkpoint scope. |
| AC-06 | PASS | Presentation gating/shutdown regressions and real recovery UI: retry, confirmed restore, disabled newer-layout actions, sanitized export, folder opening and clean exit. |
| AC-07 | PASS | Final suites, Release/MSIX builds, canonical validation, integrated primary review, focused independent review and actual guest UI/update evidence below. |

## Regression and build checks

Commands ran from the repository root with the pinned SDK at
`$HOME/.dotnet/ai-usage-sdk/dotnet.exe`:

- `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo`:
  PASS, 330/330, including 23 maintenance cases using actual Windows DPAPI/filesystem.
- `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo`:
  PASS, 157/157, including startup/provider/preference gates, restore and shutdown.
- `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore`:
  PASS, zero warnings and errors.
- Signed MSIX build using `tools/windows/Build-Package.ps1`: build/signing PASS.
  Host chain validation returned an expected failure for the untrusted development
  certificate. Guest signature validation and installation PASS after guest-only trust
  of its public CER. No host trust or package installation was performed.
- `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`
  and `git diff --check`: PASS on the final completion documents.

Final logs are ignored under `.ai-usage-local/AIU-006`: `infrastructure-final.log`,
`presentation-final.log` and `unpackaged-final.log`. No final unpackaged interactive
smoke or full unrelated Windows smoke suite is claimed; final interactive acceptance
uses the actual packaged binary below.

## Actual MSIX upgrade and recovery

`Invoke-UpgradeSmoke.ps1` runs only in the disposable WDAG guest. Networking and
clipboard transfer were disabled. Synthetic preferences and a synthetic Codex-shaped
DPAPI grant were created inside the guest package's owned root. No personal provider
credential or source CLI store was read/imported. The old UI showed the seeded Dark
preference before updating. The new package used the same package family.

| Artifact | Version | SHA-256 |
| --- | --- | --- |
| Retained old MSIX, signed staging copy | 2026.9.2202.0 | `4FB96AFF169C5A601DC511F53C2597E6EA4F0F0A8C408BEC08C771B5CBD15188` |
| Final new MSIX | 2026.9.2222.0 | `AF2652CBD1278D7C4B6F152C973CD44F0DA2EA678E5D5EA2CFFD86A6ABDF24BF` |

Local evidence root: `.ai-usage-local/AIU-006/guest-02`.

- `final/upgrade-report.json`, 2026-09-22T19:58:49Z: overall PASS; old UI, update,
  recovery and credential preservation all PASS. Old and new explicit UI scenarios
  each passed 1/1. The report's stage field retains its last progress label; terminal
  status and per-check results are the result, not that label.
- The new UI scenario holds the layout staging file open to interrupt a real migration,
  observes recovery, terminates the app, restarts, retries, corrupts the committed
  preferences, confirms restore, and verifies newer schema 99 disables Retry/Restore.
  Exact synthetic provider ciphertext remains unchanged and decrypts to the original.
- `utilities.log` and `utilities/upgrade-recovery-result.json`, 2026-09-22T20:02:15Z:
  PASS, 1/1 explicit packaged scenario on the same final installed binary, adding
  diagnostic export and both File Explorer actions to the recovery checks.
- `verified-recovery-diagnostics.txt` contains only the fixed recovery heading,
  `Condition: NewerSchema`, `Layout: 99`, and `Checkpoint available: False`.
- `final-state.json`: installed version 2026.9.2222.0, restored layout 1, zero running
  test product processes. Screenshots of old preferences, interrupted startup,
  newer-layout blocked actions and restored dashboard were inspected.

The final MSIX is retained under
`.ai-usage-local/AIU-006/packages/2026.9.2222.0/`. All generated binaries, synthetic
state, screenshots and raw reports stay ignored, outside Git. Sandbox orchestration
used the documented [Windows Sandbox CLI](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-cli).

## Failures found and resolved

Initial maintenance stubs failed 13/16 tests; added corruption cases failed 3/20 and
missing/empty-state cases failed 2/22 before fixes. The full Presentation suite exposed
a stop/resume race; marking stop intent before the asynchronous yield fixed it.
A negative diagnostic-path check exposed an exception not handled by the presentation
boundary; it now reports export failure without terminating the product.

Early UI runs exposed missing UIA peers and a closing-dialog animation race in the
harness; selectors and settling were corrected. Both local and intermediate packaged
UI found the newer-layout Restore button visually enabled despite service refusal.
The missing command notification was fixed and regression-tested; final 2222 PASS
supersedes that failed 2220 candidate.

The first guest stopped during setup without usable acceptance evidence. In the second
guest, runtime 10.0.12 installation took several minutes and completed with exit 0.
The harness now waits on the installer's own handle and correctly treats an empty
runtime list as missing. Final update execution reused that installed runtime; a fresh
cold-runtime run of the final predicate was not repeated.

## Independent review and limits

Focused read-only review of frozen `3c4e0ee` by existing Claude Code: PASS, no material
or blocking findings. See [review.md](review.md) for isolation, reviewer identity,
observations and the small post-review fixes covered by primary review and regressions.
The required Codex Luna override was unavailable; no other Codex model was substituted.

NOT_RUN: physical power-loss/power-cut durability, live-provider authentication or
quota, public release/signing trust, remote CI execution, cross-version unpackaged
concurrent access, and SQLite/WAL migration (there is no database). Exception injection,
actual file-sharing failure and process termination establish the recorded recovery
behavior, not physical storage durability. Restore deliberately returns preferences
to the checkpoint, while retaining independently managed provider records. No remaining
acceptance check for this selected scope is BLOCKED.
