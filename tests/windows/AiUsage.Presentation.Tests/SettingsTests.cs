using AiUsage.Features.Presentation;
using AiUsage.Features.Settings.Monitoring;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>F10 preferences: appearance, usage display, threshold inheritance, delivery and power simulation.</summary>
public sealed class SettingsTests
{
    [Fact]
    public async Task ThemeChoiceAppliesImmediatelyAndSystemFollowsWindows()
    {
        using var host = new TestHost();
        var shell = host.Shell();
        var appearance = host.Appearance();
        Assert.Equal(ThemePreference.System, host.Theme.Preference);
        Assert.Equal("System (light now)", appearance.ThemeOptions[0].Label);
        Assert.True(appearance.ThemeOptions[0].IsSelected);
        Assert.Contains("Following Windows: currently light", appearance.ThemeNote);

        host.Theme.ChangeSystem(EffectiveTheme.Dark);
        Assert.Equal("System (dark now)", appearance.ThemeOptions[0].Label);
        Assert.True(appearance.ThemeOptions[0].PreviewIsDark);

        await appearance.SelectThemeCommand.ExecuteAsync(appearance.ThemeOptions[2]);
        Assert.Equal(ThemePreference.Dark, host.Theme.Preference);
        Assert.Equal(ThemePreference.Dark, host.Usage.Current.Preferences.Theme);
        Assert.True(appearance.ThemeOptions[2].IsSelected);
        Assert.Equal("Fixed to Dark. Windows mode changes are ignored until you choose System.", appearance.ThemeNote);
        Assert.Equal("Theme Dark", host.Announcer.Last);
        _ = shell;
    }

    [Fact]
    public async Task DensityAlwaysOnTopAndOrderListApplyWithoutPendingState()
    {
        using var host = new TestHost();
        var shell = host.Shell();
        var appearance = host.Appearance();
        appearance.DensityIndex = 1;
        Assert.Equal(Density.Compact, host.Usage.Current.Preferences.Density);
        Assert.True(host.Overview().IsCompactDensity);
        appearance.AlwaysOnTop = true;
        Assert.True(host.Lifetime.AlwaysOnTop);
        Assert.Equal(5, appearance.OrderItems.Count);
        Assert.False(appearance.OrderItems[0].MoveUpCommand.CanExecute(null));
        await appearance.OrderItems[0].MoveDownCommand.ExecuteAsync(null);
        Assert.Equal(["Work", "Personal"], appearance.OrderItems.Take(2).Select(o => o.Label));
        await appearance.OrderItems[1].ToggleHiddenCommand.ExecuteAsync(null);
        Assert.True(appearance.OrderItems[1].IsHidden);
        Assert.Equal("Show", appearance.OrderItems[1].HideLabel);
        await appearance.OrderItems[1].ToggleMutedCommand.ExecuteAsync(null);
        Assert.Equal("Unmute", appearance.OrderItems[1].MuteLabel);
        Assert.Contains("position 2 of 5, hidden, alerts muted", appearance.OrderItems[1].AccessibleName);
        _ = shell;
    }

    [Fact]
    public void UsedRemainingFlipChangesRuleUnitsWithoutChangingStoredThresholds()
    {
        using var host = new TestHost();
        var monitoring = host.Monitoring();
        var global = monitoring.SharedRules[0];
        Assert.Equal("25 · 10 · 0 left", global.ValuesText);
        monitoring.DisplayIndex = (int)UsageDisplay.Used;
        Assert.Equal(UsageDisplay.Used, host.Usage.Current.Preferences.UsageDisplay);
        Assert.Equal("75 · 90 · 100 used", global.ValuesText);
        Assert.Equal([25, 10, 0], host.Usage.Current.Preferences.NotificationRules.Single().RemainingThresholds);
        Assert.StartsWith("Meters fill as quota is consumed", monitoring.ModeNote);
    }

    [Fact]
    public async Task AccountAndWindowTypeOverridesLeaveGlobalUnchangedAndResetRestoresInheritance()
    {
        using var host = new TestHost();
        var monitoring = host.Monitoring();
        Assert.Equal(["Global default", "Codex", "Claude", "Copilot", "Antigravity"], monitoring.SharedRules.Take(5).Select(r => r.Label));
        var type = monitoring.SharedRules.Single(r => r.Scope == RuleScope.WindowType && r.TargetId == "codex::5-hour window");
        Assert.True(type.IsInherited);
        Assert.Equal("Inherited from global", type.InheritanceText);
        Assert.Equal("Override", type.EditLabel);

        type.StartEditCommand.Execute(null);
        Assert.Equal("25, 10, 0", type.EditText);
        type.EditText = "40, 40";
        await type.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Enter unique whole numbers from 0 to 100, separated by commas. Any number of thresholds is allowed.", type.Error);
        type.EditText = "101";
        await type.SaveCommand.ExecuteAsync(null);
        Assert.True(type.HasError);
        type.EditText = "50, 30, 15, 5";
        await type.SaveCommand.ExecuteAsync(null);
        Assert.False(type.IsEditing);
        Assert.Equal("Override", type.InheritanceText);
        Assert.True(type.CanReset);

        var personal = monitoring.AccountRules.Single(r => r.AccountId == "demo-codex-1");
        // An account rule inherits from provider, then global; window-type rules only apply to their windows.
        Assert.Equal("Inherited from global", personal.AccountRule!.InheritanceText);
        monitoring.ToggleAccountRulesCommand.Execute(personal);
        var windowRule = personal.WindowRules.Single(r => r.TargetId == "c1-w1");
        Assert.Equal("Inherited from window type", windowRule.InheritanceText);
        Assert.Equal("50 · 30 · 15 · 5 left", windowRule.ValuesText);

        personal.AccountRule!.StartEditCommand.Execute(null);
        personal.AccountRule.EditText = "20";
        await personal.AccountRule.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Inherited from account", personal.WindowRules.Single(r => r.TargetId == "c1-w1").InheritanceText);
        Assert.Equal([25, 10, 0], host.Usage.Current.Preferences.NotificationRules.Single(r => r.Scope == RuleScope.Global).RemainingThresholds);

        await personal.AccountRule.ResetToInheritedCommand.ExecuteAsync(null);
        await type.ResetToInheritedCommand.ExecuteAsync(null);
        Assert.Single(host.Usage.Current.Preferences.NotificationRules);
        Assert.Equal("Inherited from global", personal.WindowRules.Single(r => r.TargetId == "c1-w1").InheritanceText);
        Assert.Equal("25 · 10 · 0 left", personal.WindowRules.Single(r => r.TargetId == "c1-w1").ValuesText);
    }

    [Fact]
    public void ThresholdsEnteredInUsedUnitAreStoredAsRemaining()
    {
        Assert.True(MonitoringSettingsViewModel.TryParseThresholds("50, 80", usedMode: true, out var remaining));
        Assert.Equal([50, 20], remaining);
        Assert.True(MonitoringSettingsViewModel.TryParseThresholds("25 10 0", usedMode: false, out remaining));
        Assert.Equal([25, 10, 0], remaining);
        Assert.False(MonitoringSettingsViewModel.TryParseThresholds("", usedMode: false, out _));
        Assert.False(MonitoringSettingsViewModel.TryParseThresholds("1.5", usedMode: false, out _));
        Assert.False(MonitoringSettingsViewModel.TryParseThresholds("10, 10", usedMode: false, out _));
    }

    [Fact]
    public async Task OverrideChangesSeverityOnMetersImmediately()
    {
        using var host = new TestHost();
        var overview = host.Overview();
        var personal = overview.FindRow("demo-codex-1")!;
        Assert.Equal(QuotaSeverity.Normal, personal.Windows[0].Severity);
        await host.Preferences.SetNotificationRuleAsync(new(RuleScope.Account, "demo-codex-1", false, [80, 75], true), CancellationToken.None);
        Assert.Equal(QuotaSeverity.Critical, personal.Windows[0].Severity);
        Assert.Equal("●", personal.Windows[0].Glyph);
        Assert.Equal(QuotaSeverity.Warning, overview.FindRow("demo-codex-2")!.Windows[0].Severity);
    }

    [Fact]
    public async Task NotificationPreviewExplainsWindowsBlockAndQuietHoursAndDeepLinks()
    {
        using var host = new TestHost();
        var monitoring = host.Monitoring();
        await monitoring.PreviewNotificationCommand.ExecuteAsync(null);
        Assert.False(host.Toast.IsOpen); // auto-dismissed because delays complete immediately in this host
        Assert.Equal("Preview · would appear in Windows Notification Center", host.Toast.Prefix);
        Assert.Equal("Experiments · Session window", host.Toast.Title);

        monitoring.SimulateNotificationsAllowed = false;
        Assert.StartsWith("Windows has notifications turned off", monitoring.DeliveryText);
        await monitoring.PreviewNotificationCommand.ExecuteAsync(null);
        Assert.Equal("Preview only · Windows notifications are off for this app", host.Toast.Prefix);

        monitoring.SimulateNotificationsAllowed = true;
        monitoring.SimulateQuietHours = true;
        await monitoring.PreviewNotificationCommand.ExecuteAsync(null);
        Assert.Equal("Preview only · quiet hours active, would be held", host.Toast.Prefix);
        host.Toast.OpenAccountCommand.Execute(null);
        Assert.Equal(new NavigationRequest(PageKey.Accounts, "demo-antigravity-1"), host.Navigation.Last);
    }

    [Fact]
    public void BatterySaverReducesAutomaticRefreshButKeepsManualRefreshAndRespectsTheOverride()
    {
        using var host = new TestHost();
        var monitoring = host.Monitoring();
        Assert.StartsWith("Automatic refresh follows", monitoring.RefreshPolicyText);
        monitoring.SimulateBatterySaver = true;
        Assert.Equal(RefreshPolicy.Reduced, host.Usage.Current.System.RefreshPolicy);
        Assert.StartsWith("Battery saver or a metered connection is active: automatic refresh runs less often.", monitoring.RefreshPolicyText);
        Assert.True(host.Overview().FindRow("demo-codex-1")!.RefreshCommand.CanExecute(null));
        monitoring.ReduceOnBatterySaver = false;
        Assert.Equal(RefreshPolicy.Normal, host.Usage.Current.System.RefreshPolicy);
        Assert.Contains("keeps its normal schedule", monitoring.RefreshPolicyText);
    }

    [Fact]
    public void ResetNoticeIsStoredOnTheGlobalRule()
    {
        using var host = new TestHost();
        var monitoring = host.Monitoring();
        Assert.True(monitoring.ResetNotice);
        monitoring.ResetNotice = false;
        Assert.False(host.Usage.Current.Preferences.NotificationRules.Single().ResetNotice);
    }
}
