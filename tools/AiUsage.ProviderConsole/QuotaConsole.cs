using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.ProviderConsole;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class QuotaConsole
{
    internal static async Task<int> RunAsync(string provider, string ownedDirectory, CancellationToken token)
    {
        if (provider is not ("codex" or "claude" or "copilot" or "antigravity") || !Directory.Exists(ownedDirectory)) return 2;
        var registration = ConsoleDiagnostics.Services();
        switch (provider)
        {
            case "codex": registration.AddCodexProductSession(ownedDirectory); break;
            case "claude": registration.AddClaudeProductSession(ownedDirectory); break;
            case "copilot": registration.AddCopilotProductSession(ownedDirectory); break;
            case "antigravity": registration.AddAntigravityProductSession(ownedDirectory); break;
        }
        using var services = registration.BuildServiceProvider();
        IProviderSession session = provider switch
        {
            "codex" => services.GetRequiredService<CodexSession>(),
            "claude" => services.GetRequiredService<ClaudeSession>(),
            "copilot" => services.GetRequiredService<CopilotSession>(),
            _ => services.GetRequiredService<AntigravitySession>()
        };
        using var operation = ConsoleDiagnostics.Current?.Begin(DiagnosticOperation.Refresh);
        var state = await session.RefreshAsync(token);
        operation?.SetOutcome(state.Status == ProviderSessionStatus.QuotaAvailable && !state.FromCache ? DiagnosticOutcome.Completed : DiagnosticOutcome.Failed);
        Console.WriteLine($"{provider}: {state.Status}; failure={state.Failure}; cached={state.FromCache}");
        return state.Status == ProviderSessionStatus.QuotaAvailable && !state.FromCache ? 0 : 3;
    }
}
