using AiUsage.Core.Usage;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>One stored Codex grant: its opaque workspace and refresh token.</summary>
internal sealed record CodexStoredGrant(string AccountId, string RefreshToken)
{
    public override string ToString() => "CodexStoredGrant (redacted)";
}

/// <summary>App-owned DPAPI CurrentUser grant using the shared exclusive, recoverable state lease.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class CodexGrantStore
{
    private readonly Action? afterStage;
    private static readonly ProviderStatePolicy<StoredRecord> Policy = new(
        "codex.grant", "AiUsage.Codex.Grant.v1"u8.ToArray(), CodexGrantJson.Default.StoredRecord,
        Revision, _ => null, (state, _) => state, Validate, MaximumBytes: 64 * 1024, SeparateJournal: true);

    public CodexGrantStore(string ownedDirectory) : this(ownedDirectory, null) { }
    internal CodexGrantStore(string ownedDirectory, Action? afterStage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedDirectory);
        Directory = Path.GetFullPath(ownedDirectory);
        this.afterStage = afterStage;
    }

    public string Directory { get; }
    internal Task<ProviderStateLease<StoredRecord>> AcquireAsync(CancellationToken cancellationToken) =>
        Policy.AcquireAsync(Directory, afterStage, cancellationToken);

    // Legacy synchronous entry points remain until the separate T-03 contract cleanup.
    public CodexStoredGrant? Read() => ReadAsync().GetAwaiter().GetResult();
    public void Write(CodexStoredGrant grant) => WriteAsync(grant).GetAwaiter().GetResult();
    public void Delete() => DeleteAsync().GetAwaiter().GetResult();

    public async Task<CodexStoredGrant?> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await AcquireAsync(cancellationToken).ConfigureAwait(false);
        var record = await lease.LoadAsync(cancellationToken).ConfigureAwait(false);
        return record is null ? null : new(record.AccountId!, record.RefreshToken!);
    }

    public async Task WriteAsync(CodexStoredGrant grant, CancellationToken cancellationToken = default)
    {
        var next = Record(grant);
        await using var lease = await AcquireAsync(cancellationToken).ConfigureAwait(false);
        var current = await lease.LoadAsync(cancellationToken).ConfigureAwait(false);
        await lease.SaveAsync(next, current is null ? null : Revision(current), cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await AcquireAsync(cancellationToken).ConfigureAwait(false);
        await lease.DeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static StoredRecord Record(CodexStoredGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        if (string.IsNullOrEmpty(grant.AccountId) || string.IsNullOrEmpty(grant.RefreshToken))
            throw new ArgumentException("A stored grant requires an account and a refresh token.", nameof(grant));
        return new() { Version = 1, AccountId = grant.AccountId, RefreshToken = grant.RefreshToken };
    }

    // Content identity avoids adding fields to the existing record. It is used only inside
    // the encrypted journal, never as a filename, diagnostic value or public identifier.
    internal static Guid Revision(StoredRecord state)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, CodexGrantJson.Default.StoredRecord);
        Span<byte> hash = stackalloc byte[32];
        try { SHA256.HashData(bytes, hash); return new Guid(hash[..16]); }
        finally { CryptographicOperations.ZeroMemory(bytes); CryptographicOperations.ZeroMemory(hash); }
    }

    private static void Validate(StoredRecord state)
    {
        if (state.Version != 1 || string.IsNullOrEmpty(state.AccountId) || string.IsNullOrEmpty(state.RefreshToken))
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
    }

    internal sealed class StoredRecord
    {
        [JsonPropertyName("v")] public int Version { get; set; }
        [JsonPropertyName("account")] public string? AccountId { get; set; }
        [JsonPropertyName("refresh")] public string? RefreshToken { get; set; }
    }
}

[JsonSerializable(typeof(CodexGrantStore.StoredRecord))]
internal partial class CodexGrantJson : JsonSerializerContext;
