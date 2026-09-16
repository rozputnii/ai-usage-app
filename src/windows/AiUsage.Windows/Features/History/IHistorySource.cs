using AiUsage.Features.Presentation;

namespace AiUsage.Features.History;

/// <summary>Adapter boundary for recorded history. Read-only; filter changes never mutate state.</summary>
public interface IHistorySource
{
    Task<HistoryResult> QueryHistoryAsync(HistoryQuery query, CancellationToken cancellationToken);
}
