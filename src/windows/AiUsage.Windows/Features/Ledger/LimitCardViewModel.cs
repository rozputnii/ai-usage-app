using System.Globalization;
using System.Text.RegularExpressions;
using AiUsage.Features.Ledger.Contract;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Ledger;

/// <summary>Delayed UI work (undo expiry, success outline, strip hide); tests run it on demand.</summary>
internal interface ILedgerScheduler
{
    IDisposable Schedule(TimeSpan delay, Action action);
}

/// <summary>Inline amount editor for a personal cap (spec S3, S5): Enter saves, Esc cancels, empty + Enter removes.</summary>
internal sealed partial class CapEditorViewModel : ObservableObject
{
    private readonly LimitValue providerLimit;
    private readonly Func<decimal?, Task<bool>> commit;
    private readonly Action close;

    public CapEditorViewModel(ScaleModel scale, LimitValue providerLimit, decimal? current, string period, Func<decimal?, Task<bool>> commit, Action close)
    {
        Scale = scale;
        this.providerLimit = providerLimit;
        HasCap = current is not null;
        Text = current is { } amount ? LedgerFormat.EditText(scale, amount) : string.Empty;
        UnitText = LedgerFormat.Unit(scale);
        Hint = HasCap
            ? "per " + period + " · Enter saves · Esc cancels · empty + Enter removes"
            : "per " + period + " · the cap is yours, not the provider’s limit";
        this.commit = commit;
        this.close = close;
    }

    public ScaleModel Scale { get; }
    public bool HasCap { get; }
    public string UnitText { get; }
    public string Hint { get; }
    [ObservableProperty] public partial string Text { get; set; }
    [ObservableProperty] public partial string? Error { get; set; }

    /// <summary>Typing filter: digits (and the money scale's decimals) never above a known provider limit.
    /// The current text is always accepted, so a cap kept above the limit opens unchanged and can only be lowered.</summary>
    public bool Accepts(string text)
    {
        var decimals = Scale.Kind == ScaleKind.Money ? Math.Clamp(Scale.Exponent ?? 2, 0, 18) : 0;
        var pattern = decimals > 0 ? @"\A[0-9]+(?:\.[0-9]{0," + decimals + @"})?\z" : @"\A[0-9]+\z";
        return text.Length == 0 || text == Text ||
            Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)) &&
            decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) && providerLimit.AllowsCap(value);
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        decimal? amount = null;
        if (!string.IsNullOrWhiteSpace(Text))
        {
            if (!LedgerFormat.TryParseAmount(Text, Scale, out var parsed))
            {
                Error = Scale.Kind == ScaleKind.Money ? "Enter an amount with at most " + (Scale.Exponent ?? 2) + " decimals" : "Enter a whole number";
                return;
            }
            if (!providerLimit.AllowsCap(parsed))
            {
                Error = "Enter at most " + LedgerFormat.Value(Scale, providerLimit.Amount!.Value) + " · the provider limit";
                return;
            }
            amount = parsed;
        }
        else if (!HasCap)
        {
            Error = "Enter a cap";
            return;
        }
        Error = null;
        if (await commit(amount))
            close();
        else
            Error = "The cap was not saved";
    }

    [RelayCommand]
    public Task RemoveAsync()
    {
        Text = string.Empty;
        return SaveAsync();
    }

    [RelayCommand]
    public void Cancel() => close();
}

/// <summary>One card of the main window. The owner supplies every command; the card holds only view state.</summary>
internal sealed partial class LimitCardViewModel : ObservableObject
{
    private readonly LedgerViewModel owner;

    public LimitCardViewModel(LedgerViewModel owner, LimitCardModel model, AccountModel account, ValueMode mode, DateTimeOffset now)
    {
        this.owner = owner;
        Model = model;
        Account = account;
        Visual = BuildVisual(mode, now);
    }

    public LimitCardModel Model { get; private set; }
    public AccountModel Account { get; private set; }
    public string CardId => Model.CardId;
    [ObservableProperty] public partial CardVisual Visual { get; private set; }
    [ObservableProperty] public partial bool IsRenaming { get; private set; }
    [ObservableProperty] public partial string RenameText { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingCap))]
    public partial CapEditorViewModel? CapEditor { get; private set; }
    public bool IsEditingCap => CapEditor is not null;
    [ObservableProperty] public partial bool IsNew { get; set; }
    [ObservableProperty] public partial bool IsHistoryOpen { get; set; }
    [ObservableProperty] public partial bool IsCompact { get; set; } = true;

    /// <summary>Every limit other than the account's primary one is drawn inside the primary card, so one account is one card (D-191).</summary>
    public bool IsAccountSection => AccountCard.Primary(Account)?.CardId != CardId;
    /// <summary>A section the owner hid; the window leaves it out and the primary card counts it.</summary>
    public bool IsHiddenSection => IsAccountSection && Model.Hidden;
    public bool CanHide => IsAccountSection;
    public string HideName => "Hide " + (Model.ScopeLabel ?? LedgerFormat.PeriodWords(Model.Period));
    [ObservableProperty] public partial int HiddenCount { get; private set; }
    [ObservableProperty] public partial Tone HiddenTone { get; private set; }
    public bool HasHidden => HiddenCount > 0;
    public string HiddenText => HiddenCount + " hidden";
    public string HiddenName => "Show " + HiddenCount + " hidden " + (HiddenCount == 1 ? "limit" : "limits");
    public string HiddenTip => "Hidden: " + string.Join(", ", HiddenCards().Select(c => c.ScopeLabel ?? LedgerFormat.PeriodWords(c.Period)));
    public string Name => Account.DisplayName;
    public string HeaderName => IsAccountSection ? string.Empty : Name;
    public string? Tag => Model.ScopeLabel;
    public bool HasTag => !string.IsNullOrEmpty(Model.ScopeLabel);
    public bool CanEditCap => Model.CapTargetId is not null;
    public bool CanSignOut => !IsAccountSection && Account.Health != AccountHealth.SignedOut;
    public bool CanOpenHistory => Model.Layout != CardLayout.Note || Model.Monetary is not null;
    public bool IsStale => Model.Freshness.IsStale;
    public string SignOutName => "Sign out " + Account.DisplayName;
    public string HistoryName => "History, " + Account.DisplayName + " " + (Model.ScopeLabel ?? LedgerFormat.PeriodWords(Model.Period));
    public string CapEditorName => "Cap for " + Account.DisplayName + " " + (Model.ScopeLabel ?? string.Empty) + (Model.Cap is { } cap ? ", " + LedgerFormat.Value(Model.Scale, cap.Amount) : ", none");

    public void Update(LimitCardModel model, AccountModel account, ValueMode mode, DateTimeOffset now)
    {
        Model = model;
        Account = account;
        Visual = BuildVisual(mode, now);
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Account-wide marks and Sign in belong to the host card; a section shows only its own limit's facts.</summary>
    private CardVisual BuildVisual(ValueMode mode, DateTimeOffset now)
    {
        var hidden = HiddenCards().ToArray();
        HiddenCount = hidden.Length;
        // The dot keeps the most urgent hidden state in sight.
        var tones = hidden.Select(c => CardVisuals.Build(c, Account, mode, now).Tone).ToArray();
        HiddenTone = tones.Contains(Tone.Critical) ? Tone.Critical : tones.Contains(Tone.Attention) ? Tone.Attention : Tone.Neutral;
        return CardVisuals.Build(!IsAccountSection ? Model : Model with
        {
            Marks = [.. Model.Marks.Where(m => m.Kind is not (MarkKind.SyncFailed or MarkKind.SignInExpired))],
            Action = Model.Action == CardAction.SignIn ? CardAction.None : Model.Action
        }, Account, mode, now);
    }

    private IEnumerable<LimitCardModel> HiddenCards() => IsAccountSection ? [] : Account.Cards.Where(c => c.Hidden && c.CardId != CardId);

    [RelayCommand]
    public void BeginRename()
    {
        if (IsAccountSection) return;
        RenameText = Account.DisplayName;
        IsRenaming = true;
    }

    [RelayCommand]
    public async Task CommitRenameAsync()
    {
        if (!IsRenaming)
            return;
        var text = RenameText.Trim();
        IsRenaming = false;
        if (text.Length > 0 && text != Account.DisplayName)
            await owner.RenameAsync(Account.AccountId, text);
    }

    [RelayCommand]
    public void CancelRename() => IsRenaming = false;

    [RelayCommand]
    public void BeginCapEdit()
    {
        if (!CanEditCap)
            return;
        var target = Model.CapTargetId!;
        var before = Model.Cap?.Status == CapStatus.CurrencyMismatch ? null : Model.Cap?.Amount;
        CapEditor = new CapEditorViewModel(Model.Scale, Model.Figures.ProviderLimit, before, LedgerFormat.PeriodWords(Model.Period),
            amount => owner.SetCapAsync(target, amount, before, Account.DisplayName + " " + (Model.ScopeLabel ?? string.Empty)),
            () => CapEditor = null);
    }

    [RelayCommand]
    public async Task ActionAsync()
    {
        if (Model.Action == CardAction.SignIn && !IsAccountSection)
            await owner.ReconnectAsync(Account.AccountId);
        else if (Model.Action == CardAction.SetCap)
            BeginCapEdit();
    }

    [RelayCommand]
    public Task SignOutAsync() => CanSignOut ? owner.SignOutAsync(Account.AccountId) : Task.CompletedTask;

    [RelayCommand]
    public Task ToggleHistoryAsync() => owner.ToggleHistoryAsync(this);

    [RelayCommand]
    public Task HideAsync() => CanHide ? owner.SetHiddenAsync(CardId, true) : Task.CompletedTask;

    [RelayCommand]
    public async Task ShowHiddenAsync()
    {
        foreach (var card in HiddenCards().ToArray())
            await owner.SetHiddenAsync(card.CardId, false);
    }

    [RelayCommand]
    public Task MoveUpAsync() => owner.MoveAsync(CardId, -1);

    [RelayCommand]
    public Task MoveDownAsync() => owner.MoveAsync(CardId, 1);
}
