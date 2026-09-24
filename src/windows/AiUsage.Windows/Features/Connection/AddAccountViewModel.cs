using System.Collections.ObjectModel;
using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Connection;

public enum ConnectStep { Idle, Connecting, Waiting, Code, Verifying }

public enum NoteTone { Neutral, Critical }

internal sealed partial class ProviderOptionViewModel(ProviderDescriptor descriptor) : ObservableObject
{
    public ProviderDescriptor Descriptor { get; } = descriptor;
    public string Name => Descriptor.Name;
    public string Glyph => Descriptor.Glyph;
    public string ProviderId => Descriptor.ProviderId;
    public bool IsPlanned => Descriptor.Origin == CapabilityOrigin.Planned;
    public string AutomationId => "AddProvider_" + ProviderId;
    [ObservableProperty] public partial string Availability { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsEnabled { get; set; } = true;
    /// <summary>The existing account a click signs in again, when the single live slot already holds one.</summary>
    [ObservableProperty] public partial string? ReconnectAccountId { get; set; }

    public bool HasAvailability => Availability.Length > 0;
    partial void OnAvailabilityChanged(string value) => OnPropertyChanged(nameof(HasAvailability));
}

internal sealed record SimulatorOption(DemoConnectOutcome Outcome, string Label);

/// <summary>
/// S03 Add account without a dialog: the header menu lists providers and one click starts browser sign-in. Progress,
/// the device code, the optional manual code and failures appear in a strip in the main window; success adds the
/// account without a confirmation step. Cancellation is neutral. The manual code is transient and cleared on submit.
/// </summary>
internal sealed partial class AddAccountViewModel : SnapshotViewModel, IAccountConnector
{
    private readonly IConnectionFlow flow;
    private readonly DemoScenarioController? demo;
    private CancellationTokenSource? operation;
    private (ProviderOptionViewModel Option, string? ReconnectAccountId)? last;

    public AddAccountViewModel(PresentationContext context, IConnectionFlow flow, CliImportViewModel cli, DemoScenarioController? demo = null) : base(context)
    {
        this.flow = flow;
        this.demo = demo;
        Cli = cli;
        var format = context.Format;
        foreach (var provider in flow.Providers)
            ProviderOptions.Add(new(provider));
        Simulators =
        [
            new(DemoConnectOutcome.Approve, format.T("Sim_Approve")),
            new(DemoConnectOutcome.ApproveWithoutQuota, format.T("Sim_ApproveNoQuota")),
            new(DemoConnectOutcome.Deny, format.T("Sim_Deny")),
            new(DemoConnectOutcome.Expire, format.T("Sim_Expire")),
            new(DemoConnectOutcome.Duplicate, format.T("Sim_Duplicate")),
        ];
        if (demo is not null)
            demo.CliImportRequested += (_, _) => OpenCliImport();
        Initialize();
    }

    public CliImportViewModel Cli { get; }
    public ObservableCollection<ProviderOptionViewModel> ProviderOptions { get; } = [];
    public IReadOnlyList<SimulatorOption> Simulators { get; }
    public bool HasSimulator => demo is not null;

    [NotifyPropertyChangedFor(nameof(IsActive), nameof(IsConnecting), nameof(IsWaiting), nameof(IsCode), nameof(IsVerifying), nameof(IsBusy), nameof(ShowStrip), nameof(CanEnterCode))]
    [ObservableProperty] public partial ConnectStep Step { get; private set; }
    [NotifyPropertyChangedFor(nameof(SupportsManualCode), nameof(CanEnterCode))]
    [ObservableProperty] public partial ProviderOptionViewModel? Provider { get; private set; }
    [ObservableProperty] public partial ConnectionMethod Method { get; private set; }
    [ObservableProperty] public partial string? ReconnectAccountId { get; private set; }
    [NotifyPropertyChangedFor(nameof(HasNote), nameof(ShowStrip))]
    [ObservableProperty] public partial string Note { get; private set; } = string.Empty;
    [ObservableProperty] public partial NoteTone NoteSeverity { get; private set; }
    [ObservableProperty] public partial bool CanRetry { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(SubmitCodeCommand))]
    [ObservableProperty] public partial string Code { get; set; } = string.Empty;
    [NotifyPropertyChangedFor(nameof(HasCodeError))]
    [ObservableProperty] public partial string CodeError { get; private set; } = string.Empty;
    [ObservableProperty] public partial string StatusText { get; private set; } = string.Empty;
    [NotifyPropertyChangedFor(nameof(HasDeviceCode))]
    [ObservableProperty] public partial string DeviceUserCode { get; private set; } = string.Empty;
    [ObservableProperty] public partial string CodePrompt { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool CanImportFromCli { get; private set; }
    [NotifyPropertyChangedFor(nameof(ShowStrip))]
    [ObservableProperty] public partial bool IsCliOpen { get; private set; }

    public bool IsActive => Step != ConnectStep.Idle;
    public bool IsConnecting => Step == ConnectStep.Connecting;
    public bool IsWaiting => Step == ConnectStep.Waiting;
    public bool IsCode => Step == ConnectStep.Code;
    public bool IsVerifying => Step == ConnectStep.Verifying;
    public bool IsBusy => Step is ConnectStep.Connecting or ConnectStep.Waiting or ConnectStep.Verifying;
    public bool HasNote => Note.Length > 0;
    public bool HasCodeError => CodeError.Length > 0;
    public bool HasDeviceCode => DeviceUserCode.Length > 0;
    public bool ShowStrip => IsActive || HasNote;
    public bool SupportsManualCode => Provider?.Descriptor.Methods.Contains(ConnectionMethod.ManualCode) == true;
    /// <summary>"Enter a code instead" is offered while the browser sign-in waits.</summary>
    public bool CanEnterCode => SupportsManualCode && Step == ConnectStep.Waiting;

    partial void OnCodeChanged(string value)
    {
        if (CodeError.Length > 0)
            CodeError = string.Empty;
    }

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        CanImportFromCli = QuotaRules.IsAvailable(snapshot, nameof(UiCommandKind.DiscoverCli));
        if (!CanImportFromCli && IsCliOpen)
            CloseCliImport();
        foreach (var option in ProviderOptions)
        {
            var existing = flow.SingleAccountPerProvider ? snapshot.Accounts.FirstOrDefault(a => a.ProviderId == option.ProviderId) : null;
            var signingIn = IsActive && Provider == option;
            option.ReconnectAccountId = existing is { Connection: not ConnectionState.Connected } ? existing.Id : null;
            option.IsEnabled = !signingIn && existing is not ({ Connection: ConnectionState.Connected } or { Operation: not AccountOperation.Idle });
            option.Availability = signingIn ? format.T("Connect_OptionSigningIn")
                : existing?.Connection == ConnectionState.Connected ? format.T("Connect_OptionConnected")
                : existing is not null ? format.T("Connect_OptionSignInAgain")
                : option.IsPlanned ? format.T("Connect_Planned")
                : string.Empty;
        }
    }

    /// <summary>One click on a provider in the Add account menu: starts browser sign-in immediately.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ConnectAsync(ProviderOptionViewModel? option) =>
        option is null || !option.IsEnabled ? Task.CompletedTask : StartAsync(option, option.ReconnectAccountId);

    /// <summary>Row and detail Reconnect: the same inline sign-in for an existing account.</summary>
    public Task ReconnectAsync(string providerId, string accountId) =>
        ProviderOptions.FirstOrDefault(p => p.ProviderId == providerId) is { } option ? StartAsync(option, accountId) : Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task RetryAsync() => last is { } previous ? StartAsync(previous.Option, previous.ReconnectAccountId) : Task.CompletedTask;

    private async Task StartAsync(ProviderOptionViewModel option, string? reconnectAccountId)
    {
        // A second provider click replaces the running attempt; its cancellation is not reported as an outcome.
        Detach();
        last = (option, reconnectAccountId);
        Provider = option;
        ReconnectAccountId = reconnectAccountId;
        var methods = option.Descriptor.Methods;
        Method = methods.Count == 0 || methods.Contains(ConnectionMethod.BrowserSignIn) ? ConnectionMethod.BrowserSignIn : methods[0];
        ClearNote();
        Code = string.Empty;
        CodeError = string.Empty;
        CodePrompt = Format.F("Connect_CodePrompt", option.Name);
        if (Method == ConnectionMethod.ManualCode && !flow.ManualCodeUsesActiveConnection)
        {
            ShowCodeEntry();
            return;
        }
        await RunAsync(token => flow.ConnectAsync(Request(), token));
    }

    [RelayCommand]
    private void EnterCodeInstead()
    {
        if (!SupportsManualCode || !IsActive) return;
        if (!flow.ManualCodeUsesActiveConnection) Detach();
        ShowCodeEntry();
    }

    private void ShowCodeEntry()
    {
        Code = string.Empty;
        StatusText = CodePrompt;
        Step = ConnectStep.Code;
        RefreshOptions();
    }

    private bool CanSubmitCode() => Step == ConnectStep.Code;

    [RelayCommand(CanExecute = nameof(CanSubmitCode))]
    private async Task SubmitCodeAsync()
    {
        var code = Code.Trim();
        if (code.Length < 8)
        {
            CodeError = Format.T("Connect_CodeError");
            return;
        }
        // The code is used once and never stored: clear it before the request starts.
        Code = string.Empty;
        if (flow.ManualCodeUsesActiveConnection)
        {
            if (flow.TrySubmitCode(Request(), code)) SetStep(ConnectStep.Verifying);
            else CodeError = Format.T("Connect_CodeError");
            return;
        }
        await RunAsync(token => flow.SubmitCodeAsync(Request(), code, token));
    }

    [RelayCommand]
    private void Cancel()
    {
        if (operation is not null && !operation.IsCancellationRequested)
        {
            operation.Cancel();
            return;
        }
        Finish(new ConnectionStage(ConnectionStageKind.Cancelled));
    }

    [RelayCommand]
    private void DismissNote() => ClearNote();

    [RelayCommand]
    private void Simulate(SimulatorOption? option)
    {
        if (option is not null)
            demo?.ResolveConnection(option.Outcome);
    }

    [RelayCommand]
    private void OpenCliImport()
    {
        if (!CanImportFromCli)
            return;
        IsCliOpen = true;
        Cli.Open();
    }

    [RelayCommand]
    private void CloseCliImport() => IsCliOpen = false;

    /// <summary>Abandons the running stage stream without reporting its cancellation as a user-visible outcome.</summary>
    private void Detach()
    {
        DeviceUserCode = string.Empty;
        var running = operation;
        operation = null;
        running?.Cancel();
    }

    private ConnectRequest Request() => new(Provider!.ProviderId, Method, ReconnectAccountId);

    private void SetStep(ConnectStep step)
    {
        Step = step;
        StatusText = step switch
        {
            ConnectStep.Connecting => Format.F("Connect_Connecting", Provider?.Name ?? string.Empty),
            ConnectStep.Waiting when HasDeviceCode => Format.T("Connect_DeviceInstructions"),
            ConnectStep.Waiting => Format.F("Connect_WaitingBody", Provider?.Name ?? string.Empty),
            ConnectStep.Code => CodePrompt,
            ConnectStep.Verifying => Format.T("Connect_VerifyingText"),
            _ => string.Empty,
        };
        RefreshOptions();
    }

    private void RefreshOptions() => OnSnapshot(Context.Usage.Current);

    private async Task RunAsync(Func<CancellationToken, IAsyncEnumerable<ConnectionStage>> start)
    {
        operation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        operation = cancellation;
        try
        {
            await foreach (var stage in start(cancellation.Token).WithCancellation(cancellation.Token))
            {
                if (operation != cancellation)
                    return;
                switch (stage.Kind)
                {
                    case ConnectionStageKind.Connecting:
                        SetStep(ConnectStep.Connecting);
                        Context.Announcer.Announce(StatusText);
                        break;
                    case ConnectionStageKind.WaitingForAuthorization:
                        DeviceUserCode = stage.DeviceUserCode ?? string.Empty;
                        SetStep(flow.ManualCodeUsesActiveConnection && Method == ConnectionMethod.ManualCode ? ConnectStep.Code : ConnectStep.Waiting);
                        Context.Announcer.Announce(Format.T("Connect_WaitingTitle"));
                        break;
                    case ConnectionStageKind.Verifying:
                        SetStep(ConnectStep.Verifying);
                        break;
                    default:
                        Finish(stage);
                        return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (operation == cancellation)
                Finish(new ConnectionStage(ConnectionStageKind.Cancelled));
        }
        finally
        {
            if (operation == cancellation)
                operation = null;
        }
    }

    private void Finish(ConnectionStage stage)
    {
        DeviceUserCode = string.Empty;
        Code = string.Empty;
        var format = Format;
        var name = Provider?.Name ?? string.Empty;
        SetStep(ConnectStep.Idle);
        switch (stage.Kind)
        {
            case ConnectionStageKind.Denied:
                ShowNote(format.T("Connect_Denied"), NoteTone.Critical, retry: true);
                break;
            case ConnectionStageKind.Expired:
                ShowNote(format.T("Connect_Expired"), NoteTone.Critical, retry: true);
                break;
            case ConnectionStageKind.Cancelled:
                Context.Announcer.Announce(format.T("Connect_Cancelled"));
                break;
            case ConnectionStageKind.Failed:
                ShowNote(stage.Failure is { } failure ? format.T(failure.MessageKey) : format.T("Dialog_OperationFailed"), NoteTone.Critical, retry: true);
                break;
            case ConnectionStageKind.Duplicate:
                var existing = Context.Usage.Current.Accounts.FirstOrDefault(a => a.Id == stage.AccountId);
                ShowNote(existing is null ? format.T("Connect_DuplicateBodyGeneric") : format.F("Connect_DuplicateBody", existing.Label), NoteTone.Neutral, retry: false);
                break;
            case ConnectionStageKind.ProviderSlotOccupied:
                ShowNote(format.T("Connect_ProviderSlotBody"), NoteTone.Neutral, retry: false);
                break;
            case ConnectionStageKind.Reconnected:
            case ConnectionStageKind.ConnectedWithoutQuota:
            case ConnectionStageKind.Connected:
                // No confirmation step: the account and its usage simply appear on the dashboard.
                var added = Context.Usage.Current.Accounts.FirstOrDefault(a => a.Id == stage.AccountId);
                Context.Announcer.Announce(format.F("Connect_SignedIn", added?.Label ?? name));
                break;
        }
    }

    private void ShowNote(string note, NoteTone tone, bool retry)
    {
        Note = note;
        NoteSeverity = tone;
        CanRetry = retry && last is not null;
        Context.Announcer.Announce(note);
    }

    private void ClearNote()
    {
        Note = string.Empty;
        CanRetry = false;
    }
}
