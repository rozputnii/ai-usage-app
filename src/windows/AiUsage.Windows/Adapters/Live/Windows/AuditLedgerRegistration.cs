using System.Text.Json;
using AiUsage.Adapters.Live;
using AiUsage.Adapters.Live.Audit;
using AiUsage.Core.Budget;
using AiUsage.Core.Diagnostics;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

namespace AiUsage.Composition;

/// <summary>Explicit --demo --audit-input=... replay. Does not register product/provider services.</summary>
internal static class AuditLedgerRegistration
{
    public static void AddAuditLedger(this IServiceCollection services, AuditReplay replay)
    {
        var input = replay.Input;
        var root = replay.Root;
        replay.RecordProcess();
        var receiptGate = new object();
        void Receipt(string value)
        {
            lock (receiptGate) File.AppendAllText(Path.Combine(root, "requests.txt"), value + Environment.NewLine +
                (value.StartsWith("Initialize", StringComparison.Ordinal) ? "Process:" + Environment.ProcessId + ":" + value + Environment.NewLine : string.Empty));
        }
        services.AddSingleton(input);
        services.AddSingleton(new AuditClock(input.Now));
        services.AddSingleton(p => new AuditAccounts(input, p.GetRequiredService<AuditClock>(), Receipt,
            () => File.Exists(Path.Combine(root, "initialize.release"))));
        services.AddSingleton(p => new LocalBudgetStore(root));
        services.AddSingleton(p =>
        {
            var file = new PresentationPreferenceFile(Path.Combine(root, "preferences", "ledger"));
            var preference = new LedgerPreferenceStore(async token => await file.ReadAsync(token) ??
                JsonSerializer.Serialize(new LedgerPreferenceStore.State { Labels = input.Labels }, LedgerPreferenceJson.Default.State), file.WriteAsync);
            return new LiveLedgerSource(p.GetRequiredService<AuditAccounts>(), p.GetRequiredService<LocalBudgetStore>(),
                p.GetRequiredService<LocalBudgetStore>(), new QuotaObservationRecorder(p.GetRequiredService<LocalBudgetStore>()),
                preference, action => LiveLedgerRegistration.Dispatch(p.GetRequiredService<DispatcherQueue>(), action),
                _ => Receipt("BrowserRequested:synthetic"), p.GetRequiredService<AuditClock>(), TimeZoneInfo.FindSystemTimeZoneById(input.ZoneId));
        });
        services.AddSingleton<ILedgerSource>(p => p.GetRequiredService<LiveLedgerSource>());
        if (input.UseProductMaintenance)
        {
            // Exercise actual storage maintenance with the already registered fake account boundary.
            // No credential migration or provider service is registered in this audit composition.
            services.AddSingleton(p => new StateMaintenance(root, diagnostics: p.GetRequiredService<IDiagnosticSink>(),
                accountMigration: _ => Task.CompletedTask));
            services.AddSingleton(p => new LedgerProductLifetime(root, p.GetRequiredService<LiveLedgerSource>(),
                p.GetRequiredService<StateMaintenance>(), p.GetRequiredService<ApplicationDiagnostics>(),
                p.GetRequiredService<DispatcherQueue>(), replay.RestartArguments));
        }
        services.AddSingleton(p => new AuditLedgerLifetime(root, input, p.GetRequiredService<LiveLedgerSource>(),
            p.GetRequiredService<LocalBudgetStore>(), p.GetRequiredService<AuditAccounts>(), Receipt,
            p.GetService<LedgerProductLifetime>()));
    }
}

internal sealed class AuditLedgerLifetime(string root, AuditInput input, LiveLedgerSource source,
    LocalBudgetStore store, AuditAccounts accounts, Action<string> receipt, LedgerProductLifetime? product = null)
{
    public async Task InitializeAsync()
    {
        if (product is not null)
        {
            await product.InitializeAsync();
            return;
        }
        var seeded = Path.Combine(root, "seeded.marker");
        if (!File.Exists(seeded))
        {
            await store.SaveConfigurationAsync(input.Configuration, CancellationToken.None);
            foreach (var batch in input.Observations.Chunk(1024)) await store.AppendAsync(batch, CancellationToken.None);
            File.WriteAllText(seeded, "synthetic");
        }
        source.DiagnosticsPreview = _ => { receipt("PreviewDiagnostics"); return Task.FromResult("SYNTHETIC AUDIT · provider transport disabled · isolated temporary storage"); };
        var recoveryFailures = input.RecoveryFailures;
        source.SupportAction = async (action, token) =>
        {
            receipt("Support:" + action);
            if (action == LedgerSupportAction.RetryRecovery && recoveryFailures-- > 0)
            {
                receipt("RecoveryFailed:synthetic");
                return CommandOutcome.Unavailable;
            }
            if (action is LedgerSupportAction.RetryRecovery or LedgerSupportAction.RestorePreferences) await source.SetRecoveryAsync(null);
            if (action == LedgerSupportAction.ExportRecovery) await File.WriteAllTextAsync(Path.Combine(root, "recovery-diagnostics.txt"), "SYNTHETIC AUDIT\nNo credentials or provider bodies\n", token);
            return CommandOutcome.Done;
        };
        source.DeleteData = async token =>
        {
            receipt("DeleteSyntheticData");
            await store.DeleteAllAsync(token);
            accounts.Clear();
            await source.WaitForIdleAsync();
            return CommandOutcome.Done;
        };
        await source.InitializeAsync(null, CancellationToken.None);
        if (input.Recovery is { } recovery) await source.SetRecoveryAsync(recovery);
    }
    public Task StopAsync() => source.StopAsync();
}
