using System.ComponentModel;
using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

internal sealed class LiveClock : IClock, IDisposable
{
    private ITimer? timer;
    private readonly TimeProvider time;
    private readonly IMotionSettings motion;
    private readonly IUiDispatcher dispatcher;
    private bool visible;
    private bool disposed;
    private int generation;

    // Construct, observe visibility and dispose on the UI thread. Timer callbacks only post to it.
    public LiveClock(IMotionSettings motion, IUiDispatcher dispatcher, TimeProvider? timeProvider = null)
    {
        this.motion = motion;
        this.dispatcher = dispatcher;
        time = timeProvider ?? TimeProvider.System;
        visible = motion.WindowVisible;
        if (visible)
            StartTimer();
        motion.PropertyChanged += OnMotionChanged;
    }
    public DateTimeOffset UtcNow => time.GetUtcNow();
    public TimeZoneInfo TimeZone => time.LocalTimeZone;
    public event EventHandler? Changed;
    public Task Delay(TimeSpan duration, CancellationToken cancellationToken) => Task.Delay(duration, cancellationToken);
    private void StartTimer()
    {
        var scheduledGeneration = generation;
        timer = time.CreateTimer(_ => dispatcher.Post(() =>
        {
            if (!disposed && visible && scheduledGeneration == generation)
                Changed?.Invoke(this, EventArgs.Empty);
        }), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private void OnMotionChanged(object? sender, PropertyChangedEventArgs e)
    {
        // MotionSettings also reports animation changes; only an actual visibility transition matters.
        if (disposed || visible == motion.WindowVisible)
            return;
        visible = motion.WindowVisible;
        generation++;
        timer?.Dispose();
        timer = null;
        if (visible)
        {
            StartTimer();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        generation++;
        motion.PropertyChanged -= OnMotionChanged;
        timer?.Dispose();
    }
}
