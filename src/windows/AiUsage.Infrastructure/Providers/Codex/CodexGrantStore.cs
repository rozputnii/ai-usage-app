using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Codex;

/// <summary>One stored Codex grant: the workspace it belongs to and the refresh token that renews it.</summary>
public sealed record CodexStoredGrant(string AccountId, string RefreshToken)
{
    public override string ToString() => "CodexStoredGrant (redacted)";
}

/// <summary>
/// Persists a single Codex grant encrypted with DPAPI CurrentUser inside an app-owned directory.
/// The record is opaque on disk: no token, account identifier or provider payload is written in plaintext,
/// and the file name carries no secret. DPAPI CurrentUser protects against other users and offline access
/// to a copied file; it is not a defence against malicious code running as this same user.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CodexGrantStore
{
    private const int CurrentVersion = 1;
    private const int MaximumRecordBytes = 64 * 1024;
    // Bound to this application and record purpose so a protected blob from elsewhere cannot be substituted.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AiUsage.Codex.Grant.v1");

    private readonly string path;

    public CodexGrantStore(string ownedDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedDirectory);
        Directory = Path.GetFullPath(ownedDirectory);
        path = Path.Combine(Directory, "codex.grant");
    }

    /// <summary>The app-owned directory this store may write to; nothing outside it is created or removed.</summary>
    public string Directory { get; }

    /// <summary>Returns the stored grant, or null when none is stored or the record cannot be used.</summary>
    public CodexStoredGrant? Read()
    {
        byte[] protected_;
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length == 0 || file.Length > MaximumRecordBytes)
                return null;
            protected_ = File.ReadAllBytes(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        try
        {
            var plain = ProtectedData.Unprotect(protected_, Entropy, DataProtectionScope.CurrentUser);
            var record = JsonSerializer.Deserialize(plain, CodexGrantJson.Default.StoredRecord);
            CryptographicOperations.ZeroMemory(plain);
            if (record is null || record.Version != CurrentVersion ||
                string.IsNullOrEmpty(record.AccountId) || string.IsNullOrEmpty(record.RefreshToken))
                return null;
            return new CodexStoredGrant(record.AccountId, record.RefreshToken);
        }
        // A record written by another user or a corrupted file is treated as absent, never as a failure to report upward.
        catch (CryptographicException) { return null; }
        catch (JsonException) { return null; }
    }

    /// <summary>Replaces the stored grant. The previous record stays intact if the write fails.</summary>
    public void Write(CodexStoredGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        if (string.IsNullOrEmpty(grant.AccountId) || string.IsNullOrEmpty(grant.RefreshToken))
            throw new ArgumentException("A stored grant requires an account and a refresh token.", nameof(grant));
        System.IO.Directory.CreateDirectory(Directory);
        var plain = JsonSerializer.SerializeToUtf8Bytes(
            new StoredRecord { Version = CurrentVersion, AccountId = grant.AccountId, RefreshToken = grant.RefreshToken },
            CodexGrantJson.Default.StoredRecord);
        byte[] protected_;
        try { protected_ = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser); }
        finally { CryptographicOperations.ZeroMemory(plain); }
        var staged = path + ".new";
        File.WriteAllBytes(staged, protected_);
        // Replace only after the new record is fully written, so an interrupted write cannot leave a truncated grant.
        File.Move(staged, path, overwrite: true);
    }

    /// <summary>Removes the stored grant. This is local only and does not revoke anything at the provider.</summary>
    public void Delete()
    {
        try
        {
            File.Delete(path);
            File.Delete(path + ".new");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A grant that cannot be removed must not look removed.
            throw new CodexException(CodexFailureKind.GrantNotRemoved);
        }
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
