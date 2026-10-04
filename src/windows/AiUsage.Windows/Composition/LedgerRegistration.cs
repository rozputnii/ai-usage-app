using AiUsage.Controls.Ledger;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger.Demo;
using AiUsage.Features.Ledger.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

namespace AiUsage.Composition;

/// <summary>The sole product presentation; --demo selects its isolated synthetic source.</summary>
internal static class LedgerRegistration
{
    public static IServiceCollection AddLedger(this IServiceCollection services)
    {
        services.AddSingleton(DispatcherQueue.GetForCurrentThread());
        services.AddSingleton<ILedgerScheduler, DispatcherLedgerScheduler>();
        services.AddSingleton(provider => new LedgerTrayViewModel(provider.GetRequiredService<ILedgerSource>()));
        return services;
    }

    public static IServiceCollection AddLedgerDemo(this IServiceCollection services)
    {
        services.AddSingleton<DemoLedgerSource>();
        services.AddSingleton<ILedgerSource>(provider => provider.GetRequiredService<DemoLedgerSource>());
        return services;
    }

    public static LedgerShell Start(IServiceProvider services, Func<Task> exitApp)
    {
        Microsoft.UI.Xaml.Application.Current.Resources.MergedDictionaries.Add(LedgerTheme.Tokens);
        var demo = services.GetService<DemoLedgerSource>();
        var scenario = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--scenario=", StringComparison.Ordinal))?["--scenario=".Length..];
        if (demo is not null && scenario is not null && DemoLedgerScenarios.All.Any(s => s.Id == scenario))
            demo.LoadScenario(scenario);
        return new LedgerShell(services, demo, exitApp);
    }
}

/// <summary>Window and tray ownership. App lifetime drains and disposes the service provider.</summary>
internal sealed class LedgerShell : IDisposable
{
    private readonly IServiceProvider services;
    private readonly LedgerWindow window;
    private readonly LedgerViewModel viewModel;
    private LedgerTrayWindow? popup;

    public LedgerShell(IServiceProvider services, DemoLedgerSource? demo, Func<Task> exitApp)
    {
        this.services = services;
        LedgerWindow? created = null;
        var controls = demo is null ? null : new LedgerDemoControls([.. DemoLedgerScenarios.All.Select(s => new DemoScenario(s.Id, s.Title))], demo.LoadScenario, demo.DismissStrip, demo.FailSignIn);
        viewModel = new LedgerViewModel(services.GetRequiredService<ILedgerSource>(), services.GetRequiredService<ILedgerScheduler>(), message => created?.Announce(message), controls);
        var tray = services.GetRequiredService<LedgerTrayViewModel>();
        created = window = new LedgerWindow(viewModel, exitApp, ShowTray);
        tray.OpenAccountRequested += (_, accountId) =>
        {
            window.ShowAndActivate();
            viewModel.FocusAccount(accountId);
        };
        window.Activate();
    }

    public void Show() => window.ShowAndActivate();

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
