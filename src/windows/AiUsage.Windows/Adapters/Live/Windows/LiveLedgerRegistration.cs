using System.Diagnostics;
using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Diagnostics;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Infrastructure.Accounts;
using AiUsage.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

namespace AiUsage.Composition;

internal static class LiveLedgerRegistration
{
    public static IServiceCollection AddLiveLedgerServices(this IServiceCollection services)
    {
        var root = ApplicationStateDirectory.Get();
        services.AddAccountServices(root);
        services.AddSingleton(p => new LocalBudgetStore(root, p.GetRequiredService<IDiagnosticSink>()));
        services.AddSingleton<IReadingSeriesStore>(p => p.GetRequiredService<LocalBudgetStore>());
        services.AddSingleton<IQuotaObservationRecorder, QuotaObservationRecorder>();
        services.AddSingleton(p =>
        {
            var file = new PresentationPreferenceFile(Path.Combine(root, "preferences", "ledger"));
            var queue = p.GetRequiredService<DispatcherQueue>();
            var diagnostics = p.GetRequiredService<IDiagnosticSink>();
            return new LiveLedgerSource(p.GetRequiredService<IAccountService>(), p.GetRequiredService<LocalBudgetStore>(),
                p.GetRequiredService<LocalBudgetStore>(), p.GetRequiredService<IQuotaObservationRecorder>(),
                new LedgerPreferenceStore(file.ReadAsync, file.WriteAsync), action => Dispatch(queue, action),
                uri => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }), diagnostics: diagnostics);
        });
        services.AddSingleton<ILedgerSource>(p => p.GetRequiredService<LiveLedgerSource>());
        services.AddSingleton(p => new LedgerProductLifetime(root, p.GetRequiredService<LiveLedgerSource>(),
            p.GetRequiredService<StateMaintenance>(), p.GetRequiredService<ApplicationDiagnostics>(), p.GetRequiredService<DispatcherQueue>(),
            p.GetRequiredService<ILedgerScheduler>()));
        return services;
    }

    internal static Task Dispatch(DispatcherQueue queue, Action action)
    {
        if (queue.HasThreadAccess) { action(); return Task.CompletedTask; }
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(() =>
        {
            try { action(); completion.TrySetResult(); }
            catch (Exception error) { completion.TrySetException(error); }
        })) completion.TrySetException(new InvalidOperationException("The UI dispatcher is unavailable."));
        return completion.Task;
    }
}
