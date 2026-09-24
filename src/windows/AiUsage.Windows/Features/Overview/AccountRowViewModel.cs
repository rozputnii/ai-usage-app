using System.Collections.ObjectModel;
using AiUsage.Features.Accounts;
using AiUsage.Features.Connection;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Overview;

/// <summary>The one status mark a compact row shows beside its label, most urgent first.</summary>
public enum RowStatus { None, Attention, Stale, Failure }

/// <summary>
/// One compact Overview row (D-181): label, one status mark, one bar per window of the primary group and the sign-out
/// action. Numbers, resets and pace advice live in each bar's hover text and in the accessible name; other groups,
/// plan, history and freshness stay in account detail.
/// </summary>
internal sealed partial class AccountRowViewModel : ObservableObject
{
    private readonly PresentationContext context;
    private readonly IAccountConnector connector;
    private readonly Action<AccountRowViewModel, int> move;
    private AccountItem account = null!;

    public AccountRowViewModel(string id, PresentationContext context, IAccountConnector connector, Action<AccountRowViewModel, int> move)
    {
        Id = id;
        this.context = context;
        this.connector = connector;
        this.move = move;
    }

    public string Id { get; }
    public string ProviderId => account.ProviderId;
    public StatusPillViewModel Pill { get; } = new();
    public FailureViewModel Failure { get; } = new();
    public ObservableCollection<QuotaWindowViewModel> Windows { get; } = [];

    [ObservableProperty] public partial string Label { get; private set; } = string.Empty;
    [ObservableProperty] public partial string HiddenTag { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsDisconnected { get; private set; }
    [ObservableProperty] public partial bool IsStale { get; private set; }
    [ObservableProperty] public partial bool HasPrimary { get; private set; }
    [ObservableProperty] public partial string NoPrimaryText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool NeedsSignIn { get; private set; }
    [ObservableProperty] public partial string SignInPrompt { get; private set; } = string.Empty;
    [ObservableProperty] public partial RowStatus Status { get; private set; }
    [ObservableProperty] public partial ValueTone StatusTone { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = string.Empty;
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(CancelRefreshCommand), nameof(RetryCommand))]
    [ObservableProperty] public partial bool IsRefreshing { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(RetryCommand))]
    [ObservableProperty] public partial bool CanRefresh { get; private set; }
    [ObservableProperty] public partial string AccessibleName { get; private set; } = string.Empty;
    [ObservableProperty] public partial int Position { get; private set; }
    [ObservableProperty] public partial bool IsHovered { get; set; }
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [ObservableProperty] public partial bool CanMoveUp { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [ObservableProperty] public partial bool CanMoveDown { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(SignOutCommand))]
    [ObservableProperty] public partial bool CanSignOut { get; private set; }
    [ObservableProperty] public partial bool CanSignIn { get; private set; }

    public bool HasHiddenTag => HiddenTag.Length > 0;
    public bool HasStatus => Status != RowStatus.None;
    /// <summary>Bars give way to a sign-in prompt when the account is signed out or its grant expired.</summary>
    public bool ShowSignInPrompt => NeedsSignIn || IsDisconnected;
    public bool ShowBars => HasPrimary && !ShowSignInPrompt;
    public bool ShowNoPrimary => !HasPrimary && !ShowSignInPrompt;
    public string RefreshName => context.Format.F("Row_RefreshName", Label);
    public string SignOutName => context.Format.F("Row_SignOutName", Label);
    public string SignInName => context.Format.F("Row_SignInName", Label);

    partial void OnHiddenTagChanged(string value) => OnPropertyChanged(nameof(HasHiddenTag));
    partial void OnStatusChanged(RowStatus value) => OnPropertyChanged(nameof(HasStatus));
    partial void OnHasPrimaryChanged(bool value) => NotifyBarsChanged();
    partial void OnNeedsSignInChanged(bool value) => NotifyBarsChanged();
    partial void OnIsDisconnectedChanged(bool value) => NotifyBarsChanged();
    partial void OnLabelChanged(string value)
    {
        OnPropertyChanged(nameof(RefreshName));
        OnPropertyChanged(nameof(SignOutName));
        OnPropertyChanged(nameof(SignInName));
    }

    private void NotifyBarsChanged()
    {
        OnPropertyChanged(nameof(ShowSignInPrompt));
        OnPropertyChanged(nameof(ShowBars));
        OnPropertyChanged(nameof(ShowNoPrimary));
    }

    public void Update(AccountItem item, UiSnapshot snapshot, int position, int count, bool canMoveUp, bool canMoveDown)
    {
        var format = context.Format;
        var preferences = snapshot.Preferences;
        account = item;
        var provider = context.Providers.Get(item.ProviderId);
        Label = item.Label;
        Pill.Update(item, format);
        Failure.Update(item, format);
        IsDisconnected = item.Connection == ConnectionState.NotConnected;
        IsStale = item.Freshness == Freshness.Stale;
        NeedsSignIn = Failure.IsVisible && Failure.Action == FailureAction.Reconnect;
        SignInPrompt = format.T(IsDisconnected ? "Row_SignedOut" : "Row_SignInExpired");
        HiddenTag = preferences.HiddenTargets.Contains(item.Id)
            ? format.T(preferences.MutedTargets.Contains(item.Id) ? "Row_HiddenMuted" : "Row_Hidden")
            : string.Empty;
        Position = position;
        CanMoveUp = canMoveUp;
        CanMoveDown = canMoveDown;
        CanRefresh = item.Operation == AccountOperation.Idle && item.Connection is ConnectionState.Connected or ConnectionState.RecoveryRequired
            && snapshot.System.Compatibility != CompatibilityState.SecurityBlocked
            && QuotaRules.IsAvailable(snapshot, CapabilityKeys.For(UiCommandKind.RefreshAccount), item.Id)
            && !(item.Failure is { Kind: FailureKinds.RateLimited, RetryAt: { } retryAt } && retryAt > context.Clock.UtcNow);
        IsRefreshing = item.Operation == AccountOperation.Refreshing;
        CanSignOut = !IsDisconnected && item.Operation == AccountOperation.Idle
            && QuotaRules.IsAvailable(snapshot, nameof(UiCommandKind.Disconnect), item.Id);
        CanSignIn = IsDisconnected && item.Operation == AccountOperation.Idle
            && snapshot.System.Compatibility != CompatibilityState.SecurityBlocked;
        RetryCommand.NotifyCanExecuteChanged();

        var selectedContext = QuotaRules.SelectedContext(item, preferences);
        var primaryGroup = QuotaRules.VisibleGroups(selectedContext, preferences).FirstOrDefault();
        var primaryWindows = primaryGroup?.Windows ?? [];
        CollectionSync.Sync(Windows, primaryWindows, w => w.Id, vm => vm.Id, w => new QuotaWindowViewModel(w.Id), (vm, w) =>
        {
            vm.Update(item, primaryGroup!, w, preferences, format);
            vm.ApplyPace(w, item.Freshness, preferences, format);
        });
        HasPrimary = Windows.Count > 0;
        NoPrimaryText = format.T(item.Connection == ConnectionState.NotConnected ? "Row_MonitoringStopped" : "Row_NoWindows");

        // One mark: a failure outranks staleness, which outranks quota attention; stale bars never look current.
        var worst = Windows.Select(w => w.PaceTone).Where(t => t is ValueTone.Warning or ValueTone.Critical).DefaultIfEmpty(ValueTone.Normal).Max();
        (Status, StatusTone, StatusText) = ShowSignInPrompt ? (RowStatus.None, ValueTone.Normal, string.Empty)
            : Failure.IsVisible
            ? (RowStatus.Failure, ValueTone.Critical, Failure.HasWait ? $"{Failure.Message} {Failure.WaitText}" : Failure.Message)
            : IsStale ? (RowStatus.Stale, ValueTone.Muted, Pill.Text)
            : worst is ValueTone.Warning or ValueTone.Critical ? (RowStatus.Attention, worst,
                string.Join(Environment.NewLine, Windows.Where(w => w.PaceTone == worst).Select(w => w.HintText)))
            : (RowStatus.None, ValueTone.Normal, string.Empty);

        var primary = Windows.FirstOrDefault();
        AccessibleName = format.F("Row_Aria", item.Label, provider.PresentationName, Pill.Text, primary?.AccessibleName ?? format.T("Row_NoMeasurement"), position, count)
            + string.Concat(Windows.Where(w => w.PaceText.Length > 0).Select(w => $". {w.Label}: {w.PaceText}"));
    }

    private bool CanRefreshNow() => CanRefresh && !IsRefreshing;

    [RelayCommand(CanExecute = nameof(CanRefreshNow))]
    private async Task RefreshAsync()
    {
        var label = Label;
        var result = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.RefreshAccount, Id), CancellationToken.None);
        context.Announcer.Announce(result.Status switch
        {
            CommandStatus.Succeeded => context.Format.F("Announce_Updated", label),
            CommandStatus.Cancelled => context.Format.T("Announce_RefreshCancelled"),
            CommandStatus.Failed => context.Format.F("Announce_RefreshFailed", label),
            _ => context.Format.F("Announce_RefreshUnavailable", label),
        });
    }

    [RelayCommand(CanExecute = nameof(IsRefreshing))]
    private Task CancelRefreshAsync() => context.Usage.ExecuteAsync(context.Command(UiCommandKind.CancelOperation, Id), CancellationToken.None);

    private bool CanRetry() => Failure.Action == FailureAction.Reconnect || (CanRefresh && !IsRefreshing);

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private Task RetryAsync() => Failure.Action == FailureAction.Reconnect ? ReconnectAsync() : RefreshAsync();

    [RelayCommand]
    private Task ReconnectAsync() => connector.ReconnectAsync(account.ProviderId, account.Id);

    /// <summary>
    /// Row sign-out: immediate, without confirmation (owner decision 2026-09-24). It removes only the app-owned
    /// credential and stops monitoring; history, label and order stay for the next sign-in (D-093). Stored data is
    /// deleted only by the separate Delete stored data action.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSignOut))]
    private async Task SignOutAsync()
    {
        var label = Label;
        var result = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.Disconnect, Id), CancellationToken.None);
        context.Announcer.Announce(result.Status == CommandStatus.Succeeded
            ? context.Format.F("Announce_SignedOut", label)
            : context.Format.F("Announce_SignOutFailed", label));
    }

    [RelayCommand]
    private void Open() => context.Navigation.Navigate(new(PageKey.Accounts, Id));

    /// <summary>The status mark retries a failed refresh where possible; otherwise it opens the account detail.</summary>
    [RelayCommand]
    private Task StatusActionAsync()
    {
        if (Status == RowStatus.Failure && CanRetry())
            return RetryAsync();
        Open();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => move(this, -1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => move(this, 1);
}
