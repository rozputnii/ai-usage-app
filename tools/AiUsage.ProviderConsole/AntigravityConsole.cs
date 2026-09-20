using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Antigravity;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;

namespace AiUsage.ProviderConsole;

internal static class AntigravityConsole
{
    internal static async Task<int> InspectAsync(string path, CancellationToken token)
    {
        await using var file = File.OpenRead(path);
        if (file.Length > 1024 * 1024) return 2;
        var bytes = new byte[(int)file.Length];
        await file.ReadExactlyAsync(bytes, token);
        Console.WriteLine(JsonSerializer.Serialize(AntigravityQuotaParser.Parse(bytes, DateTimeOffset.UtcNow), ConsoleJson.Default.QuotaSnapshot));
        return 0;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static async Task<int> RunAsync(CancellationToken token)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("Antigravity login requires an interactive terminal; credentials cannot be arguments.");
            return 2;
        }
        Console.WriteLine("Experimental OMP-compatible Antigravity connection. Google's published terms restrict third-party access to Antigravity and name account suspension as a consequence; provider approval is not established.");
        Console.WriteLine("Choose connect to sign in through your browser. An account with no Cloud Code Assist workspace is enrolled in the free tier while connecting, which changes provider-side entitlement. No CLI login is read.");
        Console.WriteLine("This build vendors no Antigravity OAuth client. Set AIU_ANTIGRAVITY_CLIENT_ID and AIU_ANTIGRAVITY_CLIENT_SECRET for this session to supply one.");
        using var services = new ServiceCollection().AddAntigravityProductSession(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage", "ProviderConsole")).BuildServiceProvider();
        var session = services.GetRequiredService<AntigravitySession>();
        Show(await session.ReadCachedStateAsync(token));
        while (!token.IsCancellationRequested)
        {
            Console.Write("connect | usage | resume | disconnect | exit > ");
            var command = Console.ReadLine();
            if (command is null or "exit") return 0;
            switch (command)
            {
                case "connect":
                    Show(await session.ConnectAsync(url =>
                    {
                        Console.WriteLine("Approve only this sign-in request in the browser. Ctrl+C cancels.");
                        using var browser = Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
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
        Console.WriteLine($"Antigravity: {state.Status}; failure: {state.Failure?.ToString() ?? "none"}; cached: {state.FromCache}.");
        if (state.Quota is { } quota) Console.WriteLine(JsonSerializer.Serialize(quota, ConsoleJson.Default.QuotaSnapshot));
    }
}
