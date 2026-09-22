using AiUsage.Core.History;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

public sealed partial class CodexSession : IProviderHistorySession
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
                credentials?.Dispose(); credentials = null; stored = null; HasStoredGrant = false;
                State = ProviderSessionState.NotConnected;
                return ProviderHistoryResult.Unavailable(range, HistoryStatus.AuthenticationRequired);
            }
            if (stored is not null && stored.AccountId != loaded.AccountId)
            {
                credentials?.Dispose(); credentials = null; stored = loaded;
                State = new(ProviderSessionStatus.QuotaUnavailable);
                return ProviderHistoryResult.Unavailable(range, HistoryStatus.AccountChanged);
            }
            if (stored is null || CodexGrantStore.Revision(loaded) != CodexGrantStore.Revision(stored))
            { credentials?.Dispose(); credentials = null; }
            stored = loaded; HasStoredGrant = true;
            if (credentials is null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Once a rotating exchange starts, finish its bounded transport and durable save.
                // Navigation cancellation applies again before any history GET is sent.
                Adopt(await auth.ResumeAsync(new(stored.AccountId!, stored.RefreshToken!), CancellationToken.None).ConfigureAwait(false));
                await PersistAsync(lease).ConfigureAwait(false);
            }
            else if (credentials.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await auth.RefreshAsync(credentials, CancellationToken.None).ConfigureAwait(false);
                await PersistAsync(lease).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            return await history.FetchAsync(credentials!, range, cancellationToken).ConfigureAwait(false);
        }
        catch (CodexException error)
        {
            if (error.Kind == ProviderFailureKind.AuthenticationRequired)
            { credentials?.Dispose(); credentials = null; State = Stale(ProviderSessionStatus.ReauthenticationRequired, error.Kind); }
            return ProviderHistoryResult.Unavailable(range, HistoryJson.Status(error.Kind));
        }
        catch (ProviderException error) { return ProviderHistoryResult.Unavailable(range, HistoryJson.Status(error.Kind)); }
        catch (IOException) { return ProviderHistoryResult.Unavailable(range, HistoryStatus.Failed); }
        catch (UnauthorizedAccessException) { return ProviderHistoryResult.Unavailable(range, HistoryStatus.Failed); }
        finally { gate.Release(); }
    }, CancellationToken.None);
}
