using System.Collections.ObjectModel;
using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Ledger;

internal sealed record ProviderItem(ProviderKind Provider, string Name, string Description, bool Added, bool SigningIn, bool Available = true)
{
    public bool IsEnabled => Available && !SigningIn;
    public string MenuRight => Added ? "added" : SigningIn ? "waiting…" : string.Empty;
    public string ButtonText => SigningIn ? "Waiting…" : "Sign in";
    public string ButtonName => "Sign in to " + Name;
}

internal sealed record DemoScenario(string Id, string Title);

/// <summary>Demo-only scenario switching; product composition passes none.</summary>
internal sealed record LedgerDemoControls(IReadOnlyList<DemoScenario> Scenarios, Action<string> Load, Action DismissStrip, Action? FailSignIn = null);

/// <summary>
/// The new main window (spec S1 to S9, S12, S13): one card grid, the title row, the sign-in strip, the inline settings
/// panel, inline history and the undo bar. Every figure and state comes from ILedgerSource; nothing here is a budget rule.
/// </summary>
internal sealed partial class LedgerViewModel : ObservableObject, IDisposable
{
    private readonly ILedgerSource source;
    private readonly ILedgerScheduler scheduler;
    private readonly Action<string> announce;
    private readonly Dictionary<string, LimitCardViewModel> cardsById = [];
    private HashSet<string>? knownAccounts;
    private IDisposable? undoExpiry;
    private IDisposable? stripHide;
    private Func<Task>? undoAction;
    private SignInStripModel? hiddenStrip;
    private bool undoHeld;
    private readonly CancellationTokenSource historyLifetime = new();
    private int historyRequest;
    private string? historyCardId;
    private bool disposed;

    public LedgerViewModel(ILedgerSource source, ILedgerScheduler scheduler, Action<string>? announce = null, LedgerDemoControls? demo = null)
    {
        this.source = source;
        this.scheduler = scheduler;
        this.announce = announce ?? (_ => { });
        Demo = demo;
        Settings = new LedgerSettingsViewModel(this, source);
        source.Changed += OnSourceChanged;
        Rebuild();
    }

    public LedgerDemoControls? Demo { get; }
    public bool IsDemo => Demo is not null;
    public LedgerSettingsViewModel Settings { get; }
    public ObservableCollection<LimitCardViewModel> Cards { get; } = [];
    public ObservableCollection<ProviderItem> Providers { get; } = [];
    public LedgerSnapshot Snapshot => source.Current;
    public LedgerPreferences Preferences => source.Preferences;

    [ObservableProperty] public partial string ClockText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsFirstRun { get; private set; }
    [ObservableProperty] public partial bool CanUseAccounts { get; private set; }
    [ObservableProperty] public partial bool IsStarting { get; private set; }
    [ObservableProperty] public partial bool IsDayOff { get; private set; }
    [ObservableProperty] public partial bool WorkTodayOn { get; private set; }
    [ObservableProperty] public partial string DayText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsLeft { get; private set; }
    [ObservableProperty] public partial bool IsCompact { get; private set; } = true;
    [ObservableProperty] public partial bool IsSettingsOpen { get; private set; }
    [ObservableProperty] public partial bool HasStrip { get; private set; }
    [ObservableProperty] public partial string StripText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string StripSub { get; private set; } = string.Empty;
    [ObservableProperty] public partial Tone StripTone { get; private set; }
    [ObservableProperty] public partial string StripAction { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool StripActionIsPrimary { get; private set; }
    [ObservableProperty] public partial bool StripBusy { get; private set; }
    [ObservableProperty] public partial bool StripAcceptsCode { get; private set; }
    [ObservableProperty] public partial string SignInCode { get; set; } = string.Empty;
    [ObservableProperty] public partial string CodeError { get; private set; } = string.Empty;
    private Guid? codeAttempt;
    [ObservableProperty] public partial bool HasUndo { get; private set; }
    [ObservableProperty] public partial string UndoText { get; private set; } = string.Empty;
    [ObservableProperty] public partial LedgerHistoryViewModel? History { get; private set; }
    [ObservableProperty] public partial bool ShowSignedOut { get; private set; }
    [ObservableProperty] public partial bool NeedsRecovery { get; private set; }
    [ObservableProperty] public partial string RecoveryText { get; private set; } = string.Empty;

    public string ValueModeName => "Show values: " + (IsLeft ? "left" : "used");

    /// <summary>Asks the view to bring a card into view and focus it (tray row click, Enter in the tray).</summary>
    public event EventHandler<string>? FocusCardRequested;

    private async void OnSourceChanged(object? sender, EventArgs e)
    {
        Rebuild();
        if (historyCardId is { } cardId)
        {
            if (!cardsById.ContainsKey(cardId)) CloseHistory();
            else await ReadHistoryAsync(cardId);
        }
    }

    private void Rebuild()
    {
        var snapshot = source.Current;
        var prefs = source.Preferences;
        var now = snapshot.LocalNow;
        NeedsRecovery = snapshot.Summaries.Recovery is not null;
        IsStarting = snapshot.Summaries.IsStarting;
        CanUseAccounts = !NeedsRecovery && !IsStarting;
        RecoveryText = snapshot.Summaries.Recovery?.Message ?? string.Empty;
        ClockText = LedgerFormat.TitleClock(now);
        IsLeft = prefs.Mode == ValueMode.Left;
        IsCompact = prefs.Density == Density.Compact;
        ShowSignedOut = prefs.ShowSignedOut;
        OnPropertyChanged(nameof(Preferences));
        IsDayOff = snapshot.Day.Kind == DayKind.DayOff;
        WorkTodayOn = IsDayOff && snapshot.Day.WorkTodayOn;
        DayText = !IsDayOff ? string.Empty : WorkTodayOn ? "Extra work day · until midnight" : "Day off";
        OnPropertyChanged(nameof(ValueModeName));

        var visible = snapshot.Accounts.Where(a => prefs.ShowSignedOut || a.Health != AccountHealth.SignedOut).ToArray();
        IsFirstRun = snapshot.Accounts.Count == 0 && CanUseAccounts;
        if (IsFirstRun)
            ClearUndo();
        var fresh = knownAccounts is null ? [] : visible.Where(a => !knownAccounts.Contains(a.AccountId)).Select(a => a.AccountId).ToHashSet();
        knownAccounts = [.. snapshot.Accounts.Select(a => a.AccountId)];

        var ordered = new List<LimitCardViewModel>();
        foreach (var account in visible)
            foreach (var card in account.Cards)
            {
                if (cardsById.TryGetValue(card.CardId, out var existing))
                    existing.Update(card, account, prefs.Mode, now);
                else
                    cardsById[card.CardId] = existing = new LimitCardViewModel(this, card, account, prefs.Mode, now);
                if (fresh.Contains(account.AccountId))
                {
                    existing.IsNew = true;
                    var target = existing;
                    scheduler.Schedule(TimeSpan.FromSeconds(2), () => target.IsNew = false);
                }
                existing.IsCompact = IsCompact;
                ordered.Add(existing);
            }
        foreach (var gone in cardsById.Keys.Except(ordered.Select(c => c.CardId)).ToArray())
            cardsById.Remove(gone);
        Sync(Cards, ordered);
        if (History is { } history && !cardsById.ContainsKey(history.CardId))
            CloseHistory();

        Sync(Providers, snapshot.Providers.Select(p => new ProviderItem(p.Provider, LedgerFormat.ProviderName(p.Provider), Describe(p.Provider), p.Added, p.SigningIn, CanUseAccounts)).ToList());
        UpdateStrip(snapshot.SignInStrip);
        Settings.Rebuild(snapshot, prefs);
    }

    private static void Sync<T>(ObservableCollection<T> target, IReadOnlyList<T> items) where T : class
    {
        if (target.SequenceEqual(items))
            return;
        for (var i = 0; i < items.Count; i++)
        {
            var index = target.IndexOf(items[i]);
            if (index == i)
                continue;
            if (index > i)
                target.Move(index, i);
            else
                target.Insert(i, items[i]);
        }
        while (target.Count > items.Count)
            target.RemoveAt(target.Count - 1);
    }

    private static string Describe(ProviderKind provider) => provider switch
    {
        ProviderKind.Claude => "5h and 7d limits, extra usage",
        ProviderKind.Codex => "ChatGPT sign-in · 5h and 7d limits, credits",
        ProviderKind.Copilot => "monthly request pools",
        _ => "model group windows",
    };

    private void UpdateStrip(SignInStripModel? strip)
    {
        stripHide?.Dispose();
        stripHide = null;
        if (strip is null || ReferenceEquals(strip, hiddenStrip) || strip == hiddenStrip)
        {
            HasStrip = false;
            SignInCode = string.Empty;
            CodeError = string.Empty;
            StripAcceptsCode = false;
            return;
        }
        var provider = LedgerFormat.ProviderName(strip.Provider);
        HasStrip = true;
        StripBusy = strip.Phase == SignInPhase.Waiting;
        StripAcceptsCode = StripBusy && strip.AcceptsManualCode;
        if (codeAttempt != strip.AttemptId || !StripBusy)
        {
            SignInCode = string.Empty;
            CodeError = string.Empty;
            codeAttempt = strip.AttemptId;
        }
        (StripTone, StripText, StripSub, StripAction, StripActionIsPrimary) = strip.Phase switch
        {
            SignInPhase.Waiting => (Tone.Neutral, "Waiting for " + provider + " sign-in in your browser", IsFirstRun ? "nothing is read until you finish" : string.Empty, "Cancel", false),
            SignInPhase.Succeeded => (Tone.Ok, (strip.AccountName ?? provider) + " added", strip.LimitsFound is { } n ? n + (n == 1 ? " limit found" : " limits found") : string.Empty, string.Empty, false),
            SignInPhase.Cancelled => (Tone.Attention, provider + " sign-in was cancelled in the browser", string.Empty, "Try again", true),
            _ => (Tone.Attention, provider + " sign-in failed", string.Empty, "Try again", true),
        };
        if (StripBusy && strip.UserCode is { } userCode) StripSub = "Enter this code in your browser: " + userCode;
        if (strip.Phase == SignInPhase.Failed) StripSub = strip.Failure switch
        {
            SignInFailure.Duplicate => "This account is already connected",
            SignInFailure.WrongAccount => "Choose the account you are reconnecting",
            SignInFailure.Storage => "Local storage needs recovery before sign-in can continue",
            SignInFailure.AccessDenied => "Authorization was denied",
            SignInFailure.Expired => "The sign-in attempt expired",
            SignInFailure.Browser => "The browser sign-in could not start",
            SignInFailure.Registration => "Provider registration is not configured on this PC",
            _ => "The provider could not complete sign-in",
        };
        if (strip.Phase == SignInPhase.Succeeded)
        {
            announce(StripText);
            stripHide = scheduler.Schedule(TimeSpan.FromSeconds(4), () =>
            {
                hiddenStrip = strip;
                HasStrip = false;
                Demo?.DismissStrip();
            });
        }
    }

    // ---- Title row ----

    [RelayCommand]
    public async Task ToggleWorkTodayAsync()
    {
        if (!IsDayOff)
            return;
        var on = !WorkTodayOn;
        await source.SetWorkTodayAsync(on, CancellationToken.None);
        if (source.Current.Day.WorkTodayOn != on)
        {
            announce("Work today could not be changed");
            return;
        }
        if (on)
            OfferUndo("Work today is on until midnight", () => source.SetWorkTodayAsync(false, CancellationToken.None));
        announce(on ? "Work today on until midnight" : "Work today off");
    }

    [RelayCommand]
    public Task SetValueModeAsync(ValueMode mode) => source.SetPreferencesAsync(source.Preferences with { Mode = mode }, CancellationToken.None);

    [RelayCommand]
    public Task ToggleValueModeAsync() => SetValueModeAsync(IsLeft ? ValueMode.Used : ValueMode.Left);

    [RelayCommand]
    public void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    [RelayCommand]
    public void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    public Task RefreshAsync() => source.RefreshAsync(CancellationToken.None);

    [RelayCommand]
    public Task ToggleShowSignedOutAsync() => source.SetPreferencesAsync(source.Preferences with { ShowSignedOut = !source.Preferences.ShowSignedOut }, CancellationToken.None);

    /// <summary>Esc closes, in order: an open cap editor or rename, history, the settings panel. Returns whether it closed something.</summary>
    public bool Escape()
    {
        foreach (var cap in Settings.Caps)
            if (cap.Editor is { } editor)
            {
                editor.Cancel();
                return true;
            }
        foreach (var card in Cards)
        {
            if (card.CapEditor is not null)
            {
                card.CapEditor.Cancel();
                return true;
            }
            if (card.IsRenaming)
            {
                card.CancelRename();
                return true;
            }
        }
        if (Settings.IsDeleteArmed)
        {
            Settings.CancelDelete();
            return true;
        }
        if (History is not null)
        {
            CloseHistory();
            return true;
        }
        if (IsSettingsOpen)
        {
            IsSettingsOpen = false;
            return true;
        }
        return false;
    }

    // ---- Sign-in ----

    [RelayCommand]
    public async Task SignInAsync(ProviderKind provider)
    {
        if (!CanUseAccounts)
        {
            IsSettingsOpen = true;
            return;
        }
        hiddenStrip = null;
        announce("Waiting for " + LedgerFormat.ProviderName(provider) + " sign-in in your browser");
        await source.SignInAsync(provider, CancellationToken.None);
    }

    public Task ReconnectAsync(string accountId)
    {
        hiddenStrip = null;
        return source.ReconnectAsync(accountId, CancellationToken.None);
    }

    [RelayCommand]
    public void SubmitSignInCode()
    {
        var code = SignInCode;
        SignInCode = string.Empty;
        CodeError = codeAttempt is { } attempt && source.TrySubmitSignInCode(attempt, code) ? string.Empty : "Code was not accepted; check the current sign-in attempt";
    }

    // Retry waits for provider completion; the same button must remain available to cancel it.
    // The source guards attempt identity and prevents concurrent sign-in attempts.
    [RelayCommand(AllowConcurrentExecutions = true)]
    public Task StripActionAsync() => source.Current.SignInStrip switch
    {
        { Phase: SignInPhase.Waiting } => source.CancelSignInAsync(CancellationToken.None),
        { Phase: SignInPhase.Cancelled or SignInPhase.Failed, ReconnectAccountId: { } id } => ReconnectAsync(id),
        { Phase: SignInPhase.Cancelled or SignInPhase.Failed } strip => SignInAsync(strip.Provider),
        _ => Task.CompletedTask,
    };

    public async Task SignOutAsync(string accountId)
    {
        var name = source.Current.Accounts.FirstOrDefault(a => a.AccountId == accountId)?.DisplayName ?? "Account";
        await source.SignOutAsync(accountId, CancellationToken.None);
        announce(source.Current.Accounts.FirstOrDefault(a => a.AccountId == accountId)?.Health == AccountHealth.SignedOut
            ? name + " signed out · history, name and caps kept" : name + " could not be signed out");
    }

    /// <summary>Opens the window at an account (tray row): its first card is focused.</summary>
    public void FocusAccount(string accountId)
    {
        var card = Cards.FirstOrDefault(c => c.Account.AccountId == accountId);
        if (card is not null)
            FocusCardRequested?.Invoke(this, card.CardId);
    }

    // ---- Card commands ----

    public async Task RenameAsync(string accountId, string name)
    {
        if (await source.RenameAccountAsync(accountId, name, CancellationToken.None) == CommandOutcome.Done)
            announce("Renamed to " + name);
    }

    /// <summary>Sets or removes a cap; removing one offers undo that restores the previous amount.</summary>
    public async Task<bool> SetCapAsync(string capTargetId, decimal? amount, decimal? before, string label)
    {
        var outcome = await source.SetCapAsync(capTargetId, amount, CancellationToken.None);
        if (outcome != CommandOutcome.Done)
            return false;
        if (amount is null && before is { } previous)
        {
            OfferUndo("Cap removed · " + label.Trim(), () => source.SetCapAsync(capTargetId, previous, CancellationToken.None));
            announce("Cap removed");
        }
        else
            announce("Cap saved");
        return true;
    }

    public async Task MoveAsync(string cardId, int offset)
    {
        await source.MoveCardAsync(cardId, offset, CancellationToken.None);
        if (Cards.Any(card => card.CardId == cardId))
            FocusCardRequested?.Invoke(this, cardId);
    }

    public async Task ToggleHistoryAsync(LimitCardViewModel card)
    {
        if (historyCardId == card.CardId)
        {
            CloseHistory();
            return;
        }
        historyCardId = card.CardId;
        await ReadHistoryAsync(card.CardId);
    }

    private async Task ReadHistoryAsync(string cardId)
    {
        if (disposed)
            return;
        var request = ++historyRequest;
        HistoryModel? model;
        try { model = await source.GetHistoryAsync(cardId, historyLifetime.Token); }
        catch (OperationCanceledException) when (disposed) { return; }
        if (disposed || request != historyRequest || model is null || !cardsById.TryGetValue(cardId, out var card))
            return;
        DateOnly? focusDate = History?.CardId == cardId ? History.FocusDate : null;
        foreach (var other in Cards)
            other.IsHistoryOpen = other == card;
        History = new LedgerHistoryViewModel(model, card.Name, card.Model.Scale, focusDate);
    }

    [RelayCommand]
    public void CloseHistory()
    {
        historyRequest++;
        historyCardId = null;
        var cardId = History?.CardId;
        foreach (var card in Cards)
            card.IsHistoryOpen = false;
        History = null;
        if (cardId is not null && Cards.Any(c => c.CardId == cardId))
            FocusCardRequested?.Invoke(this, cardId);
    }

    // ---- Undo ----

    private void ClearUndo()
    {
        undoExpiry?.Dispose();
        undoExpiry = null;
        undoAction = null;
        HasUndo = false;
    }

    public void OfferUndo(string text, Func<Task> undo)
    {
        undoExpiry?.Dispose();
        undoAction = undo;
        UndoText = text;
        HasUndo = true;
        ScheduleUndoExpiry();
    }

    private void ScheduleUndoExpiry() => undoExpiry = scheduler.Schedule(TimeSpan.FromSeconds(10), () =>
    {
        if (undoHeld)
            return;
        HasUndo = false;
        undoAction = null;
    });

    /// <summary>The undo bar stays while focused and gets a fresh 10 s once focus leaves.</summary>
    public void HoldUndo(bool held)
    {
        undoHeld = held;
        if (!held && HasUndo)
        {
            undoExpiry?.Dispose();
            ScheduleUndoExpiry();
        }
    }

    [RelayCommand]
    public async Task UndoAsync()
    {
        if (undoAction is not { } action)
            return;
        undoAction = null;
        undoExpiry?.Dispose();
        HasUndo = false;
        await action();
        announce("Undone");
    }

    // ---- Demo ----

    [RelayCommand]
    public void LoadScenario(string id)
    {
        hiddenStrip = null;
        knownAccounts = null;
        CloseHistory();
        ClearUndo();
        Demo?.Load(id);
    }

    internal void Announce(string text) => announce(text);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        historyRequest++;
        source.Changed -= OnSourceChanged;
        historyLifetime.Cancel();
        historyLifetime.Dispose();
        undoExpiry?.Dispose();
        stripHide?.Dispose();
    }
}
