using AiUsage.Core.Usage;
using System.Text.Json.Serialization.Metadata;

namespace AiUsage.Infrastructure.Providers;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed record ProviderStatePolicy<TState>(
    string FileName, byte[] Entropy, JsonTypeInfo<TState> JsonType,
    Func<TState, Guid> Revision, Func<TState, Guid?> ParentRevision,
    Func<TState, Guid?, TState> Stamp, Action<TState> Validate,
    int MaximumBytes = 2 * 1024 * 1024) where TState : class
{
    internal Task<ProviderStateLease<TState>> AcquireAsync(string directory, Action? afterStage, CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return new ProviderStateLease<TState>(directory, ProviderStatePaths.Acquire(directory, FileName + ".lock"), this, afterStage);
        }
        catch (IOException) { throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
        catch (UnauthorizedAccessException) { throw new ProviderException(ProviderFailureKind.StorageUnavailable); }
    }, cancellationToken);
}
