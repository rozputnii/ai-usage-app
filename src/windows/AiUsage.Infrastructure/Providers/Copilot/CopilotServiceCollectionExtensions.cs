using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Copilot;

public static class CopilotServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<CopilotAuthClient>(ConfigureClient).ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
        services.AddHttpClient<CopilotUsageClient>(ConfigureClient).ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
        return services;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static IServiceCollection AddCopilotProductSession(this IServiceCollection services, string ownedStateDirectory)
    {
        services.AddCopilotIntegration();
        services.TryAddSingleton(new CopilotStateStore(ownedStateDirectory));
        services.TryAddSingleton<CopilotSession>();
        return services;
    }

    private static void ConfigureClient(HttpClient client) => client.Timeout = Timeout.InfiniteTimeSpan;
    private static HttpMessageHandler CreateHandler() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };
}
