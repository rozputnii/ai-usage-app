---
id: T-NEW
type: bug
status: implemented
goal: G-003
scope_version: 1
approval_basis: owner direction, 2026-11-01, in conversation, after T-072 could not name the reason for a Claude 403: the app must keep the details in its logs so the cause of a problem can always be found; fix the logging.
---
# Error replies keep the provider's reason

## Problem

- Current: the response capture of the installed app's refused Claude usage read (403,
  2026-11-01) kept the shape of Anthropic's error envelope (`type`, `error.type`,
  `error.message`, `error.details` with two strings, a request id) but only nulls: the endpoint
  policies (R-137) classify quota and OAuth values, and no error envelope, so every value was
  "unclassified-or-sensitive-value". The failure record names only `AccessDenied`. Nothing in
  the logs says why the provider refused, against the logging policy's "evidence needed to
  explain provider problems" and T-043's "auth responses retain approved fields such as ...
  error code".
- Expected: a reply with status 400 or above keeps the provider's error codes, request id and
  message, with anything that could identify or authorize withheld.
- Unchanged: replies below 400; values under unknown names, identity names (`account`,
  `organization`, `email`, ...) and secret names (`access_token`, ...); the generic log records,
  which carry no provider text; capture retention, size limits and the `AIU_LOG_BODIES` switch.

## Requirements

- R-01: In a reply with status 400 or above, the capture keeps code-shaped values (letters
  joined by `_`, `.` or `-`) of `type`, `error`, `error_code`, `error_visibility`, `reason`,
  `code` and `status`; numeric `code` and `status`; `status` as three digits; Anthropic's
  `request_id` (`req_` and 8 to 64 letters or digits); and GitHub's `documentation_url` on
  `https://docs.github.com/`. `code` and `error_description` may be read there although other
  replies never read below them.
- R-02: `message`, `detail` and `error_description` of up to 1,000 characters keep their plain
  words and replace every other word with `[withheld]`. A plain word is letters, optionally
  joined by an apostrophe, colon, slash, underscore or hyphen, with an opening bracket or quote
  and closing punctuation, at most 40 characters. A message that lost words adds the redaction
  reason `message-words-withheld`, so the capture is `withheld-values`.
- R-03: Every other value stays withheld as before; the same envelope in a reply below 400
  keeps nothing new.
- R-04: The capture's `sanitizerVersion` is 2.

## Acceptance criteria

- AC-01: `ARefusedReadKeepsTheProviderErrorInItsCapture` (an Anthropic 403 through the
  transport), `ErrorRepliesKeepTheirEnvelope` (GitHub, Google, Codex and an OAuth error) and
  `ErrorMessagesLoseEveryWordThatCouldIdentifyOrAuthorize` show R-01 to R-04 and fail at the
  base.
- AC-02: The existing capture tests, including the canary tests for 400 replies, pass unchanged.
- AC-03: C2, C4, C5, C6, C7 and C8 pass.

## Design

The choice was between codes only and codes with the message. Codes alone (`permission_error`)
often do not say why; Anthropic's and GitHub's 403s differ only in their message. A message
cannot be proven free of private text by a blacklist (R-137), so it is reduced by an allowlist of
word shapes: everything with a digit, `@`, an inner `.`, a backslash or another symbol goes,
which removes identifiers, addresses, links, paths and tokens. Plain words stay, so a name a
provider puts in an error message is kept; the owner's direction accepts that cost. The
request id is kept because it is the provider's own handle for a support request and names no
person.

## Out of scope

- Generic log records: the failure record keeps naming only the failure category; the capture,
  linked by the operation id, carries the provider's reason.
- Correlation headers beyond the existing `x-request-id` GUID.
- Log retention (168 hours): the owner's earlier logs were gone because the app had not run
  since 2026-10-13, by its usage history.
