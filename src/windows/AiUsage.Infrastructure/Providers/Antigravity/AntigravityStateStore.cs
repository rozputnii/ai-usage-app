using AiUsage.Core.Usage;
using System.Security.Cryptography;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Antigravity;

/// <summary>
/// App-owned, DPAPI CurrentUser state. A process holds the lease across an entire credential
/// operation; the encrypted pending generation carries its parent revision for crash recovery.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AntigravityStateStore
{
    private readonly string directory;
    private readonly Action? afterStage;

    public AntigravityStateStore(string ownedDirectory) : this(ownedDirectory, null) { }
    internal AntigravityStateStore(string ownedDirectory, Action? afterStage)
    {
        directory = Path.GetFullPath(ownedDirectory);
        this.afterStage = afterStage;
    }

    // Path validation and lock acquisition are synchronous OS operations; keep them off the UI.
    internal Task<AntigravityStateLease> AcquireAsync(CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            CheckDirectory(directory);
            Directory.CreateDirectory(directory);
            CheckDirectory(directory);
            var lockPath = Path.Combine(directory, "antigravity.state.lock");
            CheckFile(lockPath);
            return new AntigravityStateLease(directory, new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None), afterStage);
        }
        catch (IOException) { throw new AntigravityException(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new AntigravityException(ProviderFailureKind.StorageUnavailable); }
    }, cancellationToken);

    internal static void CheckDirectory(string path)
    {
        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
        }
    }

    internal static void CheckFile(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class AntigravityStateLease(string directory, FileStream exclusiveLock, Action? afterStage) : IAsyncDisposable
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly byte[] Entropy = "AiUsage.Antigravity.State.v1"u8.ToArray();
    private readonly string committedPath = Path.Combine(directory, "antigravity.state");
    private readonly string pendingPath = Path.Combine(directory, "antigravity.state.pending");

    internal async Task<AntigravityStoredState?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            CheckPaths();
            var committed = await ReadAsync(committedPath, cancellationToken).ConfigureAwait(false);
            var pending = await ReadAsync(pendingPath, cancellationToken).ConfigureAwait(false);
            if (pending is null)
                return committed;
            if (pending.ParentRevision != committed?.Revision)
                throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
            // A fully staged record is the only recoverable successor. Corrupt or unrelated
            // generations stay untouched, and must never make us replay a superseded refresh token.
            await Task.Run(() =>
            {
                CheckPaths();
                File.Move(pendingPath, committedPath, overwrite: true);
            }, cancellationToken).ConfigureAwait(false);
            return pending;
        }
        catch (IOException) { throw new AntigravityException(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new AntigravityException(ProviderFailureKind.StorageUnavailable); }
    }

    internal async Task<AntigravityStoredState> SaveAsync(AntigravityStoredState next, Guid? expectedRevision, CancellationToken cancellationToken)
    {
        try
        {
            var current = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (current?.Revision != expectedRevision)
                throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
            next = next with { Version = 1, Revision = Guid.NewGuid(), ParentRevision = expectedRevision };
            Validate(next);
            var ciphertext = await Task.Run(() =>
            {
                var plaintext = JsonSerializer.SerializeToUtf8Bytes(next, AntigravityStateJson.Default.AntigravityStoredState);
                try
                {
                    if (plaintext.Length > MaximumBytes - 4096)
                        throw new AntigravityException(ProviderFailureKind.StorageUnavailable);
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
        catch (IOException) { throw new AntigravityException(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new AntigravityException(ProviderFailureKind.StorageUnavailable); }
        catch (CryptographicException) { throw new AntigravityException(ProviderFailureKind.StorageUnavailable); }
    }

    internal Task DeleteAsync(CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            CheckPaths();
            // Only these two exact app-owned files. An explicit disconnect can remove an unreadable
            // owned record, but a link, directory or redirected root is never followed.
            File.Delete(pendingPath);
            File.Delete(committedPath);
        }
        catch (IOException) { throw new AntigravityException(ProviderFailureKind.GrantNotRemoved); }
        catch (UnauthorizedAccessException) { throw new AntigravityException(ProviderFailureKind.GrantNotRemoved); }
    }, cancellationToken);

    private async Task<AntigravityStoredState?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > MaximumBytes)
            throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
        var ciphertext = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(ciphertext, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            byte[]? plaintext = null;
            try
            {
                plaintext = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                var state = JsonSerializer.Deserialize(plaintext, AntigravityStateJson.Default.AntigravityStoredState);
                if (state is null)
                    throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
                Validate(state);
                return state;
            }
            catch (CryptographicException) { throw new AntigravityException(ProviderFailureKind.RecoveryRequired); }
            catch (JsonException) { throw new AntigravityException(ProviderFailureKind.RecoveryRequired); }
            finally { if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext); }
        }, cancellationToken).ConfigureAwait(false);
    }

    private void CheckPaths()
    {
        AntigravityStateStore.CheckDirectory(directory);
        AntigravityStateStore.CheckFile(committedPath);
        AntigravityStateStore.CheckFile(pendingPath);
    }

    private static void Validate(AntigravityStoredState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision ||
            !AntigravityAuthClient.SafeIdentity(state.AccountId) ||
            !AntigravityAuthClient.SafeIdentity(state.ProjectId) ||
            !AntigravityAuthClient.SafeToken(state.RefreshToken) ||
            state.Tier is { Length: 0 or > 128 })
            throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
        if (state.CachedQuota is { } quota)
        {
            if (quota.Groups is null || quota.Groups.Count > 1024 ||
                quota.Groups.Any(group => group is null || group.Id is null || group.Windows is null ||
                    group.Windows.Any(window => window is null || window.Id is null ||
                        !Percent(window.UsedPercent) || !Percent(window.RemainingPercent))))
                throw new AntigravityException(ProviderFailureKind.RecoveryRequired);
        }
    }

    private static bool Percent(double? value) => value is null || (double.IsFinite(value.Value) && value is >= 0 and <= 100);
    public ValueTask DisposeAsync() => exclusiveLock.DisposeAsync();
}
