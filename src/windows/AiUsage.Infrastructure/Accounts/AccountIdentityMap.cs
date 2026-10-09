using System.Text.Json.Serialization;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Accounts;

internal sealed record AccountIdentityEntry(string Provider, ProviderIdentity Identity, Guid AccountId)
{
    public override string ToString() => "AccountIdentityEntry (redacted)";
}

internal sealed record AccountIdentityState
{
    public int Version { get; init; } = 1;
    public Guid Revision { get; init; }
    public Guid? ParentRevision { get; init; }
    public IReadOnlyList<AccountIdentityEntry> Accounts { get; init; } = [];
    public override string ToString() => "AccountIdentityState (redacted)";
}

/// <summary>Exact identity-map file names, for owned-data deletion.</summary>
internal static class AccountIdentityFiles
{
    internal const string Name = "accounts.identities";
    internal static readonly string[] Owned = [Name, Name + ".pending"];
    internal static bool IsQuarantine(string name) =>
        name.StartsWith(Name + ".quarantine-", StringComparison.Ordinal) && Guid.TryParseExact(name[(Name.Length + 12)..], "N", out _);
}

/// <summary>
/// T-047: provider-verified identity to app account ID, kept beside the reading series in the
/// history root so a reinstalled package re-attaches history. Holds no credential or storage reference.
/// Failures never block sign-in or startup: an unreadable map is set aside and rebuilt from the registry.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class AccountIdentityMap(string historyDirectory, IDiagnosticSink? diagnostics = null)
{
    private const string FileName = AccountIdentityFiles.Name;
    private const int MaximumEntries = 256;
    private static readonly ProviderStatePolicy<AccountIdentityState> Policy = new(
        FileName, "AiUsage.AccountIdentities.v1"u8.ToArray(),
        AccountIdentityJson.Default.AccountIdentityState, s => s.Revision, s => s.ParentRevision,
        (s, parent) => s with { Revision = Guid.NewGuid(), ParentRevision = parent }, Validate);

    internal async Task<Guid?> FindAsync(string provider, ProviderIdentity identity, CancellationToken token)
    {
        await using var lease = await AcquireAsync(token).ConfigureAwait(false);
        if (lease is null) return null;
        var state = await LoadAsync(lease, token).ConfigureAwait(false);
        return state?.Accounts.FirstOrDefault(a => a.Provider == provider && a.Identity == identity)?.AccountId;
    }

    /// <summary>The given accounts win over older entries for the same identity or account ID.</summary>
    internal async Task UpsertAsync(IReadOnlyCollection<AccountRecord> accounts, CancellationToken token)
    {
        if (accounts.Count == 0) return;
        await using var lease = await AcquireAsync(token).ConfigureAwait(false);
        if (lease is null) return;
        var current = await LoadAsync(lease, token).ConfigureAwait(false);
        var entries = (current?.Accounts ?? []).Where(e => !accounts.Any(a =>
            a.Id == e.AccountId || a.Provider == e.Provider && a.Identity == e.Identity)).ToList();
        entries.AddRange(accounts.Select(a => new AccountIdentityEntry(a.Provider, a.Identity, a.Id)));
        if (entries.Count > MaximumEntries) entries.RemoveRange(0, entries.Count - MaximumEntries);
        var next = (current ?? new()) with { Accounts = entries };
        if (current is not null && next.Accounts.SequenceEqual(current.Accounts)) return;
        try { await lease.SaveAsync(next, current?.Revision, token).ConfigureAwait(false); }
        catch (ProviderException error)
        {
            // The lease records I/O, access and protection failures; a rejected generation is recorded here.
            if (error.Kind != ProviderFailureKind.StorageUnavailable) diagnostics?.Record(DiagnosticEvent.PersistenceFailure, DiagnosticCategory.InvalidData);
        }
    }

    private async Task<ProviderStateLease<AccountIdentityState>?> AcquireAsync(CancellationToken token)
    {
        try { return await Policy.AcquireAsync(historyDirectory, null, diagnostics, token).ConfigureAwait(false); }
        catch (ProviderException error)
        {
            // Lease I/O failures are recorded by the policy; an unsafe directory is not.
            if (error.Kind == ProviderFailureKind.RecoveryRequired) diagnostics?.Record(DiagnosticEvent.PersistenceFailure, DiagnosticCategory.InvalidData);
            return null;
        }
    }

    private async Task<AccountIdentityState?> LoadAsync(ProviderStateLease<AccountIdentityState> lease, CancellationToken token)
    {
        try { return await lease.LoadAsync(token).ConfigureAwait(false); }
        catch (ProviderException error) when (error.Kind == ProviderFailureKind.RecoveryRequired)
        {
            try
            {
                await Task.Run(SetAside, token).ConfigureAwait(false);
                diagnostics?.Record(DiagnosticEvent.BudgetStoreRecovered, DiagnosticCategory.InvalidData);
            }
            catch (Exception setAside) when (setAside is IOException or UnauthorizedAccessException or ProviderException)
            { diagnostics?.Failure(DiagnosticEvent.PersistenceFailure, setAside); }
            return null;
        }
        catch (ProviderException) { return null; }
    }

    private void SetAside()
    {
        foreach (var name in AccountIdentityFiles.Owned)
        {
            var path = Path.Combine(historyDirectory, name);
            ProviderStatePaths.CheckFile(path);
            if (File.Exists(path)) File.Move(path, Path.Combine(historyDirectory, FileName + ".quarantine-" + Guid.NewGuid().ToString("N")));
        }
    }

    private static void Validate(AccountIdentityState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision ||
            state.Accounts is null || state.Accounts.Count > MaximumEntries ||
            state.Accounts.Any(a => a is null || a.AccountId == Guid.Empty || !AccountRegistry.KnownProvider(a.Provider) ||
                a.Identity is null || a.Identity.Subject is not { Length: > 0 and <= 2048 } || a.Identity.Context is { Length: 0 or > 2048 }) ||
            state.Accounts.Select(a => a.AccountId).Distinct().Count() != state.Accounts.Count ||
            state.Accounts.Select(a => (a.Provider, a.Identity)).Distinct().Count() != state.Accounts.Count)
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
    }
}

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16)]
[JsonSerializable(typeof(AccountIdentityState))]
internal partial class AccountIdentityJson : JsonSerializerContext;
