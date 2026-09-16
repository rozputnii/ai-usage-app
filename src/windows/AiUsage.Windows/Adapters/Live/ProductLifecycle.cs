using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

/// <summary>Startup is also owned work: Exit waits even if preferences are still being loaded.</summary>
internal sealed class ProductLifecycle(LiveUsageSource usage, LivePreferenceStore preferences) : IProductLifecycle
{
    private readonly object sync = new();
    private Task? initialization;
    private Task? stopping;
    public Task InitializeAsync()
    {
        lock (sync)
            return stopping is not null ? Task.CompletedTask : initialization ??= InitializeCoreAsync();
    }
    private async Task InitializeCoreAsync()
    {
        await Task.Yield();
        await preferences.LoadAsync(CancellationToken.None);
        await usage.InitializeAsync();
    }
    public Task StopAsync()
    {
        lock (sync) return stopping ??= StopCoreAsync();
    }
    private async Task StopCoreAsync()
    {
        await Task.Yield();
        await usage.StopAsync();
        if (initialization is not null) await initialization;
        await preferences.StopAsync();
    }
}
