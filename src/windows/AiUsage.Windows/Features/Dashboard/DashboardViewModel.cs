using AiUsage.Core.Dashboard;
using AiUsage.Core.Providers.Codex;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Dashboard;

/// <summary>
/// Dashboard state over the verified Codex path. Presentation only: every provider request goes
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

    /// <summary>A null <paramref name="workflow"/> means the provider could not be composed; its commands stay disabled.</summary>
    internal DashboardViewModel(DashboardWorkflow? workflow, Action<Uri> openBrowser, Func<Task> exitAsync,
        Func<string, string> resource, Func<Action, Task> dispatch, Action? showWindow = null)
    {
        this.workflow = workflow;
        this.resource = resource;
        this.dispatch = dispatch;
        StatusText = resource("EmptyState/Text");
        this.openBrowser = openBrowser;
        ExitCommand = new AsyncRelayCommand(exitAsync);
        ShowCommand = new RelayCommand(showWindow ?? (() => { }));
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !stopping && workflow is not null && !Busy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !stopping && !Busy && Connected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => !stopping && !Busy && Connected);
    }

    /// <summary>Brings the main window forward from the tray.</summary>
    public IRelayCommand ShowCommand { get; }

    public IAsyncRelayCommand ExitCommand { get; }
    public IAsyncRelayCommand ConnectCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand DisconnectCommand { get; }

    [ObservableProperty] public partial bool Busy { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string? PlanText { get; private set; }
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

    private Task ConnectAsync() => RunAsync(token => workflow!.ConnectAsync(openBrowser, token), CancellationToken.None);
    private Task RefreshAsync() => RunAsync(workflow!.RefreshAsync, CancellationToken.None);
    private Task DisconnectAsync() => RunAsync(workflow!.DisconnectAsync, CancellationToken.None);

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
    private async Task RunAsync(Func<CancellationToken, Task<CodexSessionState>> operation, CancellationToken cancellationToken)
    {
        await dispatch(() =>
        {
            Busy = true;
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
            await dispatch(() => Apply(workflow?.State ?? CodexSessionState.NotConnected));
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
    private void Apply(CodexSessionState state)
    {
        Windows = state.Quota is { } quota
            ? [.. quota.Groups.SelectMany(group => group.Windows.Select(window => new QuotaWindowItem(group, window, resource)))]
            : [];
        PlanText = state.Quota?.PlanType is { Length: > 0 } plan ? resource("PlanLabel/Text") + " " + plan : null;
        StatusText = state.Status switch
        {
            CodexSessionStatus.NotConnected => resource("EmptyState/Text"),
            CodexSessionStatus.Working => resource("WorkingState/Text"),
            CodexSessionStatus.ReauthenticationRequired => resource("ReauthenticationRequired/Text"),
            CodexSessionStatus.QuotaUnavailable => resource("QuotaUnavailable/Text"),
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
        OnPropertyChanged(nameof(Connected));
    }
}
