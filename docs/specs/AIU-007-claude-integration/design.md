---
id: AIU-007
type: design
status: approved
goal: G-003
scope_version: 2
---
# Claude integration design

This implementation design follows the owner-approved private, unsupported OMP-style scope in [spec.md](spec.md), using the inspected AIU-027 boundaries. Provider approval is not established. Work and evidence stay local.

## Approach

Implement the inspected OMP public-client flow as explicitly unsupported, with an app-owned grant. The owner's scope decision permits local development despite the recorded restriction. API billing cannot answer the requested consumer-quota question. CLI credential import is outside this task.

Keep three production projects. Core owns credential-free identity, normalized quota and application coordination. Infrastructure owns Claude authorization, token exchange, quota transport, parsing and app-owned state. Windows owns resources, dispatcher access, browser launch and presentation. The console must exercise the same Infrastructure clients before WinUI wiring. A second provider justifies reusing command serialization and quota presentation, but not a provider plugin framework or generic storage framework.

## Identity and quota

Bind a Claude connection to the provider account UUID and grant organization UUID. Email and display labels are metadata, not keys. Missing stable identity prevents credential promotion. A refresh must not silently move the grant to another account or organization; an omitted identity container retains the previously verified binding, while a present container without a valid UUID is rejected. Every reconnect must validate the new grant and its initial quota before replacing an existing connection, including reconnecting the same identity.

Reuse the existing percentage-window value model only for actual percentage limits. Represent Claude spend with explicit amount, exponent and currency, independently of subscription percentages and Codex credits. Preserve opaque group names and distinguish unknown kinds from known shared/model-scoped limits. Define duplicate/legacy-versus-current field precedence from the exact provider contract and test it; do not add together representations of the same pool. A timestamp in the past never proves that a reset occurred.

## Credentials and transport

Use the system browser, PKCE and state through the evidenced provider callback, with the OMP scope set selected by the owner. Record that a smaller monitor-only scope set is unproven. Use truthful application headers; do not imitate Claude Code identity to overcome a rejection. Support the browser callback and a deliberately submitted code/redirect fallback without putting codes in logs, arguments or persistent state.

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

After implementation, freeze the actual combined diff for a fresh `gpt-5.6-luna`/`max` read-only credential/durable-state review; resolve material findings and verify fixes. Build a new package and exercise actual provider controls plus the existing five Windows lifecycle scenarios in the applicable Windows environment, inspecting screenshots and process assertions. Live consent and account checks remain separate from synthetic/offline UI proof. Check the combined local-main candidate and integrate only after required acceptance passes. Keep the private task branch local.

## Implementation contracts

Quota parsing remains independent of transport. `ClaudeQuotaParser.TryParse(ReadOnlyMemory<byte>, DateTimeOffset, out ClaudeQuotaReading?)` returns false for critical schema failure without retaining or echoing the payload. `ClaudeQuotaReading` contains the existing `QuotaSnapshot` percentage model and a typed `ClaudeExtraUsage` value. The latter preserves explicit currency, minor units and decimal exponent; unknown amounts/currency stay unknown and never borrow Codex credit semantics. No existing Codex quota record is changed.

Authentication is a separate typed client and session. Share the already exercised loopback transport and operation-serialization behavior where their tests demonstrate the same need; provider URLs, token DTOs, identity rules and rotation remain provider-specific. Protected Claude state and cache use their own versioned files and identity binding. Presentation consumes credential-free common session state rather than concrete clients or stores.

The implemented Claude state is one DPAPI-protected grant/cache record (`claude.state`) with a parent-linked pending ciphertext generation. A process holds an exclusive file lease across each operation. A fully staged successor recovers only against the exact committed parent; torn, unsupported or unrelated records are preserved in recovery. Before refresh, the session durably marks the old grant uncertain. Only successful persistence of the returned pair clears that marker; failed or ambiguous exchanges therefore cannot replay an older grant after restart. Refresh is internal to this session authority. Every reconnect checks the new grant's initial quota before replacing the old connection; ordinary rotation persists before later quota cancellation.

The existing Codex session and stored formats remain intact behind a small adapter to the shared credential-free dashboard port. Both providers use the same bounded HTTP and loopback transport, including a five-second incomplete-header deadline, but retain separate protocol/state rules. The desktop selector retains one current connection per provider, matching the existing application slice; deferred multi-account scheduling is not introduced. `is_active` is a severity-ranking flag in the inspected Claude source and does not populate an access entitlement.
