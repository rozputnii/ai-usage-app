using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Recovery;

internal sealed partial class RecoveryView : UserControl
{
    public RecoveryView() => InitializeComponent();

    public RecoveryViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }

    private string MessageBackground(bool critical) => critical ? "CritBgBrush" : "Card2Brush";
    private string MessageBorder(bool critical) => critical ? "CritStrokeBrush" : "StrokeBrush";

    private void OnCheckpointClick(object sender, RoutedEventArgs e) =>
        _ = ViewModel?.RestoreCheckpointCommand.ExecuteAsync(((FrameworkElement)sender).Tag as CheckpointViewModel);
}
