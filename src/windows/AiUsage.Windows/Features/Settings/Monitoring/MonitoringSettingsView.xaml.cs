using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Settings.Monitoring;

internal sealed partial class MonitoringSettingsView : UserControl
{
    public MonitoringSettingsView() => InitializeComponent();

    public MonitoringSettingsViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }

    private void OnToggleAccount(object sender, RoutedEventArgs e) =>
        ViewModel?.ToggleAccountRulesCommand.Execute(((FrameworkElement)sender).Tag as AccountRulesViewModel);
}
