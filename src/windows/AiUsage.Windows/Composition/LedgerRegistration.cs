using AiUsage.Controls.Ledger;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using AiUsage.Features.Ledger.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

namespace AiUsage.Composition;

/// <summary>
/// AIU-038 composition, isolated from the product and the current demo (PD-038-01): "--demo --ledger" starts the new
/// presentation on the synthetic ILedgerSource. Product launch and plain "--demo" never reach this file (D-183). AIU-039
/// adds the live source and the product switch.
/// </summary>
internal static class LedgerRegistration
{
    public static bool Requested(IReadOnlyCollection<string> args) =>
        args.Contains("--demo", StringComparer.Ordinal) && args.Contains("--ledger", StringComparer.Ordinal);

    public static IServiceCollection AddLedgerDemo(this IServiceCollection services)
    {
        services.AddSingleton(DispatcherQueue.GetForCurrentThread());
        services.AddSingleton<ILedgerScheduler, DispatcherLedgerScheduler>();
        services.AddSingleton<DemoLedgerSource>();
        services.AddSingleton<ILedgerSource>(provider => provider.GetRequiredService<DemoLedgerSource>());
        services.AddSingleton(provider => new LedgerTrayViewModel(provider.GetRequiredService<ILedgerSource>()));
        return services;
    }

    /// <summary>The running shell; kept here so the app holds no new field.</summary>
    public static LedgerShell? Current { get; private set; }

    /// <summary>Builds the window and tray. Exit from the tray closes both and ends the app.</summary>
    public static LedgerShell Start(Action exitApp)
    {
        var services = new ServiceCollection().AddLedgerDemo().BuildServiceProvider();
        var demo = services.GetRequiredService<DemoLedgerSource>();
        var scenario = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--scenario=", StringComparison.Ordinal))?["--scenario=".Length..];
        if (scenario is not null && DemoLedgerScenarios.All.Any(s => s.Id == scenario))
            demo.LoadScenario(scenario);
        return Current = new LedgerShell(services, demo, exitApp);
    }
}

/// <summary>The running new presentation: its window, tray popup and services.</summary>
internal sealed class LedgerShell : IDisposable
{
    private readonly ServiceProvider services;
    private readonly LedgerWindow window;
    private readonly LedgerViewModel viewModel;
    private LedgerTrayWindow? popup;

    public LedgerShell(ServiceProvider services, DemoLedgerSource demo, Action exitApp)
    {
        this.services = services;
        LedgerWindow? created = null;
        var controls = new LedgerDemoControls([.. DemoLedgerScenarios.All.Select(s => new DemoScenario(s.Id, s.Title))], demo.LoadScenario, demo.DismissStrip);
        viewModel = new LedgerViewModel(services.GetRequiredService<ILedgerSource>(), services.GetRequiredService<ILedgerScheduler>(), message => created?.Announce(message), controls);
        var tray = services.GetRequiredService<LedgerTrayViewModel>();
        created = window = new LedgerWindow(viewModel, () =>
        {
            Dispose();
            exitApp();
            return Task.CompletedTask;
        }, ShowTray);
        tray.OpenAccountRequested += (_, accountId) =>
        {
            window.ShowAndActivate();
            viewModel.FocusAccount(accountId);
        };
        window.Activate();
    }

    private void ShowTray()
    {
        popup ??= new LedgerTrayWindow(services.GetRequiredService<LedgerTrayViewModel>(), accountId =>
        {
            window.ShowAndActivate();
            viewModel.FocusAccount(accountId);
        });
        popup.ShowNearTray();
    }

    public void Dispose()
    {
        popup?.CloseForExit();
        window.CloseForExit();
        viewModel.Dispose();
        services.Dispose();
    }
}

/// <summary>Runs delayed UI work on the window's dispatcher.</summary>
internal sealed class DispatcherLedgerScheduler(DispatcherQueue queue) : ILedgerScheduler
{
    public IDisposable Schedule(TimeSpan delay, Action action)
    {
        var timer = queue.CreateTimer();
        timer.Interval = delay;
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            action();
        };
        timer.Start();
        return new Stop(timer);
    }

    private sealed class Stop(DispatcherQueueTimer timer) : IDisposable
    {
        public void Dispose() => timer.Stop();
    }
}
