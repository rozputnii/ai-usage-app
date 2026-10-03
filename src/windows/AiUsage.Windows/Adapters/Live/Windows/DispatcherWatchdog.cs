using AiUsage.Core.Diagnostics;
using Microsoft.UI.Dispatching;
using System.Diagnostics;

namespace AiUsage.Composition;

/// <summary>One outstanding ping, one warning per stall. Sleep/debugger gaps restart observation.</summary>
internal sealed class DispatcherWatchdog : IDisposable
{
    private readonly DispatcherQueue queue;
    private readonly ApplicationDiagnostics diagnostics;
    private readonly Timer timer;
    private long previous = Environment.TickCount64;
    private long pending;
    private int warned;
    private int disposed;
    private int active = 1;
    public bool Active { set => Interlocked.Exchange(ref active, value ? 1 : 0); }

    internal DispatcherWatchdog(DispatcherQueue queue, ApplicationDiagnostics diagnostics)
    {
        this.queue = queue;
        this.diagnostics = diagnostics;
        timer = new Timer(_ => Check(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    private void Check()
    {
        if (Volatile.Read(ref disposed) != 0) return;
        var now = Environment.TickCount64;
        var gap = now - Interlocked.Exchange(ref previous, now);
        if (Debugger.IsAttached || gap > 6000 || Volatile.Read(ref active) == 0)
        {
            Interlocked.Exchange(ref pending, 0);
            Interlocked.Exchange(ref warned, 0);
            return;
        }
        var sent = Interlocked.Read(ref pending);
        if (sent != 0)
        {
            if (now - sent >= 10000 && Interlocked.Exchange(ref warned, 1) == 0)
                diagnostics.Signal(DiagnosticEvent.DispatcherStalled, DiagnosticSeverity.Warning);
            return;
        }
        Interlocked.Exchange(ref pending, now);
        if (!queue.TryEnqueue(() =>
        {
            var sentAt = Interlocked.Exchange(ref pending, 0);
            if (Volatile.Read(ref disposed) == 0 && Interlocked.Exchange(ref warned, 0) != 0)
                diagnostics.DispatcherRecovered(Math.Max(0, Environment.TickCount64 - sentAt));
        })) Interlocked.Exchange(ref pending, 0);
    }

    public void Dispose() { Interlocked.Exchange(ref disposed, 1); timer.Dispose(); }
}
