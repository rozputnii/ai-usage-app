# Owner inbox and unresolved setup facts

No further product questionnaire blocks bootstrap. Discover setup from the current directory and environment before asking. This is not a list of reasons to stop all preparation.

## SETUP-001 - Target repository and commit identity
- timing: AIU-001, before the first persistent Git write or remote publication.
- discover: Current directory, repository root/remotes/status, configured Git author and existing DCO policy. Redact credentials embedded in remote URLs.
- if missing: Ask only for the non-discoverable target/author authorization required by the next operation. Never invent a GitHub owner, package publisher or Signed-off-by identity.
- effect: Read-only preflight and authorized local document preparation may continue.

## SETUP-002 - OMP roles, authentication and toolchain
- timing: AIU-001 read-only preflight.
- discover: Installed stable OMP/schema, primary/planning/advisor/small/review model mappings and Windows/.NET/Windows SDK availability.
- if missing: Ask one consolidated setup question. The owner performs provider login; do not request tokens in chat.
- effect: Never claim that an unavailable advisor or untested unattended flow is ready.

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

Move resolved durable decisions into the appropriate spec or ADR. Do not duplicate the entire accepted register in this inbox. All inbox entries and durable answers must be English.
