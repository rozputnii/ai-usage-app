using AiUsage.Core.Providers.Copilot;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers.Copilot;

// Grant and last usage share one protected generation, so a cache cannot cross an account switch.
internal sealed record CopilotStoredState
{
    public int Version { get; init; } = 1;
    public Guid Revision { get; init; }
    public required CopilotIdentity Identity { get; init; }
    public required string AccessToken { get; init; }
    public string? GrantedScope { get; init; }
    public bool NeedsReauthentication { get; init; }
    public CopilotUsageReading? CachedUsage { get; init; }
    public override string ToString() => "CopilotStoredState (redacted)";
}

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32)]
[JsonSerializable(typeof(CopilotStoredState))]
internal partial class CopilotStateJson : JsonSerializerContext;

/// <summary>
/// App-owned, DPAPI CurrentUser state in its own files. The GitHub OAuth App token does not rotate,
/// so a torn stage is discarded rather than recovered: the committed generation stays valid.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class CopilotStateStore
{
    private readonly string directory;
    private readonly Action? afterStage;

    public CopilotStateStore(string ownedDirectory) : this(ownedDirectory, null) { }
    internal CopilotStateStore(string ownedDirectory, Action? afterStage)
    {
        directory = Path.GetFullPath(ownedDirectory);
        this.afterStage = afterStage;
    }

    internal Task<CopilotStateLease> AcquireAsync(CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            CheckDirectory(directory);
            Directory.CreateDirectory(directory);
            CheckDirectory(directory);
            var lockPath = Path.Combine(directory, "copilot.state.lock");
            CheckFile(lockPath);
            return new CopilotStateLease(directory, new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None), afterStage);
        }
        catch (IOException) { throw new CopilotException(CopilotFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new CopilotException(CopilotFailureKind.StorageUnavailable); }
    }, cancellationToken);

    internal static void CheckDirectory(string path)
    {
        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new CopilotException(CopilotFailureKind.RecoveryRequired);
        }
    }

    internal static void CheckFile(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new CopilotException(CopilotFailureKind.RecoveryRequired);
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class CopilotStateLease(string directory, FileStream exclusiveLock, Action? afterStage) : IAsyncDisposable
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly byte[] Entropy = "AiUsage.Copilot.State.v1"u8.ToArray();
    private readonly string committedPath = Path.Combine(directory, "copilot.state");
    private readonly string pendingPath = Path.Combine(directory, "copilot.state.pending");

    internal async Task<CopilotStoredState?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            CheckPaths();
            // An unpromoted stage was never observed as committed. Remove the owned file; the
            // committed generation, whose token GitHub does not rotate, remains authoritative.
            if (File.Exists(pendingPath))
                await Task.Run(() => { CheckPaths(); File.Delete(pendingPath); }, cancellationToken).ConfigureAwait(false);
            return await ReadAsync(committedPath, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) { throw new CopilotException(CopilotFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new CopilotException(CopilotFailureKind.StorageUnavailable); }
    }

    internal async Task<CopilotStoredState> SaveAsync(CopilotStoredState next, Guid? expectedRevision, CancellationToken cancellationToken)
    {
        try
        {
            var current = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (current?.Revision != expectedRevision)
                throw new CopilotException(CopilotFailureKind.RecoveryRequired);
            next = next with { Version = 1, Revision = Guid.NewGuid() };
            Validate(next);
            var ciphertext = await Task.Run(() =>
            {
                var plaintext = JsonSerializer.SerializeToUtf8Bytes(next, CopilotStateJson.Default.CopilotStoredState);
                try
                {
                    if (plaintext.Length > MaximumBytes - 4096)
                        throw new CopilotException(CopilotFailureKind.StorageUnavailable);
                    return ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
                }
                finally { CryptographicOperations.ZeroMemory(plaintext); }
            }, cancellationToken).ConfigureAwait(false);
            CheckPaths();
            await using (var stream = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(ciphertext, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                await Task.Run(() => stream.Flush(flushToDisk: true), cancellationToken).ConfigureAwait(false);
            }
            await Task.Run(() =>
            {
                afterStage?.Invoke();
                CheckPaths();
                File.Move(pendingPath, committedPath, overwrite: true);
            }, cancellationToken).ConfigureAwait(false);
            return next;
        }
        catch (IOException) { throw new CopilotException(CopilotFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new CopilotException(CopilotFailureKind.StorageUnavailable); }
        catch (CryptographicException) { throw new CopilotException(CopilotFailureKind.StorageUnavailable); }
    }

    internal Task DeleteAsync(CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            CheckPaths();
            // Only these two exact app-owned files; a link, directory or redirected root is never followed.
            File.Delete(pendingPath);
            File.Delete(committedPath);
        }
        catch (IOException) { throw new CopilotException(CopilotFailureKind.GrantNotRemoved); }
        catch (UnauthorizedAccessException) { throw new CopilotException(CopilotFailureKind.GrantNotRemoved); }
    }, cancellationToken);

    private async Task<CopilotStoredState?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > MaximumBytes)
            throw new CopilotException(CopilotFailureKind.RecoveryRequired);
        var ciphertext = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(ciphertext, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            byte[]? plaintext = null;
            try
            {
                plaintext = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                var state = JsonSerializer.Deserialize(plaintext, CopilotStateJson.Default.CopilotStoredState)
                    ?? throw new CopilotException(CopilotFailureKind.RecoveryRequired);
                Validate(state);
                return state;
            }
            catch (CryptographicException) { throw new CopilotException(CopilotFailureKind.RecoveryRequired); }
            catch (JsonException) { throw new CopilotException(CopilotFailureKind.RecoveryRequired); }
            finally { if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext); }
        }, cancellationToken).ConfigureAwait(false);
    }

    private void CheckPaths()
    {
        CopilotStateStore.CheckDirectory(directory);
        CopilotStateStore.CheckFile(committedPath);
        CopilotStateStore.CheckFile(pendingPath);
    }

    private static void Validate(CopilotStoredState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.Identity is null ||
            state.Identity.AccountId <= 0 || state.Identity.Login is null || !CopilotAuthClient.IsLogin(state.Identity.Login) ||
            string.IsNullOrEmpty(state.AccessToken) || state.AccessToken.Length > 4096 || state.AccessToken.Any(char.IsControl))
            throw new CopilotException(CopilotFailureKind.RecoveryRequired);
        if (state.CachedUsage is { } usage && (!ValidReport(usage.AiCredits) || !ValidReport(usage.PremiumRequests)))
            throw new CopilotException(CopilotFailureKind.RecoveryRequired);
    }

    private static bool ValidReport(CopilotUsageReport? report) => report is null ||
        (report.Items is { Count: <= 1000 } items && items.All(item => item is { Product: not null, Sku: not null, UnitType: not null } &&
            item.GrossQuantity >= 0 && item.DiscountQuantity >= 0 && item.NetQuantity >= 0 && item.NetAmount >= 0));

    public ValueTask DisposeAsync() => exclusiveLock.DisposeAsync();
}
