# AIU-009 verification

Observed results for the [Antigravity specification](spec.md). Verdicts are PASS, FAIL, NOT_RUN or BLOCKED for what was actually executed. Source inspection and a successful build never establish live-provider or interactive Windows success.

Environment: Windows 11 Pro 26200, .NET SDK from [global.json](../../../global.json), repository `main`. Deterministic checks run locally and offline against restored packages.

## Summary

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC-01 | PASS | Source provenance and provider boundary recorded in [antigravity.md](../../providers/antigravity.md) |
| AC-02 | PASS (deterministic) | `AntigravityProtocolTests`, 13 cases |
| AC-03 | PASS (deterministic) | `AntigravityQuotaParserTests` and the discovery cases in `AntigravityProtocolTests` |
| AC-04 | PASS (deterministic) | `AntigravitySessionTests`, `AntigravityStateStoreTests` |
| AC-05 | PASS (deterministic) | `AntigravityPresentationTests`, Presentation suite 125/125 |
| AC-06 | BLOCKED | Deterministic checks pass; live provider evidence is blocked, see below |
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
| Diff check | `git diff --check` | PASS |

One existing Presentation case, `OccupiedProviderExplainsLimitWithoutClaimingIdentityVerification`, used `antigravity` as a stand-in for a provider the live flow does not offer. It now uses an unregistered identifier so it still tests refusal rather than the newly supported provider.

## Registration handling

The first implementation commit carried OMP's Google client id and secret and was refused by GitHub push protection on `main`. The block was not bypassed. The registration was removed from the repository instead: the client is read from `AIU_ANTIGRAVITY_CLIENT_ID` and `AIU_ANTIGRAVITY_CLIENT_SECRET`, an absent, partial or unusable pair reports `RegistrationUnavailable` with no provider request, and that state offers no retry action because the fix is not in the app. The secret-scanning allowlist links were not used and no credential reached the remote. The published commit is b2a4288.

## What the deterministic evidence covers

Authentication: the authorization URL carries the selected registration, `response_type=code`, the OMP scope set, `access_type=offline`, `prompt=consent`, S256 PKCE and a `127.0.0.1` `/oauth-callback` redirect; the exchange repeats the exact redirect and a verifier that hashes to the advertised challenge; a foreign `state` is refused and a provider error message is never echoed; cancellation closes the callback without exchanging; malformed, unscoped, wrong-token-type and expiry-invalid responses never promote a grant; renewal keeps an unrotated refresh token and rejects a changed Google subject; `invalid_grant` maps to reauthentication.

Discovery and quota: discovery posts exactly one `loadCodeAssist` with the Antigravity IDE metadata and never `onboardUser`; a missing, empty or ineligible project reports `ProjectUnavailable` without echoing the provider's reason; quota posts the discovered project to `retrieveUserQuotaSummary` only, never the legacy model catalog or a sandbox host; a 429 is not repeated before its `Retry-After`. Parsing preserves provider groups, opaque bucket identifiers, five-hour and weekly durations, reset times and repeated labels; a shared third-party group stays one group; missing and out-of-range fractions stay unknown; a reset time alone is not exhaustion; a remaining amount keeps an unknown unit; disabled buckets are dropped and a fully disabled group is reported as not allowed; malformed payloads are rejected rather than partly interpreted.

Lifecycle: connect, resume, refresh, disconnect and reconnect against the app-owned DPAPI record; a rotated refresh token is durable before the quota request; a reconnect with a different account is refused before discovery, quota or any write; a failed replacement keeps the previous grant and cache; unauthorized quota requires reconnection without another provider request; a failed delete reports recovery and keeps the grant; a cancelled authorization writes nothing; an account without a workspace is never stored as a connection. Storage covers encryption at rest, interrupted-replacement recovery, corrupt and future-version records, invalid identity or workspace values, lease exclusivity, revision conflicts and reparse redirection. Other providers' files in the same directory are untouched.

## Live provider verification

BLOCKED, 2026-09-20. Google's published [Antigravity FAQ](https://antigravity.google/docs/faq) states that using third-party software to access Antigravity violates its Terms of Service and may be grounds for suspension or termination of the account. A live run would exercise that restriction with the owner's own Google account. The owner's task authorization covers browser sign-in for this work, but it predates this finding, so the decision is being confirmed before any provider request is made.

Nothing has contacted Google with this implementation: no authorization URL has been opened, no token exchanged, no `loadCodeAssist` or quota request sent, and no Antigravity state file written outside the temporary directories the test suite creates and deletes.

NOT_RUN until that decision: endpoint eligibility for a client AI Usage does not own, Google's acceptance of the added PKCE parameters and of a loopback port fallback, real project discovery and tier reporting, quota-summary field presence and units on a real account, parity with Antigravity's own settings page, local unpackaged Windows smoke with the new provider, MSIX build, Exit/relaunch resume, live reconnect and disconnect, and focused independent credential/state review.
