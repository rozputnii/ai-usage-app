using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

/// <summary>Controllable demo clock: fixed at the fixture instant until advanced. Latency uses real delays in the app.</summary>
internal sealed class DemoClock(Func<TimeSpan, CancellationToken, Task>? delay = null, TimeZoneInfo? timeZone = null) : IClock
{
    private TimeSpan offset;

    public DateTimeOffset UtcNow => DemoScenarioCatalog.T0 + offset;
    public TimeZoneInfo TimeZone { get; } = timeZone ?? TimeZoneInfo.Local;
    public event EventHandler? Changed;

    public Task Delay(TimeSpan duration, CancellationToken cancellationToken) => (delay ?? Task.Delay)(duration, cancellationToken);

    public void Advance(TimeSpan amount)
    {
        offset += amount;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        offset = TimeSpan.Zero;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Mock latencies from the imported prototype.</summary>
internal static class DemoLatency
{
    public static readonly TimeSpan Load = TimeSpan.FromMilliseconds(1300);
    public static readonly TimeSpan Refresh = TimeSpan.FromMilliseconds(1400);
    public static readonly TimeSpan Connect = TimeSpan.FromMilliseconds(900);
    public static readonly TimeSpan CliCandidate = TimeSpan.FromMilliseconds(550);
    public static readonly TimeSpan CliImport = TimeSpan.FromMilliseconds(750);
    public static readonly TimeSpan History = TimeSpan.FromMilliseconds(600);
    public static readonly TimeSpan HealthStep = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan ExportStage = TimeSpan.FromMilliseconds(400);
    public static readonly TimeSpan ReplaceValidate = TimeSpan.FromMilliseconds(800);
    public static readonly TimeSpan ReplaceApply = TimeSpan.FromMilliseconds(1200);
    public static readonly TimeSpan Disconnect = TimeSpan.FromMilliseconds(1000);
    public static readonly TimeSpan DeleteData = TimeSpan.FromMilliseconds(900);
    public static readonly TimeSpan FactoryReset = TimeSpan.FromMilliseconds(1200);
    public static readonly TimeSpan UpdateCheck = TimeSpan.FromMilliseconds(1200);
    public static readonly TimeSpan DownloadStep = TimeSpan.FromMilliseconds(350);
    public static readonly TimeSpan Restart = TimeSpan.FromMilliseconds(1300);
    public static readonly TimeSpan RecoveryStep = TimeSpan.FromMilliseconds(400);
    public static readonly TimeSpan RestoreStep = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan Preview = TimeSpan.FromMilliseconds(150);
}

/// <summary>
/// The single in-memory demo store. Services mutate <see cref="World"/> on the UI thread and call <see cref="Publish"/>;
/// subscribers receive immutable snapshots. Switching scenario bumps <see cref="Generation"/> so in-flight simulated
/// work from the previous scenario ends without touching the new state.
/// </summary>
internal sealed class DemoState
{
    private readonly List<Action<UiSnapshot>> subscribers = [];
    private long revision;

    public DemoState(DemoClock clock, string scenarioId = DemoScenarioCatalog.DefaultScenarioId)
    {
        Clock = clock;
        World = DemoScenarioCatalog.Build(scenarioId);
        Current = BuildSnapshot();
    }

    public DemoClock Clock { get; }
    public DemoWorld World { get; private set; }
    public UiSnapshot Current { get; private set; }
    public int Generation { get; private set; }
    public bool Loaded { get; private set; } = true;
    public bool CliImportedOnce { get; set; }

    public event EventHandler<DemoScenario>? ScenarioLoaded;

    public IDisposable Subscribe(Action<UiSnapshot> subscriber)
    {
        subscribers.Add(subscriber);
        return new Subscription(() => subscribers.Remove(subscriber));
    }

    public void Publish()
    {
        Current = BuildSnapshot();
        foreach (var subscriber in subscribers.ToArray())
            subscriber(Current);
    }

    public DemoAccount? Account(string? id) => World.Accounts.FirstOrDefault(account => account.Id == id);

    /// <summary>Latency that ends with <see cref="OperationCanceledException"/> if the scenario changed meanwhile.</summary>
    public async Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var generation = Generation;
        await Clock.Delay(duration, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (generation != Generation)
            throw new OperationCanceledException("The demo scenario changed.");
    }

    /// <summary>Loads a scenario seed. Theme stays because the demo shell keeps display simulation across scenarios.</summary>
    public void LoadScenario(string scenarioId, bool keepAppearance = true, bool skeleton = true)
    {
        var previous = World;
        Generation++;
        foreach (var account in previous.Accounts)
            account.OperationCancellation?.Cancel();
        World = DemoScenarioCatalog.Build(scenarioId);
        if (keepAppearance)
        {
            World.Theme = previous.Theme;
            World.Density = previous.Density;
            World.UsageDisplay = previous.UsageDisplay;
        }
        CliImportedOnce = false;
        Clock.Reset();
        Loaded = !skeleton || World.Accounts.Count == 0;
        Publish();
        ScenarioLoaded?.Invoke(this, DemoScenarioCatalog.Find(scenarioId));
    }

    public void SetLoaded(bool loaded)
    {
        Loaded = loaded;
        Publish();
    }

    private UiSnapshot BuildSnapshot()
    {
        var accounts = World.Accounts.Select(account => account.ToItem()).ToArray();
        var capabilities = Enum.GetValues<UiCommandKind>().Select(kind => new CapabilityItem(CapabilityKeys.For(kind), null, Availability.Available, null, CapabilityOrigin.Planned))
            .Concat(new[] { CapabilityKeys.ViewHistory, CapabilityKeys.ViewContexts, CapabilityKeys.ViewNativeAmounts, CapabilityKeys.ViewSystemStatus }
                .Select(key => new CapabilityItem(key, null, Availability.Available, null, CapabilityOrigin.Planned)))
            .ToArray();
        return new UiSnapshot(++revision, UiMode.Demo, Clock.UtcNow, accounts, capabilities, World.BuildPreferences(), World.BuildSystem())
        {
            LastRefreshAll = World.LastRefreshAll,
            Loaded = Loaded,
        };
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? onDispose = dispose;
        public void Dispose()
        {
            onDispose?.Invoke();
            onDispose = null;
        }
    }
}
