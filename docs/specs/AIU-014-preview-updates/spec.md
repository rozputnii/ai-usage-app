---
id: AIU-014
type: spec
status: implementing
goal: G-004
scope_version: 1
approval_basis: Derived from the owner's 2026-09-22 request for automatic main-push releases, narrowed to the owner as tester without official signing.
---
# Automatic development Preview updates

Every successful main push produces a distinct signed development MSIX. The only
current audience is the owner as tester; public trust and Stable promotion remain
deferred. Existing AIU-006 forward migration/recovery applies to installed updates.

Owner authorization, 2026-09-22: finish and test the update system, execute the
prepared setup script, and perform the necessary local/browser operations to configure
and verify automatic updates. This includes the dedicated CI key/secrets, Pages,
development certificate trust and test installations. No provider sign-in or credential
import is needed for this task. Preserve existing data and use synthetic test state.

## Acceptance

- AC-01: Successful main push validation triggers serialized Preview publication;
  PRs, failed checks and disabled provisioning cannot access signing/publishing.
- AC-02: Allocate UTC YYYY.M.DDNN.0 versions above all reserved release versions and
  the existing development floor 2026.9.2222.0. Failed reservations remain consumed;
  counter exhaustion and clock regression fail explicitly. Concurrent/retried runs
  cannot replace published package bytes or move the feed to an older source commit.
- AC-03: Retain AiUsage.Dev / CN=AI Usage Development identity. Use a separate
  development-only CI certificate, supplied through GitHub Actions encrypted secrets.
  Never export the existing local signing key. Verify signing before publication.
- AC-04: Publish immutable versioned GitHub prerelease assets and a GitHub Pages
  App Installer feed. Installation associates Windows with the feed; nonblocking
  launch checks and background checks fetch forward updates without forced restart.
- AC-05: A tester explicitly trusts the public development CER once. Explain initial
  prerequisites and the separate installed/unpackaged data roots. Do not change host
  trust, install on the host, copy credentials or provision signing secrets silently.
- AC-06: Exercise two actual signed versions through an App Installer feed in a
  disposable Windows guest and verify installed version, feed registration and durable
  preferences. Automated local release-policy checks and independent signing-boundary
  review pass. Missing credentials/remote/guest evidence stays NOT_RUN or BLOCKED.

## Design and boundaries

Extend the existing Validation workflow with a release job after both required jobs.
Use queue:max serialization for the publication job, with at most 100 queued runs
(GitHub's limit). Reserve a version as a draft prerelease before build. Never overwrite
an existing release asset. Changed bytes always need a fresh version; retries reserve
another version. Compare candidate ancestry against every published Preview:
late completion of an older commit cannot supersede the current feed.

Use GitHub Pages deployment artifacts for the feed, without a generated Git branch.
The feed references immutable HTTPS release asset URLs, carries exact package identity
and dependencies, disables downgrade and launch blocking, and enables background
updates. Windows App Installer owns downloads and binary replacement. A running tray
instance is not forcibly terminated; normal exit permits installation. The existing
in-app update controls will describe/delegate this OS-managed channel rather than
claiming a custom downloader is implemented.

CI signing uses a new dedicated certificate, not provider credentials or the existing
local key. Provisioning creates that certificate only when explicitly requested, keeps
PFX/password off command lines and logs, sends them to the named repository secrets,
and retains only its public CER locally. The hosted job imports the key ephemerally,
trusts its CER only on that disposable runner, and removes temporary key material.
Secret provisioning, Pages activation and host CER trust are separately reviewable
setup actions. Publicly hosted artifacts are visibly labeled development/test only.

No public CA, paid service, Store, Stable release, release pruning, remote compatibility
policy or provider authentication changes. Public signing eligibility and migration
from development publisher identity remain future AIU-014 scope; this selected phase
does not claim completion of trusted public distribution.

## Sources checked 2026-09-22

- [App Installer settings](https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings): launch and eight-hour background checks; forward-only default.
- [Automatic updates](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview): feed registration and Windows-managed policy.
- [GitHub concurrency](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency): queue:max and bounded queue behavior.
