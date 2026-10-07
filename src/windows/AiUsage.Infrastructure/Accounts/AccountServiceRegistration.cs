using AiUsage.Core.Accounts;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.Infrastructure.Accounts;

/// <summary>Product account ownership; provider transports are shared, grants and sessions are not.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class AccountServiceRegistration
{
    /// <param name="historyRoot">AIU-047 root for reading history and the identity map; defaults to <paramref name="ownedRoot"/>.</param>
    public static IServiceCollection AddAccountServices(this IServiceCollection services, string ownedRoot, string? historyRoot = null)
    {
        historyRoot ??= ownedRoot;
        ArgumentNullException.ThrowIfNull(services);
        services.AddClaudeIntegration().AddCodexIntegration().AddCopilotIntegration().AddAntigravityIntegration();
        services.AddSingleton(p => new AccountRegistry(ownedRoot, diagnostics: p.GetService<IDiagnosticSink>()));
        services.AddSingleton(p => new ProviderSessionFactory(p, ownedRoot));
        services.AddSingleton(p => new AccountIdentityMap(historyRoot, p.GetService<IDiagnosticSink>()));
        services.AddSingleton<AccountService>();
        services.AddSingleton<IAccountService>(p => p.GetRequiredService<AccountService>());
        services.AddSingleton<AccountMigration>();
        services.AddSingleton(p => new StateMaintenance(ownedRoot, diagnostics: p.GetService<IDiagnosticSink>(),
            accountMigration: p.GetRequiredService<AccountMigration>().RunAsync,
            historyMigration: new HistoryRelocation(ownedRoot, historyRoot, p.GetRequiredService<AccountRegistry>(),
                p.GetRequiredService<AccountIdentityMap>(), p.GetService<IDiagnosticSink>()).RunAsync));
        return services;
    }
}
