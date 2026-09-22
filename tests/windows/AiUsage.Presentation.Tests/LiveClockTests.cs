using AiUsage.Adapters.Live;
using AiUsage.Features.Overview;
using AiUsage.Features.Presentation;
using AiUsage.Features.Tray;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class LiveClockTests
{
    [Fact]
    public void HiddenClockStopsSchedulingAndRestoreReappliesExactlyOnce()
    {
        var motion = new TestMotion();
        var time = new ManualTime();
        var dispatcher = new QueuedDispatcher();
        using var clock = new LiveClock(motion, dispatcher, time);
        var changes = 0;
        clock.Changed += (_, _) => changes++;
        time.Timer.Fire();
        dispatcher.Drain();
        Assert.Equal(1, changes);

        motion.WindowVisible = false;
        motion.Raise();
        Assert.True(time.Timer.Disposed);
        time.Timer.Fire(); // A timer callback already queued before Dispose may still arrive.
        dispatcher.Drain();
        Assert.Equal(1, changes);
        time.Now += TimeSpan.FromHours(1);
        motion.WindowVisible = true;
        motion.Raise();
        motion.Raise(); // Duplicate and animation-setting notifications are not restores.
        dispatcher.Drain();
        Assert.Equal(2, changes);
        Assert.Equal(time.Now, clock.UtcNow);
        Assert.Equal(TimeSpan.FromSeconds(30), time.Timer.DueTime);
        time.Timer.Fire();
        dispatcher.Drain();
        Assert.Equal(3, changes);
    }

    [Fact]
    public void InitiallyHiddenClockDoesNotStartUntilVisibleRegardlessOfReducedMotion()
    {
        var motion = new TestMotion { WindowVisible = false, ReducedMotion = true };
        var time = new ManualTime();
        using var clock = new LiveClock(motion, new TestDispatcher(), time);
        Assert.Null(time.Timer);
        var changes = 0;
        clock.Changed += (_, _) => changes++;
        motion.WindowVisible = true;
        motion.Raise();
        Assert.Equal(1, changes);
        Assert.NotNull(time.Timer);
        time.Timer.Fire();
        Assert.Equal(2, changes);
    }

    [Fact]
    public void QueuedTickCannotReapplyAfterHideRestoreOrDisposal()
    {
        var motion = new TestMotion();
        var time = new ManualTime();
        var dispatcher = new QueuedDispatcher();
        using var clock = new LiveClock(motion, dispatcher, time);
        var changes = 0;
        clock.Changed += (_, _) => changes++;
        time.Timer.Fire();
        Assert.Equal(0, changes);
        var oldTimer = time.Timer;
        motion.WindowVisible = false;
        motion.Raise();
        motion.WindowVisible = true;
        motion.Raise();
        oldTimer.Fire(); // A retired callback can begin after restore, not just be queued beforehand.
        dispatcher.Drain();
        Assert.Equal(1, changes);

        time.Timer.Fire();
        clock.Dispose();
        clock.Dispose();
        dispatcher.Drain();
        motion.WindowVisible = false;
        motion.Raise();
        motion.WindowVisible = true;
        motion.Raise();
        time.Timer.Fire();
        dispatcher.Drain();
        Assert.Equal(1, changes);
        Assert.True(time.Timer.Disposed);
    }

    [Fact]
    public void RestoreUpdatesRealRelativeTextAndHiddenSnapshotsStillReachTray()
    {
        using var host = new TestHost();
        var time = new ManualTime { Now = host.Clock.UtcNow };
        using var clock = new LiveClock(host.Motion, host.Dispatcher, time);
        var context = new PresentationContext(host.Usage, host.Dispatcher, clock, host.Text,
            host.Announcer, host.Navigation, host.Dialogs, host.Motion);
        using var overview = new OverviewViewModel(context, host.History, host.Preferences);
        using var tray = new TrayViewModel(context, host.Lifetime, () => Task.CompletedTask, () => Task.CompletedTask);
        var before = overview.SummaryReset;
        host.Motion.WindowVisible = false;
        host.Motion.Raise();
        time.Now += TimeSpan.FromHours(1);
        time.Timer.Fire();
        Assert.Equal(before, overview.SummaryReset);
        host.State.World.Accounts[0].Label = "Changed while hidden";
        host.State.Publish();
        Assert.Contains(tray.Rows, row => row.Label == "Changed while hidden");
        var afterSnapshot = overview.SummaryReset;
        time.Now += TimeSpan.FromHours(1);
        host.Motion.WindowVisible = true;
        host.Motion.Raise();
        Assert.NotEqual(afterSnapshot, overview.SummaryReset);
    }

    [Fact]
    public void OpeningOrTickingTrayRefreshesOnlyTrayWhileMainClockIsStopped()
    {
        using var host = new TestHost();
        var time = new ManualTime { Now = host.Clock.UtcNow };
        using var clock = new LiveClock(host.Motion, host.Dispatcher, time);
        var context = new PresentationContext(host.Usage, host.Dispatcher, clock, host.Text,
            host.Announcer, host.Navigation, host.Dialogs, host.Motion);
        using var overview = new OverviewViewModel(context, host.History, host.Preferences);
        using var tray = new TrayViewModel(context, host.Lifetime, () => Task.CompletedTask, () => Task.CompletedTask);
        var overviewBefore = overview.SummaryReset;
        var row = tray.Rows.Single(r => r.Id == "demo-claude-1");
        var quota = row.ValueText;
        host.Motion.WindowVisible = false;
        host.Motion.Raise();
        time.Now += TimeSpan.FromHours(1);
        tray.RefreshTime();
        Assert.Equal("Resets in 1 h 14 m · Sep 15, 2:14 PM", row.SubText);
        Assert.Equal(quota, row.ValueText);
        Assert.Equal(overviewBefore, overview.SummaryReset);
        time.Now += TimeSpan.FromHours(2);
        tray.RefreshTime();
        Assert.DoesNotContain("Resets in", row.SubText, StringComparison.Ordinal);
        Assert.Equal(quota, row.ValueText); // Crossing reset time must never invent new quota.
        tray.Dispose();
        var disposedText = row.SubText;
        time.Now += TimeSpan.FromHours(1);
        tray.RefreshTime();
        Assert.Equal(disposedText, row.SubText);
    }

    internal sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public ManualTimer Timer { get; private set; } = null!;
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            Timer = new ManualTimer(callback, state, dueTime);
    }

    internal sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        public TimeSpan DueTime { get; private set; } = dueTime;
        public bool Disposed { get; private set; }
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            DueTime = dueTime;
            return true;
        }
        public void Fire() => callback(state);
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    private sealed class QueuedDispatcher : IUiDispatcher
    {
        private readonly Queue<Action> pending = new();
        public bool HasThreadAccess => true;
        public void Post(Action action) => pending.Enqueue(action);
        public void Drain()
        {
            while (pending.TryDequeue(out var action)) action();
        }
    }
}
