using AiUsage.Core.Providers.Claude;
using System.Security.Cryptography;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers.Claude;

/// <summary>
/// App-owned, DPAPI CurrentUser state. A process holds the lease across an entire credential
/// operation; the encrypted pending generation carries its parent revision for crash recovery.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class ClaudeStateStore
{
    private readonly string directory;
    private readonly Action? afterStage;

    public ClaudeStateStore(string ownedDirectory) : this(ownedDirectory, null) { }
    internal ClaudeStateStore(string ownedDirectory, Action? afterStage)
    {
        directory = Path.GetFullPath(ownedDirectory);
        this.afterStage = afterStage;
    }

    // Path validation and lock acquisition are synchronous OS operations; keep them off the UI.
    internal Task<ClaudeStateLease> AcquireAsync(CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            CheckDirectory(directory);
            Directory.CreateDirectory(directory);
            CheckDirectory(directory);
            var lockPath = Path.Combine(directory, "claude.state.lock");
            CheckFile(lockPath);
            return new ClaudeStateLease(directory, new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None), afterStage);
        }
        catch (IOException) { throw new ClaudeException(ClaudeFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new ClaudeException(ClaudeFailureKind.StorageUnavailable); }
    }, cancellationToken);

    internal static void CheckDirectory(string path)
    {
        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
        }
    }

    internal static void CheckFile(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ClaudeStateLease(string directory, FileStream exclusiveLock, Action? afterStage) : IAsyncDisposable
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly byte[] Entropy = "AiUsage.Claude.State.v1"u8.ToArray();
    private readonly string committedPath = Path.Combine(directory, "claude.state");
    private readonly string pendingPath = Path.Combine(directory, "claude.state.pending");

    internal async Task<ClaudeStoredState?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            CheckPaths();
            var committed = await ReadAsync(committedPath, cancellationToken).ConfigureAwait(false);
            var pending = await ReadAsync(pendingPath, cancellationToken).ConfigureAwait(false);
            if (pending is null)
                return committed;
            if (pending.ParentRevision != committed?.Revision)
                throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
            // A fully staged record is the only recoverable successor. Corrupt or unrelated
            // generations stay untouched, and must never make us retry the old refresh token.
            await Task.Run(() =>
            {
                CheckPaths();
                File.Move(pendingPath, committedPath, overwrite: true);
            }, cancellationToken).ConfigureAwait(false);
            return pending;
        }
        catch (IOException) { throw new ClaudeException(ClaudeFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new ClaudeException(ClaudeFailureKind.StorageUnavailable); }
    }

    internal async Task<ClaudeStoredState> SaveAsync(ClaudeStoredState next, Guid? expectedRevision, CancellationToken cancellationToken)
    {
        try
        {
            var current = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (current?.Revision != expectedRevision)
                throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
            next = next with { Version = 1, Revision = Guid.NewGuid(), ParentRevision = expectedRevision };
            Validate(next);
            var ciphertext = await Task.Run(() =>
            {
                var plaintext = JsonSerializer.SerializeToUtf8Bytes(next, ClaudeStateJson.Default.ClaudeStoredState);
                try
                {
                    if (plaintext.Length > MaximumBytes - 4096)
                        throw new ClaudeException(ClaudeFailureKind.StorageUnavailable);
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
        catch (IOException) { throw new ClaudeException(ClaudeFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new ClaudeException(ClaudeFailureKind.StorageUnavailable); }
        catch (CryptographicException) { throw new ClaudeException(ClaudeFailureKind.StorageUnavailable); }
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
        catch (IOException) { throw new ClaudeException(ClaudeFailureKind.GrantNotRemoved); }
        catch (UnauthorizedAccessException) { throw new ClaudeException(ClaudeFailureKind.GrantNotRemoved); }
    }, cancellationToken);

    private async Task<ClaudeStoredState?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > MaximumBytes)
            throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
        var ciphertext = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(ciphertext, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            byte[]? plaintext = null;
            try
            {
                plaintext = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                var state = JsonSerializer.Deserialize(plaintext, ClaudeStateJson.Default.ClaudeStoredState);
                if (state is null)
                    throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
                Validate(state);
                return state;
            }
            catch (CryptographicException) { throw new ClaudeException(ClaudeFailureKind.RecoveryRequired); }
            catch (JsonException) { throw new ClaudeException(ClaudeFailureKind.RecoveryRequired); }
            finally { if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext); }
        }, cancellationToken).ConfigureAwait(false);
    }

    private void CheckPaths()
    {
        ClaudeStateStore.CheckDirectory(directory);
        ClaudeStateStore.CheckFile(committedPath);
        ClaudeStateStore.CheckFile(pendingPath);
    }

    private static void Validate(ClaudeStoredState state)
    {
        if (state.Version != 1 || state.Revision == Guid.Empty || state.ParentRevision == state.Revision ||
            state.Identity is null || !ClaudeAuthClient.SafeIdentity(state.Identity.AccountId) ||
            !ClaudeAuthClient.SafeIdentity(state.Identity.OrganizationId) || !ClaudeAuthClient.SafeToken(state.RefreshToken))
            throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
        if (state.CachedQuota is { } reading)
        {
            if (reading.Quota is null || reading.Quota.Groups is null || reading.Quota.Groups.Count > 1024 ||
                reading.Quota.Groups.Any(group => group is null || group.Id is null || group.Windows is null ||
                    group.Windows.Any(window => window is null || window.Id is null || !Percent(window.UsedPercent) || !Percent(window.RemainingPercent))))
                throw new ClaudeException(ClaudeFailureKind.RecoveryRequired);
        }
    }

    private static bool Percent(double? value) => value is null || (double.IsFinite(value.Value) && value is >= 0 and <= 100);
    public ValueTask DisposeAsync() => exclusiveLock.DisposeAsync();
}
