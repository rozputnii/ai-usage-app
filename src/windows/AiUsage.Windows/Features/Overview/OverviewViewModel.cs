using System.Collections.ObjectModel;
using AiUsage.Features.Connection;
using AiUsage.Features.History;
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
}

/// <summary>
/// S01 Overview: provider sections in manual order, the D-115 summary (restored per D3), first-run and all-hidden
/// states. Never shows a global usage percentage; the minimum compares known percentages only.
/// </summary>
internal sealed partial class OverviewViewModel : SnapshotViewModel
{
    private readonly IHistorySource history;
    private readonly IPreferenceStore preferences;
    private readonly Dictionary<string, AccountRowViewModel> rows = new(StringComparer.Ordinal);

    public OverviewViewModel(PresentationContext context, IHistorySource history, IPreferenceStore preferences, AddAccountViewModel addAccount) : base(context)
    {
        this.history = history;
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

    [ObservableProperty] public partial string SummaryAccounts { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SummaryAccountsDetail { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SummaryLowest { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SummaryLowestSource { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool SummaryLowestCritical { get; private set; }
    [ObservableProperty] public partial string SummaryReset { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SummaryResetSource { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SummaryAttention { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SummaryAttentionDetail { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool SummaryAttentionCritical { get; private set; }
    [ObservableProperty] public partial string SummaryScope { get; private set; } = string.Empty;

    public OverviewSummary? Summary { get; private set; }

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
        });

        var summary = Summary = OverviewSummary.Compute(visible, prefs, Context.Clock.UtcNow);
        SummaryAccounts = format.Count(summary.AccountCount);
        SummaryAccountsDetail = format.F(summary.ProviderCount == 1 ? "Summary_ProvidersOne" : "Summary_ProvidersMany", summary.ProviderCount);
        if (summary.LowestRemaining is { } lowest)
        {
            SummaryLowest = format.F("Summary_LowestValue", format.Percent(lowest));
            SummaryLowestSource = format.F(summary.LowestAccount!.Freshness == Freshness.Stale ? "Summary_SourceStale" : "Summary_Source",
                summary.LowestAccount.Label, summary.LowestWindow!.Label);
            SummaryLowestCritical = lowest <= 0;
        }
        else
        {
            SummaryLowest = format.T("Value_Unavailable");
            SummaryLowestSource = format.T("Summary_LowestUnavailable");
            SummaryLowestCritical = false;
        }
        if (summary.NearestReset is { } reset)
        {
            SummaryReset = format.Relative(reset) ?? format.T("Summary_NoReset");
            SummaryResetSource = format.F("Summary_ResetSource", format.Time(reset), summary.NearestAccount!.Label, summary.NearestWindow!.Label);
        }
        else
        {
            SummaryReset = "—";
            SummaryResetSource = format.T("Summary_NoResetKnown");
        }
        SummaryAttention = format.Count(summary.NeedAttention);
        SummaryAttentionDetail = format.F("Summary_AttentionDetail", summary.Warning, summary.Critical, summary.Exhausted, summary.ReauthRequired);
        SummaryAttentionCritical = summary.Critical + summary.Exhausted + summary.ReauthRequired > 0;
        SummaryScope = format.F("Summary_Scope", summary.AccountCount,
            prefs.ShowHidden ? format.T("Summary_ScopeInclHidden") : string.Empty,
            prefs.ShowDisconnected ? string.Empty : format.T("Summary_ScopeExclDisconnected"));
    }

    private AccountRowViewModel GetRow(string id)
    {
        if (!rows.TryGetValue(id, out var row))
            rows[id] = row = new AccountRowViewModel(id, Context, history, AddAccount, (r, direction) => _ = MoveAsync(r.Id, direction));
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
