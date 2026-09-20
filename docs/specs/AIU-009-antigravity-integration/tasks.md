---
id: AIU-009
schema_version: 1
---
# Antigravity execution

### T-01 - Establish provider contract and registration
- status: in-progress
- depends_on: []
- acceptance: AC-01
- evidence: not-run

Record the stable OMP authentication, project-discovery and quota sources, the official Google OAuth and Cloud Code Assist boundary, the registration and scope provenance, and the excluded onboarding write.

### T-02 - Shared authentication, discovery and quota implementation
- status: pending
- depends_on: [T-01]
- acceptance: AC-02, AC-03
- evidence: not-run

Implement the UI-independent loopback authorization, identity validation, read-only project discovery, quota-summary parsing and console surface with synthetic regression coverage.

### T-03 - Protected lifecycle and Windows integration
- status: pending
- depends_on: [T-02]
- acceptance: AC-04, AC-05
- evidence: not-run

Add app-owned DPAPI state with resume, refresh, reconnect and disconnect, then register the live Antigravity session in the existing Windows provider composition without changing the other providers or demo isolation.

### T-04 - Integrated verification and publication
- status: pending
- depends_on: [T-03]
- acceptance: AC-06, AC-07
- evidence: not-run

Run the required regressions, document validation, diff check, MSIX build and local Windows smoke, complete the authorized live lifecycle in an isolated development profile, obtain focused independent credential/state review, and publish to `main`.
