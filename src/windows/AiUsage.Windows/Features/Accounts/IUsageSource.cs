using AiUsage.Features.Presentation;

namespace AiUsage.Features.Accounts;

/// <summary>
/// Adapter boundary for account, quota, ordering, visibility and refresh state. A live adapter publishes snapshots
/// from existing workflows; the demo adapter uses synthetic in-memory state. Subscribers are called on any thread and
/// marshal through <see cref="IUiDispatcher"/>. Unsupported commands return <see cref="CommandStatus.Unsupported"/>
/// before any side effect.
/// </summary>
public interface IUsageSource
{
    UiSnapshot Current { get; }

    IDisposable Subscribe(Action<UiSnapshot> onSnapshot);

    /// <summary>
    /// Kinds: RefreshAccount, RefreshAll, CancelOperation, Disconnect, Rename (<see cref="RenamePayload"/>),
    /// Reorder (<see cref="ReorderPayload"/>), SetVisibility (<see cref="VisibilityPayload"/>), SetMute
    /// (<see cref="MutePayload"/>), SetExpansion (<see cref="ExpansionPayload"/>), SelectContext
    /// (<see cref="ContextSelectionPayload"/>). Cancellation returns Cancelled and restores the prior stable state.
    /// </summary>
    Task<UiCommandResult> ExecuteAsync(UiCommand command, CancellationToken cancellationToken);
}
