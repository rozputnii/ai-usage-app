---
id: AIU-007
type: design
status: draft
goal: G-003
scope_version: 1
---
# Claude integration design

This is a conditional implementation design, derived from the authorized goal and inspected AIU-027 code. Authentication selection is unresolved in [PROVIDER-002](../../decisions/pending.md); no production adapter is implemented by this document.

## Approach

Prefer an app-owned provider-supported grant with subscription quota access if Anthropic makes one available. The inspected OMP public-client flow supplies concrete behavior but does not resolve the permission boundary. An explicitly unsupported local experiment is a material owner decision; it must retain the restriction and cannot be relabeled provider-approved. API billing cannot answer the requested consumer-quota question. CLI credential import is outside this task.

Keep three production projects. Core owns credential-free identity, normalized quota and application coordination. Infrastructure owns Claude authorization, token exchange, quota transport, parsing and app-owned state. Windows owns resources, dispatcher access, browser launch and presentation. The console must exercise the same Infrastructure clients before WinUI wiring. A second provider justifies reusing command serialization and quota presentation, but not a provider plugin framework or generic storage framework.

## Identity and quota

Bind a Claude connection to the provider account UUID and grant organization UUID. Email and display labels are metadata, not keys. Missing stable identity prevents credential promotion. A refresh must not silently move the grant to another account or organization; omitted refresh identity retains the previously verified binding. Reconnect must validate the new grant before replacing an existing one.

Reuse the existing percentage-window value model only for actual percentage limits. Represent Claude spend with explicit amount, exponent and currency, independently of subscription percentages and Codex credits. Preserve opaque group names and distinguish unknown kinds from known shared/model-scoped limits. Define duplicate/legacy-versus-current field precedence from the exact provider contract and test it; do not add together representations of the same pool. A timestamp in the past never proves that a reset occurred.

## Credentials and transport

If the authorization decision permits implementation, use the system browser, PKCE and state through the evidenced provider callback. Request only proven necessary scopes; OMP's inference/session/file-management scope set is not automatically appropriate for a monitor. Minimum scopes and truthful application headers require an authorized experiment. Do not imitate Claude Code identity to overcome a rejection.

Use bounded asynchronous reads with per-operation timeouts and cancellation. Disable redirects, cookies and HTTP body/header logging on credential-bearing clients. Keep quota GET policy separate from token POST policy: no automatic replay of an ambiguous rotating exchange. Respect provider throttling without unbounded retries or extra inference calls. Reuse the existing configured `SocketsHttpHandler` pattern with finite connection lifetime; do not add packages for a retry framework.

Keep new Claude files distinct from `codex.grant` and `codex.quota.json`; preserve Codex serializers, DPAPI entropy and schema versions. Bind cache identity to the protected grant generation so an unsuccessful account switch cannot expose another connection's cached reading. Stage ciphertext only, use a recoverable replacement sequence, and preserve unsupported/corrupt records rather than silently overwriting them. Persist a successfully returned rotated grant before honoring subsequent quota cancellation. A provider rotation and local file replacement are not one atomic transaction; ambiguous exchange or failed post-rotation persistence must surface reauthentication/recovery explicitly.

## Workflow and desktop

Use one operation authority for each connection. Reject or coalesce overlapping refresh work; disconnect and shutdown fence later publication and wait for credential work to drain. Independent Codex and Claude failures remain independent. Reuse AIU-027's application workflow and injected dispatcher pattern after tests identify the exact common contract. Keep I/O out of view-model properties and activation. A bounded worker operation may be needed for synchronous DPAPI/replace work; an `Async` method name or `ConfigureAwait(false)` alone does not move it off the dispatcher.

Add only the provider choice/account controls needed to operate Claude alongside Codex. Preserve existing close-to-tray, restore and explicit Exit behavior. Authentication failure presents reauthentication; transient quota failure keeps the grant and labels the last valid cache stale. A failed connection cannot replace an existing healthy card with another account's data.

## Pinned .NET review

Reviewed on 2026-09-14 against SDK `10.0.401` (`global.json`, `allowPrerelease: false`) and the existing `net10.0` projects. Infrastructure uses `Microsoft.Extensions.Http` and `System.Security.Cryptography.ProtectedData` `10.0.12`; Windows uses the existing Host/MVVM/WinUI pins. No dependency or toolchain changes are proposed. [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) lists .NET 10 as a supported LTS release.

- [HTTP lifetime guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory#avoid-typed-clients-in-singleton-services): a singleton capturing typed clients needs a configured `SocketsHttpHandler.PooledConnectionLifetime`, or short-lived clients from the factory. The existing Codex handler already sets five minutes; preserve that behavior and make Claude ownership explicit.
- [Dependency injection disposal](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines): the container disposes services it creates. The desktop owns stop/drain ordering before Host disposal; callers do not dispose injected shared services.
- [Asynchronous file I/O](https://learn.microsoft.com/en-us/dotnet/standard/io/asynchronous-file-i-o): use the asynchronous stream operations to avoid blocking desktop I/O. Cancellation around grant rotation needs a separate persistence boundary.
- [System.Text.Json source generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation): stable credential/storage DTOs use generated metadata, while evolving quota groups use explicit bounded parsing. Streaming serialization requires metadata support; do not select fast-path-only generation.
- [Generic Host lifecycle](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host): retain the existing window-owned lifetime and await stop; do not call a blocking console-style run method on the UI thread.

## Verification plan

Add protocol tests before auth/parser implementation: rejected consent/state, callback cancellation, malformed fields and timestamps, unexpected scope/identity, terminal grant failure, throttling and ambiguous token exchange. Synthetic fixtures must cover legacy and current limits, inactive-but-present groups, null/unknown/exhausted percentages, extra-spend currency/exponent and malformed responses without secret echo.

Add persistence fault tests for account binding, canceled quota after rotation, interrupted staged ciphertext, unsupported records, cache identity, failed removal and preserved Codex records. Add independent workflow/view-model tests for startup cache, overlap, canceled connection, separate provider failures, stale display, command state and shutdown drain. Run the existing validator, Infrastructure and Presentation suites offline.

After implementation, freeze the actual combined diff for a fresh `gpt-5.6-luna`/`max` read-only credential/durable-state review; resolve material findings and verify fixes. Build a new package and exercise actual provider controls plus the existing five Windows lifecycle scenarios in the applicable Windows environment, inspecting screenshots and process assertions. Live consent and account checks remain separate from synthetic/offline UI proof. Integrate only after required acceptance passes, then rerun applicable checks on the combined local-main candidate. Publish only the completed task branch.
