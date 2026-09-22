using System.Diagnostics;
using AiUsage.Adapters.Live;
using AiUsage.Core.Usage;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Persistence;
using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.Settings;
using AiUsage.Features.SystemStatusPage;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.Composition;

internal static class LiveServiceRegistration
{
    public static IServiceCollection AddLiveServices(this IServiceCollection services)
    {
        var root = ApplicationStateDirectory.Get();
        services.AddCodexProductSession(Path.Combine(root, "providers"));
        services.AddClaudeProductSession(Path.Combine(root, "providers"));
        services.AddCopilotProductSession(Path.Combine(root, "providers"));
        services.AddAntigravityProductSession(Path.Combine(root, "providers"));
        // Resolve through non-owning factories; each concrete session has one DI disposal owner.
        services.AddKeyedSingleton<Func<IProviderSession>>("codex", (p, _) => () => p.GetRequiredService<CodexSession>());
        services.AddKeyedSingleton<Func<IProviderSession>>("claude", (p, _) => () => p.GetRequiredService<ClaudeSession>());
        services.AddKeyedSingleton<Func<IProviderSession>>("copilot", (p, _) => () => p.GetRequiredService<CopilotSession>());
        services.AddKeyedSingleton<Func<IProviderSession>>("antigravity", (p, _) => () => p.GetRequiredService<AntigravitySession>());
        services.AddSingleton(p => new LiveUsageSource(p.GetRequiredService<ProviderCatalog>(),
            id => p.GetRequiredKeyedService<Func<IProviderSession>>(id)(), p.GetRequiredService<IDiagnosticSink>()));
        services.AddSingleton<IUsageSource>(p => p.GetRequiredService<LiveUsageSource>());
        services.AddSingleton<IProviderHistorySource, LiveProviderHistorySource>();
        services.AddSingleton<IConnectionFlow>(p => new LiveConnectionFlow(p.GetRequiredService<LiveUsageSource>(),
            uri => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })));
        services.AddSingleton(new PresentationPreferenceFile(Path.Combine(root, "preferences")));
        services.AddSingleton(new StateMaintenance(root, LivePreferenceStore.IsValidJson));
        services.AddSingleton<IStateMaintenance>(p => p.GetRequiredService<StateMaintenance>());
        services.AddSingleton(p =>
        {
            var source = p.GetRequiredService<LiveUsageSource>();
            var file = p.GetRequiredService<PresentationPreferenceFile>();
            return source.PreferenceStore = new LivePreferenceStore(source, file.ReadAsync, file.WriteAsync);
        });
        services.AddSingleton<IPreferenceStore>(p => p.GetRequiredService<LivePreferenceStore>());
        services.AddSingleton<IClock, LiveClock>();
        services.AddSingleton<UnavailableServices>();
        services.AddSingleton<IHistorySource>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<ICliImportService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IDiagnosticsService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IDataManagementService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton(p => new LiveRecoveryService(p.GetRequiredService<IStateMaintenance>(), p.GetRequiredService<LiveUsageSource>(), root,
            () => { Process.Start(new ProcessStartInfo(root) { UseShellExecute = true }); return Task.CompletedTask; },
            p.GetRequiredService<StateMaintenance>().ExportDiagnosticsAsync));
        services.AddSingleton<IRecoveryService>(p => p.GetRequiredService<LiveRecoveryService>());
        services.AddSingleton<IUpdateService>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<INotificationPreview>(p => p.GetRequiredService<UnavailableServices>());
        services.AddSingleton<IProductLifecycle, ProductLifecycle>();
        return services;
    }

}
