---
id: AIU-028
type: design
status: draft
goal: G-003
scope_version: 1
---
# AIU-028 design

A design is recorded because three tasks move a boundary: F-01 and F-02 introduce a shared
provider-infrastructure seam where four independent copies stand today, F-06 deletes a contract
from Core, and F-05 adds a cross-cutting diagnostics concern that does not currently exist.
Everything else in AIU-028 is a local fix inside an existing boundary and needs no design.

The [AIU-027 design](../AIU-027-architecture-refinement/design.md) is the baseline: Core owns
credential-free contracts and workflows, Infrastructure owns transport and persistence, Windows
owns presentation and desktop lifetime. Nothing here changes that. The shared seam is added
*inside* Infrastructure, below the four provider folders, not across a project boundary.

## The provider seam

Today each provider folder is a self-contained vertical: transport wrapper, exception type,
credential type, state store, session, DI extension. The audit measured three of the four state
stores as near-verbatim copies and all four transport wrappers as structurally identical. The
seam being added is deliberately narrow and holds only what was measured to be identical:

- **Transport.** One translator from `TransportFailure` and `HttpStatusCode` to
  `ProviderFailureKind`, plus the handler configuration. `ProviderHttp` already exists at
  `src/windows/AiUsage.Infrastructure/Providers/ProviderHttp.cs` and is already shared; the four
  wrappers only re-express its result in a per-provider enum. Removing them removes the
  per-provider enums with it.
- **Persistence.** One `ProviderStateLease<TState>` owning the exclusive lock, the reparse-point
  checks, the pending/committed revision generation and the flush-to-disk write. Each provider
  supplies its own file names, entropy string, `JsonSerializerContext` and validation predicate.
  Those four things are exactly what differs between the current copies, so they stay per
  provider and everything else is shared.
- **Exception.** One `ProviderException` carrying `ProviderFailureKind`, the status code and the
  retry-after. `ClaudeException`, `CopilotException` and `AntigravityException` are already
  identical modulo the name.

What deliberately stays per provider: authentication flow, quota parsing, quota-to-snapshot
mapping, endpoint constants, scopes and the session state machine. Those are where real provider
differences live, and a shared abstraction over them would be a framework rather than a seam.

**Alternative rejected: leave the duplication and add a copy checker.** A test that asserts the
three state stores stay textually equivalent would catch divergence without a refactor, and
would be cheaper. It is rejected because it institutionalises the copying: a fifth provider
still costs 250 lines, the checker has to be taught about every intentional difference, and it
would not have caught F-02, where the *older* copy is the one that diverged.

**Alternative rejected: one `ProviderBase` class the sessions inherit.** Rejected because the
sessions are where the providers genuinely differ — device flow against browser flow, workspace
discovery, extra-usage, tier — and a base class would pull that variation into a single type
with per-provider branches. Composition over a small lease and a small translator keeps the
variation in the four session files where it belongs.

## Removing the Codex contract

`ICodexSession`, `CodexSessionState` and `CodexFailureKind` are a pre-AIU-027 contract that
survived because AIU-027 adapted Codex rather than migrating it. Three providers have since been
built directly on `IProviderSession`, so the Codex-only contract has no remaining purpose and
costs a string-based enum bridge (F-03).

Deleting it is a Core surface reduction, not a boundary move: `IProviderSession` is already the
Core port, already credential-free, and already the thing presentation depends on. The migration
is mechanical — `CodexSession` changes its declared interface and its state type — and the
adapter's one genuine behavior, running the legacy synchronous store calls off the dispatcher
(`CodexDashboardSession.cs:27`), is preserved by the shared lease from F-01, which is
asynchronous throughout.

**Alternative rejected: keep the two contracts and make the mapping explicit.** A hand-written
`switch` closes F-03 at a fraction of the cost. It is rejected as the destination, not as a
step: it leaves two enums to maintain in lockstep forever, for a provider that is the only one
needing them. It is retained as the fallback if F-06 is deferred, and the task plan says so.

## Diagnostics

F-05 adds a concern the application does not have. The design constraint is the one already
written in [security and lifecycle](../../platforms/windows/security-and-lifecycle.md): an
allowlisted structure, redacted values, local only, bounded retention, no automatic network
telemetry, and full exception objects and request/response bodies are never generic log
arguments.

That rules out the obvious implementation. Enabling host default logging, or removing
`RemoveAllLoggers()` from the provider transports, would reach the opposite of the policy: the
`IHttpClientFactory` logging that `RemoveAllLoggers()` suppresses is precisely the request and
response logging that must never exist for provider traffic. `RemoveAllLoggers()` stays.

The seam is therefore a narrow application-owned sink with an explicit allowlist, injected where
failures are already caught — the three catches in `App.xaml.cs` and the classified catch that
F-04 introduces in `LiveUsageSource` — rather than an ambient logger available everywhere.
Making the sink hard to reach is the point: a logger injected into the provider clients would
eventually be used to log a payload.

`Microsoft.Extensions.Hosting` is already referenced, so no dependency is added. Whether the
sink writes a file at all, and where it sits relative to the app-owned state root, is a
data-lifecycle decision that belongs to the task, and the task records it under the
security-lifecycle skill before it is implemented.

## Build policy

F-08 and F-09 introduce `Directory.Build.props` and `Directory.Packages.props`. These are
imported implicitly by MSBuild and NuGet and therefore affect every project at once, including
`tools/` and `tests/`. The design decision is to make the move in two separate steps — first the
pure relocation with no behavioral change, verified by a full offline restore and all four
suites, then the analyzer level as its own change — so that a failure in either is
unambiguous about its cause. Combining them would make a new analyzer error and a mis-scoped
property indistinguishable.
