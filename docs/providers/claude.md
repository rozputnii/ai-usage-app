---
provider: claude
source_verified_at: null
live_verified_at: null
confidence: research-required
classification: method-specific-pending
backlog: AIU-007
---
# claude — implementation evidence assignment

Verify the current OAuth usage schema and scoped windows, minimum read scopes, token rotation and passive Windows CLI import. Source S-023 in the original evidence packet reports a third-party authentication restriction; recheck it before implementation and do not portray the integration as provider-approved. Static fixtures and unrelated shell work may proceed independently.

## First actions in OMP
1. Resolve latest stable OMP source and official provider source. Record exact ref/file/functions, not branch-only links.
2. Inspect auth and usage independently; identify all writes/side effects and omit them unless explicitly needed/approved.
3. Capture sanitized identity/quota fixtures only with consent; never credentials or browser cookies in repository/tool transcript.
4. Exercise rotation, cancellation, invalid grant, missing quota field and context switching; distinguish live proof from source inference.
5. Write contract/evidence before production adapter. If unknown or restricted, explicit blocker, not billing-API substitution.

## Not completed
This handoff did not log in, request quota, import a CLI token, or validate account-specific access. Earlier conversation endpoint examples are discovery hints only.
