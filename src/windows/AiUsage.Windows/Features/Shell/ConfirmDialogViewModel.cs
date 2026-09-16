using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Shell;

/// <summary>
/// Confirm dialog state: plain, destructive, alternate action, typed confirmation and busy. The request's action runs
/// while the dialog stays open; an error keeps it open with the message; cancellation restores the prior state.
/// </summary>
internal sealed partial class ConfirmDialogViewModel : ObservableObject
{
    private readonly ConfirmRequest request;
    private readonly TaskCompletionSource<ConfirmOutcome> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConfirmDialogViewModel(ConfirmRequest request, PresentationFormatter format)
    {
        this.request = request;
        Title = request.Title;
        Body = request.Body;
        IdleConfirmLabel = request.ConfirmLabel;
        ConfirmLabel = request.ConfirmLabel;
        AlternateLabel = request.AlternateLabel ?? string.Empty;
        TypedConfirmation = request.TypedConfirmation ?? string.Empty;
        TypedPrompt = request.TypedConfirmation is null ? string.Empty : format.F("Dialog_TypeToConfirm", request.TypedConfirmation);
        CancelLabel = format.T("Dialog_Cancel");
        IsDestructive = request.Destructive;
    }

    public string Title { get; }
    public string Body { get; }
    public string AlternateLabel { get; }
    public string TypedConfirmation { get; }
    public string TypedPrompt { get; }
    public string CancelLabel { get; }
    public bool IsDestructive { get; }
    public bool HasAlternate => AlternateLabel.Length > 0;
    public bool HasTyped => TypedConfirmation.Length > 0;
    private string IdleConfirmLabel { get; }

    /// <summary>Completes when the dialog should close.</summary>
    public Task<ConfirmOutcome> Completion => completion.Task;

    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [ObservableProperty] public partial string TypedText { get; set; } = string.Empty;

    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand), nameof(AlternateCommand), nameof(CancelCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [ObservableProperty] public partial bool IsBusy { get; private set; }

    [ObservableProperty] public partial string ConfirmLabel { get; private set; } = string.Empty;
    [NotifyPropertyChangedFor(nameof(HasError))]
    [ObservableProperty] public partial string ErrorText { get; private set; } = string.Empty;

    public bool IsIdle => !IsBusy;
    public bool HasError => ErrorText.Length > 0;
    public bool ConfirmEnabled => CanConfirm();

    partial void OnTypedTextChanged(string value) => OnPropertyChanged(nameof(ConfirmEnabled));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(ConfirmEnabled));

    private bool CanConfirm() => !IsBusy && (!HasTyped || string.Equals(TypedText.Trim(), TypedConfirmation, StringComparison.Ordinal));

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private Task ConfirmAsync() => RunAsync(request.ConfirmAction, ConfirmOutcome.Confirmed);

    private bool CanAlternate() => !IsBusy && HasAlternate;

    [RelayCommand(CanExecute = nameof(CanAlternate))]
    private Task AlternateAsync() => RunAsync(request.AlternateAction, ConfirmOutcome.Alternate);

    private bool CanCancel() => !IsBusy;

    /// <summary>Cancel and Esc close without changes. While an action is running the dialog cannot be dismissed.</summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (!IsBusy)
            completion.TrySetResult(ConfirmOutcome.Cancelled);
    }

    public bool TryDismiss()
    {
        if (IsBusy)
            return false;
        completion.TrySetResult(ConfirmOutcome.Cancelled);
        return true;
    }

    private async Task RunAsync(Func<CancellationToken, Task<string?>>? action, ConfirmOutcome outcome)
    {
        if (action is null)
        {
            completion.TrySetResult(outcome);
            return;
        }
        ErrorText = string.Empty;
        IsBusy = true;
        ConfirmLabel = request.BusyLabel ?? IdleConfirmLabel;
        using var cancellation = new CancellationTokenSource();
        try
        {
            var error = await action(cancellation.Token);
            if (error is null)
            {
                completion.TrySetResult(outcome);
                return;
            }
            ErrorText = error;
        }
        finally
        {
            IsBusy = false;
            ConfirmLabel = IdleConfirmLabel;
        }
    }
}
