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
- status: done
- depends_on: []
- acceptance: AC-01, AC-02, AC-04
- evidence: docs/specs/AIU-014-preview-updates/verification.md (2026-09-23)

- [x] Add `tests/release/Test-PreviewRelease.ps1` behavioral assertions against
  `tools/windows/PreviewRelease.psm1`: same-day increments, calendar rollover,
  floor, overflow, invalid inputs, feed identity/URLs and nonblocking forward policy.
- [x] Run `pwsh -File tests/release/Test-PreviewRelease.ps1` and observe RED; implement
  module and artifact writer; rerun to PASS.
- [x] Add `Publish-Preview.ps1` with draft reservation, signing, immutable uploads and
  feed generation; guard older source ancestry. Add gated job to validation.yml with
  queue:max. Inspect workflow privileges and parse scripts before commit/push.

### T-02 - Signing setup and tester experience
- status: done
- depends_on: [T-01]
- acceptance: AC-03, AC-05
- evidence: docs/specs/AIU-014-preview-updates/review.md (2026-09-23)

- [x] Prepare a dedicated-certificate provisioning script and public-CER installation
  instructions. Do not execute secret/trust provisioning without explicit authority.
- [x] Document OS-managed updates and first-install runtime prerequisites in README.
  Existing live product copy already says updates are managed outside the app; no
  product behavior or UI was changed by this phase.
- [x] Freeze code, obtain required fresh read-only independent review, address material
  findings and commit/push. Non-Codex reviewers were used; the Codex Luna policy did not apply.

### T-03 - Operational verification
- status: done
- depends_on: [T-02]
- acceptance: AC-01, AC-04, AC-06
- evidence: docs/specs/AIU-014-preview-updates/verification.md (2026-09-23)

- [x] Present exact prepared provisioning action; after authority, configure signing
  secrets/Pages, enable publication and observe a real successful main-push release.
- [x] In Sandbox, trust only the public CER, install through the feed, preserve
  synthetic state across a second release, and inspect feed registration and UI.
- [x] Record actual checks and limits in verification.md, update canonical state and
  commit/push. Do not mark trusted public distribution complete.

## Handoff

The development phase is operational as of 2026-09-23. Run 35785324979 failed because the
public CER was only in CurrentUserTrustedPeople, so SignTool /pa rejected the self-signed
root. d28e6be/eb52980 now trust it in LocalMachineRoot on the hosted preview runner only,
and remove it in finally. Independent review passed with no material findings. Hosted
runs 35895908547 and 35899151315 published 2026.9.2301.0 and 2026.9.2302.0 and advanced
the Pages feed. Windows Sandbox installed through the feed and was updated by Windows to
the second version with synthetic preferences preserved. Draft 2026.9.2223.0 stays a
consumed reservation. There is no host trust or host install. Each main push publishes a
Preview. Remaining AIU-014 scope is outside this phase: official signing, Stable, channel
switching and public distribution. Next action: the owner selects the next AIU-014 phase,
starting with public signing eligibility and migration from the development identity.
