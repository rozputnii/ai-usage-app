using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

/// <summary>Deterministic in-memory <see cref="IUsageSource"/>. No network, credential or storage access.</summary>
internal sealed class DemoUsageSource(DemoState state) : IUsageSource
{
    public UiSnapshot Current => state.Current;

    public IDisposable Subscribe(Action<UiSnapshot> onSnapshot) => state.Subscribe(onSnapshot);

    public async Task<UiCommandResult> ExecuteAsync(UiCommand command, CancellationToken cancellationToken)
    {
        var world = state.World;
        var account = state.Account(command.TargetId);
        switch (command.Kind)
        {
            case UiCommandKind.RefreshAccount:
                return account is null ? UiCommandResult.Conflict : await RefreshAsync(account, cancellationToken);
            case UiCommandKind.RefreshAll:
                return await RefreshAllAsync(cancellationToken);
            case UiCommandKind.CancelOperation:
                if (account?.OperationCancellation is null)
                    return UiCommandResult.Conflict;
                account.OperationCancellation.Cancel();
                return UiCommandResult.Succeeded;
            case UiCommandKind.Disconnect:
                return account is null ? UiCommandResult.Conflict : await DisconnectAsync(account, cancellationToken);
            case UiCommandKind.Rename when command.Payload is RenamePayload rename:
                var label = rename.Label.Trim();
                if (account is null || label.Length is 0 or > 80)
                    return UiCommandResult.Failed();
                account.Label = label;
                break;
            case UiCommandKind.Reorder when command.Payload is ReorderPayload reorder:
                var ids = reorder.Order.Distinct().ToList();
                if (ids.Count != world.Order.Count || ids.Except(world.Order).Any())
                    return UiCommandResult.Conflict;
                world.Order.Clear();
                world.Order.AddRange(ids);
                break;
            case UiCommandKind.SetVisibility when command.Payload is VisibilityPayload visibility && command.TargetId is { } target:
                if (visibility.Hidden)
                {
                    world.Hidden.Add(target);
                    if (visibility.MuteAlerts)
                        world.Muted.Add(target);
                }
                else
                    world.Hidden.Remove(target);
                break;
            case UiCommandKind.SetMute when command.Payload is MutePayload mute && command.TargetId is { } muteTarget:
                if (mute.Muted) world.Muted.Add(muteTarget);
                else world.Muted.Remove(muteTarget);
                break;
            case UiCommandKind.SetExpansion when command.Payload is ExpansionPayload expansion:
                var group = world.Accounts.SelectMany(a => a.Contexts).SelectMany(c => c.Groups).FirstOrDefault(g => g.Id == command.TargetId);
                if (group is null)
                    return UiCommandResult.Conflict;
                group.Expansion = expansion.Expansion;
                break;
            case UiCommandKind.SelectContext when command.Payload is ContextSelectionPayload selection:
                if (account is null || account.Contexts.All(c => c.Id != selection.ContextId))
                    return UiCommandResult.Conflict;
                world.ContextSelection[account.Id] = selection.ContextId;
                break;
            default:
                return UiCommandResult.Unsupported;
        }
        state.Publish();
        return UiCommandResult.Succeeded;
    }

    private async Task<UiCommandResult> RefreshAsync(DemoAccount account, CancellationToken cancellationToken)
    {
        var world = state.World;
        if (account.Operation != AccountOperation.Idle || account.Connection is ConnectionState.NotConnected or ConnectionState.Connecting)
            return UiCommandResult.Conflict;
        if (world.Compatibility == CompatibilityState.SecurityBlocked)
            return UiCommandResult.Unsupported;
        if (account.Connection == ConnectionState.ReauthRequired)
            return UiCommandResult.Failed(account.Failure);
        if (account.Failure is { Kind: FailureKinds.RateLimited, RetryAt: { } retryAt } && retryAt > state.Clock.UtcNow)
            return UiCommandResult.Failed(account.Failure);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        account.OperationCancellation = cancellation;
        account.Operation = AccountOperation.Refreshing;
        state.Publish();
        try
        {
            await state.DelayAsync(DemoLatency.Refresh, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            if (state.World == world)
            {
                account.Operation = AccountOperation.Idle;
                account.OperationCancellation = null;
                state.Publish();
            }
            return UiCommandResult.Cancelled;
        }
        account.OperationCancellation = null;
        account.Operation = AccountOperation.Idle;

        var outcome = world.ForcedRefreshOutcome;
        if (outcome == DemoRefreshOutcome.Scenario && account.FailNext > 0)
        {
            account.FailNext--;
            outcome = DemoRefreshOutcome.NetworkFailure;
        }
        switch (outcome)
        {
            case DemoRefreshOutcome.NetworkFailure:
                account.Failure = new(FailureKinds.NetworkFailure, "Failure_NetworkFailure", null, true);
                if (account.Freshness == Freshness.Fresh)
                    account.Freshness = Freshness.Cached;
                state.Publish();
                return UiCommandResult.Failed(account.Failure);
            case DemoRefreshOutcome.RateLimited:
                account.Failure = new(FailureKinds.RateLimited, "Failure_RateLimited", state.Clock.UtcNow + TimeSpan.FromMinutes(2), true);
                state.Publish();
                return UiCommandResult.Failed(account.Failure);
            case DemoRefreshOutcome.InvalidGrant:
                account.Connection = ConnectionState.ReauthRequired;
                account.Freshness = Freshness.Stale;
                account.Failure = new(FailureKinds.InvalidGrant, "Failure_InvalidGrant", null, true);
                state.Publish();
                return UiCommandResult.Failed(account.Failure);
        }

        var now = state.Clock.UtcNow;
        foreach (var window in account.AllWindows)
            Observe(window, now);
        account.Freshness = Freshness.Fresh;
        account.FetchedAt = now;
        account.Failure = null;
        account.ObservationRevision++;
        world.HistoryRows += account.AllWindows.Count();
        state.Publish();
        return UiCommandResult.Succeeded;
    }

    /// <summary>A new observation. A passed reset becomes 100 % only here, never from the countdown alone (D-118).</summary>
    private static void Observe(DemoWindow window, DateTimeOffset now)
    {
        if (window.NextObservation is { } next)
        {
            Set(window, next);
            window.NextObservation = null;
        }
        else if (window.State == ValueState.Exhausted && window.ResetsAt is { } reset && reset <= now)
        {
            Set(window, 100);
            window.ResetsAt = now + TimeSpan.FromSeconds(window.DurationSeconds ?? 18000);
        }
        else if (window.State == ValueState.Known && window.Remaining is { } value && value > 0)
            Set(window, Math.Max(0, value - 3));
        else if (window.State == ValueState.Unknown && window.ProviderRemaining is { } restored && window.ProviderState is ValueState.Known or ValueState.Exhausted)
            Set(window, restored);
    }

    private static void Set(DemoWindow window, double remaining)
    {
        window.Remaining = remaining;
        window.ProviderRemaining = remaining;
        window.State = remaining <= 0 ? ValueState.Exhausted : ValueState.Known;
        window.ProviderState = window.State;
    }

    private async Task<UiCommandResult> RefreshAllAsync(CancellationToken cancellationToken)
    {
        var world = state.World;
        if (world.Compatibility != CompatibilityState.Normal && !world.CompatibilityOverridden)
            return UiCommandResult.Unsupported;
        var snapshot = state.Current;
        var eligible = QuotaRules.Ordered(snapshot)
            .Where(a => QuotaRules.IsVisible(a, snapshot.Preferences) && a.Operation == AccountOperation.Idle && a.Connection == ConnectionState.Connected)
            .Select(a => state.Account(a.Id)!)
            .ToArray();
        if (eligible.Length == 0)
            return UiCommandResult.Conflict;
        world.LastRefreshAll = null;
        var results = await Task.WhenAll(eligible.Select(account => RefreshAsync(account, cancellationToken)));
        if (state.World != world)
            return UiCommandResult.Cancelled;
        var failed = eligible.Where((account, index) => results[index].Status == CommandStatus.Failed).Select(account => account.Id).ToArray();
        var updated = results.Count(result => result.Status == CommandStatus.Succeeded);
        if (results.All(result => result.Status == CommandStatus.Cancelled))
            return UiCommandResult.Cancelled;
        world.LastRefreshAll = new RefreshAllSummary(updated, failed, state.Clock.UtcNow);
        state.Publish();
        return failed.Length == 0 ? UiCommandResult.Succeeded : UiCommandResult.Failed();
    }

    private async Task<UiCommandResult> DisconnectAsync(DemoAccount account, CancellationToken cancellationToken)
    {
        if (account.Operation != AccountOperation.Idle || account.Connection == ConnectionState.NotConnected)
            return UiCommandResult.Conflict;
        var world = state.World;
        account.Operation = AccountOperation.Disconnecting;
        state.Publish();
        try
        {
            await state.DelayAsync(DemoLatency.Disconnect, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (state.World == world)
            {
                account.Operation = AccountOperation.Idle;
                state.Publish();
            }
            return UiCommandResult.Cancelled;
        }
        account.Operation = AccountOperation.Idle;
        account.Connection = ConnectionState.NotConnected;
        account.Failure = null;
        state.Publish();
        return UiCommandResult.Succeeded;
    }
}
