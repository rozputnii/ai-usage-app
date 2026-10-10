using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AiUsage.Core.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace AiUsage.Infrastructure.Diagnostics;

/// <summary>One bounded, best-effort local pipeline. Its queue contains projected JSON only.</summary>
public sealed class FileDiagnostics : IDiagnosticSink, IDisposable
{
    internal static readonly AsyncLocal<DiagnosticContext?> Operation = new();
    internal static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly DiagnosticOptions options;
    private readonly TimeProvider clock;
    private readonly object gate = new();
    private readonly LinkedList<Pending> pending = new();
    private readonly Dictionary<DiagnosticEvent, (DateTimeOffset First, DateTimeOffset Last, long Count)> suppressed = [];
    private readonly Dictionary<(DiagnosticEvent Event, string Signature), (Guid IncidentId, DateTimeOffset First, DateTimeOffset Last, long Count)> repeating = [];
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Exception, object> failures = new();
    private readonly SemaphoreSlim wake = new(0, 1);
    private readonly Task worker;
    private readonly ConcurrentQueue<string> breadcrumbs = new();
    private readonly string session = Guid.NewGuid().ToString("N");
    private readonly object environment;
    private readonly Dictionary<string, (DateTimeOffset At, Logger Logger, long Bytes)> writers = [];
    private int queuedBytes;
    private int stopped;
    private int fatal;
    private long sequence;
    private long lost;
    private long reportedLoss;
    private int writeFailed;
    private string? marker;
    private FileStream? markerLease;
    private TaskCompletionSource? flush;

    public FileDiagnostics(string ownedRoot, DiagnosticOptions? options = null, TimeProvider? clock = null, string mode = "development")
    {
        this.options = options ?? DiagnosticOptions.FromEnvironment();
        this.clock = clock ?? TimeProvider.System;
        DirectoryPath = DiagnosticFiles.Folder(ownedRoot);
        environment = new
        {
            appVersion = typeof(FileDiagnostics).Assembly.GetName().Version?.ToString(),
            build = typeof(FileDiagnostics).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion,
            runtime = Environment.Version.ToString(), os = Environment.OSVersion.Version.ToString(),
            windowsAppSdkAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Microsoft.WinUI")?.GetName().Version?.ToString(),
            mode = mode is "packaged" or "demo" or "console" ? mode : "development"
        };
        worker = Task.Run(WorkAsync);
        Event(DiagnosticEvent.SessionStarted, DiagnosticSeverity.Information, context: environment);
    }

    public string DirectoryPath { get; }
    public long LostRecords => Interlocked.Read(ref lost);
    internal bool CaptureBodies => options.CaptureBodies;
    public IDiagnosticOperation Begin(DiagnosticOperation operation, Guid? accountReference = null) => new DiagnosticScope(this, operation, accountReference);
    public void RecordDuration(DiagnosticEvent eventCode, double durationMilliseconds)
    {
        if (Enum.IsDefined(eventCode) && double.IsFinite(durationMilliseconds) && durationMilliseconds >= 0)
            Event(eventCode, DiagnosticSeverity.Information, duration: durationMilliseconds);
    }
    public void Signal(DiagnosticEvent eventCode, DiagnosticSeverity severity = DiagnosticSeverity.Information)
    {
        if (!Enum.IsDefined(eventCode) || !Enum.IsDefined(severity)) return;
        if (eventCode is DiagnosticEvent.BindingFailure or DiagnosticEvent.DispatchRejected)
        {
            lock (gate)
            {
                var now = clock.GetUtcNow();
                if (suppressed.TryGetValue(eventCode, out var previous))
                { suppressed[eventCode] = (previous.First, now, previous.Count + 1); return; }
                suppressed[eventCode] = (now, now, 0);
            }
        }
        Event(eventCode, severity);
    }

    public void Record(DiagnosticEvent eventCode, DiagnosticCategory category) => Record(eventCode, category,
        eventCode == DiagnosticEvent.BudgetStoreRecovered ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error);

    public void Record(DiagnosticEvent eventCode, DiagnosticCategory category, DiagnosticSeverity severity)
    {
        if (!Enum.IsDefined(eventCode) || !Enum.IsDefined(category) || !Enum.IsDefined(severity)) return;
        Event(eventCode, severity, context: new { category = category.ToString() });
    }

    public void Failure(DiagnosticEvent eventCode, Exception exception) => Failure(eventCode, exception, Guid.NewGuid());

    private void Failure(DiagnosticEvent eventCode, Exception exception, Guid incidentId)
    {
        try
        {
            if (!failures.TryAdd(exception, new object())) return;
            var kind = exception switch
            {
                Providers.ProviderException provider => provider.Kind,
                Providers.Codex.CodexException codex => codex.Kind,
                _ => (AiUsage.Core.Usage.ProviderFailureKind?)null
            };
            var severity = exception is OperationCanceledException ? DiagnosticSeverity.Information :
                kind is AiUsage.Core.Usage.ProviderFailureKind.AuthenticationRequired or AiUsage.Core.Usage.ProviderFailureKind.RateLimited or
                    AiUsage.Core.Usage.ProviderFailureKind.Timeout or AiUsage.Core.Usage.ProviderFailureKind.NetworkFailure ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;
            Event(eventCode, severity, exception: SafeException.Project(exception), context: new { incidentId, failureCategory = kind?.ToString() });
        }
        catch (Exception) { Interlocked.Increment(ref lost); }
    }

    /// <summary>
    /// T-056, T-062: a failure that recurs on every attempt (the tray redraw) gets one detailed record per signature
    /// (exception type and HResult, never the message); later attempts are only counted. The count is written with the
    /// detailed record's incidentId hourly, before an update install and on a fatal incident (the streak stays open),
    /// and on <see cref="Recovered"/> or stop (the streak closes, so a later failure is again logged in detail).
    /// </summary>
    public void RepeatedFailure(DiagnosticEvent eventCode, Exception exception)
    {
        var key = (eventCode, exception.GetType().FullName + ":" + exception.HResult);
        Guid incidentId;
        lock (gate)
        {
            var now = clock.GetUtcNow();
            if (repeating.TryGetValue(key, out var streak))
            {
                // After a checkpoint the next window starts at the first counted attempt.
                repeating[key] = (streak.IncidentId, streak.First == default ? now : streak.First, now, streak.Count + 1);
                return;
            }
            incidentId = Guid.NewGuid();
            repeating[key] = (incidentId, now, now, 0);
        }
        Failure(eventCode, exception, incidentId);
    }

    public void Recovered(DiagnosticEvent eventCode)
    {
        lock (gate)
        {
            foreach (var (key, streak) in repeating.ToArray())
            {
                if (key.Event != eventCode) continue;
                repeating.Remove(key);
                Summarize(key.Event, streak);
            }
        }
    }

    /// <summary>T-046 update outcomes: typed facts only, never feed bodies, URIs, paths or exception text.</summary>
    public void Update(DiagnosticEvent eventCode, UpdateFacts facts)
    {
        if (eventCode is not (DiagnosticEvent.UpdateCheckFailed or DiagnosticEvent.UpdateInstallStarted or
            DiagnosticEvent.UpdateInstallFailed or DiagnosticEvent.UpdateApplied)) return;
        // An install can force-close the app (ForceTargetAppShutdown) before stop writes pending counts.
        if (eventCode == DiagnosticEvent.UpdateInstallStarted) Checkpoint();
        var severity = eventCode switch
        {
            DiagnosticEvent.UpdateCheckFailed => DiagnosticSeverity.Warning,
            DiagnosticEvent.UpdateInstallFailed => DiagnosticSeverity.Error,
            _ => DiagnosticSeverity.Information
        };
        Event(eventCode, severity, context: new
        {
            errorCode = facts.ErrorCode is { } code ? $"0x{code:X8}" : null,
            trigger = facts.Automatic switch { true => "automatic", false => "manual", null => null },
            fromVersion = facts.From?.ToString(),
            toVersion = facts.To?.ToString(),
            consecutive = facts.Consecutive
        });
    }

    internal void Event(DiagnosticEvent id, DiagnosticSeverity severity, string? outcome = null, double? duration = null,
        SafeException? exception = null, object? context = null)
    {
        if (severity == DiagnosticSeverity.Debug && !options.TraceEnabled) return;
        try
        {
            var record = CreateRecord(id, severity, outcome, duration, exception, context);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(record, JsonOptions);
            if (bytes.Length > 64 * 1024) { Interlocked.Increment(ref lost); return; }
            breadcrumbs.Enqueue($"{record.Sequence}:{record.EventId}:{record.OperationId}");
            while (breadcrumbs.Count > 32) breadcrumbs.TryDequeue(out _);
            Enqueue(new(severity == DiagnosticSeverity.Debug ? "trace" : "application", record.Timestamp, bytes, severity));
        }
        catch (Exception) { Interlocked.Increment(ref lost); }
    }

    private DiagnosticEventRecord CreateRecord(DiagnosticEvent id, DiagnosticSeverity severity, string? outcome = null,
        double? duration = null, SafeException? exception = null, object? context = null)
    {
        var operation = Operation.Value;
        return new(1, clock.GetUtcNow(), severity.ToString(), id.ToString(), "AiUsage", Environment.ProcessId, session,
            Interlocked.Increment(ref sequence), operation?.Id, operation?.Parent, operation?.Operation, outcome, duration, exception, Context: context, AccountReference: operation?.AccountReference);
    }

    public void Fatal(DiagnosticEvent id, Exception? error, bool terminating)
    {
        if (Interlocked.Exchange(ref fatal, 1) != 0) return;
        try
        {
            var record = CreateRecord(id, DiagnosticSeverity.Critical, exception: error is null ? null : SafeException.Project(error),
                context: new { environment, breadcrumbs = breadcrumbs.ToArray() }) with
            { Terminating = terminating, ThreadId = Environment.CurrentManagedThreadId, LostRecords = LostRecords };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(record, JsonOptions);
            if (bytes.Length > 256 * 1024) throw new InvalidDataException("Critical record exceeds limit.");
            // No queue, UI/DI dependency or ordinary-writer lock. Each incident has a unique file.
            PrepareFolder();
            DiagnosticFiles.Prune(DirectoryPath, record.Timestamp, options, "critical", bytes.Length + 1, onlyReservedKind: true);
            var path = DiagnosticFiles.NewName(DirectoryPath, "critical", record.Timestamp, "jsonl");
            DiagnosticFiles.Check(path);
            using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            file.Write(bytes);
            file.WriteByte((byte)'\n');
            file.Flush(flushToDisk: true);
        }
        catch (Exception) { Interlocked.Increment(ref lost); }
        // A single bounded drain; a stalled kernel write itself cannot be cancelled.
        try
        {
            // Acquiring the ordinary queue lock must not hold this fatal thread indefinitely.
            // The outer deadline also bounds a worker that cannot acquire it. Pending summaries join the same drain.
            Task.Run(() => { FlushSuppressed(force: true); return FlushAsync(TimeSpan.FromSeconds(2)); }).Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception) { Interlocked.Increment(ref lost); }
    }

    internal bool Capture(byte[] sanitized, DateTimeOffset at, Guid captureId)
    {
        if (sanitized.Length > 8 * 1024 * 1024) { Interlocked.Increment(ref lost); return false; }
        return Enqueue(new("response", at, sanitized, DiagnosticSeverity.Information, captureId));
    }

    private bool Enqueue(Pending item)
    {
        lock (gate)
        {
            if (stopped != 0) { Interlocked.Increment(ref lost); return false; }
            if (item.Bytes.Length > options.QueueBytes) { Interlocked.Increment(ref lost); return false; }
            while (pending.Count >= options.QueueEntries || queuedBytes + item.Bytes.Length > options.QueueBytes)
            {
                var candidate = pending.First;
                while (candidate is not null && candidate.Value.Kind != "trace") candidate = candidate.Next;
                if (candidate is null && item.Severity < DiagnosticSeverity.Error) { Interlocked.Increment(ref lost); return false; }
                candidate ??= pending.First!;
                var dropped = candidate.Value;
                pending.Remove(candidate);
                queuedBytes -= dropped.Bytes.Length;
                Interlocked.Increment(ref lost);
            }
            pending.AddLast(item);
            queuedBytes += item.Bytes.Length;
        }
        Signal();
        return true;
    }

    private void Signal() { try { if (wake.CurrentCount == 0) wake.Release(); } catch (SemaphoreFullException) { } }

    public async Task<bool> FlushAsync(TimeSpan? deadline = null)
    {
        Task task;
        lock (gate) { flush ??= new(TaskCreationOptions.RunContinuationsAsynchronously); task = flush.Task; }
        Signal();
        try { await task.WaitAsync(deadline ?? TimeSpan.FromSeconds(2)).ConfigureAwait(false); return true; }
        catch (TimeoutException) { return false; }
    }

    private async Task WorkAsync()
    {
        TryInitialize();
        var sweep = clock.GetTimestamp();
        do
        {
            await wake.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            FlushSuppressed();
            while (true)
            {
                Pending? item;
                lock (gate)
                {
                    item = pending.First?.Value;
                    if (item is not null) { pending.RemoveFirst(); queuedBytes -= item.Bytes.Length; }
                }
                if (item is null) break;
                try { Write(item); }
                catch (Exception) { Interlocked.Increment(ref lost); CloseWriters(); }
            }
            if (clock.GetElapsedTime(sweep) >= TimeSpan.FromHours(1))
            {
                Checkpoint();
                try { CloseWriters(); DiagnosticFiles.Prune(DirectoryPath, clock.GetUtcNow(), options); }
                catch (Exception) { Interlocked.Increment(ref lost); }
                sweep = clock.GetTimestamp();
            }
            var losses = LostRecords;
            if (losses != reportedLoss)
            {
                try
                {
                    var health = CreateRecord(DiagnosticEvent.LoggerHealth, DiagnosticSeverity.Warning, context: new { lostRecords = losses });
                    Write(new("application", health.Timestamp, JsonSerializer.SerializeToUtf8Bytes(health, JsonOptions), DiagnosticSeverity.Warning));
                    reportedLoss = losses;
                }
                catch (Exception) { /* Retry one fixed health summary next tick, never recursively log it. */ }
            }
            TaskCompletionSource? completed;
            lock (gate) { completed = pending.Count == 0 ? flush : null; if (completed is not null) flush = null; }
            // Unbuffered Serilog file output flushes each event to the stream (fatal additionally forces disk).
            completed?.TrySetResult();
        } while (Volatile.Read(ref stopped) == 0 || pending.Count > 0);
        CloseWriters();
        markerLease?.Dispose();
        if (fatal == 0 && marker is not null)
        {
            try { DiagnosticFiles.Check(marker); File.Delete(marker); }
            catch (Exception) { Interlocked.Increment(ref lost); }
        }
    }

    private void FlushSuppressed(bool force = false)
    {
        lock (gate)
        {
            foreach (var (id, summary) in suppressed.ToArray())
            {
                if (!force && stopped == 0 && clock.GetUtcNow() - summary.First < TimeSpan.FromSeconds(10)) continue;
                suppressed.Remove(id);
                Summarize(id, summary);
            }
            if (force) Checkpoint();
        }
    }

    /// <summary>Writes every pending repeated-failure count and keeps its streak and incidentId open.</summary>
    private void Checkpoint()
    {
        lock (gate)
        {
            foreach (var (key, streak) in repeating.ToArray())
            {
                if (streak.Count == 0) continue;
                Summarize(key.Event, streak);
                repeating[key] = (streak.IncidentId, default, default, 0);
            }
        }
    }

    private void Summarize(DiagnosticEvent id, (DateTimeOffset First, DateTimeOffset Last, long Count) summary)
    {
        if (summary.Count > 0) Event(id, DiagnosticSeverity.Warning,
            context: new { suppressedCount = summary.Count, firstAt = summary.First, lastAt = summary.Last });
    }

    private void Summarize(DiagnosticEvent id, (Guid IncidentId, DateTimeOffset First, DateTimeOffset Last, long Count) streak)
    {
        if (streak.Count > 0) Event(id, DiagnosticSeverity.Warning,
            context: new { incidentId = streak.IncidentId, suppressedCount = streak.Count, firstAt = streak.First, lastAt = streak.Last });
    }

    private void TryInitialize()
    {
        try
        {
            PrepareFolder();
            DiagnosticFiles.Prune(DirectoryPath, clock.GetUtcNow(), options);
            foreach (var path in DiagnosticFiles.Owned(DirectoryPath).Where(p => p.EndsWith(".open", StringComparison.Ordinal)))
            {
                DiagnosticFiles.Check(path);
                // An exclusive lease distinguishes another live process from an incomplete earlier run.
                try { using var lease = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException) { continue; }
                Event(DiagnosticEvent.PreviousExitUnknown, DiagnosticSeverity.Warning);
                File.Delete(path);
            }
            marker = DiagnosticFiles.NewName(DirectoryPath, "session", clock.GetUtcNow(), "open", session);
            DiagnosticFiles.Check(marker);
            markerLease = new FileStream(marker, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            markerLease.WriteByte((byte)'1');
            markerLease.Flush(true);
            // Remove only the recognized legacy fixed-code file; its former events are not relabelled with a fresh age.
            var legacy = Path.Combine(Path.GetDirectoryName(DirectoryPath)!, "diagnostics.v1.log");
            DiagnosticFiles.Check(legacy);
            File.Delete(legacy);
        }
        catch (Exception) { Interlocked.Increment(ref lost); }
    }

    private void PrepareFolder() { DiagnosticFiles.Check(DirectoryPath); Directory.CreateDirectory(DirectoryPath); DiagnosticFiles.Check(DirectoryPath); }

    private void Write(Pending item)
    {
        if (DiagnosticFiles.Expired(item.Kind, item.At, clock.GetUtcNow())) { Interlocked.Increment(ref lost); return; }
        if (item.Kind == "response")
        {
            PrepareFolder();
            DiagnosticFiles.Prune(DirectoryPath, clock.GetUtcNow(), options, "response", item.Bytes.Length, onlyReservedKind: true);
            var path = DiagnosticFiles.NewName(DirectoryPath, "response", item.At, "json", item.CaptureId!.Value.ToString("N"));
            var stage = Path.ChangeExtension(path, "stage");
            DiagnosticFiles.Check(path); DiagnosticFiles.Check(stage);
            using (var file = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { using var output = options.WrapOutput(file); output.Write(item.Bytes); output.Flush(); }
            File.Move(stage, path);
            var committed = CreateRecord(DiagnosticEvent.CapturePersisted, DiagnosticSeverity.Information,
                context: new { captureId = item.CaptureId, persisted = true });
            Write(new("application", committed.Timestamp, JsonSerializer.SerializeToUtf8Bytes(committed, JsonOptions), DiagnosticSeverity.Information));
            return;
        }
        if (writers.TryGetValue(item.Kind, out var existing) &&
            (existing.At.UtcDateTime.Date != item.At.UtcDateTime.Date || item.At < existing.At || existing.Bytes + item.Bytes.Length + 1 > 8 * 1024 * 1024 ||
             DiagnosticFiles.Expired(item.Kind, existing.At, clock.GetUtcNow())))
        { existing.Logger.Dispose(); writers.Remove(item.Kind); }
        if (!writers.TryGetValue(item.Kind, out var writer))
        {
            PrepareFolder();
            // Reserve one full roll before opening it. Writes within that roll need no rescans;
            // other processes see the file, and each writer keeps its own unique, bounded roll.
            DiagnosticFiles.Prune(DirectoryPath, clock.GetUtcNow(), options, item.Kind, 8 * 1024 * 1024 + 64 * 1024, onlyReservedKind: true);
            var path = DiagnosticFiles.NewName(DirectoryPath, item.Kind, item.At, "jsonl");
            DiagnosticFiles.Check(path);
            var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Fallible(sink => sink.File(new ProjectedFormatter(), path,
                fileSizeLimitBytes: 8 * 1024 * 1024, rollOnFileSizeLimit: true, retainedFileCountLimit: null,
                buffered: false, flushToDiskInterval: TimeSpan.FromSeconds(1), hooks: new CheckedFileHooks(options.WrapOutput)), new FailureListener(this)).CreateLogger();
            writer = (item.At, logger, 0);
            writers.Add(item.Kind, writer);
        }
        writer.Logger.Information("{ProjectedRecord}", Encoding.UTF8.GetString(item.Bytes));
        // A partial failed line must never absorb the next successful record on recovery.
        if (Interlocked.Exchange(ref writeFailed, 0) != 0) { CloseWriters(); return; }
        writers[item.Kind] = (writer.At, writer.Logger, writer.Bytes + item.Bytes.Length + 1);
    }

    private void CloseWriters()
    {
        foreach (var item in writers.Values) { try { item.Logger.Dispose(); } catch (Exception) { Interlocked.Increment(ref lost); } }
        writers.Clear();
    }

    public async Task<string> PreviewAsync()
    {
        await FlushAsync().ConfigureAwait(false);
        return await Task.Run(() =>
        {
            try
            {
                var lines = new Queue<string>();
                foreach (var path in DiagnosticFiles.Owned(DirectoryPath).Order(StringComparer.Ordinal))
                {
                    DiagnosticFiles.Identify(path, out var kind, out var at);
                    if (kind is not ("application" or "critical") || DiagnosticFiles.Expired(kind, at, clock.GetUtcNow())) continue;
                    DiagnosticFiles.Check(path);
                    using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (file.Length > 9 * 1024 * 1024) continue;
                    using var reader = new StreamReader(file);
                    while (reader.ReadLine() is { } line)
                    {
                        if (line.Length > 256 * 1024) continue;
                        try
                        {
                            using var doc = JsonDocument.Parse(line);
                            var value = doc.RootElement;
                            if (!value.TryGetProperty("eventId", out var id) || !Enum.TryParse<DiagnosticEvent>(id.GetString(), out var code) || !Enum.IsDefined(code)) continue;
                            if (!value.TryGetProperty("timestamp", out var time) || !time.TryGetDateTimeOffset(out var timestamp) || DiagnosticFiles.Expired(kind, timestamp, clock.GetUtcNow())) continue;
                            // Preview rebuilds fixed fields; never returns arbitrary stored text or provider bodies.
                            lines.Enqueue($"{timestamp:O} {code}");
                            while (lines.Count > 80) lines.Dequeue();
                        }
                        catch (JsonException) { /* A partial last line does not hide earlier complete records. */ }
                    }
                }
                return string.Join(Environment.NewLine, lines);
            }
            catch (Exception) { return "Diagnostic files are unavailable."; }
        }).ConfigureAwait(false);
    }

    public void Dispose()
    {
        try { StopAsync().Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { Interlocked.Increment(ref lost); }
        GC.SuppressFinalize(this);
    }

    /// <summary>Confirms writer termination before owned-data deletion. Unlike bounded process disposal, this must be awaited.</summary>
    public Task StopAsync()
    {
        lock (gate)
        {
            if (stopped == 0)
            {
                FlushSuppressed(force: true);
                repeating.Clear();
                Event(DiagnosticEvent.SessionExited, DiagnosticSeverity.Information);
                Volatile.Write(ref stopped, 1);
                Signal();
            }
        }
        return worker;
    }

    private sealed record Pending(string Kind, DateTimeOffset At, byte[] Bytes, DiagnosticSeverity Severity, Guid? CaptureId = null);
    private sealed class ProjectedFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent.Properties.TryGetValue("ProjectedRecord", out var value) && value is ScalarValue { Value: string json }) output.WriteLine(json);
        }
    }
    private sealed class CheckedFileHooks(Func<Stream, Stream> wrap) : Serilog.Sinks.File.FileLifecycleHooks
    {
        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        { DiagnosticFiles.Check(path); return wrap(underlyingStream); }
    }
    private sealed class FailureListener(FileDiagnostics owner) : ILoggingFailureListener
    {
        public void OnLoggingFailed(object sender, LoggingFailureKind kind, string message, IReadOnlyCollection<LogEvent>? events, Exception? exception)
        {
            Interlocked.Add(ref owner.lost, events?.Count ?? 1);
            Interlocked.Exchange(ref owner.writeFailed, 1);
        }
    }
}

/// <summary>The only values an update record carries; versions are parsed, codes are numbers.</summary>
public sealed record UpdateFacts(int? ErrorCode = null, bool? Automatic = null, Version? From = null, Version? To = null, int? Consecutive = null);
