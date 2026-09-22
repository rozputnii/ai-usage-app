using AiUsage.Core.History;

namespace AiUsage.Infrastructure.Providers.Copilot;

public sealed partial class CopilotSession : IProviderHistorySession
{
    public Task<ProviderHistoryResult> GetHistoryAsync(HistoryRange range, CancellationToken cancellationToken) => Task.Run(async () =>
    {
        range.Validate();
        ObjectDisposedException.ThrowIf(disposed, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var lease = await store.AcquireAsync(cancellationToken).ConfigureAwait(false);
            var loaded = await lease.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (loaded is null || loaded.NeedsReauthentication || loaded.ExpiresAt <= clock.GetUtcNow() ||
                stored is not null && stored.AccountId != loaded.AccountId)
                return ProviderHistoryResult.Unavailable(range, HistoryStatus.AuthenticationRequired);
            return await history.FetchAsync(new(loaded.AccessToken, loaded.AccountId, loaded.ExpiresAt), range, cancellationToken).ConfigureAwait(false);
        }
        catch (ProviderException error) { return ProviderHistoryResult.Unavailable(range, HistoryJson.Status(error.Kind)); }
        finally { gate.Release(); }
    }, CancellationToken.None);
}
