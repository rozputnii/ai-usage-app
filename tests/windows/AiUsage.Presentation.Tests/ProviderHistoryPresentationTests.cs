using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class ProviderHistoryPresentationTests
{
    private sealed class Source : IProviderHistorySource
    {
        public int Calls;
        public Func<string, HistoryRange, CancellationToken, Task<ProviderHistoryResult>> Fetch = (_, range, _) =>
            Task.FromResult(new ProviderHistoryResult(range, DateTimeOffset.UtcNow,
                [new("usage", HistoryStatus.Available, [new(range.From, range.From, "usage", 12, "tokens", new Dictionary<string, string>())])]));
        public Task<ProviderHistoryResult> GetHistoryAsync(string accountId, HistoryRange range, CancellationToken cancellationToken)
        { Calls++; return Fetch(accountId, range, cancellationToken); }
    }

    [Fact]
    public async Task NavigationLoadsAllAccountsWithoutInitialFiltersOrClockPolling()
    {
        using var host = new TestHost();
        var source = new Source();
        using var vm = new ProviderHistoryViewModel(host.Context, source);
        Assert.Equal(0, source.Calls);
        host.Navigation.Navigate(new(PageKey.History));
        await vm.Pending;
        Assert.NotEmpty(vm.Sections);
        Assert.All(vm.Sections, s => Assert.NotEmpty(s.Reports));
        var calls = source.Calls;
        host.Clock.Advance(TimeSpan.FromHours(1));
        await Task.Yield();
        Assert.Equal(calls, source.Calls);
        await vm.LoadAsync();
        Assert.Equal(calls, source.Calls);
        host.Navigation.Navigate(new(PageKey.Overview));
        host.Navigation.Navigate(new(PageKey.History));
        await vm.Pending;
        Assert.Equal(calls, source.Calls);
    }

    [Fact]
    public async Task FailedRefreshKeepsSameRangeDataAsStaleAndReportsAccountsIndependently()
    {
        using var host = new TestHost();
        var source = new Source();
        using var vm = new ProviderHistoryViewModel(host.Context, source);
        host.Navigation.Navigate(new(PageKey.History, "demo-codex-1"));
        await vm.Pending;
        Assert.Single(vm.Sections);
        source.Fetch = (_, range, _) => Task.FromResult(ProviderHistoryResult.Unavailable(range, HistoryStatus.AccessDenied));
        await vm.LoadAsync(force: true);
        Assert.True(vm.Sections[0].IsStale);
        Assert.NotEmpty(vm.Sections[0].Reports[0].Rows);
        Assert.Contains("access", vm.Sections[0].Message, StringComparison.OrdinalIgnoreCase);
        source.Fetch = (_, range, _) => Task.FromResult(new ProviderHistoryResult(range, host.Clock.UtcNow,
            [new("usage", HistoryStatus.Available, [new(range.From, range.To, "usage", 45, "tokens", new Dictionary<string, string>())])]));
        await vm.LoadAsync(force: true);
        Assert.False(vm.Sections[0].IsStale);
        Assert.Contains(vm.Sections[0].Reports[0].Rows, r => r.Value == "45 tokens");
    }

    [Fact]
    public async Task NavigationAwayRejectsLateResultsEvenWhenSourceIgnoresCancellation()
    {
        using var host = new TestHost();
        var delayed = new TaskCompletionSource<ProviderHistoryResult>();
        var source = new Source { Fetch = (_, _, _) => delayed.Task };
        using var vm = new ProviderHistoryViewModel(host.Context, source);
        host.Navigation.Navigate(new(PageKey.History, "demo-codex-1"));
        await Task.Yield();
        var pending = vm.Pending;
        host.Navigation.Navigate(new(PageKey.Overview));
        delayed.SetResult(new(new(new(2026, 9, 1), new(2026, 9, 15)), DateTimeOffset.UtcNow,
            [new("usage", HistoryStatus.Available, [new(new(2026, 9, 1), new(2026, 9, 1), "usage", 999, "tokens", new Dictionary<string, string>())])]));
        await pending;
        Assert.All(vm.Sections, s => Assert.Empty(s.Reports));
        source.Fetch = (_, range, _) => Task.FromResult(new ProviderHistoryResult(range, host.Clock.UtcNow, [new("usage", HistoryStatus.Empty, [])]));
        host.Navigation.Navigate(new(PageKey.History, "demo-codex-1"));
        await vm.Pending;
        Assert.Equal(2, source.Calls);
        Assert.NotEmpty(Assert.Single(vm.Sections).Reports);
    }

    [Fact]
    public async Task DisconnectDropsCachedDataBeforeTheSameSlotReconnects()
    {
        using var host = new TestHost();
        var source = new Source();
        using var vm = new ProviderHistoryViewModel(host.Context, source);
        host.Navigation.Navigate(new(PageKey.History, "demo-codex-1"));
        await vm.Pending;
        var account = host.State.World.Accounts.Single(a => a.Id == "demo-codex-1");
        account.Connection = ConnectionState.NotConnected;
        host.State.Publish();
        await vm.Pending;
        Assert.DoesNotContain(vm.Sections, s => s.Id == account.Id);
        account.Connection = ConnectionState.Connected;
        host.State.Publish();
        host.Navigation.Navigate(new(PageKey.History, account.Id));
        await vm.Pending;
        Assert.Single(vm.Sections);
        Assert.True(source.Calls >= 2);
    }

    [Fact]
    public async Task ExternalAccountChangeClearsHistoryInsteadOfKeepingItAsStale()
    {
        using var host = new TestHost();
        var source = new Source();
        using var vm = new ProviderHistoryViewModel(host.Context, source);
        host.Navigation.Navigate(new(PageKey.History, "demo-codex-1"));
        await vm.Pending;
        source.Fetch = (_, range, _) => Task.FromResult(ProviderHistoryResult.Unavailable(range, HistoryStatus.AccountChanged));
        await vm.LoadAsync(force: true);
        Assert.False(vm.Sections[0].IsStale);
        Assert.All(vm.Sections[0].Reports, r => Assert.Empty(r.Rows));
        source.Fetch = (_, range, _) => Task.FromResult(ProviderHistoryResult.Unavailable(range, HistoryStatus.AccessDenied));
        await vm.LoadAsync(force: true);
        Assert.False(vm.Sections[0].IsStale);
        Assert.All(vm.Sections[0].Reports, r => Assert.Empty(r.Rows));
    }
}
