using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiUsage.Infrastructure.Providers.Codex;

public static class CodexServiceCollectionExtensions
{
    public static IServiceCollection AddCodexIntegration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<CodexAuthClient>(ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
        services.AddHttpClient<CodexQuotaClient>(ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHandler).RemoveAllLoggers();
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
