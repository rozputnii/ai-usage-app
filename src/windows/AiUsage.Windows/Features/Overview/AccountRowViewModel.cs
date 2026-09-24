using System.Collections.ObjectModel;
using AiUsage.Features.Accounts;
using AiUsage.Features.Connection;
using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Overview;

/// <summary>Extra group lines shown when an Overview row is expanded.</summary>
internal sealed partial class GroupLinesViewModel(string id) : ObservableObject
{
    public string Id { get; } = id;
    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    public ObservableCollection<QuotaWindowViewModel> Windows { get; } = [];
}

/// <summary>
/// One Overview row: identity column plus one line per window of the primary group. Refresh feedback ("Updating…",
/// "✓ Updated") is derived from snapshot transitions, so it is correct whether refresh started here, in detail or in the tray.
/// </summary>
internal sealed partial class AccountRowViewModel : ObservableObject
{
    private readonly PresentationContext context;
    private readonly IHistorySource history;
    private readonly IAccountConnector connector;
    private readonly Action<AccountRowViewModel, int> move;
    private AccountItem account = null!;
    private AccountOperation previousOperation;
    private long previousObservation;
    private int ackGeneration;
    private CancellationTokenSource? sparklineLoad;

    public AccountRowViewModel(string id, PresentationContext context, IHistorySource history, IAccountConnector connector, Action<AccountRowViewModel, int> move)
    {
        Id = id;
        this.context = context;
        this.history = history;
        this.connector = connector;
        this.move = move;
    }

    public string Id { get; }
    public string ProviderId => account.ProviderId;
    public StatusPillViewModel Pill { get; } = new();
    public FailureViewModel Failure { get; } = new();
    public ObservableCollection<QuotaWindowViewModel> Windows { get; } = [];
    public ObservableCollection<GroupLinesViewModel> MoreGroups { get; } = [];
    public ObservableCollection<string> ExtensionLines { get; } = [];

    [ObservableProperty] public partial string Label { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SubText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string HiddenTag { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsDisconnected { get; private set; }
    [ObservableProperty] public partial bool HasPrimary { get; private set; }
    [ObservableProperty] public partial string NoPrimaryText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool HasMore { get; private set; }
    [NotifyPropertyChangedFor(nameof(ExpandLabel))]
    [ObservableProperty] public partial bool IsExpanded { get; private set; }
    [ObservableProperty] public partial string MoreContextText { get; private set; } = string.Empty;
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(CancelRefreshCommand), nameof(RetryCommand))]
    [ObservableProperty] public partial bool IsRefreshing { get; private set; }
    [ObservableProperty] public partial bool ShowAck { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(RetryCommand))]
    [ObservableProperty] public partial bool CanRefresh { get; private set; }
    [ObservableProperty] public partial string AccessibleName { get; private set; } = string.Empty;
    [ObservableProperty] public partial int Position { get; private set; }
    [ObservableProperty] public partial ChartModel Sparkline { get; private set; } = ChartModel.Empty;
    [ObservableProperty] public partial string SparklineCaption { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsHovered { get; set; }
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [ObservableProperty] public partial bool CanMoveUp { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [ObservableProperty] public partial bool CanMoveDown { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(SignOutCommand))]
    [ObservableProperty] public partial bool CanSignOut { get; private set; }
    [ObservableProperty] public partial bool CanSignIn { get; private set; }

    public bool HasHiddenTag => HiddenTag.Length > 0;
    public bool HasMoreContext => MoreContextText.Length > 0;
    public bool HasSparkline => Sparkline.ObservedCount > 1;
    public string RefreshName => context.Format.F("Row_RefreshName", Label);
    public string SignOutName => context.Format.F("Row_SignOutName", Label);
    public string SignInName => context.Format.F("Row_SignInName", Label);

    public string ExpandLabel
    {
        get
        {
            if (IsExpanded)
                return context.Format.T("Row_ShowLess");
            return MoreGroups.Count > 0 ? context.Format.F(MoreGroups.Count == 1 ? "Row_ShowAllOne" : "Row_ShowAllMany", MoreGroups.Count) : context.Format.T("Row_ShowAll");
        }
    }

    partial void OnHiddenTagChanged(string value) => OnPropertyChanged(nameof(HasHiddenTag));
    partial void OnMoreContextTextChanged(string value) => OnPropertyChanged(nameof(HasMoreContext));
    partial void OnSparklineChanged(ChartModel value) => OnPropertyChanged(nameof(HasSparkline));
    partial void OnLabelChanged(string value)
    {
        OnPropertyChanged(nameof(RefreshName));
        OnPropertyChanged(nameof(SignOutName));
        OnPropertyChanged(nameof(SignInName));
    }

    public void Update(AccountItem item, UiSnapshot snapshot, int position, int count, bool canMoveUp, bool canMoveDown)
    {
        var format = context.Format;
        var preferences = snapshot.Preferences;
        var first = account is null;
        account = item;
        var provider = context.Providers.Get(item.ProviderId);
        Label = item.Label;
        var selectedContext = QuotaRules.SelectedContext(item, preferences);
        SubText = string.Join(" · ", new[] { item.Plan ?? format.T("Account_PlanUnknown"), item.Contexts.Count > 1 ? selectedContext?.Label : null }.Where(s => s is not null));
        Pill.Update(item, format);
        Failure.Update(item, format);
        IsDisconnected = item.Connection == ConnectionState.NotConnected;
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
        RetryCommand.NotifyCanExecuteChanged();
        CanSignOut = !IsDisconnected && item.Operation == AccountOperation.Idle
            && QuotaRules.IsAvailable(snapshot, nameof(UiCommandKind.Disconnect), item.Id);
        CanSignIn = IsDisconnected && item.Operation == AccountOperation.Idle
            && snapshot.System.Compatibility != CompatibilityState.SecurityBlocked;

        var groups = QuotaRules.VisibleGroups(selectedContext, preferences).ToArray();
        var primaryGroup = groups.FirstOrDefault();
        var primaryWindows = primaryGroup?.Windows ?? [];
        CollectionSync.Sync(Windows, primaryWindows, w => w.Id, vm => vm.Id, w => new QuotaWindowViewModel(w.Id), (vm, w) => vm.Update(item, primaryGroup!, w, preferences, format));
        HasPrimary = Windows.Count > 0;
        NoPrimaryText = format.T(item.Connection == ConnectionState.NotConnected ? "Row_MonitoringStopped" : "Row_NoWindows");

        var more = groups.Skip(1).ToArray();
        CollectionSync.Sync(MoreGroups, more, g => g.Id, vm => vm.Id, g => new GroupLinesViewModel(g.Id), (vm, g) =>
        {
            vm.Label = g.SharedPoolId is null ? g.Label : format.F("Row_SharedPoolGroup", g.Label);
            CollectionSync.Sync(vm.Windows, g.Windows, w => w.Id, w => w.Id, w => new QuotaWindowViewModel(w.Id), (wvm, w) => wvm.Update(item, g, w, preferences, format));
        });
        ExtensionLines.Clear();
        foreach (var extension in item.Extensions)
            ExtensionLines.Add(ExtensionLine(extension, format));
        MoreContextText = item.Contexts.Count > 1 ? format.F("Row_ContextsNote", item.Contexts.Count, selectedContext?.Label ?? string.Empty) : string.Empty;
        HasMore = more.Length > 0 || item.Extensions.Count > 0 || item.Contexts.Count > 1;
        OnPropertyChanged(nameof(ExpandLabel));

        var primary = Windows.FirstOrDefault();
        AccessibleName = format.F("Row_Aria", item.Label, provider.PresentationName, Pill.Text, primary?.AccessibleName ?? format.T("Row_NoMeasurement"), position, count);

        // "✓ Updated" follows an actual new observation, regardless of where the refresh started.
        if (!first && previousOperation == AccountOperation.Refreshing && item.Operation == AccountOperation.Idle
            && item.Failure is null && item.ObservationRevision != previousObservation)
            _ = AcknowledgeAsync();
        previousOperation = item.Operation;
        previousObservation = item.ObservationRevision;
        if (IsExpanded)
            _ = LoadSparklineAsync();
    }

    internal static string ExtensionLine(ExtensionItem extension, PresentationFormatter format)
    {
        var money = format.Money(extension.AmountMinor, extension.Exponent, extension.Currency);
        if (money is not null)
            return format.F("Extension_LineMoney", extension.Label, money, extension.Currency);
        return extension.AmountMinor is not null
            ? format.F("Extension_LineMinor", extension.Label, format.NativeNumber(extension.AmountMinor))
            : format.F("Extension_LineNone", extension.Label);
    }

    private async Task AcknowledgeAsync()
    {
        var generation = ++ackGeneration;
        ShowAck = true;
        await context.Clock.Delay(TimeSpan.FromMilliseconds(1500), CancellationToken.None);
        if (generation == ackGeneration)
            ShowAck = false;
    }

    private async Task LoadSparklineAsync()
    {
        var primary = QuotaRules.PrimaryWindow(account, context.Usage.Current.Preferences);
        if (primary is null || !QuotaRules.IsAvailable(context.Usage.Current, CapabilityKeys.ViewHistory))
        {
            Sparkline = ChartModel.Empty;
            return;
        }
        sparklineLoad?.Cancel();
        var cancellation = sparklineLoad = new CancellationTokenSource();
        var now = context.Clock.UtcNow;
        try
        {
            var result = await history.QueryHistoryAsync(new(account.Id, null, null, primary.Id, now - TimeSpan.FromHours(24), now, HistoryResolution.Auto, HistoryPreset.Hours24), cancellation.Token);
            if (cancellation.IsCancellationRequested)
                return;
            Sparkline = ChartModel.Build(result.Points, context.Usage.Current.Preferences.UsageDisplay, context.Format, withTicks: false);
            SparklineCaption = context.Format.F("Row_SparklineCaption", primary.Label);
        }
        catch (OperationCanceledException) { }
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

    [RelayCommand]
    private void OpenHistory() => context.Navigation.Navigate(new(PageKey.History, Id));

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
        context.Announcer.Announce(context.Format.F(IsExpanded ? "Announce_Expanded" : "Announce_Collapsed", Label));
        if (IsExpanded)
            _ = LoadSparklineAsync();
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => move(this, -1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => move(this, 1);
}
