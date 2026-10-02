---
id: AIU-037
schema_version: 1
---

# AIU-037 implementation plan and handoff

Goal: implement [spec.md](spec.md) sequentially on main using executing-plans and
test-driven-development. The repository's autonomous execution, canonical document
format and no-Codex-subagent policy govern this plan. SDK/runtime and dependencies
remain unchanged. Base reference: recorded in verification.md.

### T-01 - Native parser facts
- status: in-progress
- depends_on: []
- acceptance: AC-01, AC-02
- evidence: not-run

- [ ] Add failing semantic tests in ProviderLimitParserTests for all four parsers.
- [ ] Add QuotaSnapshot.Limits and opaque secondary amounts; normalize provider facts.
- [ ] Preserve opaque scoped identities, signed credits and precise reset evidence.
- [ ] Run targeted parser tests, inspect diff and save the coherent implementation.

### T-02 - Stored v2 migration
- status: pending
- depends_on: [T-01]
- acceptance: AC-02, AC-03
- evidence: not-run

- [ ] Add v1 fixtures and failing migration/round-trip/interruption tests.
- [ ] Add Infrastructure fact codecs and conservative v1 normalization.
- [ ] Extend ProviderStatePolicy/Lease for version-preserving format migration;
  update the three provider state policies and CodexQuotaCache.
- [ ] Verify corrupt-cache isolation, checkpoint safety, cleanup and reparse guards.

### T-03 - History and integrated verification
- status: pending
- depends_on: [T-02]
- acceptance: AC-04, AC-05
- evidence: not-run

- [ ] Test and implement native observation capture with explicit compatible aliases.
- [ ] Run Infrastructure and Presentation Release suites and document/diff checks.
- [ ] Freeze relevant source/diff for independent read-only security review.
- [ ] Resolve material findings with targeted tests; record actual evidence and publish.

## Review focus

Migration must not replay a rotated predecessor, drop a grant because its cache is
invalid, silently infer lost precision, overwrite a verified checkpoint, or join
unlike history series. Tests belong to T-02/T-03 respectively.

## Handoff

Next action: add and run failing native parser regressions for T-01.
Required independent review is pending; an external Claude CLI is available, and
Codex subagents remain disabled. No source credentials or live accounts were read.
