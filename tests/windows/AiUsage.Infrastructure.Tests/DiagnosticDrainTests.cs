using AiUsage.Infrastructure.Diagnostics;
using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class DiagnosticDrainTests
{
    [Fact]
    public async Task DeletionDrainWaitsForBlockedWriterBeforeRemovingOwnedFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiu-drain-test-" + Guid.NewGuid().ToString("N"));
        using var clock = new BlockedClock();
        using var diagnostics = new FileDiagnostics(root, clock: clock);
        try
        {
            Assert.True(clock.Entered.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            var drain = diagnostics.StopAsync();
            Assert.False(drain.IsCompleted);
            clock.Release.Set();
            await drain.WaitAsync(TestContext.Current.CancellationToken);
            await new OwnedDataDeletion(root).RunAsync(true, TestContext.Current.CancellationToken);
            Assert.Empty(Directory.EnumerateFiles(diagnostics.DirectoryPath));
            await diagnostics.StopAsync();
            Assert.Empty(Directory.EnumerateFiles(diagnostics.DirectoryPath));
        }
        finally { clock.Release.Set(); await diagnostics.StopAsync(); Directory.Delete(root, true); }
    }

    private sealed class BlockedClock : TimeProvider, IDisposable
    {
        internal readonly ManualResetEventSlim Entered = new();
        internal readonly ManualResetEventSlim Release = new();
        public override long GetTimestamp() { Entered.Set(); Release.Wait(); return base.GetTimestamp(); }
        public void Dispose() { Entered.Dispose(); Release.Dispose(); }
    }
}
