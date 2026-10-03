using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger.Contract;
using Xunit;
using FactValue = AiUsage.Core.Usage.LimitValue;

namespace AiUsage.Presentation.Tests;

public sealed class LiveLedgerSourceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Accounts : IAccountService
    {
        public IReadOnlyList<AccountSnapshot> Current { get; set; } = [];
        public event EventHandler? Changed;
        public void Emit() => Changed?.Invoke(this, EventArgs.Empty);
        public Func<Guid, AccountResult>? Refresh;
        public List<Guid> Refreshed { get; } = [];
        public Task InitializeAsync(CancellationToken token) => Task.CompletedTask;
        public Task<AccountResult> ConnectAsync(string provider, Guid? reconnectAccountId, Guid attemptId, Action<AuthorizationChallenge> authorize, CancellationToken token) => Task.FromResult(new AccountResult(AccountOutcome.Failed));
        public Task CancelConnectAsync(Guid attemptId) => Task.CompletedTask;
        public bool TrySubmitCode(Guid attemptId, string code) => true;
        public Task<AccountResult> RefreshAsync(Guid accountId, CancellationToken token)
        { Refreshed.Add(accountId); return Task.FromResult(Refresh?.Invoke(accountId) ?? new AccountResult(AccountOutcome.Done, accountId)); }
        public Task<AccountResult> DisconnectAsync(Guid accountId, CancellationToken token) => Task.FromResult(new AccountResult(AccountOutcome.Done, accountId));
        public Task StopAsync() => Task.CompletedTask;
    }
    private sealed class Store : IReadingSeriesStore, IBudgetConfigurationStore, IQuotaObservationRecorder
    {
        public BudgetConfiguration Configuration = BudgetConfiguration.Default;
        public List<string> Captured { get; } = [];
        public bool FailCapture;
        public Task RecordAsync(string accountTarget, string provider, ProviderSessionState state, CancellationToken token)
        { if (FailCapture) throw new IOException(); Captured.Add(accountTarget); return Task.CompletedTask; }
        public Task<StoreWrite> AppendAsync(IReadOnlyList<ReadingObservation> observations, CancellationToken token) => throw new NotSupportedException();
        public Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series, CancellationToken token) => Task.FromResult(new StoreRead<IReadOnlyList<ReadingRun>>([]));
        public Task<StoreRead<BudgetConfiguration>> LoadConfigurationAsync(CancellationToken token) => Task.FromResult(new StoreRead<BudgetConfiguration>(Configuration));
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
            Assert.Null(cap.CapTargetId);
            Assert.Equal(CommandOutcome.Rejected, await source.SetCapAsync(cap.CapId, 100, Token));
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
    public async Task CalendarChangesApplyAtMidnightAndPreserveCaps()
    {
        var clock = new Clock(); var store = new Store(); var id = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account(id, clock.Now, true)] };
        using var source = Source(accounts, store, clock);
        await source.InitializeAsync(null, Token);
        var target = source.Current.Accounts[0].Cards[0].CapTargetId!;
        Assert.Equal(CommandOutcome.Done, await source.SetCapAsync(target, 500, Token));
        Assert.Equal(CommandOutcome.Done, await source.SetWorkDaysAsync(new HashSet<DayOfWeek> { DayOfWeek.Saturday }, Token));
        Assert.Equal(BudgetConfiguration.Default.WorkDays, store.Configuration.WorkDays);
        clock.Now = clock.Now.AddDays(1);
        await source.TickAsync(Token);
        Assert.Equal([DayOfWeek.Saturday], store.Configuration.WorkDays);
        Assert.Single(store.Configuration.Caps);
        Assert.Equal(id.ToString("N"), store.Configuration.Caps[0].Series.AccountTarget);
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
}
