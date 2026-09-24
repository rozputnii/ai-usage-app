---
id: AIU-014
type: spec
status: draft
goal: G-004
scope_version: 1
approval_basis: Derived from the owner's 2026-09-23 selection of read-only public-signing research for AIU-014, with owner answers of 2026-09-24 (ineligible for Artifact Signing, free only, name AIUsage). Draft; the free channel choice is unapproved.
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
- Market check, 2026-09-24 (Store catalog API): at least ten free or paid Store apps
  already show AI subscription limits. Several read local CLI sign-ins and call provider
  usage endpoints, including AI Limits (Claude, Codex, Copilot and Antigravity; GPL-3.0),
  TaskbarQuota, UsageScope, Wburn and PowerQuota. This shows that Store certification has
  accepted the category. It does not prove that any provider permits it, and it does not
  change the recorded Claude restriction.

## Owner answers, 2026-09-24

- Eligibility: the owner is an individual outside the US and Canada, with no eligible
  organization. Artifact Signing Public Trust is therefore unavailable, so D-161's
  preferred option cannot be used.
- Spending: free options only. Artifact Signing and paid OV/IV/EV certificates are
  excluded.
- Privacy: the owner accepts that their verified name is shown as publisher.
- Public product name: `AIUsage`.

## Free options remaining

| Option | Trust for users | Identity | Automation fit | Main constraints |
| --- | --- | --- | --- | --- |
| Microsoft Store, individual account | Store signs MSIX; no SmartScreen warning | Name and Publisher are assigned by Partner Center; `AIUsage` becomes the reserved display name | Each submission is certified (not instant). Package flights let known testers receive test packages under the same identity | Free registration with government ID and selfie in about 200 markets; the owner's market is unconfirmed. Store policy review of provider integrations is needed. Changes D-159 and D-162 (direct first, Store later) |
| SignPath Foundation | Publicly trusted OV-level signature | Publisher is SignPath Foundation, not the owner | Every release needs manual approval, so it cannot sign every green main push | Needs an OSI license without proprietary code, and an already released project. MSIX support is not confirmed on its terms page |
| Self-signed only (current) | Users must trust the CER manually | `AiUsage.Dev` | Fully automatic (operational now) | Not suitable for public distribution |

## Proposed direction (pending owner choice)

1. Keep the direct `AiUsage.Dev` self-signed Preview as the owner's automatic test
   channel. It stays operational and unchanged.
2. Recommended: use a free Microsoft Store individual account as the public trusted
   channel for `AIUsage`. Public releases go through Store certification, and optional
   package flights go to known testers. This replaces the direct public Preview/Stable
   feed of D-157 and D-159, and needs owner amendment of D-159, D-161 and D-162 before
   implementation. First steps are a Store-policy review of the provider integrations
   and confirming the owner's market during registration, which the owner must do.
3. Alternative: SignPath Foundation for manually approved public releases only, accepting
   SignPath Foundation as publisher, after its MSIX support and eligibility are confirmed.
4. Never create accounts, submit to Store or apply to SignPath without the owner. The
   owner performs registration and identity verification personally.

## Acceptance (draft, to be restated after the choice)

- AC-01: The owner's channel choice and the resulting decision amendments are recorded.
- AC-02: The exact public identity (Name and Publisher) is recorded before the first
  public build; the development identity and feed are unchanged.
- AC-03: Public artifacts are signed only by the chosen trusted mechanism, never by a
  CI-held long-lived public key, and never published unsigned.
- AC-04: A clean Sandbox without manual certificate trust installs a public build and
  receives the next public version through the chosen channel.
- AC-05: Documentation states the publisher shown to users, the install and update path,
  and its separation from the development channel.

## Sources

- [Artifact Signing quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart): eligibility, validation and certificate subject.
- [Artifact Signing SKUs](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-change-sku) and [FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq): price, fixed CN/O, MSIX publisher mismatch.
- [MSIX signing options](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview) and [code signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options): daily certificates, tax history, OV/HSM, SmartScreen, Store.
- [MSIX persistent identity](https://learn.microsoft.com/en-us/windows/msix/package/persistent-identity): publisher bridging constraints.
- [Distribution feature status](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/distribution-feature-status): `ms-appinstaller:` disabled by default.
- [SignPath Foundation terms](https://signpath.org/terms): publisher and per-release manual approval.
- [Free individual Store registration](https://learn.microsoft.com/en-us/windows/apps/publish/whats-new-individual-developer) and [package flights](https://learn.microsoft.com/en-us/windows/apps/publish/package-flights): free ID-verified accounts, Store signing, tester flights.
