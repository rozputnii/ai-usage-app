using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace AiUsage.ProviderConsole;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["help"])
        {
            Console.WriteLine("AI Usage provider console\n  inspect <quota-json-file>  Normalize a Codex fixture offline.\n  inspect-claude <file>      Normalize a Claude fixture offline.\n  inspect-copilot <file>     Normalize an OMP Copilot fixture offline.\n  inspect-antigravity <file> Normalize an OMP Antigravity fixture offline.\n  login                     Interactive Codex sign-in; credentials remain in memory.\n  claude                    Private unsupported Claude connection with app-owned encrypted state.\n  copilot                   OMP device sign-in with app-owned encrypted state.\n  antigravity               Provider-restricted Antigravity connection with app-owned encrypted state.\n\nNo CLI auth stores, token arguments or inference requests are used.");
            return 0;
        }
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        try
        {
            if (args is ["history", var historyProvider, var ownedDirectory])
                return OperatingSystem.IsWindows() ? await HistoryConsole.RunAsync(historyProvider, ownedDirectory, cancellation.Token) : 2;
            if (args is ["inspect-antigravity", var antigravityPath])
                return await AntigravityConsole.InspectAsync(antigravityPath, cancellation.Token);
            if (args is ["antigravity"])
                return OperatingSystem.IsWindows() ? await AntigravityConsole.RunAsync(cancellation.Token) : 2;
            if (args is ["inspect-copilot", var copilotPath])
                return await CopilotConsole.InspectAsync(copilotPath, cancellation.Token);
            if (args is ["copilot"])
                return OperatingSystem.IsWindows() ? await CopilotConsole.RunAsync(cancellation.Token) : 2;
            if (args is ["inspect-claude", var claudePath])
                return await ClaudeConsole.InspectAsync(claudePath, cancellation.Token);
            if (args is ["claude"])
                return OperatingSystem.IsWindows() ? await ClaudeConsole.RunAsync(cancellation.Token) : 2;
            if (args is ["inspect", var path])
            {
                using var file = File.OpenRead(path);
                if (file.Length > 1024 * 1024)
                    throw new CodexException(ProviderFailureKind.InvalidResponse);
                var bytes = new byte[(int)file.Length];
                await file.ReadExactlyAsync(bytes, cancellation.Token);
                if (file.ReadByte() != -1)
                    throw new CodexException(ProviderFailureKind.InvalidResponse);
                PrintQuota(CodexQuotaParser.Parse(bytes, DateTimeOffset.UtcNow));
                return 0;
            }
            if (args is not ["login"])
            {
                Console.Error.WriteLine("Unknown command. Use --help. Credentials must not be supplied as arguments.");
                return 2;
            }
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Console.Error.WriteLine("Login requires an interactive terminal and explicit consent. Use inspect for offline verification.");
                return 2;
            }
            Console.WriteLine("Experimental Codex subscription integration. Uses the source-observed Codex public client, not an AI Usage registration. Third-party client permission and account access remain unverified.");
            Console.WriteLine("Your default browser opens the provider sign-in page. No settings are changed automatically. Tokens remain in this process only; no CLI credentials are read or modified.");
            Console.Write("Open the provider sign-in page in your browser now? [y/N] ");
            if (!string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase))
                return 0;
            using var services = new ServiceCollection().AddCodexIntegration().BuildServiceProvider();
            var auth = services.GetRequiredService<CodexAuthClient>();
            var quota = services.GetRequiredService<CodexQuotaClient>();
            using var authorization = auth.BeginBrowserLogin();
            Console.WriteLine("Waiting for browser sign-in. Approve only this request; Ctrl+C cancels.");
            if (!TryOpenBrowser(authorization.AuthorizationUrl))
                Console.WriteLine($"Open this URL manually: {authorization.AuthorizationUrl}");
            using var credentials = await auth.CompleteBrowserLoginAsync(authorization, cancellation.Token);
            Console.WriteLine("Signed in with an in-memory session. Account identifiers and credentials are not printed.");
            await ShowQuotaAsync(quota, credentials, cancellation.Token);
            while (!cancellation.IsCancellationRequested)
            {
                Console.Write("usage | refresh-auth | exit > ");
                var command = Console.ReadLine();
                if (command is null || command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;
                try
                {
                    if (command.Equals("usage", StringComparison.OrdinalIgnoreCase))
                        await ShowQuotaAsync(quota, credentials, cancellation.Token);
                    else if (command.Equals("refresh-auth", StringComparison.OrdinalIgnoreCase))
                    {
                        await auth.RefreshAsync(credentials, cancellation.Token);
                        Console.WriteLine("Authentication refreshed in memory. No source credential store was changed.");
                        await ShowQuotaAsync(quota, credentials, cancellation.Token);
                    }
                    else
                        Console.WriteLine("Use usage, refresh-auth or exit.");
                }
                catch (CodexException error)
                {
                    PrintFailure(error);
                    if (credentials.RequiresReauthentication)
                    {
                        Console.WriteLine("A new login is required; this session will not replay its refresh token.");
                        return 3;
                    }
                }
            }
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operation cancelled.");
            return 130;
        }
        catch (CodexException error)
        {
            PrintFailure(error);
            return 3;
        }
        catch (ProviderException error)
        {
            Console.Error.WriteLine($"Provider: {error.Kind}.");
            return 3;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine("Local input could not be read. No file content or credentials were emitted.");
            return 2;
        }
    }

    private static async Task ShowQuotaAsync(CodexQuotaClient quota, CodexCredentials credentials, CancellationToken cancellationToken)
    {
        try { PrintQuota(await quota.GetQuotaAsync(credentials, cancellationToken)); }
        catch (CodexException error)
        {
            PrintFailure(error);
            Console.WriteLine("Quota is unavailable; no zero balance or healthy account state is inferred.");
        }
    }

    private static bool TryOpenBrowser(Uri url)
    {
        try
        {
            using var browser = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
            return true; // A reused browser need not return a new process handle.
        }
        catch (System.ComponentModel.Win32Exception) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static void PrintQuota(QuotaSnapshot snapshot) => Console.WriteLine(JsonSerializer.Serialize(snapshot, ConsoleJson.Default.QuotaSnapshot));

    private static void PrintFailure(CodexException error)
    {
        var provider = error.ProviderErrorCode ?? (error.UnrecognizedProviderError is { } shape ? "unrecognized (" + shape + ")" : "none");
        Console.Error.WriteLine($"Codex: {error.Kind}; provider code: {provider}; HTTP: {(error.StatusCode is { } status ? ((int)status).ToString(System.Globalization.CultureInfo.InvariantCulture) : "not available")}; retry-after seconds: {(error.RetryAfter is { } retry ? retry.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) : "unknown")}.");
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(QuotaSnapshot))]
internal sealed partial class ConsoleJson : JsonSerializerContext;
