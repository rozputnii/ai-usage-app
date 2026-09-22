using AiUsage.Core.History;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.ProviderConsole;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class HistoryConsole
{
    internal static async Task<int> RunAsync(string provider, string ownedDirectory, CancellationToken token)
    {
        if (provider is not ("codex" or "copilot") || !Directory.Exists(ownedDirectory)) return 2;
        var registration = new ServiceCollection();
        if (provider == "codex") registration.AddCodexProductSession(ownedDirectory);
        else registration.AddCopilotProductSession(ownedDirectory);
        using var services = registration.BuildServiceProvider();
        IProviderHistorySession session = provider == "codex" ? services.GetRequiredService<CodexSession>() : services.GetRequiredService<CopilotSession>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await session.GetHistoryAsync(new(today.AddDays(-6), today), token);
        Console.WriteLine("Existing AI Usage authorization only. No account identifiers, native values or raw payloads are printed.");
        foreach (var report in result.Reports)
            Console.WriteLine($"{report.Id}: {report.Status}; rows={report.Values.Count}; datedRows={report.Values.Count(v => v.From == v.To)}; unknownValues={report.Values.Count(v => v.Value is null)}");
        return result.Reports.Any(r => r.Status == HistoryStatus.Available) ? 0 : 3;
    }
}
