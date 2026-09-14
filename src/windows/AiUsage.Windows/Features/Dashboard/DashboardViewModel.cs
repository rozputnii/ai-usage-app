using AiUsage.Infrastructure.Providers.Codex;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Dashboard;

/// <summary>
/// Dashboard state over the verified Codex path. Presentation only: every provider request goes
/// through <see cref="CodexSession"/>, and an unknown value stays unknown instead of rendering as zero.
/// </summary>
internal sealed partial class DashboardViewModel : ObservableObject
{
    private readonly CodexSession? session;
    private readonly Func<Uri, bool> openBrowser;

    /// <summary>A null <paramref name="session"/> means the provider could not be composed; its commands stay disabled.</summary>
    internal DashboardViewModel(CodexSession? session, Func<Uri, bool> openBrowser, Func<Task> exitAsync, Action? showWindow = null)
    {
        this.session = session;
        this.openBrowser = openBrowser;
        ExitCommand = new AsyncRelayCommand(exitAsync);
        ShowCommand = new RelayCommand(showWindow ?? (() => { }));
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => session is not null && !Busy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !Busy && Connected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => !Busy && Connected);
    }

    /// <summary>Brings the main window forward from the tray.</summary>
    public IRelayCommand ShowCommand { get; }

    public IAsyncRelayCommand ExitCommand { get; }
    public IAsyncRelayCommand ConnectCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand DisconnectCommand { get; }

    [ObservableProperty] public partial bool Busy { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = App.Resource("EmptyState/Text");
    [ObservableProperty] public partial string? PlanText { get; private set; }
    [ObservableProperty] public partial IReadOnlyList<QuotaWindowItem> Windows { get; private set; } = [];

    public bool Connected => session?.HasStoredGrant == true;

    /// <summary>
    /// Shows the last cached reading immediately, then restores the stored account. A first run
    /// with no grant stays in the empty state and issues no provider request.
    /// </summary>
    internal async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (session is null || !Connected)
            return;
        Apply(session.ReadCachedState());
        await RunAsync(session.ResumeAsync, cancellationToken);
    }

    private Task ConnectAsync() => RunAsync(token => session!.ConnectAsync(url =>
    {
        if (!openBrowser(url))
            throw new CodexException(CodexFailureKind.BrowserCallbackUnavailable);
    }, token), CancellationToken.None);

    private Task RefreshAsync() => RunAsync(session!.RefreshAsync, CancellationToken.None);

    private Task DisconnectAsync() => RunAsync(session!.DisconnectAsync, CancellationToken.None);

    private async Task RunAsync(Func<CancellationToken, Task<CodexSessionState>> operation, CancellationToken cancellationToken)
    {
        Busy = true;
        StatusText = App.Resource("WorkingState/Text");
        UpdateCommands();
        try { Apply(await operation(cancellationToken)); }
        catch (CodexException error) { Apply(new CodexSessionState(CodexSessionStatus.QuotaUnavailable, Failure: error.Kind)); }
        catch (OperationCanceledException) { Apply(session?.State ?? CodexSessionState.NotConnected); }
        finally
        {
            Busy = false;
            UpdateCommands();
        }
    }

    private void Apply(CodexSessionState state)
    {
        Windows = state.Quota is { } quota
            ? [.. quota.Groups.SelectMany(group => group.Windows.Select(window => new QuotaWindowItem(group, window)))]
            : [];
        PlanText = state.Quota?.PlanType is { Length: > 0 } plan ? App.Resource("PlanLabel/Text") + " " + plan : null;
        StatusText = state.Status switch
        {
            CodexSessionStatus.NotConnected => App.Resource("EmptyState/Text"),
            CodexSessionStatus.Working => App.Resource("WorkingState/Text"),
            CodexSessionStatus.ReauthenticationRequired => App.Resource("ReauthenticationRequired/Text"),
            CodexSessionStatus.QuotaUnavailable => App.Resource("QuotaUnavailable/Text"),
            _ => Windows.Count > 0 ? App.Resource("QuotaShown/Text") : App.Resource("QuotaEmpty/Text")
        };
        // Cached values must never read as a current measurement.
        if (state.FromCache && state.RetrievedAt is { } retrievedAt)
            StatusText += " " + string.Format(System.Globalization.CultureInfo.CurrentCulture,
                App.Resource("CachedNotice/Text"), retrievedAt.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture));
    }

    private void UpdateCommands()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(Connected));
    }
}
