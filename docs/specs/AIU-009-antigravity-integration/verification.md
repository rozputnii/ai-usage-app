# AIU-009 verification

Observed results for the [Antigravity specification](spec.md). Verdicts are PASS, FAIL, NOT_RUN or BLOCKED for what was actually executed. Source inspection and a successful build never establish live-provider or interactive Windows success.

Environment: Windows 11 Pro 26200, .NET SDK from [global.json](../../../global.json), repository `main`. Deterministic checks run locally and offline against restored packages.

## Summary

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Source provenance and provider boundary recorded in [antigravity.md](../../providers/antigravity.md) |
| AC-02 | PASS | `AntigravityProtocolTests`, 13 cases, and the live authorization, exchange and identity steps below |
| AC-03 | PASS for discovery, NOT_RUN for live quota | `AntigravityQuotaParserTests`, the discovery cases in `AntigravityProtocolTests`, and the live `ProjectUnavailable` outcome below |
| AC-04 | PASS (deterministic) | `AntigravitySessionTests`, `AntigravityStateStoreTests` |
| AC-05 | PASS (deterministic) | `AntigravityPresentationTests`, Presentation suite 125/125 |
| AC-06 | PARTIAL | Deterministic checks and the MSIX build pass; live quota, Windows smoke and lifecycle remain NOT_RUN, see below |
| AC-07 | NOT_RUN | Focused independent review not yet run |

## Deterministic checks, 2026-09-20

| Check | Command | Result |
| --- | --- | --- |
| Infrastructure regressions | `dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release --no-restore -- -noLogo` | PASS, 209/209 |
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
| Quota reading | NOT_RUN | Unreachable without a discovered project |

The account has no provisioned Cloud Code Assist workspace, and no Antigravity client is installed on this machine. OMP reaches a project in this situation by calling `v1internal:onboardUser` to provision the free tier, which this specification deliberately excludes as a provider-side write. So the missing quota is not a defect in the implementation: the excluded write is exactly what stands between this account and a readable quota. Whether to enable it is an owner decision on scope and external write authority, recorded as open.

No credential, code, account identifier, project identifier or raw provider payload was retained. The isolated state directory holds only an empty lock file; no `antigravity.state` was written.

NOT_RUN: quota-summary field presence and units on a real account, parity with Antigravity's own settings page, live refresh, Exit/relaunch resume, live reconnect and local disconnect, loopback port fallback, local unpackaged Windows smoke with the new provider, and focused independent credential/state review.
