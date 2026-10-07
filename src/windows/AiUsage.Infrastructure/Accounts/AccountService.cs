using AiUsage.Core.Accounts;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Accounts;

/// <summary>Owns account sessions and admission; all durable identity decisions remain in Infrastructure.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class AccountService(AccountRegistry registry, ProviderSessionFactory factory, IDiagnosticSink? diagnostics = null,
    AccountIdentityMap? identities = null)
    : IAccountService, IDisposable
{
    private readonly object sync = new();
    private readonly SemaphoreSlim mutation = new(1, 1);
    private readonly Dictionary<Guid, Entry> entries = [];
    private readonly CancellationTokenSource shutdown = new();
    private Attempt? attempt;
    private Task? initialization;
    private Task? stopping;
    private bool ready;
    private bool stopped;
    private bool disposed;

    public event EventHandler? Changed;
    public IReadOnlyList<AccountSnapshot> Current
    {
        get
        {
            lock (sync) return entries.Values.Select(e => new AccountSnapshot(e.Record.Id, e.Record.Provider,
                e.Record.Connected, e.State, e.RefreshBusy || e.Disconnecting || attempt?.Target == e.Record.Id,
                e.LastFailureAt)).ToArray();
        }
    }

    public Task InitializeAsync(CancellationToken token)
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (stopped) return Task.CompletedTask;
            return initialization ??= InitializeCoreAsync(token);
        }
    }

    private async Task InitializeCoreAsync(CancellationToken token)
    {
        await Task.Yield();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
        var state = await registry.ReadAsync(linked.Token).ConfigureAwait(false);
        foreach (var pending in state.Pending)
            await CleanupAsync(pending, linked.Token).ConfigureAwait(false);
        state = await registry.ReadAsync(linked.Token).ConfigureAwait(false);
        foreach (var record in state.Accounts)
        {
            linked.Token.ThrowIfCancellationRequested();
            var session = factory.Create(record);
            var entry = new Entry(record, session);
            lock (sync) entries.Add(record.Id, entry);
            if (record.Disconnecting)
            {
                await FinishDisconnectAsync(entry, linked.Token).ConfigureAwait(false);
                continue;
            }
            if (!record.Connected) continue;
            var cached = await session.ReadCachedStateAsync(linked.Token).ConfigureAwait(false);
            entry.State = ValidBinding(entry) ? cached : BindingFailure();
        }
        lock (sync) ready = !stopped;
        Publish();
    }

    public Task<AccountResult> ConnectAsync(string provider, Guid? reconnectAccountId, Guid attemptId,
        Action<AuthorizationChallenge> authorize, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(authorize);
        if (!AccountRegistry.KnownProvider(provider) || attemptId == Guid.Empty)
            return Task.FromResult(new AccountResult(AccountOutcome.Failed, Failure: ProviderFailureKind.RequestRejected));
        lock (sync)
        {
            if (!ready || stopped) return Task.FromResult(Unavailable());
            if (attempt is not null) return Task.FromResult(new AccountResult(AccountOutcome.Busy));
            if (reconnectAccountId is { } id && (!entries.TryGetValue(id, out var target) || target.Record.Provider != provider))
                return Task.FromResult(new AccountResult(AccountOutcome.Failed, id, ProviderFailureKind.AccountMismatch));
            if (reconnectAccountId is { } key && IsBusy(entries[key]))
                return Task.FromResult(new AccountResult(AccountOutcome.Busy, key));
            var next = new Attempt(attemptId, provider, reconnectAccountId,
                CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token));
            attempt = next;
            return next.Work = ConnectCoreAsync(next, authorize);
        }
    }

    private async Task<AccountResult> ConnectCoreAsync(Attempt active, Action<AuthorizationChallenge> authorize)
    {
        await Task.Yield();
        Publish();
        using var operation = diagnostics?.Begin(DiagnosticOperation.Connect);
        var pending = new PendingAccountStorage(active.Provider, Guid.NewGuid());
        var admitted = false;
        var uncertain = false;
        IProviderSession? candidate = null;
        AccountResult result;
        try
        {
            await MutateAsync(s => s with { Pending = [.. s.Pending, pending] }, active.Cancel.Token).ConfigureAwait(false);
            candidate = factory.Create(active.Provider, pending.StorageId);
            lock (sync) active.Session = candidate;
            var connected = await candidate.ConnectWithChallengeAsync(authorize, active.Cancel.Token).ConfigureAwait(false);
            active.Cancel.Token.ThrowIfCancellationRequested();
            var identity = ProviderSessionFactory.IdentityOf(candidate);
            if (identity is null || !candidate.HasStoredGrant || connected.Status is ProviderSessionStatus.RecoveryRequired or
                ProviderSessionStatus.ReauthenticationRequired || connected.Failure is ProviderFailureKind.AccountMismatch or ProviderFailureKind.AuthenticationRequired)
                result = new(AccountOutcome.Failed, active.Target, connected.Failure ?? ProviderFailureKind.AuthenticationRequired);
            else
            {
                await mutation.WaitAsync(active.Cancel.Token).ConfigureAwait(false);
                try
                {
                    var state = await registry.ReadAsync(active.Cancel.Token).ConfigureAwait(false);
                    var match = state.Accounts.FirstOrDefault(a => a.Provider == active.Provider && a.Identity == identity);
                    var selected = active.Target is { } selectedId ? state.Accounts.FirstOrDefault(a => a.Id == selectedId) : null;
                    if (active.Target is not null && (selected is null || selected.Identity != identity))
                        result = new(AccountOutcome.Failed, active.Target, ProviderFailureKind.AccountMismatch);
                    else if (active.Target is null && match?.Connected == true)
                        result = new(AccountOutcome.Duplicate, match.Id);
                    else if ((selected ?? match)?.Connected == true &&
                        (connected.Status != ProviderSessionStatus.QuotaAvailable || connected.Failure is not null))
                        result = new(AccountOutcome.Failed, selected?.Id ?? match?.Id, connected.Failure ?? ProviderFailureKind.InvalidResponse);
                    else
                    {
                        var previous = selected ?? match;
                        // AIU-047: after a reinstall the registry is empty, but the history root still maps this exact identity.
                        var restored = previous is null && identities is not null
                            ? await identities.FindAsync(active.Provider, identity, active.Cancel.Token).ConfigureAwait(false) : null;
                        if (restored is { } restoredId && state.Accounts.Any(a => a.Id == restoredId)) restored = null;
                        var record = new AccountRecord(previous?.Id ?? restored ?? Guid.NewGuid(), active.Provider, pending.StorageId, identity);
                        active.Cancel.Token.ThrowIfCancellationRequested();
                        uncertain = true;
                        await registry.UpdateAsync(s => s with
                        {
                            Accounts = previous is null ? [.. s.Accounts, record] : s.Accounts.Select(a => a.Id == previous.Id ? record : a).ToArray(),
                            Pending = s.Pending.Where(p => p.StorageId != pending.StorageId)
                                .Concat(previous is null ? [] : new[] { new PendingAccountStorage(previous.Provider, previous.StorageId, previous.LegacyStorage) }).ToArray()
                        }, CancellationToken.None).ConfigureAwait(false);
                        admitted = true;
                        uncertain = false;
                        IProviderSession? oldSession = null;
                        lock (sync)
                        {
                            if (!entries.TryGetValue(record.Id, out var entry))
                                entries.Add(record.Id, entry = new(record, candidate));
                            else oldSession = entry.Session;
                            entry.Record = record;
                            entry.Session = candidate;
                            entry.State = connected;
                            entry.LastFailureAt = connected.Failure is null ? null : DateTimeOffset.UtcNow;
                        }
                        (oldSession as IDisposable)?.Dispose();
                        if (restored is not null) diagnostics?.Signal(DiagnosticEvent.HistoryReattached);
                        if (identities is not null) await identities.UpsertAsync([record], CancellationToken.None).ConfigureAwait(false);
                        if (previous is not null)
                            await CleanupAsync(new(previous.Provider, previous.StorageId, previous.LegacyStorage), CancellationToken.None).ConfigureAwait(false);
                        result = new(AccountOutcome.Done, record.Id, connected.Failure);
                    }
                }
                finally { mutation.Release(); }
            }
        }
        catch (OperationCanceledException)
        {
            operation?.SetOutcome(DiagnosticOutcome.Cancelled);
            result = new(AccountOutcome.Cancelled, active.Target);
        }
        catch (ProviderException error)
        {
            operation?.SetOutcome(DiagnosticOutcome.RecoveryRequired);
            lock (sync) ready = false;
            result = new(AccountOutcome.Failed, active.Target, error.Kind);
        }
        finally
        {
            lock (sync) active.Session = null;
            if (!admitted) (candidate as IDisposable)?.Dispose();
        }
        if (!admitted && !uncertain)
        {
            try
            {
                await mutation.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try { await CleanupAsync(pending, CancellationToken.None).ConfigureAwait(false); }
                finally { mutation.Release(); }
            }
            catch (ProviderException error)
            {
                lock (sync) ready = false;
                result = new(AccountOutcome.Failed, active.Target, error.Kind);
            }
        }
        lock (sync)
        {
            if (ReferenceEquals(attempt, active)) attempt = null;
            active.Cancel.Dispose();
        }
        if (result.Outcome is AccountOutcome.Failed or AccountOutcome.Duplicate) operation?.SetOutcome(DiagnosticOutcome.Failed);
        Publish();
        return result;
    }

    public async Task CancelConnectAsync(Guid attemptId)
    {
        Task<AccountResult>? pending;
        lock (sync)
        {
            if (attempt is not { } active || active.Id != attemptId) return;
            active.Cancel.Cancel();
            pending = active.Work;
        }
        if (pending is not null) await pending.ConfigureAwait(false);
    }

    public bool TrySubmitCode(Guid attemptId, string code)
    {
        lock (sync) return attempt is { } active && active.Id == attemptId && !active.Cancel.IsCancellationRequested &&
            active.Session?.TrySubmitCode(code) == true;
    }

    public Task<AccountResult> RefreshAsync(Guid accountId, CancellationToken token)
    {
        lock (sync)
        {
            if (!ready || stopped) return Task.FromResult(Unavailable(accountId));
            if (!entries.TryGetValue(accountId, out var entry) || !entry.Record.Connected)
                return Task.FromResult(new AccountResult(AccountOutcome.Failed, accountId, ProviderFailureKind.AuthenticationRequired));
            if (IsBusy(entry) || attempt?.Target == accountId) return Task.FromResult(new AccountResult(AccountOutcome.Busy, accountId));
            if (entry.State.Status == ProviderSessionStatus.RecoveryRequired) return Task.FromResult(Unavailable(accountId));
            entry.Cancel = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
            entry.RefreshBusy = true;
            return entry.Work = RefreshCoreAsync(entry);
        }
    }

    private async Task<AccountResult> RefreshCoreAsync(Entry entry)
    {
        await Task.Yield();
        Publish();
        using var operation = diagnostics?.Begin(DiagnosticOperation.Refresh);
        try
        {
            // Revalidate the protected binding before any renewal or provider request.
            var cached = await entry.Session.ReadCachedStateAsync(entry.Cancel!.Token).ConfigureAwait(false);
            var state = cached.Status == ProviderSessionStatus.RecoveryRequired ? cached : !ValidBinding(entry) ? BindingFailure() :
                await entry.Session.RefreshAsync(entry.Cancel.Token).ConfigureAwait(false);
            if (state.Status != ProviderSessionStatus.RecoveryRequired && !ValidBinding(entry)) state = BindingFailure();
            lock (sync)
            {
                entry.State = state;
                entry.LastFailureAt = state.Failure is null ? null : DateTimeOffset.UtcNow;
            }
            operation?.SetOutcome(state.Failure is null ? DiagnosticOutcome.Completed : DiagnosticOutcome.Failed);
            return new(state.Failure is null && state.Status == ProviderSessionStatus.QuotaAvailable ? AccountOutcome.Done : AccountOutcome.Failed,
                entry.Record.Id, state.Failure);
        }
        catch (OperationCanceledException)
        {
            operation?.SetOutcome(DiagnosticOutcome.Cancelled);
            return new(AccountOutcome.Cancelled, entry.Record.Id);
        }
        finally
        {
            lock (sync) { entry.Cancel?.Dispose(); entry.Cancel = null; entry.RefreshBusy = false; }
            Publish();
        }
    }

    public Task<AccountResult> DisconnectAsync(Guid accountId, CancellationToken token)
    {
        lock (sync)
        {
            if (!ready || stopped) return Task.FromResult(Unavailable(accountId));
            if (!entries.TryGetValue(accountId, out var entry))
                return Task.FromResult(new AccountResult(AccountOutcome.Failed, accountId, ProviderFailureKind.RequestRejected));
            if (entry.Disconnecting) return Task.FromResult(new AccountResult(AccountOutcome.Busy, accountId));
            entry.Disconnecting = true;
            entry.Cancel?.Cancel();
            var reconnect = attempt?.Target == accountId ? attempt : null;
            reconnect?.Cancel.Cancel();
            return entry.DisconnectWork = DisconnectCoreAsync(entry, entry.Work, reconnect?.Work, token);
        }
    }

    private async Task<AccountResult> DisconnectCoreAsync(Entry original, Task<AccountResult>? refresh,
        Task<AccountResult>? reconnect, CancellationToken token)
    {
        await Task.Yield();
        Publish();
        using var operation = diagnostics?.Begin(DiagnosticOperation.Disconnect);
        try
        {
            if (refresh is not null) await refresh.ConfigureAwait(false);
            if (reconnect is not null) await reconnect.ConfigureAwait(false);
            Entry entry;
            lock (sync) entry = entries[original.Record.Id];
            await mutation.WaitAsync(token).ConfigureAwait(false);
            try
            {
                // Durable intent prevents restart from resuming a grant whose sign-out was interrupted.
                await registry.UpdateAsync(s => s with { Accounts = s.Accounts.Select(a => a.Id == entry.Record.Id && a.Connected
                    ? a with { Disconnecting = true } : a).ToArray() }, token).ConfigureAwait(false);
                await FinishDisconnectAsync(entry, CancellationToken.None).ConfigureAwait(false);
            }
            finally { mutation.Release(); }
            return new(AccountOutcome.Done, entry.Record.Id);
        }
        catch (ProviderException error)
        {
            operation?.SetOutcome(DiagnosticOutcome.RecoveryRequired);
            lock (sync) { ready = false; original.State = new(ProviderSessionStatus.RecoveryRequired, Failure: error.Kind); }
            return new(AccountOutcome.Failed, original.Record.Id, error.Kind);
        }
        catch (OperationCanceledException)
        {
            operation?.SetOutcome(DiagnosticOutcome.Cancelled);
            return new(AccountOutcome.Cancelled, original.Record.Id);
        }
        finally
        {
            lock (sync) original.Disconnecting = false;
            Publish();
        }
    }

    private async Task FinishDisconnectAsync(Entry entry, CancellationToken token)
    {
        var result = await entry.Session.DisconnectAsync(token).ConfigureAwait(false);
        if (result.Status != ProviderSessionStatus.NotConnected || entry.Session.HasStoredGrant)
            throw new ProviderException(result.Failure ?? ProviderFailureKind.GrantNotRemoved);
        var record = entry.Record with { Connected = false, Disconnecting = false };
        await registry.UpdateAsync(s => s with { Accounts = s.Accounts.Select(a => a.Id == record.Id ? record : a).ToArray() },
            CancellationToken.None).ConfigureAwait(false);
        lock (sync) { entry.Record = record; entry.State = ProviderSessionState.NotConnected; entry.LastFailureAt = null; }
    }

    private async Task CleanupAsync(PendingAccountStorage pending, CancellationToken token)
    {
        // Consult committed/recovered references before deleting any candidate or predecessor.
        var state = await registry.ReadAsync(token).ConfigureAwait(false);
        if (state.Accounts.Any(a => a.StorageId == pending.StorageId))
            throw new ProviderException(ProviderFailureKind.RecoveryRequired);
        if (!state.Pending.Contains(pending)) return;
        var session = factory.Create(pending);
        try
        {
            var result = await session.DisconnectAsync(token).ConfigureAwait(false);
            if (result.Status != ProviderSessionStatus.NotConnected || session.HasStoredGrant)
                throw new ProviderException(result.Failure ?? ProviderFailureKind.GrantNotRemoved);
        }
        finally { ((IDisposable)session).Dispose(); }
        await registry.UpdateAsync(s => s with { Pending = s.Pending.Where(p => p != pending).ToArray() }, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task MutateAsync(Func<AccountRegistryState, AccountRegistryState> update, CancellationToken token)
    {
        await mutation.WaitAsync(token).ConfigureAwait(false);
        try { await registry.UpdateAsync(update, token).ConfigureAwait(false); }
        finally { mutation.Release(); }
    }

    public Task StopAsync()
    {
        lock (sync)
        {
            if (stopping is not null) return stopping;
            stopped = true;
            ready = false;
            shutdown.Cancel();
            return stopping = StopCoreAsync();
        }
    }

    private async Task StopCoreAsync()
    {
        await Task.Yield();
        Task[] work;
        lock (sync) work = entries.Values.SelectMany(e => new Task?[] { e.Work, e.DisconnectWork })
            .Append(attempt?.Work).Append(initialization).OfType<Task>().ToArray();
        try { await Task.WhenAll(work).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            // WhenAll has completed every writer, including failed initialization. A completed
            // operation failure must not prevent the owner from resetting its local state.
            diagnostics?.Failure(DiagnosticEvent.OperationFailure, error);
        }
    }

    private static bool ValidBinding(Entry entry) => entry.Session.HasStoredGrant && ProviderSessionFactory.IdentityOf(entry.Session) == entry.Record.Identity;
    private static bool IsBusy(Entry entry) => entry.Disconnecting || entry.Work is { IsCompleted: false };
    private static ProviderSessionState BindingFailure() => new(ProviderSessionStatus.RecoveryRequired, Failure: ProviderFailureKind.AccountMismatch);
    private static AccountResult Unavailable(Guid? id = null) => new(AccountOutcome.Failed, id, ProviderFailureKind.RecoveryRequired);
    private void Publish() => Changed?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            if (attempt is not null || entries.Values.Any(e => IsBusy(e) || e.DisconnectWork is { IsCompleted: false }) || initialization is { IsCompleted: false })
                throw new InvalidOperationException("Drain account work before disposing it.");
            disposed = true;
            stopped = true;
            foreach (var entry in entries.Values) ((IDisposable)entry.Session).Dispose();
            shutdown.Dispose();
            mutation.Dispose();
        }
    }

    private sealed class Entry(AccountRecord record, IProviderSession session)
    {
        internal AccountRecord Record = record;
        internal IProviderSession Session = session;
        internal ProviderSessionState State = ProviderSessionState.NotConnected;
        internal DateTimeOffset? LastFailureAt;
        internal CancellationTokenSource? Cancel;
        internal Task<AccountResult>? Work;
        internal Task<AccountResult>? DisconnectWork;
        internal bool Disconnecting;
        internal bool RefreshBusy;
    }

    private sealed class Attempt(Guid id, string provider, Guid? target, CancellationTokenSource cancel)
    {
        internal readonly Guid Id = id;
        internal readonly string Provider = provider;
        internal readonly Guid? Target = target;
        internal readonly CancellationTokenSource Cancel = cancel;
        internal IProviderSession? Session;
        internal Task<AccountResult>? Work;
    }
}
