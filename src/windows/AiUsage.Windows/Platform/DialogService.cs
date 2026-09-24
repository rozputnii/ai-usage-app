using AiUsage.Features.Presentation;
using AiUsage.Features.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Platform;

/// <summary>
/// ContentDialog host for confirmations. WinUI allows one open dialog per window, so a
/// request arriving while another dialog is open waits for it to close instead of failing.
/// </summary>
internal sealed class DialogService : IDialogService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private FrameworkElement? root;
    private PresentationFormatter? format;

    public bool IsDialogOpen { get; private set; }

    public void Attach(FrameworkElement windowRoot, PresentationFormatter formatter)
    {
        root = windowRoot;
        format = formatter;
    }

    public async Task<ConfirmOutcome> ConfirmAsync(ConfirmRequest request)
    {
        if (root?.XamlRoot is null || format is null)
            return ConfirmOutcome.Cancelled;
        var viewModel = new ConfirmDialogViewModel(request, format);
        var dialog = new ConfirmDialog(viewModel);
        await ShowAsync(dialog);
        return viewModel.Completion.IsCompleted ? viewModel.Completion.Result : ConfirmOutcome.Cancelled;
    }

    private async Task ShowAsync(ContentDialog dialog)
    {
        await gate.WaitAsync();
        try
        {
            IsDialogOpen = true;
            dialog.XamlRoot = root!.XamlRoot;
            dialog.RequestedTheme = ElementTheme.Dark;
            await dialog.ShowAsync();
        }
        finally
        {
            IsDialogOpen = false;
            gate.Release();
        }
    }
}
