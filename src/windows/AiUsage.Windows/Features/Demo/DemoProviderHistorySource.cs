using AiUsage.Features.History;
using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

/// <summary>Explicit synthetic remote-report fixture; it never contacts a provider or stores samples.</summary>
internal sealed class DemoProviderHistorySource(IClock clock) : IProviderHistorySource
{
    public async Task<ProviderHistoryResult> GetHistoryAsync(string accountId, HistoryRange range, CancellationToken cancellationToken)
    {
        await clock.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        if (!accountId.Contains("codex", StringComparison.Ordinal) && !accountId.Contains("copilot", StringComparison.Ordinal))
            return ProviderHistoryResult.Unavailable(range, HistoryStatus.Unsupported);
        var rows = Enumerable.Range(0, Math.Min(30, range.To.DayNumber - range.From.DayNumber + 1))
            .Select(i => new HistoryValue(range.To.AddDays(-i), range.To.AddDays(-i), "text_total_tokens", 12000 + i * 321, "tokens",
                new Dictionary<string, string> { ["model"] = "Synthetic model", ["surface"] = "Demo" })).ToArray();
        return new(range, clock.UtcNow, [new("usage", HistoryStatus.Available, rows)]);
    }
}
