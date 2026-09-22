using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Antigravity;

public static class AntigravityServiceCollectionExtensions
{
    internal static IServiceCollection AddAntigravityIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<AntigravityAuthClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(ProviderTransport.CreateHandler).RemoveAllLoggers();
        services.AddHttpClient<AntigravityQuotaClient>(ProviderTransport.ConfigureClient).ConfigurePrimaryHttpMessageHandler(ProviderTransport.CreateHandler).RemoveAllLoggers();
        return services;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static IServiceCollection AddAntigravityProductSession(this IServiceCollection services, string ownedStateDirectory)
    {
        services.AddAntigravityIntegration();
        services.TryAddSingleton(new AntigravityStateStore(ownedStateDirectory));
        services.TryAddSingleton(p => new AntigravitySession(p.GetRequiredService<AntigravityAuthClient>(), p.GetRequiredService<AntigravityQuotaClient>(), p.GetRequiredService<AntigravityStateStore>(), p.GetRequiredService<TimeProvider>()));
        return services;
    }

}
