using System.Collections.ObjectModel;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.SystemStatusPage;

internal sealed partial class ProviderStatusViewModel(string id) : ObservableObject
{
    public string Id { get; } = id;
    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    [ObservableProperty] public partial string ProviderName { get; set; } = string.Empty;
    [ObservableProperty] public partial string StatusText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsCritical { get; set; }
}

internal sealed record HealthStepItem(string Name, string Result, bool IsWarning);

public enum HealthStage { Idle, Checking, Done }

/// <summary>S09 System Status: build/schema, refresh, storage, update, providers, a read-only health check and sanitized previews.</summary>
internal sealed partial class SystemStatusViewModel : SnapshotViewModel
{
    private readonly IDiagnosticsService diagnostics;
    private readonly Func<Task> exit;
    private CancellationTokenSource? health;

    public SystemStatusViewModel(PresentationContext context, IDiagnosticsService diagnostics, Func<Task> exit) : base(context)
    {
        this.diagnostics = diagnostics;
        this.exit = exit;
        Initialize();
    }

    public ObservableCollection<ProviderStatusViewModel> ProviderStatuses { get; } = [];
    public ObservableCollection<HealthStepItem> HealthSteps { get; } = [];

    [ObservableProperty] public partial string BuildLine { get; private set; } = string.Empty;
    [ObservableProperty] public partial string RefreshText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string StorageText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string UpdateText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool UpdateIsFailure { get; private set; }
    [NotifyPropertyChangedFor(nameof(IsHealthIdle), nameof(IsHealthChecking), nameof(IsHealthDone))]
    [ObservableProperty] public partial HealthStage HealthStage { get; private set; }
    [ObservableProperty] public partial double HealthProgress { get; private set; }
    [ObservableProperty] public partial string HealthCheckingText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string HealthResult { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool HealthResultWarning { get; private set; }
    [ObservableProperty] public partial string DiagnosticsText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string LogText { get; private set; } = string.Empty;

    public bool IsHealthIdle => HealthStage == HealthStage.Idle;
    public bool IsHealthChecking => HealthStage == HealthStage.Checking;
    public bool IsHealthDone => HealthStage == HealthStage.Done;
    public bool HasHealthResult => HealthResult.Length > 0;
    public bool HasDiagnostics => DiagnosticsText.Length > 0;
    public bool HasLog => LogText.Length > 0;
    partial void OnHealthResultChanged(string value) => OnPropertyChanged(nameof(HasHealthResult));
    partial void OnDiagnosticsTextChanged(string value) => OnPropertyChanged(nameof(HasDiagnostics));
    partial void OnLogTextChanged(string value) => OnPropertyChanged(nameof(HasLog));

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var system = snapshot.System;
        BuildLine = format.F("Status_BuildLine", system.BuildLabel, system.SchemaLabel);
        var failures = snapshot.Accounts.Count(a => a.Failure is not null);
        RefreshText = format.F(failures == 1 ? "Status_RefreshOne" : "Status_RefreshMany", format.Time(Context.Clock.UtcNow), failures,
            format.T(system.RefreshPolicy == RefreshPolicy.Reduced ? "Status_ScheduleReduced" : "Status_ScheduleNormal"));
        StorageText = format.F("Status_Storage", system.SchemaLabel, format.Count(system.HistoryRows), system.CheckpointCount,
            format.T("Retention_" + snapshot.Preferences.Retention));
        UpdateText = system.Update switch
        {
            UpdateState.Available => format.F("Update_Available", system.UpdateVersion ?? string.Empty),
            UpdateState.Ready => format.F("Update_Ready", system.UpdateVersion ?? string.Empty),
            UpdateState.Downloading => format.F("Update_Downloading", system.UpdateVersion ?? string.Empty, format.Percent(system.UpdateProgress ?? 0)),
            UpdateState.Failed => format.T("Update_Failed"),
            UpdateState.Checking => format.T("Update_Checking"),
            UpdateState.WaitingForStable => format.T("Update_Waiting"),
            UpdateState.Unsupported => format.T("Update_Unsupported"),
            _ => format.T("Update_Current"),
        };
        UpdateIsFailure = system.Update == UpdateState.Failed;
        CollectionSync.Sync(ProviderStatuses, QuotaRules.Ordered(snapshot), a => a.Id, vm => vm.Id, a => new ProviderStatusViewModel(a.Id), (vm, a) =>
        {
            vm.Label = a.Label;
            vm.ProviderName = Context.Providers.Get(a.ProviderId).PresentationName;
            vm.StatusText = string.Join(" · ", new[]
            {
                format.T("Connection_" + a.Connection),
                a.Operation == AccountOperation.Idle ? null : format.T("Operation_" + a.Operation),
                format.T("Freshness_" + a.Freshness),
            }.Where(s => s is not null));
            vm.IsCritical = a.Connection == ConnectionState.ReauthRequired;
        });
    }

    [RelayCommand]
    private async Task RunHealthCheckAsync()
    {
        health?.Cancel();
        using var cancellation = health = new CancellationTokenSource();
        HealthSteps.Clear();
        HealthResult = string.Empty;
        HealthProgress = 0;
        HealthStage = HealthStage.Checking;
        var format = Format;
        HealthCheckingText = format.F("Health_Checking", 0, 5);
        Context.Announcer.Announce(format.T("Announce_HealthStarted"));
        var warnings = 0;
        try
        {
            await foreach (var step in diagnostics.RunHealthCheckAsync(cancellation.Token))
            {
                var warning = step.Result != HealthCheckResult.Ok;
                if (warning)
                    warnings++;
                HealthSteps.Add(new(format.T(step.NameKey), step.FindingKey is null ? format.T("Health_Ok") : format.T(step.FindingKey), warning));
                HealthProgress = (double)step.Index / step.Total;
                HealthCheckingText = format.F("Health_Checking", step.Index, step.Total);
            }
            HealthResult = warnings > 0 ? format.F("Health_ResultWarning", warnings) : format.T("Health_ResultOk");
            HealthResultWarning = warnings > 0;
            Context.Announcer.Announce(HealthResult);
        }
        catch (OperationCanceledException)
        {
            HealthResult = format.T("Health_Cancelled");
            HealthResultWarning = false;
            Context.Announcer.Announce(HealthResult);
        }
        finally
        {
            if (health == cancellation)
                health = null;
            HealthStage = HealthStage.Done;
        }
    }

    [RelayCommand]
    private void CancelHealthCheck() => health?.Cancel();

    [RelayCommand]
    private async Task ToggleDiagnosticsAsync() =>
        DiagnosticsText = DiagnosticsText.Length > 0 ? string.Empty : await diagnostics.PreviewDiagnosticsAsync(CancellationToken.None);

    [RelayCommand]
    private async Task ToggleLogAsync() =>
        LogText = LogText.Length > 0 ? string.Empty : await diagnostics.PreviewLogsAsync(CancellationToken.None);

    [RelayCommand]
    private void OpenUpdates() => Context.Navigation.Navigate(new(PageKey.Settings, Tab: SettingsTab.Updates));

    [RelayCommand]
    private void OpenDataPrivacy() => Context.Navigation.Navigate(new(PageKey.Settings, Tab: SettingsTab.DataPrivacy));

    [RelayCommand]
    private Task ExitAsync() => exit();
}
