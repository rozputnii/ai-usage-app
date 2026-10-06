using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Persistence;
using AiUsage.Infrastructure.Providers.Antigravity;
using AiUsage.Infrastructure.Providers.Claude;
using AiUsage.Infrastructure.Providers.Codex;
using AiUsage.Infrastructure.Providers.Copilot;

namespace AiUsage.ProviderConsole;

/// <summary>Offline, deterministic workload; never resolves sessions or opens existing app data.</summary>
internal static class BackendMeasurements
{
    private const int Accounts = 4;
    private const int ObservationsPerSeries = 35 * 24 * 12;
    private static readonly DateTimeOffset Start = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = Start.AddDays(35).AddMinutes(-5);

    internal static async Task<int> RunAsync(CancellationToken token)
    {
#if DEBUG
        Console.Error.WriteLine("Measurements require a Release build.");
        return 2;
#else
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Runtime = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Environment.ProcessorCount,
            Accounts, LimitsPerAccount = 2, Days = 35, SamplingMinutes = 5,
            ObservationsPerSeries, Warmups = 3, Repetitions = 9,
            Cold = "First invocation in this process; OS filesystem cache is not flushed.",
            Allocations = "Process-wide managed allocation delta; includes async work, excludes setup."
        }));
        var directory = Path.Combine(Path.GetTempPath(), "AiUsage-BackendMeasurements-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var store = new LocalBudgetStore(directory);
            var observations = CreateObservations();
            foreach (var series in observations.GroupBy(x => x.Series))
            foreach (var batch in series.Chunk(1024))
                await store.AppendAsync(batch, token).ConfigureAwait(false);
            var keys = observations.Select(x => x.Series).Distinct().ToArray();
            List<ReadingRun> runs = [];
            foreach (var key in keys) runs.AddRange((await store.ReadAsync(key, token).ConfigureAwait(false)).Value);
            var all = runs.ToArray();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                StoredRuns = all.Length,
                StoredBytes = Directory.EnumerateFiles(Path.Combine(directory, "budget"), "*.json").Sum(p => new FileInfo(p).Length)
            }));
            await MeasureAsync("reading-store/read-eight-series", async () =>
            {
                long count = 0;
                foreach (var key in keys) count += (await store.ReadAsync(key, token).ConfigureAwait(false)).Value.Count;
                return count;
            }).ConfigureAwait(false);
            var duplicate = observations.GroupBy(x => x.Series).Take(2).Select(g => g.Last()).ToArray();
            await MeasureAsync("reading-store/duplicate-two-series", async () =>
            {
                await store.AppendAsync(duplicate, token).ConfigureAwait(false);
                return duplicate.Length;
            }).ConfigureAwait(false);
            int append = 0;
            await MeasureAsync("reading-store/append-eight-series", async () =>
            {
                var at = Now.AddMinutes(++append * 5);
                var next = keys.Select(k => new ReadingObservation(k, new CountQuantity(42 + append / 100m, "percent"), at)
                { PlanType = "synthetic", Source = SnapshotSource.ProviderApi }).ToArray();
                await store.AppendAsync(next, token).ConfigureAwait(false);
                return next.Length;
            }).ConfigureAwait(false);
            var period = new PeriodBounds(Start, Start.AddDays(35), ValueOrigin.Assumed, ValueOrigin.Assumed);
            await MeasureAsync("budget-history/eight-series", () =>
            {
                decimal total = 0;
                foreach (var key in keys)
                {
                    var tracked = ReadingCalculations.Track(all, key, period, Now, false, .01m);
                    var day = ReadingCalculations.DayStart(tracked.Runs, key, tracked.Runs[0].PeriodInstance, Now, TimeZoneInfo.Utc);
                    var facts = new LimitFacts(key.Limit, LimitKind.CountablePool, "percent", LimitValue.Finite(new CountQuantity(1000, "percent")));
                    var result = BudgetEngine.Calculate(new(facts, null, period, tracked.Used, day.Value, Now, TimeZoneInfo.Utc));
                    total += result.Used ?? throw new InvalidOperationException("Synthetic budget failed.");
                }
                return Task.FromResult((long)total);
            }).ConfigureAwait(false);
            await MeasureAsync("sessions/four-pairs", () =>
            {
                long count = 0;
                for (int i = 0; i < keys.Length; i += 2)
                {
                    var estimate = SessionEstimator.Estimate(all,
                        new(keys[i], keys[i + 1], "shared", "shared", TimeSpan.FromHours(5), TimeSpan.FromDays(7)), Now);
                    if (!estimate.Ready) throw new InvalidOperationException("Synthetic estimate is not ready.");
                    count += estimate.Windows;
                }
                return Task.FromResult(count);
            }).ConfigureAwait(false);
            await MeasureParsersAsync().ConfigureAwait(false);
            return 0;
        }
        finally
        {
            // This invocation creates and owns this unique temporary directory only.
            Directory.Delete(directory, recursive: true);
        }
#endif
    }

    private static ReadingObservation[] CreateObservations()
    {
        List<ReadingObservation> values = new(Accounts * 2 * ObservationsPerSeries);
        for (int account = 0; account < Accounts; account++)
        for (int limit = 0; limit < 2; limit++)
        {
            var key = new ReadingSeriesKey("synthetic-" + account.ToString(CultureInfo.InvariantCulture),
                new("synthetic", "shared", limit == 0 ? "five-hour" : "weekly"));
            int samplesPerPeriod = limit == 0 ? 60 : 2016;
            for (int sample = 0; sample < ObservationsPerSeries; sample++)
            {
                var at = Start.AddMinutes(sample * 5);
                var period = sample / samplesPerPeriod;
                values.Add(new(key, new CountQuantity(sample % samplesPerPeriod * 80m / samplesPerPeriod, "percent"), at)
                {
                    PlanType = "synthetic", Source = SnapshotSource.ProviderApi, RoundingUnit = .0001m,
                    ResetAt = Start.AddMinutes((period + 1) * samplesPerPeriod * 5),
                    PeriodStartedAt = Start.AddMinutes(period * samplesPerPeriod * 5)
                });
            }
        }
        return values.ToArray();
    }

    private static async Task MeasureParsersAsync()
    {
        byte[] codex = """{"plan_type":"synthetic","rate_limit":{"primary_window":{"used_percent":42,"limit_window_seconds":18000,"reset_at":1788580800},"secondary_window":{"used_percent":35,"limit_window_seconds":604800}}}"""u8.ToArray();
        byte[] claude = """{"five_hour":{"utilization":42,"resets_at":"2026-09-05T12:00:00Z"},"seven_day":{"utilization":35,"resets_at":"2026-09-10T12:00:00Z"}}"""u8.ToArray();
        byte[] copilot = """{"copilot_plan":"synthetic","quota_snapshots":{"premium_interactions":{"entitlement":300,"remaining":180,"percent_remaining":60},"chat":{"unlimited":true}}}"""u8.ToArray();
        byte[] antigravity = """{"buckets":[{"bucketId":"opaque/測試","window":"5h","remainingFraction":0.58},{"bucketId":"weekly","window":"weekly","remainingFraction":0.65}]}"""u8.ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new { ParserIterations = 1000, PayloadBytes = new[] { codex.Length, claude.Length, copilot.Length, antigravity.Length } }));
        await MeasureAsync("parsers/four-providers-times-1000", () =>
        {
            long count = 0;
            for (int i = 0; i < 1000; i++)
            {
                count += CodexQuotaParser.Parse(codex, Now).Groups.Count;
                if (!ClaudeQuotaParser.TryParse(claude, Now, out var reading)) throw new InvalidOperationException("Invalid synthetic Claude payload.");
                GC.KeepAlive(reading);
                count += CopilotQuotaParser.Parse(copilot, Now).Groups.Count;
                count += AntigravityQuotaParser.Parse(antigravity, Now).Groups.Count;
            }
            return Task.FromResult(count);
        }).ConfigureAwait(false);
    }

    private static async Task MeasureAsync(string name, Func<Task<long>> action)
    {
        var cold = await SampleAsync(action).ConfigureAwait(false);
        for (int i = 0; i < 3; i++) await action().ConfigureAwait(false);
        List<(double Ms, long Bytes, long Checksum)> samples = [];
        for (int i = 0; i < 9; i++) samples.Add(await SampleAsync(action).ConfigureAwait(false));
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Workload = name, ColdMs = cold.Ms, ColdAllocatedBytes = cold.Bytes,
            MedianMs = samples.Select(x => x.Ms).Order().ElementAt(4),
            MinMs = samples.Min(x => x.Ms), MaxMs = samples.Max(x => x.Ms),
            MedianAllocatedBytes = samples.Select(x => x.Bytes).Order().ElementAt(4),
            Checksum = samples[^1].Checksum
        }));
    }

    private static async Task<(double Ms, long Bytes, long Checksum)> SampleAsync(Func<Task<long>> action)
    {
        long allocated = GC.GetTotalAllocatedBytes(precise: true);
        long start = Stopwatch.GetTimestamp();
        long checksum = await action().ConfigureAwait(false);
        return (Stopwatch.GetElapsedTime(start).TotalMilliseconds, GC.GetTotalAllocatedBytes(precise: true) - allocated, checksum);
    }
}
