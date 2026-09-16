using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

public sealed class AccountTests
{
    [Fact]
    public async Task F04RefreshFailureKeepsStaleValueAndTimestampThenRetryGivesFresh39()
    {
        using var host = new TestHost("F04");
        var accounts = host.Accounts();
        accounts.Select("demo-codex-2");
        var detail = accounts.Detail;
        Assert.Equal("42 %", detail.Hero.ValueText);
        Assert.Equal("◷ Stale · cached Sep 14, 6:00 PM", detail.Pill.Text);

        await detail.RefreshCommand.ExecuteAsync(null);
        Assert.True(detail.Failure.IsVisible);
        Assert.Equal("Network unavailable. Showing the last cached reading.", detail.Failure.Message);
        Assert.Equal("42 %", detail.Hero.ValueText);
        Assert.Equal("◷ Stale · cached Sep 14, 6:00 PM", detail.Pill.Text);
        Assert.Equal(FailureAction.Retry, detail.Failure.Action);
        Assert.True(detail.Failure.ActionEnabled);

        await detail.RetryCommand.ExecuteAsync(null);
        Assert.False(detail.Failure.IsVisible);
        Assert.Equal("39 %", detail.Hero.ValueText);
        Assert.StartsWith("Fresh · 12:00 PM", detail.Pill.Text);
    }

    [Fact]
    public async Task F06ReauthRetainsCacheRateLimitBlocksRetryUntilRetryAtAndReconnectKeepsIdentity()
    {
        using var host = new TestHost("F06");
        var overview = host.Overview();
        var a = overview.FindRow("demo-codex-1")!;
        var b = overview.FindRow("demo-codex-2")!;
        Assert.Equal("Sign-in required", a.Pill.Text);
        Assert.Equal(PillTone.Critical, a.Pill.Tone);
        Assert.Equal("72 %", a.Windows[0].ValueText);
        Assert.Equal(FailureAction.Reconnect, a.Failure.Action);
        Assert.False(a.CanRefresh);

        Assert.Equal(FailureAction.Retry, b.Failure.Action);
        Assert.False(b.Failure.ActionEnabled);
        Assert.Equal("Retry available at 12:02 PM", b.Failure.WaitText);
        Assert.False(b.RefreshCommand.CanExecute(null));
        var blocked = await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.RefreshAccount, "demo-codex-2"), CancellationToken.None);
        Assert.Equal(CommandStatus.Failed, blocked.Status);

        host.Controller.AdvanceClock(TimeSpan.FromMinutes(3));
        Assert.True(b.Failure.ActionEnabled);
        Assert.True(b.RefreshCommand.CanExecute(null));

        // Reconnect A through the sheet: same id, label, order.
        var order = host.Usage.Current.Preferences.AccountOrder.ToArray();
        await a.RetryCommand.ExecuteAsync(null);
        var entry = host.Dialogs.AddAccount.Single();
        Assert.Equal(("codex", "demo-codex-1"), (entry.ProviderId, entry.ReconnectAccountId));
        var sheet = host.AddAccount();
        sheet.Open(entry);
        var connecting = sheet.StartCommand.ExecuteAsync(null);
        await Task.Yield();
        Assert.True(host.Controller.ResolveConnection(AiUsage.Features.Demo.DemoConnectOutcome.Approve));
        await connecting;
        Assert.Equal("Reconnected", sheet.ResultTitle);
        Assert.Equal(ConnectionState.Connected, host.Account("demo-codex-1").Connection);
        Assert.Equal("Personal", host.Account("demo-codex-1").Label);
        Assert.Equal(order, host.Usage.Current.Preferences.AccountOrder);
    }

    [Fact]
    public async Task F06CancelledReconnectRestoresPriorStableState()
    {
        using var host = new TestHost("F06", autoDelays: false);
        var sheet = host.AddAccount();
        sheet.Open(new(AddAccountTab.SignIn, "codex", "demo-codex-1"));
        var running = sheet.StartCommand.ExecuteAsync(null);
        await host.Delays.Advance();
        Assert.True(sheet.IsWaiting);
        sheet.CancelCommand.Execute(null);
        await running;
        Assert.True(sheet.IsMethod);
        Assert.Equal("Cancelled. Nothing was changed.", sheet.Note);
        Assert.Equal(AiUsage.Features.Connection.NoteTone.Neutral, sheet.NoteSeverity);
        Assert.Equal(ConnectionState.ReauthRequired, host.Account("demo-codex-1").Connection);
    }

    [Fact]
    public async Task F07ContextSwitchSharedPoolBadgeAndPreferenceSurvivesTemporaryAbsence()
    {
        using var host = new TestHost();
        var accounts = host.Accounts();
        accounts.Select("demo-claude-1");
        var detail = accounts.Detail;
        Assert.True(detail.HasContexts);
        Assert.Equal(["Workspace A", "Workspace B"], detail.Contexts.Select(c => c.Label));
        Assert.Contains(detail.Groups, g => g.IsSharedPool && g.SharedPoolBadge == "Shared pool · counted once");

        await detail.SelectContextCommand.ExecuteAsync(detail.Contexts[1]);
        Assert.True(detail.Contexts[1].IsSelected);
        Assert.Equal("55 %", detail.Hero.ValueText);

        host.Controller.SecondContextAvailable = false;
        Assert.False(detail.HasContexts);
        Assert.Equal("8 %", detail.Hero.ValueText);
        host.Controller.SecondContextAvailable = true;
        Assert.True(detail.Contexts[1].IsSelected);
        Assert.Equal("55 %", detail.Hero.ValueText);
    }

    [Fact]
    public async Task F07HideOnlyKeepsAlertsWhileHideAndMuteMutesAndBothKeepMonitoring()
    {
        using var host = new TestHost();
        var accounts = host.Accounts();
        accounts.Select("demo-claude-1");
        var modelGroup = accounts.Detail.Groups.Single(g => g.Label == "Model-specific");
        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        await modelGroup.ToggleHideCommand.ExecuteAsync(null);
        Assert.Equal("Hide Model-specific?", host.Dialogs.Requests.Last().Title);
        Assert.Contains("cl-a-g3", host.Usage.Current.Preferences.HiddenTargets);
        Assert.DoesNotContain("cl-a-g3", host.Usage.Current.Preferences.MutedTargets);
        Assert.Equal("Hidden. Monitoring and alerts continue.", host.Announcer.Last);

        host.Dialogs.Next(ConfirmOutcome.Alternate);
        await accounts.Detail.ToggleAccountHiddenCommand.ExecuteAsync(null);
        Assert.Contains("demo-claude-1", host.Usage.Current.Preferences.HiddenTargets);
        Assert.Contains("demo-claude-1", host.Usage.Current.Preferences.MutedTargets);
        Assert.DoesNotContain(host.Overview().Sections.SelectMany(s => s.Rows), r => r.Id == "demo-claude-1");
        // Monitoring continues: refresh still works for the hidden account.
        var refresh = await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.RefreshAccount, "demo-claude-1"), CancellationToken.None);
        Assert.Equal(CommandStatus.Succeeded, refresh.Status);

        host.Dialogs.Next(ConfirmOutcome.Cancelled);
        await accounts.Detail.Groups.First().ToggleHideCommand.ExecuteAsync(null);
        Assert.Equal("Cancelled. Nothing was changed.", host.Announcer.Last);
    }

    [Fact]
    public async Task GroupExpansionIsAutoUntilTheUserChooses()
    {
        using var host = new TestHost();
        var accounts = host.Accounts();
        accounts.Select("demo-claude-1");
        var groups = accounts.Detail.Groups;
        Assert.True(groups[0].IsExpanded);
        Assert.Equal("Expanded · auto", groups[0].ExpansionText);
        Assert.True(groups[1].IsExpanded); // shared pool at 12 % is a warning, so auto expands it
        var models = groups[2];
        Assert.False(models.IsExpanded);
        Assert.Equal("3 windows · lowest 41 % left", models.CollapsedSummary);
        await models.ToggleExpansionCommand.ExecuteAsync(null);
        Assert.True(models.IsExpanded);
        Assert.Equal("Expanded by you", models.ExpansionText);
        await groups[0].ToggleExpansionCommand.ExecuteAsync(null);
        Assert.Equal("Collapsed by you", groups[0].ExpansionText);
    }

    [Fact]
    public async Task RenameValidatesEmptyAndLongLabelsAndKeepsThePriorLabel()
    {
        using var host = new TestHost();
        var accounts = host.Accounts();
        accounts.Select("demo-codex-1");
        var rename = accounts.Detail.Rename;
        accounts.Detail.StartRenameCommand.Execute(null);
        Assert.True(rename.IsEditing);
        rename.LabelText = "   ";
        await rename.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Label cannot be empty.", rename.ErrorText);
        Assert.True(rename.IsEditing);
        rename.LabelText = new string('x', 81);
        await rename.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Keep the label under 80 characters.", rename.ErrorText);
        Assert.Equal("Personal", host.Account("demo-codex-1").Label);
        rename.LabelText = "  Home  ";
        await rename.SaveCommand.ExecuteAsync(null);
        Assert.False(rename.IsEditing);
        Assert.Equal("Home", host.Account("demo-codex-1").Label);
        Assert.Equal("demo-codex-1", host.Account("demo-codex-1").Id);
        accounts.Detail.StartRenameCommand.Execute(null);
        rename.LabelText = "Discard";
        rename.CancelCommand.Execute(null);
        Assert.Equal("Home", accounts.Detail.Label);
    }

    [Fact]
    public async Task DisconnectRequiresConfirmationKeepsIdentityAndHidesByDefault()
    {
        using var host = new TestHost(autoDelays: false);
        var accounts = host.Accounts();
        accounts.Select("demo-codex-1");
        host.Dialogs.Next(ConfirmOutcome.Cancelled);
        await accounts.Detail.DisconnectCommand.ExecuteAsync(null);
        Assert.Equal(ConnectionState.Connected, host.Account("demo-codex-1").Connection);

        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        var disconnect = accounts.Detail.DisconnectCommand.ExecuteAsync(null);
        Assert.True(host.Dialogs.Requests.Last().Destructive);
        Assert.Equal("Disconnecting…", host.Dialogs.Requests.Last().BusyLabel);
        Assert.Equal(AccountOperation.Disconnecting, host.Account("demo-codex-1").Operation);
        await host.Delays.Drain();
        await disconnect;
        var account = host.Account("demo-codex-1");
        Assert.Equal((ConnectionState.NotConnected, "Personal"), (account.Connection, account.Label));
        Assert.Equal("Personal disconnected", host.Announcer.Last);
        Assert.True(accounts.Detail.IsDisconnected);
        Assert.Equal("Connect", accounts.Detail.ReconnectLabel);
        Assert.True(accounts.Detail.ReconnectIsPrimary);
        Assert.False(host.Usage.Current.Preferences.ShowDisconnected);
        Assert.DoesNotContain(host.Overview().Sections.SelectMany(s => s.Rows), r => r.Id == "demo-codex-1");
        accounts.ShowDisconnected = true;
        Assert.Contains(host.Overview().Sections.SelectMany(s => s.Rows), r => r.Id == "demo-codex-1" && r.IsDisconnected);
    }

    [Fact]
    public async Task DeleteStoredDataIsSeparateFromDisconnectAndClearsCachedReadings()
    {
        using var host = new TestHost();
        var accounts = host.Accounts();
        accounts.Select("demo-codex-1");
        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        await accounts.Detail.DeleteStoredDataCommand.ExecuteAsync(null);
        var account = host.Account("demo-codex-1");
        Assert.Equal(ConnectionState.Connected, account.Connection);
        Assert.Null(account.FetchedAt);
        Assert.Equal("Unknown", accounts.Detail.Hero.ValueText);
        // The next observation restores a measurement from the (simulated) provider; nothing was invented meanwhile.
        await accounts.Detail.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("72 %", accounts.Detail.Hero.ValueText);
    }

    [Fact]
    public async Task MenuMoveAndMuteUpdateOrderAndAvailability()
    {
        using var host = new TestHost();
        var accounts = host.Accounts();
        accounts.Select("demo-codex-1");
        Assert.False(accounts.Detail.MoveUpCommand.CanExecute(null));
        await accounts.Detail.MoveDownCommand.ExecuteAsync(null);
        Assert.Equal("demo-codex-1", host.Usage.Current.Preferences.AccountOrder[1]);
        Assert.True(accounts.Detail.MoveUpCommand.CanExecute(null));
        await accounts.Detail.ToggleMuteCommand.ExecuteAsync(null);
        Assert.Equal("Unmute alerts", accounts.Detail.MuteMenuLabel);
        Assert.All(accounts.Detail.Groups.SelectMany(g => g.Windows), w => Assert.True(w.IsMuted));
    }
}
