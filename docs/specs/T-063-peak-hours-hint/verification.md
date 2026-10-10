# T-063 verification

Evidence for [the specification](spec.md). Host: the owner's Windows 11 Pro 10.0.26200
desktop, .NET 10, branch `worktree-bridge-cse_01KeFmzVCvJyzw8wHm21rYdo` from `main` at
`0f25e49`. Times are local, 2026-10-10.

## Research

Provider sources were read on 2026-10-10 and are recorded in
[peak hours](../../providers/peak-hours.md). Anthropic's 05:00–11:00 PT weekday window is
the only announced one; no provider exposes a peak signal. No credential, account or
provider endpoint was used.

## Checks

| Check | Result |
| --- | --- |
| C5 Presentation suite | PASS, 347/347 (adds `PeakHoursTests`, 10 cases) |
| C4 Infrastructure suite | PASS, 926/926 |
| C7 App build | PASS, 0 warnings, 0 errors |
| C9 Package build | PASS, unsigned validation-only package 2026.10.1091.0 |
| C8 Launch smoke, under the desktop lock | PASS, 3/3 on two consecutive runs. A first run failed 1/3 (comfortable demo): an exception while enumerating the tray flyout's UI Automation tree (`LedgerSmoke.cs:400`), not an assertion; it did not repeat. |
| Window-size smoke (`*SqueezedWindowKeepsTheTitleRowClearOfTheCaptionButtons*`) | PASS, 1/1; the demo Brief time (Wednesday 14:20 BST) is inside the window, so the pill is part of the squeezed title row |
| C2 Document validation | PASS after this record |
| C6 Diff check | PASS |

## Acceptance results

| Criterion | Status | Evidence |
| --- | --- | --- |
| AC-01 | PASS | `PeakHoursTests`: both edges, the caller's offset, weekends, Pacific weekday from UTC+11 and UTC+3, 26 October and 2 November 2026 |
| AC-02 | PASS | `TheHintNamesNoProviderAndSaysItIsASchedule` |
| AC-03 | PASS | `TheWindowAndTheTrayShowTheHintAndKeepItWhileTheTextIsTheSame` |
| AC-04 | PASS | Checks above; demo screenshots `ledger-demo.png` and `tray-icons.png` in the git-ignored `.ai-usage-local/peak-evidence/` show "Peak · until 19:00" in both places |

## Review

Focused independent review (`aiu-reviewer`, T3 because of the AGENTS.md rule) of `8eaf1f7`:
one blocking finding (the missing verification record) and five should-fix findings: the
title-bar regions were not refreshed when the hint changed, the review rule was too broad,
the tooltip schedule was a separate literal, two evidence details, and layout nits. All were
fixed in `9e0b115` and the re-check approved it; `/security-review` found nothing. Accepted
as is: the extra title-row column adds a permanent 10 px gap to the row and its minimum width.

## Pending owner checks

During weekday peak hours in the installed app: NOT_RUN, post-deploy owner check (R-190).
