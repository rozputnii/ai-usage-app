using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Shell;

internal sealed partial class ConfirmDialog : ContentDialog
{
    private bool closingFromViewModel;

    public ConfirmDialog(ConfirmDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        ConfirmButton.Style = (Style)Application.Current.Resources[viewModel.IsDestructive ? "DestructiveButton" : "PrimaryButton"];
        Closing += OnClosing;
        Opened += (_, _) => (viewModel.HasTyped ? (Control)TypedBox : ConfirmButton).Focus(FocusState.Programmatic);
        _ = CloseWhenCompleteAsync();
    }

    public ConfirmDialogViewModel ViewModel { get; }

    private async Task CloseWhenCompleteAsync()
    {
        await ViewModel.Completion;
        closingFromViewModel = true;
        Hide();
    }

    /// <summary>Esc closes like Cancel, except while the confirmed action is running.</summary>
    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (closingFromViewModel)
            return;
        if (!ViewModel.TryDismiss())
            args.Cancel = true;
    }
}
