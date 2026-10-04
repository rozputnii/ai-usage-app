using System.Text.Json;
using AiUsage.Adapters.Live;
using AiUsage.Adapters.Live.Audit;
using AiUsage.Core.Budget;
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
        var receiptGate = new object();
        void Receipt(string value) { lock (receiptGate) File.AppendAllText(Path.Combine(root, "requests.txt"), value + Environment.NewLine); }
        services.AddSingleton(input);
        services.AddSingleton(new AuditClock(input.Now));
        services.AddSingleton(p => new AuditAccounts(input, p.GetRequiredService<AuditClock>(), Receipt));
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
        services.AddSingleton(p => new AuditLedgerLifetime(root, input, p.GetRequiredService<LiveLedgerSource>(),
            p.GetRequiredService<LocalBudgetStore>(), p.GetRequiredService<AuditAccounts>(), Receipt));
    }
}

internal sealed class AuditLedgerLifetime(string root, AuditInput input, LiveLedgerSource source,
    LocalBudgetStore store, AuditAccounts accounts, Action<string> receipt)
{
    public async Task InitializeAsync()
    {
        var seeded = Path.Combine(root, "seeded.marker");
        if (!File.Exists(seeded))
        {
            await store.SaveConfigurationAsync(input.Configuration, CancellationToken.None);
            foreach (var batch in input.Observations.Chunk(1024)) await store.AppendAsync(batch, CancellationToken.None);
            File.WriteAllText(seeded, "synthetic");
        }
        source.DiagnosticsPreview = _ => { receipt("PreviewDiagnostics"); return Task.FromResult("SYNTHETIC AUDIT · provider transport disabled · isolated temporary storage"); };
        source.SupportAction = async (action, token) =>
        {
            receipt("Support:" + action);
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
