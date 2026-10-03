using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Accounts;

/// <summary>
/// Runs under startup's exclusive root lease after publishing the forward-only layout.
/// Adopts existing grants in place: no copy, rollback or cross-file credential cutover.
/// Provider-keyed budget and preference data remains untouched and unassigned.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class AccountMigration(AccountRegistry registry, ProviderSessionFactory factory, Action<string>? boundary = null)
{
    internal async Task RunAsync(CancellationToken token)
    {
        var state = await registry.ReadAsync(token).ConfigureAwait(false);
        if (state.LegacyMigrationComplete) return;
        foreach (var provider in new[] { "claude", "codex", "copilot", "antigravity" })
        {
            if (state.Accounts.Any(a => a.Provider == provider && a.LegacyStorage)) continue;
            var id = Guid.NewGuid();
            var session = factory.CreateLegacy(provider, id);
            try
            {
                var cached = await session.ReadCachedStateAsync(token).ConfigureAwait(false);
                if (cached.Status == ProviderSessionStatus.RecoveryRequired)
                    throw new ProviderException(cached.Failure ?? ProviderFailureKind.RecoveryRequired);
                if (!session.HasStoredGrant) continue;
                var identity = ProviderSessionFactory.IdentityOf(session) ?? throw new ProviderException(ProviderFailureKind.RecoveryRequired);
                var record = new AccountRecord(id, provider, id, identity) { LegacyStorage = true };
                // A crash after this write resumes from this exact reference; it does not clone the grant.
                state = await registry.UpdateAsync(s => s with { Accounts = [.. s.Accounts, record] }, token).ConfigureAwait(false);
                boundary?.Invoke("account-registered");
            }
            finally { ((IDisposable)session).Dispose(); }
        }
        await registry.UpdateAsync(s => s with { LegacyMigrationComplete = true }, token).ConfigureAwait(false);
        boundary?.Invoke("migration-complete");
    }
}
