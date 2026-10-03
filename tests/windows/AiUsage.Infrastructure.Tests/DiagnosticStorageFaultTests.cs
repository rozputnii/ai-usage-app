using System.Text;
using System.Text.Json;
using AiUsage.Core.Diagnostics;
using AiUsage.Infrastructure.Diagnostics;
using Xunit;

namespace AiUsage.Infrastructure.Tests;

public sealed class DiagnosticStorageFaultTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "aiu-storage-fault-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedResponseWriteHasNoCommittedReferenceAndRecovers(bool accessDenied)
    {
        var failing = true;
        using var log = new FileDiagnostics(root, new DiagnosticOptions
        {
            WrapOutput = stream => new FaultStream(stream, bytes =>
            {
                if (!failing) return;
                if (accessDenied) throw new UnauthorizedAccessException("private-canary");
                stream.Write(bytes[..Math.Min(10, bytes.Length)]);
                throw new IOException("disk-full-canary", unchecked((int)0x80070070));
            })
        });
        Assert.True(await log.FlushAsync());
        var id = Guid.NewGuid();
        Assert.True(log.Capture(Encoding.UTF8.GetBytes("{\"safe\":true}"), DateTimeOffset.UtcNow, id));
        Assert.True(await log.FlushAsync());
        Assert.True(log.LostRecords > 0);
        Assert.Empty(Directory.GetFiles(log.DirectoryPath, "response-*.json"));
        Assert.Single(Directory.GetFiles(log.DirectoryPath, "response-*.stage"));
        failing = false;
        log.Signal(DiagnosticEvent.RecoveryCompleted);
        Assert.True(await log.FlushAsync());
        var preview = await log.PreviewAsync();
        Assert.Contains("LoggerHealth", preview);
        Assert.Contains("RecoveryCompleted", preview);
        Assert.DoesNotContain("CapturePersisted", preview);
        log.Dispose();
        DiagnosticFiles.DeleteOwned(root);
        Assert.Empty(Directory.GetFiles(log.DirectoryPath));
    }

    [Fact]
    public async Task SlowOrdinaryWriterCannotBlockCriticalCaptureAndFlushDeadline()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var log = new FileDiagnostics(root, new DiagnosticOptions
        {
            WrapOutput = stream => new FaultStream(stream, _ => { entered.Set(); release.Wait(TimeSpan.FromSeconds(15)); })
        });
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.False(await log.FlushAsync(TimeSpan.FromMilliseconds(50)));
            var fatal = Task.Run(() => log.Fatal(DiagnosticEvent.UnhandledFailure, new IOException("secret-canary"), true), TestContext.Current.CancellationToken);
            await fatal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var path = Assert.Single(Directory.GetFiles(log.DirectoryPath, "critical-*.jsonl"));
            using var record = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(record.RootElement.GetProperty("terminating").GetBoolean());
            Assert.DoesNotContain("secret-canary", record.RootElement.GetRawText());
        }
        finally { release.Set(); }
        Assert.True(await log.FlushAsync());
    }

    [Fact]
    public async Task ReentrantWriteFailureRemainsBoundedAndDoesNotReplaceOriginalFault()
    {
        FileDiagnostics? current = null;
        var armed = 0;
        using var log = new FileDiagnostics(root, new DiagnosticOptions
        {
            WrapOutput = stream => new FaultStream(stream, _ =>
            {
                if (Interlocked.Exchange(ref armed, 0) == 0) return;
                current!.Failure(DiagnosticEvent.OperationFailure, new IOException("nested-canary"));
                throw new IOException("writer-canary");
            })
        });
        current = log;
        Assert.True(await log.FlushAsync());
        armed = 1;
        log.Failure(DiagnosticEvent.OperationFailure, new InvalidOperationException("original-canary"));
        Assert.True(await log.FlushAsync());
        Assert.InRange(log.LostRecords, 1, 10);
        Assert.Contains("LoggerHealth", await log.PreviewAsync());
        var written = string.Join('\n', Directory.GetFiles(log.DirectoryPath, "*.jsonl").Select(FileDiagnosticsTests.ReadShared));
        Assert.DoesNotContain("canary", written);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    private delegate void BeforeWrite(ReadOnlySpan<byte> bytes);
    private sealed class FaultStream(Stream inner, BeforeWrite before) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer) { before(buffer); inner.Write(buffer); }
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }
}
