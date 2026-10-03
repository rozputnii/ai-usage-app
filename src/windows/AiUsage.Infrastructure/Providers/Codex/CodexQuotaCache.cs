using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Core.Usage;
using AiUsage.Core.Diagnostics;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>A cached quota reading and the moment it was actually retrieved from the provider.</summary>
internal sealed record CachedQuota(QuotaSnapshot Quota, DateTimeOffset RetrievedAt);

/// <summary>
/// Stores the last quota reading in the app-owned directory so a relaunch or an unavailable
/// provider can show last-known values instead of nothing. The record holds normalized quota
/// only: no token, refresh token or provider identity. Account-scoped v3 records carry an opaque
/// app storage reference to reject another account's cache; this reference needs no encryption and
/// must never be treated as proof of current entitlement.
/// </summary>
internal sealed class CodexQuotaCache
{
    private const int MaximumRecordBytes = 512 * 1024;
    private readonly string path;
    private readonly Action? beforeDelete;
    private readonly IDiagnosticSink? diagnostics;
    private readonly Guid? binding;

    public CodexQuotaCache(string ownedDirectory) : this(ownedDirectory, null) { }

    internal CodexQuotaCache(string ownedDirectory, Action? beforeDelete, IDiagnosticSink? diagnostics = null, Guid? binding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedDirectory);
        if (binding == Guid.Empty) throw new ArgumentException("A cache binding cannot be empty.", nameof(binding));
        Directory = Path.GetFullPath(ownedDirectory);
        path = Path.Combine(Directory, "codex.quota.json");
        this.beforeDelete = beforeDelete;
        this.diagnostics = diagnostics;
        this.binding = binding;
    }

    /// <summary>The app-owned directory this cache may write to; nothing outside it is touched.</summary>
    public string Directory { get; }

    /// <summary>Returns the cached reading, or null when none is stored or the record is unusable.</summary>
    public CachedQuota? Read() => ReadAsync().GetAwaiter().GetResult();

    public Task<CachedQuota?> ReadAsync(CancellationToken cancellationToken = default) => Task.Run(async () =>
    {
        try
        {
            await using var lease = ProviderStatePaths.Acquire(Directory, "codex.quota.json.lock");
            CheckPaths();
            if (!File.Exists(path))
                return null;
            var bytes = await ReadBoundedAsync(path, cancellationToken).ConfigureAwait(false);
            var record = JsonSerializer.Deserialize(bytes, CodexCacheJson.Default.CachedQuotaRecord);
            if (record?.Version is not (1 or 2 or 3) || record.Quota is null || record.RetrievedAt is not { } retrievedAt)
                return Discarded();
            // Legacy readings have no identity evidence; never attribute them to a new account.
            if (binding is not null ? record.Version != 3 || record.Binding != binding : record.Version == 3 || record.Binding is not null)
                return Discarded();
            if (!ProviderStateMigration.Valid(record.Quota))
                return Discarded();
            if (record.Version == 1)
            {
                diagnostics?.Signal(DiagnosticEvent.MigrationStarted);
                var backup = path + ".v1.bak";
                if (File.Exists(backup))
                {
                    var existing = await ReadBoundedAsync(backup, cancellationToken).ConfigureAwait(false);
                    if (!existing.AsSpan().SequenceEqual(bytes)) return Discarded();
                }
                else
                {
                    await WriteFlushedAsync(backup + ".new", bytes, cancellationToken).ConfigureAwait(false);
                    var verified = await ReadBoundedAsync(backup + ".new", cancellationToken).ConfigureAwait(false);
                    if (!verified.AsSpan().SequenceEqual(bytes)) return Discarded();
                    CheckPaths();
                    File.Move(backup + ".new", backup);
                }
                record.Quota = ProviderStateMigration.UpgradeQuota("codex", record.Quota);
                if (record.Quota is null) return Discarded();
                await WriteRecordAsync(new(record.Quota, retrievedAt), cancellationToken).ConfigureAwait(false);
                diagnostics?.Signal(DiagnosticEvent.MigrationCompleted);
            }
            return new CachedQuota(record.Quota, retrievedAt);
        }
        // A corrupted or foreign record is treated as no cache rather than as a failure to report.
        catch (Exception error) when (error is JsonException or NotSupportedException or InvalidOperationException or KeyNotFoundException or FormatException or IOException or UnauthorizedAccessException)
        { diagnostics?.Failure(DiagnosticEvent.PersistenceFailure, error); return Discarded(); }
    }, cancellationToken);

    private CachedQuota? Discarded()
    {
        diagnostics?.Signal(DiagnosticEvent.CacheDiscarded, DiagnosticSeverity.Warning);
        return null;
    }

    /// <summary>Replaces the cached reading. A failed write leaves the previous record intact.</summary>
    public void Write(CachedQuota cached) => WriteAsync(cached).GetAwaiter().GetResult();

    public Task WriteAsync(CachedQuota cached, CancellationToken cancellationToken = default) => Task.Run(async () =>
    {
        ArgumentNullException.ThrowIfNull(cached);
        await using var lease = ProviderStatePaths.Acquire(Directory, "codex.quota.json.lock");
        await WriteRecordAsync(cached, cancellationToken).ConfigureAwait(false);
    }, cancellationToken);

    private async Task WriteRecordAsync(CachedQuota cached, CancellationToken cancellationToken)
    {
        CheckPaths();
        var staged = path + ".new";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            new CachedQuotaRecord { Version = binding is null ? 2 : 3, Binding = binding, Quota = cached.Quota, RetrievedAt = cached.RetrievedAt },
            CodexCacheJson.Default.CachedQuotaRecord);
        if (bytes.Length > MaximumRecordBytes) throw new IOException("Quota cache exceeds its size limit.");
        await WriteFlushedAsync(staged, bytes, cancellationToken).ConfigureAwait(false);
        CheckPaths();
        File.Move(staged, path, overwrite: true);
    }

    private static async Task WriteFlushedAsync(string target, byte[] bytes, CancellationToken cancellationToken)
    {
        await using (var stream = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None,
            4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(string target, CancellationToken token)
    {
        await using var stream = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > MaximumRecordBytes) throw new InvalidDataException();
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        return bytes;
    }

    /// <summary>Removes the cached reading; used when the account is disconnected.</summary>
    public void Delete() => DeleteAsync().GetAwaiter().GetResult();

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        // Fault-injection seam for cancellation between grant deletion and cache cleanup.
        beforeDelete?.Invoke();
        return Task.Run(() =>
        {
            try
            {
                using var lease = ProviderStatePaths.Acquire(Directory, "codex.quota.json.lock");
                CheckPaths();
                File.Delete(path);
                File.Delete(path + ".new");
                File.Delete(path + ".v1.bak");
                File.Delete(path + ".v1.bak.new");
            }
            // A stale cache is not a credential; failing to remove it must not block disconnecting.
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { diagnostics?.Failure(DiagnosticEvent.PersistenceFailure, error); }
        }, cancellationToken);
    }

    private void CheckPaths()
    {
        ProviderStatePaths.CheckDirectory(Directory);
        ProviderStatePaths.CheckFile(path);
        ProviderStatePaths.CheckFile(path + ".new");
        ProviderStatePaths.CheckFile(path + ".v1.bak");
        ProviderStatePaths.CheckFile(path + ".v1.bak.new");
    }

    internal sealed class CachedQuotaRecord
    {
        [JsonPropertyName("v")] public int Version { get; set; }
        [JsonPropertyName("retrievedAt")] public DateTimeOffset? RetrievedAt { get; set; }
        [JsonPropertyName("quota")] public QuotaSnapshot? Quota { get; set; }
        [JsonPropertyName("binding")] public Guid? Binding { get; set; }
    }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(QuotaQuantityConverter), typeof(QuotaLimitValueConverter)])]
[JsonSerializable(typeof(CodexQuotaCache.CachedQuotaRecord))]
internal partial class CodexCacheJson : JsonSerializerContext;
