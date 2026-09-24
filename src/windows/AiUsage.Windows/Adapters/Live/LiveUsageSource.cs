using AiUsage.Core.Dashboard;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;
using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;

namespace AiUsage.Adapters.Live;

/// <summary>One existing app-owned slot per provider. Never owns or reads credential material.</summary>
internal sealed partial class LiveUsageSource : IUsageSource, IDisposable
{
    private readonly object sync = new();
    private readonly Dictionary<string, Entry> entries;
    private readonly List<Action<UiSnapshot>> subscribers = [];
    private bool stopped;
    private bool maintenanceBlocked;
    private Task? initialization;
    private Task? stopping;
    private UiSnapshot current;
    private readonly IDiagnosticSink? diagnostics;
    internal LivePreferenceStore? PreferenceStore { get; set; }
    private IReadOnlyDictionary<string, string> labels = new Dictionary<string, string>();
    private IReadOnlyDictionary<string, ExpansionPreference> expansions = new Dictionary<string, ExpansionPreference>();

    public ProviderCatalog Providers { get; }

    public LiveUsageSource(ProviderCatalog providers, Func<string, IProviderSession> resolveSession, IDiagnosticSink? diagnostics = null)
        : this(providers.All.ToDictionary(d => d.ProviderId, d => resolveSession(d.ProviderId), StringComparer.Ordinal), diagnostics, providers) { }

    public LiveUsageSource(IReadOnlyDictionary<string, IProviderSession> sessions, IDiagnosticSink? diagnostics = null, ProviderCatalog? providers = null)
    {
        Providers = providers ?? ProviderCatalog.Default;
        this.diagnostics = diagnostics;
        entries = sessions.ToDictionary(pair => pair.Key, pair => new Entry(pair.Value));
        current = new(0, UiMode.Live, DateTimeOffset.UtcNow, [], Capabilities(),
            new([], [], [], false, false, false, [Preferences.DefaultGlobalRule]),
            new(typeof(LiveUsageSource).Assembly.GetName().Version?.ToString() ?? "", "", HealthState.Idle,
                UpdateState.Unsupported, RecoveryState.None, CompatibilityState.Normal)) { Loaded = false };
    }

    public UiSnapshot Current { get { lock (sync) return current; } }
    public IDisposable Subscribe(Action<UiSnapshot> onSnapshot)
    {
        lock (sync) subscribers.Add(onSnapshot);
        return new Subscription(() => { lock (sync) subscribers.Remove(onSnapshot); });
    }

    private static CapabilityItem[] Capabilities() =>
        Enum.GetValues<UiCommandKind>().Select(kind => new CapabilityItem(kind.ToString(), null,
            kind is UiCommandKind.Connect or UiCommandKind.RefreshAccount or UiCommandKind.RefreshAll or
                UiCommandKind.Disconnect or UiCommandKind.CancelOperation or UiCommandKind.SetPreference or
                UiCommandKind.ResetSettings or UiCommandKind.Rename or UiCommandKind.Reorder or UiCommandKind.SetVisibility or UiCommandKind.SetExpansion
                ? Availability.Available : Availability.Unavailable,
            null, CapabilityOrigin.Existing)).Append(new(CapabilityKeys.ViewHistory, null, Availability.Available, null, CapabilityOrigin.Existing)).ToArray();

    public Task InitializeAsync()
    {
        lock (sync) return maintenanceBlocked ? Task.CompletedTask : initialization ??= InitializeCoreAsync();
    }
    private async Task InitializeCoreAsync()
    {
        await Task.Yield();
        await Task.WhenAll(entries.Select(pair => RunAsync(pair.Key, AccountOperation.Loading,
            (workflow, token) => workflow.LoadAsync(state => { Update(pair.Key, state); return Task.CompletedTask; }, token), default)));
        Notification notification;
        lock (sync) notification = Publish(current with { Loaded = true });
        notification.Deliver();
    }

    public Task<UiCommandResult> ExecuteAsync(UiCommand command, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (stopped) return Task.FromResult(UiCommandResult.Cancelled);
            if (maintenanceBlocked) return Task.FromResult(UiCommandResult.Failed());
            if (!QuotaRules.IsAvailable(current, command.Kind.ToString(), command.TargetId))
                return Task.FromResult(UiCommandResult.Unsupported);
            if (command.ExpectedRevision != current.Revision) return Task.FromResult(UiCommandResult.Conflict);
            if (command.Kind is UiCommandKind.Rename or UiCommandKind.Reorder or UiCommandKind.SetVisibility or UiCommandKind.SetExpansion)
                return PreferenceStore?.ExecuteAsync(command, cancellationToken) ?? Task.FromResult(UiCommandResult.Unsupported);
            if (command.Kind == UiCommandKind.RefreshAll) return RefreshAllAsync(cancellationToken);
            if (command.TargetId is not { } id || !entries.TryGetValue(id, out var entry))
                return Task.FromResult(UiCommandResult.Unsupported);
            if (command.Kind == UiCommandKind.CancelOperation)
            {
                entry.Cancellation?.Cancel();
                return Task.FromResult(UiCommandResult.Succeeded);
            }
            return command.Kind switch
            {
                UiCommandKind.RefreshAccount when entry.Session.HasStoredGrant => RunAsync(id, AccountOperation.Refreshing, (workflow, token) => workflow.RefreshAsync(token), cancellationToken),
                UiCommandKind.Disconnect => RunAsync(id, AccountOperation.Disconnecting, (workflow, token) => workflow.DisconnectAsync(token), cancellationToken),
                _ => Task.FromResult(UiCommandResult.Unsupported)
            };
        }
    }

    private async Task<UiCommandResult> RefreshAllAsync(CancellationToken token)
    {
        var ids = entries.Where(pair => pair.Value.Session.HasStoredGrant).Select(pair => pair.Key).ToArray();
        var results = await Task.WhenAll(ids.Select(id => RunAsync(id, AccountOperation.Refreshing, (workflow, ct) => workflow.RefreshAsync(ct), token)));
        Notification notification;
        lock (sync) notification = Publish(current with { LastRefreshAll = new(results.Count(r => r.Status == CommandStatus.Succeeded),
            ids.Where((_, i) => results[i].Status != CommandStatus.Succeeded).ToArray(), DateTimeOffset.UtcNow) });
        notification.Deliver();
        return results.Any(r => r.Status == CommandStatus.Cancelled) ? UiCommandResult.Cancelled :
            results.All(r => r.Status == CommandStatus.Succeeded) ? UiCommandResult.Succeeded : UiCommandResult.Failed();
    }

    internal Task<UiCommandResult> ConnectAsync(string id, Action<Uri> openBrowser, CancellationToken token) =>
        RunAsync(id, AccountOperation.Loading, (workflow, ct) => workflow.ConnectAsync(openBrowser, ct), token);
    internal Task<UiCommandResult> ConnectWithChallengeAsync(string id, Action<AuthorizationChallenge> authorize, CancellationToken token) =>
        RunAsync(id, AccountOperation.Loading, (workflow, ct) => workflow.ConnectWithChallengeAsync(authorize, ct), token);
    internal bool TrySubmitCode(string id, string code)
    {
        lock (sync) return !stopped && entries.TryGetValue(id, out var entry) && entry.Workflow.TrySubmitCode(code);
    }

    private Task<UiCommandResult> RunAsync(string id, AccountOperation operation,
        Func<DashboardWorkflow, CancellationToken, Task<ProviderSessionState>> run, CancellationToken token)
    {
        lock (sync)
        {
            if (stopped) return Task.FromResult(UiCommandResult.Cancelled);
            if (maintenanceBlocked) return Task.FromResult(UiCommandResult.Failed());
            if (!entries.TryGetValue(id, out var entry)) return Task.FromResult(UiCommandResult.Unsupported);
            if (entry.HistoryActive && entry.Pending is { IsCompleted: false } history)
            {
                entry.Cancellation?.Cancel();
                return RunAfterHistoryAsync(history, id, operation, run, token);
            }
            if (entry.Pending is { IsCompleted: false }) return Task.FromResult(UiCommandResult.Conflict);
            entry.Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            return entry.Pending = ExecuteCoreAsync(id, entry, operation, run, entry.Cancellation);
        }
    }

    private async Task<UiCommandResult> ExecuteCoreAsync(string id, Entry entry, AccountOperation operation,
        Func<DashboardWorkflow, CancellationToken, Task<ProviderSessionState>> run, CancellationTokenSource cancellation)
    {
        await Task.Yield();
        Notification started;
        lock (sync)
        {
            var account = current.Accounts.FirstOrDefault(a => a.Id == id) ?? LiveMapping.Map(id, entry.Session.State, entry.Session.HasStoredGrant, Providers);
            started = Replace(account with { Operation = operation });
        }
        started.Deliver();
        try
        {
            var state = await run(entry.Workflow, cancellation.Token).ConfigureAwait(false);
            Update(id, state);
            return state.Failure is not null || state.Status is ProviderSessionStatus.ReauthenticationRequired or ProviderSessionStatus.RecoveryRequired ||
                operation == AccountOperation.Refreshing && (state.Status != ProviderSessionStatus.QuotaAvailable || state.FromCache)
                ? UiCommandResult.Failed(LiveMapping.Failure(state.Failure)) : UiCommandResult.Succeeded;
        }
        catch (OperationCanceledException)
        {
            // Rotation can have committed before cancellation; the session is authoritative, never restore a stale grant state.
            Update(id, entry.Session.State);
            return UiCommandResult.Cancelled;
        }
        catch (Exception error)
        {
            diagnostics?.Record(DiagnosticEvent.OperationFailure, DiagnosticProjection.Category(error));
            Update(id, entry.Session.State with { Failure = ProviderFailureKind.InternalError });
            return UiCommandResult.Failed(LiveMapping.Failure(ProviderFailureKind.InternalError));
        }
        finally
        {
            lock (sync) { entry.Cancellation = null; cancellation.Dispose(); }
        }
    }

    private void Update(string id, ProviderSessionState state)
    {
        Notification notification;
        lock (sync)
        {
            var entry = entries[id];
            entry.WasConnected |= entry.Session.HasStoredGrant;
            if (!entry.WasConnected && state.Status == ProviderSessionStatus.NotConnected)
                notification = Publish(current with { Accounts = current.Accounts.Where(a => a.Id != id).ToArray() });
            else
            {
                var old = current.Accounts.FirstOrDefault(a => a.Id == id);
                var next = LiveMapping.Map(id, state, entries[id].Session.HasStoredGrant, Providers);
                notification = Replace(next with { ObservationRevision = (old?.ObservationRevision ?? 0) +
                    (state.Status == ProviderSessionStatus.QuotaAvailable && !state.FromCache ? 1 : 0) });
            }
        }
        notification.Deliver();
    }
    private Notification Replace(AccountItem item) => Publish(current with
    {
        Accounts = current.Accounts.Where(a => a.Id != item.Id).Append(item).OrderBy(a => a.ProviderId).ToArray()
    });
    internal void SetRecovery(RecoveryState state)
    {
        Notification notification;
        lock (sync)
        {
            maintenanceBlocked = state != RecoveryState.None;
            notification = Publish(current with { System = current.System with { Recovery = state } });
        }
        notification.Deliver();
    }

    internal void SetPreferences(Preferences preferences, IReadOnlyDictionary<string, string> accountLabels,
        IReadOnlyDictionary<string, ExpansionPreference> groupExpansions)
    {
        Notification notification;
        lock (sync)
        {
            labels = accountLabels;
            expansions = groupExpansions;
            notification = Publish(current with { Preferences = preferences });
        }
        notification.Deliver();
    }
    /// <summary>
    /// Assigns the next snapshot and its revision under <see cref="sync"/>, and returns the
    /// subscribers to invoke. Subscribers are app code this source knows nothing about, so they
    /// are never called while the lock is held: the caller delivers after leaving it.
    /// </summary>
    private Notification Publish(UiSnapshot snapshot)
    {
        current = snapshot with { Revision = current.Revision + 1, ObservedAt = DateTimeOffset.UtcNow,
            Accounts = snapshot.Accounts.Select(a => a with
            {
                Label = labels.GetValueOrDefault(a.Id, Providers.Get(a.ProviderId).Name),
                Contexts = a.Contexts.Select(c => c with { Groups = c.Groups.Select(g => g with
                { Expansion = expansions.GetValueOrDefault(g.Id, ExpansionPreference.Auto) }).ToArray() }).ToArray()
            }).ToArray() };
        return new(current, subscribers.ToArray());
    }
    /// <summary>One published snapshot and the subscribers it is owed, delivered outside the lock.</summary>
    private readonly record struct Notification(UiSnapshot Snapshot, Action<UiSnapshot>[] Subscribers)
    {
        public void Deliver()
        {
            foreach (var subscriber in Subscribers) subscriber(Snapshot);
        }
    }

    public Task StopAsync()
    {
        lock (sync) { stopped = true; return stopping ??= DrainAsync(); }
    }
    private async Task DrainAsync()
    {
        await Task.Yield();
        Task[] pending;
        lock (sync)
        {
            foreach (var entry in entries.Values) entry.Cancellation?.Cancel();
            pending = entries.Values.Select(e => e.Pending ?? Task.CompletedTask).ToArray();
        }
        await Task.WhenAll(pending).ConfigureAwait(false);
        await Task.WhenAll(entries.Values.Select(e => e.Workflow.StopAsync())).ConfigureAwait(false);
        if (initialization is not null) await initialization.ConfigureAwait(false);
    }
    public void Dispose()
    {
        foreach (var entry in entries.Values) entry.Workflow.Dispose();
    }
    private sealed class Entry(IProviderSession session)
    {
        public IProviderSession Session { get; } = session;
        public DashboardWorkflow Workflow { get; } = new(session);
        public CancellationTokenSource? Cancellation { get; set; }
        public Task<UiCommandResult>? Pending { get; set; }
        public bool WasConnected { get; set; }
        public bool HistoryActive { get; set; }
        public AiUsage.Core.History.HistoryRange? HistoryRange { get; set; }
        public Task<AiUsage.Core.History.ProviderHistoryResult>? HistoryTask { get; set; }
    }
    private sealed class Subscription(Action unsubscribe) : IDisposable { public void Dispose() => unsubscribe(); }
}
