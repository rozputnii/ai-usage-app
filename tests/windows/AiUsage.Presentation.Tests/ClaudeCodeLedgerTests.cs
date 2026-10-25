using AiUsage.Adapters.Live;
using AiUsage.Core.Accounts;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using Xunit;
using FactValue = AiUsage.Core.Usage.LimitValue;

namespace AiUsage.Presentation.Tests;

/// <summary>A Claude account read through Claude Code on this PC, next to the browser sign-in (menu variant B).</summary>
public sealed class ClaudeCodeLedgerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    private sealed class Accounts : IAccountService
    {
        public IReadOnlyList<AccountSnapshot> Current { get; set; } = [];
        public event EventHandler? Changed;
        public List<(string Provider, Guid? Target)> Connects { get; } = [];
        public AccountResult Result = new(AccountOutcome.Failed);
        public Task InitializeAsync(CancellationToken token) => Task.CompletedTask;
        public Task<AccountResult> ConnectAsync(string provider, Guid? reconnectAccountId, Guid attemptId, Action<AuthorizationChallenge> authorize, CancellationToken token)
        {
            Connects.Add((provider, reconnectAccountId));
            return Task.FromResult(Result);
        }
        public Task CancelConnectAsync(Guid attemptId) => Task.CompletedTask;
        public bool TrySubmitCode(Guid attemptId, string code) => false;
        public Task<AccountResult> RefreshAsync(Guid accountId, CancellationToken token) => Task.FromResult(new AccountResult(AccountOutcome.Done, accountId));
        public Task<AccountResult> DisconnectAsync(Guid accountId, CancellationToken token) => Task.FromResult(new AccountResult(AccountOutcome.Done, accountId));
        public Task StopAsync() => Task.CompletedTask;
        public void Emit() => Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class Store : IReadingSeriesStore, IBudgetConfigurationStore, IQuotaObservationRecorder
    {
        public Task RecordAsync(string accountTarget, string provider, ProviderSessionState state, CancellationToken token) => Task.CompletedTask;
        public Task<StoreWrite> AppendAsync(IReadOnlyList<ReadingObservation> observations, CancellationToken token) => throw new NotSupportedException();
        public Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series, CancellationToken token) =>
            Task.FromResult(new StoreRead<IReadOnlyList<ReadingRun>>([]));
        public Task<StoreRead<BudgetConfiguration>> LoadConfigurationAsync(CancellationToken token) =>
            Task.FromResult(new StoreRead<BudgetConfiguration>(BudgetConfiguration.Default));
        public Task<StoreWrite> SaveConfigurationAsync(BudgetConfiguration configuration, CancellationToken token) => Task.FromResult(new StoreWrite());
    }

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static AccountSnapshot Account(string provider, Guid? id = null, string? plan = "Max")
    {
        var facts = new LimitFacts(new("claude", "CL-W", "weekly"), LimitKind.PercentWindow, "percent", FactValue.Finite(new CountQuantity(100, "percent")))
        { Used = new CountQuantity(52, "percent"), Reset = new(Now.AddDays(2), ValueOrigin.Provider, ResetMeaning.Replenish) };
        var quota = new QuotaSnapshot(Now, plan, [], null, null, null, null) { Limits = new(Now, plan, SnapshotSource.ProviderApi, "test", [facts]) };
        return new(id ?? Guid.NewGuid(), provider, true, new(ProviderSessionStatus.QuotaAvailable, quota, FromCache: true), false, null);
    }

    private static LiveLedgerSource Source(Accounts accounts, bool installed = true) => new(accounts, new Store(), new Store(), new Store(),
        new LedgerPreferenceStore(_ => Task.FromResult<string?>(null), (_, _) => Task.CompletedTask),
        action => { action(); return Task.CompletedTask; }, _ => Assert.Fail("No browser is opened."), new Clock(), TimeZoneInfo.Utc)
        { ClaudeCodeInstalled = () => installed };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClaudeIsOfferedWithTheBrowserAndClaudeCodeMethods(bool installed)
    {
        var accounts = new Accounts { Current = [Account("claude-code")] };
        using var source = Source(accounts, installed);
        try
        {
            await source.InitializeAsync(null, Token);
            var claude = source.Current.Providers.Where(p => p.Provider == ProviderKind.Claude).ToArray();
            Assert.Equal([SignInMethod.Provider, SignInMethod.ClaudeCode], claude.Select(p => p.Method));
            Assert.False(claude[0].Added);
            Assert.True(claude[1].Added);
            Assert.True(claude[0].Available);
            Assert.Equal(installed, claude[1].Available);
            Assert.All(source.Current.Providers.Where(p => p.Provider != ProviderKind.Claude), p => Assert.Equal(SignInMethod.Provider, p.Method));
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task RebuildsReuseTheClaudeCodeLookupForAMinute()
    {
        var accounts = new Accounts();
        var lookups = 0;
        using var source = Source(accounts);
        source.ClaudeCodeInstalled = () => { lookups++; return true; };
        try
        {
            await source.InitializeAsync(null, Token);
            for (var i = 0; i < 3; i++) { accounts.Emit(); await source.WaitForIdleAsync(); }
            Assert.Equal(1, lookups);
            Assert.True(source.Current.Providers.Single(p => p.Method == SignInMethod.ClaudeCode).Available);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task AClaudeCodeAccountIsPresentedAsClaudeAndSaysWhereItComesFrom()
    {
        var accounts = new Accounts { Current = [Account("claude-code"), Account("claude", plan: null)] };
        using var source = Source(accounts);
        try
        {
            await source.InitializeAsync(null, Token);
            var models = source.Current.Accounts;
            Assert.Equal([ProviderKind.Claude, ProviderKind.Claude], models.Select(a => a.Provider));
            Assert.Equal([SignInMethod.ClaudeCode, SignInMethod.Provider], models.Select(a => a.Method));
            Assert.Equal(["Claude Max", "Claude"], models.Select(a => a.DisplayName));
            using var window = new LedgerViewModel(source, new ManualScheduler());
            var tags = window.Cards.Where(c => !c.IsAccountSection).Select(c => c.Tag).ToArray();
            Assert.Equal(new[] { "via Claude Code", null }, tags);
        }
        finally { await source.StopAsync(); }
    }

    [Theory]
    [InlineData(ProviderFailureKind.ProviderUnavailable, (int)SignInFailure.ClaudeCodeMissing, "Claude Code not found")]
    [InlineData(ProviderFailureKind.AuthenticationRequired, (int)SignInFailure.ClaudeCodeSignedOut, "sign in to Claude Code with a subscription first")]
    [InlineData(ProviderFailureKind.InvalidResponse, (int)SignInFailure.Provider, "sign-in failed")]
    public async Task AClaudeCodeSignInUsesItsOwnKeyAndExplainsALocalFailure(ProviderFailureKind kind, int expected, string text)
    {
        var accounts = new Accounts { Result = new(AccountOutcome.Failed, Failure: kind) };
        using var source = Source(accounts);
        try
        {
            await source.InitializeAsync(null, Token);
            using var window = new LedgerViewModel(source, new ManualScheduler());
            await window.SignInAsync(ProviderKind.Claude, SignInMethod.ClaudeCode);
            await source.WaitForIdleAsync();
            Assert.Equal([("claude-code", (Guid?)null)], accounts.Connects);
            var strip = source.Current.SignInStrip!;
            Assert.Equal((SignInFailure)expected, strip.Failure);
            Assert.Equal(SignInMethod.ClaudeCode, strip.Method);
            Assert.False(strip.AcceptsManualCode);
            Assert.Equal(text, window.StripText);

            await window.StripActionAsync();
            await source.WaitForIdleAsync();
            Assert.Equal("claude-code", accounts.Connects[^1].Provider);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task TheBrowserMethodKeepsTheProviderKey()
    {
        var accounts = new Accounts();
        using var source = Source(accounts);
        try
        {
            await source.InitializeAsync(null, Token);
            await source.SignInAsync(ProviderKind.Claude, SignInMethod.Provider, Token);
            await source.SignInAsync(ProviderKind.Codex, Token);
            Assert.Equal(["claude", "codex"], accounts.Connects.Select(c => c.Provider));
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task ReconnectingAClaudeCodeAccountUsesClaudeCodeAgain()
    {
        var id = Guid.NewGuid();
        var accounts = new Accounts { Current = [Account("claude-code", id) with { Connected = false }] };
        using var source = Source(accounts);
        try
        {
            await source.InitializeAsync(null, Token);
            await source.ReconnectAsync(id.ToString("N"), Token);
            Assert.Equal([("claude-code", (Guid?)id)], accounts.Connects);
        }
        finally { await source.StopAsync(); }
    }

    [Fact]
    public async Task TheMenuOpensClaudeIntoItsTwoMethodsAndTheFirstRunListsEachMethod()
    {
        var accounts = new Accounts();
        using var source = Source(accounts, installed: false);
        try
        {
            await source.InitializeAsync(null, Token);
            using var window = new LedgerViewModel(source, new ManualScheduler());
            var group = Assert.Single(window.Providers, p => p.Provider == ProviderKind.Claude);
            Assert.True(group.IsGroup);
            Assert.Equal("▸", group.MenuRight);
            Assert.Equal(4, window.Providers.Count);

            window.ToggleProviderGroup(ProviderKind.Claude);
            var rows = window.Providers.Where(p => p.Provider == ProviderKind.Claude).ToArray();
            Assert.Equal(["Claude", "Sign in with browser", "Use Claude Code on this PC"], rows.Select(r => r.Name));
            Assert.Equal("▾", rows[0].MenuRight);
            Assert.True(rows[1].IsMethod && rows[1].IsEnabled);
            Assert.True(rows[2].IsMethod);
            Assert.False(rows[2].IsEnabled);
            Assert.Equal("not found", rows[2].MenuRight);

            window.ToggleProviderGroup(ProviderKind.Claude);
            Assert.Equal(4, window.Providers.Count);
            Assert.Equal(["Codex", "Claude", "Claude Code", "GitHub Copilot", "Antigravity"],
                window.FirstRunProviders.Select(p => p.Name).OrderBy(n => Order(n)));
            Assert.Equal("Not found", window.FirstRunProviders.Single(p => p.Method == SignInMethod.ClaudeCode).ButtonText);
        }
        finally { await source.StopAsync(); }

        static int Order(string name) => Array.IndexOf(new[] { "Codex", "Claude", "Claude Code", "GitHub Copilot", "Antigravity" }, name);
    }
}
