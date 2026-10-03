# AIU-044 preparation evidence

## Task-definition review, 2026-10-04

Base: `0e5d13d`. This is a documentation-only preparation step, not implemented behavior.

- PASS (source inspection): `LiveLedgerProjection.Account` skips every `CL-X` before
  card construction and substitutes NoDisplayedLimits when only those readings remain.
- PASS (source inspection): `ClaudeQuotaParser.ParseExtraUsage` prefers `spend` over
  `extra_usage`; `QuotaLimitMapping.FromLegacy` emits the same CL-X family. The parser
  returns null PlanType. These facts do not classify personal versus work subscriptions.
- PASS (record inspection): AIU-034's Claude (T-02) source matrix describes CL-X/CL-D as alternative
  extra-spend representations and leaves Team/Enterprise scope and wire period unknown.
  The imported Provider States reference has Claude Work D1-D6 with a finite monthly
  monetary pool, and A8 with spending since a window filled.
- PASS (source inspection): `ExtraUsageEvidence.Calculate` and the projection retain
  the window-fill baseline; it is not a day-start spending metric. The owner explicitly
  retained that meaning in this conversation.
- Inference, not live evidence: a work allowance supplied through the existing monetary
  mapping would also be hidden. No real work-account response was inspected in this step.
- Proposal: account-owned monetary sections based on available facts, with neutral
  presentation for unresolved commercial purpose/scope. No invented account taxonomy.

Provider evidence classification for this preparation: repository-source inspection only;
source_verified_at: 2026-10-04; live_verified_at: null. No claim of fresh upstream
protocol verification, provider approval or live work-plan coverage is made.

## Checks and implementation limits

- PASS: `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json`
  returned `valid: true` with no diagnostics.
- PASS: primary diff/link/consistency review and `git diff --check`. Reviewed that the
  draft preserves the accepted mark baseline, does not guess plan type, and does not
  require a commercial label before allowing an otherwise evidenced budget.
AC-01 through AC-06 implementation verification: NOT_RUN. No product code, live provider,
credential store, user-state file or imported design artifact was changed.

Next action after owner selection: verify the monetary source's scope evidence and
define the minimal account-owned rendering against the draft specification.
