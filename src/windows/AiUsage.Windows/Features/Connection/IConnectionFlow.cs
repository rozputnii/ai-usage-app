using AiUsage.Features.Presentation;

namespace AiUsage.Features.Connection;

public enum ConnectionMethod { BrowserSignIn, ManualCode }

public enum ConnectionStageKind
{
    Connecting,
    WaitingForAuthorization,
    Verifying,
    Connected,
    ConnectedWithoutQuota,
    Reconnected,
    Duplicate,
    Denied,
    Expired,
    Cancelled,
    Failed,
}

public sealed record ProviderDescriptor(string ProviderId, string Name, string Glyph, IReadOnlyList<ConnectionMethod> Methods, CapabilityOrigin Origin);

public sealed record ConnectRequest(string ProviderId, ConnectionMethod Method, string? ReconnectAccountId);

/// <summary><paramref name="AccountId"/> is the created, reconnected or already-connected account when one exists.</summary>
public sealed record ConnectionStage(ConnectionStageKind Kind, string? AccountId = null, FailureItem? Failure = null)
{
    public bool IsTerminal => Kind is not (ConnectionStageKind.Connecting or ConnectionStageKind.WaitingForAuthorization or ConnectionStageKind.Verifying);
}

/// <summary>
/// Adapter boundary for connect and reconnect. Stages stream until a terminal stage. A manually entered code is
/// transient: it is passed once and never stored, logged or included in snapshots.
/// </summary>
public interface IConnectionFlow
{
    IReadOnlyList<ProviderDescriptor> Providers { get; }

    IAsyncEnumerable<ConnectionStage> ConnectAsync(ConnectRequest request, CancellationToken cancellationToken);

    IAsyncEnumerable<ConnectionStage> SubmitCodeAsync(ConnectRequest request, string transientCode, CancellationToken cancellationToken);
}
