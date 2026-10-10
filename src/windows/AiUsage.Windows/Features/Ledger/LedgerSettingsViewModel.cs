using System.Collections.ObjectModel;
using System.Globalization;
using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Ledger;

internal sealed partial class WorkDayToggle(LedgerSettingsViewModel owner, DayOfWeek day, bool isOn) : ObservableObject
{
    public DayOfWeek Day { get; } = day;
    public string Label => LedgerFormat.WeekdayShort(Day);
    public string AccessibleName => LedgerFormat.WeekdayName(Day) + (IsOn ? ", work day" : ", day off");
    public bool CanToggle => !IsOn || owner.WorkDays.Count(d => d.IsOn) > 1;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleName))]
    public partial bool IsOn { get; set; } = isOn;

    [RelayCommand(CanExecute = nameof(CanToggle))]
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
    /// <summary>A kept row re-reads what depends on the cards rather than on its cap (T-061 R-06).</summary>
    public void Refresh() => OnPropertyChanged(nameof(CanAct));
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
        CapStatus.AboveLimit => "above the provider limit " + LedgerFormat.Value(Model.Scale, Model.ProviderLimit.Amount ?? 0) + " · kept, not applied",
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
        Editor = new CapEditorViewModel(card.Scale, card.Figures.ProviderLimit, current, LedgerFormat.PeriodWords(card.Period),
            amount => owner.Owner.SetCapAsync(target, amount, current, Label), () => Editor = null);
    }
}

/// <summary>
/// The settings sheet (spec S5, R-193): work days with undo, personal caps, view (with the refresh interval), updates, and a
/// footer with a status problem, the rare support actions and Delete stored data. Explanations are tooltips.
/// </summary>
internal sealed partial class LedgerSettingsViewModel(LedgerViewModel owner, ILedgerSource source) : ObservableObject
{
    private LedgerSnapshot? snapshot;

    public LedgerViewModel Owner { get; } = owner;
    public ObservableCollection<WorkDayToggle> WorkDays { get; } = [];
    public ObservableCollection<CapRow> Caps { get; } = [];
    [ObservableProperty] public partial bool IsCompact { get; private set; }
    [ObservableProperty] public partial bool AlwaysOnTop { get; private set; }
    /// <summary>T-055 R-11: the saved refresh interval in whole minutes, 1 to 60; the buttons stop at the ends.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RefreshTip))]
    [NotifyCanExecuteChangedFor(nameof(DecreaseRefreshCommand), nameof(IncreaseRefreshCommand))]
    public partial int RefreshMinutes { get; private set; } = LedgerPreferences.Default.RefreshMinutes;
    /// <summary>The interval box, two-way bound while typing; a commit sets it back to the saved minutes.</summary>
    [ObservableProperty] public partial string RefreshText { get; set; } = LedgerPreferences.Default.RefreshMinutes.ToString(CultureInfo.InvariantCulture);
    [ObservableProperty] public partial string UpdatesText { get; private set; } = string.Empty;
    /// <summary>The update status needs attention or action; otherwise it is only a tooltip.</summary>
    [ObservableProperty] public partial bool IsUpdateNotable { get; private set; }
    [ObservableProperty] public partial string UpdateVersionText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool CanCheckUpdates { get; private set; }
    [ObservableProperty] public partial bool CanInstallUpdate { get; private set; }
    [ObservableProperty] public partial string InstallUpdateText { get; private set; } = "Install and restart";
    [ObservableProperty] public partial bool IsInstallingUpdate { get; private set; }
    [ObservableProperty] public partial bool IsUpdateAlways { get; private set; }
    [ObservableProperty] public partial bool IsUpdateOnLaunch { get; private set; }
    [ObservableProperty] public partial bool IsUpdateOff { get; private set; }
    /// <summary>A local-data or sync problem, empty while all is well; the footer shows it beside ⋯.</summary>
    [ObservableProperty] public partial string SystemStatusText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool HasSystemStatus { get; private set; }
    [ObservableProperty] public partial bool IsDeleteArmed { get; private set; }
    [ObservableProperty] public partial bool HasCaps { get; private set; }
    [ObservableProperty] public partial string RecoveryText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool NeedsRecovery { get; private set; }
    [ObservableProperty] public partial bool CanRetryRecovery { get; private set; }
    [ObservableProperty] public partial bool CanRestorePreferences { get; private set; }
    [ObservableProperty] public partial string DiagnosticText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SupportStatus { get; private set; } = string.Empty;

    public string WorkDaysNote =>
        "Daily budgets split each period over work days (Mon–Fri by default). A day off shows today’s would-be share in neutral; Work today in the title bar colours it until midnight.";
    public string CapsNote => "A cap is yours, never the provider’s limit, and cannot exceed it. Caps are set in the pool’s own unit or currency.";
    public string DeleteNote => "Deletes sign-ins, names, preferences, caps, history and logs on this PC.";
    public string RefreshTip => $"Refresh every {RefreshMinutes} min · {LedgerPreferences.MinRefreshMinutes} to {LedgerPreferences.MaxRefreshMinutes}";

    internal LimitCardModel? CapCard(string capTargetId) => snapshot?.Accounts.SelectMany(a => a.Cards).FirstOrDefault(c => c.CapTargetId == capTargetId);
    internal bool HasCard(string capTargetId) => CapCard(capTargetId) is not null;

    public void Rebuild(LedgerSnapshot current, LedgerPreferences prefs)
    {
        snapshot = current;
        NeedsRecovery = current.Summaries.Recovery is not null;
        RecoveryText = current.Summaries.Recovery?.Message ?? string.Empty;
        CanRetryRecovery = current.Summaries.Recovery?.CanRetry ?? false;
        CanRestorePreferences = current.Summaries.Recovery?.CanRestorePreferences ?? false;
        IsCompact = prefs.Density == Density.Compact;
        AlwaysOnTop = prefs.AlwaysOnTop;
        // Only a changed saved value moves the box, so a rebuild never overwrites what is being typed.
        if (RefreshMinutes != prefs.RefreshMinutes)
        {
            RefreshMinutes = prefs.RefreshMinutes;
            RefreshText = RefreshMinutes.ToString(CultureInfo.InvariantCulture);
        }
        var updates = current.Summaries.Updates;
        UpdatesText = LedgerFormat.UpdateText(updates);
        IsUpdateNotable = updates.State is UpdateState.Available or UpdateState.Ready or UpdateState.Installing or UpdateState.CheckFailed
            or UpdateState.InstallFailed or UpdateState.NotApplied;
        UpdateVersionText = updates.Version is { } version ? "Version " + version : string.Empty;
        CanCheckUpdates = updates.State is not (UpdateState.NotPackaged or UpdateState.NoFeed or UpdateState.Checking or UpdateState.Installing);
        // The button stays in place while installing, so the click visibly took effect until Windows closes the app.
        IsInstallingUpdate = updates.State == UpdateState.Installing;
        CanInstallUpdate = updates.State is UpdateState.Available or UpdateState.Ready or UpdateState.Installing or UpdateState.InstallFailed or UpdateState.NotApplied;
        InstallUpdateText = updates.State switch { UpdateState.Installing => "Installing", UpdateState.InstallFailed => "Retry", _ => "Install and restart" };
        IsUpdateAlways = prefs.Updates == UpdateMode.Always;
        IsUpdateOnLaunch = prefs.Updates == UpdateMode.OnLaunch;
        IsUpdateOff = prefs.Updates == UpdateMode.Off;
        SystemStatusText = current.Summaries.Recovery?.Message ?? (current.Summaries.IsStarting ? "Opening local data…" : current.Summaries.LocalStatus) ?? (current.Summaries.FailedSyncs == 0
            ? string.Empty
            : current.Summaries.FailedSyncs + (current.Summaries.FailedSyncs == 1 ? " sync failed · " : " syncs failed · ") + string.Join(", ", current.Summaries.FailedProviders.Select(LedgerFormat.ProviderName)));
        HasSystemStatus = SystemStatusText.Length > 0;

        DayOfWeek[] order = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
        if (WorkDays.Count == 0)
            foreach (var day in order)
                WorkDays.Add(new WorkDayToggle(this, day, current.Budget.WorkDays.Contains(day)));
        else
            foreach (var toggle in WorkDays)
                toggle.IsOn = current.Budget.WorkDays.Contains(toggle.Day);
        foreach (var toggle in WorkDays)
            toggle.ToggleCommand.NotifyCanExecuteChanged();

        // T-061 R-06: equal caps keep their rows, so an open cap editor and its text survive; only card-dependent state refreshes.
        if (Caps.Select(c => c.Model).SequenceEqual(current.Budget.Caps))
            foreach (var row in Caps)
                row.Refresh();
        else
        {
            Caps.Clear();
            foreach (var cap in current.Budget.Caps)
                Caps.Add(new CapRow(this, cap));
        }
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
    public Task SetUpdateModeAsync(UpdateMode mode) => source.SetPreferencesAsync(source.Preferences with { Updates = mode }, CancellationToken.None);

    [RelayCommand(CanExecute = nameof(CanDecreaseRefresh))]
    public Task DecreaseRefreshAsync() => SetRefreshMinutesAsync(Math.Max(LedgerPreferences.MinRefreshMinutes, RefreshMinutes - 1));

    [RelayCommand(CanExecute = nameof(CanIncreaseRefresh))]
    public Task IncreaseRefreshAsync() => SetRefreshMinutesAsync(Math.Min(LedgerPreferences.MaxRefreshMinutes, RefreshMinutes + 1));

    private bool CanDecreaseRefresh() => RefreshMinutes > LedgerPreferences.MinRefreshMinutes;
    private bool CanIncreaseRefresh() => RefreshMinutes < LedgerPreferences.MaxRefreshMinutes;

    /// <summary>Typing filter: nothing, or up to two ASCII digits; the range is checked when the box is committed.</summary>
    public bool AcceptsRefreshText(string text) => text.Length <= 2 && text.All(char.IsAsciiDigit);

    /// <summary>R-11: whole minutes from 1 to 60 save at once; anything else is refused and the box returns to the saved value.</summary>
    public async Task<bool> CommitRefreshTextAsync(string text)
    {
        var accepted = int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
            && minutes is >= LedgerPreferences.MinRefreshMinutes and <= LedgerPreferences.MaxRefreshMinutes;
        await SetRefreshMinutesAsync(accepted ? minutes : RefreshMinutes);
        return accepted;
    }

    private async Task SetRefreshMinutesAsync(int minutes)
    {
        if (minutes != RefreshMinutes)
            await source.SetPreferencesAsync(source.Preferences with { RefreshMinutes = minutes }, CancellationToken.None);
        // What is saved decides what the box shows: this also turns 05 into 5 and undoes a value that could not be written.
        RefreshText = source.Preferences.RefreshMinutes.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    public Task CheckForUpdatesAsync() => source.CheckForUpdatesAsync(CancellationToken.None);

    [RelayCommand]
    public Task InstallUpdateAsync() => source.InstallUpdateAsync(CancellationToken.None);

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
