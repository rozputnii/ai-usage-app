using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.Infrastructure.Accounts;

/// <summary>Creates one session per owned storage reference; the account service owns disposal.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ProviderSessionFactory(IServiceProvider services, string ownedRoot)
{
    internal string DirectoryFor(Guid storageId)
    {
        if (storageId == Guid.Empty) throw new ArgumentException("An account storage reference is required.", nameof(storageId));
        return Path.Combine(Path.GetFullPath(ownedRoot), "accounts", storageId.ToString("N"));
    }

    internal IProviderSession Create(string provider, Guid storageId) => CreateInDirectory(provider, DirectoryFor(storageId));

    private IProviderSession CreateInDirectory(string provider, string directory)
    {
        var diagnostics = services.GetService<IDiagnosticSink>();
        var clock = services.GetRequiredService<TimeProvider>();
        return provider switch
        {
            "claude" => new ClaudeSession(services.GetRequiredService<ClaudeAuthClient>(), services.GetRequiredService<ClaudeQuotaClient>(),
                new ClaudeStateStore(directory, null, diagnostics), clock, diagnostics),
            "codex" => new CodexSession(services.GetRequiredService<CodexAuthClient>(), services.GetRequiredService<CodexQuotaClient>(),
                new CodexGrantStore(directory, null, diagnostics), new CodexQuotaCache(directory, null, diagnostics),
                services.GetRequiredService<CodexHistoryClient>(), diagnostics),
            "copilot" => new CopilotSession(services.GetRequiredService<CopilotAuthClient>(), services.GetRequiredService<CopilotQuotaClient>(),
                new CopilotStateStore(directory, null, diagnostics), clock, services.GetRequiredService<CopilotHistoryClient>(), diagnostics),
            "antigravity" => new AntigravitySession(services.GetRequiredService<AntigravityAuthClient>(), services.GetRequiredService<AntigravityQuotaClient>(),
                new AntigravityStateStore(directory, null, diagnostics), clock, diagnostics),
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };
    }

    internal static ProviderIdentity? IdentityOf(IProviderSession session) => session switch
    {
        ClaudeSession claude => claude.Identity,
        CodexSession codex => codex.Identity,
        CopilotSession copilot => copilot.Identity,
        AntigravitySession antigravity => antigravity.Identity,
        _ => null
    };
}
