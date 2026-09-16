using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>S07 tray (D-126/D-127 as resolved in D9), shell lifetime and the demo control panel.</summary>
public sealed class ShellAndTrayTests
{
    [Fact]
    public void TrayRowsSortByAttentionAndKeepManualOrderForTies()
    {
        using var host = new TestHost("F06");
        var shell = host.Shell();
        var tray = host.Tray(shell);
        // Re-auth › failure (Work, rate limited) › exhausted (Experiments) › critical (Research) › normal.
        Assert.Equal(["Personal", "Work", "Experiments", "Research", "Development"], tray.Rows.Select(r => r.Label));
        Assert.Equal(AttentionLevel.ReauthRequired, tray.WorstAttention);
        Assert.Equal("AI Usage · Sign-in required: Personal (Stale Sep 15, 7:00 AM) · 5 accounts · 4 need attention", tray.ToolTip);
        Assert.Equal("5 accounts · 4 need attention", tray.CountText);
        Assert.Equal("Sign-in required · sign-in expired", tray.Rows[0].SubText);
        Assert.Equal("Fresh · 12:00 PM · rate limited", tray.Rows[1].SubText);
        var development = tray.Rows.Single(r => r.Label == "Development");
        Assert.Equal(("Connected · quota unavailable", "Unavailable", false), (development.SubText, development.ValueText, development.HasMeter));
        var research = tray.Rows.Single(r => r.Label == "Research");
        Assert.Equal(("● 8 %", "Resets in 2 h 14 m · Sep 15, 2:14 PM"), (research.ValueText, research.SubText));
    }

    [Fact]
    public async Task TrayRefreshOpenAndSettingsShowTheWindowAndNavigate()
    {
        using var host = new TestHost();
        var shell = host.Shell();
        var tray = host.Tray(shell);
        var closes = 0;
        tray.ClosePopupRequested += (_, _) => closes++;
        await tray.Rows.Single(r => r.Id == "demo-codex-1").RefreshCommand.ExecuteAsync(null);
        Assert.Equal("Personal updated", host.Announcer.Last);
        tray.Rows.Single(r => r.Id == "demo-claude-1").OpenCommand.Execute(null);
        Assert.Equal(new NavigationRequest(PageKey.Accounts, "demo-claude-1"), host.Navigation.Last);
        tray.OpenSettingsCommand.Execute(null);
        Assert.Equal(PageKey.Settings, host.Navigation.Current);
        tray.OpenDashboardCommand.Execute(null);
        Assert.Equal(PageKey.Overview, host.Navigation.Current);
        Assert.Equal(3, host.Lifetime.Shown);
        Assert.Equal(3, closes);
        await tray.RefreshAllCommand.ExecuteAsync(null);
        Assert.True(shell.ShowResult);
        Assert.False(tray.IsRefreshingAll);
    }

    [Fact]
    public async Task ExitRequiresConfirmationShowsTheWindowAndIgnoresRepeatedRequests()
    {
        using var host = new TestHost();
        var shell = host.Shell();
        var tray = host.Tray(shell);
        host.Lifetime.HideToTray();
        host.Dialogs.Next(ConfirmOutcome.Cancelled);
        await tray.ExitCommand.ExecuteAsync(null);
        Assert.Equal(0, host.Lifetime.Exits);
        Assert.False(host.Lifetime.IsHiddenToTray);
        Assert.Equal("Exit AI Usage?", host.Dialogs.Requests.Single().Title);

        host.Dialogs.IsDialogOpen = true;
        await shell.ExitCommand.ExecuteAsync(null);
        Assert.Single(host.Dialogs.Requests);
        host.Dialogs.IsDialogOpen = false;

        host.Dialogs.Next(ConfirmOutcome.Confirmed);
        await shell.ExitCommand.ExecuteAsync(null);
        Assert.Equal(1, host.Lifetime.Exits);
    }

    [Fact]
    public async Task ShellAppliesThemeAndAlwaysOnTopFromPreferencesAndMarksDemo()
    {
        using var host = new TestHost();
        var shell = host.Shell();
        Assert.Equal("Demo · sample data", shell.DemoMarker);
        Assert.Equal([ThemePreference.System], host.Theme.Applied);
        Assert.False(host.Lifetime.AlwaysOnTop);
        await host.Preferences.SetPreferenceAsync(new(AiUsage.Features.Settings.PreferenceKey.Theme, ThemePreference.Light), CancellationToken.None);
        await host.Preferences.SetPreferenceAsync(new(AiUsage.Features.Settings.PreferenceKey.AlwaysOnTop, true), CancellationToken.None);
        Assert.Equal(ThemePreference.Light, host.Theme.Preference);
        Assert.True(host.Lifetime.AlwaysOnTop);
        shell.NavigateCommand.Execute(PageKey.History);
        Assert.Equal(PageKey.History, shell.CurrentPage);
        Assert.True(shell.CanGoBack);
        shell.GoBackCommand.Execute(null);
        Assert.Equal(PageKey.Overview, shell.CurrentPage);
    }

    [Fact]
    public async Task SnapshotUpdatesAreDeliveredThroughTheDispatcher()
    {
        using var host = new TestHost();
        var overview = host.Overview();
        var posts = host.Dispatcher.Posts;
        await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.Rename, "demo-codex-1", new RenamePayload("Home")), CancellationToken.None);
        Assert.Equal("Home", overview.FindRow("demo-codex-1")!.Label);
        Assert.Equal(posts, host.Dispatcher.Posts); // same-thread updates run inline; cross-thread updates are posted
    }

    [Fact]
    public async Task DemoControlSwitchesScenarioAdvancesClockAndDrivesSimulations()
    {
        using var host = new TestHost();
        var demo = host.DemoControl();
        Assert.Equal("F02", demo.SelectedScenario!.Id);
        Assert.Equal(19, demo.Scenarios.Count);
        demo.SelectedScenario = demo.Scenarios.Single(s => s.Id == "F01");
        await Task.Yield();
        Assert.Empty(host.Usage.Current.Accounts);

        demo.AdvanceClockCommand.Execute(null);
        Assert.Equal(DemoScenarioCatalog.T0.AddHours(1), host.Clock.UtcNow);
        await demo.ResetCommand.ExecuteAsync(null);
        Assert.Equal(DemoScenarioCatalog.T0, host.Clock.UtcNow);

        demo.WindowsModeIndex = 2;
        Assert.Equal(EffectiveTheme.Dark, host.Display.SimulatedSystemTheme);
        demo.MotionIndex = 1;
        Assert.True(host.Display.ReducedMotionOverride);
        demo.ScaleIndex = 2;
        Assert.Equal(2, host.Display.ContentScale);
        demo.HighContrast = true;
        Assert.True(host.Display.SimulatedHighContrast);
        demo.ProviderHues = true;
        Assert.True(host.Display.ProviderHues);
        demo.ResizeWindowCommand.Execute("560");
        Assert.Equal((560, 720), host.Display.Resized);

        demo.SelectedScenario = demo.Scenarios.Single(s => s.Id == "F02");
        await Task.Yield();
        demo.OutcomeIndex = (int)DemoRefreshOutcome.RateLimited;
        var result = await host.Usage.ExecuteAsync(host.Context.Command(UiCommandKind.RefreshAccount, "demo-codex-1"), CancellationToken.None);
        Assert.Equal(CommandStatus.Failed, result.Status);
        Assert.Equal(FailureKinds.RateLimited, host.Account("demo-codex-1").Failure!.Kind);

        demo.ToggleHideToTrayCommand.Execute(null);
        Assert.True(host.Lifetime.IsHiddenToTray);
        Assert.Equal("Restore window", demo.HideLabel);
        demo.ToggleHideToTrayCommand.Execute(null);
        Assert.False(host.Lifetime.IsHiddenToTray);
        demo.ShowTrayPopupCommand.Execute(null);
        Assert.Equal(1, host.Lifetime.PopupShown);
    }
}

/// <summary>Confirm dialog state: plain, destructive, alternate, typed confirmation and the busy action.</summary>
public sealed class ConfirmDialogTests
{
    private static AiUsage.Features.Shell.ConfirmDialogViewModel Create(ConfirmRequest request, TestHost host) => new(request, host.Format);

    [Fact]
    public async Task CancelCompletesWithoutRunningTheAction()
    {
        using var host = new TestHost();
        var ran = false;
        var dialog = Create(new("Title", "Body", "Confirm", ConfirmAction: _ => { ran = true; return Task.FromResult<string?>(null); }), host);
        dialog.CancelCommand.Execute(null);
        Assert.Equal(ConfirmOutcome.Cancelled, await dialog.Completion);
        Assert.False(ran);
    }

    [Fact]
    public async Task TypedConfirmationGatesConfirmAndTheAlternateActionReportsItsOwnOutcome()
    {
        using var host = new TestHost();
        var dialog = Create(new("Erase", "Body", "Erase", Destructive: true, AlternateLabel: "Hide only", TypedConfirmation: "RESET"), host);
        Assert.True(dialog.HasTyped);
        Assert.False(dialog.ConfirmEnabled);
        dialog.TypedText = "reset";
        Assert.False(dialog.ConfirmEnabled);
        dialog.TypedText = "RESET";
        Assert.True(dialog.ConfirmEnabled);
        dialog.AlternateCommand.Execute(null);
        Assert.Equal(ConfirmOutcome.Alternate, await dialog.Completion);
    }

    [Fact]
    public async Task FailingActionKeepsTheDialogOpenWithItsMessageAndTheBusyLabel()
    {
        using var host = new TestHost();
        var release = new TaskCompletionSource<string?>();
        var dialog = Create(new("Disconnect", "Body", "Disconnect", BusyLabel: "Disconnecting…", ConfirmAction: _ => release.Task), host);
        var confirm = dialog.ConfirmCommand.ExecuteAsync(null);
        Assert.True(dialog.IsBusy);
        Assert.Equal("Disconnecting…", dialog.ConfirmLabel);
        Assert.False(dialog.TryDismiss());
        release.SetResult("Could not complete the operation.");
        await confirm;
        Assert.False(dialog.IsBusy);
        Assert.Equal("Could not complete the operation.", dialog.ErrorText);
        Assert.Equal("Disconnect", dialog.ConfirmLabel);
        Assert.False(dialog.Completion.IsCompleted);
        Assert.True(dialog.TryDismiss());
        Assert.Equal(ConfirmOutcome.Cancelled, await dialog.Completion);
    }
}
