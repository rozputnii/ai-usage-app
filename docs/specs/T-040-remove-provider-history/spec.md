---
id: T-040
type: feature
status: implemented
goal: G-003
scope_version: 1
approval_basis: Owner selected implementation of T-040 on 2026-10-03. This specification derives the existing backlog acceptance criteria and R-184 within that authorized scope.
---

# Remove provider-history retrieval

## Outcome and approach

Remove the retired T-011 remote history capability after T-039 switched the
product to Ledger. Local T-036 readings remain the sole history source under
[R-184](../../decisions/accepted.md#d-184---local-only-usage-history).

Delete the Core history contracts, Codex/Copilot history clients, parsers and session
methods, their composition dependencies, console command and obsolete tests/resources.
The old live/demo provider-history sources were already removed by T-039; retain
Ledger's distinct local-history contract and implementations. Narrow diagnostics by
removing obsolete endpoint policies. Preserve authentication, quota retrieval, local
observations, account isolation and settings. No new dependency, endpoint, migration,
credential access or presentation contract is needed.

## Data lifecycle

T-011 reports, selection and throttling were memory-only. Inspect remaining stores
and preferences and record their disposition in verification. Preserve unrelated and
unknown bytes; do not run cleanup on user state. Existing diagnostic evidence continues
under the unchanged T-043 retention policy. Removal must not reinterpret legacy
preferences or attach old readings to another account.

## Acceptance criteria

- AC-01: No runtime or console path requests provider-history routes; no composition
  registration or test depends on the removed types. Ledger retains local history.
- AC-02: A security-lifecycle review records all affected data/preferences. Any
  historical unknown data remains preserved without rewriting other data.
- AC-03: Infrastructure, Presentation and validator regressions, document validation
  and `git diff --check` pass. Build the console and Windows composition; distinguish
  build evidence from actual interactive or live-provider execution.
- AC-04: Close T-011 in the backlog as superseded by R-184 and T-040, preserving
  its historical verification limitations.

## Verification

Use synthetic data only. Verify logging rejects retired report values, keep meaningful
session safety regressions on remaining quota operations, run required suites, inspect
the integrated diff and scan executable source for removed types/routes. Record actual
results and focused independent safety review in [verification.md](verification.md).
