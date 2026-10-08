using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Diagnostics;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Adapters.Live;

/// <summary>Serializes local commands and observation capture; publishes complete snapshots through the UI dispatcher.</summary>
internal sealed class LiveLedgerSource : ILedgerSource, IDisposable
{
    internal static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private readonly IAccountService accounts;
    private readonly IReadingSeriesStore readings;
    private readonly IBudgetConfigurationStore budgets;
    private readonly IQuotaObservationRecorder recorder;
    private readonly LedgerPreferenceStore preferences;
    private readonly Func<Action, Task> dispatch;
    private readonly Action<Uri> openBrowser;
    private readonly TimeProvider time;
    private readonly TimeZoneInfo zone;
    private readonly IDiagnosticSink? diagnostics;
    private readonly object sync = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource shutdown = new();
    private readonly Queue<AccountSnapshot> captures = [];
    private readonly Dictionary<Guid, DateTimeOffset> observed = [];
    private readonly HashSet<Guid> lostCaptures = [];
    private readonly Dictionary<Guid, (DateTimeOffset At, int Failures)> attempts = [];
    private Dictionary<string, LedgerLimit> limits = [];
    private BudgetConfiguration configuration = BudgetConfiguration.Default;
    private Task rebuild = Task.CompletedTask;
    private Task tick = Task.CompletedTask;
    private Task? stopping;
    private bool dirty;
    private bool rebuilding;
    private bool stopped;
    private bool disposed;
    private bool initialized;
    private bool configurationWritable;
    private string? localStatus;
    private SignInStripModel? strip;
    private LedgerRecoveryModel? recovery;
    private UpdateStatus updates = UpdateStatus.NotPackaged;
    private bool paused;

    public LiveLedgerSource(IAccountService accounts, IReadingSeriesStore readings, IBudgetConfigurationStore budgets,
        IQuotaObservationRecorder recorder, LedgerPreferenceStore preferences, Func<Action, Task> dispatch,
        Action<Uri> openBrowser, TimeProvider? time = null, TimeZoneInfo? zone = null, IDiagnosticSink? diagnostics = null)
    {
        this.accounts = accounts; this.readings = readings; this.budgets = budgets; this.recorder = recorder;
        this.preferences = preferences; this.dispatch = dispatch; this.openBrowser = openBrowser;
        this.time = time ?? TimeProvider.System; this.zone = zone ?? TimeZoneInfo.Local; this.diagnostics = diagnostics;
        Current = new(this.time.GetUtcNow(), new(DayKind.WorkDay, false, null), [], Options([]), null,
            new(BudgetSettingsModel.MondayToFriday, []), new(RefreshInterval, UpdateStatus.NotPackaged, 0, []) { IsStarting = true });
        accounts.Changed += AccountsChanged;
    }

    public LedgerSnapshot Current { get; private set; }
    public LedgerPreferences Preferences => preferences.Current.Preferences;
    public event EventHandler? Changed;
    // Set by the desktop lifetime. Deletion owns stopping/draining and restarting the product.
    public Func<CancellationToken, Task<CommandOutcome>>? DeleteData { get; set; }
    public Func<LedgerSupportAction, CancellationToken, Task<CommandOutcome>>? SupportAction { get; set; }
    public Func<CancellationToken, Task<string>>? DiagnosticsPreview { get; set; }
    public Task<CommandOutcome> SupportAsync(LedgerSupportAction action, CancellationToken ct) => SupportAction?.Invoke(action, ct) ?? Task.FromResult(CommandOutcome.Unavailable);
    public Task<string> PreviewDiagnosticsAsync(CancellationToken ct) => DiagnosticsPreview?.Invoke(ct) ?? Task.FromResult("Diagnostics unavailable");
    public Task SetRecoveryAsync(LedgerRecoveryModel? value)
    {
        recovery = value;
        return dispatch(() =>
        {
            Current = Current with { Summaries = Current.Summaries with { Recovery = value, IsStarting = false, DiagnosticsAvailable = DiagnosticsPreview is not null } };
            Changed?.Invoke(this, EventArgs.Empty);
        });
    }

    // Set by the desktop lifetime; the update coordinator owns checking and installing.
    public Func<CancellationToken, Task>? CheckUpdates { get; set; }
    public Func<CancellationToken, Task>? InstallUpdate { get; set; }
    public Task CheckForUpdatesAsync(CancellationToken ct) => CheckUpdates?.Invoke(ct) ?? Task.CompletedTask;
    public Task InstallUpdateAsync(CancellationToken ct) => InstallUpdate?.Invoke(ct) ?? Task.CompletedTask;
    public Task SetUpdatesAsync(UpdateStatus value)
    {
        lock (sync) updates = value;
        return dispatch(() =>
        {
            if (stopped) return;
            Current = Current with { Summaries = Current.Summaries with { Updates = value } };
            Changed?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>Stops automatic refresh and waits for running provider work before an update closes the process.</summary>
    public async Task<bool> PauseRefreshAsync(TimeSpan busyLimit, CancellationToken token)
    {
        Task running;
        lock (sync) { paused = true; running = tick; }
        try { await running; }
        catch (Exception error) when (error is not OperationCanceledException || !token.IsCancellationRequested) { /* The lifetime logs tick failures. */ }
        var started = time.GetTimestamp();
        while (accounts.Current.Any(a => a.Busy) || SigningIn)
        {
            if (time.GetElapsedTime(started) >= busyLimit)
            {
                ResumeRefresh();
                return false;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(250), time, token);
        }
        return true;
    }

    public void ResumeRefresh() { lock (sync) paused = false; }
    // Provider commands start no new work while an update install is draining; Windows may close the process next.
    private bool Paused { get { lock (sync) return paused; } }
    private bool SigningIn { get { lock (sync) return strip?.Phase == SignInPhase.Waiting; } }

    public async Task InitializeAsync(string? legacyPreferences, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (stopped || initialized) return;
            if (!await preferences.LoadAsync(legacyPreferences, token)) localStatus = "Preferences need recovery; changes cannot be saved";
            try
            {
                var loaded = await budgets.LoadConfigurationAsync(token);
                configuration = loaded.Value;
                configurationWritable = true;
                if (loaded.Recovered) localStatus = "Budget settings were recovered; review your caps";
            }
            catch (Exception error) when (error is not OperationCanceledException)
            { diagnostics?.Failure(DiagnosticEvent.PersistenceFailure, error); localStatus = "Budget settings need recovery; changes cannot be saved"; }
            await accounts.InitializeAsync(token);
            initialized = true;
        }
        finally { gate.Release(); }
        QueueRebuild();
        await WaitForIdleAsync();
    }

    private void AccountsChanged(object? sender, EventArgs args) => QueueRebuild();
    private void QueueRebuild()
    {
        lock (sync)
        {
            if (stopped) return;
            foreach (var account in accounts.Current)
                if (account.Session is { Status: ProviderSessionStatus.QuotaAvailable, FromCache: false, Failure: null, Quota: { } quota } &&
                    observed.GetValueOrDefault(account.AccountId) != quota.FetchedAt)
                {
                    observed[account.AccountId] = quota.FetchedAt;
                    captures.Enqueue(account);
                }
            dirty = true;
            if (!rebuilding) { rebuilding = true; rebuild = RebuildLoopAsync(); }
        }
    }

    public async Task WaitForIdleAsync()
    {
        while (true)
        {
            Task pending;
            lock (sync) pending = rebuild;
            await pending;
            lock (sync) if (pending == rebuild && !dirty) return;
        }
    }

    private async Task RebuildLoopAsync()
    {
        await Task.Yield();
        while (true)
        {
            lock (sync) { if (stopped || !dirty) { dirty = false; rebuilding = false; return; } dirty = false; }
            try
            {
                await gate.WaitAsync(shutdown.Token);
                try { if (initialized) await BuildAsync(shutdown.Token); }
                finally { gate.Release(); }
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
            catch (Exception error)
            {
                diagnostics?.Failure(DiagnosticEvent.BackgroundFailure, error);
                localStatus = "Local data could not be loaded; the previous view is retained";
                await PublishAsync(Current with { Summaries = Current.Summaries with { LocalStatus = localStatus } });
            }
        }
    }

    private async Task BuildAsync(CancellationToken token)
    {
        var now = time.GetUtcNow();
        var date = WorkCalendar.Date(now, zone);
        if (preferences.Current is { PendingWorkDays: { } pending, WorkDaysEffectiveOn: { } on } && date >= on && configurationWritable)
        {
            var next = configuration with { WorkDays = pending };
            await budgets.SaveConfigurationAsync(next, token);
            configuration = next;
            // Retaining the pending operation after a failed preference save makes the retry idempotent.
            await preferences.ChangeAsync(s => s with { PendingWorkDays = null, WorkDaysEffectiveOn = null }, token);
        }
        while (true)
        {
            AccountSnapshot capture;
            lock (sync) { if (!captures.TryDequeue(out capture!)) break; }
            try { await recorder.RecordAsync(capture.AccountId.ToString("N"), capture.Provider, capture.Session, token); }
            catch (OperationCanceledException) { throw; }
            catch (Exception error)
            {
                lostCaptures.Add(capture.AccountId);
                diagnostics?.Failure(DiagnosticEvent.CaptureLost, error);
            }
        }
        var snapshots = accounts.Current;
        var models = new List<AccountModel>();
        var nextLimits = new Dictionary<string, LedgerLimit>();
        foreach (var account in snapshots)
        {
            var id = account.AccountId.ToString("N");
            var data = new List<LedgerLimit>();
            foreach (var fact in account.Session.Quota?.Limits?.Limits ?? [])
            {
                var series = new ReadingSeriesKey(id, fact.Key);
                var read = await readings.ReadAsync(series, token);
                if (read.Recovered) lostCaptures.Add(account.AccountId);
                var limit = new LedgerLimit(fact, series, read.Value);
                data.Add(limit);
            }
            data = [.. LiveLedgerProjection.AccountLimits(data)];
            foreach (var limit in data) nextLimits.Add(LiveLedgerProjection.CardId(limit.Series), limit);
            var name = preferences.Current.Labels.GetValueOrDefault(id) ?? DefaultName(account, snapshots);
            var model = LiveLedgerProjection.Account(account, name, data, configuration, now, zone, preferences.Current.WorkToday);
            model = model with { Cards = model.Cards.OrderBy(c => Order(c.CardId))
                    .Select(c => preferences.Current.Hidden.Contains(c.CardId) ? c with { Hidden = true } : c).ToArray(),
                NextRetryAt = NextRetry(account) is { } retry ? TimeZoneInfo.ConvertTime(retry, zone) : null };
            models.Add(model);
        }
        limits = nextLimits;
        var caps = configuration.Caps.Select(cap =>
        {
            var id = LiveLedgerProjection.CardId(cap.Series);
            var card = models.SelectMany(a => a.Cards).FirstOrDefault(c => c.CardId == id);
            var name = models.FirstOrDefault(a => a.AccountId == cap.Series.AccountTarget)?.DisplayName ?? "Unassigned legacy account";
            var facts = limits.GetValueOrDefault(id)?.Facts;
            return new CapSettingModel(id, card?.CapTargetId, name, card?.ScopeLabel,
                (cap.Cap.Amount is MoneyQuantity money ? new(ScaleKind.Money, null, money.Currency, money.Exponent) : ScaleModel.Count(((CountQuantity)cap.Cap.Amount).Unit)),
                LiveLedgerProjection.Amount(cap.Cap.Amount) ?? 0, card?.Cap?.Status ?? CapStatus.Unmatched,
                card?.Cap?.Binding ?? false, card?.Figures.ProviderLimit ?? AiUsage.Features.Ledger.Contract.LimitValue.Unknown,
                facts?.Kind == LimitKind.MonetaryPool ? facts.Unit : null, card?.Figures.Tracking);
        }).ToArray();
        var off = !configuration.WorkDays.Contains(date.DayOfWeek);
        var workToday = off && preferences.Current.WorkToday == date;
        var failed = models.Where(a => a.Health is not (AccountHealth.Ok or AccountHealth.SignedOut)).ToArray();
        SignInStripModel? currentStrip;
        lock (sync) currentStrip = strip;
        await PublishAsync(new(TimeZoneInfo.ConvertTime(now, zone), new(off ? DayKind.DayOff : DayKind.WorkDay, workToday,
            workToday ? WorkCalendar.Midnight(date.AddDays(1), zone) : null), models, Options(snapshots), currentStrip,
            new((preferences.Current.PendingWorkDays ?? configuration.WorkDays).ToHashSet(), caps),
            new(RefreshInterval, updates, failed.Length, failed.Select(a => a.Provider).Distinct().ToArray())
            { LocalStatus = lostCaptures.Count > 0 ? "History has missing readings because local capture failed" : localStatus,
                Recovery = recovery, DiagnosticsAvailable = DiagnosticsPreview is not null }));
    }

    private int Order(string id) { var index = Array.IndexOf(preferences.Current.Order, id); return index < 0 ? int.MaxValue : index; }
    private static string DefaultName(AccountSnapshot account, IReadOnlyList<AccountSnapshot> all)
    {
        var provider = LiveLedgerProjection.Provider(account.Provider).ToString();
        var number = all.Where(a => a.Provider == account.Provider).TakeWhile(a => a.AccountId != account.AccountId).Count() + 1;
        return provider + (account.Session.Quota?.PlanType is { Length: > 0 } plan ? " " + plan : string.Empty) + (number > 1 ? " " + number : string.Empty);
    }
    private ProviderOption[] Options(IReadOnlyList<AccountSnapshot> current) => Enum.GetValues<ProviderKind>().Select(p =>
        new ProviderOption(p, current.Any(a => LiveLedgerProjection.Provider(a.Provider) == p && a.Connected), strip?.Phase == SignInPhase.Waiting)).ToArray();
    private Task PublishAsync(LedgerSnapshot snapshot) => dispatch(() => { if (!stopped) { Current = snapshot; Changed?.Invoke(this, EventArgs.Empty); } });

    private async Task<CommandOutcome> ChangeAsync(Func<CancellationToken, Task<CommandOutcome>> change, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
        try
        {
            await gate.WaitAsync(linked.Token);
            try
            {
                if (!initialized || stopped) return CommandOutcome.Unavailable;
                var result = await change(linked.Token);
                if (result == CommandOutcome.Done) await BuildAsync(linked.Token);
                return result;
            }
            finally { gate.Release(); }
        }
        catch (OperationCanceledException) { return CommandOutcome.Unavailable; }
        catch (Exception error)
        {
            diagnostics?.Failure(DiagnosticEvent.PersistenceFailure, error);
            localStatus = "The change could not be saved; local data needs attention";
            QueueRebuild();
            return CommandOutcome.Unavailable;
        }
    }
    public Task<CommandOutcome> RenameAccountAsync(string accountId, string name, CancellationToken ct) => ChangeAsync(token =>
        accounts.Current.Any(a => a.AccountId.ToString("N") == accountId) && name.Trim().Length is > 0 and <= 100
            ? preferences.ChangeAsync(s => s with { Labels = new(s.Labels) { [accountId] = name.Trim() } }, token)
            : Task.FromResult(CommandOutcome.Rejected), ct);
    public async Task SetPreferencesAsync(LedgerPreferences value, CancellationToken ct) =>
        await ChangeAsync(token => preferences.ChangeAsync(s => s with { Preferences = value }, token), ct);
    public async Task SetWorkTodayAsync(bool on, CancellationToken ct) => await ChangeAsync(token =>
        preferences.ChangeAsync(s => s with { WorkToday = on ? WorkCalendar.Date(time.GetUtcNow(), zone) : null }, token), ct);
    public Task<CommandOutcome> SetWorkDaysAsync(IReadOnlySet<DayOfWeek> days, CancellationToken ct) => ChangeAsync(token =>
        days.Count is > 0 and <= 7 && days.All(Enum.IsDefined) ? preferences.ChangeAsync(s => s with
        { PendingWorkDays = days.Order().ToArray(), WorkDaysEffectiveOn = WorkCalendar.Date(time.GetUtcNow(), zone).AddDays(1) }, token)
        : Task.FromResult(CommandOutcome.Rejected), ct);
    public async Task MoveCardAsync(string cardId, int offset, CancellationToken ct) => await ChangeAsync(token =>
    {
        var cards = Current.Accounts.FirstOrDefault(a => a.Cards.Any(c => c.CardId == cardId))?.Cards.Select(c => c.CardId).ToList();
        var index = cards?.IndexOf(cardId) ?? -1;
        var target = (long)index + offset;
        if (cards is null || index < 0 || target < 0 || target >= cards.Count) return Task.FromResult(CommandOutcome.Rejected);
        (cards[index], cards[(int)target]) = (cards[(int)target], cards[index]);
        return preferences.ChangeAsync(s => s with { Order = cards.Concat(s.Order.Except(cards)).ToArray() }, token);
    }, ct);
    public Task<CommandOutcome> SetCardHiddenAsync(string cardId, bool hidden, CancellationToken ct) => ChangeAsync(token =>
    {
        var account = Current.Accounts.FirstOrDefault(a => a.Cards.Any(c => c.CardId == cardId));
        if (account is null || hidden && AccountCard.Primary(account)?.CardId == cardId) return Task.FromResult(CommandOutcome.Rejected);
        return preferences.ChangeAsync(s => s with { Hidden = hidden ? [.. s.Hidden.Append(cardId).Distinct()] : [.. s.Hidden.Where(id => id != cardId)] }, token);
    }, ct);

    public Task<CommandOutcome> SetCapAsync(string capTargetId, decimal? amount, CancellationToken ct) => ChangeAsync(async token =>
    {
        if (!configurationWritable) return CommandOutcome.Unavailable;
        if (!limits.TryGetValue(capTargetId, out var limit) || limit.Facts.Kind == LimitKind.PercentWindow || amount < 0) return CommandOutcome.Rejected;
        var card = Current.Accounts.SelectMany(a => a.Cards).FirstOrDefault(c => c.CapTargetId == capTargetId);
        if (card is null || amount is { } requested && !card.Figures.ProviderLimit.AllowsCap(requested)) return CommandOutcome.Rejected;
        Quantity? quantity = null;
        if (amount is { } value)
        {
            if (card.Scale.Kind == ScaleKind.Money && card.Scale is { Currency: { } currency, Exponent: >= 0 and <= 18 })
            {
                decimal minor = value;
                for (int i = 0; i < card.Scale.Exponent; i++) minor = checked(minor * 10);
                if (minor != decimal.Truncate(minor) || minor > long.MaxValue) return CommandOutcome.Rejected;
                quantity = new MoneyQuantity((long)minor, card.Scale.Exponent, currency);
            }
            else if (card.Scale is { Kind: ScaleKind.Count, UnitName: { } unit } && value == decimal.Truncate(value)) quantity = new CountQuantity(value, unit);
            else return CommandOutcome.Rejected;
        }
        var caps = configuration.Caps.Where(c => c.Series != limit.Series).ToList();
        if (quantity is not null) caps.Add(new(limit.Series, new(quantity, time.GetUtcNow())));
        var next = configuration with { Caps = caps };
        await budgets.SaveConfigurationAsync(next, token); configuration = next;
        return CommandOutcome.Done;
    }, ct);
    public Task<CommandOutcome> RemoveUnmatchedCapAsync(string capId, CancellationToken ct) => ChangeAsync(async token =>
    {
        if (!configurationWritable) return CommandOutcome.Unavailable;
        if (!Current.Budget.Caps.Any(c => c.CapId == capId && c.Status == CapStatus.Unmatched)) return CommandOutcome.Rejected;
        var next = configuration with { Caps = configuration.Caps.Where(c => LiveLedgerProjection.CardId(c.Series) != capId).ToArray() };
        await budgets.SaveConfigurationAsync(next, token); configuration = next;
        return CommandOutcome.Done;
    }, ct);
    public async Task<HistoryModel?> GetHistoryAsync(string cardId, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try { return !stopped && limits.TryGetValue(cardId, out var limit) ? LiveLedgerProjection.History(limit, time.GetUtcNow(), zone) : null; }
        finally { gate.Release(); }
    }

    public Task SignInAsync(ProviderKind provider, CancellationToken ct) => ConnectAsync(provider, null, ct);
    public Task ReconnectAsync(string accountId, CancellationToken ct)
    {
        var account = accounts.Current.FirstOrDefault(a => a.AccountId.ToString("N") == accountId);
        return account is null ? Task.CompletedTask : ConnectAsync(LiveLedgerProjection.Provider(account.Provider), account.AccountId, ct);
    }
    private async Task ConnectAsync(ProviderKind provider, Guid? reconnect, CancellationToken token)
    {
        Guid attempt = Guid.NewGuid();
        lock (sync)
        {
            if (stopped || paused || !initialized || strip?.Phase == SignInPhase.Waiting) return;
            strip = new(SignInPhase.Waiting, provider, null, null) { AttemptId = attempt, ReconnectAccountId = reconnect?.ToString("N") };
        }
        QueueRebuild();
        var result = await accounts.ConnectAsync(provider.ToString().ToLowerInvariant(), reconnect, attempt, challenge =>
        {
            lock (sync)
            {
                if (stopped || strip?.AttemptId != attempt || strip.Phase != SignInPhase.Waiting) return;
                strip = strip with { UserCode = challenge.UserCode, AcceptsManualCode = provider == ProviderKind.Claude };
            }
            QueueRebuild();
            openBrowser(challenge.VerificationUri);
        }, token);
        lock (sync)
        {
            if (strip?.AttemptId != attempt) return;
            strip = strip with
            {
                Phase = result.Outcome == AccountOutcome.Done ? SignInPhase.Succeeded : result.Outcome == AccountOutcome.Cancelled ? SignInPhase.Cancelled : SignInPhase.Failed,
                UserCode = null, AcceptsManualCode = false,
                AccountName = accounts.Current.FirstOrDefault(a => a.AccountId == result.AccountId) is { } a ? DefaultName(a, accounts.Current) : null,
                LimitsFound = accounts.Current.FirstOrDefault(a => a.AccountId == result.AccountId)?.Session.Quota?.Limits?.Limits.Count,
                Failure = result.Outcome == AccountOutcome.Duplicate ? SignInFailure.Duplicate : result.Failure switch
                {
                    ProviderFailureKind.AccountMismatch => SignInFailure.WrongAccount,
                    ProviderFailureKind.StorageUnavailable or ProviderFailureKind.RecoveryRequired => SignInFailure.Storage,
                    ProviderFailureKind.AccessDenied => SignInFailure.AccessDenied,
                    ProviderFailureKind.DeviceCodeExpired or ProviderFailureKind.LoginAttemptExpired => SignInFailure.Expired,
                    ProviderFailureKind.BrowserCallbackUnavailable => SignInFailure.Browser,
                    ProviderFailureKind.RegistrationUnavailable => SignInFailure.Registration,
                    _ => SignInFailure.Provider
                }
            };
        }
        QueueRebuild(); await WaitForIdleAsync();
    }
    public bool TrySubmitSignInCode(Guid attemptId, string code)
    {
        lock (sync) return !stopped && strip is { Phase: SignInPhase.Waiting, AcceptsManualCode: true } && strip.AttemptId == attemptId && accounts.TrySubmitCode(attemptId, code);
    }
    public async Task CancelSignInAsync(CancellationToken ct)
    {
        Guid? attempt;
        lock (sync) attempt = strip?.Phase == SignInPhase.Waiting ? strip.AttemptId : null;
        if (attempt is { } id) await accounts.CancelConnectAsync(id);
    }
    public async Task SignOutAsync(string accountId, CancellationToken ct)
    {
        if (Paused) return;
        if (Guid.TryParseExact(accountId, "N", out var id) && (await accounts.DisconnectAsync(id, ct)).Outcome != AccountOutcome.Done)
            localStatus = "Sign-out could not finish; the stored sign-in still needs attention";
        QueueRebuild(); await WaitForIdleAsync();
    }
    public async Task RefreshAccountAsync(string accountId, CancellationToken ct)
    {
        if (Paused) return;
        if (Guid.TryParseExact(accountId, "N", out var id)) await accounts.RefreshAsync(id, ct);
        QueueRebuild(); await WaitForIdleAsync();
    }
    public async Task RefreshAsync(CancellationToken ct)
    {
        if (Paused) return;
        await Task.WhenAll(accounts.Current.Where(a => a.Connected).Select(a => accounts.RefreshAsync(a.AccountId, ct)));
        QueueRebuild(); await WaitForIdleAsync();
    }
    public Task<CommandOutcome> DeleteStoredDataAsync(CancellationToken ct) => DeleteData?.Invoke(ct) ?? Task.FromResult(CommandOutcome.Unavailable);

    public Task TickAsync(CancellationToken token)
    {
        lock (sync) return stopped || paused ? Task.CompletedTask : !tick.IsCompleted ? tick : tick = TickCoreAsync(token);
    }
    private async Task TickCoreAsync(CancellationToken token)
    {
        await Task.Yield();
        QueueRebuild(); await WaitForIdleAsync();
        var now = time.GetUtcNow();
        var due = accounts.Current.Where(a => a.Connected && !a.Busy &&
            a.Session.Status is not (ProviderSessionStatus.ReauthenticationRequired or ProviderSessionStatus.RecoveryRequired) &&
            a.Session.Failure is not (ProviderFailureKind.AccountMismatch or ProviderFailureKind.RegistrationUnavailable or ProviderFailureKind.ProjectUnavailable or ProviderFailureKind.InternalError) &&
            (NextRetry(a) is not { } retry || retry <= now) &&
            (a.Session.Quota is null || a.Session.Failure is not null || now - a.Session.Quota.FetchedAt >= RefreshInterval)).ToArray();
        var results = await Task.WhenAll(due.Select(a => accounts.RefreshAsync(a.AccountId, token)));
        lock (sync)
            for (int i = 0; i < results.Length; i++)
                if (results[i].Outcome != AccountOutcome.Busy)
                    attempts[due[i].AccountId] = (now, results[i].Outcome == AccountOutcome.Done ? 0 : Math.Min(3, attempts.GetValueOrDefault(due[i].AccountId).Failures + 1));
        QueueRebuild(); await WaitForIdleAsync();
    }
    private DateTimeOffset? NextRetry(AccountSnapshot account)
    {
        lock (sync) return attempts.TryGetValue(account.AccountId, out var attempt) ? attempt.At +
            TimeSpan.FromMinutes(account.Session.Failure is not null ? Math.Min(30, 5 << attempt.Failures) : 5) : null;
    }

    public Task StopAsync() { lock (sync) return stopping ??= StopCoreAsync(); }
    private async Task StopCoreAsync()
    {
        await Task.Yield();
        lock (sync) stopped = true;
        accounts.Changed -= AccountsChanged;
        await shutdown.CancelAsync();
        await accounts.StopAsync();
        try { await Task.WhenAll(rebuild, tick); }
        catch (OperationCanceledException) { }
        catch (Exception error) { diagnostics?.Failure(DiagnosticEvent.BackgroundFailure, error); }
        await gate.WaitAsync();
        try { await preferences.StopAsync(); }
        finally { gate.Release(); }
    }
    public void Dispose()
    {
        if (disposed) return;
        if (stopping?.IsCompleted != true) throw new InvalidOperationException("Drain the Ledger source before disposal.");
        disposed = true;
        preferences.Dispose(); gate.Dispose(); shutdown.Dispose();
    }
}
