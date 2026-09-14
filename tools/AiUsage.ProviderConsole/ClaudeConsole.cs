using AiUsage.Core.Providers.Claude;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers.Claude;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUsage.ProviderConsole;

internal static class ClaudeConsole
{
    internal static async Task<int> InspectAsync(string path, CancellationToken token)
    {
        await using var file = File.OpenRead(path);
        if (file.Length > 1024 * 1024) return 2;
        var bytes = new byte[(int)file.Length];
        await file.ReadExactlyAsync(bytes, token);
        if (!ClaudeQuotaParser.TryParse(bytes, DateTimeOffset.UtcNow, out var reading))
        {
            Console.Error.WriteLine("Claude fixture has an unsupported or invalid shape.");
            return 2;
        }
        Console.WriteLine(JsonSerializer.Serialize(reading, ClaudeConsoleJson.Default.ClaudeQuotaReading));
        return 0;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static async Task<int> RunAsync(CancellationToken token)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("Claude connection requires an interactive terminal; credentials cannot be passed as arguments.");
            return 2;
        }
        Console.WriteLine("Private, unsupported Claude integration using OMP's flow. Anthropic prohibits third-party Claude.ai login and token storage. This is not provider-approved.");
        Console.WriteLine("This console uses its own encrypted connection. It does not read CLI logins or the desktop app's connection. Choose connect to open consent in your browser.");
        using var services = new ServiceCollection().AddClaudeIntegration().BuildServiceProvider();
        var store = new ClaudeStateStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiUsage", "ProviderConsole"));
        ClaudeSession CreateSession() => new(services.GetRequiredService<ClaudeAuthClient>(), services.GetRequiredService<ClaudeQuotaClient>(), store, TimeProvider.System);
        var session = CreateSession();
        Task<ProviderSessionState>? activeConnection = null;
        try
        {
            Show(await session.ReadCachedStateAsync(token));
            while (true)
            {
                Console.Write("connect | usage | renew | disconnect | exit > ");
                var command = await ReadLineAsync(echo: true, token);
                if (command is null or "exit") return 0;
                if (command == "connect")
                {
                    using var attemptCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                    var connect = session.ConnectAsync(url =>
                    {
                        using var browser = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
                        if (browser is null) throw new InvalidOperationException();
                    }, attemptCancellation.Token);
                    activeConnection = connect;
                    Console.WriteLine("Complete sign-in in your browser. M enters a hidden fallback code; Escape cancels.");
                    while (!connect.IsCompleted)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(intercept: true).Key;
                            if (key == ConsoleKey.Escape) attemptCancellation.Cancel();
                            if (key == ConsoleKey.M)
                            {
                                Console.Write("Code or final redirect (hidden): ");
                                var code = await ReadLineAsync(echo: false, attemptCancellation.Token, connect);
                                Console.WriteLine(session.TrySubmitCode(code ?? "") ? "Code submitted." : "Code did not match this attempt.");
                            }
                        }
                        await Task.Delay(50, token);
                    }
                    try { Show(await connect); }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested) { Console.WriteLine("Connection canceled."); }
                }
                else if (command == "usage") Show(await session.RefreshAsync(token));
                else if (command == "renew")
                {
                    session.Dispose();
                    session = CreateSession();
                    Show(await session.ResumeAsync(token));
                }
                else if (command == "disconnect") Show(await session.DisconnectAsync(token));
                else Console.WriteLine("Choose a listed command.");
            }
        }
        finally
        {
            if (activeConnection is not null)
            {
                try { await activeConnection; }
                catch (OperationCanceledException) { }
            }
            session.Dispose();
        }
    }

    private static void Show(ProviderSessionState state)
    {
        Console.WriteLine($"Claude: {state.Status}; failure: {state.Failure?.ToString() ?? "none"}; cached: {state.FromCache}.");
        if (state.Quota is { } quota)
            Console.WriteLine(JsonSerializer.Serialize(new ClaudeQuotaReading(quota, state.ExtraUsage), ClaudeConsoleJson.Default.ClaudeQuotaReading));
    }

    private static async Task<string?> ReadLineAsync(bool echo, CancellationToken token, Task? stopWhenComplete = null)
    {
        var input = new StringBuilder();
        while (true)
        {
            token.ThrowIfCancellationRequested();
            if (stopWhenComplete?.IsCompleted == true) return null;
            if (!Console.KeyAvailable) { await Task.Delay(50, token); continue; }
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return input.ToString(); }
            if (key.Key == ConsoleKey.Escape) { Console.WriteLine(); return null; }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0) { input.Length--; if (echo) Console.Write("\b \b"); }
            }
            else if (!char.IsControl(key.KeyChar) && input.Length < (echo ? 128 : 65536))
            {
                input.Append(key.KeyChar);
                if (echo) Console.Write(key.KeyChar);
            }
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ClaudeQuotaReading))]
internal partial class ClaudeConsoleJson : JsonSerializerContext;
