using AiUsage.Features.Settings.Monitoring;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Shell;

internal sealed partial class ToastPreview : UserControl
{
    public ToastPreview() => InitializeComponent();

    public ToastViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }
}
