using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Settings.Updates;

/// <summary>
/// S12 Updates and compatibility: Check (cancellable) → Available → Download (determinate) → Ready → confirmed
/// Restart &amp; update. Failure keeps the current build. Preview → Stable waits for Stable without downgrade (D-158).
/// </summary>
internal sealed partial class UpdatesViewModel : SnapshotViewModel
{
    private readonly IUpdateService updates;
    private readonly DemoScenarioController? demo;
    private CancellationTokenSource? operation;
    private bool applying;

    public UpdatesViewModel(PresentationContext context, IUpdateService updates, DemoScenarioController? demo = null) : base(context)
    {
        this.updates = updates;
        this.demo = demo;
        ChannelLabels = [context.Format.T("Channel_Stable"), context.Format.T("Channel_Preview")];
        Initialize();
    }

    public IReadOnlyList<string> ChannelLabels { get; }
    public bool HasSimulator => demo is not null;

    [NotifyPropertyChangedFor(nameof(ShowCheck), nameof(IsChecking), nameof(ShowDownload), nameof(IsDownloading), nameof(ShowRestart))]
    [ObservableProperty] public partial UpdateState State { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool StatusIsFailure { get; private set; }
    [ObservableProperty] public partial string BuildText { get; private set; } = string.Empty;
    [ObservableProperty] public partial double DownloadProgress { get; private set; }
    [ObservableProperty] public partial int ChannelIndex { get; set; }
    [ObservableProperty] public partial bool CanOverrideCompatibility { get; private set; }
    [ObservableProperty] public partial string CompatibilityText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsSecurityBlocked { get; private set; }

    public bool ShowCheck => State is UpdateState.Current or UpdateState.Failed or UpdateState.WaitingForStable or UpdateState.Unsupported;
    public bool IsChecking => State == UpdateState.Checking;
    public bool ShowDownload => State == UpdateState.Available;
    public bool IsDownloading => State == UpdateState.Downloading;
    public bool ShowRestart => State == UpdateState.Ready;

    partial void OnChannelIndexChanged(int value)
    {
        if (applying || value < 0)
            return;
        var channel = (UpdateChannel)value;
        _ = ChangeChannelAsync(channel);
    }

    private async Task ChangeChannelAsync(UpdateChannel channel)
    {
        var result = await updates.SetChannelAsync(channel, CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded && Snapshot.System.Update == UpdateState.WaitingForStable)
            Context.Announcer.Announce(Format.T("Announce_WaitingForStable"));
    }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var system = snapshot.System;
        applying = true;
        try
        {
            State = system.Update;
            var version = system.UpdateVersion ?? "1.1.0";
            StatusText = system.Update switch
            {
                UpdateState.Unsupported => format.T("Update_Unsupported"),
                UpdateState.Checking => format.T("Update_Checking"),
                UpdateState.Available => format.F("Update_Available", version),
                UpdateState.Downloading => format.F("Update_Downloading", version, format.Percent(system.UpdateProgress ?? 0)),
                UpdateState.Ready => format.F("Update_Ready", version),
                UpdateState.Failed => format.T(system.UpdateFailureKey ?? "Update_Failed"),
                UpdateState.WaitingForStable => format.T("Update_Waiting"),
                _ => system.InstalledUpdateVersion is { } installed ? format.F("Update_CurrentInstalled", installed) : format.T("Update_Current"),
            };
            StatusIsFailure = system.Update == UpdateState.Failed;
            DownloadProgress = (system.UpdateProgress ?? 0) / 100;
            BuildText = format.F("Update_BuildLine", system.BuildLabel,
                system.LastUpdateCheck is { } checkedAt ? format.F("Update_LastChecked", format.DateTime(checkedAt)) : format.T("Update_NeverChecked"));
            ChannelIndex = (int)system.Channel;
            IsSecurityBlocked = system.Compatibility == CompatibilityState.SecurityBlocked;
            CanOverrideCompatibility = system.Compatibility == CompatibilityState.CompatibilityBlocked && system.Channel == UpdateChannel.Preview && !system.CompatibilityOverridden;
            CompatibilityText = system.Compatibility switch
            {
                CompatibilityState.SecurityBlocked => format.T("Compat_SecurityStatus"),
                CompatibilityState.CompatibilityBlocked when system.CompatibilityOverridden => format.T("Compat_Overridden"),
                CompatibilityState.CompatibilityBlocked => format.T(system.Channel == UpdateChannel.Preview ? "Compat_BlockedPreview" : "Compat_BlockedStable"),
                _ => format.T("Compat_Normal"),
            };
        }
        finally { applying = false; }
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        operation?.Cancel();
        using var cancellation = operation = new CancellationTokenSource();
        Context.Announcer.Announce(Format.T("Update_Checking"));
        var result = await updates.CheckAsync(cancellation.Token);
        if (operation == cancellation)
            operation = null;
        Context.Announcer.Announce(result.Status switch
        {
            CommandStatus.Succeeded => Format.F("Update_Available", Snapshot.System.UpdateVersion ?? string.Empty),
            CommandStatus.Cancelled => Format.T("Announce_CheckCancelled"),
            _ => Format.T("Update_Failed"),
        });
    }

    [RelayCommand]
    private void CancelCheck() => operation?.Cancel();

    [RelayCommand]
    private async Task DownloadAsync()
    {
        operation?.Cancel();
        using var cancellation = operation = new CancellationTokenSource();
        var result = await updates.DownloadAsync(cancellation.Token);
        if (operation == cancellation)
            operation = null;
        if (result.Status == CommandStatus.Succeeded)
            Context.Announcer.Announce(Format.T("Announce_UpdateReady"));
    }

    [RelayCommand]
    private async Task RestartAndUpdateAsync()
    {
        var format = Format;
        var version = Snapshot.System.UpdateVersion ?? string.Empty;
        var outcome = await Context.Dialogs.ConfirmAsync(new(format.T("Restart_Title"), format.F("Restart_Body", version), format.T("Restart_Confirm"),
            BusyLabel: format.T("Restart_Busy"),
            ConfirmAction: async token => (await updates.RestartAndUpdateAsync(token)).Status == CommandStatus.Succeeded ? null : format.T("Dialog_OperationFailed")));
        Context.Announcer.Announce(format.T(outcome == ConfirmOutcome.Confirmed ? "Announce_UpdateInstalled" : "Announce_Cancelled"));
    }

    [RelayCommand]
    private async Task OverrideCompatibilityAsync()
    {
        var format = Format;
        var outcome = await Context.Dialogs.ConfirmAsync(new(format.T("Override_Title"), format.T("Override_Body"), format.T("Override_Confirm"),
            ConfirmAction: async token => (await updates.OverrideCompatibilityBlockAsync(token)).Status == CommandStatus.Succeeded ? null : format.T("Dialog_OperationFailed")));
        if (outcome == ConfirmOutcome.Confirmed)
            Context.Announcer.Announce(format.T("Compat_Overridden"));
    }

    [RelayCommand]
    private Task SimulateCheckFailureAsync()
    {
        if (demo is null)
            return Task.CompletedTask;
        demo.FailNextUpdateCheck = true;
        return CheckAsync();
    }

    [RelayCommand]
    private void ToggleCompatibilityBlock()
    {
        if (demo is not null)
            demo.CompatibilityBlocked = !demo.CompatibilityBlocked;
    }

    [RelayCommand]
    private void ToggleSecurityBlock()
    {
        if (demo is not null)
            demo.SecurityBlocked = !demo.SecurityBlocked;
    }
}
