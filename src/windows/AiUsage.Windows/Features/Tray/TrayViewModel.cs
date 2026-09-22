using System.Collections.ObjectModel;
using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Tray;

internal sealed partial class TrayRowViewModel(string id, TrayViewModel owner) : ObservableObject
{
    public string Id { get; } = id;
    public QuotaWindowViewModel Primary { get; } = new("tray-primary");
    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    [ObservableProperty] public partial string Glyph { get; set; } = string.Empty;
    [ObservableProperty] public partial string ProviderId { get; set; } = string.Empty;
    [ObservableProperty] public partial string SubText { get; set; } = string.Empty;
    [ObservableProperty] public partial string ValueText { get; set; } = string.Empty;
    [ObservableProperty] public partial ValueTone Tone { get; set; }
    [ObservableProperty] public partial bool HasMeter { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [ObservableProperty] public partial bool CanRefresh { get; set; }
    [ObservableProperty] public partial string AccessibleName { get; set; } = string.Empty;
    [ObservableProperty] public partial string RefreshName { get; set; } = string.Empty;
    [ObservableProperty] public partial AttentionLevel Attention { get; set; }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private Task RefreshAsync() => owner.RefreshAccountAsync(this);

    [RelayCommand]
    private void Open() => owner.OpenAccount(Id);
}

/// <summary>
/// S07 tray mini-dashboard and menu. Rows are sorted by attention (D-126 as resolved in D9) and show no charts.
/// Shares the central snapshot with the dashboard (D-128).
/// </summary>
internal sealed partial class TrayViewModel : SnapshotViewModel
{
    private readonly IAppLifetime lifetime;
    private readonly Func<Task> refreshAll;
    private readonly Func<Task> exit;

    public TrayViewModel(PresentationContext context, IAppLifetime lifetime, Func<Task> refreshAll, Func<Task> exit) : base(context)
    {
        this.lifetime = lifetime;
        this.refreshAll = refreshAll;
        this.exit = exit;
        Initialize();
    }

    public ObservableCollection<TrayRowViewModel> Rows { get; } = [];

    /// <summary>The visible popup refreshes its own relative text even while the main clock is stopped.</summary>
    public void RefreshTime() => RefreshSnapshot();

    [ObservableProperty] public partial string CountText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ToolTip { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsRefreshingAll { get; set; }
    [ObservableProperty] public partial AttentionLevel WorstAttention { get; private set; }
    [ObservableProperty] public partial bool IsEmpty { get; private set; }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var prefs = snapshot.Preferences;
        var ordered = QuotaRules.Ordered(snapshot).Where(a => QuotaRules.IsVisible(a, prefs)).ToArray();
        // Stable sort: equal attention keeps the user's manual order.
        var sorted = ordered.Select((account, index) => (account, index, attention: QuotaRules.Attention(account, prefs)))
            .OrderByDescending(t => t.attention).ThenBy(t => t.index).ToArray();
        CollectionSync.Sync(Rows, sorted, t => t.account.Id, vm => vm.Id, t => new TrayRowViewModel(t.account.Id, this), (vm, t) =>
        {
            var account = t.account;
            vm.Label = account.Label;
            vm.ProviderId = account.ProviderId;
            vm.Glyph = Context.Providers.Get(account.ProviderId).Glyph;
            vm.Attention = t.attention;
            var primary = QuotaRules.PrimaryWindow(account, prefs);
            var pill = new StatusPillViewModel();
            pill.Update(account, format);
            if (primary is not null)
            {
                var group = QuotaRules.VisibleGroups(QuotaRules.SelectedContext(account, prefs), prefs).First(g => g.Windows.Contains(primary));
                vm.Primary.Update(account, group, primary, prefs, format);
                vm.ValueText = vm.Primary.Glyph.Length > 0 ? $"{vm.Primary.Glyph} {vm.Primary.ValueText}" : vm.Primary.ValueText;
                vm.Tone = vm.Primary.Tone;
                vm.HasMeter = vm.Primary.Kind == MeterKind.Bar;
            }
            else
            {
                vm.ValueText = "—";
                vm.Tone = ValueTone.Muted;
                vm.HasMeter = false;
            }
            vm.SubText = account.Connection != ConnectionState.Connected || account.Failure is not null || !vm.HasMeter
                ? pill.Text + (account.Failure is { } failure ? " · " + format.T("Failure_Short_" + failure.Kind) : string.Empty)
                : vm.Primary.ResetExact.Length > 0 ? $"{vm.Primary.ResetRelative} · {vm.Primary.ResetExact}" : vm.Primary.ResetRelative;
            vm.IsRefreshing = account.Operation == AccountOperation.Refreshing;
            vm.CanRefresh = account.Operation == AccountOperation.Idle && account.Connection == ConnectionState.Connected
                && snapshot.System.Compatibility != CompatibilityState.SecurityBlocked;
            vm.AccessibleName = format.F("Tray_RowAria", account.Label, Context.Providers.Get(account.ProviderId).PresentationName, vm.ValueText, vm.Primary.UnitText, vm.SubText);
            vm.RefreshName = format.F("Row_RefreshName", account.Label);
        });
        IsEmpty = Rows.Count == 0;
        var summary = OverviewSummary.Compute(ordered, prefs, Context.Clock.UtcNow);
        CountText = format.F(summary.AccountCount == 1 ? "Tray_CountOne" : "Tray_CountMany", summary.AccountCount, summary.NeedAttention);
        WorstAttention = sorted.Length > 0 ? sorted[0].attention : AttentionLevel.Normal;

        // D-126: the tooltip explains the most urgent cause, its account and freshness; it never fabricates a minimum.
        if (sorted.Length == 0)
            ToolTip = format.T(snapshot.Accounts.Count == 0 ? "Tray_TipNoAccounts" : "Tray_TipAllHidden");
        else
        {
            var (worst, _, attention) = sorted[0];
            var freshness = worst.FetchedAt is { } fetched ? format.F("Tray_TipFreshness", format.T("Freshness_" + worst.Freshness), format.DateTime(fetched)) : format.T("Freshness_Unknown");
            ToolTip = attention == AttentionLevel.Normal
                ? format.F("Tray_TipNormal", CountText)
                : format.F("Tray_TipAttention", format.T("Attention_" + attention), worst.Label, freshness, CountText);
        }
    }

    internal async Task RefreshAccountAsync(TrayRowViewModel row)
    {
        var result = await Context.Usage.ExecuteAsync(Context.Command(UiCommandKind.RefreshAccount, row.Id), CancellationToken.None);
        Context.Announcer.Announce(result.Status == CommandStatus.Succeeded ? Format.F("Announce_Updated", row.Label) : Format.F("Announce_RefreshFailed", row.Label));
    }

    internal void OpenAccount(string id)
    {
        lifetime.ShowMainWindow();
        Context.Navigation.Navigate(new(PageKey.Accounts, id));
        ClosePopupRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ClosePopupRequested;

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        IsRefreshingAll = true;
        try { await refreshAll(); }
        finally { IsRefreshingAll = false; }
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        lifetime.ShowMainWindow();
        Context.Navigation.Navigate(new(PageKey.Overview));
        ClosePopupRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        lifetime.ShowMainWindow();
        Context.Navigation.Navigate(new(PageKey.Settings, Tab: SettingsTab.Appearance));
        ClosePopupRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ShowPopup() => lifetime.ShowTrayPopup();

    [RelayCommand]
    private void ClosePopup() => ClosePopupRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private Task ExitAsync()
    {
        ClosePopupRequested?.Invoke(this, EventArgs.Empty);
        return exit();
    }
}
