namespace AiUsage.Infrastructure.Tests;

/// <summary>Deterministic time with timers that fire only when a test advances the clock.</summary>
internal sealed class ManualClock(DateTimeOffset start) : TimeProvider
{
    private readonly object sync = new();
    private readonly List<Timer> timers = [];
    private DateTimeOffset now = start;

    public override DateTimeOffset GetUtcNow() { lock (sync) return now; }

    public void Advance(TimeSpan by)
    {
        List<Timer> due;
        lock (sync)
        {
            now += by;
            due = timers.Where(timer => timer.DueAt <= now).ToList();
        }
        foreach (var timer in due) timer.Fire();
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new Timer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    private sealed class Timer(ManualClock owner, TimerCallback callback, object? state) : ITimer
    {
        internal DateTimeOffset DueAt { get; private set; } = DateTimeOffset.MaxValue;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (owner.sync)
            {
                owner.timers.Remove(this);
                if (dueTime == Timeout.InfiniteTimeSpan) { DueAt = DateTimeOffset.MaxValue; return true; }
                DueAt = owner.now + dueTime;
                owner.timers.Add(this);
            }
            if (dueTime == TimeSpan.Zero) Fire();
            return true;
        }

        internal void Fire()
        {
            lock (owner.sync)
            {
                if (!owner.timers.Remove(this)) return;
                DueAt = DateTimeOffset.MaxValue;
            }
            callback(state);
        }

        public void Dispose() { lock (owner.sync) owner.timers.Remove(this); }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
