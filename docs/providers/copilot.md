---
provider: copilot
source_verified_at: 2026-09-16
live_verified_at: null
confidence: source-verified-live-pending
classification: method-specific
backlog: AIU-008
---
# GitHub Copilot authentication and usage evidence

Source and official-documentation inspection on 2026-09-16. No live account request has succeeded or been observed: the owner-requested console probe produced no request on 2026-09-16, and the owner then directed completion without it, with live testing to follow in the app. Every live claim below is therefore NOT_RUN. The owner selected the authentication client and quota source in [the AIU-008 spec](../specs/AIU-008-copilot-integration/spec.md).

## Official billing semantics

GitHub's [usage-based billing for individuals](https://docs.github.com/en/copilot/concepts/billing/usage-based-billing-for-individuals) records that, since 1 June 2026, Copilot usage is measured in GitHub AI Credits (1 credit = USD 0.01), priced from input, output and cached tokens per model. Paid individual plans include base credits plus a variable flex allotment. The included allowance resets at 00:00:00 UTC on the first day of each calendar month and does not carry over. Code completions and next edit suggestions are not billed in credits and remain unlimited on paid plans. Additional usage requires a budget.

[What changed with billing (legacy)](https://docs.github.com/en/copilot/reference/copilot-billing/request-based-billing-legacy/what-changed-with-billing) records that some Pro/Pro+ annual subscribers remain on premium request units with model multipliers until their annual plan ends. Premium requests and AI credits are different units and must not be combined. Organization-paid Business/Enterprise seats are governed by administrator budgets, not a personal allowance.

The flex allotment is explicitly variable, so the plan table is not a stable entitlement contract. AI Usage does not derive a remaining allowance or percentage from it.

## Quota method: documented billing usage report

[Billing usage REST endpoints](https://docs.github.com/en/rest/billing/usage) (API version 2022-11-28), inspected 2026-09-16:

| Endpoint | Documented access |
| --- | --- |
| `GET /users/{username}/settings/billing/ai_credit/usage` | Fine-grained token types: GitHub App user access tokens and fine-grained PATs, with "Plan" user permission (read) |
| `GET /users/{username}/settings/billing/premium_request/usage` | Same token types and permission |

Query parameters are `year`, `month` (default current), `day`, `model` and `product`; only the last 24 months are available. Response: `timePeriod{year, month?, day?}`, `user`, optional `product`/`model`, and `usageItems[]` with `product`, `sku`, `model`, `unitType`, `pricePerUnit`, `grossQuantity`, `grossAmount`, `discountQuantity`, `discountAmount`, `netQuantity`, `netAmount`. Documented statuses: 200, 400, 403, 404, 500, 503. The AI credit report applies where the user purchased their own plan.

Interpretation used by AI Usage: gross is consumption, discount is consumption covered by the included allowance, and net is billed usage. The report does not contain the allowance itself or a remaining value. Unit types are opaque, and lines with different unit types are never added together. No currency field is present, so amounts are shown as provider amounts without an inferred currency.

**Unresolved compatibility risk:** the owner selected a classic OAuth App token (below) with this documented report. The documentation lists only fine-grained token types and does not state whether a classic `read:user` OAuth token is accepted. The app surfaces 403/404 as AccessDenied/ReportUnavailable instead of substituting another source. Live verification decides this.

## Authentication method: reused third-party OAuth App, device flow

Primary verification of GitHub's release API on 2026-09-16 identified oh-my-pi [v18.2.2](https://github.com/can1357/oh-my-pi/releases/tag/v18.2.2), published 16:29:05 UTC, tag commit `60c9a115b2e8decc0f75825459362d14188a8bc0`. Files fetched at that commit:

| Source | Relevant declaration/function |
| --- | --- |
| [Auth rule](https://github.com/can1357/oh-my-pi/blob/60c9a115b2e8decc0f75825459362d14188a8bc0/packages/catalog/src/compat/rules/auth/github-copilot.kdl) | `auth "github-copilot"`, custom login/refresh hooks |
| [OAuth hook](https://github.com/can1357/oh-my-pi/blob/60c9a115b2e8decc0f75825459362d14188a8bc0/packages/ai/src/registry/oauth/github-copilot.ts) | `resolveOAuthClientId`, `startDeviceFlow`, `pollForGitHubAccessToken`, `refreshGitHubCopilotToken`, `enableAllGitHubCopilotModels`, `loginGitHubCopilot` |
| [Usage adapter](https://github.com/can1357/oh-my-pi/blob/60c9a115b2e8decc0f75825459362d14188a8bc0/packages/ai/src/usage/github-copilot.ts) | `fetchInternalUsage`, `fetchBillingUsage`, `normalizeQuotaSnapshots`, `normalizeBillingUsage` |

| Item | OMP source behavior; not provider authorization |
| --- | --- |
| Client | Public github.com uses OpenCode's OAuth App `Ov23li8tweQw6odWQebz`; GitHub Enterprise domains use the GitHub-owned Copilot CLI client `Ov23ctDVkRmgkPke0Mmm`. The source comment explains the narrower existing-grant display for OpenCode |
| Scope | `read:user` |
| Flow | Form POST `https://github.com/login/device/code`; poll `https://github.com/login/oauth/access_token` with `grant_type=urn:ietf:params:oauth:grant-type:device_code`; handles `authorization_pending` and `slow_down`; other errors are terminal |
| Lifetime | The GitHub token is stored directly with a ten-year nominal expiry; "refresh" returns the same token. No rotation or exchange |
| Side effects | After login OMP POSTs `/models/{id}/policy` with `state: enabled` for bundled models. AI Usage omits this model-policy mutation |
| Usage | OAuth credentials call undocumented `GET {api}/copilot_internal/user` (fields `copilot_plan`, `quota_reset_date`, `quota_snapshots.{chat,completions,premium_interactions}` with `entitlement`, `remaining`, `percent_remaining`, `unlimited`, `overage_count`, `overage_permitted`). API-key credentials first try the documented premium request report. AI Usage uses neither the internal endpoint nor OMP's premium-SKU summation |

The official [GitHub App device flow](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app#using-the-device-flow-to-generate-a-user-access-token) and [refresh token](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/refreshing-user-access-tokens) documents define the same endpoints, 900-second default code lifetime, 5-second default interval, `slow_down` adding 5 seconds, and terminal `expired_token`, `access_denied`, `device_flow_disabled`, `incorrect_client_credentials` and `incorrect_device_code` errors. Expiring GitHub App tokens last eight hours with six-month refresh tokens that rotate on use; that applies to GitHub Apps, not the OAuth App reused here.

Classification: undocumented third-party public-client reuse. OpenCode's permission for another application to use its client is not established, and GitHub's consent page names OpenCode rather than AI Usage. A registered AI Usage GitHub App with "Plan" read access was offered as the documented app-owned alternative and was not selected. The authorization persists on GitHub after local disconnect. Revoking it requires the owner's action under Settings, Applications, because token deletion requires the OAuth App's client secret.

## Identity and context

`GET /user` returns numeric `id` and `login`. The numeric id is the stable binding key. The login is renameable routing data for the report URL, so it is re-read once per loaded generation and a changed id is an account mismatch. A returned report `user` that differs from the bound login is rejected. GitHub Enterprise Server/Data Residency domains, organization and enterprise billing reports, and multiple accounts are out of scope.

## Context and import capabilities

[Authenticating Copilot CLI](https://docs.github.com/en/copilot/how-tos/copilot-cli/set-up-copilot-cli/authenticate-copilot-cli) documents OAuth (`gho_`), user-owned fine-grained PATs with "Copilot Requests" permission, and GitHub App user tokens (`ghu_`). The CLI stores OAuth tokens in the OS keychain under service `copilot-cli`, falls back to plaintext `~/.copilot/config.json` only after prompting, and resolves `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, `GITHUB_TOKEN`, keychain, then `gh auth token`. These are importable sources in principle, but CLI credential import is excluded and no source store was read. A fine-grained PAT with "Plan" read permission is also a documented route to the reports; entering or storing a user-supplied PAT is not implemented.

## Fixture and verification status

Parser, protocol, session, storage and presentation tests use synthetic fixtures only (`tests/windows/AiUsage.Infrastructure.Tests/Fixtures/copilot-usage.synthetic.json` and inline cases). No real token, account identifier or usage payload was captured. Live NOT_RUN: device consent, token issuance, `/user`, both usage reports, report acceptance for this token type, rate limits, revocation and restart resume.
