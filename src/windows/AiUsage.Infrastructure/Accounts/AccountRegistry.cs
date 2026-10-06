using System.Text.Json.Serialization;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Accounts;

internal sealed record AccountRecord(Guid Id, string Provider, Guid StorageId, ProviderIdentity Identity, bool Connected = true)
{
    public bool Disconnecting { get; init; }
    public bool LegacyStorage { get; init; }
    public override string ToString() => "AccountRecord (redacted)";
}

/// <summary>Owned candidate or superseded grant, cleaned only after it is no longer referenced.</summary>
internal sealed record PendingAccountStorage(string Provider, Guid StorageId, bool LegacyStorage = false);

internal sealed record AccountRegistryState
{
    public int Version { get; init; } = 1;
    public Guid Revision { get; init; }
    public Guid? ParentRevision { get; init; }
    public IReadOnlyList<AccountRecord> Accounts { get; init; } = [];
    public IReadOnlyList<PendingAccountStorage> Pending { get; init; } = [];
    public bool LegacyMigrationComplete { get; init; }
    public override string ToString() => "AccountRegistryState (redacted)";
}

/// <summary>Identity metadata uses the existing protected, revision-checked replacement protocol.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class AccountRegistry(string ownedDirectory, IDiagnosticSink? diagnostics = null, Action? afterStage = null)
{
    private static readonly ProviderStatePolicy<AccountRegistryState> Policy = new(
        "accounts.state", "AiUsage.AccountRegistry.v1"u8.ToArray(),
        AccountRegistryJson.Default.AccountRegistryState, s => s.Revision, s => s.ParentRevision,
        (s, parent) => s with { Revision = Guid.NewGuid(), ParentRevision = parent }, Validate);

    internal async Task<AccountRegistryState> ReadAsync(CancellationToken token)
    {
        await using var lease = await Policy.AcquireAsync(ownedDirectory, afterStage, diagnostics, token).ConfigureAwait(false);
        return await lease.LoadAsync(token).ConfigureAwait(false) ?? new();
    }

    internal async Task<AccountRegistryState> UpdateAsync(Func<AccountRegistryState, AccountRegistryState> update, CancellationToken token)
    {
        await using var lease = await Policy.AcquireAsync(ownedDirectory, afterStage, diagnostics, token).ConfigureAwait(false);
        var current = await lease.LoadAsync(token).ConfigureAwait(false);
        return await lease.SaveAsync(update(current ?? new()), current?.Revision, token).ConfigureAwait(false);
    }

    internal static bool KnownProvider(string? provider) => provider is "claude" or "codex" or "copilot" or "antigravity";

    private static void Validate(AccountRegistryState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision ||
            state.Accounts is null || state.Accounts.Count > 256 || state.Pending is null || state.Pending.Count > 256 ||
            state.Accounts.Any(a => a is null || a.Id == Guid.Empty || a.StorageId == Guid.Empty || !KnownProvider(a.Provider) ||
                a.Identity is null || a.Identity.Subject is not { Length: > 0 and <= 2048 } ||
                a.Identity.Context is { Length: 0 or > 2048 } || a.Disconnecting && !a.Connected) ||
            state.Pending.Any(p => p is null || p.StorageId == Guid.Empty || !KnownProvider(p.Provider)))
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        if (state.Accounts.Select(a => a.Id).Distinct().Count() != state.Accounts.Count ||
            state.Accounts.Select(a => (a.Provider, a.Identity)).Distinct().Count() != state.Accounts.Count ||
            state.Accounts.Where(a => a.LegacyStorage).Select(a => a.Provider)
                .Concat(state.Pending.Where(p => p.LegacyStorage).Select(p => p.Provider)).GroupBy(p => p).Any(g => g.Count() > 1) ||
            state.Accounts.Select(a => a.StorageId).Concat(state.Pending.Select(p => p.StorageId)).Distinct().Count() !=
                state.Accounts.Count + state.Pending.Count)
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
    }
}

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16)]
[JsonSerializable(typeof(AccountRegistryState))]
internal partial class AccountRegistryJson : JsonSerializerContext;
