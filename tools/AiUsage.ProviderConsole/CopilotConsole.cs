using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;

namespace AiUsage.ProviderConsole;

internal static class CopilotConsole
{
    internal static async Task<int> InspectAsync(string path, CancellationToken token)
    {
        await using var file = File.OpenRead(path);
        if (file.Length > 1024 * 1024) return 2;
        var bytes = new byte[(int)file.Length];
        await file.ReadExactlyAsync(bytes, token);
        Console.WriteLine(JsonSerializer.Serialize(CopilotQuotaParser.Parse(bytes, DateTimeOffset.UtcNow), ConsoleJson.Default.QuotaSnapshot));
        return 0;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static async Task<int> RunAsync(CancellationToken token)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("Copilot login requires an interactive terminal; credentials cannot be arguments.");
            return 2;
        }
        Console.WriteLine("Experimental OMP-compatible Copilot connection using OpenCode's registration. Provider approval is not established.");
        Console.WriteLine("Choose connect to authorize in your browser. State is encrypted separately from the Windows app; no CLI login is read.");
        using var services = new ServiceCollection().AddCopilotProductSession(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage", "ProviderConsole")).BuildServiceProvider();
        var session = services.GetRequiredService<CopilotSession>();
        Show(await session.ReadCachedStateAsync(token));
        while (!token.IsCancellationRequested)
        {
            Console.Write("connect | usage | resume | disconnect | exit > ");
            var command = Console.ReadLine();
            if (command is null or "exit") return 0;
            switch (command)
            {
                case "connect":
                    Show(await session.ConnectWithChallengeAsync(challenge =>
                    {
                        Console.WriteLine($"Enter {challenge.UserCode} at {challenge.VerificationUri}. Ctrl+C cancels.");
                        using var browser = Process.Start(new ProcessStartInfo(challenge.VerificationUri.AbsoluteUri) { UseShellExecute = true });
                    }, token));
                    break;
                case "usage": Show(await session.RefreshAsync(token)); break;
                case "resume": Show(await session.ResumeAsync(token)); break;
                case "disconnect": Show(await session.DisconnectAsync(token)); break;
                default: Console.WriteLine("Choose a listed command."); break;
            }
        }
        return 0;
    }
    private static void Show(ProviderSessionState state)
    {
        Console.WriteLine($"Copilot: {state.Status}; failure: {state.Failure?.ToString() ?? "none"}; cached: {state.FromCache}.");
        if (state.Quota is { } quota) Console.WriteLine(JsonSerializer.Serialize(quota, ConsoleJson.Default.QuotaSnapshot));
    }
}
