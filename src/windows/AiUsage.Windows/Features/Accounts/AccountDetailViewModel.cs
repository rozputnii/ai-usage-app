using System.Collections.ObjectModel;
using AiUsage.Features.Connection;
using AiUsage.Features.History;
using AiUsage.Features.Presentation;
using AiUsage.Features.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Accounts;

/// <summary>
/// S02 account detail for the selected account: hero, contexts, groups, native amounts, extensions, rename and the
/// account menu (move, hide, mute, delete stored data, sign out). Sign-out is immediate and keeps label, order and
/// history (D-093/D-094); Reconnect uses the same inline sign-in as the Add account menu.
/// </summary>
internal sealed partial class AccountDetailViewModel : ObservableObject
{
    private readonly PresentationContext context;
    private readonly IHistorySource history;
    private readonly IDataManagementService data;
    private readonly IAccountConnector connector;
    private AccountItem? account;
    private AccountOperation previousOperation;
    private DateTimeOffset? previousFetchedAt;
    private long previousObservation;
    private string? previousId;
    private int ackGeneration;
    private CancellationTokenSource? sparklineLoad;

    public AccountDetailViewModel(PresentationContext context, IHistorySource history, IDataManagementService data, IAccountConnector connector)
    {
        this.context = context;
        this.history = history;
        this.data = data;
        this.connector = connector;
        Rename = new RenameAccountViewModel(SaveLabelAsync, context.Format.Text);
    }

    public RenameAccountViewModel Rename { get; }

    [RelayCommand]
    private void OpenHistory() => context.Navigation.Navigate(new(PageKey.History, AccountId));
    public StatusPillViewModel Pill { get; } = new();
    public FailureViewModel Failure { get; } = new();
    public QuotaWindowViewModel Hero { get; } = new("hero");
    public ObservableCollection<ContextOptionViewModel> Contexts { get; } = [];
    public ObservableCollection<QuotaGroupViewModel> Groups { get; } = [];
    public ObservableCollection<ExtensionViewModel> Extensions { get; } = [];
    public ObservableCollection<string> InfoLines { get; } = [];

    [ObservableProperty] public partial string AccountId { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ProviderId { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Glyph { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Meta { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Label { get; private set; } = string.Empty;
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand), nameof(DisconnectCommand), nameof(ReconnectCommand))]
    [ObservableProperty] public partial bool IsRefreshing { get; private set; }
    [ObservableProperty] public partial bool ShowAck { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [ObservableProperty] public partial bool CanRefresh { get; private set; }
    [ObservableProperty] public partial string ReconnectLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ReconnectIsPrimary { get; private set; }
    [ObservableProperty] public partial bool HasHero { get; private set; }
    [ObservableProperty] public partial string HeroLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool HasContexts { get; private set; }
    [ObservableProperty] public partial string ContextKindText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string DisconnectedNote { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsDisconnected { get; private set; }
    [ObservableProperty] public partial string ObservedText { get; private set; } = string.Empty;
    [ObservableProperty] public partial ChartModel Sparkline { get; private set; } = ChartModel.Empty;
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [ObservableProperty] public partial bool CanMoveUp { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [ObservableProperty] public partial bool CanMoveDown { get; private set; }
    [ObservableProperty] public partial bool IsHidden { get; private set; }
    [ObservableProperty] public partial bool IsMuted { get; private set; }
    [ObservableProperty] public partial string HideMenuLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial string MuteMenuLabel { get; private set; } = string.Empty;
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [ObservableProperty] public partial bool CanDisconnect { get; private set; }
    [ObservableProperty] public partial string DisconnectMenuLabel { get; private set; } = string.Empty;

    public bool HasDisconnectedNote => DisconnectedNote.Length > 0;
    public bool HasExtensions => Extensions.Count > 0;
    public bool HasInfo => InfoLines.Count > 0;
    public bool HasSparkline => Sparkline.ObservedCount > 1;
    partial void OnDisconnectedNoteChanged(string value) => OnPropertyChanged(nameof(HasDisconnectedNote));
    partial void OnSparklineChanged(ChartModel value) => OnPropertyChanged(nameof(HasSparkline));

    public void Update(AccountItem item, UiSnapshot snapshot)
    {
        var format = context.Format;
        var preferences = snapshot.Preferences;
        var switched = previousId != item.Id;
        if (switched && Rename.IsEditing)
            Rename.CancelCommand.Execute(null);
        account = item;
        var provider = context.Providers.Get(item.ProviderId);
        AccountId = item.Id;
        ProviderId = item.ProviderId;
        Glyph = provider.Glyph;
        Meta = format.F("Detail_Meta", provider.PresentationName, item.Plan ?? format.T("Account_PlanUnknown"));
        Label = item.Label;
        Pill.Update(item, format);
        Failure.Update(item, format);
        IsRefreshing = item.Operation == AccountOperation.Refreshing;
        IsDisconnected = item.Connection == ConnectionState.NotConnected;
        CanRefresh = item.Operation == AccountOperation.Idle && item.Connection is ConnectionState.Connected or ConnectionState.RecoveryRequired
            && snapshot.System.Compatibility != CompatibilityState.SecurityBlocked
            && !(item.Failure is { Kind: FailureKinds.RateLimited, RetryAt: { } retryAt } && retryAt > context.Clock.UtcNow);
        ReconnectLabel = format.T(IsDisconnected ? "Action_Connect" : "Action_Reconnect");
        ReconnectIsPrimary = IsDisconnected || item.Connection == ConnectionState.ReauthRequired;
        ReconnectCommand.NotifyCanExecuteChanged();

        var ordered = QuotaRules.Ordered(snapshot).Select(a => a.Id).ToList();
        var position = ordered.IndexOf(item.Id);
        CanMoveUp = position > 0;
        CanMoveDown = position >= 0 && position < ordered.Count - 1;
        IsHidden = preferences.HiddenTargets.Contains(item.Id);
        IsMuted = preferences.MutedTargets.Contains(item.Id);
        HideMenuLabel = format.T(IsHidden ? "Menu_ShowInOverview" : "Menu_Hide");
        MuteMenuLabel = format.T(IsMuted ? "Menu_UnmuteAlerts" : "Menu_MuteAlerts");
        CanDisconnect = !IsDisconnected && item.Operation == AccountOperation.Idle;
        DisconnectMenuLabel = format.T(IsDisconnected ? "Menu_Disconnected" : "Menu_Disconnect");

        var selected = QuotaRules.SelectedContext(item, preferences);
        var available = item.Contexts.Where(c => c.Available).ToArray();
        HasContexts = available.Length > 1;
        CollectionSync.Sync(Contexts, available, c => c.Id, vm => vm.Id, c => new ContextOptionViewModel(c.Id), (vm, c) =>
        {
            vm.Label = c.Label;
            vm.IsSelected = c.Id == selected?.Id;
        });
        ContextKindText = selected is null ? string.Empty : format.F("Detail_ContextNote", format.T("ContextKind_" + selected.Kind));

        var primaryGroup = QuotaRules.VisibleGroups(selected, preferences).FirstOrDefault();
        var primary = QuotaRules.PrimaryWindow(item, preferences);
        HasHero = primary is not null && primaryGroup is not null;
        if (primary is not null && primaryGroup is not null)
        {
            var owningGroup = QuotaRules.VisibleGroups(selected, preferences).First(g => g.Windows.Contains(primary));
            Hero.Update(item, owningGroup, primary, preferences, format);
            HeroLabel = string.Join(" · ", new[] { Hero.StateWord, primary.Label, item.Contexts.Count > 1 ? selected?.Label : null }.Where(s => !string.IsNullOrEmpty(s)));
        }

        DisconnectedNote = IsDisconnected ? format.T("Detail_DisconnectedNote") : string.Empty;
        var groups = QuotaRules.VisibleGroups(selected, preferences).ToArray();
        CollectionSync.Sync(Groups, groups, g => g.Id, vm => vm.Id, g => new QuotaGroupViewModel(g.Id, this), (vm, g) => vm.Update(item, g, Array.IndexOf(groups, g), preferences, format));

        Extensions.Clear();
        foreach (var extension in item.Extensions)
            Extensions.Add(ExtensionViewModel.From(extension, format));
        OnPropertyChanged(nameof(HasExtensions));
        InfoLines.Clear();
        if (item.AvailableResetCredits is { } credits)
            InfoLines.Add(format.F("Detail_ResetCredits", credits));
        if (item.SpendControlReached is { } reached)
            InfoLines.Add(format.T(reached ? "Detail_SpendControlReached" : "Detail_SpendControlNotReached"));
        if (item.LimitReachedType is { } limitReason)
            InfoLines.Add(format.F("Detail_LimitReason", limitReason));
        foreach (var group in item.Contexts.SelectMany(c => c.Groups))
        {
            if (group.Allowed == false) InfoLines.Add(format.F("Detail_GroupNotAllowed", group.Label));
            if (group.LimitReached == true) InfoLines.Add(format.F("Detail_GroupLimitReached", group.Label));
        }
        OnPropertyChanged(nameof(HasInfo));

        ObservedText = item.FetchedAt is { } fetched
            ? format.F("Detail_LastObservation", format.FullDateTime(fetched), format.T("Freshness_" + item.Freshness),
                item.Connection == ConnectionState.ReauthRequired ? format.T("Detail_CachedUntilReconnect") : string.Empty)
            : format.T("Detail_NoObservation");

        if (!switched && previousOperation == AccountOperation.Refreshing && item.Operation == AccountOperation.Idle && item.Failure is null
            && item.ObservationRevision != previousObservation)
            _ = AcknowledgeAsync();
        if (switched)
        {
            ShowAck = false;
            _ = LoadSparklineAsync();
        }
        else if (previousObservation != item.ObservationRevision || previousFetchedAt != item.FetchedAt)
            _ = LoadSparklineAsync();
        previousId = item.Id;
        previousOperation = item.Operation;
        previousObservation = item.ObservationRevision;
        previousFetchedAt = item.FetchedAt;
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
        if (account is null || QuotaRules.PrimaryWindow(account, context.Usage.Current.Preferences) is not { } primary)
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
            if (!cancellation.IsCancellationRequested)
                Sparkline = ChartModel.Build(result.Points, context.Usage.Current.Preferences.UsageDisplay, context.Format, withTicks: false);
        }
        catch (OperationCanceledException) { }
    }

    private async Task<bool> SaveLabelAsync(string label)
    {
        if (account is null)
            return false;
        var result = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.Rename, account.Id, new RenamePayload(label)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
            context.Announcer.Announce(context.Format.F("Announce_Renamed", label));
        return result.Status == CommandStatus.Succeeded;
    }

    internal async Task SetExpansionAsync(QuotaGroupViewModel group, ExpansionPreference expansion)
    {
        var result = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.SetExpansion, group.Id, new ExpansionPayload(expansion)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
            context.Announcer.Announce(context.Format.F(expansion == ExpansionPreference.Expanded ? "Announce_Expanded" : "Announce_Collapsed", group.Label));
    }

    internal async Task ToggleHideAsync(string targetId, string label, bool currentlyHidden)
    {
        if (currentlyHidden)
        {
            var shown = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.SetVisibility, targetId, new VisibilityPayload(false, false)), CancellationToken.None);
            if (shown.Status == CommandStatus.Succeeded) context.Announcer.Announce(context.Format.T("Announce_Shown"));
            return;
        }
        var format = context.Format;
        var outcome = await context.Dialogs.ConfirmAsync(new(
            format.F("Hide_Title", label), format.T("Hide_Body"), format.T("Hide_Only"),
            AlternateLabel: QuotaRules.IsAvailable(context.Usage.Current, nameof(UiCommandKind.SetMute)) ? format.T("Hide_AndMute") : null));
        if (outcome == ConfirmOutcome.Cancelled)
        {
            context.Announcer.Announce(format.T("Announce_Cancelled"));
            return;
        }
        var mute = outcome == ConfirmOutcome.Alternate;
        var result = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.SetVisibility, targetId, new VisibilityPayload(true, mute)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded) context.Announcer.Announce(format.T(mute ? "Announce_HiddenMuted" : "Announce_Hidden"));
    }

    private bool CanRefreshNow() => CanRefresh && !IsRefreshing;

    [RelayCommand(CanExecute = nameof(CanRefreshNow))]
    private async Task RefreshAsync()
    {
        if (account is null)
            return;
        var label = account.Label;
        var result = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.RefreshAccount, account.Id), CancellationToken.None);
        context.Announcer.Announce(result.Status switch
        {
            CommandStatus.Succeeded => context.Format.F("Announce_Updated", label),
            CommandStatus.Cancelled => context.Format.T("Announce_RefreshCancelled"),
            CommandStatus.Failed => context.Format.F("Announce_RefreshFailed", label),
            _ => context.Format.F("Announce_RefreshUnavailable", label),
        });
    }

    [RelayCommand]
    private Task CancelRefreshAsync() => account is null ? Task.CompletedTask : context.Usage.ExecuteAsync(context.Command(UiCommandKind.CancelOperation, account.Id), CancellationToken.None);

    private bool CanReconnect() => account is not null && !IsRefreshing && context.Usage.Current.System.Compatibility != CompatibilityState.SecurityBlocked;

    [RelayCommand(CanExecute = nameof(CanReconnect))]
    private Task ReconnectAsync() => account is null ? Task.CompletedTask : connector.ReconnectAsync(account.ProviderId, account.Id);

    [RelayCommand]
    private Task RetryAsync() => Failure.Action == FailureAction.Reconnect ? ReconnectAsync() : RefreshAsync();

    [RelayCommand]
    private void StartRename()
    {
        if (account is not null)
            Rename.Begin(account.Label);
    }

    [RelayCommand]
    private Task SelectContextAsync(ContextOptionViewModel? option) =>
        account is null || option is null ? Task.CompletedTask
            : AnnounceAfter(context.Usage.ExecuteAsync(context.Command(UiCommandKind.SelectContext, account.Id, new ContextSelectionPayload(option.Id)), CancellationToken.None),
                context.Format.F("Announce_Context", option.Label));

    private async Task AnnounceAfter(Task<UiCommandResult> operation, string message)
    {
        if ((await operation).Status == CommandStatus.Succeeded)
            context.Announcer.Announce(message);
    }

    private Task MoveAsync(int direction)
    {
        if (account is null)
            return Task.CompletedTask;
        var order = QuotaRules.Ordered(context.Usage.Current).Select(a => a.Id).ToList();
        var from = order.IndexOf(account.Id);
        var to = from + direction;
        if (from < 0 || to < 0 || to >= order.Count)
            return Task.CompletedTask;
        (order[from], order[to]) = (order[to], order[from]);
        return AnnounceAfter(context.Usage.ExecuteAsync(context.Command(UiCommandKind.Reorder, null, new ReorderPayload(order)), CancellationToken.None),
            context.Format.F("Announce_MovedTo", account.Label, to + 1, order.Count));
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private Task MoveUpAsync() => MoveAsync(-1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private Task MoveDownAsync() => MoveAsync(1);

    [RelayCommand]
    private Task ToggleAccountHiddenAsync() => account is null ? Task.CompletedTask : ToggleHideAsync(account.Id, account.Label, IsHidden);

    [RelayCommand]
    private Task ToggleMuteAsync() => account is null ? Task.CompletedTask
        : AnnounceAfter(context.Usage.ExecuteAsync(context.Command(UiCommandKind.SetMute, account.Id, new MutePayload(!IsMuted)), CancellationToken.None),
            context.Format.T(IsMuted ? "Announce_Unmuted" : "Announce_Muted"));

    [RelayCommand]
    private async Task DeleteStoredDataAsync()
    {
        if (account is null)
            return;
        var target = account;
        var format = context.Format;
        var outcome = await context.Dialogs.ConfirmAsync(new(
            format.F("DeleteData_Title", target.Label), format.T("DeleteData_Body"), format.T("DeleteData_Confirm"), Destructive: true,
            BusyLabel: format.T("DeleteData_Busy"),
            ConfirmAction: async token =>
            {
                var result = await data.DeleteAccountDataAsync(target.Id, token);
                return result.Status == CommandStatus.Succeeded ? null : format.T("Dialog_OperationFailed");
            }));
        context.Announcer.Announce(outcome == ConfirmOutcome.Confirmed ? format.F("Announce_DataDeleted", target.Label) : format.T("Announce_Cancelled"));
    }

    /// <summary>Sign-out without confirmation (owner decision 2026-09-24); history, label and order are kept.</summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        if (account is null)
            return;
        var label = account.Label;
        var result = await context.Usage.ExecuteAsync(context.Command(UiCommandKind.Disconnect, account.Id), CancellationToken.None);
        context.Announcer.Announce(result.Status == CommandStatus.Succeeded
            ? context.Format.F("Announce_SignedOut", label)
            : context.Format.F("Announce_SignOutFailed", label));
    }
}
