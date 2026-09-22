# AIU-014 development Preview evidence

Started 2026-09-22. Prepared code is committed through ff30d80. Operational activation
is incomplete. Public signing and Stable are outside the selected development phase.

| Criterion | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | NOT_RUN | Workflow gates are implemented and accepted by GitHub. Preview remains disabled; no signing secrets provisioned. Actual successful-main publication is pending. |
| AC-02 | PASS | 23 local release-policy/real Git ancestry assertions; hosted validate job at ff30d80 passes. Actual remote reservation/retry execution remains pending. |
| AC-03 | NOT_RUN | Dedicated new-key provisioning and signing code prepared and independently reviewed; no key created/uploaded, no hosted signing execution. |
| AC-04 | NOT_RUN | Production feed XML rules tested; Releases/Pages publication and Windows auto-update unverified. |
| AC-05 | PASS | Explicit provisioning guard and tester instructions present. No host trust, host install or credential copying performed. |
| AC-06 | BLOCKED | Independent review finding resolved; proposed disposable-guest feed test was rejected by automatic approval review before execution. No concrete reason was returned beyond blocked by policy. |

## Observed checks

- `pwsh -NoProfile -File tests/release/Test-PreviewRelease.ps1`: PASS, 23 assertions.
  Initial stubs failed; ancestry checks failed before implementation. The checks use
  actual repository ancestry and real XML parsing, not golden-only comparison.
- PowerShell AST syntax parsing of tools/windows and tests/release: PASS.
- Canonical document validation and git diff check: PASS on final preparation records.
- Hosted run [35783928512](https://github.com/rozputnii/ai-usage-app/actions/runs/35783928512)
  at 55286df: windows-package PASS, validate FAIL (shallow checkout).
- Hosted run [35784326591](https://github.com/rozputnii/ai-usage-app/actions/runs/35784326591)
  at ff30d80: overall PASS; validate and windows-package PASS; preview correctly SKIPPED
  while provisioning is disabled.
- [Independent review](review.md): initial FAIL on shallow checkout, addressed by
  9064d76 with actual failure evidence and subsequent hosted PASS. No proven security
  defect found; operational assumptions remain unverified.

## Failures and limits

Previous CI failed because Build-Package.ps1 splatted a scalar `/restore` as individual
characters. 55286df preserves an array; the real hosted package job then passed. Earlier
AIU-006 local `-NoRestore` builds did not exercise that argument path. The new ancestry
test initially needed deeper checkout, then its expected Git rejection left a nonzero
native exit status. Both were reproduced and corrected, not hidden by skipping checks.

Local YAML libraries were unavailable; no local YAML parser PASS is claimed. GitHub
accepted and executed the workflow, including its queue:max syntax.

The Sandbox preparation/launch command was rejected before execution. Only two
loopback feed fixtures were generated under ignored `.ai-usage-local/AIU-014` from the
real production XML generator; these use an explicit test-only HTTP loopback override.
No guest installation or automatic-update result is inferred from those fixtures.
The old/new AIU-006 package-install evidence is not proof of this App Installer channel.

Required next setup is explicit authorization to create a new dedicated CI key, send
it to GitHub Actions encrypted secrets, activate Pages and enable publication. Host
trust remains a separate action. The channel is not currently operational and the task
must not be marked complete.
