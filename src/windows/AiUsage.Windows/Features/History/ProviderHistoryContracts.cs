namespace AiUsage.Features.History;

// Presentation data only. The live adapter maps domain reports; demo needs no backend types.
public sealed record HistoryRange(DateOnly From, DateOnly To);
public enum HistoryStatus { Available, Empty, Unsupported, AccessDenied, AuthenticationRequired, RateLimited, Failed, Busy, AccountChanged }
public sealed record HistoryValue(DateOnly From, DateOnly To, string Metric, decimal? Value, string? Unit, IReadOnlyDictionary<string, string> Dimensions);
public sealed record HistoryReport(string Id, HistoryStatus Status, IReadOnlyList<HistoryValue> Values, DateTimeOffset? RetryAt = null);
public sealed record ProviderHistoryResult(HistoryRange Range, DateTimeOffset FetchedAt, IReadOnlyList<HistoryReport> Reports)
{
    public static ProviderHistoryResult Unavailable(HistoryRange range, HistoryStatus status) =>
        new(range, DateTimeOffset.UtcNow, [new("history", status, [])]);
}
public interface IProviderHistorySource
{
    Task<ProviderHistoryResult> GetHistoryAsync(string accountId, HistoryRange range, CancellationToken cancellationToken);
}
