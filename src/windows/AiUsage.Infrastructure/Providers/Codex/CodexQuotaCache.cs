using System.Text.Json;
using System.Text.Json.Serialization;
using AiUsage.Core.Usage;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>A cached quota reading and the moment it was actually retrieved from the provider.</summary>
public sealed record CachedQuota(QuotaSnapshot Quota, DateTimeOffset RetrievedAt);

/// <summary>
/// Stores the last quota reading in the app-owned directory so a relaunch or an unavailable
/// provider can show last-known values instead of nothing. The record holds normalized quota
/// only: no token, no refresh token and no account identifier, so it needs no encryption and
/// must never be treated as proof of current entitlement.
/// </summary>
public sealed class CodexQuotaCache
{
    private const int MaximumRecordBytes = 512 * 1024;
    private readonly string path;

    public CodexQuotaCache(string ownedDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedDirectory);
        Directory = Path.GetFullPath(ownedDirectory);
        path = Path.Combine(Directory, "codex.quota.json");
    }

    /// <summary>The app-owned directory this cache may write to; nothing outside it is touched.</summary>
    public string Directory { get; }

    /// <summary>Returns the cached reading, or null when none is stored or the record is unusable.</summary>
    public CachedQuota? Read()
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length == 0 || file.Length > MaximumRecordBytes)
                return null;
            var record = JsonSerializer.Deserialize(File.ReadAllBytes(path), CodexCacheJson.Default.CachedQuotaRecord);
            if (record?.Version != 1 || record.Quota is null || record.RetrievedAt is not { } retrievedAt)
                return null;
            return new CachedQuota(record.Quota, retrievedAt);
        }
        // A corrupted or foreign record is treated as no cache rather than as a failure to report.
        catch (JsonException) { return null; }
        catch (NotSupportedException) { return null; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return null; }
    }

    /// <summary>Replaces the cached reading. A failed write leaves the previous record intact.</summary>
    public void Write(CachedQuota cached)
    {
        ArgumentNullException.ThrowIfNull(cached);
        System.IO.Directory.CreateDirectory(Directory);
        var staged = path + ".new";
        File.WriteAllBytes(staged, JsonSerializer.SerializeToUtf8Bytes(
            new CachedQuotaRecord { Version = 1, Quota = cached.Quota, RetrievedAt = cached.RetrievedAt },
            CodexCacheJson.Default.CachedQuotaRecord));
        File.Move(staged, path, overwrite: true);
    }

    /// <summary>Removes the cached reading; used when the account is disconnected.</summary>
    public void Delete()
    {
        try
        {
            File.Delete(path);
            File.Delete(path + ".new");
        }
        // A stale cache is not a credential; failing to remove it must not block disconnecting.
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }

    internal sealed class CachedQuotaRecord
    {
        [JsonPropertyName("v")] public int Version { get; set; }
        [JsonPropertyName("retrievedAt")] public DateTimeOffset? RetrievedAt { get; set; }
        [JsonPropertyName("quota")] public QuotaSnapshot? Quota { get; set; }
    }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CodexQuotaCache.CachedQuotaRecord))]
internal partial class CodexCacheJson : JsonSerializerContext;
