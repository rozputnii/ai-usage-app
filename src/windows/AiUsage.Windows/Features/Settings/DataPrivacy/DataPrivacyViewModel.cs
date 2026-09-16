using System.Collections.ObjectModel;
using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Settings.DataPrivacy;

public enum ExportStage { Idle, Preparing, Ready }

public enum ReplaceStage { Idle, Validating, Invalid, Valid, Processing, Done }

internal sealed record AccountOption(string Id, string Label);

/// <summary>
/// S10 Data and privacy: history collection and retention, export preview, replace-from-bundle with separate checking
/// and processing stages, settings reset vs factory reset, and per-account data deletion (D-149–D-153).
/// </summary>
internal sealed partial class DataPrivacyViewModel : SnapshotViewModel
{
    private readonly IPreferenceStore preferences;
    private readonly IDataManagementService data;
    private CancellationTokenSource? exportOperation;
    private bool applying;

    public DataPrivacyViewModel(PresentationContext context, IPreferenceStore preferences, IDataManagementService data) : base(context)
    {
        this.preferences = preferences;
        this.data = data;
        var format = context.Format;
        RetentionLabels = [format.T("Retention_Days30"), format.T("Retention_Days90"), format.T("Retention_Year1"), format.T("Retention_KeepEverything")];
        Initialize();
        _ = LoadCandidatesAsync();
    }

    public IReadOnlyList<string> RetentionLabels { get; }
    public ObservableCollection<string> ExportItems { get; } = [];
    public ObservableCollection<ReplaceCandidate> ReplaceCandidates { get; } = [];
    public ObservableCollection<AccountOption> DeleteTargets { get; } = [];

    [ObservableProperty] public partial bool HistoryEnabled { get; set; }
    [ObservableProperty] public partial int RetentionIndex { get; set; }
    [NotifyPropertyChangedFor(nameof(IsExportIdle), nameof(IsExportPreparing), nameof(IsExportReady))]
    [ObservableProperty] public partial ExportStage ExportStage { get; private set; }
    [ObservableProperty] public partial double ExportProgress { get; private set; }
    [ObservableProperty] public partial string ExportTitle { get; private set; } = string.Empty;
    [ObservableProperty] public partial string SaveDisabledReason { get; private set; } = string.Empty;
    [NotifyPropertyChangedFor(nameof(IsReplaceIdle), nameof(IsReplaceValidating), nameof(IsReplaceInvalid), nameof(IsReplaceValid), nameof(IsReplaceProcessing), nameof(IsReplaceDone))]
    [ObservableProperty] public partial ReplaceStage ReplaceStage { get; private set; }
    [ObservableProperty] public partial ReplaceCandidate? SelectedCandidate { get; set; }
    [ObservableProperty] public partial string ReplaceMessage { get; private set; } = string.Empty;
    [ObservableProperty] public partial AccountOption? SelectedDeleteTarget { get; set; }
    [ObservableProperty] public partial bool HasAccounts { get; private set; }

    public bool IsExportIdle => ExportStage == ExportStage.Idle;
    public bool IsExportPreparing => ExportStage == ExportStage.Preparing;
    public bool IsExportReady => ExportStage == ExportStage.Ready;
    public bool IsReplaceIdle => ReplaceStage == ReplaceStage.Idle;
    public bool IsReplaceValidating => ReplaceStage == ReplaceStage.Validating;
    public bool IsReplaceInvalid => ReplaceStage == ReplaceStage.Invalid;
    public bool IsReplaceValid => ReplaceStage == ReplaceStage.Valid;
    public bool IsReplaceProcessing => ReplaceStage == ReplaceStage.Processing;
    public bool IsReplaceDone => ReplaceStage == ReplaceStage.Done;

    partial void OnHistoryEnabledChanged(bool value)
    {
        if (!applying)
            _ = Set(new(PreferenceKey.HistoryEnabled, value), Format.T(value ? "Announce_HistoryOn" : "Announce_HistoryOff"));
    }

    partial void OnRetentionIndexChanged(int value)
    {
        if (!applying && value >= 0)
            _ = Set(new(PreferenceKey.Retention, (HistoryRetention)value), null);
    }

    partial void OnSelectedCandidateChanged(ReplaceCandidate? value)
    {
        if (!applying && ReplaceStage is not (ReplaceStage.Validating or ReplaceStage.Processing))
            ReplaceStage = ReplaceStage.Idle;
    }

    private async Task Set(PreferenceChange change, string? announcement)
    {
        var result = await preferences.SetPreferenceAsync(change, CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded && announcement is not null)
            Context.Announcer.Announce(announcement);
    }

    private async Task LoadCandidatesAsync()
    {
        foreach (var candidate in await data.ListReplaceCandidatesAsync(CancellationToken.None))
            ReplaceCandidates.Add(candidate);
        applying = true;
        SelectedCandidate = ReplaceCandidates.FirstOrDefault();
        applying = false;
    }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        applying = true;
        try
        {
            HistoryEnabled = snapshot.Preferences.HistoryEnabled;
            RetentionIndex = (int)snapshot.Preferences.Retention;
            var accounts = QuotaRules.Ordered(snapshot).Select(a => new AccountOption(a.Id, a.Label)).ToArray();
            var selected = SelectedDeleteTarget?.Id;
            CollectionSync.Sync(DeleteTargets, accounts, a => a.Id + "|" + a.Label, a => a.Id + "|" + a.Label, a => a, (_, _) => { });
            SelectedDeleteTarget = DeleteTargets.FirstOrDefault(a => a.Id == selected) ?? DeleteTargets.FirstOrDefault();
            HasAccounts = accounts.Length > 0;
        }
        finally { applying = false; }
    }

    [RelayCommand]
    private async Task PrepareExportAsync()
    {
        exportOperation?.Cancel();
        using var cancellation = exportOperation = new CancellationTokenSource();
        ExportStage = ExportStage.Preparing;
        ExportProgress = 0;
        Context.Announcer.Announce(Format.T("Announce_ExportPreparing"));
        try
        {
            var preview = await data.PreviewExportAsync(new InlineProgress(value => ExportProgress = value), cancellation.Token);
            var format = Format;
            ExportTitle = format.F("Export_PreviewTitle", preview.FileName);
            ExportItems.Clear();
            ExportItems.Add(format.T("Export_Accounts"));
            if (preview.IncludesPreferences)
                ExportItems.Add(format.T("Export_Preferences"));
            ExportItems.Add(format.F("Export_History", format.Count(preview.HistoryRows), preview.AccountCount));
            ExportItems.Add(format.T("Export_Excluded"));
            SaveDisabledReason = preview.CanSave ? string.Empty : format.T("Export_SaveDisabled");
            ExportStage = ExportStage.Ready;
            Context.Announcer.Announce(format.T("Announce_ExportReady"));
        }
        catch (OperationCanceledException)
        {
            ExportStage = ExportStage.Idle;
            Context.Announcer.Announce(Format.T("Announce_ExportCancelled"));
        }
        finally
        {
            if (exportOperation == cancellation)
                exportOperation = null;
        }
    }

    [RelayCommand]
    private void CancelExport() => exportOperation?.Cancel();

    [RelayCommand]
    private void CloseExport() => ExportStage = ExportStage.Idle;

    [RelayCommand]
    private async Task ValidateReplaceAsync()
    {
        if (SelectedCandidate is not { } candidate)
            return;
        ReplaceStage = ReplaceStage.Validating;
        Context.Announcer.Announce(Format.T("Announce_Validating"));
        try
        {
            var result = await data.ValidateReplaceImportAsync(candidate.Id, CancellationToken.None);
            var format = Format;
            if (result.Valid)
            {
                ReplaceMessage = format.F("Replace_Valid", result.SchemaVersion, result.Accounts, format.Count(result.HistoryRows ?? 0),
                    result.ExportedAt is { } exported ? format.DateTime(exported) : format.T("Value_Unknown"));
                ReplaceStage = ReplaceStage.Valid;
            }
            else
            {
                ReplaceMessage = format.T(result.FailureKey ?? "Replace_InvalidChecksum");
                ReplaceStage = ReplaceStage.Invalid;
            }
            Context.Announcer.Announce(ReplaceMessage);
        }
        catch (OperationCanceledException)
        {
            ReplaceStage = ReplaceStage.Idle;
        }
    }

    [RelayCommand]
    private async Task ConfirmReplaceAsync()
    {
        if (SelectedCandidate is not { } candidate || ReplaceStage != ReplaceStage.Valid)
            return;
        var format = Format;
        var outcome = await Context.Dialogs.ConfirmAsync(new(format.T("Replace_ConfirmTitle"), format.T("Replace_ConfirmBody"), format.T("Replace_Confirm"),
            Destructive: true, BusyLabel: format.T("Replace_Busy"),
            ConfirmAction: async token =>
            {
                ReplaceStage = ReplaceStage.Processing;
                var result = await data.ApplyReplaceImportAsync(candidate.Id, token);
                if (result.Status == CommandStatus.Succeeded)
                    return null;
                ReplaceStage = ReplaceStage.Valid;
                return format.T("Dialog_OperationFailed");
            }));
        if (outcome == ConfirmOutcome.Confirmed)
        {
            ReplaceStage = ReplaceStage.Done;
            Context.Announcer.Announce(format.T("Announce_ReplaceDone"));
        }
        else
        {
            ReplaceStage = ReplaceStage.Valid;
            Context.Announcer.Announce(format.T("Announce_Cancelled"));
        }
    }

    [RelayCommand]
    private void ResetReplace() => ReplaceStage = ReplaceStage.Idle;

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        var format = Format;
        var outcome = await Context.Dialogs.ConfirmAsync(new(format.T("ResetSettings_Title"), format.T("ResetSettings_Body"), format.T("ResetSettings_Confirm"),
            ConfirmAction: async token => (await preferences.ResetSettingsAsync(token)).Status == CommandStatus.Succeeded ? null : format.T("Dialog_OperationFailed")));
        Context.Announcer.Announce(format.T(outcome == ConfirmOutcome.Confirmed ? "Announce_SettingsReset" : "Announce_Cancelled"));
    }

    [RelayCommand]
    private async Task FactoryResetAsync()
    {
        var format = Format;
        var outcome = await Context.Dialogs.ConfirmAsync(new(format.T("Factory_Title"), format.T("Factory_Body"), format.T("Factory_Confirm"),
            Destructive: true, TypedConfirmation: "RESET", BusyLabel: format.T("Factory_Busy"),
            ConfirmAction: async token => (await data.FactoryResetAsync(token)).Status == CommandStatus.Succeeded ? null : format.T("Dialog_OperationFailed")));
        if (outcome == ConfirmOutcome.Confirmed)
        {
            Context.Navigation.Navigate(new(PageKey.Overview));
            Context.Announcer.Announce(format.T("Announce_FactoryReset"));
        }
        else
            Context.Announcer.Announce(format.T("Announce_Cancelled"));
    }

    [RelayCommand]
    private async Task DeleteAccountDataAsync()
    {
        if (SelectedDeleteTarget is not { } target)
            return;
        var format = Format;
        var outcome = await Context.Dialogs.ConfirmAsync(new(format.F("DeleteData_Title", target.Label), format.T("DeleteData_Body"), format.T("DeleteData_Confirm"),
            Destructive: true, BusyLabel: format.T("DeleteData_Busy"),
            ConfirmAction: async token => (await data.DeleteAccountDataAsync(target.Id, token)).Status == CommandStatus.Succeeded ? null : format.T("Dialog_OperationFailed")));
        Context.Announcer.Announce(outcome == ConfirmOutcome.Confirmed ? format.F("Announce_DataDeleted", target.Label) : format.T("Announce_Cancelled"));
    }

    private sealed class InlineProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
