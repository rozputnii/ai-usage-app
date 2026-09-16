using System.Collections.ObjectModel;
using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Demo;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Connection;

public enum ConnectStep { PickProvider, Method, Connecting, Waiting, Code, Verifying, Result }

public enum NoteTone { Neutral, Critical }

internal sealed partial class ProviderOptionViewModel(ProviderDescriptor descriptor, string availability) : ObservableObject
{
    public ProviderDescriptor Descriptor { get; } = descriptor;
    public string Name => Descriptor.Name;
    public string Glyph => Descriptor.Glyph;
    public string ProviderId => Descriptor.ProviderId;
    public string Availability { get; } = availability;
    public bool IsPlanned => Descriptor.Origin == CapabilityOrigin.Planned;
}

internal sealed partial class MethodOptionViewModel(ConnectionMethod method, string label) : ObservableObject
{
    public ConnectionMethod Method { get; } = method;
    public string Label { get; } = label;
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

internal sealed record SimulatorOption(DemoConnectOutcome Outcome, string Label);

/// <summary>
/// S03 Add account › Sign in: provider → method → Connecting… → Waiting for authorization (Cancel, "Enter a code
/// instead") → result. Deny/expire/cancel return to the method step with a note; cancellation is neutral. The manual
/// code is transient and cleared after submission.
/// </summary>
internal sealed partial class AddAccountViewModel : ObservableObject
{
    private readonly PresentationContext context;
    private readonly IConnectionFlow flow;
    private readonly DemoScenarioController? demo;
    private CancellationTokenSource? operation;

    public AddAccountViewModel(PresentationContext context, IConnectionFlow flow, CliImportViewModel cli, DemoScenarioController? demo = null)
    {
        this.context = context;
        this.flow = flow;
        this.demo = demo;
        Cli = cli;
        var format = context.Format;
        foreach (var provider in flow.Providers)
            ProviderOptions.Add(new(provider, format.T(provider.Origin == CapabilityOrigin.Planned ? "Connect_Planned" : "Connect_Available")));
        Simulators =
        [
            new(DemoConnectOutcome.Approve, format.T("Sim_Approve")),
            new(DemoConnectOutcome.ApproveWithoutQuota, format.T("Sim_ApproveNoQuota")),
            new(DemoConnectOutcome.Deny, format.T("Sim_Deny")),
            new(DemoConnectOutcome.Expire, format.T("Sim_Expire")),
            new(DemoConnectOutcome.Duplicate, format.T("Sim_Duplicate")),
        ];
    }

    public CliImportViewModel Cli { get; }
    public ObservableCollection<ProviderOptionViewModel> ProviderOptions { get; } = [];
    public ObservableCollection<MethodOptionViewModel> Methods { get; } = [];
    public IReadOnlyList<SimulatorOption> Simulators { get; }
    public bool HasSimulator => demo is not null;
    public string ProviderHelp => context.Format.T(HasSimulator ? "Connect_ProviderHelpDemo" : "Connect_PickHelpLive");

    [NotifyPropertyChangedFor(nameof(IsPick), nameof(IsMethod), nameof(IsConnecting), nameof(IsWaiting), nameof(IsCode), nameof(IsVerifying), nameof(IsResult), nameof(IsBusy))]
    [ObservableProperty] public partial ConnectStep Step { get; private set; }
    [ObservableProperty] public partial AddAccountTab Tab { get; set; }
    [ObservableProperty] public partial ProviderOptionViewModel? Provider { get; private set; }
    [NotifyPropertyChangedFor(nameof(StartLabel), nameof(IsManualMethod))]
    [ObservableProperty] public partial ConnectionMethod Method { get; private set; }
    [ObservableProperty] public partial string? ReconnectAccountId { get; private set; }
    [ObservableProperty] public partial string Title { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Note { get; private set; } = string.Empty;
    [ObservableProperty] public partial NoteTone NoteSeverity { get; private set; }
    [NotifyCanExecuteChangedFor(nameof(SubmitCodeCommand))]
    [ObservableProperty] public partial string Code { get; set; } = string.Empty;
    [ObservableProperty] public partial string CodeError { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ResultTitle { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ResultBody { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool ResultPositive { get; private set; }
    [ObservableProperty] public partial string? ResultAccountId { get; private set; }
    [ObservableProperty] public partial string WaitingText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string CodePrompt { get; private set; } = string.Empty;
    [ObservableProperty] public partial string ConnectingText { get; private set; } = string.Empty;

    public bool IsPick => Step == ConnectStep.PickProvider;
    public bool IsMethod => Step == ConnectStep.Method;
    public bool IsConnecting => Step == ConnectStep.Connecting;
    public bool IsWaiting => Step == ConnectStep.Waiting;
    public bool IsCode => Step == ConnectStep.Code;
    public bool IsVerifying => Step == ConnectStep.Verifying;
    public bool IsResult => Step == ConnectStep.Result;
    public bool IsBusy => Step is ConnectStep.Connecting or ConnectStep.Waiting or ConnectStep.Verifying;
    public bool IsManualMethod => Method == ConnectionMethod.ManualCode;
    public bool HasNote => Note.Length > 0;
    public bool HasCodeError => CodeError.Length > 0;
    public bool SupportsManualCode => Provider?.Descriptor.Methods.Contains(ConnectionMethod.ManualCode) == true;
    public string StartLabel => context.Format.T(Method == ConnectionMethod.ManualCode ? "Connect_Continue" : ReconnectAccountId is null ? "Action_Connect" : "Action_Reconnect");

    /// <summary>Raised when the sheet should close (Close, Done, Open account).</summary>
    public event EventHandler? CloseRequested;

    partial void OnNoteChanged(string value) => OnPropertyChanged(nameof(HasNote));
    partial void OnCodeErrorChanged(string value) => OnPropertyChanged(nameof(HasCodeError));
    partial void OnCodeChanged(string value)
    {
        if (CodeError.Length > 0)
            CodeError = string.Empty;
    }
    partial void OnProviderChanged(ProviderOptionViewModel? value) => OnPropertyChanged(nameof(SupportsManualCode));

    public void Open(AddAccountEntry entry)
    {
        Detach();
        Tab = entry.Tab;
        ReconnectAccountId = entry.ReconnectAccountId;
        Title = context.Format.T(entry.ReconnectAccountId is null ? "AddAccount_Title" : "AddAccount_ReconnectTitle");
        Note = string.Empty;
        Code = string.Empty;
        CodeError = string.Empty;
        if (entry.ProviderId is { } providerId && ProviderOptions.FirstOrDefault(p => p.ProviderId == providerId) is { } option)
            PickProvider(option);
        else
        {
            Provider = null;
            Step = ConnectStep.PickProvider;
        }
        OnPropertyChanged(nameof(StartLabel));
        if (entry.Tab == AddAccountTab.ImportFromCli)
            Cli.Open();
    }

    [RelayCommand]
    private void PickProvider(ProviderOptionViewModel? option)
    {
        if (option is null)
            return;
        Provider = option;
        Methods.Clear();
        foreach (var method in option.Descriptor.Methods)
            Methods.Add(new(method, context.Format.T("Method_" + method)));
        SelectMethod(Methods.FirstOrDefault());
        Note = string.Empty;
        var name = option.Name;
        WaitingText = context.Format.F("Connect_WaitingBody", name);
        CodePrompt = context.Format.F("Connect_CodePrompt", name);
        ConnectingText = context.Format.F("Connect_Connecting", name);
        Step = ConnectStep.Method;
    }

    [RelayCommand]
    private void SelectMethod(MethodOptionViewModel? option)
    {
        if (option is null)
            return;
        foreach (var method in Methods)
            method.IsSelected = method == option;
        Method = option.Method;
    }

    [RelayCommand]
    private void ChangeProvider()
    {
        if (IsBusy)
            return;
        Provider = null;
        Note = string.Empty;
        Step = ConnectStep.PickProvider;
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (Provider is null)
            return;
        if (Method == ConnectionMethod.ManualCode && !flow.ManualCodeUsesActiveConnection)
        {
            Note = string.Empty;
            Step = ConnectStep.Code;
            return;
        }
        await RunAsync(token => flow.ConnectAsync(Request(), token));
    }

    [RelayCommand]
    private void EnterCodeInstead()
    {
        if (!SupportsManualCode) return;
        if (!flow.ManualCodeUsesActiveConnection) Detach();
        Code = string.Empty;
        Step = ConnectStep.Code;
    }

    private bool CanSubmitCode() => Step == ConnectStep.Code;

    [RelayCommand(CanExecute = nameof(CanSubmitCode))]
    private async Task SubmitCodeAsync()
    {
        var code = Code.Trim();
        if (code.Length < 8)
        {
            CodeError = context.Format.T("Connect_CodeError");
            return;
        }
        // The code is used once and never stored: clear it before the request starts.
        Code = string.Empty;
        if (flow.ManualCodeUsesActiveConnection)
        {
            if (flow.TrySubmitCode(Request(), code)) Step = ConnectStep.Verifying;
            else CodeError = context.Format.T("Connect_CodeError");
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
        Code = string.Empty;
        Finish(new ConnectionStage(ConnectionStageKind.Cancelled));
    }

    [RelayCommand]
    private void Simulate(SimulatorOption? option)
    {
        if (option is not null)
            demo?.ResolveConnection(option.Outcome);
    }

    [RelayCommand]
    private void Close()
    {
        Detach();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenResult()
    {
        var id = ResultAccountId;
        CloseRequested?.Invoke(this, EventArgs.Empty);
        context.Navigation.Navigate(new(PageKey.Accounts, id));
    }

    /// <summary>Abandons the running stage stream without reporting its cancellation as a user-visible outcome.</summary>
    private void Detach()
    {
        var running = operation;
        operation = null;
        running?.Cancel();
    }

    private ConnectRequest Request() => new(Provider!.ProviderId, Method, ReconnectAccountId);

    private async Task RunAsync(Func<CancellationToken, IAsyncEnumerable<ConnectionStage>> start)
    {
        operation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        operation = cancellation;
        Note = string.Empty;
        try
        {
            await foreach (var stage in start(cancellation.Token).WithCancellation(cancellation.Token))
            {
                if (operation != cancellation)
                    return;
                switch (stage.Kind)
                {
                    case ConnectionStageKind.Connecting:
                        Step = ConnectStep.Connecting;
                        context.Announcer.Announce(ConnectingText);
                        break;
                    case ConnectionStageKind.WaitingForAuthorization:
                        Step = flow.ManualCodeUsesActiveConnection && Method == ConnectionMethod.ManualCode ? ConnectStep.Code : ConnectStep.Waiting;
                        context.Announcer.Announce(context.Format.T("Connect_WaitingTitle"));
                        break;
                    case ConnectionStageKind.Verifying:
                        Step = ConnectStep.Verifying;
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
        var format = context.Format;
        var name = Provider?.Name ?? string.Empty;
        switch (stage.Kind)
        {
            case ConnectionStageKind.Denied:
                ToMethod(format.T("Connect_Denied"), NoteTone.Critical);
                break;
            case ConnectionStageKind.Expired:
                ToMethod(format.T("Connect_Expired"), NoteTone.Critical);
                break;
            case ConnectionStageKind.Cancelled:
                ToMethod(format.T("Connect_Cancelled"), NoteTone.Neutral);
                break;
            case ConnectionStageKind.Failed:
                ToMethod(stage.Failure is { } failure ? format.T(failure.MessageKey) : format.T("Dialog_OperationFailed"), NoteTone.Critical);
                break;
            case ConnectionStageKind.Duplicate:
                var existing = context.Usage.Current.Accounts.FirstOrDefault(a => a.Id == stage.AccountId);
                Result(format.T("Connect_DuplicateTitle"), existing is null ? format.T("Connect_DuplicateBodyGeneric") : format.F("Connect_DuplicateBody", existing.Label), false, stage.AccountId);
                break;
            case ConnectionStageKind.ProviderSlotOccupied:
                Result(format.T("Connect_ProviderSlotTitle"), format.T("Connect_ProviderSlotBody"), false, stage.AccountId);
                break;
            case ConnectionStageKind.Reconnected:
                var reconnected = context.Usage.Current.Accounts.FirstOrDefault(a => a.Id == stage.AccountId);
                Result(format.T("Connect_Reconnected"), format.F("Connect_ReconnectedBody", reconnected?.Label ?? name), true, stage.AccountId);
                break;
            case ConnectionStageKind.ConnectedWithoutQuota:
                Result(format.T("Connect_ConnectedNoQuota"), format.T("Connect_ConnectedNoQuotaBody"), true, stage.AccountId);
                break;
            case ConnectionStageKind.Connected:
                var added = context.Usage.Current.Accounts.FirstOrDefault(a => a.Id == stage.AccountId);
                Result(format.T("Connect_Connected"), format.F("Connect_ConnectedBody", added?.Label ?? name), true, stage.AccountId);
                break;
        }
    }

    private void ToMethod(string note, NoteTone tone)
    {
        Note = note;
        NoteSeverity = tone;
        Step = Provider is null ? ConnectStep.PickProvider : ConnectStep.Method;
        context.Announcer.Announce(note);
    }

    private void Result(string title, string body, bool positive, string? accountId)
    {
        ResultTitle = title;
        ResultBody = body;
        ResultPositive = positive;
        ResultAccountId = accountId;
        Step = ConnectStep.Result;
        context.Announcer.Announce(title);
    }
}
