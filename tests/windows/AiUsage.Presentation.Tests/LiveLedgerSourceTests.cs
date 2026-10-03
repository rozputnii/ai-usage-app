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
        public Task InitializeAsync(CancellationToken token) => Task.CompletedTask;
        public Task<AccountResult> ConnectAsync(string provider, Guid? reconnectAccountId, Guid attemptId, Action<AuthorizationChallenge> authorize, CancellationToken token) => Task.FromResult(new AccountResult(AccountOutcome.Failed));
        public Task CancelConnectAsync(Guid attemptId) => Task.CompletedTask;
        public bool TrySubmitCode(Guid attemptId, string code) => true;
        public Task<AccountResult> RefreshAsync(Guid accountId, CancellationToken token) => Task.FromResult(new AccountResult(AccountOutcome.Done, accountId));
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
}
