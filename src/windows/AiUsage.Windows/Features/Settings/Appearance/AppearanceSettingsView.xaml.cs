using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AiUsage.Features.Settings.Appearance;

internal sealed partial class AppearanceSettingsView : UserControl
{
    public AppearanceSettingsView() => InitializeComponent();

    public AppearanceSettingsViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }


    /// <summary>Alt+↑/↓ on a focused order row moves the account, the keyboard equivalent of the ▲▼ buttons.</summary>
    private void OnOrderKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not OrderItemViewModel item)
            return;
        var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (!alt)
            return;
        if (e.Key == Windows.System.VirtualKey.Up && item.MoveUpCommand.CanExecute(null))
        {
            _ = item.MoveUpCommand.ExecuteAsync(null);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Down && item.MoveDownCommand.CanExecute(null))
        {
            _ = item.MoveDownCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }
}
