# Error replies keep the provider's reason - verification

Evidence for [the specification](spec.md). Environment: Windows 11 Pro 10.0.26300, .NET SDK
from global.json, Release, desktop unlocked. Date: 2026-11-01.

## Diagnosis

The installed app's capture `response-…-73f5ea2e….json` (2026-11-01 19:26, read locally, not
copied) of the refused Claude usage read: status 403, policy `claude/quota/v1`, completeness
`withheld-values`, body `{"type":null,"error":{"type":null,"withheld_1":null,"withheld_2":{…}},"withheld_2":null}`
with eleven redactions, all `unclassified-name` or `unclassified-or-sensitive-value`.
`EndpointPolicy.StringValue` classifies quota values and the fixed OAuth `error` codes only, and
`message`, `details` and `request_id` are not known names, so the reason was lost. The matching
`OperationFailure` record names `AccessDenied` and nothing more, by design.

## Acceptance results

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | The three new tests (six cases). Red at the base (a temporary worktree at `2166f38` with only a `bool error = false` parameter added): every case failed on nulls or withheld names, and the transport test on `sanitizerVersion` 1 |
| AC-02 | PASS | `ProviderCaptureTests` 42/42, including `CapturePreservesApprovedNumbersBeforeProjectionAndWithholdsUnknownValues` (400) and `DisabledCaptureAndMalformedErrorAreNeverCalledComplete` |
| AC-03 | NOT_RUN | Checks below, on the merged tree |

## Checks

Pending.

## Not run

| Item | Reason |
| --- | --- |
| The next refused read of the free-plan Claude account in the installed app shows the provider's reason | Post-deploy owner check (R-190); the reason itself is live provider text |

## Review

Pending.
