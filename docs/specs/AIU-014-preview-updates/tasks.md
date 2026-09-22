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

- [x] Add `tests/release/Test-PreviewRelease.ps1` behavioral assertions against
  `tools/windows/PreviewRelease.psm1`: same-day increments, calendar rollover,
  floor, overflow, invalid inputs, feed identity/URLs and nonblocking forward policy.
- [x] Run `pwsh -File tests/release/Test-PreviewRelease.ps1` and observe RED; implement
  module and artifact writer; rerun to PASS.
- [x] Add `Publish-Preview.ps1` with draft reservation, signing, immutable uploads and
  feed generation; guard older source ancestry. Add gated job to validation.yml with
  queue:max. Inspect workflow privileges and parse scripts before commit/push.

### T-02 - Signing setup and tester experience
- status: pending
- depends_on: [T-01]
- acceptance: AC-03, AC-05
- evidence: not-run

- [x] Prepare a dedicated-certificate provisioning script and public-CER installation
  instructions. Do not execute secret/trust provisioning without explicit authority.
- [x] Document OS-managed updates and first-install runtime prerequisites in README.
  Existing live product copy already says updates are managed outside the app; no
  product behavior or UI was changed by this phase.
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

Base f708954. Prepared implementation dc0548e; CI fixes 55286df, 9064d76, ff30d80.
23 release-policy assertions, script syntax and full hosted CI at ff30d80 PASS.
Independent review's shallow-checkout defect is resolved. No signing key was created/uploaded and no host
trust changed. Automatic approval review blocked the proposed Sandbox feed proof.
Next action: obtain explicit confirmation for the prepared
`Initialize-PreviewSigning.ps1 -Apply` action (dedicated CI key/secrets, Pages and
publication enablement), then execute it if authorized. See verification.md for limits.
