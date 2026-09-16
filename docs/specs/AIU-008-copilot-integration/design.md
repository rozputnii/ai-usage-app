---
id: AIU-008
type: design
status: approved
goal: G-003
scope_version: 1
---
# GitHub Copilot integration design

Implements the owner decisions in [spec.md](spec.md) from the [provider evidence](../../providers/copilot.md).

## Boundaries

Core adds credential-free `CopilotIdentity`, `CopilotUsageReport`/`CopilotUsageItem`/`CopilotUsageReading` and `CopilotFailureKind`. `ProviderSessionState` gains an optional `CopilotUsage`, and `IProviderSession` gains a default `PendingUserCode` (null for Codex/Claude). `DashboardWorkflow` passes that code through. Infrastructure owns `CopilotAuthClient` (device flow and `/user`), `CopilotUsageClient`/`CopilotUsageParser` (documented reports), `CopilotStateStore` and `CopilotSession`. Windows owns the card, resources and `CopilotUsageText`. No shared storage or provider framework is introduced.

## Authentication

The device-code response must name `https://github.com/login/device` over the default HTTPS port, so a payload cannot redirect the browser. The polling interval is at least five seconds, and `slow_down` increases it by at least five seconds. The lifetime is capped at 30 minutes. Polling uses the injected `TimeProvider`, so tests are deterministic. Once a token is issued, or a terminal error arrives, the attempt cannot be replayed. The token is accepted only after `/user` returns a positive numeric id and a valid login. The session publishes the user code before invoking the browser callback. Transport reuses `ProviderHttp`: bounded bodies, a 15-second deadline, no redirects or cookies, loggers removed. Failures cross the boundary only as allowlisted kinds.

## Usage

Each refresh re-verifies identity once per loaded generation, then requests the AI-credit report and the premium-request report. Authentication failure, account mismatch and throttling stop the refresh. One unavailable report does not hide the other. If both are unavailable, the AI-credit failure is reported and the cache is labeled stale. Presentation groups lines by opaque unit type, showing used, included, billed and billed amount. The legacy report appears only when it contains lines. No totals across units, percentages, allowances or currency are derived.

## State

`copilot.state` is one DPAPI CurrentUser record (entropy `AiUsage.Copilot.State.v1`), with an exclusive lock file, reparse-point checks, bounded size, a strict source-generated schema and validation. It is separate from Codex and Claude files. The reused OAuth App token does not rotate, so no refresh-uncertainty marker is required. A staged pending file whose move did not complete is deleted on the next load, and the committed generation remains authoritative. The first connection persists the token before its first usage read. A reconnect must read usage with the new token before replacing the previous record, even for another account. A 401 marks the record for reauthentication and stops further requests. Corrupt or unsupported records are preserved and surface RecoveryRequired. Disconnect deletes only the two owned files and tells the owner to revoke the GitHub authorization.

## Verification plan

Synthetic protocol, parser, session and store tests; presentation tests; the existing Codex/Claude suites; a signed package with a `copilot-controls` offline Sandbox scenario; focused independent credential/durable-state review; and owner-led live verification in the installed app.
