# Provider evidence index

Codex now has a source-backed, UI-independent integration library and a console verification surface. Its deterministic protocol tests and synthetic console smoke do not establish live account access or permission to reuse the public Codex client. Other provider files remain research assignments, not a verified endpoint catalog. Inspect actual stable source, record commit/file/function/flow, and obtain explicitly authorized live proof where necessary.

Order: Codex -> Claude -> Copilot -> Antigravity. Missing permission, credentials or access does not block unrelated shell/tests, but the affected integration cannot be marked complete.

Required record per auth/quota method: official/undocumented/reverse-engineered; URLs + exact source ref; client ID/scopes/redirect/PKCE/state; token expiry/refresh/rotation; identity and context; quota fields/units/shared pools/reset clock; fixture origin; source_verified_at vs live_verified_at; actual errors/Retry-After/side effects; permission risk.

No provider config/client secret copied from a chat example as production source of truth. No inference request just to collect quota headers, model policy changes, account onboarding or quota-reset purchases in a read-only monitor.

## Codex console
See [source contracts and permission boundaries](codex.md) and [executed verification](../specs/AIU-003-codex-console/verification.md).

From the repository root, with the verified .NET 10 SDK available:

```text
dotnet run --project tools/AiUsage.ProviderConsole -- --help
dotnet run --project tools/AiUsage.ProviderConsole -- inspect tests/windows/AiUsage.Infrastructure.Tests/Fixtures/codex-usage.synthetic.json
dotnet run --project tests/windows/AiUsage.Infrastructure.Tests -c Release -- -noLogo
dotnet run --project tools/AiUsage.ProviderConsole -- login
```

`inspect` is offline. `login` requires interactive input/output, discloses the experimental client boundary and asks before making an authentication request. The owner completes provider consent themselves. After login, `usage` makes a read-only quota request, `refresh-auth` explicitly refreshes the separate in-memory grant, and `exit` drops the local session. A terminal/ambiguous authentication failure requires a new console login; no CLI refresh token is imported or replayed. Do not pass credentials as arguments or redirect login output.

Consumers use `AddCodexIntegration` to obtain the same typed clients with redirect/cookie handling and HTTP factory logging disabled. Do not substitute an arbitrary or auto-redirecting HttpClient for authenticated operations. The console contains no separate provider HTTP implementation, durable credential store or WinUI dependency. UI integration remains deferred.
