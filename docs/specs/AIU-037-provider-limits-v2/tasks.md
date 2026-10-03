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
- status: done
- depends_on: [T-02]
- acceptance: AC-04, AC-05
- evidence: docs/specs/AIU-037-provider-limits-v2/verification.md

- [x] Test and implement native observation capture with explicit compatible aliases.
- [x] Run Infrastructure and Presentation Release suites and document/diff checks.
- [x] Freeze relevant source/diff and attempt independent read-only security review.
- [x] Obtain an independent verdict and resolve any material findings.
- [x] Record actual evidence and publish a WIP save point under CONTRIBUTING.

## Review focus

Migration must not replay a rotated predecessor, drop a grant because its cache is
invalid, silently infer lost precision, overwrite a verified checkpoint, or join
unlike history series. Tests belong to T-02/T-03 respectively.

## Handoff

AIU-037 is complete. No next action remains within this item. Independent read-only
review of a3ef130 against 954ae68 returned FAIL with four P2 findings. The owner then
authorized this session to implement their resolution. Correction commit f844c33
preserves grants during quota-size recovery, rejects incompatible history aliases,
preserves unknown Claude kind identity and retains unlimited Copilot percentages.

All findings have targeted regression coverage. The initial focused run reproduced
seven failures; the corrected focused run passed 69/69. Additional interruption and
mixed-history coverage is included in the final Infrastructure 530/530 PASS.
Presentation 179/179 and the unpackaged Windows consumer build PASS. Document and
diff validation evidence is recorded in verification.md.

The initial independent verdict is retained as evidence for its frozen baseline.
Subsequent correction verification and final acceptance are primary integration work,
under CONTRIBUTING's targeted-after-fixes policy, not a new independent verdict.
No subagent, other conversation, live provider or source credential was used.

V1 reset precision stays Unknown; ambiguous scoped histories stay separate; CX-I raw
amount strings remain opaque. Live/UI/package-upgrade evidence remains NOT_RUN for
this item. Completion does not select another backlog item or authorize release.
