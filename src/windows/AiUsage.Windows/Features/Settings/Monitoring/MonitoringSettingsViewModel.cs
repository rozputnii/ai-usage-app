using System.Collections.ObjectModel;
using System.Globalization;
using AiUsage.Features.Accounts;
using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Settings.Monitoring;

/// <summary>
/// One threshold rule row (D1 resolution): remaining-percent values entered and shown in the current display unit.
/// Inherited rows show their parent's values; Override creates a rule at this scope; Reset to inherited removes it.
/// </summary>
internal sealed partial class ThresholdRuleViewModel(RuleScope scope, string? targetId, MonitoringSettingsViewModel owner) : ObservableObject
{
    public RuleScope Scope { get; } = scope;
    public string? TargetId { get; } = targetId;
    public string Key => Scope + ":" + TargetId;

    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    [ObservableProperty] public partial string SubText { get; set; } = string.Empty;
    [ObservableProperty] public partial string ValuesText { get; set; } = string.Empty;
    [ObservableProperty] public partial string InheritanceText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsInherited { get; set; }
    [NotifyPropertyChangedFor(nameof(IsNotEditing))]
    [ObservableProperty] public partial bool IsEditing { get; set; }
    [ObservableProperty] public partial string EditText { get; set; } = string.Empty;
    [ObservableProperty] public partial string Error { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial bool CanReset { get; set; }
    [ObservableProperty] public partial string UnitHint { get; set; } = string.Empty;
    [ObservableProperty] public partial IReadOnlyList<int> Effective { get; set; } = [];

    public bool IsNotEditing => !IsEditing;
    public bool HasError => Error.Length > 0;
    public bool HasInheritanceText => InheritanceText.Length > 0;
    partial void OnErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInheritanceTextChanged(string value) => OnPropertyChanged(nameof(HasInheritanceText));

    [RelayCommand]
    private void StartEdit()
    {
        EditText = owner.DisplayList(Effective, ", ");
        Error = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        Error = string.Empty;
    }

    [RelayCommand]
    private Task SaveAsync() => owner.SaveRuleAsync(this);

    [RelayCommand]
    private Task ResetToInheritedAsync() => owner.ResetRuleAsync(this);
}

internal sealed partial class AccountRulesViewModel(string accountId) : ObservableObject
{
    public string AccountId { get; } = accountId;
    [ObservableProperty] public partial string Header { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsExpanded { get; set; }
    [ObservableProperty] public partial ThresholdRuleViewModel? AccountRule { get; set; }
    public ObservableCollection<ThresholdRuleViewModel> WindowRules { get; } = [];
}

/// <summary>In-window notification preview (no Windows toast is dispatched). Deep-links to the account.</summary>
internal sealed partial class ToastViewModel(PresentationContext context) : ObservableObject
{
    private int generation;

    [ObservableProperty] public partial bool IsOpen { get; private set; }
    [ObservableProperty] public partial string Prefix { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Title { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Body { get; private set; } = string.Empty;
    [ObservableProperty] public partial string? TargetAccountId { get; private set; }

    public async Task ShowAsync(string prefix, string title, string body, string? accountId)
    {
        var current = ++generation;
        (Prefix, Title, Body, TargetAccountId) = (prefix, title, body, accountId);
        IsOpen = true;
        context.Announcer.Announce(prefix + ". " + title);
        await context.Clock.Delay(TimeSpan.FromSeconds(6), CancellationToken.None);
        if (current == generation)
            IsOpen = false;
    }

    [RelayCommand]
    private void Dismiss()
    {
        generation++;
        IsOpen = false;
    }

    [RelayCommand]
    private void OpenAccount()
    {
        var target = TargetAccountId;
        Dismiss();
        context.Navigation.Navigate(new(PageKey.Accounts, target));
    }
}

/// <summary>S06 Monitoring and notifications.</summary>
internal sealed partial class MonitoringSettingsViewModel : SnapshotViewModel
{
    private readonly IPreferenceStore preferences;
    private readonly INotificationPreview preview;
    private readonly DemoScenarioController? demo;
    private readonly Dictionary<string, ThresholdRuleViewModel> ruleCache = new(StringComparer.Ordinal);
    private bool applying;

    public MonitoringSettingsViewModel(PresentationContext context, IPreferenceStore preferences, INotificationPreview preview, ToastViewModel toast, DemoScenarioController? demo = null) : base(context)
    {
        this.preferences = preferences;
        this.preview = preview;
        this.demo = demo;
        Toast = toast;
        DisplayLabels = [context.Format.T("Display_Remaining"), context.Format.T("Display_Used")];
        if (demo is not null)
            demo.EnvironmentChanged += (_, _) => Dispatch(() => OnSnapshot(Snapshot));
        Initialize();
    }

    public ToastViewModel Toast { get; }
    public IReadOnlyList<string> DisplayLabels { get; }
    public ObservableCollection<ThresholdRuleViewModel> SharedRules { get; } = [];
    public ObservableCollection<AccountRulesViewModel> AccountRules { get; } = [];
    public bool HasSimulator => demo is not null;

    [ObservableProperty] public partial int DisplayIndex { get; set; }
    [ObservableProperty] public partial string ModeNote { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ResetNotice { get; set; }
    [ObservableProperty] public partial string DeliveryText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool SimulateNotificationsAllowed { get; set; }
    [ObservableProperty] public partial bool SimulateQuietHours { get; set; }
    [ObservableProperty] public partial bool SimulateBatterySaver { get; set; }
    [ObservableProperty] public partial string RefreshPolicyText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ReduceOnBatterySaver { get; set; }
    [ObservableProperty] public partial string QuietHoursText { get; private set; } = string.Empty;

    private bool UsedMode => Snapshot.Preferences.UsageDisplay == UsageDisplay.Used;

    partial void OnDisplayIndexChanged(int value)
    {
        if (applying || value < 0)
            return;
        var display = (UsageDisplay)value;
        _ = SetAndAnnounce(new(PreferenceKey.UsageDisplay, display), Format.T(display == UsageDisplay.Used ? "Announce_ShowUsed" : "Announce_ShowRemaining"));
    }

    partial void OnReduceOnBatterySaverChanged(bool value)
    {
        if (!applying)
            _ = SetAndAnnounce(new(PreferenceKey.ReduceRefreshOnBatterySaver, value), null);
    }

    partial void OnResetNoticeChanged(bool value)
    {
        if (applying)
            return;
        var global = Snapshot.Preferences.NotificationRules.FirstOrDefault(r => r.Scope == RuleScope.Global) ?? Preferences.DefaultGlobalRule;
        _ = preferences.SetNotificationRuleAsync(global with { ResetNotice = value }, CancellationToken.None);
    }

    partial void OnSimulateNotificationsAllowedChanged(bool value)
    {
        if (!applying && demo is not null)
            demo.NotificationsAllowed = value;
    }

    partial void OnSimulateQuietHoursChanged(bool value)
    {
        if (!applying && demo is not null)
            demo.QuietHours = value;
    }

    partial void OnSimulateBatterySaverChanged(bool value)
    {
        if (!applying && demo is not null)
            demo.BatterySaver = value;
    }

    private async Task SetAndAnnounce(PreferenceChange change, string? announcement)
    {
        var result = await preferences.SetPreferenceAsync(change, CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded && announcement is not null)
            Context.Announcer.Announce(announcement);
    }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var prefs = snapshot.Preferences;
        var system = snapshot.System;
        applying = true;
        try
        {
            DisplayIndex = (int)prefs.UsageDisplay;
            ModeNote = format.T(prefs.UsageDisplay == UsageDisplay.Used ? "Display_NoteUsed" : "Display_NoteRemaining");
            var global = prefs.NotificationRules.FirstOrDefault(r => r.Scope == RuleScope.Global) ?? Preferences.DefaultGlobalRule;
            ResetNotice = global.ResetNotice;
            ReduceOnBatterySaver = prefs.ReduceRefreshOnBatterySaver;
            SimulateNotificationsAllowed = system.NotificationsAllowed ?? true;
            SimulateQuietHours = system.QuietHours ?? false;
            SimulateBatterySaver = demo?.BatterySaver ?? false;
            DeliveryText = system.NotificationsAllowed switch
            {
                true => format.T("Delivery_Allowed"),
                false => format.T("Delivery_Blocked"),
                null => format.T("Delivery_Unknown"),
            };
            QuietHoursText = format.T(system.QuietHours == true ? "Quiet_Active" : "Quiet_Inactive");
            RefreshPolicyText = system.RefreshPolicy == RefreshPolicy.Reduced
                ? format.T("Refresh_PolicyReduced")
                : demo?.BatterySaver == true ? format.T("Refresh_PolicyBatteryNotReduced") : format.T("Refresh_PolicyNormal");
            if (snapshot.Mode == UiMode.Live)
            {
                QuietHoursText = format.T("Capability_Unavailable");
                RefreshPolicyText = format.T("Refresh_ManualOnly");
            }

            var accounts = QuotaRules.Ordered(snapshot);
            var shared = new List<ThresholdRuleViewModel> { Rule(RuleScope.Global, null, prefs, format.T("Rules_GlobalLabel"), format.T("Rules_GlobalSub"), null, null, null) };
            foreach (var providerId in accounts.Select(a => a.ProviderId).Distinct())
            {
                var name = Context.Providers.Get(providerId).PresentationName;
                shared.Add(Rule(RuleScope.Provider, providerId, prefs, name, format.T("Rules_ProviderSub"), null, null, providerId));
            }
            var types = accounts.SelectMany(a => a.Contexts.SelectMany(c => c.Groups).SelectMany(g => g.Windows)
                    .Where(w => w.ValueState is not (ValueState.Unlimited or ValueState.Unavailable))
                    .Select(w => (Account: a, Window: w)))
                .DistinctBy(t => QuotaRules.WindowTypeKey(t.Account.ProviderId, t.Window.Label));
            foreach (var (account, window) in types)
                shared.Add(Rule(RuleScope.WindowType, QuotaRules.WindowTypeKey(account.ProviderId, window.Label), prefs, window.Label,
                    format.F("Rules_WindowTypeSub", Context.Providers.Get(account.ProviderId).PresentationName), account, window, account.ProviderId));
            CollectionSync.Sync(SharedRules, shared, r => r.Key, r => r.Key, r => r, (_, _) => { });

            CollectionSync.Sync(AccountRules, accounts, a => a.Id, vm => vm.AccountId, a => new AccountRulesViewModel(a.Id), (vm, a) =>
            {
                vm.Header = format.F("Rules_AccountHeader", a.Label, Context.Providers.Get(a.ProviderId).PresentationName);
                vm.AccountRule = Rule(RuleScope.Account, a.Id, prefs, format.T("Rules_AccountLabel"), format.T("Rules_AccountSub"), a, null, a.ProviderId);
                if (!vm.IsExpanded)
                {
                    vm.WindowRules.Clear();
                    return;
                }
                BuildWindowRules(vm, a, prefs);
            });
        }
        finally { applying = false; }
    }

    private void BuildWindowRules(AccountRulesViewModel vm, AccountItem account, Preferences prefs)
    {
        var windows = account.Contexts.SelectMany(c => c.Groups.SelectMany(g => g.Windows.Select(w => (Context: c, Group: g, Window: w))))
            .Where(t => t.Window.ValueState is not (ValueState.Unlimited or ValueState.Unavailable))
            .DistinctBy(t => t.Window.Id)
            .ToArray();
        var rules = windows.Select(t => Rule(RuleScope.Window, t.Window.Id, prefs,
            account.Contexts.Count > 1 ? Format.F("Rules_WindowLabelContext", t.Context.Label, t.Window.Label) : t.Window.Label,
            t.Group.Label, account, t.Window, account.ProviderId)).ToArray();
        CollectionSync.Sync(vm.WindowRules, rules, r => r.Key, r => r.Key, r => r, (_, _) => { });
    }

    [RelayCommand]
    private void ToggleAccountRules(AccountRulesViewModel? vm)
    {
        if (vm is null)
            return;
        vm.IsExpanded = !vm.IsExpanded;
        if (vm.IsExpanded && Snapshot.Accounts.FirstOrDefault(a => a.Id == vm.AccountId) is { } account)
            BuildWindowRules(vm, account, Snapshot.Preferences);
        else
            vm.WindowRules.Clear();
    }

    private ThresholdRuleViewModel Rule(RuleScope scope, string? target, Preferences prefs, string label, string sub, AccountItem? account, WindowItem? window, string? providerId)
    {
        var format = Format;
        var key = scope + ":" + target;
        if (!ruleCache.TryGetValue(key, out var vm))
            ruleCache[key] = vm = new ThresholdRuleViewModel(scope, target, this);
        var own = prefs.NotificationRules.FirstOrDefault(r => r.Scope == scope && r.TargetId == target && !r.Inherit);
        var parent = QuotaRules.ResolveParent(prefs.NotificationRules, scope, account, window, providerId);
        var effective = own?.RemainingThresholds ?? parent.Remaining;
        vm.Label = label;
        vm.SubText = sub;
        vm.Effective = effective;
        vm.IsInherited = own is null && scope != RuleScope.Global;
        vm.ValuesText = format.F("Rules_Values", DisplayList(effective, " · "), format.T(UsedMode ? "Value_UnitUsed" : "Value_UnitLeft"));
        vm.InheritanceText = scope == RuleScope.Global ? string.Empty
            : own is null ? format.F("Rules_InheritedFrom", format.T("Scope_" + parent.Source)) : format.T("Rules_Override");
        vm.EditLabel = format.T(vm.IsInherited ? "Rules_OverrideAction" : "Rules_EditAction");
        vm.CanReset = own is not null && scope != RuleScope.Global;
        vm.UnitHint = format.T(UsedMode ? "Rules_UnitUsed" : "Rules_UnitRemaining");
        return vm;
    }

    /// <summary>Remaining thresholds rendered in the display unit, most permissive first.</summary>
    internal string DisplayList(IReadOnlyList<int> remaining, string separator)
    {
        var values = UsedMode ? remaining.Select(t => 100 - t).OrderBy(t => t) : remaining.OrderByDescending(t => t);
        return string.Join(separator, values.Select(v => v.ToString(CultureInfo.CurrentCulture)));
    }

    /// <summary>Accepts any number of unique whole numbers 0–100 in the display unit; invalid input keeps the prior rule.</summary>
    internal static bool TryParseThresholds(string text, bool usedMode, out IReadOnlyList<int> remaining)
    {
        remaining = [];
        var parts = (text ?? string.Empty).Split([',', ' ', ';', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;
        var values = new List<int>();
        foreach (var part in parts)
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) || value is < 0 or > 100)
                return false;
            values.Add(usedMode ? 100 - value : value);
        }
        if (values.Distinct().Count() != values.Count)
            return false;
        remaining = values.OrderByDescending(v => v).ToArray();
        return true;
    }

    internal async Task SaveRuleAsync(ThresholdRuleViewModel rule)
    {
        if (!TryParseThresholds(rule.EditText, UsedMode, out var remaining))
        {
            rule.Error = Format.T("Rules_Error");
            return;
        }
        var resetNotice = (Snapshot.Preferences.NotificationRules.FirstOrDefault(r => r.Scope == RuleScope.Global) ?? Preferences.DefaultGlobalRule).ResetNotice;
        var result = await preferences.SetNotificationRuleAsync(new(rule.Scope, rule.TargetId, false, remaining, resetNotice), CancellationToken.None);
        if (result.Status != CommandStatus.Succeeded)
        {
            rule.Error = Format.T("Rules_Error");
            return;
        }
        rule.Error = string.Empty;
        rule.IsEditing = false;
        Context.Announcer.Announce(Format.T("Announce_ThresholdsSaved"));
    }

    internal async Task ResetRuleAsync(ThresholdRuleViewModel rule)
    {
        var result = await preferences.SetNotificationRuleAsync(new(rule.Scope, rule.TargetId, true, [], true), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
            Context.Announcer.Announce(Format.T("Announce_ThresholdsReset"));
    }

    /// <summary>Previews the most urgent visible account in-window, explaining when Windows or quiet hours would hold it.</summary>
    [RelayCommand]
    private async Task PreviewNotificationAsync()
    {
        var snapshot = Snapshot;
        var format = Format;
        var account = QuotaRules.Ordered(snapshot).Where(a => QuotaRules.IsVisible(a, snapshot.Preferences))
            .OrderByDescending(a => QuotaRules.Attention(a, snapshot.Preferences, includeFailures: false)).FirstOrDefault();
        if (account is null)
        {
            Context.Announcer.Announce(format.T("Preview_NoAccount"));
            return;
        }
        var window = QuotaRules.PrimaryWindow(account, snapshot.Preferences);
        var result = await preview.PreviewAsync(new(account.Id, null, window?.Id), CancellationToken.None);
        var prefix = format.T(result.Delivery switch
        {
            NotificationDelivery.BlockedByWindows => "Preview_Blocked",
            NotificationDelivery.HeldByQuietHours => "Preview_Quiet",
            NotificationDelivery.WouldAppear => "Preview_WouldAppear",
            _ => "Preview_Unknown",
        });
        string body;
        if (window is null)
            body = format.T("Row_NoMeasurement");
        else
        {
            var group = QuotaRules.VisibleGroups(QuotaRules.SelectedContext(account, snapshot.Preferences), snapshot.Preferences).First(g => g.Windows.Contains(window));
            var display = new QuotaWindowViewModel(window.Id);
            display.Update(account, group, window, snapshot.Preferences, format);
            body = format.F("Preview_Body", $"{display.ValueText} {display.UnitText}".Trim(), display.ResetExact.Length > 0 ? $"{display.ResetRelative} · {display.ResetExact}" : display.ResetRelative);
        }
        await Toast.ShowAsync(prefix, window is null ? account.Label : format.F("Preview_Title", account.Label, window.Label), body, account.Id);
    }
}
