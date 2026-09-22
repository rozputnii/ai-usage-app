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
            if (loaded is null)
            {
                stored = null; HasStoredGrant = false;
                State = AiUsage.Core.Usage.ProviderSessionState.NotConnected;
                return ProviderHistoryResult.Unavailable(range, HistoryStatus.AuthenticationRequired);
            }
            HasStoredGrant = true;
            if (stored is not null && stored.AccountId != loaded.AccountId)
            {
                stored = loaded;
                State = new(AiUsage.Core.Usage.ProviderSessionStatus.QuotaUnavailable);
                return ProviderHistoryResult.Unavailable(range, HistoryStatus.AccountChanged);
            }
            stored = loaded;
            if (loaded.NeedsReauthentication || loaded.ExpiresAt <= clock.GetUtcNow())
            {
                State = Cached();
                return ProviderHistoryResult.Unavailable(range, HistoryStatus.AuthenticationRequired);
            }
            return await history.FetchAsync(new(loaded.AccessToken, loaded.AccountId, loaded.ExpiresAt), range, cancellationToken).ConfigureAwait(false);
        }
        catch (ProviderException error) { return ProviderHistoryResult.Unavailable(range, HistoryJson.Status(error.Kind)); }
        finally { gate.Release(); }
    }, CancellationToken.None);
}
