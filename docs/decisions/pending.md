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
- unresolved fact: Actual legal-entity, billing and identity-validation eligibility.
- source: ../research/sources.md, S-013.
- rule: Do not create paid resources or purchase an alternative without the owner.

## PROVIDER-001 - Authentication permission and quota access
- timing: The corresponding provider spike.
- decision already made: Evidence-first product policy allows undocumented integrations.
- unresolved fact: Product policy is not provider authorization. Verify actual scopes, endpoints and refresh behavior.
- rule: Never label an untested or restricted integration officially approved, and never replace unavailable consumer quotas with API billing.

## PROVIDER-002 - Claude third-party OAuth authorization
- timing: AIU-007, before implementing or launching production Claude authentication.
- affected: AIU-007 / G-003.
- evidence: [Current provider evidence](../providers/claude.md), source-checked on 2026-09-14; [conditional design](../specs/AIU-007-claude-integration/design.md).
- question: Is there Anthropic permission for this application's subscription login and token storage, or does the owner explicitly intend an unsupported private experiment with a different delivery boundary?
- options: Establish provider permission/supported quota access; explicitly authorize and scope an unsupported private experiment while retaining the documented restriction; keep Claude auth blocked. API billing and CLI import are not substitutes for this task.
- recommendation: Keep production authentication and live consent blocked until the authorization basis is established. Complete source/design preparation and retain the task branch without main integration or publication.
- impact: The choice changes authentication authority, requested scopes and acceptance/release boundaries. Owner permission for a private experiment would not grant provider approval. Minimum monitor scopes and truthful-header compatibility would still need separately authorized live proof.
- status: Owner clarification requested on 2026-09-14; no answer recorded.

Move resolved durable decisions into the appropriate spec or ADR. Do not duplicate the entire accepted register in this inbox.
