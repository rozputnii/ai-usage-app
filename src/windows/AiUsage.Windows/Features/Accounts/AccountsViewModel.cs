using System.Collections.ObjectModel;
using AiUsage.Features.Connection;
using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using AiUsage.Features.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Accounts;

/// <summary>S02 list + detail. Filters are preferences shared with Overview; selection survives navigation.</summary>
internal sealed partial class AccountsViewModel : SnapshotViewModel
{
    private readonly IPreferenceStore preferences;
    private bool applying;

    public AccountsViewModel(PresentationContext context, IHistorySource history, IDataManagementService data, IPreferenceStore preferences, IAccountConnector connector) : base(context)
    {
        this.preferences = preferences;
        Detail = new AccountDetailViewModel(context, history, data, connector);
        context.Navigation.Navigated += (_, request) =>
        {
            if (request.Page == PageKey.Accounts && request.AccountId is { } id)
                Select(id);
        };
        Initialize();
    }

    public AccountDetailViewModel Detail { get; }
    public ObservableCollection<AccountListItemViewModel> Items { get; } = [];

    [ObservableProperty] public partial string? SelectedAccountId { get; private set; }
    [ObservableProperty] public partial bool HasDetail { get; private set; }
    [ObservableProperty] public partial bool IsEmpty { get; private set; }
    [ObservableProperty] public partial bool ShowHidden { get; set; }
    [ObservableProperty] public partial bool ShowDisconnected { get; set; }
    [ObservableProperty] public partial string ShowHiddenLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ShowDisconnectedLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial AccountListItemViewModel? SelectedItem { get; set; }

    public bool ShowNoSelection => !HasDetail && !IsEmpty;
    partial void OnHasDetailChanged(bool value) => OnPropertyChanged(nameof(ShowNoSelection));
    partial void OnIsEmptyChanged(bool value) => OnPropertyChanged(nameof(ShowNoSelection));

    partial void OnShowHiddenChanged(bool value)
    {
        if (!applying)
            _ = preferences.SetPreferenceAsync(new(PreferenceKey.ShowHidden, value), CancellationToken.None);
    }

    partial void OnShowDisconnectedChanged(bool value)
    {
        if (!applying)
            _ = preferences.SetPreferenceAsync(new(PreferenceKey.ShowDisconnected, value), CancellationToken.None);
    }

    partial void OnSelectedItemChanged(AccountListItemViewModel? value)
    {
        if (!applying && value is not null)
            Select(value.Id);
    }

    public void Select(string accountId)
    {
        SelectedAccountId = accountId;
        OnSnapshot(Snapshot);
    }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        applying = true;
        try
        {
            var format = Format;
            var prefs = snapshot.Preferences;
            ShowHidden = prefs.ShowHidden;
            ShowDisconnected = prefs.ShowDisconnected;
            ShowHiddenLabel = format.F("Accounts_ShowHidden", snapshot.Accounts.Count(a => prefs.HiddenTargets.Contains(a.Id)));
            ShowDisconnectedLabel = format.F("Accounts_ShowDisconnected", snapshot.Accounts.Count(a => a.Connection == ConnectionState.NotConnected));
            var visible = QuotaRules.Ordered(snapshot).Where(a => QuotaRules.IsVisible(a, prefs)).ToArray();
            IsEmpty = snapshot.Loaded && visible.Length == 0;

            if (SelectedAccountId is null || snapshot.Accounts.All(a => a.Id != SelectedAccountId))
                SelectedAccountId = visible.FirstOrDefault()?.Id;
            CollectionSync.Sync(Items, visible, a => a.Id, vm => vm.Id, a => new AccountListItemViewModel(a.Id), (vm, a) =>
            {
                vm.Label = a.Label;
                vm.IsSelected = a.Id == SelectedAccountId;
                vm.IsDisconnected = a.Connection == ConnectionState.NotConnected;
                var primary = QuotaRules.PrimaryWindow(a, prefs);
                if (vm.IsDisconnected)
                {
                    vm.ValueText = format.T("Accounts_Off");
                    vm.Tone = ValueTone.Muted;
                }
                else if (primary is null)
                {
                    vm.ValueText = "—";
                    vm.Tone = ValueTone.Muted;
                }
                else
                {
                    var group = QuotaRules.VisibleGroups(QuotaRules.SelectedContext(a, prefs), prefs).First(g => g.Windows.Contains(primary));
                    var window = new QuotaWindowViewModel(primary.Id);
                    window.Update(a, group, primary, prefs, format);
                    vm.ValueText = window.Glyph.Length > 0 ? $"{window.Glyph} {window.ValueText}" : window.ValueText;
                    vm.Tone = window.Tone;
                }
                vm.AccessibleName = format.F("Accounts_ItemAria", a.Label, Context.Providers.Get(a.ProviderId).PresentationName, vm.ValueText);
            });
            SelectedItem = Items.FirstOrDefault(i => i.Id == SelectedAccountId);
            var selected = snapshot.Accounts.FirstOrDefault(a => a.Id == SelectedAccountId);
            HasDetail = selected is not null;
            if (selected is not null)
                Detail.Update(selected, snapshot);
        }
        finally
        {
            applying = false;
        }
    }
}
