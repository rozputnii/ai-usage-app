# Owner inbox and unresolved setup facts

No further product questionnaire blocks bootstrap. Discover setup from the current directory and environment before asking. This is not a list of reasons to stop all preparation.

## SETUP-001 - Resolved local repository and commit identity
- resolved: 2026-09-12, explicit owner reply after read-only discovery.
- result: Existing rozputnii/ai-usage-app repository; repository-local author `rozputnii <daniil.rozputnii@gmail.com>`. Owner authorized the existing license's MIT replacement.
- evidence: ../workflow/environment.md.
- remaining boundary: Remote publication is not authorized by local setup. Main protection is now explicitly deferred at low priority in AIU-026, not a current development PR prerequisite. No remote changes or bypass were performed.

## SETUP-002 - Historical toolchain setup (retired runtime)
- observed: Stable OMP 18.1.18 and profile roles discovered. Fresh native SDK confirms read-only advisor attachment. Owner authorized a user-local .NET SDK; 10.0.401 runs successfully.
- resolved: Native isolation, primary integration, selection/cancel, pause/resume, budget stop and fresh-session continuity exercised. Independent replacement review PASS, no material findings; the owner-authorized capture-recovery exception and three deferred MINORs are recorded in the AIU-001 verification report.
- current disposition: Runtime roles are retired prerequisites. SDK 10.0.401 remains the product toolchain; AIU-002 tooling and guest verification are recorded as completed in its evidence.
- evidence: ../workflow/environment.md.

## RELEASE-001 - Public signing eligibility
- timing: AIU-014; not a bootstrap blocker.
- decision already made: Azure Artifact Signing is preferred.
- owner answers (2026-09-24): individual outside the US and Canada with no eligible organization, so Artifact Signing is unavailable. Free options only (no Artifact Signing, no OV/IV/EV purchase). Verified name as publisher accepted. Public product name `AIUsage`.
- resolved (2026-09-24): owner deferred publicly trusted distribution. The app is a personal tool, distributed as the self-signed GitHub channel with explicit certificate trust (D-161 amendment). Reopen only by explicit owner request.
- draft: ../specs/AIU-014-public-signing/spec.md (2026-09-23 research).
- source: ../research/sources.md, S-013, S-025, S-026.
- rule: Do not create paid resources or purchase an alternative without the owner.

## PROVIDER-001 - Authentication permission and quota access
- timing: The corresponding provider spike.
- decision already made: Evidence-first product policy allows undocumented integrations.
- unresolved fact: Product policy is not provider authorization. Verify actual scopes, endpoints and refresh behavior.
- rule: Never label an untested or restricted integration officially approved, and never replace unavailable consumer quotas with API billing.

PROVIDER-002's local implementation decision was resolved by the owner's explicit private, unsupported scope amendment on 2026-09-14; see [AIU-007 spec](../specs/AIU-007-claude-integration/spec.md). The [provider restriction](../providers/claude.md) remains recorded and no Anthropic permission is claimed.

Move resolved durable decisions into the appropriate spec or ADR. Do not duplicate the entire accepted register in this inbox.
