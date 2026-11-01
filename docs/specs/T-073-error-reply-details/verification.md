# Error replies keep the provider's reason - verification

Evidence for [the specification](spec.md). Code: `185e60f` and the review fixes `fa3de31` and
`a7abe34`; final checks on `a7abe34`, which equals `origin/main` (`2166f38`) merged. Environment:
Windows 11 Pro 10.0.26300, .NET SDK from global.json, Release, desktop unlocked. Date: 2026-11-01.

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
| AC-02 | PASS | `ProviderCaptureTests` 52/52 (ten cases added by the review fixes), including `CapturePreservesApprovedNumbersBeforeProjectionAndWithholdsUnknownValues` (400) and `DisabledCaptureAndMalformedErrorAreNeverCalledComplete` |
| AC-03 | PASS | Checks below |

## Checks

On the merged tree (`a7abe34`) before the push to `main`.

| Check | Result |
| --- | --- |
| C2 Document validation | PASS, valid, no diagnostics |
| C3 Final document validation | PASS, `--final` valid, no diagnostics |
| C4 Infrastructure suite | PASS, 1002/1002 |
| C5 Presentation suite | PASS, 380/380 |
| C6 Diff check | PASS, clean |
| C7 App build | PASS, 0 warnings, 0 errors |
| C8 Launch smoke | PASS, 3/3 under the desktop lock |
| Window-size smoke | PASS, 1/1 under the desktop lock |

## Not run

| Item | Reason |
| --- | --- |
| The next refused read of the free-plan Claude account in the installed app shows the provider's reason | Post-deploy owner check (R-190); the reason itself is live provider text |

## Review

Focused independent review (`aiu-reviewer`, T3, at `185e60f`) with the convergence-review
skill and an inline `/security-review` (no finding at confidence 8 or above); `/code-review`
NOT_RUN in the reviewer (no sub-agent tooling there), the code reviewed by hand. Tier T3
confirmed, no security-lifecycle area. No secret, credential, token, PKCE value, email, URL with
a query, absolute path or identity-name value reaches a capture through the new rules. C4, C5,
C2 and C6 rerun PASS; fails at base confirmed. Verdict: changes needed, one Important and three
Minor, all fixed:

- Important: the envelope names were known in every reply, so a reply below 400 read below
  `details`, `status` and the rest (only values the endpoint policies already approve, no privacy
  exposure). They are now known only in a reply of 400 or above;
  `RepliesBelow400KeepNothingNewUnderTheEnvelopeNames` keeps a reply below 400 exactly as before.
- Minor: code values had no length bound and took dotted host names, numbers had no bound, and
  `$` accepted a final line break. Codes are now letters joined by `_` or `-`, at most 64
  characters; numbers are whole and at most 99,999; every shape ends with `\z`
  (`ErrorValuesAreBounded`).
- Minor: R-235, the spec and the code summary said identifiers and paths are covered; they now
  say letters-only logins, slugs, codes and relative paths remain.
- Minor: an error inside a 200 reply (Antigravity operations) keeps nothing new; recorded as out
  of scope.

Re-check of the fixed lines at `fa3de31`: approve, all four closed, C4 1002/1002; a reply below
400 reproduced exactly as at the base on the Claude quota and an OpenAI auth route. One new
Minor, fixed: the `EndpointPolicy.Error` summary and logging.md still described the kept words
too narrowly; they now say "plain word" and name letters-only codes and relative paths.
