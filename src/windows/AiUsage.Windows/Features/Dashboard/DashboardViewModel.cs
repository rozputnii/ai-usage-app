using AiUsage.Core.Dashboard;
using AiUsage.Core.Usage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Dashboard;

/// <summary>
/// One provider's dashboard state. Presentation only: every provider request goes
/// through <see cref="DashboardWorkflow"/>, and an unknown value stays unknown instead of rendering as zero.
/// </summary>
internal sealed partial class DashboardViewModel : ObservableObject
{
    private readonly DashboardWorkflow? workflow;
    private readonly Action<Uri> openBrowser;
    private readonly Func<string, string> resource;
    private readonly Func<Action, Task> dispatch;
    private Task? loadTask;
    private bool stopping;
    private bool connecting;
    private bool refreshing;
    private readonly bool supportsManualCode;

    /// <summary>A null <paramref name="workflow"/> means the provider could not be composed; its commands stay disabled.</summary>
    internal DashboardViewModel(DashboardWorkflow? workflow, Action<Uri> openBrowser,
        Func<string, string> resource, Func<Action, Task> dispatch, string providerName = "Codex", bool supportsManualCode = false)
    {
        this.workflow = workflow;
        this.resource = resource;
        this.dispatch = dispatch;
        this.supportsManualCode = supportsManualCode;
        ProviderName = providerName;
        StatusText = resource("EmptyState/Text");
        this.openBrowser = openBrowser;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !stopping && workflow is not null && !Busy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !stopping && !Busy && Connected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => !stopping && !Busy && Connected);
        CancelCommand = new RelayCommand(() => { ConnectCommand.Cancel(); RefreshCommand.Cancel(); }, () => Busy && !stopping && (connecting || refreshing));
    }

    public string ProviderName { get; }
    public string ConnectText => string.Format(System.Globalization.CultureInfo.CurrentCulture, resource("ConnectProviderFormat/Text"), ProviderName);
    public string NoticeText => supportsManualCode ? resource("ClaudeUnsupported/Text") : string.Empty;
    public bool ManualEntryVisible => supportsManualCode && connecting && Busy;
    public IAsyncRelayCommand ConnectCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand DisconnectCommand { get; }
    public IRelayCommand CancelCommand { get; }

    [ObservableProperty] public partial bool Busy { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string? PlanText { get; private set; }
    [ObservableProperty] public partial string FailureText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ExtraUsageText { get; private set; } = string.Empty;
    [ObservableProperty] public partial IReadOnlyList<QuotaWindowItem> Windows { get; private set; } = [];

    public bool Connected => workflow?.Connected == true;

    /// <summary>
    /// Shows the last cached reading immediately, then restores the stored account. A first run
    /// with no grant stays in the empty state and issues no provider request.
    /// </summary>
    internal Task LoadAsync(CancellationToken cancellationToken = default) => loadTask ??= LoadCoreAsync(cancellationToken);

    private async Task LoadCoreAsync(CancellationToken cancellationToken)
    {
        if (workflow is null)
            return;
        await RunAsync(token => workflow.LoadAsync(state => dispatch(() => Apply(state)), token), cancellationToken);
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        connecting = true;
        try { await RunAsync(token => workflow!.ConnectAsync(openBrowser, token), cancellationToken); }
        finally { connecting = false; OnPropertyChanged(nameof(ManualEntryVisible)); }
    }
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        refreshing = true;
        try { await RunAsync(workflow!.RefreshAsync, cancellationToken); }
        finally { refreshing = false; }
    }
    private Task DisconnectAsync() => RunAsync(workflow!.DisconnectAsync, CancellationToken.None);

    internal void SubmitCode(string code)
    {
        if (ManualEntryVisible && workflow?.TrySubmitCode(code) != true)
            FailureText = resource("ManualCodeInvalid/Text");
    }

    /// <summary>Called on the UI thread by the desktop owner before disposing the Host.</summary>
    internal async Task StopAsync()
    {
        stopping = true;
        UpdateCommands();
        try
        {
            if (workflow is not null)
                await workflow.StopAsync();
        }
        finally
        {
            // Provider completion alone does not mean its presentation continuation has drained.
            await Task.WhenAll(new[] { loadTask, ConnectCommand.ExecutionTask, RefreshCommand.ExecutionTask,
                DisconnectCommand.ExecutionTask }.OfType<Task>());
        }
    }
    private async Task RunAsync(Func<CancellationToken, Task<ProviderSessionState>> operation, CancellationToken cancellationToken)
    {
        await dispatch(() =>
        {
            Busy = true;
            FailureText = string.Empty;
            StatusText = resource("WorkingState/Text");
            UpdateCommands();
        });
        try
        {
            var state = await operation(cancellationToken);
            await dispatch(() => Apply(state));
        }
        catch (OperationCanceledException)
        {
            await dispatch(() => Apply(workflow?.State ?? ProviderSessionState.NotConnected));
        }
        finally
        {
            await dispatch(() =>
            {
                Busy = false;
                UpdateCommands();
            });
        }
    }
    private void Apply(ProviderSessionState state)
    {
        Windows = state.Quota is { } quota
            ? [.. quota.Groups.SelectMany(group => group.Windows.Select(window => new QuotaWindowItem(group, window, resource)))]
            : [];
        PlanText = state.Quota?.PlanType is { Length: > 0 } plan ? resource("PlanLabel/Text") + " " + plan : null;
        ExtraUsageText = ClaudeExtraUsageText.Format(state.ExtraUsage, resource);
        FailureText = state.Failure is { } failure ? resource("Failure" + failure + "/Text") : string.Empty;
        StatusText = state.Status switch
        {
            ProviderSessionStatus.NotConnected => resource("EmptyState/Text"),
            ProviderSessionStatus.Working => resource("WorkingState/Text"),
            ProviderSessionStatus.ReauthenticationRequired => resource("ReauthenticationRequired/Text"),
            ProviderSessionStatus.RecoveryRequired => resource("RecoveryRequired/Text"),
            ProviderSessionStatus.QuotaUnavailable => resource("QuotaUnavailable/Text"),
            _ => Windows.Count > 0 ? resource("QuotaShown/Text") : resource("QuotaEmpty/Text")
        };
        // Cached values must never read as a current measurement.
        if (state.FromCache && state.RetrievedAt is { } retrievedAt)
            StatusText += " " + string.Format(System.Globalization.CultureInfo.CurrentCulture,
                resource("CachedNotice/Text"), retrievedAt.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture));
    }

    private void UpdateCommands()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(Connected));
        OnPropertyChanged(nameof(ManualEntryVisible));
    }
}
