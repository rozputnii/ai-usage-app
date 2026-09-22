using System.Runtime.CompilerServices;
using AiUsage.Features.Connection;
using AiUsage.Features.Presentation;

namespace AiUsage.Features.Demo;

public enum DemoConnectOutcome { Approve, ApproveWithoutQuota, Deny, Expire, Duplicate }

/// <summary>
/// Simulated connection flow. The waiting stage resolves only when the demo simulator chooses a browser outcome or
/// the user cancels; no browser, credential or provider is contacted.
/// </summary>
internal sealed class DemoConnectionFlow(DemoState state, ProviderCatalog? providers = null) : IConnectionFlow
{
    private TaskCompletionSource<DemoConnectOutcome>? pending;

    public IReadOnlyList<ProviderDescriptor> Providers { get; } = (providers ?? ProviderCatalog.Default).Demo;

    public bool IsWaiting => pending is not null;

    /// <summary>Demo-only: completes the simulated browser authorization.</summary>
    public bool Resolve(DemoConnectOutcome outcome) => pending?.TrySetResult(outcome) == true;

    public async IAsyncEnumerable<ConnectionStage> ConnectAsync(ConnectRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (state.World.Compatibility == CompatibilityState.SecurityBlocked)
        {
            yield return new(ConnectionStageKind.Failed, Failure: new("SecurityBlocked", "Failure_ConnectionsBlocked", null, false));
            yield break;
        }
        yield return new(ConnectionStageKind.Connecting);
        var cancelled = false;
        try { await state.DelayAsync(DemoLatency.Connect, cancellationToken); }
        catch (OperationCanceledException) { cancelled = true; }
        if (cancelled)
        {
            yield return new(ConnectionStageKind.Cancelled);
            yield break;
        }
        yield return new(ConnectionStageKind.WaitingForAuthorization);

        var completion = new TaskCompletionSource<DemoConnectOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending = completion;
        DemoConnectOutcome outcome = default;
        try
        {
            using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
                outcome = await completion.Task;
        }
        catch (OperationCanceledException) { cancelled = true; }
        finally { pending = null; }
        if (cancelled)
        {
            yield return new(ConnectionStageKind.Cancelled);
            yield break;
        }
        yield return Complete(request, outcome);
    }

    public async IAsyncEnumerable<ConnectionStage> SubmitCodeAsync(ConnectRequest request, string transientCode, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // The code is validated for shape by the view model and deliberately not retained here.
        _ = transientCode.Length;
        yield return new(ConnectionStageKind.Verifying);
        var cancelled = false;
        try { await state.DelayAsync(DemoLatency.Connect, cancellationToken); }
        catch (OperationCanceledException) { cancelled = true; }
        if (cancelled)
        {
            yield return new(ConnectionStageKind.Cancelled);
            yield break;
        }
        yield return Complete(request, DemoConnectOutcome.Approve);
    }

    private ConnectionStage Complete(ConnectRequest request, DemoConnectOutcome outcome)
    {
        var world = state.World;
        var provider = Providers.First(p => p.ProviderId == request.ProviderId);
        switch (outcome)
        {
            case DemoConnectOutcome.Deny:
                return new(ConnectionStageKind.Denied);
            case DemoConnectOutcome.Expire:
                return new(ConnectionStageKind.Expired);
            case DemoConnectOutcome.Duplicate:
                var existing = world.Accounts.FirstOrDefault(a => a.ProviderId == request.ProviderId) ?? world.Accounts.FirstOrDefault();
                return new(ConnectionStageKind.Duplicate, existing?.Id);
        }

        var now = state.Clock.UtcNow;
        if (request.ReconnectAccountId is { } reconnectId && state.Account(reconnectId) is { } reconnect)
        {
            reconnect.Connection = ConnectionState.Connected;
            reconnect.Failure = null;
            reconnect.Freshness = Freshness.Fresh;
            reconnect.FetchedAt = now;
            reconnect.ObservationRevision++;
            state.Publish();
            return new(ConnectionStageKind.Reconnected, reconnect.Id);
        }

        var number = world.Accounts.Count(a => a.ProviderId == request.ProviderId) + 1;
        var id = $"demo-{request.ProviderId}-new{number}";
        while (state.Account(id) is not null)
            id = $"demo-{request.ProviderId}-new{++number}";
        var label = $"{provider.Name} account {number}";
        DemoAccount account;
        if (outcome == DemoConnectOutcome.ApproveWithoutQuota)
        {
            account = new(id, request.ProviderId, label,
                [new(id + "-ctx", "Account", ContextKind.Account, [new(id + "-g", "Usage", [new(id + "-w", "Remaining", null, ValueState.Unavailable)])])])
            { Plan = null };
        }
        else
        {
            account = new(id, request.ProviderId, label,
                [new(id + "-ctx", "Account", ContextKind.Account, [new(id + "-g", "Usage limits",
                [
                    new(id + "-w1", "Session window", 100, ValueState.Known) { ResetsAt = now + TimeSpan.FromHours(5) },
                    new(id + "-w2", "Weekly window", 100, ValueState.Known) { ResetsAt = now + TimeSpan.FromDays(7), DurationSeconds = 604800 },
                ])])]);
        }
        account.FetchedAt = now;
        world.Accounts.Add(account);
        world.Order.Add(account.Id);
        state.Publish();
        return new(outcome == DemoConnectOutcome.ApproveWithoutQuota ? ConnectionStageKind.ConnectedWithoutQuota : ConnectionStageKind.Connected, account.Id);
    }
}
