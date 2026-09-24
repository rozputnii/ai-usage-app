using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class OverviewTests
{
    [Fact]
    public async Task F01FirstRunListsProvidersDirectlyAndCancelLeavesItEmpty()
    {
        using var host = new TestHost("F01", autoDelays: false);
        var overview = host.Overview();
        Assert.True(overview.IsFirstRun);
        Assert.False(overview.HasRows);
        Assert.Empty(overview.Sections);
        var connect = overview.AddAccount;
        Assert.Equal(["codex", "claude", "copilot", "antigravity"], connect.ProviderOptions.Select(p => p.ProviderId));
        var running = connect.ConnectCommand.ExecuteAsync(connect.ProviderOptions[0]);
        Assert.True(connect.IsConnecting);
        connect.CancelCommand.Execute(null);
        await host.Delays.Drain();
        await running;
        Assert.False(connect.ShowStrip);
        Assert.True(overview.IsFirstRun);
        Assert.Empty(host.Usage.Current.Accounts);
    }

    [Fact]
    public async Task LoadingShowsSkeletonUntilTheSeedArrives()
    {
        using var host = new TestHost(autoDelays: false);
        var overview = host.Overview();
        var load = host.Controller.LoadScenarioAsync("F02", openEntry: false);
        Assert.True(overview.IsLoading);
        Assert.False(overview.HasRows);
        Assert.False(overview.IsFirstRun);
        await host.Delays.Drain();
        await load;
        Assert.False(overview.IsLoading);
        Assert.True(overview.HasRows);
    }

    [Fact]
    public void F02GroupsByProviderInManualOrderAndRestoresTheD115Summary()
    {
        using var host = new TestHost();
        var overview = host.Overview();
        Assert.Equal(["codex", "claude", "copilot", "antigravity"], overview.Sections.Select(s => s.ProviderId));
        Assert.Equal(["Personal", "Work"], overview.Sections[0].Rows.Select(r => r.Label));
        Assert.Equal("5", overview.SummaryAccounts);
        Assert.Equal("across 4 providers", overview.SummaryAccountsDetail);
        Assert.Equal("0 % left", overview.SummaryLowest);
        Assert.Contains("Experiments", overview.SummaryLowestSource);
        Assert.Equal("in 1 m", overview.SummaryReset);
        // Summary counts: Work warning (20), Research critical (8), Experiments exhausted; no global percentage anywhere.
        Assert.Equal("1 warning · 1 critical · 1 exhausted · 0 sign-in", overview.SummaryAttentionDetail);
        Assert.DoesNotContain("average", overview.SummaryScope, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shared pools counted once", overview.SummaryScope);
    }

    [Fact]
    public void SharedPoolIsCountedOnceAcrossContexts()
    {
        using var host = new TestHost();
        var prefs = host.Usage.Current.Preferences;
        var pool = new GroupItem("pool", "Shared pool", "demo-shared-1", false, ExpansionPreference.Auto,
            [new WindowItem("demo-shared-1-w", "Pool window", 12, 88, ValueState.Known, null, 18000, null, false)]);
        AccountItem Member(string id) => new(id, "claude", id, null, ConnectionState.Connected, AccountOperation.Idle, Freshness.Fresh, host.Clock.UtcNow, null,
            [new ContextItem(id + "-ctx", "Account", ContextKind.Account, false, [pool])], []);
        var summary = OverviewSummary.Compute([Member("a"), Member("b")], prefs, host.Clock.UtcNow);
        // Two references to demo-shared-1 are one pool: one warning, not two.
        Assert.Equal(1, summary.Warning);
        Assert.Equal("a", summary.LowestAccount!.Id);
        Assert.Equal(12, summary.LowestRemaining);
    }

    [Fact]
    public async Task F02KeyboardReorderAndDropProduceTheSameOrderAndPersistAcrossViews()
    {
        using var host = new TestHost();
        var overview = host.Overview();
        var work = overview.FindRow("demo-codex-2")!;
        Assert.True(work.MoveUpCommand.CanExecute(null));
        await overview.MoveAsync("demo-codex-2", -1);
        Assert.Equal(["Work", "Personal"], overview.Sections[0].Rows.Select(r => r.Label));
        Assert.Contains("moved to position 1 of 2", host.Announcer.Last);

        using var second = new TestHost();
        var other = second.Overview();
        await other.DropAsync("demo-codex-2", "demo-codex-1");
        Assert.Equal(host.Usage.Current.Preferences.AccountOrder, second.Usage.Current.Preferences.AccountOrder);

        // A fresh view (navigation away and back) reads the same persisted order.
        var revisited = host.Overview();
        Assert.Equal(["Work", "Personal"], revisited.Sections[0].Rows.Select(r => r.Label));
        Assert.Equal("demo-codex-2", host.Accounts().Items[0].Id);
        // Cross-provider drops are ignored rather than silently regrouping rows.
        await overview.DropAsync("demo-claude-1", "demo-codex-1");
        Assert.Equal(["Work", "Personal"], overview.Sections[0].Rows.Select(r => r.Label));
    }

    [Fact]
    public async Task F02RefreshAllIsolatesTheFailingAccountAndReportsPartialResult()
    {
        using var host = new TestHost(autoDelays: false);
        var overview = host.Overview();
        var shell = host.Shell();
        var run = shell.RefreshAllCommand.ExecuteAsync(null);
        Assert.True(shell.IsRefreshingAll);
        Assert.Equal("Refreshing…", shell.RefreshAllLabel);
        Assert.False(shell.RefreshAllCommand.CanExecute(null));
        var personal = overview.FindRow("demo-codex-1")!;
        Assert.True(personal.IsRefreshing);
        Assert.Equal("72 %", personal.Windows[0].ValueText);
        await host.Delays.Drain();
        await run;
        Assert.False(shell.IsRefreshingAll);
        Assert.True(shell.ShowResult);
        Assert.True(shell.ResultHasFailures);
        Assert.Equal("4 updated · 1 failed (Work) — cached readings kept", shell.ResultText);
        var work = overview.FindRow("demo-codex-2")!;
        Assert.True(work.Failure.IsVisible);
        Assert.Equal("20 %", work.Windows[0].ValueText);
        Assert.Equal("69 %", personal.Windows[0].ValueText);
        Assert.Equal(FailureAction.Retry, work.Failure.Action);
        shell.DismissResultCommand.Execute(null);
        Assert.False(shell.ShowResult);
    }

    [Fact]
    public async Task RowRefreshShowsUpdatingThenAcknowledgesOnlyAfterNewData()
    {
        using var host = new TestHost(autoDelays: false);
        var overview = host.Overview();
        var row = overview.FindRow("demo-codex-1")!;
        var refresh = row.RefreshCommand.ExecuteAsync(null);
        Assert.True(row.IsRefreshing);
        Assert.False(row.RefreshCommand.CanExecute(null));
        Assert.True(row.CancelRefreshCommand.CanExecute(null));
        Assert.False(row.ShowAck);
        await host.Delays.Advance();
        await refresh;
        Assert.False(row.IsRefreshing);
        Assert.True(row.ShowAck);
        Assert.Equal("Personal updated", host.Announcer.Last);
        await host.Delays.Drain();
        Assert.False(row.ShowAck);
    }

    [Fact]
    public async Task CancellingARefreshRestoresThePriorStateWithoutAFailure()
    {
        using var host = new TestHost(autoDelays: false);
        var overview = host.Overview();
        var row = overview.FindRow("demo-codex-1")!;
        var refresh = row.RefreshCommand.ExecuteAsync(null);
        await row.CancelRefreshCommand.ExecuteAsync(null);
        await host.Delays.Drain();
        await refresh;
        Assert.False(row.IsRefreshing);
        Assert.False(row.Failure.IsVisible);
        Assert.False(row.ShowAck);
        Assert.Equal("72 %", row.Windows[0].ValueText);
        Assert.Equal("Refresh cancelled. Previous reading kept.", host.Announcer.Last);
    }

    [Fact]
    public async Task HiddenAndDisconnectedFiltersProduceAllHiddenNoteWithShowAll()
    {
        using var host = new TestHost();
        var overview = host.Overview();
        foreach (var id in host.Usage.Current.Accounts.Select(a => a.Id).ToArray())
            await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.SetVisibility, id, new VisibilityPayload(true, false)), CancellationToken.None);
        Assert.True(overview.AllHidden);
        Assert.False(overview.HasRows);
        await overview.ShowHiddenAndDisconnectedCommand.ExecuteAsync(null);
        Assert.True(overview.HasRows);
        Assert.Equal("hidden", overview.FindRow("demo-codex-1")!.HiddenTag);
    }

    [Fact]
    public void ExpandedRowShowsOtherGroupsSharedPoolNoteContextsAndExtensions()
    {
        using var host = new TestHost();
        var row = host.Overview().FindRow("demo-claude-1")!;
        Assert.Equal(["Session window", "Weekly window"], row.Windows.Select(w => w.Label));
        Assert.True(row.HasMore);
        Assert.Equal("Show all · 2 more groups", row.ExpandLabel);
        row.ToggleExpandCommand.Execute(null);
        Assert.True(row.IsExpanded);
        Assert.Equal("Show less", row.ExpandLabel);
        Assert.Equal(["Shared pool · counted once", "Model-specific"], row.MoreGroups.Select(g => g.Label));
        Assert.Equal("2 contexts · showing Workspace A", row.MoreContextText);
        Assert.Equal(["Extra usage: $12.50 used · USD", "Credits: 1,250 minor units · currency not reported"], row.ExtensionLines);
        Assert.True(row.HasSparkline);
    }

    [Fact]
    public void F15StressKeepsAllAccountsAndTheLongLabel()
    {
        using var host = new TestHost("F15");
        var overview = host.Overview();
        Assert.Equal(20, overview.Sections.Sum(s => s.Rows.Count));
        Assert.Contains(overview.Sections.SelectMany(s => s.Rows), r => r.Label.Length > 60);
        Assert.All(overview.Sections.SelectMany(s => s.Rows), r => Assert.Equal(2, r.Windows.Count));
        Assert.All(overview.Sections.SelectMany(s => s.Rows), r => Assert.Equal(11, r.MoreGroups.Count));
    }
}
