using AiUsage.Core.Providers.Codex;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>
/// Adapts the existing Codex session without changing its stored formats or public protocol API.
/// Its synchronous legacy storage calls run off the dispatcher and never from UI properties.
/// </summary>
public sealed class CodexDashboardSession(ICodexSession session) : IProviderSession
{
    public bool HasStoredGrant { get; private set; }
    public ProviderSessionState State { get; private set; } = ProviderSessionState.NotConnected;
    public Task<ProviderSessionState> ReadCachedStateAsync(CancellationToken cancellationToken = default) =>
        RunAsync(_ => Task.FromResult(session.ReadCachedState()), cancellationToken);
    public Task<ProviderSessionState> ResumeAsync(CancellationToken cancellationToken = default) => RunAsync(session.ResumeAsync, cancellationToken);
    public Task<ProviderSessionState> ConnectAsync(Action<Uri> openAuthorizationUrl, CancellationToken cancellationToken = default) =>
        RunAsync(token => session.ConnectAsync(url =>
        {
            try { openAuthorizationUrl(url); }
            catch (InvalidOperationException) { throw new CodexException(CodexFailureKind.BrowserCallbackUnavailable); }
            catch (System.ComponentModel.Win32Exception) { throw new CodexException(CodexFailureKind.BrowserCallbackUnavailable); }
        }, token), cancellationToken);
    public Task<ProviderSessionState> RefreshAsync(CancellationToken cancellationToken = default) => RunAsync(session.RefreshAsync, cancellationToken);
    public Task<ProviderSessionState> DisconnectAsync(CancellationToken cancellationToken = default) => RunAsync(session.DisconnectAsync, cancellationToken);

    private Task<ProviderSessionState> RunAsync(Func<CancellationToken, Task<CodexSessionState>> operation, CancellationToken token) => Task.Run(async () =>
    {
        try { return State = Map(await operation(token).ConfigureAwait(false)); }
        catch (OperationCanceledException) { State = Map(session.State); throw; }
        catch (IOException) { return State = new(ProviderSessionStatus.RecoveryRequired, Failure: ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { return State = new(ProviderSessionStatus.RecoveryRequired, Failure: ProviderFailureKind.StorageUnavailable); }
        finally { HasStoredGrant = session.HasStoredGrant; }
    }, token);

    internal static ProviderSessionState Map(CodexSessionState state) => new(
        Enum.Parse<ProviderSessionStatus>(state.Status.ToString()), state.Quota,
        state.Failure is { } failure ? Enum.Parse<ProviderFailureKind>(failure.ToString()) : null,
        state.RetrievedAt, state.FromCache);
}
