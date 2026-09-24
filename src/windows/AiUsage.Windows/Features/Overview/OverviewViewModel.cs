using System.Collections.ObjectModel;
using AiUsage.Features.Connection;
using AiUsage.Features.Presentation;
using AiUsage.Features.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Overview;

internal sealed partial class ProviderSectionViewModel(string providerId) : ObservableObject
{
    public string ProviderId { get; } = providerId;
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string Glyph { get; set; } = string.Empty;
    [ObservableProperty] public partial string ListName { get; set; } = string.Empty;
    public ObservableCollection<AccountRowViewModel> Rows { get; } = [];
    /// <summary>Window names shown once above the bar columns, taken from the first account of the provider.</summary>
    public ObservableCollection<string> Columns { get; } = [];
}

/// <summary>
/// S01 Overview, compact (D-181): provider sections in manual order with window names once per provider, one bar row
/// per account, first-run and all-hidden states. No summary block and no global usage percentage.
/// </summary>
internal sealed partial class OverviewViewModel : SnapshotViewModel
{
    private readonly IPreferenceStore preferences;
    private readonly Dictionary<string, AccountRowViewModel> rows = new(StringComparer.Ordinal);

    public OverviewViewModel(PresentationContext context, IPreferenceStore preferences, AddAccountViewModel addAccount) : base(context)
    {
        this.preferences = preferences;
        AddAccount = addAccount;
        Initialize();
    }

    public ObservableCollection<ProviderSectionViewModel> Sections { get; } = [];
    /// <summary>First run lists the providers directly; one click starts sign-in like the header menu.</summary>
    public AddAccountViewModel AddAccount { get; }

    [ObservableProperty] public partial bool IsLoading { get; private set; }
    [ObservableProperty] public partial bool IsFirstRun { get; private set; }
    [ObservableProperty] public partial bool AllHidden { get; private set; }
    [ObservableProperty] public partial bool HasRows { get; private set; }
    [ObservableProperty] public partial bool IsCompactDensity { get; private set; }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var prefs = snapshot.Preferences;
        IsCompactDensity = prefs.Density == Density.Compact;
        var ordered = QuotaRules.Ordered(snapshot);
        var visible = ordered.Where(a => QuotaRules.IsVisible(a, prefs)).ToArray();
        IsLoading = !snapshot.Loaded;
        IsFirstRun = snapshot.Loaded && snapshot.Accounts.Count == 0;
        AllHidden = snapshot.Loaded && snapshot.Accounts.Count > 0 && visible.Length == 0;
        HasRows = snapshot.Loaded && visible.Length > 0;

        // Providers are ordered by their first visible account; rows keep manual order inside each provider.
        var providerOrder = visible.Select(a => a.ProviderId).Distinct().ToArray();
        var sections = providerOrder.Select(id => (Id: id, Accounts: visible.Where(a => a.ProviderId == id).ToArray())).ToArray();
        foreach (var stale in rows.Keys.Except(visible.Select(a => a.Id)).ToArray())
            rows.Remove(stale);
        CollectionSync.Sync(Sections, sections, s => s.Id, vm => vm.ProviderId, s => new ProviderSectionViewModel(s.Id), (section, source) =>
        {
            var provider = Context.Providers.Get(source.Id);
            section.Name = provider.PresentationName;
            section.Glyph = provider.Glyph;
            section.ListName = format.F("Overview_ProviderList", provider.PresentationName);
            CollectionSync.Sync(section.Rows, source.Accounts, a => a.Id, r => r.Id, a => GetRow(a.Id), (row, account) =>
            {
                var index = Array.IndexOf(source.Accounts, account);
                row.Update(account, snapshot, index + 1, source.Accounts.Length, index > 0, index < source.Accounts.Length - 1);
            });
            var columns = section.Rows.FirstOrDefault()?.Windows.Select(w => w.Label).ToArray() ?? [];
            if (!section.Columns.SequenceEqual(columns))
            {
                section.Columns.Clear();
                foreach (var column in columns)
                    section.Columns.Add(column);
            }
        });
    }

    private AccountRowViewModel GetRow(string id)
    {
        if (!rows.TryGetValue(id, out var row))
            rows[id] = row = new AccountRowViewModel(id, Context, AddAccount, (r, direction) => _ = MoveAsync(r.Id, direction));
        return row;
    }

    public AccountRowViewModel? FindRow(string id) => rows.GetValueOrDefault(id);

    /// <summary>Keyboard reorder (Alt+↑/↓): swaps with the adjacent visible account of the same provider in manual order.</summary>
    public async Task MoveAsync(string accountId, int direction)
    {
        var snapshot = Snapshot;
        var prefs = snapshot.Preferences;
        var ordered = QuotaRules.Ordered(snapshot);
        var account = ordered.FirstOrDefault(a => a.Id == accountId);
        if (account is null)
            return;
        var siblings = ordered.Where(a => a.ProviderId == account.ProviderId && QuotaRules.IsVisible(a, prefs)).ToList();
        var index = siblings.IndexOf(account);
        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= siblings.Count)
            return;
        var order = ordered.Select(a => a.Id).ToList();
        var from = order.IndexOf(accountId);
        var to = order.IndexOf(siblings[targetIndex].Id);
        (order[from], order[to]) = (order[to], order[from]);
        var result = await Context.Usage.ExecuteAsync(Context.Command(UiCommandKind.Reorder, null, new ReorderPayload(order)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
            Context.Announcer.Announce(Format.F("Announce_MovedTo", account.Label, targetIndex + 1, siblings.Count));
    }

    /// <summary>Drag and drop: places the dragged account before the target within the same provider; identical outcome to keyboard moves.</summary>
    public async Task DropAsync(string draggedId, string targetId)
    {
        if (draggedId == targetId)
            return;
        var snapshot = Snapshot;
        var ordered = QuotaRules.Ordered(snapshot);
        var dragged = ordered.FirstOrDefault(a => a.Id == draggedId);
        var target = ordered.FirstOrDefault(a => a.Id == targetId);
        if (dragged is null || target is null || dragged.ProviderId != target.ProviderId)
            return;
        var order = ordered.Select(a => a.Id).Where(id => id != draggedId).ToList();
        var targetIndex = order.IndexOf(targetId);
        var originalDragged = ordered.ToList().IndexOf(dragged);
        var originalTarget = ordered.ToList().IndexOf(target);
        order.Insert(originalDragged < originalTarget ? targetIndex + 1 : targetIndex, draggedId);
        var result = await Context.Usage.ExecuteAsync(Context.Command(UiCommandKind.Reorder, null, new ReorderPayload(order)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
        {
            var siblings = QuotaRules.Ordered(Context.Usage.Current).Where(a => a.ProviderId == dragged.ProviderId && QuotaRules.IsVisible(a, Context.Usage.Current.Preferences)).ToList();
            Context.Announcer.Announce(Format.F("Announce_MovedTo", dragged.Label, siblings.FindIndex(a => a.Id == draggedId) + 1, siblings.Count));
        }
    }

    [RelayCommand]
    private async Task ShowHiddenAndDisconnectedAsync()
    {
        await preferences.SetPreferenceAsync(new(PreferenceKey.ShowHidden, true), CancellationToken.None);
        await preferences.SetPreferenceAsync(new(PreferenceKey.ShowDisconnected, true), CancellationToken.None);
        Context.Announcer.Announce(Format.T("Announce_FiltersShowAll"));
    }
}
