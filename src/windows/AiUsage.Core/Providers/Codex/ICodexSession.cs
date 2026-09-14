namespace AiUsage.Core.Providers.Codex;

/// <summary>Credential-free product port. Infrastructure owns the grant and all provider traffic.</summary>
public interface ICodexSession
{
    bool HasStoredGrant { get; }
    CodexSessionState State { get; }
    CodexSessionState ReadCachedState();
    Task<CodexSessionState> ResumeAsync(CancellationToken cancellationToken = default);
    Task<CodexSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default);
    Task<CodexSessionState> RefreshAsync(CancellationToken cancellationToken = default);
    Task<CodexSessionState> DisconnectAsync(CancellationToken cancellationToken = default);
}
