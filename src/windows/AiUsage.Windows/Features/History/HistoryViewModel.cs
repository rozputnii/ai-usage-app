using System.Collections.ObjectModel;
using System.Globalization;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.History;

internal sealed record HistoryOption(string Id, string Label, string? ContextId = null, string? GroupId = null);

internal sealed partial class RangePresetViewModel(HistoryPreset preset, string label) : ObservableObject
{
    public HistoryPreset Preset { get; } = preset;
    public string Label { get; } = label;
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

/// <summary>
/// S04 History: account and window filters (context and group are folded into the window label), range presets and a
/// validated custom range. Loading shows a skeleton; outcomes are chart, empty with reason, or disabled-collection banner.
/// </summary>
internal sealed partial class HistoryViewModel : SnapshotViewModel
{
    internal const string InputFormat = "yyyy-MM-dd HH:mm";
    private readonly IHistorySource source;
    private CancellationTokenSource? load;
    private bool applying;
    private string? loadedKey;

    public HistoryViewModel(PresentationContext context, IHistorySource source) : base(context)
    {
        this.source = source;
        var format = context.Format;
        Presets =
        [
            new(HistoryPreset.Hours24, format.T("Range_24h")),
            new(HistoryPreset.Days7, format.T("Range_7d")),
            new(HistoryPreset.Days30, format.T("Range_30d")),
            new(HistoryPreset.Days90, format.T("Range_90d")),
            new(HistoryPreset.Year1, format.T("Range_1y")),
            new(HistoryPreset.Custom, format.T("Range_Custom")),
        ];
        Presets[0].IsSelected = true;
        var now = context.Clock.UtcNow;
        CustomFrom = TimeZoneInfo.ConvertTime(now - TimeSpan.FromHours(36), context.Clock.TimeZone).ToString(InputFormat, CultureInfo.InvariantCulture);
        CustomTo = TimeZoneInfo.ConvertTime(now, context.Clock.TimeZone).ToString(InputFormat, CultureInfo.InvariantCulture);
        context.Navigation.Navigated += (_, request) =>
        {
            if (request.Page == PageKey.History && request.AccountId is { } account)
                SelectFromNavigation(account, request.WindowId);
        };
        Initialize();
    }

    public ObservableCollection<HistoryOption> AccountOptions { get; } = [];
    public ObservableCollection<HistoryOption> WindowOptions { get; } = [];
    public IReadOnlyList<RangePresetViewModel> Presets { get; }

    [ObservableProperty] public partial HistoryOption? SelectedAccount { get; set; }
    [ObservableProperty] public partial HistoryOption? SelectedWindow { get; set; }
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    [ObservableProperty] public partial HistoryPreset Preset { get; private set; } = HistoryPreset.Hours24;
    [ObservableProperty] public partial string CustomFrom { get; set; }
    [ObservableProperty] public partial string CustomTo { get; set; }
    [ObservableProperty] public partial string RangeError { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsLoading { get; private set; }
    [ObservableProperty] public partial bool IsEmpty { get; private set; }
    [ObservableProperty] public partial string EmptyReason { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool HasChart { get; private set; }
    [ObservableProperty] public partial bool CollectionDisabled { get; private set; }
    [ObservableProperty] public partial bool IsPartial { get; private set; }
    [ObservableProperty] public partial ChartModel Chart { get; private set; } = ChartModel.Empty;
    [ObservableProperty] public partial string ChartTitle { get; private set; } = string.Empty;
    [ObservableProperty] public partial string RangeText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ChartAccessibleName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Subtitle { get; private set; } = string.Empty;
    [ObservableProperty] public partial string LegendObserved { get; private set; } = string.Empty;
    [ObservableProperty] public partial string PointCountText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string AxisTop { get; private set; } = string.Empty;
    [ObservableProperty] public partial string AxisMiddle { get; private set; } = string.Empty;
    [ObservableProperty] public partial string AxisBottom { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Tooltip { get; private set; } = string.Empty;

    public bool IsCustom => Preset == HistoryPreset.Custom;
    public bool HasRangeError => RangeError.Length > 0;
    public bool HasTooltip => Tooltip.Length > 0;
    partial void OnRangeErrorChanged(string value) => OnPropertyChanged(nameof(HasRangeError));
    partial void OnTooltipChanged(string value) => OnPropertyChanged(nameof(HasTooltip));

    partial void OnSelectedAccountChanged(HistoryOption? value)
    {
        if (applying || value is null)
            return;
        RebuildWindows(Snapshot, preferredWindow: null);
        _ = LoadAsync();
    }

    partial void OnSelectedWindowChanged(HistoryOption? value)
    {
        if (!applying && value is not null)
            _ = LoadAsync();
    }

    private void SelectFromNavigation(string accountId, string? windowId)
    {
        applying = true;
        SelectedAccount = AccountOptions.FirstOrDefault(a => a.Id == accountId) ?? SelectedAccount;
        RebuildWindows(Snapshot, windowId);
        applying = false;
        _ = LoadAsync();
    }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var usedMode = snapshot.Preferences.UsageDisplay == UsageDisplay.Used;
        Subtitle = format.T(usedMode ? "History_SubtitleUsed" : "History_SubtitleRemaining");
        LegendObserved = format.T(usedMode ? "History_LegendUsed" : "History_LegendRemaining");
        AxisTop = format.Percent(100);
        AxisMiddle = format.Percent(50);
        AxisBottom = format.Percent(0);
        CollectionDisabled = !snapshot.Preferences.HistoryEnabled;

        applying = true;
        try
        {
            var accounts = QuotaRules.Ordered(snapshot).Select(a => new HistoryOption(a.Id, a.Label)).ToArray();
            var selectedId = SelectedAccount?.Id;
            CollectionSync.Sync(AccountOptions, accounts, a => a.Id + "|" + a.Label, a => a.Id + "|" + a.Label, a => a, (_, _) => { });
            SelectedAccount = AccountOptions.FirstOrDefault(a => a.Id == selectedId) ?? AccountOptions.FirstOrDefault();
            RebuildWindows(snapshot, SelectedWindow?.Id);
        }
        finally { applying = false; }

        var key = $"{SelectedAccount?.Id}|{SelectedWindow?.Id}|{snapshot.Preferences.UsageDisplay}|{snapshot.Preferences.HistoryEnabled}|{snapshot.Preferences.Retention}|{Account(snapshot)?.FetchedAt}|{Context.Clock.UtcNow}";
        if (key != loadedKey)
            _ = LoadAsync();
    }

    private AccountItem? Account(UiSnapshot snapshot) => snapshot.Accounts.FirstOrDefault(a => a.Id == SelectedAccount?.Id);

    private void RebuildWindows(UiSnapshot snapshot, string? preferredWindow)
    {
        var account = Account(snapshot);
        var options = new List<HistoryOption>();
        if (account is not null)
            foreach (var context in account.Contexts)
                foreach (var group in context.Groups)
                    foreach (var window in group.Windows)
                        options.Add(new(window.Id + "|" + context.Id, account.Contexts.Count > 1
                            ? Format.F("History_WindowOptionContext", context.Label, group.Label, window.Label)
                            : Format.F("History_WindowOption", group.Label, window.Label), context.Id, group.Id));
        var previous = preferredWindow ?? SelectedWindow?.Id;
        var wasApplying = applying;
        applying = true;
        CollectionSync.Sync(WindowOptions, options, o => o.Id + "|" + o.Label, o => o.Id + "|" + o.Label, o => o, (_, _) => { });
        SelectedWindow = WindowOptions.FirstOrDefault(o => o.Id == previous || o.Id.Split('|')[0] == previous) ?? WindowOptions.FirstOrDefault();
        applying = wasApplying;
    }

    [RelayCommand]
    private Task SelectPresetAsync(RangePresetViewModel? option)
    {
        if (option is null)
            return Task.CompletedTask;
        foreach (var preset in Presets)
            preset.IsSelected = preset == option;
        Preset = option.Preset;
        RangeError = string.Empty;
        return option.Preset == HistoryPreset.Custom ? Task.CompletedTask : LoadAsync();
    }

    /// <summary>Validates format, order and future end separately, keeping the previous chart on error.</summary>
    [RelayCommand]
    private Task ApplyCustomAsync()
    {
        var format = Format;
        if (!TryParse(CustomFrom, out var from) || !TryParse(CustomTo, out var to))
        {
            RangeError = format.T("History_ErrorFormat");
            return Task.CompletedTask;
        }
        if (from >= to)
        {
            RangeError = format.T("History_ErrorOrder");
            return Task.CompletedTask;
        }
        if (to > Context.Clock.UtcNow + TimeSpan.FromHours(1))
        {
            RangeError = format.T("History_ErrorFuture");
            return Task.CompletedTask;
        }
        RangeError = string.Empty;
        return LoadAsync();
    }

    private bool TryParse(string text, out DateTimeOffset value)
    {
        value = default;
        if (!DateTime.TryParseExact(text?.Trim(), InputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return false;
        var zone = Context.Clock.TimeZone;
        value = new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
        return true;
    }

    [RelayCommand]
    private void ShowTooltip(ChartMarker? marker) => Tooltip = marker?.Tooltip ?? string.Empty;

    [RelayCommand]
    private void HideTooltip() => Tooltip = string.Empty;

    [RelayCommand]
    private void OpenDataPrivacy() => Context.Navigation.Navigate(new(PageKey.Settings, Tab: SettingsTab.DataPrivacy));

    public async Task LoadAsync()
    {
        var snapshot = Snapshot;
        var account = Account(snapshot);
        var option = SelectedWindow;
        loadedKey = $"{SelectedAccount?.Id}|{SelectedWindow?.Id}|{snapshot.Preferences.UsageDisplay}|{snapshot.Preferences.HistoryEnabled}|{snapshot.Preferences.Retention}|{account?.FetchedAt}|{Context.Clock.UtcNow}";
        load?.Cancel();
        var format = Format;
        Tooltip = string.Empty;
        if (account is null || option is null)
        {
            ShowEmpty(format.T("History_EmptyNoAccount"));
            return;
        }
        var now = Context.Clock.UtcNow;
        DateTimeOffset from, to = now;
        if (Preset == HistoryPreset.Custom)
        {
            if (!TryParse(CustomFrom, out from) || !TryParse(CustomTo, out to) || from >= to)
                return;
        }
        else
        {
            from = now - Preset switch
            {
                HistoryPreset.Days7 => TimeSpan.FromDays(7),
                HistoryPreset.Days30 => TimeSpan.FromDays(30),
                HistoryPreset.Days90 => TimeSpan.FromDays(90),
                HistoryPreset.Year1 => TimeSpan.FromDays(365),
                _ => TimeSpan.FromHours(24),
            };
        }
        var cancellation = load = new CancellationTokenSource();
        IsLoading = true;
        HasChart = false;
        IsEmpty = false;
        try
        {
            var windowId = option.Id.Split('|')[0];
            var result = await source.QueryHistoryAsync(new(account.Id, option.ContextId, option.GroupId, windowId, from, to, HistoryResolution.Auto, Preset), cancellation.Token);
            if (cancellation.IsCancellationRequested)
                return;
            IsLoading = false;
            IsPartial = result.Partial;
            var display = Context.Usage.Current.Preferences.UsageDisplay;
            var chart = ChartModel.Build(result.Points, display, format);
            if (result.Outcome != HistoryOutcome.Points || chart.ObservedCount == 0)
            {
                ShowEmpty(result.Outcome switch
                {
                    HistoryOutcome.NoComparablePercentage => format.T("History_EmptyNoPercentage"),
                    HistoryOutcome.DataDeleted => format.T("History_EmptyDeleted"),
                    _ when !result.CollectionEnabled => format.T("History_EmptyCollectionOff"),
                    _ => format.T("History_EmptyRange"),
                });
                return;
            }
            Chart = chart;
            HasChart = true;
            IsEmpty = false;
            ChartTitle = format.F("History_ChartTitle", account.Label, option.Label);
            RangeText = result.Points.Count > 1 ? format.F("History_RangeText", format.DateTime(result.Points[0].At), format.DateTime(result.Points[^1].At)) : string.Empty;
            ChartAccessibleName = format.F(display == UsageDisplay.Used ? "History_ChartAriaUsed" : "History_ChartAriaRemaining", chart.ObservedCount, chart.Gaps.Count, chart.Resets.Count);
            PointCountText = format.F("History_PointCount", chart.ObservedCount);
        }
        catch (OperationCanceledException)
        {
            // A cancelled query (scenario switch or a newer request) must not leave the chart area blank forever.
            if (load == cancellation)
                loadedKey = null;
        }
        finally
        {
            if (load == cancellation)
                IsLoading = false;
        }
    }

    private void ShowEmpty(string reason)
    {
        IsLoading = false;
        HasChart = false;
        Chart = ChartModel.Empty;
        IsEmpty = true;
        EmptyReason = reason;
    }
}
