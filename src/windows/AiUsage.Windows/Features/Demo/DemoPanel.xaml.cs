using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Demo;

internal sealed partial class DemoPanel : UserControl
{
    public DemoPanel() => InitializeComponent();

    public DemoControlViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }
}
