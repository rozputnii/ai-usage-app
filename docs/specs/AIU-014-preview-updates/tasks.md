---
id: AIU-014
schema_version: 1
---
# Preview update implementation plan

Goal: automatic successful-main development releases and Windows-managed updates.
Architecture: release policy/script -> gated hosted signing -> immutable Releases ->
Pages App Installer feed -> Windows deployment. Existing .NET/WinUI product boundaries
remain. Spec: [spec.md](spec.md). Execute sequentially with executing-plans and
test-driven-development; repository policy selects main and the primary implementer.

## Review focus

Older commit finishing late must not replace the feed. Reservations must survive build
failure. Missing secrets must not produce unsigned releases. PFX/password must not
appear in output. First install must establish feed association and prerequisites.

### T-01 - Release policy and artifacts
- status: in-progress
- depends_on: []
- acceptance: AC-01, AC-02, AC-04
- evidence: not-run

- [ ] Add `tests/release/Test-PreviewRelease.ps1` behavioral assertions against
  `tools/windows/PreviewRelease.psm1`: same-day increments, calendar rollover,
  floor, overflow, invalid inputs, feed identity/URLs and nonblocking forward policy.
- [ ] Run `pwsh -File tests/release/Test-PreviewRelease.ps1` and observe RED; implement
  module and artifact writer; rerun to PASS.
- [ ] Add `Publish-Preview.ps1` with draft reservation, signing, immutable uploads and
  feed generation; guard older source ancestry. Add gated job to validation.yml with
  queue:max. Inspect workflow privileges and parse scripts before commit/push.

### T-02 - Signing setup and tester experience
- status: pending
- depends_on: [T-01]
- acceptance: AC-03, AC-05
- evidence: not-run

- [ ] Prepare a dedicated-certificate provisioning script and public-CER installation
  instructions. Do not execute secret/trust provisioning without explicit authority.
- [ ] Describe OS-managed updates in the live product and README; exercise applicable
  presentation/build checks and include runtime prerequisites in the first install.
- [ ] Freeze code, obtain required fresh read-only independent review, address material
  findings and commit/push. Codex Luna is unavailable; use existing non-Codex reviewer.

### T-03 - Operational verification
- status: pending
- depends_on: [T-02]
- acceptance: AC-01, AC-04, AC-06
- evidence: not-run

- [ ] Present exact prepared provisioning action; after authority, configure signing
  secrets/Pages, enable publication and observe a real successful main-push release.
- [ ] In Sandbox, trust only the public CER, install through the feed, preserve
  synthetic state across a second release, and inspect feed registration and UI.
- [ ] Record actual checks and limits in verification.md, update canonical state and
  commit/push. Do not mark trusted public distribution complete.

## Handoff

Base f708954. Selected development-only phase; host trust and CI signing provisioning
not performed. Next action: add and run release-policy tests before implementation.
