using AiUsage.Core.Diagnostics;
using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

/// <summary>
/// D-099 automatic refresh. Each connected account is read again once its last reading is <see cref="Interval"/> old;
/// failed attempts back off by doubling up to <see cref="MaximumBackoff"/>. Accounts that need sign-in, recovery or a
/// fix no retry can make are left to the user (D-103). Ticks missed while the PC sleeps are not replayed and the PC is
/// never woken: the first tick after resume catches up. A reading nobody renewed for <see cref="StaleAfter"/> is shown
/// as stale rather than fresh.
/// </summary>
internal sealed class LiveAutoRefresh : IDisposable
{
    internal static readonly TimeSpan Tick = TimeSpan.FromMinutes(1);
    internal static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan MaximumBackoff = TimeSpan.FromMinutes(30);
    internal static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(15);

    private readonly LiveUsageSource source;
    private readonly TimeProvider time;
    private readonly IDiagnosticSink? diagnostics;
    private readonly object sync = new();
    private readonly Dictionary<string, Attempt> attempts = new(StringComparer.Ordinal);
    private ITimer? timer;
    private Task? pass;
    private bool disposed;

    public LiveAutoRefresh(LiveUsageSource source, TimeProvider? timeProvider = null, IDiagnosticSink? diagnostics = null)
    {
        this.source = source;
        time = timeProvider ?? TimeProvider.System;
        this.diagnostics = diagnostics;
    }

    public void Start()
    {
        lock (sync)
        {
            if (disposed || timer is not null) return;
            timer = time.CreateTimer(_ => OnTick(), null, Tick, Tick);
        }
    }

    private void OnTick()
    {
        lock (sync)
        {
            // A slow provider must not stack passes; the next tick after this one completes picks up what is due.
            if (disposed || pass is { IsCompleted: false }) return;
            pass = RunOnceAsync();
        }
    }

    /// <summary>One scheduling pass: mark aged readings, then refresh every account that is due, in parallel.</summary>
    internal async Task RunOnceAsync()
    {
        try
        {
            var now = time.GetUtcNow();
            source.ExpireReadings(now, StaleAfter);
            var due = source.Current.Accounts.Where(account => IsDue(account, now)).Select(account => account.Id).ToArray();
            var results = await Task.WhenAll(due.Select(source.RefreshInBackgroundAsync)).ConfigureAwait(false);
            lock (sync)
            {
                for (var i = 0; i < due.Length; i++)
                {
                    if (results[i].Status == CommandStatus.Conflict) continue; // Something else ran; it was not an attempt.
                    var failures = results[i].Status == CommandStatus.Succeeded ? 0 : attempts.GetValueOrDefault(due[i]).Failures + 1;
                    attempts[due[i]] = new(now, failures);
                }
            }
        }
        catch (Exception error)
        {
            diagnostics?.Record(DiagnosticEvent.OperationFailure, DiagnosticProjection.Category(error));
        }
    }

    private bool IsDue(AccountItem account, DateTimeOffset now)
    {
        if (account.Connection != ConnectionState.Connected || account.Operation != AccountOperation.Idle)
            return false;
        if (account.Failure is { Recoverable: false } || account.Failure is { RetryAt: { } retryAt } && retryAt > now)
            return false;
        Attempt attempt;
        lock (sync) attempt = attempts.GetValueOrDefault(account.Id);
        // Backoff applies only while the account still shows a failure; a later successful manual refresh ends it.
        var failing = account.Failure is not null;
        var wait = failing && attempt.Failures > 0 ? Backoff(attempt.Failures) : Interval;
        if (attempt.At is { } at && now - at < wait)
            return false;
        return failing || account.FetchedAt is not { } fetched || now - fetched >= Interval;
    }

    private static TimeSpan Backoff(int failures) =>
        TimeSpan.FromTicks(Math.Min(Interval.Ticks << Math.Min(failures, 3), MaximumBackoff.Ticks));

    public void Dispose()
    {
        lock (sync)
        {
            disposed = true;
            timer?.Dispose();
            timer = null;
        }
    }

    private readonly record struct Attempt(DateTimeOffset? At, int Failures);
}
