namespace AiUsage.Core.History;

/// <summary>Inclusive UTC calendar dates. Request bounds are not a retention guarantee.</summary>
public sealed record HistoryRange(DateOnly From, DateOnly To)
{
    public void Validate()
    {
        if (From > To || To.DayNumber - From.DayNumber > 730)
            throw new ArgumentOutOfRangeException(nameof(From), "History requires an ordered range of at most 731 days.");
    }
}

public enum HistoryStatus { Available, Empty, Unsupported, AccessDenied, AuthenticationRequired, RateLimited, Failed, Busy, AccountChanged }

/// <summary>A native value over its actual reporting period, never an inferred observation.</summary>
public sealed record HistoryValue(DateOnly From, DateOnly To, string Metric, decimal? Value,
    string? Unit, IReadOnlyDictionary<string, string> Dimensions);

public sealed record HistoryReport(string Id, HistoryStatus Status, IReadOnlyList<HistoryValue> Values,
    DateTimeOffset? RetryAt = null);

public sealed record ProviderHistoryResult(HistoryRange Range, DateTimeOffset FetchedAt, IReadOnlyList<HistoryReport> Reports)
{
    public static ProviderHistoryResult Unavailable(HistoryRange range, HistoryStatus status) =>
        new(range, DateTimeOffset.UtcNow, [new("history", status, [])]);
}

/// <summary>Optional provider capability. Credentials remain behind the provider's session gate.</summary>
public interface IProviderHistorySession
{
    Task<ProviderHistoryResult> GetHistoryAsync(HistoryRange range, CancellationToken cancellationToken);
}

/// <summary>Application account routing and lifetime, with no grant or provider transport details.</summary>
public interface IProviderHistorySource
{
    Task<ProviderHistoryResult> GetHistoryAsync(string accountId, HistoryRange range, CancellationToken cancellationToken);
}
