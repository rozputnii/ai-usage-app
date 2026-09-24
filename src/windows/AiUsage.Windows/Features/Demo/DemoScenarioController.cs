using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

/// <summary>Display simulation hooks implemented by Platform/ for the demo shell only.</summary>
public interface IDisplaySimulation
{
    /// <summary>Null follows the real Windows app mode.</summary>
    EffectiveTheme? SimulatedSystemTheme { get; set; }
    bool SimulatedHighContrast { get; set; }
    /// <summary>Null follows the Windows animation setting.</summary>
    bool? ReducedMotionOverride { get; set; }
    /// <summary>1, 1.5 or 2: lays out the window content at that effective-pixel scale.</summary>
    double ContentScale { get; set; }
    bool ProviderHues { get; set; }
    void ResizeWindow(int effectiveWidth, int effectiveHeight);
}

/// <summary>Demo-only control surface: scenario selection, clock, forced outcomes and environment simulation.</summary>
internal sealed class DemoScenarioController(
    DemoState state,
    DemoConnectionFlow connection,
    DemoUpdateService updates,
    DemoCliImportService cli,
    INavigationService navigation)
{
    private int loadToken;

    public DemoState State => state;
    public DemoConnectionFlow Connection => connection;
    public string CurrentScenarioId => state.World.ScenarioId;

    public event EventHandler? EnvironmentChanged;

    /// <summary>Raised when a scenario opens the CLI import surface under the Add account menu.</summary>
    public event EventHandler? CliImportRequested;

    /// <summary>Loads a seed, shows the skeleton for the prototype load latency, then opens the scenario's entry surface.</summary>
    public async Task LoadScenarioAsync(string scenarioId, bool openEntry = true)
    {
        var scenario = DemoScenarioCatalog.Find(scenarioId);
        state.LoadScenario(scenarioId);
        EnvironmentChanged?.Invoke(this, EventArgs.Empty);
        var token = ++loadToken;
        if (openEntry)
            navigation.Navigate(scenario.Entry);
        if (!state.Loaded)
        {
            try { await state.DelayAsync(DemoLatency.Load, CancellationToken.None); }
            catch (OperationCanceledException) { return; }
            if (token != loadToken)
                return;
            state.SetLoaded(true);
        }
        if (openEntry && scenario.Surface == DemoEntrySurface.AddAccountCli)
            CliImportRequested?.Invoke(this, EventArgs.Empty);
    }

    public Task ResetAsync() => LoadScenarioAsync(state.World.ScenarioId);

    public async Task ReloadWithSkeletonAsync()
    {
        var token = ++loadToken;
        state.SetLoaded(false);
        try { await state.DelayAsync(DemoLatency.Refresh, CancellationToken.None); }
        catch (OperationCanceledException) { return; }
        if (token == loadToken)
            state.SetLoaded(true);
    }

    public void AdvanceClock(TimeSpan amount)
    {
        state.Clock.Advance(amount);
        state.Publish();
    }

    public DemoRefreshOutcome ForcedRefreshOutcome
    {
        get => state.World.ForcedRefreshOutcome;
        set { state.World.ForcedRefreshOutcome = value; state.Publish(); }
    }

    public bool NotificationsAllowed
    {
        get => state.World.NotificationsAllowed;
        set { state.World.NotificationsAllowed = value; Changed(); }
    }

    public bool QuietHours
    {
        get => state.World.QuietHours;
        set { state.World.QuietHours = value; Changed(); }
    }

    public bool BatterySaver
    {
        get => state.World.BatterySaver;
        set { state.World.BatterySaver = value; Changed(); }
    }

    public bool CompatibilityBlocked
    {
        get => state.World.Compatibility == CompatibilityState.CompatibilityBlocked;
        set { state.World.Compatibility = value ? CompatibilityState.CompatibilityBlocked : CompatibilityState.Normal; state.World.CompatibilityOverridden = false; Changed(); }
    }

    public bool SecurityBlocked
    {
        get => state.World.Compatibility == CompatibilityState.SecurityBlocked;
        set { state.World.Compatibility = value ? CompatibilityState.SecurityBlocked : CompatibilityState.Normal; state.World.CompatibilityOverridden = false; Changed(); }
    }

    public bool FailNextUpdateCheck
    {
        get => updates.FailNextCheck;
        set { updates.FailNextCheck = value; EnvironmentChanged?.Invoke(this, EventArgs.Empty); }
    }

    public bool NextCliScanEmpty
    {
        get => cli.NextScanEmpty;
        set { cli.NextScanEmpty = value; EnvironmentChanged?.Invoke(this, EventArgs.Empty); }
    }

    /// <summary>F07: temporarily removes the second context of the first multi-context account without deleting preferences.</summary>
    public bool SecondContextAvailable
    {
        get => state.World.Accounts.FirstOrDefault(a => a.Contexts.Count > 1)?.Contexts[1].Available ?? true;
        set
        {
            if (state.World.Accounts.FirstOrDefault(a => a.Contexts.Count > 1) is { } account)
            {
                account.Contexts[1].Available = value;
                Changed();
            }
        }
    }

    public bool HasMultiContextAccount => state.World.Accounts.Any(a => a.Contexts.Count > 1);

    public bool ResolveConnection(DemoConnectOutcome outcome) => connection.Resolve(outcome);

    private void Changed()
    {
        state.Publish();
        EnvironmentChanged?.Invoke(this, EventArgs.Empty);
    }
}
