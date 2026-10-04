---
id: AIU-044
type: design
status: implemented
goal: G-003
scope_version: 1
---

# AIU-044 implementation design

The owner selected implementation of the existing specification on 2026-10-04.
This is a bounded adaptation of Ledger, implemented by the primary on main.

## Evidence and presentation contract

Repository evidence in `docs/providers/claude.md`, AIU-034 research,
`ClaudeQuotaParser.ParseExtraUsage` and `QuotaLimitMapping.FromLegacy` establishes
one CL-X series, current-over-legacy precedence, native money and no commercial
classification. Neither source establishes whether the monetary total belongs to
one member or a shared organization. Live scope verification remains NOT_RUN.

Add an ephemeral `LedgerLimit.MonetaryScope` (Unknown, Account, Shared). It is
evidence about a reading's scope, never an account type. Current live mappings
remain Unknown; synthetic compatible Account fixtures exercise the existing
monthly budget contract. Do not persist this annotation, change wire mappings,
or infer it from names, finite limits, caps, windows or wire variants.

Add optional monetary details to the presentation card: independent native used,
limit and retained cap facts, qualification and missing-prerequisite explanation.
Unknown/shared scope shows neutral facts and local history without a personal
remaining allowance, budget or editable cap. Compatible Account scope reuses
Core budget/period calculations, including visibly assumed calendar fallback.
Disabled and incompatible monetary inputs retain facts without budget arithmetic.
Existing caps remain stored and visibly retained when they cannot be applied.

Keep all cards keyed by their existing reading series. In the main view attach
the monetary view inside the first nonmonetary account card, suppressing its
duplicate account name and account actions. A money-only account uses its normal
header. History and cap actions retain the monetary series target. Tray projection
still consumes each account's flat limit list once, with existing note eligibility.
This changes only presentation contracts; no durable format, credentials, source
CLI access, migration, additional requests or data deletion is involved.

The extra-usage mark requires compatible Account monetary scope, enabled/current
evidence and the existing covered window-exhaustion baseline. Preserve start,
reset, currency and correction behavior across midnight. Add freshness checks to
the calculation; do not substitute a local-day baseline.

## Implementation and checks

1. Add failing projection/parser regressions for visible monetary-only/mixed
   accounts, native facts, unknown/shared scope, finite monthly budgets and caps,
   disabled/missing/zero/mismatched money, independent identities, and stale or
   corrected spending since a full window. Adapt projection and factual visuals.
2. Add view-model grouping/history/account-action regressions; attach the monetary
   view using the existing card control. Add a synthetic demo and ordinary Windows
   smoke for subordinate and monetary-only content, history, caps and account actions.
3. Run Infrastructure and Presentation suites, document validation, unpackaged and
   unsigned package builds, and applicable actual Windows smoke. Review the integrated
   diff and obtain a fresh read-only independent review, resolve material findings,
   record actual evidence, then commit/push. Save meaningful intermediate progress.

Logging review: projection and rendering are pure local transformations. Existing
capture, refresh, persistence and command boundaries already own diagnostics; no
new operational failure boundary or additional logging is needed.


## Review corrections

The integrated review identified two material issues, corrected in `2b34d6c`:
retained cap amounts need their own display currency but replacements use the
current limit scale with an empty editor for mismatches; and monetary views must
detach from both grid and old account hosts before moving between standalone and
subordinate presentation. Incompatible caps cannot be removed through a
replacement editor, so decimal-only removal undo cannot reinterpret their currency.
Targeted regressions reproduced both findings before their corrections.
