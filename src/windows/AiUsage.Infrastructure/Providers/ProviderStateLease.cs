using AiUsage.Core.Usage;
using System.Security.Cryptography;
using System.Text.Json;

namespace AiUsage.Infrastructure.Providers;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ProviderStateLease<TState>(string directory, FileStream exclusiveLock,
    ProviderStatePolicy<TState> policy, Action? afterStage) : IAsyncDisposable where TState : class
{
    private readonly string committedPath = Path.Combine(directory, policy.FileName);
    private readonly string pendingPath = Path.Combine(directory, policy.FileName + ".pending");

    internal async Task<TState?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            CheckPaths();
            var committed = await ReadAsync(committedPath, cancellationToken).ConfigureAwait(false);
            var pending = await ReadAsync(pendingPath, cancellationToken).ConfigureAwait(false);
            if (pending is null)
                return committed;
            if (policy.ParentRevision(pending) != Revision(committed))
                throw new ProviderException(ProviderFailureKind.RecoveryRequired);
            // A fully staged record is the only recoverable successor. Corrupt or unrelated
            // generations stay untouched, and must never make us retry the old refresh token.
            await Task.Run(() =>
            {
                CheckPaths();
                File.Move(pendingPath, committedPath, overwrite: true);
            }, cancellationToken).ConfigureAwait(false);
            return pending;
        }
        catch (IOException) { throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
    }

    internal async Task<TState> SaveAsync(TState next, Guid? expectedRevision, CancellationToken cancellationToken)
    {
        try
        {
            var current = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (Revision(current) != expectedRevision)
                throw new ProviderException(ProviderFailureKind.RecoveryRequired);
            next = policy.Stamp(next, expectedRevision);
            policy.Validate(next);
            var ciphertext = await Task.Run(() =>
            {
                var plaintext = JsonSerializer.SerializeToUtf8Bytes(next, policy.JsonType);
                try
                {
                    if (plaintext.Length > policy.MaximumBytes - 4096)
                        throw new ProviderException(ProviderFailureKind.StorageUnavailable);
                    return ProtectedData.Protect(plaintext, policy.Entropy, DataProtectionScope.CurrentUser);
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
        catch (IOException) { throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
        catch (CryptographicException) { throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
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
        catch (IOException) { throw new ProviderException(ProviderFailureKind.GrantNotRemoved); }
        catch (UnauthorizedAccessException) { throw new ProviderException(ProviderFailureKind.GrantNotRemoved); }
    }, cancellationToken);

    private async Task<TState?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length <= 0 || stream.Length > policy.MaximumBytes)
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        var ciphertext = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(ciphertext, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            byte[]? plaintext = null;
            try
            {
                plaintext = ProtectedData.Unprotect(ciphertext, policy.Entropy, DataProtectionScope.CurrentUser);
                var state = JsonSerializer.Deserialize(plaintext, policy.JsonType);
                if (state is null)
                    throw new ProviderException(ProviderFailureKind.RecoveryRequired);
                policy.Validate(state);
                return state;
            }
            catch (CryptographicException) { throw new ProviderException(ProviderFailureKind.RecoveryRequired); }
            catch (JsonException) { throw new ProviderException(ProviderFailureKind.RecoveryRequired); }
            finally { if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext); }
        }, cancellationToken).ConfigureAwait(false);
    }

    private Guid? Revision(TState? state) => state is null ? null : policy.Revision(state);

    private void CheckPaths()
    {
        ProviderStatePaths.CheckDirectory(directory);
        ProviderStatePaths.CheckFile(committedPath);
        ProviderStatePaths.CheckFile(pendingPath);
    }

    public ValueTask DisposeAsync() => exclusiveLock.DisposeAsync();
}
