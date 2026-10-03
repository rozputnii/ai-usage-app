using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Claude;

public static class ClaudeServiceCollectionExtensions
{
    internal static IServiceCollection AddClaudeIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(ProviderTransportOptions.Default);
        services.AddHttpClient<ClaudeAuthClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(p => ProviderTransport.CreateHandler(p.GetRequiredService<ProviderTransportOptions>())).RemoveAllLoggers();
        services.AddHttpClient<ClaudeQuotaClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(p => ProviderTransport.CreateHandler(p.GetRequiredService<ProviderTransportOptions>())).RemoveAllLoggers();
        return services;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static IServiceCollection AddClaudeProductSession(this IServiceCollection services, string ownedStateDirectory)
    {
        services.AddClaudeIntegration();
        services.TryAddSingleton(p => new ClaudeStateStore(ownedStateDirectory, null, p.GetService<AiUsage.Core.Diagnostics.IDiagnosticSink>()));
        services.TryAddSingleton(p => new ClaudeSession(p.GetRequiredService<ClaudeAuthClient>(), p.GetRequiredService<ClaudeQuotaClient>(), p.GetRequiredService<ClaudeStateStore>(), p.GetRequiredService<TimeProvider>(), p.GetService<AiUsage.Core.Diagnostics.IDiagnosticSink>()));
        return services;
    }

}
