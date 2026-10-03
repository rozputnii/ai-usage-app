using AiUsage.Core.Budget;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Persistence;

/// <summary>Resolve one series without copying, merging or deleting history. New adapters
/// use the same resolution for reads and caps; an ambiguous key has no legacy alias.</summary>
public static class CompatibleReadingSeries
{
    public static async Task<ReadingSeriesKey> ResolveAsync(IReadingSeriesStore store, string accountTarget,
        LimitFacts facts, CancellationToken token)
    {
        var native = new ReadingSeriesKey(accountTarget, facts.Key);
        if (facts.LegacyKey is not { } alias || alias.Provider != facts.Key.Provider) return native;
        if ((await store.ReadAsync(native, token).ConfigureAwait(false)).Value.Count > 0) return native;
        var legacy = new ReadingSeriesKey(accountTarget, alias);
        var readings = (await store.ReadAsync(legacy, token).ConfigureAwait(false)).Value;
        var balance = facts.Key.Provider == "codex" && facts.Key.Family == "CX-B";
        return readings.Count > 0 && readings.All(r => r.IsBalance == balance && QuantityMath.IsValidFor(r.Value, facts))
            ? legacy : native;
    }
}
