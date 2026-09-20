# AIU-009 verification

Observed results for the [Antigravity specification](spec.md). Verdicts are PASS, FAIL, NOT_RUN or BLOCKED for what was actually executed. Source inspection and a successful build never establish live-provider or interactive Windows success.

Environment: Windows 11 Pro 26200, .NET SDK from [global.json](../../../global.json), repository `main`. Deterministic checks run locally and offline against restored packages.

## Summary

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Source provenance and provider boundary recorded in [antigravity.md](../../providers/antigravity.md) |
| AC-02 | PASS | `AntigravityProtocolTests`, 13 cases, and the live authorization, exchange and identity steps below |
| AC-03 | PASS | Discovery, the eligibility fence and the provisioning failure paths in `AntigravityProtocolTests`, confirmed live below |
| AC-08 | PASS (deterministic) | `AntigravityQuotaParserTests`; live quota is BLOCKED by the provider, see below |
| AC-04 | PASS (deterministic) | `AntigravitySessionTests`, `AntigravityStateStoreTests` |
| AC-05 | PASS (deterministic) | `AntigravityPresentationTests`, Presentation suite 125/125 |
| AC-06 | PARTIAL | Deterministic checks and the MSIX build pass; live quota, Windows smoke and lifecycle remain NOT_RUN, see below |
| AC-07 | NOT_RUN | Focused independent review not yet run |

## Deterministic checks, 2026-09-20

| Check | Command | Result |
| --- | --- | --- |
| Infrastructure regressions | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 215/215 |
| Presentation regressions | `dotnet run --project tests/windows/AiUsage.Presentation.Tests -c Release --no-restore -- -noLogo` | PASS, 125/125 |
| Validator regressions | `dotnet run --project tests/AiUsage.ProjectValidation.Tests --no-restore -- -noLogo` | PASS |
| Document validation | `dotnet run --project tools/AiUsage.ProjectValidation --no-restore -- --root . --json` | PASS, `{"valid":true,"diagnostics":[]}` |
| Unpackaged Windows build | `dotnet build src/windows/AiUsage.Windows/AiUsage.Windows.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None --no-restore` | PASS, zero warnings |
| Provider console build | `dotnet build tools/AiUsage.ProviderConsole/AiUsage.ProviderConsole.csproj -c Release --no-restore` | PASS, zero warnings |
| Unsigned MSIX build | `./tools/windows/Build-Package.ps1 -MsixVersion 2026.9.2001.0` | PASS, `unsigned-validation-only`, sha256 `C924D71C…AAD9C726`, one external `mspdbcmf.exe` symbol-tool warning |
| Diff check | `git diff --check` | PASS |

One existing Presentation case, `OccupiedProviderExplainsLimitWithoutClaimingIdentityVerification`, used `antigravity` as a stand-in for a provider the live flow does not offer. It now uses an unregistered identifier so it still tests refusal rather than the newly supported provider.

## Registration handling

The first implementation commit carried OMP's Google client id and secret and was refused by GitHub push protection on `main`. The block was not bypassed. The registration was removed from the repository instead: the client is read from `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET`, an absent, partial or unusable pair reports `RegistrationUnavailable` with no provider request, and that state offers no retry action because the fix is not in the app. The secret-scanning allowlist links were not used and no credential reached the remote. The published commit is b2a4288.

## What the deterministic evidence covers

Authentication: the authorization URL carries the selected registration, `response_type=code`, the OMP scope set, `access_type=offline`, `prompt=consent`, S256 PKCE and a `127.0.0.1` `/oauth-callback` redirect; the exchange repeats the exact redirect and a verifier that hashes to the advertised challenge; a foreign `state` is refused and a provider error message is never echoed; cancellation closes the callback without exchanging; malformed, unscoped, wrong-token-type and expiry-invalid responses never promote a grant; renewal keeps an unrotated refresh token and rejects a changed Google subject; `invalid_grant` maps to reauthentication.

Discovery and quota: discovery posts exactly one `loadCodeAssist` with the Antigravity IDE metadata and never `onboardUser`; a missing, empty or ineligible project reports `ProjectUnavailable` without echoing the provider's reason; quota posts the discovered project to `retrieveUserQuotaSummary` only, never the legacy model catalog or a sandbox host; a 429 is not repeated before its `Retry-After`. Parsing preserves provider groups, opaque bucket identifiers, five-hour and weekly durations, reset times and repeated labels; a shared third-party group stays one group; missing and out-of-range fractions stay unknown; a reset time alone is not exhaustion; a remaining amount keeps an unknown unit; disabled buckets are dropped and a fully disabled group is reported as not allowed; malformed payloads are rejected rather than partly interpreted.

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

### Second run, with provisioning enabled

Same result: `ProjectUnavailable`, and the provisioning write was never sent. The eligibility fence stopped it, which a third run with a throwaway diagnostic outside the repository confirmed by asking the control plane directly.

`loadCodeAssist` offers this account exactly one tier, `standard-tier` ("Gemini Code Assist"), flagged `userDefinedCloudaicompanionProject` and `usesGcpTos`. It lists `free-tier` as ineligible with reason code `UNSUPPORTED_CLIENT` and a message saying that this client is no longer supported for Gemini Code Assist for individuals and directing the user to the Antigravity products themselves. The diagnostic then called `onboardUser` deliberately, against the fence the product applies, and the provider answered `403 PERMISSION_DENIED` with reason `FREE_TIER_USER_NOT_ELIGIBLE`. Nothing was provisioned and no entitlement changed.

So the free tier is refused, and the refusal names two different subjects: the tier listing blames the client, the onboarding error blames the account. Separating them would require either presenting the real Antigravity client's identity, which this implementation refuses to do, or an account already known to hold a workspace. The remaining allowed tier is a different product on Google Cloud terms with a user-supplied project, which this feature's outcome explicitly keeps separate from Antigravity subscription quota.

This is a provider-side block, not an implementation defect, and the run is evidence for three design choices: the eligibility fence prevented a write the provider would have rejected, the failure surfaced as a single distinct state, and no provider text reached the app.

No credential, code, account identifier, project identifier or raw provider payload was retained in the repository. The isolated state directory holds only an empty lock file; no `antigravity.state` was written in any of the three runs.

NOT_RUN, and not reachable on this account while the provider refuses the tier: quota-summary field presence and units, parity with Antigravity's own settings page, live refresh, Exit/relaunch resume, live reconnect and local disconnect, loopback port fallback, local unpackaged Windows smoke with a connected account, and focused independent credential/state review.
