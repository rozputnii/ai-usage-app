using AiUsage.Core.Usage;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUsage.Infrastructure.Providers;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ProviderStateLease<TState>(string directory, FileStream exclusiveLock,
    ProviderStatePolicy<TState> policy, Action? afterStage) : IAsyncDisposable where TState : class
{
    private readonly string committedPath = Path.Combine(directory, policy.FileName);
    private readonly string pendingPath = Path.Combine(directory, policy.FileName + ".pending");
    private readonly string replacementPath = Path.Combine(directory, policy.FileName + ".new");

    internal async Task<TState?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            CheckPaths();
            var committed = await ReadAsync(committedPath, cancellationToken).ConfigureAwait(false);
            if (policy.SeparateJournal)
                return await RecoverJournalAsync(committed, cancellationToken).ConfigureAwait(false);
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
            var staged = policy.SeparateJournal
                ? await Task.Run(() => ProtectJournal(new ProviderPendingGeneration(1, expectedRevision, ciphertext)), cancellationToken).ConfigureAwait(false)
                : ciphertext;
            CheckPaths();
            await WriteFlushedAsync(pendingPath, staged, FileMode.CreateNew, cancellationToken).ConfigureAwait(false);
            await Task.Run(() => afterStage?.Invoke(), cancellationToken).ConfigureAwait(false);
            if (policy.SeparateJournal)
            {
                await PromoteJournalAsync(ciphertext, cancellationToken).ConfigureAwait(false);
                return next;
            }
            await Task.Run(() =>
            {
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
            // Remove the predecessor first: if its deletion fails, retain the pending successor
            // so a later load cannot silently replay an obsolete rotating grant.
            File.Delete(committedPath);
            File.Delete(pendingPath);
            if (policy.SeparateJournal) File.Delete(replacementPath);
        }
        catch (IOException) { throw new ProviderException(ProviderFailureKind.GrantNotRemoved); }
        catch (UnauthorizedAccessException) { throw new ProviderException(ProviderFailureKind.GrantNotRemoved); }
    }, cancellationToken);

    private async Task<TState?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var ciphertext = await ReadBytesAsync(path, policy.MaximumBytes, cancellationToken).ConfigureAwait(false);
        return ciphertext is null ? null : await Task.Run(() => Decode(ciphertext), cancellationToken).ConfigureAwait(false);
    }

    private TState Decode(byte[] ciphertext)
    {
        byte[]? plaintext = null;
        try
        {
            plaintext = ProtectedData.Unprotect(ciphertext, policy.Entropy, DataProtectionScope.CurrentUser);
            var state = JsonSerializer.Deserialize(plaintext, policy.JsonType);
            if (state is null) throw new ProviderException(ProviderFailureKind.RecoveryRequired);
            policy.Validate(state);
            return state;
        }
        catch (CryptographicException) { throw new ProviderException(ProviderFailureKind.RecoveryRequired); }
        catch (JsonException) { throw new ProviderException(ProviderFailureKind.RecoveryRequired); }
        finally { if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext); }
    }

    private async Task<TState?> RecoverJournalAsync(TState? committed, CancellationToken cancellationToken)
    {
        var bytes = await ReadBytesAsync(pendingPath, policy.MaximumBytes * 2, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            // A legacy unjournaled staging file is ambiguous: never silently replay its predecessor.
            if (File.Exists(replacementPath)) throw new ProviderException(ProviderFailureKind.RecoveryRequired);
            return committed;
        }
        var (journal, next) = await Task.Run(() =>
        {
            byte[]? plaintext = null;
            try
            {
                plaintext = ProtectedData.Unprotect(bytes, policy.Entropy, DataProtectionScope.CurrentUser);
                var journal = JsonSerializer.Deserialize(plaintext, ProviderPendingJson.Default.ProviderPendingGeneration);
                if (journal is null || journal.Version != 1 || journal.Ciphertext is not { Length: > 0 } || journal.Ciphertext.Length > policy.MaximumBytes)
                    throw new ProviderException(ProviderFailureKind.RecoveryRequired);
                return (journal, Decode(journal.Ciphertext));
            }
            catch (CryptographicException) { throw new ProviderException(ProviderFailureKind.RecoveryRequired); }
            catch (JsonException) { throw new ProviderException(ProviderFailureKind.RecoveryRequired); }
            finally { if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext); }
        }, cancellationToken).ConfigureAwait(false);
        var currentRevision = Revision(committed);
        if (currentRevision != journal.ParentRevision && currentRevision != policy.Revision(next))
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        // Replaying this local cutover is idempotent, including after promotion but before journal deletion.
        await PromoteJournalAsync(journal.Ciphertext, cancellationToken).ConfigureAwait(false);
        return next;
    }

    private byte[] ProtectJournal(ProviderPendingGeneration journal)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(journal, ProviderPendingJson.Default.ProviderPendingGeneration);
        try { return ProtectedData.Protect(plaintext, policy.Entropy, DataProtectionScope.CurrentUser); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private async Task PromoteJournalAsync(byte[] ciphertext, CancellationToken cancellationToken)
    {
        CheckPaths();
        await WriteFlushedAsync(replacementPath, ciphertext, FileMode.Create, cancellationToken).ConfigureAwait(false);
        await Task.Run(() =>
        {
            CheckPaths();
            File.Move(replacementPath, committedPath, overwrite: true);
            File.Delete(pendingPath);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteFlushedAsync(string path, byte[] bytes, FileMode mode, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        await Task.Run(() => stream.Flush(flushToDisk: true), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]?> ReadBytesAsync(string path, int maximum, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            if (stream.Length <= 0 || stream.Length > maximum) throw new ProviderException(ProviderFailureKind.RecoveryRequired);
            var bytes = new byte[(int)stream.Length];
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            return bytes;
        }
        catch (FileNotFoundException) { return null; }
    }

    private Guid? Revision(TState? state) => state is null ? null : policy.Revision(state);

    private void CheckPaths()
    {
        ProviderStatePaths.CheckDirectory(directory);
        ProviderStatePaths.CheckFile(committedPath);
        ProviderStatePaths.CheckFile(pendingPath);
        if (policy.SeparateJournal) ProviderStatePaths.CheckFile(replacementPath);
    }

    public ValueTask DisposeAsync() => exclusiveLock.DisposeAsync();
}

internal sealed record ProviderPendingGeneration(int Version, Guid? ParentRevision, byte[] Ciphertext);

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32)]
[JsonSerializable(typeof(ProviderPendingGeneration))]
internal partial class ProviderPendingJson : JsonSerializerContext;
