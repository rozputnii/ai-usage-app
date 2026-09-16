using AiUsage.Infrastructure.Persistence;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class PresentationPreferenceFileTests
{
    [Fact]
    public async Task CancelledWriteKeepsLastCommittedPreferencesAndUnrelatedData()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aiu-preferences-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new PresentationPreferenceFile(directory);
            var token = TestContext.Current.CancellationToken;
            await store.WriteAsync("first", token);
            await File.WriteAllTextAsync(Path.Combine(directory, "opaque"), "owned-by-someone-else", token);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.WriteAsync("second", cancellation.Token));
            Assert.Equal("first", await store.ReadAsync(token));
            Assert.Equal("owned-by-someone-else", await File.ReadAllTextAsync(Path.Combine(directory, "opaque"), token));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally { Directory.Delete(directory, true); }
    }
}
