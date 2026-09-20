using System.Runtime.CompilerServices;
using AiUsage.Features.Connection;
using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

internal sealed class LiveConnectionFlow(LiveUsageSource source, Action<Uri> openBrowser) : IConnectionFlow
{
    public bool ManualCodeUsesActiveConnection => true;
    public IReadOnlyList<ProviderDescriptor> Providers { get; } =
    [
        new("codex", "Codex", "›_", [ConnectionMethod.BrowserSignIn], CapabilityOrigin.Existing),
        new("claude", "Claude", "✳", [ConnectionMethod.BrowserSignIn, ConnectionMethod.ManualCode], CapabilityOrigin.Existing),
        new("copilot", "GitHub Copilot", "⊙", [ConnectionMethod.BrowserSignIn], CapabilityOrigin.Existing),
        new("antigravity", "Antigravity", "↑", [ConnectionMethod.BrowserSignIn], CapabilityOrigin.Existing)
    ];

    public bool TrySubmitCode(ConnectRequest request, string transientCode) =>
        request.ProviderId == "claude" && source.TrySubmitCode(request.ProviderId, transientCode);

    public async IAsyncEnumerable<ConnectionStage> ConnectAsync(ConnectRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Providers.Any(p => p.ProviderId == request.ProviderId && p.Methods.Contains(request.Method)) ||
            request.ReconnectAccountId is { } reconnect && reconnect != request.ProviderId)
        {
            yield return new(ConnectionStageKind.Failed);
            yield break;
        }
        var account = source.Current.Accounts.FirstOrDefault(a => a.Id == request.ProviderId);
        if (request.ReconnectAccountId is null && account?.Connection is ConnectionState.Connected or ConnectionState.ReauthRequired or ConnectionState.RecoveryRequired)
        {
            yield return new(ConnectionStageKind.ProviderSlotOccupied, request.ProviderId);
            yield break;
        }
        yield return new(ConnectionStageKind.Connecting);
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var opened = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = source.ConnectWithChallengeAsync(request.ProviderId, challenge =>
        {
            openBrowser(challenge.VerificationUri);
            opened.TrySetResult(challenge.UserCode);
        }, attempt.Token);
        try
        {
            if (await Task.WhenAny(opened.Task, pending).ConfigureAwait(false) == opened.Task)
                yield return new(ConnectionStageKind.WaitingForAuthorization) { DeviceUserCode = await opened.Task.ConfigureAwait(false) };
            var result = await pending.ConfigureAwait(false);
            account = source.Current.Accounts.FirstOrDefault(a => a.Id == request.ProviderId);
            yield return new(result.Status switch
            {
                CommandStatus.Cancelled => ConnectionStageKind.Cancelled,
                _ when result.Failure?.Kind == "AccessDenied" => ConnectionStageKind.Denied,
                _ when result.Failure?.Kind == "DeviceCodeExpired" => ConnectionStageKind.Expired,
                _ when request.ReconnectAccountId is null && account?.Connection == ConnectionState.Connected &&
                    (account.Failure is not null || account.FetchedAt is null) => ConnectionStageKind.ConnectedWithoutQuota,
                CommandStatus.Succeeded when account?.Connection == ConnectionState.Connected && account.FetchedAt is null => ConnectionStageKind.ConnectedWithoutQuota,
                CommandStatus.Succeeded when account?.Connection == ConnectionState.Connected => request.ReconnectAccountId is null ? ConnectionStageKind.Connected : ConnectionStageKind.Reconnected,
                _ => ConnectionStageKind.Failed
            }, request.ProviderId, result.Failure);
        }
        finally
        {
            await attempt.CancelAsync();
            await pending.ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<ConnectionStage> SubmitCodeAsync(ConnectRequest request, string transientCode,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new(TrySubmitCode(request, transientCode) ? ConnectionStageKind.Verifying : ConnectionStageKind.Failed);
        await Task.CompletedTask;
    }
}
