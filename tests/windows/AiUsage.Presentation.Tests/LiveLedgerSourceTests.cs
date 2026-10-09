using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using AiUsage.Features.Ledger;
using Xunit;
using FactValue = AiUsage.Core.Usage.LimitValue;

namespace AiUsage.Presentation.Tests;

public sealed class LiveLedgerSourceTests
{
    [Fact]
    public async Task OpenHistoryUpdatesAfterStoredReadingsChangeAndKeepsItsSelectedDay()
    {
        var clock = new Clock(); var id = Guid.NewGuid();
        var account = Account(id, clock.Now, true);
        var facts = account.Session.Quota!.Limits!.Limits[0];
        var series = new ReadingSeriesKey(id.ToString("N"), facts.Key);
        var midnight = new DateTimeOffset(clock.Now.Date, TimeSpan.Zero);
        ReadingRun Run(decimal value, DateTimeOffset at) => new(series, new CountQuantity(value, "requests"), at, at, "month", null, SnapshotSource.ProviderApi)
            { PeriodStartedAt = midnight.AddDays(-4), ResetAt = facts.Reset!.At };
        var store = new Store { Runs = [Run(10, midnight), Run(20, clock.Now)] };
        var accounts = new Accounts { Current = [account] };
        using var source = Source(accounts, store, clock);
        try
        {
            await source.InitializeAsync(null, Token);
            using var window = new LedgerViewModel(source, new ManualScheduler());
            var card = Assert.Single(window.Cards);
            await window.ToggleHistoryAsync(card);
            Assert.Equal(10, Assert.Single(window.History!.Bars).Value);
            window.History.MoveFocus(-1);
            store.Runs = [Run(10, midnight), Run(35, clock.Now)];
            var quota = account.Session.Quota;
            var changed = facts with { Used = new CountQuantity(35, "requests") };
            accounts.Current = [account with { Session = account.Session with { Quota = quota with { Limits = quota.Limits with { Limits = [changed] } } } }];
            accounts.Emit(); await source.WaitForIdleAsync();
            for (var attempt = 0; attempt < 20 && window.History!.Bars[0].Value != 25; attempt++) await Task.Delay(10, Token);
            Assert.Equal(25, Assert.Single(window.History!.Bars).Value);
            Assert.Equal(33, window.History.FocusIndex);
            await source.RenameAccountAsync(id.ToString("N"), "SYNTHETIC renamed history", Token);
            for (var attempt = 0; attempt < 20 && window.History!.Title != "SYNTHETIC renamed history"; attempt++) await Task.Delay(10, Token);
            Assert.Equal("SYNTHETIC renamed history", window.History!.Title);
            var selected = window.History.FocusDate;
            clock.Now = clock.Now.AddDays(1);
            await source.TickAsync(Token);
            for (var attempt = 0; attempt < 20 && window.History!.FocusIndex != 32; attempt++) await Task.Delay(10, Token);
            Assert.Equal(selected, window.History!.FocusDate);
            Assert.Equal(32, window.History.FocusIndex);
        }
        finally { await source.StopAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingHistoryWhileItsReadIsPendingRejectsTheLateCompletion(bool dispose)
    {
        var clock = new Clock(); var account = Account(Guid.NewGuid(), clock.Now, true);
        var store = new Store(); var accounts = new Accounts { Current = [account] };
        using var source = Source(accounts, store, clock);
        try
        {
            await source.InitializeAsync(null, Token);
            using var window = new LedgerViewModel(source, new ManualScheduler());
            store.HoldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
            store.ReadEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            accounts.Emit();
            await store.ReadEntered.Task.WaitAsync(Token);
            var history = window.ToggleHistoryAsync(Assert.Single(window.Cards));
            Assert.False(history.IsCompleted);
            if (dispose) window.Dispose();
            else window.CloseHistory();
            store.HoldRead.SetResult();
            await source.WaitForIdleAsync(); await history;
            Assert.Null(window.History);
            Assert.False(Assert.Single(window.Cards).IsHistoryOpen);
        }
        finally { store.HoldRead?.TrySetResult(); await source.StopAsync(); }
    }

    [Fact]
    public async Task AHistoryRefreshKeepsNavigationMadeWhileTheReadIsPending()
    {
        var clock = new Clock(); var store = new Store();
        var accounts = new Accounts { Current = [Account(Guid.NewGuid(), clock.Now, true)] };
        using var source = Source(accounts, store, clock);
        try
        {
            await source.InitializeAsync(null, Token);
            using var window = new LedgerViewModel(source, new ManualScheduler());
            await window.ToggleHistoryAsync(Assert.Single(window.Cards));
            var previous = window.History!;
            // Publish holds the source gate. The view-model's earlier event subscriber queues its
            // history read; this subscriber then navigates before that queued read can complete.
            source.Changed += (_, _) => previous.MoveFocus(-1);
            accounts.Emit(); await source.WaitForIdleAsync();
            for (var attempt = 0; attempt < 20 && ReferenceEquals(previous, window.History); attempt++) await Task.Delay(10, Token);
            Assert.NotSame(previous, window.History);
            Assert.Equal(previous.FocusDate, window.History!.FocusDate);
            Assert.Equal(33, window.History.FocusIndex);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task SwitchingHistoryDuringARefreshKeepsTheRequestedAccount()
    {
        var clock = new Clock();
        var store = new Store();
        var accounts = new Accounts { Current = [Account(Guid.NewGuid(), clock.Now, true), Account(Guid.NewGuid(), clock.Now, true)] };
        using var source = Source(accounts, store, clock);
        try
        {
            await source.InitializeAsync(null, Token);
            using var window = new LedgerViewModel(source, new ManualScheduler());
            await window.ToggleHistoryAsync(window.Cards[0]);
            store.HoldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
            store.ReadEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            accounts.Emit(); await store.ReadEntered.Task.WaitAsync(Token);
            var requested = window.Cards[1].CardId;
            var history = window.ToggleHistoryAsync(window.Cards[1]);
            Assert.False(history.IsCompleted);
            store.HoldRead.SetResult();
            await source.WaitForIdleAsync(); await history;
            for (var attempt = 0; attempt < 20 && window.History!.CardId != requested; attempt++) await Task.Delay(10, Token);
            Assert.Equal(requested, window.History!.CardId);
            Assert.False(window.Cards[0].IsHistoryOpen);
            Assert.True(window.Cards[1].IsHistoryOpen);
        }
        finally { store.HoldRead?.TrySetResult(); await source.StopAsync(); }
    }
    [Fact]
    public async Task StripCancelStaysAvailableWhileLoginIsPendingAndHidesTheStrip()
    {
        var accounts = new Accounts { HoldLogin = true };
        using var source = Source(accounts, new Store(), new Clock());
        await source.InitializeAsync(null, Token);
        using var window = new LedgerViewModel(source, new ManualScheduler());
        try
        {
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var signIn = window.SignInCommand.ExecuteAsync(ProviderKind.Claude);
                await source.WaitForIdleAsync();
                Assert.Equal("Cancel", window.StripAction);
                Assert.False(signIn.IsCompleted);
                Assert.True(window.StripActionCommand.CanExecute(null));
                await window.StripActionCommand.ExecuteAsync(null);
                await signIn;
                Assert.Equal(attempt, accounts.CancelledLogins);
                Assert.False(window.HasStrip);
            }
            Assert.Empty(source.Current.Accounts);
        }
        finally { await source.StopAsync(); }
    }
    [Fact]
    public async Task RecoveryDoesNotOfferSignInOrClaimAccountsAreSynced()
    {
        using var source = Source(new Accounts(), new Store(), new Clock());
        try
        {
        await source.SetRecoveryAsync(new("Local data is unavailable", true, false));
        using var window = new LedgerViewModel(source, new ManualScheduler());
        Assert.False(window.IsFirstRun);
        Assert.All(window.Providers, p => Assert.False(p.IsEnabled));
        Assert.Equal("Local data is unavailable", window.Settings.SystemStatusText);
        await window.SignInAsync(ProviderKind.Codex);
        Assert.True(window.IsSettingsOpen);
        Assert.Null(source.Current.SignInStrip);

        await source.InitializeAsync(null, Token);
        await source.SetRecoveryAsync(null);
        Assert.True(window.IsFirstRun);
        Assert.All(window.Providers, p => Assert.True(p.IsEnabled));
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task UpdateStatusSurvivesSnapshotRebuild()
    {
        var clock = new Clock();
        var accounts = new Accounts { Current = [Account(Guid.NewGuid(), clock.Now)] };
        using var source = Source(accounts, new Store(), clock);
        try
        {
            Assert.Equal(UpdateStatus.NotPackaged, source.Current.Summaries.Updates);
            await source.InitializeAsync(null, Token);
            await source.SetUpdatesAsync(new(UpdateState.UpToDate, "2026.10.604.0"));
            await source.TickAsync(Token);
            Assert.Equal(new UpdateStatus(UpdateState.UpToDate, "2026.10.604.0"), source.Current.Summaries.Updates);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task PauseRefreshWaitsForBusyAccounts()
    {
        var clock = new Clock(); var id = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account(id, clock.Now) with { Busy = true }] };
        using var source = Source(accounts, new Store(), clock);
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.False(await source.PauseRefreshAsync(TimeSpan.FromMilliseconds(300), Token));
            // A drain that timed out leaves refresh running.
            accounts.Current = [Account(id, clock.Now)];
            clock.Now = clock.Now.AddMinutes(10);
            await source.TickAsync(Token);
            Assert.Equal([id], accounts.Refreshed);
            accounts.Refreshed.Clear();

            Assert.True(await source.PauseRefreshAsync(TimeSpan.FromMilliseconds(300), Token));
            clock.Now = clock.Now.AddMinutes(10);
            await source.TickAsync(Token);
            Assert.Empty(accounts.Refreshed);
            source.ResumeRefresh();
            await source.TickAsync(Token);
            Assert.Equal([id], accounts.Refreshed);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task ProviderCommandsDoNothingWhileRefreshIsPaused()
    {
        var clock = new Clock(); var id = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account(id, clock.Now)] };
        using var source = Source(accounts, new Store(), clock);
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.True(await source.PauseRefreshAsync(TimeSpan.FromMilliseconds(300), Token));
            await source.RefreshAsync(Token);
            await source.RefreshAccountAsync(id.ToString("N"), Token);
            await source.SignInAsync(ProviderKind.Copilot, Token);
            Assert.Empty(accounts.Refreshed);
            Assert.Null(source.Current.SignInStrip);
            source.ResumeRefresh();
            await source.RefreshAccountAsync(id.ToString("N"), Token);
            Assert.Equal([id], accounts.Refreshed);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task PauseWaitsForAWaitingSignIn()
    {
        var clock = new Clock();
        var accounts = new Accounts { HoldLogin = true };
        using var source = Source(accounts, new Store(), clock);
        try
        {
            await source.InitializeAsync(null, Token);
            var signIn = source.SignInAsync(ProviderKind.Copilot, Token);
            // The strip reaches the snapshot through the asynchronous rebuild; wait for it before reading Current.
            await source.WaitForIdleAsync();
            Assert.Equal(SignInPhase.Waiting, source.Current.SignInStrip?.Phase);
            Assert.False(await source.PauseRefreshAsync(TimeSpan.FromMilliseconds(300), Token));
            await source.CancelSignInAsync(Token);
            await signIn;
            Assert.True(await source.PauseRefreshAsync(TimeSpan.FromMilliseconds(300), Token));
        }
        finally { await source.StopAsync(); }
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Accounts : IAccountService
    {
        public bool HoldLogin;
        public int CancelledLogins;
        private TaskCompletionSource<AccountResult>? login;
        public IReadOnlyList<AccountSnapshot> Current { get; set; } = [];
        public event EventHandler? Changed;
        public void Emit() => Changed?.Invoke(this, EventArgs.Empty);
        public Func<Guid, AccountResult>? Refresh;
        public List<Guid> Refreshed { get; } = [];
        public Task InitializeAsync(CancellationToken token) => Task.CompletedTask;
        public Task<AccountResult> ConnectAsync(string provider, Guid? reconnectAccountId, Guid attemptId, Action<AuthorizationChallenge> authorize, CancellationToken token)
        {
            if (!HoldLogin) return Task.FromResult(new AccountResult(AccountOutcome.Failed));
            login = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return login.Task;
        }
        public Task CancelConnectAsync(Guid attemptId)
        {
            if (login?.TrySetResult(new(AccountOutcome.Cancelled)) == true) CancelledLogins++;
            return Task.CompletedTask;
        }
        public bool TrySubmitCode(Guid attemptId, string code) => true;
        public Task<AccountResult> RefreshAsync(Guid accountId, CancellationToken token)
        { Refreshed.Add(accountId); return Task.FromResult(Refresh?.Invoke(accountId) ?? new AccountResult(AccountOutcome.Done, accountId)); }
        public Task<AccountResult> DisconnectAsync(Guid accountId, CancellationToken token) => Task.FromResult(new AccountResult(AccountOutcome.Done, accountId));
        public Task StopAsync() { login?.TrySetResult(new(AccountOutcome.Cancelled)); return Task.CompletedTask; }
    }
    private sealed class Store : IReadingSeriesStore, IBudgetConfigurationStore, IQuotaObservationRecorder
    {
        public BudgetConfiguration Configuration = BudgetConfiguration.Default;
        public List<string> Captured { get; } = [];
        public bool FailCapture;
        public bool FailLoad;
        public ReadingRun[] Runs = [];
        public TaskCompletionSource? HoldRead;
        public TaskCompletionSource? ReadEntered;
        public Task RecordAsync(string accountTarget, string provider, ProviderSessionState state, CancellationToken token)
        { if (FailCapture) throw new IOException(); Captured.Add(accountTarget); return Task.CompletedTask; }
        public Task<StoreWrite> AppendAsync(IReadOnlyList<ReadingObservation> observations, CancellationToken token) => throw new NotSupportedException();
        public async Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series, CancellationToken token)
        {
            if (HoldRead is { } hold) { ReadEntered?.TrySetResult(); await hold.Task.WaitAsync(token); }
            return new(Runs.Where(r => r.Series == series).ToArray());
        }
        public Task<StoreRead<BudgetConfiguration>> LoadConfigurationAsync(CancellationToken token) =>
            FailLoad ? throw new IOException() : Task.FromResult(new StoreRead<BudgetConfiguration>(Configuration));
        public Task<StoreWrite> SaveConfigurationAsync(BudgetConfiguration configuration, CancellationToken token)
        { Configuration = configuration; return Task.FromResult(new StoreWrite()); }
    }
    private static AccountSnapshot Account(Guid id, DateTimeOffset at, bool cached = false)
    {
        var facts = new LimitFacts(new("copilot", "GH-P", "premium"), LimitKind.CountablePool, "requests", FactValue.Finite(new CountQuantity(1000, "requests")))
        { Used = new CountQuantity(20, "requests"), IsMonthly = true, Reset = new(at.AddDays(20), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var quota = new QuotaSnapshot(at, "Pro", [], null, null, null, null) { Limits = new(at, "Pro", SnapshotSource.ProviderApi, "test", [facts]) };
        return new(id, "copilot", true, new(ProviderSessionStatus.QuotaAvailable, quota, FromCache: cached), false, null);
    }
    private static LiveLedgerSource Source(Accounts accounts, Store store, Clock clock) => new(accounts, store, store, store,
        new LedgerPreferenceStore(_ => Task.FromResult<string?>(null), (_, _) => Task.CompletedTask),
        action => { action(); return Task.CompletedTask; }, _ => { }, clock, TimeZoneInfo.Utc);

    [Fact]
    public async Task MonetaryCapsStayMatchedAndSeparateAcrossRenameAndSourceRestart()
    {
        var clock = new Clock(); var store = new Store(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var facts = new LimitFacts(new("claude", "CL-X", "extra-usage"), LimitKind.MonetaryPool, "USD", FactValue.Finite(new MoneyQuantity(50000, 2, "USD")))
        { Used = new MoneyQuantity(1250, 2, "USD"), AllowsCalendarFallback = true, Enabled = true };
        AccountSnapshot Money(Guid id) => new(id, "claude", true, new(ProviderSessionStatus.QuotaAvailable,
            new QuotaSnapshot(clock.Now, null, [], null, null, null, null) { Limits = new(clock.Now, null, SnapshotSource.ProviderApi, "test", [facts]) }, FromCache: true), false, null);
        var accounts = new Accounts { Current = [Money(first), Money(second)] };
        var key = new ReadingSeriesKey(first.ToString("N"), facts.Key);
        store.Configuration = store.Configuration with { Caps = [new(key, new(new MoneyQuantity(20000, 2, "EUR"), clock.Now))] };
        var before = store.Configuration;
        string? savedPreferences = null;
        LedgerPreferenceStore Preferences() => new(_ => Task.FromResult(savedPreferences), (value, _) => { savedPreferences = value; return Task.CompletedTask; });
        LiveLedgerSource Create() => new(accounts, store, store, store, Preferences(), action => { action(); return Task.CompletedTask; }, _ => { }, clock, TimeZoneInfo.Utc);
        using (var source = Create())
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal(CommandOutcome.Done, await source.RenameAccountAsync(first.ToString("N"), "Personal", Token));
            var cap = Assert.Single(source.Current.Budget.Caps);
            Assert.Equal(CapStatus.CurrencyMismatch, cap.Status);
            Assert.Equal("EUR", cap.Scale.Currency);
            Assert.Equal(cap.CapId, cap.CapTargetId);
            Assert.Equal(before, store.Configuration);
            await source.StopAsync();
        }
        using var restarted = Create();
        await restarted.InitializeAsync(null, Token);
        Assert.Equal("Personal", restarted.Current.Accounts[0].DisplayName);
        Assert.NotEqual(restarted.Current.Accounts[0].Cards[0].CardId, restarted.Current.Accounts[1].Cards[0].CardId);
        Assert.Equal(before, store.Configuration);
        Assert.Empty(store.Captured);
        await restarted.StopAsync();
    }

    [Fact]
    public async Task AccountOrderMovesPersistsAndSurvivesRestart()
    {
        var clock = new Clock(); var store = new Store();
        Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid(), d = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account(a, clock.Now), Account(b, clock.Now), Account(c, clock.Now)] };
        string? saved = null;
        LiveLedgerSource Create() => new(accounts, store, store, store,
            new LedgerPreferenceStore(_ => Task.FromResult(saved), (value, _) => { saved = value; return Task.CompletedTask; }),
            action => { action(); return Task.CompletedTask; }, _ => { }, clock, TimeZoneInfo.Utc);
        static string N(Guid id) => id.ToString("N");
        static string[] Ids(LiveLedgerSource source) => [.. source.Current.Accounts.Select(x => x.AccountId)];

        using (var source = Create())
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal([N(a), N(b), N(c)], Ids(source));
            Assert.Equal(CommandOutcome.Done, await source.MoveAccountAsync(N(c), N(a), Token));
            Assert.Equal([N(c), N(a), N(b)], Ids(source));
            Assert.Equal(CommandOutcome.Done, await source.MoveAccountAsync(N(a), null, Token));
            Assert.Equal([N(c), N(b), N(a)], Ids(source));
            Assert.Equal(CommandOutcome.Rejected, await source.MoveAccountAsync(N(Guid.NewGuid()), null, Token));
            Assert.Equal(CommandOutcome.Rejected, await source.MoveAccountAsync(N(a), N(a), Token));
            Assert.Equal(CommandOutcome.Rejected, await source.MoveAccountAsync(N(a), N(Guid.NewGuid()), Token));
            Assert.Equal([N(c), N(b), N(a)], Ids(source));
            await source.StopAsync();
        }
        using var restarted = Create();
        try
        {
            await restarted.InitializeAsync(null, Token);
            Assert.Equal([N(c), N(b), N(a)], Ids(restarted));
            Assert.Equal([N(c), N(b), N(a)], LedgerTrayViewModel.Project(restarted.Current, restarted.Preferences).Select(r => r.AccountId));
            accounts.Current = [.. accounts.Current, Account(d, clock.Now)];
            accounts.Emit(); await restarted.WaitForIdleAsync();
            Assert.Equal([N(c), N(b), N(a), N(d)], Ids(restarted));
        }
        finally { await restarted.StopAsync(); }
    }

    [Fact]
    public async Task HiddenSectionsPersistAcrossRestartAndThePrimaryCannotBeHidden()
    {
        var clock = new Clock(); var store = new Store(); var id = Guid.NewGuid();
        LimitFacts Pool(string family, string name) => new(new("copilot", family, name), LimitKind.CountablePool, "requests",
            FactValue.Finite(new CountQuantity(300, "requests"))) { Used = new CountQuantity(10, "requests") };
        var account = new AccountSnapshot(id, "copilot", true, new(ProviderSessionStatus.QuotaAvailable,
            new QuotaSnapshot(clock.Now, null, [], null, null, null, null)
            { Limits = new(clock.Now, null, SnapshotSource.ProviderApi, "test", [Pool("GH-P", "premium"), Pool("GH-C", "chat"), Pool("GH-I", "completions")]) },
            FromCache: true), false, null);
        var accounts = new Accounts { Current = [account] };
        string? saved = null;
        LiveLedgerSource Create() => new(accounts, store, store, store,
            new LedgerPreferenceStore(_ => Task.FromResult(saved), (value, _) => { saved = value; return Task.CompletedTask; }),
            action => { action(); return Task.CompletedTask; }, _ => { }, clock, TimeZoneInfo.Utc);

        string chat;
        using (var source = Create())
        {
            await source.InitializeAsync(null, Token);
            var cards = source.Current.Accounts[0].Cards;
            Assert.Equal("Premium requests", AccountCard.Primary(source.Current.Accounts[0])!.ScopeLabel);
            chat = cards.Single(c => c.ScopeLabel == "Chat").CardId;
            Assert.Equal(CommandOutcome.Rejected, await source.SetCardHiddenAsync(cards[0].CardId, true, Token));
            Assert.Equal(CommandOutcome.Rejected, await source.SetCardHiddenAsync("missing", true, Token));
            Assert.Equal(CommandOutcome.Done, await source.SetCardHiddenAsync(chat, true, Token));
            Assert.True(source.Current.Accounts[0].Cards.Single(c => c.CardId == chat).Hidden);
            await source.StopAsync();
        }
        using var restarted = Create();
        await restarted.InitializeAsync(null, Token);
        Assert.Equal([chat], restarted.Current.Accounts[0].Cards.Where(c => c.Hidden).Select(c => c.CardId));
        Assert.Equal(CommandOutcome.Done, await restarted.SetCardHiddenAsync(chat, false, Token));
        Assert.DoesNotContain(restarted.Current.Accounts[0].Cards, c => c.Hidden);
        await restarted.StopAsync();
    }

    [Fact]
    public async Task CachedStartupAndClockChangesDoNotCaptureAndTwoAccountsRemainSeparate()
    {
        var clock = new Clock(); var store = new Store();
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account(first, clock.Now, true), Account(second, clock.Now, true)] };
        using var source = Source(accounts, store, clock);
        await source.InitializeAsync(null, Token);
        Assert.Empty(store.Captured);
        Assert.Equal(2, source.Current.Accounts.Count);
        Assert.Equal(CommandOutcome.Done, await source.RenameAccountAsync(first.ToString("N"), "Work", Token));
        Assert.Equal("Work", source.Current.Accounts[0].DisplayName);
        Assert.NotEqual("Work", source.Current.Accounts[1].DisplayName);
        accounts.Current = [Account(first, clock.Now), Account(second, clock.Now)];
        accounts.Emit(); await source.WaitForIdleAsync();
        Assert.Equal(2, store.Captured.Count);
        accounts.Emit(); await source.WaitForIdleAsync();
        clock.Now = clock.Now.AddMinutes(1);
        await source.TickAsync(Token);
        Assert.Equal(2, store.Captured.Count);
        Assert.False(source.TrySubmitSignInCode(Guid.NewGuid(), "not-an-active-code"));
        await source.StopAsync();
    }

    [Fact]
    public async Task WorkDayChangesApplyAtOnceAndPreserveCaps()
    {
        var clock = new Clock(); var id = Guid.NewGuid();
        var account = Account(id, clock.Now, true);
        var facts = account.Session.Quota!.Limits!.Limits[0];
        var series = new ReadingSeriesKey(id.ToString("N"), facts.Key);
        var midnight = new DateTimeOffset(clock.Now.Date, TimeSpan.Zero);
        ReadingRun Run(decimal value, DateTimeOffset at) => new(series, new CountQuantity(value, "requests"), at, at, "month", null, SnapshotSource.ProviderApi)
            { PeriodStartedAt = midnight.AddDays(-4), ResetAt = facts.Reset!.At };
        var store = new Store { Runs = [Run(10, midnight), Run(20, clock.Now)] };
        using var source = Source(new Accounts { Current = [account] }, store, clock);
        LimitCardModel Card() => source.Current.Accounts[0].Cards[0];
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(Card().CapTargetId!, 500, Token));
            var weekdays = Card().Figures.TodayEnd;
            Assert.NotNull(weekdays);

            var sixDays = new HashSet<DayOfWeek>(BudgetSettingsModel.MondayToFriday) { DayOfWeek.Saturday };
            Assert.Equal(CommandOutcome.Done, await source.SetWorkDaysAsync(sixDays, Token));
            Assert.Equal(sixDays.Order(), store.Configuration.WorkDays);
            Assert.True(source.Current.Budget.WorkDays.SetEquals(sixDays));
            Assert.True(Card().Figures.TodayEnd < weekdays);

            // Monday 5 October stops being a work day without waiting for midnight.
            Assert.Equal(CommandOutcome.Done, await source.SetWorkDaysAsync(new HashSet<DayOfWeek> { DayOfWeek.Saturday }, Token));
            Assert.Equal(DayKind.DayOff, source.Current.Day.Kind);
            Assert.Equal(CardState.DayOff, Card().State);
            var cap = Assert.Single(store.Configuration.Caps);
            Assert.Equal(id.ToString("N"), cap.Series.AccountTarget);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task WorkDaysDeferredByAnEarlierVersionApplyAtOnce()
    {
        var clock = new Clock(); var store = new Store();
        string? saved = null;
        LedgerPreferenceStore Preferences() => new(_ => Task.FromResult(saved), (value, _) => { saved = value; return Task.CompletedTask; });
        using (var earlier = Preferences())
        {
            Assert.True(await earlier.LoadAsync(null, Token));
            Assert.Equal(CommandOutcome.Done, await earlier.ChangeAsync(s => s with
                { PendingWorkDays = [DayOfWeek.Saturday], WorkDaysEffectiveOn = new DateOnly(2026, 10, 6) }, Token));
        }
        using var source = new LiveLedgerSource(new Accounts { Current = [Account(Guid.NewGuid(), clock.Now, true)] }, store, store, store,
            Preferences(), action => { action(); return Task.CompletedTask; }, _ => { }, clock, TimeZoneInfo.Utc);
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal([DayOfWeek.Saturday], store.Configuration.WorkDays);
            Assert.True(source.Current.Budget.WorkDays.SetEquals([DayOfWeek.Saturday]));
            Assert.Equal(DayKind.DayOff, source.Current.Day.Kind);
            using var reopened = Preferences();
            Assert.True(await reopened.LoadAsync(null, Token));
            Assert.Null(reopened.Current.PendingWorkDays);
            Assert.Null(reopened.Current.WorkDaysEffectiveOn);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task WorkDaysAreUnavailableWhileBudgetSettingsNeedRecovery()
    {
        var clock = new Clock(); var store = new Store { FailLoad = true };
        using var source = Source(new Accounts { Current = [Account(Guid.NewGuid(), clock.Now, true)] }, store, clock);
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal(CommandOutcome.Unavailable, await source.SetWorkDaysAsync(new HashSet<DayOfWeek> { DayOfWeek.Saturday }, Token));
            Assert.Equal(BudgetConfiguration.Default.WorkDays, store.Configuration.WorkDays);
            Assert.True(source.Current.Budget.WorkDays.SetEquals(BudgetSettingsModel.MondayToFriday));
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task CapCannotExceedTheProviderLimit()
    {
        var clock = new Clock(); var store = new Store();
        var accounts = new Accounts { Current = [Account(Guid.NewGuid(), clock.Now, true)] };
        using var source = Source(accounts, store, clock);
        await source.InitializeAsync(null, Token);
        var target = source.Current.Accounts[0].Cards[0].CapTargetId!;
        Assert.Equal(CommandOutcome.Rejected, await source.SetCapAsync(target, 1001, Token));
        Assert.Empty(store.Configuration.Caps);
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(target, 1000, Token));
        Assert.Equal(1000m, source.Current.Accounts[0].Cards[0].Cap!.Amount);
        await source.StopAsync();
    }

    [Fact]
    public async Task PercentCapIsStoredWholeAndListedInPercent()
    {
        var clock = new Clock(); var store = new Store(); var id = Guid.NewGuid();
        var weekKey = new LimitKey("claude", "CL-W", "shared");
        var week = new LimitFacts(weekKey, LimitKind.PercentWindow, "percent", FactValue.NotApplicable)
        { UsedPercent = 40, Duration = TimeSpan.FromDays(7), Reset = new(clock.Now.AddDays(4), ValueOrigin.Provider, ResetMeaning.Replenish) };
        store.Runs = [Run(id, weekKey, new CountQuantity(30, "percent"), TrackedFrom), Run(id, weekKey, new CountQuantity(40, "percent"), clock.Now)];
        using var source = Source(new Accounts { Current = [Snapshot(id, "claude", clock.Now, week)] }, store, clock);
        await source.InitializeAsync(null, Token);
        var target = source.Current.Accounts[0].Cards[0].CapTargetId!;
        Assert.Equal(CommandOutcome.Rejected, await source.SetCapAsync(target, 101, Token));
        Assert.Equal(CommandOutcome.Rejected, await source.SetCapAsync(target, 90.5m, Token));
        Assert.Empty(store.Configuration.Caps);
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(target, 90, Token));
        Assert.Equal(new CountQuantity(90, "percent"), Assert.Single(store.Configuration.Caps).Cap.Amount);
        var card = source.Current.Accounts[0].Cards[0];
        Assert.Equal(new CapModel(90, true, CapStatus.Applied), card.Cap);
        Assert.Equal(90, card.Figures.EffectiveLimit);
        var listed = Assert.Single(source.Current.Budget.Caps);
        Assert.Equal(ScaleKind.Percent, listed.Scale.Kind);
        Assert.Equal(90, listed.Amount);
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(target, null, Token));
        Assert.Empty(store.Configuration.Caps);
        await source.StopAsync();
    }

    [Fact]
    public async Task PercentCapOnAFiveHourWindowIsNotAppliedAndCanBeRemoved()
    {
        var clock = new Clock(); var store = new Store(); var id = Guid.NewGuid();
        var shortKey = new LimitKey("codex", "CX-P", "primary");
        var window = new LimitFacts(shortKey, LimitKind.PercentWindow, "percent", FactValue.NotApplicable)
        { UsedPercent = 50, Duration = TimeSpan.FromHours(5), Reset = new(clock.Now.AddHours(3), ValueOrigin.Provider, ResetMeaning.Replenish) };
        store.Runs = [Run(id, shortKey, new CountQuantity(50, "percent"), clock.Now)];
        store.Configuration = store.Configuration with { Caps = [new(new(id.ToString("N"), shortKey), new(new CountQuantity(90, "percent"), clock.Now))] };
        using var source = Source(new Accounts { Current = [Snapshot(id, "codex", clock.Now, window)] }, store, clock);
        await source.InitializeAsync(null, Token);
        var card = source.Current.Accounts[0].Cards[0];
        Assert.Null(card.CapTargetId);
        Assert.Equal(CapStatus.CurrencyMismatch, card.Cap!.Status);
        Assert.NotEqual(CardState.CapReached, card.State);
        var listed = Assert.Single(source.Current.Budget.Caps);
        Assert.Equal(CapStatus.Unmatched, listed.Status);
        Assert.Equal(CommandOutcome.Done, await source.RemoveUnmatchedCapAsync(listed.CapId, Token));
        Assert.Empty(store.Configuration.Caps);
        await source.StopAsync();
    }

    [Fact]
    public async Task CaptureFailureIsVisibleWithoutClaimingSavedHistory()
    {
        var clock = new Clock(); var store = new Store { FailCapture = true };
        var accounts = new Accounts { Current = [Account(Guid.NewGuid(), clock.Now)] };
        using var source = Source(accounts, store, clock);
        await source.InitializeAsync(null, Token);
        Assert.Contains("History", source.Current.Summaries.LocalStatus);
        Assert.Empty(store.Captured);
        await source.StopAsync();
    }

    [Fact]
    public async Task RetryBackoffIsAccountScopedAndStopPreventsFurtherTicks()
    {
        var clock = new Clock(); var store = new Store(); var failing = Guid.NewGuid(); var healthy = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account(failing, clock.Now), Account(healthy, clock.Now)] };
        accounts.Refresh = id =>
        {
            accounts.Current = accounts.Current.Select(a => a.AccountId != id ? a : id == failing
                ? a with { Session = a.Session with { Failure = ProviderFailureKind.RateLimited }, LastFailureAt = clock.Now }
                : Account(id, clock.Now)).ToArray();
            return new(id == failing ? AccountOutcome.Failed : AccountOutcome.Done, id);
        };
        using var source = Source(accounts, store, clock);
        await source.InitializeAsync(null, Token);
        clock.Now = clock.Now.AddMinutes(5); await source.TickAsync(Token);
        Assert.Equal([failing, healthy], accounts.Refreshed);
        clock.Now = clock.Now.AddMinutes(5); await source.TickAsync(Token);
        Assert.Equal([failing, healthy, healthy], accounts.Refreshed);
        clock.Now = clock.Now.AddMinutes(5); await source.TickAsync(Token);
        Assert.Equal(2, accounts.Refreshed.Count(id => id == failing));
        clock.Now = clock.Now.AddMinutes(19); await source.TickAsync(Token);
        Assert.Equal(2, accounts.Refreshed.Count(id => id == failing));
        clock.Now = clock.Now.AddMinutes(1); await source.TickAsync(Token);
        Assert.Equal(3, accounts.Refreshed.Count(id => id == failing));
        await source.RefreshAccountAsync(healthy.ToString("N"), Token);
        Assert.Equal(healthy, accounts.Refreshed[^1]);
        await source.StopAsync();
        var before = accounts.Refreshed.Count;
        clock.Now = clock.Now.AddHours(1); await source.TickAsync(Token);
        Assert.Equal(before, accounts.Refreshed.Count);
    }

    // ---- T-055 R-12 to R-14: the refresh interval ----

    [Fact]
    public async Task RefreshIntervalDrivesTheScheduleWithTickSlack()
    {
        var clock = new Clock(); var id = Guid.NewGuid(); var start = clock.Now;
        var accounts = new Accounts { Current = [Account(id, clock.Now)] };
        // A fetch finishes a moment after the tick that started it.
        accounts.Refresh = refreshed => { accounts.Current = [Account(refreshed, clock.Now.AddSeconds(2))]; return new(AccountOutcome.Done, refreshed); };
        using var source = Source(accounts, new Store(), clock);
        async Task TickAt(TimeSpan after) { clock.Now = start + after; await source.TickAsync(Token); }
        try
        {
            await source.InitializeAsync(null, Token);
            await source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = 1 }, Token);
            for (var minute = 1; minute <= 3; minute++)
            {
                await TickAt(TimeSpan.FromMinutes(minute));
                Assert.Equal(minute, accounts.Refreshed.Count);
            }
            // A tick that runs a second early still refreshes.
            await TickAt(TimeSpan.FromMinutes(4) - TimeSpan.FromSeconds(1));
            Assert.Equal(4, accounts.Refreshed.Count);

            await source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = 5 }, Token);
            await TickAt(TimeSpan.FromMinutes(8) - TimeSpan.FromSeconds(1));
            Assert.Equal(4, accounts.Refreshed.Count);
            await TickAt(TimeSpan.FromMinutes(9) - TimeSpan.FromSeconds(1));
            Assert.Equal(5, accounts.Refreshed.Count);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task ChangedIntervalAppliesAtTheNextTick()
    {
        var clock = new Clock(); var id = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account(id, clock.Now)] };
        using var source = Source(accounts, new Store(), clock);
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal(5, source.Preferences.RefreshMinutes);
            clock.Now = clock.Now.AddMinutes(2); await source.TickAsync(Token);
            Assert.Empty(accounts.Refreshed);
            await source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = 2 }, Token);
            await source.TickAsync(Token);
            Assert.Equal([id], accounts.Refreshed);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task BackoffWinsOverAShortInterval()
    {
        var clock = new Clock(); var id = Guid.NewGuid(); var start = clock.Now;
        var accounts = new Accounts { Current = [Account(id, clock.Now)] };
        accounts.Refresh = failing =>
        {
            accounts.Current = [.. accounts.Current.Select(a => a with { Session = a.Session with { Failure = ProviderFailureKind.RateLimited }, LastFailureAt = clock.Now })];
            return new(AccountOutcome.Failed, failing);
        };
        using var source = Source(accounts, new Store(), clock);
        try
        {
            await source.InitializeAsync(null, Token);
            await source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = 1 }, Token);
            clock.Now = start.AddMinutes(1); await source.TickAsync(Token);
            Assert.Equal([id], accounts.Refreshed);
            for (var minute = 2; minute <= 10; minute++)
            {
                clock.Now = start.AddMinutes(minute); await source.TickAsync(Token);
                Assert.Single(accounts.Refreshed);
            }
            clock.Now = start.AddMinutes(11); await source.TickAsync(Token);
            Assert.Equal([id, id], accounts.Refreshed);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task SummaryReportsTheConfiguredInterval()
    {
        var clock = new Clock();
        using var source = Source(new Accounts { Current = [Account(Guid.NewGuid(), clock.Now)] }, new Store(), clock);
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal(TimeSpan.FromMinutes(5), source.Current.Summaries.RefreshInterval);
            await source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = 10 }, Token);
            Assert.Equal(TimeSpan.FromMinutes(10), source.Current.Summaries.RefreshInterval);
        }
        finally { await source.StopAsync(); }

        // R-14: the demo source reports the interval too, also after it loads another scenario.
        var demo = new AiUsage.Features.Ledger.Demo.DemoLedgerSource(new ManualScheduler());
        Assert.Equal(TimeSpan.FromMinutes(5), demo.Current.Summaries.RefreshInterval);
        await demo.SetPreferencesAsync(demo.Preferences with { RefreshMinutes = 10 }, Token);
        Assert.Equal(TimeSpan.FromMinutes(10), demo.Current.Summaries.RefreshInterval);
        demo.LoadScenario(demo.ScenarioId);
        Assert.Equal(TimeSpan.FromMinutes(10), demo.Current.Summaries.RefreshInterval);
    }

    [Fact]
    public async Task StalenessFollowsTheConfiguredInterval()
    {
        var clock = new Clock(); var id = Guid.NewGuid();
        // The last reading is 40 minutes old and the latest refresh failed.
        var account = Account(id, clock.Now.AddMinutes(-40));
        var failed = account with { Session = account.Session with { Failure = ProviderFailureKind.RateLimited }, LastFailureAt = clock.Now };
        using var source = Source(new Accounts { Current = [failed] }, new Store(), clock);
        try
        {
            await source.InitializeAsync(null, Token);
            Assert.Equal(AccountHealth.SyncFailedStale, source.Current.Accounts[0].Health);
            await source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = 30 }, Token);
            Assert.Equal(AccountHealth.SyncFailedFresh, source.Current.Accounts[0].Health);
            Assert.False(source.Current.Accounts[0].Cards[0].Freshness.IsStale);
        }
        finally { await source.StopAsync(); }
    }

    // ---- R-199: units and today's use ----

    private static readonly DateTimeOffset TrackedFrom = new(2026, 10, 5, 10, 43, 0, TimeSpan.Zero);
    private static readonly LimitKey Premium = new("copilot", "GH-P", "premium");
    private static readonly LimitKey Chat = new("copilot", "GH-C", "chat");
    private static ReadingRun Run(Guid id, LimitKey key, Quantity value, DateTimeOffset at) =>
        new(new(id.ToString("N"), key), value, at, at, "one", null, SnapshotSource.ProviderApi);
    private static AccountSnapshot Snapshot(Guid id, string provider, DateTimeOffset at, params LimitFacts[] limits) =>
        new(id, provider, true, new(ProviderSessionStatus.QuotaAvailable,
            new QuotaSnapshot(at, null, [], null, null, null, null) { Limits = new(at, null, SnapshotSource.ProviderApi, "test", limits) }, FromCache: true), false, null);
    private static (AccountSnapshot Account, ReadingRun[] Runs) CopilotCredits(Guid id, DateTimeOffset now)
    {
        LimitFacts Pool(LimitKey key, decimal limit, decimal used) => new(key, LimitKind.CountablePool, "requests", FactValue.Finite(new CountQuantity(limit, "requests")))
        { Used = new CountQuantity(used, "requests"), IsMonthly = true, Reset = new(now.AddDays(20), ValueOrigin.Provider, ResetMeaning.Replenish) };
        return (Snapshot(id, "copilot", now, Pool(Premium, 17500, 3240), Pool(Chat, 300, 10)),
            [Run(id, Premium, new CountQuantity(3120, "requests"), TrackedFrom), Run(id, Premium, new CountQuantity(3240, "requests"), now),
             Run(id, Chat, new CountQuantity(5, "requests"), TrackedFrom), Run(id, Chat, new CountQuantity(10, "requests"), now)]);
    }
    private static LimitCardModel CardOf(LiveLedgerSource source, string cardId) => source.Current.Accounts.SelectMany(a => a.Cards).Single(c => c.CardId == cardId);
    private static decimal? TodayOf(LimitCardModel card) => card.Figures.Used - card.Figures.DayStart;

    [Fact]
    public async Task UnitsSwitchCardsCapsAndHistoryAndPersist()
    {
        var clock = new Clock(); var store = new Store(); var id = Guid.NewGuid();
        var (account, runs) = CopilotCredits(id, clock.Now);
        store.Runs = runs;
        var accounts = new Accounts { Current = [account] };
        string? saved = null;
        LiveLedgerSource Create() => new(accounts, store, store, store,
            new LedgerPreferenceStore(_ => Task.FromResult(saved), (value, _) => { saved = value; return Task.CompletedTask; }),
            action => { action(); return Task.CompletedTask; }, _ => { }, clock, TimeZoneInfo.Utc);
        string premium;
        using (var source = Create())
        {
            await source.InitializeAsync(null, Token);
            premium = source.Current.Accounts[0].Cards.Single(c => c.ScopeLabel == "Premium requests").CardId;
            var chat = source.Current.Accounts[0].Cards.Single(c => c.ScopeLabel == "Chat").CardId;
            Assert.Equal(ScaleModel.Count("credits"), CardOf(source, premium).Scale);
            Assert.Equal(CommandOutcome.Done, await source.SetUnitsAsync(premium, true, 0.01m, Token));
            Assert.Equal(ScaleModel.Money("USD", 2), CardOf(source, premium).Scale);
            Assert.Equal(32.40m, CardOf(source, premium).Figures.Used);
            Assert.Equal(1.20m, (await source.GetHistoryAsync(premium, Token))!.Days[^1].Used);
            Assert.Equal(CommandOutcome.Rejected, await source.SetUnitsAsync(premium, true, 0m, Token));
            Assert.Equal(CommandOutcome.Rejected, await source.SetUnitsAsync(chat, true, 0.01m, Token));
            Assert.Equal(CommandOutcome.Rejected, await source.SetUnitsAsync("missing", true, 0.01m, Token));
            await source.StopAsync();
        }
        using var restarted = Create();
        await restarted.InitializeAsync(null, Token);
        Assert.Equal(32.40m, CardOf(restarted, premium).Figures.Used);
        await restarted.StopAsync();
    }

    [Fact]
    public async Task DollarCapIsStoredInWholeCreditsWithTheProviderUnit()
    {
        var clock = new Clock(); var store = new Store(); var id = Guid.NewGuid();
        var (account, runs) = CopilotCredits(id, clock.Now);
        store.Runs = runs;
        using var source = Source(new Accounts { Current = [account] }, store, clock);
        await source.InitializeAsync(null, Token);
        var premium = source.Current.Accounts[0].Cards.Single(c => c.ScopeLabel == "Premium requests");
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(premium.CapTargetId!, 6000, Token));
        Assert.Equal(new CountQuantity(6000, "requests"), Assert.Single(store.Configuration.Caps).Cap.Amount);
        Assert.Equal(CommandOutcome.Done, await source.SetUnitsAsync(premium.CardId, true, 0.01m, Token));
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(premium.CapTargetId!, 50.00m, Token));
        Assert.Equal(new CountQuantity(5000, "requests"), Assert.Single(store.Configuration.Caps).Cap.Amount);
        Assert.Equal(CommandOutcome.Rejected, await source.SetCapAsync(premium.CapTargetId!, 175.01m, Token));
        var row = Assert.Single(source.Current.Budget.Caps);
        Assert.Equal((ScaleModel.Money("USD", 2), 50.00m), (row.Scale, row.Amount));
        Assert.Equal(CommandOutcome.Done, await source.SetUnitsAsync(premium.CardId, false, 0.01m, Token));
        row = Assert.Single(source.Current.Budget.Caps);
        Assert.Equal((ScaleModel.Count("credits"), 5000m), (row.Scale, row.Amount));
        await source.StopAsync();
    }

    [Fact]
    public async Task DollarAmountsRoundToCreditsWithinTheLimits()
    {
        var clock = new Clock(); var store = new Store(); var id = Guid.NewGuid();
        var (account, runs) = CopilotCredits(id, clock.Now);
        store.Runs = runs;
        using var source = Source(new Accounts { Current = [account] }, store, clock);
        await source.InitializeAsync(null, Token);
        var premium = source.Current.Accounts[0].Cards.Single(c => c.ScopeLabel == "Premium requests");
        Assert.Equal(CommandOutcome.Done, await source.SetUnitsAsync(premium.CardId, true, 0.04m, Token));
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(premium.CapTargetId!, 1.01m, Token));
        Assert.Equal(new CountQuantity(25, "requests"), Assert.Single(store.Configuration.Caps).Cap.Amount);
        Assert.Equal(1.00m, CardOf(source, premium.CardId).Cap!.Amount);
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(premium.CapTargetId!, null, Token));
        Assert.Equal(CommandOutcome.Done, await source.SetTodayUsedAsync(premium.CardId, 129.60m, Token));
        Assert.Equal(129.60m, TodayOf(CardOf(source, premium.CardId)));
        await source.StopAsync();
    }

    [Fact]
    public async Task TodayValueValidationRejectsInvalidAmounts()
    {
        var clock = new Clock(); var store = new Store(); var copilot = Guid.NewGuid(); var claude = Guid.NewGuid(); var weekly = Guid.NewGuid();
        var (account, runs) = CopilotCredits(copilot, clock.Now);
        var spendKey = new LimitKey("claude", "CL-X", "extra-usage");
        var spend = new LimitFacts(spendKey, LimitKind.MonetaryPool, "USD", FactValue.Finite(new MoneyQuantity(50000, 2, "USD")))
        { Used = new MoneyQuantity(1250, 2, "USD"), AllowsCalendarFallback = true, Enabled = true };
        var weekKey = new LimitKey("claude", "CL-W", "shared");
        var week = new LimitFacts(weekKey, LimitKind.PercentWindow, "percent", FactValue.NotApplicable)
        { UsedPercent = 40, Duration = TimeSpan.FromDays(7), Reset = new(clock.Now.AddDays(4), ValueOrigin.Provider, ResetMeaning.Replenish) };
        store.Runs = [.. runs, Run(claude, spendKey, new MoneyQuantity(1000, 2, "USD"), TrackedFrom), Run(claude, spendKey, new MoneyQuantity(1250, 2, "USD"), clock.Now),
            Run(weekly, weekKey, new CountQuantity(30, "percent"), TrackedFrom), Run(weekly, weekKey, new CountQuantity(40, "percent"), clock.Now)];
        string? saved = """{"Version":1,"Today":[{"Card":"x","Date":"2026-08-31","Instance":"i","DayStart":1},{"Card":"y","Date":"2026-09-01","Instance":"i","DayStart":1}]}""";
        using var source = new LiveLedgerSource(new Accounts { Current = [account, Snapshot(claude, "claude", clock.Now, spend), Snapshot(weekly, "claude", clock.Now, week)] },
            store, store, store, new LedgerPreferenceStore(_ => Task.FromResult<string?>(saved), (value, _) => { saved = value; return Task.CompletedTask; }),
            action => { action(); return Task.CompletedTask; }, _ => { }, clock, TimeZoneInfo.Utc);
        await source.InitializeAsync(null, Token);
        var premium = source.Current.Accounts[0].Cards.Single(c => c.ScopeLabel == "Premium requests").CardId;
        Assert.Equal(CommandOutcome.Done, await source.SetTodayUsedAsync(premium, 900, Token));
        Assert.Equal(900, TodayOf(CardOf(source, premium)));
        Assert.True(CardOf(source, premium).TodayUse!.Manual);
        Assert.DoesNotContain("2026-08-31", saved!);
        Assert.Contains("2026-09-01", saved!);
        Assert.Equal(CommandOutcome.Rejected, await source.SetTodayUsedAsync(premium, -1, Token));
        Assert.Equal(CommandOutcome.Rejected, await source.SetTodayUsedAsync(premium, 3241, Token));
        Assert.Equal(CommandOutcome.Done, await source.SetTodayUsedAsync(premium, null, Token));
        Assert.Equal(120, TodayOf(CardOf(source, premium)));
        Assert.False(CardOf(source, premium).TodayUse!.Manual);
        var money = source.Current.Accounts[1].Cards.Single().CardId;
        Assert.NotNull(CardOf(source, money).TodayUse);
        Assert.Equal(CommandOutcome.Rejected, await source.SetTodayUsedAsync(money, 1.005m, Token));
        Assert.Equal(CommandOutcome.Done, await source.SetTodayUsedAsync(money, 5.00m, Token));
        Assert.Equal(5.00m, TodayOf(CardOf(source, money)));
        var percent = source.Current.Accounts[2].Cards.Single().CardId;
        Assert.Equal(CommandOutcome.Rejected, await source.SetTodayUsedAsync(percent, 5, Token));
        await source.StopAsync();
    }
}
