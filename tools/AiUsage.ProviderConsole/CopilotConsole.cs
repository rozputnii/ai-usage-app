using AiUsage.Core.Providers.Copilot;
using AiUsage.Infrastructure.Providers.Copilot;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUsage.ProviderConsole;

internal static class CopilotConsole
{
    internal static async Task<int> InspectAsync(string path, CancellationToken token)
    {
        await using var file = File.OpenRead(path);
        if (file.Length > 1024 * 1024) return 2;
        var bytes = new byte[(int)file.Length];
        await file.ReadExactlyAsync(bytes, token);
        if (!CopilotUsageParser.TryParse(bytes, CopilotUsageReportKind.AiCredits, DateTimeOffset.UtcNow, out var report, out _))
        {
            Console.Error.WriteLine("Copilot usage fixture has an unsupported or invalid shape.");
            return 2;
        }
        Console.WriteLine(JsonSerializer.Serialize(report!, CopilotConsoleJson.Default.CopilotUsageReport));
        return 0;
    }

    /// <summary>In-memory device login followed by read-only identity and documented usage report requests.</summary>
    internal static async Task<int> ProbeAsync(CancellationToken token)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("The Copilot probe requires an interactive terminal; credentials cannot be passed as arguments.");
            return 2;
        }
        Console.WriteLine("GitHub Copilot probe. Uses OMP's reused OpenCode OAuth App (scope read:user), not an AI Usage registration.");
        Console.WriteLine("After you approve on github.com it makes read-only GETs: /user and the documented AI-credit and premium-request usage reports.");
        Console.WriteLine("The token stays in this process and is not stored. No CLI login, model policy or inference is used.");
        Console.WriteLine("The GitHub authorization remains listed under Settings > Applications until you revoke it.");
        Console.Write("Request a GitHub device code now? [y/N] ");
        if (!string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase)) return 0;
        using var services = new ServiceCollection().AddCopilotIntegration().BuildServiceProvider();
        var auth = services.GetRequiredService<CopilotAuthClient>();
        var usage = services.GetRequiredService<CopilotUsageClient>();
        try
        {
            var authorization = await auth.BeginDeviceLoginAsync(token);
            Console.WriteLine($"Open {authorization.VerificationUri.AbsoluteUri} and enter code {authorization.UserCode}. Expires {authorization.ExpiresAt:u}. Ctrl+C cancels.");
            var credentials = await auth.CompleteDeviceLoginAsync(authorization, token);
            Console.WriteLine($"Connected. Stable numeric account id verified; granted scope: '{credentials.GrantedScope ?? "(not reported)"}'.");
            foreach (var kind in new[] { CopilotUsageReportKind.AiCredits, CopilotUsageReportKind.PremiumRequests })
            {
                try { Show(await usage.GetUsageAsync(credentials, kind, token)); }
                catch (CopilotException error) { PrintFailure(kind.ToString(), error); }
            }
            return 0;
        }
        catch (CopilotException error)
        {
            PrintFailure("login", error);
            return 3;
        }
    }

    private static void Show(CopilotUsageReport report)
    {
        Console.WriteLine($"{report.Kind}: HTTP 200; period {report.Year}-{report.Month?.ToString(CultureInfo.InvariantCulture) ?? "*"}-{report.Day?.ToString(CultureInfo.InvariantCulture) ?? "*"}; items {report.Items.Count}.");
        foreach (var item in report.Items)
            Console.WriteLine($"  product={item.Product} sku={item.Sku} model={item.Model ?? "-"} unit={item.UnitType} price={item.PricePerUnit} gross={item.GrossQuantity} discount={item.DiscountQuantity} net={item.NetQuantity} netAmount={item.NetAmount}");
    }

    private static void PrintFailure(string step, CopilotException error) =>
        Console.Error.WriteLine($"Copilot {step}: {error.Kind}; HTTP: {(error.StatusCode is { } status ? ((int)status).ToString(CultureInfo.InvariantCulture) : "not available")}; retry-after seconds: {(error.RetryAfter is { } retry ? retry.TotalSeconds.ToString(CultureInfo.InvariantCulture) : "unknown")}.");
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CopilotUsageReport))]
internal partial class CopilotConsoleJson : JsonSerializerContext;
