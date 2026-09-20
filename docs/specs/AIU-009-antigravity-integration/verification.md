# AIU-009 verification

Observed results for the [Antigravity specification](spec.md). Verdicts are PASS, FAIL, NOT_RUN or BLOCKED for what was actually executed. Source inspection and a successful build never establish live-provider or interactive Windows success.

Environment: Windows 11 Pro 26200, .NET SDK from [global.json](../../../global.json), repository `main`. Deterministic checks run locally and offline against restored packages.

## Summary

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Source provenance and provider boundary recorded in [antigravity.md](../../providers/antigravity.md) |
| AC-02 | PASS | `AntigravityProtocolTests`, including attempt expiry and callback fallback, and the live authorization, exchange and identity steps below |
| AC-03 | PASS | Discovery, the eligibility fence and the provisioning failure paths in `AntigravityProtocolTests`, confirmed live below |
| AC-08 | PASS | `AntigravityQuotaParserTests` plus the live reading and its parity check below |
| AC-04 | PASS (deterministic) | `AntigravitySessionTests`, `AntigravityStateStoreTests` |
| AC-05 | PASS (deterministic) | `AntigravityPresentationTests`, Presentation suite 125/125 |
| AC-06 | PASS | Deterministic checks, MSIX build, Windows smoke 7/7, and the live connect, quota, refresh, resume, product UI and disconnect run below |
| AC-07 | PASS after fixes | Independent review below; every material finding resolved |
| AC-09 | PASS | `OnlyTheControlPlaneReceivesTheAntigravityClientIdentity` and `TheClientVersionIsOverridableAndRejectsAnUnusableValue`, with the other three providers' truthful identity still pinned by their own suites; confirmed live by the run below |

## Deterministic checks, 2026-09-20

| Check | Command | Result |
| --- | --- | --- |
| Infrastructure regressions | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 226/226 |
| Interactive Windows smoke | `dotnet run --project tests/windows/AiUsage.Windows.Tests -c Release --no-restore -- -noLogo` with `AIU_SMOKE_EXE` and an empty `AIU_DEVELOPMENT_STATE_DIRECTORY` | PASS, 7/7 on a local unlocked desktop |
| Presentation regressions | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 125/125 |
| Validator regressions | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, `{"valid":true,"diagnostics":[]}` |
| Unpackaged Windows build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, zero warnings |
| Provider console build | `dotnet build tools/AiUsage.ProviderConsole/AiUsage.ProviderConsole.csproj -c Release --no-restore` | PASS, zero warnings |
| Unsigned MSIX build | `./tools/windows/Build-Package.ps1 -MsixVersion 2026.9.2002.0` | PASS, `unsigned-validation-only`, sha256 `76890130…8C89A7A1`, one external `mspdbcmf.exe` symbol-tool warning |
| Diff check | `git diff --check` | PASS |

One existing Presentation case, `OccupiedProviderExplainsLimitWithoutClaimingIdentityVerification`, used `antigravity` as a stand-in for a provider the live flow does not offer. It now uses an unregistered identifier so it still tests refusal rather than the newly supported provider.

## Registration handling

The first implementation commit carried OMP's Google client id and secret and was refused by GitHub push protection on `main`. The block was not bypassed. The registration was removed from the repository instead: the client is read from `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET`, an absent, partial or unusable pair reports `RegistrationUnavailable` with no provider request, and that state offers no retry action because the fix is not in the app. The secret-scanning allowlist links were not used and no credential reached the remote. The published commit is b2a4288.

## What the deterministic evidence covers

Authentication: the authorization URL carries the selected registration, `response_type=code`, the OMP scope set, `access_type=offline`, `prompt=consent`, S256 PKCE and a `127.0.0.1` `/oauth-callback` redirect; the exchange repeats the exact redirect and a verifier that hashes to the advertised challenge; a foreign `state` is refused and a provider error message is never echoed; cancellation closes the callback without exchanging; malformed, unscoped, wrong-token-type and expiry-invalid responses never promote a grant; renewal keeps an unrotated refresh token and rejects a changed Google subject; `invalid_grant` maps to reauthentication.

Discovery and quota: discovery posts `loadCodeAssist` with the Antigravity IDE metadata, repeating OMP's project-scoped read, and provisions the free tier through `onboardUser` only on the connect path, only when the account has no current tier, and only when the provider's own tier listing offers that tier; an account that is already provisioned, is refused, or whose offered tiers cannot be read is never sent to provisioning, and a missing project reports `ProjectUnavailable` without echoing the provider's reason; quota posts the discovered project to `retrieveUserQuotaSummary` only, never the legacy model catalog or a sandbox host; a 429 is not repeated before its `Retry-After`. Parsing preserves provider groups, opaque bucket identifiers, five-hour and weekly durations, reset times and repeated labels; a shared third-party group stays one group; missing and out-of-range fractions stay unknown; a reset time alone is not exhaustion; a remaining amount keeps an unknown unit; disabled buckets are dropped and a fully disabled group is reported as not allowed; malformed payloads are rejected rather than partly interpreted.

Lifecycle: connect, resume, refresh, disconnect and reconnect against the app-owned DPAPI record; a rotated refresh token is durable before the quota request; a reconnect with a different account is refused before discovery, quota or any write; a failed replacement keeps the previous grant and cache; unauthorized quota requires reconnection without another provider request; a failed delete reports recovery and keeps the grant; a cancelled authorization writes nothing; an account without a workspace is never stored as a connection. Storage covers encryption at rest, interrupted-replacement recovery, corrupt and future-version records, invalid identity or workspace values, lease exclusivity, revision conflicts and reparse redirection. Other providers' files in the same directory are untouched.

## Live provider verification, 2026-09-20

Google's published [Antigravity FAQ](https://antigravity.google/docs/faq) states that using third-party software to access Antigravity violates its Terms of Service and may be grounds for suspension or termination of the account. The owner was shown that finding and chose to proceed on their own main Google account. That decision is theirs and is recorded here; it does not establish provider approval.

The run used the shipped provider code through a temporary non-interactive harness outside the repository, an isolated state directory in the session scratchpad, and OMP's client supplied only through the process environment. The owner approved the consent screen in their own default browser; the agent's browser-automation tools were refused by the session's permission classifier, so no agent-driven interaction with Google's sign-in pages occurred.

| Step | Verdict | Observed |
| --- | --- | --- |
| Authorization request accepted | PASS | Google served the consent screen for the authorization URL as built, including S256 PKCE, `access_type=offline`, `prompt=consent` and the `http://127.0.0.1:51121/oauth-callback` loopback redirect. The added PKCE parameters and that redirect are accepted in practice, not only in theory |
| Loopback callback | PASS | The redirect reached the app-owned listener with a matching `state`; the browser received the local completion page |
| Token exchange | PASS | The form exchange at `https://oauth2.googleapis.com/token` with client id, client secret, code and verifier returned a grant whose granted scopes satisfied the required control-plane scope; any other outcome would have produced a different failure before discovery |
| Account identity | PASS | The Google userinfo read returned a usable opaque subject |
| Workspace discovery | PASS as designed, no quota | `v1internal:loadCodeAssist` with the Antigravity IDE metadata returned success but no `cloudaicompanionProject`. The session reported `ProjectUnavailable`, wrote no state file, and did not call `onboardUser` |
| Free-tier provisioning | BLOCKED by the provider | The provider declares this account ineligible for the free tier; the fence stopped the write, and a deliberate diagnostic call was refused with `403 PERMISSION_DENIED` |
| Quota reading | NOT_RUN | Unreachable without a workspace |

The account has no provisioned Cloud Code Assist workspace, and no Antigravity client is installed on this machine. The owner then selected OMP's provisioning behavior, and a second live run was made with `onboardUser` implemented.

### Second run, with provisioning enabled, truthful identity

Same result: `ProjectUnavailable`, and the provisioning write was never sent. The eligibility fence stopped it, which a third run with a throwaway diagnostic outside the repository confirmed by asking the control plane directly.

`loadCodeAssist` offers this account exactly one tier, `standard-tier` ("Gemini Code Assist"), flagged `userDefinedCloudaicompanionProject` and `usesGcpTos`. It lists `free-tier` as ineligible with reason code `UNSUPPORTED_CLIENT` and a message saying that this client is no longer supported for Gemini Code Assist for individuals and directing the user to the Antigravity products themselves. The diagnostic then called `onboardUser` deliberately, against the fence the product applies, and the provider answered `403 PERMISSION_DENIED` with reason `FREE_TIER_USER_NOT_ELIGIBLE`. Nothing was provisioned and no entitlement changed.

So the free tier is refused, and the refusal names two different subjects: the tier listing blames the client, the onboarding error blames the account. Separating them would require either presenting the real Antigravity client's identity, which this implementation refuses to do, or an account already known to hold a workspace. The remaining allowed tier is a different product on Google Cloud terms with a user-supplied project, which this feature's outcome explicitly keeps separate from Antigravity subscription quota.

This is a provider-side block, not an implementation defect, and the run is evidence for three design choices: the eligibility fence prevented a write the provider would have rejected, the failure surfaced as a single distinct state, and no provider text reached the app.

### Third run, with the Antigravity client identity

The owner was shown the refusal and the two candidate causes and directed that the app adopt OMP's `antigravity/hub` `User-Agent` on the control plane. That single change resolved it, which settles the ambiguity: the block was the client identity, not the account.

| Step | Verdict | Observed |
| --- | --- | --- |
| Connect | PASS | `QuotaAvailable` with a real quota reading; the DPAPI record was written |
| Workspace | PASS, no write | The provider returned a project and `currentTier` directly, so the eligibility branch and `onboardUser` were never reached. The Gemini window was already part-consumed with a reset one day out, so the workspace predates this session: the earlier "ineligible, no project" answer was a filtered view for an unsupported client, not an unprovisioned account. Nothing was provisioned at any point |
| Quota shape | PASS | Two provider groups, "Gemini Models" with `gemini-weekly` and "Claude and GPT models" with `3p-weekly`, each a seven-day window with a reset time. `PlanType` is the discovered `free-tier` |
| Official parity | PASS | The [plans page](https://antigravity.google/docs/plans) states that accounts below AI Pro and Ultra get quota refreshed weekly, with no five-hour bucket. The response contains exactly the weekly buckets and no five-hour bucket, so the structure matches the published semantics for this plan. A five-hour bucket remains unobserved and unverified |
| Refresh | PASS | Renewal through the stored grant followed by a fresh reading; the cached reading was served first and marked stale |
| Resume | PASS | A separate process read the protected record, served the cached reading, then refreshed to a live one |
| Windows product UI | PASS, agent-observed | The unpackaged Release app showed one Antigravity account, plan `free-tier`, a fresh timestamp, `gemini-weekly` at 63% remaining resetting in 21 hours, and the shared `3p-weekly` group at 100%. The agent captured and inspected the window; the screenshot shows live account quota, so it is kept outside the repository and cannot be corroborated from the tree |
| Disconnect | PASS | `NotConnected`, `antigravity.state` removed; a later process reported `NotConnected` with no cached reading |

An observed provider behavior worth recording: the untouched `3p-weekly` bucket reports a reset time exactly seven days after each request, so its timestamp moved between two readings a minute apart. A reset time on an unused bucket is therefore not a stable instant, and no freshness or consumption may be inferred from its movement.

No credential, code, account identifier, project identifier or raw provider payload was retained in the repository. All live state lived in a session scratchpad directory and was removed by the disconnect; the app's own development profile was never used.

NOT_RUN live: five-hour buckets and paid tiers, AI-credit overage, disabled or exhausted buckets, `remainingAmount` values, rate limiting, server-side revocation, refresh-token rotation, reconnect after a denial, and packaged activation with a connected account. The loopback port fallback now has deterministic coverage but remains unexercised against the provider.

## Independent review, AC-07

Required by CONTRIBUTING for a credential and durable-state change. Performed read-only against the frozen tree at `d7c6334` by a reviewer with no implementation transcript, working from the specification, this record and the diff. It re-ran the Infrastructure and Presentation suites and confirmed the counts claimed above; it did not run the MSIX build, the validator or the interactive Windows smoke, which it recorded as NOT_RUN for itself.

Verdicts: PASS for credential handling, durable state, the OAuth flow, the provider-side write, the client identity, quota parsing and test quality; FAIL for evidence honesty. Nine findings, none disputing the live results.

| Finding | Severity | Resolution |
| --- | --- | --- |
| The deterministic-evidence paragraph still claimed exactly one `loadCodeAssist` and no `onboardUser`, contradicting this same file further down | High | Corrected. The sentence had been accurate before the provisioning amendment and survived it |
| `AntigravityWorkspace` and `AntigravitySession` doc comments still said no tier is provisioned | High | Both rewritten to state the write and where it can happen |
| Backlog scope-note and task T-04 still described onboarding as excluded and live work as blocked | Medium | Both updated |
| The provisioning fence was default-open: a response with no tier listing, or a refusal carrying no `reasonMessage`, would provision | Medium | Fixed in `EnsureFreeTierOffered`; the gate is now positive, requiring the provider to offer the free tier, and six payload shapes covering both holes are pinned in `AnAccountTheProviderDoesNotOfferTheFreeTierIsNeverSentToProvisioning` |
| AC-07 pointed at a section that did not exist | Low | This section |
| The Windows UI row did not distinguish agent-observed evidence, and its screenshot is not in the tree | Low | Labelled agent-observed with the reason the image stays out of the repository |
| The provider record asserted a previous refresh token "must never be replayed", which a failed durable write cannot guarantee | Low | Sentence narrowed to what the code does |
| No coverage for login-attempt expiry, an unlaunchable browser, the loopback port fallback, or in-session access-token renewal | Low | Four tests added; the suite is now 226 |
| The rule this client-identity exception departs from is stated absolutely in the Copilot record, which did not mention the exception | Low | Cross-reference added there |

The reviewer's only requested code change before closing was the provisioning fence. Its integration recommendation was to integrate the code and correct the records first, with no unresolved material finding against AC-02, AC-03, AC-04, AC-05 or AC-08. All nine are resolved above; the suites were re-run after the fixes.

One reviewer observation is recorded rather than changed: a quota group's identity derives from the provider's `displayName`, so a relabelled group changes identity between readings. The response carries no group-level identifier, so there is no more stable choice available.
