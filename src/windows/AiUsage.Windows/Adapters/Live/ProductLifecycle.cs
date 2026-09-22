using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

/// <summary>Startup is also owned work: Exit waits even if preferences are still being loaded.</summary>
internal sealed class ProductLifecycle : IProductLifecycle
{
    private readonly LiveUsageSource usage;
    private readonly LivePreferenceStore preferences;
    private readonly LiveRecoveryService? recovery;
    public ProductLifecycle(LiveUsageSource usage, LivePreferenceStore preferences, LiveRecoveryService? recovery = null)
    { this.usage = usage; this.preferences = preferences; this.recovery = recovery; }
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
        if (recovery is not null) await recovery.InitializeAsync(StartProductAsync);
        else await StartProductAsync();
    }
    private async Task StartProductAsync()
    {
        await preferences.LoadAsync(CancellationToken.None);
        await usage.InitializeAsync();
    }
    public Task StopAsync()
    {
        lock (sync) return stopping ??= StopCoreAsync();
    }
    private async Task StopCoreAsync()
    {
        var maintenanceStopping = recovery?.StopAsync();
        await Task.Yield();
        await usage.StopAsync();
        if (maintenanceStopping is not null) await maintenanceStopping;
        if (initialization is not null) await initialization;
        await preferences.StopAsync();
    }
}
