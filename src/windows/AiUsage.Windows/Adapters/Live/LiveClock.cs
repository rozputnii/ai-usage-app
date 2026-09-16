using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

internal sealed class LiveClock : IClock, IDisposable
{
    private readonly Timer timer;
    public LiveClock() => timer = new(_ => Changed?.Invoke(this, EventArgs.Empty), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public TimeZoneInfo TimeZone => TimeZoneInfo.Local;
    public event EventHandler? Changed;
    public Task Delay(TimeSpan duration, CancellationToken cancellationToken) => Task.Delay(duration, cancellationToken);
    public void Dispose() => timer.Dispose();
}
