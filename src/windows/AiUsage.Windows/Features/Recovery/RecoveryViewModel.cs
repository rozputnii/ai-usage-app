using System.Collections.ObjectModel;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Recovery;

internal sealed partial class CheckpointViewModel(RecoveryCheckpoint checkpoint, string label, string meta) : ObservableObject
{
    public RecoveryCheckpoint Checkpoint { get; } = checkpoint;
    public string Label { get; } = label;
    public string Meta { get; } = meta;
}

/// <summary>S11 blocking recovery surface. Nothing is wiped or restored automatically; failures keep current data.</summary>
internal sealed partial class RecoveryViewModel : SnapshotViewModel
{
    private readonly IRecoveryService recovery;

    public RecoveryViewModel(PresentationContext context, IRecoveryService recovery) : base(context)
    {
        this.recovery = recovery;
        Initialize();
    }

    public ObservableCollection<CheckpointViewModel> Checkpoints { get; } = [];

    [ObservableProperty] public partial bool IsActive { get; private set; }
    [ObservableProperty] public partial string Title { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Body { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SchemaText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string CheckpointText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string FolderText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Message { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool MessageIsCritical { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(RetryCommand), nameof(ToggleCheckpointsCommand))]
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty] public partial double Progress { get; private set; }
    [ObservableProperty] public partial string ProgressLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial string RetryLabel { get; private set; } = string.Empty;
    [NotifyCanExecuteChangedFor(nameof(RetryCommand))]
    [ObservableProperty] public partial bool RetryAvailable { get; private set; }
    [ObservableProperty] public partial string DisabledNote { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ShowCheckpoints { get; private set; }
    [ObservableProperty] public partial string DiagnosticsText { get; private set; } = string.Empty;

    public bool HasMessage => Message.Length > 0;
    public bool HasProgress => IsBusy && Progress > 0;
    public bool HasDisabledNote => DisabledNote.Length > 0;
    public bool HasDiagnostics => DiagnosticsText.Length > 0;
    partial void OnMessageChanged(string value) => OnPropertyChanged(nameof(HasMessage));
    partial void OnProgressChanged(double value) => OnPropertyChanged(nameof(HasProgress));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(HasProgress));
    partial void OnDisabledNoteChanged(string value) => OnPropertyChanged(nameof(HasDisabledNote));
    partial void OnDiagnosticsTextChanged(string value) => OnPropertyChanged(nameof(HasDiagnostics));

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var state = snapshot.System.Recovery;
        var wasActive = IsActive;
        IsActive = state != RecoveryState.None;
        if (!IsActive)
            return;
        if (!wasActive)
        {
            Message = string.Empty;
            DiagnosticsText = string.Empty;
            ShowCheckpoints = false;
            Progress = 0;
        }
        var format = Format;
        var details = recovery.Details;
        Title = format.T("Recovery_Title_" + state);
        Body = format.T((snapshot.Mode == UiMode.Live ? "RecoveryLive_Body_" : "Recovery_Body_") + state);
        SchemaText = details.SchemaText;
        CheckpointText = details.CheckpointId;
        FolderText = snapshot.Mode == UiMode.Live ? details.DataFolderPreview : format.F("Recovery_FolderPreview", details.DataFolderPreview);
        RetryAvailable = state != RecoveryState.NewerSchema;
        ToggleCheckpointsCommand.NotifyCanExecuteChanged();
        RetryLabel = format.T(state == RecoveryState.RestoreFailed ? "Recovery_RetryRestore" : "Recovery_RetryMigration");
        DisabledNote = state == RecoveryState.NewerSchema ? format.T(snapshot.Mode == UiMode.Live ? "RecoveryLive_NewerSchemaNote" : "Recovery_NewerSchemaNote") : string.Empty;
    }

    private bool CanRetry() => RetryAvailable && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private async Task RetryAsync()
    {
        var format = Format;
        if (Snapshot.System.Recovery == RecoveryState.RestoreFailed)
        {
            await RestoreAsync(CheckpointText);
            return;
        }
        IsBusy = true;
        Message = string.Empty;
        ProgressLabel = format.T("Recovery_Retrying");
        RetryLabel = format.T("Recovery_RetryingButton");
        Context.Announcer.Announce(ProgressLabel);
        try
        {
            var result = await recovery.RetryAsync(new InlineProgress(value => Progress = value), CancellationToken.None);
            if (result.Status == CommandStatus.Succeeded)
                Context.Announcer.Announce(format.T("Recovery_Recovered"));
            else
            {
                Message = format.T(result.Failure?.MessageKey ?? "Recovery_RetryFailed");
                MessageIsCritical = true;
                Context.Announcer.Announce(format.T("Announce_RetryFailed"));
            }
        }
        finally
        {
            IsBusy = false;
            Progress = 0;
            OnSnapshot(Snapshot);
        }
    }

    private bool CanToggleCheckpoints() => !IsBusy && (Snapshot.Mode != UiMode.Live || Snapshot.System.Recovery != RecoveryState.NewerSchema);

    [RelayCommand(CanExecute = nameof(CanToggleCheckpoints))]
    private async Task ToggleCheckpointsAsync()
    {
        ShowCheckpoints = !ShowCheckpoints;
        if (!ShowCheckpoints || Checkpoints.Count > 0)
            return;
        var format = Format;
        foreach (var checkpoint in await recovery.ListCheckpointsAsync(CancellationToken.None))
            Checkpoints.Add(new(checkpoint, format.F("Recovery_CheckpointLabel", format.T(checkpoint.KindKey), format.DateTime(checkpoint.CreatedAt)),
                Snapshot.Mode == UiMode.Live ? format.F("RecoveryLive_CheckpointMeta", checkpoint.SchemaVersion) :
                format.F("Recovery_CheckpointMeta", checkpoint.SchemaVersion, checkpoint.Accounts, format.Count(checkpoint.HistoryRows))));
    }

    [RelayCommand]
    private Task RestoreCheckpointAsync(CheckpointViewModel? checkpoint) => checkpoint is null ? Task.CompletedTask : RestoreAsync(checkpoint.Checkpoint.Id);

    private async Task RestoreAsync(string checkpointId)
    {
        var format = Format;
        var outcome = await Context.Dialogs.ConfirmAsync(new(format.T("Restore_Title"), format.F(Snapshot.Mode == UiMode.Live ? "RestoreLive_Body" : "Restore_Body", checkpointId), format.T("Restore_Confirm"),
            Destructive: true, BusyLabel: format.T("Restore_Busy"),
            ConfirmAction: async token =>
            {
                IsBusy = true;
                Message = string.Empty;
                ProgressLabel = format.T("Recovery_Restoring");
                try
                {
                    var result = await recovery.RestoreCheckpointAsync(checkpointId, new InlineProgress(value => Progress = value), token);
                    if (result.Status != CommandStatus.Succeeded)
                    {
                        Message = format.T(result.Failure?.MessageKey ?? "Recovery_RestoreFailed");
                        MessageIsCritical = true;
                    }
                    return null;
                }
                finally
                {
                    IsBusy = false;
                    Progress = 0;
                }
            }));
        if (outcome != ConfirmOutcome.Confirmed)
            Context.Announcer.Announce(format.T("Announce_Cancelled"));
        else if (Snapshot.System.Recovery == RecoveryState.None)
        {
            Context.Navigation.Navigate(new(PageKey.Overview));
            Context.Announcer.Announce(format.T("Announce_Restored"));
        }
        else
            Context.Announcer.Announce(format.T("Announce_RestoreFailed"));
    }

    [RelayCommand]
    private async Task ToggleDiagnosticsAsync() =>
        DiagnosticsText = DiagnosticsText.Length > 0 ? string.Empty : await recovery.PreviewDiagnosticsAsync(CancellationToken.None);

    [RelayCommand]
    private async Task PreviewDataFolderAsync()
    {
        if (Snapshot.Mode == UiMode.Live)
        {
            var result = await recovery.OpenDataFolderAsync(CancellationToken.None);
            Message = result.Status == CommandStatus.Succeeded ? string.Empty : Format.T("Dialog_OperationFailed");
            MessageIsCritical = result.Status != CommandStatus.Succeeded;
            return;
        }
        Message = Format.F("Recovery_FolderMessage", recovery.Details.DataFolderPreview);
        MessageIsCritical = false;
    }

    public bool CanExportDiagnostics => Snapshot.Mode == UiMode.Live;

    [RelayCommand]
    private async Task ExportDiagnosticsAsync()
    {
        var result = await recovery.ExportDiagnosticsAsync(CancellationToken.None);
        Message = Format.T(result.Status == CommandStatus.Succeeded ? "RecoveryLive_Exported" : "Dialog_OperationFailed");
        MessageIsCritical = result.Status != CommandStatus.Succeeded;
    }

    private sealed class InlineProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
