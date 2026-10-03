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
- status: done
- depends_on: []
- acceptance: AC-01, AC-02
- evidence: docs/specs/AIU-037-provider-limits-v2/verification.md

- [x] Add failing semantic tests in ProviderLimitParserTests for all four parsers.
- [x] Add QuotaSnapshot.Limits and opaque secondary amounts; normalize provider facts.
- [x] Preserve opaque scoped identities, signed credits and precise reset evidence.
- [x] Run targeted parser tests, inspect diff and save the coherent implementation.

### T-02 - Stored v2 migration
- status: done
- depends_on: [T-01]
- acceptance: AC-02, AC-03
- evidence: docs/specs/AIU-037-provider-limits-v2/verification.md

- [x] Add v1 fixtures and failing migration/round-trip/interruption tests.
- [x] Add Infrastructure fact codecs and conservative v1 normalization.
- [x] Extend ProviderStatePolicy/Lease for version-preserving format migration;
  update the three provider state policies and CodexQuotaCache.
- [x] Verify corrupt-cache isolation, checkpoint safety, cleanup and reparse guards.

### T-03 - History and integrated verification
- status: blocked
- depends_on: [T-02]
- acceptance: AC-04, AC-05
- evidence: docs/specs/AIU-037-provider-limits-v2/verification.md

- [x] Test and implement native observation capture with explicit compatible aliases.
- [x] Run Infrastructure and Presentation Release suites and document/diff checks.
- [x] Freeze relevant source/diff and attempt independent read-only security review.
- [ ] Obtain an independent verdict and resolve any material findings.
- [x] Record actual evidence and publish a WIP save point under CONTRIBUTING.

## Review focus

Migration must not replay a rotated predecessor, drop a grant because its cache is
invalid, silently infer lost precision, overwrite a verified checkpoint, or join
unlike history series. Tests belong to T-02/T-03 respectively.

## Handoff

Next action: obtain a focused independent security-lifecycle review of the current
AIU-037 diff against base 954ae68 and this specification using an available authorized reviewer.

Implementation, primary diff review and local verification are complete. Infrastructure
521/521 and Presentation 179/179 PASS; Windows Release consumer build and offline console
inspection PASS. Required independent review is BLOCKED: the external Claude CLI returned
HTTP 429 with a weekly usage-limit message before any model review. No reviewer verdict
or worker artifact exists. No Codex subagent was started. No source credentials or live
provider accounts were read. Specification stays implementing; feature is not done.

Rulings within the accepted scope: v1 reset precision is explicitly Unknown; ambiguous
Claude slug-based scoped histories remain separate; CX-I preserves all three opaque raw
amount strings. CL-X/CL-D share the extra-usage key because current replaces legacy.
These choices avoid inventing lost facts or merging unlike histories.
