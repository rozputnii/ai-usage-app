using System.Collections.ObjectModel;
using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Ledger;

internal sealed partial class WorkDayToggle(LedgerSettingsViewModel owner, DayOfWeek day, bool isOn) : ObservableObject
{
    public DayOfWeek Day { get; } = day;
    public string Label => LedgerFormat.WeekdayShort(Day);
    public string AccessibleName => LedgerFormat.WeekdayName(Day) + (IsOn ? ", work day" : ", day off");
    [ObservableProperty] public partial bool IsOn { get; set; } = isOn;

    [RelayCommand]
    public Task ToggleAsync() => owner.ToggleWorkDayAsync(Day);
}

internal sealed partial class CapRow : ObservableObject
{
    private readonly LedgerSettingsViewModel owner;

    public CapRow(LedgerSettingsViewModel owner, CapSettingModel model)
    {
        this.owner = owner;
        Model = model;
    }

    public CapSettingModel Model { get; }
    public string Label => Model.AccountName + (Model.ScopeLabel is { } scope ? " · " + scope : string.Empty);
    public string AmountText => LedgerFormat.Value(Model.Scale, Model.Amount);
    public bool IsApplied => Model.Status == CapStatus.Applied;
    public bool IsWarning => Model.Status != CapStatus.Applied;
    public string ActionText => Model.Status == CapStatus.Unmatched ? "Remove" : "Edit";
    public bool CanAct => Model.Status == CapStatus.Unmatched || Model.CapTargetId is not null && owner.HasCard(Model.CapTargetId);
    public string AccessibleName => "Cap for " + Label + ", " + AmountText + ", " + Note;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    public partial CapEditorViewModel? Editor { get; private set; }
    public bool IsEditing => Editor is not null;

    public string Note => Model.Status switch
    {
        CapStatus.Unmatched => "unmatched · this limit is no longer reported · kept, not applied",
        CapStatus.Inactive => "kept, not applied · compatible spending scope, period and enabled amounts are required",
        CapStatus.CurrencyMismatch => "currency mismatch · provider reports " + (Model.ProviderCurrency ?? "another currency") + " · cap kept, not applied",
        _ when Model.Tracking is not null && Model.ProviderLimit.Kind == LimitValueKind.Unknown =>
            "provider sends a balance only · used is tracked" + (Model.Tracking.TrackedSince is { } since ? " since " + LedgerFormat.DayMonth(since) : string.Empty) + " (estimate)",
        _ when Model.ProviderLimit.Kind == LimitValueKind.Unlimited => "provider: unlimited · your cap binds",
        _ when Model.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit } =>
            "provider limit " + LedgerFormat.Value(Model.Scale, limit) + " · " + (Model.Binding ? "your cap binds" : "the provider limit binds"),
        _ => "provider limit unknown · your cap binds",
    };

    [RelayCommand]
    public async Task ActAsync()
    {
        if (Model.Status == CapStatus.Unmatched)
        {
            await owner.RemoveUnmatchedAsync(Model.CapId);
            return;
        }
        if (Model.CapTargetId is not { } target || owner.CapCard(target) is not { } card)
            return;
        // A mismatched retained amount is not a conversion or a removable cap in this scale.
        decimal? current = Model.Status == CapStatus.CurrencyMismatch ? null : Model.Amount;
        Editor = new CapEditorViewModel(card.Scale, current, LedgerFormat.PeriodWords(card.Period),
            amount => owner.Owner.SetCapAsync(target, amount, current, Label), () => Editor = null);
    }
}

/// <summary>The inline settings panel (spec S5): work days with undo, personal caps, appearance, data and status rows.</summary>
internal sealed partial class LedgerSettingsViewModel(LedgerViewModel owner, ILedgerSource source) : ObservableObject
{
    private LedgerSnapshot? snapshot;

    public LedgerViewModel Owner { get; } = owner;
    public ObservableCollection<WorkDayToggle> WorkDays { get; } = [];
    public ObservableCollection<CapRow> Caps { get; } = [];
    [ObservableProperty] public partial bool IsLeft { get; private set; }
    [ObservableProperty] public partial bool IsCompact { get; private set; }
    [ObservableProperty] public partial bool ShowSignedOut { get; private set; }
    [ObservableProperty] public partial bool AlwaysOnTop { get; private set; }
    [ObservableProperty] public partial string MonitoringText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string UpdatesText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SystemStatusText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsDeleteArmed { get; private set; }
    [ObservableProperty] public partial bool HasCaps { get; private set; }
    [ObservableProperty] public partial string RecoveryText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool NeedsRecovery { get; private set; }
    [ObservableProperty] public partial bool CanRetryRecovery { get; private set; }
    [ObservableProperty] public partial bool CanRestorePreferences { get; private set; }
    [ObservableProperty] public partial string DiagnosticText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SupportStatus { get; private set; } = string.Empty;

    public string WorkDaysNote =>
        "Daily budgets split each period over work days (Mon–Fri by default). A day off shows today’s would-be share in neutral; Work today in the title bar colours it until midnight. Changes apply from the next local midnight.";
    public string CapsNote => "A cap is yours, never the provider’s limit. The lower of cap and provider limit applies. Caps are set in the pool’s own unit or currency.";
    public string DeleteNote => "Deletes sign-ins, names, preferences, caps, history and logs on this PC, including retained history from older versions. Enter confirms, Esc cancels.";

    internal LimitCardModel? CapCard(string capTargetId) => snapshot?.Accounts.SelectMany(a => a.Cards).FirstOrDefault(c => c.CapTargetId == capTargetId);
    internal bool HasCard(string capTargetId) => CapCard(capTargetId) is not null;

    public void Rebuild(LedgerSnapshot current, LedgerPreferences prefs)
    {
        snapshot = current;
        NeedsRecovery = current.Summaries.Recovery is not null;
        RecoveryText = current.Summaries.Recovery?.Message ?? string.Empty;
        CanRetryRecovery = current.Summaries.Recovery?.CanRetry ?? false;
        CanRestorePreferences = current.Summaries.Recovery?.CanRestorePreferences ?? false;
        IsLeft = prefs.Mode == ValueMode.Left;
        IsCompact = prefs.Density == Density.Compact;
        ShowSignedOut = prefs.ShowSignedOut;
        AlwaysOnTop = prefs.AlwaysOnTop;
        MonitoringText = "every " + LedgerFormat.Duration(current.Summaries.RefreshInterval);
        UpdatesText = current.Summaries.UpdatesSummary;
        SystemStatusText = current.Summaries.LocalStatus ?? (current.Summaries.FailedSyncs == 0
            ? "all accounts synced"
            : current.Summaries.FailedSyncs + (current.Summaries.FailedSyncs == 1 ? " sync failed · " : " syncs failed · ") + string.Join(", ", current.Summaries.FailedProviders.Select(LedgerFormat.ProviderName)));

        DayOfWeek[] order = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
        if (WorkDays.Count == 0)
            foreach (var day in order)
                WorkDays.Add(new WorkDayToggle(this, day, current.Budget.WorkDays.Contains(day)));
        else
            foreach (var toggle in WorkDays)
                toggle.IsOn = current.Budget.WorkDays.Contains(toggle.Day);

        Caps.Clear();
        foreach (var cap in current.Budget.Caps)
            Caps.Add(new CapRow(this, cap));
        HasCaps = Caps.Count > 0;
    }

    [RelayCommand]
    public async Task SupportAsync(LedgerSupportAction action)
    {
        var outcome = await source.SupportAsync(action, CancellationToken.None);
        SupportStatus = outcome == CommandOutcome.Done ? "Done" : "Action unavailable; local data is preserved";
    }

    [RelayCommand]
    public async Task PreviewDiagnosticsAsync() => DiagnosticText = await source.PreviewDiagnosticsAsync(CancellationToken.None);

    public async Task ToggleWorkDayAsync(DayOfWeek day)
    {
        var before = new HashSet<DayOfWeek>(source.Current.Budget.WorkDays);
        var after = new HashSet<DayOfWeek>(before);
        var removed = after.Remove(day);
        if (!removed)
            after.Add(day);
        if (await source.SetWorkDaysAsync(after, CancellationToken.None) != CommandOutcome.Done)
        {
            Owner.Announce("At least one work day is needed");
            return;
        }
        var text = LedgerFormat.WeekdayName(day) + (removed ? " removed from work days" : " added to work days");
        Owner.OfferUndo(text, () => source.SetWorkDaysAsync(before, CancellationToken.None));
        Owner.Announce(text);
    }

    public Task RemoveUnmatchedAsync(string capId) => source.RemoveUnmatchedCapAsync(capId, CancellationToken.None);

    [RelayCommand]
    public Task SetDensityAsync(Density density) => source.SetPreferencesAsync(source.Preferences with { Density = density }, CancellationToken.None);

    [RelayCommand]
    public Task SetValueModeAsync(ValueMode mode) => source.SetPreferencesAsync(source.Preferences with { Mode = mode }, CancellationToken.None);

    [RelayCommand]
    public Task ToggleShowSignedOutAsync() => source.SetPreferencesAsync(source.Preferences with { ShowSignedOut = !source.Preferences.ShowSignedOut }, CancellationToken.None);

    [RelayCommand]
    public Task ToggleAlwaysOnTopAsync() => source.SetPreferencesAsync(source.Preferences with { AlwaysOnTop = !source.Preferences.AlwaysOnTop }, CancellationToken.None);

    /// <summary>R-12: the control turns into Confirm · Cancel in place; focus moves to Cancel.</summary>
    [RelayCommand]
    public void ArmDelete() => IsDeleteArmed = true;

    [RelayCommand]
    public void CancelDelete() => IsDeleteArmed = false;

    [RelayCommand]
    public async Task ConfirmDeleteAsync()
    {
        if (!IsDeleteArmed)
            return;
        IsDeleteArmed = false;
        if (await source.DeleteStoredDataAsync(CancellationToken.None) == CommandOutcome.Done)
            Owner.Announce("Stored data deleted");
    }
}
