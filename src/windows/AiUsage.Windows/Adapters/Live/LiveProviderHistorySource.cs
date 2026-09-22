using AiUsage.Features.History;
using Domain = AiUsage.Core.History;

namespace AiUsage.Adapters.Live;

internal sealed class LiveProviderHistorySource(LiveUsageSource usage) : IProviderHistorySource
{
    public async Task<ProviderHistoryResult> GetHistoryAsync(string accountId, HistoryRange range, CancellationToken cancellationToken)
    {
        var result = await usage.GetHistoryAsync(accountId, new(range.From, range.To), cancellationToken).ConfigureAwait(false);
        return new(range, result.FetchedAt, result.Reports.Select(r => new HistoryReport(r.Id, r.Status switch
        {
            Domain.HistoryStatus.Available => HistoryStatus.Available, Domain.HistoryStatus.Empty => HistoryStatus.Empty,
            Domain.HistoryStatus.Unsupported => HistoryStatus.Unsupported, Domain.HistoryStatus.AccessDenied => HistoryStatus.AccessDenied,
            Domain.HistoryStatus.AuthenticationRequired => HistoryStatus.AuthenticationRequired, Domain.HistoryStatus.RateLimited => HistoryStatus.RateLimited,
            Domain.HistoryStatus.Busy => HistoryStatus.Busy, _ => HistoryStatus.Failed
        }, r.Values.Select(v => new HistoryValue(v.From, v.To, v.Metric, v.Value, v.Unit, v.Dimensions)).ToArray(), r.RetryAt)).ToArray());
    }
}
