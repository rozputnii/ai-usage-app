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
        return (await store.ReadAsync(legacy, token).ConfigureAwait(false)).Value.Count > 0 ? legacy : native;
    }
}
