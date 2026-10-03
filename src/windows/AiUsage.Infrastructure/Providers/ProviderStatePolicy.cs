using AiUsage.Core.Usage;
using AiUsage.Core.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace AiUsage.Infrastructure.Providers;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed record ProviderStatePolicy<TState>(
    string FileName, byte[] Entropy, JsonTypeInfo<TState> JsonType,
    Func<TState, Guid> Revision, Func<TState, Guid?> ParentRevision,
    Func<TState, Guid?, TState> Stamp, Action<TState> Validate,
    int MaximumBytes = 2 * 1024 * 1024, bool SeparateJournal = false,
    Func<ReadOnlyMemory<byte>, TState>? ReadState = null,
    Func<TState, bool>? NeedsMigration = null, Func<TState, TState>? Migrate = null,
    Func<TState, TState>? WithoutMigrationCache = null) where TState : class
{
    internal Task<ProviderStateLease<TState>> AcquireAsync(string directory, Action? afterStage, IDiagnosticSink? diagnostics, CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return new ProviderStateLease<TState>(directory, ProviderStatePaths.Acquire(directory, FileName + ".lock"), this, afterStage, diagnostics);
        }
        catch (IOException error) { diagnostics?.Failure(DiagnosticEvent.LeaseUnavailable, error); throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException error) { diagnostics?.Failure(DiagnosticEvent.LeaseUnavailable, error); throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
    }, cancellationToken);
}
