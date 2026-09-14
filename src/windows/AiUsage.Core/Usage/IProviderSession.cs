namespace AiUsage.Core.Usage;

/// <summary>Credential-free application port. Properties are memory-only; storage reads are explicit.</summary>
public interface IProviderSession
{
    bool HasStoredGrant { get; }
    ProviderSessionState State { get; }
    Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default);
    Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default);
    Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default);
    Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default);
    Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default);
    bool TrySubmitCode(string code) => false;
}
