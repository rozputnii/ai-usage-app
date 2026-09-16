using AiUsage.Features.Presentation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AiUsage.Features.Connection;

/// <summary>S03/S08 Add account sheet. The view model owns every state; code-behind forwards item clicks and focus only.</summary>
internal sealed partial class AddAccountDialog : ContentDialog
{
    private bool closingFromViewModel;

    public AddAccountDialog(AddAccountViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        viewModel.CloseRequested += OnCloseRequested;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AddAccountViewModel.IsCode) && viewModel.IsCode)
                DispatcherQueue.TryEnqueue(() => CodeBox.Focus(FocusState.Programmatic));
        };
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            viewModel.CloseRequested -= OnCloseRequested;
            // The transient manual code never outlives the sheet.
            viewModel.Code = string.Empty;
        };
    }

    public AddAccountViewModel ViewModel { get; }

    private Visibility ReconnectCollapsed(string? accountId) => accountId is null ? Visibility.Visible : Visibility.Collapsed;
    private string CodeBorder(bool error) => error ? "CritBrush" : "Stroke2Brush";
    private string ResultGlyphBrush(bool positive) => positive ? "OkBrush" : "Text2Brush";

    private void OnSignInChecked(object sender, RoutedEventArgs e) => ViewModel.Tab = AddAccountTab.SignIn;

    private void OnCliChecked(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Tab != AddAccountTab.ImportFromCli)
        {
            ViewModel.Tab = AddAccountTab.ImportFromCli;
            ViewModel.Cli.Open();
        }
    }

    private void OnProviderClick(object sender, RoutedEventArgs e) =>
        ViewModel.PickProviderCommand.Execute(((FrameworkElement)sender).Tag as ProviderOptionViewModel);

    private void OnMethodClick(object sender, RoutedEventArgs e) =>
        ViewModel.SelectMethodCommand.Execute(((FrameworkElement)sender).Tag as MethodOptionViewModel);

    private void OnSimulatorClick(object sender, RoutedEventArgs e) =>
        ViewModel.SimulateCommand.Execute(((FrameworkElement)sender).Tag as SimulatorOption);

    private void OnCodeKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && ViewModel.SubmitCodeCommand.CanExecute(null))
        {
            e.Handled = true;
            _ = ViewModel.SubmitCodeCommand.ExecuteAsync(null);
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        closingFromViewModel = true;
        Hide();
    }

    /// <summary>Esc behaves like the sheet's close button: running stages are abandoned without inventing an outcome.</summary>
    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (closingFromViewModel)
            return;
        args.Cancel = true;
        DispatcherQueue.TryEnqueue(() => ViewModel.CloseCommand.Execute(null));
    }
}
