using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Diagnostics;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.ProviderConsole;

internal static class LoggingMeasurements
{
    internal static async Task<int> RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-log-measure-" + Guid.NewGuid().ToString("N"));
        var results = new List<object>();
        using var client = new HttpClient(new SyntheticHandler());
        try
        {
            foreach (var enabled in new[] { false, true })
            {
                var startup = Stopwatch.StartNew();
                using var log = enabled ? new FileDiagnostics(root) : null;
                if (log is not null) await log.FlushAsync();
                startup.Stop();
                var options = new ProviderTransportOptions { Diagnostics = log };
                var allocated = GC.GetTotalAllocatedBytes(true);
                var elapsed = Stopwatch.StartNew();
                for (var i = 0; i < 200; i++)
                {
                    using var operation = log?.Begin(DiagnosticOperation.Refresh);
                    using var request = new HttpRequestMessage(HttpMethod.Get, "https://chatgpt.com/backend-api/wham/usage");
                    using var response = await ProviderHttp.SendAsync(client, request, TimeProvider.System, CancellationToken.None, options);
                }
                elapsed.Stop();
                var drain = Stopwatch.StartNew();
                var drained = log is null || await log.FlushAsync(TimeSpan.FromSeconds(10));
                drain.Stop();
                results.Add(new { enabled, attempts = 200, startupMs = startup.Elapsed.TotalMilliseconds,
                    refreshTotalMs = elapsed.Elapsed.TotalMilliseconds, refreshMeanMs = elapsed.Elapsed.TotalMilliseconds / 200,
                    allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated, drainMs = drain.Elapsed.TotalMilliseconds,
                    drained, lostRecords = log?.LostRecords ?? 0, maximumPendingBytes = 16 * 1024 * 1024, maximumPendingEntries = 4096 });
            }
            Console.WriteLine(JsonSerializer.Serialize(results));
            return 0;
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class SyntheticHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"rate_limit":{"primary_window":{"used_percent":12.5,"limit_window_seconds":18000,"reset_at":1800000000}},"plan_type":"pro"}""") });
    }
}
