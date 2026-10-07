# AIU-050 verification

Evidence for [the specification](spec.md). Host: the owner's Windows 11 Pro 10.0.26200
desktop, .NET 10, branch `users/account-limits-ui-variants-6f623a`. Product source at
`c89ff41`, merged with `main` at `7a32b8e`. Times are local, 2026-10-07.

## Commands

| Check | Command | Result |
| --- | --- | --- |
| Presentation suite | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 247/247 on the merged tree. The first run of the change failed two reorder tests, because sections could no longer be moved; Alt+Up/Down now moves sections within the account and both pass. |
| Infrastructure suite | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 902/902 at `c89ff41` before the merge; the merge changed only presentation and sign-in strip files. |
| App build (README) | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Debug -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, 0 warnings, 0 errors on the merged tree |
| Validator tests | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS, 82/82 |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, valid with no diagnostics after the goal scope and AC format were fixed |
| Diff check | `git diff --check` | PASS |
| MSIX package build | CI on push | NOT_RUN locally; CI builds the unsigned package |

## Acceptance

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 One card per account | PASS | `AccountCardTests.EveryLimitOfAnAccountIsASectionOfOneCard...`; demo app (`--demo`, Brief): Copilot Free shows completions with chat and premium sections, Antigravity shows group 1 with a group 2 section, Claude Pro and Codex Pro keep their sections. |
| AC-02 Hide and show | PASS | Same test and `HiddenCountKeepsTheMostUrgentHiddenColour`. Demo app through UI Automation: "Hide chat" removed the chat section and Copilot Free showed a neutral "1 hidden"; "Hide extra usage" (over today) showed a red "1 hidden" on Claude Pro; "Show 1 hidden limit" restored the section with keyboard focus on it. The first demo run showed the count with zero width, because the header panel did not re-measure when a child became visible; `ShrinkFirstPanel` now re-measures and the second run passed. |
| AC-03 Live source | PASS | `LiveLedgerSourceTests.HiddenSectionsPersistAcrossRestartAndThePrimaryCannotBeHidden` |
| AC-04 Preference file | PASS | `LedgerPreferenceTests`: a file without the list loads; null and duplicated lists are rejected without a write. |
| AC-05 Checks | PASS | Commands above |
| Live Copilot Business account in the installed app | NOT_RUN (post-deploy owner check, D-190) | Needs the owner's signed-in account. |

Screenshots are in the git-ignored `.ai-usage-local/AIU-050/` folder of the worktree.
They were captured with `PrintWindow` while the owner used the desktop; the cursor was
not moved.

## Review

Routine presentation change with a new local preference field; no credential,
destructive-data or privilege change, so CONTRIBUTING requires no independent review.
Older builds keep the unknown field through the preference file's extension data.
