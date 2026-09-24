using AiUsage.Features.Presentation;
using AiUsage.Features.Settings.Monitoring;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Shell;

/// <summary>
/// Application shell: one main usage view without tabs, header actions (Back, Refresh all, Add account menu, Settings),
/// compatibility banner, Refresh-all result bar, notification preview host, recovery takeover, always-on-top
/// application and confirmed Exit.
/// </summary>
internal sealed partial class ShellViewModel : SnapshotViewModel
{
    private readonly IAppLifetime lifetime;
    private RefreshAllSummary? dismissedResult;
    private bool exiting;
    private bool? appliedOnTop;

    public ShellViewModel(PresentationContext context, IAppLifetime lifetime, ToastViewModel toast, bool isDemo) : base(context)
    {
        this.lifetime = lifetime;
        Toast = toast;
        IsDemo = isDemo;
        context.Navigation.Navigated += (_, request) => Dispatch(() => CurrentPage = request.Page);
        CurrentPage = context.Navigation.Current;
        Initialize();
    }

    public ToastViewModel Toast { get; }
    public bool IsDemo { get; }

    [NotifyPropertyChangedFor(nameof(CanGoBack), nameof(IsOverview), nameof(IsSettings))]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    [ObservableProperty] public partial PageKey CurrentPage { get; private set; }
    [ObservableProperty] public partial bool IsRecovery { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(RefreshAllCommand))]
    [ObservableProperty] public partial bool IsRefreshingAll { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(RefreshAllCommand))]
    [ObservableProperty] public partial bool HasEligibleAccounts { get; private set; }
    [ObservableProperty] public partial string RefreshAllLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial string RefreshAllDisabledReason { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ShowCompatibilityBanner { get; private set; }
    [ObservableProperty] public partial bool CompatibilityIsSecurity { get; private set; }
    [ObservableProperty] public partial string CompatibilityTitle { get; private set; } = string.Empty;
    [ObservableProperty] public partial string CompatibilityBody { get; private set; } = string.Empty;
    [NotifyCanExecuteChangedFor(nameof(RefreshAllCommand))]
    [ObservableProperty] public partial bool RefreshBlocked { get; private set; }
    [ObservableProperty] public partial bool ShowResult { get; private set; }
    [ObservableProperty] public partial string ResultText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ResultHasFailures { get; private set; }
    [ObservableProperty] public partial string DemoMarker { get; private set; } = string.Empty;

    public bool CanGoBack => CurrentPage != PageKey.Overview;
    public bool IsOverview => CurrentPage == PageKey.Overview;
    public bool IsSettings => CurrentPage == PageKey.Settings;

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var prefs = snapshot.Preferences;
        var system = snapshot.System;
        DemoMarker = IsDemo ? format.T("Demo_Marker") : string.Empty;
        IsRecovery = system.Recovery != RecoveryState.None;
        if (appliedOnTop != prefs.AlwaysOnTop)
        {
            appliedOnTop = prefs.AlwaysOnTop;
            lifetime.SetAlwaysOnTop(prefs.AlwaysOnTop);
        }

        var blocked = system.Compatibility != CompatibilityState.Normal && !system.CompatibilityOverridden;
        RefreshBlocked = blocked;
        ShowCompatibilityBanner = system.Compatibility != CompatibilityState.Normal;
        CompatibilityIsSecurity = system.Compatibility == CompatibilityState.SecurityBlocked;
        CompatibilityTitle = format.T(CompatibilityIsSecurity ? "Compat_SecurityTitle" : "Compat_BlockedTitle");
        CompatibilityBody = format.T(CompatibilityIsSecurity ? "Compat_SecurityBody"
            : system.CompatibilityOverridden ? "Compat_OverriddenBody" : "Compat_BlockedBody");

        HasEligibleAccounts = snapshot.Accounts.Any(a => QuotaRules.IsVisible(a, prefs) && a.Operation == AccountOperation.Idle && a.Connection == ConnectionState.Connected);
        RefreshAllLabel = format.T(IsRefreshingAll ? "Header_Refreshing" : "Header_RefreshAll");
        RefreshAllDisabledReason = blocked ? format.T("Header_RefreshBlocked") : !HasEligibleAccounts ? format.T("Header_RefreshNothing") : string.Empty;

        if (snapshot.LastRefreshAll is { } summary && !ReferenceEquals(summary, dismissedResult))
        {
            var failedLabels = summary.FailedAccountIds.Select(id => snapshot.Accounts.FirstOrDefault(a => a.Id == id)?.Label).Where(l => l is not null).ToArray();
            ResultHasFailures = failedLabels.Length > 0;
            ResultText = ResultHasFailures
                ? format.F("RefreshAll_ResultFailures", summary.Updated, failedLabels.Length, string.Join(", ", failedLabels))
                : format.F(summary.Updated == 1 ? "RefreshAll_ResultOne" : "RefreshAll_ResultMany", summary.Updated);
            ShowResult = true;
        }
        else
            ShowResult = false;
    }

    private bool CanRefreshAll() => !IsRefreshingAll && HasEligibleAccounts && !RefreshBlocked;

    [RelayCommand(CanExecute = nameof(CanRefreshAll))]
    private async Task RefreshAllAsync()
    {
        IsRefreshingAll = true;
        RefreshAllLabel = Format.T("Header_Refreshing");
        dismissedResult = Snapshot.LastRefreshAll;
        ShowResult = false;
        Context.Announcer.Announce(Format.T("Announce_RefreshAllStarted"));
        try
        {
            var result = await Context.Usage.ExecuteAsync(Context.Command(UiCommandKind.RefreshAll), CancellationToken.None);
            dismissedResult = null;
            Context.Announcer.Announce(result.Status switch
            {
                CommandStatus.Succeeded => Format.T("Announce_RefreshAllDone"),
                CommandStatus.Failed => Format.T("Announce_RefreshAllPartial"),
                CommandStatus.Cancelled => Format.T("Announce_RefreshCancelled"),
                _ => Format.T("Header_RefreshBlocked"),
            });
        }
        finally
        {
            IsRefreshingAll = false;
            OnSnapshot(Context.Usage.Current);
        }
    }

    /// <summary>Tray and header entry point; returns once the operation finishes.</summary>
    public Task RefreshAllFromTrayAsync() => RefreshAllCommand.CanExecute(null) ? RefreshAllCommand.ExecuteAsync(null) : Task.CompletedTask;

    [RelayCommand]
    private void DismissResult()
    {
        dismissedResult = Snapshot.LastRefreshAll;
        ShowResult = false;
    }

    [RelayCommand]
    private void OpenUpdates() => Context.Navigation.Navigate(new(PageKey.Settings, Tab: SettingsTab.Updates));

    /// <summary>The header gear: opens settings in place of the usage view, or returns to usage when already open.</summary>
    [RelayCommand]
    private void ToggleSettings() => Context.Navigation.Navigate(new(IsSettings ? PageKey.Overview : PageKey.Settings));

    /// <summary>Back from settings, history or account detail; the usage view is the root, so Back never leaves it.</summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        if (Context.Navigation.CanGoBack)
            Context.Navigation.GoBack();
        else
            Context.Navigation.Navigate(new(PageKey.Overview));
    }

    /// <summary>Explicit Exit with confirmation; the window is shown first so the dialog is visible even from the tray.</summary>
    [RelayCommand]
    private async Task ExitAsync()
    {
        if (exiting || Context.Dialogs.IsDialogOpen)
            return;
        exiting = true;
        try
        {
            lifetime.ShowMainWindow();
            var format = Format;
            var outcome = await Context.Dialogs.ConfirmAsync(new(format.T("Exit_Title"), format.T("Exit_Body"), format.T("Exit_Confirm")));
            if (outcome == ConfirmOutcome.Confirmed)
            {
                await lifetime.ExitAsync();
                return;
            }
            Context.Announcer.Announce(format.T("Announce_Cancelled"));
        }
        finally
        {
            exiting = false;
        }
    }
}
