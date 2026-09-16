using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Settings.DataPrivacy;

internal sealed partial class DataPrivacyView : UserControl
{
    public DataPrivacyView() => InitializeComponent();

    public DataPrivacyViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }
}
