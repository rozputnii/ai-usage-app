---
id: AIU-014
type: spec
status: draft
goal: G-004
scope_version: 1
approval_basis: Derived from the owner's 2026-09-23 selection of read-only public-signing research for AIU-014. Draft only; provider, eligibility, spending and identity decisions are unapproved.
---
# Public signing and production identity

This follows the operational development Preview phase
([preview-updates](../AIU-014-preview-updates/spec.md)). It selects how public Preview and
Stable packages are signed and which package identity they use. No account, identity
validation, paid resource, certificate or workflow change is authorized by this draft.

## Findings, checked 2026-09-23

- Artifact Signing, preferred in D-161, costs $9.99 per month (Basic, 5,000 signatures).
  Billing is not pro-rated. Public Trust is available to organizations in the US,
  Canada, EU, UK, Australia, New Zealand, Japan, South Korea, Singapore, Switzerland,
  Norway and Israel. Individual developers must be in the US or Canada. MSIX guidance
  says organizations need a verifiable tax history of three or more years. Validation
  takes 1–20 business days.
- The certificate CN and O are the validated legal name; custom values are not allowed.
  An individual certificate also shows city, state or province, and country. The MSIX
  `Publisher` must equal the certificate subject exactly, so the final string is known
  only after validation.
- Certificates are reissued daily and valid about three days. Every signature must
  therefore carry an RFC 3161 timestamp. The current development pipeline does not
  timestamp; that is acceptable for its two-year self-signed key, not for public use.
- SmartScreen reputation still builds over time with Artifact Signing or OV/EV. No
  option gives immediate trust outside the Store.
- Signing needs the Artifact Signing client dlib with SignTool, or the documented GitHub
  action. Authenticating through Azure federated credentials should avoid any
  long-lived signing secret; that is unverified here and must be confirmed during
  implementation.
- An OV certificate from a public CA is the documented alternative for individuals
  outside the US and Canada, at roughly $150–300 per year. Since 2023 its key must be
  in an HSM, a token or a cloud HSM; CI use depends on the vendor's cloud-HSM option.
- SignPath Foundation is free for OSI-licensed projects. However, SignPath Foundation
  appears as publisher, and every release needs manual approval. That conflicts with
  automatic Preview for every green main push under one identity for Preview and Stable
  (D-157).
- Microsoft Store re-signs MSIX for free and has no SmartScreen warnings. It is a
  separate identity and channel (D-162) and does not sign direct-download packages.
- MSIX publisher bridging ("persistent identity", Windows 11 21H2+) lets packages signed
  by a new publisher update the old family. It needs an artifact signed by the old key
  before that key expires, and Microsoft says the old certificate must still be
  installed on the machine. That makes it unsuitable for public installs.
- The `ms-appinstaller:` protocol has been disabled by default since December 2023.
  Users must download and open the `.appinstaller` file.

## Proposed direction

1. Use Artifact Signing if the owner qualifies as an individual in the US or Canada, or
   through an eligible organization. Otherwise stop and choose separately between an
   OV/IV CA certificate with a cloud HSM and deferring public distribution. Never
   purchase or create resources without the owner.
2. Give public packages a new identity: a product package name plus the validated
   publisher. `AiUsage.Dev` stays the self-signed development/test channel with its own
   data root. Do not bridge development publisher to public publisher, because only the
   owner has development installs. Moving the owner's data between the two is a
   documented manual step until export/import exists.
3. Sign public Preview on every green main push and promote exact Stable bytes, per
   D-157. Use Azure federated credentials limited to the certificate-profile signer
   role, from the protected main-push job only. Timestamp every signature and verify
   before upload, failing closed.
4. Test rotation with the service's real daily certificates: an update signed on a later
   day, by a different certificate with the same subject, must install over an earlier
   one.

## Acceptance (draft)

- AC-01: The owner records eligibility (individual country or legal entity), provider
  and budget. No paid resource or identity validation exists without that decision.
- AC-02: The exact public `Name` and certificate-subject `Publisher` are recorded before
  the first public build; the development identity and feed are unchanged.
- AC-03: Hosted public signing uses short-lived federated credentials, no long-lived key
  or password. Only the gated main-push job can sign. Every signature is timestamped,
  and verification fails closed before publication.
- AC-04: In a clean Sandbox with no manual certificate trust, two public-signed versions
  signed with different daily certificates install and update through the public feed.
- AC-05: A package signed at least four days earlier still verifies and installs after
  its signing certificate has expired.
- AC-06: Documentation states the publisher details visible to users, expected
  SmartScreen warnings, the download-and-open `.appinstaller` flow, and the separation
  from the development channel.

## Owner decisions required

- Eligibility: individual in the US or Canada, an eligible organization, or neither.
- Privacy: an individual certificate publishes the legal name and city/state/country in
  every signed package.
- Spending: $9.99 per month for Artifact Signing, or an OV certificate if ineligible.
- Public package name, for example `AiUsage`, and whether public Preview starts before
  Stable promotion is designed (AIU-015 remains separate).

## Sources

- [Artifact Signing quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart): eligibility, validation and certificate subject.
- [Artifact Signing SKUs](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-change-sku) and [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq): price, fixed CN/O, MSIX publisher mismatch.
- [MSIX signing options](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview) and [code signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options): daily certificates, tax history, OV/HSM, SmartScreen, Store.
- [MSIX persistent identity](https://learn.microsoft.com/en-us/windows/msix/package/persistent-identity): publisher bridging constraints.
- [Distribution feature status](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/distribution-feature-status): `ms-appinstaller:` disabled by default.
- [SignPath Foundation terms](https://signpath.org/terms): publisher and per-release manual approval.
