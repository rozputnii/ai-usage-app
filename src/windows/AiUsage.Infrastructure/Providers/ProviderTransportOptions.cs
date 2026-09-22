namespace AiUsage.Infrastructure.Providers;

internal sealed class ProviderTransportOptions
{
    internal static ProviderTransportOptions Default { get; } = new();

    public TimeSpan RequestDeadline { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan PooledConnectionLifetime { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan RetryAfterFallback { get; init; } = TimeSpan.FromMinutes(1);
}
