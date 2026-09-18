---
id: AIU-008
schema_version: 1
---
# Copilot verification

## Candidate and environment

Date: 2026-09-18. Base: bc67aae; branch: codex/aiu-008-copilot-integration. Final source patch (git diff over src, tests, tools; UTF-8 local artifact): SHA256 4434F4C8671980978F0D8CA38CAC9782C5F6479C346403C2E2C175336AEB6F0D. Documentation is recorded separately in the completion commit.

Windows host, .NET 10, x64 Release, local unpackaged application with an unlocked desktop. Live data is isolated under the ignored .ai-usage-local/AIU-008/live-state directory; automated offline product smoke uses a separate empty profile. No source CLI credentials, host trust changes or package installation were used. Native Chrome was operated under the owner's current explicit authorization for independent checks and GitHub consent.

Preparation initially had no implementation/live evidence and an unresolved registration choice. The owner subsequently selected only OMP; the results below supersede those preparation limitations. Exact source provenance and public authentication/private quota classifications are in [provider evidence](../../providers/copilot.md).

## Acceptance results

| Criterion | Verdict | Observed evidence |
| --- | --- | --- |
| AC-01 contract evidence | PASS | Stable OMP v18.2.6 commit 78b753124d11f8dd3ae73e2524125890ff7c977e inspected. OpenCode registration/read:user selected by owner. Truthful AI Usage headers work live; permission and account-context limitations remain explicit. |
| AC-02 shared authentication | PASS | Synthetic protocol tests cover device intervals, slow-down, denial, expiry, cancellation, malformed URLs/responses and optional token type. Live native UI displayed the transient code, Chrome authorized OpenCode, and Windows connected. No inference/model-policy endpoint is called by the implementation. |
| AC-03 quota semantics | PASS for selected OMP request contract | Independent assertions cover native amounts, missing/unlimited values, unknown group units, retained opaque details, deterministic order and null provider restriction flags. The website's Included credits is not treated as the OMP request pool. Post-fix live presentation check is recorded below. |
| AC-04 protected lifecycle | PASS | DPAPI round trip, staged recovery, corrupt/future records, revisions/exclusivity, reparse rejection and unrelated-file preservation tested. Failed deletion, expired grant, failed replacement and reconnect identity mismatch have explicit tests. Authorized live refresh/restart/disconnect/reconnect passed. No refresh-token rotation exists in the selected OMP path. |
| AC-05 Windows integration | PASS | Device-code display/clearing and native unlimited/zero-entitlement mapping pass presentation tests. Actual product and demo Windows smoke cover navigation, theme, close-to-tray, exit/repeated exit and capability honesty. |
| AC-06 verification | PASS for required local checks | Infrastructure 162/162; Presentation 120/120; unpackaged and console builds; unsigned MSIX; product 7/7 and demo 7/7 actual desktop smoke. Final document validation recorded below. |
| AC-07 review/publication | PASS | Focused independent review completed; all four material findings fixed and checked. Primary diff/document checks passed. Implementation commit 0d04763 was pushed to origin/codex/aiu-008-copilot-integration. |

## Commands and builds

- PASS: dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo: 162 passed, zero failed/skipped, 6.393 s.
- PASS: dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo: 120 passed, zero failed/skipped, 0.681 s.
- PASS: dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore: zero warnings/errors.
- PASS: dotnet build tools/AiUsage.ProviderConsole/AiUsage.ProviderConsole.csproj -c Release --no-restore: zero warnings/errors. --help confirms inspect-copilot and copilot commands.
- PASS: unsigned MSIX version 2026.9.1801.0 using the Build-Package.ps1 build/manifest checks with existing restored assets and no /restore. Local wrapper: .ai-usage-local/AIU-008/Build-OfflinePackage.ps1. Embedded identity/version checked. Package SHA256 81E2F64C4EBBA1867A1794709348AE4B4F2AE0B895D85E35DEB26FD2044EBB9C. One external tooling warning: mspdbcmf.exe unavailable, so no symbols package. No owned-code warnings. No signing, install, release or trust change is claimed.
- PASS: actual Windows smoke executable from .ai-usage-local/AIU-010/t11-tray-icon-fix-smoke/published, pointed at the new Release executable via AIU_SMOKE_EXE. Product: 7/7, 38.7 s, evidence .ai-usage-local/AIU-008/product-smoke. Final demo: 7/7, 48.686 s, evidence .ai-usage-local/AIU-008/final-demo-smoke. These were desktop executions, not publication-only evidence.

## Authorized live observations

Times below are local Europe/Lisbon on 2026-09-18. Only sanitized outcomes are retained; no token, device code, GitHub identity or raw account response is committed.

| Check | Verdict | Observation |
| --- | --- | --- |
| Device authorization and initial quota | PASS | Chrome displayed OpenCode by Anomaly and read-only profile permission. GitHub confirmed the device connection. Windows showed Connected and a fresh OMP quota reading at 22:47:42. |
| Manual refresh | PASS | A new fresh observation appeared at 22:48:29. |
| Exit and relaunch | PASS | Ctrl+Q/Exit ended the original process. Relaunch of the isolated profile resumed without browser login and showed a fresh observation at 22:48:55. |
| Local disconnect | PASS | Windows became Disconnected; Refresh disabled and Connect enabled. Exact owned state/pending file existence checks returned false. No server revocation is claimed. |
| Cancellation | PASS | A new device flow was cancelled while waiting. Windows reported Cancelled/Nothing was changed and cleared the device code. |
| Reconnect after disconnect | PASS | A subsequent device authorization succeeded; Windows showed Reconnected and a fresh reading at 22:52:25. |
| Provider website comparison | PASS with limited parity | GitHub settings/copilot/features identified Copilot Free, Inline suggestions 0% used and Included credits 0% used. OMP returned plan individual and request pools. These distinct metrics are kept separate; full website parity is not established. |

The first live pass exposed two presentation defects: source JSON order selected chat as the primary group, and a derived exhaustion flag was described as a provider restriction. Both were corrected. Zero entitlement is now neutral/unknown while retaining the explicit native amount; it does not imply credits were consumed.

## Focused independent review and closure

Reviewer: fresh read-only GPT-5.6 Luna, reasoning max, under convergence-review and CONTRIBUTING. Frozen candidate: bc67aae plus patch SHA256 6A23BF31956590AF4FC9C20F04072737EAC91D6500C64E66CF250D351A1435C0. No reviewer edits, live calls or credential access. Reviewer independently ran Infrastructure 156/156 and Presentation 119/119 on that candidate and diff check.

Initial verdict FAIL, with four material findings:

1. Reconnect could adopt a different account while keeping slot/history. Fixed by checking the established numeric ID before quota request or write. ReconnectCannotReplaceAccountIdentityEvenWhenQuotaWouldSucceed proves rejection and preservation.
2. LimitReached was synthesized from zero remaining percentage. Fixed to null; parser and presentation tests confirm restriction provenance remains unknown.
3. Source JSON ordering selected the primary quota. Fixed with premium/chat/completions ordering; reversed-order assertion added.
4. Opaque quota details were dropped. Nullable typed QuotaSourceDetails now retains quota_id, quota_remaining, overage_count and overage_permitted independently; parser and encrypted-state round-trip assertions verify them.

The optional-token-type compatibility note was also addressed: omitted accepted, explicit incompatible type rejected. Primary reviewed the integrated fixes and final test results; no unresolved material finding remains. Per review policy, targeted fixes do not require a repeated full review.

## Explicit limits

NOT_RUN: paid and organization plans, enterprise hosts, live provider denial/revocation/expiry/rate-limit scenarios, package installation/update, CI and public release. Synthetic tests establish the relevant failure handling but do not claim live execution. The selected OMP path has no applicable token rotation. Source CLI imports, inference, model-policy writes, SDK and billing fallback are excluded. AIU-010 remains paused with its separate acceptance gates preserved.

## Final presentation and local closure

PASS: post-fix Release relaunched and resumed at 23:07:50; manual refresh produced a fresh observation at 23:09:33. Native UI inspection confirmed premium/chat/completions order, an unknown neutral primary ratio for the explicit zero entitlement, no synthesized provider-limit message, and no exhausted-account alert. The source observation and native amount remain visible. Demo smoke passed on this same final source candidate.

Document validation initially caught the implementing/done metadata transition before backlog closure. The lifecycle metadata was reconciled; final validation and publication results follow in the completion record.

PASS: final document validation (dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json) returned valid=true with no diagnostics. git diff --check passed. Primary reviewed the integrated diff, provider/state boundaries and outgoing file list; local runtime data and credentials are ignored and absent from the change. Remote preflight found main unchanged at bc67aae and no existing task branch.

## Publication

PASS: implementation commit 0d047632fae06147fbe0dbdd3385cf290d391416 was pushed to origin/codex/aiu-008-copilot-integration on 2026-09-18. Outgoing changes were inspected; the worktree was clean after commit and main remained at bc67aae. This follow-up records completion without changing product source.

NOT_RUN: GitHub CI. A read-only run listing returned no runs for this branch; the Validation workflow triggers on pull requests, main pushes or explicit manual dispatch, not task-branch pushes. No PR, manual dispatch, main merge or release was requested or performed. Local required checks and actual authorized browser/Windows checks passed as recorded above.
